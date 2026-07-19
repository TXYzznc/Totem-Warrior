
using UnityEngine;

public enum TotemCombatRunMode
{
    Standard = 0,
    ExplorationPreview = 1,
}

public sealed class TotemCombatService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float ChargeThreshold = 0.4f;

    private const float MoveSpeed = 5f;
    private const float PreviewMoveSpeedMin = 0.5f;
    private const float PreviewMoveSpeedMax = 15f;
    private const float DodgeDistance = 4f;
    private const float AttackCooldown = 0.35f;
    private const float SkillCooldown = 3f;
    private const float BasicDamage = 25f;
    private const float ChargedDamage = 45f;
    private const float SkillDamage = 15f;
    private const float SkillRadius = 6f;

    private TotemGameFlowService flowService;
    private TotemInputService inputService;
    private TotemActorService actorService;
    private TotemWeaponService weaponService;
    private TotemSkillService skillService;
    private TotemStatusService statusService;
    private TotemTattooService tattooService;
    private TotemCombatRelationshipService relationshipService;
    private TotemMatchClockService matchClock;
    private TotemEnemyService enemyService;
    private TotemEconomyService economyService;
    private TotemVfxService vfxService;
    private TotemAudioService audioService;
    private TotemUIService uiService;
    private TotemRunStatsService runStatsService;
    private float attackCooldownRemaining;
    private float skillCooldownRemaining;
    private float elapsedSec;
    private int killCount;
    private bool active;
    private bool runFinished;
    private TotemCombatRunMode nextCombatRunMode = TotemCombatRunMode.Standard;
    private TotemCombatRunMode currentCombatRunMode = TotemCombatRunMode.Standard;
    private float explorationPreviewMoveSpeed = MoveSpeed;
    private string lastAction = string.Empty;
    private string lastReason = string.Empty;
    private int lastTargetActorId;
    private string lastTargetName = string.Empty;
    private float lastDamage;
    private bool lastKilled;
    private string lastWeaponId = string.Empty;
    private string lastTraitId = string.Empty;
    private string lastSkillId = string.Empty;
    private int lastHitCount;
    private string lastTargetingMode = string.Empty;
    private float lastAimSpreadHalfDegrees;
    private Vector3 lastAimForward = Vector3.forward;
    private TotemRunResultSnapshot lastRunResult;
    private readonly TotemEnemyModel[] enemyTargetBuffer = new TotemEnemyModel[TotemEnemyService.DefaultEnemyCapacity];

    public override string ServiceName => "Combat";

    public TotemRunResultSnapshot LastRunResult => lastRunResult;

    public bool IsExplorationPreview => currentCombatRunMode == TotemCombatRunMode.ExplorationPreview;

    /// <summary>PCG 预览专用的当前玩家移速；正式对局始终返回正式基础值。</summary>
    public float CurrentPlayerMoveSpeed => IsExplorationPreview ? explorationPreviewMoveSpeed : MoveSpeed;

    /// <summary>
    /// 仅在 PCG 探索预览中临时覆盖玩家移速。运行模式离开 CombatHud 后自动复位，
    /// 不写入数值配置、存档或正式对局状态。
    /// </summary>
    public bool SetExplorationPreviewPlayerMoveSpeed(float moveSpeed)
    {
        if (!IsExplorationPreview)
        {
            return false;
        }

        explorationPreviewMoveSpeed = Mathf.Clamp(moveSpeed, PreviewMoveSpeedMin, PreviewMoveSpeedMax);
        return true;
    }

    /// <summary>
    /// 指定下一次进入 CombatHud 时使用的运行模式。
    /// 请求仅消费一次；正式对局默认保留结算规则。
    /// </summary>
    public void RequestNextCombatRunMode(TotemCombatRunMode runMode)
    {
        nextCombatRunMode = runMode;
    }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        actorService = runtime.GetService<TotemActorService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        skillService = runtime.GetService<TotemSkillService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        enemyService = runtime.GetService<TotemEnemyService>();
        economyService = runtime.GetService<TotemEconomyService>();
        vfxService = runtime.GetService<TotemVfxService>();
        audioService = runtime.GetService<TotemAudioService>();
        uiService = runtime.GetService<TotemUIService>();
        runStatsService = runtime.GetService<TotemRunStatsService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        active = false;
        nextCombatRunMode = TotemCombatRunMode.Standard;
        currentCombatRunMode = TotemCombatRunMode.Standard;
        explorationPreviewMoveSpeed = MoveSpeed;
        inputService = null;
        actorService = null;
        weaponService = null;
        skillService = null;
        statusService = null;
        tattooService = null;
        relationshipService = null;
        matchClock = null;
        enemyService = null;
        economyService = null;
        vfxService = null;
        audioService = null;
        uiService = null;
        runStatsService = null;
        lastRunResult = null;
    }

    public void Tick(float deltaTime)
    {
        if (!active || runFinished || actorService?.Player == null)
        {
            return;
        }

        elapsedSec += deltaTime;
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - deltaTime);
        }

        if (skillCooldownRemaining > 0f)
        {
            skillCooldownRemaining = Mathf.Max(0f, skillCooldownRemaining - deltaTime);
        }

        var input = inputService?.Current ?? TotemInputSnapshot.Empty;
        var readiness = Runtime.GetService<TotemParticipantReadinessService>();
        if (readiness != null && !readiness.CanAct(actorService.Player))
        {
            EvaluateRunEnd();
            return;
        }

        TickMovement(input, deltaTime);
        TickAttack(input);
        TickSkill(input);
        EvaluateRunEnd();
    }

    public TotemCombatSnapshot CaptureCombatSnapshot()
    {
        var actorSnapshot = actorService?.CaptureActorSnapshot();
        return new TotemCombatSnapshot
        {
            active = active,
            playerHealth = actorService?.Player?.Health ?? 0f,
            aliveParticipantCount = actorSnapshot?.aliveParticipantCount ?? 0,
            aliveEnemyCount = enemyService?.CaptureSnapshot().aliveEnemyCount ?? 0,
            winnerParticipantId = lastRunResult?.winnerParticipantId ?? 0,
            killCount = killCount,
            lastAction = lastAction,
            lastReason = lastReason,
            lastTargetActorId = lastTargetActorId,
            lastTargetName = lastTargetName,
            lastDamage = lastDamage,
            lastKilled = lastKilled,
            lastWeaponId = lastWeaponId,
            lastTraitId = lastTraitId,
            lastSkillId = lastSkillId,
            lastHitCount = lastHitCount,
            lastTargetingMode = lastTargetingMode,
            lastAimSpreadHalfDegrees = lastAimSpreadHalfDegrees,
            lastAimForward = lastAimForward,
            elapsedSec = elapsedSec,
            attackCooldownRemaining = attackCooldownRemaining,
            skillCooldownRemaining = skillCooldownRemaining,
        };
    }

    public static TotemRunResultSnapshot BuildRunResult(bool win, string reason, int killCount, float playerHealth, int aliveParticipantCount, float elapsedSec)
    {
        return new TotemRunResultSnapshot
        {
            win = win,
            reason = string.IsNullOrWhiteSpace(reason) ? (win ? "Victory" : "Defeat") : reason,
            killCount = Mathf.Max(0, killCount),
            playerHealth = Mathf.Max(0f, playerHealth),
            aliveParticipantCount = Mathf.Max(0, aliveParticipantCount),
            elapsedSec = Mathf.Max(0f, elapsedSec),
        };
    }

    public static Vector3 ResolveAimForward(TotemInputSnapshot input, Vector3 origin, Vector3 fallbackForward)
    {
        if (input.hasAimWorldPoint)
        {
            var aimDirection = input.aimWorldPoint - origin;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                return aimDirection.normalized;
            }
        }

        if (input.move.sqrMagnitude > 0.001f)
        {
            return new Vector3(input.move.x, 0f, input.move.y).normalized;
        }

        fallbackForward.y = 0f;
        if (fallbackForward.sqrMagnitude > 0.0001f)
        {
            return fallbackForward.normalized;
        }

        return Vector3.forward;
    }

    private void TickMovement(TotemInputSnapshot input, float deltaTime)
    {
        float movementMultiplier = statusService == null ? 1f : statusService.GetMoveSpeedMultiplier(actorService.Player);
        Vector2 move = input.move;
        if (move.sqrMagnitude > 0.001f && movementMultiplier > 0f)
        {
            float movedDistance = CurrentPlayerMoveSpeed * movementMultiplier * deltaTime;
            actorService.MoveActor(actorService.Player, new Vector3(move.x, 0f, move.y) * movedDistance);
            tattooService?.Trigger("MoveTickEvent", actorService.Player, null, movedDistance);
        }

        if (input.dodgePressed)
        {
            if (movementMultiplier <= 0f)
            {
                RecordCombatAction("DodgeBlockedByStatus", "Status:Stun");
                GFTrace.Info("TotemCombat", "Dodge.Blocked", null, GFTrace.Data("reason", "Status:Stun"));
                return;
            }

            Vector3 dodgeDirection = move.sqrMagnitude > 0.001f
                ? new Vector3(move.x, 0f, move.y)
                : Vector3.forward;
            float dodgeDistance = DodgeDistance * movementMultiplier;
            actorService.MoveActor(actorService.Player, dodgeDirection.normalized * dodgeDistance);
            actorService.NotifyActorDodge(actorService.Player, "PlayerDodge");
            tattooService?.Trigger("DodgePressedEvent", actorService.Player, null, dodgeDistance);
            audioService?.PlaySfxCue("sfx_dodge", actorService.Player.Position, "Combat.Dodge");
            RecordCombatAction("Dodge", "Applied", null, 0f, false, null, null, null, 0);
            GFTrace.Info("TotemCombat", "Dodge", null, GFTrace.Data("distance", dodgeDistance.ToString("F1")));
        }
    }

    private void TickAttack(TotemInputSnapshot input)
    {
        bool charged = input.attackHeld && input.attackHoldDuration >= ChargeThreshold;
        if ((!input.attackPressed && !charged) || attackCooldownRemaining > 0f)
        {
            return;
        }

        if (statusService != null && !statusService.CanAct(actorService.Player))
        {
            RecordCombatAction("AttackBlockedByStatus", "Status:Stun");
            GFTrace.Info("TotemCombat", "Attack.Blocked", null, GFTrace.Data("reason", "Status:Stun"));
            return;
        }

        float maxRange = 30f;
        float aimSpreadHalfDegrees = 180f;
        var weaponState = weaponService?.GetOrCreateState(actorService.Player);
        if (weaponState?.Weapon != null)
        {
            var multipliers = TotemWeaponService.GetMultipliers(weaponState.Level);
            float rangeMultiplier = tattooService == null ? 1f : tattooService.ResolveRangeMultiplier(actorService.Player);
            maxRange = (weaponState.Weapon.Range + multipliers.RangeAdd) * rangeMultiplier;
            aimSpreadHalfDegrees = weaponState.Weapon.AimSpreadHalfDegrees;
        }

        Vector3 aimForward = ResolveAimForward(input, actorService.Player.Position, GetActorFallbackForward(actorService.Player));
        TotemCombatantModel target = SelectRuntimeAimTarget(
            actorService.Player.Position,
            aimForward,
            maxRange,
            aimSpreadHalfDegrees,
            out string targetingMode);
        lastTargetingMode = targetingMode;
        lastAimSpreadHalfDegrees = aimSpreadHalfDegrees;
        lastAimForward = aimForward;
        if (target == null)
        {
            RecordCombatAction("AttackNoTarget", "NoTarget");
            return;
        }

        float chargeRatio = input.attackHoldDuration <= 0f ? 0f : Mathf.Clamp01(input.attackHoldDuration / 1.2f);
        var fireResult = weaponService?.FireWeapon(actorService.Player, target, charged, chargeRatio);
        if (fireResult != null && !fireResult.Fired)
        {
            RecordCombatAction(fireResult.Reason, fireResult.Reason, target);
            return;
        }

        float baseDamage = fireResult?.Damage ?? (charged ? ChargedDamage : BasicDamage);
        float damage = tattooService == null
            ? baseDamage
            : tattooService.ResolveAttackDamage(actorService.Player, target as TotemActorModel, baseDamage, out _);
        actorService.NotifyActorAttack(actorService.Player, charged ? "PlayerChargedAttack" : "PlayerAttack");
        bool killed = ApplyDamageAndEvaluate(target, damage, actorService.Player, charged ? "PlayerChargedAttack" : "PlayerAttack");
        if (killed)
        {
            killCount++;
        }

        if (target is TotemActorModel participantTarget)
        {
            weaponService?.ApplyTraitEffect(fireResult, actorService.Player, participantTarget, killed);
        }
        else if (target is TotemEnemyModel enemyTarget)
        {
            weaponService?.ApplyTraitEffect(fireResult, actorService.Player, enemyTarget, killed);
        }
        vfxService?.SpawnProjectileTrail(actorService.Player.Position, target.Position, fireResult?.Projectile, true, charged);
        vfxService?.SpawnAttackHit(target.Position, fireResult?.Weapon?.WeaponId, charged);
        if (target is TotemEnemyModel tattooEnemyTarget)
        {
            tattooService?.TriggerEnemy("AttackHitEvent", actorService.Player, tattooEnemyTarget, baseDamage);
        }
        else
        {
            tattooService?.Trigger("AttackHitEvent", actorService.Player, target as TotemActorModel, baseDamage);
        }
        attackCooldownRemaining = fireResult == null ? AttackCooldown : 0f;
        RecordCombatAction(
            charged ? "ChargedAttack" : "Attack",
            "Applied",
            target,
            damage,
            killed,
            fireResult?.Weapon?.WeaponId,
            fireResult?.ActiveTrait?.TraitId,
            null,
            1);
        GFTrace.Info("TotemCombat", "Attack", null, GFTrace.Data(
            "target", target.Name,
            "damage", damage.ToString("F1"),
            "killed", killed.ToString()));
    }

    private void TickSkill(TotemInputSnapshot input)
    {
        int slot = ResolveRequestedSkillSlot(input);
        if (slot < 0 || skillCooldownRemaining > 0f)
        {
            return;
        }

        if (statusService != null && !statusService.CanAct(actorService.Player))
        {
            RecordCombatAction("SkillBlockedByStatus", "Status:Stun");
            GFTrace.Info("TotemCombat", "Skill.Blocked", null, GFTrace.Data("reason", "Status:Stun"));
            return;
        }

        TotemSkillDefinition skill = null;
        if (skillService != null && !skillService.TryCastSlot(actorService.Player, slot, out skill))
        {
            RecordCombatAction("SkillUnavailable", "CooldownOrNoCharges");
            return;
        }

        actorService.NotifyActorAttack(actorService.Player, skill == null ? "PlayerSkill" : $"PlayerSkill:{skill.SkillId}");

        var weaponState = weaponService?.GetOrCreateState(actorService.Player);
        float skillDamage = TotemSkillService.ResolveSkillDamage(skill, weaponState?.Weapon, SkillDamage);
        float rangeMultiplier = tattooService == null ? 1f : tattooService.ResolveRangeMultiplier(actorService.Player);
        float skillRadius = (skill == null || skill.Radius <= 0f ? SkillRadius : skill.Radius) * rangeMultiplier;
        Vector3 origin = actorService.Player.Position;
        int hitCount = 0;
        bool skillKilled = false;
        TotemCombatantModel lastSkillTarget = null;

        if (skill != null && skill.HitShape != TotemSkillHitShape.Circle)
        {
            float weaponRange = 30f;
            if (weaponState?.Weapon != null)
            {
                var multipliers = TotemWeaponService.GetMultipliers(weaponState.Level);
                weaponRange = (weaponState.Weapon.Range + multipliers.RangeAdd) * rangeMultiplier;
            }

            float maxRange = Mathf.Max(skillRadius, weaponRange);
            float aimSpreadHalfDegrees = weaponState?.Weapon?.AimSpreadHalfDegrees ?? 180f;
            Vector3 aimForward = ResolveAimForward(input, origin, GetActorFallbackForward(actorService.Player));
            TotemCombatantModel target = SelectRuntimeAimTarget(
                origin,
                aimForward,
                maxRange,
                aimSpreadHalfDegrees,
                out string targetingMode);
            lastTargetingMode = $"Skill:{targetingMode}";
            lastAimSpreadHalfDegrees = aimSpreadHalfDegrees;
            lastAimForward = aimForward;
            if (target != null && skillDamage > 0f)
            {
                bool killed = ApplyDamageAndEvaluate(target, skillDamage, actorService.Player, $"PlayerSkill:{skill.SkillId}");
                if (killed)
                {
                    killCount++;
                    skillKilled = true;
                }

                lastSkillTarget = target;
                hitCount = 1;
                if (target is TotemEnemyModel enemyTarget && enemyTarget.IsAlive)
                {
                    tattooService?.TriggerEnemy("SkillCastEvent", actorService.Player, enemyTarget, skillDamage);
                }
            }
        }
        else
        {
            lastTargetingMode = "Skill:Circle";
            lastAimSpreadHalfDegrees = 0f;
            lastAimForward = Vector3.zero;
            float radiusSqr = skillRadius * skillRadius;
            for (int i = 0; i < actorService.Actors.Count; i++)
            {
                var actor = actorService.Actors[i];
                if (!IsLegalParticipantTarget(actor))
                {
                    continue;
                }

                if ((actor.Position - origin).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (skillDamage > 0f)
                {
                    bool killed = ApplyDamageAndEvaluate(actor, skillDamage, actorService.Player, skill == null ? "PlayerSkill" : $"PlayerSkill:{skill.SkillId}");
                    if (killed)
                    {
                        killCount++;
                        skillKilled = true;
                    }
                }

                lastSkillTarget = actor;
                hitCount++;
            }

            int enemyCount = enemyService?.CopyAliveEnemies(enemyTargetBuffer) ?? 0;
            for (int i = 0; i < enemyCount; i++)
            {
                TotemEnemyModel enemy = enemyTargetBuffer[i];
                if (enemy == null || (enemy.Position - origin).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (skillDamage > 0f)
                {
                    bool killed = ApplyDamageAndEvaluate(enemy, skillDamage, actorService.Player, skill == null ? "PlayerSkill" : $"PlayerSkill:{skill.SkillId}");
                    if (killed)
                    {
                        killCount++;
                        skillKilled = true;
                    }
                }

                lastSkillTarget = enemy;
                hitCount++;
                if (enemy.IsAlive)
                {
                    tattooService?.TriggerEnemy("SkillCastEvent", actorService.Player, enemy, skillDamage);
                }
            }
        }

        tattooService?.Trigger("SkillCastEvent", actorService.Player, null, skillDamage);
        vfxService?.SpawnSkillBurst(origin, skill?.SkillId, skillRadius);
        audioService?.PlaySfxCue("sfx_skill_cast", origin, skill == null ? "Combat.Skill" : $"Combat.Skill.{skill.SkillId}");
        skillCooldownRemaining = skillService == null ? SkillCooldown : 0f;
        RecordCombatAction("Skill", hitCount > 0 ? "Applied" : "NoTarget", lastSkillTarget, skillDamage, skillKilled, null, null, skill?.SkillId, hitCount);
        GFTrace.Info("TotemCombat", "Skill", null, GFTrace.Data("hitCount", hitCount.ToString()));
    }

    private static int ResolveRequestedSkillSlot(TotemInputSnapshot input)
    {
        if (input.skillSlotEPressed || input.skillPressed)
        {
            return 0;
        }

        if (input.skillSlotQPressed)
        {
            return 1;
        }

        return -1;
    }

    private TotemCombatantModel SelectRuntimeAimTarget(
        Vector3 origin,
        Vector3 forward,
        float maxRange,
        float halfAngleDegrees,
        out string targetingMode)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude <= 0.0001f ? Vector3.forward : forward.normalized;
        float maxSqr = maxRange <= 0f ? float.MaxValue : maxRange * maxRange;
        float cosHalfAngle = halfAngleDegrees >= 179.99f
            ? -1f
            : Mathf.Cos(Mathf.Clamp(halfAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);
        TotemActorModel participantTarget = null;
        float participantScore = float.MaxValue;
        var actors = actorService?.Actors;
        if (actors != null)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                TotemActorModel candidate = actors[i];
                if (!IsLegalParticipantTarget(candidate))
                {
                    continue;
                }

                Vector3 delta = candidate.Position - origin;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr <= 0.0001f || sqr > maxSqr)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(sqr);
                float dot = Vector3.Dot(forward, delta / distance);
                if (dot < cosHalfAngle)
                {
                    continue;
                }

                float score = (1f - dot) * 100f + distance;
                if (score < participantScore)
                {
                    participantScore = score;
                    participantTarget = candidate;
                }
            }
        }

        TotemEnemyModel enemyTarget = enemyService?.FindBestAimTarget(origin, forward, maxRange, halfAngleDegrees);
        float enemyScore = enemyTarget == null
            ? float.MaxValue
            : ComputeAimScore(origin, forward, enemyTarget.Position);
        TotemCombatantModel result = enemyScore <= participantScore ? enemyTarget : participantTarget;
        string shape = halfAngleDegrees >= 179.99f ? "FullLock" : halfAngleDegrees <= 0.01f ? "RaycastGeometry" : "Cone";
        targetingMode = result == null ? shape : shape + ":" + result.Domain;
        return result;
    }

    private bool IsLegalParticipantTarget(TotemActorModel candidate)
    {
        if (candidate == null || candidate == actorService?.Player || !candidate.IsAlive)
        {
            return false;
        }

        if (relationshipService == null)
        {
            return true;
        }

        var decision = relationshipService.EvaluateDamage(
            actorService.Player,
            candidate,
            new TotemCombatRelationshipContext(matchClock?.WorldTime ?? elapsedSec));
        return decision.Allowed;
    }

    private static float ComputeAimScore(Vector3 origin, Vector3 forward, Vector3 target)
    {
        Vector3 delta = target - origin;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            return 0f;
        }

        return (1f - Vector3.Dot(forward, delta / distance)) * 100f + distance;
    }

    private void RecordCombatAction(
        string action,
        string reason = null,
        TotemCombatantModel target = null,
        float damage = 0f,
        bool killed = false,
        string weaponId = null,
        string traitId = null,
        string skillId = null,
        int hitCount = 0)
    {
        lastAction = action ?? string.Empty;
        lastReason = reason ?? string.Empty;
        lastTargetActorId = target?.CombatantId ?? 0;
        lastTargetName = target?.Name ?? string.Empty;
        lastDamage = Mathf.Max(0f, damage);
        lastKilled = killed;
        lastWeaponId = weaponId ?? string.Empty;
        lastTraitId = traitId ?? string.Empty;
        lastSkillId = skillId ?? string.Empty;
        lastHitCount = Mathf.Max(0, hitCount);
    }

    private bool ApplyDamageAndEvaluate(TotemCombatantModel target, float damage, TotemActorModel source = null, string reason = null)
    {
        if (target is TotemActorModel participant)
        {
            return actorService.ApplyDamage(participant, damage, source, reason);
        }

        if (target is TotemEnemyModel enemy && enemyService != null)
        {
            bool wasAlive = enemy.IsAlive;
            enemyService.TryApplyDamage(
                enemy.CombatantId,
                source,
                damage,
                reason,
                matchClock?.WorldTime ?? elapsedSec,
                out _);
            return wasAlive && !enemy.IsAlive;
        }

        return false;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            currentCombatRunMode = nextCombatRunMode;
            nextCombatRunMode = TotemCombatRunMode.Standard;
            explorationPreviewMoveSpeed = MoveSpeed;
            active = true;
            runFinished = false;
            elapsedSec = 0f;
            killCount = 0;
            attackCooldownRemaining = 0f;
            skillCooldownRemaining = 0f;
            lastAction = "CombatStarted";
            lastTargetingMode = string.Empty;
            lastAimSpreadHalfDegrees = 0f;
            lastAimForward = Vector3.forward;
            lastRunResult = null;
            GFTrace.Success("TotemCombat", "Combat.Started");
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            active = false;
            runFinished = false;
            currentCombatRunMode = TotemCombatRunMode.Standard;
            explorationPreviewMoveSpeed = MoveSpeed;
            GFTrace.Info("TotemCombat", "Combat.Stopped", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void EvaluateRunEnd()
    {
        if (currentCombatRunMode == TotemCombatRunMode.ExplorationPreview
            || runFinished
            || actorService?.Player == null)
        {
            return;
        }

        var snapshot = actorService.CaptureActorSnapshot();
        if (snapshot.aliveParticipantCount == 1)
        {
            var winner = actorService.FindUniqueAliveParticipant();
            int winnerParticipantId = winner?.ActorId ?? 0;
            bool localPlayerWon = ReferenceEquals(winner, actorService.Player);
            FinishRun(localPlayerWon, "LastParticipantStanding", snapshot, winnerParticipantId);
            return;
        }

        if (snapshot.aliveParticipantCount == 0)
        {
            FinishRun(false, "NoParticipantsAlive", snapshot, 0);
            return;
        }

        if (!actorService.Player.IsAlive)
        {
            FinishRun(false, "PlayerDefeated", snapshot, 0);
        }
    }

    private void FinishRun(bool win, string reason, TotemActorSnapshot actorSnapshot, int winnerParticipantId)
    {
        runFinished = true;
        active = false;
        var result = BuildRunResult(
            win,
            reason,
            killCount,
            actorService?.Player?.Health ?? 0f,
            actorSnapshot?.aliveParticipantCount ?? 0,
            elapsedSec);
        result.aliveEnemyCount = enemyService?.CaptureSnapshot().aliveEnemyCount ?? 0;
        result.winnerParticipantId = winnerParticipantId;
        lastRunResult = result;
        lastAction = win ? "RunWin" : "RunDefeat";
        if (winnerParticipantId > 0)
        {
            GFTrace.Success("TotemCombat", "Run.WinnerResolved", null, GFTrace.Data(
                "winnerParticipantId", winnerParticipantId.ToString(),
                "localPlayerWon", win.ToString(),
                "reason", result.reason));
        }

        GFTrace.Success("TotemCombat", "Run.Ended", null, GFTrace.Data(
            "win", win.ToString(),
            "reason", result.reason,
            "killCount", result.killCount.ToString(),
            "aliveParticipantCount", result.aliveParticipantCount.ToString(),
            "aliveEnemyCount", result.aliveEnemyCount.ToString(),
            "winnerParticipantId", result.winnerParticipantId.ToString(),
            "elapsed", result.elapsedSec.ToString("F1")));
        runStatsService ??= Runtime.GetService<TotemRunStatsService>();
        result.cumulativeStats = runStatsService?.RecordRun(result);
        uiService ??= Runtime.GetService<TotemUIService>();
        uiService?.OpenRunResult(result);
    }

    private static Vector3 GetActorFallbackForward(TotemActorModel actor)
    {
        if (actor?.GameObject != null)
        {
            var forward = actor.GameObject.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                return forward.normalized;
            }
        }

        return Vector3.forward;
    }
}
