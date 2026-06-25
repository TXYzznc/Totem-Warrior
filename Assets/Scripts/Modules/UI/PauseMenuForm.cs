using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tattoo.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 暂停菜单面板（UGUI v2.1）。
    ///
    /// Prefab 落点：Assets/Resources/UI/v21/PauseMenu.prefab
    /// 触发条件：PauseRequestedEvent（由 InputModule 发出）
    /// 打开时 Time.timeScale = 0，关闭时恢复。
    /// </summary>
    public sealed class PauseMenuForm : MonoBehaviour, IUIForm
    {
        [Header("按钮")]
        [SerializeField] Button _resumeBtn;
        [SerializeField] Button _quitBtn;

        // ── 运行时 ────────────────────────────────────────────
        EventBus     _bus;
        ModuleRunner _runner;

        readonly List<IDisposable> _subs = new();

        // ── IUIForm ───────────────────────────────────────────

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // 非 InGame 状态时关闭（如直接切换到 MainMenu）
            if (newState != GameState.InGame && gameObject.activeSelf)
                Close();
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
            _resumeBtn?.onClick.AddListener(OnResumeClicked);
            _quitBtn?.onClick.AddListener(OnQuitClicked);
            SubscribeEvents();
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            if (Time.timeScale == 0f) Time.timeScale = 1f; // 安全恢复
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }

        // ── 事件订阅 ──────────────────────────────────────────

        void SubscribeEvents()
        {
            _subs.Add(_bus.Subscribe<PauseRequestedEvent>(_ => Open()));
        }

        // ── Open / Close ──────────────────────────────────────

        void Open()
        {
            gameObject.SetActive(true);
            Time.timeScale = 0f;
            FrameworkLogger.Info("PauseMenuForm", "Action=Open");
        }

        void Close()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            FrameworkLogger.Info("PauseMenuForm", "Action=Close");
        }

        /// <summary>恢复按钮点击（PlayMode 测试可直接调用）。</summary>
        public void OnResumeClicked() => Close();

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
