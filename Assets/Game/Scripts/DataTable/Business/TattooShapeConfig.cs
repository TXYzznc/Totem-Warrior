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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooShapeConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooShapeConfig : DataRowBase
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
        /// ShapeBehavior 名
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 形状参数 1（AOEBurst: AreaFactor / StackingMark: Threshold / MultiHit: Segments / ChainJump: MaxJumps / ProbBurst: Probability / TrailZone: TickFactor / SummonForm: SummonMultiplier）
        /// </summary>
        public float Param1
        {
            get;
            private set;
        }

        /// <summary>
        /// 形状参数 2（AOEBurst: MaxTargets / StackingMark: BurstMul / ChainJump: Decay / ProbBurst: BurstMultiplier / TrailZone: Ticks）
        /// </summary>
        public float Param2
        {
            get;
            private set;
        }

        /// <summary>
        /// 形状参数 3（ProbBurst: Seed）
        /// </summary>
        public float Param3
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
            Param1 = float.Parse(columnStrings[index++]);
            Param2 = float.Parse(columnStrings[index++]);
            Param3 = float.Parse(columnStrings[index++]);

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
                    Param1 = binaryReader.ReadSingle();
                    Param2 = binaryReader.ReadSingle();
                    Param3 = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
