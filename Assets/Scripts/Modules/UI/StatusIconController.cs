using System;
using System.Collections.Generic;
using AttackSystem.Events;
using DG.Tweening;
using Tattoo.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoo.UI
{
    /// <summary>
    /// 头顶状态图标控制器（MonoBehaviour）。
    ///
    /// 挂点：EnemyActor / PlayerActor Prefab 的头顶锚点 GameObject。
    /// Prefab 关联：运行时动态创建 World Space Canvas；调用方在 Awake/Start 后
    /// 通过 <see cref="SetTarget"/> 绑定对应 <see cref="Target"/>。
    ///
    /// 图标槽固定 3 个（burn / poison / stun），水平排列，间距 8px，图标 32×32。
    /// 有则显示，无则隐藏；不因隐藏重排布局（固定槽位）。
    ///
    /// DOTween 动画：
    ///   出现：DOFade 0→1 + DOScale 0.5→1.2→1（0.15s ScalePop）
    ///   消失：DOFade 1→0 + DOScale 1→0.5（0.2s ScaleShrink）
    ///
    /// Sprite 路径（Resources.Load）：
    ///   Sprite/UI/StatusIcon/burn
    ///   Sprite/UI/StatusIcon/poison
    ///   Sprite/UI/StatusIcon/stun
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIconController : MonoBehaviour
    {
        // ── 常量 ─────────────────────────────────────────────────
        const float CanvasHeightOffset = 1.5f;
        const float IconSize           = 32f;
        const float IconSpacing        = 8f;

        // 固定槽顺序（与规格一致）
        static readonly string[] SlotNames = { "burn", "poison", "stun" };

        // DOTween 动画参数
        const float AppearDuration  = 0.15f;
        const float DisappearDuration = 0.2f;

        // ── 内部数据结构 ─────────────────────────────────────────

        sealed class IconSlot
        {
            public readonly string    StatusName;
            public readonly GameObject Root;        // 槽根 GameObject（始终存在，控制 active）
            public readonly Image     Img;
            public readonly CanvasGroup CG;
            public bool Active;

            public IconSlot(string statusName, GameObject root, Image img, CanvasGroup cg)
            {
                StatusName = statusName;
                Root       = root;
                Img        = img;
                CG         = cg;
                Active     = false;
            }
        }

        // ── 运行时状态 ───────────────────────────────────────────
        Target                    _target;
        readonly List<IDisposable> _subs = new();

        // statusName → IconSlot（小写 key，容纳大小写不敏感匹配）
        readonly Dictionary<string, IconSlot> _slots = new(StringComparer.OrdinalIgnoreCase);

        // DOTween Kill key——避免 Update GC，直接用 slot.Root 作为 target
        // （DOTween.Kill(root) 杀掉该 target 上所有 tween）

        // ── 依赖获取 ─────────────────────────────────────────────
        EventBus _bus;

        // ── MonoBehaviour ────────────────────────────────────────

        void Awake()
        {
            BuildWorldCanvas();
            LoadSprites();
        }

        void Start()
        {
            // 等待 GameApp 就绪后订阅事件
            StartCoroutine(WaitAndSubscribe());
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            DOTween.Kill(transform); // 清理挂在本 transform 上的动画（无）
            foreach (var slot in _slots.Values)
            {
                if (slot.Root != null) DOTween.Kill(slot.Root.transform);
            }
        }

        // ── 公共 API ─────────────────────────────────────────────

        /// <summary>
        /// 绑定对应战斗目标引用。Actor 在 Awake/Start 后调用。
        /// 通过引用相等过滤事件，比 Name 字符串更可靠。
        /// </summary>
        public void SetTarget(Target target)
        {
            _target = target;
        }

        // ── 内部：Canvas + 槽位创建 ─────────────────────────────

        void BuildWorldCanvas()
        {
            // 创建世界空间 Canvas（子物体，跟随 actor）
            var canvasGO = new GameObject("StatusCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, CanvasHeightOffset, 0f);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var rt = canvas.GetComponent<RectTransform>();
            // 画布宽高仅作 WorldSpace 参考，实际渲染由子元素决定
            rt.sizeDelta = new Vector2(200f, 50f);
            rt.localScale = Vector3.one * 0.01f; // 1 pixel = 0.01 unit

            // 横向布局根
            var rowGO = new GameObject("IconRow");
            rowGO.transform.SetParent(canvasGO.transform, false);
            var rowRT = rowGO.AddComponent<RectTransform>();
            rowRT.anchoredPosition = Vector2.zero;

            // 创建 3 个固定槽位
            float totalWidth = SlotNames.Length * IconSize + (SlotNames.Length - 1) * IconSpacing;
            float startX     = -totalWidth * 0.5f + IconSize * 0.5f;

            for (int i = 0; i < SlotNames.Length; i++)
            {
                string sName = SlotNames[i];

                var slotGO = new GameObject(sName + "_slot");
                slotGO.transform.SetParent(rowGO.transform, false);

                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.sizeDelta        = new Vector2(IconSize, IconSize);
                slotRT.anchoredPosition = new Vector2(startX + i * (IconSize + IconSpacing), 0f);

                var cg  = slotGO.AddComponent<CanvasGroup>();
                var img = slotGO.AddComponent<Image>();
                img.preserveAspect = true;

                cg.alpha = 0f;
                slotGO.SetActive(false);

                _slots[sName] = new IconSlot(sName, slotGO, img, cg);
            }
        }

        void LoadSprites()
        {
            foreach (var slot in _slots.Values)
            {
                // Resources.Load 路径不含扩展名
                var sp = Resources.Load<Sprite>($"Sprite/UI/StatusIcon/{slot.StatusName}");
                if (sp != null)
                {
                    slot.Img.sprite = sp;
                }
                else
                {
                    FrameworkLogger.Warn("StatusIconController",
                        $"Action=LoadSprite Status={slot.StatusName} Err=Sprite not found at Sprite/UI/StatusIcon/{slot.StatusName}");
                }
            }
        }

        // ── 等待 GameApp 并订阅 ──────────────────────────────────

        System.Collections.IEnumerator WaitAndSubscribe()
        {
            GameApp app = null;
            float timeout = Time.unscaledTime + 10f;
            while (Time.unscaledTime < timeout)
            {
                app = FindObjectOfType<GameApp>();
                EventBus bus = null;
                ModuleRunner runner = null;
                if (app != null && app.TryGetRuntime(out bus, out runner))
                {
                    _bus = bus;
                    break;
                }
                yield return null;
            }

            if (_bus == null)
            {
                FrameworkLogger.Error("StatusIconController",
                    "Action=WaitAndSubscribe Err=GameApp 就绪超时，事件订阅跳过");
                yield break;
            }

            _subs.Add(_bus.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied));
            _subs.Add(_bus.Subscribe<StatusEffectExpiredEvent>(OnStatusExpired));

            FrameworkLogger.Info("StatusIconController",
                $"Action=Subscribed Target={_target?.Name ?? "unbound"}");
        }

        // ── 事件处理 ─────────────────────────────────────────────

        void OnStatusApplied(StatusEffectAppliedEvent e)
        {
            // 过滤：仅处理绑定 target（引用相等）
            if (_target == null || !ReferenceEquals(e.Target, _target)) return;

            string key = e.StatusName;
            if (!_slots.TryGetValue(key, out var slot)) return;  // 不认识的状态，忽略
            if (slot.Active) return;                              // 已显示，幂等

            slot.Active = true;
            slot.Root.SetActive(true);

            // Kill 上次动画，防止残留 tween 交叉
            DOTween.Kill(slot.Root.transform);

            // DOFade 0→1
            slot.CG.alpha = 0f;
            DOTween.To(() => slot.CG.alpha, v => slot.CG.alpha = v, 1f, AppearDuration)
                   .SetUpdate(true)
                   .SetLink(slot.Root);

            // DOScale 0.5→1.2→1（ScalePop）
            slot.Root.transform.localScale = Vector3.one * 0.5f;
            slot.Root.transform.DOScale(1.2f, AppearDuration * 0.6f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(slot.Root)
                .OnComplete(() =>
                    slot.Root.transform.DOScale(1f, AppearDuration * 0.4f)
                        .SetEase(Ease.InQuad)
                        .SetUpdate(true)
                        .SetLink(slot.Root));
        }

        void OnStatusExpired(StatusEffectExpiredEvent e)
        {
            // 过滤：仅处理绑定 target（引用相等）
            if (_target == null || !ReferenceEquals(e.Target, _target)) return;

            string key = e.StatusName;
            if (!_slots.TryGetValue(key, out var slot)) return;
            if (!slot.Active) return;

            slot.Active = false;

            // Kill 上次动画
            DOTween.Kill(slot.Root.transform);

            // DOFade 1→0 + DOScale 1→0.5（ScaleShrink）
            DOTween.To(() => slot.CG.alpha, v => slot.CG.alpha = v, 0f, DisappearDuration)
                   .SetUpdate(true)
                   .SetLink(slot.Root);

            slot.Root.transform.DOScale(0.5f, DisappearDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetLink(slot.Root)
                .OnComplete(() =>
                {
                    // 动画结束后再隐藏，确保 Shrink 效果可见
                    if (slot.Root != null) slot.Root.SetActive(false);
                });
        }
    }
}
