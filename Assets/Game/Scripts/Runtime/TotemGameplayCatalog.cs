using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TotemGameplayCatalog
{
    public int schemaVersion = 1;
    public string source = string.Empty;
    public TotemGameplayCatalogGenerationInfo generation = new TotemGameplayCatalogGenerationInfo();
    public TotemMapResourcePickupCatalogEntry[] mapResourcePickups = Array.Empty<TotemMapResourcePickupCatalogEntry>();
    public TotemMapTemplateCatalogEntry[] mapTemplates = Array.Empty<TotemMapTemplateCatalogEntry>();
    public TotemZonePhaseCatalogEntry[] zonePhases = Array.Empty<TotemZonePhaseCatalogEntry>();
    public TotemAudioCueCatalogEntry[] audioCues = Array.Empty<TotemAudioCueCatalogEntry>();
    public TotemBotProfileCatalogEntry[] botProfiles = Array.Empty<TotemBotProfileCatalogEntry>();
    public TotemBotBuildPresetCatalogEntry[] botBuildPresets = Array.Empty<TotemBotBuildPresetCatalogEntry>();
    public TotemAITuningDefinition aiTuning = TotemAITuningDefinition.Default;

    public void Normalize()
    {
        if (generation == null) generation = new TotemGameplayCatalogGenerationInfo();
        if (mapResourcePickups == null) mapResourcePickups = Array.Empty<TotemMapResourcePickupCatalogEntry>();
        if (mapTemplates == null) mapTemplates = Array.Empty<TotemMapTemplateCatalogEntry>();
        if (zonePhases == null) zonePhases = Array.Empty<TotemZonePhaseCatalogEntry>();
        if (audioCues == null) audioCues = Array.Empty<TotemAudioCueCatalogEntry>();
        if (botProfiles == null) botProfiles = Array.Empty<TotemBotProfileCatalogEntry>();
        if (botBuildPresets == null) botBuildPresets = Array.Empty<TotemBotBuildPresetCatalogEntry>();
        if (aiTuning == null) aiTuning = TotemAITuningDefinition.Default;
    }

    public TotemMapResourcePickupDefinition[] CreateMapResourcePickupDefinitions()
    {
        var result = new TotemMapResourcePickupDefinition[mapResourcePickups.Length];
        for (int i = 0; i < mapResourcePickups.Length; i++)
        {
            result[i] = mapResourcePickups[i].ToDefinition();
        }

        return result;
    }

    public TotemMapTemplateDefinition[] CreateMapTemplates()
    {
        var result = new TotemMapTemplateDefinition[mapTemplates.Length];
        for (int i = 0; i < mapTemplates.Length; i++)
        {
            result[i] = mapTemplates[i].ToDefinition();
        }

        return result;
    }

    public TotemZonePhase[] CreateZonePhases()
    {
        var result = new TotemZonePhase[zonePhases.Length];
        for (int i = 0; i < zonePhases.Length; i++)
        {
            result[i] = zonePhases[i].ToPhase();
        }

        return result;
    }






    public TotemAudioCueDefinition[] CreateAudioCueDefinitions()
    {
        var result = new TotemAudioCueDefinition[audioCues.Length];
        for (int i = 0; i < audioCues.Length; i++)
        {
            result[i] = audioCues[i].ToDefinition();
        }

        return result;
    }

    public TotemBotProfileDefinition[] CreateBotProfiles()
    {
        var result = new TotemBotProfileDefinition[botProfiles.Length];
        for (int i = 0; i < botProfiles.Length; i++)
        {
            result[i] = botProfiles[i].ToDefinition();
        }

        return result;
    }

    public TotemBotBuildPresetDefinition[] CreateBotBuildPresets()
    {
        var result = new TotemBotBuildPresetDefinition[botBuildPresets.Length];
        for (int i = 0; i < botBuildPresets.Length; i++)
        {
            result[i] = botBuildPresets[i].ToDefinition();
        }

        return result;
    }

    public static TotemGameplayCatalog BuildDefault()
    {
        return new TotemGameplayCatalog
        {
            schemaVersion = 1,
            source = "runtime-default",
            generation = new TotemGameplayCatalogGenerationInfo
            {
                generatedBy = "runtime-default",
                sourceRoot = "runtime-default",
                sourceFileCount = 0,
                sourceContentHash = "BuildDefault",
            },
            mapResourcePickups = BuildDefaultMapResourcePickups(),
            mapTemplates = BuildDefaultMapTemplates(),
            zonePhases = new[]
            {
                new TotemZonePhaseCatalogEntry { id = 0, phaseName = "Shrink1", startTime = 0f, duration = 30f, targetRadius = 110f, outZoneDamage = 2f, centerOffsetMode = "None" },
                new TotemZonePhaseCatalogEntry { id = 1, phaseName = "Shrink2", startTime = 0f, duration = 30f, targetRadius = 75f, outZoneDamage = 3f, centerOffsetMode = "None" },
                new TotemZonePhaseCatalogEntry { id = 2, phaseName = "Shrink3", startTime = 0f, duration = 30f, targetRadius = 50f, outZoneDamage = 5f, centerOffsetMode = "None" },
                new TotemZonePhaseCatalogEntry { id = 3, phaseName = "Shrink4", startTime = 0f, duration = 30f, targetRadius = 35f, outZoneDamage = 8f, centerOffsetMode = "None" },
            },
            audioCues = BuildDefaultAudioCues(),
            botProfiles = BuildDefaultBotProfiles(),
            botBuildPresets = BuildDefaultBotBuildPresets(),
            aiTuning = TotemAITuningDefinition.Default,
        };
    }

    private static TotemMapResourcePickupCatalogEntry[] BuildDefaultMapResourcePickups()
    {
        return new[]
        {
            MapResource("pigment.fire.small", "Fire", 4, 6, 60, "pickup.pigment.fire.small"),
            MapResource("pigment.fire.medium", "Fire", 8, 12, 30, "pickup.pigment.fire.medium"),
            MapResource("pigment.fire.large", "Fire", 16, 20, 10, "pickup.pigment.fire.large"),
            MapResource("pigment.ice.small", "Ice", 4, 6, 60, "pickup.pigment.ice.small"),
            MapResource("pigment.ice.medium", "Ice", 8, 12, 30, "pickup.pigment.ice.medium"),
            MapResource("pigment.ice.large", "Ice", 16, 20, 10, "pickup.pigment.ice.large"),
            MapResource("pigment.lightning.small", "Lightning", 4, 6, 60, "pickup.pigment.lightning.small"),
            MapResource("pigment.lightning.medium", "Lightning", 8, 12, 30, "pickup.pigment.lightning.medium"),
            MapResource("pigment.lightning.large", "Lightning", 16, 20, 10, "pickup.pigment.lightning.large"),
        };
    }

    private static TotemMapResourcePickupCatalogEntry MapResource(
        string pickupId,
        string element,
        int minAmount,
        int maxAmount,
        int weight,
        string assetKey)
    {
        return new TotemMapResourcePickupCatalogEntry
        {
            pickupId = pickupId,
            category = "Pigment",
            resourceId = "pigment." + element.ToLowerInvariant(),
            element = element,
            minAmount = minAmount,
            maxAmount = maxAmount,
            weight = weight,
            minRound = 1,
            maxRound = 3,
            assetKey = assetKey,
            enabled = true,
        };
    }

    public static TotemMapTemplateCatalogEntry[] BuildDefaultMapTemplates()
    {
        return new[]
        {
            new TotemMapTemplateCatalogEntry { id = 1, themeName = "OASIS_CITY", mapSize = 400f, minRoomSize = 40f, prefabPath = "Assets/Game/Scene/OasisCity.unity", hudAccentColor = "#379091", dominantColor = "#B7824F" },
        };
    }














    private static TotemAudioCueCatalogEntry[] BuildDefaultAudioCues()
    {
        return new[]
        {
            new TotemAudioCueCatalogEntry { cueId = "bgm_main_menu", kind = "Bgm", assetName = "BGM/main_menu.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Main menu and non-combat front-end flow.", legacySource = "GameState.MainMenu" },
            new TotemAudioCueCatalogEntry { cueId = "bgm_in_game", kind = "Bgm", assetName = "BGM/in_game.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Combat BGM.", legacySource = "GameState.InGame" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_hit_ranged", kind = "Sfx", assetName = "SFX/hit_ranged.wav", volume = 1f, loop = false, minIntervalSec = 0.05f, usage = "Ranged weapon hit.", legacySource = "WeaponClass.Ranged" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_kill", kind = "Sfx", assetName = "SFX/kill.wav", volume = 1f, loop = false, minIntervalSec = 0.08f, usage = "Opponent eliminated.", legacySource = "TargetKilledEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_player_died", kind = "Sfx", assetName = "SFX/player_died.wav", volume = 1f, loop = false, minIntervalSec = 0f, usage = "Player death.", legacySource = "PlayerDiedEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_dodge", kind = "Sfx", assetName = "SFX/dodge.wav", volume = 0.8f, loop = false, minIntervalSec = 0.12f, usage = "Dodge input feedback.", legacySource = "DodgePressedEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_ui_click", kind = "Sfx", assetName = "ui/ui_click.wav", volume = 0.8f, loop = false, minIntervalSec = 0.05f, usage = "Shared UI click feedback.", legacySource = "GF UIFormBase" },
        };
    }

    private static TotemBotProfileCatalogEntry[] BuildDefaultBotProfiles()
    {
        return new[]
        {
            NewBotProfile(1, "Smart", "Smart Aggressive", "Aggressive", 1, 20f, 18f, 210, 0.88f, 0.95f, 1.00f, 1.00f, 0.25f, 0.82f, attackCooldown: 0.85f),
            NewBotProfile(2, "Smart", "Smart Conservative", "Conservative", 3, 22f, 13f, 320, 0.72f, 0.45f, 0.45f, 0.30f, 0.25f, 0.25f, attackCooldown: 1.10f),
            NewBotProfile(3, "Smart", "Smart Resource", "ResourceAcquisition", 5, 21f, 16f, 260, 0.78f, 1.60f, 0.25f, 0.20f, 1.80f, 0.50f, attackCooldown: 1.00f),
            NewBotProfile(4, "Smart", "Smart Player Priority", "PlayerPriority", 2, 22f, 19f, 220, 0.86f, 0.70f, 1.25f, 1.25f, 0.30f, 0.80f, attackCooldown: 0.85f),
            NewBotProfile(101, "Light", "Light Scout A", "Hybrid", 4, 14f, 12f, 350, 0.45f, 0.30f, 0.90f, 0.90f, 0.40f, 0.60f, rethinkInterval: 45f, attackCooldown: 1.00f),
            NewBotProfile(102, "Light", "Light Scout B", "Hybrid", 7, 14f, 12f, 400, 0.40f, 0.30f, 0.90f, 0.90f, 0.40f, 0.60f, rethinkInterval: 45f, attackCooldown: 1.10f),
        };
    }

    private static TotemBotProfileCatalogEntry NewBotProfile(
        int botId,
        string type,
        string displayName,
        string personality,
        int preferredPreset,
        float visionRadius,
        float aggroRadius,
        int dodgeReactionMs,
        float confidence,
        float lootGreedFactor,
        float targetPlayerWeight,
        float targetHumanoidAiWeight,
        float targetResourceWeight,
        float riskTolerance,
        float rethinkInterval = 20f,
        float attackCooldown = 0.7f)
    {
        return new TotemBotProfileCatalogEntry
        {
            botId = botId,
            type = type,
            displayName = displayName,
            personality = personality,
            rethinkInterval = rethinkInterval,
            attackCooldown = attackCooldown,
            visionRadius = visionRadius,
            aggroRadius = aggroRadius,
            dodgeReactionMs = dodgeReactionMs,
            confidence = confidence,
            preferredPreset = preferredPreset,
            lootGreedFactor = lootGreedFactor,
            targetPlayerWeight = targetPlayerWeight,
            targetHumanoidAiWeight = targetHumanoidAiWeight,
            targetResourceWeight = targetResourceWeight,
            riskTolerance = riskTolerance,
        };
    }

    private static TotemBotBuildPresetCatalogEntry[] BuildDefaultBotBuildPresets()
    {
        return new[]
        {
            NewPreset(1, "Rush A", "Rush"),
            NewPreset(2, "Pivot A", "Pivot"),
            NewPreset(3, "Camp A", "Camp"),
            NewPreset(4, "Camp B", "Camp"),
            NewPreset(5, "Hybrid A", "Hybrid"),
            NewPreset(6, "Camp C", "Camp"),
            NewPreset(7, "Pivot B", "Pivot"),
        };
    }

    private static TotemBotBuildPresetCatalogEntry NewPreset(int presetId, string name, string behaviorMacro)
    {
        return new TotemBotBuildPresetCatalogEntry
        {
            presetId = presetId,
            name = name,
            behaviorMacro = behaviorMacro,
        };
    }
}

