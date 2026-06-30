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
                var go = CreateCard($"Color_{row.Name}", _colorRoot, row.Name, () => SetSelectedColor(captureId));
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
                    var go = CreateCard($"Color_{kv.Value.Name}", _colorRoot, kv.Value.Name, () => SetSelectedColor(captureId));
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
                var go = CreateCard($"Weapon_{kv.Value.WeaponId}", _weaponRoot, kv.Value.Name, () => SetSelectedWeapon(captureId));
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
                var go = CreateCard($"Pattern_{row.Name}", _patternRoot, row.Name, () => ToggleSelectedPattern(captureId));
                _patternCards.Add(go);
                _patternIds.Add(row.Id);
            }

            RefreshConfirmButton();
        }

        /// <summary>
        /// 代码动态创建卡片 GameObject（Image + Button + Text）。
        /// Prefab 缺失时兜底，不阻塞开发进度。
        /// </summary>
        GameObject CreateCard(string goName, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 120f);

            var bg = go.AddComponent<Image>();
            bg.color = NormalTint;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            // 文本子节点
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var txt = labelGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 14;
            txt.color     = Color.black;

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

            Close();
        }

        void OnCancel()
        {
            // TODO change#20: 取消 → 回主菜单或保持 CharacterSelect（待 gd-lead 决策）
            Close();
        }
    }
}
