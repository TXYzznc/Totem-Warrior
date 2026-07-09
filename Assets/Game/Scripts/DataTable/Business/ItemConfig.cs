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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ItemConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class ItemConfig : DataRowBase
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
        /// 物品唯一 ID，全局不重复
        /// </summary>
        public int ItemId
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：Coin | InkBottle | RecipeShard | RecipeFull | Equipment | Antidote
        /// </summary>
        public string ItemType
        {
            get;
            private set;
        }

        /// <summary>
        /// 细分类型：颜料填 ColorId（1–7）；武器填品质（Common/Uncommon/Rare/Legendary）；其他留空
        /// </summary>
        public string SubType
        {
            get;
            private set;
        }

        /// <summary>
        /// 颜料档位：1=Basic / 2=Standard / 3=Premium；非颜料物品填 0
        /// </summary>
        public int Tier
        {
            get;
            private set;
        }

        /// <summary>
        /// 本地化显示名（中文，运行时替换为 LocalizationKey）
        /// </summary>
        public string DisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：Common | Uncommon | Rare | Epic | Legendary
        /// </summary>
        public string Rarity
        {
            get;
            private set;
        }

        /// <summary>
        /// 单槽最大堆叠数；金币 = 9999，颜料 = 99，其他 = 1
        /// </summary>
        public int MaxStack
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础定价（coin）；供商人 ShopStockConfig 引用计算实际价格
        /// </summary>
        public int BasePrice
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家出售时的回收比例（× BasePrice）；默认 0.4
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
            ItemId = int.Parse(columnStrings[index++]);
            ItemType = columnStrings[index++];
            SubType = columnStrings[index++];
            Tier = int.Parse(columnStrings[index++]);
            DisplayName = columnStrings[index++];
            Rarity = columnStrings[index++];
            MaxStack = int.Parse(columnStrings[index++]);
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
                    ItemId = binaryReader.Read7BitEncodedInt32();
                    ItemType = binaryReader.ReadString();
                    SubType = binaryReader.ReadString();
                    Tier = binaryReader.Read7BitEncodedInt32();
                    DisplayName = binaryReader.ReadString();
                    Rarity = binaryReader.ReadString();
                    MaxStack = binaryReader.Read7BitEncodedInt32();
                    BasePrice = binaryReader.Read7BitEncodedInt32();
                    SellRatio = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
