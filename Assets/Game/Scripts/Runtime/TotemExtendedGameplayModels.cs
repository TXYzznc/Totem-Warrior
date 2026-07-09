using System;
using UnityEngine;

public enum TotemTattooPart
{
    Head = 1,
    Torso = 2,
    LeftArm = 3,
    RightArm = 4,
    LeftLeg = 5,
    RightLeg = 6,
}

public enum TotemTattooElement
{
    Fire = 1,
    Lightning = 2,
    Nature = 3,
    Frost = 4,
    Mutation = 5,
    Holy = 6,
    Pure = 7,
}

public enum TotemTattooShape
{
    SingleHit = 1,
    AOEBurst = 2,
    StackingMark = 3,
    MultiHit = 4,
    ChainJump = 5,
    ProbBurst = 6,
    TrailZone = 7,
    SummonForm = 8,
}

public enum TotemTattooEnchantAffixType
{
    Unknown = 0,
    ElementDamageBonus = 1,
    AttackSpeed = 2,
    CritChance = 3,
    CooldownReduction = 4,
    SelfHealOnHit = 5,
    StatusChance = 6,
    RangeBonus = 7,
    CritDamage = 8,
}

public enum TotemWeaponClass
{
    Melee = 0,
    Ranged = 1,
    Special = 2,
}

public enum TotemWeaponTraitEffectType
{
    Unknown = 0,
    Status = 1,
    Pierce = 2,
    Stun = 3,
    Chain = 4,
    Explosive = 5,
    MultiShot = 6,
    Pull = 7,
    Quick = 8,
}

public enum TotemSkillPhase
{
    Idle = 0,
    Startup = 1,
    Active = 2,
    Recovery = 3,
}

public enum TotemSkillChargeModel
{
    Cooldown = 0,
    Charges = 1,
    HoldRelease = 2,
}

public enum TotemSkillHitShape
{
    Single = 0,
    Circle = 1,
    Line = 2,
    Cone = 3,
}

public enum TotemNpcType
{
    Tattooist = 0,
    Merchant = 1,
}

public enum TotemChoiceEffectType
{
    WeaponUpgrade = 0,
    CoinReward = 1,
    StatusCleanse = 2,
    TattooBonus = 3,
    SkillRefresh = 4,
    SkillAcquire = 5,
    Heal = 6,
    RecipeUnlock = 7,
}

public enum TotemChoiceOptionType
{
    Unknown = 0,
    TattooRecipe = 1,
    PatternRecipe = 2,
    WeaponUpgrade = 3,
    SkillUpgrade = 4,
    SkillAcquire = 5,
    CoinBonus = 6,
    Heal = 7,
    OneTimeScroll = 8,
}

public enum TotemChoiceRuntimeState
{
    Idle = 0,
    Showing = 1,
    Resolved = 2,
    Timeout = 3,
    Closed = 4,
}

public enum TotemGameplayEventType
{
    Unknown = 0,
    Combat = 1,
    Choice = 2,
    Puzzle = 3,
    Merchant = 4,
    Boss = 5,
    Lore = 6,
    Curse = 7,
}

public enum TotemShopRewardType
{
    Unknown = 0,
    Ink = 1,
    WeaponUpgrade = 2,
    SkillCore = 3,
    StatusCleanse = 4,
}

public enum TotemChestRewardType
{
    Unknown = 0,
    Weapon = 1,
    Gold = 2,
    Potion = 3,
}

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
    BossPriority = 4,
    PlayerPriority = 5,
}

public sealed class TotemTattooDefinition
{
    public int PartId;
    public string PartName;
    public string TriggerEvent;
    public string ScaleStat;
    public string SymmetryGroup;
    public float ScaleFactor;
    public string PassiveDimension;
    public int ColorId;
    public string ColorName;
    public TotemTattooElement Element;
    public float ColorMultiplier;
    public float ElementBaseMultiplier;
    public float ElementParam1;
    public float ElementParam2;
    public float ElementParam3;
    public int PatternId;
    public string PatternName;
    public TotemTattooShape Shape;
    public float PatternMultiplier;
    public float ShapeParam1;
    public float ShapeParam2;
    public float ShapeParam3;
    public float Magnitude;
}

