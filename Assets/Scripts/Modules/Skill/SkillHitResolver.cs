using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Skill.Events;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

/// <summary>
/// change #20 D7 落地：技能伤害结算桥。
///
/// 职责：
/// - 订阅 SkillActivatedEvent（SkillModule 在 Active 帧第 1 帧发布）
/// - 读取 SkillConfig.DamageMul × WeaponModule.GetBaseDamage(caster) → 计算伤害值
/// - 按 SkillConfig.HitShape 计算命中目标（single + circle 本期实现；line/cone TODO）
/// - 对每个命中目标发 AttackHitEvent → 走 TattooModule.Fire 刺青链（统一伤害口径）
/// - 读当前武器 NormalTraitId/ChargedTraitId → 查 WeaponTraitConfig → 调度副作用
///   - EffectType="Status"  → 调 StatusEffectModule.ApplyStatus（Burn/Poison）
///   - EffectType="AoE"     → TODO log 占位，本期不实装范围伤害
///   - 其余 EffectType      → TODO log 占位，后续各期实装
///
/// 不做：技能 startup / active / recovery 时序（→ SkillModule）、视觉粒子（→ VFXModule）、
///       直接扣 target.Health（必须走 AttackHitEvent → TattooModule.Fire，否则 Build 不可见）。
///
/// CONTRACT §2.1 + §3.1 锁定：
///   ModuleCategory = 3; Dependencies = [WeaponModule, DataTableModule]
///   ❌ 严禁直接修改 target.Health
/// </summary>
public sealed class SkillHitResolver : IGameModule
{
    public int    ModuleCategory => 3;
    public Type[] Dependencies   => new[] { typeof(WeaponModule), typeof(DataTableModule) };

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    WeaponModule          _weapon;
    DataTableModule       _dt;
    SkillConfig           _skillCfg;
    WeaponConfig          _weaponCfg;
    WeaponTraitConfig     _traitCfg;

    // circle AoE 命中列表复用，避免 GC alloc（每帧最多同时激活一个技能，无并发问题）
    readonly List<Target> _circleHitBuffer = new(8);

