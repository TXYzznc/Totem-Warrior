using System;
using System.Collections.Generic;

public static class TotemFirstPlayableArtHandoff
{
    public const string ChangeId = "rebaseline-pvpve-art-resources";
    public const string UiDeliveryId = "ART-UI-FP-001";
    public const string VfxDeliveryId = "ART-CMB-VFX-001";
    public const string WeaponKey = "weapon.rifle.patrol.v1";

    public static readonly string[] FormIds =
    {
        "UI-FP-MAIN-001",
        "UI-FP-MATCH-001",
        "UI-FP-ARCH-001",
        "UI-FP-HELP-001",
        "UI-FP-SET-001",
        "UI-FP-CREDIT-001",
        "UI-FP-EXIT-001",
        "UI-FP-HUD-001",
        "UI-FP-BUILD-001",
        "UI-FP-INTEL-001",
        "UI-FP-REQ-001",
        "UI-FP-DOWN-001",
        "UI-FP-SPEC-001",
        "UI-FP-RESULT-001",
    };

    public static class Anchors
    {
        public const string Muzzle = "Socket_Muzzle";
        public const string ElementRail = "Socket_ElementRail";
        public const string TargetCore = "VFX_TargetCore";
        public const string StatusTop = "VFX_StatusTop";
        public const string TattooSlotPrefix = "VFX_TattooSlot_";
    }

    public static class VfxKeys
    {
        public const string RifleMuzzle = "vfx.weapon.rifle.muzzle";
        public const string RifleHitBody = "vfx.weapon.rifle.hit_body";
        public const string RifleHitWeakpoint = "vfx.weapon.rifle.hit_weakpoint";
        public const string HeatShock = "vfx.reaction.heat_shock";
        public const string Overload = "vfx.reaction.overload";
        public const string Stasis = "vfx.reaction.stasis";
        public const string QueueP01 = "vfx.queue.p01";
        public const string QueueP02 = "vfx.queue.p02";
        public const string QueueWeakpoint = "vfx.queue.weakpoint";
        public const string QueueRifleArm = "vfx.queue.rifle_arm";
        public const string QueueTorso = "vfx.queue.torso";
    }

    public static class FallbackKeys
    {
        public const string MissingSprite = "fallback.sprite.geometric_labeled";
        public const string MissingMaterial = "fallback.material.neutral_unlit";
        public const string MissingVfx = "fallback.vfx.resolved_pulse";
        public const string MissingPortrait = "fallback.portrait.neutral_silhouette";
    }
}

public enum TotemPresentationAssetKind : byte
{
    Sprite = 0,
    Material = 1,
    Vfx = 2,
    Prefab = 3,
}

[Serializable]
public sealed class TotemPresentationAssetContract
{
    public string stableId = string.Empty;
    public TotemPresentationAssetKind kind;
    public string assetKey = string.Empty;
    public string fallbackKey = string.Empty;
    public string handoffId = string.Empty;
}

[Serializable]
public sealed class TotemEffectPriorityConfig
{
    public int activeSkillArm = TotemEffectPriority.ActiveSkillArm;
    public int dodge = TotemEffectPriority.Dodge;
    public int move = TotemEffectPriority.Move;
    public int weakpoint = TotemEffectPriority.Weakpoint;
    public int rifleArm = TotemEffectPriority.RifleArm;
    public int torso = TotemEffectPriority.Torso;
}

public enum TotemFirstPlayablePatternBehavior : byte
{
    SingleTargetFocus = 1,
    NeighborSpread = 2,
}

[Serializable]
public sealed class TotemFirstPlayableTattooPatternConfig
{
    public TotemFirstPlayablePatternId pattern;
    public TotemFirstPlayablePatternBehavior behavior;
    public bool initiallyAvailable = true;
    public string publicEffectText = string.Empty;
}

[Serializable]
public sealed class TotemFirstPlayableTattooBuildConfig
{
    public const string P01PublicEffectText = "命中后聚焦单个目标的效果。";
    public const string P02PublicEffectText = "命中后向邻近目标扩散的效果。";

