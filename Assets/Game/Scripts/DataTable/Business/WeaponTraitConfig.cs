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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/WeaponTraitConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class WeaponTraitConfig : DataRowBase
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
        /// 主键，如 trait_quickslash / trait_pierce
        /// </summary>
        public string TraitId
        {
            get;
            private set;
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// UI tooltip（含具体数值，无占位）
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// 枚举：Status / Pierce / Stun / Chain / Explosive / MultiShot / Pull / Quick
        /// </summary>
        public string EffectType
        {
            get;
            private set;
        }

        /// <summary>
        /// 主参数（见各条目说明）
        /// </summary>
        public float EffectParam1
        {
            get;
            private set;
        }

        /// <summary>
        /// 副参数（见各条目说明）
        /// </summary>
        public float EffectParam2
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
            TraitId = columnStrings[index++];
            Name = columnStrings[index++];
            Description = columnStrings[index++];
            EffectType = columnStrings[index++];
            EffectParam1 = float.Parse(columnStrings[index++]);
            EffectParam2 = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    TraitId = binaryReader.ReadString();
                    Name = binaryReader.ReadString();
                    Description = binaryReader.ReadString();
                    EffectType = binaryReader.ReadString();
                    EffectParam1 = binaryReader.ReadSingle();
                    EffectParam2 = binaryReader.ReadSingle();
                }
            }

            return true;
        }
}
