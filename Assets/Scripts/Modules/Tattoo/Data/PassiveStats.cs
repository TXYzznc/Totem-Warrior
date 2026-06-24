using System.Collections.Generic;

namespace Tattoo.Data
{
    /// <summary>由装备 Build 推导出的"被动属性增量"。在 BuildChangedEvent 后重算。</summary>
    public class PassiveStats
    {
        public Dictionary<ElementType, float> Resistance   = new();
        public Dictionary<ElementType, float> ElementBonus = new();

        public float CritRateBonus    = 0f;
        public float MaxHealthBonus   = 0f;
        public float SkillPowerBonus  = 0f;
        public float WeaponDmgBonus   = 0f;
        public float DodgeFramesBonus = 0f;
        public float MoveSpeedBonus   = 0f;

        public readonly List<string> EntryLog = new();

        public void Add(string entry) => EntryLog.Add(entry);

        public void AddResist(ElementType e, float v)
        {
            Resistance.TryGetValue(e, out var x);
            Resistance[e] = x + v;
        }

        public void AddElemBonus(ElementType e, float v)
        {
            ElementBonus.TryGetValue(e, out var x);
            ElementBonus[e] = x + v;
        }
    }
}
