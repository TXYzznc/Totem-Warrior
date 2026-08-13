#if UNITY_EDITOR
using System.Collections.Generic;

namespace UGF.EditorTools
{
    public sealed class TotemFiveRoundMatchFlowDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Five Round Match Flow";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var flow = new TotemMatchFlowService();
            var visited = new List<TotemMatchPhase>();
            int shrinkCount = 0;
            flow.PhaseChanged += (_, next) => visited.Add(next);
            flow.ActivityChanged += (_, next) =>
            {
                if (next == TotemMatchActivity.ZoneShrink)
                {
                    shrinkCount++;
                }
            };

            flow.BeginMatch(useFastMode: true);
            context.Assert(flow.IsGameplaySuspended, "Opening build must suspend gameplay simulation.");
            flow.Advance(580f);

            context.AssertEqual(TotemMatchPhase.Result, flow.CurrentPhase, "matchFlow.finalPhase");
            context.AssertEqual(TotemMatchActivity.Result, flow.CurrentActivity, "matchFlow.finalActivity");
            context.AssertEqual(4, shrinkCount, "matchFlow.shrinkCount");
            context.AssertEqual(11, visited.Count, "matchFlow.visitedPhaseCount");

            var clock = new TotemMatchClockAccumulator();
            clock.Activate();
            clock.Advance(5f, 5f, gameplaySuspended: true);
            context.AssertEqual(0f, clock.WorldTime, "matchClock.buildWorldTime");
            context.AssertEqual(5f, clock.UiTime, "matchClock.buildUiTime");
            clock.Advance(5f, 5f, gameplaySuspended: false);
            context.AssertEqual(5f, clock.WorldTime, "matchClock.combatWorldTime");
            context.AssertEqual(10f, clock.UiTime, "matchClock.combatUiTime");

            context.Detail("matchFlow.mode", "fast");
            context.Detail("matchFlow.sequence", string.Join(" -> ", visited));
            context.Detail("matchFlow.result", flow.CurrentPhase.ToString());
            context.Pass("Opening build, five combat rounds, four shrink activities and unscaled UI timing complete at Result without an Enemy or Boss phase.");
        }
    }
}
#endif