[Serializable]
public sealed class TotemGameplayCatalogGenerationInfo
{
    public string generatedBy = string.Empty;
    public string sourceRoot = string.Empty;
    public int sourceFileCount;
    public string sourceContentHash = string.Empty;
}

[Serializable]
public sealed class TotemMapTemplateCatalogEntry
{
    public int id;
    public string themeName;
    public float mapSize;
    public float minRoomSize;
    public string prefabPath;
    public string hudAccentColor;
    public string dominantColor;

    public TotemMapTemplateDefinition ToDefinition()
    {
        return new TotemMapTemplateDefinition
        {
            Id = id,
            ThemeName = themeName ?? string.Empty,
            MapSize = mapSize > 0f ? mapSize : TotemMapService.DefaultMapSize,
            MinRoomSize = minRoomSize > 0f ? minRoomSize : 15f,
            PrefabPath = prefabPath ?? string.Empty,
            HudAccentColor = hudAccentColor ?? string.Empty,
            DominantColor = dominantColor ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemZonePhaseCatalogEntry
{
    public int id;
    public string phaseName;
    public float startTime;
    public float duration;
    public float targetRadius;
    public float outZoneDamage;
    public string centerOffsetMode;

    public TotemZonePhase ToPhase()
    {
        return new TotemZonePhase
        {
            Id = id,
            PhaseName = phaseName,
            StartTime = startTime,
            Duration = duration,
            TargetRadius = targetRadius,
            OutZoneDamage = outZoneDamage,
            CenterOffsetMode = centerOffsetMode,
        };
    }
}

[Serializable]
public sealed class TotemAudioCueCatalogEntry
{
    public string cueId;
    public string kind;
    public string assetName;
    public float volume = 1f;
    public bool loop;
    public float minIntervalSec;
    public string usage;
    public string legacySource;

    public TotemAudioCueDefinition ToDefinition()
    {
        return new TotemAudioCueDefinition
        {
            CueId = cueId ?? string.Empty,
            Kind = TotemCatalogEnum.Parse(kind, TotemAudioCueKind.Unknown),
            AssetName = assetName ?? string.Empty,
            Volume = volume <= 0f ? 1f : Mathf.Clamp01(volume),
            Loop = loop,
            MinIntervalSec = Mathf.Max(0f, minIntervalSec),
            Usage = usage ?? string.Empty,
            LegacySource = legacySource ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemBotProfileCatalogEntry
{
    public int botId;
    public string type;
    public string displayName;
    public float rethinkInterval;
    public float attackCooldown;
    public float visionRadius;
    public float aggroRadius;
    public int dodgeReactionMs;
    public float confidence;
    public int preferredPreset;
    public float lootGreedFactor;
    public string personality;
    public float targetPlayerWeight;
    public float targetHumanoidAiWeight;
    public float targetResourceWeight;
    public float riskTolerance;

    public TotemBotProfileDefinition ToDefinition()
    {
        var resolvedPersonality = TotemCatalogEnum.Parse(personality, TotemAIPersonality.Hybrid);
        return new TotemBotProfileDefinition
        {
            BotId = botId,
            ActorKind = ParseBotKind(type),
            DisplayName = displayName,
            RethinkInterval = rethinkInterval,
            AttackCooldown = attackCooldown,
            VisionRadius = visionRadius,
            AggroRadius = aggroRadius,
            DodgeReactionSec = Mathf.Max(0f, dodgeReactionMs * 0.001f),
            Confidence = Mathf.Clamp01(confidence),
            PreferredPreset = preferredPreset,
            LootGreedFactor = lootGreedFactor,
            Personality = resolvedPersonality,
            TargetPlayerWeight = ResolveWeight(targetPlayerWeight, DefaultTargetPlayerWeight(resolvedPersonality)),
            TargetHumanoidAiWeight = ResolveWeight(targetHumanoidAiWeight, DefaultTargetHumanoidAiWeight(resolvedPersonality)),
            TargetResourceWeight = ResolveWeight(targetResourceWeight, DefaultTargetResourceWeight(resolvedPersonality)),
            RiskTolerance = ResolveWeight(riskTolerance, DefaultRiskTolerance(resolvedPersonality)),
        };
    }

    private static float ResolveWeight(float value, float fallback)
    {
        return value > 0f ? value : fallback;
    }

    private static float DefaultTargetPlayerWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.PlayerPriority:
                return 1.1f;
            case TotemAIPersonality.Conservative:
                return 0.45f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.35f;
            default:
                return 0.9f;
        }
    }

    private static float DefaultTargetHumanoidAiWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.PlayerPriority:
                return 1.1f;
            case TotemAIPersonality.Conservative:
                return 0.35f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.25f;
            default:
                return 0.9f;
        }
    }

    private static float DefaultTargetResourceWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.ResourceAcquisition:
                return 1.6f;
            case TotemAIPersonality.Conservative:
                return 0.25f;
            default:
                return 0.4f;
        }
    }

