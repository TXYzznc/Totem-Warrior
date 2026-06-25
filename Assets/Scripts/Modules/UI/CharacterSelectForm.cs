using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 角色选择面板（UGUI v2.1）— 空壳，待后续迭代实装。
    ///
    /// Prefab 落点：Assets/Resources/UI/v21/CharacterSelect.prefab
    /// 触发条件：MainMenuForm「开始」按钮
    /// </summary>
    public sealed class CharacterSelectForm : MonoBehaviour, IUIForm
    {
        EventBus     _bus;
        ModuleRunner _runner;
        readonly List<IDisposable> _subs = new();

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState) { }

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
