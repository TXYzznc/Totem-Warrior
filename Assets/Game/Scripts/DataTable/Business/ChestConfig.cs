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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ChestConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class ChestConfig : DataRowBase
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
        /// 宝箱类型主键，如 chest_common / chest_rare
        /// </summary>
        public string ChestId
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：Weapon / Gold / Potion
        /// </summary>
        public string RewardType
        {
            get;
            private set;
        }

        /// <summary>
        /// Weapon 时填 WeaponId（从 WeaponDropConfig 随机选）；Gold/Potion 时留空字符串
        /// </summary>
        public string RewardId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gold 时为金币数量（单位 coin）；Weapon 时恒为 1；Potion 时为药水数量
        /// </summary>
        public int RewardAmount
        {
            get;
            private set;
        }

        /// <summary>
        /// 同 ChestId 内概率（整数百分比），同 ChestId 所有行之和必须 = 100（DataTableModule Assert）
        /// </summary>
        public int Probability
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
            ChestId = columnStrings[index++];
            RewardType = columnStrings[index++];
            RewardId = columnStrings[index++];
            RewardAmount = int.Parse(columnStrings[index++]);
            Probability = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    ChestId = binaryReader.ReadString();
                    RewardType = binaryReader.ReadString();
                    RewardId = binaryReader.ReadString();
                    RewardAmount = binaryReader.Read7BitEncodedInt32();
                    Probability = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
