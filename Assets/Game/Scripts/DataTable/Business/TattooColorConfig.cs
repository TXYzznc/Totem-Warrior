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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooColorConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooColorConfig : DataRowBase
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
        /// Red / Yellow / Green / Blue / Purple / Gold / White
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// ElementType enum 名：Fire / Lightning / Nature / Frost / Mutation / Holy / Pure
        /// </summary>
        public string Element
        {
            get;
            private set;
        }

        /// <summary>
        /// 颜色级倍率（基础 1.0）
        /// </summary>
        public float ColorMultiplier
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
            Name = columnStrings[index++];
            Element = columnStrings[index++];
            ColorMultiplier = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    Element = binaryReader.ReadString();
                    ColorMultiplier = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
