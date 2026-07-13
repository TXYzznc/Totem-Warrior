using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TotemRuntimeAssetCatalog
{
    public int schemaVersion = 1;
    public string source = string.Empty;
    public TotemRuntimeAssetEntry[] entries = Array.Empty<TotemRuntimeAssetEntry>();
    [NonSerialized] private bool enemyEntriesNormalized;

    public void Normalize()
    {
        if (entries == null)
        {
            entries = Array.Empty<TotemRuntimeAssetEntry>();
        }
        if (enemyEntriesNormalized)
        {
            return;
        }

        var requiredEnemyEntries = BuildEnemyEntries();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && !string.IsNullOrWhiteSpace(entries[i].key)) keys.Add(entries[i].key);
        }
        var merged = new List<TotemRuntimeAssetEntry>(entries.Length + requiredEnemyEntries.Length);
        merged.AddRange(entries);
        for (int i = 0; i < requiredEnemyEntries.Length; i++)
        {
            if (keys.Add(requiredEnemyEntries[i].key)) merged.Add(requiredEnemyEntries[i]);
        }
        if (merged.Count != entries.Length) entries = merged.ToArray();
        enemyEntriesNormalized = true;
    }

    public bool TryGetEntry(string key, out TotemRuntimeAssetEntry entry)
    {
        Normalize();
        for (int i = 0; i < entries.Length; i++)
        {
            if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
            {
                entry = entries[i];
                return true;
            }
        }

        entry = null;
        return false;
    }

    public static TotemRuntimeAssetCatalog BuildDefault()
    {
        return new TotemRuntimeAssetCatalog
        {
            schemaVersion = 1,
            source = "runtime-default",
            entries = BuildEnemyEntries(),
        };
    }

    private static TotemRuntimeAssetEntry[] BuildEnemyEntries()
    {
        var entries = new List<TotemRuntimeAssetEntry>(26);
        AddFallback(entries, "common", "light", "Assets/Game/Prefabs/Entity/Actors/LightAI.prefab", "#D7DEE8", 0.8f);
        AddFallback(entries, "common", "elite", "Assets/Game/Prefabs/Entity/Actors/SmartAI.prefab", "#F2B84B", 1.1f);
        AddThemeFallbacks(entries, "ai_ruins", "#65D7FF", "#2F8FCC", "#58D6FF");
        AddThemeFallbacks(entries, "alien_hive", "#7DFF88", "#35B85A", "#A8FF6A");
        AddThemeFallbacks(entries, "virus_swamp", "#B6FF3C", "#7ACB32", "#D14DCE");

        AddEnemy(entries, "enemy_common_hunter", "common", "light");
        AddEnemy(entries, "enemy_common_shooter", "common", "light");
        AddEnemy(entries, "enemy_common_guardian", "common", "elite");
        AddEnemy(entries, "enemy_ai_servo", "ai_ruins", "light");
        AddEnemy(entries, "enemy_ai_arc_drone", "ai_ruins", "light");
        AddEnemy(entries, "enemy_ai_manager", "ai_ruins", "elite");
        AddEnemy(entries, "boss_ai_core_zero", "ai_ruins", "boss");
        AddEnemy(entries, "enemy_alien_crawler", "alien_hive", "light");
        AddEnemy(entries, "enemy_alien_spitter", "alien_hive", "light");
        AddEnemy(entries, "enemy_alien_guard", "alien_hive", "elite");
        AddEnemy(entries, "boss_alien_hive_mother", "alien_hive", "boss");
        AddEnemy(entries, "enemy_virus_mutant", "virus_swamp", "light");
        AddEnemy(entries, "enemy_virus_spore_carrier", "virus_swamp", "light");
        AddEnemy(entries, "enemy_virus_spore_host", "virus_swamp", "elite");
        AddEnemy(entries, "boss_virus_terminus", "virus_swamp", "boss");
        return entries.ToArray();
    }

    private static void AddThemeFallbacks(List<TotemRuntimeAssetEntry> entries, string theme, string lightTint, string eliteTint, string bossTint)
    {
        AddFallback(entries, theme, "light", "Assets/Game/Prefabs/Entity/Actors/LightAI.prefab", lightTint, 0.8f);
        AddFallback(entries, theme, "elite", "Assets/Game/Prefabs/Entity/Actors/SmartAI.prefab", eliteTint, 1.1f);
        AddFallback(entries, theme, "boss", "Assets/Game/Prefabs/Entity/Actors/Boss.prefab", bossTint, 2f);
    }

    private static void AddFallback(List<TotemRuntimeAssetEntry> entries, string theme, string tier, string activePath, string tint, float scale)
    {
        entries.Add(new TotemRuntimeAssetEntry
        {
            key = "enemy.fallback." + theme + "." + tier,
            assetKind = "Prefab",
            role = "Explicit " + theme + "/" + tier + " Enemy fallback",
            legacySourcePath = activePath,
            activeAssetPath = activePath,
            loadMode = "EditorAssetDatabaseThenGFResource",
            fallbackPrimitive = tier == "boss" ? "Cube" : "Capsule",
            tint = tint,
            scaleX = scale,
            scaleY = scale,
            scaleZ = scale,
            notes = "Theme/Tier placeholder used until final enemy art is available.",
        });
    }

    private static void AddEnemy(List<TotemRuntimeAssetEntry> entries, string enemyId, string theme, string tier)
    {
        string fallbackKey = "enemy.fallback." + theme + "." + tier;
        TotemRuntimeAssetEntry fallback = entries.Find(entry => string.Equals(entry.key, fallbackKey, StringComparison.Ordinal));
        string plannedFinalPath = "Assets/Game/Prefabs/Entity/Enemies/" + enemyId + ".prefab";
        entries.Add(new TotemRuntimeAssetEntry
        {
            key = "enemy." + enemyId,
            assetKind = "Prefab",
            role = "Native Enemy view for " + enemyId,
            legacySourcePath = fallback?.activeAssetPath ?? string.Empty,
            activeAssetPath = fallback?.activeAssetPath ?? string.Empty,
            loadMode = "EditorAssetDatabaseThenGFResource",
            fallbackKey = fallbackKey,
            fallbackPrimitive = fallback?.fallbackPrimitive ?? "Capsule",
            tint = fallback?.tint ?? "#FFFFFF",
            scaleX = fallback?.scaleX ?? 1f,
            scaleY = fallback?.scaleY ?? 1f,
            scaleZ = fallback?.scaleZ ?? 1f,
            notes = "Current observable placeholder; planned final art path: " + plannedFinalPath,
        });
    }
}

