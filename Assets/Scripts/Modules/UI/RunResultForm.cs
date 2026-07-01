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
    public sealed class RunResultForm : MonoBehaviour, IUIForm, IUIFormBootstrap
    {
        EventBus     _bus;
        ModuleRunner _runner;
        readonly List<IDisposable> _subs = new();

        TMP_Text _titleText;
        Button   _returnBtn;

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                // GameOver → 显示结算面板（兜底：RunEndedEvent 若先到则标题已设好，否则默认"本局结束"）
                if (_titleText != null && !gameObject.activeSelf)
                    _titleText.text = "本局结束";
                gameObject.SetActive(true);
                FrameworkLogger.Info("RunResultForm", $"Action=ShownByGameOver OldState={oldState}");
            }
            else if (gameObject.activeSelf)
            {
                // 离开 GameOver（玩家点了返回主菜单）→ 隐藏
                gameObject.SetActive(false);
            }
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

        public void Bootstrap(EventBus bus, ModuleRunner runner)
        {
            // UIModule 同步调用：避免 Start 在 inactive 下不跑而导致 RunEndedEvent 订阅丢失
            _bus = bus;
            _runner = runner;
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
