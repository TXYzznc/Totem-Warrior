#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemEnemyPureLogicDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Enemy Pure Logic";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckFsmTransitions(context);
            CheckThreatCapacityAndIdentityParity(context);
            CheckRecentAttackerAndTargetHysteresis(context);
            CheckAbilityTimelines(context);
            CheckInterruptibleRegeneration(context);
            CheckSummonActiveCap(context);
            CheckBuiltInDefinitionBindings(context);
            CheckLodAndPathBudget(context);
            CheckSteadyCombatTickAllocations(context);
            context.Pass("Enemy FSM, threat, abilities, interruption, summon caps, catalog bindings, LOD, path budget and steady combat Tick allocation expose deterministic evidence.");
        }

        private static void CheckFsmTransitions(GFDiagnosticScenarioContext context)
        {
            var observer = new RecordingObserver();
            DiagnosticController controller = CreateController(1001, CreateControllerDefinition("fsm_probe"), observer);

            context.AssertEqual(TotemEnemyState.Dormant, controller.State, "enemyLogic.fsm.initial");
            context.Assert(controller.TryDiagnosticTransition(TotemEnemyState.Spawn, "LegalDormantSpawn", 0f), "Dormant -> Spawn must be legal.");

            int beforeIllegal = observer.StateChanges.Count;
            context.Assert(!controller.TryDiagnosticTransition(TotemEnemyState.Chase, "IllegalSpawnChase", 0.1f), "Spawn -> Chase must be rejected.");
            context.AssertEqual(TotemEnemyState.Spawn, controller.State, "enemyLogic.fsm.illegalPreservesState");
            context.AssertEqual(beforeIllegal, observer.StateChanges.Count, "enemyLogic.fsm.illegalEmitsNoEvent");

            context.Assert(controller.TryDiagnosticTransition(TotemEnemyState.Patrol, "LegalSpawnPatrol", 0.2f), "Spawn -> Patrol must be legal.");
            context.Assert(!controller.TryDiagnosticTransition(TotemEnemyState.Recover, "IllegalPatrolRecover", 0.3f), "Patrol -> Recover must be rejected.");
            context.Assert(controller.TryDiagnosticTransition(TotemEnemyState.Alert, "LegalPatrolAlert", 0.4f), "Patrol -> Alert must be legal.");
            context.Assert(controller.TryDiagnosticTransition(TotemEnemyState.Chase, "LegalAlertChase", 0.5f), "Alert -> Chase must be legal.");
            context.Assert(!controller.TryDiagnosticTransition(TotemEnemyState.Spawn, "IllegalChaseSpawn", 0.6f), "Chase -> Spawn must be rejected.");
            context.Assert(controller.MarkDead(0.7f, "DiagnosticDeath", null), "Any live non-Dormant state must accept Dead.");
            context.Assert(!controller.TryDiagnosticTransition(TotemEnemyState.Patrol, "IllegalDeadPatrol", 0.8f), "Dead must be terminal.");
            context.AssertEqual(TotemEnemyState.Dead, controller.State, "enemyLogic.fsm.terminalState");
            context.AssertEqual(5, observer.StateChanges.Count, "enemyLogic.fsm.legalEventCount");
            context.Detail("enemyLogic.fsm.evidence", string.Join(",", observer.StateChanges.Select(item => item.Previous + ">" + item.Current)));
        }

        private static void CheckThreatCapacityAndIdentityParity(GFDiagnosticScenarioContext context)
        {
            TotemActorModel human = CreateActor(1101, TotemParticipantControllerKind.Human, Vector3.zero);
            TotemActorModel smart = CreateActor(1102, TotemParticipantControllerKind.SmartBot, Vector3.zero);
            TotemActorModel light = CreateActor(1103, TotemParticipantControllerKind.LightBot, Vector3.zero);

            var fixedCapacity = new TotemEnemyThreatTable(2);
            fixedCapacity.AddAlert(human, 10f, 0f);
            fixedCapacity.AddAlert(smart, 20f, 0f);
            fixedCapacity.AddAlert(light, 1f, 0f);
            context.AssertEqual(2, fixedCapacity.Capacity, "enemyLogic.threat.capacity");
            context.AssertEqual(2, fixedCapacity.Count, "enemyLogic.threat.countAfterOverflow");
            AssertNear(context, 0f, fixedCapacity.GetScore(human, 0f, 0f), "enemyLogic.threat.evictedLowest");
            AssertNear(context, 20f, fixedCapacity.GetScore(smart, 0f, 0f), "enemyLogic.threat.retainedHighest");
            AssertNear(context, 1f, fixedCapacity.GetScore(light, 0f, 0f), "enemyLogic.threat.insertedOverflowEntry");

            var identityParity = new TotemEnemyThreatTable(3);
            identityParity.AddAlert(human, 10f, 1f);
            identityParity.AddAlert(smart, 10f, 1f);
            identityParity.AddAlert(light, 10f, 1f);
            float humanScore = identityParity.GetScore(human, 4f, 1f);
            float smartScore = identityParity.GetScore(smart, 4f, 1f);
            float lightScore = identityParity.GetScore(light, 4f, 1f);
            context.AssertEqual(TotemParticipantControllerKind.Human, human.ControllerKind, "enemyLogic.identity.humanKind");
            context.AssertEqual(TotemParticipantControllerKind.SmartBot, smart.ControllerKind, "enemyLogic.identity.smartKind");
            context.AssertEqual(TotemParticipantControllerKind.LightBot, light.ControllerKind, "enemyLogic.identity.lightKind");
            AssertNear(context, humanScore, smartScore, "enemyLogic.identity.humanSmartParity");
            AssertNear(context, humanScore, lightScore, "enemyLogic.identity.humanLightParity");
            context.Detail("enemyLogic.identity.equalThreatScore", humanScore);
        }

        private static void CheckRecentAttackerAndTargetHysteresis(GFDiagnosticScenarioContext context)
        {
            TotemActorModel recentAttacker = CreateActor(1201, TotemParticipantControllerKind.Human, Vector3.zero);
            var recentThreat = new TotemEnemyThreatTable(1);
            recentThreat.AddDamage(recentAttacker, 10f, 100f);
            AssertNear(context, 20f, recentThreat.GetScore(recentAttacker, 0f, 102.99f), "enemyLogic.threat.recentScore");
            AssertNear(context, 10f, recentThreat.GetScore(recentAttacker, 0f, 103.01f), "enemyLogic.threat.expiredRecentScore");
            AssertNear(context, 3f, TotemEnemyThreatTable.RecentAttackerDuration, "enemyLogic.threat.recentDuration");

            TotemActorModel current = CreateActor(1202, TotemParticipantControllerKind.Human, Vector3.zero);
            TotemActorModel challenger = CreateActor(1203, TotemParticipantControllerKind.SmartBot, Vector3.zero);
            var participants = new ParticipantSource(current, challenger);
            var host = new RecordingHost { AllowMovement = false };
            DiagnosticController controller = CreateController(1204, CreateControllerDefinition("hysteresis_probe"), null);
            controller.EnterChase(0f);
            controller.SetDiagnosticTarget(current, "DiagnosticCurrent", 0f);
            controller.Threat.AddAlert(current, 10f, 0f);
            controller.Threat.AddAlert(challenger, 14.99f, 0f);

            controller.Tick(0.02f, 1f, participants, host, null, null);
            context.Assert(ReferenceEquals(current, controller.Target), "A challenger below 1.25x current score must not replace the current target.");

            controller.Threat.AddAlert(challenger, 0.02f, 1.01f);
            controller.Tick(0.02f, 1.02f, participants, host, null, null);
            context.Assert(ReferenceEquals(challenger, controller.Target), "A challenger above 1.25x current score must replace the current target.");
            AssertNear(context, 1.25f, TotemEnemyThreatTable.TargetSwitchMultiplier, "enemyLogic.threat.switchMultiplier");
            context.Detail("enemyLogic.threat.hysteresisEvidence", "current=20,held=24.99,switched=25.01");
        }

        private static void CheckAbilityTimelines(GFDiagnosticScenarioContext context)
        {
            int testedCount = 0;
            var concreteTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (TotemEnemyAbilityType abilityType in Enum.GetValues(typeof(TotemEnemyAbilityType)))
            {
                if (abilityType == TotemEnemyAbilityType.Unknown)
                {
                    continue;
                }

                testedCount++;
                TotemEnemyAbilityRuntimeDefinition abilityDefinition = CreateAbilityDefinition(abilityType);
                ITotemEnemyAbility ability = TotemEnemyAbilityFactory.Create(abilityDefinition);
                context.Assert(ability != null, "Ability factory must bind " + abilityType + ".");
                if (ability == null)
                {
                    continue;
                }

                concreteTypes.Add(ability.GetType().Name);
                if (abilityType == TotemEnemyAbilityType.DeathBurst)
                {
                    CheckDeathBurstTimeline(context, ability);
                    continue;
                }

                if (abilityType == TotemEnemyAbilityType.PhaseTransition)
                {
                    CheckPhaseTransitionTimeline(context, ability);
                    continue;
                }

                CheckStandardAbilityTimeline(context, abilityType, ability);
            }

            context.AssertEqual(13, testedCount, "enemyLogic.abilities.typeCount");
            context.AssertEqual(13, concreteTypes.Count, "enemyLogic.abilities.concreteTypeCount");
        }

        private static void CheckStandardAbilityTimeline(
            GFDiagnosticScenarioContext context,
            TotemEnemyAbilityType abilityType,
            ITotemEnemyAbility ability)
        {
            TotemEnemyRuntimeDefinition ownerDefinition = CreateControllerDefinition("ability_" + abilityType);
            DiagnosticController controller = CreateController(1300 + (int)abilityType, ownerDefinition, null);
            TotemActorModel target = CreateActor(1400 + (int)abilityType, TotemParticipantControllerKind.Human, Vector3.right);
            var host = new RecordingHost();
            if (abilityType == TotemEnemyAbilityType.Regenerate)
            {
                controller.Enemy.ApplyDamage(20f);
            }

            float healthBefore = controller.Enemy.Health;
            var abilityContext = new TotemEnemyAbilityContext(controller, host, target, 0f);
            context.Assert(ability.CanStart(abilityContext), abilityType + " must be startable in its valid diagnostic context.");
            ability.Begin(abilityContext);
            context.AssertEqual(TotemEnemyAbilityPhase.Windup, ability.Phase, "enemyLogic.abilities." + abilityType + ".windup");
            ability.Tick(abilityContext, 0.11f);
            context.AssertEqual(TotemEnemyAbilityPhase.Active, ability.Phase, "enemyLogic.abilities." + abilityType + ".active");
            ability.Tick(abilityContext, 0.10f);
            context.AssertEqual(TotemEnemyAbilityPhase.Recovery, ability.Phase, "enemyLogic.abilities." + abilityType + ".recovery");
            ability.Tick(abilityContext, 0.10f);
            context.AssertEqual(TotemEnemyAbilityPhase.Complete, ability.Phase, "enemyLogic.abilities." + abilityType + ".complete");
            AssertNear(context, 2f, ability.CooldownRemaining, "enemyLogic.abilities." + abilityType + ".cooldownStart");
            context.Assert(!ability.CanStart(abilityContext), abilityType + " must be gated while cooldown remains.");
            ability.Tick(abilityContext, 0.5f);
            AssertNear(context, 1.5f, ability.CooldownRemaining, "enemyLogic.abilities." + abilityType + ".cooldownTick");
            ability.Tick(abilityContext, 1.5f);
            AssertNear(context, 0f, ability.CooldownRemaining, "enemyLogic.abilities." + abilityType + ".cooldownComplete");
            context.Assert(ability.CanStart(abilityContext), abilityType + " must become startable after cooldown.");
            context.AssertEqual("Windup>Active>Recovery>Complete", host.AbilityTimeline, "enemyLogic.abilities." + abilityType + ".timeline");
            AssertStandardAbilityEffect(context, abilityType, controller, host, healthBefore);
            context.Detail("enemyLogic.abilities." + abilityType + ".signature", ability.GetType().Name + ":" + host.EffectSignature(controller, healthBefore));
        }

        private static void CheckDeathBurstTimeline(GFDiagnosticScenarioContext context, ITotemEnemyAbility ability)
        {
            DiagnosticController controller = CreateController(1350, CreateControllerDefinition("death_burst_probe"), null);
            TotemActorModel target = CreateActor(1450, TotemParticipantControllerKind.Human, Vector3.right);
            var host = new RecordingHost();
            var abilityContext = new TotemEnemyAbilityContext(controller, host, target, 0f);
            context.Assert(!ability.CanStart(abilityContext), "DeathBurst must not enter the selectable timeline.");
            ability.OnOwnerDeath(abilityContext);
            ability.OnOwnerDeath(abilityContext);
            context.AssertEqual(TotemEnemyAbilityPhase.Inactive, ability.Phase, "enemyLogic.abilities.DeathBurst.phase");
            AssertNear(context, 0f, ability.CooldownRemaining, "enemyLogic.abilities.DeathBurst.cooldown");
            context.AssertEqual(1, host.RadiusDamageCount, "enemyLogic.abilities.DeathBurst.radiusCount");
            context.AssertEqual(1, host.HazardCount, "enemyLogic.abilities.DeathBurst.hazardCount");
            context.AssertEqual(1, host.AbilityNotificationCount, "enemyLogic.abilities.DeathBurst.onceOnly");
            context.Detail("enemyLogic.abilities.DeathBurst.timeline", "Inactive>OwnerDeath(one-shot),cooldown=0");
        }

        private static void CheckPhaseTransitionTimeline(GFDiagnosticScenarioContext context, ITotemEnemyAbility ability)
        {
            TotemEnemyRuntimeDefinition definition = TotemEnemyBuiltInCatalog.CreateDefinitions()
                .First(item => item.tier == TotemEnemyTier.Boss);
            var enemy = new TotemEnemyModel(1360, definition.enemyId, definition.displayName, definition.themeId, definition.tier, definition.maxHealth, Vector3.zero);
            var observer = new RecordingObserver();
            var controller = new TotemBossEnemyController(enemy, definition, observer);
            var host = new RecordingHost();
            var abilityContext = new TotemEnemyAbilityContext(controller, host, null, 0f);

            context.Assert(!ability.CanStart(abilityContext), "PhaseTransition must not enter the selectable timeline.");
            controller.Tick(0.01f, 0.5f, null, host, null, null);
            context.AssertEqual(1, controller.BossPhase, "enemyLogic.abilities.PhaseTransition.phase1");
            context.AssertEqual(1, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.phase1EventCount");

            enemy.ApplyDamage(enemy.MaxHealth * 0.45f);
            controller.Tick(0.01f, 1f, null, host, null, null);
            context.AssertEqual(TotemEnemyAbilityPhase.Inactive, ability.Phase, "enemyLogic.abilities.PhaseTransition.phase");
            AssertNear(context, 0f, ability.CooldownRemaining, "enemyLogic.abilities.PhaseTransition.cooldown");
            context.AssertEqual(2, controller.BossPhase, "enemyLogic.abilities.PhaseTransition.bossPhase");
            context.AssertEqual(2, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.eventCount");
            context.AssertEqual(1, observer.BossPhaseChanges[0].CurrentPhase, "enemyLogic.abilities.PhaseTransition.initialPhase");
            context.AssertEqual(2, observer.BossPhaseChanges[1].CurrentPhase, "enemyLogic.abilities.PhaseTransition.thresholdPhase");

            enemy.Heal(enemy.MaxHealth * 0.1f);
            controller.Tick(0.01f, 2f, null, host, null, null);
            context.AssertEqual(2, controller.BossPhase, "enemyLogic.abilities.PhaseTransition.healDoesNotDowngrade");
            context.AssertEqual(2, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.healEmitsNoEvent");

            enemy.ApplyDamage(enemy.MaxHealth * 0.1f);
            controller.Tick(0.01f, 3f, null, host, null, null);
            context.AssertEqual(2, controller.BossPhase, "enemyLogic.abilities.PhaseTransition.samePhaseRetained");
            context.AssertEqual(2, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.samePhaseNoDuplicate");

            enemy.ApplyDamage(enemy.MaxHealth * 0.3f);
            controller.Tick(0.01f, 4f, null, host, null, null);
            context.AssertEqual(3, controller.BossPhase, "enemyLogic.abilities.PhaseTransition.phase3");
            context.AssertEqual(3, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.phase3EventCount");
            context.AssertEqual(3, observer.BossPhaseChanges[2].CurrentPhase, "enemyLogic.abilities.PhaseTransition.finalPhase");

            controller.Tick(0.01f, 5f, null, host, null, null);
            context.AssertEqual(3, observer.BossPhaseChanges.Count, "enemyLogic.abilities.PhaseTransition.stablePhaseNoDuplicate");
            context.AssertEqual(TotemEnemyAbilityType.PhaseTransition, host.LastCueAbilityType, "enemyLogic.abilities.PhaseTransition.cueType");
            context.Detail("enemyLogic.abilities.PhaseTransition.timeline", "Phase1>Phase2>Heal(no downgrade)>Phase2(no duplicate)>Phase3(no duplicate),cooldown=0");
        }

        private static void CheckInterruptibleRegeneration(GFDiagnosticScenarioContext context)
        {
            const int enemyCombatantId = 1501;
            TotemEnemyRuntimeDefinition definition = CreateControllerDefinition("interruptible_regenerate_probe");
            definition.tier = TotemEnemyTier.Elite;
            definition.behavior.attackRange = 20f;
            definition.behavior.eliteHotHz = 100f;
            definition.behavior.eliteWarmHz = 100f;
            definition.behavior.eliteColdHz = 100f;
            definition.abilities = new[]
            {
                new TotemEnemyAbilityRuntimeDefinition
                {
                    abilityId = "diagnostic_interruptible_regenerate",
                    abilityType = TotemEnemyAbilityType.Regenerate,
                    range = 20f,
                    cooldown = 9f,
                    windup = 0.9f,
                    active = 0.1f,
                    recovery = 0.7f,
                    healAmount = 35f,
                    score = 100f,
                    interruptible = true,
                    minimumBossPhase = 1,
                },
            };

            TotemActorModel participant = CreateActor(1502, TotemParticipantControllerKind.Human, Vector3.right);
            var service = new TotemEnemyService(4, TotemEnemyService.DefaultDefinitionCapacity, 1);
            service.Configure(new ParticipantSource(participant), null, null, null, null);
            context.Assert(service.RegisterDefinition(definition), "Interruptible Regenerate definition must register.");
            context.Assert(
                service.TrySpawn(
                    new TotemEnemySpawnRequest(enemyCombatantId, definition.enemyId, Vector3.zero, 1500, "diagnostic.regenerate", 0f),
                    out TotemEnemyModel enemy,
                    out TotemEnemySpawnBlockReason spawnReason),
                "Interruptible Regenerate owner must spawn: " + spawnReason);
            context.Assert(enemy != null, "Interruptible Regenerate owner must resolve after spawn.");
            if (enemy == null)
            {
                return;
            }

            context.Assert(
                service.TryApplyDamage(enemyCombatantId, participant, 20f, "DiagnosticSetupDamage", 0f, out float setupDamage, canInterrupt: false),
                "Setup damage must make Regenerate selectable.");
            context.Assert(setupDamage > 0f, "Setup damage must reduce owner health.");

            service.Tick(0.01f);
            service.Tick(0.01f);
            service.Tick(0.01f);
            TotemEnemyControllerBase controller = service.FindController(enemyCombatantId);
            context.Assert(controller != null, "Interruptible Regenerate controller must resolve.");
            context.AssertEqual(TotemEnemyState.Cast, controller?.State ?? TotemEnemyState.Dormant, "enemyLogic.regenerate.stateBeforeInterrupt");
            context.AssertEqual(TotemEnemyAbilityType.Regenerate, controller?.ActiveAbility?.Definition?.abilityType ?? TotemEnemyAbilityType.Unknown, "enemyLogic.regenerate.activeAbility");
            context.AssertEqual(TotemEnemyAbilityPhase.Windup, controller?.ActiveAbility?.Phase ?? TotemEnemyAbilityPhase.Inactive, "enemyLogic.regenerate.phaseBeforeInterrupt");

            context.Assert(
                service.TryApplyDamage(enemyCombatantId, participant, 1f, "DiagnosticInterrupt", service.WorldTime, out float interruptDamage),
                "Damage during Regenerate windup must be applied.");
            context.Assert(interruptDamage > 0f, "Interrupt damage must reduce owner health.");
            float healthAfterInterrupt = enemy.Health;
            context.AssertEqual(TotemEnemyState.Stagger, controller?.State ?? TotemEnemyState.Dormant, "enemyLogic.regenerate.interruptedState");
            context.AssertEqual(TotemEnemyAbilityPhase.Cancelled, controller?.ActiveAbility?.Phase ?? TotemEnemyAbilityPhase.Inactive, "enemyLogic.regenerate.cancelledPhase");

            service.Tick(0.8f);
            AssertNear(context, healthAfterInterrupt, enemy.Health, "enemyLogic.regenerate.noHealAfterInterrupt");
            context.Detail("enemyLogic.regenerate.evidence", "Windup>InterruptedByDamage>Cancelled>Stagger;heal=0");
        }

        private static void CheckSummonActiveCap(GFDiagnosticScenarioContext context)
        {
            const int enemyCombatantId = 1511;
            TotemEnemyRuntimeDefinition definition = CreateControllerDefinition("summon_active_cap_probe");
            definition.abilities = new[]
            {
                new TotemEnemyAbilityRuntimeDefinition
                {
                    abilityId = "diagnostic_summon_active_cap",
                    abilityType = TotemEnemyAbilityType.Summon,
                    range = 20f,
                    cooldown = 5f,
                    windup = 0.1f,
                    active = 0.1f,
                    recovery = 0.1f,
                    score = 100f,
                    summonEnemyId = "enemy_common_hunter",
                    summonCount = 1,
                    minimumBossPhase = 1,
                },
            };

            TotemActorModel participant = CreateActor(1512, TotemParticipantControllerKind.Human, Vector3.right);
            var gate = new ActiveCapSpawnGate();
            var service = new TotemEnemyService(4, TotemEnemyService.DefaultDefinitionCapacity, 1);
            service.Configure(new ParticipantSource(participant), null, null, gate, null);
            context.Assert(service.RegisterDefinition(definition), "ActiveCap Summon definition must register.");
            context.Assert(
                service.TrySpawn(
                    new TotemEnemySpawnRequest(enemyCombatantId, definition.enemyId, Vector3.zero, 1510, "diagnostic.summon", 0f),
                    out _,
                    out TotemEnemySpawnBlockReason spawnReason),
                "ActiveCap Summon owner must spawn before the gate closes: " + spawnReason);

            gate.BlockWithActiveCap = true;
            service.Tick(0.01f);
            service.Tick(0.01f);
            service.Tick(0.01f);
            service.Tick(0.11f);

            TotemEnemyRuntimeSnapshot snapshot = service.CaptureSnapshot();
            context.AssertEqual(1, snapshot.blockedSummons, "enemyLogic.summon.activeCapBlockedCount");
            context.AssertEqual(TotemEnemySpawnBlockReason.EncounterActiveCap.ToString(), snapshot.lastSpawnBlockReason, "enemyLogic.summon.activeCapReason");
            context.AssertEqual(1, snapshot.enemyCount, "enemyLogic.summon.activeCapEnemyCount");
            context.Detail("enemyLogic.summon.activeCapEvidence", "Summon>Blocked.EncounterActiveCap;spawnedChildren=0");
        }

        private static void AssertStandardAbilityEffect(
            GFDiagnosticScenarioContext context,
            TotemEnemyAbilityType abilityType,
            DiagnosticController controller,
            RecordingHost host,
            float healthBefore)
        {
            switch (abilityType)
            {
                case TotemEnemyAbilityType.Melee:
                case TotemEnemyAbilityType.Beam:
                    context.AssertEqual(1, host.TargetDamageCount, "enemyLogic.abilities." + abilityType + ".damageCount");
                    break;
                case TotemEnemyAbilityType.Projectile:
                    context.AssertEqual(1, host.ProjectileCount, "enemyLogic.abilities.Projectile.projectileCount");
                    context.AssertEqual(1, host.TargetDamageCount, "enemyLogic.abilities.Projectile.damageCount");
                    break;
                case TotemEnemyAbilityType.Charge:
                    context.AssertEqual(1, host.MoveCount, "enemyLogic.abilities.Charge.moveCount");
                    context.AssertEqual(1, host.TargetDamageCount, "enemyLogic.abilities.Charge.damageCount");
                    break;
                case TotemEnemyAbilityType.Leap:
                    context.AssertEqual(1, host.MoveCount, "enemyLogic.abilities.Leap.moveCount");
                    context.AssertEqual(1, host.RadiusDamageCount, "enemyLogic.abilities.Leap.radiusCount");
                    break;
                case TotemEnemyAbilityType.ConeSweep:
                    context.AssertEqual(1, host.ConeDamageCount, "enemyLogic.abilities.ConeSweep.coneCount");
                    break;
                case TotemEnemyAbilityType.AreaPulse:
                    context.AssertEqual(1, host.RadiusDamageCount, "enemyLogic.abilities.AreaPulse.radiusCount");
                    break;
                case TotemEnemyAbilityType.HazardZone:
                    context.AssertEqual(1, host.HazardCount, "enemyLogic.abilities.HazardZone.hazardCount");
                    context.AssertEqual(1, host.RadiusDamageCount, "enemyLogic.abilities.HazardZone.radiusCount");
                    break;
                case TotemEnemyAbilityType.Shield:
                    context.Assert(controller.Shield > 0f, "Shield must change the controller shield value.");
                    break;
                case TotemEnemyAbilityType.Summon:
                    context.AssertEqual(1, host.SummonCount, "enemyLogic.abilities.Summon.summonCount");
                    break;
                case TotemEnemyAbilityType.Regenerate:
                    context.Assert(controller.Enemy.Health > healthBefore, "Regenerate must increase owner health.");
                    break;
            }
        }

        private static void CheckBuiltInDefinitionBindings(GFDiagnosticScenarioContext context)
        {
            TotemEnemyRuntimeDefinition[] definitions = TotemEnemyBuiltInCatalog.CreateDefinitions();
            var service = new TotemEnemyService(16, TotemEnemyBuiltInCatalog.DefinitionCount, 1);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            context.AssertEqual(15, TotemEnemyBuiltInCatalog.DefinitionCount, "enemyLogic.catalog.declaredCount");
            context.AssertEqual(15, definitions.Length, "enemyLogic.catalog.createdCount");
            context.AssertEqual(15, service.DefinitionCount, "enemyLogic.catalog.serviceCount");

            for (int i = 0; i < definitions.Length; i++)
            {
                TotemEnemyRuntimeDefinition definition = definitions[i];
                context.Assert(definition != null && !string.IsNullOrEmpty(definition.enemyId), "Every built-in Enemy definition must have an id.");
                if (definition == null || string.IsNullOrEmpty(definition.enemyId))
                {
                    continue;
                }

                context.Assert(ids.Add(definition.enemyId), "Enemy definition id must be unique: " + definition.enemyId);
                context.Assert(definition.behavior != null && !string.IsNullOrEmpty(definition.behavior.behaviorProfileId), "Enemy behavior binding is missing: " + definition.enemyId);
                context.Assert(definition.abilities != null && definition.abilities.Length > 0, "Enemy ability binding is missing: " + definition.enemyId);
                string expectedAbilityIds = string.Join(",", definition.abilities.Select(item => item?.abilityId ?? string.Empty));
                context.AssertEqual(expectedAbilityIds, definition.abilityIds, "enemyLogic.catalog." + definition.enemyId + ".abilityIds");

                bool found = service.TryGetDefinition(definition.enemyId, out TotemEnemyRuntimeDefinition bound);
                context.Assert(found && bound != null, "EnemyService must bind built-in definition: " + definition.enemyId);
                string signature = BuildDefinitionSignature(definition);
                context.Assert(signatures.Add(signature), "Behavior/ability signature must remain observable and distinct: " + definition.enemyId);
                if (found && bound != null)
                {
                    context.AssertEqual(signature, BuildDefinitionSignature(bound), "enemyLogic.catalog." + definition.enemyId + ".serviceSignature");
                }

                for (int abilityIndex = 0; abilityIndex < definition.abilities.Length; abilityIndex++)
                {
                    TotemEnemyAbilityRuntimeDefinition abilityDefinition = definition.abilities[abilityIndex];
                    context.Assert(
                        abilityDefinition != null && TotemEnemyAbilityFactory.Create(abilityDefinition) != null,
                        "Built-in ability must bind to a concrete implementation: " + definition.enemyId + "." + abilityDefinition?.abilityId);
                }

                context.Detail("enemyLogic.catalog." + definition.enemyId + ".signature", signature);
            }

            context.AssertEqual(15, ids.Count, "enemyLogic.catalog.uniqueIdCount");
            context.AssertEqual(15, signatures.Count, "enemyLogic.catalog.distinctSignatureCount");
        }

        private static void CheckLodAndPathBudget(GFDiagnosticScenarioContext context)
        {
            TotemEnemyRuntimeDefinition lodDefinition = CreateControllerDefinition("lod_probe");
            lodDefinition.behavior.hotRadius = 20f;
            lodDefinition.behavior.warmRadius = 60f;
            lodDefinition.behavior.detectRange = 100f;
            TotemActorModel participant = CreateActor(1501, TotemParticipantControllerKind.LightBot, new Vector3(5f, 0f, 0f));
            var participants = new ParticipantSource(participant);
            var host = new RecordingHost { AllowMovement = false };
            DiagnosticController controller = CreateController(1502, lodDefinition, null);
            controller.Activate(0f);

            controller.Tick(0.01f, 0.01f, participants, host, null, null);
            context.AssertEqual(TotemEnemyLod.Hot, controller.Lod, "enemyLogic.lod.hot");
            participant.Position = new Vector3(30f, 0f, 0f);
            controller.Tick(0.01f, 0.02f, participants, host, null, null);
            context.AssertEqual(TotemEnemyLod.Warm, controller.Lod, "enemyLogic.lod.warm");
            participant.Position = new Vector3(70f, 0f, 0f);
            controller.Tick(0.01f, 0.03f, participants, host, null, null);
            context.AssertEqual(TotemEnemyLod.Cold, controller.Lod, "enemyLogic.lod.cold");

            var directBudget = new TotemEnemyPathBudget(2);
            context.Assert(directBudget.TryConsume() && directBudget.TryConsume(), "Configured path requests must be consumable.");
            context.Assert(!directBudget.TryConsume(), "Path budget must reject requests after capacity is consumed.");
            directBudget.BeginFrame();
            context.AssertEqual(2, directBudget.Remaining, "enemyLogic.pathBudget.frameReset");

            TotemEnemyRuntimeDefinition pathDefinition = CreateControllerDefinition("path_probe");
            pathDefinition.behavior.noProgressSeconds = 0.1f;
            pathDefinition.behavior.detectRange = 100f;
            pathDefinition.behavior.attackRange = 0.1f;
            TotemActorModel pathTarget = CreateActor(1510, TotemParticipantControllerKind.Human, new Vector3(10f, 0f, 0f));
            var pathParticipants = new ParticipantSource(pathTarget);
            var pathHost = new RecordingHost { AllowMovement = false };
            var pathProvider = new RecordingPathProvider();
            var sharedBudget = new TotemEnemyPathBudget(1);
            DiagnosticController first = CreateController(1511, pathDefinition, null);
            DiagnosticController second = CreateController(1512, pathDefinition, null);
            first.EnterChase(0f);
            second.EnterChase(0f);
            first.SetDiagnosticTarget(pathTarget, "PathProbe", 0f);
            second.SetDiagnosticTarget(pathTarget, "PathProbe", 0f);
            sharedBudget.BeginFrame();
            first.Tick(0.11f, 1f, pathParticipants, pathHost, pathProvider, sharedBudget);
            second.Tick(0.11f, 1f, pathParticipants, pathHost, pathProvider, sharedBudget);

            context.AssertEqual(1, pathProvider.RequestCount, "enemyLogic.pathBudget.providerRequestCount");
            context.AssertEqual(1, pathHost.AcceptedPathCount, "enemyLogic.pathBudget.acceptedCount");
            context.AssertEqual(1, pathHost.RejectedPathCount, "enemyLogic.pathBudget.rejectedCount");
            context.AssertEqual(0, sharedBudget.Remaining, "enemyLogic.pathBudget.remaining");
            context.Detail("enemyLogic.lod.evidence", "5m=Hot,30m=Warm,70m=Cold");
        }

        private static void CheckSteadyCombatTickAllocations(GFDiagnosticScenarioContext context)
        {
            const int enemyCount = 30;
            const int warmupFrames = 64;
            const int measuredFrames = 256;
            const float deltaTime = 1f / 60f;

            TotemEnemyRuntimeDefinition definition = CreateControllerDefinition("allocation_probe");
            definition.behavior.detectRange = 200f;
            definition.behavior.leashRange = 400f;
            definition.behavior.attackRange = 0.1f;
            definition.behavior.moveSpeed = 2f;

            TotemActorModel participant = CreateActor(
                1601,
                TotemParticipantControllerKind.Human,
                new Vector3(100f, 0f, 0f));
            var participantSource = new ParticipantSource(participant);
            var service = new TotemEnemyService(enemyCount + 2, TotemEnemyService.DefaultDefinitionCapacity, 4);
            service.Configure(participantSource, null, null, null, null);
            context.Assert(service.RegisterDefinition(definition), "Allocation probe definition must register.");

            for (int i = 0; i < enemyCount; i++)
            {
                bool spawned = service.TrySpawn(
                    new TotemEnemySpawnRequest(
                        1610 + i,
                        definition.enemyId,
                        new Vector3(0f, 0f, i * 0.25f),
                        1600,
                        "diagnostic.allocation",
                        0f),
                    out _,
                    out TotemEnemySpawnBlockReason reason);
                context.Assert(spawned, "Allocation probe enemy must spawn: index=" + i + ", reason=" + reason);
            }

            for (int frame = 0; frame < warmupFrames; frame++)
            {
                service.Tick(deltaTime);
            }

            // Warm the counter/JIT before collecting the measured steady-state window.
            // The counter is cumulative, so forced collection/finalizer waits are unnecessary
            // and would make the regular diagnostics suite stall on unrelated editor finalizers.
            GC.GetAllocatedBytesForCurrentThread();

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < measuredFrames; frame++)
            {
                service.Tick(deltaTime);
            }
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            TotemEnemyRuntimeSnapshot snapshot = service.CaptureSnapshot();
            context.AssertEqual(enemyCount, snapshot.enemyCount, "enemyLogic.alloc.enemyCount");
            context.Assert(snapshot.totalDecisions > 0, "Allocation probe must exercise target selection and combat decisions.");
            context.Assert(
                allocatedBytes == 0L,
                "Steady EnemyService combat Tick must allocate 0 managed bytes, actual=" + allocatedBytes + ".");
            context.Detail("enemyLogic.alloc.measuredFrames", measuredFrames);
            context.Detail("enemyLogic.alloc.enemyTicks", enemyCount * measuredFrames);
            context.Detail("enemyLogic.alloc.totalDecisions", snapshot.totalDecisions);
            context.Detail("enemyLogic.alloc.managedBytes", allocatedBytes);
        }

        private static DiagnosticController CreateController(
            int combatantId,
            TotemEnemyRuntimeDefinition definition,
            ITotemEnemyObserver observer)
        {
            var enemy = new TotemEnemyModel(
                combatantId,
                definition.enemyId,
                definition.displayName,
                definition.themeId,
                definition.tier,
                definition.maxHealth,
                Vector3.zero)
            {
                SpawnPosition = Vector3.zero,
                LeashRange = definition.behavior?.leashRange ?? 24f,
                BehaviorProfileId = definition.behavior?.behaviorProfileId ?? string.Empty,
                AbilityIds = definition.abilityIds,
            };
            return new DiagnosticController(enemy, definition, observer);
        }

        private static TotemEnemyRuntimeDefinition CreateControllerDefinition(string id)
        {
            return new TotemEnemyRuntimeDefinition
            {
                enemyId = id,
                displayName = id,
                themeId = "diagnostic",
                tier = TotemEnemyTier.Light,
                maxHealth = 100f,
                baseDamage = 10f,
                behavior = new TotemEnemyBehaviorDefinition
                {
                    behaviorProfileId = id,
                    detectRange = 100f,
                    attackRange = 0.1f,
                    leashRange = 200f,
                    moveSpeed = 2f,
                    hotRadius = 20f,
                    warmRadius = 60f,
                    lightHotHz = 100f,
                    lightWarmHz = 100f,
                    lightColdHz = 100f,
                    noProgressSeconds = 0.1f,
                    pathCacheSeconds = 1f,
                    pathNodeCapacity = 8,
                },
                abilities = Array.Empty<TotemEnemyAbilityRuntimeDefinition>(),
            };
        }

        private static TotemEnemyAbilityRuntimeDefinition CreateAbilityDefinition(TotemEnemyAbilityType abilityType)
        {
            return new TotemEnemyAbilityRuntimeDefinition
            {
                abilityId = "diagnostic_" + abilityType,
                abilityType = abilityType,
                range = 10f,
                radius = 10f,
                cooldown = 2f,
                windup = 0.1f,
                active = 0.1f,
                recovery = 0.1f,
                damageMultiplier = 1f,
                score = 1f,
                shieldAmount = 5f,
                healAmount = 5f,
                moveDistance = 2f,
                coneHalfAngle = 45f,
                summonEnemyId = "enemy_common_hunter",
                summonCount = 1,
                minimumBossPhase = 1,
            };
        }

        private static TotemActorModel CreateActor(
            int actorId,
            TotemParticipantControllerKind controllerKind,
            Vector3 position)
        {
            TotemActorKind actorKind = controllerKind == TotemParticipantControllerKind.SmartBot
                ? TotemActorKind.SmartAi
                : controllerKind == TotemParticipantControllerKind.LightBot ? TotemActorKind.LightAi : TotemActorKind.Player;
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = actorId,
                Name = "DiagnosticActor" + actorId,
                Kind = actorKind,
                ControllerKind = controllerKind,
                Position = position,
                MaxHealth = 100f,
            });
        }

        private static string BuildDefinitionSignature(TotemEnemyRuntimeDefinition definition)
        {
            TotemEnemyBehaviorDefinition behavior = definition.behavior ?? new TotemEnemyBehaviorDefinition();
            string abilitySignature = string.Join(",", (definition.abilities ?? Array.Empty<TotemEnemyAbilityRuntimeDefinition>())
                .Select(item =>
                {
                    ITotemEnemyAbility ability = TotemEnemyAbilityFactory.Create(item);
                    return (item?.abilityId ?? string.Empty) + ":" + (item?.abilityType.ToString() ?? "Null") + ":" + (ability?.GetType().Name ?? "Unbound");
                }));
            return definition.tier + "|" + behavior.behaviorProfileId + "|" +
                   behavior.detectRange.ToString("F2") + "|" + behavior.attackRange.ToString("F2") + "|" +
                   behavior.leashRange.ToString("F2") + "|" + abilitySignature;
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, name + ": expected=" + expected + ", actual=" + actual);
            context.Detail(name + ".actual", actual);
        }

        private sealed class DiagnosticController : TotemEnemyControllerBase
        {
            public DiagnosticController(
                TotemEnemyModel enemy,
                TotemEnemyRuntimeDefinition definition,
                ITotemEnemyObserver observer)
                : base(enemy, definition, observer)
            {
            }

            public bool TryDiagnosticTransition(TotemEnemyState state, string reason, float worldTime)
            {
                return Transition(state, reason, worldTime);
            }

            public void SetDiagnosticTarget(TotemActorModel target, string reason, float worldTime)
            {
                SetTarget(target, reason, worldTime);
            }

            public void EnterChase(float worldTime)
            {
                Transition(TotemEnemyState.Spawn, "DiagnosticSpawn", worldTime);
                Transition(TotemEnemyState.Patrol, "DiagnosticPatrol", worldTime);
                Transition(TotemEnemyState.Alert, "DiagnosticAlert", worldTime);
                Transition(TotemEnemyState.Chase, "DiagnosticChase", worldTime);
            }
        }

        private sealed class ParticipantSource : ITotemEnemyParticipantSource
        {
            private readonly TotemActorModel[] _participants;

            public ParticipantSource(params TotemActorModel[] participants)
            {
                _participants = participants ?? Array.Empty<TotemActorModel>();
            }

            public int ParticipantCount => _participants.Length;

            public TotemActorModel GetParticipantAt(int index)
            {
                return index >= 0 && index < _participants.Length ? _participants[index] : null;
            }

            public bool CanBeTargeted(TotemActorModel participant)
            {
                return participant != null && participant.IsAlive;
            }

            public bool IsReachable(TotemEnemyModel enemy, TotemActorModel participant)
            {
                return enemy != null && participant != null;
            }
        }

        private sealed class RecordingPathProvider : ITotemEnemyPathProvider
        {
            public int RequestCount { get; private set; }

            public bool TryBuildPath(Vector3 start, Vector3 destination, Vector3[] nodeBuffer, out int nodeCount)
            {
                RequestCount++;
                if (nodeBuffer == null || nodeBuffer.Length == 0)
                {
                    nodeCount = 0;
                    return false;
                }

                nodeBuffer[0] = destination;
                nodeCount = 1;
                return true;
            }
        }

        private sealed class ActiveCapSpawnGate : ITotemEnemySpawnGate
        {
            public bool BlockWithActiveCap { get; set; }

            public bool CanSpawn(
                int encounterInstanceId,
                string enemyId,
                int requestedCount,
                out TotemEnemySpawnBlockReason reason)
            {
                reason = BlockWithActiveCap
                    ? TotemEnemySpawnBlockReason.EncounterActiveCap
                    : TotemEnemySpawnBlockReason.None;
                return !BlockWithActiveCap;
            }
        }

        private sealed class RecordingObserver : ITotemEnemyObserver
        {
            public readonly List<TotemEnemyStateChangedEvent> StateChanges = new List<TotemEnemyStateChangedEvent>();
            public readonly List<TotemBossPhaseChangedEvent> BossPhaseChanges = new List<TotemBossPhaseChangedEvent>();

            public void OnStateChanged(in TotemEnemyStateChangedEvent evt)
            {
                StateChanges.Add(evt);
            }

            public void OnTargetChanged(in TotemEnemyTargetChangedEvent evt)
            {
            }

            public void OnAbilityChanged(in TotemEnemyAbilityEvent evt)
            {
            }

            public void OnBossPhaseChanged(in TotemBossPhaseChangedEvent evt)
            {
                BossPhaseChanges.Add(evt);
            }
        }

        private sealed class RecordingHost : ITotemEnemyAbilityHost
        {
            private readonly List<TotemEnemyAbilityPhase> _abilityPhases = new List<TotemEnemyAbilityPhase>();

            public bool AllowMovement { get; set; } = true;
            public int TargetDamageCount { get; private set; }
            public int RadiusDamageCount { get; private set; }
            public int ConeDamageCount { get; private set; }
            public int MoveCount { get; private set; }
            public int SummonCount { get; private set; }
            public int ProjectileCount { get; private set; }
            public int HazardCount { get; private set; }
            public int AbilityNotificationCount { get; private set; }
            public int AcceptedPathCount { get; private set; }
            public int RejectedPathCount { get; private set; }
            public TotemEnemyAbilityType LastCueAbilityType { get; private set; }

            public string AbilityTimeline => string.Join(">", _abilityPhases.Select(item => item.ToString()));

            public bool TryDamageTarget(
                TotemEnemyControllerBase controller,
                TotemActorModel target,
                TotemEnemyAbilityRuntimeDefinition definition,
                float multiplier)
            {
                TargetDamageCount++;
                return true;
            }

            public int DamageParticipantsInRadius(
                TotemEnemyControllerBase controller,
                Vector3 center,
                float radius,
                TotemEnemyAbilityRuntimeDefinition definition)
            {
                RadiusDamageCount++;
                return 1;
            }

            public int DamageParticipantsInCone(
                TotemEnemyControllerBase controller,
                Vector3 origin,
                Vector3 forward,
                float radius,
                float halfAngle,
                TotemEnemyAbilityRuntimeDefinition definition)
            {
                ConeDamageCount++;
                return 1;
            }

            public bool TryMove(TotemEnemyControllerBase controller, Vector3 delta)
            {
                MoveCount++;
                if (AllowMovement && controller?.Enemy != null)
                {
                    controller.Enemy.Position += delta;
                }

                return AllowMovement;
            }

            public bool TrySummon(
                TotemEnemyControllerBase controller,
                TotemEnemyAbilityRuntimeDefinition definition,
                int count)
            {
                SummonCount++;
                return true;
            }

            public void SpawnProjectile(
                TotemEnemyControllerBase controller,
                TotemActorModel target,
                TotemEnemyAbilityRuntimeDefinition definition)
            {
                ProjectileCount++;
            }

            public void CreateHazard(
                TotemEnemyControllerBase controller,
                Vector3 position,
                TotemEnemyAbilityRuntimeDefinition definition)
            {
                HazardCount++;
            }

            public void PlayCue(TotemEnemyControllerBase controller, TotemEnemyAbilityRuntimeDefinition definition)
            {
                LastCueAbilityType = definition?.abilityType ?? TotemEnemyAbilityType.Unknown;
            }

            public void NotifyAbility(TotemEnemyControllerBase controller, ITotemEnemyAbility ability, string reason)
            {
                AbilityNotificationCount++;
                _abilityPhases.Add(ability?.Phase ?? TotemEnemyAbilityPhase.Inactive);
            }

            public void NotifyPathRequest(TotemEnemyControllerBase controller, bool accepted)
            {
                if (accepted)
                {
                    AcceptedPathCount++;
                }
                else
                {
                    RejectedPathCount++;
                }
            }

            public string EffectSignature(DiagnosticController controller, float healthBefore)
            {
                return "target=" + TargetDamageCount +
                       ",radius=" + RadiusDamageCount +
                       ",cone=" + ConeDamageCount +
                       ",move=" + MoveCount +
                       ",summon=" + SummonCount +
                       ",projectile=" + ProjectileCount +
                       ",hazard=" + HazardCount +
                       ",shield=" + controller.Shield.ToString("F1") +
                       ",heal=" + (controller.Enemy.Health - healthBefore).ToString("F1");
            }
        }
    }
}
#endif
