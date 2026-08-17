
using UnityEngine;

public sealed class TotemCombatService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    private const float MoveSpeed = 5f;
    private const float DodgeDistance = 4f;

    private TotemGameFlowService flowService;
    private TotemInputService inputService;
    private TotemActorService actorService;
    private TotemWeaponService weaponService;
    private TotemStatusService statusService;
    private TotemFirstPlayableElementService elementService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemCombatRelationshipService relationshipService;
    private TotemMatchClockService matchClock;
    private TotemVfxService vfxService;
    private TotemAudioService audioService;
    private TotemUIService uiService;
    private TotemRunStatsService runStatsService;
    private TotemMatchFlowService matchFlowService;
    private TotemFirstPlayableSocialService socialService;
    private float elapsedSec;
    private int killCount;
    private int gameplayCommandSequence;
    private bool active;
    private bool runFinished;
    private string lastAction = string.Empty;
    private string lastReason = string.Empty;
    private int lastTargetActorId;
    private string lastTargetName = string.Empty;
    private float lastDamage;
    private bool lastKilled;
    private string lastWeaponId = string.Empty;
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
        statusService = runtime.GetService<TotemStatusService>();
        elementService = runtime.GetService<TotemFirstPlayableElementService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        vfxService = runtime.GetService<TotemVfxService>();
        audioService = runtime.GetService<TotemAudioService>();
        uiService = runtime.GetService<TotemUIService>();
        runStatsService = runtime.GetService<TotemRunStatsService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        socialService = runtime.GetService<TotemFirstPlayableSocialService>();
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
        statusService = null;
        elementService = null;
        lifecycleService = null;
        relationshipService = null;
        matchClock = null;
        vfxService = null;
        audioService = null;
        uiService = null;
        runStatsService = null;
        matchFlowService = null;
        socialService = null;
        lastRunResult = null;
        gameplayCommandSequence = 0;
    }

    public void Tick(float deltaTime)
    {
        if (!active || runFinished || actorService?.Player == null)
        {
            return;
        }

        elapsedSec += deltaTime;
        var input = inputService?.Current ?? TotemInputSnapshot.Empty;
        var readiness = Runtime.GetService<TotemParticipantReadinessService>();
        if (readiness != null && !readiness.CanAct(actorService.Player))
        {
            if (lifecycleService?.IsDowned(actorService.Player) == true)
            {
                TickDownedMovement(input, deltaTime);
            }

            EvaluateRunEnd();
            return;
        }

        TickMovement(input, deltaTime);
        TickAttack(input);
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
            winnerParticipantId = lastRunResult?.winnerParticipantId ?? 0,
            killCount = killCount,
            lastAction = lastAction,
            lastReason = lastReason,
            lastTargetActorId = lastTargetActorId,
            lastTargetName = lastTargetName,
            lastDamage = lastDamage,
            lastKilled = lastKilled,
            lastWeaponId = lastWeaponId,
            lastTargetingMode = lastTargetingMode,
            lastAimSpreadHalfDegrees = lastAimSpreadHalfDegrees,
            lastAimForward = lastAimForward,
            elapsedSec = elapsedSec,
        };
    }

    public TotemRunResultSnapshot FinishFiveRoundFlow()
    {
        if (runFinished)
        {
            return lastRunResult;
        }

        TotemActorSnapshot actorSnapshot = actorService?.CaptureActorSnapshot() ?? new TotemActorSnapshot();
        TotemMatchSettlement settlement = ResolveTimeoutSettlement();
        if (!settlement.Resolved || settlement.Draw)
        {
            FinishRun(false, "FiveRoundExactTie", actorSnapshot, 0, 0, draw: true);
            return lastRunResult;
        }

        bool localTeamWon = actorService?.Player != null
            && actorService.Player.TeamId.Value == settlement.Winner.TeamId;
        FinishRun(
            localTeamWon,
            "FiveRoundScoreResolved",
            actorSnapshot,
            settlement.Winner.RepresentativeParticipantId,
            settlement.Winner.TeamId,
            draw: false);
        return lastRunResult;
    }

    public TotemRunResultSnapshot FinishLocalTeamExtraction(int teamId, int representativeParticipantId)
    {
        if (runFinished)
        {
            return lastRunResult;
        }

        TotemActorSnapshot actorSnapshot = actorService?.CaptureActorSnapshot() ?? new TotemActorSnapshot();
        FinishRun(
            true,
            "LocalTeamExtracted",
            actorSnapshot,
            representativeParticipantId,
            teamId,
            draw: false,
            extracted: true);
        return lastRunResult;
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
        if (elementService != null && actorService.Player != null)
        {
            movementMultiplier *= elementService.GetMoveSpeedMultiplier(actorService.Player.CombatantId);
        }
        if (lifecycleService != null && actorService.Player != null)
        {
            movementMultiplier *= lifecycleService.GetMoveSpeedMultiplier(actorService.Player);
        }
        Vector2 move = input.move;
        if (move.sqrMagnitude > 0.001f && movementMultiplier > 0f)
        {
            var command = new TotemGameplayCommand(
                new TotemParticipantId(actorService.Player.ParticipantId),
                TotemGameplayCommandSource.HumanInput,
                TotemGameplayCommandType.Move,
                gameplayCommandSequence++,
                new Vector3(move.x, 0f, move.y));
            actorService.TryApplyFirstPlayableMoveCommand(
                command,
                deltaTime,
                MoveSpeed * movementMultiplier,
                out _);
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
            audioService?.PlaySfxCue("sfx_dodge", actorService.Player.Position, "Combat.Dodge");
            RecordCombatAction("Dodge", "Applied");
            GFTrace.Info("TotemCombat", "Dodge", null, GFTrace.Data("distance", dodgeDistance.ToString("F1")));
        }
    }

    private void TickDownedMovement(TotemInputSnapshot input, float deltaTime)
    {
        Vector2 move = input.move;
        if (move.sqrMagnitude <= 0.001f || deltaTime <= 0f)
        {
            return;
        }

        float multiplier = statusService == null ? 1f : statusService.GetMoveSpeedMultiplier(actorService.Player);
        if (elementService != null)
        {
            multiplier *= elementService.GetMoveSpeedMultiplier(actorService.Player.CombatantId);
        }
        if (lifecycleService != null)
        {
            multiplier *= lifecycleService.GetMoveSpeedMultiplier(actorService.Player);
        }

        if (multiplier > 0f)
        {
            var command = new TotemGameplayCommand(
                new TotemParticipantId(actorService.Player.ParticipantId),
                TotemGameplayCommandSource.HumanInput,
                TotemGameplayCommandType.Move,
                gameplayCommandSequence++,
                new Vector3(move.x, 0f, move.y));
            actorService.TryApplyFirstPlayableMoveCommand(
                command,
                deltaTime,
                MoveSpeed * multiplier,
                out _);
        }
    }

    private void TickAttack(TotemInputSnapshot input)
    {
        if (!input.attackPressed)
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
            maxRange = weaponState.Weapon.Range + multipliers.RangeAdd;
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

        if (weaponService == null)
        {
            RecordCombatAction("AttackUnavailable", "WeaponServiceMissing", target);
            return;
        }

        var fireCommand = new TotemGameplayCommand(
            new TotemParticipantId(actorService.Player.ParticipantId),
            TotemGameplayCommandSource.HumanInput,
            TotemGameplayCommandType.Fire,
            gameplayCommandSequence++,
            aimForward,
            target.CombatantId);
        if (!weaponService.TryApplyFirstPlayableFireCommand(
                fireCommand,
                damageMultiplier: 1f,
                out TotemGunAttackResult attackResult))
        {
            RecordCombatAction(attackResult.Reason, attackResult.Reason, target);
            return;
        }

        if (attackResult.Killed)
        {
            killCount++;
        }

        RecordCombatAction(
            "GunAttack",
            attackResult.Reason,
            target,
            attackResult.DirectDamage.EffectiveDamage,
            attackResult.Killed,
            attackResult.Weapon.WeaponId);
        GFTrace.Info("TotemCombat", "Attack", null, GFTrace.Data(
            "target", target.Name,
            "damage", attackResult.DirectDamage.EffectiveDamage.ToString("F1"),
            "killed", attackResult.Killed.ToString()));
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

        TotemCombatantModel result = participantTarget;
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

    private void RecordCombatAction(
        string action,
        string reason = null,
        TotemCombatantModel target = null,
        float damage = 0f,
        bool killed = false,
        string weaponId = null)
    {
        lastAction = action ?? string.Empty;
        lastReason = reason ?? string.Empty;
        lastTargetActorId = target?.CombatantId ?? 0;
        lastTargetName = target?.Name ?? string.Empty;
        lastDamage = Mathf.Max(0f, damage);
        lastKilled = killed;
        lastWeaponId = weaponId ?? string.Empty;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            active = true;
            runFinished = false;
            elapsedSec = 0f;
            killCount = 0;
            lastAction = "CombatStarted";
            lastTargetingMode = string.Empty;
            lastAimSpreadHalfDegrees = 0f;
            lastAimForward = Vector3.forward;
            lastRunResult = null;
            gameplayCommandSequence = 0;
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
        if (runFinished
            || actorService?.Player == null)
        {
            return;
        }

        var snapshot = actorService.CaptureActorSnapshot();
        TotemActorModel winningTeamRepresentative = actorService.FindUniqueAliveTeamRepresentative(out int aliveTeamCount);
        if (aliveTeamCount == 0 || snapshot.aliveParticipantCount == 0)
        {
            FinishRun(false, "NoParticipantsAlive", snapshot, 0, 0, draw: true);
            return;
        }

        if (aliveTeamCount == 1 && winningTeamRepresentative != null)
        {
            bool localTeamWon = winningTeamRepresentative.TeamId == actorService.Player.TeamId;
            FinishRun(localTeamWon, "LastTeamStanding", snapshot, winningTeamRepresentative.ActorId, winningTeamRepresentative.TeamId.Value, draw: false);
            return;
        }
    }

    private void FinishRun(
        bool win,
        string reason,
        TotemActorSnapshot actorSnapshot,
        int winnerParticipantId,
        int winnerTeamId,
        bool draw,
        bool extracted = false)
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
        result.winnerParticipantId = winnerParticipantId;
        result.winnerTeamId = winnerTeamId;
        result.draw = draw;
        result.extracted = extracted;
        lastRunResult = result;
        lastAction = win ? "RunWin" : "RunDefeat";
        matchFlowService ??= Runtime.GetService<TotemMatchFlowService>();
        matchFlowService?.CompleteMatchToResult(reason);
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
            "winnerParticipantId", result.winnerParticipantId.ToString(),
            "winnerTeamId", result.winnerTeamId.ToString(),
            "draw", result.draw.ToString(),
            "elapsed", result.elapsedSec.ToString("F1")));
        runStatsService ??= Runtime.GetService<TotemRunStatsService>();
        result.cumulativeStats = runStatsService?.RecordRun(result);
        uiService ??= Runtime.GetService<TotemUIService>();
        uiService?.OpenRunResult(result);
    }

    private TotemMatchSettlement ResolveTimeoutSettlement()
    {
        var actors = actorService?.Actors;
        if (actors == null || actors.Count == 0)
        {
            return default;
        }

        var candidates = new TotemTeamSettlementCandidate[TotemFirstPlayableRules.TeamCount];
        var teamIds = new int[TotemFirstPlayableRules.TeamCount];
        var eliminations = new int[TotemFirstPlayableRules.TeamCount];
        var damage = new float[TotemFirstPlayableRules.TeamCount];
        var alive = new int[TotemFirstPlayableRules.TeamCount];
        var health = new float[TotemFirstPlayableRules.TeamCount];
        var representative = new int[TotemFirstPlayableRules.TeamCount];
        int teamCount = 0;

        socialService ??= Runtime.GetService<TotemFirstPlayableSocialService>();
        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel actor = actors[i];
            if (actor == null || actor.TeamId.Value <= 0)
            {
                continue;
            }

            int teamIndex = -1;
            for (int team = 0; team < teamCount; team++)
            {
                if (teamIds[team] == actor.TeamId.Value)
                {
                    teamIndex = team;
                    break;
                }
            }

            if (teamIndex < 0)
            {
                if (teamCount >= teamIds.Length)
                {
                    continue;
                }

                teamIndex = teamCount++;
                teamIds[teamIndex] = actor.TeamId.Value;
                representative[teamIndex] = actor.ParticipantId;
            }
            else if (actor.ParticipantId < representative[teamIndex])
            {
                representative[teamIndex] = actor.ParticipantId;
            }

            TotemMatchAchievementSnapshot achievement = socialService?.CaptureAchievement(new TotemParticipantId(actor.ParticipantId)) ?? default;
            eliminations[teamIndex] += achievement.playerEliminations;
            damage[teamIndex] += achievement.playerDamage;
            TotemFirstPlayableParticipantLifeState life = lifecycleService?.GetOrCreateState(actor);
            if (life == null || !life.IsEliminated)
            {
                alive[teamIndex]++;
                health[teamIndex] += Mathf.Max(0f, actor.Health);
            }
        }

        for (int i = 0; i < teamCount; i++)
        {
            candidates[i] = new TotemTeamSettlementCandidate(
                teamIds[i],
                eliminations[i],
                damage[i],
                alive[i],
                health[i],
                representative[i]);
        }

        return TotemFirstPlayableMatchSettlement.Resolve(candidates, teamCount);
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
