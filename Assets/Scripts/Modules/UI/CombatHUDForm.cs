using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;          // DOTween.To / SetUpdate
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tattoo.Data;          // EffectResult / TattooSlot
using Tattoo.Events;        // BossSpawnedEvent / BossPhaseChangedEvent / DamagedEvent / SkillCastEvent
using Economy.Events;       // ItemPickedEvent / ActorDiedEvent / CoinChangedEvent
using MapGen.Events;        // ZoneShrinkPhaseEvent

namespace Tattoo.UI
{
    /// <summary>
    /// 战斗 HUD（UGUI 版，v2.1）。
    ///
    /// Prefab 落点：Assets/Resources/Prefab/UI/CombatHUD.prefab
    /// 场景放置：Launch.unity → UICanvas（Sort Order 0）→ CombatHUD GameObject
    ///
    /// 结构（SerializeField 绑定，不做 Q&lt;T&gt;）：
    ///   HP 条    ← _hpBar (Image, Filled)
    ///   Q 技能槽 ← _skillQ (Image) + _cdMaskQ (Image, Filled)
    ///   E 技能槽 ← _skillE (Image) + _cdMaskE (Image, Filled)
    ///   武器图标 ← _weaponIcon (Image)
    ///   弹药数   ← _ammoText (TMP_Text)
    ///   Build 列表 ← _buildListRoot (Transform, VerticalLayoutGroup)
    ///   战斗日志 ← _logListRoot (Transform, VerticalLayoutGroup)
    ///   缩圈倒计时 ← _zoneTimerText (TMP_Text)
    ///   小地图    ← _minimapImage (RawImage)
    ///   Boss HP  ← _bossHpBar (Image, Filled)  + _bossHpRoot (GameObject)
    /// </summary>
    public sealed class CombatHUDForm : MonoBehaviour, IUIForm
    {
        // ── 序列化字段（Inspector 绑定）──────────────────────
        [Header("HP")]
        [SerializeField] Image _hpBar;

        [Header("技能槽")]
        [SerializeField] Image _skillQ;
        [SerializeField] Image _cdMaskQ;
        [SerializeField] Image _skillE;
        [SerializeField] Image _cdMaskE;

        [Header("武器 / 弹药")]
        [SerializeField] Image     _weaponIcon;
        [SerializeField] TMP_Text  _ammoText;

        [Header("Build 列表")]
        [SerializeField] Transform _buildListRoot;

        [Header("战斗日志")]
        [SerializeField] Transform _logListRoot;
        [SerializeField] TMP_Text  _logRowTemplate; // 用于 Instantiate，SetActive(false)

        [Header("缩圈")]
        [SerializeField] TMP_Text  _zoneTimerText;

        [Header("小地图")]
        [SerializeField] RawImage  _minimapImage;

        [Header("Boss HP")]
        [SerializeField] GameObject _bossHpRoot;
        [SerializeField] Image      _bossHpBar;

        // ── 运行时引用 ────────────────────────────────────────
        EventBus     _bus;
        ModuleRunner _runner;

        float _maxHp  = 100f;
        float _curHp  = 100f;

        const int MaxLogRows = 30;
        readonly List<TMP_Text> _logRows = new();

        /// <summary>Form 是否完成初始化（PlayMode 测试用）</summary>
        public bool IsReady { get; private set; }

        /// <summary>HP 条当前填充量（PlayMode 测试用）</summary>
        public float HpBarFillAmount => _hpBar != null ? _hpBar.fillAmount : 0f;

        // ── IDisposable 订阅列表 ─────────────────────────────
        readonly List<IDisposable> _subs = new();

        // ── MonoBehaviour ────────────────────────────────────

        void Awake()
        {
            // 初始隐藏，RunStartedEvent 后再显示
            gameObject.SetActive(false);
            if (_bossHpRoot) _bossHpRoot.SetActive(false);
        }

        async void Start()
        {
            // 等待 GameApp 就绪（最多 10s）
            GameApp app = null;
            float timeout = Time.unscaledTime + 10f;
            while (Time.unscaledTime < timeout)
            {
                app = FindObjectOfType<GameApp>();
                if (app != null && app.TryGetRuntime(out _bus, out _runner)) break;
                await UniTask.Yield();
            }

            if (_bus == null || _runner == null)
            {
                FrameworkLogger.Error("CombatHUDForm", "Action=Start 等待 GameApp 就绪超时");
                return;
            }

            _runner.GetModule<UIModule>().Register(this);
            SubscribeEvents();
            IsReady = true;
            FrameworkLogger.Info("CombatHUDForm", "Action=Ready");
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
            DG.Tweening.DOTween.Kill(transform);
            try { _runner?.GetModule<UIModule>().Unregister(this); } catch { /* shutting down */ }
        }

        // ── IUIForm ──────────────────────────────────────────

        public GameObject GameObject => gameObject;

        public void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // HUD 仅在 InGame 状态显示
            bool show = newState == GameState.InGame;
            if (gameObject.activeSelf != show)
                gameObject.SetActive(show);
        }

        // ── 事件订阅 ─────────────────────────────────────────

