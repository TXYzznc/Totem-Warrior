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
/// Business configuration maintained in the GF_X AI DataTable workflow. Id is numeric and business keys are preserved as data columns.
/// </summary>
public class ZoneShrinkConfig : DataRowBase
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
        /// 阶段名（Shrink1 / Shrink2 / Shrink3 / Shrink4）
        /// </summary>
        public string PhaseName
        {
            get;
            private set;
        }

        /// <summary>
        /// 缩圈活动内局部起始时间（秒）
        /// </summary>
        public float StartTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 正常模式缩圈时长（秒；快速模式由 MatchTiming 配置）
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 本次缩圈结束时的目标半径（米）
        /// </summary>
        public float TargetRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 圈外伤害（HP/s）
        /// </summary>
        public float OutZoneDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// 圈心偏移模式（第一版仅 None）
        /// </summary>
        public string CenterOffsetMode
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
            PhaseName = columnStrings[index++];
            StartTime = float.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            TargetRadius = float.Parse(columnStrings[index++]);
            OutZoneDamage = float.Parse(columnStrings[index++]);
            CenterOffsetMode = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PhaseName = binaryReader.ReadString();
                    StartTime = binaryReader.ReadSingle();
                    Duration = binaryReader.ReadSingle();
                    TargetRadius = binaryReader.ReadSingle();
                    OutZoneDamage = binaryReader.ReadSingle();
                    CenterOffsetMode = binaryReader.ReadString();
                }
            }

            return true;
        }
}
