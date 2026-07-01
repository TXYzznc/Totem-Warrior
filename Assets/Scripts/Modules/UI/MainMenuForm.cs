using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 主菜单面板（UGUI v2.1）— 空壳，待后续迭代实装。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/MainMenu.prefab
    /// 初始状态：SetActive(true)（游戏启动即显示）
    /// </summary>
    public sealed class MainMenuForm : MonoBehaviour, IUIForm
    {
        [Header("按钮")]
        [SerializeField] Button _startBtn;
        [SerializeField] Button _settingsBtn;

        EventBus     _bus;
        ModuleRunner _runner;
        readonly List<IDisposable> _subs = new();

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            bool show = newState == GameState.MainMenu;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }

        void Awake()
        {
            // 主菜单初始显示
            gameObject.SetActive(true);

            // 缺失时按名自动绑定（Prefab 控件命名契约：StartButton / SettingsButton）
            if (_startBtn    == null) _startBtn    = FindChildByName<Button>("StartButton");
            if (_settingsBtn == null) _settingsBtn = FindChildByName<Button>("SettingsButton");
        }

        T FindChildByName<T>(string name) where T : Component
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) { var c = t.GetComponent<T>(); if (c != null) return c; }
            return null;
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
            _startBtn?.onClick.AddListener(OnStartClicked);
            _settingsBtn?.onClick.AddListener(OnSettingsClicked);
        }

        /// <summary>开始游戏：打开 CharacterSelectForm 进入选择流程（不再直接切 InGame）。</summary>
        public void OnStartClicked()
        {
            var charSel = UnityEngine.Object.FindObjectOfType<CharacterSelectForm>(true);
            if (charSel == null)
            {
                FrameworkLogger.Warn("MainMenuForm", "Action=StartClicked CharacterSelectForm=null 未找到实例");
                return;
            }
            charSel.Open();
            FrameworkLogger.Info("MainMenuForm", "Action=StartClicked → CharacterSelectForm.Open");
        }

        /// <summary>设置按钮点击：打开设置面板。</summary>
        void OnSettingsClicked()
        {
            // SettingsForm 已由 UIModule 在 GameReady 后加载并实例化
            // 通过 FindObjectOfType 找到已实例化的 SettingsForm 并打开
            // 注：FindObjectOfType 仅在 Start/Click 慢路径中使用，符合规范
            var settingsForm = UnityEngine.Object.FindObjectOfType<Tattoo.UI.SettingsForm>();
            if (settingsForm != null)
            {
                settingsForm.Open();
                FrameworkLogger.Info("MainMenuForm", "Action=SettingsClicked → SettingsForm.Open");
            }
            else
            {
                FrameworkLogger.Warn("MainMenuForm", "Action=SettingsClicked SettingsForm=null 未找到实例");
            }
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }
    }
}
