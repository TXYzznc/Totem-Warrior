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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ThreeChoiceOptionConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class ThreeChoiceOptionConfig : DataRowBase
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
        /// 选项唯一 ID
        /// </summary>
        public string OptionId
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：tattoo_recipe / pattern_recipe / weapon_upgrade / skill_upgrade / skill_acquire / coin_bonus / heal / one_time_scroll
        /// </summary>
        public string OptionType
        {
            get;
            private set;
        }

        /// <summary>
        /// 选项标题（本地化 Key）
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// 选项描述（本地化 Key）
        /// </summary>
        public string DescKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 内容引用：tattoo_recipe/pattern_recipe=配方ID；weapon_upgrade=升级配置ID；skill_upgrade=技能升级ID；skill_acquire=技能ID；其余为空
        /// </summary>
        public string ContentRef
        {
            get;
            private set;
        }

        /// <summary>
        /// 仅 skill_upgrade 用：目标技能槽编号，值域 {0, 1}；其余类型填 -1
        /// </summary>
        public int SkillSlot
        {
            get;
            private set;
        }

        /// <summary>
        /// 数值型内容（金币量/治疗量/百分比）；非数值型填 0
        /// </summary>
        public int ValueInt
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础抽取权重 (1–100)
        /// </summary>
        public int WeightBase
        {
            get;
            private set;
        }

        /// <summary>
        /// Build 联动加权 JSON：{ elementTag: bonusWeight }；与 Build 元素匹配时额外叠加
        /// </summary>
        public string WeightBuildBonus
        {
            get;
            private set;
        }

        /// <summary>
        /// 最早出现时间(s)；0=无限制
        /// </summary>
        public float MinRunElapsedSec
        {
            get;
            private set;
        }

        /// <summary>
        /// 同一 Run 内只能出现一次（pattern_recipe = true）
        /// </summary>
        public bool IsUnique
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
            OptionId = columnStrings[index++];
            OptionType = columnStrings[index++];
            DisplayName = columnStrings[index++];
            DescKey = columnStrings[index++];
            ContentRef = columnStrings[index++];
            SkillSlot = int.Parse(columnStrings[index++]);
            ValueInt = int.Parse(columnStrings[index++]);
            WeightBase = int.Parse(columnStrings[index++]);
            WeightBuildBonus = columnStrings[index++];
            MinRunElapsedSec = float.Parse(columnStrings[index++]);
            IsUnique = bool.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    OptionId = binaryReader.ReadString();
                    OptionType = binaryReader.ReadString();
                    DisplayName = binaryReader.ReadString();
                    DescKey = binaryReader.ReadString();
                    ContentRef = binaryReader.ReadString();
                    SkillSlot = binaryReader.Read7BitEncodedInt32();
                    ValueInt = binaryReader.Read7BitEncodedInt32();
                    WeightBase = binaryReader.Read7BitEncodedInt32();
                    WeightBuildBonus = binaryReader.ReadString();
                    MinRunElapsedSec = binaryReader.ReadSingle();
                    IsUnique = binaryReader.ReadBoolean();
                }
            }

            return true;
        }
}
