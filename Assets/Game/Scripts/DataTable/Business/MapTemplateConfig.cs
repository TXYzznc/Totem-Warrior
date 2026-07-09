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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/MapTemplateConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class MapTemplateConfig : DataRowBase
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
        /// 主题枚举名（AI_RUINS / ALIEN_HIVE / VIRUS_SWAMP）
        /// </summary>
        public string ThemeName
        {
            get;
            private set;
        }

        /// <summary>
        /// 固定手工地图边界尺寸（单位 m，固定 400）
        /// </summary>
        public float MapSize
        {
            get;
            private set;
        }

        /// <summary>
        /// BSP 最小房间尺寸（单位 m）
        /// </summary>
        public float MinRoomSize
        {
            get;
            private set;
        }

        /// <summary>
        /// BSP 最大递归深度（v2.1 = 4）
        /// </summary>
        public int BspMaxDepth
        {
            get;
            private set;
        }

        /// <summary>
        /// 地块池 ID，用于 tile 替换。MVP 暂未消费
        /// </summary>
        public int TerrainPoolId
        {
            get;
            private set;
        }

        /// <summary>
        /// 房间 Prefab 路径前缀（相对 Resources/Prefab/Map/）。MVP 留空
        /// </summary>
        public string PrefabPath
        {
            get;
            private set;
        }

        /// <summary>
        /// HUD 强调色（hex，如 #66CCFF）。透传字段
        /// </summary>
        public string HudAccentColor
        {
            get;
            private set;
        }

        /// <summary>
        /// 主色调（hex）。透传字段
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