    private static float DefaultRiskTolerance(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.Aggressive:
            case TotemAIPersonality.PlayerPriority:
                return 0.85f;
            case TotemAIPersonality.Conservative:
                return 0.35f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.55f;
            default:
                return 0.6f;
        }
    }

    private static TotemActorKind ParseBotKind(string value)
    {
        if (string.Equals(value, "Smart", StringComparison.OrdinalIgnoreCase))
        {
            return TotemActorKind.SmartAi;
        }

        if (string.Equals(value, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return TotemActorKind.LightAi;
        }

        return TotemCatalogEnum.Parse(value, TotemActorKind.LightAi);
    }
}

[Serializable]
public sealed class TotemBotProfileDefinition
{
    public int BotId;
    public TotemActorKind ActorKind;
    public string DisplayName;
    public float RethinkInterval;
    public float AttackCooldown;
    public float VisionRadius;
    public float AggroRadius;
    public float DodgeReactionSec;
    public float Confidence;
    public int PreferredPreset;
    public float LootGreedFactor;
    public TotemAIPersonality Personality;
    public float TargetPlayerWeight;
    public float TargetHumanoidAiWeight;
    public float TargetResourceWeight;
    public float RiskTolerance;
}

[Serializable]
public sealed class TotemBotBuildPresetCatalogEntry
{
    public int presetId;
    public string name;
    public string behaviorMacro;

