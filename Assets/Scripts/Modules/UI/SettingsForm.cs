using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 设置面板（UGUI v2.0，阶段5重写）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/Settings.prefab
    /// 触发条件：MainMenuForm / PauseMenuForm 的「设置」按钮 → Open()
    /// 覆盖层互斥：实现 IExclusiveUIForm（SortOrder=20）
    ///
    /// 范围（v2.0）：
    ///   - BGM / SFX 音量（拖动即时 Preview）
    ///   - 画质三档（Toggle 组互斥切换）
    ///   - 按键重绑定（UI 展示静态文本，按钮 interactable=false；待 InputModule 升级后接通）
    ///
    /// 生命周期：
    ///   Open() → BeginEdit → 控件填值
    ///   滑条 / 画质切换 → Preview(draft)
    ///   SaveButton → Commit → Close
    ///   CancelButton / CloseButton → Rollback → Close
    /// </summary>
    public sealed class SettingsForm : MonoBehaviour, IExclusiveUIForm
    {
        // ── Inspector 引用（与 Settings.prefab 层级命名一一对应）──────

        [Header("根节点")]
        [SerializeField] CanvasGroup _canvasGroup;

        [Header("音量区")]
        [SerializeField] Slider   _sliderBGM;
        [SerializeField] TMP_Text _valueTextBGM;
        [SerializeField] Slider   _sliderSFX;
        [SerializeField] TMP_Text _valueTextSFX;

        [Header("画质区（Toggle 组）")]
        [SerializeField] Toggle   _togglePerformant;
        [SerializeField] Image    _circlePerformant;
        [SerializeField] Toggle   _toggleBalanced;
        [SerializeField] Image    _circleBalanced;
        [SerializeField] Toggle   _toggleHighFidelity;
        [SerializeField] Image    _circleHighFidelity;

        [Header("画质 Sprite 资源")]
        [SerializeField] Sprite   _spriteRadioNormal;
        [SerializeField] Sprite   _spriteRadioSelected;

        [Header("按键重绑定区（v2.0 仅展示）")]
        [SerializeField] TMP_Text _keyBindTextMove;
        [SerializeField] TMP_Text _keyBindTextAttack;
        [SerializeField] TMP_Text _keyBindTextPause;

        [Header("底部按钮")]
        [SerializeField] Button   _cancelButton;
        [SerializeField] Button   _saveButton;
        [SerializeField] Button   _closeButton;

        // ── 运行时 ────────────────────────────────────────────────────

        EventBus     _bus;
        ModuleRunner _runner;
        bool         _isOpen;
        int          _currentQuality;

        // ── 默认按键展示（v2.0 静态）────────────────────────────────
        const string DefaultMoveText   = "WASD";
        const string DefaultAttackText = "鼠标左键";
        const string DefaultPauseText  = "Esc";

        // ── IExclusiveUIForm ──────────────────────────────────────────

        public bool IsOpen         => _isOpen;
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

        async void Start()
        {
            GameApp app   = null;
            float timeout = Time.unscaledTime + 10f;
            while (Time.unscaledTime < timeout)
            {
                app = FindObjectOfType<GameApp>();
                if (app != null && app.TryGetRuntime(out _bus, out _runner)) break;
                await UniTask.Yield();
            }
            if (_bus == null) return;

            _runner.GetModule<UIModule>().Register(this);
            BindControls();
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

            var data = settings?.GetCurrent() ?? new SettingsData();
            RefreshAll(data);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }
            FrameworkLogger.Info("SettingsForm", "Action=Open");
        }

        void Close()
        {
            _isOpen = false;
            if (_canvasGroup != null)
                _canvasGroup.DOFade(0f, 0.15f).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);

            _runner?.GetModule<UIModule>().CloseCurrentExclusive();
            FrameworkLogger.Info("SettingsForm", "Action=Close");
        }

        // ── 绑定 ──────────────────────────────────────────────────────

        void BindControls()
        {
            _sliderBGM?.onValueChanged.AddListener(OnBgmChanged);
            _sliderSFX?.onValueChanged.AddListener(OnSfxChanged);

            _togglePerformant?.onValueChanged.AddListener(on  => { if (on) SelectQuality(0); });
            _toggleBalanced?.onValueChanged.AddListener(on    => { if (on) SelectQuality(1); });
            _toggleHighFidelity?.onValueChanged.AddListener(on => { if (on) SelectQuality(2); });

            _saveButton?.onClick.AddListener(OnSaveClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);
            _closeButton?.onClick.AddListener(OnCancelClicked);
        }

        // ── 刷新 ──────────────────────────────────────────────────────

        void RefreshAll(SettingsData data)
        {
            // 滑条（SetValueWithoutNotify 避免触发 onValueChanged）
            _sliderBGM?.SetValueWithoutNotify(data.MusicVolume);
            _valueTextBGM?.SetText("{0:0.00}", data.MusicVolume);

            _sliderSFX?.SetValueWithoutNotify(data.SfxVolume);
            _valueTextSFX?.SetText("{0:0.00}", data.SfxVolume);

            // 画质 Radio
            _currentQuality = data.QualityLevel;
            RefreshRadioSprites(_currentQuality);

            // 按键文字（静态）
            _keyBindTextMove?.SetText(DefaultMoveText);
            _keyBindTextAttack?.SetText(DefaultAttackText);
            _keyBindTextPause?.SetText(DefaultPauseText);
        }

        void RefreshRadioSprites(int quality)
        {
            SetCircleSprite(_circlePerformant,  quality == 0);
            SetCircleSprite(_circleBalanced,    quality == 1);
            SetCircleSprite(_circleHighFidelity, quality == 2);
        }

        void SetCircleSprite(Image circle, bool selected)
        {
            if (circle == null) return;
            circle.sprite = selected ? _spriteRadioSelected : _spriteRadioNormal;
            // RadioDot（子节点，第一个子 Image）同步
            if (circle.transform.childCount > 0)
                circle.transform.GetChild(0).gameObject.SetActive(selected);
        }

        // ── 回调 ──────────────────────────────────────────────────────

        void OnBgmChanged(float value)
        {
            _valueTextBGM?.SetText("{0:0.00}", value);
            var draft = BuildDraft();
            draft.MusicVolume = value;
            _runner?.GetModule<SettingsModule>()?.Preview(draft);
            FrameworkLogger.Info("SettingsForm", $"Action=PreviewBGM Value={value:F2}");
        }

        void OnSfxChanged(float value)
        {
            _valueTextSFX?.SetText("{0:0.00}", value);
            var draft = BuildDraft();
            draft.SfxVolume = value;
            _runner?.GetModule<SettingsModule>()?.Preview(draft);
            FrameworkLogger.Info("SettingsForm", $"Action=PreviewSFX Value={value:F2}");
        }

        void SelectQuality(int level)
        {
            _currentQuality = level;
            RefreshRadioSprites(level);
            var draft = BuildDraft();
            draft.QualityLevel = level;
            _runner?.GetModule<SettingsModule>()?.Preview(draft);
            FrameworkLogger.Info("SettingsForm", $"Action=PreviewQuality Level={level}");
        }

        void OnSaveClicked()
        {
            _runner?.GetModule<SettingsModule>()?.Commit();
            Close();
            FrameworkLogger.Info("SettingsForm", "Action=Save");
        }

        void OnCancelClicked()
        {
            _runner?.GetModule<SettingsModule>()?.Rollback();
            Close();
            FrameworkLogger.Info("SettingsForm", "Action=Cancel");
        }

        // ── 工具 ──────────────────────────────────────────────────────

        SettingsData BuildDraft()
        {
            var current = _runner?.GetModule<SettingsModule>()?.GetCurrent() ?? new SettingsData();
            if (_sliderBGM != null) current.MusicVolume  = _sliderBGM.value;
            if (_sliderSFX != null) current.SfxVolume    = _sliderSFX.value;
            current.QualityLevel = _currentQuality;
            return current;
        }

        /// <summary>
        /// Awake 阶段自动按节点名匹配缺失的 SerializeField 引用。
        /// 节点名约定与 prefab-layout.md 一致。
        /// </summary>
        void AutoBindMissing()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            var lookup = new Dictionary<string, Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (!lookup.ContainsKey(t.name)) lookup[t.name] = t;

            T Find<T>(string n) where T : Component =>
                lookup.TryGetValue(n, out var t) ? t.GetComponent<T>() : null;

            if (_sliderBGM    == null) _sliderBGM    = Find<Slider>("Slider_BGM");
            if (_valueTextBGM == null) _valueTextBGM = Find<TMP_Text>("ValueText_BGM");
            if (_sliderSFX    == null) _sliderSFX    = Find<Slider>("Slider_SFX");
            if (_valueTextSFX == null) _valueTextSFX = Find<TMP_Text>("ValueText_SFX");

            if (_togglePerformant   == null) _togglePerformant   = Find<Toggle>("RadioBtn_Performant");
            if (_toggleBalanced     == null) _toggleBalanced     = Find<Toggle>("RadioBtn_Balanced");
            if (_toggleHighFidelity == null) _toggleHighFidelity = Find<Toggle>("RadioBtn_HighFidelity");

            // CircleImg 在各 RadioBtn 下的 RadioCircle 子节点（三个同名，需要通过父节点区分）
            if (_circlePerformant   == null) _circlePerformant   = FindRadioCircle("RadioBtn_Performant", lookup);
            if (_circleBalanced     == null) _circleBalanced     = FindRadioCircle("RadioBtn_Balanced", lookup);
            if (_circleHighFidelity == null) _circleHighFidelity = FindRadioCircle("RadioBtn_HighFidelity", lookup);

            if (_keyBindTextMove   == null) _keyBindTextMove   = Find<TMP_Text>("KeyBindText_Move");
            if (_keyBindTextAttack == null) _keyBindTextAttack = Find<TMP_Text>("KeyBindText_Attack");
            if (_keyBindTextPause  == null) _keyBindTextPause  = Find<TMP_Text>("KeyBindText_Pause");

            if (_cancelButton == null) _cancelButton = Find<Button>("CancelButton");
            if (_saveButton   == null) _saveButton   = Find<Button>("SaveButton");
            if (_closeButton  == null) _closeButton  = Find<Button>("CloseButton");

            // Sprite 资源：从已入库的 RadioCircle Image 上读
            if (_spriteRadioNormal   == null || _spriteRadioSelected == null)
                TryLoadRadioSprites();
        }

        static Image FindRadioCircle(string radioBtnName, Dictionary<string, Transform> lookup)
        {
            if (!lookup.TryGetValue(radioBtnName, out var btnT)) return null;
            var circleT = btnT.Find("RadioCircle");
            return circleT != null ? circleT.GetComponent<Image>() : null;
        }

        void TryLoadRadioSprites()
        {
            // 从 Prefab 上的 RadioCircle Image 读取 sprite（Prefab 已经序列化了正确引用）
            // 若仍为 null，从 Resources 加载（兜底，避免运行时 Radio 无图）
            if (_circleBalanced != null)
            {
                if (_spriteRadioSelected == null) _spriteRadioSelected = _circleBalanced.sprite;
            }
            if (_circlePerformant != null)
            {
                if (_spriteRadioNormal == null) _spriteRadioNormal = _circlePerformant.sprite;
            }
            // 最终兜底：Resources.Load
            if (_spriteRadioNormal   == null) _spriteRadioNormal   = Resources.Load<Sprite>("Sprite/UI/SettingsForm/RadioCircle_Normal");
            if (_spriteRadioSelected == null) _spriteRadioSelected = Resources.Load<Sprite>("Sprite/UI/SettingsForm/RadioCircle_Selected");
        }
    }
}