    public SkillHitResolver(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─── 生命周期 ────────────────────────────────────────────────────

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _weapon    = _runner.GetModule<WeaponModule>();
        _dt        = _runner.GetModule<DataTableModule>();
        _skillCfg  = _dt.GetTable<SkillConfig>();
        _weaponCfg = _dt.GetTable<WeaponConfig>();
        _traitCfg  = _dt.GetTable<WeaponTraitConfig>();

        FrameworkLogger.Info("SkillHitResolver",
            $"Action=Initialized SkillCount={_skillCfg.All.Count} TraitCount={_traitCfg.All.Count}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("SkillHitResolver", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─── 事件处理 ────────────────────────────────────────────────────

    [EventHandler]
    void OnSkillActivated(SkillActivatedEvent e)
    {
        if (e.Caster == null)
        {
            FrameworkLogger.Warn("SkillHitResolver", "Action=OnSkillActivated Reason=NullCaster");
            return;
        }

        // 1. 读 SkillConfig
        if (!_skillCfg.TryGetById(e.SkillId, out var skillRow))
        {
            FrameworkLogger.Warn("SkillHitResolver",
                $"Action=OnSkillActivated SkillId={e.SkillId} Reason=SkillConfigNotFound");
            return;
        }

        // 2. 伤害计算：baseDmg（当前武器基础值）× DamageMul（技能系数）
        float baseDmg   = _weapon.GetBaseDamage(e.Caster);
        float finalDmg  = baseDmg * skillRow.DamageMul;

        // 3. 根据 HitShape 解算命中目标列表
        var hits = ResolveHits(e.Caster, e.AimTarget, skillRow);

        if (hits.Count == 0)
        {
            FrameworkLogger.Info("SkillHitResolver",
                $"Action=OnSkillActivated SkillId={e.SkillId} Hits=0");
            return;
        }

        // 4. 对每个目标发 AttackHitEvent → TattooModule.Fire 刺青链
        //    同时调度当前武器 NormalTraitId 的副作用（技能走普攻 trait）
        var weaponState = _weapon.GetEquippedWeapon(e.Caster);
        string traitId  = weaponState.Weapon?.NormalTraitId ?? string.Empty;

        foreach (var target in hits)
        {
            // 4a. 发 AttackHitEvent（走刺青链，统一伤害口径，不直接扣血）
            _bus.Publish(new AttackHitEvent(target, finalDmg));

            // 4b. 调度 weapon trait 副作用（观察+副作用模式，不重发 AttackHitEvent）
            DispatchTraitEffect(e.Caster, target, traitId, finalDmg);
        }

        FrameworkLogger.Info("SkillHitResolver",
            $"Action=OnSkillActivated SkillId={e.SkillId} HitShape={skillRow.HitShape} " +
            $"Hits={hits.Count} BaseDmg={baseDmg:F1} DamageMul={skillRow.DamageMul:F2} FinalDmg={finalDmg:F1} TraitId={traitId}");
    }

    // ─── 命中目标解算 ─────────────────────────────────────────────────

    List<Target> ResolveHits(Target caster, Target aimTarget, SkillConfigRow skillRow)
    {
        var result = new List<Target>(4);

        switch (skillRow.HitShape)
        {
            case "single":
                // 单体：直接命中 AimTarget
                if (aimTarget != null)
                    result.Add(aimTarget);
                break;

            case "circle":
            {
                // 圆形范围：从 SpawnerModule.Enemies 遍历，按世界距离筛选
                // 避免依赖 Physics Layer 查询（Target 无 GameObject 引用，通过 EntityRef 关联）
                var spawner = _runner.GetModule<SpawnerModule>();
                if (spawner == null)
                {
                    FrameworkLogger.Warn("SkillHitResolver",
                        $"Action=ResolveHits HitShape=circle Reason=SpawnerModuleNotFound SkillId={skillRow.SkillId}");
                    // 兜底：命中 AimTarget
                    if (aimTarget != null) result.Add(aimTarget);
                    break;
                }

                // 找 caster 的世界坐标
                Vector3 origin = FindActorPosition(spawner, caster);
                float   radiusSq = skillRow.HitRadius * skillRow.HitRadius;

                _circleHitBuffer.Clear();
                foreach (var enemyGo in spawner.Enemies)
                {
                    if (enemyGo == null) continue;
                    var er = enemyGo.GetComponent<EntityRef>();
                    if (er?.Target == null) continue;

                    float distSq = (enemyGo.transform.position - origin).sqrMagnitude;
                    if (distSq <= radiusSq)
                        _circleHitBuffer.Add(er.Target);
                }

                result.AddRange(_circleHitBuffer);
                break;
            }

            case "line":
                // TODO change#20: line 形状本期不实装，后续 change 补充
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=ResolveHits HitShape=line SkillId={skillRow.SkillId} TODO=NotImplementedThisPeriod");
                if (aimTarget != null)
                    result.Add(aimTarget); // 兜底：至少命中 AimTarget
                break;

            case "cone":
                // TODO change#20: cone 形状本期不实装，后续 change 补充
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=ResolveHits HitShape=cone SkillId={skillRow.SkillId} TODO=NotImplementedThisPeriod");
                if (aimTarget != null)
                    result.Add(aimTarget); // 兜底：至少命中 AimTarget
                break;

            default:
                FrameworkLogger.Warn("SkillHitResolver",
                    $"Action=ResolveHits HitShape={skillRow.HitShape} SkillId={skillRow.SkillId} Reason=UnknownHitShape");
                if (aimTarget != null)
                    result.Add(aimTarget);
                break;
        }

        return result;
    }

    // ─── Trait 副作用调度（方案 A：观察+副作用，不重发 AttackHitEvent）────

    void DispatchTraitEffect(Target caster, Target target, string traitId, float finalDmg)
    {
        if (string.IsNullOrEmpty(traitId))
            return;
        if (!_traitCfg.TryGetById(traitId, out var traitRow))
        {
            FrameworkLogger.Warn("SkillHitResolver",
                $"Action=DispatchTraitEffect TraitId={traitId} Reason=TraitConfigNotFound");
            return;
        }

        switch (traitRow.EffectType)
        {
            case "Status":
                // 实装：调 StatusEffectModule.ApplyStatus
                // StatusEffectModule 不在 Dependencies → 运行时 GetModule
                var statusModule = _runner.GetModule<StatusEffectModule>();
                if (statusModule != null)
                {
                    // EffectParam1 = DPS，EffectParam2 = Duration（秒）
                    // traitId 命名约定：trait_dot_burn → "Burn"，trait_dot_poison → "Poison"
                    // 从 TraitId 推导状态名（保持和 StatusEffectModule 的约定一致）
                    string statusName = DeriveStatusName(traitRow.TraitId);
                    statusModule.ApplyStatus(target, statusName, traitRow.EffectParam1, traitRow.EffectParam2, source: caster);

                    FrameworkLogger.Info("SkillHitResolver",
                        $"Action=ApplyStatus Target={target.Name} Status={statusName} DPS={traitRow.EffectParam1} Duration={traitRow.EffectParam2}");
                }
                else
                {
                    FrameworkLogger.Warn("SkillHitResolver",
                        "Action=DispatchTraitEffect EffectType=Status Reason=StatusEffectModuleNotFound");
                }
                break;

            case "Quick":
                // Quick trait（快斩/微吸血）：本期 SkillHitResolver 只做 log 占位
                // Quick 的后摇缩短由 SkillModule/WeaponModule 处理，连击附加 Burn 逻辑本期 TODO
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Quick TraitId={traitId} Target={target.Name} " +
                    $"TODO=RecoveryReductionAndComboStatusNotImplemented");
                break;

            case "Pierce":
                // Pierce trait：穿透多目标由 ResolveHits 层面处理（命中列表），
                // SkillHitResolver 本期不实装衰减系数，所有命中目标取等量伤害
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Pierce TraitId={traitId} Target={target.Name} " +
                    $"TODO=PierceDamageDecayNotImplemented Param1={traitRow.EffectParam1} Param2={traitRow.EffectParam2}");
                break;

            case "Stun":
                // Stun trait：需 StatusEffectModule 支持 Stun 状态，本期 StatusEffectModule 最小集只做 Burn/Poison
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Stun TraitId={traitId} Target={target.Name} " +
                    $"TODO=StunStatusNotImplementedThisPeriod Duration={traitRow.EffectParam1}");
                break;

            case "Chain":
                // Chain trait：跳传逻辑需要多次 OverlapSphere + 递归命中，本期 TODO
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Chain TraitId={traitId} Target={target.Name} " +
                    $"TODO=ChainJumpNotImplementedThisPeriod MaxJumps={traitRow.EffectParam1} Decay={traitRow.EffectParam2}");
                break;

