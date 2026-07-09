using System;
using UnityEngine;

public enum TotemRoomType
{
    SpawnRoom = 0,
    TattooStudio = 1,
    Merchant = 2,
    BossRoom = 3,
}

public enum TotemActorKind
{
    Player = 0,
    SmartAi = 1,
    LightAi = 2,
    Boss = 3,
}

public enum TotemEnemyTier
{
    Unknown = 0,
    Light = 1,
    Elite = 2,
    Boss = 3,
}

public enum TotemItemType
{
    Unknown = 0,
    Coin = 1,
    InkBottle = 2,
    RecipeShard = 3,
    RecipeFull = 4,
    Equipment = 5,
    Antidote = 6,
}

public enum TotemAudioCueKind
{
    Unknown = 0,
    Bgm = 1,
    Sfx = 2,
}

public sealed class TotemAudioCueDefinition
{
    public string CueId;
    public TotemAudioCueKind Kind;
    public string AssetName;
    public float Volume = 1f;
    public bool Loop;
    public float MinIntervalSec;
    public string Usage;
    public string LegacySource;
}

public sealed class TotemAudioSnapshot
{
    public bool active;
    public bool backendAvailable;
    public int cueCount;
    public int bgmRequestCount;
    public int sfxRequestCount;
    public int backendPlayAttemptCount;
    public int backendPlaySuccessCount;
    public int backendUnavailableCount;
    public int missingCueCount;
    public int intervalSkipCount;
    public string currentBgmCueId;
    public string currentBgmAssetName;
    public string lastSfxCueId;
    public string lastSfxAssetName;
    public string lastMissingCueId;
    public string lastSkippedCueId;
    public string lastReason;
    public float bgmVolume;
    public float sfxVolume;
    public int observedBossPhase;
    public string observedBossBgmCueId;
}

public sealed class TotemCameraSnapshot
{
    public bool hasCamera;
    public bool following;
    public Vector3 focusPosition;
    public Vector3 rawFocusPosition;
    public bool focusClamped;
    public int focusClampCount;
    public Vector3 basePosition;
    public Vector3 cameraPosition;
    public float orthographicSize;
    public float tiltX;
    public float cameraDistance;
    public float mapSize;
    public int shakeRequestCount;
    public int shakeSkippedCount;
    public float shakeRemainingSec;
    public float lastShakeAmplitude;
    public float lastShakeDuration;
    public Vector3 lastShakeOffset;
}

public sealed class TotemVfxSnapshot
{
    public int activeCount;
    public int spawnedCount;
    public int projectileSpawnedCount;
    public int spriteRequestCount;
    public int spriteMissingCount;
    public string lastAssetKey;
    public string lastMissingAssetKey;
    public string lastProjectileId;
    public int cameraShakeRequestCount;
    public int cameraShakeSkippedCount;
    public float lastCameraShakeAmplitude;
    public float lastCameraShakeDuration;
    public bool vignettePulsing;
    public bool vignetteOverlayActive;
    public int vignettePulseCount;
    public float vignetteIntensity;
    public float playerHealthRatio;
    public int floatingTextActiveCount;
    public int floatingTextSpawnedCount;
    public string lastFloatingText;
    public bool lastFloatingTextStrong;
}

public sealed class TotemItemDefinition
{
    public int ItemId;
    public TotemItemType ItemType;
    public string SubType;
    public int Tier;
    public string DisplayName;
    public string Rarity;
    public int MaxStack;
    public int BasePrice;
    public float SellRatio;
}

public sealed class TotemResourceDefinition
{
    public int Id;
    public string Name;
    public string ResourceType;
    public string LoadPath;
    public string AssetKey;
    public string ActiveAssetPath;
}

public sealed class TotemMerchantSlotDefinition
{
    public int SlotIndex;
    public string WeaponId;
    public int GoldCost;
    public int RefreshWeight;
}

public sealed class TotemRoomInfo
{
    public int RoomId;
    public string Label;
    public TotemRoomType RoomType;
    public Rect Bounds;
    public Vector3 CenterWorld;
    public float Footprint;
}

public enum TotemTerrainType : byte
{
    None = 0,
    Ground = 1,
    Slow = 2,
    Blocked = 3,
    Cover = 4,
    Hazard = 5,
}

public enum TotemMapAnchorKind : byte
{
    Unknown = 0,
    PlayerSpawn = 1,
    BossSpawn = 2,
    EnemySpawn = 3,
    Merchant = 4,
    Tattooist = 5,
    Chest = 6,
    Resource = 7,
    Event = 8,
}

public sealed class TotemMapAnchor
{
    public string AnchorId;
    public TotemMapAnchorKind Kind;
    public TotemRoomType RoomType;
    public Vector3 Position;
    public int Order;
    public string PayloadId;
}

