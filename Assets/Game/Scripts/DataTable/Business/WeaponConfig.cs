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
/// Business table migrated from LegacyProjectArchive/Assets/Resources/DataTable/WeaponConfig.json. GF_X Id is numeric; original business keys are preserved as data columns.
/// </summary>
public class WeaponConfig : DataRowBase
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
        /// 主键，如 knife_basic / hammer_heavy / pistol_basic / bow_charge / energy_fist
        /// </summary>
        public string WeaponId
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
        /// Melee / Ranged / Special
        /// </summary>
        public string Class
        {
            get;
            private set;
        }

        /// <summary>
        /// 基础伤害，TattooModule scaleStat 基准
        /// </summary>
        public float BaseDamage
        {
            get;
            private set;
        }

        /// <summary>
        /// AttackSpeedModifier，叠加到 PassiveStats.AttackSpeed
        /// </summary>
        public float AttackSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 近战 Hitbox 球形半径（米），远程为最大有效距离
        /// </summary>
        public float Range
        {
            get;
            private set;
        }

        /// <summary>
        /// 蓄力伤害倍率，覆盖默认 1.5x
        /// </summary>
        public float ChargedMul
        {
            get;
            private set;
        }

        /// <summary>
        /// 远程武器对应飞行物 ID，近战留空
        /// </summary>
        public string ProjectileId
        {
            get;
            private set;
        }

        /// <summary>
        /// 稀有度 0=普通 1=精良 2=稀有 3=史诗
        /// </summary>
        public int Rarity
        {
            get;
            private set;
        }

        /// <summary>
        /// -1=无限（近战），远程为弹匣容量
        /// </summary>
        public int MaxAmmo
        {
            get;
            private set;
        }

        /// <summary>
        /// 前摇帧数（60fps 基准）
        /// </summary>
        public int BaseStartup
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害帧数（Hitbox 开启区间）
        /// </summary>
        public int BaseActive
        {
            get;
            private set;
        }

        /// <summary>
        /// 后摇帧数
        /// </summary>
        public int BaseRecovery
        {
            get;
            private set;
        }

        /// <summary>
        /// true=需蓄力才激活伤害判定（弓）
        /// </summary>
        public bool RequiresCharge
        {
            get;
            private set;
        }

        /// <summary>
        /// change#20: 鼠标射击半角(度)，0=Raycast严格 / 180=自动锁定最近敌 / 中间=SphereCast锥形
        /// </summary>
        public float AimSpreadHalfDeg
        {
            get;
            private set;
        }

        /// <summary>
        /// change#20: 普攻 trait，引用 WeaponTraitConfig.TraitId
        /// </summary>
        public string NormalTraitId
        {
            get;
            private set;
        }

        /// <summary>
        /// change#20: 蓄力 trait，引用 WeaponTraitConfig.TraitId
        /// </summary>
        public string ChargedTraitId
        {
            get;
            private set;
        }

        /// <summary>
        /// change#20: 武器 prefab 在 Resources 下的相对路径（不含扩展名），元气骑士范式动画用
        /// </summary>
        public string WeaponPrefabPath
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
            WeaponId = columnStrings[index++];
            Name = columnStrings[index++];
            Class = columnStrings[index++];
            BaseDamage = float.Parse(columnStrings[index++]);
            AttackSpeed = float.Parse(columnStrings[index++]);
            Range = float.Parse(columnStrings[index++]);
            ChargedMul = float.Parse(columnStrings[index++]);
            ProjectileId = columnStrings[index++];
            Rarity = int.Parse(columnStrings[index++]);
            MaxAmmo = int.Parse(columnStrings[index++]);
            BaseStartup = int.Parse(columnStrings[index++]);
            BaseActive = int.Parse(columnStrings[index++]);
            BaseRecovery = int.Parse(columnStrings[index++]);
            RequiresCharge = bool.Parse(columnStrings[index++]);
            AimSpreadHalfDeg = float.Parse(columnStrings[index++]);
            NormalTraitId = columnStrings[index++];
            ChargedTraitId = columnStrings[index++];
            WeaponPrefabPath = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    WeaponId = binaryReader.ReadString();
                    Name = binaryReader.ReadString();
                    Class = binaryReader.ReadString();
                    BaseDamage = binaryReader.ReadSingle();
                    AttackSpeed = binaryReader.ReadSingle();
                    Range = binaryReader.ReadSingle();
                    ChargedMul = binaryReader.ReadSingle();
                    ProjectileId = binaryReader.ReadString();
                    Rarity = binaryReader.Read7BitEncodedInt32();
                    MaxAmmo = binaryReader.Read7BitEncodedInt32();
                    BaseStartup = binaryReader.Read7BitEncodedInt32();
                    BaseActive = binaryReader.Read7BitEncodedInt32();
                    BaseRecovery = binaryReader.Read7BitEncodedInt32();
                    RequiresCharge = binaryReader.ReadBoolean();
                    AimSpreadHalfDeg = binaryReader.ReadSingle();
                    NormalTraitId = binaryReader.ReadString();
                    ChargedTraitId = binaryReader.ReadString();
                    WeaponPrefabPath = binaryReader.ReadString();
                }
            }

            return true;
        }
}
