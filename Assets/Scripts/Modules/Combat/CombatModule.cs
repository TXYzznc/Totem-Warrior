using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 战斗主循环：
    /// - 在 OnUpdate 中轮询 InputModule，把高层动作转为 Tattoo 战斗事件
    /// - 监听 EffectAppliedEvent → 判定目标击杀
    /// - 击杀 / 玩家死亡 → 发 TargetKilledEvent / PlayerDiedEvent / CombatEndedEvent
    ///
    /// 输入不走 Update 直接读 KeyCode；全部通过 InputModule。
    /// </summary>
    public sealed class CombatModule : IGameModule, ITickable
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[] { typeof(TattooModule), typeof(SpawnerModule), typeof(InputModule) };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        TattooModule  _tattoo;
        SpawnerModule _spawner;
        InputModule   _input;

        float _moveTickAccum;
        const float MoveTickInterval = 0.5f;

        bool _combatEnded;

        public CombatModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            _tattoo  = _runner.GetModule<TattooModule>();
            _spawner = _runner.GetModule<SpawnerModule>();
            _input   = _runner.GetModule<InputModule>();

            FrameworkLogger.Info("CombatModule", "Action=Initialized");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            FrameworkLogger.Info("CombatModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ===== 输入 → 事件 =====

        public void OnUpdate(float dt)
        {
            if (_combatEnded) return;

            // 移动
            var dir = _input.GetMoveDirection();
            if (dir.sqrMagnitude > 0.001f && _spawner.Player != null)
            {
                float speed = _tattoo.Stats.MoveSpeed + _tattoo.Player.Passive.MoveSpeedBonus;
                _spawner.Player.transform.position += new Vector3(dir.x, 0, dir.y) * speed * dt;

                _moveTickAccum += dt;
                if (_moveTickAccum >= MoveTickInterval)
                {
                    _moveTickAccum = 0f;
                    var path = CollectNearbyEnemies(_spawner.Player.transform.position, radius: 3f, max: 4);
                    _bus.Publish(new MoveTickEvent(path, dir.magnitude * speed * MoveTickInterval));
                }
            }

            // 普攻
            if (_input.IsAttackPressed())
            {
                var t = FindClosestEnemyTarget();
                if (t != null)
                {
                    bool crit = UnityEngine.Random.value < 0.25f; // 占位 25% 暴击率
                    if (crit) _bus.Publish(new CritHitEvent(t, _tattoo.Stats.WeaponDamage));
                    else      _bus.Publish(new AttackHitEvent(t, _tattoo.Stats.WeaponDamage));
                }
            }

            // 技能
            if (_input.IsSkillPressed())
            {
                _bus.Publish(new SkillCastEvent("default"));
            }

            // 闪避
            if (_input.IsDodgePressed())
            {
                _bus.Publish(new DodgePressedEvent());
            }
        }

        // ===== 监听结果 =====

        [EventHandler]
        void OnEffectApplied(EffectAppliedEvent e)
        {
            // 击杀判定：扫描所有敌人 EntityRef
            foreach (var go in _spawner.Enemies)
            {
                if (go == null) continue;
                var entRef = go.GetComponent<EntityRef>();
                if (entRef == null || entRef.Target == null) continue;

                if (entRef.Target.Health <= 0f && go.activeSelf)
                {
                    go.SetActive(false);
                    _bus.Publish(new TargetKilledEvent(entRef.Target));
                }
            }

            // 玩家死亡
            var playerTarget = _spawner.PlayerTarget;
            if (playerTarget != null && playerTarget.Health <= 0f)
            {
                _bus.Publish(new PlayerDiedEvent());
                EndCombat(playerWin: false);
                return;
            }

            // 所有敌人死亡
            bool allDead = true;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                allDead = false;
                break;
            }
            if (allDead && !_combatEnded) EndCombat(playerWin: true);
        }

        void EndCombat(bool playerWin)
        {
            _combatEnded = true;
            _bus.Publish(new CombatEndedEvent(playerWin));
            FrameworkLogger.Info("CombatModule", $"Action=CombatEnded PlayerWin={playerWin}");
        }

        // ===== 工具 =====

        Target FindClosestEnemyTarget()
        {
            if (_spawner.Player == null) return null;
            var pp = _spawner.Player.transform.position;
            float min = float.MaxValue;
            Target closest = null;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                var entRef = go.GetComponent<EntityRef>();
                if (entRef == null || entRef.Target == null) continue;

                float d = (go.transform.position - pp).sqrMagnitude;
                if (d < min)
                {
                    min = d;
                    closest = entRef.Target;
                }
            }
            return closest;
        }

        Target[] CollectNearbyEnemies(Vector3 origin, float radius, int max)
        {
            var list = new System.Collections.Generic.List<Target>();
            float sqr = radius * radius;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                if ((go.transform.position - origin).sqrMagnitude > sqr) continue;
                var entRef = go.GetComponent<EntityRef>();
                if (entRef?.Target != null) list.Add(entRef.Target);
                if (list.Count >= max) break;
            }
            return list.ToArray();
        }
    }
}
