using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class TotemTattooService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const int PartCount = 6;
    public const int ColorCount = 7;
    public const int PatternCount = 8;
    public const int CombinationCount = PartCount * ColorCount * PatternCount;
    public const float SelfTattooManualCancelDepositRate = 0.10f;
    public const float DefaultStatusApplyChance = 1.0f;
    private const float DefaultHeadCritMultiplier = 1.5f;
    private const float HeadPassiveCritRatePerStrength = 0.005f;
    private const float HeadPassiveElementBonusPerStrength = 0.01f;

    private readonly List<TotemTattooDefinition> equipped = new List<TotemTattooDefinition>(PartCount);
    private readonly List<TotemTattooEffectResult> effectLog = new List<TotemTattooEffectResult>(32);
    private readonly List<TotemActorTattooRuntimeState> actorStates = new List<TotemActorTattooRuntimeState>(64);
    private readonly List<TotemTattooPendingTrigger> pendingTriggers = new List<TotemTattooPendingTrigger>(8);
    private readonly List<TotemTattooMarkState> markStates = new List<TotemTattooMarkState>(64);
    private readonly List<TotemActorModel> shapeTargetBuffer = new List<TotemActorModel>(8);
    private readonly TotemEnemyModel[] enemyShapeTargetBuffer = new TotemEnemyModel[TotemEnemyService.DefaultEnemyCapacity];
    private readonly List<TotemTattooEnchantAffixDefinition> activeEnchantAffixes = new List<TotemTattooEnchantAffixDefinition>(8);
    private TotemTattooDefinition[] runtimeCatalog = Array.Empty<TotemTattooDefinition>();
    private TotemTattooReadingTimeDefinition[] runtimeReadingTimes = Array.Empty<TotemTattooReadingTimeDefinition>();
    private TotemTattooEnchantAffixDefinition[] runtimeEnchantAffixes = Array.Empty<TotemTattooEnchantAffixDefinition>();
    private TotemTattooEnchantRecipeDefinition[] runtimeEnchantRecipes = Array.Empty<TotemTattooEnchantRecipeDefinition>();
    private bool selfTattooInProgress;
    private float selfTattooRemainingSec;
    private int pendingPartId;
    private int pendingColorId;
    private int pendingPatternId;
    private int enchantedCount;
    private TotemTattooEnchantAffixDefinition lastEnchantAffix;
    private TotemTattooEnchantRecipeDefinition lastEnchantRecipe;
    private int pendingTriggerCreatedCount;
    private int pendingTriggerConsumedCount;
    private string lastPendingTriggerSource = string.Empty;
    private string lastPendingTriggerConsumeEvent = string.Empty;
    private string lastPendingTriggerSummary = string.Empty;
    private int selfTattooCancelledCount;
    private string lastSelfTattooCancelReason = string.Empty;
    private int critTriggeredCount;
    private float lastCritBaseDamage;
    private float lastCritDamage;
    private float lastCritChance;
    private float lastCritMultiplier;
    private float lastCritRoll;
    private float lastHeadPassiveCritRateBonus;
    private float lastHeadPassiveElementBonus;
    private string lastCritSourceName = string.Empty;
    private string lastCritTargetName = string.Empty;
    private string lastCritTattooSummary = string.Empty;
    private bool afterDodgeEnchantPending;
    private int afterDodgeEnchantActorId;
    private string afterDodgeEnchantActorName = string.Empty;
    private int afterDodgeEnchantCreatedCount;
    private int afterDodgeEnchantConsumedCount;
    private bool resolvingDamageTriggeredTattoo;

    private TotemGameFlowService flowService;
    private TotemStatusService statusService;
    private TotemActorService actorService;
    private TotemCombatRelationshipService relationshipService;
    private TotemMatchClockService matchClock;
    private TotemEnemyService enemyService;

    public override string ServiceName => "Tattoo";

    public IReadOnlyList<TotemTattooDefinition> Equipped => equipped;

    public IReadOnlyList<TotemTattooEffectResult> EffectLog => effectLog;

    public event Action<TotemActorModel, string, int> SelfTattooCancelled;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        statusService = runtime.GetService<TotemStatusService>();
        actorService = runtime.GetService<TotemActorService>();
        relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        enemyService = runtime.GetService<TotemEnemyService>();
        if (actorService != null)
        {
            actorService.DamageResolved += OnDamageResolved;
        }

        var gameplayCatalog = runtime.GetService<TotemDataService>()?.GameplayCatalog;
        runtimeCatalog = NonEmpty(gameplayCatalog?.CreateTattooDefinitions(), LoadCatalog());
        runtimeReadingTimes = NonEmpty(gameplayCatalog?.CreateTattooReadingTimeDefinitions(), LoadReadingTimes());
        runtimeEnchantAffixes = NonEmpty(gameplayCatalog?.CreateTattooEnchantAffixDefinitions(), LoadEnchantAffixes());
        runtimeEnchantRecipes = NonEmpty(gameplayCatalog?.CreateTattooEnchantRecipeDefinitions(), LoadEnchantRecipes());
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

        statusService = null;
        relationshipService = null;
        matchClock = null;
        enemyService = null;
        if (actorService != null)
        {
            actorService.DamageResolved -= OnDamageResolved;
            actorService = null;
        }

        runtimeCatalog = Array.Empty<TotemTattooDefinition>();
        runtimeReadingTimes = Array.Empty<TotemTattooReadingTimeDefinition>();
        runtimeEnchantAffixes = Array.Empty<TotemTattooEnchantAffixDefinition>();
        runtimeEnchantRecipes = Array.Empty<TotemTattooEnchantRecipeDefinition>();
        Clear();
    }

    public static IReadOnlyList<TotemTattooDefinition> GetCatalog()
    {
        return LoadCatalog();
    }

    public static TotemTattooDefinition[] BuildAllCombinations()
    {
        var catalog = LoadCatalog();
        var copy = new TotemTattooDefinition[catalog.Length];
        Array.Copy(catalog, copy, catalog.Length);
        return copy;
    }

    public static bool TryGetDefinition(int partId, int colorId, int patternId, out TotemTattooDefinition definition)
    {
        definition = null;
        if (partId < 1 || partId > PartCount || colorId < 1 || colorId > ColorCount || patternId < 1 || patternId > PatternCount)
        {
            return false;
        }

        var catalog = LoadCatalog();
        for (int i = 0; i < catalog.Length; i++)
        {
            var item = catalog[i];
            if (item != null && item.PartId == partId && item.ColorId == colorId && item.PatternId == patternId)
            {
                definition = item;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<TotemTattooDefinition> GetRuntimeCatalog()
    {
        return runtimeCatalog;
    }

    public IReadOnlyList<TotemTattooReadingTimeDefinition> GetRuntimeReadingTimes()
    {
        return runtimeReadingTimes;
    }

    public IReadOnlyList<TotemTattooEnchantAffixDefinition> GetRuntimeEnchantAffixes()
    {
        return runtimeEnchantAffixes;
    }

    public IReadOnlyList<TotemTattooEnchantRecipeDefinition> GetRuntimeEnchantRecipes()
    {
        return runtimeEnchantRecipes;
    }

    public bool TryGetRuntimeEnchantRecipe(string colorTier, out TotemTattooEnchantRecipeDefinition selectedRecipe)
    {
        string resolvedTier = string.IsNullOrWhiteSpace(colorTier) ? "Common" : colorTier;
        return TryGetEnchantRecipe(resolvedTier, out selectedRecipe);
    }

    public static string ResolveColorTier(int colorId)
    {
        if (colorId <= 0)
        {
            return "Common";
        }

        if (colorId <= 4)
        {
            return "Common";
        }

        if (colorId <= 6)
        {
            return "Rare";
        }

        return "Legendary";
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        TickPlayerSelfTattoo(deltaTime);
        for (int i = 0; i < actorStates.Count; i++)
        {
            TickActorSelfTattoo(actorStates[i], deltaTime);
        }
    }

    private void TickPlayerSelfTattoo(float deltaTime)
    {
        if (!selfTattooInProgress)
        {
            return;
        }

        selfTattooRemainingSec = Mathf.Max(0f, selfTattooRemainingSec - deltaTime);
        if (selfTattooRemainingSec > 0f)
        {
            return;
        }

        bool equippedResult = Equip(pendingPartId, pendingColorId, pendingPatternId);
        GFTrace.Success("TotemTattoo", "SelfTattoo.Finished", null, GFTrace.Data(
            "partId", pendingPartId.ToString(),
            "colorId", pendingColorId.ToString(),
            "patternId", pendingPatternId.ToString(),
            "equipped", equippedResult.ToString()));
        ClearSelfTattooPending();
    }

    private void TickActorSelfTattoo(TotemActorTattooRuntimeState state, float deltaTime)
    {
        if (state == null || !state.SelfTattooInProgress)
        {
            return;
        }

        state.SelfTattooRemainingSec = Mathf.Max(0f, state.SelfTattooRemainingSec - deltaTime);
        if (state.SelfTattooRemainingSec > 0f)
        {
            return;
        }

        bool equippedResult = Equip(state, state.PendingPartId, state.PendingColorId, state.PendingPatternId);
        GFTrace.Success("TotemTattoo", "ActorSelfTattoo.Finished", null, GFTrace.Data(
            "actor", state.ActorName ?? string.Empty,
            "actorId", state.ActorId.ToString(),
            "partId", state.PendingPartId.ToString(),
            "colorId", state.PendingColorId.ToString(),
            "patternId", state.PendingPatternId.ToString(),
            "equipped", equippedResult.ToString()));
        ClearSelfTattooPending(state);
    }

    public bool TryGetRuntimeDefinition(int partId, int colorId, int patternId, out TotemTattooDefinition definition)
    {
        definition = null;
        var catalog = runtimeCatalog == null || runtimeCatalog.Length <= 0 ? LoadCatalog() : runtimeCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            var item = catalog[i];
            if (item != null && item.PartId == partId && item.ColorId == colorId && item.PatternId == patternId)
            {
                definition = item;
                return true;
            }
        }

        return false;
    }

    public bool Equip(int partId, int colorId, int patternId)
    {
        if (!TryGetRuntimeDefinition(partId, colorId, patternId, out var definition))
        {
            GFTrace.Warning("TotemTattoo", "Equip.Rejected", null, GFTrace.Data(
                "partId", partId.ToString(),
                "colorId", colorId.ToString(),
                "patternId", patternId.ToString()));
            return false;
        }

        EquipDefinition(equipped, definition);
        GFTrace.Info("TotemTattoo", "Equip.Player", null, GFTrace.Data(
            "part", definition.PartName,
            "color", definition.ColorName,
            "pattern", definition.PatternName));
        return true;
    }

    public bool Equip(TotemActorModel actor, int partId, int colorId, int patternId)
    {
        if (actor == null)
        {
            return Equip(partId, colorId, patternId);
        }

        var state = GetOrCreateActorState(actor);
        return Equip(state, partId, colorId, patternId);
    }

    private bool Equip(TotemActorTattooRuntimeState state, int partId, int colorId, int patternId)
    {
        if (state == null || !TryGetRuntimeDefinition(partId, colorId, patternId, out var definition))
        {
            GFTrace.Warning("TotemTattoo", "Equip.Actor.Rejected", null, GFTrace.Data(
                "actor", state?.ActorName ?? string.Empty,
                "actorId", (state?.ActorId ?? 0).ToString(),
                "partId", partId.ToString(),
                "colorId", colorId.ToString(),
                "patternId", patternId.ToString()));
            return false;
        }

        EquipDefinition(state.Equipped, definition);
        GFTrace.Info("TotemTattoo", "Equip.Actor", null, GFTrace.Data(
            "actor", state.ActorName ?? string.Empty,
            "actorId", state.ActorId.ToString(),
            "part", definition.PartName,
            "color", definition.ColorName,
            "pattern", definition.PatternName));
        return true;
    }

    public bool StartSelfTattoo(int partId, int colorId, int patternId)
    {
        if (selfTattooInProgress || !TryGetRuntimeDefinition(partId, colorId, patternId, out _))
        {
            GFTrace.Warning("TotemTattoo", "SelfTattoo.Rejected", null, GFTrace.Data(
                "partId", partId.ToString(),
                "colorId", colorId.ToString(),
                "patternId", patternId.ToString(),
                "inProgress", selfTattooInProgress.ToString()));
            return false;
        }

        pendingPartId = partId;
        pendingColorId = colorId;
        pendingPatternId = patternId;
        selfTattooRemainingSec = GetRuntimeSelfTattooDuration(partId);
        selfTattooInProgress = true;
        GFTrace.Success("TotemTattoo", "SelfTattoo.Started", null, GFTrace.Data(
            "partId", partId.ToString(),
            "colorId", colorId.ToString(),
            "patternId", patternId.ToString(),
            "duration", selfTattooRemainingSec.ToString("F1")));
        return true;
    }

    public bool StartSelfTattoo(TotemActorModel actor, int partId, int colorId, int patternId)
    {
        if (actor == null || !TryGetRuntimeDefinition(partId, colorId, patternId, out _))
        {
            GFTrace.Warning("TotemTattoo", "ActorSelfTattoo.Rejected", null, GFTrace.Data(
                "actor", actor?.Name ?? string.Empty,
                "actorId", (actor?.ActorId ?? 0).ToString(),
                "partId", partId.ToString(),
                "colorId", colorId.ToString(),
                "patternId", patternId.ToString(),
                "reason", actor == null ? "MissingActor" : "InvalidCombination"));
            return false;
        }

        var state = GetOrCreateActorState(actor);
        if (state.SelfTattooInProgress)
        {
            GFTrace.Warning("TotemTattoo", "ActorSelfTattoo.Rejected", null, GFTrace.Data(
                "actor", state.ActorName ?? string.Empty,
                "actorId", state.ActorId.ToString(),
                "partId", partId.ToString(),
                "colorId", colorId.ToString(),
                "patternId", patternId.ToString(),
                "reason", "AlreadyInProgress"));
            return false;
        }

        state.PendingPartId = partId;
        state.PendingColorId = colorId;
        state.PendingPatternId = patternId;
        state.SelfTattooRemainingSec = GetRuntimeSelfTattooDuration(partId);
        state.SelfTattooInProgress = true;
        GFTrace.Success("TotemTattoo", "ActorSelfTattoo.Started", null, GFTrace.Data(
            "actor", state.ActorName ?? string.Empty,
            "actorId", state.ActorId.ToString(),
            "partId", partId.ToString(),
            "colorId", colorId.ToString(),
            "patternId", patternId.ToString(),
            "duration", state.SelfTattooRemainingSec.ToString("F1")));
        return true;
    }

    public bool CancelSelfTattoo()
    {
        return CancelPlayerSelfTattoo(actorService?.Player, "Manual");
    }

    private bool CancelSelfTattoo(string reason)
    {
        return CancelPlayerSelfTattoo(actorService?.Player, reason);
    }

    private bool CancelPlayerSelfTattoo(TotemActorModel actor, string reason)
    {
        if (!selfTattooInProgress)
        {
            return false;
        }

        RecordSelfTattooCancelled(null, reason);
        string resolvedReason = SanitizeCancelReason(reason);
        GFTrace.Info("TotemTattoo", "SelfTattoo.Cancelled", null, GFTrace.Data(
            "pending", BuildPendingSelfTattooSummary(),
            "reason", resolvedReason));
        int manualCancelDeposit = CalculateManualCancelDeposit(pendingColorId, resolvedReason);
        SelfTattooCancelled?.Invoke(actor, resolvedReason, manualCancelDeposit);
        ClearSelfTattooPending();
        return true;
    }

    public bool CancelSelfTattoo(TotemActorModel actor)
    {
        return CancelSelfTattoo(actor, "Manual");
    }

    private bool CancelSelfTattoo(TotemActorModel actor, string reason)
    {
        if (actor == null || !TryGetActorState(actor, out var state) || !state.SelfTattooInProgress)
        {
            return false;
        }

        RecordSelfTattooCancelled(state, reason);
        string resolvedReason = SanitizeCancelReason(reason);
        GFTrace.Info("TotemTattoo", "ActorSelfTattoo.Cancelled", null, GFTrace.Data(
            "actor", state.ActorName ?? string.Empty,
            "actorId", state.ActorId.ToString(),
            "pending", BuildPendingSelfTattooSummary(state),
            "reason", resolvedReason));
        int manualCancelDeposit = CalculateManualCancelDeposit(state.PendingColorId, resolvedReason);
        SelfTattooCancelled?.Invoke(actor, resolvedReason, manualCancelDeposit);
        ClearSelfTattooPending(state);
        return true;
    }

    public bool IsSelfTattooInProgress(TotemActorModel actor)
    {
        return actor != null && TryGetActorState(actor, out var state) && state.SelfTattooInProgress;
    }

    public bool ApplyMinorEnchant()
    {
        return ApplyEnchant("Common");
    }

    public bool ApplyEnchant(string colorTier)
    {
        if (equipped.Count <= 0)
        {
            return false;
        }

        string resolvedTier = string.IsNullOrWhiteSpace(colorTier) ? "Common" : colorTier;
        var target = equipped[0];
        if (!TrySelectEnchantAffix(target, resolvedTier, enchantedCount, out var selectedAffix) ||
            !TryGetEnchantRecipe(resolvedTier, out var selectedRecipe))
        {
            GFTrace.Warning("TotemTattoo", "Enchant.Rejected", null, GFTrace.Data(
                "tier", resolvedTier,
                "target", $"{target.PartName}/{target.ColorName}/{target.PatternName}"));
            return false;
        }

        enchantedCount++;
        activeEnchantAffixes.Add(selectedAffix);
        lastEnchantAffix = selectedAffix;
        lastEnchantRecipe = selectedRecipe;
        GFTrace.Success("TotemTattoo", "Enchant.Applied", null, GFTrace.Data(
            "target", $"{target.PartName}/{target.ColorName}/{target.PatternName}",
            "tier", selectedAffix.ColorTier,
            "affixId", selectedAffix.Id.ToString(),
            "affixType", selectedAffix.AffixType.ToString(),
            "statKey", selectedAffix.StatKey,
            "value", selectedAffix.Value.ToString("F2"),
            "coinCost", selectedRecipe.CoinCost.ToString(),
            "rarePigmentCost", selectedRecipe.RarePigmentCost.ToString(),
            "enchantedCount", enchantedCount.ToString()));
        return true;
    }

    public void ApplyStartupSelection(TotemStartupSelection selection)
    {
        Clear();
        int colorId = selection == null || selection.ColorId <= 0 ? 1 : selection.ColorId;
        int[] patternIds = selection?.PatternIds;
        if (patternIds == null || patternIds.Length <= 0)
        {
            patternIds = new[] { 1 };
        }

        int[] startupParts = { 4, 1, 3, 5, 2, 6 };
        for (int i = 0; i < patternIds.Length && i < startupParts.Length; i++)
        {
            Equip(startupParts[i], colorId, patternIds[i]);
        }

        GFTrace.Success("TotemTattoo", "StartupSelection.Applied", null, GFTrace.Data(
            "colorId", colorId.ToString(),
            "patterns", string.Join(",", patternIds)));
    }

    public void Clear()
    {
        equipped.Clear();
        effectLog.Clear();
        pendingTriggers.Clear();
        actorStates.Clear();
        markStates.Clear();
        shapeTargetBuffer.Clear();
        activeEnchantAffixes.Clear();
        enchantedCount = 0;
        lastEnchantAffix = null;
        lastEnchantRecipe = null;
        pendingTriggerCreatedCount = 0;
        pendingTriggerConsumedCount = 0;
        lastPendingTriggerSource = string.Empty;
        lastPendingTriggerConsumeEvent = string.Empty;
        lastPendingTriggerSummary = string.Empty;
        selfTattooCancelledCount = 0;
        lastSelfTattooCancelReason = string.Empty;
        critTriggeredCount = 0;
        lastCritBaseDamage = 0f;
        lastCritDamage = 0f;
        lastCritChance = 0f;
        lastCritMultiplier = 0f;
        lastCritRoll = 0f;
        lastHeadPassiveCritRateBonus = 0f;
        lastHeadPassiveElementBonus = 0f;
        lastCritSourceName = string.Empty;
        lastCritTargetName = string.Empty;
        lastCritTattooSummary = string.Empty;
        afterDodgeEnchantPending = false;
        afterDodgeEnchantActorId = 0;
        afterDodgeEnchantActorName = string.Empty;
        afterDodgeEnchantCreatedCount = 0;
        afterDodgeEnchantConsumedCount = 0;
        ClearSelfTattooPending();
    }

    public TotemTattooEffectResult[] Trigger(string triggerEvent, TotemActorModel source, TotemActorModel target, float baseMagnitude)
    {
        HandleSelfTattooInterruption(triggerEvent, source, baseMagnitude);
        HandleAfterDodgeEnchant(triggerEvent, source);
        if (target == null && string.Equals(triggerEvent, "MoveTickEvent", StringComparison.Ordinal))
        {
            target = ResolveMoveTickTarget(source, baseMagnitude);
        }

        var sourceEquipped = ResolveSourceEquipped(source, out var sourceEffectLog, out var sourcePendingTriggers, out var sourceState);
        if (string.IsNullOrWhiteSpace(triggerEvent) ||
            ((sourceEquipped == null || sourceEquipped.Count <= 0) && (sourcePendingTriggers == null || sourcePendingTriggers.Count <= 0)))
        {
            return Array.Empty<TotemTattooEffectResult>();
        }

        var results = new List<TotemTattooEffectResult>((sourceEquipped?.Count ?? 0) + (sourcePendingTriggers?.Count ?? 0));
        for (int i = 0; sourceEquipped != null && i < sourceEquipped.Count; i++)
        {
            var definition = sourceEquipped[i];
            if (!string.Equals(definition.TriggerEvent, triggerEvent, StringComparison.Ordinal))
            {
                continue;
            }

            var result = ShouldCreatePendingTrigger(definition)
                ? CreatePendingTriggerResult(definition, source, baseMagnitude, sourcePendingTriggers, sourceState)
                : ApplyDefinition(definition, source, target, baseMagnitude);
            if (result != null)
            {
                results.Add(result);
                sourceEffectLog?.Add(result);
            }
        }

        ConsumePendingTriggers(triggerEvent, source, target, sourcePendingTriggers, sourceEffectLog, results, sourceState);
        return results.Count == 0 ? Array.Empty<TotemTattooEffectResult>() : results.ToArray();
    }

    public TotemTattooEffectResult[] TriggerEnemy(
        string triggerEvent,
        TotemActorModel source,
        TotemEnemyModel target,
        float baseMagnitude)
    {
        HandleSelfTattooInterruption(triggerEvent, source, baseMagnitude);
        HandleAfterDodgeEnchant(triggerEvent, source);
        var sourceEquipped = ResolveSourceEquipped(source, out var sourceEffectLog, out var sourcePendingTriggers, out var sourceState);
        if (target == null
            || enemyService == null
            || string.IsNullOrWhiteSpace(triggerEvent)
            || ((sourceEquipped == null || sourceEquipped.Count <= 0) && (sourcePendingTriggers == null || sourcePendingTriggers.Count <= 0)))
        {
            return Array.Empty<TotemTattooEffectResult>();
        }

        var results = new List<TotemTattooEffectResult>((sourceEquipped?.Count ?? 0) + (sourcePendingTriggers?.Count ?? 0));
        for (int i = 0; sourceEquipped != null && i < sourceEquipped.Count; i++)
        {
            TotemTattooDefinition definition = sourceEquipped[i];
            if (!string.Equals(definition.TriggerEvent, triggerEvent, StringComparison.Ordinal))
            {
                continue;
            }

            TotemTattooEffectResult result = ShouldCreatePendingTrigger(definition)
                ? CreatePendingTriggerResult(definition, source, baseMagnitude, sourcePendingTriggers, sourceState)
                : ApplyEnemyDefinition(definition, source, target, baseMagnitude);
            if (result != null)
            {
                results.Add(result);
                sourceEffectLog?.Add(result);
            }
        }

        ConsumeEnemyPendingTriggers(triggerEvent, source, target, sourcePendingTriggers, sourceEffectLog, results, sourceState);
        return results.Count == 0 ? Array.Empty<TotemTattooEffectResult>() : results.ToArray();
    }

    public float ResolveAttackDamage(TotemActorModel source, TotemActorModel target, float baseDamage)
    {
        return ResolveAttackDamage(source, target, baseDamage, out _);
    }

    public float ResolveAttackDamage(TotemActorModel source, TotemActorModel target, float baseDamage, out TotemTattooEffectResult criticalResult)
    {
        criticalResult = null;
        baseDamage = Mathf.Max(0f, baseDamage);
        if (baseDamage <= 0f)
        {
            return 0f;
        }

        var sourceEquipped = ResolveSourceEquipped(source, out var sourceEffectLog, out _, out var sourceState);
        if (!TryGetHeadDefinition(sourceEquipped, out var headDefinition))
        {
            return baseDamage;
        }

        float strength = Mathf.Max(0f, headDefinition.ScaleFactor > 0f ? headDefinition.ScaleFactor : headDefinition.Magnitude);
        float passiveCritRateBonus = ComputeHeadCritRateBonus(strength);
        if (ShouldUseGlobalEnchantAffixes(source))
        {
            passiveCritRateBonus += SumEnchantAffixValue(TotemTattooEnchantAffixType.CritChance);
        }

        float passiveElementBonus = ComputeHeadElementBonus(strength);
        float critChance = ComputeHeadCritChance(headDefinition.PatternMultiplier, passiveCritRateBonus);
        float critRoll = UnityEngine.Random.value;
        float critMultiplier = ResolveHeadCritMultiplier(DefaultHeadCritMultiplier + (ShouldUseGlobalEnchantAffixes(source) ? SumEnchantAffixValue(TotemTattooEnchantAffixType.CritDamage) : 0f));
        if (!ShouldHeadCrit(critChance, critRoll))
        {
            return baseDamage;
        }

        float finalDamage = baseDamage * critMultiplier;
        criticalResult = new TotemTattooEffectResult
        {
            Definition = headDefinition,
            Source = source,
            Target = target,
            IsCritical = true,
            BaseDamage = baseDamage,
            Damage = finalDamage,
            HitCount = target != null && target.IsAlive ? 1 : 0,
            StatusName = "Critical",
            Note = "HeadCritical",
            CritChance = critChance,
            CritMultiplier = critMultiplier,
            CritRoll = critRoll,
            PassiveCritRateBonus = passiveCritRateBonus,
            PassiveElementBonus = passiveElementBonus,
        };

        sourceEffectLog?.Add(criticalResult);
        RecordHeadCritical(sourceState, criticalResult);
        GFTrace.Info("TotemTattoo", "HeadCrit.Resolved", null, GFTrace.Data(
            "source", source?.Name ?? string.Empty,
            "target", target?.Name ?? string.Empty,
            "baseDamage", baseDamage.ToString("F1"),
            "finalDamage", finalDamage.ToString("F1"),
            "chance", critChance.ToString("F3"),
            "roll", critRoll.ToString("F3")));
        return finalDamage;
    }

    public static float ComputeHeadCritRateBonus(float strength)
    {
        return Mathf.Max(0f, strength) * HeadPassiveCritRatePerStrength;
    }

    public static float ComputeHeadElementBonus(float strength)
    {
        return Mathf.Max(0f, strength) * HeadPassiveElementBonusPerStrength;
    }

    public static float ComputeHeadCritChance(float patternMultiplier, float critRateBonus)
    {
        return Mathf.Clamp01(Mathf.Max(0f, patternMultiplier) * (1f + Mathf.Max(0f, critRateBonus)));
    }

    public static float ResolveHeadCritMultiplier(float configuredMultiplier)
    {
        return configuredMultiplier > 0f ? configuredMultiplier : DefaultHeadCritMultiplier;
    }

    public static bool ShouldHeadCrit(float critChance, float roll01)
    {
        critChance = Mathf.Clamp01(critChance);
        if (critChance >= 1f)
        {
            return true;
        }

        return critChance > 0f && Mathf.Clamp01(roll01) < critChance;
    }

    public static float ComputeStatusApplyChance(float baseChance, float statusChanceBonus)
    {
        return Mathf.Clamp01(Mathf.Max(0f, baseChance) + Mathf.Max(0f, statusChanceBonus));
    }

    public static bool ShouldApplyStatus(float statusChance, float roll01)
    {
        statusChance = Mathf.Clamp01(statusChance);
        if (statusChance >= 1f)
        {
            return true;
        }

        return statusChance > 0f && Mathf.Clamp01(roll01) < statusChance;
    }

    public float ResolveWeaponCooldownMultiplier(TotemActorModel source)
    {
        if (source == null || !ShouldUseGlobalEnchantAffixes(source))
        {
            return 1f;
        }

        return ComputeWeaponCooldownMultiplier(
            SumEnchantAffixValue(TotemTattooEnchantAffixType.AttackSpeed),
            SumEnchantAffixValue(TotemTattooEnchantAffixType.CooldownReduction));
    }

    public float ResolveSkillCooldownMultiplier(TotemActorModel source)
    {
        if (source == null || !ShouldUseGlobalEnchantAffixes(source))
        {
            return 1f;
        }

        return ComputeSkillCooldownMultiplier(SumEnchantAffixValue(TotemTattooEnchantAffixType.CooldownReduction));
    }

    public float ResolveRangeMultiplier(TotemActorModel source)
    {
        if (source == null || !ShouldUseGlobalEnchantAffixes(source))
        {
            return 1f;
        }

        return ComputeRangeMultiplier(SumEnchantAffixValue(TotemTattooEnchantAffixType.RangeBonus));
    }

    public float ResolveStatusChanceBonus(TotemActorModel source)
    {
        if (source == null || !ShouldUseGlobalEnchantAffixes(source))
        {
            return 0f;
        }

        return SumEnchantAffixValue(TotemTattooEnchantAffixType.StatusChance);
    }

    public static float ComputeWeaponCooldownMultiplier(float attackSpeedBonus, float cooldownReduction)
    {
        float speedDivisor = 1f + Mathf.Max(0f, attackSpeedBonus);
        float reductionMul = 1f - Mathf.Clamp01(cooldownReduction);
        return Mathf.Clamp(reductionMul / Mathf.Max(0.01f, speedDivisor), 0.1f, 1f);
    }

    public static float ComputeSkillCooldownMultiplier(float cooldownReduction)
    {
        return Mathf.Clamp(1f - Mathf.Clamp01(cooldownReduction), 0.1f, 1f);
    }

    public static float ComputeRangeMultiplier(float rangeBonus)
    {
        return 1f + Mathf.Max(0f, rangeBonus);
    }

    public TotemTattooSnapshot CaptureSnapshot()
    {
        return new TotemTattooSnapshot
        {
            catalogCombinationCount = CountOrFallback(runtimeCatalog, LoadCatalog()),
            readingTimeCount = CountOrFallback(runtimeReadingTimes, LoadReadingTimes()),
            enchantAffixCount = CountOrFallback(runtimeEnchantAffixes, LoadEnchantAffixes()),
            enchantRecipeCount = CountOrFallback(runtimeEnchantRecipes, LoadEnchantRecipes()),
            equippedCount = equipped.Count,
            appliedEffectCount = effectLog.Count,
            equippedSummary = BuildEquippedSummary(equipped),
            selfTattooInProgress = selfTattooInProgress,
            selfTattooRemainingSec = selfTattooRemainingSec,
            pendingSelfTattooSummary = BuildPendingSelfTattooSummary(),
            selfTattooCancelledCount = selfTattooCancelledCount,
            lastSelfTattooCancelReason = lastSelfTattooCancelReason,
            enchantedCount = enchantedCount,
            lastEnchantAffixId = lastEnchantAffix?.Id ?? 0,
            lastEnchantAffixType = lastEnchantAffix?.AffixType.ToString() ?? string.Empty,
            lastEnchantColorTier = lastEnchantAffix?.ColorTier ?? string.Empty,
            lastEnchantStatKey = lastEnchantAffix?.StatKey ?? string.Empty,
            lastEnchantValue = lastEnchantAffix?.Value ?? 0f,
            lastEnchantDisplayText = lastEnchantAffix?.DisplayText ?? string.Empty,
            lastEnchantCoinCost = lastEnchantRecipe?.CoinCost ?? 0,
            lastEnchantRarePigmentCost = lastEnchantRecipe?.RarePigmentCost ?? 0,
            activeEnchantAffixCount = activeEnchantAffixes.Count,
            activeEnchantSummary = BuildEnchantSummary(activeEnchantAffixes),
            activeElementDamageBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.ElementDamageBonus),
            activeSelfHealOnHit = SumEnchantAffixValue(TotemTattooEnchantAffixType.SelfHealOnHit),
            activeCritChanceBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.CritChance),
            activeCritDamageBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.CritDamage),
            activeAttackSpeedBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.AttackSpeed),
            activeCooldownReduction = SumEnchantAffixValue(TotemTattooEnchantAffixType.CooldownReduction),
            activeStatusChanceBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.StatusChance),
            activeRangeBonus = SumEnchantAffixValue(TotemTattooEnchantAffixType.RangeBonus),
            afterDodgeEnchantPending = afterDodgeEnchantPending,
            afterDodgeEnchantCreatedCount = afterDodgeEnchantCreatedCount,
            afterDodgeEnchantConsumedCount = afterDodgeEnchantConsumedCount,
            lastAfterDodgeEnchantActorId = afterDodgeEnchantActorId,
            lastAfterDodgeEnchantActorName = afterDodgeEnchantActorName,
            actorStateCount = actorStates.Count,
            actorEquippedCount = CountActorEquipped(),
            actorSelfTattooInProgressCount = CountActorSelfTattooInProgress(),
            actorAppliedEffectCount = CountActorAppliedEffects(),
            actorSelfTattooCancelledCount = CountActorSelfTattooCancelled(),
            pendingTriggerCount = pendingTriggers.Count,
            pendingTriggerCreatedCount = pendingTriggerCreatedCount,
            pendingTriggerConsumedCount = pendingTriggerConsumedCount,
            lastPendingTriggerSource = lastPendingTriggerSource,
            lastPendingTriggerConsumeEvent = lastPendingTriggerConsumeEvent,
            lastPendingTriggerSummary = lastPendingTriggerSummary,
            actorPendingTriggerCount = CountActorPendingTriggers(),
            critTriggeredCount = critTriggeredCount,
            actorCritTriggeredCount = CountActorCritTriggers(),
            lastCritBaseDamage = lastCritBaseDamage,
            lastCritDamage = lastCritDamage,
            lastCritChance = lastCritChance,
            lastCritMultiplier = lastCritMultiplier,
            lastCritRoll = lastCritRoll,
            lastHeadPassiveCritRateBonus = lastHeadPassiveCritRateBonus,
            lastHeadPassiveElementBonus = lastHeadPassiveElementBonus,
            lastCritSourceName = lastCritSourceName,
            lastCritTargetName = lastCritTargetName,
            lastCritTattooSummary = lastCritTattooSummary,
        };
    }

    public TotemTattooSnapshot CaptureSnapshot(TotemActorModel actor)
    {
        if (actor == null || !TryGetActorState(actor, out var state))
        {
            return new TotemTattooSnapshot
            {
                catalogCombinationCount = CountOrFallback(runtimeCatalog, LoadCatalog()),
                readingTimeCount = CountOrFallback(runtimeReadingTimes, LoadReadingTimes()),
                enchantAffixCount = CountOrFallback(runtimeEnchantAffixes, LoadEnchantAffixes()),
                enchantRecipeCount = CountOrFallback(runtimeEnchantRecipes, LoadEnchantRecipes()),
            };
        }

        return new TotemTattooSnapshot
        {
            catalogCombinationCount = CountOrFallback(runtimeCatalog, LoadCatalog()),
            readingTimeCount = CountOrFallback(runtimeReadingTimes, LoadReadingTimes()),
            enchantAffixCount = CountOrFallback(runtimeEnchantAffixes, LoadEnchantAffixes()),
            enchantRecipeCount = CountOrFallback(runtimeEnchantRecipes, LoadEnchantRecipes()),
            equippedCount = state.Equipped.Count,
            appliedEffectCount = state.EffectLog.Count,
            equippedSummary = BuildEquippedSummary(state.Equipped),
            selfTattooInProgress = state.SelfTattooInProgress,
            selfTattooRemainingSec = state.SelfTattooRemainingSec,
            pendingSelfTattooSummary = BuildPendingSelfTattooSummary(state),
            selfTattooCancelledCount = state.SelfTattooCancelledCount,
            lastSelfTattooCancelReason = state.LastSelfTattooCancelReason,
            enchantedCount = state.EnchantedCount,
            actorStateCount = 1,
            actorEquippedCount = state.Equipped.Count,
            actorSelfTattooInProgressCount = state.SelfTattooInProgress ? 1 : 0,
            actorAppliedEffectCount = state.EffectLog.Count,
            actorSelfTattooCancelledCount = state.SelfTattooCancelledCount,
            pendingTriggerCount = state.PendingTriggers.Count,
            pendingTriggerCreatedCount = state.PendingTriggerCreatedCount,
            pendingTriggerConsumedCount = state.PendingTriggerConsumedCount,
            lastPendingTriggerSource = state.LastPendingTriggerSource,
            lastPendingTriggerConsumeEvent = state.LastPendingTriggerConsumeEvent,
            lastPendingTriggerSummary = state.LastPendingTriggerSummary,
            actorPendingTriggerCount = state.PendingTriggers.Count,
            critTriggeredCount = state.CritTriggeredCount,
            actorCritTriggeredCount = state.CritTriggeredCount,
            lastCritBaseDamage = state.LastCritBaseDamage,
            lastCritDamage = state.LastCritDamage,
            lastCritChance = state.LastCritChance,
            lastCritMultiplier = state.LastCritMultiplier,
            lastCritRoll = state.LastCritRoll,
            lastHeadPassiveCritRateBonus = state.LastHeadPassiveCritRateBonus,
            lastHeadPassiveElementBonus = state.LastHeadPassiveElementBonus,
            lastCritSourceName = state.LastCritSourceName,
            lastCritTargetName = state.LastCritTargetName,
            lastCritTattooSummary = state.LastCritTattooSummary,
        };
    }

    public static float GetSelfTattooDuration(int partId)
    {
        return ResolveSelfTattooDuration(LoadReadingTimes(), partId);
    }

    public float GetRuntimeSelfTattooDuration(int partId)
    {
        return ResolveSelfTattooDuration(runtimeReadingTimes, partId);
    }

    private static float ResolveSelfTattooDuration(TotemTattooReadingTimeDefinition[] source, int partId)
    {
        var rows = source == null || source.Length <= 0 ? LoadReadingTimes() : source;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (row != null && row.PartId == partId && row.DurationSec > 0f)
            {
                return row.DurationSec;
            }
        }

        return 2.2f;
    }

    private bool TrySelectEnchantAffix(TotemTattooDefinition target, string colorTier, int sequence, out TotemTattooEnchantAffixDefinition selectedAffix)
    {
        selectedAffix = null;
        var rows = runtimeEnchantAffixes == null || runtimeEnchantAffixes.Length <= 0 ? LoadEnchantAffixes() : runtimeEnchantAffixes;
        int matchCount = CountMatchingAffixes(rows, target, colorTier);
        if (matchCount <= 0)
        {
            return false;
        }

        int targetIndex = Mathf.Abs(sequence) % matchCount;
        int cursor = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (!MatchesEnchantAffix(row, target, colorTier))
            {
                continue;
            }

            if (cursor == targetIndex)
            {
                selectedAffix = row;
                return true;
            }

            cursor++;
        }

        return false;
    }

    private static int CountMatchingAffixes(TotemTattooEnchantAffixDefinition[] rows, TotemTattooDefinition target, string colorTier)
    {
        int count = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (MatchesEnchantAffix(rows[i], target, colorTier))
            {
                count++;
            }
        }

        return count;
    }

    private static bool MatchesEnchantAffix(TotemTattooEnchantAffixDefinition row, TotemTattooDefinition target, string colorTier)
    {
        return row != null &&
               target != null &&
               row.Weight > 0f &&
               row.AffixType != TotemTattooEnchantAffixType.Unknown &&
               (row.PartId == 0 || row.PartId == target.PartId) &&
               string.Equals(row.ColorTier, colorTier, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetEnchantRecipe(string colorTier, out TotemTattooEnchantRecipeDefinition selectedRecipe)
    {
        selectedRecipe = null;
        var rows = runtimeEnchantRecipes == null || runtimeEnchantRecipes.Length <= 0 ? LoadEnchantRecipes() : runtimeEnchantRecipes;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (row != null && string.Equals(row.ColorTier, colorTier, StringComparison.OrdinalIgnoreCase))
            {
                selectedRecipe = row;
                return true;
            }
        }

        return false;
    }

    private int CalculateManualCancelDeposit(int colorId, string reason)
    {
        if (!string.Equals(reason, "Manual", StringComparison.Ordinal))
        {
            return 0;
        }

        if (!TryGetEnchantRecipe(ResolveColorTier(colorId), out var recipe))
        {
            return 0;
        }

        return Mathf.CeilToInt(Mathf.Max(0, recipe.CoinCost) * SelfTattooManualCancelDepositRate);
    }

    private TotemTattooEffectResult ApplyDefinition(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float baseMagnitude,
        bool magnitudeAlreadyResolved = false,
        string note = "Applied",
        string reasonOverride = null)
    {
        float magnitude = magnitudeAlreadyResolved ? Mathf.Max(0f, baseMagnitude) : ResolveDefinitionMagnitude(definition, baseMagnitude);
        if (!magnitudeAlreadyResolved && ShouldUseGlobalEnchantAffixes(source))
        {
            magnitude *= 1f + SumEnchantAffixValue(TotemTattooEnchantAffixType.ElementDamageBonus, source, target, evaluateConditions: true, consumeAfterDodge: true);
        }

        string damageReason = string.IsNullOrWhiteSpace(reasonOverride) ? $"Tattoo:{definition.TriggerEvent}" : reasonOverride;
        var result = ApplyShapeDefinition(definition, source, target, magnitude, damageReason);
        if (string.IsNullOrWhiteSpace(result.Note))
        {
            result.Note = result.HitCount == 0 && !result.BurstTriggered ? "NoTarget" : note;
        }

        if (!string.IsNullOrWhiteSpace(result.StatusName) && result.Note == note)
        {
            result.Note = $"{note}/{result.StatusName}";
        }

        return result;
    }

    private static float ResolveDefinitionMagnitude(TotemTattooDefinition definition, float baseMagnitude)
    {
        return Mathf.Max(0f, baseMagnitude) * (definition?.Magnitude ?? 0f);
    }

    private static float ResolvePendingMagnitude(TotemTattooDefinition definition, float baseMagnitude)
    {
        return ResolveDefinitionMagnitude(definition, baseMagnitude);
    }

    private TotemTattooEffectResult ApplyShapeDefinition(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason)
    {
        var result = new TotemTattooEffectResult
        {
            Definition = definition,
            Source = source,
            Target = target,
            BaseDamage = magnitude,
            StatusName = GetStatusName(definition.Element),
            StackThreshold = 0,
        };

        if (definition == null || target == null || !target.IsAlive)
        {
            return result;
        }

        switch (definition.Shape)
        {
            case TotemTattooShape.AOEBurst:
                ApplyAreaBurst(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.MultiHit:
                ApplyMultiHit(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.ChainJump:
                ApplyChainJump(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.StackingMark:
                ApplyStackingMark(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.ProbBurst:
                ApplyProbabilityBurst(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.TrailZone:
                ApplyTrailZone(definition, source, target, magnitude, damageReason, result);
                break;
            case TotemTattooShape.SummonForm:
                ApplySingleDamage(definition, source, target, magnitude * Mathf.Max(1f, definition.ShapeParam1), damageReason, result);
                result.StatusName = $"summon/{result.StatusName}";
                break;
            default:
                ApplySingleDamage(definition, source, target, magnitude, damageReason, result);
                break;
        }

        result.SourceHeal = ApplySourceElementEffect(definition, source, result.Damage);
        return result;
    }

    private TotemTattooEffectResult ApplyEnemyDefinition(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemEnemyModel target,
        float baseMagnitude,
        bool magnitudeAlreadyResolved = false,
        string note = "Applied",
        string reasonOverride = null)
    {
        if (definition == null)
        {
            return null;
        }

        float magnitude = magnitudeAlreadyResolved ? Mathf.Max(0f, baseMagnitude) : ResolveDefinitionMagnitude(definition, baseMagnitude);
        if (!magnitudeAlreadyResolved && ShouldUseGlobalEnchantAffixes(source))
        {
            magnitude *= 1f + SumEnchantAffixValue(
                TotemTattooEnchantAffixType.ElementDamageBonus,
                source,
                null,
                evaluateConditions: true,
                consumeAfterDodge: true);
        }

        string damageReason = string.IsNullOrWhiteSpace(reasonOverride) ? "Tattoo:" + definition.TriggerEvent : reasonOverride;
        var result = new TotemTattooEffectResult
        {
            Definition = definition,
            Source = source,
            BaseDamage = magnitude,
            StatusName = GetStatusName(definition.Element),
            Note = note,
        };
        if (target == null || !target.IsAlive || enemyService == null)
        {
            result.Note = "NoTarget";
            return result;
        }

        switch (definition.Shape)
        {
            case TotemTattooShape.AOEBurst:
                ApplyEnemyArea(definition, source, target, magnitude, damageReason, result, chain: false);
                break;
            case TotemTattooShape.MultiHit:
                int segments = Mathf.Max(1, definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 4);
                float perHit = magnitude / segments;
                for (int i = 0; i < segments && target.IsAlive; i++)
                {
                    ApplyEnemySingleDamage(definition, source, target, perHit, damageReason, result);
                }
                result.StatusName = "x" + result.HitCount + "/" + GetStatusName(definition.Element);
                break;
            case TotemTattooShape.ChainJump:
                ApplyEnemyArea(definition, source, target, magnitude, damageReason, result, chain: true);
                break;
            case TotemTattooShape.StackingMark:
                int threshold = Mathf.Max(1, definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 5);
                int stacks = IncrementMarkStack(source?.ActorId ?? 0, target.CombatantId, definition, threshold, out bool burst);
                result.StackCount = stacks;
                result.StackThreshold = threshold;
                result.BurstTriggered = burst;
                if (burst)
                {
                    float burstMultiplier = definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 4f;
                    ApplyEnemySingleDamage(definition, source, target, magnitude * burstMultiplier, damageReason, result);
                    result.StatusName = "BurstAt" + threshold + "/" + GetStatusName(definition.Element);
                }
                else
                {
                    result.StatusName = "Stack" + stacks + "/" + threshold;
                    result.Note = "StackingMark:Stack" + stacks + "/" + threshold;
                }
                break;
            case TotemTattooShape.ProbBurst:
                float probability = Mathf.Clamp01(definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 1f);
                if (ResolveEnemyDeterministicRoll(source, target, definition) <= probability)
                {
                    float multiplier = definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 2f;
                    ApplyEnemySingleDamage(definition, source, target, magnitude * multiplier, damageReason, result);
                    result.BurstTriggered = true;
                    result.StatusName = "x" + multiplier.ToString("F1") + "/" + GetStatusName(definition.Element);
                }
                else
                {
                    result.StatusName = "miss";
                    result.Note = "ProbBurst:Miss";
                }
                break;
            case TotemTattooShape.TrailZone:
                int ticks = Mathf.Max(1, definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 3);
                float tickDamage = magnitude * (definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 0.4f);
                for (int i = 0; i < ticks && target.IsAlive; i++)
                {
                    ApplyEnemySingleDamage(definition, source, target, tickDamage, damageReason, result);
                }
                result.StatusName = "trail/" + GetStatusName(definition.Element);
                break;
            case TotemTattooShape.SummonForm:
                ApplyEnemySingleDamage(definition, source, target, magnitude * Mathf.Max(1f, definition.ShapeParam1), damageReason, result);
                result.StatusName = "summon/" + GetStatusName(definition.Element);
                break;
            default:
                ApplyEnemySingleDamage(definition, source, target, magnitude, damageReason, result);
                break;
        }

        result.SourceHeal = ApplySourceElementEffect(definition, source, result.Damage);
        if (result.HitCount <= 0 && !result.BurstTriggered && string.Equals(result.Note, note, StringComparison.Ordinal))
        {
            result.Note = "NoTarget";
        }
        else if (!string.IsNullOrWhiteSpace(result.StatusName) && string.Equals(result.Note, note, StringComparison.Ordinal))
        {
            result.Note = note + "/" + result.StatusName;
        }

        return result;
    }

    private void ApplyEnemyArea(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemEnemyModel primary,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result,
        bool chain)
    {
        int maxTargets = chain
            ? (definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 3)
            : (definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 5);
        maxTargets = Mathf.Max(1, maxTargets);
        float radiusSqr = 64f;
        float damage = chain ? magnitude : magnitude * (definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 0.6f);
        float decay = chain && definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 0.7f;
        ApplyEnemySingleDamage(definition, source, primary, damage, damageReason, result);
        int count = enemyService.CopyAliveEnemies(enemyShapeTargetBuffer);
        int appliedTargets = 1;
        for (int i = 0; i < count && appliedTargets < maxTargets; i++)
        {
            TotemEnemyModel candidate = enemyShapeTargetBuffer[i];
            if (candidate == null || candidate == primary || (candidate.Position - primary.Position).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            if (chain)
            {
                damage *= decay;
            }
            ApplyEnemySingleDamage(definition, source, candidate, damage, damageReason, result);
            appliedTargets++;
        }

        result.StatusName = (chain ? "jumps" : "aoe") + result.HitCount + "/" + GetStatusName(definition.Element);
    }

    private void ApplyEnemySingleDamage(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemEnemyModel target,
        float damage,
        string damageReason,
        TotemTattooEffectResult result)
    {
        if (target == null || !target.IsAlive || damage <= 0f || enemyService == null)
        {
            return;
        }

        float worldTime = matchClock?.WorldTime ?? enemyService.WorldTime;
        if (!enemyService.TryApplyDamage(
                target.CombatantId,
                source,
                damage,
                damageReason,
                worldTime,
                out float appliedDamage))
        {
            return;
        }

        result.Damage += appliedDamage;
        result.HitCount++;
        ApplyEnemyElementStatus(definition, source, target, damage, damageReason, result, worldTime);
    }

    private void ApplyEnemyElementStatus(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemEnemyModel target,
        float damage,
        string damageReason,
        TotemTattooEffectResult result,
        float worldTime)
    {
        string statusName = GetStatusName(definition.Element);
        if (string.IsNullOrWhiteSpace(statusName) || target == null || !target.IsAlive)
        {
            return;
        }

        float statusChanceBonus = ResolveStatusChanceBonus(source);
        float statusChance = ComputeStatusApplyChance(ResolveBaseStatusApplyChance(definition), statusChanceBonus);
        float statusRoll = ResolveEnemyStatusRoll(source, target, definition, result?.HitCount ?? 0, statusChance);
        if (result != null)
        {
            result.StatusChance = statusChance;
            result.StatusChanceBonus = statusChanceBonus;
            result.StatusRoll = statusRoll;
        }

        if (!ShouldApplyStatus(statusChance, statusRoll))
        {
            return;
        }

        if (enemyService.TryApplyStatus(
                target.CombatantId,
                source,
                statusName,
                ResolveStatusPower(definition, damage),
                ResolveStatusDuration(definition),
                damageReason,
                worldTime,
                out _)
            && result != null)
        {
            result.StatusApplied = true;
        }
    }

    private void ApplyAreaBurst(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        int maxTargets = definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 5;
        float perTarget = magnitude * (definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 0.6f);
        FillShapeTargets(source, target, maxTargets);
        for (int i = 0; i < shapeTargetBuffer.Count; i++)
        {
            ApplySingleDamage(definition, source, shapeTargetBuffer[i], perTarget, damageReason, result);
        }
    }

    private void ApplyMultiHit(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        int segments = definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 4;
        segments = Mathf.Max(1, segments);
        float perHit = magnitude / segments;
        for (int i = 0; i < segments && target.IsAlive; i++)
        {
            ApplySingleDamage(definition, source, target, perHit, damageReason, result);
        }

        result.StatusName = $"x{result.HitCount}/{GetStatusName(definition.Element)}";
    }

    private void ApplyChainJump(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        int maxJumps = definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 3;
        float decay = definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 0.7f;
        FillShapeTargets(source, target, maxJumps);
        float damage = magnitude;
        for (int i = 0; i < shapeTargetBuffer.Count; i++)
        {
            ApplySingleDamage(definition, source, shapeTargetBuffer[i], damage, damageReason, result);
            damage *= decay;
        }

        result.StatusName = $"jumps{result.HitCount}/{GetStatusName(definition.Element)}";
    }

    private void ApplyStackingMark(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        int threshold = definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 5;
        float burstMultiplier = definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 4f;
        threshold = Mathf.Max(1, threshold);
        int stacks = IncrementMarkStack(source, target, definition, threshold, out bool burst);
        result.StackCount = stacks;
        result.StackThreshold = threshold;
        result.BurstTriggered = burst;
        if (!burst)
        {
            result.StatusName = $"Stack{stacks}/{threshold}";
            result.Note = $"StackingMark:Stack{stacks}/{threshold}";
            return;
        }

        ApplySingleDamage(definition, source, target, magnitude * burstMultiplier, damageReason, result);
        result.StatusName = $"BurstAt{threshold}/{GetStatusName(definition.Element)}";
        result.Note = $"StackingMark:BurstAt{threshold}";
    }

    private void ApplyProbabilityBurst(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        float probability = Mathf.Clamp01(definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 1f);
        float roll = ResolveDeterministicRoll(source, target, definition);
        if (roll > probability)
        {
            result.StatusName = "miss";
            result.Note = "ProbBurst:Miss";
            return;
        }

        float multiplier = definition.ShapeParam2 > 0f ? definition.ShapeParam2 : 2f;
        ApplySingleDamage(definition, source, target, magnitude * multiplier, damageReason, result);
        result.BurstTriggered = true;
        result.StatusName = $"x{multiplier:F1}/{GetStatusName(definition.Element)}";
        result.Note = "ProbBurst:Burst";
    }

    private void ApplyTrailZone(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float magnitude,
        string damageReason,
        TotemTattooEffectResult result)
    {
        int ticks = definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 3;
        float perTick = magnitude * (definition.ShapeParam1 > 0f ? definition.ShapeParam1 : 0.4f);
        FillShapeTargets(source, target, 5);
        for (int i = 0; i < shapeTargetBuffer.Count; i++)
        {
            var item = shapeTargetBuffer[i];
            for (int tick = 0; tick < ticks && item.IsAlive; tick++)
            {
                ApplySingleDamage(definition, source, item, perTick, damageReason, result);
            }
        }

        result.StatusName = $"trail/{GetStatusName(definition.Element)}";
    }

    private void ApplySingleDamage(
        TotemTattooDefinition definition,
        TotemActorModel source,
        TotemActorModel target,
        float damage,
        string damageReason,
        TotemTattooEffectResult result)
    {
        if (target == null || !target.IsAlive || damage <= 0f)
        {
            return;
        }

        ApplyDamage(target, damage, source, damageReason);
        result.Damage += damage;
        result.HitCount++;
        ApplyElementStatus(definition, source, target, damage, damageReason, result);
    }

    private void ApplyElementStatus(TotemTattooDefinition definition, TotemActorModel source, TotemActorModel target, float damage, string damageReason, TotemTattooEffectResult result)
    {
        string statusName = GetStatusName(definition.Element);
        if (string.IsNullOrWhiteSpace(statusName) || target == null || !target.IsAlive || statusService == null)
        {
            return;
        }

        float statusChanceBonus = ResolveStatusChanceBonus(source);
        float statusChance = ComputeStatusApplyChance(ResolveBaseStatusApplyChance(definition), statusChanceBonus);
        float statusRoll = ResolveStatusRoll(source, target, definition, result?.HitCount ?? 0, statusChance);
        if (result != null)
        {
            result.StatusChance = statusChance;
            result.StatusChanceBonus = statusChanceBonus;
            result.StatusRoll = statusRoll;
        }

        if (!ShouldApplyStatus(statusChance, statusRoll))
        {
            GFTrace.Info("TotemTattoo", "Status.Skipped", null, GFTrace.Data(
                "status", statusName,
                "chance", statusChance.ToString("F3"),
                "bonus", statusChanceBonus.ToString("F3"),
                "roll", statusRoll.ToString("F3"),
                "source", source?.Name ?? string.Empty,
                "target", target.Name ?? string.Empty));
            return;
        }

        statusService.ApplyStatus(
            target,
            statusName,
            ResolveStatusPower(definition, damage),
            ResolveStatusDuration(definition),
            source,
            damageReason);
        if (result != null)
        {
            result.StatusApplied = true;
        }
    }

    private float ApplySourceElementEffect(TotemTattooDefinition definition, TotemActorModel source, float totalDamage)
    {
        if (definition == null || source == null || totalDamage <= 0f)
        {
            return 0f;
        }

        float healed = 0f;
        if (definition.Element == TotemTattooElement.Holy)
        {
            float percent = definition.ElementParam1 > 0f ? definition.ElementParam1 : 0.15f;
            healed += source.Heal(totalDamage * percent);
        }

        if (ShouldUseGlobalEnchantAffixes(source))
        {
            healed += source.Heal(SumEnchantAffixValue(TotemTattooEnchantAffixType.SelfHealOnHit));
        }

        return healed;
    }

    private void FillShapeTargets(TotemActorModel source, TotemActorModel primary, int maxTargets)
    {
        shapeTargetBuffer.Clear();
        if (primary == null || maxTargets <= 0)
        {
            return;
        }

        if (primary.IsAlive)
        {
            shapeTargetBuffer.Add(primary);
        }

        if (actorService == null || actorService.Actors == null || shapeTargetBuffer.Count >= maxTargets)
        {
            return;
        }

        const float nearbyRadius = 8f;
        float nearbyRadiusSqr = nearbyRadius * nearbyRadius;
        while (shapeTargetBuffer.Count < maxTargets)
        {
            TotemActorModel best = null;
            float bestSqr = float.MaxValue;
            var actors = actorService.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                var candidate = actors[i];
                if (candidate == null ||
                    candidate == source ||
                    candidate == primary ||
                    !candidate.IsAlive ||
                    !IsValidShapeTarget(source, candidate) ||
                    shapeTargetBuffer.Contains(candidate))
                {
                    continue;
                }

                float sqr = (candidate.Position - primary.Position).sqrMagnitude;
                if (sqr > nearbyRadiusSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null)
            {
                break;
            }

            shapeTargetBuffer.Add(best);
        }
    }

    private TotemActorModel ResolveMoveTickTarget(TotemActorModel source, float movedDistance)
    {
        if (source == null || actorService == null || actorService.Actors == null)
        {
            return null;
        }

        float radius = Mathf.Max(2.5f, Mathf.Max(0f, movedDistance) + 1f);
        float radiusSqr = radius * radius;
        TotemActorModel best = null;
        float bestSqr = float.MaxValue;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (candidate == null ||
                candidate == source ||
                !candidate.IsAlive ||
                !IsValidShapeTarget(source, candidate))
            {
                continue;
            }

            float sqr = (candidate.Position - source.Position).sqrMagnitude;
            if (sqr > radiusSqr || sqr >= bestSqr)
            {
                continue;
            }

            bestSqr = sqr;
            best = candidate;
        }

        return best;
    }

    private bool IsValidShapeTarget(TotemActorModel source, TotemActorModel candidate)
    {
        if (candidate == null || candidate == source || !candidate.IsAlive)
        {
            return false;
        }

        if (source == null || relationshipService == null)
        {
            return true;
        }

        return relationshipService.EvaluateDamage(
            source,
            candidate,
            new TotemCombatRelationshipContext(matchClock?.WorldTime ?? 0f)).Allowed;
    }

    private int IncrementMarkStack(
        TotemActorModel source,
        TotemActorModel target,
        TotemTattooDefinition definition,
        int threshold,
        out bool burst)
    {
        return IncrementMarkStack(source?.ActorId ?? 0, target?.ActorId ?? 0, definition, threshold, out burst);
    }

    private int IncrementMarkStack(
        int sourceId,
        int targetId,
        TotemTattooDefinition definition,
        int threshold,
        out bool burst)
    {
        burst = false;
        var mark = GetOrCreateMarkState(sourceId, targetId, definition);
        mark.Stacks++;
        if (mark.Stacks >= threshold)
        {
            mark.Stacks = 0;
            burst = true;
            return threshold;
        }

        return mark.Stacks;
    }

    private TotemTattooMarkState GetOrCreateMarkState(TotemActorModel source, TotemActorModel target, TotemTattooDefinition definition)
    {
        return GetOrCreateMarkState(source?.ActorId ?? 0, target?.ActorId ?? 0, definition);
    }

    private TotemTattooMarkState GetOrCreateMarkState(int sourceId, int targetId, TotemTattooDefinition definition)
    {
        for (int i = 0; i < markStates.Count; i++)
        {
            var item = markStates[i];
            if (item.SourceId == sourceId &&
                item.TargetId == targetId &&
                item.PartId == definition.PartId &&
                item.ColorId == definition.ColorId &&
                item.PatternId == definition.PatternId)
            {
                return item;
            }
        }

        var created = new TotemTattooMarkState
        {
            SourceId = sourceId,
            TargetId = targetId,
            PartId = definition.PartId,
            ColorId = definition.ColorId,
            PatternId = definition.PatternId,
        };
        markStates.Add(created);
        return created;
    }

    private static float ResolveDeterministicRoll(TotemActorModel source, TotemActorModel target, TotemTattooDefinition definition)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + (source?.ActorId ?? 0);
            seed = seed * 31 + (target?.ActorId ?? 0);
            seed = seed * 31 + definition.PartId;
            seed = seed * 31 + definition.ColorId;
            seed = seed * 31 + definition.PatternId;
            seed = seed * 31 + Mathf.RoundToInt(definition.ShapeParam3);
            var rng = new System.Random(seed);
            return (float)rng.NextDouble();
        }
    }

    private static float ResolveEnemyDeterministicRoll(
        TotemActorModel source,
        TotemEnemyModel target,
        TotemTattooDefinition definition)
    {
        unchecked
        {
            uint hash = (uint)(source?.ActorId ?? 0) * 73856093u;
            hash ^= (uint)(target?.CombatantId ?? 0) * 19349663u;
            hash ^= (uint)(definition?.PartId ?? 0) * 83492791u;
            hash ^= (uint)(definition?.ColorId ?? 0) * 2654435761u;
            hash ^= (uint)(definition?.PatternId ?? 0) * 2246822519u;
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static float ResolveBaseStatusApplyChance(TotemTattooDefinition definition)
    {
        return DefaultStatusApplyChance;
    }

    private static float ResolveStatusRoll(TotemActorModel source, TotemActorModel target, TotemTattooDefinition definition, int hitIndex, float statusChance)
    {
        if (statusChance >= 1f)
        {
            return 0f;
        }

        unchecked
        {
            int seed = 29;
            seed = seed * 31 + (source?.ActorId ?? 0);
            seed = seed * 31 + (target?.ActorId ?? 0);
            seed = seed * 31 + (definition?.PartId ?? 0);
            seed = seed * 31 + (definition?.ColorId ?? 0);
            seed = seed * 31 + (definition?.PatternId ?? 0);
            seed = seed * 31 + Mathf.Max(0, hitIndex);
            var rng = new System.Random(seed);
            return (float)rng.NextDouble();
        }
    }

    private static float ResolveEnemyStatusRoll(
        TotemActorModel source,
        TotemEnemyModel target,
        TotemTattooDefinition definition,
        int hitIndex,
        float statusChance)
    {
        if (statusChance >= 1f)
        {
            return 0f;
        }

        unchecked
        {
            uint hash = (uint)(source?.ActorId ?? 0) * 73856093u;
            hash ^= (uint)(target?.CombatantId ?? 0) * 19349663u;
            hash ^= (uint)(definition?.PartId ?? 0) * 83492791u;
            hash ^= (uint)(definition?.ColorId ?? 0) * 2654435761u;
            hash ^= (uint)(definition?.PatternId ?? 0) * 2246822519u;
            hash ^= (uint)Mathf.Max(0, hitIndex) * 3266489917u;
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }

    private TotemTattooEffectResult CreatePendingTriggerResult(
        TotemTattooDefinition definition,
        TotemActorModel source,
        float baseMagnitude,
        List<TotemTattooPendingTrigger> targetPendingTriggers,
        TotemActorTattooRuntimeState sourceState)
    {
        if (definition == null || targetPendingTriggers == null)
        {
            return null;
        }

        var pending = new TotemTattooPendingTrigger
        {
            Definition = definition,
            ConsumeEvent = "AttackHitEvent",
            Magnitude = ResolvePendingMagnitude(definition, baseMagnitude),
            SourcePart = definition.PartName ?? string.Empty,
            ExpiresAfter = 1,
        };
        targetPendingTriggers.Add(pending);
        RecordPendingCreated(sourceState, pending);

        GFTrace.Info("TotemTattoo", "PendingTrigger.Created", null, GFTrace.Data(
            "sourcePart", pending.SourcePart,
            "consumeEvent", pending.ConsumeEvent,
            "magnitude", pending.Magnitude.ToString("F1")));

        return new TotemTattooEffectResult
        {
            Definition = definition,
            Damage = 0f,
            HitCount = 0,
            StatusName = "PendingTrigger",
            Note = $"Intercepted/PendingTrigger({pending.ConsumeEvent})",
        };
    }

    private void ConsumePendingTriggers(
        string triggerEvent,
        TotemActorModel source,
        TotemActorModel target,
        List<TotemTattooPendingTrigger> sourcePendingTriggers,
        List<TotemTattooEffectResult> sourceEffectLog,
        List<TotemTattooEffectResult> results,
        TotemActorTattooRuntimeState sourceState)
    {
        if (sourcePendingTriggers == null || sourcePendingTriggers.Count <= 0 || string.IsNullOrWhiteSpace(triggerEvent))
        {
            return;
        }

        for (int i = sourcePendingTriggers.Count - 1; i >= 0; i--)
        {
            var pending = sourcePendingTriggers[i];
            if (pending == null || pending.Definition == null)
            {
                sourcePendingTriggers.RemoveAt(i);
                continue;
            }

            if (!string.Equals(pending.ConsumeEvent, triggerEvent, StringComparison.Ordinal))
            {
                continue;
            }

            var result = ApplyDefinition(
                pending.Definition,
                source,
                target,
                pending.Magnitude * (1f + SumEnchantAffixValue(TotemTattooEnchantAffixType.ElementDamageBonus, source, target, evaluateConditions: true, consumeAfterDodge: true)),
                magnitudeAlreadyResolved: true,
                note: $"ConsumedPending@{triggerEvent}",
                reasonOverride: $"Tattoo:PendingTrigger:{pending.SourcePart}");
            if (result != null)
            {
                results?.Add(result);
                sourceEffectLog?.Add(result);
            }

            RecordPendingConsumed(sourceState, pending, triggerEvent);
            if (pending.ExpiresAfter > 0)
            {
                pending.ExpiresAfter--;
            }

            if (pending.ExpiresAfter == 0)
            {
                sourcePendingTriggers.RemoveAt(i);
            }
        }
    }

    private void ConsumeEnemyPendingTriggers(
        string triggerEvent,
        TotemActorModel source,
        TotemEnemyModel target,
        List<TotemTattooPendingTrigger> sourcePendingTriggers,
        List<TotemTattooEffectResult> sourceEffectLog,
        List<TotemTattooEffectResult> results,
        TotemActorTattooRuntimeState sourceState)
    {
        if (sourcePendingTriggers == null || sourcePendingTriggers.Count <= 0 || string.IsNullOrWhiteSpace(triggerEvent))
        {
            return;
        }

        for (int i = sourcePendingTriggers.Count - 1; i >= 0; i--)
        {
            TotemTattooPendingTrigger pending = sourcePendingTriggers[i];
            if (pending == null || pending.Definition == null)
            {
                sourcePendingTriggers.RemoveAt(i);
                continue;
            }

            if (!string.Equals(pending.ConsumeEvent, triggerEvent, StringComparison.Ordinal))
            {
                continue;
            }

            float magnitude = pending.Magnitude * (1f + SumEnchantAffixValue(
                TotemTattooEnchantAffixType.ElementDamageBonus,
                source,
                null,
                evaluateConditions: true,
                consumeAfterDodge: true));
            TotemTattooEffectResult result = ApplyEnemyDefinition(
                pending.Definition,
                source,
                target,
                magnitude,
                magnitudeAlreadyResolved: true,
                note: "ConsumedPending@" + triggerEvent,
                reasonOverride: "Tattoo:PendingTrigger:" + pending.SourcePart);
            if (result != null)
            {
                results?.Add(result);
                sourceEffectLog?.Add(result);
            }

            RecordPendingConsumed(sourceState, pending, triggerEvent);
            if (pending.ExpiresAfter > 0)
            {
                pending.ExpiresAfter--;
            }

            if (pending.ExpiresAfter == 0)
            {
                sourcePendingTriggers.RemoveAt(i);
            }
        }
    }

    private static bool ShouldCreatePendingTrigger(TotemTattooDefinition definition)
    {
        return definition != null &&
               definition.PartId == 5 &&
               string.Equals(definition.TriggerEvent, "DodgePressedEvent", StringComparison.Ordinal);
    }

    private void RecordPendingCreated(TotemActorTattooRuntimeState sourceState, TotemTattooPendingTrigger pending)
    {
        string summary = FormatPendingTrigger(pending);
        if (sourceState != null)
        {
            sourceState.PendingTriggerCreatedCount++;
            sourceState.LastPendingTriggerSource = pending.SourcePart ?? string.Empty;
            sourceState.LastPendingTriggerConsumeEvent = pending.ConsumeEvent ?? string.Empty;
            sourceState.LastPendingTriggerSummary = summary;
            return;
        }

        pendingTriggerCreatedCount++;
        lastPendingTriggerSource = pending.SourcePart ?? string.Empty;
        lastPendingTriggerConsumeEvent = pending.ConsumeEvent ?? string.Empty;
        lastPendingTriggerSummary = summary;
    }

    private void RecordPendingConsumed(TotemActorTattooRuntimeState sourceState, TotemTattooPendingTrigger pending, string triggerEvent)
    {
        string summary = FormatPendingTrigger(pending);
        if (sourceState != null)
        {
            sourceState.PendingTriggerConsumedCount++;
            sourceState.LastPendingTriggerSource = pending.SourcePart ?? string.Empty;
            sourceState.LastPendingTriggerConsumeEvent = triggerEvent ?? string.Empty;
            sourceState.LastPendingTriggerSummary = summary;
        }
        else
        {
            pendingTriggerConsumedCount++;
            lastPendingTriggerSource = pending.SourcePart ?? string.Empty;
            lastPendingTriggerConsumeEvent = triggerEvent ?? string.Empty;
            lastPendingTriggerSummary = summary;
        }

        GFTrace.Info("TotemTattoo", "PendingTrigger.Consumed", null, GFTrace.Data(
            "sourcePart", pending.SourcePart ?? string.Empty,
            "consumeEvent", triggerEvent ?? string.Empty,
            "magnitude", pending.Magnitude.ToString("F1")));
    }

    private void HandleSelfTattooInterruption(string triggerEvent, TotemActorModel source, float magnitude)
    {
        if (string.Equals(triggerEvent, "MoveTickEvent", StringComparison.Ordinal) && magnitude > 0f)
        {
            CancelSelfTattooForSource(source, "Moved");
        }
    }

    private void HandleAfterDodgeEnchant(string triggerEvent, TotemActorModel source)
    {
        if (!string.Equals(triggerEvent, "DodgePressedEvent", StringComparison.Ordinal) ||
            !ShouldUseGlobalEnchantAffixes(source) ||
            !HasAfterDodgeEnchantAffix())
        {
            return;
        }

        afterDodgeEnchantPending = true;
        afterDodgeEnchantActorId = source?.ActorId ?? 0;
        afterDodgeEnchantActorName = source?.Name ?? string.Empty;
        afterDodgeEnchantCreatedCount++;
        GFTrace.Info("TotemTattoo", "Enchant.AfterDodge.Ready", null, GFTrace.Data(
            "actorId", afterDodgeEnchantActorId.ToString(),
            "actor", afterDodgeEnchantActorName));
    }

    private void OnDamageResolved(TotemDamageRecord record)
    {
        if (record.Target == null || record.Amount <= 0f)
        {
            return;
        }

        if (record.Target.Kind == TotemActorKind.Player)
        {
            CancelPlayerSelfTattoo(record.Target, "Damaged");
        }

        CancelSelfTattoo(record.Target, "Damaged");
        if (resolvingDamageTriggeredTattoo ||
            !(record.Source is TotemActorModel sourceActor) ||
            !sourceActor.IsAlive ||
            !record.Target.IsAlive)
        {
            return;
        }

        resolvingDamageTriggeredTattoo = true;
        try
        {
            Trigger("DamagedEvent", record.Target, sourceActor, record.Amount);
        }
        finally
        {
            resolvingDamageTriggeredTattoo = false;
        }
    }

    private void CancelSelfTattooForSource(TotemActorModel source, string reason)
    {
        if (source == null)
        {
            CancelSelfTattoo(reason);
            return;
        }

        if (source.Kind == TotemActorKind.Player)
        {
            CancelSelfTattoo(reason);
        }

        CancelSelfTattoo(source, reason);
    }

    private void RecordSelfTattooCancelled(TotemActorTattooRuntimeState sourceState, string reason)
    {
        string resolvedReason = SanitizeCancelReason(reason);
        if (sourceState != null)
        {
            sourceState.SelfTattooCancelledCount++;
            sourceState.LastSelfTattooCancelReason = resolvedReason;
            return;
        }

        selfTattooCancelledCount++;
        lastSelfTattooCancelReason = resolvedReason;
    }

    private static string SanitizeCancelReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason;
    }

    private bool ApplyDamage(TotemActorModel target, float damage, TotemActorModel source, string reason)
    {
        if (actorService != null)
        {
            return actorService.ApplyDamage(target, damage, source, reason);
        }

        if (target == null || damage <= 0f || !target.IsAlive)
        {
            return false;
        }

        target.ApplyDamage(damage);
        if (!target.IsAlive && target.GameObject != null)
        {
            target.GameObject.SetActive(false);
        }

        return !target.IsAlive;
    }

    private static void EquipDefinition(List<TotemTattooDefinition> target, TotemTattooDefinition definition)
    {
        if (target == null || definition == null)
        {
            return;
        }

        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (target[i].PartId == definition.PartId)
            {
                target.RemoveAt(i);
            }
        }

        target.Add(definition);
    }

    private IReadOnlyList<TotemTattooDefinition> ResolveSourceEquipped(
        TotemActorModel source,
        out List<TotemTattooEffectResult> sourceEffectLog,
        out List<TotemTattooPendingTrigger> sourcePendingTriggers,
        out TotemActorTattooRuntimeState sourceState)
    {
        sourceState = null;
        if (source != null && TryGetActorState(source, out var state))
        {
            sourceState = state;
            sourceEffectLog = state.EffectLog;
            sourcePendingTriggers = state.PendingTriggers;
            return state.Equipped;
        }

        if (source != null && source.Kind != TotemActorKind.Player)
        {
            sourceEffectLog = null;
            sourcePendingTriggers = null;
            return null;
        }

        sourceEffectLog = effectLog;
        sourcePendingTriggers = pendingTriggers;
        return equipped;
    }

    private TotemActorTattooRuntimeState GetOrCreateActorState(TotemActorModel actor)
    {
        if (TryGetActorState(actor, out var state))
        {
            return state;
        }

        state = new TotemActorTattooRuntimeState
        {
            ActorId = actor.ActorId,
            ActorName = actor.Name,
        };
        actorStates.Add(state);
        return state;
    }

    private bool TryGetActorState(TotemActorModel actor, out TotemActorTattooRuntimeState state)
    {
        state = null;
        if (actor == null)
        {
            return false;
        }

        int actorId = actor.ActorId;
        for (int i = 0; i < actorStates.Count; i++)
        {
            var item = actorStates[i];
            if (item.ActorId == actorId)
            {
                state = item;
                return true;
            }
        }

        return false;
    }

    private int CountActorEquipped()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            count += actorStates[i].Equipped.Count;
        }

        return count;
    }

    private int CountActorSelfTattooInProgress()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            if (actorStates[i].SelfTattooInProgress)
            {
                count++;
            }
        }

        return count;
    }

    private int CountActorSelfTattooCancelled()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            count += actorStates[i].SelfTattooCancelledCount;
        }

        return count;
    }

    private int CountActorAppliedEffects()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            count += actorStates[i].EffectLog.Count;
        }

        return count;
    }

    private int CountActorPendingTriggers()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            count += actorStates[i].PendingTriggers.Count;
        }

        return count;
    }

    private int CountActorCritTriggers()
    {
        int count = 0;
        for (int i = 0; i < actorStates.Count; i++)
        {
            count += actorStates[i].CritTriggeredCount;
        }

        return count;
    }

    private float SumEnchantAffixValue(TotemTattooEnchantAffixType affixType)
    {
        return SumEnchantAffixValue(affixType, null, null, evaluateConditions: false);
    }

    private float SumEnchantAffixValue(TotemTattooEnchantAffixType affixType, TotemActorModel source, TotemActorModel target, bool evaluateConditions)
    {
        return SumEnchantAffixValue(affixType, source, target, evaluateConditions, consumeAfterDodge: false);
    }

    private float SumEnchantAffixValue(TotemTattooEnchantAffixType affixType, TotemActorModel source, TotemActorModel target, bool evaluateConditions, bool consumeAfterDodge)
    {
        float value = 0f;
        bool shouldConsumeAfterDodge = false;
        for (int i = 0; i < activeEnchantAffixes.Count; i++)
        {
            var affix = activeEnchantAffixes[i];
            bool usesAfterDodge = false;
            bool conditionMet = !evaluateConditions || IsEnchantConditionMet(affix, source, target, out usesAfterDodge);
            if (affix != null &&
                affix.AffixType == affixType &&
                conditionMet)
            {
                value += Mathf.Max(0f, affix.Value);
                shouldConsumeAfterDodge |= usesAfterDodge;
            }
        }

        if (consumeAfterDodge && shouldConsumeAfterDodge)
        {
            ConsumeAfterDodgeEnchant(source);
        }

        return value;
    }

    private bool IsEnchantConditionMet(TotemTattooEnchantAffixDefinition affix, TotemActorModel source, TotemActorModel target, out bool usesAfterDodge)
    {
        usesAfterDodge = false;
        if (affix == null || string.IsNullOrWhiteSpace(affix.ConditionKey))
        {
            return true;
        }

        if (string.Equals(affix.ConditionKey, "DistanceGt8m", StringComparison.OrdinalIgnoreCase))
        {
            if (source == null || target == null)
            {
                return false;
            }

            float threshold = affix.ConditionVal > 0f ? affix.ConditionVal : 8f;
            return Vector3.Distance(source.Position, target.Position) > threshold;
        }

        if (IsAfterDodgeAffix(affix))
        {
            usesAfterDodge = afterDodgeEnchantPending && target != null && ShouldUseGlobalEnchantAffixes(source);
            return usesAfterDodge;
        }

        return false;
    }

    private bool HasAfterDodgeEnchantAffix()
    {
        for (int i = 0; i < activeEnchantAffixes.Count; i++)
        {
            if (IsAfterDodgeAffix(activeEnchantAffixes[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAfterDodgeAffix(TotemTattooEnchantAffixDefinition affix)
    {
        return affix != null && string.Equals(affix.ConditionKey, "AfterDodge", StringComparison.OrdinalIgnoreCase);
    }

    private void ConsumeAfterDodgeEnchant(TotemActorModel source)
    {
        if (!afterDodgeEnchantPending)
        {
            return;
        }

        afterDodgeEnchantPending = false;
        afterDodgeEnchantActorId = source?.ActorId ?? afterDodgeEnchantActorId;
        afterDodgeEnchantActorName = source?.Name ?? afterDodgeEnchantActorName;
        afterDodgeEnchantConsumedCount++;
        GFTrace.Info("TotemTattoo", "Enchant.AfterDodge.Consumed", null, GFTrace.Data(
            "actorId", afterDodgeEnchantActorId.ToString(),
            "actor", afterDodgeEnchantActorName,
            "count", afterDodgeEnchantConsumedCount.ToString()));
    }

    private bool ShouldUseGlobalEnchantAffixes(TotemActorModel source)
    {
        return activeEnchantAffixes.Count > 0 &&
               (source == null || (source.Kind == TotemActorKind.Player && !TryGetActorState(source, out _)));
    }

    private static bool TryGetHeadDefinition(IReadOnlyList<TotemTattooDefinition> source, out TotemTattooDefinition definition)
    {
        definition = null;
        if (source == null)
        {
            return false;
        }

        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (item == null)
            {
                continue;
            }

            if (item.PartId == 1 || string.Equals(item.PartName, "Head", StringComparison.Ordinal))
            {
                definition = item;
                return true;
            }
        }

        return false;
    }

    private void RecordHeadCritical(TotemActorTattooRuntimeState sourceState, TotemTattooEffectResult result)
    {
        if (result == null)
        {
            return;
        }

        string summary = FormatTattooSummary(result.Definition);
        if (sourceState != null)
        {
            sourceState.CritTriggeredCount++;
            sourceState.LastCritBaseDamage = result.BaseDamage;
            sourceState.LastCritDamage = result.Damage;
            sourceState.LastCritChance = result.CritChance;
            sourceState.LastCritMultiplier = result.CritMultiplier;
            sourceState.LastCritRoll = result.CritRoll;
            sourceState.LastHeadPassiveCritRateBonus = result.PassiveCritRateBonus;
            sourceState.LastHeadPassiveElementBonus = result.PassiveElementBonus;
            sourceState.LastCritSourceName = result.Source?.Name ?? string.Empty;
            sourceState.LastCritTargetName = result.Target?.Name ?? string.Empty;
            sourceState.LastCritTattooSummary = summary;
            return;
        }

        critTriggeredCount++;
        lastCritBaseDamage = result.BaseDamage;
        lastCritDamage = result.Damage;
        lastCritChance = result.CritChance;
        lastCritMultiplier = result.CritMultiplier;
        lastCritRoll = result.CritRoll;
        lastHeadPassiveCritRateBonus = result.PassiveCritRateBonus;
        lastHeadPassiveElementBonus = result.PassiveElementBonus;
        lastCritSourceName = result.Source?.Name ?? string.Empty;
        lastCritTargetName = result.Target?.Name ?? string.Empty;
        lastCritTattooSummary = summary;
    }

    private static string BuildEquippedSummary(IReadOnlyList<TotemTattooDefinition> source)
    {
        if (source == null || source.Count <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(96);
        for (int i = 0; i < source.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(",");
            }

            builder.Append(source[i].PartName);
            builder.Append("/");
            builder.Append(source[i].ColorName);
            builder.Append("/");
            builder.Append(source[i].PatternName);
        }

        return builder.ToString();
    }

    private static string FormatTattooSummary(TotemTattooDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        return $"{definition.PartName}/{definition.ColorName}/{definition.PatternName}";
    }

    private static string BuildEnchantSummary(IReadOnlyList<TotemTattooEnchantAffixDefinition> source)
    {
        if (source == null || source.Count <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(96);
        for (int i = 0; i < source.Count; i++)
        {
            var affix = source[i];
            if (affix == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(",");
            }

            builder.Append(affix.AffixType);
            builder.Append("+");
            builder.Append(affix.Value.ToString("F2"));
        }

        return builder.ToString();
    }

    private string BuildPendingSelfTattooSummary()
    {
        return selfTattooInProgress
            ? $"Part{pendingPartId}/Color{pendingColorId}/Pattern{pendingPatternId}"
            : string.Empty;
    }

    private static string BuildPendingSelfTattooSummary(TotemActorTattooRuntimeState state)
    {
        return state != null && state.SelfTattooInProgress
            ? $"Part{state.PendingPartId}/Color{state.PendingColorId}/Pattern{state.PendingPatternId}"
            : string.Empty;
    }

    private static string FormatPendingTrigger(TotemTattooPendingTrigger pending)
    {
        if (pending == null || pending.Definition == null)
        {
            return string.Empty;
        }

        return $"{pending.SourcePart}/{pending.Definition.ColorName}/{pending.Definition.PatternName}->{pending.ConsumeEvent}";
    }

    private void ClearSelfTattooPending()
    {
        selfTattooInProgress = false;
        selfTattooRemainingSec = 0f;
        pendingPartId = 0;
        pendingColorId = 0;
        pendingPatternId = 0;
    }

    private static void ClearSelfTattooPending(TotemActorTattooRuntimeState state)
    {
        if (state == null)
        {
            return;
        }

        state.SelfTattooInProgress = false;
        state.SelfTattooRemainingSec = 0f;
        state.PendingPartId = 0;
        state.PendingColorId = 0;
        state.PendingPatternId = 0;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ApplyStartupSelection(flowService?.StartupSelection);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            Clear();
            GFTrace.Info("TotemTattoo", "RunState.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private static TotemTattooDefinition[] LoadCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateTattooDefinitions(),
            Array.Empty<TotemTattooDefinition>());
    }

    private static TotemTattooReadingTimeDefinition[] LoadReadingTimes()
    {
        var rows = NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().tattooReadingTimes,
            Array.Empty<TotemTattooReadingTimeCatalogEntry>());
        var result = new TotemTattooReadingTimeDefinition[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            result[i] = rows[i].ToDefinition();
        }

        return result;
    }

    private static TotemTattooEnchantAffixDefinition[] LoadEnchantAffixes()
    {
        var rows = NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().tattooEnchantAffixes,
            Array.Empty<TotemTattooEnchantAffixCatalogEntry>());
        var result = new TotemTattooEnchantAffixDefinition[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            result[i] = rows[i].ToDefinition();
        }

        return result;
    }

    private static TotemTattooEnchantRecipeDefinition[] LoadEnchantRecipes()
    {
        var rows = NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().tattooEnchantRecipes,
            Array.Empty<TotemTattooEnchantRecipeCatalogEntry>());
        var result = new TotemTattooEnchantRecipeDefinition[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            result[i] = rows[i].ToDefinition();
        }

        return result;
    }

    private static int CountOrFallback<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback?.Length ?? 0 : primary.Length;
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    private static int GetHitCount(TotemTattooDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        switch (definition.Shape)
        {
            case TotemTattooShape.AOEBurst:
                return definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 3;
            case TotemTattooShape.MultiHit:
                return definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 4;
            case TotemTattooShape.ChainJump:
                return definition.ShapeParam1 > 0f ? Mathf.RoundToInt(definition.ShapeParam1) : 3;
            case TotemTattooShape.TrailZone:
                return definition.ShapeParam2 > 0f ? Mathf.RoundToInt(definition.ShapeParam2) : 1;
            case TotemTattooShape.SummonForm:
                return 1;
            default:
                return 1;
        }
    }

    private static float ResolveStatusPower(TotemTattooDefinition definition, float damage)
    {
        if (definition == null)
        {
            return Mathf.Max(1f, damage * 0.1f);
        }

        switch (definition.Element)
        {
            case TotemTattooElement.Fire:
            case TotemTattooElement.Nature:
            case TotemTattooElement.Frost:
            case TotemTattooElement.Holy:
                return definition.ElementParam1 > 0f ? definition.ElementParam1 : Mathf.Max(1f, damage * 0.1f);
            case TotemTattooElement.Lightning:
                return 1f;
            case TotemTattooElement.Mutation:
                return definition.ElementParam3 > 0f ? definition.ElementParam3 : 1f;
            default:
                return Mathf.Max(1f, damage * 0.1f);
        }
    }

    private static float ResolveStatusDuration(TotemTattooDefinition definition)
    {
        if (definition == null)
        {
            return 2f;
        }

        switch (definition.Element)
        {
            case TotemTattooElement.Lightning:
                return definition.ElementParam1 > 0f ? definition.ElementParam1 : 1f;
            case TotemTattooElement.Fire:
            case TotemTattooElement.Nature:
            case TotemTattooElement.Frost:
                return definition.ElementParam2 > 0f ? definition.ElementParam2 : 2f;
            default:
                return 2f;
        }
    }

    private static string GetStatusName(TotemTattooElement element)
    {
        switch (element)
        {
            case TotemTattooElement.Fire:
                return "Burn";
            case TotemTattooElement.Nature:
                return "Poison";
            case TotemTattooElement.Frost:
                return "Slow";
            case TotemTattooElement.Lightning:
                return "Shock";
            case TotemTattooElement.Mutation:
                return "Mutation";
            case TotemTattooElement.Holy:
                return "HealMark";
            default:
                return string.Empty;
        }
    }

    private sealed class TotemActorTattooRuntimeState
    {
        public int ActorId;
        public string ActorName;
        public readonly List<TotemTattooDefinition> Equipped = new List<TotemTattooDefinition>(PartCount);
        public readonly List<TotemTattooEffectResult> EffectLog = new List<TotemTattooEffectResult>(16);
        public readonly List<TotemTattooPendingTrigger> PendingTriggers = new List<TotemTattooPendingTrigger>(8);
        public bool SelfTattooInProgress;
        public float SelfTattooRemainingSec;
        public int PendingPartId;
        public int PendingColorId;
        public int PendingPatternId;
        public int EnchantedCount;
        public int SelfTattooCancelledCount;
        public string LastSelfTattooCancelReason = string.Empty;
        public int PendingTriggerCreatedCount;
        public int PendingTriggerConsumedCount;
        public string LastPendingTriggerSource = string.Empty;
        public string LastPendingTriggerConsumeEvent = string.Empty;
        public string LastPendingTriggerSummary = string.Empty;
        public int CritTriggeredCount;
        public float LastCritBaseDamage;
        public float LastCritDamage;
        public float LastCritChance;
        public float LastCritMultiplier;
        public float LastCritRoll;
        public float LastHeadPassiveCritRateBonus;
        public float LastHeadPassiveElementBonus;
        public string LastCritSourceName = string.Empty;
        public string LastCritTargetName = string.Empty;
        public string LastCritTattooSummary = string.Empty;
    }

    private sealed class TotemTattooPendingTrigger
    {
        public TotemTattooDefinition Definition;
        public string ConsumeEvent;
        public float Magnitude;
        public string SourcePart;
        public int ExpiresAfter = -1;
    }

    private sealed class TotemTattooMarkState
    {
        public int SourceId;
        public int TargetId;
        public int PartId;
        public int ColorId;
        public int PatternId;
        public int Stacks;
    }
}
