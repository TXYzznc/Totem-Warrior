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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/SkillConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class SkillConfig : DataRowBase
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
        /// 唯一技能 ID，如 skill_fireball_01
        /// </summary>
        public string SkillId
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
        /// 0=纯冷却 / 1=充能 / 2=蓄力释放
        /// </summary>
        public int ChargeModel
        {
            get;
            private set;
        }

        /// <summary>
        /// 冷却时长(s)，ChargeModel=0 时生效
        /// </summary>
        public float Cooldown
        {
            get;
            private set;
        }

        /// <summary>
        /// 最大充能层数，ChargeModel=1 时生效
        /// </summary>
        public int MaxCharges
        {
            get;
            private set;
        }

        /// <summary>
        /// 单层恢复时间(s)，ChargeModel=1 时生效
        /// </summary>
        public float ChargeRegenTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 蓄力满充时长(s)，ChargeModel=2 时生效
        /// </summary>
        public float HoldDuration
        {
            get;
            private set;
        }

        /// <summary>
        /// 过载窗口时长(s)，ChargeModel=2 时生效
        /// </summary>
        public float OverchargeWindow
        {
            get;
            private set;
        }

        /// <summary>
        /// 预备帧 (60fps 基准)
        /// </summary>
        public int StartupFrames
        {
            get;
            private set;
        }

        /// <summary>
        /// 激活帧 (60fps 基准)
        /// </summary>
        public int ActiveFrames
        {
            get;
            private set;
        }

        /// <summary>
        /// 恢复帧 (60fps 基准)
        /// </summary>
        public int RecoveryFrames
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害倍率，乘以 BaseDamage
        /// </summary>
        public float DamageMul
        {
            get;
            private set;
        }

        /// <summary>
        /// single / circle / line / cone
        /// </summary>
        public string HitShape
        {
            get;
            private set;
        }

        /// <summary>
        /// 命中范围半径(m)，single 时忽略
        /// </summary>
        public float HitRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 元素染色：Fire / Frost / Lightning / Holy / Nature / None
        /// </summary>
        public string Element
        {
            get;
            private set;
        }

        /// <summary>
        /// Startup 帧内是否允许闪避取消
        /// </summary>
        public bool CancelableByDodge
        {
            get;
            private set;
        }

        /// <summary>
        /// 商人出售 ItemId，0 表示非商品
        /// </summary>
        public int ItemId
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
            SkillId = columnStrings[index++];
            Name = columnStrings[index++];
            ChargeModel = int.Parse(columnStrings[index++]);
            Cooldown = float.Parse(columnStrings[index++]);
            MaxCharges = int.Parse(columnStrings[index++]);
            ChargeRegenTime = float.Parse(columnStrings[index++]);
            HoldDuration = float.Parse(columnStrings[index++]);
            OverchargeWindow = float.Parse(columnStrings[index++]);
            StartupFrames = int.Parse(columnStrings[index++]);
            ActiveFrames = int.Parse(columnStrings[index++]);
            RecoveryFrames = int.Parse(columnStrings[index++]);
            DamageMul = float.Parse(columnStrings[index++]);
            HitShape = columnStrings[index++];
            HitRadius = float.Parse(columnStrings[index++]);
            Element = columnStrings[index++];
            CancelableByDodge = bool.Parse(columnStrings[index++]);
            ItemId = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SkillId = binaryReader.ReadString();
                    Name = binaryReader.ReadString();
                    ChargeModel = binaryReader.Read7BitEncodedInt32();
                    Cooldown = binaryReader.ReadSingle();
                    MaxCharges = binaryReader.Read7BitEncodedInt32();
                    ChargeRegenTime = binaryReader.ReadSingle();
                    HoldDuration = binaryReader.ReadSingle();
                    OverchargeWindow = binaryReader.ReadSingle();
                    StartupFrames = binaryReader.Read7BitEncodedInt32();
                    ActiveFrames = binaryReader.Read7BitEncodedInt32();
                    RecoveryFrames = binaryReader.Read7BitEncodedInt32();
                    DamageMul = binaryReader.ReadSingle();
                    HitShape = binaryReader.ReadString();
                    HitRadius = binaryReader.ReadSingle();
                    Element = binaryReader.ReadString();
                    CancelableByDodge = binaryReader.ReadBoolean();
                    ItemId = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }
}
