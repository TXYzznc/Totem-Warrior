using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;
using Tattoo.Strategies;
using Tattoo.Strategies.Elements;
using Tattoo.Strategies.Parts;
using Tattoo.Strategies.Shapes;

namespace Tattoo
{
    /// <summary>
    /// 纹身系统核心。承担：
    /// - 装备 Build 管理（Equip/Clear/RecomputePassive）
    /// - 6 个战斗事件订阅 → Fire 调度
    /// - PendingTrigger 消耗
    ///
    /// 依赖 DataTableModule 读取 5 张 JSON 配置；自身不直接读 JSON 或文件。
    /// </summary>
    public sealed class TattooModule : IGameModule
    {
        public int ModuleCategory => 1;
        public Type[] Dependencies => new[] { typeof(DataTableModule) };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        // ===== 配置表（InitializeAsync 中赋值） =====
        TattooPartConfig    _partConfig;
        TattooColorConfig   _colorConfig;
        TattooPatternConfig _patternConfig;
        TattooElementConfig _elementConfig;
        TattooShapeConfig   _shapeConfig;

        // ===== 策略实例（按 Name 索引） =====
        readonly Dictionary<string, IPartBehavior>    _partBehaviors    = new();
        readonly Dictionary<string, IElementBehavior> _elementBehaviors = new();
        readonly Dictionary<string, IShapeBehavior>   _shapeBehaviors   = new();

        // ===== 事件名 → Type 映射 =====
        readonly Dictionary<string, Type> _eventTypeMap = new()
        {
            { "AttackHitEvent",    typeof(AttackHitEvent) },
            { "CritHitEvent",      typeof(CritHitEvent) },
            { "DamagedEvent",      typeof(DamagedEvent) },
            { "SkillCastEvent",    typeof(SkillCastEvent) },
            { "DodgePressedEvent", typeof(DodgePressedEvent) },
            { "MoveTickEvent",     typeof(MoveTickEvent) },
        };

        // ===== 运行时状态 =====
        public PlayerStats  Stats   { get; } = new();
        public PlayerState  Player  { get; } = new();
        public IReadOnlyList<TattooSlot> Equipped => _equipped;

        readonly List<TattooSlot>  _equipped = new();
        readonly SynergyCalculator _synergy  = new();

        public TattooModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            var dt = _runner.GetModule<DataTableModule>();
            _partConfig    = dt.GetTable<TattooPartConfig>();
            _colorConfig   = dt.GetTable<TattooColorConfig>();
            _patternConfig = dt.GetTable<TattooPatternConfig>();
            _elementConfig = dt.GetTable<TattooElementConfig>();
            _shapeConfig   = dt.GetTable<TattooShapeConfig>();

            RegisterPartStrategies();
            RegisterElementStrategies();
            RegisterShapeStrategies();

