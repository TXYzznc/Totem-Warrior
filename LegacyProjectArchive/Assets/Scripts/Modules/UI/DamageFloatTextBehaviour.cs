using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Tattoo.Events;
using Weapon.Events;
using UnityEngine;

namespace Tattoo.UI
{
    /// <summary>
    /// 伤害飘字管理器（change 19-A 暴击数字飘字）。
    ///
    /// 挂载点：GameApp 同级 GameObject，DontDestroyOnLoad。
    ///
    /// 订阅：
    ///   CritHitEvent                           → 红色大字 #FF2222 fontSize=36，弹出 Scale 0→1.2→1 + 上浮 1.2 单位 0.7s
    ///   WeaponAttackHitEvent（IsCrit==false）  → 白色 #FFFFFF fontSize=24，上浮 0.8 单位 0.6s + 淡出
    ///
    /// 命中位置 fallback：两事件均不携带世界坐标（Target 为纯数据类，无 Transform）。
    /// 飘字统一在 Vector3.zero（世界原点）附加随机水平扰动生成。
    /// 若后续 Target 引入 Position 字段，修改 GetSpawnPosition 即可。
    ///
    /// 对象池：8 个 TextMeshProUGUI 节点，挂载于 World Space Canvas（DamageFloatCanvas）下。
    /// </summary>
    public sealed class DamageFloatTextBehaviour : MonoBehaviour
    {
        // ── 常量 ────────────────────────────────────────────────
        private const int    PoolSize         = 8;
        private const float  NormalDuration   = 0.6f;
        private const float  NormalRiseY      = 0.8f;
        private const float  NormalFadeStart  = 0.4f;
        private const float  CritRiseY        = 1.2f;
        private const float  CritDuration     = 0.7f;
        private const float  CritFadeStart    = 0.5f;
        private const int    NormalFontSize   = 24;
        private const int    CritFontSize     = 36;
        private const string CanvasName       = "DamageFloatCanvas";
        private const float  BusWaitTimeout   = 10f;

        // ── 静态颜色（避免运行时 new Color GC）─────────────────
        private static readonly Color ColorNormal = new Color(1f, 1f, 1f, 1f);
        private static readonly Color ColorCrit   = new Color(1f, 0.133f, 0.133f, 1f); // #FF2222

        // ── 对象池 ──────────────────────────────────────────────
        private Canvas                  _canvas;
        private Queue<TextMeshProUGUI>  _pool;

        // ── EventBus 订阅句柄 ───────────────────────────────────
        private IDisposable _subCrit;
        private IDisposable _subWeapon;

        // ============================================================
        // 生命周期
        // ============================================================

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitCanvas();
            InitPool();
        }

        private async void Start()
        {
            var bus = await WaitForBusAsync();
            if (bus == null)
            {
                FrameworkLogger.Error("DamageFloatTextBehaviour",
                    "Action=Start 等待 GameApp 就绪超时，飘字系统未能启动");
                return;
            }
            RegisterSubscriptions(bus);
            FrameworkLogger.Info("DamageFloatTextBehaviour", "Action=Ready");
        }

        private void OnDestroy()
        {
            DisposeSubscriptions();
            DOTween.Kill(gameObject);
        }

        // ============================================================
        // 等待 GameApp 就绪
        // ============================================================

        private static async UniTask<EventBus> WaitForBusAsync()
        {
            float deadline = Time.unscaledTime + BusWaitTimeout;
            while (Time.unscaledTime < deadline)
            {
                var app = FindObjectOfType<GameApp>();
                if (app != null && app.TryGetRuntime(out var bus, out _))
                    return bus;
                await UniTask.Yield();
            }
            return null;
        }

        // ============================================================
        // 初始化
        // ============================================================

