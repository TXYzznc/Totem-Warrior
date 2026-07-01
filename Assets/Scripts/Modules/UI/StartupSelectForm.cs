using System;
using System.Collections.Generic;
using AttackSystem.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// change #20 起手 Build 三选 UI（颜料 3 + 武器 5 + 图案 2）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/StartupSelect.prefab（fan-out 阶段美术建）
    /// 触发条件：CharacterSelectForm.OnNextClicked / 进入 InGame 状态前
    /// 关闭条件：OnConfirm 后自闭；SpawnerModule 接 StartupSelectedEvent 后装备玩家
    ///
    /// 三段 UI 布局：
    ///   颜料区：3 张卡片（红 / 蓝 / 黄 占位；从 TattooColorConfig 取候选）
    ///   武器区：5 张卡片（knife/hammer/pistol/bow/fist）
    ///   图案区：2 张卡片（Line / Ring，由 SaveData.PatternUnlocks 过滤）
    ///   底部：[确定] 按钮
    /// </summary>
    public sealed class StartupSelectForm : MonoBehaviour, IUIForm, IUIFormBootstrap
    {
        [Header("根容器")]
        [SerializeField] Transform _colorRoot;
        [SerializeField] Transform _weaponRoot;
        [SerializeField] Transform _patternRoot;

        [Header("按钮")]
        [SerializeField] Button _confirmBtn;
        [SerializeField] Button _cancelBtn;

        // ─── 运行时 ────────────────────────────────────────────
        EventBus     _bus;
        ModuleRunner _runner;

        // 当前选择
        int    _selectedColorId = -1;
        string _selectedWeaponId;
        readonly List<int> _selectedPatternIds = new();

        // 卡片 GameObject 缓存，用于高亮
        readonly List<GameObject> _colorCards   = new();
        readonly List<GameObject> _weaponCards  = new();
        readonly List<GameObject> _patternCards = new();

        // 各列表对应的 id（与上面列表同索引）
        readonly List<int>    _colorIds   = new();
        readonly List<string> _weaponIds  = new();
        readonly List<int>    _patternIds = new();

        readonly List<IDisposable> _subs = new();

        static readonly Color NormalTint    = Color.white;
        static readonly Color SelectedTint  = new Color(0.5f, 1f, 0.5f);

        // ── 素材路径映射（change #21 landing）──
        // 颜料 ColorId → sprite 路径（不带扩展名，Resources.Load 用）
        static readonly Dictionary<int, string> ColorSpritePaths = new()
        {
            { 1, "Sprite/Paints/paint_red_common"    },
            { 2, "Sprite/Paints/paint_yellow_common" },
            { 4, "Sprite/Paints/paint_blue_common"   },
        };
        // 武器 WeaponId → sprite 路径
        static readonly Dictionary<string, string> WeaponSpritePaths = new()
        {
            { "knife_basic",  "Sprite/Weapons/weapon_short_blade"  },
            { "hammer_heavy", "Sprite/Weapons/weapon_heavy_hammer" },
            { "pistol_basic", "Sprite/Weapons/weapon_pistol"       },
            { "bow_charge",   "Sprite/Weapons/weapon_bow"          },
            { "energy_fist",  "Sprite/Weapons/weapon_energy_fist"  },
        };
        // 图案 PatternId → sprite 路径
        static readonly Dictionary<int, string> PatternSpritePaths = new()
        {
            { 1, "Sprite/Tattoo/Pattern/Line" },
            { 2, "Sprite/Tattoo/Pattern/Ring" },
        };

        static Sprite LoadSpriteSafe(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return Resources.Load<Sprite>(path);
        }

        // ── IUIForm ───────────────────────────────────────────

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // 进入 InGame（开局确认后）或回 MainMenu 时自动关闭，避免遮挡 HUD
            if (gameObject.activeSelf && newState != GameState.Loading)
                Close();
        }

        // ── MonoBehaviour ─────────────────────────────────────

        void Awake()
        {
            gameObject.SetActive(false);

            // 按名兜底绑定，与 PauseMenuForm 同款
            if (_colorRoot    == null) _colorRoot    = FindChildTransform("ColorRoot");
            if (_weaponRoot   == null) _weaponRoot   = FindChildTransform("WeaponRoot");
            if (_patternRoot  == null) _patternRoot  = FindChildTransform("PatternRoot");
            if (_confirmBtn   == null) _confirmBtn   = FindChildComponent<Button>("ConfirmButton");
            if (_cancelBtn    == null) _cancelBtn    = FindChildComponent<Button>("CancelButton");
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
            _confirmBtn?.onClick.AddListener(OnConfirm);
            _cancelBtn?.onClick.AddListener(OnCancel);
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
            BuildOptions();
            FrameworkLogger.Info("StartupSelectForm", "Action=Open");
        }

        void Close()
        {
            gameObject.SetActive(false);
            FrameworkLogger.Info("StartupSelectForm", "Action=Close");
        }

        // ── 选项构建 ──────────────────────────────────────────

        /// <summary>
        /// 从 DataTable 拉颜料 / 武器 / 图案候选 → 实例化卡片到对应 Root。
        /// 每次 Open 时重建，保证数据新鲜。
        /// </summary>
        void BuildOptions()
        {
            // 重置选择状态
            _selectedColorId  = -1;
            _selectedWeaponId = null;
            _selectedPatternIds.Clear();

            ClearCards(_colorRoot,   _colorCards,   _colorIds);
            ClearCards(_weaponRoot,  _weaponCards,  _weaponIds);
            ClearCards(_patternRoot, _patternCards, _patternIds);

            var dt = _runner.GetModule<DataTableModule>();

            // ── 颜料：取前 3（红/蓝/黄占位，Id=1/4/2） ──────────
            var colorTable = dt.GetTable<TattooColorConfig>();
            int colorAdded = 0;
            int[] preferredColorIds = { 1, 4, 2 }; // Red / Blue / Yellow
            foreach (int cid in preferredColorIds)
            {
                if (colorAdded >= 3) break;
                if (!colorTable.TryGetById(cid, out var row)) continue;
                int captureId = row.Id;
                Sprite icon = LoadSpriteSafe(ColorSpritePaths.TryGetValue(cid, out var p) ? p : null);
                var go = CreateCard($"Color_{row.Name}", _colorRoot, row.Name, icon, () => SetSelectedColor(captureId));
                _colorCards.Add(go);
                _colorIds.Add(row.Id);
                colorAdded++;
            }
            // 若 preferredColorIds 不够 3 个则从 All 补
            if (colorAdded < 3)
            {
                foreach (var kv in colorTable.All)
                {
                    if (colorAdded >= 3) break;
                    bool already = false;
                    foreach (int pid in preferredColorIds) if (pid == kv.Key) { already = true; break; }
                    if (already) continue;
                    int captureId = kv.Value.Id;
                    Sprite icon = LoadSpriteSafe(ColorSpritePaths.TryGetValue(kv.Key, out var p) ? p : null);
                    var go = CreateCard($"Color_{kv.Value.Name}", _colorRoot, kv.Value.Name, icon, () => SetSelectedColor(captureId));
                    _colorCards.Add(go);
                    _colorIds.Add(kv.Value.Id);
                    colorAdded++;
                }
            }

            // ── 武器：全 5 把 ──────────────────────────────────
            var weaponTable = dt.GetTable<WeaponConfig>();
            foreach (var kv in weaponTable.All)
            {
                string captureId = kv.Value.WeaponId;
                Sprite icon = LoadSpriteSafe(WeaponSpritePaths.TryGetValue(captureId, out var p) ? p : null);
                var go = CreateCard($"Weapon_{kv.Value.WeaponId}", _weaponRoot, kv.Value.Name, icon, () => SetSelectedWeapon(captureId));
                _weaponCards.Add(go);
                _weaponIds.Add(kv.Value.WeaponId);
            }

            // ── 图案：Meta 解锁过滤 ────────────────────────────
            var unlockedPatternIds = GetUnlockedPatternIds();
            var patternTable = dt.GetTable<TattooPatternConfig>();
            foreach (int pid in unlockedPatternIds)
            {
                if (!patternTable.TryGetById(pid, out var row)) continue;
                int captureId = row.Id;
                Sprite icon = LoadSpriteSafe(PatternSpritePaths.TryGetValue(pid, out var p) ? p : null);
                var go = CreateCard($"Pattern_{row.Name}", _patternRoot, row.Name, icon, () => ToggleSelectedPattern(captureId));
                _patternCards.Add(go);
                _patternIds.Add(row.Id);
            }

            RefreshConfirmButton();
        }

        /// <summary>
        /// 代码动态创建卡片 GameObject（IconBg + Icon + Name）。
        /// 结构 = 卡片框（tint 表达选中）+ 中央 icon（sprite）+ 底部文字。
        /// </summary>
        GameObject CreateCard(string goName, Transform parent, string label, Sprite icon, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 120f);

            var bg = go.AddComponent<Image>();
            bg.color = NormalTint;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            // Icon 子节点（居中）
            if (icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(80f, 80f);
                iconRect.anchoredPosition = new Vector2(0f, 8f);
                var iImg = iconGo.AddComponent<Image>();
                iImg.sprite = icon;
                iImg.raycastTarget = false;
                iImg.preserveAspect = true;
            }

            // Name 子节点（底部）
            var labelGo = new GameObject("Name", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);

            var txt = labelGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 14;
            txt.color     = Color.black;
            txt.raycastTarget = false;

            return go;
        }

        static void ClearCards<T>(Transform root, List<GameObject> cards, List<T> ids)
        {
            foreach (var c in cards)
                if (c != null) Destroy(c);
            cards.Clear();
            ids.Clear();
        }

        /// <summary>从 SaveData 取已解锁的图案 Id 列表；找不到 SaveModule 则兜底 [1, 2]。</summary>
        List<int> GetUnlockedPatternIds()
        {
            var result = new List<int>();

            SaveModule saveModule = null;
            try { saveModule = _runner.GetModule<SaveModule>(); } catch { }

            if (saveModule?.Data?.PatternUnlocks != null)
            {
                var patternTable = _runner.GetModule<DataTableModule>().GetTable<TattooPatternConfig>();
                // PatternUnlocks key = patternId(字符串), value = bool[6]
                // 只要 bool[] 中有任意 true（= 该图案的某个变体已解锁），就显示
                foreach (var kv in saveModule.Data.PatternUnlocks)
                {
                    bool anyUnlocked = false;
                    if (kv.Value != null)
                        foreach (var b in kv.Value)
                            if (b) { anyUnlocked = true; break; }
                    if (!anyUnlocked) continue;

                    // 反查 TattooPatternConfig.Name == kv.Key 得 Id
                    foreach (var pr in patternTable.All)
                    {
                        if (string.Equals(pr.Value.Name, kv.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(pr.Value.Id);
                            break;
                        }
                    }
                }
            }

            // 兜底：默认 [1, 2]（Line + Ring）
            if (result.Count == 0)
            {
                // TODO change#20: Meta 系统成熟后移除兜底，依赖真实 PatternUnlocks
                result.Add(1); // Line
                result.Add(2); // Ring
            }

            return result;
        }

        // ── 选择回调（卡片 onClick 调用）──────────────────────

        public void SetSelectedColor(int colorId)
        {
            _selectedColorId = colorId;
            ApplyHighlight(_colorCards, _colorIds, colorId);
            RefreshConfirmButton();
            FrameworkLogger.Info("StartupSelectForm", $"Action=SelectColor ColorId={colorId}");
        }

        public void SetSelectedWeapon(string weaponId)
        {
            _selectedWeaponId = weaponId;
            ApplyHighlightStr(_weaponCards, _weaponIds, weaponId);
            RefreshConfirmButton();
            FrameworkLogger.Info("StartupSelectForm", $"Action=SelectWeapon WeaponId={weaponId}");
        }

        public void ToggleSelectedPattern(int patternId)
        {
            if (_selectedPatternIds.Contains(patternId))
            {
                _selectedPatternIds.Remove(patternId);
            }
            else if (_selectedPatternIds.Count < 2)
            {
                _selectedPatternIds.Add(patternId);
            }
            ApplyHighlightPattern();
            RefreshConfirmButton();
            FrameworkLogger.Info("StartupSelectForm", $"Action=TogglePattern PatternId={patternId} Selected=[{string.Join(",", _selectedPatternIds)}]");
        }

        void RefreshConfirmButton()
        {
            bool ok = _selectedColorId > 0
                   && !string.IsNullOrEmpty(_selectedWeaponId)
                   && _selectedPatternIds.Count > 0;
            if (_confirmBtn != null) _confirmBtn.interactable = ok;
        }

        // ── 高亮辅助 ─────────────────────────────────────────

        static void ApplyHighlight(List<GameObject> cards, List<int> ids, int selectedId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var img = cards[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i < ids.Count && ids[i] == selectedId) ? SelectedTint : NormalTint;
            }
        }

        static void ApplyHighlightStr(List<GameObject> cards, List<string> ids, string selectedId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var img = cards[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i < ids.Count && ids[i] == selectedId) ? SelectedTint : NormalTint;
            }
        }

        void ApplyHighlightPattern()
        {
            for (int i = 0; i < _patternCards.Count; i++)
            {
                if (_patternCards[i] == null) continue;
                var img = _patternCards[i].GetComponent<Image>();
                if (img != null)
                {
                    bool sel = i < _patternIds.Count && _selectedPatternIds.Contains(_patternIds[i]);
                    img.color = sel ? SelectedTint : NormalTint;
                }
            }
        }

        // ── 确认 / 取消 ───────────────────────────────────────

        /// <summary>确认按钮：发 StartupSelectedEvent → SpawnerModule 装备玩家 → 自闭。</summary>
        public void OnConfirm()
        {
            if (_bus == null) return;

            if (_selectedColorId <= 0 || string.IsNullOrEmpty(_selectedWeaponId) || _selectedPatternIds.Count == 0)
            {
                FrameworkLogger.Warn("StartupSelectForm", "Action=OnConfirm Rejected=三选未齐全");
                return;
            }

            var patArr = _selectedPatternIds.ToArray();
            _bus.Publish(new StartupSelectedEvent(_selectedColorId, _selectedWeaponId, patArr));

            FrameworkLogger.Info("StartupSelectForm",
                $"Action=Confirm Color={_selectedColorId} Weapon={_selectedWeaponId} Patterns=[{string.Join(",", patArr)}]");

            // change#21：起手确认 → 切 InGame，触发 RunStartedEvent（HUD 初始化等）
            try
            {
                var gs = _runner?.GetModule<GameStateModule>();
                if (gs != null && gs.CurrentState != GameState.InGame)
                {
                    gs.StartGame();
                    FrameworkLogger.Info("StartupSelectForm", "Action=Confirm → GameState.InGame");
                }
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error("StartupSelectForm",
                    $"Action=Confirm StartGame Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            }

            Close();
        }

        void OnCancel()
        {
            // change#21：取消 → 回到 CharacterSelect（让玩家重选角色 / 再进武器）
            Close();
            var charSel = UnityEngine.Object.FindObjectOfType<CharacterSelectForm>(true);
            if (charSel != null)
            {
                charSel.Open();
                FrameworkLogger.Info("StartupSelectForm", "Action=Cancel → CharacterSelectForm.Open");
            }
        }
    }
}
