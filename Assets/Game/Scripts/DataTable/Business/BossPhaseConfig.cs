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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/BossPhaseConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class BossPhaseConfig : DataRowBase
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
        /// 对应 EnemyConfig.EnemyId
        /// </summary>
        public string BossId
        {
            get;
            private set;
        }

        /// <summary>
        /// 阶段编号：1 / 2 / 3
        /// </summary>
        public int PhaseIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// 进入本阶段的 HP 百分比：1.0=满血，0.6=60%HP
        /// </summary>
        public float HPThreshold
        {
            get;
            private set;
        }

        /// <summary>
        /// 本阶段解锁技能 ID，逗号分隔，叠加不替换
        /// </summary>
        public string NewSkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 本阶段伤害倍率，1.0=无变化
        /// </summary>
        public float EnrageMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// 阶段转换特效 ID
        /// </summary>
        public string PhaseVFXId
        {
            get;
            private set;
        }

        /// <summary>
        /// 阶段 BGM cue ID
        /// </summary>
        public string PhaseBGMCueId
        {
            get;
            private set;
        }

        /// <summary>
        /// v2.1 新增：Boss 死亡时必掉的主题配方 ID（仅 PhaseIndex=3 有效，其余填空）
        /// </summary>
        public string DeathPatternRecipeId
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
            BossId = columnStrings[index++];
            PhaseIndex = int.Parse(columnStrings[index++]);
            HPThreshold = float.Parse(columnStrings[index++]);
            NewSkillIds = columnStrings[index++];
            EnrageMultiplier = float.Parse(columnStrings[index++]);
            PhaseVFXId = columnStrings[index++];
            PhaseBGMCueId = columnStrings[index++];
            DeathPatternRecipeId = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    BossId = binaryReader.ReadString();
                    PhaseIndex = binaryReader.Read7BitEncodedInt32();
                    HPThreshold = binaryReader.ReadSingle();
                    NewSkillIds = binaryReader.ReadString();
                    EnrageMultiplier = binaryReader.ReadSingle();
                    PhaseVFXId = binaryReader.ReadString();
                    PhaseBGMCueId = binaryReader.ReadString();
                    DeathPatternRecipeId = binaryReader.ReadString();
                }
            }

            return true;
        }
}
