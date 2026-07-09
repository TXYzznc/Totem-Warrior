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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ShopStockConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class ShopStockConfig : DataRowBase
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
        /// 被 NPCConfig.ShopStockTable 引用的表段 ID
        /// </summary>
        public string TableId
        {
            get;
            private set;
        }

        /// <summary>
        /// 物品 ID
        /// </summary>
        public int ItemId
        {
            get;
            private set;
        }

        /// <summary>
        /// Weapon / Skill / Ink / Antidote / Remover / RareInk
        /// </summary>
        public string Category
        {
            get;
            private set;
        }

        /// <summary>
        /// 同 TableId 内归一化抽取权重
        /// </summary>
        public float Weight
        {
            get;
            private set;
        }

        /// <summary>
        /// 本局库存最小数量
        /// </summary>
        public int MinCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 本局库存最大数量
        /// </summary>
        public int MaxCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础售价（× ThemePriceMul = 实际售价）
        /// </summary>
        public int BasePrice
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家出售回收比例（× BasePrice）
        /// </summary>
        public float SellRatio
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
            TableId = columnStrings[index++];
            ItemId = int.Parse(columnStrings[index++]);
            Category = columnStrings[index++];
            Weight = float.Parse(columnStrings[index++]);
            MinCount = int.Parse(columnStrings[index++]);
            MaxCount = int.Parse(columnStrings[index++]);
            BasePrice = int.Parse(columnStrings[index++]);
            SellRatio = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    TableId = binaryReader.ReadString();
                    ItemId = binaryReader.Read7BitEncodedInt32();
                    Category = binaryReader.ReadString();
                    Weight = binaryReader.ReadSingle();
                    MinCount = binaryReader.Read7BitEncodedInt32();
                    MaxCount = binaryReader.Read7BitEncodedInt32();
                    BasePrice = binaryReader.Read7BitEncodedInt32();
                    SellRatio = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
