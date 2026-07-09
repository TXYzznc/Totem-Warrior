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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/WeaponDropConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class WeaponDropConfig : DataRowBase
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
        /// 主键，全局唯一掉落记录 ID
        /// </summary>
        public string DropId
        {
            get;
            private set;
        }

        /// <summary>
        /// 对应 WeaponConfig.WeaponId
        /// </summary>
        public string WeaponId
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：Elite / Chest / Merchant
        /// </summary>
        public string DropSource
        {
            get;
            private set;
        }

        /// <summary>
        /// 同 DropSource 内加权随机权重，越高越易出
        /// </summary>
        public int Weight
        {
            get;
            private set;
        }

        /// <summary>
        /// 该武器最早可出现的房间序号（关卡进度门控，1=起始房）
        /// </summary>
        public int MinRoomIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// 该武器最晚可出现的房间序号（10=最终Boss前一房）
        /// </summary>
        public int MaxRoomIndex
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
            DropId = columnStrings[index++];
            WeaponId = columnStrings[index++];
            DropSource = columnStrings[index++];
            Weight = int.Parse(columnStrings[index++]);
            MinRoomIndex = int.Parse(columnStrings[index++]);
            MaxRoomIndex = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    DropId = binaryReader.ReadString();
                    WeaponId = binaryReader.ReadString();
                    DropSource = binaryReader.ReadString();
                    Weight = binaryReader.Read7BitEncodedInt32();
                    MinRoomIndex = binaryReader.Read7BitEncodedInt32();
                    MaxRoomIndex = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