        private void InitCanvas()
        {
            var canvasGo = GameObject.Find(CanvasName);
            if (canvasGo == null)
            {
                canvasGo = new GameObject(CanvasName);
                DontDestroyOnLoad(canvasGo);
            }

            _canvas = canvasGo.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode   = RenderMode.WorldSpace;
                _canvas.sortingOrder = 100;
                var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;
            }
        }

        private void InitPool()
        {
            _pool = new Queue<TextMeshProUGUI>(PoolSize);
            for (int i = 0; i < PoolSize; i++)
            {
                var go  = new GameObject("DmgFloat_" + i, typeof(TextMeshProUGUI));
                go.transform.SetParent(_canvas.transform, false);
                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.alignment          = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                go.SetActive(false);
                _pool.Enqueue(tmp);
            }
        }

        private void RegisterSubscriptions(EventBus bus)
        {
            _subCrit   = bus.Subscribe<CritHitEvent>(OnCritHit);
            _subWeapon = bus.Subscribe<WeaponAttackHitEvent>(OnWeaponAttackHit);
        }

        private void DisposeSubscriptions()
        {
            _subCrit?.Dispose();   _subCrit   = null;
            _subWeapon?.Dispose(); _subWeapon = null;
        }

        // ============================================================
        // 事件处理
        // ============================================================

        private void OnCritHit(CritHitEvent e)
        {
            SpawnText(damage: e.BaseDamage, position: Vector3.zero, isCrit: true);
        }

        private void OnWeaponAttackHit(WeaponAttackHitEvent e)
        {
            // IsCrit==true 由 CritHitEvent 处理，避免重复弹字
            if (e.IsCrit) return;
            SpawnText(damage: e.BaseDamage, position: Vector3.zero, isCrit: false);
        }

        // ============================================================
        // 飘字生成
        // ============================================================

        private void SpawnText(float damage, Vector3 position, bool isCrit)
        {
            var tmp = GetFromPool();
            if (tmp == null) return;

            tmp.color    = isCrit ? ColorCrit : ColorNormal;
            tmp.fontSize = isCrit ? CritFontSize : NormalFontSize;
            tmp.SetText(Mathf.RoundToInt(damage).ToString());

            // 随机水平扰动，防止多字重叠
            float offsetX = UnityEngine.Random.Range(-0.3f, 0.3f);
            tmp.transform.position   = position + new Vector3(offsetX, 0f, 0f);
            tmp.transform.localScale = Vector3.one;

            var cg = GetOrAddCanvasGroup(tmp.gameObject);
            cg.alpha = 1f;

            tmp.gameObject.SetActive(true);

            if (isCrit)
                PlayCritSequence(tmp, cg);
            else
                PlayNormalSequence(tmp, cg);
        }

        // 普通：上浮 0.8 单位 0.6s，在 0.4-0.6s 区间淡出
        private void PlayNormalSequence(TextMeshProUGUI tmp, CanvasGroup cg)
        {
            var startPos = tmp.transform.position;
            var seq = DOTween.Sequence().SetLink(tmp.gameObject);
            seq.Join(tmp.transform
                .DOMove(startPos + Vector3.up * NormalRiseY, NormalDuration)
                .SetEase(Ease.OutCubic));
            seq.Insert(NormalFadeStart,
                cg.DOFade(0f, NormalDuration - NormalFadeStart));
            seq.OnComplete(() => ReturnToPool(tmp));
        }

        // 暴击：弹出 Scale 0→1.2→1，再上浮 1.2 单位 0.7s，在 0.5-0.7s 区间淡出
        private void PlayCritSequence(TextMeshProUGUI tmp, CanvasGroup cg)
        {
            var startPos = tmp.transform.position;
            tmp.transform.localScale = Vector3.zero;

            var seq = DOTween.Sequence().SetLink(tmp.gameObject);
            // 弹出：Scale 0 → 1.2 → 1
            seq.Append(tmp.transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutBack));
            seq.Append(tmp.transform.DOScale(1f,   0.05f).SetEase(Ease.InOutSine));
            // 上浮（与弹出同步开始）
            seq.Join(tmp.transform
                .DOMove(startPos + Vector3.up * CritRiseY, CritDuration)
                .SetEase(Ease.OutCubic));
            // 淡出
            seq.Insert(CritFadeStart, cg.DOFade(0f, CritDuration - CritFadeStart));
            seq.OnComplete(() => ReturnToPool(tmp));
        }

        // ============================================================
        // 对象池管理
        // ============================================================

        private TextMeshProUGUI GetFromPool()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            // 池耗尽：强制回收最早激活的节点
            foreach (Transform child in _canvas.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                DOTween.Kill(child.gameObject);
                var old = child.GetComponent<TextMeshProUGUI>();
                if (old != null)
                {
                    child.gameObject.SetActive(false);
                    return old;
                }
            }
            return null;
        }

        private void ReturnToPool(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            tmp.gameObject.SetActive(false);
            tmp.transform.localScale = Vector3.one;
            _pool.Enqueue(tmp);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            return cg != null ? cg : go.AddComponent<CanvasGroup>();
        }
    }
}
