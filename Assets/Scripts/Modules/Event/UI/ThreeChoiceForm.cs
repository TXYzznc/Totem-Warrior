using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 三选一面板（UGUI v2.1）— 空壳，待后续迭代实装。
    ///
    /// Prefab 落点：Assets/Resources/UI/v21/ThreeChoice.prefab
    /// 触发条件：ThreeChoiceShownEvent
    /// 注：打开后 3 秒内按钮 interactable=false（UnscaledDeltaTime 防误触锁）。
    /// </summary>
    public sealed class ThreeChoiceForm : MonoBehaviour, IExclusiveUIForm
    {
        EventBus     _bus;
        ModuleRunner _runner;
        bool         _isOpen;
        readonly List<IDisposable> _subs = new();

        // ── 测试访问器 ────────────────────────────────────────
        /// <summary>选项按钮当前是否可交互（PlayMode 测试用）。</summary>
        public bool AreChoiceButtonsInteractable { get; private set; }

        // ── IExclusiveUIForm ──────────────────────────────────
        public bool IsOpen => _isOpen;
        public GameObject GameObject => gameObject;
        public void ForceClose() { _isOpen = false; gameObject.SetActive(false); }

        // ── IUIForm ───────────────────────────────────────────
        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.InGame && _isOpen) ForceClose();
        }

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
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }
    }
}
