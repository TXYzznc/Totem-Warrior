// 手写：参考 TattooReadingTimeConfig 风格。v2.1 Bot 行为画像配置。
// 不走 DataTableGenerator —— 手维护 schema 即可。
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class BotConfigRow
{
    public int    BotId { get; set; }
    /// <summary>Smart / Light</summary>
    public string Type { get; set; }
    public string DisplayName { get; set; }
    public float  RethinkInterval { get; set; }
    public float  AttackCooldown { get; set; }
    public float  VisionRadius { get; set; }
    public float  AggroRadius { get; set; }
    public int    DodgeReactionMs { get; set; }
    public float  Confidence { get; set; }
    public int    PreferredPreset { get; set; }
    public float  LootGreedFactor { get; set; }
    public float  SelfTattooBoldness { get; set; }
    public float  EnchantGreed { get; set; }
}

/// <summary>v2.1 Bot 行为画像。按 BotId 查询。</summary>
public sealed class BotConfig : IDataTable
{
    readonly Dictionary<int, BotConfigRow> _rows = new();
    // 按 Type 分桶便于 Module 启动时按比例抽样
    readonly List<BotConfigRow> _smart = new();
    readonly List<BotConfigRow> _light = new();

    public void Load(string json)
    {
        var file = JsonConvert.DeserializeObject<DataTableFile<BotConfigRow>>(json);
        _rows.Clear();
        _smart.Clear();
        _light.Clear();
        foreach (var row in file.rows)
        {
            _rows[row.BotId] = row;
            if (row.Type == "Smart") _smart.Add(row);
            else if (row.Type == "Light") _light.Add(row);
        }
    }

    public BotConfigRow GetById(int id) => _rows[id];
    public bool TryGetById(int id, out BotConfigRow row) => _rows.TryGetValue(id, out row);

    public IReadOnlyList<BotConfigRow> SmartRows => _smart;
    public IReadOnlyList<BotConfigRow> LightRows => _light;
    public IReadOnlyDictionary<int, BotConfigRow> All => _rows;
}
