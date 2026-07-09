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
    /// 商店面板（UGUI v2.1）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/Shop.prefab
    /// 触发条件：NPCInteractStartEvent（NPC=Shop）
    /// </summary>
    public sealed class ShopForm : MonoBehaviour, IExclusiveUIForm
    {
        [Header("面板根节点")]
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("金币文本")]
        [SerializeField] TMP_Text _coinText;

        [Header("库存格容器（GridLayoutGroup）")]
        [SerializeField] Transform _inventoryRoot;

        [Header("按钮")]
        [SerializeField] Button _closeBtn;

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
        }

        // ── IUIForm ───────────────────────────────────────────

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.InGame && _isOpen) ForceClose();
        }

        // ── MonoBehaviour ─────────────────────────────────────

        void Awake() => gameObject.SetActive(false);

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
                if (e.Npc.Type == NPCType.Merchant) Open();
            }));
            _subs.Add(_bus.Subscribe<ShopClosedEvent>(_ => Close()));
            _subs.Add(_bus.Subscribe<ShopPurchaseEvent>(_ => RefreshInventory()));
            _subs.Add(_bus.Subscribe<ShopRefreshEvent>(_ => RefreshInventory()));
            _subs.Add(_bus.Subscribe<CoinChangedEvent>(e =>
            {
                if (_coinText) _coinText.SetText("{0}", e.NewTotal);
            }));
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

        void RefreshInventory()
        {
            // 占位：后续迭代接入 ShopModule 库存数据
        }
    }
}
