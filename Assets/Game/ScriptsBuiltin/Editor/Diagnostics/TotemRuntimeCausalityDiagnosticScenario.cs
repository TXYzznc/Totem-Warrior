#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemRuntimeCausalityDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Runtime Causality Smoke";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var timeline = new List<string>(16);
            var runtimeObject = new GameObject("[TotemCausalityDiagnosticRuntime]");
            TotemGameRuntime runtime = null;

            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.StartRuntime();

                var runtimeSnapshot = runtime.CaptureSnapshot();
                context.Assert(runtimeSnapshot.servicesReady, "Causality smoke requires all default runtime services ready.");
                context.AssertEqual(runtimeSnapshot.serviceCount, runtimeSnapshot.readyServiceCount, "causality.runtime.readyServiceCount");
                AddStep(timeline, $"Runtime ready: services={runtimeSnapshot.readyServiceCount}/{runtimeSnapshot.serviceCount}");

                var flow = RequireService<TotemGameFlowService>(context, runtime, "GameFlow");
                var input = RequireService<TotemInputService>(context, runtime, "Input");
                var data = RequireService<TotemDataService>(context, runtime, "Data");
                var map = RequireService<TotemMapService>(context, runtime, "Map");
                var actor = RequireService<TotemActorService>(context, runtime, "Actor");
                var economy = RequireService<TotemEconomyService>(context, runtime, "Economy");
                var status = RequireService<TotemStatusService>(context, runtime, "Status");
                var tattoo = RequireService<TotemTattooService>(context, runtime, "Tattoo");
                var weapon = RequireService<TotemWeaponService>(context, runtime, "Weapon");
                var skill = RequireService<TotemSkillService>(context, runtime, "Skill");
                var combat = RequireService<TotemCombatService>(context, runtime, "Combat");
                var chest = RequireService<TotemChestService>(context, runtime, "Chest");
                var npc = RequireService<TotemNpcService>(context, runtime, "Npc");
                var zone = RequireService<TotemZoneService>(context, runtime, "Zone");
                var boss = RequireService<TotemBossService>(context, runtime, "Boss");
                var ai = RequireService<TotemAIService>(context, runtime, "AI");
                var interaction = RequireService<TotemInteractionService>(context, runtime, "Interaction");
                var vfx = RequireService<TotemVfxService>(context, runtime, "VFX");
                var audio = RequireService<TotemAudioService>(context, runtime, "Audio");

                context.Assert(data.GameplayCatalogLoadedFromFile && !data.GameplayCatalogUsingFallback, $"Causality smoke must use the external Business catalog: {data.GameplayCatalogMessage}");
                AddStep(timeline, $"Catalog loaded: source={data.GameplayCatalog?.source}, hash={data.GameplayCatalogContentHash}");

                var provider = new CausalityInputProvider { UnscaledTime = 10f };
                input.SetInputProvider(provider);

                flow.EnterMainMenu();
                flow.EnterCharacterSelect();
                flow.SelectCharacter(2);
                flow.EnterStartupSelect();
                flow.ConfirmStartup(2, "knife_basic", new[] { 1, 2 });

                context.AssertEqual(TotemGameFlowState.CombatHud, flow.CurrentState, "causality.flow.state");
                var player = actor.Player;
                context.Assert(player != null, "Causality smoke should spawn a player.");
                context.AssertEqual(50, actor.CaptureActorSnapshot().actorCount, "causality.actor.countWithoutBoss");
                context.AssertEqual(1, actor.CaptureActorSnapshot().bossCount, "causality.actor.bossCount");
                AddStep(timeline, $"Startup confirmed: player={player?.Name}, weapon=knife_basic, actors=50, boss=1");

                var target = actor.Actors.FirstOrDefault(item => item.Kind == TotemActorKind.LightAi && item.IsAlive);
                context.Assert(target != null, "Causality smoke requires a live Light AI target.");
                MoveEnemiesAway(actor, player);
                target.Position = player.Position + new Vector3(0f, 0f, 0.8f);
                economy.AddCoins(target, 80);
                actor.SetCombatElapsedSecondsForDiagnostics(TotemActorService.ParticipantDamageProtectionSeconds + 0.1f);

                provider.ClearAll();
                provider.SetMouse(0, held: true, down: true);
                input.Tick(0.05f);
                combat.Tick(0.05f);
                var attackSnapshot = combat.CaptureCombatSnapshot();
                context.AssertEqual("Attack", attackSnapshot.lastAction, "causality.combat.attack.action");
                context.AssertEqual(target.ActorId, attackSnapshot.lastTargetActorId, "causality.combat.attack.target");
                context.Assert(attackSnapshot.lastDamage > 0f, "Causality attack should apply damage.");
                AddStep(timeline, $"Attack resolved: target={target.Name}, damage={attackSnapshot.lastDamage:0.##}, hp={target.Health:0.##}");

                provider.ClearAll();
                provider.Press(KeyCode.E);
                input.Tick(0.05f);
                combat.Tick(0.05f);
                var skillSnapshot = combat.CaptureCombatSnapshot();
                context.AssertEqual("Skill", skillSnapshot.lastAction, "causality.combat.skill.action");
                context.AssertEqual("skill_fireball_01", skillSnapshot.lastSkillId, "causality.combat.skill.id");
                context.Assert(!target.IsAlive, "Fireball should finish the prepared Light AI target in the causality smoke.");
                context.AssertEqual(1, economy.PendingDeathChestCount, "causality.economy.deathChest.pendingAfterKill");
                AddStep(timeline, $"Skill resolved: skill={skillSnapshot.lastSkillId}, damage={skillSnapshot.lastDamage:0.##}, killed={target.Name}, pendingDeathChests={economy.PendingDeathChestCount}");

                context.Assert(economy.TryLootDeathChest(player, target, out var deathChest), "Player should loot the generated death chest.");
                var playerInventoryAfterLoot = economy.CaptureInventory(player);
                context.Assert(playerInventoryAfterLoot.coins >= deathChest.coins, "Death chest loot should add coins to the player inventory.");
                AddStep(timeline, $"Death chest looted: coins={deathChest.coins}, playerCoins={playerInventoryAfterLoot.coins}");

                var commonChest = chest.ActiveChests.FirstOrDefault(item => item.ChestId == "chest_common" && !item.Opened);
                context.Assert(commonChest != null, "Causality smoke requires a common chest spawned by CombatHud.");
                context.Assert(chest.TryOpenChest(player, commonChest, 50, out var chestResult), $"Common chest should open: {chestResult.reason}");
                context.AssertEqual(TotemChestRewardType.Gold, chestResult.rewardType, "causality.chest.rewardType");
                AddStep(timeline, $"Chest opened: chest={chestResult.chestId}, reward={chestResult.rewardType}, coins={chestResult.coinsAdded}");

                var merchant = npc.Npcs.FirstOrDefault(item => item.Type == TotemNpcType.Merchant && item.ShopStockTable == "general_shop");
                context.Assert(merchant != null, "Causality smoke requires the general merchant.");
                economy.AddCoins(player, 500);
                context.Assert(npc.TryPurchase(player, merchant, 101, out var purchase), $"Shop purchase should succeed: {purchase.reason}");
                context.AssertEqual(TotemShopRewardType.Ink, purchase.rewardType, "causality.shop.rewardType");
                AddStep(timeline, $"Shop purchase: merchant={merchant.NpcId}, item={purchase.itemId}, price={purchase.actualPrice}, reward={purchase.rewardType}");

                float healthBeforeZone = player.Health;
                player.Position = Vector3.zero;
                zone.Tick(1f);
                var zoneSnapshot = zone.CaptureSnapshot();
                context.Assert(zoneSnapshot.outZoneAffectedActorCount > 0, "Shrink zone should affect at least one actor after moving player out of bounds.");
                context.Assert(player.Health < healthBeforeZone, "Shrink zone should damage the out-of-zone player.");
                AddStep(timeline, $"Zone tick: phase={zoneSnapshot.currentPhaseId}, affected={zoneSnapshot.outZoneAffectedActorCount}, playerHp={player.Health:0.##}");

                var bossActor = actor.Boss;
                context.Assert(bossActor != null && bossActor.IsAlive, "Causality smoke requires an active boss.");
                actor.ApplyDamage(bossActor, bossActor.MaxHealth * 0.4f, player, "CausalityBossPhaseDrop");
                boss.Tick(0.1f);
                var bossSnapshot = boss.CaptureSnapshot();
                context.Assert(bossSnapshot.currentPhase >= 2, "Boss should enter phase 2 after the diagnostic HP drop.");
                AddStep(timeline, $"Boss phase: phase={bossSnapshot.currentPhase}, hpRatio={bossSnapshot.hpRatio:0.##}, enrage={bossSnapshot.enrageMultiplier:0.##}");

                int decisionsBefore = ai.CaptureSnapshot().totalDecisions;
                var smartState = ai.States.FirstOrDefault(state => state.Actor != null && state.Actor.Kind == TotemActorKind.SmartAi && state.Actor.IsAlive);
                context.Assert(smartState != null, "Causality smoke requires a live Smart AI state.");
                if (smartState != null)
                {
                    SuppressAiDecisionsExcept(ai, smartState);
                    smartState.Actor.Position = player.Position + new Vector3(0f, 0f, 1.2f);
                    smartState.NextDecisionTime = 0f;
                    smartState.AttackCooldownRemaining = 0f;
                    smartState.SkillCooldownRemaining = 999f;
                    smartState.SafetyScore = 1f;
                    ai.Tick(0.1f);
                }

                var aiSnapshot = ai.CaptureSnapshot();
                context.Assert(aiSnapshot.totalDecisions > decisionsBefore, "Smart AI should record a decision in the causality smoke.");
                AddStep(timeline, $"AI decision: action={aiSnapshot.lastDecisionAction}, reason={aiSnapshot.lastDecisionReason}, target={aiSnapshot.lastDecisionTargetKind}, total={aiSnapshot.totalDecisions}");

                flow.EnterMainMenu();
                context.AssertEqual(0, actor.CaptureActorSnapshot().actorCount, "causality.cleanup.actorCount");
                context.AssertEqual(0, chest.CaptureSnapshot().activeChestCount, "causality.cleanup.chestCount");
                context.AssertEqual(0, npc.CaptureSnapshot().npcCount, "causality.cleanup.npcCount");
                AddStep(timeline, "Cleanup: CombatHud left, actors/chests/npcs cleared");

                RunPlayableCombatLoop(
                    context,
                    timeline,
                    flow,
                    input,
                    provider,
                    map,
                    actor,
                    economy,
                    status,
                    tattoo,
                    weapon,
                    skill,
                    zone,
                    boss,
                    ai,
                    interaction,
                    vfx,
                    audio,
                    combat);

                WriteTimeline(context, timeline);
                context.Pass("Totem runtime causality smoke is ready.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RunPlayableCombatLoop(
            GFDiagnosticScenarioContext context,
            List<string> timeline,
            TotemGameFlowService flow,
            TotemInputService input,
            CausalityInputProvider provider,
            TotemMapService map,
            TotemActorService actor,
            TotemEconomyService economy,
            TotemStatusService status,
            TotemTattooService tattoo,
            TotemWeaponService weapon,
            TotemSkillService skill,
            TotemZoneService zone,
            TotemBossService boss,
            TotemAIService ai,
            TotemInteractionService interaction,
            TotemVfxService vfx,
            TotemAudioService audio,
            TotemCombatService combat)
        {
            flow.EnterMainMenu();
            flow.EnterCharacterSelect();
            flow.SelectCharacter(1);
            flow.EnterStartupSelect();
            flow.ConfirmStartup(1, "hammer_heavy", new[] { 1, 2 });

            var player = actor.Player;
            context.Assert(player != null && player.IsAlive, "Playable combat loop requires a live player.");
            context.AssertEqual(TotemGameFlowState.CombatHud, flow.CurrentState, "playableLoop.flow.state");
            context.Assert(map.CurrentMap != null, "Playable combat loop requires an active runtime map.");
            context.Assert(combat.CaptureCombatSnapshot().active, "Playable combat loop requires active combat.");
            actor.SetCombatElapsedSecondsForDiagnostics(TotemActorService.ParticipantDamageProtectionSeconds + 0.1f);

            MoveEnemiesAway(actor, player);
            KeyCode moveKey = FindWalkableMoveKey(map.CurrentMap, player.Position, out _);
            Vector3 loopStart = player.Position;
            provider.ClearAll();
            provider.SetKey(moveKey, held: true);
            for (int i = 0; i < 10; i++)
            {
                TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            }

            provider.ClearAll();
            TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            float movedDistance = FlatDistance(loopStart, player.Position);
            context.Detail("playableLoop.move.key", moveKey.ToString());
            context.Detail("playableLoop.move.distance", movedDistance.ToString("F2"));
            context.Assert(movedDistance > 1.5f, "Playable combat loop should move the player through TotemInputService.");

            Vector3 beforeDodge = player.Position;
            provider.ClearAll();
            provider.SetKey(moveKey, held: true);
            provider.Press(KeyCode.Space);
            TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            provider.ClearAll();
            TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            float dodgeDistance = FlatDistance(beforeDodge, player.Position);
            context.Detail("playableLoop.dodge.distance", dodgeDistance.ToString("F2"));
            context.AssertEqual("Dodge", combat.CaptureCombatSnapshot().lastAction, "playableLoop.dodge.lastAction");
            context.Assert(dodgeDistance > 2f, "Playable combat loop should apply a meaningful dodge displacement.");

            var target = actor.Actors.FirstOrDefault(item => item.Kind == TotemActorKind.LightAi && item.IsAlive);
            context.Assert(target != null, "Playable combat loop requires a live Light AI target.");
            SetActorPosition(target, player.Position + Vector3.forward * 1.2f);
            economy.AddCoins(target, 80);
            int killsBefore = combat.CaptureCombatSnapshot().killCount;
            float targetStartHealth = target.Health;
            for (int attempt = 0; attempt < 8 && target.IsAlive; attempt++)
            {
                provider.ClearAll();
                provider.SetMouse(0, held: true, down: true);
                TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
                provider.ClearAll();
                TickGameplayFrame(0.4f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);

                if (!target.IsAlive)
                {
                    break;
                }

                provider.Press(KeyCode.E);
                TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
                provider.ClearAll();
                for (int wait = 0; wait < 10 && target.IsAlive; wait++)
                {
                    TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
                }
            }

            var afterKillCombat = combat.CaptureCombatSnapshot();
            int killDelta = afterKillCombat.killCount - killsBefore;
            context.Detail("playableLoop.target.startHealth", targetStartHealth.ToString("F1"));
            context.Detail("playableLoop.kill.delta", killDelta.ToString());
            context.Detail("playableLoop.combat.lastActionAfterKill", afterKillCombat.lastAction);
            context.Assert(killDelta >= 1, "Playable combat loop should kill at least one nearby enemy through player combat input.");
            context.Assert(economy.PendingDeathChestCount >= 1, "Playable combat loop should create a death chest after the scripted kill.");

            var pressureState = ai.States.FirstOrDefault(state =>
                state.Actor != null &&
                state.Actor.Kind == TotemActorKind.SmartAi &&
                state.Actor.IsAlive);
            context.Assert(pressureState != null, "Playable combat loop requires a live Smart AI pressure actor.");
            SuppressAiDecisionsExcept(ai, pressureState);
            SetActorPosition(pressureState.Actor, player.Position + Vector3.forward * 2.2f);
            pressureState.NextDecisionTime = 0f;
            pressureState.AttackCooldownRemaining = 0f;
            pressureState.SkillCooldownRemaining = 999f;
            pressureState.SafetyScore = 1f;

            var aiBeforePressure = ai.CaptureSnapshot();
            float healthBeforePressure = player.Health;
            for (int i = 0; i < 16; i++)
            {
                provider.ClearAll();
                TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            }

            var aiAfterPressure = ai.CaptureSnapshot();
            context.Detail("playableLoop.ai.decisionsDelta", (aiAfterPressure.totalDecisions - aiBeforePressure.totalDecisions).ToString());
            context.Detail("playableLoop.ai.attacksDelta", (aiAfterPressure.totalAttacks - aiBeforePressure.totalAttacks).ToString());
            context.Detail("playableLoop.player.healthBeforePressure", healthBeforePressure.ToString("F1"));
            context.Detail("playableLoop.player.healthAfterPressure", player.Health.ToString("F1"));
            context.Assert(aiAfterPressure.totalDecisions > aiBeforePressure.totalDecisions, "Playable combat loop should record Smart AI decisions under pressure.");
            context.Assert(aiAfterPressure.totalAttacks > aiBeforePressure.totalAttacks || player.Health < healthBeforePressure, "Playable combat loop should apply observable AI pressure.");
            context.Assert(player.IsAlive && player.Health > player.MaxHealth * 0.25f, "Playable combat loop should keep the player alive after short AI pressure.");

            SetActorPosition(pressureState.Actor, player.Position + new Vector3(120f, 0f, 120f));
            pressureState.NextDecisionTime = 999999f;
            pressureState.AttackCooldownRemaining = 999f;
            pressureState.SkillCooldownRemaining = 999f;
            for (int i = 0; i < 24; i++)
            {
                provider.ClearAll();
                TickGameplayFrame(0.1f, provider, input, actor, status, tattoo, weapon, skill, zone, boss, ai, interaction, vfx, audio, combat);
            }

            var finalCombat = combat.CaptureCombatSnapshot();
            var finalActors = actor.CaptureActorSnapshot();
            var zoneSnapshot = zone.CaptureSnapshot();
            context.Detail("playableLoop.elapsedSec", finalCombat.elapsedSec.ToString("F1"));
            context.Detail("playableLoop.final.playerHealth", finalCombat.playerHealth.ToString("F1"));
            context.Detail("playableLoop.final.aliveEnemyCount", finalCombat.aliveEnemyCount.ToString());
            context.Detail("playableLoop.zone.radius", zoneSnapshot.currentRadius.ToString("F1"));
            context.Assert(finalCombat.elapsedSec >= 5f, "Playable combat loop should run for a multi-second combat slice.");
            context.Assert(finalCombat.active, "Playable combat loop should remain active instead of ending prematurely.");
            context.Assert(player.IsAlive, "Playable combat loop player should survive the baseline slice.");
            context.Assert(finalCombat.playerHealth > player.MaxHealth * 0.25f, "Playable combat loop should remain recoverably playable after the pressure slice.");
            context.Assert(finalActors.aliveEnemyCount > 0, "Playable combat loop should leave enough enemies alive for continued play.");

            flow.EnterMainMenu();
            context.AssertEqual(0, actor.CaptureActorSnapshot().actorCount, "playableLoop.cleanup.actorCount");
            AddStep(timeline, $"Playable loop: moved={movedDistance:0.##}, dodge={dodgeDistance:0.##}, kills={killDelta}, aiAttacks={aiAfterPressure.totalAttacks - aiBeforePressure.totalAttacks}, playerHp={player.Health:0.##}, elapsed={finalCombat.elapsedSec:0.##}");
        }

        private static void TickGameplayFrame(
            float deltaTime,
            CausalityInputProvider provider,
            TotemInputService input,
            TotemActorService actor,
            TotemStatusService status,
            TotemTattooService tattoo,
            TotemWeaponService weapon,
            TotemSkillService skill,
            TotemZoneService zone,
            TotemBossService boss,
            TotemAIService ai,
            TotemInteractionService interaction,
            TotemVfxService vfx,
            TotemAudioService audio,
            TotemCombatService combat)
        {
            provider.UnscaledTime += Mathf.Max(0f, deltaTime);
            input.Tick(deltaTime);
            actor.Tick(deltaTime);
            status.Tick(deltaTime);
            tattoo.Tick(deltaTime);
            weapon.Tick(deltaTime);
            skill.Tick(deltaTime);
            zone.Tick(deltaTime);
            boss.Tick(deltaTime);
            ai.Tick(deltaTime);
            interaction.Tick(deltaTime);
            vfx.Tick(deltaTime);
            audio.Tick(deltaTime);
            combat.Tick(deltaTime);
            provider.ClearTransient();
        }

        private static KeyCode FindWalkableMoveKey(TotemMapSnapshot map, Vector3 origin, out Vector3 direction)
        {
            if (IsWalkable(map, origin + Vector3.right * 6f))
            {
                direction = Vector3.right;
                return KeyCode.D;
            }

            if (IsWalkable(map, origin + Vector3.forward * 6f))
            {
                direction = Vector3.forward;
                return KeyCode.W;
            }

            if (IsWalkable(map, origin + Vector3.left * 6f))
            {
                direction = Vector3.left;
                return KeyCode.A;
            }

            direction = Vector3.back;
            return KeyCode.S;
        }

        private static bool IsWalkable(TotemMapSnapshot map, Vector3 position)
        {
            return map == null || TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, position));
        }

        private static void SetActorPosition(TotemActorModel actor, Vector3 position)
        {
            if (actor == null)
            {
                return;
            }

            actor.Position = position;
            if (actor.GameObject != null)
            {
                actor.GameObject.transform.position = position;
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static TService RequireService<TService>(GFDiagnosticScenarioContext context, TotemGameRuntime runtime, string serviceName)
            where TService : class, ITotemRuntimeService
        {
            var service = runtime.GetService<TService>();
            context.Assert(service != null, $"Causality smoke requires service: {serviceName}");
            return service;
        }

        private static void MoveEnemiesAway(TotemActorService actor, TotemActorModel player)
        {
            int index = 0;
            foreach (var enemy in actor.Actors.Where(TotemActorService.IsEnemy))
            {
                enemy.Position = player.Position + new Vector3(25f + index, 0f, 25f + index);
                index++;
            }
        }

        private static void SuppressAiDecisionsExcept(TotemAIService ai, TotemAIActorState allowed)
        {
            for (int i = 0; i < ai.States.Count; i++)
            {
                var state = ai.States[i];
                if (ReferenceEquals(state, allowed))
                {
                    continue;
                }

                state.NextDecisionTime = 999999f;
                state.AttackCooldownRemaining = 999f;
                state.SkillCooldownRemaining = 999f;
            }
        }

        private static void AddStep(List<string> timeline, string message)
        {
            timeline.Add(message);
        }

        private static void WriteTimeline(GFDiagnosticScenarioContext context, List<string> timeline)
        {
            context.Detail("causality.timeline.count", timeline.Count);
            for (int i = 0; i < timeline.Count; i++)
            {
                context.Detail($"causality.timeline.{i:00}", timeline[i]);
            }
        }

        private sealed class CausalityInputProvider : ITotemInputProvider
        {
            private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
            private readonly HashSet<KeyCode> downKeys = new HashSet<KeyCode>();
            private readonly bool[] mouseHeld = new bool[3];
            private readonly bool[] mouseDown = new bool[3];

            public float UnscaledTime { get; set; }

            public Vector3 MousePosition { get; set; } = new Vector3(float.NaN, float.NaN, float.NaN);

            public bool GetKey(KeyCode keyCode)
            {
                return heldKeys.Contains(keyCode);
            }

            public bool GetKeyDown(KeyCode keyCode)
            {
                return downKeys.Contains(keyCode);
            }

            public bool GetMouseButton(int button)
            {
                return button >= 0 && button < mouseHeld.Length && mouseHeld[button];
            }

            public bool GetMouseButtonDown(int button)
            {
                return button >= 0 && button < mouseDown.Length && mouseDown[button];
            }

            public void Press(params KeyCode[] keys)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    downKeys.Add(keys[i]);
                }
            }

            public void SetKey(KeyCode key, bool held, bool down = false)
            {
                if (held)
                {
                    heldKeys.Add(key);
                }
                else
                {
                    heldKeys.Remove(key);
                }

                if (down)
                {
                    downKeys.Add(key);
                }
            }

            public void SetMouse(int button, bool held, bool down)
            {
                if (button < 0 || button >= mouseHeld.Length)
                {
                    return;
                }

                mouseHeld[button] = held;
                mouseDown[button] = down;
            }

            public void ClearTransient()
            {
                downKeys.Clear();
                for (int i = 0; i < mouseDown.Length; i++)
                {
                    mouseDown[i] = false;
                }
            }

            public void ClearAll()
            {
                heldKeys.Clear();
                downKeys.Clear();
                for (int i = 0; i < mouseHeld.Length; i++)
                {
                    mouseHeld[i] = false;
                    mouseDown[i] = false;
                }
            }
        }
    }
}
#endif
