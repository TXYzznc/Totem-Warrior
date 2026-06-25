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
        VisualElement _partPreview;
        VisualElement _colorPreview;
        VisualElement _patternPreview;
        Label _statusLabel;
        Label _playerHpLabel;
        Label _playerBuffsLabel;
        VisualElement _equippedList;
        VisualElement _passiveList;
        VisualElement _logList;

        ResourceModule _resource;
        // Name → ResourceId 静态映射（与 ResourceConfig.json 保持一致）
        static readonly Dictionary<string, int> PartIcons = new()
        {
            { "Head", 1001 }, { "Torso", 1002 }, { "LeftArm", 1003 },
            { "RightArm", 1004 }, { "LeftLeg", 1005 }, { "RightLeg", 1006 },
        };
        static readonly Dictionary<string, int> ColorIcons = new()
        {
            { "Red", 1101 }, { "Yellow", 1102 }, { "Green", 1103 }, { "Blue", 1104 },
            { "Purple", 1105 }, { "Gold", 1106 }, { "White", 1107 },
        };
        static readonly Dictionary<string, int> PatternIcons = new()
        {
            { "Line", 1201 }, { "Ring", 1202 }, { "Spiral", 1203 }, { "Zigzag", 1204 },
            { "Bolt", 1205 }, { "Star", 1206 }, { "Stream", 1207 }, { "Beast", 1208 },
        };

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

            _tattoo   = _runner.GetModule<TattooModule>();
            _spawner  = _runner.GetModule<SpawnerModule>();
            _resource = _runner.GetModule<ResourceModule>();
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
            _partPreview     = _root.Q<VisualElement>("part-preview");
            _colorPreview    = _root.Q<VisualElement>("color-preview");
            _patternPreview  = _root.Q<VisualElement>("pattern-preview");
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
            _partDropdown.RegisterValueChangedCallback(_ => RefreshSelectionPreview());

            _colorDropdown.choices   = _colorIds.Select(id => $"{id} - {ResolveColorName(id)}").ToList();
            _colorDropdown.index = 0;
            _colorDropdown.RegisterValueChangedCallback(_ => RefreshSelectionPreview());

            _patternDropdown.choices = _patternIds.Select(id => $"{id} - {ResolvePatternName(id)}").ToList();
            _patternDropdown.index = 0;
            _patternDropdown.RegisterValueChangedCallback(_ => RefreshSelectionPreview());

            RefreshSelectionPreview();
        }

        void RefreshSelectionPreview()
        {
            int partId    = _partIds[Math.Max(0, _partDropdown.index)];
            int colorId   = _colorIds[Math.Max(0, _colorDropdown.index)];
            int patternId = _patternIds[Math.Max(0, _patternDropdown.index)];
            ApplySpriteByName(_partPreview,    PartIcons,    ResolvePartName(partId));
            ApplySpriteByName(_colorPreview,   ColorIcons,   ResolveColorName(colorId));
            ApplySpriteByName(_patternPreview, PatternIcons, ResolvePatternName(patternId));
        }

        void ApplySpriteByName(VisualElement target, Dictionary<string, int> map, string name)
        {
            if (target == null) return;
            if (_resource != null && map.TryGetValue(name, out var resId))
            {
                var sprite = _resource.Load<Sprite>(resId);
                if (sprite != null)
                {
                    target.style.backgroundImage = new StyleBackground(sprite);
                    return;
                }
            }
            // 资源缺失时清空（占位 CSS 背景生效）
            target.style.backgroundImage = StyleKeyword.None;
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
                var row = new VisualElement();
                row.AddToClassList("equipped-slot");

                row.Add(MakeSlotIcon(PartIcons,    slot.PartName));
                row.Add(MakeSlotIcon(ColorIcons,   slot.ColorName));
                row.Add(MakeSlotIcon(PatternIcons, slot.PatternName));

                var lbl = new Label($"{slot.PartName} × {slot.ColorName} × {slot.PatternName}");
                lbl.AddToClassList("slot-label");
                row.Add(lbl);

                _equippedList.Add(row);
            }
            RefreshPlayerState();
        }

        VisualElement MakeSlotIcon(Dictionary<string, int> map, string name)
        {
            var icon = new VisualElement();
            icon.AddToClassList("slot-icon");
            if (_resource != null && map.TryGetValue(name, out var resId))
            {
                var sp = _resource.Load<Sprite>(resId);
                if (sp != null) icon.style.backgroundImage = new StyleBackground(sp);
            }
            return icon;
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
