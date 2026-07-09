using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Economy.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 纹身工作室面板（UGUI v2.1）。
    /// 实现 IExclusiveUIForm 参与覆盖层互斥管理。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/TattooStudio.prefab
    /// 触发条件：NPCInteractStartEvent（NPC=TattooArtist）
    /// </summary>
    public sealed class TattooStudioForm : MonoBehaviour, IExclusiveUIForm
    {
        [Header("面板根节点")]
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("按钮")]
        [SerializeField] Button _closeBtn;

        [Header("Build 预览区")]
        [SerializeField] Transform _buildPreviewRoot;

        // ── 运行时 ────────────────────────────────────────────
        EventBus     _bus;
        ModuleRunner _runner;
        bool         _isOpen;

        readonly List<IDisposable> _subs = new();

        // ── IExclusiveUIForm ──────────────────────────────────

        public bool IsOpen => _isOpen;

        public GameObject GameObject => gameObject;

        public void ForceClose()
        {
            _isOpen = false;
            gameObject.SetActive(false);
            FrameworkLogger.Info("TattooStudioForm", "Action=ForceClose");
        }

        // ── IUIForm ───────────────────────────────────────────

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // 非 InGame 时强制关闭
            if (newState != GameState.InGame && _isOpen)
                ForceClose();
        }

        // ── MonoBehaviour ─────────────────────────────────────

        void Awake()
        {
            gameObject.SetActive(false);
        }

        async void Start()
        {
            GameApp app = null;
            float timeout = Time.unscaledTime + 10f;
            while (Time.unscaledTime < timeout)
            {
                app = FindObjectOfType<GameApp>();
                if (app != null && app.TryGetRuntime(out _bus, out _runner)) break;
                await UniTask.Yield();
            }
            if (_bus == null) return;

            _runner.GetModule<UIModule>().Register(this);
            _closeBtn?.onClick.AddListener(Close);
            SubscribeEvents();
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            DOTween.Kill(transform);
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }

        // ── 事件订阅 ──────────────────────────────────────────

        void SubscribeEvents()
        {
            _subs.Add(_bus.Subscribe<NPCInteractStartEvent>(e =>
            {
                if (e.Npc.Type == NPCType.Tattooist) Open();
            }));
            _subs.Add(_bus.Subscribe<TattooSessionEndEvent>(_ => Close()));
            _subs.Add(_bus.Subscribe<TattooEquippedEvent>(_ => RefreshBuildPreview()));
        }

        // ── Open / Close ──────────────────────────────────────

        void Open()
        {
            _runner.GetModule<UIModule>().RequestOpenExclusive(this);
            gameObject.SetActive(true);
            _isOpen = true;
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }
        }

        void Close()
        {
            _isOpen = false;
            if (_canvasGroup)
                _canvasGroup.DOFade(0f, 0.2f).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
            _runner?.GetModule<UIModule>().CloseCurrentExclusive();
        }

        void RefreshBuildPreview()
        {
            // 占位：后续迭代接入 TattooModule 槽位数据
        }
    }
}
