using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Combat;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo.Bot
{
    /// <summary>
    /// v2.1 BotControllerModule —— 伪联机 AI 大脑。
    ///
    /// 职责：
    /// 1) InitializeAsync 期间：让 Spawner 创建 49 个 actor → 为每个 actor 装配 Smart/Light controller → 注册到 CombatModule。
    /// 2) OnUpdate：维护 4 桶 LOD（SmartHot/SmartCold/LightHot/LightCold），round-robin 给冷桶 controller 写 safetyScore。
    /// 3) 监听 RequestSelfTattooEvent 由 Smart 发起 → 直接走 TattooModule.StartSelfTattoo。
    /// 4) 监听 DamagedEvent/TargetKilledEvent 转写给对应 controller。
    ///
    /// 性能：0 GC alloc per Update。所有列表预分配；遍历用 for + Count 缓存。
    /// </summary>
    public sealed class BotControllerModule : IGameModule, ITickable
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[]
        {
            typeof(SpawnerModule),
            typeof(TattooModule),
            typeof(CombatModule),
            typeof(DataTableModule),
        };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        SpawnerModule _spawner;
        TattooModule  _tattoo;
        CombatModule  _combat;
        BotConfig _botCfg;
        BotBuildPresetConfig _presetCfg;

        // ===== 注册的 controller =====
        readonly List<SmartBotPlayerController> _smartHot  = new();
        readonly List<SmartBotPlayerController> _smartCold = new();
        readonly List<LightBotPlayerController> _lightHot  = new();
        readonly List<LightBotPlayerController> _lightCold = new();

        // actor → controller 快表（[EventHandler] 处理伤害事件时反查）
        readonly Dictionary<Target, SmartBotPlayerController> _smartByActor = new();
        readonly Dictionary<Target, LightBotPlayerController> _lightByActor = new();

        // LOD 桶迁移 timer
        float _lodAccum;
        const float LodCheckInterval = 0.2f;

        // 冷桶 round-robin 游标
        int _smartColdCursor;
        int _lightColdCursor;
        float _coldAccumSmart;
        float _coldAccumLight;
        const float SmartColdInterval = 0.5f;
        const float LightColdInterval = 2f;

        public BotControllerModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            _spawner = _runner.GetModule<SpawnerModule>();
            _tattoo  = _runner.GetModule<TattooModule>();
            _combat  = _runner.GetModule<CombatModule>();
            var dt   = _runner.GetModule<DataTableModule>();
            _botCfg    = dt.GetTable<BotConfig>();
            _presetCfg = dt.GetTable<BotBuildPresetConfig>();

            BuildControllers();

            FrameworkLogger.Info("BotControllerModule",
                $"Action=Initialized Smart={_smartByActor.Count} Light={_lightByActor.Count}");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            // 注销所有 controller
            foreach (var c in _smartByActor.Values) _combat.UnregisterController(c);
            foreach (var c in _lightByActor.Values) _combat.UnregisterController(c);
            _smartByActor.Clear(); _lightByActor.Clear();
            _smartHot.Clear(); _smartCold.Clear();
            _lightHot.Clear(); _lightCold.Clear();
            FrameworkLogger.Info("BotControllerModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        void BuildControllers()
        {
            // 期望 SpawnerModule 已在 InitializeAsync 中生成 49 个 actor GameObject 到 Enemies 列表。
            // 我们按 actor 顺序前 20 装 Smart，后 29 装 Light（与 BotConfig 行循环匹配）。
            var enemies = _spawner.Enemies;
            int smartCount = 0;
            int lightCount = 0;
            const int targetSmart = 20;
            const int targetLight = 29;

            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i];
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er == null || er.Target == null) continue;

                IPlayerController ctrl = null;
                if (smartCount < targetSmart)
                {
                    var cfg = PickConfig(_botCfg.SmartRows, smartCount);
                    var preset = ResolvePreset(cfg.PreferredPreset);
                    var smart = new SmartBotPlayerController(er.Target, _spawner, _tattoo, cfg, preset);
                    _smartByActor[er.Target] = smart;
                    _smartHot.Add(smart); // 默认全进 Hot，下一帧 LOD 桶迁移
                    ctrl = smart;
                    BotVisualBinder.ApplyColorAndOutline(go, isSmart: true, smartCount);
                    smartCount++;
                }
                else if (lightCount < targetLight)
                {
                    var cfg = PickConfig(_botCfg.LightRows, lightCount);
                    var light = new LightBotPlayerController(er.Target, _spawner, cfg);
                    _lightByActor[er.Target] = light;
                    _lightHot.Add(light);
                    ctrl = light;
                    BotVisualBinder.ApplyColorAndOutline(go, isSmart: false, lightCount);
                    lightCount++;
                }
                else break;

                if (ctrl != null) _combat.RegisterController(ctrl);
            }
        }

        static BotConfigRow PickConfig(IReadOnlyList<BotConfigRow> pool, int index)
        {
            if (pool == null || pool.Count == 0)
                throw new InvalidOperationException("BotConfig pool 为空");
            return pool[index % pool.Count];
        }

        BotBuildPresetRow ResolvePreset(int presetId)
        {
            if (_presetCfg.TryGetById(presetId, out var row)) return row;
            // 兜底取第一行
            foreach (var r in _presetCfg.All.Values) return r;
            return null;
        }

        // ===== Tick =====

        public void OnUpdate(float dt)
        {
            // 1) LOD 桶迁移：每 0.2s 检查一次
            _lodAccum += dt;
            if (_lodAccum >= LodCheckInterval)
            {
                _lodAccum = 0f;
                RebalanceLOD();
            }

            // 2) 写 safetyScore：Smart Hot 每帧；Smart Cold round-robin（0.5s 内打散）
            UpdateSmartHotSafety();
            UpdateSmartColdSafety(dt);

            // 3) Light 桶无需 safetyScore（不读条），仅做存活回收
            _coldAccumLight += dt;
            if (_coldAccumLight >= LightColdInterval) _coldAccumLight = 0f;
        }

        void RebalanceLOD()
        {
            var player = _spawner.Player;
            if (player == null) return;
            var p = player.transform.position;

            // Smart：以 20m 为热半径
            MigrateBucket(_smartHot, _smartCold, p, 20f * 20f, smartGetGo: true);
            MigrateBucket(_lightHot, _lightCold, p, 20f * 20f, smartGetGo: false);
        }

        void MigrateBucket<T>(List<T> hot, List<T> cold, Vector3 playerPos, float sqrR, bool smartGetGo)
            where T : class, IPlayerController
        {
            // 从 hot 出到 cold
            for (int i = hot.Count - 1; i >= 0; i--)
            {
                var c = hot[i];
                var go = GetGo(c);
                if (go == null) { hot.RemoveAt(i); continue; }
                if ((go.transform.position - playerPos).sqrMagnitude > sqrR)
                {
                    cold.Add(c);
                    hot.RemoveAt(i);
                }
            }
            // 从 cold 进入 hot
            for (int i = cold.Count - 1; i >= 0; i--)
            {
                var c = cold[i];
                var go = GetGo(c);
                if (go == null) { cold.RemoveAt(i); continue; }
                if ((go.transform.position - playerPos).sqrMagnitude <= sqrR)
                {
                    hot.Add(c);
                    cold.RemoveAt(i);
                }
            }
        }

        GameObject GetGo(IPlayerController c)
        {
            var actor = c.OwnerActor;
            if (actor == null) return null;
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er != null && er.Target == actor) return go;
            }
            return null;
        }

        void UpdateSmartHotSafety()
        {
            for (int i = 0; i < _smartHot.Count; i++)
            {
                var c = _smartHot[i];
                c.NotifySafety(CalcSafety(c));
            }
        }

        void UpdateSmartColdSafety(float dt)
        {
            _coldAccumSmart += dt;
            if (_coldAccumSmart < SmartColdInterval || _smartCold.Count == 0) return;
            _coldAccumSmart = 0f;
            // 一次性扫一遍冷桶（数量小，可承受），写 safety
            for (int i = 0; i < _smartCold.Count; i++)
            {
                var c = _smartCold[i];
                c.NotifySafety(CalcSafety(c));
            }
            _smartColdCursor = (_smartColdCursor + 1) % Math.Max(_smartCold.Count, 1);
        }

        float CalcSafety(SmartBotPlayerController c)
        {
            var go = GetGo(c);
            if (go == null) return 1f;
            // 20m 内敌对 actor 数 -> safety 反比例
            var origin = go.transform.position;
            int hostiles = 0;
            const float r2 = 20f * 20f;
            var list = _spawner.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var g = list[i];
                if (g == null || !g.activeSelf) continue;
                var er = g.GetComponent<EntityRef>();
                if (er == null || er.Target == null) continue;
                if (er.Target == c.OwnerActor) continue;
                if (er.Target.Health <= 0f) continue;
                if ((g.transform.position - origin).sqrMagnitude > r2) continue;
                hostiles++;
            }
            // hostiles=0 → 1.0；每个敌对 -0.15；下限 0
            float s = 1f - hostiles * 0.15f;
            return s < 0f ? 0f : s;
        }

        // ===== 事件订阅 =====

        /// <summary>把 DamagedEvent 转写给受击 actor 对应的 controller。</summary>
        [EventHandler]
        void OnDamaged(DamagedEvent e)
        {
            // 注：当前 DamagedEvent 只携带 Attacker / Damage，没有 Victim 字段。
            // 玩家是隐含的受击者；Bot 受击逻辑后续要扩 DamagedEvent.Victim 才能精准定向。
            // 本期：Smart Bot 在 ShouldDodge 内自检 lastDamagedTime；Light 同理。
            // 占位：暂不分发，等 DamagedEvent 字段扩展后接通。
        }

        /// <summary>Bot 发起的自纹身请求 → TattooModule 启动读条。</summary>
        [EventHandler]
        void OnRequestSelfTattoo(RequestSelfTattooEvent e)
        {
            if (e.Requester == null) return;
            // 只处理 Bot 请求；玩家请求由 UI Form / 其他流程触发的同名事件路径处理
            if (!_smartByActor.ContainsKey(e.Requester)) return;

            // 查读条时长：默认 5s；若 TattooReadingTimeConfig 已加载则按 PartId 取
            float duration = 5f;
            try
            {
                var dt = _runner.GetModule<DataTableModule>();
                var rt = dt.GetTable<TattooReadingTimeConfig>();
                if (rt.TryGetById(e.PartId, out var row)) duration = row.DurationSec;
            }
            catch { /* 配置缺失则用默认 */ }

            _tattoo.StartSelfTattoo(e.Requester, e.PartId, e.ColorId, e.PatternId, duration);
            FrameworkLogger.Info("BotControllerModule",
                $"Action=BotSelfTattooStart Bot={e.Requester.Name} Part={e.PartId} Color={e.ColorId} Pattern={e.PatternId} Duration={duration}");
        }

        /// <summary>actor 死亡 → 清理 controller。</summary>
        [EventHandler]
        void OnTargetKilled(TargetKilledEvent e)
        {
            if (e.Target == null) return;
            if (_smartByActor.TryGetValue(e.Target, out var smart))
            {
                _combat.UnregisterController(smart);
                _smartHot.Remove(smart);
                _smartCold.Remove(smart);
                _smartByActor.Remove(e.Target);
                if (_tattoo.IsInProgress(e.Target))
                    _tattoo.CancelSelfTattoo(e.Target, CancelReason.Killed);
            }
            else if (_lightByActor.TryGetValue(e.Target, out var light))
            {
                _combat.UnregisterController(light);
                _lightHot.Remove(light);
                _lightCold.Remove(light);
                _lightByActor.Remove(e.Target);
            }
        }

        // ===== 调试只读 =====
        public int SmartHotCount  => _smartHot.Count;
        public int SmartColdCount => _smartCold.Count;
        public int LightHotCount  => _lightHot.Count;
        public int LightColdCount => _lightCold.Count;
    }
}
