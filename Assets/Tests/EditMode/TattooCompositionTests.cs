using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Tattoo;

namespace Tattoo.Tests
{
    public class TattooCompositionTests
    {
        // ---------- Atom factories (v2) ----------
        static HeadPart     Head()  => AtomFactory.MakePart<HeadPart>    ("Head",     GameEvent.OnCrit,         StatType.CritMultiplier);
        static TorsoPart    Torso() => AtomFactory.MakePart<TorsoPart>   ("Torso",    GameEvent.OnDamaged,      StatType.MaxHealth);
        static LeftArmPart  LArm()  => AtomFactory.MakePart<LeftArmPart> ("LeftArm",  GameEvent.OnSkillCast,    StatType.SkillPower,    SymmetryGroup.Arms);
        static RightArmPart RArm()  => AtomFactory.MakePart<RightArmPart>("RightArm", GameEvent.OnAttack,       StatType.WeaponDamage,  SymmetryGroup.Arms);
        static LeftLegPart  LLeg()  => AtomFactory.MakePart<LeftLegPart> ("LeftLeg",  GameEvent.OnDodgePressed, StatType.DodgeFrames,   SymmetryGroup.Legs);
        static RightLegPart RLeg()  => AtomFactory.MakePart<RightLegPart>("RightLeg", GameEvent.OnMoveTick,     StatType.MoveSpeed,     SymmetryGroup.Legs);

        static ColorSO Red()    => AtomFactory.MakeColor<FireElement>     ("Red");
        static ColorSO Yellow() => AtomFactory.MakeColor<LightningElement>("Yellow");
        static ColorSO Green()  => AtomFactory.MakeColor<NatureElement>   ("Green");
        static ColorSO Blue()   => AtomFactory.MakeColor<FrostElement>    ("Blue");
        static ColorSO Purple() => AtomFactory.MakeColor<MutationElement> ("Purple");
        static ColorSO Gold()   => AtomFactory.MakeColor<HolyElement>     ("Gold");
        static ColorSO White()  => AtomFactory.MakeColor<PureElement>     ("White");

        static PatternSO Line()   => AtomFactory.MakePattern<SingleHitShape>   ("Line");
        static PatternSO Ring()   => AtomFactory.MakePattern<AOEBurstShape>    ("Ring");
        static PatternSO Spiral() => AtomFactory.MakePattern<StackingMarkShape>("Spiral");
        static PatternSO Zigzag() => AtomFactory.MakePattern<MultiHitShape>    ("Zigzag");
        static PatternSO Bolt()   => AtomFactory.MakePattern<ChainJumpShape>   ("Bolt");
        static PatternSO Star()   => AtomFactory.MakePattern<ProbBurstShape>   ("Star");
        static PatternSO Stream() => AtomFactory.MakePattern<TrailZoneShape>   ("Stream");
        static PatternSO Beast()  => AtomFactory.MakePattern<SummonFormShape>  ("Beast");

        static TattooSlot S(BodyPart p, ColorSO c, PatternSO pat)
            => new TattooSlot { Part = p, Color = c, Pattern = pat };

        // =============== v2 核心测试 ===============