            FrameworkLogger.Info("TattooModule",
                $"Action=Initialized Parts={_partBehaviors.Count} Elements={_elementBehaviors.Count} Shapes={_shapeBehaviors.Count}");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _equipped.Clear();
            _partBehaviors.Clear();
            _elementBehaviors.Clear();
            _shapeBehaviors.Clear();
            FrameworkLogger.Info("TattooModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ===== 策略注册 =====

        void RegisterPartStrategies()
        {
            _partBehaviors["Head"]     = new HeadPartBehavior();
            _partBehaviors["Torso"]    = new TorsoPartBehavior();
            _partBehaviors["LeftArm"]  = new LeftArmPartBehavior();
            _partBehaviors["RightArm"] = new RightArmPartBehavior();
            _partBehaviors["LeftLeg"]  = new LeftLegPartBehavior();
            _partBehaviors["RightLeg"] = new RightLegPartBehavior();
        }

        void RegisterElementStrategies()
        {
            foreach (var row in _elementConfig.All.Values)
            {
                IElementBehavior beh = row.Name switch
                {
                    "Fire"      => new FireElementBehavior(row.BaseMultiplier, burnDPS: row.Param1, burnDuration: row.Param2),
                    "Lightning" => new LightningElementBehavior(row.BaseMultiplier, paralyzeDuration: row.Param1),
                    "Nature"    => new NatureElementBehavior(row.BaseMultiplier, poisonDPS: row.Param1, poisonDuration: row.Param2, maxRegenStacks: (int)row.Param3),
                    "Frost"     => new FrostElementBehavior(row.BaseMultiplier, slowFactor: row.Param1, slowDuration: row.Param2, maxArmorStacks: (int)row.Param3),
                    "Mutation"  => new MutationElementBehavior(row.BaseMultiplier, seed: (int)row.Param3),
                    "Holy"      => new HolyElementBehavior(row.BaseMultiplier, healPercent: row.Param1),
                    "Pure"      => new PureElementBehavior(row.BaseMultiplier, magnitudeBonus: row.Param1, focusStackBonus: row.Param2, maxFocusStacks: (int)row.Param3),
                    _ => null,
                };
                if (beh == null)
                {
                    FrameworkLogger.Error("TattooModule", $"Action=RegisterElement 未知元素 Name={row.Name}");
                    continue;
                }
                _elementBehaviors[row.Name] = beh;
            }
        }

        void RegisterShapeStrategies()
        {
            foreach (var row in _shapeConfig.All.Values)
            {
                IShapeBehavior beh = row.Name switch
                {
                    "SingleHit"    => new SingleHitShapeBehavior(),
                    "AOEBurst"     => new AOEBurstShapeBehavior(areaFactor: row.Param1, maxTargets: (int)row.Param2),
                    "StackingMark" => new StackingMarkShapeBehavior(threshold: (int)row.Param1, burstMul: row.Param2),
                    "MultiHit"     => new MultiHitShapeBehavior(segments: (int)row.Param1),
                    "ChainJump"    => new ChainJumpShapeBehavior(maxJumps: (int)row.Param1, decay: row.Param2),
                    "ProbBurst"    => new ProbBurstShapeBehavior(probability: row.Param1, burstMultiplier: row.Param2, seed: (int)row.Param3),
                    "TrailZone"    => new TrailZoneShapeBehavior(tickFactor: row.Param1, ticks: (int)row.Param2),
                    "SummonForm"   => new SummonFormShapeBehavior(summonMultiplier: row.Param1),
                    _ => null,
                };
                if (beh == null)
                {
                    FrameworkLogger.Error("TattooModule", $"Action=RegisterShape 未知形状 Name={row.Name}");
                    continue;
                }
                _shapeBehaviors[row.Name] = beh;
            }
        }

        // ===== 装备 API =====

        /// <summary>装备一个槽位。partId / colorId / patternId 必须存在于对应 DataTable。</summary>
        public bool Equip(int partId, int colorId, int patternId)
        {
            if (!_partConfig.TryGetById(partId, out var partRow))
            {
                FrameworkLogger.Error("TattooModule", $"Action=Equip PartId={partId} Result=NotFound");
                return false;
            }
            if (!_colorConfig.TryGetById(colorId, out var colorRow))
            {
                FrameworkLogger.Error("TattooModule", $"Action=Equip ColorId={colorId} Result=NotFound");
                return false;
            }
            if (!_patternConfig.TryGetById(patternId, out var patternRow))
            {
                FrameworkLogger.Error("TattooModule", $"Action=Equip PatternId={patternId} Result=NotFound");
                return false;
            }

            if (!_partBehaviors.TryGetValue(partRow.Name, out var partBeh) ||
                !_elementBehaviors.TryGetValue(colorRow.Element, out var elemBeh) ||
                !_shapeBehaviors.TryGetValue(patternRow.Shape, out var shapeBeh))
            {
                FrameworkLogger.Error("TattooModule",
                    $"Action=Equip 策略未注册 Part={partRow.Name} Element={colorRow.Element} Shape={patternRow.Shape}");
                return false;
            }

            if (!_eventTypeMap.TryGetValue(partRow.TriggerEvent, out var triggerType))
            {
                FrameworkLogger.Error("TattooModule",
                    $"Action=Equip 未知 TriggerEvent={partRow.TriggerEvent} Part={partRow.Name}");
                return false;
            }

            var slot = new TattooSlot
            {
                PartId   = partId,
                ColorId  = colorId,
                PatternId = patternId,
                Part     = partBeh,
                Element  = elemBeh,
                Shape    = shapeBeh,
                TriggerEventType = triggerType,
                ColorMultiplier  = colorRow.ColorMultiplier,
                PatternMultiplier = patternRow.PatternMultiplier,
                ScaleStat        = Enum.TryParse<StatType>(partRow.ScaleStat, out var st) ? st : StatType.WeaponDamage,
                ScaleFactor      = partRow.ScaleFactor,
                SymmetryGroup    = Enum.TryParse<SymmetryGroup>(partRow.SymmetryGroup, out var sg) ? sg : SymmetryGroup.None,
                PartName         = partRow.Name,
                ColorName        = colorRow.Name,
                PatternName      = patternRow.Name,
                ElementType      = Enum.TryParse<ElementType>(colorRow.Element, out var et) ? et : ElementType.Pure,
            };

            _equipped.Add(slot);
            RecomputePassive();

            _bus.Publish(new BuildChangedEvent(_equipped));
            _bus.Publish(new PassiveRecomputedEvent(Player.Passive));
            return true;
        }

        /// <summary>清空当前 Build。重置玩家临时状态。</summary>
        public void Clear()
        {
            _equipped.Clear();
            Player.PendingTriggers.Clear();
            Player.Buffs.Clear();
            Player.Stacks.Clear();
            Player.Passive = new PassiveStats();
            _bus.Publish(new BuildChangedEvent(_equipped));
            _bus.Publish(new PassiveRecomputedEvent(Player.Passive));
        }

        void RecomputePassive()
        {
            Player.Passive = new PassiveStats();
            foreach (var slot in _equipped)
            {
                float strength = slot.PatternMultiplier * 10f;
                slot.Part.ContributePassive(Player.Passive, slot.ElementType, strength, slot.ColorName, slot.PatternName);
            }
        }

        // ===== 战斗事件订阅 =====

        [EventHandler] void OnAttackHit(AttackHitEvent e)        => Fire(typeof(AttackHitEvent),    primary: e.Target,   attacker: null);
        [EventHandler] void OnCritHit(CritHitEvent e)            => Fire(typeof(CritHitEvent),      primary: e.Target,   attacker: null);
        [EventHandler] void OnDamaged(DamagedEvent e)            => Fire(typeof(DamagedEvent),      primary: null,       attacker: e.Attacker);
        [EventHandler] void OnSkillCast(SkillCastEvent e)        => Fire(typeof(SkillCastEvent),    primary: null,       attacker: null);
        [EventHandler] void OnDodgePressed(DodgePressedEvent e)  => Fire(typeof(DodgePressedEvent), primary: null,       attacker: null);
        [EventHandler] void OnMoveTick(MoveTickEvent e)          => Fire(typeof(MoveTickEvent),     primary: null,       attacker: null, path: e.Path);

        /// <summary>触发匹配槽位 → 应用策略 → 消耗 PendingTrigger → 发 EffectAppliedEvent。</summary>
        void Fire(Type eventType, Target primary, Target attacker, Target[] path = null)
        {
            var ctx = new EffectContext
            {
                EventType     = eventType,
                Stats         = Stats,
                Self          = Player,
                PrimaryTarget = primary,
                LastAttacker  = attacker,
            };
            if (path != null)
            {
                foreach (var t in path) ctx.MovementPath.Add(t);
            }

            // 1. 触发匹配槽位
            var snapshot = _equipped.ToArray();
            foreach (var slot in snapshot)
            {
                if (slot.TriggerEventType != eventType) continue;

                slot.Part.PrepareContext(ctx);

                float scale      = Stats.Get(slot.ScaleStat) * slot.ScaleFactor;
                float elementMul = slot.ColorMultiplier * slot.Element.BaseMultiplier;
                float patternMul = slot.PatternMultiplier;
                float synergyMul = _synergy.Compute(_equipped, slot);
                float magnitude  = scale * elementMul * patternMul * synergyMul;
                magnitude = slot.Element.ModifyMagnitude(ctx, magnitude);

                bool intercepted = slot.Part.InterceptApply(ctx, slot.Shape, slot.Element, magnitude);
                if (!intercepted)
                {
                    slot.Shape.Apply(ctx, slot.Element, magnitude, slot.PartName, synergyMul);
                    slot.Element.AffectSelf(Player, ctx, magnitude);
                    slot.Element.OnHitExtra(ctx, slot.Shape, ctx.PrimaryTarget, magnitude);
                }
                slot.Part.AffectSelf(Player, ctx);
            }

            // 2. 消耗 PendingTrigger
            ConsumePendingTriggers(eventType, ctx);

            // 3. 广播结果
            if (ctx.Log.Count > 0)
                _bus.Publish(new EffectAppliedEvent(ctx.Log));
        }

        void ConsumePendingTriggers(Type eventType, EffectContext ctx)
        {
            if (Player.PendingTriggers.Count == 0) return;
            var snapshot = Player.PendingTriggers.ToArray();
            var consumed = new List<PendingTrigger>();

            foreach (var pt in snapshot)
            {
                if (pt.ConsumeOnEventType != eventType) continue;
                if (pt.Shape == null) continue;

                pt.Shape.Apply(ctx, pt.Element, pt.Magnitude, $"[来自{pt.Source}]延迟", 1f);
                pt.Element.AffectSelf(Player, ctx, pt.Magnitude);
                ctx.Log.Add(new EffectResult
                {
                    Part = pt.Source,
                    Element = pt.Element?.ElementName ?? "?",
                    Shape = pt.Shape?.ShapeName ?? "?",
                    Damage = pt.Magnitude,
                    HitCount = 1,
                    Status = "ConsumedPending",
                    Note = "ConsumePending@" + eventType.Name,
                });
                if (pt.ExpiresAfter > 0) pt.ExpiresAfter--;
                if (pt.ExpiresAfter == 0) consumed.Add(pt);
            }
            foreach (var pt in consumed) Player.PendingTriggers.Remove(pt);
        }
    }
}
