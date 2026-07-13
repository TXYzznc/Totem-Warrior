#if UNITY_EDITOR
using System.Collections.Generic;

namespace UGF.EditorTools
{
    public sealed class TotemEncounterCapacityDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Encounter Capacity Fast";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var entry = new TotemSpawnPlanEntry
            {
                PlanEntryId = "diagnostic.light.0",
                EncounterId = "encounter.diagnostic.light",
                EnemyId = "enemy_common_hunter",
                Tier = TotemEnemyTier.Light,
                TriggerTime = 0f,
                ActiveCap = 1,
                TotalCap = 2,
                RetryInterval = 45f,
            };
            var clock = new TotemEncounterClock();
            clock.Reset(new TotemSpawnPlan { Entries = new[] { entry } });
            var output = new List<TotemSpawnPlanEntry>(1);

            context.AssertEqual(0, clock.CollectDueSpawns(new TotemEncounterClockContext(true, 0f, 1, 0, 0), output),
                "encounterCapacity.full.initial");
            context.AssertEqual(0, clock.CaptureSnapshot().processedEntryCount,
                "encounterCapacity.full.notProcessed");

            output.Clear();
            context.AssertEqual(0, clock.CollectDueSpawns(new TotemEncounterClockContext(true, 44.9f, 0, 0, 0), output),
                "encounterCapacity.beforeRetry");
            output.Clear();
            context.AssertEqual(1, clock.CollectDueSpawns(new TotemEncounterClockContext(true, 45f, 0, 0, 0), output),
                "encounterCapacity.retrySpawn");
            context.AssertEqual(1, clock.CaptureSnapshot().processedEntryCount,
                "encounterCapacity.retryProcessed");
            context.AssertEqual(1, clock.CaptureSnapshot().deferredActiveCapCount,
                "encounterCapacity.deferredCount");
            context.Pass("ActiveCap defers due entries to the next wave interval without consuming TotalCap.");
        }
    }
}
#endif
