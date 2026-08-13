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
/// 第一版 Bot 行为预设。纹身构筑由 FirstPlayableBotBuildPlanner 独立处理；此表不再包含旧元素、技能、武器或附魔字段。
/// </summary>
public class BotBuildPreset : DataRowBase
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
        /// 稳定行为预设 ID。
        /// </summary>
        public int PresetId
        {
            get;
            private set;
        }

        /// <summary>
        /// 调试显示名。
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Rush / Camp / Pivot / Hybrid。
        /// </summary>
        public string BehaviorMacro
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
            PresetId = int.Parse(columnStrings[index++]);
            Name = columnStrings[index++];
            BehaviorMacro = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PresetId = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    BehaviorMacro = binaryReader.ReadString();
                }
            }

            return true;
        }
}