[Serializable]
public sealed class TotemRuntimeAssetEntry
{
    public string key;
    public string assetKind;
    public string role;
    public string legacySourcePath;
    public string activeAssetPath;
    public string loadMode;
    public string fallbackKey;
    public string fallbackPrimitive;
    public string tint;
    public float scaleX = 1f;
    public float scaleY = 1f;
    public float scaleZ = 1f;
    public string notes;

    public Vector3 Scale => new Vector3(scaleX <= 0f ? 1f : scaleX, scaleY <= 0f ? 1f : scaleY, scaleZ <= 0f ? 1f : scaleZ);
}

public static class TotemRuntimeAssetCatalogValidator
{
    private static readonly string[] RequiredKeys =
    {
        "actor.player",
        "actor.player.1",
        "actor.player.2",
        "actor.player.3",
        "actor.smartAi",
        "actor.lightAi",
        "actor.boss",
        "enemy.fallback.common.light",
        "enemy.fallback.common.elite",
        "enemy.fallback.ai_ruins.light",
        "enemy.fallback.ai_ruins.elite",
        "enemy.fallback.ai_ruins.boss",
        "enemy.fallback.alien_hive.light",
        "enemy.fallback.alien_hive.elite",
        "enemy.fallback.alien_hive.boss",
        "enemy.fallback.virus_swamp.light",
        "enemy.fallback.virus_swamp.elite",
        "enemy.fallback.virus_swamp.boss",
        "enemy.enemy_common_hunter",
        "enemy.enemy_common_shooter",
        "enemy.enemy_common_guardian",
        "enemy.enemy_ai_servo",
        "enemy.enemy_ai_arc_drone",
        "enemy.enemy_ai_manager",
        "enemy.boss_ai_core_zero",
        "enemy.enemy_alien_crawler",
        "enemy.enemy_alien_spitter",
        "enemy.enemy_alien_guard",
        "enemy.boss_alien_hive_mother",
        "enemy.enemy_virus_mutant",
        "enemy.enemy_virus_spore_carrier",
        "enemy.enemy_virus_spore_host",
        "enemy.boss_virus_terminus",
        "ui.character.card.unlocked",
        "ui.character.portrait.2",
        "ui.character.portrait.3",
        "npc.tattooist",
        "npc.merchant",
        "chest.chest_common",
        "chest.chest_rare",
        "weapon.knife_basic",
        "weapon.hammer_heavy",
        "weapon.pistol_basic",
        "weapon.bow_charge",
        "weapon.energy_fist",
        "skill.skill_fireball_01",
        "skill.skill_frost_field_01",
        "skill.skill_chain_lightning_01",
        "skill.skill_heal_aura_01",
        "skill.skill_shield_01",
        "skill.skill_stealth_01",
        "skill.skill_summon_01",
        "skill.skill_time_slow_01",
        "skill.skill_phase_dash",
        "skill.skill_ink_shield",
        "skill.skill_stomp",
        "skill.skill_beam",
        "skill.skill_summon",
        "skill.skill_enrage_aoe",
        "effect.attack.hit",
        "effect.skill.burst",
        "effect.boss.bolt",
        "effect.projectile.bullet_pistol",
        "effect.projectile.arrow_bow",
    };

