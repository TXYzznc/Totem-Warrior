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
/// 第一版地图拾取物配置。每个合法资源锚点按权重选择一种拾取物，并在该类型的 MinAmount/MaxAmount 区间内确定性随机数量。
/// </summary>
public class MapResourcePickupConfig : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// GF_X numeric row id.
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 稳定且唯一的拾取定义 ID。
        /// </summary>
        public string PickupId
        {
            get;
            private set;
        }

        /// <summary>
        /// 资源类别；第一版仅启用 Pigment。
        /// </summary>
        public string Category
        {
            get;
            private set;
        }

        /// <summary>
        /// 入账资源 ID。
        /// </summary>
        public string ResourceId
        {
            get;
            private set;
        }

        /// <summary>
        /// 颜料元素：Fire/Ice/Lightning。
        /// </summary>
        public string Element
        {
            get;
            private set;
        }

        /// <summary>
        /// 同类拾取物最小数量（包含）。
        /// </summary>
        public int MinAmount
        {
            get;
            private set;
        }

        /// <summary>
        /// 同类拾取物最大数量（包含）。
        /// </summary>
        public int MaxAmount
        {
            get;
            private set;
        }

        /// <summary>
        /// 满足回合条件时的相对生成权重。
        /// </summary>
        public int Weight
        {
            get;
            private set;
        }

        /// <summary>
        /// 最早生成回合。
        /// </summary>
        public int MinRound
        {
            get;
            private set;
        }

        /// <summary>
        /// 最晚生成回合。
        /// </summary>
        public int MaxRound
        {
            get;
            private set;
        }

        /// <summary>
        /// 运行时资源索引键；美术资源未完成时允许仅作语义键。
        /// </summary>
        public string AssetKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否参与生成。
        /// </summary>
        public bool Enabled
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
            PickupId = columnStrings[index++];
            Category = columnStrings[index++];
            ResourceId = columnStrings[index++];
            Element = columnStrings[index++];
            MinAmount = int.Parse(columnStrings[index++]);
            MaxAmount = int.Parse(columnStrings[index++]);
            Weight = int.Parse(columnStrings[index++]);
            MinRound = int.Parse(columnStrings[index++]);
            MaxRound = int.Parse(columnStrings[index++]);
            AssetKey = columnStrings[index++];
            Enabled = bool.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PickupId = binaryReader.ReadString();
                    Category = binaryReader.ReadString();
                    ResourceId = binaryReader.ReadString();
                    Element = binaryReader.ReadString();
                    MinAmount = binaryReader.Read7BitEncodedInt32();
                    MaxAmount = binaryReader.Read7BitEncodedInt32();
                    Weight = binaryReader.Read7BitEncodedInt32();
                    MinRound = binaryReader.Read7BitEncodedInt32();
                    MaxRound = binaryReader.Read7BitEncodedInt32();
                    AssetKey = binaryReader.ReadString();
                    Enabled = binaryReader.ReadBoolean();
                }
            }

            return true;
        }
}
