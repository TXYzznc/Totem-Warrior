namespace Tattoo.Data
{
    /// <summary>玩家基础属性（不变量级别）。被 PassiveStats 增量叠加。</summary>
    public class PlayerStats
    {
        public float CritMultiplier = 1.5f;
        public float MaxHealth      = 100f;
        public float SkillPower     = 20f;
        public float WeaponDamage   = 10f;
        public float DodgeFrames    = 0.3f;
        public float MoveSpeed      = 5f;

        public float Get(StatType s) => s switch
        {
            StatType.CritMultiplier => CritMultiplier,
            StatType.MaxHealth      => MaxHealth,
            StatType.SkillPower     => SkillPower,
            StatType.WeaponDamage   => WeaponDamage,
            StatType.DodgeFrames    => DodgeFrames,
            StatType.MoveSpeed      => MoveSpeed,
            _ => 1f,
        };
    }
}
