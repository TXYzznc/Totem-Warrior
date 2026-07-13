using System;
using UnityEngine;

public static class TotemEnemyCatalogAdapter
{
    public static TotemEnemyRuntimeDefinition CreateRuntimeDefinition(
        TotemEnemyDefinition source,
        TotemEnemyAbilityDefinition[] abilityCatalog,
        TotemBossPhase[] bossPhaseCatalog)
    {
        if (source == null)
        {
            return null;
        }

        return new TotemEnemyRuntimeDefinition
        {
            enemyId = source.EnemyId ?? string.Empty,
            displayName = source.DisplayName ?? source.EnemyId ?? string.Empty,
            themeId = source.ThemeId ?? string.Empty,
            tier = source.Tier,
            runtimeAssetKey = source.RuntimeAssetKey ?? string.Empty,
            lootTableId = source.LootTableId ?? string.Empty,
            guaranteedLootIds = source.GuaranteedLootIds ?? string.Empty,
            abilityIds = source.AbilityIds ?? string.Empty,
            maxHealth = Mathf.Max(1f, source.BaseHP),
            baseDamage = Mathf.Max(0f, source.BaseDamage),
            behavior = new TotemEnemyBehaviorDefinition
            {
                behaviorProfileId = source.BehaviorProfileId ?? string.Empty,
                detectRange = Mathf.Max(0.1f, source.DetectRange),
                attackRange = Mathf.Max(0.1f, source.AttackRange),
                leashRange = Mathf.Max(0.1f, source.LeashRange),
                moveSpeed = Mathf.Max(0f, source.MoveSpeed),
            },
            abilities = ResolveAbilities(source.AbilityIds, source.BaseHP, abilityCatalog),
            bossPhases = ResolveBossPhases(source.EnemyId, bossPhaseCatalog),
        };
    }

    private static TotemEnemyAbilityRuntimeDefinition[] ResolveAbilities(
        string abilityIds,
        float baseHealth,
        TotemEnemyAbilityDefinition[] catalog)
    {
        if (string.IsNullOrEmpty(abilityIds) || catalog == null || catalog.Length == 0)
        {
            return Array.Empty<TotemEnemyAbilityRuntimeDefinition>();
        }

        int count = 0;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && ContainsDelimitedToken(abilityIds, catalog[i].AbilityId)) count++;
        }

        var result = new TotemEnemyAbilityRuntimeDefinition[count];
        int cursor = 0;
        for (int i = 0; i < catalog.Length; i++)
        {
            TotemEnemyAbilityDefinition source = catalog[i];
            if (source == null || !ContainsDelimitedToken(abilityIds, source.AbilityId)) continue;
            result[cursor++] = new TotemEnemyAbilityRuntimeDefinition
            {
                abilityId = source.AbilityId ?? string.Empty,
                abilityType = source.AbilityType,
                range = source.Range,
                radius = source.Radius,
                cooldown = source.Cooldown,
                windup = source.Windup,
                active = source.Active,
                recovery = source.Recovery,
                damageMultiplier = source.DamageMultiplier,
                statusId = source.StatusId ?? string.Empty,
                statusChance = source.StatusChance,
                summonEnemyId = source.SummonEnemyId ?? string.Empty,
                summonCount = source.SummonCount,
                vfxId = source.VfxId ?? string.Empty,
                audioCueId = source.AudioCueId ?? string.Empty,
                shieldAmount = source.AbilityType == TotemEnemyAbilityType.Shield ? Mathf.Max(1f, baseHealth * 0.15f) : 0f,
                healAmount = source.AbilityType == TotemEnemyAbilityType.Regenerate ? Mathf.Max(1f, baseHealth * 0.1f) : 0f,
                moveDistance = source.AbilityType == TotemEnemyAbilityType.Charge || source.AbilityType == TotemEnemyAbilityType.Leap
                    ? Mathf.Max(0f, source.Range * 0.75f)
                    : 0f,
            };
        }

        return result;
    }

    private static TotemBossPhaseDefinition[] ResolveBossPhases(string enemyId, TotemBossPhase[] catalog)
    {
        if (string.IsNullOrEmpty(enemyId) || catalog == null || catalog.Length == 0)
        {
            return Array.Empty<TotemBossPhaseDefinition>();
        }

        int count = 0;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].BossId, enemyId, StringComparison.Ordinal)) count++;
        }

        var result = new TotemBossPhaseDefinition[count];
        int cursor = 0;
        for (int i = 0; i < catalog.Length; i++)
        {
            TotemBossPhase source = catalog[i];
            if (source == null || !string.Equals(source.BossId, enemyId, StringComparison.Ordinal)) continue;
            result[cursor++] = new TotemBossPhaseDefinition
            {
                phase = Mathf.Max(1, source.PhaseIndex),
                enterHealthRatio = Mathf.Clamp01(source.HPThreshold),
                damageMultiplier = Mathf.Max(0f, source.EnrageMultiplier),
                transitionSeconds = 0.5f,
                vfxId = source.PhaseVFXId ?? string.Empty,
                audioCueId = source.PhaseBGMCueId ?? string.Empty,
            };
        }

        return result;
    }

    private static bool ContainsDelimitedToken(string values, string token)
    {
        if (string.IsNullOrEmpty(values) || string.IsNullOrEmpty(token)) return false;
        int start = 0;
        for (int i = 0; i <= values.Length; i++)
        {
            if (i < values.Length && values[i] != ',' && values[i] != ';' && values[i] != '|') continue;
            int left = start;
            int right = i - 1;
            while (left <= right && char.IsWhiteSpace(values[left])) left++;
            while (right >= left && char.IsWhiteSpace(values[right])) right--;
            int length = right - left + 1;
            if (length == token.Length && string.CompareOrdinal(values, left, token, 0, length) == 0) return true;
            start = i + 1;
        }

        return false;
    }
}
