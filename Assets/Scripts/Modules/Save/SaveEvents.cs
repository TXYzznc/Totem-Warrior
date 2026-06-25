using Economy;

/// <summary>
/// SaveModule 发布的事件（CONTRACT §1.9 附属）。
/// </summary>

/// <summary>
/// 存档首次加载完成后广播。其他模块订阅后读取初始状态（音量 / 已解锁角色 / 图案等）。
/// 注意：仅在 SaveModule.InitializeAsync 完成后由模块内部主动 Publish，不在 InitializeAsync 内部发布。
/// </summary>
public sealed class SaveLoadedEvent
{
    /// <summary>加载完成的存档快照（只读引用）。</summary>
    public SaveData Data;

    public SaveLoadedEvent(SaveData data) { Data = data; }
}

/// <summary>
/// 原子写盘完成后广播。
/// </summary>
public sealed class SaveWrittenEvent
{
    /// <summary>写盘是否成功。</summary>
    public bool   Success;
    /// <summary>写盘路径（调试用）。</summary>
    public string FilePath;

    public SaveWrittenEvent(bool success, string filePath)
    {
        Success  = success;
        FilePath = filePath;
    }
}

// ── CONTRACT §1.9：Run 生命周期事件 ──────────────────────────────

/// <summary>
/// Run 开始事件（CONTRACT §1.9）。
/// 由 GameStateModule / CombatModule 在 Run 初始化完成时发布。
/// </summary>
public sealed class RunStartedEvent
{
    public Actor PlayerActor;
    public int   Seed;
    /// <summary>玩家初始最大血量（HUD 初始化用）。默认 100。</summary>
    public float MaxHealth;
    public RunStartedEvent(Actor player, int seed, float maxHealth = 100f)
    {
        PlayerActor = player;
        Seed        = seed;
        MaxHealth   = maxHealth;
    }
}

/// <summary>
/// Run 结束事件（CONTRACT §1.9，签名锁定）。
/// CombatModule 在玩家死亡/BOSS 击败时发布。
/// SaveModule 订阅后自动累计统计并触发写盘。
/// </summary>
public sealed class RunEndedEvent
{
    /// <summary>本局玩家 Actor。</summary>
    public Actor    PlayerActor;
    /// <summary>是否获胜（击败 Boss）。</summary>
    public bool     Win;
    /// <summary>本局统计数据。</summary>
    public RunStats Stats;

    public RunEndedEvent(Actor player, bool win, RunStats stats)
    {
        PlayerActor = player;
        Win         = win;
        Stats       = stats;
    }
}

/// <summary>
/// 成就解锁事件（MVP 阶段由各模块自行发布，SaveModule 订阅记录）。
/// </summary>
public sealed class AchievementUnlockedEvent
{
    /// <summary>成就唯一 ID。</summary>
    public string AchievementId;
    public AchievementUnlockedEvent(string id) { AchievementId = id; }
}
