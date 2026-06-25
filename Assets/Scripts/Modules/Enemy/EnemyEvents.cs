using UnityEngine;

namespace Tattoo.Events
{
    // ========== 怪物生成事件 ==========

    /// <summary>单只怪物生成时广播。UI/VFX 订阅用于标记精英和 Boss。</summary>
    public sealed class EnemySpawnedEvent
    {
        public string    EnemyId { get; }
        public EnemyTier Tier    { get; }
        public Vector3   Pos     { get; }
        public GameObject Go     { get; }

        public EnemySpawnedEvent(string enemyId, EnemyTier tier, Vector3 pos, GameObject go)
        {
            EnemyId = enemyId;
            Tier    = tier;
            Pos     = pos;
            Go      = go;
        }
    }

    /// <summary>怪物/Boss 死亡时广播。命名与 Economy.Events.ActorDiedEvent 区分（EnemyActorData vs Actor）。</summary>
    public sealed class EnemyDiedEvent
    {
        public EnemyActorData DeadActor { get; }
        public EnemyActorData Killer    { get; }
        public Vector3        DeathPos  { get; }

        public EnemyDiedEvent(EnemyActorData deadActor, EnemyActorData killer, Vector3 deathPos)
        {
            DeadActor = deadActor;
            Killer    = killer;
            DeathPos  = deathPos;
        }
    }

    // ========== Boss 专属事件 ==========
    // BossSpawnedEvent 已在 Tattoo.Events 命名空间定义（TattooEvents.cs），此处不重复。

    /// <summary>Boss 阶段切换。VFX/Audio/UI 订阅后各自消费。</summary>
    public sealed class BossPhaseChangedEvent
    {
        public string BossId             { get; }
        public int    FromPhase          { get; }
        public int    ToPhase            { get; }
        public float  NewEnrageMultiplier { get; }
        /// <summary>本阶段剩余血量比例（0~1，UI 进度条用）。</summary>
        public float  HpRatio            { get; }

        public BossPhaseChangedEvent(string bossId, int from, int to, float enrage, float hpRatio = 1f)
        {
            BossId              = bossId;
            FromPhase           = from;
            ToPhase             = to;
            NewEnrageMultiplier = enrage;
            HpRatio             = hpRatio;
        }
    }

    // ========== 掉落事件（由 EconomyModule 消费，本模块仅发出占位信号）==========

    /// <summary>
    /// 死亡宝箱生成信号。EnemyModule 发出；EconomyModule（未来）订阅后在 Pos 位置生成宝箱并写入物品列表。
    /// v2.1：精英必含 GuaranteedLootIds；Boss 必含 DeathPatternRecipeId。
    /// </summary>
    public sealed class DeathChestSpawnedEvent
    {
        public EnemyActorData DeadActor        { get; }
        public Vector3        Pos              { get; }
        /// <summary>必掉物品 ID 列表（精英 = GuaranteedLootIds；Boss = DeathPatternRecipeId）。</summary>
        public string[]       GuaranteedItemIds { get; }

        public DeathChestSpawnedEvent(EnemyActorData deadActor, Vector3 pos, string[] guaranteedItemIds)
        {
            DeadActor         = deadActor;
            Pos               = pos;
            GuaranteedItemIds = guaranteedItemIds;
        }
    }
}

/// <summary>怪物层级枚举。</summary>
public enum EnemyTier { Light, Elite, Boss }

/// <summary>
/// 怪物运行时 actor 数据（轻量值对象）。
/// 不依赖 Tattoo.Data.Target，怪物独立于 50-actor 预算之外。
/// </summary>
public sealed class EnemyActorData
{
    public string    EnemyId  { get; set; }
    public EnemyTier Tier     { get; set; }
    public float     MaxHP    { get; set; }
    public float     HP       { get; set; }
    public float     BaseDmg  { get; set; }
    public float     EnrageMult { get; set; } = 1f;
    public bool      IsDead   => HP <= 0f;
}
