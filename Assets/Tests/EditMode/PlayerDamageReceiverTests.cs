using System.Collections.Generic;
using AttackSystem.Events;
using NUnit.Framework;
using Tattoo.Data;
using Tattoo.Events;

namespace Combat.Tests
{
    /// <summary>
    /// TC-Damage-D6/D7 + 死亡防抖 300ms
    ///
    /// PlayerDamageReceiver.ApplyDamage 单元测试。
    ///
    /// 策略：
    ///   - InitializeAsync 依赖 SpawnerModule（会创建 GameObjects），不适合 EditMode。
    ///   - 改为直接调用 ApplyDamage 公共方法，通过反射或构造后手动设置内部字段。
    ///   - 使用 FakeSpawnerModule + FakeModuleRunner 最小桩注入。
    ///   - PlayerTarget 通过直接赋值绕过 InitializeAsync 的 SpawnerModule 查询。
    /// </summary>
    public class PlayerDamageReceiverTests
    {
        EventBus              _bus;
        PlayerDamageReceiver  _receiver;
        Target                _playerTarget;

        List<DamagedEvent>              _damagedEvents  = new();
        List<PlayerHealthChangedEvent>  _healthEvents   = new();
        List<PlayerDiedEvent>           _diedEvents     = new();

        [SetUp]
        public void SetUp()
        {
            _bus          = new EventBus();
            _playerTarget = new Target { Name = "Player", Health = 100f };

            // 构造 FakeSpawnerModule + FakeRunner，注入 PlayerTarget
            var fakeSpawner = new FakeSpawnerModule(_playerTarget, maxHp: 100f);
            var fakeRunner  = new FakeModuleRunner(_bus, fakeSpawner);

            _receiver = new PlayerDamageReceiver(fakeRunner, _bus);

            // 手动模拟 InitializeAsync 已完成的关键字段（避免跑完整 init 创建 GameObject）
            // InitializeAsync 内 _spawner = _runner.GetModule<SpawnerModule>()
            // 借助 FakeRunner.GetModule<T>() 返回 fakeSpawner 来完成这一步
            // （UniTask 版本：EditMode 下同步调用即可）
            _receiver.InitializeAsync().GetAwaiter().GetResult();

            _damagedEvents.Clear();
            _healthEvents.Clear();
            _diedEvents.Clear();

            _bus.Subscribe<DamagedEvent>(e              => _damagedEvents.Add(e));
            _bus.Subscribe<PlayerHealthChangedEvent>(e  => _healthEvents.Add(e));
            _bus.Subscribe<PlayerDiedEvent>(_ => _diedEvents.Add(new PlayerDiedEvent()));
        }

        // ════════════════════════════════════════════════
        // TC-Damage-D6: 敌人普攻 ApplyDamage 扣血
        // ════════════════════════════════════════════════

        [Test]
        public void ApplyDamage_ReducesPlayerHP_AndEmitsEvents()
        {
            _receiver.ApplyDamage(20f, "Enemy");

            Assert.AreEqual(80f, _playerTarget.Health, 0.001f, "Health 应扣减 20");
            Assert.AreEqual(80f, _receiver.CurrentHP,  0.001f, "CurrentHP 缓存同步");

            Assert.AreEqual(1, _damagedEvents.Count, "应发 DamagedEvent");
            Assert.AreEqual(20f, _damagedEvents[0].Damage,  0.001f);
            Assert.AreEqual(80f, _damagedEvents[0].NewHp,   0.001f);

            Assert.AreEqual(1, _healthEvents.Count, "应发 PlayerHealthChangedEvent");
            Assert.AreEqual(80f, _healthEvents[0].Current, 0.001f);
            Assert.AreEqual(-20f, _healthEvents[0].Delta,  0.001f);

            Assert.AreEqual(0, _diedEvents.Count, "HP>0 时不应发 PlayerDiedEvent");
        }

        [Test]
        public void ApplyDamage_ZeroAmount_EmitsNothing()
        {
            _receiver.ApplyDamage(0f, "Enemy");

            Assert.AreEqual(100f, _playerTarget.Health, 0.001f, "damage=0 时 HP 不变");
            Assert.AreEqual(0, _damagedEvents.Count,  "damage=0 时不发 DamagedEvent");
            Assert.AreEqual(0, _healthEvents.Count,   "damage=0 时不发 HealthChanged");
            Assert.AreEqual(0, _diedEvents.Count,     "damage=0 时不发 Died");
        }

        [Test]
        public void ApplyDamage_NegativeAmount_EmitsNothing()
        {
            _receiver.ApplyDamage(-10f, "Heal");

            Assert.AreEqual(100f, _playerTarget.Health, 0.001f, "负伤害忽略，HP 不变");
            Assert.AreEqual(0, _damagedEvents.Count);
        }

