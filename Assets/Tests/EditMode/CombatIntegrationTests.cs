using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Tattoo;

namespace Tattoo.Tests
{
    public class CombatIntegrationTests
    {
        [SetUp]
        public void SetUp()
        {
            // 清理任何残留 GameObject 防止测试间污染
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == "PlayerTest" || go.name == "EnemyTest")
                    Object.DestroyImmediate(go);
            }
            UnityEngine.Random.InitState(12345);
        }

        static (GameObject pgo, Player p, GameObject ego, Enemy e) MakePlayerAndEnemy(float enemyDistance = 1f)
        {
            var pgo = new GameObject("PlayerTest");
            var p = pgo.AddComponent<Player>();
            // 在 EditMode 测试 batch 中，Awake 不一定被调用，手动 fallback 初始化
            if (p.Composer == null)
            {
                p.Composer = new TattooComposer();
                p.Composer.Player = new PlayerSelf { Name = "玩家", Health = p.MaxHP };
                p.HP = p.MaxHP;
            }
            var ego = new GameObject("EnemyTest");
            ego.transform.position = new Vector3(enemyDistance, 0, 0);
            var e = ego.AddComponent<Enemy>();
            e.Owner = p;
            e.MaxHP = 1000;
            e.T = new Target { Name = "敌人测试", Health = 1000 };
            // 确保 Enemies list 存在
            if (p.Enemies == null) p.Enemies = new List<Enemy>();
            p.Enemies.Add(e);
            return (pgo, p, ego, e);
        }

        static BodyPart Head()    => AtomFactory.MakePart<HeadPart>    ("Head",     GameEvent.OnCrit,         StatType.CritMultiplier);
        static BodyPart Torso()   => AtomFactory.MakePart<TorsoPart>   ("Torso",    GameEvent.OnDamaged,      StatType.MaxHealth);
        static BodyPart RArm()    => AtomFactory.MakePart<RightArmPart>("RightArm", GameEvent.OnAttack,       StatType.WeaponDamage,  SymmetryGroup.Arms);
        static BodyPart LLeg()    => AtomFactory.MakePart<LeftLegPart> ("LeftLeg",  GameEvent.OnDodgePressed, StatType.DodgeFrames,   SymmetryGroup.Legs);

        static ColorSO Red()   => AtomFactory.MakeColor<FireElement>     ("Red");
        static PatternSO Line() => AtomFactory.MakePattern<SingleHitShape>("Line");

        [Test]
        public void Combat_PlayerAttack_DamagesNearbyEnemy_WithBaseAttackOnly()
        {
            var (pgo, p, ego, e) = MakePlayerAndEnemy(1f);
            try
            {
                // 诊断信息
                string diag = $"Composer={p.Composer != null} EnemyCount={p.Enemies.Count} ePos={ego.transform.position} pPos={pgo.transform.position} eT={e.T != null} eHp={(e.T != null ? e.T.Health.ToString() : "null")}";
                p.DoAttack();
                Assert.Less(e.T.Health, 1000f, $"敌人应受到至少基础普攻伤害。DIAG: {diag}, after dmg eHp={e.T.Health}");
            }
            finally
            {
                Object.DestroyImmediate(pgo);
                Object.DestroyImmediate(ego);
            }
        }

        [Test]
        public void Combat_PlayerAttack_WithRArmRedLineEquipped_DealsExtraOnAttackEffect()
        {
            var (pgo, p, ego, e) = MakePlayerAndEnemy(1f);
            try
            {
                // baseline 普攻无装备
                p.DoAttack();
                float baseDmg = 1000f - e.T.Health;
                e.T.Health = 1000f;

                // 装 RArm + Red + Line（OnAttack 触发，加单体火伤）
                p.Composer.Equip(new TattooSlot { Part = RArm(), Color = Red(), Pattern = Line() });
                p.DoAttack();
                float withTattooDmg = 1000f - e.T.Health;

                Assert.Greater(withTattooDmg, baseDmg, "装备右臂红线后应造成更多伤害");
                Assert.IsTrue(e.T.Statuses.Any(s => s.StartsWith("Burn")), "应附加燃烧 DOT");
            }
            finally
            {
                Object.DestroyImmediate(pgo);
                Object.DestroyImmediate(ego);
            }
        }

        [Test]
        public void Combat_EnemyAttack_WithTorsoRedLine_ReflectsToAttacker()
        {
            var (pgo, p, ego, e) = MakePlayerAndEnemy(1f);
            try
            {
                p.Composer.Equip(new TattooSlot { Part = Torso(), Color = Red(), Pattern = Line() });

                float attackerBefore = e.T.Health;
                p.TakeDamage(10, e);

                Assert.Less(e.T.Health, attackerBefore, "Torso 红线应当把伤害反弹回攻击者");
                Assert.IsTrue(e.T.Statuses.Any(s => s.StartsWith("Burn")), "攻击者应被点燃");
            }
            finally
            {
                Object.DestroyImmediate(pgo);
                Object.DestroyImmediate(ego);
            }
        }

        [Test]
        public void Combat_PlayerDodge_GrantsIFrame_AndPackagesPendingTrigger()
        {
            var (pgo, p, ego, e) = MakePlayerAndEnemy(1f);
            try
            {
                p.Composer.Equip(new TattooSlot { Part = LLeg(), Color = Red(), Pattern = Line() });

                p.DoDodge();
                Assert.Greater(p.DodgeIFrame, 0f, "闪避后应有无敌帧");
                Assert.AreEqual(1, p.Composer.Player.PendingTriggers.Count, "应有一个 PendingTrigger 排队");

                // 在无敌帧内受到伤害应被免疫
                int hpBefore = p.HP;
                p.TakeDamage(10, e);
                Assert.AreEqual(hpBefore, p.HP, "无敌帧期间应免疫敌人伤害");

                // 关掉 IFrame，下次普攻会消耗 PendingTrigger
                p.DodgeIFrame = 0f;
                float enemyHpBefore = e.T.Health;
                p.DoAttack();
                Assert.Less(e.T.Health, enemyHpBefore, "普攻应造成伤害");
                Assert.AreEqual(0, p.Composer.Player.PendingTriggers.Count, "PendingTrigger 应被消耗");
            }
            finally
            {
                Object.DestroyImmediate(pgo);
                Object.DestroyImmediate(ego);
            }
        }

        [Test]
        public void Combat_BuildSwap_ChangesNextAttackBehavior()
        {
            var (pgo, p, ego, e) = MakePlayerAndEnemy(1f);
            try
            {
                // 装备 1: RArm × Red × Line（火）
                p.Composer.Equip(new TattooSlot { Part = RArm(), Color = Red(), Pattern = Line() });
                p.DoAttack();
                bool hadBurn = e.T.Statuses.Any(s => s.StartsWith("Burn"));
                Assert.IsTrue(hadBurn);

                // 清空 + 重装备 2: RArm × Yellow × Line（雷）
                p.Composer = new TattooComposer();
                p.Composer.Player = new PlayerSelf { Name = "玩家", Health = p.MaxHP };
                p.Composer.Equip(new TattooSlot
                {
                    Part    = RArm(),
                    Color   = AtomFactory.MakeColor<LightningElement>("Yellow"),
                    Pattern = Line(),
                });
                e.T = new Target { Name = "敌人测试", Health = 1000 };
                p.DoAttack();
                bool hasParalyze = e.T.Statuses.Any(s => s.StartsWith("Paralyze"));
                bool hasNoBurnInNewStatus = !e.T.Statuses.Any(s => s.StartsWith("Burn"));
                Assert.IsTrue(hasParalyze, "切到雷电后应附加麻痹");
                Assert.IsTrue(hasNoBurnInNewStatus, "切到雷电后不应再附加燃烧");
            }
            finally
            {
                Object.DestroyImmediate(pgo);
                Object.DestroyImmediate(ego);
            }
        }
    }
}
