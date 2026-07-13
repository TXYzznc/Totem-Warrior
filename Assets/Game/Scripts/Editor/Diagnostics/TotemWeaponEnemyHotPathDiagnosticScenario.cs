#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemWeaponEnemyHotPathDiagnosticScenario : GFDiagnosticScenarioBase
    {
        private const string TraitId = "trait_diagnostic_status";
        private const int SourceCombatantId = 41001;
        private const int TargetCombatantId = 42001;
        private const float StatusChance = 0.5f;

        public override string Name => "Totem Weapon Enemy Hot Path";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            float previousRoll = -1f;
            bool observedVariation = false;
            for (uint sequence = 1; sequence <= 32; sequence++)
            {
                float firstRun = TotemWeaponService.ResolveDeterministicStatusRoll(
                    TraitId,
                    SourceCombatantId,
                    TargetCombatantId,
                    sequence,
                    StatusChance);
                float replayRun = TotemWeaponService.ResolveDeterministicStatusRoll(
                    TraitId,
                    SourceCombatantId,
                    TargetCombatantId,
                    sequence,
                    StatusChance);

                context.Assert(firstRun >= 0f && firstRun < 1f, "weaponHotPath.roll.range.sequence" + sequence);
                context.Assert(firstRun == replayRun, "weaponHotPath.roll.replay.sequence" + sequence);
                if (previousRoll >= 0f && firstRun != previousRoll)
                {
                    observedVariation = true;
                }

                previousRoll = firstRun;
            }

            context.Assert(observedVariation, "The same target must receive varying rolls across fire sequences.");

            string weaponServicePath = Path.Combine(
                Application.dataPath,
                "Game/Scripts/Runtime/Services/TotemWeaponService.cs");
            string source = File.ReadAllText(weaponServicePath);
            context.Assert(!source.Contains("new System.Random"), "Weapon hot paths must not allocate System.Random.");
            context.Assert(!source.Contains("new int[maxExtraHits]"), "Pierce must reuse its fixed hit buffer.");
            context.Assert(!source.Contains("new int[maxJumps]"), "Chain must reuse its fixed hit buffer.");
            context.Assert(!source.Contains("new int[extraProjectiles]"), "MultiShot must reuse its fixed hit buffer.");
            context.Assert(source.Contains("traitHitActorIdBuffer"), "Weapon secondary-hit paths must retain fixed-buffer evidence.");

            context.Detail("weaponHotPath.sampleCount", 32);
            context.Detail("weaponHotPath.lastRoll", previousRoll);
            context.Detail("weaponHotPath.fixedBufferEvidence", true);
            context.Pass("Weapon status rolls vary by fire sequence, replay exactly, and secondary-hit paths avoid per-attack array/Random allocation.");
        }
    }
}
#endif