        // ════════════════════════════════════════════════
        // TC-Damage-D7: 大额伤害 HP clamp 至 0
        // ════════════════════════════════════════════════

        [Test]
        public void ApplyDamage_ExceedsHP_ClampsToZero_AndEmitsDied()
        {
            _receiver.ApplyDamage(150f, "EnemySkill");

            Assert.AreEqual(0f, _playerTarget.Health, 0.001f, "HP 不应低于 0");
            Assert.AreEqual(0f, _receiver.CurrentHP,  0.001f);
            Assert.AreEqual(1, _diedEvents.Count, "HP 归零应发 PlayerDiedEvent");
        }

        [Test]
        public void ApplyDamage_Sequential_FirstKillsPlayer_SecondDoesNotRepeatDied()
        {
            // 第 1 次：HP 30 → 0
            _playerTarget.Health = 30f;
            _receiver.ApplyDamage(30f, "EnemyNormal");
            Assert.AreEqual(1, _diedEvents.Count, "第一次致死应发 PlayerDiedEvent");

            // 第 2 次：_diedFired=true 且 _dying=true（300ms 内），不重发
            _receiver.ApplyDamage(10f, "EnemyNormal");
            Assert.AreEqual(1, _diedEvents.Count, "300ms 内第二次伤害不应重发 PlayerDiedEvent");
        }

        [Test]
        public void ApplyDamage_HealthChangedEvent_DeltaIsNegative()
        {
            _receiver.ApplyDamage(35f, "EnemySkill");

            Assert.AreEqual(-35f, _healthEvents[0].Delta, 0.001f,
                "PlayerHealthChangedEvent.Delta 应为负值（伤害方向）");
        }

        // ════════════════════════════════════════════════
        // 桩：FakeSpawnerModule + FakeModuleRunner
        // ════════════════════════════════════════════════

        /// <summary>
        /// 最小 SpawnerModule 桩：提供 PlayerTarget + PlayerMaxHp。
        /// 注意 SpawnerModule 是 sealed，不能直接继承。
        /// PlayerDamageReceiver.InitializeAsync 调用 _runner.GetModule&lt;SpawnerModule&gt;()。
        /// 因此 FakeModuleRunner.GetModule&lt;T&gt;() 须返回真实 SpawnerModule 实例，
        /// 但该实例的 InitializeAsync 不调用（跳过 CreateScene）。
        ///
        /// 实现方式：构造一个 SpawnerModule 但不 Init，
        /// 再用反射直接写入 _playerTarget 和 _playerMaxHp 私有字段。
        /// </summary>
        class FakeSpawnerModule
        {
            public readonly SpawnerModule Real;

            public FakeSpawnerModule(Target playerTarget, float maxHp)
            {
                // 用同一个 EventBus 构造（此时未使用）
                var innerBus    = new EventBus();
                var innerRunner = new ModuleRunner(innerBus);
                Real = new SpawnerModule(innerRunner, innerBus);

                // 反射写入 PlayerTarget（只读属性，走 backing field）
                var type = typeof(SpawnerModule);
                SetAutoProperty(type, Real, "PlayerTarget", playerTarget);
                SetAutoProperty(type, Real, "PlayerMaxHp",  maxHp);
            }

            static void SetAutoProperty(System.Type type, object target, string propName, object value)
            {
                // Unity C# auto-property backing field 命名：<PropName>k__BackingField
                var field = type.GetField($"<{propName}>k__BackingField",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                // 兜底：尝试通过属性 setter（如果是只读属性则跳过）
                var prop = type.GetProperty(propName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                prop?.SetValue(target, value);
            }
        }

        /// <summary>
        /// FakeModuleRunner：GetModule&lt;SpawnerModule&gt;() 返回 FakeSpawnerModule.Real。
        /// ModuleRunner 是普通类，可以直接子类化并 override GetModule。
        /// 但 ModuleRunner.GetModule 是非虚方法，需用 new 关键字隐藏或使用接口桩。
        ///
        /// 替代方案：向 FakeRunner 的内部 _moduleMap 注入 SpawnerModule 实例。
        /// </summary>
        class FakeModuleRunner : ModuleRunner
        {
            public FakeModuleRunner(EventBus bus, FakeSpawnerModule fakeSpawner) : base(bus)
            {
                // 将 fakeSpawner.Real 注册到内部 map，使 GetModule&lt;SpawnerModule&gt;() 能返回
                // ModuleRunner.AddModule 是 public，但会触发依赖检查和注册流程。
                // 改用反射直接插入 _moduleMap：
                var mapField = typeof(ModuleRunner).GetField("_moduleMap",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (mapField?.GetValue(this) is System.Collections.Generic.Dictionary<System.Type, IGameModule> map)
                {
                    map[typeof(SpawnerModule)] = fakeSpawner.Real;
                }
            }
        }
    }
}