    public TotemBotBuildPresetDefinition ToDefinition()
    {
        return new TotemBotBuildPresetDefinition
        {
            PresetId = presetId,
            Name = name,
            BehaviorMacro = TotemCatalogEnum.Parse(behaviorMacro, TotemAIBehaviorMacro.Hybrid),
        };
    }
}

[Serializable]
public sealed class TotemBotBuildPresetDefinition
{
    public int PresetId;
    public string Name;
    public TotemAIBehaviorMacro BehaviorMacro;
}

[Serializable]
public sealed class TotemAITuningDefinition
{
    public float lodRadius;
    public float lodScanInterval;
    public float smartColdInterval;
    public float lightHotInterval;
    public float lightColdInterval;
    public float smartAttackRange;
    public float lightAttackRange;
    public float smartMoveSpeed;
    public float lightMoveSpeed;
    public float smartAttackCooldown;
    public float lightAttackCooldown;
    public float smartVisionRadius;
    public float lightVisionRadius;
    public float smartSkillRadius;
    public float smartSkillCooldown;
    public float dodgeReactionSec;
    public float smartDamage;
    public float lightDamage;

    public static TotemAITuningDefinition Default => new TotemAITuningDefinition
    {
        lodRadius = TotemAIService.LodRadius,
        lodScanInterval = TotemAIService.LodScanInterval,
        smartColdInterval = TotemAIService.SmartColdInterval,
        lightHotInterval = TotemAIService.LightHotInterval,
        lightColdInterval = TotemAIService.LightColdInterval,
        smartAttackRange = TotemAIService.SmartAttackRange,
        lightAttackRange = TotemAIService.LightAttackRange,
        smartMoveSpeed = 4.2f,
        lightMoveSpeed = 2.4f,
        smartAttackCooldown = 1.0f,
        lightAttackCooldown = 1.5f,
        smartVisionRadius = 22f,
        lightVisionRadius = 16f,
        smartSkillRadius = 8f,
        smartSkillCooldown = 3f,
        dodgeReactionSec = 0.18f,
        smartDamage = 8f,
        lightDamage = 5f,
    };
}

