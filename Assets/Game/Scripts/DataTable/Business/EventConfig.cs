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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/EventConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class EventConfig : DataRowBase
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
        /// 事件唯一 ID，格式 event_<type>_<序号>
        /// </summary>
        public string EventId
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：combat_event / choice_event / puzzle_event / merchant_event / boss_event / lore_event / curse_event
        /// </summary>
        public string EventType
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家可见名称（本地化 Key）
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// 前置条件 JSON：{ minElapsedSec, minRoomCleared, requiredFlag }；空字符串=无条件
        /// </summary>
        public string TriggerCondition
        {
            get;
            private set;
        }

        /// <summary>
        /// 完成时发放基础金币；0=不发
        /// </summary>
        public int BaseRewardCoin
        {
            get;
            private set;
        }

        /// <summary>
        /// 额外掉落池 ID，引用 LootPoolConfig；空=无额外掉落
        /// </summary>
        public string RewardPoolId
        {
            get;
            private set;
        }

        /// <summary>
        /// 超时时间(s)；choice_event 填 20(v2.1)；-1=不超时(merchant_event)
        /// </summary>
        public float TimeoutSec
        {
            get;
            private set;
        }

        /// <summary>
        /// 仅 curse_event 用：施加的 Debuff ID；其余留空
        /// </summary>
        public string CurseDebuffId
        {
            get;
            private set;
        }

        /// <summary>
        /// 地图生成时随机选取事件类型的基础权重 (1–100)
        /// </summary>
        public int WeightBase
        {
            get;
            private set;
        }

        /// <summary>
        /// 同一 Run 内是否允许同一 EventId 重复出现
        /// </summary>
        public bool IsRepeatAllowed
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
            EventId = columnStrings[index++];
            EventType = columnStrings[index++];
            DisplayName = columnStrings[index++];
            TriggerCondition = columnStrings[index++];
            BaseRewardCoin = int.Parse(columnStrings[index++]);
            RewardPoolId = columnStrings[index++];
            TimeoutSec = float.Parse(columnStrings[index++]);
            CurseDebuffId = columnStrings[index++];
            WeightBase = int.Parse(columnStrings[index++]);
            IsRepeatAllowed = bool.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    EventId = binaryReader.ReadString();
                    EventType = binaryReader.ReadString();
                    DisplayName = binaryReader.ReadString();
                    TriggerCondition = binaryReader.ReadString();
                    BaseRewardCoin = binaryReader.Read7BitEncodedInt32();
                    RewardPoolId = binaryReader.ReadString();
                    TimeoutSec = binaryReader.ReadSingle();
                    CurseDebuffId = binaryReader.ReadString();
                    WeightBase = binaryReader.Read7BitEncodedInt32();
                    IsRepeatAllowed = binaryReader.ReadBoolean();
                }
            }

            return true;
        }
}