    public int slotCount = TotemFirstPlayableTattooBuildState.SlotCount;
    public int equipPigmentCost = TotemFirstPlayableTattooBuildState.EquipPigmentCost;
    public int removePigmentRefund = TotemFirstPlayableTattooBuildState.RemovePigmentRefund;
    public TotemFirstPlayableElement[] elements =
    {
        TotemFirstPlayableElement.Fire,
        TotemFirstPlayableElement.Ice,
        TotemFirstPlayableElement.Lightning,
    };
    public TotemFirstPlayableTattooPatternConfig[] patterns =
    {
        new TotemFirstPlayableTattooPatternConfig
        {
            pattern = TotemFirstPlayablePatternId.P01,
            behavior = TotemFirstPlayablePatternBehavior.SingleTargetFocus,
            publicEffectText = P01PublicEffectText,
        },
        new TotemFirstPlayableTattooPatternConfig
        {
            pattern = TotemFirstPlayablePatternId.P02,
            behavior = TotemFirstPlayablePatternBehavior.NeighborSpread,
            publicEffectText = P02PublicEffectText,
        },
    };
}

[Serializable]
public sealed class TotemFirstPlayableContractConfig
{
    public int schemaVersion = 1;
    public int participantCount = TotemFirstPlayableRules.ParticipantCount;
    public int teamCount = TotemFirstPlayableRules.TeamCount;
    public int teamSize = TotemFirstPlayableRules.TeamSize;
    public string artChangeId = TotemFirstPlayableArtHandoff.ChangeId;
    public string weaponKey = TotemFirstPlayableArtHandoff.WeaponKey;
    public TotemMatchTimingConfig timing = new TotemMatchTimingConfig();
    public TotemEffectPriorityConfig effectPriorities = new TotemEffectPriorityConfig();
    public TotemFirstPlayableTattooBuildConfig tattooBuild = new TotemFirstPlayableTattooBuildConfig();
    public TotemPresentationAssetContract[] assets = Array.Empty<TotemPresentationAssetContract>();
}

public static class TotemFirstPlayableContractValidator
{
    public static bool Validate(TotemFirstPlayableContractConfig config, IList<string> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (config == null)
        {
            errors.Add("FirstPlayable contract config is null.");
            return false;
        }

        Require(config.schemaVersion > 0, "schemaVersion must be positive.", errors);
        Require(config.participantCount == TotemFirstPlayableRules.ParticipantCount, "participantCount must be 6.", errors);
        Require(config.teamCount == TotemFirstPlayableRules.TeamCount, "teamCount must be 3.", errors);
        Require(config.teamSize == TotemFirstPlayableRules.TeamSize, "teamSize must be 2.", errors);
        Require(config.teamCount * config.teamSize == config.participantCount, "team capacity must equal participantCount.", errors);
        Require(config.artChangeId == TotemFirstPlayableArtHandoff.ChangeId, "artChangeId must reference the independent art change.", errors);
        Require(!string.IsNullOrWhiteSpace(config.weaponKey), "weaponKey is required.", errors);

        if (config.timing == null)
        {
            errors.Add("timing is required.");
        }
        else
        {
            Require(config.timing.openingBuildSeconds > 0, "openingBuildSeconds must be positive.", errors);
            Require(config.timing.laterBuildSeconds > 0, "laterBuildSeconds must be positive.", errors);
            Require(config.timing.normalCombatSeconds > 0, "normalCombatSeconds must be positive.", errors);
            Require(config.timing.normalShrinkSeconds > 0, "normalShrinkSeconds must be positive.", errors);
            Require(config.timing.fastCombatSeconds > 0, "fastCombatSeconds must be positive.", errors);
            Require(config.timing.fastShrinkSeconds > 0, "fastShrinkSeconds must be positive.", errors);
        }

        if (config.effectPriorities == null)
        {
            errors.Add("effectPriorities is required.");
        }
        else
        {
            Require(config.effectPriorities.activeSkillArm > config.effectPriorities.dodge, "active skill priority must be above dodge.", errors);
            Require(config.effectPriorities.dodge > config.effectPriorities.move, "dodge priority must be above move.", errors);
            Require(config.effectPriorities.move > config.effectPriorities.weakpoint, "move priority must be above weakpoint.", errors);
            Require(config.effectPriorities.weakpoint > config.effectPriorities.rifleArm, "weakpoint priority must be above rifle arm.", errors);
            Require(config.effectPriorities.rifleArm > config.effectPriorities.torso, "rifle arm priority must be above torso.", errors);
        }

        ValidateTattooBuild(config.tattooBuild, errors);

        ValidateAssets(config.assets, errors);
        return errors.Count == 0;
    }

