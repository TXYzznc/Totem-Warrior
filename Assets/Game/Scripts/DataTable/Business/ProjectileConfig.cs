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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/ProjectileConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class ProjectileConfig : DataRowBase
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
        /// 主键，与 WeaponConfig.ProjectileId 关联
        /// </summary>
        public string ProjectileId
        {
            get;
            private set;
        }

        /// <summary>
        /// 飞行速度 (m/s)
        /// </summary>
        public float Speed
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大飞行距离，超出后回池 (m)
        /// </summary>
        public float MaxRange
        {
            get;
            private set;
        }

        /// <summary>
        /// true=穿透，命中后继续飞行可触发多次 AttackHitEvent
        /// </summary>
        public bool Piercing
        {
            get;
            private set;
        }

        /// <summary>
        /// >0=着弹点范围判定半径 (m)
        /// </summary>
        public float AoeRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// Resources/Prefabs/Projectiles/ 下的 Prefab 路径名
        /// </summary>
        public string VisualPrefabPath
        {
            get;
            private set;
        }

        /// <summary>
        /// 对象池预分配容量
        /// </summary>
        public int PoolSize
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
            ProjectileId = columnStrings[index++];
            Speed = float.Parse(columnStrings[index++]);
            MaxRange = float.Parse(columnStrings[index++]);
            Piercing = bool.Parse(columnStrings[index++]);
            AoeRadius = float.Parse(columnStrings[index++]);
            VisualPrefabPath = columnStrings[index++];
            PoolSize = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    ProjectileId = binaryReader.ReadString();
                    Speed = binaryReader.ReadSingle();
                    MaxRange = binaryReader.ReadSingle();
                    Piercing = binaryReader.ReadBoolean();
                    AoeRadius = binaryReader.ReadSingle();
                    VisualPrefabPath = binaryReader.ReadString();
                    PoolSize = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