public sealed class TotemMapSnapshot
{
    public int Seed;
    public int ThemeId;
    public string ThemeName;
    public float MapSize;
    public float MinRoomSize;
    public int BspMaxDepth;
    public int TerrainPoolId;
    public string PrefabPath;
    public string HudAccentColor;
    public string DominantColor;
    public Vector2 InitialZoneCenter;
    public TotemRoomInfo[] Rooms = Array.Empty<TotemRoomInfo>();
    public TotemMapAnchor[] AnchorPlacements = Array.Empty<TotemMapAnchor>();
    public int TerrainCellSize;
    public int TerrainGridWidth;
    public int TerrainGridHeight;
    public byte[] TerrainGrid = Array.Empty<byte>();
    public int GroundCellCount;
    public int SlowCellCount;
    public int BlockedCellCount;
    public int CoverCellCount;
    public int HazardCellCount;
    public bool IsPcgGenerated;
    public int PcgWidth;
    public int PcgHeight;
    public int PcgVisualCount;
    public int PcgReachableCells;
    public int PcgUnreachableCells;
    public ulong PcgContentHash;
    public string PcgValidationSummary;
    public PCGMap.PCGMapData PcgMapData;
}

public sealed class TotemMapRuntimeSnapshot
{
    public bool hasRoot;
    public string rootName;
    public int spawnedObjectCount;
    public int rootChildCount;
    public int groundObjectCount;
    public int wallObjectCount;
    public int roomMarkerObjectCount;
    public int materialRequestCount;
    public int materialFallbackCount;
    public string lastMaterialAssetKey;
    public string lastMaterialFallbackAssetKey;
    public float mapSize;
    public string themeName;
    public bool isPcgGenerated;
    public int pcgCellObjectCount;
    public int pcgVisualObjectCount;
    public int pcgMissingSpriteCount;
    public int pcgSpriteLoadCount;
    public int pcgSpriteCreateCount;
    public ulong pcgContentHash;
}

public sealed class TotemMapTemplateDefinition
{
    public int Id;
    public string ThemeName;
    public float MapSize;
    public float MinRoomSize;
    public int BspMaxDepth;
    public int TerrainPoolId;
    public string PrefabPath;
    public string HudAccentColor;
    public string DominantColor;
}

public sealed class TotemActorSpawnInfo
{
    public int ActorId;
    public string Name;
    public TotemActorKind Kind;
    public Vector3 Position;
    public float MaxHealth;
    public string EnemyId;
    public string DisplayName;
    public string ThemeId;
    public TotemEnemyTier EnemyTier;
    public float HpCurveK;
    public float BaseDamage;
    public float DamageCurveK;
    public float MoveSpeed;
    public float AttackRange;
    public float DetectRange;
    public string SkillIds;
    public string LootTableId;
    public string GuaranteedLootIds;
    public bool ElitePaintDropRare;
    public int XPReward;
    public int CoinRewardMin;
    public int CoinRewardMax;
    public string PoolIds;
}

public sealed class TotemActorModel
{
    public int ActorId { get; }
    public string Name { get; }
    public TotemActorKind Kind { get; }
    public float MaxHealth { get; }
    public float Health { get; private set; }
    public Vector3 Position { get; set; }
    public GameObject GameObject { get; set; }
    public string VisualAssetKey { get; set; }
    public bool AnimationMoving { get; set; }
    public int AnimationDirection { get; set; }
    public bool AnimationDead { get; set; }
    public int AnimationAttackTriggerCount { get; set; }
    public int AnimationDeathTriggerCount { get; set; }
    public string AnimationLastReason { get; set; } = string.Empty;
    public string EnemyId { get; }
    public string DisplayName { get; }
    public string ThemeId { get; }
    public TotemEnemyTier EnemyTier { get; }
    public float HpCurveK { get; }
    public float BaseDamage { get; }
    public float DamageCurveK { get; }
    public float MoveSpeed { get; }
    public float AttackRange { get; }
    public float DetectRange { get; }
    public string SkillIds { get; }
    public string LootTableId { get; }
    public string GuaranteedLootIds { get; }
    public bool ElitePaintDropRare { get; }
    public int XPReward { get; }
    public int CoinRewardMin { get; }
    public int CoinRewardMax { get; }
    public string PoolIds { get; }