    private static void ValidateTattooBuild(TotemFirstPlayableTattooBuildConfig tattoo, IList<string> errors)
    {
        if (tattoo == null)
        {
            errors.Add("tattooBuild is required.");
            return;
        }

        Require(tattoo.slotCount == TotemFirstPlayableTattooBuildState.SlotCount, "tattooBuild.slotCount must be 6.", errors);
        Require(tattoo.equipPigmentCost == TotemFirstPlayableTattooBuildState.EquipPigmentCost, "tattooBuild.equipPigmentCost must be 10.", errors);
        Require(tattoo.removePigmentRefund == TotemFirstPlayableTattooBuildState.RemovePigmentRefund, "tattooBuild.removePigmentRefund must be 6.", errors);

        if (tattoo.elements == null || tattoo.elements.Length != 3)
        {
            errors.Add("tattooBuild.elements must contain exactly Fire, Ice and Lightning.");
        }
        else
        {
            bool fire = false;
            bool ice = false;
            bool lightning = false;
            for (int i = 0; i < tattoo.elements.Length; i++)
            {
                fire |= tattoo.elements[i] == TotemFirstPlayableElement.Fire;
                ice |= tattoo.elements[i] == TotemFirstPlayableElement.Ice;
                lightning |= tattoo.elements[i] == TotemFirstPlayableElement.Lightning;
            }

            Require(fire && ice && lightning, "tattooBuild.elements must contain exactly Fire, Ice and Lightning.", errors);
        }

        if (tattoo.patterns == null || tattoo.patterns.Length != 2)
        {
            errors.Add("tattooBuild.patterns must contain exactly P01 and P02.");
            return;
        }

        bool p01 = false;
        bool p02 = false;
        for (int i = 0; i < tattoo.patterns.Length; i++)
        {
            TotemFirstPlayableTattooPatternConfig pattern = tattoo.patterns[i];
            if (pattern == null)
            {
                errors.Add($"tattooBuild.patterns[{i}] is null.");
                continue;
            }

            Require(pattern.initiallyAvailable, $"tattooBuild.patterns[{i}] must be initially available.", errors);
            Require(!string.IsNullOrWhiteSpace(pattern.publicEffectText), $"tattooBuild.patterns[{i}].publicEffectText is required.", errors);
            if (pattern.pattern == TotemFirstPlayablePatternId.P01)
            {
                p01 = true;
                Require(pattern.behavior == TotemFirstPlayablePatternBehavior.SingleTargetFocus, "P01 must use SingleTargetFocus behavior.", errors);
            }
            else if (pattern.pattern == TotemFirstPlayablePatternId.P02)
            {
                p02 = true;
                Require(pattern.behavior == TotemFirstPlayablePatternBehavior.NeighborSpread, "P02 must use NeighborSpread behavior.", errors);
            }
            else
            {
                errors.Add($"tattooBuild.patterns[{i}] must be P01 or P02.");
            }
        }

        Require(p01 && p02, "tattooBuild.patterns must contain exactly P01 and P02.", errors);
    }

    private static void ValidateAssets(TotemPresentationAssetContract[] assets, IList<string> errors)
    {
        if (assets == null)
        {
            errors.Add("assets cannot be null.");
            return;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            var asset = assets[i];
            if (asset == null)
            {
                errors.Add($"assets[{i}] is null.");
                continue;
            }

            Require(!string.IsNullOrWhiteSpace(asset.stableId), $"assets[{i}].stableId is required.", errors);
            Require(!string.IsNullOrWhiteSpace(asset.assetKey), $"assets[{i}].assetKey is required.", errors);
            Require(!string.IsNullOrWhiteSpace(asset.fallbackKey), $"assets[{i}].fallbackKey is required.", errors);
            Require(!string.IsNullOrWhiteSpace(asset.handoffId), $"assets[{i}].handoffId is required.", errors);
            for (int j = 0; j < i; j++)
            {
                if (assets[j] != null && assets[j].stableId == asset.stableId)
                {
                    errors.Add($"Duplicate presentation asset stableId '{asset.stableId}'.");
                    break;
                }
            }
        }
    }

    private static void Require(bool condition, string error, IList<string> errors)
    {
        if (!condition)
        {
            errors.Add(error);
        }
    }
}
