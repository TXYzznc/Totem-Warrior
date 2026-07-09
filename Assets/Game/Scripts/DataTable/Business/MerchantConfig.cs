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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/MerchantConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class MerchantConfig : DataRowBase
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
        /// 商人展示槽位索引，范围 0~2（每局刷新时按 RefreshWeight 随机选定该槽武器）
        /// </summary>
        public int SlotIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// 候选武器 ID，对应 WeaponConfig.WeaponId
        /// </summary>
        public string WeaponId
        {
            get;
            private set;
        }

        /// <summary>
        /// 购买所需金币（单位 coin），范围 [50, 200]
        /// </summary>
        public int GoldCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 商人每局刷新时该候选被选中的概率权重（同 SlotIndex 内竞争）
        /// </summary>
        public int RefreshWeight
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
            SlotIndex = int.Parse(columnStrings[index++]);
            WeaponId = columnStrings[index++];
            GoldCost = int.Parse(columnStrings[index++]);
            RefreshWeight = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SlotIndex = binaryReader.Read7BitEncodedInt32();
                    WeaponId = binaryReader.ReadString();
                    GoldCost = binaryReader.Read7BitEncodedInt32();
                    RefreshWeight = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
