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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooReadingTimeConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooReadingTimeConfig : DataRowBase
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
        /// 主键，对齐 TattooPartConfig.Id（1-6）
        /// </summary>
        public int PartId
        {
            get;
            private set;
        }

        /// <summary>
        /// 部位名（仅作可读冗余）
        /// </summary>
        public string PartName
        {
            get;
            private set;
        }

        /// <summary>
        /// 自纹身读条秒数（v2.1）
        /// </summary>
        public float DurationSec
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
            PartId = int.Parse(columnStrings[index++]);
            PartName = columnStrings[index++];
            DurationSec = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PartId = binaryReader.Read7BitEncodedInt32();
                    PartName = binaryReader.ReadString();
                    DurationSec = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
