using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MapGen;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 2.5D 正交俯角相机模块。
    ///
    /// 职责：
    ///   - 接管/新建 MainCamera，设正交投影 + 俯角参数
    ///   - LateUpdate 平滑跟随玩家（SmoothDamp + 死区 + lookahead + 边界 clamp）
    ///   - 提供 PlayShake(duration, magnitude) 供 VFXModule 调用（衰减正弦）
    ///   - 暴露 CameraTiltX / OrthographicSize 供 BillboardSprite 读取
    ///
    /// 全程 0 GC alloc（LateUpdate 内 Vector3 struct + 缓存 velocity 字段，无 new / 无闭包）。
    /// InitializeAsync 不发事件（遵循 IGameModule 约束）。
    ///
    /// change #25：2.5D 相机系统
    /// </summary>
    public sealed class CameraModule : IGameModule, ILateTickable
    {
        // ── IGameModule ──────────────────────────────────────────────────
        public int    ModuleCategory => 2;
        public Type[] Dependencies   => new[] { typeof(SpawnerModule), typeof(MapGenModule) };

        // ── 构造 ────────────────────────────────────────────────────────
        readonly ModuleRunner _runner;
        readonly EventBus     _bus;

        public CameraModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        // ── 相机参数（可调） ────────────────────────────────────────────
        /// <summary>相机俯角（度）。BillboardSprite 读此值校正 sprite X 轴旋转。</summary>
        public float CameraTiltX      { get; private set; } = 55f;

        /// <summary>正交视野半高（世界单位）。playtest 阶段可调。</summary>
        public float OrthographicSize { get; private set; } = 9f;

        // ── 跟随参数（playtest 阶段按截图微调，此处为默认值）──────────
        float _smoothTime      = 0.15f;
        float _deadZoneHalfW   = 1.5f;   // 死区半宽（屏幕平面世界单位）
        float _deadZoneHalfH   = 1.0f;   // 死区半高
        float _lookaheadDist   = 3.0f;   // 最大预偏距离
        float _lookaheadSmooth = 0.3f;   // lookahead 自身平滑时间
        float _boundaryMargin  = 10f;    // 边界 clamp margin（世界单位）

        // ── 私有运行时状态 ───────────────────────────────────────────────
        Camera        _cam;
        SpawnerModule _spawner;
        MapGenModule  _mapGen;

        // 跟随
        Vector3 _basePos;            // 当前基准位（不含震动）
        Vector3 _followVelocity;     // SmoothDamp ref（缓存字段，0 GC）
        Vector3 _lookaheadTarget;    // lookahead 目标（平滑后）
        Vector3 _lookaheadVelocity;  // lookahead SmoothDamp ref

        Vector3 _prevPlayerPos;      // 上一帧玩家位置（算移动方向）
        bool    _prevPlayerPosValid;

        // 世界边界（Init 时一次算好）
        float _worldHalf;  // MapSize/2 - margin

        // 相机到焦点距离（俯角固定时常量，Init 时算）
        float _camDistance; // 沿相机 forward 方向的距离

        // 震动
        float _shakeTimeLeft;
        float _shakeDuration;
        float _shakeMag;

        // ── 初始化 ────────────────────────────────────────────────────────
        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            // 1. 缓存依赖模块
            _spawner = _runner.GetModule<SpawnerModule>();
            _mapGen  = _runner.GetModule<MapGenModule>();

            // 2. 接管或新建相机
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("MainCamera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
            }

            // 3. 设正交参数
            _cam.orthographic      = true;
            _cam.orthographicSize  = OrthographicSize;
            _cam.nearClipPlane     = 0.1f;
            _cam.farClipPlane      = 100f;
            _cam.clearFlags        = CameraClearFlags.SolidColor;
            _cam.backgroundColor   = new Color(0.18f, 0.18f, 0.22f);
            _cam.transform.eulerAngles = new Vector3(CameraTiltX, 0f, 0f);

            // 4. 算边界（MapSize/2 - margin）
            float mapHalf = _mapGen != null ? _mapGen.MapSize * 0.5f : 75f;
            _worldHalf = mapHalf - _boundaryMargin;

            // 5. 算相机到焦点的距离（俯角固定时常量：相机在焦点正上方 + 斜向偏移）
            //    俯角 55° → sin55° ≈ 0.819，cos55° ≈ 0.574
            //    沿相机 forward 方向单位向量 = (0, -sin55, cos55)（负 Z 方向）
            //    取适当高度 H，使相机看向 Y=0 焦点时高度合理；这里用 MapSize/4 高度
            //    camDistance = H / sin(tiltRad) 使 cam.pos = focus + cam.backward * camDist
            float tiltRad = CameraTiltX * Mathf.Deg2Rad;
            float targetHeight = 18f; // 相机世界高度目标（与原 Spawner 保持一致）
            _camDistance = targetHeight / Mathf.Sin(tiltRad);

            // 6. 初始位置：对准玩家
            if (_spawner != null && _spawner.Player != null)
            {
                Vector3 playerPos = _spawner.Player.transform.position;
                Vector3 focus = new Vector3(playerPos.x, 0f, playerPos.z);
                _basePos = FocusToCamera(focus);
                _prevPlayerPos = playerPos;
                _prevPlayerPosValid = true;
            }
            else
            {
                // fallback
                _basePos = new Vector3(0f, targetHeight, -_camDistance * Mathf.Cos(tiltRad));
            }

            _cam.transform.position = _basePos;

            // 7. lookahead 初始化
            _lookaheadTarget   = Vector3.zero;
            _lookaheadVelocity = Vector3.zero;

            // 不发事件（遵循 IGameModule 约束）
            FrameworkLogger.Info("CameraModule",
                $"Action=Initialized orthographic=true orthographicSize={OrthographicSize} tilt={CameraTiltX} camPos={_basePos}");

            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _cam = null;
            _spawner = null;
            _mapGen  = null;
            _shakeTimeLeft = 0f;
            FrameworkLogger.Info("CameraModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ── ILateTickable —— 每帧跟随主循环（§七 8 步）────────────────────
        public void OnLateUpdate(float dt)
        {
            if (_cam == null || _spawner == null || _spawner.Player == null) return;

            // 1. 读玩家位置
            Vector3 playerPos = _spawner.Player.transform.position;
            Vector3 playerXZ  = new Vector3(playerPos.x, 0f, playerPos.z);

            // 2. 死区检测
            //    当前焦点（_basePos 对应的 Y=0 落点）
            Vector3 currentFocus = CameraToFocus(_basePos);

            float dx = playerXZ.x - currentFocus.x;
            float dz = playerXZ.z - currentFocus.z;
            Vector3 targetFocus;
            if (Mathf.Abs(dx) <= _deadZoneHalfW && Mathf.Abs(dz) <= _deadZoneHalfH)
            {
                // 玩家在死区内，焦点不动
                targetFocus = currentFocus;
            }
            else
            {
                // 把玩家拉回死区边缘
                float cx = playerXZ.x - Mathf.Clamp(dx, -_deadZoneHalfW, _deadZoneHalfW);
                float cz = playerXZ.z - Mathf.Clamp(dz, -_deadZoneHalfH, _deadZoneHalfH);
                targetFocus = new Vector3(cx, 0f, cz);
            }

            // 3. lookahead：按玩家移动方向预偏
            Vector3 moveDir = Vector3.zero;
            if (_prevPlayerPosValid)
            {
                Vector3 rawMove = playerXZ - new Vector3(_prevPlayerPos.x, 0f, _prevPlayerPos.z);
                if (rawMove.sqrMagnitude > 1e-6f)
                    moveDir = rawMove.normalized;
            }
            _prevPlayerPos = playerPos;
            _prevPlayerPosValid = true;

            Vector3 lookaheadGoal = moveDir * _lookaheadDist;
            // SmoothDamp lookahead（平滑转向，避免瞬跳）
            _lookaheadTarget = Vector3.SmoothDamp(
                _lookaheadTarget, lookaheadGoal, ref _lookaheadVelocity, _lookaheadSmooth);

            targetFocus.x += _lookaheadTarget.x;
            targetFocus.z += _lookaheadTarget.z;

            // 4. 换算为相机基准位
            Vector3 basePosTarget = FocusToCamera(targetFocus);

            // 5. SmoothDamp 平滑
            _basePos = Vector3.SmoothDamp(_basePos, basePosTarget, ref _followVelocity, _smoothTime);

            // 6. 边界 clamp（限制焦点落在世界 bbox 内）
            Vector3 clampedFocus = CameraToFocus(_basePos);
            clampedFocus.x = Mathf.Clamp(clampedFocus.x, -_worldHalf, _worldHalf);
            clampedFocus.z = Mathf.Clamp(clampedFocus.z, -_worldHalf, _worldHalf);
            _basePos = FocusToCamera(clampedFocus);

            // 7. 叠震动偏移
            Vector3 shakeOffset = Vector3.zero;
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= dt;
                if (_shakeTimeLeft < 0f) _shakeTimeLeft = 0f;

                float elapsed = _shakeDuration - _shakeTimeLeft;
                float decay   = 1f - elapsed / _shakeDuration;
                const float freq = 40f;
                float sx = Mathf.Sin(elapsed * freq * 1.1f) * _shakeMag * decay;
                float sy = Mathf.Sin(elapsed * freq * 0.9f + 1.3f) * _shakeMag * decay;
                // 相机局部 XY 偏移（相机朝向固定，局部 X = 世界 X，局部 Y = 世界 Y 近似）
                shakeOffset = _cam.transform.right * sx + _cam.transform.up * sy;
            }

            // 8. 写入相机位置（不改旋转）
            _cam.transform.position = _basePos + shakeOffset;
        }

        // ── PlayShake ───────────────────────────────────────────────────
        /// <summary>触发一次震动。供 VFXModule 在打击反馈时调用。0 GC alloc。</summary>
        public void PlayShake(float duration, float magnitude)
        {
            if (duration <= 0f) return;
            _shakeDuration = duration;
            _shakeMag      = magnitude;
            _shakeTimeLeft = duration;
        }

        // ── 工具：焦点 ↔ 相机位换算 ───────────────────────────────────────
        /// <summary>
        /// 由焦点（XZ 平面 Y=0）反算相机世界位。
        /// 相机 forward = (0, -sinTilt, cosTilt)，
        /// 相机位 = focus - forward * camDistance
        ///        = focus + backward * camDistance
        /// </summary>
        Vector3 FocusToCamera(Vector3 focus)
        {
            float tiltRad = CameraTiltX * Mathf.Deg2Rad;
            float sinT = Mathf.Sin(tiltRad);
            float cosT = Mathf.Cos(tiltRad);
            // camera forward in world: (0, -sinT, cosT)，所以 backward = (0, sinT, -cosT)
            return new Vector3(
                focus.x,
                focus.y + sinT * _camDistance,
                focus.z - cosT * _camDistance
            );
        }

        /// <summary>
        /// 由相机世界位反算焦点（Y=0 平面投影）。
        /// focus.xz = cam.xz + forward.xz * camDistance
        ///          = cam.xz + (0, cosT) * camDistance
        /// </summary>
        Vector3 CameraToFocus(Vector3 camPos)
        {
            float tiltRad = CameraTiltX * Mathf.Deg2Rad;
            float cosT    = Mathf.Cos(tiltRad);
            return new Vector3(
                camPos.x,
                0f,
                camPos.z + cosT * _camDistance
            );
        }
    }
}
