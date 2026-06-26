using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 设置面板（UGUI v1.0）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/Settings.prefab
    /// 触发条件：MainMenuForm / PauseMenuForm 的「设置」按钮 → Open()
    /// 覆盖层互斥：实现 IExclusiveUIForm（SortOrder=20）
    ///
    /// 范围（v1.0）：
    ///   - ✅ BGM / SFX 音量（拖动即时生效）
    ///   - ✅ 画质三档（低/中/高）
    ///   - ⏸ 按键重绑定（UI 展示静态文本，按钮 idle 不响应；待 InputModule 升级 New Input System 后接通）
    ///
    /// 生命周期：
    ///   Open() → BeginEdit → 控件填值
    ///   滑条 / 画质切换 → Preview(draft)
    ///   保存按钮 → Commit → Close
    ///   取消按钮 / X → Rollback → Close
    /// </summary>
    public sealed class SettingsForm : MonoBehaviour, IExclusiveUIForm
    {
        // ── Inspector 引用（按标注稿 §2 PascalCase 命名）────────────────

        [Header("面板")]
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("音量区")]
        [SerializeField] Slider     _sliderBGM;
        [SerializeField] TMP_Text   _valueTextBGM;
        [SerializeField] Slider     _sliderSFX;
        [SerializeField] TMP_Text   _valueTextSFX;

        [Header("画质区（Radio 按钮组）")]
        [SerializeField] Button     _radioBtnLow;
        [SerializeField] Image      _radioCircleLow;
        [SerializeField] Image      _radioDotLow;
        [SerializeField] TMP_Text   _radioLabelLow;
        [SerializeField] Button     _radioBtnMed;
        [SerializeField] Image      _radioCircleMed;
        [SerializeField] Image      _radioDotMed;
        [SerializeField] TMP_Text   _radioLabelMed;
        [SerializeField] Button     _radioBtnHigh;
        [SerializeField] Image      _radioCircleHigh;
        [SerializeField] Image      _radioDotHigh;
        [SerializeField] TMP_Text   _radioLabelHigh;

        [Header("按键重绑定区（v1.0 仅展示，按钮不响应）")]
        [SerializeField] Button     _keyBindButtonMove;
        [SerializeField] TMP_Text   _keyBindTextMove;
        [SerializeField] Button     _keyBindButtonAttack;
        [SerializeField] TMP_Text   _keyBindTextAttack;
        [SerializeField] Button     _keyBindButtonPause;
        [SerializeField] TMP_Text   _keyBindTextPause;
        [SerializeField] TMP_Text   _keyBindSectionNotice;  // 「即将推出」提示，Section header 下方

        [Header("底部按钮")]
        [SerializeField] Button     _cancelButton;
        [SerializeField] Button     _saveButton;
        [SerializeField] Button     _closeButton;

        // ── 运行时 ────────────────────────────────────────────────────

        EventBus     _bus;
        ModuleRunner _runner;
        bool         _isOpen;

        // ── 颜色常量（与标注稿 §5 一致）─────────────────────────────

        static readonly Color ColorAccent      = new Color(1f,    0.706f, 0f);       // #FFB400
        static readonly Color ColorRadioEmpty  = new Color(0f,    0f,    0f, 0f);    // 透明
        static readonly Color ColorLabelNormal = new Color(0.659f, 0.659f, 0.753f);  // #A8A9C0
        static readonly Color ColorLabelActive = new Color(0.973f, 0.976f, 0.980f);  // #F8F9FA

        // ── 默认按键展示文本（v1.0 静态，与 InputModule 真实键位无关）────
        const string DefaultMoveText   = "WASD";
        const string DefaultAttackText = "鼠标左键";
        const string DefaultPauseText  = "Esc";

        // ── IExclusiveUIForm ──────────────────────────────────────────

        public bool IsOpen => _isOpen;
        public GameObject GameObject => gameObject;

        public void ForceClose()
        {
            _isOpen = false;
            gameObject.SetActive(false);
        }

        public void OnGameStateChanged(GameState oldState, GameState newState) { }

        // ── MonoBehaviour ─────────────────────────────────────────────

        void Awake()
        {
            AutoBindMissing();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 自动按 GameObject 名匹配绑定缺失的 SerializeField 引用。
        /// 允许 Prefab 在没有手动拖引用的情况下也能正确工作（依赖控件命名契约，见 annotations.md §2）。
        /// </summary>
        void AutoBindMissing()
        {
            // 用 hashset 加速 lookup
            var lookup = new Dictionary<string, Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (!lookup.ContainsKey(t.name)) lookup[t.name] = t;

            T Find<T>(string n) where T : Component
                => lookup.TryGetValue(n, out var t) ? t.GetComponent<T>() : null;

            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            if (_sliderBGM    == null) _sliderBGM    = Find<Slider>("Slider_BGM");
            if (_valueTextBGM == null) _valueTextBGM = Find<TMP_Text>("ValueText_BGM");
            if (_sliderSFX    == null) _sliderSFX    = Find<Slider>("Slider_SFX");
            if (_valueTextSFX == null) _valueTextSFX = Find<TMP_Text>("ValueText_SFX");

            if (_radioBtnLow     == null) _radioBtnLow     = Find<Button>("Radio_Low");
            if (_radioCircleLow  == null) _radioCircleLow  = Find<Image>("RadioCircle_Low");
            if (_radioDotLow     == null) _radioDotLow     = Find<Image>("RadioDot_Low");
            if (_radioLabelLow   == null) _radioLabelLow   = Find<TMP_Text>("RadioLabel_Low");
            if (_radioBtnMed     == null) _radioBtnMed     = Find<Button>("Radio_Med");
            if (_radioCircleMed  == null) _radioCircleMed  = Find<Image>("RadioCircle_Med");
            if (_radioDotMed     == null) _radioDotMed     = Find<Image>("RadioDot_Med");
            if (_radioLabelMed   == null) _radioLabelMed   = Find<TMP_Text>("RadioLabel_Med");
            if (_radioBtnHigh    == null) _radioBtnHigh    = Find<Button>("Radio_High");
            if (_radioCircleHigh == null) _radioCircleHigh = Find<Image>("RadioCircle_High");
            if (_radioDotHigh    == null) _radioDotHigh    = Find<Image>("RadioDot_High");
            if (_radioLabelHigh  == null) _radioLabelHigh  = Find<TMP_Text>("RadioLabel_High");

            if (_keyBindButtonMove   == null) _keyBindButtonMove   = Find<Button>("KeyBindButton_Move");
            if (_keyBindTextMove     == null) _keyBindTextMove     = Find<TMP_Text>("KeyBindText_Move");
            if (_keyBindButtonAttack == null) _keyBindButtonAttack = Find<Button>("KeyBindButton_Attack");
            if (_keyBindTextAttack   == null) _keyBindTextAttack   = Find<TMP_Text>("KeyBindText_Attack");
            if (_keyBindButtonPause  == null) _keyBindButtonPause  = Find<Button>("KeyBindButton_Pause");
            if (_keyBindTextPause    == null) _keyBindTextPause    = Find<TMP_Text>("KeyBindText_Pause");
            if (_keyBindSectionNotice == null) _keyBindSectionNotice = Find<TMP_Text>("KeyBindNotice");

            if (_cancelButton == null) _cancelButton = Find<Button>("CancelButton");
            if (_saveButton   == null) _saveButton   = Find<Button>("SaveButton");
            if (_closeButton  == null) _closeButton  = Find<Button>("CloseButton");
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
            BindButtons();
            ApplyKeyBindLabels();
        }

        void OnDestroy()
        {
            DOTween.Kill(transform);
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }

        // ── Open / Close ──────────────────────────────────────────────

        public void Open()
        {
            _runner.GetModule<UIModule>().RequestOpenExclusive(this);
            gameObject.SetActive(true);
            _isOpen = true;

            var settings = _runner.GetModule<SettingsModule>();
            settings?.BeginEdit();
            RefreshControls(settings?.GetCurrent() ?? new SettingsData());

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }
            FrameworkLogger.Info("SettingsForm", "Action=Open");
        }

        void Close()
        {
            _isOpen = false;
            if (_canvasGroup)
                _canvasGroup.DOFade(0f, 0.15f).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
            _runner?.GetModule<UIModule>().CloseCurrentExclusive();
            FrameworkLogger.Info("SettingsForm", "Action=Close");
        }

        // ── 控件绑定 ──────────────────────────────────────────────────

        void BindButtons()
        {
            _sliderBGM?.onValueChanged.AddListener(OnBgmSliderChanged);
            _sliderSFX?.onValueChanged.AddListener(OnSfxSliderChanged);

            _radioBtnLow?.onClick.AddListener(() => OnQualitySelected(0));
            _radioBtnMed?.onClick.AddListener(() => OnQualitySelected(1));
            _radioBtnHigh?.onClick.AddListener(() => OnQualitySelected(2));

            // v1.0 重绑定按钮不挂回调，UI 整段禁用 interactable，文字保持默认
            if (_keyBindButtonMove)   _keyBindButtonMove.interactable   = false;
            if (_keyBindButtonAttack) _keyBindButtonAttack.interactable = false;
            if (_keyBindButtonPause)  _keyBindButtonPause.interactable  = false;
            if (_keyBindSectionNotice) _keyBindSectionNotice.SetText("即将推出");

            _saveButton?.onClick.AddListener(OnSaveClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);
            _closeButton?.onClick.AddListener(OnCancelClicked);
        }

        // ── 控件刷新 ──────────────────────────────────────────────────

        void RefreshControls(SettingsData data)
        {
            if (_sliderBGM) _sliderBGM.SetValueWithoutNotify(data.MusicVolume);
            if (_valueTextBGM) _valueTextBGM.SetText("{0:0.00}", data.MusicVolume);

            if (_sliderSFX) _sliderSFX.SetValueWithoutNotify(data.SfxVolume);
            if (_valueTextSFX) _valueTextSFX.SetText("{0:0.00}", data.SfxVolume);

            RefreshRadio(data.QualityLevel);
        }

        void RefreshRadio(int level)
        {
            SetRadioState(level == 0, _radioCircleLow,  _radioDotLow,  _radioLabelLow);
            SetRadioState(level == 1, _radioCircleMed,  _radioDotMed,  _radioLabelMed);
            SetRadioState(level == 2, _radioCircleHigh, _radioDotHigh, _radioLabelHigh);
        }

        void SetRadioState(bool selected, Image circle, Image dot, TMP_Text label)
        {
            if (circle) circle.color = selected ? ColorAccent : ColorRadioEmpty;
            if (dot) dot.gameObject.SetActive(selected);
            if (label)
            {
                label.color = selected ? ColorLabelActive : ColorLabelNormal;
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        void ApplyKeyBindLabels()
        {
            if (_keyBindTextMove)   _keyBindTextMove.SetText(DefaultMoveText);
            if (_keyBindTextAttack) _keyBindTextAttack.SetText(DefaultAttackText);
            if (_keyBindTextPause)  _keyBindTextPause.SetText(DefaultPauseText);
        }

        // ── 事件回调 ─────────────────────────────────────────────────

        void OnBgmSliderChanged(float value)
        {
            if (_valueTextBGM) _valueTextBGM.SetText("{0:0.00}", value);
            var draft = BuildDraft();
            draft.MusicVolume = value;
            _runner.GetModule<SettingsModule>()?.Preview(draft);
        }

        void OnSfxSliderChanged(float value)
        {
            if (_valueTextSFX) _valueTextSFX.SetText("{0:0.00}", value);
            var draft = BuildDraft();
            draft.SfxVolume = value;
            _runner.GetModule<SettingsModule>()?.Preview(draft);
        }

        void OnQualitySelected(int level)
        {
            RefreshRadio(level);
            var draft = BuildDraft();
            draft.QualityLevel = level;
            _runner.GetModule<SettingsModule>()?.Preview(draft);
        }

        void OnSaveClicked()
        {
            _runner.GetModule<SettingsModule>()?.Commit();
            Close();
            FrameworkLogger.Info("SettingsForm", "Action=Save");
        }

        void OnCancelClicked()
        {
            _runner.GetModule<SettingsModule>()?.Rollback();
            Close();
            FrameworkLogger.Info("SettingsForm", "Action=Cancel");
        }

        // ── 工具 ─────────────────────────────────────────────────────

        SettingsData BuildDraft()
        {
            var current = _runner.GetModule<SettingsModule>()?.GetCurrent() ?? new SettingsData();
            if (_sliderBGM) current.MusicVolume = _sliderBGM.value;
            if (_sliderSFX) current.SfxVolume   = _sliderSFX.value;
            return current;
        }
    }
}