        void SubscribeEvents()
        {
            _subs.Add(_bus.Subscribe<RunStartedEvent>(OnRunStarted));
            _subs.Add(_bus.Subscribe<DamagedEvent>(OnDamaged));
            _subs.Add(_bus.Subscribe<EffectAppliedEvent>(e => AppendEffectLog(e.Results)));
            _subs.Add(_bus.Subscribe<TargetKilledEvent>(e => AppendLog($"<击杀> {e.Target.Name}")));
            _subs.Add(_bus.Subscribe<ActorDiedEvent>(OnActorDied));
            _subs.Add(_bus.Subscribe<BuildChangedEvent>(_ => RefreshBuildList()));
            _subs.Add(_bus.Subscribe<PassiveRecomputedEvent>(_ => RefreshBuildList()));
            _subs.Add(_bus.Subscribe<SkillCastEvent>(OnSkillCast));
            _subs.Add(_bus.Subscribe<ItemPickedEvent>(OnItemPicked));
            _subs.Add(_bus.Subscribe<MapGeneratedEvent>(_ => { /* 小地图纹理更新留待后续迭代 */ }));
            _subs.Add(_bus.Subscribe<ZoneShrinkPhaseEvent>(OnZoneShrink));
            _subs.Add(_bus.Subscribe<BossSpawnedEvent>(OnBossSpawned));
            _subs.Add(_bus.Subscribe<BossPhaseChangedEvent>(OnBossPhaseChanged));
        }

        // ── 事件处理 ─────────────────────────────────────────

        void OnRunStarted(RunStartedEvent e)
        {
            _maxHp = e.MaxHealth > 0 ? e.MaxHealth : 100f;
            _curHp = _maxHp;
            UpdateHpBar();
            gameObject.SetActive(true);
        }

        void OnDamaged(DamagedEvent e)
        {
            _curHp = e.NewHp;
            _maxHp = e.MaxHp > 0 ? e.MaxHp : _maxHp;
            UpdateHpBar();
        }

        void OnActorDied(ActorDiedEvent e)
        {
            if (e.DeadActor != null && e.DeadActor.IsPlayer)
            {
                AppendLog("<玩家阵亡>");
                gameObject.SetActive(false);
            }
        }

        void OnSkillCast(SkillCastEvent e)
        {
            // Q/E 冷却遮罩 DOTween 驱动（radial filled 从 1→0）
            Image mask = e.SlotIndex == 0 ? _cdMaskQ : _cdMaskE;
            if (mask == null || e.Cooldown <= 0f) return;
            mask.fillAmount = 1f;
            DG.Tweening.DOTween.To(
                () => mask.fillAmount,
                v => mask.fillAmount = v,
                0f,
                e.Cooldown
            ).SetUpdate(true); // 不受 timeScale 影响
        }

        void OnItemPicked(ItemPickedEvent e)
        {
            if (_ammoText) _ammoText.SetText("{0}", e.Ammo);
            // 武器 Sprite 留待资源系统集成
        }

        void OnZoneShrink(ZoneShrinkPhaseEvent e)
        {
            if (_zoneTimerText == null) return;
            int sec = Mathf.CeilToInt(e.SecondsRemaining);
            _zoneTimerText.SetText("{0}s", sec);
            _zoneTimerText.color = sec <= 10 ? Color.red : Color.white;
        }

        void OnBossSpawned(BossSpawnedEvent e)
        {
            if (_bossHpRoot) _bossHpRoot.SetActive(true);
            if (_bossHpBar)  _bossHpBar.fillAmount = 1f;
        }

        void OnBossPhaseChanged(BossPhaseChangedEvent e)
        {
            if (_bossHpBar) _bossHpBar.fillAmount = e.HpRatio;
        }

        // ── 视图刷新 ─────────────────────────────────────────

        void UpdateHpBar()
        {
            if (_hpBar == null) return;
            float ratio = _maxHp > 0 ? Mathf.Clamp01(_curHp / _maxHp) : 0f;
            _hpBar.fillAmount = ratio;
            _hpBar.color = ratio > 0.5f ? Color.green
                         : ratio > 0.25f ? Color.yellow
                         : Color.red;
        }

        void RefreshBuildList()
        {
            // 占位：后续迭代接入 TattooModule 数据
        }

        void AppendEffectLog(IReadOnlyList<EffectResult> results)
        {
            foreach (var r in results)
                AppendLog($"{r.Part}|{r.Element}|{r.Shape} 伤={r.Damage:F1} x{r.SynergyMul:F2}");
        }

        void AppendLog(string text)
        {
            if (_logListRoot == null || _logRowTemplate == null) return;

            TMP_Text row;
            if (_logRows.Count < MaxLogRows)
            {
                row = Instantiate(_logRowTemplate, _logListRoot);
                row.gameObject.SetActive(true);
                _logRows.Add(row);
            }
            else
            {
                // 循环复用：把最旧的移到最末
                row = _logRows[0];
                _logRows.RemoveAt(0);
                _logRows.Add(row);
                row.transform.SetAsLastSibling();
            }

            row.text = text;
        }
    }
}
