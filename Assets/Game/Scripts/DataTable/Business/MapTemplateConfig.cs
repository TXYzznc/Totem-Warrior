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
/// 六人纯 PVP 第一版只使用已完成的绿洲新城场景。
/// </summary>
public class MapTemplateConfig : DataRowBase
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
        /// 场景主题名。
        /// </summary>
        public string ThemeName
        {
            get;
            private set;
        }

        /// <summary>
        /// 地图边界尺寸（米）。
        /// </summary>
        public float MapSize
        {
            get;
            private set;
        }

        /// <summary>
        /// 运行时布局最小房间尺寸。
        /// </summary>
        public float MinRoomSize
        {
            get;
            private set;
        }

        /// <summary>
        /// 运行时布局最大递归深度。
        /// </summary>
        public int BspMaxDepth
        {
            get;
            private set;
        }

        /// <summary>
        /// 地块池 ID。
        /// </summary>
        public int TerrainPoolId
        {
            get;
            private set;
        }

        /// <summary>
        /// 场景或预制体路径。
        /// </summary>
        public string PrefabPath
        {
            get;
            private set;
        }

        /// <summary>
        /// HUD 强调色。
        /// </summary>
        public string HudAccentColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 环境主色。
        /// </summary>
        public string DominantColor
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
            ThemeName = columnStrings[index++];
            MapSize = float.Parse(columnStrings[index++]);
            MinRoomSize = float.Parse(columnStrings[index++]);
            BspMaxDepth = int.Parse(columnStrings[index++]);
            TerrainPoolId = int.Parse(columnStrings[index++]);
            PrefabPath = columnStrings[index++];
            HudAccentColor = columnStrings[index++];
            DominantColor = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    ThemeName = binaryReader.ReadString();
                    MapSize = binaryReader.ReadSingle();
                    MinRoomSize = binaryReader.ReadSingle();
                    BspMaxDepth = binaryReader.Read7BitEncodedInt32();
                    TerrainPoolId = binaryReader.Read7BitEncodedInt32();
                    PrefabPath = binaryReader.ReadString();
                    HudAccentColor = binaryReader.ReadString();
                    DominantColor = binaryReader.ReadString();
                }
            }

            return true;
        }
}