public sealed class TotemTattooEffectResult
{
    public TotemTattooDefinition Definition;
    public TotemActorModel Source;
    public TotemActorModel Target;
    public bool IsCritical;
    public float BaseDamage;
    public float Damage;
    public int HitCount;
    public string StatusName;
    public string Note;
    public float SourceHeal;
    public int StackCount;
    public int StackThreshold;
    public bool BurstTriggered;
    public float CritChance;
    public float CritMultiplier;
    public float CritRoll;
    public float PassiveCritRateBonus;
    public float PassiveElementBonus;
    public bool StatusApplied;
    public float StatusChance;
    public float StatusChanceBonus;
    public float StatusRoll;
}

public sealed class TotemTattooReadingTimeDefinition
{
    public int PartId;
    public string PartName;
    public float DurationSec;
}

public sealed class TotemTattooElementDefinition
{
    public int Id;
    public TotemTattooElement Element;
    public string Name;
    public float BaseMultiplier;
    public float Param1;
    public float Param2;
    public float Param3;
}

public sealed class TotemTattooShapeDefinition
{
    public int Id;
    public TotemTattooShape Shape;
    public string Name;
    public float Param1;
    public float Param2;
    public float Param3;
}

public sealed class TotemTattooEnchantAffixDefinition
{
    public int Id;
    public int PartId;
    public string ColorTier;
    public TotemTattooEnchantAffixType AffixType;
    public string StatKey;
    public float Value;
    public string ConditionKey;
    public float ConditionVal;
    public string DisplayText;
    public float Weight;
}

public sealed class TotemTattooEnchantRecipeDefinition
{
    public int Id;
    public string ColorTier;
    public int CoinCost;
    public int RarePigmentCost;
    public int MaxAffixPerSlot;
}

public sealed class TotemTattooSnapshot
{
    public int catalogCombinationCount;
    public int readingTimeCount;
    public int enchantAffixCount;
    public int enchantRecipeCount;
    public int equippedCount;
    public int appliedEffectCount;
    public string equippedSummary;
    public bool selfTattooInProgress;
    public float selfTattooRemainingSec;
    public string pendingSelfTattooSummary;
    public int selfTattooCancelledCount;
    public string lastSelfTattooCancelReason;
    public int enchantedCount;
    public int lastEnchantAffixId;
    public string lastEnchantAffixType;
    public string lastEnchantColorTier;
    public string lastEnchantStatKey;
    public float lastEnchantValue;
    public string lastEnchantDisplayText;
    public int lastEnchantCoinCost;
    public int lastEnchantRarePigmentCost;
    public int activeEnchantAffixCount;
    public string activeEnchantSummary;
    public float activeElementDamageBonus;
    public float activeSelfHealOnHit;
    public float activeCritChanceBonus;
    public float activeCritDamageBonus;
    public float activeAttackSpeedBonus;
    public float activeCooldownReduction;
    public float activeStatusChanceBonus;
    public float activeRangeBonus;
    public bool afterDodgeEnchantPending;
    public int afterDodgeEnchantCreatedCount;
    public int afterDodgeEnchantConsumedCount;
    public int lastAfterDodgeEnchantActorId;
    public string lastAfterDodgeEnchantActorName;
    public int actorStateCount;
    public int actorEquippedCount;
    public int actorSelfTattooInProgressCount;
    public int actorAppliedEffectCount;
    public int actorSelfTattooCancelledCount;
    public int pendingTriggerCount;
    public int pendingTriggerCreatedCount;
    public int pendingTriggerConsumedCount;
    public string lastPendingTriggerSource;
    public string lastPendingTriggerConsumeEvent;
    public string lastPendingTriggerSummary;
    public int actorPendingTriggerCount;
    public int critTriggeredCount;
    public int actorCritTriggeredCount;
    public float lastCritBaseDamage;
    public float lastCritDamage;
    public float lastCritChance;
    public float lastCritMultiplier;
    public float lastCritRoll;
    public float lastHeadPassiveCritRateBonus;
    public float lastHeadPassiveElementBonus;
    public string lastCritSourceName;
    public string lastCritTargetName;
    public string lastCritTattooSummary;
}

