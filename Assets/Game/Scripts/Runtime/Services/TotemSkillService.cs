using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemSkillService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const int SlotCount = 2;

    private readonly Dictionary<int, TotemSkillSlotState[]> actorStates = new Dictionary<int, TotemSkillSlotState[]>(64);
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemTattooService tattooService;
    private TotemSkillDefinition[] runtimeCatalog = Array.Empty<TotemSkillDefinition>();

    public override string ServiceName => "Skill";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        tattooService = runtime.GetService<TotemTattooService>();
        runtimeCatalog = NonEmpty(runtime.GetService<TotemDataService>()?.GameplayCatalog?.CreateSkillDefinitions(), LoadCatalog());
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
        tattooService = null;
        runtimeCatalog = Array.Empty<TotemSkillDefinition>();
        ResetRunState();
    }

    private void ResetRunState()
    {
        actorStates.Clear();
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (var pair in actorStates)
        {
            var slots = pair.Value;
            var actor = FindRuntimeActor(pair.Key);
            float cooldownMultiplier = actor == null || tattooService == null ? 1f : tattooService.ResolveSkillCooldownMultiplier(actor);
            for (int i = 0; i < slots.Length; i++)
            {
                TickSlot(slots[i], deltaTime, cooldownMultiplier);
            }
        }
    }

    public static IReadOnlyList<TotemSkillDefinition> GetCatalog()
    {
        return LoadCatalog();
    }

    public static bool TryGetDefinition(string skillId, out TotemSkillDefinition definition)
    {
        var catalog = LoadCatalog();
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].SkillId, skillId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public IReadOnlyList<TotemSkillDefinition> GetRuntimeCatalog()
    {
        return runtimeCatalog;
    }

    public bool TryGetRuntimeDefinition(string skillId, out TotemSkillDefinition definition)
    {
        var catalog = runtimeCatalog == null || runtimeCatalog.Length <= 0 ? LoadCatalog() : runtimeCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].SkillId, skillId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public void EquipDefaultLoadout(TotemActorModel actor)
    {
        EquipSkill(actor, 0, "skill_fireball_01");
        EquipSkill(actor, 1, "skill_stealth_01");
    }

    public bool EquipSkill(TotemActorModel actor, int slot, string skillId)
    {
        if (actor == null || slot < 0 || slot >= SlotCount || !TryGetRuntimeDefinition(skillId, out var definition))
        {
            return false;
        }

        var slots = GetOrCreateSlots(actor);
        slots[slot] = new TotemSkillSlotState
        {
            Skill = definition,
            Phase = TotemSkillPhase.Idle,
            CurrentCharges = Mathf.Max(1, definition.MaxCharges),
        };

        GFTrace.Info("TotemSkill", "Equip", null, GFTrace.Data(
            "actor", actor.Name,
            "slot", slot.ToString(),
            "skillId", skillId));
        return true;
    }

    public bool TryCastSlot(TotemActorModel actor, int slot, out TotemSkillDefinition skill)
    {
        skill = null;
        if (actor == null || slot < 0 || slot >= SlotCount)
        {
            return false;
        }

        var slots = GetOrCreateSlots(actor);
        var state = slots[slot];
        if (state?.Skill == null || state.Phase != TotemSkillPhase.Idle || state.CooldownRemaining > 0f || state.CurrentCharges <= 0)
        {
            return false;
        }

        skill = state.Skill;
        state.CurrentCharges = Mathf.Max(0, state.CurrentCharges - 1);
        state.Phase = TotemSkillPhase.Startup;
        state.PhaseElapsed = 0f;
        ApplyCastCost(actor, state, skill);
        GFTrace.Info("TotemSkill", "Cast", null, GFTrace.Data(
            "actor", actor.Name,
            "slot", slot.ToString(),
            "skillId", skill.SkillId));
        return true;
    }

    public float GetCooldownRemaining(TotemActorModel actor, int slot)
    {
        if (actor == null || slot < 0 || slot >= SlotCount || !actorStates.TryGetValue(actor.ActorId, out var slots))
        {
            return 0f;
        }

        return slots[slot]?.CooldownRemaining ?? 0f;
    }

    public int GetCurrentCharges(TotemActorModel actor, int slot)
    {
        if (actor == null || slot < 0 || slot >= SlotCount || !actorStates.TryGetValue(actor.ActorId, out var slots))
        {
            return 0;
        }

        return slots[slot]?.CurrentCharges ?? 0;
    }

    public string GetEquippedSkillId(TotemActorModel actor, int slot)
    {
        if (actor == null || slot < 0 || slot >= SlotCount || !actorStates.TryGetValue(actor.ActorId, out var slots))
        {
            return string.Empty;
        }

        return slots[slot]?.Skill?.SkillId ?? string.Empty;
    }

    public static float ResolveSkillDamage(TotemSkillDefinition skill, TotemWeaponDefinition weapon, float fallbackDamage)
    {
        if (skill == null)
        {
            return Mathf.Max(0f, fallbackDamage);
        }

        if (skill.Damage > 0f)
        {
            return skill.Damage;
        }

        if (skill.DamageMultiplier <= 0f)
        {
            return 0f;
        }

        float baseDamage = weapon == null || weapon.BaseDamage <= 0f ? fallbackDamage : weapon.BaseDamage;
        return Mathf.Max(0f, baseDamage * skill.DamageMultiplier);
    }

    public bool RefreshSkillSlot(TotemActorModel actor, int slot)
    {
        if (actor == null || slot < 0 || slot >= SlotCount)
        {
            return false;
        }

        var slots = GetOrCreateSlots(actor);
        var state = slots[slot];
        if (state?.Skill == null)
        {
            return false;
        }

        state.Phase = TotemSkillPhase.Idle;
        state.CooldownRemaining = 0f;
        state.ChargeRegenRemaining = 0f;
        state.PhaseElapsed = 0f;
        state.CurrentCharges = Mathf.Max(1, state.Skill.MaxCharges);
        GFTrace.Success("TotemSkill", "Refresh", null, GFTrace.Data(
            "actor", actor.Name,
            "slot", slot.ToString(),
            "skillId", state.Skill.SkillId));
        return true;
    }

    private static TotemSkillDefinition[] LoadCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateSkillDefinitions(),
            Array.Empty<TotemSkillDefinition>());
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    private TotemSkillSlotState[] GetOrCreateSlots(TotemActorModel actor)
    {
        if (!actorStates.TryGetValue(actor.ActorId, out var slots))
        {
            slots = new TotemSkillSlotState[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = new TotemSkillSlotState
                {
                    Phase = TotemSkillPhase.Idle,
                    CurrentCharges = 0,
                };
            }

            actorStates[actor.ActorId] = slots;
        }

        return slots;
    }

    private void TickSlot(TotemSkillSlotState state, float deltaTime, float cooldownMultiplier)
    {
        if (state == null)
        {
            return;
        }

        var skill = state.Skill;
        if (state.CooldownRemaining > 0f)
        {
            state.CooldownRemaining = Mathf.Max(0f, state.CooldownRemaining - deltaTime);
            if (state.CooldownRemaining <= 0f && skill != null && skill.ChargeModel != TotemSkillChargeModel.Charges)
            {
                state.CurrentCharges = Mathf.Max(1, skill.MaxCharges);
            }
        }

        if (skill != null && skill.ChargeModel == TotemSkillChargeModel.Charges && state.CurrentCharges < Mathf.Max(1, skill.MaxCharges))
        {
            int maxCharges = Mathf.Max(1, skill.MaxCharges);
            float regenTime = Mathf.Max(0.01f, skill.ChargeRegenTime * Mathf.Max(0.1f, cooldownMultiplier));
            if (state.ChargeRegenRemaining <= 0f)
            {
                state.ChargeRegenRemaining = regenTime;
            }

            state.ChargeRegenRemaining -= deltaTime;
            while (state.ChargeRegenRemaining <= 0f && state.CurrentCharges < maxCharges)
            {
                state.CurrentCharges++;
                if (state.CurrentCharges < maxCharges)
                {
                    state.ChargeRegenRemaining += regenTime;
                }
            }

            if (state.CurrentCharges >= maxCharges)
            {
                state.ChargeRegenRemaining = 0f;
            }
        }

        if (skill == null || state.Phase == TotemSkillPhase.Idle)
        {
            return;
        }

        state.PhaseElapsed += deltaTime;
        switch (state.Phase)
        {
            case TotemSkillPhase.Startup:
                if (state.PhaseElapsed >= skill.Startup)
                {
                    state.Phase = TotemSkillPhase.Active;
                    state.PhaseElapsed = 0f;
                }
                break;
            case TotemSkillPhase.Active:
                if (state.PhaseElapsed >= skill.Active)
                {
                    state.Phase = TotemSkillPhase.Recovery;
                    state.PhaseElapsed = 0f;
                }
                break;
            case TotemSkillPhase.Recovery:
                if (state.PhaseElapsed >= skill.Recovery)
                {
                    state.Phase = TotemSkillPhase.Idle;
                    state.PhaseElapsed = 0f;
                }
                break;
        }
    }

    private void ApplyCastCost(TotemActorModel actor, TotemSkillSlotState state, TotemSkillDefinition skill)
    {
        float cooldownMultiplier = actor == null || tattooService == null ? 1f : tattooService.ResolveSkillCooldownMultiplier(actor);
        switch (skill.ChargeModel)
        {
            case TotemSkillChargeModel.Charges:
                if (state.CurrentCharges < Mathf.Max(1, skill.MaxCharges) && state.ChargeRegenRemaining <= 0f)
                {
                    state.ChargeRegenRemaining = Mathf.Max(0.01f, skill.ChargeRegenTime * cooldownMultiplier);
                }

                state.CooldownRemaining = 0f;
                break;
            case TotemSkillChargeModel.HoldRelease:
                state.CooldownRemaining = Mathf.Max(0.01f, (skill.HoldDuration + skill.OverchargeWindow) * cooldownMultiplier);
                state.ChargeRegenRemaining = 0f;
                break;
            default:
                state.CooldownRemaining = Mathf.Max(0.01f, skill.Cooldown * cooldownMultiplier);
                state.ChargeRegenRemaining = 0f;
                break;
        }
    }

    private TotemActorModel FindRuntimeActor(int actorId)
    {
        if (actorService?.Actors == null)
        {
            return null;
        }

        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor != null && actor.ActorId == actorId)
            {
                return actor;
            }
        }

        return null;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            if (actorService?.Player != null)
            {
                EquipDefaultLoadout(actorService.Player);
            }

            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            GFTrace.Info("TotemSkill", "RunState.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }
}
