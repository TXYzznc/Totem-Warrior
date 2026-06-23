using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 玩家：暴露 6 事件源到 Composer。所有动作公开 (DoAttack/DoSkill/DoDodge/DoMove/TakeDamage) 以便测试。
    /// 基础数值（武器伤害5、技能伤害25）作为"事件载体"，纹身效果在事件发生时按合成规则叠加伤害到目标。
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("数值")]
        public float Speed       = 8f;
        public int   MaxHP       = 200;
        public int   HP;
        public float CritRate    = 0.3f;
        public float AttackRange = 2.5f;
        public float SkillRange  = 5f;
        public int   AttackDamage = 5;
        public int   SkillDamage  = 25;
        public float SkillCDDuration   = 2f;
        public float DodgeCDDuration   = 1f;
        public float DodgeIFrameDuration = 0.3f;
        public float MoveTickInterval  = 0.5f;

        [Header("状态")]
        public float SkillCD;
        public float DodgeCD;
        public float DodgeIFrame;
        public int   Kills;

        public TattooComposer Composer { get; set; }
        public List<Enemy>    Enemies = new();

        public event Action<string> OnLog;

        /// <summary>最近触发的 EffectResult，UI 实时显示用</summary>
        public readonly List<EffectResult> RecentEffects = new();
        public int MaxRecentEffects = 25;

        float moveTickTimer = 0f;

        void Awake()
        {
            Composer = new TattooComposer();
            Composer.Player = new PlayerSelf { Name = "玩家", Health = MaxHP };
            HP = MaxHP;
        }

        void Update()
        {
            if (HP <= 0) return;
            var dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            DoMove(dir);
            if (Input.GetMouseButtonDown(0)) DoAttack();
            if (Input.GetKeyDown(KeyCode.E)) DoSkill();
            if (Input.GetKeyDown(KeyCode.Space)) DoDodge();

            SkillCD     = Mathf.Max(0, SkillCD - Time.deltaTime);
            DodgeCD     = Mathf.Max(0, DodgeCD - Time.deltaTime);
            DodgeIFrame = Mathf.Max(0, DodgeIFrame - Time.deltaTime);
        }

        // ---------- 公开动作 API（测试 + Update 共用）----------

        public void DoMove(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            transform.position += dir.normalized * Speed * Time.deltaTime;
            moveTickTimer += Time.deltaTime;
            if (moveTickTimer >= MoveTickInterval)
            {
                moveTickTimer = 0f;
                FireMoveTick();
            }
        }

        public void DoAttack()
        {
            var nearby = EnemiesInRange(AttackRange);
            if (nearby.Count == 0) { Log("普攻（无目标）"); return; }
            var ctx = MakeCtx(nearby);
            bool crit = UnityEngine.Random.value < CritRate;
            // 基础普攻伤害（武器伤害）
            float hpBefore = nearby[0].T.Health;
            nearby[0].T.Health -= AttackDamage;
            // 触发纹身事件
            Composer.Fire(GameEvent.OnAttack, ctx);
            if (crit) Composer.Fire(GameEvent.OnCrit, ctx);
            CheckKills(nearby);
            Report(crit ? "普攻(暴击)" : "普攻", ctx, hpBefore - nearby[0].T.Health);
        }

        public void DoSkill()
        {
            if (SkillCD > 0) { Log($"技能冷却中（剩 {SkillCD:F1}s）"); return; }
            SkillCD = SkillCDDuration;
            var nearby = EnemiesInRange(SkillRange);
            var ctx = MakeCtx(nearby);
            if (nearby.Count > 0)
            {
                float before = nearby[0].T.Health;
                nearby[0].T.Health -= SkillDamage;
                Composer.Fire(GameEvent.OnSkillCast, ctx);
                CheckKills(nearby);
                Report("技能", ctx, before - nearby[0].T.Health);
            }
            else
            {
                Composer.Fire(GameEvent.OnSkillCast, ctx);
                Report("技能(空放)", ctx, 0);
            }
        }

        public void DoDodge()
        {
            if (DodgeCD > 0) { Log($"闪避冷却中（剩 {DodgeCD:F1}s）"); return; }
            DodgeCD     = DodgeCDDuration;
            DodgeIFrame = DodgeIFrameDuration;
            var ctx = new EffectContext { Self = Composer.Player };
            Composer.Fire(GameEvent.OnDodgePressed, ctx);
            Report("闪避", ctx, 0);
        }

        public void TakeDamage(int dmg, Enemy attacker)
        {
            if (DodgeIFrame > 0) { Log($"无敌帧躲过 {attacker.T.Name} 的攻击"); return; }
            HP -= dmg;
            var ctx = new EffectContext
            {
                LastAttacker  = attacker.T,
                PrimaryTarget = attacker.T,
            };
            float hpBefore = attacker.T.Health;
            Composer.Fire(GameEvent.OnDamaged, ctx);
            CheckKills(new List<Enemy> { attacker });
            Report($"受到 {attacker.T.Name} 攻击({dmg})", ctx, hpBefore - attacker.T.Health);
            if (HP <= 0) Respawn();
        }

        void FireMoveTick()
        {
            var nearby = EnemiesInRange(AttackRange * 1.6f);
            if (nearby.Count == 0) return;
            var ctx = MakeCtx(nearby);
            float before = nearby[0].T.Health;
            Composer.Fire(GameEvent.OnMoveTick, ctx);
            CheckKills(nearby);
            if (ctx.Log.Count > 0) Report("移动 tick", ctx, before - nearby[0].T.Health);
        }

        // ---------- 工具 ----------

        EffectContext MakeCtx(List<Enemy> nearby)
        {
            var ctx = new EffectContext { Self = Composer.Player };
            if (nearby.Count > 0) ctx.PrimaryTarget = nearby[0].T;
            for (int i = 1; i < nearby.Count; i++) ctx.NearbyTargets.Add(nearby[i].T);
            // 路径上的敌人 = NearbyTargets（简化）
            foreach (var t in ctx.NearbyTargets) ctx.MovementPath.Add(t);
            return ctx;
        }

        List<Enemy> EnemiesInRange(float range)
        {
            var r = new List<Enemy>();
            foreach (var e in Enemies)
            {
                if (e == null || !e.IsAlive) continue;
                if (Vector3.Distance(e.transform.position, transform.position) <= range) r.Add(e);
            }
            return r;
        }

        void CheckKills(List<Enemy> potential)
        {
            foreach (var e in potential)
                if (e != null && e.T.Health <= 0 && !e.WasDeadLastFrame)
                {
                    Kills++;
                    e.WasDeadLastFrame = true;
                }
        }

        void Respawn()
        {
            Log("玩家死亡，重生");
            HP = MaxHP;
            transform.position = Vector3.zero;
            if (Composer.Player != null) Composer.Player.Health = MaxHP;
        }

        void Report(string action, EffectContext ctx, float damageDealt)
        {
            Log($"【{action}】触发 {ctx.Log.Count} 条效果，主目标承担 {damageDealt:F1} 伤害");
            foreach (var r in ctx.Log)
            {
                Log($"  · {r}");
                RecentEffects.Add(r);
                while (RecentEffects.Count > MaxRecentEffects) RecentEffects.RemoveAt(0);
            }
        }

        void Log(string s)
        {
            OnLog?.Invoke(s);
            Debug.Log("[战斗] " + s);
        }
    }
}