public sealed class TotemWeaponDefinition
{
    public string WeaponId;
    public string DisplayName;
    public TotemWeaponClass Class;
    public float BaseDamage;
    public float Cooldown;
    public float Range;
    public float AttackSpeedModifier;
    public float ChargedMultiplier = 1.5f;
    public string ProjectileId;
    public int Rarity;
    public bool RequiresCharge;
    public int MaxAmmo;
    public int StartupFrames;
    public int ActiveFrames;
    public int RecoveryFrames;
    public float AimSpreadHalfDegrees;
    public string NormalTraitId;
    public string ChargedTraitId;
    public string WeaponPrefabPath;
}

public sealed class TotemWeaponState
{
    public TotemWeaponDefinition Weapon;
    public int Level = 1;
    public int CurrentAmmo;
    public float CooldownRemaining;
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

public sealed class TotemWeaponFireResult
{
    public bool Fired;
    public string Reason;
    public TotemWeaponDefinition Weapon;
    public TotemProjectileDefinition Projectile;
    public TotemWeaponTraitDefinition ActiveTrait;
    public float Damage;
    public float Range;
    public bool IsCharged;
}

public sealed class TotemWeaponTraitEffectResult
{
    public bool applied;
    public string reason;
    public string traitId;
    public string effectType;
    public string statusName;
    public float statusDps;
    public float statusDuration;
    public bool statusApplied;
    public float statusChance;
    public float statusChanceBonus;
    public float statusRoll;
    public float sourceHeal;
    public float cooldownRemaining;
    public int secondaryHitCount;
    public float secondaryDamage;
    public int extraProjectileCount;
    public float displacement;
    public float effectRadius;
    public int sourceActorId;
    public int targetActorId;
}

public sealed class TotemProjectileDefinition
{
    public string ProjectileId;
    public float Speed;
    public float MaxRange;
    public bool Piercing;
    public float AoeRadius;
    public string VisualPrefabPath;
    public int PoolSize;
}

public sealed class TotemWeaponTraitDefinition
{
    public string TraitId;
    public string DisplayName;
    public string Description;
    public TotemWeaponTraitEffectType EffectType;
    public float EffectParam1;
    public float EffectParam2;
}

public sealed class TotemWeaponDropDefinition
{
    public string DropId;
    public string WeaponId;
    public string DropSource;
    public int Weight;
    public int MinRoomIndex;
    public int MaxRoomIndex;
}

public sealed class TotemWeaponPickupModel
{
    public int InstanceId;
    public string WeaponId;
    public string Source;
    public Vector3 Position;
    public GameObject GameObject;
    public string VisualAssetKey;
}

public sealed class TotemWeaponPickupResult
{
    public bool picked;
    public string reason;
    public int pickupInstanceId;
    public string weaponId;
    public bool upgraded;
    public int weaponLevel;
    public int convertedGold;
}

public sealed class TotemWeaponPickupSnapshot
{
    public int activePickupCount;
    public int spawnedPickupCount;
    public int pickedPickupCount;
    public int visualAssetPickupCount;
    public int visualFallbackPickupCount;
    public string lastVisualAssetKey;
    public string lastVisualFallbackKey;
    public string lastPickupWeaponId;
    public int lastPickupActorId;
    public int mapResourcePickupCount;
    public string lastMapResourceAnchorId;
}

public sealed class TotemChestRewardDefinition
{
    public string ChestId;
    public TotemChestRewardType RewardType;
    public string RewardId;
    public int RewardAmount;
    public int Probability;
}

public sealed class TotemChestModel
{
    public int InstanceId;
    public string ChestId;
    public Vector3 Position;
    public bool Opened;
    public GameObject GameObject;
}

public sealed class TotemChestOpenResult
{
    public bool opened;
    public string reason;
    public int chestInstanceId;
    public string chestId;
    public TotemChestRewardType rewardType;
    public string rewardId;
    public int rewardAmount;
    public int spawnedWeaponPickupId;
    public int coinsAdded;
    public float healAmount;
}

public sealed class TotemChestSnapshot
{
    public int activeChestCount;
    public int openedChestCount;
    public int commonChestCount;
    public int rareChestCount;
    public string lastOpenedChestId;
    public string lastRewardType;
}

public sealed class TotemSkillDefinition
{
    public string SkillId;
    public string DisplayName;
    public TotemSkillChargeModel ChargeModel;
    public float Cooldown;
    public int MaxCharges;
    public float ChargeRegenTime;
    public float HoldDuration;
    public float OverchargeWindow;
    public float Startup;
    public float Active;
    public float Recovery;
    public int StartupFrames;
    public int ActiveFrames;
    public int RecoveryFrames;
    public float Damage;
    public float DamageMultiplier;
    public TotemSkillHitShape HitShape;
    public float Radius;
    public TotemTattooElement Element;
    public bool CancelableByDodge;
    public int ItemId;
}

public sealed class TotemSkillSlotState
{
    public TotemSkillDefinition Skill;
    public TotemSkillPhase Phase;
    public float CooldownRemaining;
    public float ChargeRegenRemaining;
    public float PhaseElapsed;
    public int CurrentCharges;
}

public sealed class TotemStatusInstance
{
    public TotemActorModel Target;
    public TotemActorModel Source;
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

public sealed class TotemBossPhase
{
    public string BossId;
    public int PhaseIndex;
    public float HPThreshold;
    public string SkillIds;
    public float EnrageMultiplier;
    public string PhaseVFXId;
    public string PhaseBGMCueId;
    public string DeathPatternRecipeId;
}

public sealed class TotemBossSnapshot
{
    public bool active;
    public string bossId;
    public int currentPhase;
    public string currentPhaseSkillIds;
    public string currentPhaseVFXId;
    public string currentPhaseBGMCueId;
    public float hpRatio;
    public float enrageMultiplier;
    public bool transitioning;
    public string deathPatternRecipeId;
    public bool deathRewardClaimed;
    public string lastDeathRewardRecipeId;
}

public sealed class TotemInventorySnapshot
{
    public int actorId;
    public int coins;
    public int inkBottleCount;
    public int recipeShardCount;
    public int recipeUnlockCount;
    public string[] recipeIds;
    public int equipmentCount;
}

public sealed class TotemDeathChestSnapshot
{
    public int deadActorId;
    public int coins;
    public int inkBottleCount;
    public int recipeCopyCount;
    public int equipmentCount;
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
    public string reason;
    public int killCount;
    public float playerHealth;
    public int aliveEnemyCount;
    public float elapsedSec;
    public bool bossRewardClaimed;
    public string bossDeathPatternRecipeId;
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

public sealed class TotemShopOffer
{
    public int ItemId;
    public string Category;
    public string DisplayName;
    public int Price;
    public int Stock;
    public int Weight;
    public TotemShopRewardType RewardType;
    public string RewardId;
    public int RewardAmount;
    public int RewardSlot = -1;
}

public sealed class TotemShopPurchaseResult
{
    public bool purchased;
    public string reason;
    public int itemId;
    public int actualPrice;
    public int stockLeft;
    public TotemShopRewardType rewardType;
    public string rewardSummary;
}

public sealed class TotemTattooEnchantPurchaseResult
{
    public bool succeeded;
    public string reason;
    public string colorTier;
    public int coinCost;
    public int rarePigmentCost;
    public int coinsAfter;
    public int inkAfter;
    public int affixId;
    public string affixSummary;
}

public sealed class TotemNpcModel
{
    public int ConfigId;
    public string NpcId;
    public TotemNpcType Type;
    public string MapTheme;
    public string ShopStockTable;
    public Vector3 Position;
    public float InteractRadius;
    public float ThemePriceMultiplier;
    public float GuardRadius;
    public float ServiceCooldown;
    public string GuardSpawnId;
    public int GuardCount1;
    public int GuardCount2;
    public TotemShopOffer[] Offers = Array.Empty<TotemShopOffer>();
}

public sealed class TotemNpcSnapshot
{
    public int npcCount;
    public int tattooistCount;
    public int merchantCount;
    public int shopOfferCount;
}

public sealed class TotemChoiceOption
{
    public string OptionId;
    public TotemChoiceOptionType OptionType;
    public string DisplayName;
    public string DescKey;
    public string ContentRef;
    public int SkillSlot;
    public int ValueInt;
    public int WeightBase;
    public string WeightBuildBonus;
    public float MinRunElapsedSec;
    public bool IsUnique;
    public TotemChoiceEffectType EffectType;
    public float Magnitude;
}

public sealed class TotemGameplayEventDefinition
{
    public string EventId;
    public TotemGameplayEventType EventType;
    public string DisplayName;
    public string TriggerCondition;
    public int BaseRewardCoin;
    public string RewardPoolId;
    public float TimeoutSec;
    public string CurseDebuffId;
    public int WeightBase;
    public bool IsRepeatAllowed;
}

public sealed class TotemChoiceSnapshot
{
    public string EventId;
    public TotemChoiceOption[] Options = Array.Empty<TotemChoiceOption>();
    public TotemChoiceRuntimeState State;
    public float TimeoutSec;
    public float RemainingSec;
    public float RunElapsedSec;
    public bool TimedOut;
    public string SelectedOptionId;
    public string LastResolutionReason;
    public int UsedUniqueOptionCount;
}

public sealed class TotemInteractionSnapshot
{
    public bool hasNpc;
    public string npcId;
    public string npcType;
    public bool hasDeathChest;
    public int deathChestActorId;
    public bool hasWeaponPickup;
    public int weaponPickupInstanceId;
    public string weaponPickupWeaponId;
    public bool hasChest;
    public int chestInstanceId;
    public string chestId;
    public bool hasMapEvent;
    public string mapEventAnchorId;
    public string mapEventId;
    public string prompt;
    public string lastInteraction;
    public string choiceEventId;
    public int choiceCount;
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
    public float NextBuildRethinkTime;
    public float SelfTattooReadRemaining;
    public bool SelfTattooAwaitingCompletion;
    public float AttackCooldownRemaining;
    public float SkillCooldownRemaining;
    public float DodgeCooldownRemaining;
    public float LastDamagedElapsed = 999f;
    public float SafetyScore = 1f;
    public int Decisions;
    public int Attacks;
    public int SkillUses;
    public int PlannedTattooCount;
    public int PlannedTattooPartMask;
    public string LastPlannedTattoo;
    public TotemActorModel LootTargetActor;
    public TotemWeaponPickupModel ResourcePickupTarget;
    public TotemNpcModel ShopTargetNpc;
    public int DeathChestLoots;
    public int ResourcePickupClaims;
    public int ShopPurchases;
    public TotemAIDecisionRecord LastDecision = new TotemAIDecisionRecord();
}

public sealed class TotemAISnapshot
{
    public bool active;
    public int smartCount;
    public int lightCount;
    public int hotCount;
    public int coldCount;
    public int chaseCount;
    public int attackCount;
    public int wanderCount;
    public int totalDecisions;
    public int totalAttacks;
    public int totalSkillUses;
    public int lootCount;
    public int totalDeathChestLoots;
    public int totalResourcePickupClaims;
    public int totalShopPurchases;
    public int profiledCount;
    public int smartProfileCount;
    public int lightProfileCount;
    public int smartReadingCount;
    public int totalPlannedTattooCount;
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
    public float lastDecisionDistance;
    public float lastDecisionSafetyScore;
    public int lastDecisionProfileBotId;
    public int lastDecisionBuildPresetId;
    public string lastDecisionWeaponId;
    public string lastDecisionSkillId;
    public int lastDecisionPickupInstanceId;
    public string lastDecisionPickupWeaponId;
    public string lastDecisionPickupSource;
    public string lastDecisionNpcId;
    public int lastDecisionShopItemId;
    public int lastDecisionShopPrice;
    public int lastDecisionShopStockLeft;
    public TotemShopRewardType lastDecisionShopRewardType;
    public string lastDecisionShopRewardSummary;
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
    public float Distance;
    public float ActorHealth;
    public float TargetHealth;
    public float SafetyScore;
    public int ProfileBotId;
    public int BuildPresetId;
    public string WeaponId;
    public string SkillId;
    public int PickupInstanceId;
    public string PickupWeaponId;
    public string PickupSource;
    public string NpcId;
    public int ShopItemId;
    public int ShopPrice;
    public int ShopStockLeft;
    public TotemShopRewardType ShopRewardType;
    public string ShopRewardSummary;
    public TotemAIPersonality Personality;
}
