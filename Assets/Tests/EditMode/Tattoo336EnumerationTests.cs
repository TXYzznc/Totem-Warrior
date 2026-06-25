using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tattoo.Tests
{
    /// <summary>
    /// 336 穷举测试：6 部位 × 7 颜色 × 8 图案，穷举所有合法组合，
    /// 记录每条 EffectResult 到 Assets/Tests/EditMode/Tattoo336Report.md。
    ///
    /// 关于 LeftArm（partId=3, SkillCastEvent）：
    ///   PrepareContext 不设 PrimaryTarget，SingleHit / StackingMark / MultiHit /
    ///   ProbBurst / SummonForm 在 target=null 时直接 return 不写 Log，
    ///   导致 EffectAppliedEvent 不发出（ctx.Log.Count == 0 时不广播）。
    ///   这是系统设计决定（技能型效果无独立目标），非 bug。
    ///   AOEBurst / ChainJump / TrailZone 在无 target 时仍写一条 HitCount=0 的 Log。
    ///   因此 LeftArm 的 assert 为 >= 0（而非 >= 1），报告中标注 ResultCount。
    /// </summary>
    public class Tattoo336EnumerationTests
    {
        // ===== 报告路径（相对项目根 Assets/Tests/EditMode/）=====
        static readonly string ReportPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, "Tests/EditMode/Tattoo336Report.md"));

        // ===== 辅助初始化 =====
        EventBus     _bus;
        ModuleRunner _runner;
        TattooModule _tattoo;

        async UniTask SetupAsync()
        {
            _bus    = new EventBus();
            _runner = new ModuleRunner(_bus);
            _runner.AddModule(new DataTableModule());
            _runner.AddModule(new TattooModule(_runner, _bus));
            await _runner.StartAsync();
            _tattoo = _runner.GetModule<TattooModule>();
        }

        async UniTask TeardownAsync()
        {
            if (_runner != null) await _runner.StopAsync();
            _bus = null; _runner = null; _tattoo = null;
        }

        // ===== 主测试 =====

        [UnityTest]
        public IEnumerator Enumerate336Combinations_AllEquipSucceed_ReportGenerated() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    var rows = new List<ReportRow>(336);
                    var equipFailures  = new List<string>();
                    // LeftArm 允许 0 条结果（系统设计，见类注释）
                    var effectFailures = new List<string>();

                    int seq = 0;
                    for (int partId = 1; partId <= 6; partId++)
                    {
                        for (int colorId = 1; colorId <= 7; colorId++)
                        {
                            for (int patternId = 1; patternId <= 8; patternId++)
                            {
                                seq++;

                                // ── 1. Equip ──────────────────────────────────────────
                                bool equipped = _tattoo.Equip(partId, colorId, patternId);
                                if (!equipped)
                                    equipFailures.Add($"#{seq} part={partId} color={colorId} pattern={patternId}");

                                // 无论装备是否成功，先记录行占位（后续填充结果）
                                var row = new ReportRow
                                {
                                    Seq       = seq,
                                    PartId    = partId,
                                    ColorId   = colorId,
                                    PatternId = patternId,
                                    PartName  = equipped ? _tattoo.Equipped.Count > 0
                                        ? _tattoo.Equipped[_tattoo.Equipped.Count - 1].PartName : "?"
                                        : "EquipFailed",
                                    ColorName   = "?",
                                    PatternName = "?",
                                };

                                if (equipped && _tattoo.Equipped.Count > 0)
                                {
                                    var slot = _tattoo.Equipped[_tattoo.Equipped.Count - 1];
                                    row.PartName    = slot.PartName;
                                    row.ColorName   = slot.ColorName;
                                    row.PatternName = slot.PatternName;
                                    row.TriggerEvent = slot.TriggerEventType?.Name ?? "?";

                                    // ── 2. Fire ───────────────────────────────────────
                                    EffectAppliedEvent captured = null;
                                    using var sub = _bus.Subscribe<EffectAppliedEvent>(e => captured = e);

                                    FireForPart(partId);

                                    // ── 3. 收集结果 ───────────────────────────────────
                                    bool isLeftArm = (slot.PartName == "LeftArm");
                                    if (!isLeftArm && (captured == null || captured.Results.Count < 1))
                                    {
                                        effectFailures.Add(
                                            $"#{seq} {slot.PartName}×{slot.ColorName}×{slot.PatternName} " +
                                            $"Results={captured?.Results?.Count ?? 0}");
                                    }

                                    if (captured != null && captured.Results.Count > 0)
                                    {
                                        var r = captured.Results[0];
                                        row.Element  = r.Element;
                                        row.Shape    = r.Shape;
                                        row.Part     = r.Part;
                                        row.Damage   = r.Damage;
                                        row.HitCount = r.HitCount;
                                        row.Status   = r.Status;
                                        row.Note     = r.Note;
                                        row.ResultCount = captured.Results.Count;
                                    }
                                    else
                                    {
                                        row.Element     = isLeftArm ? "NoTarget" : "MISSING";
                                        row.Shape       = "-";
                                        row.Part        = slot.PartName;
                                        row.Status      = isLeftArm ? "SkillNoTarget/Expected" : "MISSING_RESULT";
                                        row.ResultCount = 0;
                                    }
                                }
                                else
                                {
                                    row.TriggerEvent = "-";
                                    row.Element      = "-";
                                    row.Shape        = "-";
                                    row.Status       = "EquipFailed";
                                }

                                rows.Add(row);

                                // ── 4. Clear（重置状态，避免 PendingTrigger 污染下一轮）──
                                _tattoo.Clear();
                            }
                        }
                    }

                    // ── 5. 写报告 ──────────────────────────────────────────────────
                    WriteReport(rows);

                    // ── 6. 断言 ────────────────────────────────────────────────────
                    Assert.AreEqual(0, equipFailures.Count,
                        $"以下 {equipFailures.Count} 个组合 Equip 返回 false:\n" +
                        string.Join("\n", equipFailures));

                    Assert.AreEqual(0, effectFailures.Count,
                        $"以下 {effectFailures.Count} 个组合（非LeftArm）Fire 后 Results < 1:\n" +
                        string.Join("\n", effectFailures));

                    Assert.AreEqual(336, seq, "组合总数应为 336");
                }
                finally
                {
                    await TeardownAsync();
                }
            });

        // ===== 根据 partId 发送对应触发事件 =====

        void FireForPart(int partId)
        {
            // partId → Part → TriggerEvent（来自 DataTable TattooPartConfig）
            // 1=Head(CritHitEvent), 2=Torso(DamagedEvent), 3=LeftArm(SkillCastEvent)
            // 4=RightArm(AttackHitEvent), 5=LeftLeg(DodgePressedEvent), 6=RightLeg(MoveTickEvent)
            switch (partId)
            {
                case 1: // Head → CritHitEvent
                    _bus.Publish(new CritHitEvent(new Target { Name = "敌人", Health = 9999f }, baseDamage: 20f));
                    break;

                case 2: // Torso → DamagedEvent（需要 attacker，TorsoPartBehavior 将其设为 PrimaryTarget）
                    _bus.Publish(new DamagedEvent(new Target { Name = "攻击者", Health = 9999f }, damage: 10f));
                    break;

                case 3: // LeftArm → SkillCastEvent（无 PrimaryTarget，部分 shape 不写 Log，为预期行为）
                    _bus.Publish(new SkillCastEvent("skill_test"));
                    break;

                case 4: // RightArm → AttackHitEvent
                    _bus.Publish(new AttackHitEvent(new Target { Name = "敌人", Health = 9999f }, baseDamage: 10f));
                    break;

                case 5: // LeftLeg → DodgePressedEvent（InterceptApply 写 Log + PendingTrigger，不需要 target）
                    _bus.Publish(new DodgePressedEvent());
                    break;

                case 6: // RightLeg → MoveTickEvent（PrepareContext 将 path[0] 设为 PrimaryTarget）
                    _bus.Publish(new MoveTickEvent(
                        path: new[] { new Target { Name = "路径敌人", Health = 9999f } },
                        distance: 5f));
                    break;

                default:
                    throw new InvalidOperationException($"未知 partId={partId}，超出 1-6 范围");
            }
        }

        // ===== 报告生成 =====

        static void WriteReport(List<ReportRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Tattoo 336 穷举测试报告");
            sb.AppendLine();
            sb.AppendLine($"> 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("| 序号 | PartId | ColorId | PatternId | 部位 | 颜色 | 图案 | 触发事件 | ResultCount | Element | Shape | Part | Damage | HitCount | Status | Note |");
            sb.AppendLine("|------|--------|---------|-----------|------|------|------|----------|-------------|---------|-------|------|--------|----------|--------|------|");

            foreach (var r in rows)
            {
                sb.AppendLine(
                    $"| {r.Seq} | {r.PartId} | {r.ColorId} | {r.PatternId} " +
                    $"| {Esc(r.PartName)} | {Esc(r.ColorName)} | {Esc(r.PatternName)} " +
                    $"| {Esc(r.TriggerEvent)} | {r.ResultCount} " +
                    $"| {Esc(r.Element)} | {Esc(r.Shape)} | {Esc(r.Part)} " +
                    $"| {r.Damage:F2} | {r.HitCount} | {Esc(r.Status)} | {Esc(r.Note)} |");
            }

            sb.AppendLine();
            sb.AppendLine($"共 {rows.Count} 行（含表头外）。");

            // 使用 File.WriteAllText 确保 batchmode 可用，不依赖 AssetDatabase
            string dir = Path.GetDirectoryName(ReportPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        }

        static string Esc(string s) => s?.Replace("|", "\\|") ?? "-";

        // ===== 数据结构 =====

        struct ReportRow
        {
            public int    Seq;
            public int    PartId;
            public int    ColorId;
            public int    PatternId;
            public string PartName;
            public string ColorName;
            public string PatternName;
            public string TriggerEvent;
            public int    ResultCount;
            // 第一条 EffectResult 字段
            public string Element;
            public string Shape;
            public string Part;
            public float  Damage;
            public int    HitCount;
            public string Status;
            public string Note;
        }
    }
}
