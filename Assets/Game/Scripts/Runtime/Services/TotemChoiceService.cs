using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemChoiceService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float DefaultChoiceTimeoutSec = 20f;

    private readonly HashSet<string> usedUniqueOptionIds = new HashSet<string>(StringComparer.Ordinal);
    private TotemEconomyService economyService;
    private TotemWeaponService weaponService;
    private TotemSkillService skillService;
    private TotemStatusService statusService;
    private TotemTattooService tattooService;
    private TotemActorService actorService;
    private TotemGameFlowService flowService;
    private TotemChoiceOption[] runtimeCatalog = Array.Empty<TotemChoiceOption>();
    private TotemGameplayEventDefinition[] runtimeEvents = Array.Empty<TotemGameplayEventDefinition>();
    private TotemChoiceSnapshot current;
    private TotemGameplayEventDefinition currentEvent;
    private TotemChoiceRuntimeState state = TotemChoiceRuntimeState.Idle;
    private float runElapsedSec;
    private float choiceTimeoutSec;
    private float choiceRemainingSec;
    private float previousTimeScale = 1f;
    private bool timeScalePausedByChoice;
    private string lastSelectedOptionId = string.Empty;
    private string lastResolutionReason = string.Empty;
    private bool lastResolutionTimedOut;

    public override string ServiceName => "Choice";

    public TotemChoiceSnapshot Current => current;

    public TotemChoiceRuntimeState ChoiceState => state;

    public float RunElapsedSec => runElapsedSec;

    public int UsedUniqueOptionCount => usedUniqueOptionIds.Count;

    public void Tick(float deltaTime)
    {
        float tickDelta = deltaTime > 0f ? deltaTime : Time.unscaledDeltaTime;
        if (tickDelta <= 0f)
        {
            return;
        }

        if (flowService?.CurrentState == TotemGameFlowState.CombatHud)
        {
            runElapsedSec += tickDelta;
        }

        if (state != TotemChoiceRuntimeState.Showing || current == null)
        {
            return;
        }

        choiceRemainingSec = Mathf.Max(0f, choiceRemainingSec - tickDelta);
        UpdateCurrentRuntimeFields();
        if (choiceRemainingSec <= 0f)
        {
            ResolveTimeout();
        }
    }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        economyService = runtime.GetService<TotemEconomyService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        skillService = runtime.GetService<TotemSkillService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        actorService = runtime.GetService<TotemActorService>();
        flowService = runtime.GetService<TotemGameFlowService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        var gameplayCatalog = runtime.GetService<TotemDataService>()?.GameplayCatalog;
        runtimeCatalog = NonEmpty(gameplayCatalog?.CreateChoiceOptions(), LoadChoiceCatalog());
        runtimeEvents = NonEmpty(gameplayCatalog?.CreateEvents(), LoadEventCatalog());
        ResetRunState();
    }

    protected override void OnShutdown()
    {
        RestoreTimeScaleIfNeeded();
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        economyService = null;
        weaponService = null;
        skillService = null;
        statusService = null;
        tattooService = null;
        actorService = null;
        runtimeCatalog = Array.Empty<TotemChoiceOption>();
        runtimeEvents = Array.Empty<TotemGameplayEventDefinition>();
        current = null;
        currentEvent = null;
        usedUniqueOptionIds.Clear();
        state = TotemChoiceRuntimeState.Idle;
    }

    public static IReadOnlyList<TotemChoiceOption> GetCatalog()
    {
        return LoadChoiceCatalog();
    }

    public IReadOnlyList<TotemChoiceOption> GetRuntimeCatalog()
    {
        return runtimeCatalog;
    }

    public IReadOnlyList<TotemGameplayEventDefinition> GetRuntimeEvents()
    {
        return runtimeEvents;
    }

    public TotemGameplayEventDefinition SelectEvent(TotemGameplayEventType eventType, int seed)
    {
        return SelectEvent(eventType, seed, runtimeEvents);
    }

    public static TotemGameplayEventDefinition SelectEvent(TotemGameplayEventType eventType, int seed, IReadOnlyList<TotemGameplayEventDefinition> events)
    {
        int totalWeight = 0;
        int count = events?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var item = events[i];
            if (item != null && item.EventType == eventType && item.WeightBase > 0)
            {
                totalWeight += item.WeightBase;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        var rng = new System.Random(seed);
        int roll = rng.Next(0, totalWeight);
        int cursor = 0;
        for (int i = 0; i < count; i++)
        {
            var item = events[i];
            if (item == null || item.EventType != eventType || item.WeightBase <= 0)
            {
                continue;
            }

            cursor += item.WeightBase;
            if (roll < cursor)
            {
                return item;
            }
        }

        return null;
    }

    public TotemChoiceSnapshot RollThreeChoices(string eventId, int seed)
    {
        if (state == TotemChoiceRuntimeState.Showing)
        {
            CloseCurrentChoice("Choice.Replaced");
        }

        currentEvent = FindEvent(eventId) ?? SelectEvent(TotemGameplayEventType.Choice, seed);
        current = BuildThreeChoices(eventId, seed, runtimeCatalog, runElapsedSec, usedUniqueOptionIds);
        BeginChoice(current, currentEvent);
        return current;
    }

    public TotemChoiceSnapshot RollAnchorChoice(TotemMapAnchor anchor, int seed)
    {
        if (anchor == null || anchor.Kind != TotemMapAnchorKind.Event)
        {
            return null;
        }

        string eventId = string.IsNullOrWhiteSpace(anchor.PayloadId) ? anchor.AnchorId : anchor.PayloadId;
        int anchorSeed = unchecked(seed + anchor.Order * 31);
        var choice = RollThreeChoices(eventId, anchorSeed);
        if (choice != null)
        {
            GFTrace.Success("TotemChoice", "AnchorChoice.Rolled", null, GFTrace.Data(
                "anchorId", anchor.AnchorId ?? string.Empty,
                "eventId", choice.EventId,
                "count", (choice.Options?.Length ?? 0).ToString()));
        }

        return choice;
    }

    public static TotemChoiceSnapshot BuildThreeChoices(string eventId, int seed)
    {
        return BuildThreeChoices(eventId, seed, LoadChoiceCatalog());
    }

    public static TotemChoiceSnapshot BuildThreeChoices(string eventId, int seed, IReadOnlyList<TotemChoiceOption> catalog)
    {
        return BuildThreeChoices(eventId, seed, catalog, 0f, null);
    }

    public static TotemChoiceSnapshot BuildThreeChoices(
        string eventId,
        int seed,
        IReadOnlyList<TotemChoiceOption> catalog,
        float elapsedSec,
        IReadOnlyCollection<string> usedUniqueOptionIds)
    {
        var rng = new System.Random(seed);
        var selected = new List<TotemChoiceOption>(3);
        int count = catalog?.Count ?? 0;
        var used = new bool[count];
        while (selected.Count < 3)
        {
            int totalWeight = 0;
            for (int i = 0; i < count; i++)
            {
                if (!used[i] && IsOptionAvailable(catalog[i], elapsedSec, usedUniqueOptionIds))
                {
                    totalWeight += Math.Max(1, catalog[i].WeightBase);
                }
            }

            if (totalWeight <= 0)
            {
                break;
            }

            int roll = rng.Next(0, totalWeight);
            int cursor = 0;
            int selectedIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (used[i] || !IsOptionAvailable(catalog[i], elapsedSec, usedUniqueOptionIds))
                {
                    continue;
                }

                cursor += Math.Max(1, catalog[i].WeightBase);
                if (roll < cursor)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0)
            {
                break;
            }

            used[selectedIndex] = true;
            selected.Add(catalog[selectedIndex]);
        }

        if (selected.Count < 3)
        {
            for (int i = 0; i < count && selected.Count < 3; i++)
            {
                if (used[i] || !IsOptionAvailable(catalog[i], elapsedSec, usedUniqueOptionIds))
                {
                    continue;
                }

                used[i] = true;
                selected.Add(catalog[i]);
            }
        }

        return new TotemChoiceSnapshot
        {
            EventId = string.IsNullOrWhiteSpace(eventId) ? "choice_event" : eventId,
            Options = selected.ToArray(),
            State = TotemChoiceRuntimeState.Idle,
            TimeoutSec = DefaultChoiceTimeoutSec,
            RemainingSec = DefaultChoiceTimeoutSec,
            RunElapsedSec = Mathf.Max(0f, elapsedSec),
            UsedUniqueOptionCount = usedUniqueOptionIds?.Count ?? 0,
        };
    }

    private static bool IsOptionAvailable(TotemChoiceOption option, float elapsedSec)
    {
        return IsOptionAvailable(option, elapsedSec, null);
    }

    private static bool IsOptionAvailable(TotemChoiceOption option, float elapsedSec, IReadOnlyCollection<string> usedUniqueOptionIds)
    {
        if (option == null || option.WeightBase <= 0 || option.MinRunElapsedSec > elapsedSec)
        {
            return false;
        }

        if (!option.IsUnique || usedUniqueOptionIds == null)
        {
            return true;
        }

        string optionId = option.OptionId ?? string.Empty;
        foreach (string usedOptionId in usedUniqueOptionIds)
        {
            if (string.Equals(usedOptionId, optionId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static bool ApplyChoiceEffect(
        TotemChoiceOption option,
        TotemActorModel player,
        TotemEconomyService economy,
        TotemWeaponService weapon,
        TotemStatusService status,
        TotemTattooService tattoo,
        out string summary)
    {
        return ApplyChoiceEffect(option, player, economy, weapon, null, status, tattoo, out summary);
    }

    public static bool ApplyChoiceEffect(
        TotemChoiceOption option,
        TotemActorModel player,
        TotemEconomyService economy,
        TotemWeaponService weapon,
        TotemSkillService skill,
        TotemStatusService status,
        TotemTattooService tattoo,
        out string summary)
    {
        summary = string.Empty;
        if (option == null || player == null)
        {
            return false;
        }

        switch (option.EffectType)
        {
            case TotemChoiceEffectType.CoinReward:
                int coins = ResolveIntMagnitude(option);
                economy?.AddCoins(player, coins);
                summary = $"Coins +{coins}";
                return economy != null;
            case TotemChoiceEffectType.Heal:
                int healAmount = ResolveIntMagnitude(option);
                float healed = player.Heal(healAmount);
                summary = $"Heal +{(int)healed}";
                return healed > 0f;
            case TotemChoiceEffectType.StatusCleanse:
                status?.ClearAllStatuses(player);
                summary = "Statuses cleansed";
                return status != null;
            case TotemChoiceEffectType.WeaponUpgrade:
                if (weapon == null)
                {
                    summary = "Weapon service unavailable";
                    return false;
                }

                bool upgraded = weapon.TryUpgradeEquipped(player, 50, out int convertedGold);
                if (!upgraded && convertedGold > 0)
                {
                    economy?.AddCoins(player, convertedGold);
                }

                summary = upgraded ? $"{weapon.GetEquippedWeaponId(player)} upgraded" : $"Max weapon converted {convertedGold}";
                return upgraded || convertedGold > 0;
            case TotemChoiceEffectType.SkillRefresh:
                if (skill == null)
                {
                    summary = "Skill service unavailable";
                    return false;
                }

                int refreshSlot = option.SkillSlot < 0 ? 0 : option.SkillSlot;
                bool refreshed = skill.RefreshSkillSlot(player, refreshSlot);
                summary = refreshed ? $"Skill slot {refreshSlot} refreshed" : $"Skill slot {refreshSlot} unavailable";
                return refreshed;
            case TotemChoiceEffectType.SkillAcquire:
                if (skill == null || string.IsNullOrWhiteSpace(option.ContentRef))
                {
                    summary = "Skill acquire unavailable";
                    return false;
                }

                int acquireSlot = option.SkillSlot < 0 ? 0 : option.SkillSlot;
                bool equipped = skill.EquipSkill(player, acquireSlot, option.ContentRef);
                summary = equipped ? $"Skill {option.ContentRef} equipped" : $"Skill {option.ContentRef} unavailable";
                return equipped;
            case TotemChoiceEffectType.RecipeUnlock:
                if (economy == null)
                {
                    summary = "Economy service unavailable";
                    return false;
                }

                string recipeId = string.IsNullOrWhiteSpace(option.ContentRef) ? option.OptionId : option.ContentRef;
                bool unlocked = economy.UnlockRecipe(player, recipeId);
                summary = unlocked ? $"Recipe {recipeId} unlocked" : $"Recipe {recipeId} already known";
                return unlocked;
            case TotemChoiceEffectType.TattooBonus:
                bool enchanted = tattoo != null && tattoo.ApplyMinorEnchant();
                summary = enchanted ? "Tattoo enchant applied" : "No equipped tattoo";
                return enchanted;
            default:
                summary = "Unknown effect";
                return false;
        }
    }

    private static int ResolveIntMagnitude(TotemChoiceOption option)
    {
        if (option == null)
        {
            return 0;
        }

        return option.ValueInt > 0 ? option.ValueInt : Math.Max(0, (int)option.Magnitude);
    }

    private static TotemChoiceOption[] LoadChoiceCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateChoiceOptions(),
            Array.Empty<TotemChoiceOption>());
    }

    private static TotemGameplayEventDefinition[] LoadEventCatalog()
    {
        var rows = NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateEvents(),
            Array.Empty<TotemGameplayEventDefinition>());
        var result = new TotemGameplayEventDefinition[rows.Length];
        Array.Copy(rows, result, rows.Length);
        return result;
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    public void CloseCurrentChoice(string reason)
    {
        if (state != TotemChoiceRuntimeState.Showing)
        {
            return;
        }

        lastSelectedOptionId = string.Empty;
        lastResolutionTimedOut = false;
        lastResolutionReason = string.IsNullOrWhiteSpace(reason) ? "Closed" : reason;
        state = TotemChoiceRuntimeState.Closed;
        choiceRemainingSec = 0f;
        RestoreTimeScaleIfNeeded();
        UpdateCurrentRuntimeFields();
        GFTrace.Info("TotemChoice", "Closed", null, GFTrace.Data("reason", lastResolutionReason));
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud && state == TotemChoiceRuntimeState.Showing)
        {
            CloseCurrentChoice("Flow.LeavingCombat");
        }
    }

    private void ResetRunState()
    {
        RestoreTimeScaleIfNeeded();
        usedUniqueOptionIds.Clear();
        current = null;
        currentEvent = null;
        state = TotemChoiceRuntimeState.Idle;
        runElapsedSec = 0f;
        choiceTimeoutSec = 0f;
        choiceRemainingSec = 0f;
        lastSelectedOptionId = string.Empty;
        lastResolutionReason = string.Empty;
        lastResolutionTimedOut = false;
    }

    private void BeginChoice(TotemChoiceSnapshot choice, TotemGameplayEventDefinition eventDefinition)
    {
        if (choice == null)
        {
            return;
        }

        choiceTimeoutSec = eventDefinition != null && eventDefinition.TimeoutSec > 0f
            ? eventDefinition.TimeoutSec
            : DefaultChoiceTimeoutSec;
        choiceRemainingSec = choiceTimeoutSec;
        lastSelectedOptionId = string.Empty;
        lastResolutionReason = "Showing";
        lastResolutionTimedOut = false;
        state = TotemChoiceRuntimeState.Showing;
        PauseTimeScaleForChoice();
        UpdateCurrentRuntimeFields();
        GFTrace.Success("TotemChoice", "Shown", null, GFTrace.Data(
            "eventId", choice.EventId,
            "timeout", choiceTimeoutSec.ToString("F1"),
            "runElapsed", runElapsedSec.ToString("F1"),
            "optionCount", (choice.Options?.Length ?? 0).ToString()));
    }

    private void ResolveTimeout()
    {
        if (state != TotemChoiceRuntimeState.Showing || current == null)
        {
            return;
        }

        var option = SelectTimeoutOption(current);
        bool applied = option != null && actorService?.Player != null &&
            ApplyChoiceEffect(option, actorService.Player, economyService, weaponService, skillService, statusService, tattooService, out _);
        ResolveChoice(option, timedOut: true, applied ? "TimeoutApplied" : "TimeoutNoEffect");
        GFTrace.Warning("TotemChoice", "Timeout", null, GFTrace.Data(
            "eventId", current.EventId,
            "optionId", option?.OptionId ?? string.Empty,
            "applied", applied.ToString()));
    }

    private void ResolveChoice(TotemChoiceOption option, bool timedOut, string reason)
    {
        if (option != null && option.IsUnique && !string.IsNullOrWhiteSpace(option.OptionId))
        {
            usedUniqueOptionIds.Add(option.OptionId);
        }

        lastSelectedOptionId = option?.OptionId ?? string.Empty;
        lastResolutionTimedOut = timedOut;
        lastResolutionReason = reason ?? string.Empty;
        state = timedOut ? TotemChoiceRuntimeState.Timeout : TotemChoiceRuntimeState.Resolved;
        choiceRemainingSec = 0f;
        RestoreTimeScaleIfNeeded();
        UpdateCurrentRuntimeFields();
    }

    private TotemChoiceOption SelectTimeoutOption(TotemChoiceSnapshot choice)
    {
        var options = choice?.Options;
        if (options == null || options.Length <= 0)
        {
            return null;
        }

        int seed = ComputeStableSeed(choice.EventId, usedUniqueOptionIds.Count);
        var rng = new System.Random(seed);
        int start = rng.Next(0, options.Length);
        for (int offset = 0; offset < options.Length; offset++)
        {
            var option = options[(start + offset) % options.Length];
            if (option != null)
            {
                return option;
            }
        }

        return null;
    }

    private TotemGameplayEventDefinition FindEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        for (int i = 0; i < runtimeEvents.Length; i++)
        {
            var item = runtimeEvents[i];
            if (item != null && string.Equals(item.EventId, eventId, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private void PauseTimeScaleForChoice()
    {
        if (timeScalePausedByChoice)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timeScalePausedByChoice = true;
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!timeScalePausedByChoice)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        timeScalePausedByChoice = false;
    }

    private void UpdateCurrentRuntimeFields()
    {
        if (current == null)
        {
            return;
        }

        current.State = state;
        current.TimeoutSec = choiceTimeoutSec > 0f ? choiceTimeoutSec : DefaultChoiceTimeoutSec;
        current.RemainingSec = Mathf.Max(0f, choiceRemainingSec);
        current.RunElapsedSec = Mathf.Max(0f, runElapsedSec);
        current.TimedOut = lastResolutionTimedOut;
        current.SelectedOptionId = lastSelectedOptionId;
        current.LastResolutionReason = lastResolutionReason;
        current.UsedUniqueOptionCount = usedUniqueOptionIds.Count;
    }

    private static int ComputeStableSeed(string text, int salt)
    {
        text ??= string.Empty;
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash * 31 + salt;
        }
    }

    private TotemChoiceOption FindCurrentOption(TotemChoiceOption option)
    {
        var options = current?.Options;
        if (option == null || options == null)
        {
            return null;
        }

        string optionId = option.OptionId ?? string.Empty;
        for (int i = 0; i < options.Length; i++)
        {
            var candidate = options[i];
            if (candidate == null)
            {
                continue;
            }

            if (ReferenceEquals(candidate, option))
            {
                return candidate;
            }

            if (!string.IsNullOrWhiteSpace(optionId) &&
                string.Equals(candidate.OptionId, optionId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    public bool ApplyChoice(TotemChoiceOption option)
    {
        if (option == null || actorService?.Player == null || state != TotemChoiceRuntimeState.Showing || current == null)
        {
            return false;
        }

        var currentOption = FindCurrentOption(option);
        if (currentOption == null)
        {
            GFTrace.Warning("TotemChoice", "ApplyRejected", null, GFTrace.Data(
                "optionId", option.OptionId ?? string.Empty,
                "effectType", option.EffectType.ToString(),
                "summary", "OptionNotInCurrentChoice"));
            return false;
        }

        bool applied = ApplyChoiceEffect(currentOption, actorService.Player, economyService, weaponService, skillService, statusService, tattooService, out string summary);
        if (applied)
        {
            ResolveChoice(currentOption, timedOut: false, "Selected");
            GFTrace.Success("TotemChoice", "Apply", null, GFTrace.Data(
                "optionId", currentOption.OptionId,
                "effectType", currentOption.EffectType.ToString(),
                "summary", summary));
        }
        else
        {
            GFTrace.Warning("TotemChoice", "ApplyRejected", null, GFTrace.Data(
                "optionId", currentOption.OptionId,
                "effectType", currentOption.EffectType.ToString(),
                "summary", summary));
        }

        return applied;
    }
}
