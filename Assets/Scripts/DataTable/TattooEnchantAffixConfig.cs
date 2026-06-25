// v2.1 新增：纹身师附魔词缀池
// 参考 TattooPartConfig 风格手写（非 generator 产出）

using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class TattooEnchantAffixConfigRow
{
    /// <summary>词缀唯一 ID，主键</summary>
    public int Id { get; set; }
    /// <summary>适用部位：0=全部位 1=Head 2=Torso 3=LeftArm 4=RightArm 5=LeftLeg 6=RightLeg</summary>
    public int PartId { get; set; }
    /// <summary>适用颜料档：Common / Rare / Legendary / Any</summary>
    public string ColorTier { get; set; }
    /// <summary>效果类型：AffixType 枚举名（ElementDamageBonus / CooldownReduction / AttackSpeed / CritChance / CritDamage / RangeBonus / StatusChance / SelfHealOnHit）</summary>
    public string AffixType { get; set; }
    /// <summary>影响的数值 Key（ElementDmg / CritRate / CooldownPct 等）</summary>
    public string StatKey { get; set; }
    /// <summary>词缀数值（百分比类已存储为小数，0.15 = 15%）</summary>
    public float Value { get; set; }
    /// <summary>条件 Key（无条件留空字符串；如 DistanceGt8m / AfterDodge）</summary>
    public string ConditionKey { get; set; }
    /// <summary>条件阈值（ConditionKey 为空时填 0）</summary>
    public float ConditionVal { get; set; }
    /// <summary>UI 展示文案（如 距离>8m 攻击 +30%）</summary>
    public string DisplayText { get; set; }
    /// <summary>同 PartId+ColorTier 池内的抽取权重（归一化前原始值）</summary>
    public float Weight { get; set; }
}

/// <summary>v2.1：纹身师附魔词缀池。EnchantSlot 从此表抽样并写入 TattooSlot.Affixes。</summary>
public sealed class TattooEnchantAffixConfig : IDataTable
{
    readonly Dictionary<int, TattooEnchantAffixConfigRow> _rows = new();

    public void Load(string json)
    {
        var file = JsonConvert.DeserializeObject<DataTableFile<TattooEnchantAffixConfigRow>>(json);
        _rows.Clear();
        foreach (var row in file.rows)
        {
            _rows[row.Id] = row;
        }
    }

    public TattooEnchantAffixConfigRow GetById(int id)
    {
        if (_rows.TryGetValue(id, out var row))
            return row;
        throw new KeyNotFoundException($"TattooEnchantAffixConfig 未找到 Id={id}");
    }

    public bool TryGetById(int id, out TattooEnchantAffixConfigRow row) => _rows.TryGetValue(id, out row);

    public IReadOnlyDictionary<int, TattooEnchantAffixConfigRow> All => _rows;
}
