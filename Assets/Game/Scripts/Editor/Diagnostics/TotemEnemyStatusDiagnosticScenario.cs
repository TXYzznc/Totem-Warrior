#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemEnemyStatusDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Enemy Status Fast";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var service = new TotemEnemyService(8, TotemEnemyBuiltInCatalog.DefinitionCount + 1, 1);
            service.RegisterDefinition(CreateDefinition());
            TotemActorModel source = CreateParticipant(91001);
            TotemEnemyModel durationTarget = Spawn(service, 92001);
            TotemEnemyModel lethalTarget = Spawn(service, 92002);
            TotemEnemyModel slowTarget = Spawn(service, 92003);
            TotemEnemyModel stunTarget = Spawn(service, 92004);

            int deathCount = 0;
            TotemEnemyDiedEvent deathEvent = default;
            service.EnemyDied += evt =>
            {
                deathCount++;
                deathEvent = evt;
            };

            var poison = new TotemEnemyStatusDefinition(
                TotemStatusService.PoisonStatus,
                TotemEnemyStatusKind.DamageOverTime,
                duration: 1f,
                power: 4f,
                tickInterval: 0.25f);
            context.Assert(
                service.TryApplyStatus(durationTarget.CombatantId, source, poison, "DiagnosticDuration", 0f, out TotemEnemyStatusApplyResult poisonResult),
                "Configurable Poison must apply to a native Enemy target.");
            context.AssertEqual(TotemEnemyStatusApplyResult.Applied, poisonResult, "enemyStatus.poison.applyResult");

            service.TryApplyDamage(
                lethalTarget.CombatantId,
                source,
                lethalTarget.MaxHealth - 1f,
                "DiagnosticSetup",
                0f,
                out _);
            context.Assert(
                service.TryApplyStatus(lethalTarget.CombatantId, source, poison, "DiagnosticLethalDot", 0f, out _),
                "Lethal DOT must apply before ticking.");

            context.Assert(TotemEnemyStatusDefinition.TryCreateBuiltIn("Slow", 0.5f, 0.5f, out TotemEnemyStatusDefinition slow), "Slow definition must resolve.");
            context.Assert(service.TryApplyStatus(slowTarget.CombatantId, source, slow, "DiagnosticSlow", 0f, out _), "Slow must apply.");
            AssertNear(context, 0.5f, service.GetMoveSpeedMultiplier(slowTarget.CombatantId), "enemyStatus.slow.multiplier");

            context.Assert(TotemEnemyStatusDefinition.TryCreateBuiltIn("Shock", 0f, 0.5f, out TotemEnemyStatusDefinition shock), "Shock definition must resolve as a stun.");
            context.Assert(service.TryApplyStatus(stunTarget.CombatantId, source, shock, "DiagnosticShock", 0f, out _), "Shock must apply.");
            context.Assert(service.IsStunned(stunTarget.CombatantId), "Shock must suppress Enemy AI for its duration.");

            context.Assert(
                !service.TryApplyStatus(stunTarget.CombatantId, source, default, "Invalid", 0f, out TotemEnemyStatusApplyResult invalidResult),
                "Invalid status definitions must be rejected.");
            context.AssertEqual(TotemEnemyStatusApplyResult.InvalidDefinition, invalidResult, "enemyStatus.invalid.result");

            service.Tick(0.25f);
            AssertNear(context, 19f, durationTarget.Health, "enemyStatus.poison.firstTickHealth");
            context.Assert(service.TryGetStatusRemaining(durationTarget.CombatantId, "Poison", out float remaining), "Poison must remain active after its first tick.");
            AssertNear(context, 0.75f, remaining, "enemyStatus.poison.remaining");
            context.AssertEqual(1, deathCount, "enemyStatus.dotDeath.count");
            context.Assert(ReferenceEquals(lethalTarget, deathEvent.Enemy), "DOT death must publish the native Enemy instance.");
            context.Assert(ReferenceEquals(source, deathEvent.Killer), "DOT death must retain its Participant source.");
            context.Assert(deathEvent.Reason.Contains("Status:Poison:DiagnosticLethalDot"), "DOT death reason must preserve status and source evidence.");

            service.Tick(0.25f);
            context.Assert(!service.HasStatus(slowTarget.CombatantId, "Slow"), "Slow must expire at its configured duration.");
            AssertNear(context, 1f, service.GetMoveSpeedMultiplier(slowTarget.CombatantId), "enemyStatus.slow.recoveredMultiplier");
            context.Assert(!service.IsStunned(stunTarget.CombatantId), "Shock must release control at its configured duration.");
            service.Tick(0.1f);
            context.AssertEqual(TotemEnemyState.Patrol, service.FindController(stunTarget.CombatantId).State, "enemyStatus.stun.resumedState");

            service.Tick(0.5f);
            AssertNear(context, 16f, durationTarget.Health, "enemyStatus.poison.finalHealth");
            context.Assert(!service.HasStatus(durationTarget.CombatantId, "Poison"), "Poison must expire after its configured duration.");

            TotemEnemyRuntimeSnapshot snapshot = service.CaptureSnapshot();
            context.AssertEqual(0, snapshot.activeStatusCount, "enemyStatus.activeAfterExpiry");
            context.AssertEqual(4, snapshot.totalStatusApplications, "enemyStatus.applicationCount");
            context.AssertEqual(5, snapshot.totalStatusTicks, "enemyStatus.tickCount");
            context.AssertEqual(1, snapshot.rejectedStatusApplications, "enemyStatus.rejectedCount");
            context.Detail("enemyStatus.dotDeathReason", deathEvent.Reason);
            context.Detail("enemyStatus.finalDurationTargetHealth", durationTarget.Health);
            CheckReentrantDotDespawn(context, source, poison);
            context.Pass("Enemy status application, deterministic duration, lethal DOT provenance, Slow/Stun recovery and invalid rejection are complete.");
        }

        private static void CheckReentrantDotDespawn(
            GFDiagnosticScenarioContext context,
            TotemActorModel source,
            TotemEnemyStatusDefinition poison)
        {
            var service = new TotemEnemyService(2, TotemEnemyBuiltInCatalog.DefinitionCount + 1, 1);
            service.RegisterDefinition(CreateDefinition());
            TotemEnemyModel target = Spawn(service, 92999);
            service.EnemyDied += evt =>
            {
                if (ReferenceEquals(evt.Enemy, target))
                {
                    service.Despawn(target.CombatantId, "DiagnosticSynchronousWorldDespawn");
                }
            };
            service.TryApplyDamage(target.CombatantId, source, target.MaxHealth - 1f, "DiagnosticReentrantSetup", 0f, out _);
            context.Assert(service.TryApplyStatus(target.CombatantId, source, poison, "DiagnosticReentrantDot", 0f, out _),
                "Reentrant DOT target must accept Poison.");
            service.Tick(0.25f);
            context.AssertEqual(0, service.EnemyCount, "enemyStatus.reentrantDespawn.enemyCount");
            context.AssertEqual(1, service.CaptureSnapshot().totalDeaths, "enemyStatus.reentrantDespawn.deathCount");
        }

        private static TotemEnemyRuntimeDefinition CreateDefinition()
        {
            return new TotemEnemyRuntimeDefinition
            {
                enemyId = "diagnostic_status_enemy",
                displayName = "Diagnostic Status Enemy",
                themeId = "diagnostic",
                tier = TotemEnemyTier.Light,
                maxHealth = 20f,
                baseDamage = 1f,
                behavior = new TotemEnemyBehaviorDefinition
                {
                    behaviorProfileId = "diagnostic_status",
                    moveSpeed = 4f,
                    detectRange = 0f,
                    attackRange = 0f,
                    leashRange = 10f,
                },
                abilities = System.Array.Empty<TotemEnemyAbilityRuntimeDefinition>(),
            };
        }

        private static TotemEnemyModel Spawn(TotemEnemyService service, int combatantId)
        {
            bool spawned = service.TrySpawn(
                new TotemEnemySpawnRequest(combatantId, "diagnostic_status_enemy", Vector3.zero, 1, "status_probe", 0f),
                out TotemEnemyModel enemy,
                out TotemEnemySpawnBlockReason reason);
            if (!spawned)
            {
                throw new System.InvalidOperationException("Diagnostic Enemy spawn failed: " + reason);
            }

            return enemy;
        }

        private static TotemActorModel CreateParticipant(int actorId)
        {
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = actorId,
                Name = "StatusSource",
                Kind = TotemActorKind.Player,
                ControllerKind = TotemParticipantControllerKind.Human,
                MaxHealth = 100f,
                Position = Vector3.right,
            });
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string key)
        {
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, key + " expected=" + expected.ToString("F3") + " actual=" + actual.ToString("F3"));
        }
    }
}
#endif