public static class TotemGameplayCatalogValidator
{
    public static bool Validate(TotemGameplayCatalog catalog, IList<string> errors)
    {
        if (catalog == null)
        {
            errors?.Add("Catalog is null.");
            return false;
        }

        catalog.Normalize();
        Require(catalog.schemaVersion > 0, errors, "schemaVersion must be positive.");
        Require(catalog.mapTemplates.Length == 1, errors, "First playable must define exactly one map template.");
        Require(MapTemplatesAreValid(catalog), errors, "Map templates must define ids, theme names, map size and colors.");
        Require(MapResourcePickupsAreValid(catalog), errors, "Map resource pickups must define unique enabled pigment types with valid amount ranges, weights and rounds.");
        Require(catalog.zonePhases.Length == 4, errors, "Zone phase count must be 4.");
        Require(ZonePhasesAreValid(catalog), errors, "Zone phases must preserve tuned ZoneShrinkConfig timing, radii, damage and offset modes.");
        Require(catalog.audioCues.Length >= 7, errors, "At least 7 first-playable audio cue rows are required.");
        Require(AudioCuesAreValid(catalog), errors, "Audio cues must define BGM/SFX ids and GF_X asset names.");
        Require(CountBotProfiles(catalog, "Smart") >= 4, errors, "At least 4 Smart bot profiles are required.");
        Require(CountBotProfiles(catalog, "Light") >= 2, errors, "At least 2 Light bot profiles are required.");
        Require(catalog.botBuildPresets.Length >= 7, errors, "At least 7 bot build presets are required.");
        Require(BotProfilesHaveValidPresets(catalog), errors, "Bot profiles must reference existing build presets.");
        Require(BotProfilePersonalitiesAreValid(catalog), errors, "Smart bot profiles must preserve the pure-PVP personality distribution and target weights.");
        Require(catalog.aiTuning != null && catalog.aiTuning.lodRadius > 0f, errors, "AI tuning lodRadius must be positive.");

        return errors == null || errors.Count <= 0;
    }