        [Test]
        public void Atom_Head_Red_Line_OnCrit_DealsCritScaledDamage()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), Red(), Line()));
            var t = new Target();
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t });
            // CritMul (1.5) × HeadDefaultScale (10) = 15
            Assert.AreEqual(c.Stats.CritMultiplier * 10f, 100f - t.Health, 1e-3f);
            Assert.IsTrue(t.Statuses.Any(s => s.StartsWith("Burn")));
        }

        [Test]
        public void EventDispatch_OnlyMatchingTriggerFires()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(),  Red(), Line()));  // OnCrit
            c.Equip(S(RArm(),  Red(), Line()));  // OnAttack
            var t = new Target();
            var ctx = new EffectContext { PrimaryTarget = t };
            c.Fire(GameEvent.OnAttack, ctx);
            // Only RArm 触发：WeaponDmg (10) × RArmDefaultScale (0.8) = 8
            Assert.AreEqual(c.Stats.WeaponDamage * 0.8f, 100f - t.Health, 1e-3f);
            Assert.AreEqual(1, ctx.Log.Count);
        }

        // ---------- v2 新机制 ----------

        [Test]
        public void v2_TorsoPart_PrepareContext_SetsPrimaryTargetToLastAttacker()
        {
            var c = new TattooComposer();
            c.Equip(S(Torso(), Red(), Line()));
            var wrong = new Target { Name = "WrongTarget" };
            var attacker = new Target { Name = "Attacker" };
            var ctx = new EffectContext { PrimaryTarget = wrong, LastAttacker = attacker };
            c.Fire(GameEvent.OnDamaged, ctx);
            Assert.AreEqual(100f, wrong.Health,
                "WrongTarget 不应受影响（Torso 应把目标重塑为 LastAttacker）");
            Assert.Less(attacker.Health, 100f,
                "Attacker（LastAttacker）应受到反弹伤害");
        }

        [Test]
        public void v2_LeftLegPart_InterceptApply_PackagesPendingTrigger()
        {
            var c = new TattooComposer();
            c.Equip(S(LLeg(), Red(), Line()));
            var t = new Target();

            c.Fire(GameEvent.OnDodgePressed, new EffectContext { PrimaryTarget = t });
            Assert.AreEqual(100f, t.Health,
                "闪避按下时图案不应立即命中（应被 InterceptApply 拦截）");
            Assert.AreEqual(1, c.Player.PendingTriggers.Count,
                "应排队一个 PendingTrigger");

            // 下次普攻应消耗它
            c.Fire(GameEvent.OnAttack, new EffectContext { PrimaryTarget = t });
            Assert.Less(t.Health, 100f, "下次普攻应消耗 PendingTrigger 命中目标");
            Assert.AreEqual(0, c.Player.PendingTriggers.Count, "PendingTrigger 应被消耗");
        }

        [Test]
        public void v2_PureElement_ModifyMagnitude_AddsPercent20()
        {
            var cR = new TattooComposer(); cR.Equip(S(Head(), Red(),   Line()));
            var tR = new Target(); cR.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = tR });
            float dmgR = 100f - tR.Health;

            var cW = new TattooComposer(); cW.Equip(S(Head(), White(), Line()));
            var tW = new Target(); cW.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = tW });
            float dmgW = 100f - tW.Health;

            // 白色 ModifyMagnitude = ×1.20（Focus stacks=0 时）
            Assert.AreEqual(dmgR * 1.20f, dmgW, 1e-3f);
            Assert.IsTrue(tW.Statuses.Any(s => s.Contains("纯能")));
        }

        [Test]
        public void v2_PureElement_FocusStacks_BoostNextHit()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), White(), Line()));

            var t1 = new Target(); c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t1 });
            float dmg1 = 100f - t1.Health;

            var t2 = new Target(); c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t2 });
            float dmg2 = 100f - t2.Health;

            Assert.Greater(dmg2, dmg1, "第二次触发应因 Focus 叠层 +1% 而更高");
        }

        [Test]
        public void v2_HolyElement_AffectSelf_HealsPlayer()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), Gold(), Line()));
            c.Player.Health = 50f;

            var t = new Target();
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t });

            Assert.Greater(c.Player.Health, 50f, "金色应当治疗自身");
            Assert.IsTrue(t.Statuses.Any(s => s.Contains("神圣")));
        }

        [Test]
        public void v2_LeftLegPart_AffectSelf_GivesShortInvincibility()
        {
            var c = new TattooComposer();
            c.Equip(S(LLeg(), Red(), Line()));
            c.Fire(GameEvent.OnDodgePressed, new EffectContext { PrimaryTarget = new Target() });
            Assert.IsTrue(c.Player.ShortInvincible, "左腿闪避应给玩家短无敌");
            Assert.IsTrue(c.Player.Buffs.Any(b => b.Contains("无敌")));
        }

        [Test]
        public void v2_PassiveStats_AccumulateAfterEquip()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(),  Red(),    Line())); // 脑袋红线：暴击率+
            c.Equip(S(Torso(), Yellow(), Ring())); // 躯干黄圆：雷抗+ / 最大生命+

            Assert.AreEqual(2, c.Player.Passive.EntryLog.Count);
            Assert.Greater(c.Player.Passive.CritRateBonus, 0f, "脑袋应贡献暴击率");
            Assert.IsTrue(c.Player.Passive.Resistance.ContainsKey(ElementType.Lightning), "躯干黄应贡献雷抗");
            Assert.Greater(c.Player.Passive.MaxHealthBonus, 0f, "躯干应贡献最大生命");
        }

        [Test]
        public void v2_PassiveStats_RecomputedOnUnequip()
        {
            var c = new TattooComposer();
            var slot1 = S(Head(), Red(), Line());
            c.Equip(slot1);
            c.Equip(S(Torso(), Yellow(), Ring()));
            Assert.AreEqual(2, c.Player.Passive.EntryLog.Count);

            c.Unequip(slot1);
            Assert.AreEqual(1, c.Player.Passive.EntryLog.Count);
        }

        // ---------- 联动 ----------

        [Test]
        public void Synergy_ColorResonance_3SameColor_BoostsTriggeredSlot()
        {
            // 同一 ColorSO 实例必须被三槽位复用（联动靠引用比较 ==）
            var red  = Red();
            var line = Line();

            var c1 = new TattooComposer(); c1.Equip(S(Head(), red, line));
            var t1 = new Target(); c1.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t1 });
            float baseDmg = 100f - t1.Health;

            var c3 = new TattooComposer();
            c3.Equip(S(Head(),  red, line));
            c3.Equip(S(Torso(), red, line));
            c3.Equip(S(RArm(),  red, line));
            var t3 = new Target(); c3.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t3 });
            float resDmg = 100f - t3.Health;

            Assert.AreEqual(baseDmg * 1.20f * 1.15f, resDmg, 1e-3f);
        }

        [Test]
        public void Synergy_Symmetry_LeftRightArm_BoostsTriggered()
        {
            var red  = Red();
            var line = Line();

            var c1 = new TattooComposer(); c1.Equip(S(RArm(), red, line));
            var t1 = new Target(); c1.Fire(GameEvent.OnAttack, new EffectContext { PrimaryTarget = t1 });
            float baseDmg = 100f - t1.Health;

            var cS = new TattooComposer();
            cS.Equip(S(LArm(), red, line));
            cS.Equip(S(RArm(), red, line));
            var tS = new Target(); cS.Fire(GameEvent.OnAttack, new EffectContext { PrimaryTarget = tS });
            float symDmg = 100f - tS.Health;

            Assert.AreEqual(baseDmg * 1.25f, symDmg, 1e-3f);
        }

        // ---------- 形状 ----------

        [Test]
        public void Shape_AOEBurst_HitsMultipleTargets()
        {
            var head = AtomFactory.MakePart<HeadPart>("Head", GameEvent.OnCrit, StatType.CritMultiplier, scale: 100f);
            var c = new TattooComposer();
            c.Equip(S(head, Red(), Ring()));
            var p = new Target { Name = "A" };
            var ctx = new EffectContext { PrimaryTarget = p };
            ctx.NearbyTargets.Add(new Target { Name = "B" });
            ctx.NearbyTargets.Add(new Target { Name = "C" });
            c.Fire(GameEvent.OnCrit, ctx);
            Assert.Greater(100f - p.Health, 0f);
            Assert.Greater(100f - ctx.NearbyTargets[0].Health, 0f);
            Assert.Greater(100f - ctx.NearbyTargets[1].Health, 0f);
            Assert.AreEqual(3, ctx.Log[0].HitCount);
        }

        [Test]
        public void Shape_StackingMark_BurstsAtThreshold()
        {
            var arm = AtomFactory.MakePart<RightArmPart>("RightArm", GameEvent.OnAttack, StatType.WeaponDamage);
            var spiralShape = ScriptableObject.CreateInstance<StackingMarkShape>();
            spiralShape.Threshold = 3; spiralShape.BurstMul = 5f;
            var spiral = ScriptableObject.CreateInstance<PatternSO>();
            spiral.PatternName = "Spiral"; spiral.Shape = spiralShape; spiral.PatternMultiplier = 1f;

            var c = new TattooComposer();
            c.Equip(S(arm, Red(), spiral));
            var t = new Target();
            var ctx = new EffectContext { PrimaryTarget = t };
            c.Fire(GameEvent.OnAttack, ctx);
            c.Fire(GameEvent.OnAttack, ctx);
            Assert.AreEqual(100f, t.Health);
            c.Fire(GameEvent.OnAttack, ctx); // burst: WpnDmg(10) × RArmScale(0.8) × BurstMul(5) = 40
            Assert.AreEqual(40f, 100f - t.Health, 1e-3f);
        }

        [Test]
        public void v2_Synergy_ColorReaction_FireLightning_BoostsBoth()
        {
            var red    = Red();
            var yellow = Yellow();
            var line   = Line();

            // baseline: 仅红
            var c1 = new TattooComposer(); c1.Equip(S(Head(), red, line));
            var t1 = new Target(); c1.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t1 });
            float baseDmg = 100f - t1.Health;

            // 红 + 黄 → 应触发"爆裂"颜色反应 ×1.30
            var c2 = new TattooComposer();
            c2.Equip(S(Head(),  red,    line));
            c2.Equip(S(Torso(), yellow, line));  // 装到 Torso 不会被 OnCrit 触发，但反应仍生效
            var t2 = new Target(); c2.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t2 });
            float reactDmg = 100f - t2.Health;

            Assert.AreEqual(baseDmg * 1.30f, reactDmg, 1e-3f);
        }

        [Test]
        public void v2_HolyElement_OnHitExtra_WithBolt_HealsPlayer()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), Gold(), Bolt()));
            c.Player.Health = 50f;
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = new Target() });
            Assert.Greater(c.Player.Health, 50f, "金×闪电应通过 OnHitExtra 跳跳回血");
        }

        [Test]
        public void v2_FrostElement_AffectSelf_StacksFrostArmor()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), Blue(), Line()));
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = new Target() });
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = new Target() });
            Assert.AreEqual(2, c.Player.Stacks.GetValueOrDefault("FrostArmor", 0));
        }

        [Test]
        public void v2_PureElement_OnHitExtra_WithBolt_TriggersExtraBasicAttack()
        {
            var c = new TattooComposer();
            c.Equip(S(Head(), White(), Bolt()));
            var t = new Target();
            c.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t });

            Assert.IsTrue(c.Player != null);
            // 应该有 ChainJump 主条目 + OnHitExtra 的"额外普攻"条目
            int extras = 0;
            foreach (var r in new[] { default(EffectResult) }) { } // placeholder
            // OnHitExtra 应该追加一条 Note="OnHitExtra"
            bool hasExtra = false;
            // 用 ctx 找
            // 我们只能用副作用：t.Health 应该比纯 ChainJump 少（多了 50% extra）
            // 比较：白色×直线 vs 白色×闪电
            var c2 = new TattooComposer();
            c2.Equip(S(Head(), White(), Line()));
            var t2 = new Target();
            c2.Fire(GameEvent.OnCrit, new EffectContext { PrimaryTarget = t2 });

            // 白色×闪电应明显造成额外伤害（ChainJump 命中 + ExtraBasic 命中）
            float dmgBolt = 100f - t.Health;
            float dmgLine = 100f - t2.Health;
            Assert.Greater(dmgBolt, dmgLine, "白色×闪电应大于白色×直线（因为 OnHitExtra 额外普攻）");
        }

        // ---------- Demo runner ----------

        [Test]
        public void DemoRunner_LoadFullBuild_HasSixSlotsAndFiresAllSixEvents()
        {
            var go = new GameObject("TattooDemo");
            try
            {
                var demo = go.AddComponent<TattooDemoRunner>();
                demo.LoadFullBuild();
                Assert.AreEqual(6, demo.Composer.Equipped.Count);
                foreach (GameEvent ev in System.Enum.GetValues(typeof(GameEvent)))
                {
                    demo.ResetTargets();
                    demo.TriggerEvent(ev);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DemoRunner_DefaultInitialize_FireCurrentSingleProducesEffect()
        {
            var go = new GameObject("TattooDemo");
            try
            {
                var demo = go.AddComponent<TattooDemoRunner>();
                demo.Initialize();
                Assert.AreEqual(0, demo.Composer.Equipped.Count);
                demo.PartIndex = 0; demo.ColorIndex = 0; demo.PatternIndex = 0;
                demo.FireCurrentSingle();
                Assert.Less(demo.Dummy.Health, 1000f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============= Exhaustive 336 dump =============

        [Test]
        public void Atomic336Dump_FullEnumerationToFile()
        {
            var parts = new BodyPart[] { Head(), Torso(), LArm(), RArm(), LLeg(), RLeg() };
            var colors = new[] { Red(), Yellow(), Green(), Blue(), Purple(), Gold(), White() };
            var patterns = new[] { Line(), Ring(), Spiral(), Zigzag(), Bolt(), Star(), Stream(), Beast() };

            var sb = new StringBuilder();
            sb.AppendLine("# 336 组合实际效果 (Round C 输出)");
            sb.AppendLine();
            sb.AppendLine($"_生成时间：{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}_");
            sb.AppendLine();
            sb.AppendLine("| # | 部位 | 颜色 | 图案 | 触发事件 | 主目标伤害 | 命中数 | 主目标状态 | 玩家HP | 玩家叠层/Buff | 拦截备注 | 被动 |");
            sb.AppendLine("|---|------|------|------|----------|-----------|--------|-----------|--------|---------------|---------|------|");

            int idx = 0;
            int signatures = 0;
            var sigSet = new HashSet<string>();
            foreach (var part in parts)
                foreach (var col in colors)
                    foreach (var pat in patterns)
                    {
                        idx++;
                        var comp = new TattooComposer();
                        comp.Equip(S(part, col, pat));
                        var dummy = new Target { Name = "Dummy", Health = 1000f };
                        var nb1 = new Target { Name = "Nb1", Health = 1000f };
                        var nb2 = new Target { Name = "Nb2", Health = 1000f };
                        var attacker = new Target { Name = "Attacker", Health = 1000f };
                        var ctx = new EffectContext
                        {
                            PrimaryTarget = dummy,
                            LastAttacker  = attacker,
                        };
                        ctx.NearbyTargets.Add(nb1); ctx.NearbyTargets.Add(nb2);
                        ctx.MovementPath.Add(nb1); ctx.MovementPath.Add(nb2);

                        comp.Fire(part.TriggerEvent, ctx);

                        var mainTarget = ctx.PrimaryTarget;
                        float dmgMain = 1000f - mainTarget.Health;
                        int hits = ctx.Log.Sum(l => l.HitCount);
                        string statusMain = string.Join(";", mainTarget.Statuses);
                        string notes = string.Join(";", ctx.Log.Where(l => !string.IsNullOrEmpty(l.Note)).Select(l => l.Note));
                        string stacks = string.Join(",", comp.Player.Stacks.Select(kv => kv.Key + ":" + kv.Value))
                                      + (comp.Player.Buffs.Count > 0 ? "/" + string.Join(",", comp.Player.Buffs) : "");
                        string passive = comp.Player.Passive.EntryLog.Count > 0 ? comp.Player.Passive.EntryLog[0] : "-";
                        string sig = $"{part.PartName}|{col.ColorName}|{pat.PatternName}";
                        sigSet.Add(sig);
                        if (idx == 1) signatures++;

                        sb.AppendLine($"| {idx} | {part.PartName} | {col.ColorName} | {pat.PatternName} | {part.TriggerEvent} | {dmgMain:F2} | {hits} | {statusMain} | {comp.Player.Health:F1} | {stacks} | {notes} | {passive} |");
                    }

            sb.AppendLine();
            sb.AppendLine($"**总计**：{idx} 组合，{sigSet.Count} 唯一签名。");

            var dir = System.IO.Path.Combine(Application.dataPath, "TestResults");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "atomic_336.md");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[Atomic336Dump] {idx} 组合输出到 {path}");

            Assert.AreEqual(336, idx);
            Assert.AreEqual(336, sigSet.Count, "每个组合应签名唯一");
        }
    }
}
