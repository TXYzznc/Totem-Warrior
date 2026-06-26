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
        }

        /// <summary>开始游戏：切换状态到 InGame，CombatHUDForm 等会按 OnGameStateChanged 自动显示。</summary>
        public void OnStartClicked()
        {
            var gs = _runner?.GetModule<GameStateModule>();
            if (gs == null) return;
            gs.StartGame();
            FrameworkLogger.Info("MainMenuForm", "Action=StartClicked → GameState.InGame");
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }
    }
}
