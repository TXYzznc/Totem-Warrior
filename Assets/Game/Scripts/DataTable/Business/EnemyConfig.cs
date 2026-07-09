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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/EnemyConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class EnemyConfig : DataRowBase
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
        /// 怪物唯一 ID，如 enemy_ai_servo_light
        /// </summary>
        public string EnemyId
        {
            get;
            private set;
        }

        /// <summary>
        /// 本地化键，如 enemy.ai_servo
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// AI_RUINS | ALIEN_HIVE | VIRUS_SWAMP | common
        /// </summary>
        public string ThemeId
        {
            get;
            private set;
        }

        /// <summary>
        /// Light | Elite | Boss
        /// </summary>
        public string Tier
        {
            get;
            private set;
        }

        /// <summary>
        /// Run 第 0 分钟基础 HP
        /// </summary>
        public float BaseHP
        {
            get;
            private set;
        }

        /// <summary>
        /// HP 线性增长系数，HP_t = BaseHP × (1 + K × t_min)
        /// </summary>
        public float HPCurveK
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础攻击伤害 (point)
        /// </summary>
        public float BaseDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害随时间增长系数
        /// </summary>
        public float DamageCurveK
        {
            get;
            private set;
        }

        /// <summary>
        /// 移动速度 (m/s)
        /// </summary>
        public float MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击判定范围 (m)
        /// </summary>
        public float AttackRange
        {
            get;
            private set;
        }

        /// <summary>
        /// 感知半径 (m)；失追距离 = DetectRange × 2
        /// </summary>
        public float DetectRange
        {
            get;
            private set;
        }

        /// <summary>
        /// 技能 ID 逗号分隔；Light 通常空
        /// </summary>
        public string SkillIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 随机掉落表 ID
        /// </summary>
        public string LootTableId
        {
            get;
            private set;
        }

        /// <summary>
        /// v2.1 新增：必掉物品 ID 逗号分隔；Elite 必含稀有颜料 ID
        /// </summary>
        public string GuaranteedLootIds
        {
            get;
            private set;
        }

        /// <summary>
        /// v2.1 新增：精英必掉稀有颜料标记；1=必掉，0=不掉
        /// </summary>
        public int ElitePaintDropRare
        {
            get;
            private set;
        }

        /// <summary>
        /// 击杀奖励 XP
        /// </summary>
        public int XPReward
        {
            get;
            private set;
        }

        /// <summary>
        /// 击杀掉落金币范围，格式 min-max
        /// </summary>
        public string CoinReward
        {
            get;
            private set;
        }

        /// <summary>
        /// 所属怪物池 ID，逗号分隔
        /// </summary>
        public string PoolIds
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
            BaseHP = float.Parse(columnStrings[index++]);
            HPCurveK = float.Parse(columnStrings[index++]);
            BaseDamage = float.Parse(columnStrings[index++]);
            DamageCurveK = float.Parse(columnStrings[index++]);
            MoveSpeed = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);
            DetectRange = float.Parse(columnStrings[index++]);
            SkillIds = columnStrings[index++];
            LootTableId = columnStrings[index++];
            GuaranteedLootIds = columnStrings[index++];
            ElitePaintDropRare = int.Parse(columnStrings[index++]);
            XPReward = int.Parse(columnStrings[index++]);
            CoinReward = columnStrings[index++];
            PoolIds = columnStrings[index++];

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
                    BaseHP = binaryReader.ReadSingle();
                    HPCurveK = binaryReader.ReadSingle();
                    BaseDamage = binaryReader.ReadSingle();
                    DamageCurveK = binaryReader.ReadSingle();
                    MoveSpeed = binaryReader.ReadSingle();
                    AttackRange = binaryReader.ReadSingle();
                    DetectRange = binaryReader.ReadSingle();
                    SkillIds = binaryReader.ReadString();
                    LootTableId = binaryReader.ReadString();
                    GuaranteedLootIds = binaryReader.ReadString();
                    ElitePaintDropRare = binaryReader.Read7BitEncodedInt32();
                    XPReward = binaryReader.Read7BitEncodedInt32();
                    CoinReward = binaryReader.ReadString();
                    PoolIds = binaryReader.ReadString();
                }
            }

            return true;
        }
}
