using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 角色选择面板（UGUI v2.1）— change #21 landing。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/CharacterSelect.prefab
    /// 触发条件：MainMenuForm「开始」按钮 → Open()
    /// 关闭条件：Next 按钮 → 打开 StartupSelectForm + Close()；Cancel 回主菜单
    ///
    /// 3 张角色卡片当前无属性差异（只是 UI 层可选），SelectedCharacterId 只用于日志。
    /// 后续 change 若引入 CharacterConfig 差异化，可在此扩展。
    /// </summary>
    public sealed class CharacterSelectForm : MonoBehaviour, IUIForm, IUIFormBootstrap
    {
        [Header("根容器 / 按钮")]
        [SerializeField] Transform _characterRoot;
        [SerializeField] Button _nextBtn;

        EventBus     _bus;
        ModuleRunner _runner;
        readonly List<IDisposable> _subs = new();

        readonly List<GameObject> _cards = new();
        readonly List<int>        _cardIds = new();
        int _selectedCharacterId = -1;

        static readonly Color NormalTint   = Color.white;
        static readonly Color SelectedTint = new Color(0.5f, 1f, 0.5f);

        // 3 角色 icon 路径（复用 change#17 战斗 sprite）
        static readonly (int id, string label, string spritePath)[] Characters =
        {
            (1, "角色 1", "Sprite/Character/Player1/Idle/Down"),
            (2, "角色 2", "Sprite/Character/Player2/Idle/Down"),
            (3, "角色 3", "Sprite/Character/Player3/Idle/Down"),
        };

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // 回主菜单时自动关闭
            if (gameObject.activeSelf && newState == GameState.MainMenu && oldState != GameState.MainMenu)
                Close();
        }

        void Awake()
        {
            gameObject.SetActive(false);

            if (_characterRoot == null) _characterRoot = FindChildTransform("CharacterRoot");
            if (_nextBtn       == null) _nextBtn       = FindChildComponent<Button>("NextButton");
        }

        Transform FindChildTransform(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        T FindChildComponent<T>(string name) where T : Component
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) { var c = t.GetComponent<T>(); if (c != null) return c; }
            return null;
        }

        public void Bootstrap(EventBus bus, ModuleRunner runner)
        {
            _bus    = bus;
            _runner = runner;
            _nextBtn?.onClick.AddListener(OnNextClicked);
            if (_nextBtn != null) _nextBtn.interactable = false;
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { }
        }

        // ── Open / Close ──────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            BuildCards();
            FrameworkLogger.Info("CharacterSelectForm", "Action=Open");
        }

        void Close()
        {
            gameObject.SetActive(false);
            FrameworkLogger.Info("CharacterSelectForm", "Action=Close");
        }

        // ── 卡片构建 ──────────────────────────────────────────

        void BuildCards()
        {
            _selectedCharacterId = -1;
            foreach (var c in _cards) if (c != null) Destroy(c);
            _cards.Clear();
            _cardIds.Clear();
            if (_nextBtn != null) _nextBtn.interactable = false;

            if (_characterRoot == null)
            {
                FrameworkLogger.Warn("CharacterSelectForm", "Action=BuildCards CharacterRoot=null 无法构建卡片");
                return;
            }

            var frameSprite = Resources.Load<Sprite>("Sprite/UI/CharacterSelectForm/CharacterSelectForm_card_frame_unlocked");

            foreach (var (id, label, spritePath) in Characters)
            {
                int captureId = id;
                var portrait = Resources.Load<Sprite>(spritePath);
                var go = CreateCard($"Card_{id}", _characterRoot, label, frameSprite, portrait, () => SetSelectedCharacter(captureId));
                _cards.Add(go);
                _cardIds.Add(id);
            }
        }

        /// <summary>创建单张角色卡片（框 + 立绘 + 名称）。</summary>
        GameObject CreateCard(string goName, Transform parent, string label, Sprite frame, Sprite portrait, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 280f);

            var bg = go.AddComponent<Image>();
            bg.sprite = frame;
            bg.color = NormalTint;
            bg.type = frame != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            // 立绘子节点
            if (portrait != null)
            {
                var portraitGo = new GameObject("Portrait", typeof(RectTransform));
                portraitGo.transform.SetParent(go.transform, false);
                var portraitRect = portraitGo.GetComponent<RectTransform>();
                portraitRect.sizeDelta = new Vector2(180f, 180f);
                portraitRect.anchoredPosition = new Vector2(0f, 20f);
                var pImg = portraitGo.AddComponent<Image>();
                pImg.sprite = portrait;
                pImg.raycastTarget = false;
                pImg.preserveAspect = true;
            }

            // 名称子节点
            var labelGo = new GameObject("Name", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 40f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);

            var txt = labelGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 20;
            txt.color = Color.black;
            txt.raycastTarget = false;

            return go;
        }

        public void SetSelectedCharacter(int id)
        {
            _selectedCharacterId = id;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var img = _cards[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i < _cardIds.Count && _cardIds[i] == id) ? SelectedTint : NormalTint;
            }
            if (_nextBtn != null) _nextBtn.interactable = true;
            FrameworkLogger.Info("CharacterSelectForm", $"Action=SelectCharacter Id={id}");
        }

        /// <summary>Next → 打开 StartupSelectForm 并关闭自身。</summary>
        public void OnNextClicked()
        {
            if (_selectedCharacterId <= 0)
            {
                FrameworkLogger.Warn("CharacterSelectForm", "Action=NextClicked Rejected=未选角色");
                return;
            }

            var startup = UnityEngine.Object.FindObjectOfType<StartupSelectForm>(true);
            if (startup == null)
            {
                FrameworkLogger.Warn("CharacterSelectForm", "Action=NextClicked StartupSelectForm=null 未找到实例");
                return;
            }
            startup.Open();
            FrameworkLogger.Info("CharacterSelectForm", $"Action=NextClicked Character={_selectedCharacterId} → StartupSelectForm.Open");
            Close();
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
        }
    }
}
