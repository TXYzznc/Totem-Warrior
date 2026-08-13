#if UNITY_EDITOR
namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayablePurePvpDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Pure PVP Contract";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            context.AssertEqual(6, TotemFirstPlayableRules.ParticipantCount, "purePvp.participantCount");
            context.AssertEqual(3, TotemFirstPlayableRules.TeamCount, "purePvp.teamCount");
            context.AssertEqual(2, TotemFirstPlayableRules.TeamSize, "purePvp.teamSize");

            TotemGameplayCatalog catalog = TotemDataService.LoadGameplayCatalogOrDefault();
            TotemMapResourcePickupDefinition[] pickups = catalog.CreateMapResourcePickupDefinitions();
            context.Assert(
                TotemMapResourceGenerator.ValidateDefinitions(pickups, out string pickupError),
                "Map-resource pickup definitions must be config driven and valid: " + pickupError);
            context.AssertEqual(9, pickups.Length, "purePvp.pickupDefinitionCount");
            bool hasDifferentAmountRanges = false;
            for (int i = 0; i < pickups.Length; i++)
            {
                context.Assert(
                    pickups[i].MinAmount < pickups[i].MaxAmount,
                    $"Pickup {pickups[i].PickupId} must use a non-fixed amount range.");
                if (i > 0
                    && (pickups[i].MinAmount != pickups[0].MinAmount
                        || pickups[i].MaxAmount != pickups[0].MaxAmount))
                {
                    hasDifferentAmountRanges = true;
                }
            }
            context.Assert(hasDifferentAmountRanges, "Different pickup definitions must support different amount ranges.");

            TotemMapSnapshot map = TotemMapService.BuildLayout(seed: 260811, themeId: 1);
            TotemMapAnchor[] spawnAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.PlayerSpawn);
            TotemMapAnchor[] resourceAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Resource);
            TotemMapAnchor[] extractionAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Extraction);
            context.Assert(spawnAnchors.Length >= TotemFirstPlayableRules.TeamCount, "Map must expose at least one legal spawn candidate per duo team.");
            context.Assert(resourceAnchors.Length >= 8, "Map must expose enough legal resource anchors for the first-playable development loop.");
            context.Assert(extractionAnchors.Length >= 3, "Map must expose at least three dedicated extraction anchors.");
            context.Detail("purePvp.spawnAnchorCount", spawnAnchors.Length.ToString());
            context.Detail("purePvp.resourceAnchorCount", resourceAnchors.Length.ToString());
            context.Detail("purePvp.extractionAnchorCount", extractionAnchors.Length.ToString());
            var extractionA = new TotemExtractionPoint[TotemExtractionPointGenerator.MaxPointCount];
            var extractionB = new TotemExtractionPoint[TotemExtractionPointGenerator.MaxPointCount];
            int extractionCountA = TotemExtractionPointGenerator.Generate(map, 260811, 3, extractionA);
            int extractionCountB = TotemExtractionPointGenerator.Generate(map, 260811, 3, extractionB);
            context.AssertEqual(3, extractionCountA, "purePvp.extractionPointCount");
            context.AssertEqual(extractionCountA, extractionCountB, "purePvp.deterministicExtractionPointCount");
            for (int i = 0; i < extractionCountA; i++)
            {
                context.AssertEqual(extractionA[i].AnchorId, extractionB[i].AnchorId, $"purePvp.extraction[{i}].anchorId");
            }
            context.Assert(!TotemExtractionService.CanUnlockInPhase(TotemMatchPhase.Round3Combat), "Extraction must remain locked before Round4Combat.");
            context.Assert(TotemExtractionService.CanUnlockInPhase(TotemMatchPhase.Round4Combat), "Extraction must unlock from Round4Combat onward.");
            var generatedA = new TotemMapResourcePickup[TotemMapResourceGenerator.MaxPickupCount];
            var generatedB = new TotemMapResourcePickup[TotemMapResourceGenerator.MaxPickupCount];
            int generatedCountA = TotemMapResourceGenerator.Generate(pickups, map, 260811, 2, generatedA);
            int generatedCountB = TotemMapResourceGenerator.Generate(pickups, map, 260811, 2, generatedB);
            context.AssertEqual(generatedCountA, generatedCountB, "purePvp.deterministicPickupCount");
            for (int i = 0; i < generatedCountA; i++)
            {
                context.AssertEqual(generatedA[i].PickupId, generatedB[i].PickupId, $"purePvp.pickup[{i}].id");
                context.AssertEqual(generatedA[i].Amount, generatedB[i].Amount, $"purePvp.pickup[{i}].amount");
                TotemMapResourcePickupDefinition definition = FindPickup(pickups, generatedA[i].PickupId);
                context.Assert(definition != null, $"Generated pickup {generatedA[i].PickupId} must come from config.");
                if (definition != null)
                {
                    context.Assert(
                        generatedA[i].Amount >= definition.MinAmount && generatedA[i].Amount <= definition.MaxAmount,
                        $"Generated amount for {generatedA[i].PickupId} must stay inside its own configured range.");
                }
            }
            context.AssertEqual("OasisCity", map.SourceSceneName, "purePvp.authoritativeScene");
            context.Assert(map.WorldMin.x < 0f && map.WorldMin.y < 0f, "OasisCity must preserve centered authored coordinates.");
            context.Assert(map.WorldMax.x > 0f && map.WorldMax.y > 0f, "OasisCity authored bounds must span both axes.");

            var teams = new[]
            {
                new TotemTeamSettlementCandidate(1, 1, 500f, 2, 160f, 1),
                new TotemTeamSettlementCandidate(2, 2, 300f, 1, 40f, 3),
                new TotemTeamSettlementCandidate(3, 1, 700f, 2, 180f, 5),
            };
            TotemMatchSettlement settlement = TotemFirstPlayableMatchSettlement.Resolve(teams, teams.Length);
            context.Assert(settlement.Resolved && !settlement.Draw, "Round-three settlement must resolve a winner.");
            context.AssertEqual(2, settlement.Winner.TeamId, "purePvp.settlementWinnerTeam");
            context.Pass("Six-player pure-PVP roster, config-driven pickups and deterministic settlement are available without Enemy runtime services.");
        }

        private static TotemMapResourcePickupDefinition FindPickup(
            TotemMapResourcePickupDefinition[] definitions,
            string pickupId)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                if (string.Equals(definitions[i]?.PickupId, pickupId, System.StringComparison.Ordinal))
                {
                    return definitions[i];
                }
            }

            return null;
        }
    }
}
#endif
