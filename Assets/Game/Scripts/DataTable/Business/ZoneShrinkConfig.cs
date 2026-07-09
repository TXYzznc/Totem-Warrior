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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ZoneShrinkConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
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
        /// 阶段名（Phase0_Slow / Phase1_Offset / Phase2_Rush）
        /// </summary>
        public string PhaseName
        {
            get;
            private set;
        }

        /// <summary>
        /// 本阶段进入时刻（秒，Run 启动 0 起）
        /// </summary>
        public float StartTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 本阶段持续时长（秒）
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 本阶段结束时的目标半径（米）
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
        /// 圈心偏移模式（None / Drift / Fixed），MVP 仅 None
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
