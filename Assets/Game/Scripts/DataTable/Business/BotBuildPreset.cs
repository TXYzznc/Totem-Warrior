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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/BotBuildPreset.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class BotBuildPreset : DataRowBase
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
        /// 主键 1-7
        /// </summary>
        public int PresetId
        {
            get;
            private set;
        }

        /// <summary>
        /// 如 火爆右臂流
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 7 元素倾向 vector JSON：[Fire,Lightning,Nature,Frost,Mutation,Holy,Pure] sum=1.0
        /// </summary>
        public string Tendency
        {
            get;
            private set;
        }

        /// <summary>
        /// 推荐刻的部位 ID 序列 JSON，按优先级排
        /// </summary>
        public string PreferredParts
        {
            get;
            private set;
        }

        /// <summary>
        /// 推荐 build 序列 JSON：[{partId,colorId,patternId},...] 长度1..6
        /// </summary>
        public string RecommendedSeq
        {
            get;
            private set;
        }

        /// <summary>
        /// WeaponId 默认起手武器
        /// </summary>
        public int EarlyGameWeapon
        {
            get;
            private set;
        }

        /// <summary>
        /// Rush / Camp / Pivot / Hybrid
        /// </summary>
        public string BehaviorMacro
        {
            get;
            private set;
        }

        /// <summary>
        /// 起手 Q 槽推荐技能 SkillId
        /// </summary>
        public int PreferredSkillQ
        {
            get;
            private set;
        }

        /// <summary>
        /// 起手 E 槽推荐技能 SkillId
        /// </summary>
        public int PreferredSkillE
        {
            get;
            private set;
        }

        /// <summary>
        /// 希望最终叠到的词缀 ID 列表 JSON
        /// </summary>
        public string TargetEnchantAffixes
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
            PresetId = int.Parse(columnStrings[index++]);
            Name = columnStrings[index++];
            Tendency = columnStrings[index++];
            PreferredParts = columnStrings[index++];
            RecommendedSeq = columnStrings[index++];
            EarlyGameWeapon = int.Parse(columnStrings[index++]);
            BehaviorMacro = columnStrings[index++];
            PreferredSkillQ = int.Parse(columnStrings[index++]);
            PreferredSkillE = int.Parse(columnStrings[index++]);
            TargetEnchantAffixes = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PresetId = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    Tendency = binaryReader.ReadString();
                    PreferredParts = binaryReader.ReadString();
                    RecommendedSeq = binaryReader.ReadString();
                    EarlyGameWeapon = binaryReader.Read7BitEncodedInt32();
                    BehaviorMacro = binaryReader.ReadString();
                    PreferredSkillQ = binaryReader.Read7BitEncodedInt32();
                    PreferredSkillE = binaryReader.Read7BitEncodedInt32();
                    TargetEnchantAffixes = binaryReader.ReadString();
                }
            }

            return true;
        }
}
