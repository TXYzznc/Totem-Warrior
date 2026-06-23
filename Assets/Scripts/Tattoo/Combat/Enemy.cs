using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 敌人：简单追逐 + 攻击玩家。Target 字段与纹身系统共享：
    /// Composer 改 T.Health 时，Enemy 也跟着同步血量。
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("数值")]
        public float Speed          = 3f;
        public float AttackRange    = 1.5f;
        public float AttackInterval = 1.2f;
        public int   AttackDamage   = 8;
        public int   MaxHP          = 50;

        public Target T;
        public Player Owner;
        public bool   WasDeadLastFrame;

        float attackTimer = 0f;

        public bool IsAlive => T != null && T.Health > 0;
        public int  HP      => T != null ? Mathf.RoundToInt(T.Health) : 0;

        void Awake()
        {
            T = new Target { Name = string.IsNullOrEmpty(name) ? "敌人" : name, Health = MaxHP };
        }

        void Update()
        {
            if (!IsAlive)
            {
                if (!WasDeadLastFrame) WasDeadLastFrame = true;
                attackTimer = 0f;
                Respawn();
                return;
            }
            if (Owner == null) return;

            var toPlayer = Owner.transform.position - transform.position;
            toPlayer.y = 0;
            float dist = toPlayer.magnitude;
            if (dist > AttackRange)
                transform.position += toPlayer.normalized * Speed * Time.deltaTime;

            attackTimer -= Time.deltaTime;
            if (dist <= AttackRange && attackTimer <= 0f)
            {
                attackTimer = AttackInterval;
                Owner.TakeDamage(AttackDamage, this);
            }
        }

        void Respawn()
        {
            T = new Target { Name = name, Health = MaxHP };
            WasDeadLastFrame = false;
            transform.position = new Vector3(
                UnityEngine.Random.Range(-12f, 12f), 0,
                UnityEngine.Random.Range(-12f, 12f));
        }
    }
}
