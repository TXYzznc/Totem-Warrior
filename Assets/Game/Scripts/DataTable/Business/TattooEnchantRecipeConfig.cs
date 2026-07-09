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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooEnchantRecipeConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooEnchantRecipeConfig : DataRowBase
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
        /// 颜料档：Common / Rare / Legendary
        /// </summary>
        public string ColorTier
        {
            get;
            private set;
        }

        /// <summary>
        /// 附魔金币花费
        /// </summary>
        public int CoinCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 附魔稀有颜料花费（固定 1 瓶）
        /// </summary>
        public int RarePigmentCost
        {
            get;
            private set;
        }

        /// <summary>
        /// 每个纹身槽最大词缀数（固定 2）
        /// </summary>
        public int MaxAffixPerSlot
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
            ColorTier = columnStrings[index++];
            CoinCost = int.Parse(columnStrings[index++]);
            RarePigmentCost = int.Parse(columnStrings[index++]);
            MaxAffixPerSlot = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    ColorTier = binaryReader.ReadString();
                    CoinCost = binaryReader.Read7BitEncodedInt32();
                    RarePigmentCost = binaryReader.Read7BitEncodedInt32();
                    MaxAffixPerSlot = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
