using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tattoo.UI
{
    /// <summary>
    /// 战斗 HUD（UI Toolkit）。
    ///
    /// 在 Launch.unity 中：
    /// 1. 创建一个 GameObject 命名 "CombatHUD"
    /// 2. 挂 UIDocument 组件，把 PanelSettings 与 visualTreeAsset 指到 CombatHUD.uxml
    /// 3. 同 GameObject 上挂本组件
    ///
    /// 本组件 Start() 时通过 FindObjectOfType&lt;GameApp&gt;() 拿 EventBus / ModuleRunner，
    /// 不需要手工绑定 inspector 字段。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatHUDForm : MonoBehaviour, IUIForm
    {
        UIDocument _doc;
        VisualElement _root;

        EventBus _bus;
        ModuleRunner _runner;
        TattooModule _tattoo;
        SpawnerModule _spawner;

        DropdownField _partDropdown;
        DropdownField _colorDropdown;
        DropdownField _patternDropdown;
        Label _statusLabel;
        Label _playerHpLabel;
        Label _playerBuffsLabel;
        VisualElement _equippedList;
        VisualElement _passiveList;
        VisualElement _logList;

        IDisposable _subBuildChanged;
        IDisposable _subPassive;
        IDisposable _subEffect;
        IDisposable _subTargetKilled;
        IDisposable _subPlayerDied;

        const int MaxLogEntries = 30;

        // ===== 部位 / 颜色 / 图案 选项缓存（Id 顺序）=====
        readonly List<int> _partIds    = new() { 1, 2, 3, 4, 5, 6 };
        readonly List<int> _colorIds   = new() { 1, 2, 3, 4, 5, 6, 7 };
        readonly List<int> _patternIds = new() { 1, 2, 3, 4, 5, 6, 7, 8 };

        async void Start()
        {
            _doc = GetComponent<UIDocument>();

            // 等 UIDocument 与 GameApp 全部就绪（模块异步初始化期间避免 NRE）
            GameApp app = null;
            float timeoutAt = Time.unscaledTime + 10f;
            while (Time.unscaledTime < timeoutAt)
            {
                if (_doc != null && _doc.rootVisualElement != null)
                {
                    app = FindObjectOfType<GameApp>();
                    if (app != null && app.TryGetRuntime(out _bus, out _runner)) break;
                }
                await UniTask.Yield();
            }

            if (app == null || _bus == null || _runner == null)
            {
                FrameworkLogger.Error("CombatHUDForm", "Action=Start 等待 GameApp 就绪超时");
                return;
            }
            _root = _doc.rootVisualElement;

            _tattoo  = _runner.GetModule<TattooModule>();
            _spawner = _runner.GetModule<SpawnerModule>();
            _runner.GetModule<UIModule>().Register(this);

            QueryControls();
            BindOptions();
            BindButtons();
            SubscribeEvents();

            RefreshPlayerState();
            RefreshEquipped();
            RefreshPassive();
        }

        void OnDestroy()
        {
            _subBuildChanged?.Dispose();
            _subPassive?.Dispose();
            _subEffect?.Dispose();
            _subTargetKilled?.Dispose();
            _subPlayerDied?.Dispose();
            if (_runner != null)
            {
                try { _runner.GetModule<UIModule>().Unregister(this); } catch { /* shutting down */ }
            }
        }

        // ===== UI 控件查询 =====

        void QueryControls()
        {
            _partDropdown    = _root.Q<DropdownField>("part-dropdown");
            _colorDropdown   = _root.Q<DropdownField>("color-dropdown");
            _patternDropdown = _root.Q<DropdownField>("pattern-dropdown");
            _statusLabel     = _root.Q<Label>("status-label");
            _playerHpLabel   = _root.Q<Label>("player-hp-label");
            _playerBuffsLabel = _root.Q<Label>("player-buffs-label");
            _equippedList    = _root.Q<VisualElement>("equipped-list");
            _passiveList     = _root.Q<VisualElement>("passive-list");
            _logList         = _root.Q<VisualElement>("log-list");
        }

        void BindOptions()
        {
            _partDropdown.choices    = _partIds.Select(id => $"{id} - {ResolvePartName(id)}").ToList();
            _partDropdown.index = 0;
            _colorDropdown.choices   = _colorIds.Select(id => $"{id} - {ResolveColorName(id)}").ToList();
            _colorDropdown.index = 0;
            _patternDropdown.choices = _patternIds.Select(id => $"{id} - {ResolvePatternName(id)}").ToList();
            _patternDropdown.index = 0;
        }

        void BindButtons()
        {
            var equipBtn = _root.Q<Button>("equip-button");
            var clearBtn = _root.Q<Button>("clear-button");

            equipBtn.clicked += () =>
            {
                int partId    = _partIds[Math.Max(0, _partDropdown.index)];
                int colorId   = _colorIds[Math.Max(0, _colorDropdown.index)];
                int patternId = _patternIds[Math.Max(0, _patternDropdown.index)];
                bool ok = _tattoo.Equip(partId, colorId, patternId);
                _statusLabel.text = ok
                    ? $"装入: {ResolvePartName(partId)} × {ResolveColorName(colorId)} × {ResolvePatternName(patternId)}"
                    : "装入失败（见 Console）";
            };

            clearBtn.clicked += () =>
            {
                _tattoo.Clear();
                _statusLabel.text = "已清空 Build";
            };
        }

        // ===== 事件订阅 =====

        void SubscribeEvents()
        {
            _subBuildChanged  = _bus.Subscribe<BuildChangedEvent>(_ => RefreshEquipped());
            _subPassive       = _bus.Subscribe<PassiveRecomputedEvent>(_ => RefreshPassive());
            _subEffect        = _bus.Subscribe<EffectAppliedEvent>(e => AppendEffectLog(e.Results));
            _subTargetKilled  = _bus.Subscribe<TargetKilledEvent>(e => AppendLog($"<击杀> {e.Target.Name}", "log-entry-ext"));
            _subPlayerDied    = _bus.Subscribe<PlayerDiedEvent>(_  => AppendLog("<玩家阵亡>", "log-entry-pending"));
        }

        // ===== 名称解析（容错：DataTable 还没就绪时返回 Id）=====

        string ResolvePartName(int id)
        {
            try { return _runner.GetModule<DataTableModule>().GetTable<TattooPartConfig>().GetById(id).Name; }
            catch { return id.ToString(); }
        }
        string ResolveColorName(int id)
        {
            try { return _runner.GetModule<DataTableModule>().GetTable<TattooColorConfig>().GetById(id).Name; }
            catch { return id.ToString(); }
        }
        string ResolvePatternName(int id)
        {
            try { return _runner.GetModule<DataTableModule>().GetTable<TattooPatternConfig>().GetById(id).Name; }
            catch { return id.ToString(); }
        }

        // ===== 刷新视图 =====

        void RefreshPlayerState()
        {
            if (_spawner == null || _spawner.PlayerTarget == null) return;
            _playerHpLabel.text = $"HP {_spawner.PlayerTarget.Health:F0} / {100f}";
            var buffs = _tattoo.Player.Buffs;
            _playerBuffsLabel.text = "Buffs: " + (buffs.Count == 0 ? "-" : string.Join(", ", buffs.TakeLast(4)));
        }

        void RefreshEquipped()
        {
            _equippedList.Clear();
            foreach (var slot in _tattoo.Equipped)
            {
                var lbl = new Label($"· {slot.PartName} × {slot.ColorName} × {slot.PatternName}");
                lbl.AddToClassList("info");
                _equippedList.Add(lbl);
            }
            RefreshPlayerState();
        }

        void RefreshPassive()
        {
            _passiveList.Clear();
            foreach (var entry in _tattoo.Player.Passive.EntryLog)
            {
                var lbl = new Label($"· {entry}");
                lbl.AddToClassList("info");
                _passiveList.Add(lbl);
            }
        }

        void AppendEffectLog(IReadOnlyList<EffectResult> results)
        {
            foreach (var r in results)
            {
                var text = $"→ {r.Part} | {r.Element} | {r.Shape}: 伤={r.Damage:F1} 命中={r.HitCount} 联动={r.SynergyMul:F2}× [{r.Status}] {r.Note}";
                var cls = r.Note != null && r.Note.Contains("OnHitExtra") ? "log-entry-ext"
                        : r.Status == "ConsumedPending" ? "log-entry-pending"
                        : "log-entry";
                AppendLog(text, cls);
            }
            RefreshPlayerState();
        }

        void AppendLog(string text, string cssClass)
        {
            var lbl = new Label(text);
            lbl.AddToClassList(cssClass);
            _logList.Add(lbl);
            while (_logList.childCount > MaxLogEntries) _logList.RemoveAt(0);
            // 滚动到底
            var sv = _logList.parent as ScrollView;
            sv?.ScrollTo(lbl);
        }

        // ===== IUIForm =====

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            _root.style.display = newState == GameState.InGame || newState == GameState.MainMenu
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
