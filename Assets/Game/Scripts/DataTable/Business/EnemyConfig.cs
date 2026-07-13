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
/// Native Enemy definitions. JSON is the editable source; xlsx is generated from this manifest.
/// </summary>
public class EnemyConfig : DataRowBase
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
        /// Stable enemy definition id.
        /// </summary>
        public string EnemyId
        {
            get;
            private set;
        }

        /// <summary>
        /// Localization key.
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// common | ai_ruins | alien_hive | virus_swamp.
        /// </summary>
        public string ThemeId
        {
            get;
            private set;
        }

        /// <summary>
        /// Light | Elite | Boss.
        /// </summary>
        public string Tier
        {
            get;
            private set;
        }

        /// <summary>
        /// Final enemy runtime asset key.
        /// </summary>
        public string RuntimeAssetKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Explicit Theme/Tier fallback runtime asset key.
        /// </summary>
        public string FallbackRuntimeAssetKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Enemy controller policy profile id.
        /// </summary>
        public string BehaviorProfileId
        {
            get;
            private set;
        }

        /// <summary>
        /// Comma-separated EnemyAbilityConfig ids.
        /// </summary>
        public string AbilityIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Base health points at world minute zero.
        /// </summary>
        public float BaseHP
        {
            get;
            private set;
        }

        /// <summary>
        /// HP multiplier slope per world minute.
        /// </summary>
        public float HPCurveK
        {
            get;
            private set;
        }

        /// <summary>
        /// Base damage points.
        /// </summary>
        public float BaseDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// Damage multiplier slope per world minute.
        /// </summary>
        public float DamageCurveK
        {
            get;
            private set;
        }

        /// <summary>
        /// Movement speed in meters per second.
        /// </summary>
        public float MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// Preferred attack range in meters.
        /// </summary>
        public float AttackRange
        {
            get;
            private set;
        }

        /// <summary>
        /// Detection range in meters.
        /// </summary>
        public float DetectRange
        {
            get;
            private set;
        }

        /// <summary>
        /// Maximum pursuit distance in meters.
        /// </summary>
        public float LeashRange
        {
            get;
            private set;
        }

        /// <summary>
        /// EnemyLootConfig table id.
        /// </summary>
        public string LootTableId
        {
            get;
            private set;
        }

        /// <summary>
        /// Comma-separated guaranteed EnemyLootConfig entry ids.
        /// </summary>
        public string GuaranteedLootIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Encounter budget cost.
        /// </summary>
        public int SpawnCost
        {
            get;
            private set;
        }

        /// <summary>
        /// Comma-separated encounter pool ids.
        /// </summary>
        public string PoolIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Legacy participant skill bridge; empty for native enemies when possible.
        /// </summary>
        public string SkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// Legacy compatibility flag derived from EnemyLootConfig.
        /// </summary>
        public int ElitePaintDropRare
        {
            get;
            private set;
        }

        /// <summary>
        /// Kill XP reward.
        /// </summary>
        public int XPReward
        {
            get;
            private set;
        }

        /// <summary>
        /// Legacy coin range mirror in min-max form.
        /// </summary>
        public string CoinReward
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
            EnemyId = columnStrings[index++];
            DisplayName = columnStrings[index++];
            ThemeId = columnStrings[index++];
            Tier = columnStrings[index++];
            RuntimeAssetKey = columnStrings[index++];
            FallbackRuntimeAssetKey = columnStrings[index++];
            BehaviorProfileId = columnStrings[index++];
            AbilityIds = columnStrings[index++];
            BaseHP = float.Parse(columnStrings[index++]);
            HPCurveK = float.Parse(columnStrings[index++]);
            BaseDamage = float.Parse(columnStrings[index++]);
            DamageCurveK = float.Parse(columnStrings[index++]);
            MoveSpeed = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);
            DetectRange = float.Parse(columnStrings[index++]);
            LeashRange = float.Parse(columnStrings[index++]);
            LootTableId = columnStrings[index++];
            GuaranteedLootIds = columnStrings[index++];
            SpawnCost = int.Parse(columnStrings[index++]);
            PoolIds = columnStrings[index++];
            SkillIds = columnStrings[index++];
            ElitePaintDropRare = int.Parse(columnStrings[index++]);
            XPReward = int.Parse(columnStrings[index++]);
            CoinReward = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    EnemyId = binaryReader.ReadString();
                    DisplayName = binaryReader.ReadString();
                    ThemeId = binaryReader.ReadString();
                    Tier = binaryReader.ReadString();
                    RuntimeAssetKey = binaryReader.ReadString();
                    FallbackRuntimeAssetKey = binaryReader.ReadString();
                    BehaviorProfileId = binaryReader.ReadString();
                    AbilityIds = binaryReader.ReadString();
                    BaseHP = binaryReader.ReadSingle();
                    HPCurveK = binaryReader.ReadSingle();
                    BaseDamage = binaryReader.ReadSingle();
                    DamageCurveK = binaryReader.ReadSingle();
                    MoveSpeed = binaryReader.ReadSingle();
                    AttackRange = binaryReader.ReadSingle();
                    DetectRange = binaryReader.ReadSingle();
                    LeashRange = binaryReader.ReadSingle();
                    LootTableId = binaryReader.ReadString();
                    GuaranteedLootIds = binaryReader.ReadString();
                    SpawnCost = binaryReader.Read7BitEncodedInt32();
                    PoolIds = binaryReader.ReadString();
                    SkillIds = binaryReader.ReadString();
                    ElitePaintDropRare = binaryReader.Read7BitEncodedInt32();
                    XPReward = binaryReader.Read7BitEncodedInt32();
                    CoinReward = binaryReader.ReadString();
                }
            }

            return true;
        }
}
