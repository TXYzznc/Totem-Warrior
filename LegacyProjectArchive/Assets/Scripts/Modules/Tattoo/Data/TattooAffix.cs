namespace Tattoo.Data
{
    /// <summary>纹身词缀（v2.1）。由纹身师附魔产生。每个纹身槽最多 2 个词缀。</summary>
    public struct TattooAffix
    {
        public int AffixId;          // 配置表主键
        public string DisplayName;   // 「火伤 +15%」
        public AffixType Type;       // 加成类型
        public float Value;          // 加成数值（+0.15 = +15%）
    }

    public enum AffixType
    {
        ElementDamageBonus,    // 元素伤害加成
        CooldownReduction,     // 冷却减少
        AttackSpeed,           // 攻速
        CritChance,            // 暴击率
        CritDamage,            // 暴击伤害
        RangeBonus,            // 距离加成
        StatusChance,          // 状态命中率
        SelfHealOnHit,         // 命中回血
        // 后续扩展
    }
}
