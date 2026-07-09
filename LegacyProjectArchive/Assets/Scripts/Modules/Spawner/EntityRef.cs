using Tattoo.Data;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 把 GameObject 与 Tattoo.Data.Target 双向绑定。
    /// SpawnerModule 创建实体时挂上本组件。CombatModule 通过 GetComponent&lt;EntityRef&gt;() 拿到 Target。
    /// </summary>
    public sealed class EntityRef : MonoBehaviour
    {
        public Target Target;
        public bool IsPlayer;
        public float MaxHP = 100f;
    }
}
