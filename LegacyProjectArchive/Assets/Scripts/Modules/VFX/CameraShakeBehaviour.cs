/// <summary>
/// CameraShakeBehaviour — 挂在与 Camera.main 同级的 GameObject 上（运行时 FindOrCreate 附加）。
/// 订阅 WeaponAttackHitEvent，使用 DOTween DOShakePosition 抖动相机。
///
/// 设计决策：
/// - Camera 原始 localPosition 在 Awake / Initialize 时记录，shake 结束后强制归位。
/// - 新 shake 到来时先 Kill 当前 Tween 再播，防止位移叠加。
/// - 若 Camera.main == null，静默跳过，不抛异常。
///
/// 普通：amplitude=0.05  duration=0.12s  vibrato=10  randomness=90
/// 暴击：amplitude=0.1   duration=0.18s  vibrato=10  randomness=90
///
/// Variant 说明：无 multi_compile，0 额外变体开销。
/// </summary>

using System;
using DG.Tweening;
using UnityEngine;
using Weapon.Events;

namespace Tattoo.VFX
{
    public sealed class CameraShakeBehaviour : MonoBehaviour
    {
        // ===== 参数 =====
        const float NormalAmplitude  = 0.05f;
        const float NormalDuration   = 0.12f;
        const int   NormalVibrato    = 10;
        const float NormalRandomness = 90f;

        const float CritAmplitude   = 0.1f;
        const float CritDuration    = 0.18f;
        const int   CritVibrato     = 10;
        const float CritRandomness  = 90f;

        // ===== 内部状态 =====
        EventBus   _bus;
        IDisposable _sub;
        Tween      _currentTween;
        Vector3    _originalLocalPos;
        Camera     _cam;

        // ===== 工厂方法：FindOrCreate 附加到 Camera.main 同级 =====

        /// <summary>
        /// 确保 CameraShakeBehaviour 存在并注入 EventBus。
        /// 若 Camera.main 为 null，返回 null（调用方自行处理）。
        /// </summary>
        public static CameraShakeBehaviour FindOrCreate(EventBus bus)
        {
            if (Camera.main == null)
            {
                FrameworkLogger.Warn("CameraShakeBehaviour",
                    "Action=FindOrCreate Camera.main=null — 相机震动跳过初始化");
                return null;
            }

            // 先看是否已存在（避免重复挂载）
            var existing = Camera.main.GetComponent<CameraShakeBehaviour>();
            if (existing != null)
            {
                existing.Reinitialize(bus);
                return existing;
            }

            var behaviour = Camera.main.gameObject.AddComponent<CameraShakeBehaviour>();
            behaviour.Initialize(bus);
            return behaviour;
        }

        // ===== 初始化 =====

        void Initialize(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _cam = Camera.main;
            _originalLocalPos = _cam != null ? _cam.transform.localPosition : Vector3.zero;
            _sub = _bus.Subscribe<WeaponAttackHitEvent>(OnHit);
            FrameworkLogger.Info("CameraShakeBehaviour",
                $"Action=Initialized Camera={(_cam != null ? _cam.name : "null")} OriginalLocalPos={_originalLocalPos}");
        }

        void Reinitialize(EventBus bus)
        {
            // 已存在时更新订阅（避免重复订阅）
            _sub?.Dispose();
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _cam = Camera.main;
            _originalLocalPos = _cam != null ? _cam.transform.localPosition : Vector3.zero;
            _sub = _bus.Subscribe<WeaponAttackHitEvent>(OnHit);
        }

        void OnDestroy()
        {
            _sub?.Dispose();
            _sub = null;
            _currentTween?.Kill();
            _currentTween = null;
            // 还原相机位置
            if (_cam != null)
                _cam.transform.localPosition = _originalLocalPos;
        }

        // ===== 事件处理 =====

        void OnHit(WeaponAttackHitEvent e)
        {
            if (_cam == null)
            {
                // 运行时 Camera 已销毁
                _cam = Camera.main;
                if (_cam == null) return;
            }

            // Kill 当前 tween 并立刻归位，防止偏移叠加
            _currentTween?.Kill(complete: false);
            _cam.transform.localPosition = _originalLocalPos;

            bool isCrit      = e.IsCrit;
            float amplitude  = isCrit ? CritAmplitude  : NormalAmplitude;
            float duration   = isCrit ? CritDuration   : NormalDuration;
            int   vibrato    = isCrit ? CritVibrato     : NormalVibrato;
            float randomness = isCrit ? CritRandomness  : NormalRandomness;

            _currentTween = _cam.transform
                .DOShakePosition(duration, amplitude, vibrato, randomness, snapping: false, fadeOut: true)
                .SetUpdate(UpdateType.Normal)
                .OnComplete(OnShakeComplete);
        }

        void OnShakeComplete()
        {
            // shake 结束后强制归位，消除 DOTween fadeOut 残留偏移
            if (_cam != null)
                _cam.transform.localPosition = _originalLocalPos;
            _currentTween = null;
        }
    }
}