    public static bool Validate(TotemRuntimeAssetCatalog catalog, IList<string> errors)
    {
        if (catalog == null)
        {
            errors?.Add("Runtime asset catalog is null.");
            return false;
        }

        catalog.Normalize();
        Require(catalog.schemaVersion > 0, errors, "schemaVersion must be positive.");
        for (int i = 0; i < RequiredKeys.Length; i++)
        {
            Require(catalog.TryGetEntry(RequiredKeys[i], out _), errors, $"Required runtime asset key is missing: {RequiredKeys[i]}");
        }

        for (int i = 0; i < catalog.entries.Length; i++)
        {
            var entry = catalog.entries[i];
            Require(!string.IsNullOrWhiteSpace(entry.key), errors, $"Entry[{i}] key is empty.");
            Require(!string.IsNullOrWhiteSpace(entry.assetKind), errors, $"Entry[{i}] assetKind is empty.");
            Require(!string.IsNullOrWhiteSpace(entry.activeAssetPath), errors, $"Entry[{i}] activeAssetPath is empty.");
            Require(!string.IsNullOrWhiteSpace(entry.fallbackPrimitive), errors, $"Entry[{i}] fallbackPrimitive is empty.");
            if (entry != null && entry.key != null && entry.key.StartsWith("enemy.", StringComparison.Ordinal) &&
                !entry.key.StartsWith("enemy.fallback.", StringComparison.Ordinal))
            {
                Require(!string.IsNullOrWhiteSpace(entry.fallbackKey), errors, $"Entry[{i}] enemy fallbackKey is empty.");
                Require(catalog.TryGetEntry(entry.fallbackKey, out _), errors, $"Entry[{i}] enemy fallbackKey is missing: {entry.fallbackKey}");
            }
        }

        return errors == null || errors.Count <= 0;
    }

    private static void Require(bool condition, IList<string> errors, string message)
    {
        if (!condition)
        {
            errors?.Add(message);
        }
    }
}
