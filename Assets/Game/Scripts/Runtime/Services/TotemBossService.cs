using System.Collections.Generic;
using UnityEngine;

public sealed class TotemBossService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float TransitionDuration = 0.8f;
    public const float SkillCooldown = 4f;

    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemBossPhase[] runtimePhases = System.Array.Empty<TotemBossPhase>();
    private bool active;
    private int currentPhase = 1;
    private float transitionRemaining;
    private float skillCooldownRemaining;
    private int phaseSkillCursor;
    private int phaseSkillCursorPhase = -1;
    private bool deathRewardClaimed;
    private string lastDeathRewardRecipeId = string.Empty;

    public override string ServiceName => "Boss";

    public bool IsActive => active;

    public int CurrentPhase => currentPhase;

    public float EnrageMultiplier => GetRuntimePhase(currentPhase).EnrageMultiplier;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        runtimePhases = NonEmpty(runtime.GetService<TotemDataService>()?.GameplayCatalog?.CreateBossPhases(), LoadPhases());
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

        actorService = null;
        runtimePhases = System.Array.Empty<TotemBossPhase>();
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        active = false;
        currentPhase = 1;
        transitionRemaining = 0f;
        skillCooldownRemaining = 0f;
        phaseSkillCursor = 0;
        phaseSkillCursorPhase = -1;
        deathRewardClaimed = false;
        lastDeathRewardRecipeId = string.Empty;
    }

    public void Tick(float deltaTime)
    {
        if (!active || deltaTime <= 0f)
        {
            return;
        }

        if (transitionRemaining > 0f)
        {
            transitionRemaining = Mathf.Max(0f, transitionRemaining - deltaTime);
            return;
        }

        if (skillCooldownRemaining > 0f)
        {
            skillCooldownRemaining = Mathf.Max(0f, skillCooldownRemaining - deltaTime);
        }

        EvaluateBossHealth();
    }

    public static IReadOnlyList<TotemBossPhase> GetPhases()
    {
        return LoadPhases();
    }

    public static int ResolvePhaseByHpRatio(float hpRatio)
    {
        if (hpRatio <= GetPhase(3).HPThreshold)
        {
            return 3;
        }

        if (hpRatio <= GetPhase(2).HPThreshold)
        {
            return 2;
        }

        return 1;
    }

    public IReadOnlyList<TotemBossPhase> GetRuntimePhases()
    {
        return runtimePhases;
    }

    public int ResolveRuntimePhaseByHpRatio(float hpRatio)
    {
        if (hpRatio <= GetRuntimePhase(3).HPThreshold)
        {
            return 3;
        }

        if (hpRatio <= GetRuntimePhase(2).HPThreshold)
        {
            return 2;
        }

        return 1;
    }

    public void EvaluateBossHealth()
    {
        var boss = actorService?.Boss;
        if (boss == null || boss.MaxHealth <= 0f)
        {
            return;
        }

        float ratio = boss.Health / boss.MaxHealth;
        int resolvedPhase = ResolveRuntimePhaseByHpRatio(ratio);
        if (currentPhase == 1 && resolvedPhase >= 2)
        {
            TransitionToPhase(2);
        }
        else if (currentPhase == 2 && resolvedPhase >= 3)
        {
            TransitionToPhase(3);
        }
    }

    public bool CanUseSkill(out string skillId)
    {
        skillId = string.Empty;
        if (!active || transitionRemaining > 0f || skillCooldownRemaining > 0f)
        {
            return false;
        }

        skillId = NextRuntimeSkillId();
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        skillCooldownRemaining = SkillCooldown;
        return true;
    }

    public bool TryClaimDeathReward(out string recipeId)
    {
        recipeId = string.Empty;
        if (deathRewardClaimed)
        {
            return false;
        }

        var boss = actorService?.Boss;
        if (boss == null || boss.IsAlive)
        {
            return false;
        }

        recipeId = GetRuntimePhase(3).DeathPatternRecipeId;
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            recipeId = string.Empty;
            return false;
        }

        deathRewardClaimed = true;
        lastDeathRewardRecipeId = recipeId;
        GFTrace.Success("TotemBoss", "DeathReward.Claimed", null, GFTrace.Data(
            "boss", boss.Name,
            "recipeId", recipeId));
        return true;
    }

    public TotemBossSnapshot CaptureSnapshot()
    {
        var boss = actorService?.Boss;
        float hpRatio = boss == null || boss.MaxHealth <= 0f ? 0f : boss.Health / boss.MaxHealth;
        var phase = GetRuntimePhase(currentPhase);
        return new TotemBossSnapshot
        {
            active = active,
            bossId = phase.BossId,
            currentPhase = currentPhase,
            currentPhaseSkillIds = phase.SkillIds,
            currentPhaseVFXId = phase.PhaseVFXId,
            currentPhaseBGMCueId = phase.PhaseBGMCueId,
            hpRatio = hpRatio,
            enrageMultiplier = EnrageMultiplier,
            transitioning = transitionRemaining > 0f,
            deathPatternRecipeId = GetRuntimePhase(3).DeathPatternRecipeId,
            deathRewardClaimed = deathRewardClaimed,
            lastDeathRewardRecipeId = lastDeathRewardRecipeId,
        };
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            active = true;
            currentPhase = 1;
            transitionRemaining = 0f;
            skillCooldownRemaining = 0f;
            phaseSkillCursor = 0;
            phaseSkillCursorPhase = currentPhase;
            deathRewardClaimed = false;
            lastDeathRewardRecipeId = string.Empty;
            GFTrace.Success("TotemBoss", "Activated");
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRuntimeState();
            GFTrace.Info("TotemBoss", "Deactivated", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void TransitionToPhase(int phase)
    {
        int previous = currentPhase;
        currentPhase = phase;
        transitionRemaining = TransitionDuration;
        phaseSkillCursor = 0;
        phaseSkillCursorPhase = currentPhase;
        GFTrace.Success("TotemBoss", "PhaseChanged", null, GFTrace.Data(
            "from", previous.ToString(),
            "to", currentPhase.ToString(),
            "enrage", EnrageMultiplier.ToString("F2")));
    }

    private static TotemBossPhase GetPhase(int phase)
    {
        return FindPhase(LoadPhases(), phase);
    }

    private TotemBossPhase GetRuntimePhase(int phase)
    {
        var phases = NonEmpty(runtimePhases, LoadPhases());
        return FindPhase(phases, phase);
    }

    private static TotemBossPhase[] LoadPhases()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateBossPhases(),
            System.Array.Empty<TotemBossPhase>());
    }

    private static TotemBossPhase FindPhase(TotemBossPhase[] phases, int phase)
    {
        phases = NonEmpty(phases, System.Array.Empty<TotemBossPhase>());
        if (phases.Length <= 0)
        {
            return new TotemBossPhase();
        }

        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i].PhaseIndex == phase)
            {
                return phases[i];
            }
        }

        return phases[0];
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    private string NextRuntimeSkillId()
    {
        if (phaseSkillCursorPhase != currentPhase)
        {
            phaseSkillCursor = 0;
            phaseSkillCursorPhase = currentPhase;
        }

        var phase = GetRuntimePhase(currentPhase);
        string[] ids = SplitSkillIds(phase.SkillIds);
        if (ids.Length <= 0)
        {
            return string.Empty;
        }

        string skillId = ids[Mathf.Abs(phaseSkillCursor) % ids.Length];
        phaseSkillCursor++;
        return skillId;
    }

    private static string[] SplitSkillIds(string skillIds)
    {
        if (string.IsNullOrWhiteSpace(skillIds))
        {
            return System.Array.Empty<string>();
        }

        string[] raw = skillIds.Split(',');
        var result = new List<string>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            string id = raw[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(id))
            {
                result.Add(id);
            }
        }

        return result.ToArray();
    }
}
