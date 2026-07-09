using System.Collections.Generic;
using UnityEngine;

public sealed class TotemCombatService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float ChargeThreshold = 0.4f;

    private const float MoveSpeed = 5f;
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
    private TotemBossService bossService;
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

    public override string ServiceName => "Combat";

    public TotemRunResultSnapshot LastRunResult => lastRunResult;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        actorService = runtime.GetService<TotemActorService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        skillService = runtime.GetService<TotemSkillService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        bossService = runtime.GetService<TotemBossService>();
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
        inputService = null;
        actorService = null;
        weaponService = null;
        skillService = null;
        statusService = null;
        tattooService = null;
        bossService = null;
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
            aliveEnemyCount = actorSnapshot?.aliveEnemyCount ?? 0,
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

    public static TotemRunResultSnapshot BuildRunResult(bool win, string reason, int killCount, float playerHealth, int aliveEnemyCount, float elapsedSec)
    {
        return new TotemRunResultSnapshot
        {
            win = win,
            reason = string.IsNullOrWhiteSpace(reason) ? (win ? "Victory" : "Defeat") : reason,
            killCount = Mathf.Max(0, killCount),
            playerHealth = Mathf.Max(0f, playerHealth),
            aliveEnemyCount = Mathf.Max(0, aliveEnemyCount),
            elapsedSec = Mathf.Max(0f, elapsedSec),
        };
    }

    public static TotemActorModel FindBestConeTarget(IReadOnlyList<TotemActorModel> actors, Vector3 origin, Vector3 forward, float maxRange, float halfAngleDegrees)
    {
        if (actors == null || actors.Count <= 0)
        {
            return null;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        if (halfAngleDegrees >= 179.99f)
        {
            return FindClosestAliveEnemy(actors, origin, maxRange: 0f);
        }

        float cosHalfAngle = Mathf.Cos(halfAngleDegrees * Mathf.Deg2Rad);
        float bestScore = float.MaxValue;
        TotemActorModel bestTarget = null;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (!TotemActorService.IsEnemy(actor) || !actor.IsAlive)
            {
                continue;
            }

            Vector3 toEnemy = actor.Position - origin;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;
            if (distance > maxRange || distance < 0.001f)
            {
                continue;
            }

            float dot = Vector3.Dot(forward, toEnemy / distance);
            if (dot < cosHalfAngle)
            {
                continue;
            }

            float score = (1f - dot) * 100f + distance;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }

    public static TotemActorModel SelectAimTarget(
        IReadOnlyList<TotemActorModel> actors,
        Vector3 origin,
        Vector3 forward,
        float maxRange,
        float halfAngleDegrees,
        out string targetingMode)
    {
        if (halfAngleDegrees >= 179.99f)
        {
            targetingMode = "FullLock";
            return FindClosestAliveEnemy(actors, origin, maxRange: 0f);
        }

        if (halfAngleDegrees <= 0.01f)
        {
            targetingMode = "RaycastGeometry";
            return FindBestConeTarget(actors, origin, forward, maxRange, 0.01f);
        }

        targetingMode = "Cone";
        return FindBestConeTarget(actors, origin, forward, maxRange, halfAngleDegrees);
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

    public static TotemActorModel FindClosestAliveEnemy(IReadOnlyList<TotemActorModel> actors, Vector3 origin, float maxRange)
    {
        float maxRangeSqr = maxRange <= 0f ? float.MaxValue : maxRange * maxRange;
        float bestDistance = float.MaxValue;
        TotemActorModel bestTarget = null;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (!TotemActorService.IsEnemy(actor) || !actor.IsAlive)
            {
                continue;
            }

            float sqrDistance = (actor.Position - origin).sqrMagnitude;
            if (sqrDistance > maxRangeSqr || sqrDistance >= bestDistance)
            {
                continue;
            }

            bestDistance = sqrDistance;
            bestTarget = actor;
        }

        return bestTarget;
    }

    private void TickMovement(TotemInputSnapshot input, float deltaTime)
    {
        float movementMultiplier = statusService == null ? 1f : statusService.GetMoveSpeedMultiplier(actorService.Player);
        Vector2 move = input.move;
        if (move.sqrMagnitude > 0.001f && movementMultiplier > 0f)
        {
            float movedDistance = MoveSpeed * movementMultiplier * deltaTime;
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
        var target = SelectAimTarget(
            actorService.Actors,
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
            : tattooService.ResolveAttackDamage(actorService.Player, target, baseDamage, out _);
        actorService.NotifyActorAttack(actorService.Player, charged ? "PlayerChargedAttack" : "PlayerAttack");
        bool killed = ApplyDamageAndEvaluate(target, damage, actorService.Player, charged ? "PlayerChargedAttack" : "PlayerAttack");
        if (killed)
        {
            killCount++;
        }

        weaponService?.ApplyTraitEffect(fireResult, actorService.Player, target, killed);
        vfxService?.SpawnProjectileTrail(actorService.Player.Position, target.Position, fireResult?.Projectile, true, charged);
        vfxService?.SpawnAttackHit(target.Position, fireResult?.Weapon?.WeaponId, charged);
        tattooService?.Trigger("AttackHitEvent", actorService.Player, target, baseDamage);
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
        TotemActorModel lastSkillTarget = null;

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
            var target = SelectAimTarget(
                actorService.Actors,
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
                if (!TotemActorService.IsEnemy(actor) || !actor.IsAlive)
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

    private void RecordCombatAction(
        string action,
        string reason = null,
        TotemActorModel target = null,
        float damage = 0f,
        bool killed = false,
        string weaponId = null,
        string traitId = null,
        string skillId = null,
        int hitCount = 0)
    {
        lastAction = action ?? string.Empty;
        lastReason = reason ?? string.Empty;
        lastTargetActorId = target?.ActorId ?? 0;
        lastTargetName = target?.Name ?? string.Empty;
        lastDamage = Mathf.Max(0f, damage);
        lastKilled = killed;
        lastWeaponId = weaponId ?? string.Empty;
        lastTraitId = traitId ?? string.Empty;
        lastSkillId = skillId ?? string.Empty;
        lastHitCount = Mathf.Max(0, hitCount);
    }

    private bool ApplyDamageAndEvaluate(TotemActorModel target, float damage, TotemActorModel source = null, string reason = null)
    {
        bool killed = actorService.ApplyDamage(target, damage, source, reason);
        if (target != null && target.Kind == TotemActorKind.Boss)
        {
            bossService?.EvaluateBossHealth();
        }

        return killed;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
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
            GFTrace.Info("TotemCombat", "Combat.Stopped", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void EvaluateRunEnd()
    {
        if (runFinished || actorService?.Player == null)
        {
            return;
        }

        var snapshot = actorService.CaptureActorSnapshot();
        if (!actorService.Player.IsAlive)
        {
            FinishRun(false, "PlayerDefeated", snapshot);
            return;
        }

        if (snapshot.aliveEnemyCount <= 0)
        {
            FinishRun(true, "AllEnemiesDefeated", snapshot);
        }
    }

    private void FinishRun(bool win, string reason, TotemActorSnapshot actorSnapshot)
    {
        runFinished = true;
        active = false;
        var result = BuildRunResult(
            win,
            reason,
            killCount,
            actorService?.Player?.Health ?? 0f,
            actorSnapshot?.aliveEnemyCount ?? 0,
            elapsedSec);
        ApplyVictoryRewards(result);
        lastRunResult = result;
        lastAction = win ? "RunWin" : "RunDefeat";
        GFTrace.Success("TotemCombat", "Run.Ended", null, GFTrace.Data(
            "win", win.ToString(),
            "reason", result.reason,
            "killCount", result.killCount.ToString(),
            "aliveEnemyCount", result.aliveEnemyCount.ToString(),
            "elapsed", result.elapsedSec.ToString("F1"),
            "bossReward", result.bossDeathPatternRecipeId ?? string.Empty));
        runStatsService ??= Runtime.GetService<TotemRunStatsService>();
        result.cumulativeStats = runStatsService?.RecordRun(result);
        uiService ??= Runtime.GetService<TotemUIService>();
        uiService?.OpenRunResult(result);
    }

    private void ApplyVictoryRewards(TotemRunResultSnapshot result)
    {
        if (result == null || !result.win)
        {
            return;
        }

        bossService ??= Runtime.GetService<TotemBossService>();
        if (bossService == null || !bossService.TryClaimDeathReward(out string recipeId))
        {
            return;
        }

        result.bossRewardClaimed = true;
        result.bossDeathPatternRecipeId = recipeId;
        economyService ??= Runtime.GetService<TotemEconomyService>();
        bool unlocked = economyService != null
            && actorService?.Player != null
            && economyService.UnlockRecipe(actorService.Player, recipeId);
        GFTrace.Success("TotemCombat", "BossReward.Applied", null, GFTrace.Data(
            "recipeId", recipeId,
            "unlocked", unlocked.ToString()));
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
