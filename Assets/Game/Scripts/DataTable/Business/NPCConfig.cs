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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/NPCConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class NPCConfig : DataRowBase
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
        /// 实例唯一字符串 ID（如 tattooist_default）
        /// </summary>
        public string NPCId
        {
            get;
            private set;
        }

        /// <summary>
        /// Tattooist | Merchant
        /// </summary>
        public string Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 适用地图主题：All / Slum / Lab / Alien
        /// </summary>
        public string MapTheme
        {
            get;
            private set;
        }

        /// <summary>
        /// 主题价格倍率（普通 1.0 / 实验室 1.1 / 外星 1.2）
        /// </summary>
        public float ThemePriceMul
        {
            get;
            private set;
        }

        /// <summary>
        /// 关联 ShopStockConfig 中的 TableId；纹身师留空字符串
        /// </summary>
        public string ShopStockTable
        {
            get;
            private set;
        }

        /// <summary>
        /// 触发交互提示距离（m）
        /// </summary>
        public float InteractRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 警卫怪巡逻半径（m）
        /// </summary>
        public float GuardRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 被攻击后关闭服务时长（s）
        /// </summary>
        public float ServiceCooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// 被攻击时生成的警卫怪 PrefabId
        /// </summary>
        public string GuardSpawnId
        {
            get;
            private set;
        }

        /// <summary>
        /// 首次警告警卫数量
        /// </summary>
        public int GuardCount1
        {
            get;
            private set;
        }

        /// <summary>
        /// 升级警告警卫数量
        /// </summary>
        public int GuardCount2
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
            NPCId = columnStrings[index++];
            Type = columnStrings[index++];
            MapTheme = columnStrings[index++];
            ThemePriceMul = float.Parse(columnStrings[index++]);
            ShopStockTable = columnStrings[index++];
            InteractRadius = float.Parse(columnStrings[index++]);
            GuardRadius = float.Parse(columnStrings[index++]);
            ServiceCooldown = float.Parse(columnStrings[index++]);
            GuardSpawnId = columnStrings[index++];
            GuardCount1 = int.Parse(columnStrings[index++]);
            GuardCount2 = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    NPCId = binaryReader.ReadString();
                    Type = binaryReader.ReadString();
                    MapTheme = binaryReader.ReadString();
                    ThemePriceMul = binaryReader.ReadSingle();
                    ShopStockTable = binaryReader.ReadString();
                    InteractRadius = binaryReader.ReadSingle();
                    GuardRadius = binaryReader.ReadSingle();
                    ServiceCooldown = binaryReader.ReadSingle();
                    GuardSpawnId = binaryReader.ReadString();
                    GuardCount1 = binaryReader.Read7BitEncodedInt32();
                    GuardCount2 = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
