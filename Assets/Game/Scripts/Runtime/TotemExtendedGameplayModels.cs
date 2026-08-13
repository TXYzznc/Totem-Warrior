using System;
using UnityEngine;

public enum TotemAIState
{
    Idle = 0,
    Wander = 1,
    Chase = 2,
    Attack = 3,
    Retreat = 4,
    Dead = 5,
    Loot = 6,
}

public enum TotemAILodBucket
{
    Hot = 0,
    Cold = 1,
}

public enum TotemAIBehaviorMacro
{
    Rush = 0,
    Camp = 1,
    Pivot = 2,
    Hybrid = 3,
}

public enum TotemAIPersonality
{
    Hybrid = 0,
    Aggressive = 1,
    Conservative = 2,
    ResourceAcquisition = 3,
    PlayerPriority = 4,
}

public sealed class TotemWeaponDefinition
{
    public string WeaponId;
    public string DisplayName;
    public float BaseDamage;
    public float Cooldown;
    public float Range;
    public float AimSpreadHalfDegrees;
}

public sealed class TotemWeaponState
{
    public TotemWeaponDefinition Weapon;
    public int Level = 1;
    public float CooldownRemaining;
    public uint FireSequence;
}

public struct TotemWeaponMultipliers
{
    public float DamageMul;
    public float RangeAdd;
    public float CooldownMul;

    public static TotemWeaponMultipliers Identity => new TotemWeaponMultipliers
    {
        DamageMul = 1f,
        RangeAdd = 0f,
        CooldownMul = 1f,
    };
}

public sealed class TotemStatusInstance
{
    public TotemActorModel Target;
    public TotemCombatantModel Source;
    public string StatusName;
    public string SourceReason;
    public float DPS;
    public float RemainingSec;
    public float TickAccumulator;
}

public sealed class TotemStatusSnapshot
{
    public int actorId;
    public int activeCount;
    public int appliedCount;
    public int expiredCount;
    public int tickDamageCount;
    public float totalDps;
    public string summary;
    public string lastStatusName;
    public string lastExpiredStatusName;
    public string[] statusNames = Array.Empty<string>();
    public float[] remainingSeconds = Array.Empty<float>();
}

public sealed class TotemZonePhase
{
    public int Id;
    public string PhaseName;
    public float StartTime;
    public float Duration;
    public float TargetRadius;
    public float OutZoneDamage;
    public string CenterOffsetMode;
}

public sealed class TotemZoneSnapshot
{
    public bool active;
    public float elapsedSec;
    public int currentPhaseId;
    public float currentRadius;
    public float outZoneDamage;
    public int outZoneAffectedActorCount;
    public int outZoneKilledActorCount;
    public float lastOutZoneDamageTick;
    public float totalOutZoneDamage;
}

[Serializable]
public sealed class TotemSettingsSnapshot
{
    public float bgmVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public int qualityLevel = 1;
    public bool editing;
}

public sealed class TotemRunResultSnapshot
{
    public bool win;
    public bool draw;
    public bool extracted;
    public string reason;
    public int killCount;
    public float playerHealth;
    public int aliveParticipantCount;
    public int winnerParticipantId;
    public int winnerTeamId;
    public float elapsedSec;
    public TotemRunStatsSnapshot cumulativeStats;
}

[Serializable]
public sealed class TotemRunStatsSnapshot
{
    public int totalRuns;
    public int totalWins;
    public int totalLosses;
    public int totalKills;
    public float totalPlayTimeSec;
    public int bestKills;
    public float bestWinTimeSec;
    public string lastResultReason;
    public string lastSavedUtc;
}

[Serializable]
public sealed class TotemPatternUnlockSnapshot
{
    public string patternId;
    public bool[] slots = Array.Empty<bool>();
}

[Serializable]
public sealed class TotemMetaProgressSnapshot
{
    public bool[] characterSlots = Array.Empty<bool>();
    public TotemPatternUnlockSnapshot[] patternUnlocks = Array.Empty<TotemPatternUnlockSnapshot>();
    public string[] unlockedDecorations = Array.Empty<string>();
    public string[] unlockedTitles = Array.Empty<string>();
    public string[] unlockedGallery = Array.Empty<string>();
    public string[] completedAchievements = Array.Empty<string>();
    public string lastSavedUtc;
}

public sealed class TotemInteractionSnapshot
{
    public bool hasMapResourcePickup;
    public int mapResourcePickupInstanceId;
    public string mapResourcePickupId;
    public int mapResourcePickupAmount;
    public string prompt;
    public string lastInteraction;
}

public sealed class TotemAIActorState
{
    public TotemActorModel Actor;
    public TotemAIState State;
    public TotemAILodBucket Bucket;
    public TotemBotProfileDefinition Profile;
    public TotemBotBuildPresetDefinition BuildPreset;
    public Vector3 WanderDirection = Vector3.forward;
    public float NextDecisionTime;
    public float AttackCooldownRemaining;
    public float DodgeCooldownRemaining;
    public float LastDamagedElapsed = 999f;
    public float SafetyScore = 1f;
    public int Decisions;
    public int Attacks;
    public TotemMapResourcePickup ResourcePickupTarget;
    public int ResourcePickupClaims;
    public TotemAIDecisionRecord LastDecision = new TotemAIDecisionRecord();
}

public sealed class TotemAISnapshot
{
    public bool active;
    public bool playerStartupTargetSuppressed;
    public int smartCount;
    public int lightCount;
    public int hotCount;
    public int coldCount;
    public int chaseCount;
    public int attackCount;
    public int wanderCount;
    public int totalDecisions;
    public int totalAttacks;
    public int lootCount;
    public int totalResourcePickupClaims;
    public int profiledCount;
    public int smartProfileCount;
    public int lightProfileCount;
    public int lastDecisionSequence;
    public int lastDecisionActorId;
    public string lastDecisionActorName;
    public TotemActorKind lastDecisionActorKind;
    public TotemAIState lastDecisionState;
    public TotemAILodBucket lastDecisionBucket;
    public string lastDecisionAction;
    public string lastDecisionReason;
    public int lastDecisionTargetActorId;
    public string lastDecisionTargetName;
    public TotemActorKind lastDecisionTargetKind;
    public TotemCombatantDomain lastDecisionTargetDomain;
    public float lastDecisionDistance;
    public float lastDecisionSafetyScore;
    public int lastDecisionProfileBotId;
    public int lastDecisionBuildPresetId;
    public string lastDecisionWeaponId;
    public TotemAIPersonality lastDecisionPersonality;
}

public sealed class TotemAIDecisionRecord
{
    public int Sequence;
    public float ElapsedSec;
    public int ActorId;
    public string ActorName;
    public TotemActorKind ActorKind;
    public TotemAIState State;
    public TotemAILodBucket Bucket;
    public string Action;
    public string Reason;
    public int TargetActorId;
    public string TargetName;
    public TotemActorKind TargetKind;
    public TotemCombatantDomain TargetDomain;
    public float Distance;
    public float ActorHealth;
    public float TargetHealth;
    public float SafetyScore;
    public int ProfileBotId;
    public int BuildPresetId;
    public string WeaponId;
    public TotemAIPersonality Personality;
}
