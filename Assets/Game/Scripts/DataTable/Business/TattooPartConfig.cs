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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/TattooPartConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class TattooPartConfig : DataRowBase
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
        /// Head / Torso / LeftArm / RightArm / LeftLeg / RightLeg
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 事件类名：AttackHitEvent / CritHitEvent / DamagedEvent / SkillCastEvent / DodgePressedEvent / MoveTickEvent
        /// </summary>
        public string TriggerEvent
        {
            get;
            private set;
        }

        /// <summary>
        /// StatType enum 名
        /// </summary>
        public string ScaleStat
        {
            get;
            private set;
        }

        /// <summary>
        /// None / Arms / Legs
        /// </summary>
        public string SymmetryGroup
        {
            get;
            private set;
        }

        /// <summary>
        /// 缩放系数
        /// </summary>
        public float ScaleFactor
        {
            get;
            private set;
        }

        /// <summary>
        /// 被动维度标签（暴击/抗性/技能/武器/闪避/移速）
        /// </summary>
        public string PassiveDimension
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
            TriggerEvent = columnStrings[index++];
            ScaleStat = columnStrings[index++];
            SymmetryGroup = columnStrings[index++];
            ScaleFactor = float.Parse(columnStrings[index++]);
            PassiveDimension = columnStrings[index++];

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
                    TriggerEvent = binaryReader.ReadString();
                    ScaleStat = binaryReader.ReadString();
                    SymmetryGroup = binaryReader.ReadString();
                    ScaleFactor = binaryReader.ReadSingle();
                    PassiveDimension = binaryReader.ReadString();
                }
            }

            return true;
        }
}
