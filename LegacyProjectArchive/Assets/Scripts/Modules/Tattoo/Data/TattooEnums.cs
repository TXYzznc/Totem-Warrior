namespace Tattoo.Data
{
    /// <summary>玩家基础属性维度。对应 PlayerStats 字段。</summary>
    public enum StatType
    {
        CritMultiplier,
        MaxHealth,
        SkillPower,
        WeaponDamage,
        DodgeFrames,
        MoveSpeed,
    }

    /// <summary>部位左右对称分组。</summary>
    public enum SymmetryGroup
    {
        None,
        Arms,
        Legs,
    }

    /// <summary>元素类型。颜色 → 元素 1:1 映射。</summary>
    public enum ElementType
    {
        Fire,
        Lightning,
        Nature,
        Frost,
        Mutation,
        Holy,
        Pure,
    }
}
