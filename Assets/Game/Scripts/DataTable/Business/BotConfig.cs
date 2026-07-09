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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/BotConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class BotConfig : DataRowBase
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
        /// Primary profile id
        /// </summary>
        public int BotId
        {
            get;
            private set;
        }

        /// <summary>
        /// Smart or Light
        /// </summary>
        public string Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Runtime/debug display name
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// Build planner rethink interval in seconds
        /// </summary>
        public float RethinkInterval
        {
            get;
            private set;
        }

        /// <summary>
        /// Minimum normal attack cooldown
        /// </summary>
        public float AttackCooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// Target search radius
        /// </summary>
        public float VisionRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// Preferred combat/chase radius
        /// </summary>
        public float AggroRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// Threat reaction latency in milliseconds
        /// </summary>
        public int DodgeReactionMs
        {
            get;
            private set;
        }

        /// <summary>
        /// Decision confidence 0..1
        /// </summary>
        public float Confidence
        {
            get;
            private set;
        }

        /// <summary>
        /// BotBuildPreset.PresetId
        /// </summary>
        public int PreferredPreset
        {
            get;
            private set;
        }

        /// <summary>
        /// Death chest/resource greed 0..2
        /// </summary>
        public float LootGreedFactor
        {
            get;
            private set;
        }

        /// <summary>
        /// Smart self-tattoo boldness 0..1
        /// </summary>
        public float SelfTattooBoldness
        {
            get;
            private set;
        }

        /// <summary>
        /// Enchant/shop upgrade preference 0..1
        /// </summary>
        public float EnchantGreed
        {
            get;
            private set;
        }

        /// <summary>
        /// Smart AI personality
        /// </summary>
        public string Personality
        {
            get;
            private set;
        }

        /// <summary>
        /// Score weight for the real player
        /// </summary>
        public float TargetPlayerWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Score weight for Smart/Light AI targets
        /// </summary>
        public float TargetHumanoidAiWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Score weight for active Boss target
        /// </summary>
        public float TargetBossWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Score weight for resource seeking
        /// </summary>
        public float TargetResourceWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Bonus weight against self-tattoo reading targets
        /// </summary>
        public float ReadingTargetWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// Shop and upgrade preference
        /// </summary>
        public float ShopPreference
        {
            get;
            private set;
        }

        /// <summary>
        /// Risk tolerance 0..1
        /// </summary>
        public float RiskTolerance
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
            BotId = int.Parse(columnStrings[index++]);
            Type = columnStrings[index++];
            DisplayName = columnStrings[index++];
            RethinkInterval = float.Parse(columnStrings[index++]);
            AttackCooldown = float.Parse(columnStrings[index++]);
            VisionRadius = float.Parse(columnStrings[index++]);
            AggroRadius = float.Parse(columnStrings[index++]);
            DodgeReactionMs = int.Parse(columnStrings[index++]);
            Confidence = float.Parse(columnStrings[index++]);
            PreferredPreset = int.Parse(columnStrings[index++]);
            LootGreedFactor = float.Parse(columnStrings[index++]);
            SelfTattooBoldness = float.Parse(columnStrings[index++]);
            EnchantGreed = float.Parse(columnStrings[index++]);
            Personality = columnStrings[index++];
            TargetPlayerWeight = float.Parse(columnStrings[index++]);
            TargetHumanoidAiWeight = float.Parse(columnStrings[index++]);
            TargetBossWeight = float.Parse(columnStrings[index++]);
            TargetResourceWeight = float.Parse(columnStrings[index++]);
            ReadingTargetWeight = float.Parse(columnStrings[index++]);
            ShopPreference = float.Parse(columnStrings[index++]);
            RiskTolerance = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    BotId = binaryReader.Read7BitEncodedInt32();
                    Type = binaryReader.ReadString();
                    DisplayName = binaryReader.ReadString();
                    RethinkInterval = binaryReader.ReadSingle();
                    AttackCooldown = binaryReader.ReadSingle();
                    VisionRadius = binaryReader.ReadSingle();
                    AggroRadius = binaryReader.ReadSingle();
                    DodgeReactionMs = binaryReader.Read7BitEncodedInt32();
                    Confidence = binaryReader.ReadSingle();
                    PreferredPreset = binaryReader.Read7BitEncodedInt32();
                    LootGreedFactor = binaryReader.ReadSingle();
                    SelfTattooBoldness = binaryReader.ReadSingle();
                    EnchantGreed = binaryReader.ReadSingle();
                    Personality = binaryReader.ReadString();
                    TargetPlayerWeight = binaryReader.ReadSingle();
                    TargetHumanoidAiWeight = binaryReader.ReadSingle();
                    TargetBossWeight = binaryReader.ReadSingle();
                    TargetResourceWeight = binaryReader.ReadSingle();
                    ReadingTargetWeight = binaryReader.ReadSingle();
                    ShopPreference = binaryReader.ReadSingle();
                    RiskTolerance = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