    private static void Require(bool condition, IList<string> errors, string message)
    {
        if (!condition)
        {
            errors?.Add(message);
        }
    }

    private static bool AllDelimitedIdsExist(string value, HashSet<string> ids)
    {
        var parsed = new HashSet<string>(StringComparer.Ordinal);
        AddDelimitedIds(value, parsed);
        if (parsed.Count == 0) return false;
        foreach (string id in parsed)
        {
            if (!ids.Contains(id)) return false;
        }
        return true;
    }

    private static void AddDelimitedIds(string value, HashSet<string> target)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string[] parts = value.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string id = parts[i].Trim();
            if (!string.IsNullOrWhiteSpace(id)) target.Add(id);
        }
    }

    private static bool ZonePhasesAreValid(TotemGameplayCatalog catalog)
    {
        if (catalog.zonePhases == null || catalog.zonePhases.Length != 4)
        {
            return false;
        }

        return ZonePhaseMatches(catalog.zonePhases, 0, "Shrink1", 0f, 30f, 110f, 2f, "None") &&
            ZonePhaseMatches(catalog.zonePhases, 1, "Shrink2", 0f, 30f, 75f, 3f, "None") &&
            ZonePhaseMatches(catalog.zonePhases, 2, "Shrink3", 0f, 30f, 50f, 5f, "None") &&
            ZonePhaseMatches(catalog.zonePhases, 3, "Shrink4", 0f, 30f, 35f, 8f, "None");
    }

    private static bool ZonePhaseMatches(
        TotemZonePhaseCatalogEntry[] phases,
        int index,
        string phaseName,
        float startTime,
        float duration,
        float targetRadius,
        float outZoneDamage,
        string centerOffsetMode)
    {
        var phase = phases[index];
        return phase != null &&
            phase.id == index &&
            string.Equals(phase.phaseName, phaseName, StringComparison.Ordinal) &&
            Mathf.Abs(phase.startTime - startTime) <= 0.001f &&
            Mathf.Abs(phase.duration - duration) <= 0.001f &&
            Mathf.Abs(phase.targetRadius - targetRadius) <= 0.001f &&
            Mathf.Abs(phase.outZoneDamage - outZoneDamage) <= 0.001f &&
            string.Equals(phase.centerOffsetMode, centerOffsetMode, StringComparison.Ordinal);
    }


    private static bool AudioCuesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool hasMenuBgm = false;
        bool hasInGameBgm = false;
        bool hasKill = false;
        bool hasPlayerDied = false;
        for (int i = 0; i < catalog.audioCues.Length; i++)
        {
            var cue = catalog.audioCues[i];
            if (cue == null || string.IsNullOrWhiteSpace(cue.cueId) || !ids.Add(cue.cueId) ||
                string.IsNullOrWhiteSpace(cue.assetName) || cue.volume <= 0f || cue.volume > 1f ||
                cue.minIntervalSec < 0f)
            {
                return false;
            }

            var definition = cue.ToDefinition();
            if (definition.Kind == TotemAudioCueKind.Unknown)
            {
                return false;
            }

            if (definition.Kind == TotemAudioCueKind.Bgm && !definition.Loop)
            {
                return false;
            }

            hasMenuBgm |= string.Equals(cue.cueId, "bgm_main_menu", StringComparison.Ordinal);
            hasInGameBgm |= string.Equals(cue.cueId, "bgm_in_game", StringComparison.Ordinal);
            hasKill |= string.Equals(cue.cueId, "sfx_kill", StringComparison.Ordinal);
            hasPlayerDied |= string.Equals(cue.cueId, "sfx_player_died", StringComparison.Ordinal);
        }

        if (!hasMenuBgm || !hasInGameBgm || !hasKill || !hasPlayerDied)
        {
            return false;
        }

        return true;
    }

    private static bool MapTemplatesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        bool hasOasisCity = false;
        for (int i = 0; i < catalog.mapTemplates.Length; i++)
        {
            var row = catalog.mapTemplates[i];
            if (row == null || row.id <= 0 || !ids.Add(row.id) ||
                string.IsNullOrWhiteSpace(row.themeName) || Mathf.Abs(row.mapSize - TotemMapService.DefaultMapSize) > 0.001f ||
                row.minRoomSize <= 0f ||
                string.IsNullOrWhiteSpace(row.hudAccentColor) || string.IsNullOrWhiteSpace(row.dominantColor))
            {
                return false;
            }

            hasOasisCity |= string.Equals(row.themeName, "OASIS_CITY", StringComparison.Ordinal)
                && string.Equals(row.prefabPath, "Assets/Game/Scene/OasisCity.unity", StringComparison.Ordinal);
        }

        return hasOasisCity;
    }

    private static bool MapResourcePickupsAreValid(TotemGameplayCatalog catalog)
    {
        TotemMapResourcePickupDefinition[] definitions = catalog.CreateMapResourcePickupDefinitions();
        if (!TotemMapResourceGenerator.ValidateDefinitions(definitions, out _))
        {
            return false;
        }

        bool hasFire = false;
        bool hasIce = false;
        bool hasLightning = false;
        for (int i = 0; i < definitions.Length; i++)
        {
            switch (definitions[i].Pigment)
            {
                case TotemPigmentKind.Fire: hasFire = true; break;
                case TotemPigmentKind.Ice: hasIce = true; break;
                case TotemPigmentKind.Lightning: hasLightning = true; break;
            }
        }

        return hasFire && hasIce && hasLightning;
    }

    private static int CountBotProfiles(TotemGameplayCatalog catalog, string profileType)
    {
        int count = 0;
        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            if (string.Equals(catalog.botProfiles[i].type, profileType, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static bool BotProfilesHaveValidPresets(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            int presetId = catalog.botProfiles[i].preferredPreset;
            bool found = false;
            for (int presetIndex = 0; presetIndex < catalog.botBuildPresets.Length; presetIndex++)
            {
                if (catalog.botBuildPresets[presetIndex].presetId == presetId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool BotProfilePersonalitiesAreValid(TotemGameplayCatalog catalog)
    {
        int aggressive = 0;
        int conservative = 0;
        int resource = 0;
        int player = 0;
        bool playerWeightsValid = false;
        bool resourceWeightsValid = false;

        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            var row = catalog.botProfiles[i];
            if (row == null || !string.Equals(row.type, "Smart", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var profile = row.ToDefinition();
            switch (profile.Personality)
            {
                case TotemAIPersonality.Aggressive:
                    aggressive++;
                    break;
                case TotemAIPersonality.Conservative:
                    conservative++;
                    break;
                case TotemAIPersonality.ResourceAcquisition:
                    resource++;
                    resourceWeightsValid |= profile.TargetResourceWeight > profile.TargetPlayerWeight;
                    break;
                case TotemAIPersonality.PlayerPriority:
                    player++;
                    playerWeightsValid |= Mathf.Abs(profile.TargetPlayerWeight - profile.TargetHumanoidAiWeight) <= 0.001f;
                    break;
            }
        }

        return aggressive >= 1 &&
               conservative >= 1 &&
               resource >= 1 &&
               player >= 1 &&
               playerWeightsValid &&
               resourceWeightsValid;
    }
}

public static class TotemCatalogEnum
{
    public static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out TEnum parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
