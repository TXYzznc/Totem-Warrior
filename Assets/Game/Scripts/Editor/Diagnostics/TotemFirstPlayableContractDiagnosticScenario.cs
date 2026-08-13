#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableContractDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Contract";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var config = new TotemFirstPlayableContractConfig
            {
                assets = new[]
                {
                    new TotemPresentationAssetContract
                    {
                        stableId = "rifle.hit.body",
                        kind = TotemPresentationAssetKind.Vfx,
                        assetKey = TotemFirstPlayableArtHandoff.VfxKeys.RifleHitBody,
                        fallbackKey = TotemFirstPlayableArtHandoff.FallbackKeys.MissingVfx,
                        handoffId = TotemFirstPlayableArtHandoff.VfxDeliveryId,
                    },
                },
            };
            var errors = new List<string>();
            context.Assert(TotemFirstPlayableContractValidator.Validate(config, errors),
                errors.Count == 0 ? "FirstPlayable config validation failed." : string.Join(" | ", errors));

            var roster = new TotemRosterSlot[TotemFirstPlayableRules.ParticipantCount];
            for (int i = 0; i < roster.Length; i++)
            {
                roster[i] = new TotemRosterSlot(
                    new TotemParticipantId(i + 1),
                    new TotemTeamId(i / TotemFirstPlayableRules.TeamSize),
                    i == 0 ? TotemFirstPlayableParticipantKind.Human : TotemFirstPlayableParticipantKind.Bot,
                    TotemFirstPlayableLifeState.Alive);
            }

            context.Assert(TotemRosterContract.Validate(roster, out string rosterError), rosterError);
            context.Assert(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.FrontEnd, TotemMatchPhase.OpeningBuild),
                "FrontEnd must transition to OpeningBuild.");
            context.Assert(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Round3Combat, TotemMatchPhase.Build4),
                "Round3Combat must transition to Build4.");
            context.Assert(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Round5Combat, TotemMatchPhase.Result),
                "Round5Combat must transition to Result.");
            context.Assert(!TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Round5Combat, TotemMatchPhase.Build2),
                "The first playable must not loop after the fifth round.");

            var hit = new TotemGunHitContext(
                new TotemParticipantId(1),
                new TotemTeamId(0),
                100,
                new TotemTeamId(1),
                TotemHitRegion.Weakpoint,
                Vector3.zero,
                Vector3.up,
                10f);
            context.Assert(new TotemDirectDamageResult(hit, 0f, 10f, true).CanSubmitRifleArmEvent,
                "Positive effective direct damage must allow a rifle-arm event.");
            context.Assert(!new TotemDirectDamageResult(hit, 0f, 0f, true).CanSubmitRifleArmEvent,
                "Zero direct damage must not allow a rifle-arm event.");
            context.Assert(TotemEffectPriority.Weakpoint > TotemEffectPriority.RifleArm &&
                           TotemEffectPriority.RifleArm > TotemEffectPriority.Torso,
                "Frozen effect priorities are invalid.");

            context.Detail("contract.participantCount", TotemFirstPlayableRules.ParticipantCount.ToString());
            context.Detail("contract.teamCount", TotemFirstPlayableRules.TeamCount.ToString());
            context.Detail("contract.phaseCount", "12");
            context.Detail("contract.artChange", TotemFirstPlayableArtHandoff.ChangeId);
            context.Detail("contract.uiDelivery", TotemFirstPlayableArtHandoff.UiDeliveryId);
            context.Detail("contract.vfxDelivery", TotemFirstPlayableArtHandoff.VfxDeliveryId);
            context.Pass("First-playable immutable runtime contracts, serializable config and art fallbacks are valid.");
        }
    }
}
#endif