            case "Explosive":
                // AoE 爆炸：本期不实装范围伤害，仅 log 占位
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Explosive TraitId={traitId} Target={target.Name} " +
                    $"TODO=AoEExplosionNotImplementedThisPeriod Radius={traitRow.EffectParam1}");
                break;

            case "MultiShot":
                // MultiShot：扇形多弹由 WeaponModule/ProjectileModule 处理，本期 TODO
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=MultiShot TraitId={traitId} Target={target.Name} " +
                    $"TODO=MultiShotNotImplementedThisPeriod Count={traitRow.EffectParam1}");
                break;

            case "Pull":
                // Pull trait：需要物理拖拽 Actor，本期 TODO
                FrameworkLogger.Info("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType=Pull TraitId={traitId} Target={target.Name} " +
                    $"TODO=PullNotImplementedThisPeriod TargetDist={traitRow.EffectParam1}");
                break;

            default:
                FrameworkLogger.Warn("SkillHitResolver",
                    $"Action=DispatchTraitEffect EffectType={traitRow.EffectType} TraitId={traitId} Reason=UnknownEffectType");
                break;
        }
    }

    // ─── 工具方法 ────────────────────────────────────────────────────

    /// <summary>
    /// 通过 SpawnerModule 查找 actor 的世界坐标（与 WeaponModule.FindActorPosition 逻辑一致）。
    /// 玩家走 spawner.Player；敌人走 spawner.Enemies + EntityRef。
    /// </summary>
    static Vector3 FindActorPosition(SpawnerModule spawner, Target actor)
    {
        if (spawner.PlayerTarget == actor && spawner.Player != null)
            return spawner.Player.transform.position;

        foreach (var go in spawner.Enemies)
        {
            if (go == null) continue;
            var er = go.GetComponent<EntityRef>();
            if (er?.Target == actor) return go.transform.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 从 TraitId 推导状态名，与 StatusEffectModule 约定一致。
    /// trait_dot_burn   → "Burn"
    /// trait_dot_poison → "Poison"
    /// 其他 Status trait → TraitId 去掉前缀后 TitleCase
    /// </summary>
    static string DeriveStatusName(string traitId)
    {
        if (traitId.EndsWith("_burn",   StringComparison.OrdinalIgnoreCase)) return "Burn";
        if (traitId.EndsWith("_poison", StringComparison.OrdinalIgnoreCase)) return "Poison";

        // 兜底：取最后一个下划线之后的子串，首字母大写
        int lastUnderscore = traitId.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < traitId.Length - 1)
        {
            string suffix = traitId.Substring(lastUnderscore + 1);
            return char.ToUpper(suffix[0]) + suffix.Substring(1);
        }
        return traitId;
    }
}
