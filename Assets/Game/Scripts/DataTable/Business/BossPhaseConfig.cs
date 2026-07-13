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
/// Deterministic three-phase Boss configuration with 60% and 30% transitions.
/// </summary>
public class BossPhaseConfig : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// GF_X numeric row id.
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// EnemyConfig boss id.
        /// </summary>
        public string BossId
        {
            get;
            private set;
        }

        /// <summary>
        /// Phase number 1..3.
        /// </summary>
        public int PhaseIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// First-crossing HP ratio threshold.
        /// </summary>
        public float HPThreshold
        {
            get;
            private set;
        }

        /// <summary>
        /// Comma-separated EnemyAbilityConfig ids enabled in this phase.
        /// </summary>
        public string AbilityIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Legacy participant-skill bridge for the old boss service.
        /// </summary>
        public string NewSkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Phase damage multiplier.
        /// </summary>
        public float EnrageMultiplier
        {
            get;
            private set;
        }

        /// <summary>
        /// Phase VFX cue.
        /// </summary>
        public string PhaseVFXId
        {
            get;
            private set;
        }

        /// <summary>
        /// Existing audio cue id.
        /// </summary>
        public string PhaseBGMCueId
        {
            get;
            private set;
        }

        /// <summary>
        /// Theme recipe emitted by phase three death.
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
            AbilityIds = columnStrings[index++];
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
                    AbilityIds = binaryReader.ReadString();
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
