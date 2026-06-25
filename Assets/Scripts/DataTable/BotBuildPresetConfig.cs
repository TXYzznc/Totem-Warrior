// 手写：v2.1 Bot Build Preset 配置。Tendency / PreferredParts / RecommendedSeq / TargetEnchantAffixes
// 用 string 字段承载 JSON 子结构，Load 时再 parse 一遍，零运行时分配。
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class BotBuildPresetRow
{
    public int    PresetId { get; set; }
    public string Name { get; set; }
    /// <summary>原始 JSON 字符串，Load 时 parse 为 Tendency[7]。</summary>
    public string Tendency { get; set; }
    public string PreferredParts { get; set; }
    public string RecommendedSeq { get; set; }
    public int    EarlyGameWeapon { get; set; }
    public string BehaviorMacro { get; set; }
    public int    PreferredSkillQ { get; set; }
    public int    PreferredSkillE { get; set; }
    public string TargetEnchantAffixes { get; set; }

    // ===== Load 后展开的结构（避免 Update 中重复 parse） =====
    [JsonIgnore] public float[] TendencyVec;          // length 7
    [JsonIgnore] public int[]   PreferredPartsArr;
    [JsonIgnore] public BotPlannedSlot[] RecommendedArr;
    [JsonIgnore] public int[]   TargetAffixIds;
}

/// <summary>v2.1 BotBuildPlanner 输出最小结构：要刻哪个槽。</summary>
public readonly struct BotPlannedSlot
{
    public readonly int PartId;
    public readonly int ColorId;
    public readonly int PatternId;
    public BotPlannedSlot(int p, int c, int pat) { PartId = p; ColorId = c; PatternId = pat; }
}

public sealed class BotBuildPresetConfig : IDataTable
{
    readonly Dictionary<int, BotBuildPresetRow> _rows = new();

    public void Load(string json)
    {
        var file = JsonConvert.DeserializeObject<DataTableFile<BotBuildPresetRow>>(json);
        _rows.Clear();
        foreach (var row in file.rows)
        {
            row.TendencyVec       = JsonConvert.DeserializeObject<float[]>(row.Tendency);
            row.PreferredPartsArr = JsonConvert.DeserializeObject<int[]>(row.PreferredParts);
            row.TargetAffixIds    = JsonConvert.DeserializeObject<int[]>(row.TargetEnchantAffixes);

            // 推荐序列：[{partId,colorId,patternId},...]
            var rawList = JsonConvert.DeserializeObject<List<Dictionary<string, int>>>(row.RecommendedSeq);
            var arr = new BotPlannedSlot[rawList.Count];
            for (int i = 0; i < rawList.Count; i++)
            {
                var d = rawList[i];
                arr[i] = new BotPlannedSlot(d["partId"], d["colorId"], d["patternId"]);
            }
            row.RecommendedArr = arr;

            _rows[row.PresetId] = row;
        }
    }

    public BotBuildPresetRow GetById(int id) => _rows[id];
    public bool TryGetById(int id, out BotBuildPresetRow row) => _rows.TryGetValue(id, out row);
    public IReadOnlyDictionary<int, BotBuildPresetRow> All => _rows;
}
