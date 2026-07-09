using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Tattoo.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 自助纹身/出招面板（UGUI v2.1）。
    /// 实现 IExclusiveUIForm 参与覆盖层互斥管理。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/SelfTattoo.prefab
    /// 触发条件：玩家按 Tab 键（InputModule.IsSelfTattooTogglePressed），自行轮询开关，不走 NPC 互动事件。
    /// </summary>
    public sealed class SelfTattooForm : MonoBehaviour, IExclusiveUIForm, IUIFormBootstrap
    {
        [Header("面板根节点")]
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("标题/关闭")]
        [SerializeField] TMP_Text _titleText;
        [SerializeField] Button   _closeBtn;

        [Header("部位选择（人体轮廓上的 6 个热点按钮，按 TattooPartConfig Id 1-6 顺序）")]
        [SerializeField] Button[]   _partButtons;
        [SerializeField] TMP_Text   _selectedPartText;

        [Header("颜色选择（按 TattooColorConfig Id 1-7 顺序：红黄绿蓝紫金白）")]
        [SerializeField] Button[]   _colorButtons;
        [SerializeField] Image      _colorSelectedGlow;

        [Header("图案选择（按 TattooPatternConfig Id 1-8 顺序）")]
        [SerializeField] Button[]   _patternButtons;

        [Header("预览/读条")]
        [SerializeField] TMP_Text _previewText;
        [SerializeField] TMP_Text _readingTimeText;
        [SerializeField] Image    _hourglassIcon;

        [Header("操作按钮")]
        [SerializeField] Button _startButton;
        [SerializeField] TMP_Text _startButtonText;
        [SerializeField] Button _cancelButton;

        // ── 运行时 ────────────────────────────────────────────
        EventBus     _bus;
        ModuleRunner _runner;
        bool         _isOpen;

        int _selectedPartId = -1;
        int _selectedColorId = -1;
        int _selectedPatternId = -1;

        readonly List<IDisposable> _subs = new();

        // ── IExclusiveUIForm ──────────────────────────────────

        public bool IsOpen => _isOpen;

        public GameObject GameObject => gameObject;

        public void ForceClose()
        {
            _isOpen = false;
            gameObject.SetActive(false);
            FrameworkLogger.Info("SelfTattooForm", "Action=ForceClose");
        }

        // ── IUIForm ───────────────────────────────────────────

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.InGame && _isOpen)
                ForceClose();
        }

        // ── MonoBehaviour ─────────────────────────────────────

        void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Bootstrap(EventBus bus, ModuleRunner runner)
        {
            // UIModule 在 EarlyRegister 之后同步调用：Form 起始 inactive，Start() / Update() 都不会跑，
            // 所以 Tab 切换不能再走 Update 轮询，必须订阅 CombatModule 桥接的 SelfTattooToggleRequestedEvent。
            _bus = bus;
            _runner = runner;

            _closeBtn?.onClick.AddListener(Close);
            BindPartButtons();
            BindColorButtons();
            BindPatternButtons();
            _startButton?.onClick.AddListener(OnStartClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);

            SubscribeEvents();
            RefreshPreview();
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
            _subs.Add(_bus.Subscribe<TattooInProgressEvent>(e => RefreshReadingState(true, e.DurationSec)));
            _subs.Add(_bus.Subscribe<TattooFinishedEvent>(_ => RefreshReadingState(false, 0f)));
            _subs.Add(_bus.Subscribe<TattooCancelledEvent>(_ => RefreshReadingState(false, 0f)));
            _subs.Add(_bus.Subscribe<SelfTattooToggleRequestedEvent>(_ => { if (_isOpen) Close(); else Open(); }));
        }

        // ── 选择绑定 ──────────────────────────────────────────

        void BindPartButtons()
        {
            if (_partButtons == null) return;
            for (int i = 0; i < _partButtons.Length; i++)
            {
                int partId = i + 1; // TattooPartConfig Id 从 1 开始
                _partButtons[i]?.onClick.AddListener(() => SelectPart(partId));
            }
        }

        void BindColorButtons()
        {
            if (_colorButtons == null) return;
            for (int i = 0; i < _colorButtons.Length; i++)
            {
                int colorId = i + 1; // TattooColorConfig Id 从 1 开始
                _colorButtons[i]?.onClick.AddListener(() => SelectColor(colorId));
            }
        }

        void BindPatternButtons()
        {
            if (_patternButtons == null) return;
            for (int i = 0; i < _patternButtons.Length; i++)
            {
                int patternId = i + 1; // TattooPatternConfig Id 从 1 开始
                _patternButtons[i]?.onClick.AddListener(() => SelectPattern(patternId));
            }
        }

        void SelectPart(int partId)
        {
            _selectedPartId = partId;
            RefreshPreview();
        }

        void SelectColor(int colorId)
        {
            _selectedColorId = colorId;
            if (_colorSelectedGlow != null && _colorButtons != null && colorId - 1 < _colorButtons.Length)
                _colorSelectedGlow.transform.position = _colorButtons[colorId - 1].transform.position;
            RefreshPreview();
        }

        void SelectPattern(int patternId)
        {
            _selectedPatternId = patternId;
            RefreshPreview();
        }

        void RefreshPreview()
        {
            bool ready = _selectedPartId > 0 && _selectedColorId > 0 && _selectedPatternId > 0;
            if (_startButton != null) _startButton.interactable = ready;
            if (_previewText != null)
            {
                _previewText.text = ready
                    ? $"部位{_selectedPartId} × 颜色{_selectedColorId} × 图案{_selectedPatternId}"
                    : "请选择部位 / 颜色 / 图案";
            }
        }

        // ── 读条状态 ──────────────────────────────────────────

        void RefreshReadingState(bool inProgress, float duration)
        {
            if (_readingTimeText != null)
                _readingTimeText.text = inProgress ? $"{duration:F1}s" : string.Empty;
            if (_startButton != null) _startButton.gameObject.SetActive(!inProgress);
            if (_cancelButton != null) _cancelButton.gameObject.SetActive(inProgress);
        }

        // ── 操作回调 ──────────────────────────────────────────

        void OnStartClicked()
        {
            if (_selectedPartId <= 0 || _selectedColorId <= 0 || _selectedPatternId <= 0) return;

            var spawner = _runner?.GetModule<SpawnerModule>();
            var actor = spawner?.PlayerTarget;
            if (actor == null)
            {
                FrameworkLogger.Warn("SelfTattooForm", "Action=OnStartClicked PlayerTarget=null");
                return;
            }

            _runner.GetModule<TattooModule>().StartSelfTattoo(actor, _selectedPartId, _selectedColorId, _selectedPatternId);
        }

        void OnCancelClicked()
        {
            var spawner = _runner?.GetModule<SpawnerModule>();
            var actor = spawner?.PlayerTarget;
            if (actor == null) return;

            _runner.GetModule<TattooModule>().CancelSelfTattoo(actor, CancelReason.UserAbort);
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
    }
}