    public TotemActorModel(TotemActorSpawnInfo spawnInfo)
    {
        if (spawnInfo == null)
        {
            throw new ArgumentNullException(nameof(spawnInfo));
        }

        ActorId = spawnInfo.ActorId;
        Name = string.IsNullOrWhiteSpace(spawnInfo.Name) ? $"Actor{spawnInfo.ActorId}" : spawnInfo.Name;
        Kind = spawnInfo.Kind;
        MaxHealth = spawnInfo.MaxHealth <= 0f ? 1f : spawnInfo.MaxHealth;
        Health = MaxHealth;
        Position = spawnInfo.Position;
        EnemyId = spawnInfo.EnemyId ?? string.Empty;
        DisplayName = string.IsNullOrWhiteSpace(spawnInfo.DisplayName) ? Name : spawnInfo.DisplayName;
        ThemeId = spawnInfo.ThemeId ?? string.Empty;
        EnemyTier = spawnInfo.EnemyTier;
        HpCurveK = Mathf.Max(0f, spawnInfo.HpCurveK);
        BaseDamage = Mathf.Max(0f, spawnInfo.BaseDamage);
        DamageCurveK = Mathf.Max(0f, spawnInfo.DamageCurveK);
        MoveSpeed = Mathf.Max(0f, spawnInfo.MoveSpeed);
        AttackRange = Mathf.Max(0f, spawnInfo.AttackRange);
        DetectRange = Mathf.Max(0f, spawnInfo.DetectRange);
        SkillIds = spawnInfo.SkillIds ?? string.Empty;
        LootTableId = spawnInfo.LootTableId ?? string.Empty;
        GuaranteedLootIds = spawnInfo.GuaranteedLootIds ?? string.Empty;
        ElitePaintDropRare = spawnInfo.ElitePaintDropRare;
        XPReward = Mathf.Max(0, spawnInfo.XPReward);
        CoinRewardMin = Mathf.Max(0, spawnInfo.CoinRewardMin);
        CoinRewardMax = Mathf.Max(CoinRewardMin, spawnInfo.CoinRewardMax);
        PoolIds = spawnInfo.PoolIds ?? string.Empty;
    }

    public bool IsAlive => Health > 0f;

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || Health <= 0f)
        {
            return;
        }

        Health = Mathf.Max(0f, Health - amount);
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || Health <= 0f)
        {
            return 0f;
        }

        float before = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        return Health - before;
    }
}

public struct TotemDamageRecord
{
    public int Sequence;
    public TotemActorModel Source;
    public TotemActorModel Target;
    public float Amount;
    public bool Killed;
    public string Reason;
    public float TargetHealthAfter;
}

public sealed class TotemActorSnapshot
{
    public int actorCount;
    public int playerCount;
    public int smartAiCount;
    public int lightAiCount;
    public int bossCount;
    public int aliveEnemyCount;
    public int visualAssetActorCount;
    public int visualFallbackActorCount;
    public string lastVisualAssetKey;
    public string lastVisualFallbackKey;
    public int terrainHazardHitCount;
    public float lastTerrainHazardDamageTick;
    public int terrainCoverReducedHitCount;
    public float lastTerrainCoverDamageBefore;
    public float lastTerrainCoverDamageAfter;
}

public sealed class TotemActorAnimationSnapshot
{
    public int actorId;
    public string actorName;
    public TotemActorKind actorKind;
    public bool hasGameObject;
    public bool hasAnimator;
    public bool animationMoving;
    public int animationDirection;
    public bool animationDead;
    public int attackTriggerCount;
    public int deathTriggerCount;
    public string lastReason;
    public bool animatorHasIsMoving;
    public bool animatorHasDirection;
    public bool animatorHasAttackTrigger;
    public bool animatorHasDie;
    public bool animatorHasDead;
    public bool animatorIsMoving;
    public int animatorDirection;
    public bool animatorDead;
}

public struct TotemInputSnapshot
{
    public Vector2 move;
    public bool hasAimWorldPoint;
    public Vector3 aimWorldPoint;
    public bool attackPressed;
    public bool attackHeld;
    public float attackHoldDuration;
    public bool skillPressed;
    public bool skillSlotEPressed;
    public bool skillSlotQPressed;
    public bool dodgePressed;
    public bool interactPressed;
    public bool escapePressed;
    public bool selfTattooTogglePressed;

    public static TotemInputSnapshot Empty => default;
}

public sealed class TotemUISnapshot
{
    public bool canUseGFUI;
    public int currentFormId;
    public int overlayFormCount;
    public int selfTattooFormId;
    public bool selfTattooOverlayTracked;
    public int exclusiveOpenRequestCount;
    public int overlayOpenRequestCount;
    public int overlayCloseRequestCount;
    public int selfTattooToggleRequestCount;
    public string lastExclusiveView;
    public string lastOverlayView;
    public bool lastExclusiveSucceeded;
    public bool lastOverlaySucceeded;
    public bool lastOverlayAllowEscape;
    public int lastOverlaySortOrder;
    public bool hasActiveShopNpc;
    public bool hasActiveTattooNpc;
    public bool hasActiveChoice;
    public bool hasActiveRunResult;
}

public sealed class TotemCombatSnapshot
{
    public bool active;
    public float playerHealth;
    public int aliveEnemyCount;
    public int killCount;
    public string lastAction;
    public string lastReason;
    public int lastTargetActorId;
    public string lastTargetName;
    public float lastDamage;
    public bool lastKilled;
    public string lastWeaponId;
    public string lastTraitId;
    public string lastSkillId;
    public int lastHitCount;
    public string lastTargetingMode;
    public float lastAimSpreadHalfDegrees;
    public Vector3 lastAimForward;
    public float elapsedSec;
    public float attackCooldownRemaining;
    public float skillCooldownRemaining;
}
