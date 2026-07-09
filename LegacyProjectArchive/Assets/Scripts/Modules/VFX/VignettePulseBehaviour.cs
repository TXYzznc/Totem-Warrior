using System;
using AttackSystem.Events;
using DG.Tweening;
using Tattoo;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tattoo.VFX
{
    /// <summary>
    /// HP &lt; 30% 时：屏幕边缘血色暗角 DOTween Yoyo 脉冲。
    ///
    /// 挂载到任意常驻 GameObject（如 HUD Root），Awake 自动查找或创建 Global Volume + Vignette Override。
    ///
    /// Variant 说明：无 multi_compile，0 额外 shader 变体。
    /// 后处理顺序：Vignette 在 URP Volume 栈内，位于 Bloom / ToneMapping 之前（由 URP 默认顺序保证）。
    /// 性能：DOTween Tween 仅在 pulse 期间存活；非危险状态下开销为零。
    /// Fallback：若 Volume 或 Vignette Override 不可用，Warn 日志后禁用自身，不抛异常。
    /// </summary>
    public sealed class VignettePulseBehaviour : MonoBehaviour
    {
        // ===== 配置 =====

        [Header("危险阈值")]
        [Range(0f, 1f)]
        [SerializeField] float _dangerRatio = 0.3f;

        [Header("Vignette 参数")]
        [SerializeField] float _intensityMax  = 0.45f;
        [SerializeField] float _pulseDuration = 1f;   // 单程时长（Yoyo 完整周期 = ×2）
        [SerializeField] float _fadeOutDuration = 0.4f;

        static readonly Color VignetteColor = new Color(0.8f, 0.05f, 0.05f); // #CC0D0D

        // ===== 运行时 =====

        Volume   _volume;
        Vignette _vignette;
        Tweener  _pulseTweener;
        IDisposable _eventSub;
        bool _isPulsing;

        // ===== Unity 生命周期 =====

        void Awake()
        {
            if (!TryAcquireVignette())
            {
                FrameworkLogger.Warn("VignettePulseBehaviour", "Action=Awake Result=VignetteNotFound — disabled");
                enabled = false;
                return;
            }

            // 初始状态：强度归零，不可见
            _vignette.intensity.value = 0f;
            _vignette.color.value     = VignetteColor;
        }

        void Start()
        {
            // 在 Start 订阅，确保 EventBus 已由 ModuleRunner 初始化
            var bus = FindEventBus();
            if (bus == null)
            {
                FrameworkLogger.Warn("VignettePulseBehaviour", "Action=Start Result=EventBusNotFound — disabled");
                enabled = false;
                return;
            }

            _eventSub = bus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            FrameworkLogger.Info("VignettePulseBehaviour", "Action=Start Result=Subscribed");
        }

        void OnDestroy()
        {
            _eventSub?.Dispose();
            _eventSub = null;
            KillPulseTween();
            // 还原 Vignette 强度，避免残留
            if (_vignette != null)
                _vignette.intensity.value = 0f;
        }

        // ===== 事件处理 =====

        void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
        {
            float ratio = e.Max > 0f ? e.Current / e.Max : 0f;

            if (ratio < _dangerRatio)
                StartPulse();
            else
                StopPulse();
        }

        // ===== Pulse 控制 =====

        void StartPulse()
        {
            if (_isPulsing) return;
            _isPulsing = true;

            KillPulseTween();

            // 从当前强度开始，避免跳变
            float startIntensity = _vignette != null ? _vignette.intensity.value : 0f;

            _pulseTweener = DOTween.To(
                    () => _vignette.intensity.value,
                    v  => _vignette.intensity.value = v,
                    _intensityMax,
                    _pulseDuration)
                .From(startIntensity)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);

            FrameworkLogger.Info("VignettePulseBehaviour", "Action=StartPulse IntensityMax=" + _intensityMax);
        }

        void StopPulse()
        {
            if (!_isPulsing) return;
            _isPulsing = false;

            KillPulseTween();

            // 淡出到 0
            if (_vignette != null && _vignette.intensity.value > 0f)
            {
                _pulseTweener = DOTween.To(
                        () => _vignette.intensity.value,
                        v  => _vignette.intensity.value = v,
                        0f,
                        _fadeOutDuration)
                    .SetEase(Ease.OutSine)
                    .SetLink(gameObject);
            }

            FrameworkLogger.Info("VignettePulseBehaviour", "Action=StopPulse FadeOut=" + _fadeOutDuration);
        }

        void KillPulseTween()
        {
            if (_pulseTweener != null && _pulseTweener.IsActive())
                _pulseTweener.Kill();
            _pulseTweener = null;
        }

        // ===== Volume / Vignette 获取 =====

        /// <summary>
        /// 查找场景中的 Global Volume；若不存在则自动创建一个并添加 Vignette Override。
        /// 成功返回 true，失败返回 false（脚本将被禁用）。
        /// </summary>
        bool TryAcquireVignette()
        {
            // 1. 找已有的 Global Volume
            var allVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var vol in allVolumes)
            {
                if (!vol.isGlobal) continue;
                if (vol.profile != null && vol.profile.TryGet(out _vignette))
                {
                    _volume = vol;
                    return true;
                }
            }

            // 2. 有 Global Volume 但 Profile 里没有 Vignette Override → 尝试添加
            foreach (var vol in allVolumes)
            {
                if (!vol.isGlobal || vol.profile == null) continue;
                _vignette = vol.profile.Add<Vignette>(overrides: true);
                _volume   = vol;
                FrameworkLogger.Info("VignettePulseBehaviour", "Action=AddVignetteOverride Volume=" + vol.name);
                return true;
            }

            // 3. 没有任何 Global Volume → 自动创建
            var go      = new GameObject("VignetteVolume [AutoCreated]");
            _volume      = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10;
            var profile  = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = profile;
            _vignette    = profile.Add<Vignette>(overrides: true);
            FrameworkLogger.Info("VignettePulseBehaviour", "Action=CreateVolume Name=VignetteVolume");
            return true;
        }

        /// <summary>
        /// 通过 GameApp.TryGetRuntime 拿 EventBus。
        /// 若找不到则返回 null。
        /// </summary>
        static EventBus FindEventBus()
        {
            var app = FindFirstObjectByType<GameApp>();
            if (app == null) return null;
            app.TryGetRuntime(out var bus, out _);
            return bus;
        }
    }
}
