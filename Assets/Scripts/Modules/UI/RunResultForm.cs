using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 本局结算面板（UGUI v2.1）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/RunResult.prefab
    /// 触发：订阅 RunEndedEvent → 显示「胜利」/「失败」+ 等玩家点 ReturnToMenuBtn 回主菜单
    /// 控件契约（按 Prefab 节点名）：TitleText / ReturnToMenuBtn
    /// </summary>
    public sealed class RunResultForm : MonoBehaviour, IUIForm
    {
        EventBus     _bus;
        ModuleRunner _runner;
        readonly List<IDisposable> _subs = new();

        TMP_Text _titleText;
        Button   _returnBtn;

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // 离开 GameOver（玩家点了返回主菜单）→ 隐藏
            if (newState != GameState.GameOver && gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void Awake()
        {
            gameObject.SetActive(false);
            _titleText = FindChildByName<TMP_Text>("TitleText");
            _returnBtn = FindChildByName<Button>("ReturnToMenuBtn");
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
            _returnBtn?.onClick.AddListener(OnReturnClicked);
            _subs.Add(_bus.Subscribe<RunEndedEvent>(OnRunEnded));
        }

        void OnRunEnded(RunEndedEvent e)
        {
            if (_titleText != null)
                _titleText.text = e.Win ? "胜利" : "本局结束";
            gameObject.SetActive(true);
            FrameworkLogger.Info("RunResultForm", $"Action=Shown Win={e.Win}");
        }

        void OnReturnClicked()
        {
            _runner?.GetModule<GameStateModule>()?.GoToMainMenu();
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }
    }
}
