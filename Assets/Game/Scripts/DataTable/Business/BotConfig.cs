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
/// 六人纯 PVP 第一版 Bot 配置。纹身构筑由 FirstPlayableBotBuildPlanner 处理，本表只描述战斗与目标选择行为。
/// </summary>
public class BotConfig : DataRowBase
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
        /// 稳定 Bot 配置 ID。
        /// </summary>
        public int BotId
        {
            get;
            private set;
        }

        /// <summary>
        /// Smart / Light。
        /// </summary>
        public string Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 调试显示名。
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// 决策重算间隔（秒）。
        /// </summary>
        public float RethinkInterval
        {
            get;
            private set;
        }

        /// <summary>
        /// 最小攻击冷却。
        /// </summary>
        public float AttackCooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// 目标搜索半径。
        /// </summary>
        public float VisionRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 偏好交战半径。
        /// </summary>
        public float AggroRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 闪避反应延迟（毫秒）。
        /// </summary>
        public int DodgeReactionMs
        {
            get;
            private set;
        }

        /// <summary>
        /// 决策置信度 0..1。
        /// </summary>
        public float Confidence
        {
            get;
            private set;
        }

        /// <summary>
        /// 行为宏预设 ID。
        /// </summary>
        public int PreferredPreset
        {
            get;
            private set;
        }

        /// <summary>
        /// 资源拾取偏好。
        /// </summary>
        public float LootGreedFactor
        {
            get;
            private set;
        }

        /// <summary>
        /// Aggressive / Conservative / ResourceAcquisition / PlayerPriority / Hybrid。
        /// </summary>
        public string Personality
        {
            get;
            private set;
        }

        /// <summary>
        /// 真人目标权重。
        /// </summary>
        public float TargetPlayerWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 其他 Bot 目标权重。
        /// </summary>
        public float TargetHumanoidAiWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 地图资源目标权重。
        /// </summary>
        public float TargetResourceWeight
        {
            get;
            private set;
        }

        /// <summary>
        /// 风险容忍度 0..1。
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
            Personality = columnStrings[index++];
            TargetPlayerWeight = float.Parse(columnStrings[index++]);
            TargetHumanoidAiWeight = float.Parse(columnStrings[index++]);
            TargetResourceWeight = float.Parse(columnStrings[index++]);
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
                    Personality = binaryReader.ReadString();
                    TargetPlayerWeight = binaryReader.ReadSingle();
                    TargetHumanoidAiWeight = binaryReader.ReadSingle();
                    TargetResourceWeight = binaryReader.ReadSingle();
                    RiskTolerance = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
