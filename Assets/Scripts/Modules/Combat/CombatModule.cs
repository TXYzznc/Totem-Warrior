using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// v2.1 战斗主循环：
    /// - 维护 IPlayerController 列表（玩家 1 + Bot 49）
    /// - 每帧遍历 controllers，消费意图 → 发战斗事件
    /// - 不再直接轮询 InputModule（HumanPlayerController 内部接 Input）
    /// - 玩家正在自纹身读条时，跳过攻击/蓄力/闪避（保留移动 + 技能逃生）
    /// </summary>
    public sealed class CombatModule : IGameModule, ITickable
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[] {
            typeof(TattooModule), typeof(SpawnerModule), typeof(InputModule)
        };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        TattooModule  _tattoo;
        SpawnerModule _spawner;
        InputModule   _input;

        readonly List<Combat.IPlayerController> _controllers = new();
        readonly Dictionary<Combat.IPlayerController, float> _moveTickAccum = new();
        const float MoveTickInterval = 0.5f;

        bool _combatEnded;
        bool _runStarted;

        // change#20: 蓄力判定阈值（秒）。与 HumanPlayerController.ChargeThreshold 保持一致（0.4s）。
        const float ChargeThreshold = 0.4f;

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

            // 装配玩家 HumanPlayerController（Bot controllers 由 BotControllerModule 注册）
            // WeaponModule 在 GameApp 中注册早于 CombatModule，InitAsync 时可安全 GetModule
            if (_spawner.PlayerTarget != null)
            {
                var weapon = _runner.GetModule<WeaponModule>();
                RegisterController(new Combat.HumanPlayerController(
                    _spawner.PlayerTarget, _input, _spawner, weapon));
            }

            FrameworkLogger.Info("CombatModule", "Action=Initialized v2.1");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _controllers.Clear();
            FrameworkLogger.Info("CombatModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        /// <summary>由 BotControllerModule 在 Spawner 完成后注册各 Bot controller。</summary>
        public void RegisterController(Combat.IPlayerController controller)
        {
            if (controller == null || _controllers.Contains(controller)) return;
            _controllers.Add(controller);
            _moveTickAccum[controller] = 0f;
        }

        public void UnregisterController(Combat.IPlayerController controller)
        {
            if (controller == null) return;
            _controllers.Remove(controller);
            _moveTickAccum.Remove(controller);
        }

        public void OnUpdate(float dt)
        {
            // Esc 弹暂停（v2.1：InputModule 不发事件，由 CombatModule 在 Tick 内桥接）
            if (_input.IsEscapePressed())
                _bus.Publish(new PauseRequestedEvent());

            // Tab 开/关自助纹身面板（同样桥接：Form 起始 inactive，Update 不跑，必须事件驱动）
            if (_input.IsSelfTattooTogglePressed())
                _bus.Publish(new SelfTattooToggleRequestedEvent());

            if (_combatEnded) return;

            for (int i = 0; i < _controllers.Count; i++)
            {
                var c = _controllers[i];
                if (c?.OwnerActor == null) continue;
                if (c.OwnerActor.Health <= 0f) continue;
                ProcessController(c, dt);
            }

            _tattoo.TickInProgressTattoos();
        }

        void ProcessController(Combat.IPlayerController c, float dt)
        {
            // v2.1: 玩家正在自纹身读条 → 跳过攻击/蓄力/闪避（保留移动 + 技能）
            bool inTattooReading = _tattoo.IsInProgress(c.OwnerActor);

            // 移动 (玩家用真实 GameObject，Bot 用 EntityRef GameObject)
            var dir = c.GetMoveInput();
            var go = ResolveGameObject(c);
            if (dir.sqrMagnitude > 0.001f && go != null)
            {
                float speed = _tattoo.Stats.MoveSpeed + _tattoo.Player.Passive.MoveSpeedBonus;
                go.transform.position += new Vector3(dir.x, 0, dir.y) * speed * dt;

                if (!_moveTickAccum.ContainsKey(c)) _moveTickAccum[c] = 0f;
                _moveTickAccum[c] += dt;
                if (_moveTickAccum[c] >= MoveTickInterval)
                {
                    _moveTickAccum[c] = 0f;
                    var path = CollectNearbyEnemies(go.transform.position, 3f, 4);
                    _bus.Publish(new MoveTickEvent(path, dir.magnitude * speed * MoveTickInterval));
                }
            }

            if (inTattooReading) return; // 跳过下面攻击类意图

            // 普攻 / 蓄力攻击
            // change#20: 删除原 25% 硬编码暴击；WeaponModule.FireWeapon 做命中检测 →
            //   发 WeaponAttackHitEvent → HeadPartBehavior 按 PatternMultiplier 概率决定是否发 CritHitEvent。
            bool wantsCharged = c.ShouldChargedAttack();
            if (c.ShouldAttack() || wantsCharged)
            {
                var t = c.GetAimTarget();
                if (t != null)
                {
                    float chargeRatio = wantsCharged
                        ? Mathf.Clamp01(_input.GetAttackHoldDuration() / ChargeThreshold)
                        : 0f;

                    // CONTRACT §H 方案 A：查升级倍率 → 注入 WeaponModule → FireWeapon
                    var weaponMod   = _runner.GetModule<WeaponModule>();
                    var upgradeMod  = _runner.GetModule<WeaponUpgradeModule>();
                    var equippedId  = weaponMod.GetEquippedWeapon(c.OwnerActor).Weapon?.WeaponId ?? string.Empty;
                    var mul = upgradeMod != null
                        ? upgradeMod.GetMultipliers(c.OwnerActor, equippedId)
                        : WeaponMultipliers.Identity;

                    // 非 Identity 时才写日志（L1 默认 Identity，避免每帧刷屏）
                    if (mul.DamageMul != 1f || mul.RangeAdd != 0f || mul.CooldownMul != 1f)
                    {
                        float baseDmg  = weaponMod.GetBaseDamage(c.OwnerActor);
                        float finalDmg = baseDmg * mul.DamageMul;
                        FrameworkLogger.Info("CombatModule",
                            $"Action=UpgradeMulApplied Actor={c.OwnerActor.Name} WeaponId={equippedId} " +
                            $"BaseDmg={baseDmg:F1} DamageMul={mul.DamageMul:F3} FinalDmg={finalDmg:F1} " +
                            $"RangeAdd={mul.RangeAdd:F2} CooldownMul={mul.CooldownMul:F3}");
                    }

                    weaponMod.SetPendingMultipliers(c.OwnerActor, mul);
                    weaponMod.FireWeapon(c.OwnerActor, t, wantsCharged, chargeRatio);
                }
            }

            // 技能（v2.1: 仅 0/1 槽位）
            for (int slot = 0; slot < 2; slot++)
            {
                if (c.ShouldUseSkill(slot))
                    _bus.Publish(new SkillCastEvent($"slot{slot}"));
            }

            // 闪避
            if (c.ShouldDodge())
                _bus.Publish(new DodgePressedEvent());
        }

        GameObject ResolveGameObject(Combat.IPlayerController c)
        {
            if (c.Type == Combat.PlayerControllerType.Human) return _spawner.Player;
            // Bot：从 Enemies 列表里找匹配 OwnerActor
            foreach (var go in _spawner.Enemies)
            {
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target == c.OwnerActor) return go;
            }
            return null;
        }

        [EventHandler]
        void OnGameStateChanged(GameStateChangedEvent e)
        {
            // 进入 InGame 状态 → 发 RunStartedEvent，HUD 初始化 HP 条
            if (e.NewState == GameState.InGame && !_runStarted && _spawner?.PlayerActor != null)
            {
                _runStarted = true;
                _combatEnded = false;
                _bus.Publish(new RunStartedEvent(
                    _spawner.PlayerActor,
                    seed: UnityEngine.Random.Range(1, int.MaxValue),
                    maxHealth: _spawner.PlayerMaxHp));
                FrameworkLogger.Info("CombatModule",
                    $"Action=RunStarted MaxHp={_spawner.PlayerMaxHp}");
            }
            // 返回主菜单 → 重置 run 状态，允许下一局重新进入
            else if (e.NewState == GameState.MainMenu && _runStarted)
            {
                _runStarted = false;
            }
        }

        [EventHandler]
        void OnEffectApplied(EffectAppliedEvent e)
        {
            // 击杀判定
            foreach (var go in _spawner.Enemies)
            {
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target == null) continue;
                if (er.Target.Health <= 0f && go.activeSelf)
                {
                    go.SetActive(false);
                    _bus.Publish(new TargetKilledEvent(er.Target));
                }
            }

            // 玩家死亡
            var pt = _spawner.PlayerTarget;
            if (pt != null && pt.Health <= 0f)
            {
                _bus.Publish(new PlayerDiedEvent());
                EndCombat(false);
                return;
            }

            // 所有敌人死亡
            bool allDead = true;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                allDead = false; break;
            }
            if (allDead && !_combatEnded) EndCombat(true);
        }

        void EndCombat(bool playerWin)
        {
            _combatEnded = true;
            _bus.Publish(new CombatEndedEvent(playerWin));

            // v2.1 收尾：发 RunEndedEvent 给 SaveModule + 切 GameState 让 RunResultForm 弹出
            var stats = new RunStats { Win = playerWin, Kills = 0, DurationSec = 0f };
            _bus.Publish(new RunEndedEvent(_spawner?.PlayerActor, playerWin, stats));

            var gs = _runner.GetModule<GameStateModule>();
            gs?.GameOver();

            FrameworkLogger.Info("CombatModule", $"Action=CombatEnded PlayerWin={playerWin}");
        }

        Target[] CollectNearbyEnemies(Vector3 origin, float radius, int max)
        {
            var list = new List<Target>();
            float sqr = radius * radius;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null || !go.activeSelf) continue;
                if ((go.transform.position - origin).sqrMagnitude > sqr) continue;
                var er = go.GetComponent<EntityRef>();
                if (er?.Target != null) list.Add(er.Target);
                if (list.Count >= max) break;
            }
            return list.ToArray();
        }
    }
}
