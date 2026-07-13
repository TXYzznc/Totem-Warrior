#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemAIRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem AI Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckAIStateBuild(context);
            CheckLodAndDecisionIntervals(context);
            CheckProfileDrivenBehavior(context);
            CheckSmartPersonalityTargeting(context);
            CheckAggressiveConservativePersonalityBehavior(context);
            CheckWholeRosterDecisionCoverage(context);
            CheckRuntimeCombatServiceRouting(context);
            CheckBossPhaseSkillRuntime(context);
            CheckStatusDrivenBehavior(context);
            CheckDecisionExplainability(context);
            CheckRuntimeDeathChestLooting(context);
            CheckActorDamageEvent(context);
            context.Pass("Totem AI runtime contract is ready.");
        }

        private static void CheckAIStateBuild(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection());
            var actors = roster.Select(info => new TotemActorModel(info)).ToArray();
            var player = actors.First(actor => actor.Kind == TotemActorKind.Player);
            var states = TotemAIService.BuildInitialStates(actors, player.Position);
            var firstSmartActor = actors.First(actor => actor.Kind == TotemActorKind.SmartAi);
            var firstLightActor = actors.First(actor => actor.Kind == TotemActorKind.LightAi);

            context.AssertEqual(50, actors.Length, "ai.participantCount");
            context.AssertEqual(49, states.Length, "ai.stateCount");
            context.AssertEqual(20, states.Count(state => state.Actor.Kind == TotemActorKind.SmartAi), "ai.smartStateCount");
            context.AssertEqual(29, states.Count(state => state.Actor.Kind == TotemActorKind.LightAi), "ai.lightStateCount");
            context.AssertEqual(20, states.Count(state => state.Actor.Kind == TotemActorKind.SmartAi && state.State == TotemAIState.Chase), "ai.smartInitialChase");
            context.AssertEqual(29, states.Count(state => state.Actor.Kind == TotemActorKind.LightAi && state.State == TotemAIState.Wander), "ai.lightInitialWander");
            int initialHotCount = states.Count(state => state.Bucket == TotemAILodBucket.Hot);
            context.Detail("ai.initialHotCount", initialHotCount);
            context.Detail("ai.initialColdCount", states.Length - initialHotCount);
            context.Assert(initialHotCount < states.Length, "Dispersed participant spawning should not force every AI into the initial hot LOD bucket.");
            context.Assert(actors.All(actor => actor.Domain == TotemCombatantDomain.Participant), "AI roster actors must all belong to the Participant domain.");
            context.Assert(states.All(state => state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot
                || state.Actor.ControllerKind == TotemParticipantControllerKind.LightBot), "AI service must control SmartBot/LightBot Participants only.");
            context.AssertEqual(TotemCombatantDomain.Participant, firstSmartActor.Domain,
                "ai.smartBot.domain");
            context.AssertEqual(TotemCombatantDomain.Participant, firstLightActor.Domain,
                "ai.lightBot.domain");
        }

        private static void CheckLodAndDecisionIntervals(GFDiagnosticScenarioContext context)
        {
            var hot = TotemAIService.ResolveBucket(Vector3.zero, new Vector3(0f, 0f, 19.9f));
            var cold = TotemAIService.ResolveBucket(Vector3.zero, new Vector3(0f, 0f, 20.1f));
            context.AssertEqual(TotemAILodBucket.Hot, hot, "ai.bucket.hot");
            context.AssertEqual(TotemAILodBucket.Cold, cold, "ai.bucket.cold");

            AssertNear(context, 0f, TotemAIService.GetDecisionInterval(TotemActorKind.SmartAi, TotemAILodBucket.Hot), "ai.smartHotInterval");
            AssertNear(context, 0.5f, TotemAIService.GetDecisionInterval(TotemActorKind.SmartAi, TotemAILodBucket.Cold), "ai.smartColdInterval");
            AssertNear(context, 0.2f, TotemAIService.GetDecisionInterval(TotemActorKind.LightAi, TotemAILodBucket.Hot), "ai.lightHotInterval");
            AssertNear(context, 2f, TotemAIService.GetDecisionInterval(TotemActorKind.LightAi, TotemAILodBucket.Cold), "ai.lightColdInterval");
        }

        private static void CheckProfileDrivenBehavior(GFDiagnosticScenarioContext context)
        {
            context.Assert(TotemDataService.TryLoadGameplayCatalogFromFile(TotemDataService.GetGameplayCatalogPath(), out var catalog, out string error), $"Gameplay catalog should load for AI profile diagnostics: {error}");
            var profiles = catalog.CreateBotProfiles();
            var presets = catalog.CreateBotBuildPresets();
            context.AssertEqual(23, profiles.Length, "ai.profile.count");
            context.AssertEqual(20, profiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi), "ai.profile.smartCount");
            context.AssertEqual(3, profiles.Count(profile => profile.ActorKind == TotemActorKind.LightAi), "ai.profile.lightCount");
            context.AssertEqual(7, presets.Length, "ai.preset.count");
            AssertPersonalityCount(context, profiles, TotemAIPersonality.Aggressive, 5, "ai.profile.personality.aggressive");
            AssertPersonalityCount(context, profiles, TotemAIPersonality.Conservative, 3, "ai.profile.personality.conservative");
            AssertPersonalityCount(context, profiles, TotemAIPersonality.ResourceAcquisition, 4, "ai.profile.personality.resource");
            AssertPersonalityCount(context, profiles, TotemAIPersonality.BossPriority, 4, "ai.profile.personality.bossPriority");
            AssertPersonalityCount(context, profiles, TotemAIPersonality.PlayerPriority, 4, "ai.profile.personality.playerPriority");

            var bossProfile = profiles.First(profile => profile.Personality == TotemAIPersonality.BossPriority);
            var playerProfile = profiles.First(profile => profile.Personality == TotemAIPersonality.PlayerPriority);
            var resourceProfile = profiles.First(profile => profile.Personality == TotemAIPersonality.ResourceAcquisition);
            context.Assert(bossProfile.TargetBossWeight > bossProfile.TargetResourceWeight && bossProfile.TargetBossWeight > bossProfile.TargetPlayerWeight, "Boss-priority profile should weight active Boss above resources and humanoids.");
            AssertNear(context, playerProfile.TargetPlayerWeight, playerProfile.TargetHumanoidAiWeight, "ai.profile.playerPriority.samePlayerAiWeight");
            context.Assert(resourceProfile.TargetResourceWeight > resourceProfile.TargetPlayerWeight && resourceProfile.ShopPreference > 0.5f, "Resource profile should prefer resource/shop behavior over direct target pressure.");

            var map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection());
            var actors = roster.Select(info => new TotemActorModel(info)).ToArray();
            var player = actors.First(actor => actor.Kind == TotemActorKind.Player);
            var states = TotemAIService.BuildInitialStates(actors, player.Position, profiles, presets);
            context.AssertEqual(49, states.Count(state => state.Profile != null), "ai.profiledStateCount");
            context.AssertEqual(20, states.Count(state => state.Actor.Kind == TotemActorKind.SmartAi && state.BuildPreset != null), "ai.profiledSmartBuildCount");
            context.AssertEqual(29, states.Count(state => state.Actor.Kind == TotemActorKind.LightAi && state.Profile != null), "ai.profiledLightCount");

            var firstSmart = states.First(state => state.Actor.Kind == TotemActorKind.SmartAi);
            var eighthSmart = states.Where(state => state.Actor.Kind == TotemActorKind.SmartAi).Skip(7).First();
            var twentiethSmart = states.Where(state => state.Actor.Kind == TotemActorKind.SmartAi).Skip(19).First();
            context.AssertEqual(1, firstSmart.Profile.BotId, "ai.profile.firstSmartBotId");
            context.AssertEqual(8, eighthSmart.Profile.BotId, "ai.profile.eighthSmartBotId");
            context.AssertEqual(20, twentiethSmart.Profile.BotId, "ai.profile.twentiethSmartBotId");
            context.AssertEqual(1, firstSmart.BuildPreset.PresetId, "ai.profile.firstSmartPreset");
            context.AssertEqual(TotemAIBehaviorMacro.Rush, firstSmart.BuildPreset.BehaviorMacro, "ai.profile.firstSmartMacro");

            var firstLight = states.First(state => state.Actor.Kind == TotemActorKind.LightAi);
            var fourthLight = states.Where(state => state.Actor.Kind == TotemActorKind.LightAi).Skip(3).First();
            context.AssertEqual(101, firstLight.Profile.BotId, "ai.profile.firstLightBotId");
            context.AssertEqual(101, fourthLight.Profile.BotId, "ai.profile.lightCycleBotId");

            context.Assert(TotemAIService.TryPlanNextBuild(firstSmart.BuildPreset, 0, out var firstPlan), "Smart preset should produce first tattoo plan.");
            context.AssertEqual("Part4/Color1/Pattern2", firstPlan.Format(), "ai.build.firstPlan");
            context.Assert(TotemAIService.TryPlanNextBuild(firstSmart.BuildPreset, 1 << firstPlan.partId, out var secondPlan), "Smart preset should produce second tattoo plan.");
            context.AssertEqual("Part1/Color1/Pattern6", secondPlan.Format(), "ai.build.secondPlan");
            context.Assert(TotemAIService.ShouldStartSelfTattoo(0.60f, firstSmart.Profile.SelfTattooBoldness), "Smart self tattoo should start above boldness threshold.");
            context.Assert(!TotemAIService.ShouldStartSelfTattoo(0.30f, firstSmart.Profile.SelfTattooBoldness), "Smart self tattoo should wait below boldness threshold.");
        }

        private static void CheckAggressiveConservativePersonalityBehavior(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIPersonalityBehaviorDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var aggressiveState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.Aggressive);
                var conservativeState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.Conservative);
                var aggressiveTarget = ai.States.First(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).Actor;
                var conservativeTarget = ai.States.Where(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).Skip(1).First().Actor;

                context.Assert(aggressiveState.BuildPreset != null && aggressiveState.BuildPreset.BehaviorMacro == TotemAIBehaviorMacro.Rush, "Aggressive profile should use Rush behavior macro.");
                context.Assert(conservativeState.BuildPreset != null && conservativeState.BuildPreset.BehaviorMacro == TotemAIBehaviorMacro.Camp, "Conservative profile should use Camp behavior macro.");
                context.Assert(aggressiveState.Profile.AttackCooldown < conservativeState.Profile.AttackCooldown, "Aggressive profile should attack more frequently than conservative profile.");
                context.Assert(aggressiveState.Profile.AggroRadius > conservativeState.Profile.AggroRadius, "Aggressive profile should accept a wider chase radius than conservative profile.");
                context.Assert(aggressiveState.Profile.TargetHumanoidAiWeight > conservativeState.Profile.TargetHumanoidAiWeight, "Aggressive profile should score humanoid targets above conservative profile.");
                context.Assert(aggressiveState.Profile.RiskTolerance > conservativeState.Profile.RiskTolerance, "Aggressive profile should tolerate more risk than conservative profile.");

                MoveActorsAwayExcept(actor, aggressiveState.Actor, conservativeState.Actor, aggressiveTarget, conservativeTarget);
                aggressiveState.Actor.Position = new Vector3(0f, 0.5f, 0f);
                aggressiveTarget.Position = aggressiveState.Actor.Position + new Vector3(16f, 0f, 0f);
                conservativeState.Actor.Position = new Vector3(100f, 0.5f, 0f);
                conservativeTarget.Position = conservativeState.Actor.Position + new Vector3(16f, 0f, 0f);

                SuppressRegularAiDecisions(ai);
                aggressiveState.NextBuildRethinkTime = 999f;
                aggressiveState.NextDecisionTime = 0f;
                aggressiveState.AttackCooldownRemaining = 999f;
                aggressiveState.SkillCooldownRemaining = 999f;
                aggressiveState.SafetyScore = 0.4f;

                float aggressiveDistanceBefore = FlatDistance(aggressiveState.Actor.Position, aggressiveTarget.Position);
                ai.Tick(0.1f);
                float aggressiveDistanceAfter = FlatDistance(aggressiveState.Actor.Position, aggressiveTarget.Position);
                context.Detail("ai.personality.aggressive.distanceBefore", aggressiveDistanceBefore);
                context.Detail("ai.personality.aggressive.distanceAfter", aggressiveDistanceAfter);
                context.AssertEqual("Chase", aggressiveState.LastDecision.Action, "ai.personality.aggressive.action");
                context.AssertEqual("TargetVisible", aggressiveState.LastDecision.Reason, "ai.personality.aggressive.reason");
                context.AssertEqual(aggressiveTarget.ActorId, aggressiveState.LastDecision.TargetActorId, "ai.personality.aggressive.targetId");
                context.Assert(aggressiveDistanceAfter < aggressiveDistanceBefore, "Aggressive AI should move toward a visible humanoid target at 16m.");

                SuppressRegularAiDecisions(ai);
                conservativeState.NextBuildRethinkTime = 999f;
                conservativeState.NextDecisionTime = 0f;
                conservativeState.AttackCooldownRemaining = 999f;
                conservativeState.SkillCooldownRemaining = 999f;
                conservativeState.SafetyScore = 0.4f;

                float conservativeDistanceBefore = FlatDistance(conservativeState.Actor.Position, conservativeTarget.Position);
                ai.Tick(0.1f);
                float conservativeDistanceAfter = FlatDistance(conservativeState.Actor.Position, conservativeTarget.Position);
                context.Detail("ai.personality.conservative.distanceBefore", conservativeDistanceBefore);
                context.Detail("ai.personality.conservative.distanceAfter", conservativeDistanceAfter);
                context.AssertEqual("Wander", conservativeState.LastDecision.Action, "ai.personality.conservative.action");
                context.AssertEqual("TargetOutsideChasePreference", conservativeState.LastDecision.Reason, "ai.personality.conservative.reason");
                context.AssertEqual(conservativeTarget.ActorId, conservativeState.LastDecision.TargetActorId, "ai.personality.conservative.targetId");
                AssertNear(context, conservativeDistanceBefore, conservativeDistanceAfter, "ai.personality.conservative.distanceAfter");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckSmartPersonalityTargeting(GFDiagnosticScenarioContext context)
        {
            CheckBossPriorityTargetOverride(context);
            CheckPlayerPriorityTargetTieBreak(context);
            CheckResourceAcquisitionPickupTargeting(context);
            CheckResourceAcquisitionShopPurchase(context);
        }

        private static void CheckBossPriorityTargetOverride(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIBossPriorityDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var enemies = runtime.GetService<TotemEnemyService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                runtime.GetService<TotemMatchClockService>()?.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);
                var bossState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.BossPriority);
                var bossHunter = bossState.Actor;
                var boss = SpawnEnemy(context, enemies, 930001, "boss_ai_core_zero", Vector3.zero, "diagnostic.ai.bossPriority");
                var chestVictim = ai.States.First(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).Actor;
                context.Assert(boss != null && boss.IsAlive, "Boss priority diagnostic requires an active EnemyService Boss.");

                MoveActorsAwayExcept(actor, bossHunter, chestVictim);
                bossHunter.Position = new Vector3(0f, 0.5f, 0f);
                boss.Position = bossHunter.Position + new Vector3(12f, 0f, 0f);
                chestVictim.Position = bossHunter.Position + new Vector3(1.2f, 0f, 0f);
                economy.AddCoins(chestVictim, 100);
                context.Assert(actor.ApplyDamage(chestVictim, chestVictim.Health + 1f, bossHunter, "DiagnosticBossPriorityChest"), "Boss-priority diagnostic victim should die.");
                context.AssertEqual(1, economy.PendingDeathChestCount, "ai.personality.bossPriority.pendingChestBefore");

                SuppressRegularAiDecisions(ai);
                bossState.NextBuildRethinkTime = 999f;
                bossState.NextDecisionTime = 0f;
                bossState.AttackCooldownRemaining = 999f;
                bossState.SkillCooldownRemaining = 999f;
                bossState.SafetyScore = 1f;
                float bossDistanceBefore = FlatDistance(bossHunter.Position, boss.Position);
                ai.Tick(0.1f);
                float bossDistanceAfter = FlatDistance(bossHunter.Position, boss.Position);

                context.Detail("ai.personality.bossPriority.distanceBefore", bossDistanceBefore);
                context.Detail("ai.personality.bossPriority.distanceAfter", bossDistanceAfter);
                context.AssertEqual("Chase", bossState.LastDecision.Action, "ai.personality.bossPriority.action");
                context.AssertEqual(TotemCombatantDomain.Enemy, bossState.LastDecision.TargetDomain, "ai.personality.bossPriority.targetDomain");
                context.AssertEqual(TotemEnemyTier.Boss, bossState.LastDecision.TargetEnemyTier, "ai.personality.bossPriority.targetTier");
                context.AssertEqual(boss.CombatantId, bossState.LastDecision.TargetActorId, "ai.personality.bossPriority.targetId");
                context.AssertEqual("BossPriority", bossState.LastDecision.Reason, "ai.personality.bossPriority.reason");
                AssertNear(context, bossDistanceBefore, bossState.LastDecision.Distance, "ai.personality.bossPriority.decisionDistance");
                context.Assert(bossDistanceAfter < bossDistanceBefore, "Boss-priority AI should chase the active Boss and close distance.");
                context.AssertEqual(TotemAIPersonality.BossPriority, bossState.LastDecision.Personality, "ai.personality.bossPriority.recordPersonality");
                context.AssertEqual(1, economy.PendingDeathChestCount, "ai.personality.bossPriority.leavesResourceChest");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckPlayerPriorityTargetTieBreak(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIPlayerPriorityDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var playerState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.PlayerPriority);
                var hunter = playerState.Actor;
                var player = actor.Player;
                var closerHumanoid = ai.States.First(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).Actor;

                MoveActorsAwayExcept(actor, hunter, player, closerHumanoid);
                hunter.Position = new Vector3(0f, 0.5f, 0f);
                closerHumanoid.Position = hunter.Position + new Vector3(6f, 0f, 0f);
                player.Position = hunter.Position + new Vector3(10f, 0f, 0f);

                SuppressRegularAiDecisions(ai);
                playerState.NextBuildRethinkTime = 999f;
                playerState.NextDecisionTime = 0f;
                playerState.AttackCooldownRemaining = 999f;
                playerState.SkillCooldownRemaining = 999f;
                playerState.SafetyScore = 1f;
                float playerPriorityDistanceBefore = FlatDistance(hunter.Position, closerHumanoid.Position);
                ai.Tick(0.1f);
                float playerPriorityDistanceAfter = FlatDistance(hunter.Position, closerHumanoid.Position);

                context.Detail("ai.personality.playerPriority.distanceBefore", playerPriorityDistanceBefore);
                context.Detail("ai.personality.playerPriority.distanceAfter", playerPriorityDistanceAfter);
                context.AssertEqual("Chase", playerState.LastDecision.Action, "ai.personality.playerPriority.action");
                context.AssertEqual(closerHumanoid.ActorId, playerState.LastDecision.TargetActorId, "ai.personality.playerPriority.targetId");
                context.AssertEqual(TotemActorKind.LightAi, playerState.LastDecision.TargetKind, "ai.personality.playerPriority.targetKind");
                context.AssertEqual("PlayerPriorityTarget", playerState.LastDecision.Reason, "ai.personality.playerPriority.reason");
                AssertNear(context, playerPriorityDistanceBefore, playerState.LastDecision.Distance, "ai.personality.playerPriority.decisionDistance");
                context.Assert(playerPriorityDistanceAfter < playerPriorityDistanceBefore, "Player-priority AI should chase the selected humanoid target and close distance.");
                context.AssertEqual(TotemAIPersonality.PlayerPriority, playerState.LastDecision.Personality, "ai.personality.playerPriority.recordPersonality");
                context.Assert(playerState.LastDecision.TargetActorId != player.ActorId, "Player-priority should not force the real player over a closer humanoid AI yet.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckResourceAcquisitionPickupTargeting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIResourcePickupDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var weapon = runtime.GetService<TotemWeaponService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var resourceState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.ResourceAcquisition);
                var resourceHunter = resourceState.Actor;
                var pickup = weapon.ActivePickups.FirstOrDefault(item => item != null && item.Source == "MapResource");
                context.Assert(pickup != null, "Resource acquisition diagnostic requires a map resource pickup.");
                context.Assert(resourceState.Profile.TargetResourceWeight >= TotemAIService.MinMapResourceChaseWeight, "Resource acquisition profile should be allowed to chase map pickups.");

                MoveActorsAwayExcept(actor, resourceHunter);
                SuppressRegularAiDecisions(ai);
                resourceHunter.Position = pickup.Position + new Vector3(TotemWeaponService.PickupInteractRadius + 1f, 0f, 0f);
                resourceState.NextBuildRethinkTime = 999f;
                resourceState.NextDecisionTime = 0f;
                resourceState.AttackCooldownRemaining = 999f;
                resourceState.SkillCooldownRemaining = 999f;
                resourceState.SafetyScore = 1f;

                float distanceBeforeChase = FlatDistance(resourceHunter.Position, pickup.Position);
                ai.Tick(0.1f);
                context.Detail("ai.personality.resource.pickupDistanceBeforeChase", distanceBeforeChase);
                context.Detail("ai.personality.resource.pickupDistanceAfterChase", FlatDistance(resourceHunter.Position, pickup.Position));
                context.AssertEqual("Loot", resourceState.LastDecision.Action, "ai.personality.resource.chase.action");
                context.AssertEqual("ChaseMapResourcePickup", resourceState.LastDecision.Reason, "ai.personality.resource.chase.reason");
                context.AssertEqual(pickup.InstanceId, resourceState.LastDecision.PickupInstanceId, "ai.personality.resource.chase.pickupId");
                context.AssertEqual(pickup.WeaponId, resourceState.LastDecision.PickupWeaponId, "ai.personality.resource.chase.weaponId");
                context.AssertEqual("MapResource", resourceState.LastDecision.PickupSource, "ai.personality.resource.chase.source");
                context.Assert(resourceState.ResourcePickupTarget == pickup, "Resource acquisition state should remember the active pickup target.");

                int activeBeforePickup = weapon.CapturePickupSnapshot().activePickupCount;
                resourceHunter.Position = pickup.Position + new Vector3(TotemWeaponService.PickupInteractRadius * 0.5f, 0f, 0f);
                resourceState.NextDecisionTime = 0f;
                ai.Tick(0.1f);
                var pickupSnapshot = weapon.CapturePickupSnapshot();
                context.AssertEqual(activeBeforePickup - 1, pickupSnapshot.activePickupCount, "ai.personality.resource.pickup.activeCount");
                context.AssertEqual(resourceHunter.ActorId, pickupSnapshot.lastPickupActorId, "ai.personality.resource.pickup.actorId");
                context.AssertEqual(pickup.WeaponId, pickupSnapshot.lastPickupWeaponId, "ai.personality.resource.pickup.weaponId");
                context.AssertEqual(1, resourceState.ResourcePickupClaims, "ai.personality.resource.pickup.claimCount");
                context.AssertEqual("ClaimMapResourcePickup", resourceState.LastDecision.Reason, "ai.personality.resource.pickup.reason");
                context.AssertEqual(pickup.InstanceId, resourceState.LastDecision.PickupInstanceId, "ai.personality.resource.pickup.pickupId");
                context.AssertEqual(1, ai.CaptureSnapshot().totalResourcePickupClaims, "ai.personality.resource.pickup.snapshotClaims");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckResourceAcquisitionShopPurchase(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIResourceShopDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var npc = runtime.GetService<TotemNpcService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var resourceState = ai.States.First(state => state.Profile != null && state.Profile.Personality == TotemAIPersonality.ResourceAcquisition);
                var shopper = resourceState.Actor;
                var merchant = npc.Npcs.FirstOrDefault(item => item != null && item.Type == TotemNpcType.Merchant && item.Offers != null && item.Offers.Length > 0);
                context.Assert(merchant != null, "Resource shop diagnostic requires a merchant with offers.");
                context.Assert(resourceState.Profile.ShopPreference >= TotemAIService.MinSmartShopPreference, "Resource profile should be allowed to pursue shop purchases.");

                MoveActorsAwayExcept(actor, shopper);
                SuppressRegularAiDecisions(ai);
                merchant.Position = new Vector3(220f, 0.5f, 220f);
                shopper.Position = merchant.Position + new Vector3(merchant.InteractRadius + 2f, 0f, 0f);
                economy.AddCoins(shopper, 200);
                resourceState.NextBuildRethinkTime = 999f;
                resourceState.NextDecisionTime = 0f;
                resourceState.AttackCooldownRemaining = 999f;
                resourceState.SkillCooldownRemaining = 999f;
                resourceState.SafetyScore = 1f;

                float distanceBeforeChase = FlatDistance(shopper.Position, merchant.Position);
                ai.Tick(0.1f);
                float distanceAfterChase = FlatDistance(shopper.Position, merchant.Position);
                context.Detail("ai.personality.resource.shopDistanceBeforeChase", distanceBeforeChase);
                context.Detail("ai.personality.resource.shopDistanceAfterChase", distanceAfterChase);
                context.AssertEqual("Shop", resourceState.LastDecision.Action, "ai.personality.resource.shop.chase.action");
                context.AssertEqual("ChaseMerchant", resourceState.LastDecision.Reason, "ai.personality.resource.shop.chase.reason");
                context.AssertEqual(merchant.NpcId, resourceState.LastDecision.NpcId, "ai.personality.resource.shop.chase.npcId");
                context.Assert(resourceState.LastDecision.ShopItemId > 0, "Shop chase decision should expose the intended item id.");
                context.Assert(distanceAfterChase < distanceBeforeChase, "Resource acquisition AI should move toward a reachable merchant.");
                context.Assert(resourceState.ShopTargetNpc == merchant, "Resource acquisition state should remember the active shop target.");

                int coinsBeforePurchase = economy.CaptureInventory(shopper).coins;
                int stockBeforePurchase = resourceState.LastDecision.ShopStockLeft;
                shopper.Position = merchant.Position + new Vector3(merchant.InteractRadius * 0.5f, 0f, 0f);
                resourceState.NextDecisionTime = 0f;
                ai.Tick(0.1f);
                var inventoryAfter = economy.CaptureInventory(shopper);
                var aiSnapshot = ai.CaptureSnapshot();
                context.AssertEqual("Shop", resourceState.LastDecision.Action, "ai.personality.resource.shop.purchase.action");
                context.AssertEqual("PurchaseShopOffer", resourceState.LastDecision.Reason, "ai.personality.resource.shop.purchase.reason");
                context.AssertEqual(merchant.NpcId, resourceState.LastDecision.NpcId, "ai.personality.resource.shop.purchase.npcId");
                context.AssertEqual(TotemShopRewardType.WeaponUpgrade, resourceState.LastDecision.ShopRewardType, "ai.personality.resource.shop.purchase.rewardType");
                context.Assert(resourceState.LastDecision.ShopPrice > 0, "Shop purchase decision should expose the actual price.");
                context.Assert(inventoryAfter.coins < coinsBeforePurchase, "Shop purchase should spend AI coins through TotemEconomyService.");
                context.AssertEqual(stockBeforePurchase - 1, resourceState.LastDecision.ShopStockLeft, "ai.personality.resource.shop.purchase.stockLeft");
                context.AssertEqual(1, resourceState.ShopPurchases, "ai.personality.resource.shop.purchase.count");
                context.AssertEqual(1, aiSnapshot.totalShopPurchases, "ai.personality.resource.shop.purchase.snapshotCount");
                context.AssertEqual(resourceState.LastDecision.ShopItemId, aiSnapshot.lastDecisionShopItemId, "ai.personality.resource.shop.purchase.snapshotItemId");
                context.AssertEqual(resourceState.LastDecision.ShopRewardType, aiSnapshot.lastDecisionShopRewardType, "ai.personality.resource.shop.purchase.snapshotRewardType");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckRuntimeCombatServiceRouting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAICombatDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var skill = runtime.GetService<TotemSkillService>();
                var tattoo = runtime.GetService<TotemTattooService>();
                var vfx = runtime.GetService<TotemVfxService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                runtime.GetService<TotemMatchClockService>()?.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);
                context.Assert(actor.Player != null, "AI combat diagnostic player should spawn.");
                context.AssertEqual(49, ai.States.Count, "ai.runtime.stateCount");

                var smartState = ai.States.First(state => state.Actor.Kind == TotemActorKind.SmartAi);
                var smart = smartState.Actor;
                var player = actor.Player;
                context.AssertEqual(TotemParticipantLifecycle.Active, smart.Lifecycle, "ai.runtime.smartLifecycle");
                context.AssertEqual(TotemParticipantLifecycle.Active, player.Lifecycle, "ai.runtime.playerLifecycle");
                MoveActorsAwayExcept(actor, smart, player);
                smart.Position = player.Position + new Vector3(1f, 0f, 0f);
                smartState.NextBuildRethinkTime = 999f;
                smartState.AttackCooldownRemaining = 0f;
                smartState.SkillCooldownRemaining = 0f;
                smartState.SafetyScore = 1f;

                context.AssertEqual("pistol_basic", weapon.GetEquippedWeaponId(smart), "ai.runtime.smartWeapon");
                context.Assert(skill.GetCurrentCharges(smart, 0) > 0, "Smart AI default skill slot should have charges.");
                context.Assert(tattoo.Equip(smart, 4, 1, 1), "Smart AI attack tattoo should equip.");

                int ammoBefore = weapon.GetOrCreateState(smart).CurrentAmmo;
                float hpBefore = player.Health;
                int vfxBefore = vfx.SpawnedCount;
                ai.Tick(0.2f);
                context.Assert(player.Health < hpBefore, "Smart AI weapon-routed attack should damage player.");
                context.Assert(weapon.GetOrCreateState(smart).CurrentAmmo < ammoBefore, "Smart AI attack should consume pistol ammo.");
                context.Assert(tattoo.CaptureSnapshot(smart).appliedEffectCount > 0, "Smart AI attack should trigger actor-scoped tattoo effects.");
                context.Assert(vfx.SpawnedCount > vfxBefore, "Smart AI attack should spawn VFX through TotemVfxService.");

                context.Assert(tattoo.Equip(smart, 3, 1, 1), "Smart AI skill tattoo should equip.");
                int skillUsesBefore = ai.CaptureSnapshot().totalSkillUses;
                int tattooEffectsBefore = tattoo.CaptureSnapshot(smart).appliedEffectCount;
                smartState.AttackCooldownRemaining = 999f;
                for (int i = 0; i < 100 && ai.CaptureSnapshot().totalSkillUses <= skillUsesBefore; i++)
                {
                    smartState.NextDecisionTime = 0f;
                    smartState.SkillCooldownRemaining = 0f;
                    ai.Tick(0.1f);
                }

                context.Assert(ai.CaptureSnapshot().totalSkillUses > skillUsesBefore, "Smart AI should route skills through TotemSkillService.");
                context.Assert(tattoo.CaptureSnapshot(smart).appliedEffectCount > tattooEffectsBefore, "Smart AI skill should trigger actor-scoped skill tattoos.");

                var nearbyDecoy = ai.States.First(state => state.Actor.Kind == TotemActorKind.LightAi).Actor;
                var readingPrey = ai.States.Where(state => state.Actor.Kind == TotemActorKind.LightAi).Skip(1).First().Actor;
                MoveActorsAwayExcept(actor, smart, nearbyDecoy, readingPrey);
                SuppressRegularAiDecisions(ai);
                nearbyDecoy.Position = smart.Position + new Vector3(1.2f, 0f, 0f);
                readingPrey.Position = smart.Position + new Vector3(5.5f, 0f, 0f);
                context.Assert(tattoo.StartSelfTattoo(readingPrey, 2, 1, 1), "Reading prey should start self tattoo.");

                float decoyHpBefore = nearbyDecoy.Health;
                float preyHpBefore = readingPrey.Health;
                weapon.GetOrCreateState(smart).CooldownRemaining = 0f;
                smartState.AttackCooldownRemaining = 0f;
                smartState.SkillCooldownRemaining = 999f;
                smartState.NextDecisionTime = 0f;
                ai.Tick(0.1f);
                context.Assert(readingPrey.Health < preyHpBefore, "Smart AI should prioritize and attack a reading prey inside aggro radius.");
                context.AssertEqual(readingPrey.ActorId, smartState.LastDecision.TargetActorId, "ai.readingPrey.primaryTargetId");
                context.Detail("ai.readingPrey.decoySecondaryDamage", (decoyHpBefore - nearbyDecoy.Health).ToString("F2"));
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterAiCombatDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemMatchClockService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemCombatRelationshipService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemVfxService());
            runtime.RegisterService(new TotemEnemyWorldService());
            runtime.RegisterService(new TotemEnemyService());
            runtime.RegisterService(new TotemAIService());
        }

        private static void CheckBossPhaseSkillRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIBossSkillDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var enemies = runtime.GetService<TotemEnemyService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                var boss = SpawnEnemy(
                    context,
                    enemies,
                    930002,
                    "boss_ai_core_zero",
                    player.Position + Vector3.forward * 2f,
                    "diagnostic.ai.nativeBoss");
                context.Assert(player != null && boss != null, "Boss skill diagnostic requires a player Participant and EnemyService Boss.");
                context.AssertEqual(50, actor.Actors.Count, "ai.nativeBoss.participantCount");
                context.AssertEqual(49, ai.States.Count, "ai.nativeBoss.participantAiCount");
                context.Assert(ai.States.All(state => state.Actor.Domain == TotemCombatantDomain.Participant),
                    "Native Boss must not create an AI actor state.");
                MoveActorsAwayExcept(actor, player);
                SuppressRegularAiDecisions(ai);

                int phaseEventCount = 0;
                int lastPhase = 0;
                enemies.BossPhaseChanged += evt =>
                {
                    if (evt.Enemy == boss)
                    {
                        phaseEventCount++;
                        lastPhase = evt.CurrentPhase;
                    }
                };

                enemies.Tick(0.1f);
                context.AssertEqual(1, lastPhase, "ai.nativeBoss.phase1");
                context.Assert(enemies.TryApplyDamage(
                    boss.CombatantId,
                    player,
                    boss.MaxHealth * 0.45f,
                    "DiagnosticNativeBossPhase2",
                    0.2f,
                    out var phase2Damage) && phase2Damage > 0f,
                    "Participant damage should enter EnemyService for phase 2.");
                enemies.Tick(0.1f);
                context.AssertEqual(2, lastPhase, "ai.nativeBoss.phase2");
                context.Assert(enemies.TryApplyDamage(
                    boss.CombatantId,
                    player,
                    boss.MaxHealth * 0.35f,
                    "DiagnosticNativeBossPhase3",
                    0.3f,
                    out var phase3Damage) && phase3Damage > 0f,
                    "Participant damage should enter EnemyService for phase 3.");
                enemies.Tick(0.1f);

                var controller = enemies.FindController(boss.CombatantId);
                var enemySnapshot = enemies.CaptureSnapshot();
                context.AssertEqual(3, controller?.BossPhase ?? 0, "ai.nativeBoss.controllerPhase");
                context.AssertEqual(3, lastPhase, "ai.nativeBoss.lastPhaseEvent");
                context.AssertEqual(3, phaseEventCount, "ai.nativeBoss.phaseEventCount");
                context.AssertEqual(1, enemySnapshot.bossCount, "ai.nativeBoss.snapshotBossCount");

                float playerHealthBeforeEnemyAttack = player.Health;
                for (int i = 0; i < 100 && player.IsAlive && player.Health >= playerHealthBeforeEnemyAttack; i++)
                {
                    enemies.Tick(0.1f);
                }

                context.Assert(player.Health < playerHealthBeforeEnemyAttack, "Native EnemyService Boss should damage the player through EnemyWorld bridge.");
                TotemCombatantModel damageSource = actor.LastDamage.Source;
                context.Detail("ai.nativeBoss.damageSource.actual", damageSource?.CombatantId ?? 0);
                context.Assert(
                    damageSource is TotemEnemyModel && enemies.FindEnemy(damageSource.CombatantId) == damageSource,
                    "Native Enemy combat damage source should be the actual registered Enemy attacker.");
                context.AssertEqual(TotemCombatantDomain.Enemy, actor.LastDamage.Source?.Domain ?? TotemCombatantDomain.Participant, "ai.nativeBoss.damageDomain");
                context.Assert(actor.LastDamage.Reason.StartsWith("Enemy", System.StringComparison.Ordinal),
                    "Native Boss damage reason should come from the Enemy runtime bridge.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckWholeRosterDecisionCoverage(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIWholeRosterDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var ai = runtime.GetService<TotemAIService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                ai.Tick(0.2f);
                var snapshot = ai.CaptureSnapshot();
                context.AssertEqual(49, snapshot.smartCount + snapshot.lightCount, "ai.wholeRoster.stateCount");
                context.AssertEqual(20, snapshot.smartCount, "ai.wholeRoster.smartCount");
                context.AssertEqual(29, snapshot.lightCount, "ai.wholeRoster.lightCount");
                context.Detail("ai.wholeRoster.hotCount", snapshot.hotCount);
                context.Detail("ai.wholeRoster.coldCount", snapshot.smartCount + snapshot.lightCount - snapshot.hotCount);
                context.Assert(snapshot.hotCount < snapshot.smartCount + snapshot.lightCount, "Whole-roster AI diagnostic should preserve dispersed cold LOD actors.");
                context.Assert(snapshot.totalDecisions >= 49, "AI whole roster should produce at least one decision per non-human participant AI.");
                context.AssertEqual(20, ai.States.Count(state => state.Actor.Kind == TotemActorKind.SmartAi && state.Decisions > 0 && state.LastDecision.Sequence > 0), "ai.wholeRoster.smartDecisionCount");
                context.AssertEqual(29, ai.States.Count(state => state.Actor.Kind == TotemActorKind.LightAi && state.Decisions > 0 && state.LastDecision.Sequence > 0), "ai.wholeRoster.lightDecisionCount");
                context.Assert(ai.States.All(state => !string.IsNullOrWhiteSpace(state.LastDecision.Action)), "Every AI state should expose a non-empty last decision action.");
                context.Assert(snapshot.lastDecisionSequence > 0, "AI whole roster snapshot should expose latest decision sequence.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckDecisionExplainability(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIDecisionDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var weapon = runtime.GetService<TotemWeaponService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                runtime.GetService<TotemMatchClockService>()?.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);
                var smartState = ai.States.First(state => state.Actor.Kind == TotemActorKind.SmartAi);
                var smart = smartState.Actor;
                var player = actor.Player;
                MoveActorsAwayExcept(actor, smart, player);
                smart.Position = player.Position + new Vector3(1.2f, 0f, 0f);
                smartState.NextBuildRethinkTime = 999f;
                smartState.NextDecisionTime = 0f;
                smartState.AttackCooldownRemaining = 0f;
                smartState.SkillCooldownRemaining = 999f;
                smartState.SafetyScore = 1f;
                weapon.GetOrCreateState(smart).CooldownRemaining = 0f;

                ai.Tick(0.2f);

                var snapshot = ai.CaptureSnapshot();
                var smartDecision = smartState.LastDecision;
                context.Detail("ai.decision.sequence", snapshot.lastDecisionSequence);
                context.Detail("ai.decision.action", snapshot.lastDecisionAction);
                context.Detail("ai.decision.reason", snapshot.lastDecisionReason);
                context.Detail("ai.decision.actor", snapshot.lastDecisionActorName);
                context.Detail("ai.decision.target", snapshot.lastDecisionTargetName);
                context.Assert(snapshot.lastDecisionSequence > 0, "AI snapshot should expose a decision sequence.");
                context.Assert(!string.IsNullOrWhiteSpace(snapshot.lastDecisionAction), "AI snapshot should expose a decision action.");
                context.Assert(smartDecision != null && smartDecision.Sequence > 0, "Smart AI state should expose its own last decision.");
                context.AssertEqual(smart.ActorId, smartDecision.ActorId, "ai.decision.smart.actorId");
                context.AssertEqual("Attack", smartDecision.Action, "ai.decision.smart.action");
                context.AssertEqual("WeaponAttack", smartDecision.Reason, "ai.decision.smart.reason");
                context.AssertEqual(player.ActorId, smartDecision.TargetActorId, "ai.decision.smart.targetId");
                context.AssertEqual("pistol_basic", smartDecision.WeaponId, "ai.decision.smart.weaponId");
                context.Assert(smartDecision.Distance > 0f, "Smart AI decision should expose target distance.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckStatusDrivenBehavior(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIStatusDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var status = runtime.GetService<TotemStatusService>();

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var smartState = ai.States.First(state => state.Actor.Kind == TotemActorKind.SmartAi);
                var smart = smartState.Actor;
                var player = actor.Player;
                context.AssertEqual(TotemParticipantLifecycle.Active, smart.Lifecycle, "ai.status.stun.smartLifecycle");
                context.AssertEqual(TotemParticipantLifecycle.Active, player.Lifecycle, "ai.status.stun.playerLifecycle");
                MoveActorsAwayExcept(actor, smart, player);
                smart.Position = player.Position + new Vector3(1.2f, 0f, 0f);
                smartState.NextBuildRethinkTime = 999f;
                smartState.NextDecisionTime = 0f;
                smartState.AttackCooldownRemaining = 0f;
                smartState.SkillCooldownRemaining = 0f;
                smartState.SafetyScore = 1f;
                weapon.GetOrCreateState(smart).CooldownRemaining = 0f;

                float playerHpBeforeStun = player.Health;
                int attacksBeforeStun = smartState.Attacks;
                status.ApplyStatus(smart, TotemStatusService.StunStatus, 0f, 1f);
                ai.Tick(0.2f);
                AssertNear(context, playerHpBeforeStun, player.Health, "ai.status.stun.noAttackDamage");
                context.AssertEqual(attacksBeforeStun, smartState.Attacks, "ai.status.stun.noAttackCount");
                context.AssertEqual(TotemAIState.Idle, smartState.State, "ai.status.stun.state");
                context.AssertEqual("Status:Stun", smartState.LastDecision.Reason, "ai.status.stun.reason");

                status.ClearAllStatuses(smart);
                smart.Position = player.Position + new Vector3(8f, 0f, 0f);
                smartState.NextDecisionTime = 0f;
                smartState.AttackCooldownRemaining = 999f;
                smartState.SkillCooldownRemaining = 999f;
                Vector3 baselineStart = smart.Position;
                ai.Tick(0.2f);
                float baselineMove = (smart.Position - baselineStart).magnitude;
                context.Assert(baselineMove > 0.01f, "AI should move toward target before slow comparison.");

                smart.Position = player.Position + new Vector3(8f, 0f, 0f);
                smartState.NextDecisionTime = 0f;
                status.ApplyStatus(smart, TotemStatusService.SlowStatus, 0.5f, 1f);
                Vector3 slowedStart = smart.Position;
                ai.Tick(0.2f);
                float slowedMove = (smart.Position - slowedStart).magnitude;
                context.Assert(slowedMove > 0.01f, "Slowed AI should still be able to move.");
                context.Assert(slowedMove < baselineMove * 0.75f, "Slow status should reduce AI movement distance.");
                AssertNear(context, 0.5f, status.GetMoveSpeedMultiplier(smart), "ai.status.slow.multiplier");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckRuntimeDeathChestLooting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAIDeathChestDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAiCombatDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var ai = runtime.GetService<TotemAIService>();
                var economy = runtime.GetService<TotemEconomyService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                runtime.GetService<TotemMatchClockService>()?.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);

                var smartState = ai.States.First(state => state.Actor.Kind == TotemActorKind.SmartAi);
                var smart = smartState.Actor;
                var firstVictim = ai.States.First(state => state.Actor.Kind == TotemActorKind.LightAi).Actor;
                MoveActorsAwayExcept(actor, smart, firstVictim);
                smart.Position = new Vector3(20f, 0.5f, 20f);
                firstVictim.Position = smart.Position + new Vector3(1.2f, 0f, 0f);
                smartState.NextBuildRethinkTime = 999f;
                smartState.NextDecisionTime = 0f;

                economy.AddCoins(firstVictim, 80);
                economy.AddInk(firstVictim, 2);
                context.Assert(actor.ApplyDamage(firstVictim, firstVictim.Health + 1f, smart, "DiagnosticAIDeathChestKill"), "AI loot victim should die.");
                context.AssertEqual(1, economy.PendingDeathChestCount, "ai.loot.pendingBeforeSmart");

                var smartInventoryBefore = economy.CaptureInventory(smart);
                ai.Tick(0.1f);
                var smartInventoryAfter = economy.CaptureInventory(smart);
                context.AssertEqual(0, economy.PendingDeathChestCount, "ai.loot.pendingAfterSmart");
                context.AssertEqual(TotemAIState.Loot, smartState.State, "ai.loot.smartState");
                context.AssertEqual(1, smartState.DeathChestLoots, "ai.loot.smartLootCount");
                context.AssertEqual(smartInventoryBefore.coins + 40, smartInventoryAfter.coins, "ai.loot.smartCoins");
                context.AssertEqual(smartInventoryBefore.inkBottleCount + 1, smartInventoryAfter.inkBottleCount, "ai.loot.smartInk");
                context.AssertEqual(1, ai.CaptureSnapshot().totalDeathChestLoots, "ai.loot.snapshotTotal");

                var lowGreedState = ai.States.Where(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).First();
                var secondVictim = ai.States.Where(state => state.Actor.Kind == TotemActorKind.LightAi && state.Actor.IsAlive).Skip(1).First().Actor;
                MoveActorsAwayExcept(actor, lowGreedState.Actor, secondVictim);
                lowGreedState.Actor.Position = new Vector3(40f, 0.5f, 40f);
                secondVictim.Position = lowGreedState.Actor.Position + new Vector3(5f, 0f, 0f);
                lowGreedState.NextDecisionTime = 0f;
                economy.AddCoins(secondVictim, 80);
                context.Assert(actor.ApplyDamage(secondVictim, secondVictim.Health + 1f, lowGreedState.Actor, "DiagnosticLowGreedChest"), "Low-greed loot victim should die.");
                context.AssertEqual(TotemAIService.DeathChestLootRadius, TotemAIService.GetDeathChestSearchRadius(lowGreedState, 14f), "ai.loot.lowGreedSearchRadius");

                ai.Tick(0.1f);
                context.AssertEqual(1, economy.PendingDeathChestCount, "ai.loot.lowGreedLeavesFarChest");
                context.Assert(lowGreedState.State != TotemAIState.Loot, "Low-greed Light AI should not chase a far death chest.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void MoveActorsAwayExcept(TotemActorService actorService, params TotemActorModel[] keepActors)
        {
            var actors = actorService.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || ContainsActor(keepActors, actor))
                {
                    continue;
                }

                actor.Position = new Vector3(1000f + i * 3f, 0.5f, 1000f);
            }
        }

        private static void SuppressRegularAiDecisions(TotemAIService ai)
        {
            if (ai == null)
            {
                return;
            }

            for (int i = 0; i < ai.States.Count; i++)
            {
                ai.States[i].NextDecisionTime = 999999f;
            }
        }

        private static bool ContainsActor(TotemActorModel[] actors, TotemActorModel target)
        {
            if (actors == null)
            {
                return false;
            }

            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckActorDamageEvent(GFDiagnosticScenarioContext context)
        {
            var service = new TotemActorService();
            var target = new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = 2,
                Name = "DamageProbe",
                Kind = TotemActorKind.SmartAi,
                Position = Vector3.zero,
                MaxHealth = 10f,
            });

            int eventCount = 0;
            bool killed = false;
            service.DamageApplied += (actor, amount, isKilled) =>
            {
                if (actor == target)
                {
                    eventCount++;
                    killed = isKilled;
                }
            };

            bool result = service.ApplyDamage(target, 12f);
            context.Assert(result, "ActorService.ApplyDamage should return killed=true when HP reaches zero.");
            context.AssertEqual(1, eventCount, "ai.damageEvent.count");
            context.Assert(killed, "DamageApplied event should include killed=true.");
        }

        private static TotemEnemyModel SpawnEnemy(
            GFDiagnosticScenarioContext context,
            TotemEnemyService enemies,
            int combatantId,
            string enemyId,
            Vector3 position,
            string anchorId)
        {
            context.Assert(enemies != null, "AI diagnostic requires EnemyService.");
            if (enemies == null)
            {
                return null;
            }

            bool spawned = enemies.TrySpawn(
                new TotemEnemySpawnRequest(combatantId, enemyId, position, 1, anchorId, 0f),
                out var enemy,
                out var reason);
            context.Assert(spawned, $"EnemyService should spawn {enemyId}: {reason}");
            return enemy;
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static void AssertPersonalityCount(GFDiagnosticScenarioContext context, TotemBotProfileDefinition[] profiles, TotemAIPersonality personality, int expected, string name)
        {
            int actual = profiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == personality);
            context.Detail($"{name}.actual", actual);
            context.AssertEqual(expected, actual, name);
        }
    }
}
#endif
