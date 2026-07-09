//------------------------------------------------------------
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：__DATA_TABLE_CREATE_TIME__
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
#endif
/// <summary>
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooEnchantAffixConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooEnchantAffixConfig : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// GF_X numeric row id. Original business key is preserved as a data column when its name is not Id.
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 适用部位：0=全部位 1=Head 2=Torso 3=LeftArm 4=RightArm 5=LeftLeg 6=RightLeg
        /// </summary>
        public int PartId
        {
            get;
            private set;
        }

        /// <summary>
        /// 适用颜料档：Common / Rare / Legendary / Any
        /// </summary>
        public string ColorTier
        {
            get;
            private set;
        }

        /// <summary>
        /// 效果类型：ElementDamageBonus / CooldownReduction / AttackSpeed / CritChance / CritDamage / RangeBonus / StatusChance / SelfHealOnHit
        /// </summary>
        public string AffixType
        {
            get;
            private set;
        }

        /// <summary>
        /// 影响的数值 Key（ElementDmg / CritRate / CooldownPct 等）
        /// </summary>
        public string StatKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 词缀数值（百分比类已存储为小数，0.15 = 15%）
        /// </summary>
        public float Value
        {
            get;
            private set;
        }

        /// <summary>
        /// 条件 Key（无条件留空字符串；如 DistanceGt8m / AfterDodge）
        /// </summary>
        public string ConditionKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 条件阈值（ConditionKey 为空时填 0）
        /// </summary>
        public float ConditionVal
        {
            get;
            private set;
        }

        /// <summary>
        /// UI 展示文案（如 距离>8m 攻击 +30%）
        /// </summary>
        public string DisplayText
        {
            get;
            private set;
        }

        /// <summary>
        /// 同 PartId+ColorTier 池内的抽取权重（归一化前原始值）
        /// </summary>
        public float Weight
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            PartId = int.Parse(columnStrings[index++]);
            ColorTier = columnStrings[index++];
            AffixType = columnStrings[index++];
            StatKey = columnStrings[index++];
            Value = float.Parse(columnStrings[index++]);
            ConditionKey = columnStrings[index++];
            ConditionVal = float.Parse(columnStrings[index++]);
            DisplayText = columnStrings[index++];
            Weight = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PartId = binaryReader.Read7BitEncodedInt32();
                    ColorTier = binaryReader.ReadString();
                    AffixType = binaryReader.ReadString();
                    StatKey = binaryReader.ReadString();
                    Value = binaryReader.ReadSingle();
                    ConditionKey = binaryReader.ReadString();
                    ConditionVal = binaryReader.ReadSingle();
                    DisplayText = binaryReader.ReadString();
                    Weight = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
