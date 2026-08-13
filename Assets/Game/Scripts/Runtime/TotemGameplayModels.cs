using System;
using UnityEngine;

public enum TotemRoomType
{
    SouthWestArea = 0,
    NorthWestArea = 1,
    NorthEastArea = 2,
    SouthEastArea = 3,
}

public enum TotemActorKind
{
    Player = 0,
    SmartAi = 1,
    LightAi = 2,
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
    public int followParticipantId;
    public bool spectatingTeammate;
}

public sealed class TotemVfxSnapshot
{
    public int activeCount;
    public int spawnedCount;
    public int rifleTrailSpawnedCount;
    public int spriteRequestCount;
    public int spriteMissingCount;
    public string lastAssetKey;
    public string lastMissingAssetKey;
    public string lastRifleTrailWeaponId;
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

public sealed class TotemResourceDefinition
{
    public int Id;
    public string Name;
    public string ResourceType;
    public string LoadPath;
    public string AssetKey;
    public string ActiveAssetPath;
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
    Extraction = 6,
    Resource = 7,
}

public sealed class TotemMapAnchor
{
    public string AnchorId;
    public TotemMapAnchorKind Kind;
    public TotemRoomType RoomType;
    public Vector3 Position;
    public int Order;
    public string PayloadId;
    public string VisualRole;
    public string ZoneRole;
    public float SearchRadius;
    public bool IsReachable;
}

public sealed class TotemMapSnapshot
{
    public int Seed;
    public int ThemeId;
    public string ThemeName;
    public float MapSize;
    public Vector2 WorldMin;
    public Vector2 WorldMax;
    public float WorldGroundY;
    public string SourceSceneName;
    public float MinRoomSize;
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
    public string sourceSceneName;
    public int authoredAnchorCount;
}

public sealed class TotemMapTemplateDefinition
{
    public int Id;
    public string ThemeName;
    public float MapSize;
    public float MinRoomSize;
    public int TerrainPoolId;
    public string PrefabPath;
    public string HudAccentColor;
    public string DominantColor;
}

public sealed class TotemActorSpawnInfo
{
    public int ActorId;
    public int TeamId = -1;
    public string Name;
    public TotemActorKind Kind;
    public TotemParticipantControllerKind ControllerKind;
    public Vector3 Position;
    public float MaxHealth;
}

public sealed class TotemActorModel : TotemParticipantModel
{
    public int ActorId => ParticipantId;
    public TotemActorKind Kind { get; }
    public bool AnimationMoving { get; set; }
    public int AnimationDirection { get; set; }
    public bool AnimationDead { get; set; }
    public int AnimationAttackTriggerCount { get; set; }
    public int AnimationDeathTriggerCount { get; set; }
    public string AnimationLastReason { get; set; } = string.Empty;
    public TotemActorModel(TotemActorSpawnInfo spawnInfo)
        : base(
            spawnInfo?.ActorId ?? throw new ArgumentNullException(nameof(spawnInfo)),
            string.IsNullOrWhiteSpace(spawnInfo.Name) ? $"Actor{spawnInfo.ActorId}" : spawnInfo.Name,
            ResolveControllerKind(spawnInfo),
            spawnInfo.MaxHealth,
            spawnInfo.Position,
            TotemParticipantLifecycle.Active,
            spawnInfo.TeamId)
    {
        Kind = spawnInfo.Kind;
    }

    private static TotemParticipantControllerKind ResolveControllerKind(TotemActorSpawnInfo spawnInfo)
    {
        switch (spawnInfo.Kind)
        {
            case TotemActorKind.SmartAi:
                return TotemParticipantControllerKind.SmartBot;
            case TotemActorKind.LightAi:
                return TotemParticipantControllerKind.LightBot;
            default:
                return spawnInfo.ControllerKind;
        }
    }
}

public struct TotemDamageRecord
{
    public int Sequence;
    public TotemCombatantModel Source;
    public TotemActorModel Target;
    public float Amount;
    public bool Killed;
    public string Reason;
    public float TargetHealthAfter;
}

public sealed class TotemActorSnapshot
{
    public int actorCount;
    public int participantCount;
    public int aliveParticipantCount;
    public int playerCount;
    public int smartAiCount;
    public int lightAiCount;
    public int visualAssetActorCount;
    public int visualFallbackActorCount;
    public string lastVisualAssetKey;
    public string lastVisualFallbackKey;
    public int terrainHazardHitCount;
    public float lastTerrainHazardDamageTick;
    public int terrainCoverReducedHitCount;
    public float lastTerrainCoverDamageBefore;
    public float lastTerrainCoverDamageAfter;
    public bool playerStartupInvulnerable;
    public int playerStartupDamageBlockedCount;
    public string playerStartupProtectionReason;
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
    public bool dodgePressed;
    public bool extractionUnlockPressed;
    public bool interactPressed;
    public bool interactHeld;
    public bool escapePressed;

    public static TotemInputSnapshot Empty => default;
}

public sealed class TotemUISnapshot
{
    public bool canUseGFUI;
    public int currentFormId;
    public int overlayFormCount;
    public int exclusiveOpenRequestCount;
    public int overlayOpenRequestCount;
    public int overlayCloseRequestCount;
    public string lastExclusiveView;
    public string lastOverlayView;
    public bool lastExclusiveSucceeded;
    public bool lastOverlaySucceeded;
    public bool lastOverlayAllowEscape;
    public int lastOverlaySortOrder;
    public bool hasActiveRunResult;
}

public sealed class TotemCombatSnapshot
{
    public bool active;
    public float playerHealth;
    public int aliveParticipantCount;
    public int winnerParticipantId;
    public int killCount;
    public string lastAction;
    public string lastReason;
    public int lastTargetActorId;
    public string lastTargetName;
    public float lastDamage;
    public bool lastKilled;
    public string lastWeaponId;
    public string lastTargetingMode;
    public float lastAimSpreadHalfDegrees;
    public Vector3 lastAimForward;
    public float elapsedSec;
}
