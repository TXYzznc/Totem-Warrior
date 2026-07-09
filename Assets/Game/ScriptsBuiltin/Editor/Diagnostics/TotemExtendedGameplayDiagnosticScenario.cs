#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemExtendedGameplayDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Extended Gameplay";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckTattooCatalog(context);
            CheckActorScopedTattooRuntime(context);
            CheckWeaponCatalogAndUpgrade(context);
            CheckWeaponDropAndPickupRuntime(context);
            CheckChestRuntime(context);
            CheckSkillAndStatusTiming(context);
            CheckSelfTattooInterruption(context);
            CheckActorDamageEdgeContracts(context);
            CheckStatusAndTattooDamageRouting(context);
            CheckDeathChestRuntime(context);
            CheckDeathChestInteractionRuntime(context);
            CheckZoneAndBossContracts(context);
            CheckZoneDamageRuntime(context);
            CheckBossRewardRuntime(context);
            CheckEconomyNpcAndChoices(context);
            CheckInteractionContracts(context);
            CheckNpcInteractionRuntime(context);
            CheckMapEventInteractionRuntime(context);
            CheckRuntimeShopPurchase(context);
            CheckShopAndChoiceRewardRouting(context);
            CheckSettingsSelfTattooAndRunResult(context);
            context.Pass("Totem extended gameplay contract is ready.");
        }

        private static void CheckActorDamageEdgeContracts(GFDiagnosticScenarioContext context)
        {
            var actorService = new TotemActorService();
            var player = NewActor(31, TotemActorKind.Player, 100f);
            int appliedCount = 0;
            int resolvedCount = 0;
            int killedCount = 0;
            float lastAppliedDamage = 0f;
            TotemDamageRecord lastResolved = default;

            actorService.DamageApplied += (_, damage, killed) =>
            {
                appliedCount++;
                lastAppliedDamage = damage;
                if (killed)
                {
                    killedCount++;
                }
            };
            actorService.DamageResolved += record =>
            {
                resolvedCount++;
                lastResolved = record;
            };

            context.Assert(!actorService.ApplyDamage(player, 0f, null, "ZeroDamage"), "Zero actor damage should not resolve.");
            context.Assert(!actorService.ApplyDamage(player, -10f, null, "NegativeDamage"), "Negative actor damage should not resolve.");
            AssertNear(context, 100f, player.Health, "actor.damage.invalid.health");
            context.AssertEqual(0, appliedCount, "actor.damage.invalid.appliedCount");
            context.AssertEqual(0, resolvedCount, "actor.damage.invalid.resolvedCount");
            context.AssertEqual(0, killedCount, "actor.damage.invalid.killedCount");
            context.AssertEqual(0, actorService.LastDamage.Sequence, "actor.damage.invalid.sequence");

            context.Assert(actorService.ApplyDamage(player, 150f, null, "EnemySkill"), "Excess actor damage should kill the target.");
            AssertNear(context, 0f, player.Health, "actor.damage.kill.health");
            context.AssertEqual(1, appliedCount, "actor.damage.kill.appliedCount");
            context.AssertEqual(1, resolvedCount, "actor.damage.kill.resolvedCount");
            context.AssertEqual(1, killedCount, "actor.damage.kill.killedCount");
            AssertNear(context, 150f, lastAppliedDamage, "actor.damage.kill.amount");
            context.AssertEqual(1, lastResolved.Sequence, "actor.damage.kill.sequence");
            context.AssertEqual("EnemySkill", lastResolved.Reason, "actor.damage.kill.reason");
            AssertNear(context, 0f, lastResolved.TargetHealthAfter, "actor.damage.kill.targetHealthAfter");
            var deathSnapshot = actorService.CaptureAnimationSnapshot(player);
            context.Assert(deathSnapshot.animationDead, "Killed player actor should enter death animation state.");
            context.AssertEqual(1, deathSnapshot.deathTriggerCount, "actor.damage.kill.deathTriggerCount");

            context.Assert(!actorService.ApplyDamage(player, 10f, null, "EnemyNormal"), "Repeated damage on a dead actor should not resolve.");
            context.AssertEqual(1, appliedCount, "actor.damage.repeat.appliedCount");
            context.AssertEqual(1, resolvedCount, "actor.damage.repeat.resolvedCount");
            context.AssertEqual(1, killedCount, "actor.damage.repeat.killedCount");
            context.AssertEqual(1, actorService.LastDamage.Sequence, "actor.damage.repeat.sequence");
            context.AssertEqual(1, actorService.CaptureAnimationSnapshot(player).deathTriggerCount, "actor.damage.repeat.deathTriggerCount");
        }

        private static void CheckTattooCatalog(GFDiagnosticScenarioContext context)
        {
            var catalog = TotemTattooService.BuildAllCombinations();
            context.AssertEqual(336, catalog.Length, "tattoo.combinationCount");
            context.Assert(catalog.Any(item => item.PartId == 4 && item.ColorId == 1 && item.PatternId == 1 && item.TriggerEvent == "AttackHitEvent"), "RightArm Red Line must map to AttackHitEvent.");
            context.Assert(catalog.Any(item => item.PartId == 3 && item.TriggerEvent == "SkillCastEvent"), "LeftArm must map to SkillCastEvent.");
            var rightArmRedLine = catalog.FirstOrDefault(item => item.PartId == 4 && item.ColorId == 1 && item.PatternId == 1);
            context.AssertEqual("WeaponDamage", rightArmRedLine?.ScaleStat ?? string.Empty, "tattoo.part.rightArm.scaleStat");
            AssertNear(context, 0.8f, rightArmRedLine?.ScaleFactor ?? -1f, "tattoo.part.rightArm.scaleFactor");
            AssertNear(context, 1f, rightArmRedLine?.Magnitude ?? -1f, "tattoo.magnitude.oldTable");
            AssertNear(context, 8f, TotemTattooService.GetSelfTattooDuration(1), "tattoo.reading.head");
            AssertNear(context, 5f, TotemTattooService.GetSelfTattooDuration(4), "tattoo.reading.rightArm");
            AssertNear(context, 3f, TotemTattooService.GetSelfTattooDuration(6), "tattoo.reading.rightLeg");

            var service = new TotemTattooService();
            var emptySnapshot = service.CaptureSnapshot();
            context.AssertEqual(6, emptySnapshot.readingTimeCount, "tattoo.reading.catalogCount");
            context.AssertEqual(24, emptySnapshot.enchantAffixCount, "tattoo.enchant.affixCount");
            context.AssertEqual(3, emptySnapshot.enchantRecipeCount, "tattoo.enchant.recipeCount");
            context.Assert(service.Equip(4, 1, 1), "RightArm Red Line should equip.");
            var target = NewActor(2, TotemActorKind.SmartAi, 100f);
            var results = service.Trigger("AttackHitEvent", NewActor(1, TotemActorKind.Player, 100f), target, 10f);
            context.AssertEqual(1, results.Length, "tattoo.attackResultCount");
            AssertNear(context, 10f, results.FirstOrDefault()?.Damage ?? -1f, "tattoo.attackDamage.oldMultiplier");
            context.Assert(target.Health < 100f, "Tattoo trigger should modify target health.");
            context.Assert(!service.Equip(1, 999, 1), "Invalid tattoo color id should reject without modifying equipped state.");
            var invalidEquipSnapshot = service.CaptureSnapshot();
            context.AssertEqual(1, invalidEquipSnapshot.equippedCount, "tattoo.invalidEquip.equippedCount");
            context.AssertEqual(1, invalidEquipSnapshot.appliedEffectCount, "tattoo.invalidEquip.effectLogCount");
            context.AssertEqual("RightArm/Red/Line", invalidEquipSnapshot.equippedSummary, "tattoo.invalidEquip.summary");
            context.Assert(service.Equip(4, 1, 2), "RightArm Red Ring should equip.");
            var ringResults = service.Trigger("AttackHitEvent", NewActor(3, TotemActorKind.Player, 100f), NewActor(4, TotemActorKind.SmartAi, 100f), 10f);
            context.AssertEqual(1, ringResults.FirstOrDefault()?.HitCount ?? -1, "tattoo.shape.aoe.standaloneHitCount");
            AssertNear(context, 6f, ringResults.FirstOrDefault()?.Damage ?? -1f, "tattoo.shape.aoe.standaloneDamage");
            context.Assert(service.Equip(5, 1, 1), "LeftLeg Red Line should equip for clear diagnostic.");
            var pendingResults = service.Trigger("DodgePressedEvent", NewActor(5, TotemActorKind.Player, 100f), null, 12f);
            context.AssertEqual(1, pendingResults.Length, "tattoo.clear.pendingImmediateResultCount");
            context.AssertEqual("PendingTrigger", pendingResults.FirstOrDefault()?.StatusName ?? string.Empty, "tattoo.clear.pendingStatus");
            context.AssertEqual(1, service.CaptureSnapshot().pendingTriggerCount, "tattoo.clear.pendingBefore");
            context.Assert(service.ApplyEnchant("Common"), "Tattoo clear diagnostic should apply one enchant.");
            context.Assert(service.Equip(NewActor(7, TotemActorKind.SmartAi, 100f), 4, 1, 1), "Tattoo clear diagnostic should create one actor state.");
            var dirtySnapshot = service.CaptureSnapshot();
            context.Assert(dirtySnapshot.equippedCount > 0, "Tattoo clear diagnostic should have equipped tattoos before Clear.");
            context.Assert(dirtySnapshot.appliedEffectCount > 0, "Tattoo clear diagnostic should have effect log before Clear.");
            context.Assert(dirtySnapshot.pendingTriggerCount > 0, "Tattoo clear diagnostic should have pending trigger before Clear.");
            context.Assert(dirtySnapshot.activeEnchantAffixCount > 0, "Tattoo clear diagnostic should have enchant affix before Clear.");
            context.Assert(dirtySnapshot.actorStateCount > 0, "Tattoo clear diagnostic should have actor state before Clear.");
            service.Clear();
            var clearedTattoo = service.CaptureSnapshot();
            context.AssertEqual(0, clearedTattoo.equippedCount, "tattoo.clear.equippedCount");
            context.AssertEqual(0, clearedTattoo.appliedEffectCount, "tattoo.clear.effectLogCount");
            context.AssertEqual(0, clearedTattoo.pendingTriggerCount, "tattoo.clear.pendingTriggerCount");
            context.AssertEqual(0, clearedTattoo.activeEnchantAffixCount, "tattoo.clear.enchantAffixCount");
            context.AssertEqual(0, clearedTattoo.actorStateCount, "tattoo.clear.actorStateCount");
            context.AssertEqual(string.Empty, clearedTattoo.equippedSummary, "tattoo.clear.summary");
            CheckTattooShapeRuntime(context);

            AssertNear(context, 0.05f, TotemTattooService.ComputeHeadCritRateBonus(10f), "tattoo.head.passiveCritRate");
            AssertNear(context, 0.1f, TotemTattooService.ComputeHeadElementBonus(10f), "tattoo.head.passiveElementBonus");
            AssertNear(context, 1f, TotemTattooService.ComputeHeadCritChance(1f, 0.05f), "tattoo.head.critChance.clamped");
            AssertNear(context, 0f, TotemTattooService.ComputeHeadCritChance(0f, 0f), "tattoo.head.critChance.zeroPattern");
            context.Assert(TotemTattooService.ShouldHeadCrit(1f, 0.999f), "Head PatternMultiplier=1 should always crit.");
            context.Assert(!TotemTattooService.ShouldHeadCrit(0f, 0f), "Head PatternMultiplier=0 should never crit.");
            AssertNear(context, 1.5f, TotemTattooService.ResolveHeadCritMultiplier(0f), "tattoo.head.defaultCritMultiplier");
            AssertNear(context, 2f, TotemTattooService.ResolveHeadCritMultiplier(2f), "tattoo.head.configuredCritMultiplier");

            var noHeadService = new TotemTattooService();
            context.Assert(noHeadService.Equip(4, 1, 1), "RightArm-only tattoo should equip for no-head crit diagnostic.");
            float noHeadDamage = noHeadService.ResolveAttackDamage(NewActor(11, TotemActorKind.Player, 100f), NewActor(12, TotemActorKind.LightAi, 100f), 20f, out var noHeadCrit);
            AssertNear(context, 20f, noHeadDamage, "tattoo.head.noHead.damage");
            context.Assert(noHeadCrit == null, "RightArm-only tattoo should not emit Head crit.");
            context.AssertEqual(0, noHeadService.CaptureSnapshot().critTriggeredCount, "tattoo.head.noHead.count");

            var headCritService = new TotemTattooService();
            context.Assert(headCritService.Equip(1, 1, 1), "Head Red Line should equip for crit diagnostic.");
            var headSource = NewActor(13, TotemActorKind.Player, 100f);
            var headTarget = NewActor(14, TotemActorKind.LightAi, 100f);
            float critDamage = headCritService.ResolveAttackDamage(headSource, headTarget, 20f, out var headCrit);
            AssertNear(context, 30f, critDamage, "tattoo.head.critDamage");
            context.Assert(headCrit != null && headCrit.IsCritical, "Head crit should return a critical result.");
            AssertNear(context, 20f, headCrit?.BaseDamage ?? -1f, "tattoo.head.critResult.baseDamage");
            AssertNear(context, 30f, headCrit?.Damage ?? -1f, "tattoo.head.critResult.finalDamage");
            AssertNear(context, 1.5f, headCrit?.CritMultiplier ?? -1f, "tattoo.head.critResult.multiplier");
            AssertNear(context, 100f, headTarget.Health, "tattoo.head.resolveDoesNotApplyDamage");
            var headSnapshot = headCritService.CaptureSnapshot();
            context.AssertEqual(1, headSnapshot.critTriggeredCount, "tattoo.head.snapshot.critCount");
            AssertNear(context, 0.05f, headSnapshot.lastHeadPassiveCritRateBonus, "tattoo.head.snapshot.passiveCrit");
            AssertNear(context, 0.1f, headSnapshot.lastHeadPassiveElementBonus, "tattoo.head.snapshot.passiveElement");
            AssertNear(context, 30f, headSnapshot.lastCritDamage, "tattoo.head.snapshot.lastDamage");
            context.AssertEqual("Head/Red/Line", headSnapshot.lastCritTattooSummary, "tattoo.head.snapshot.summary");

            var actorHeadService = new TotemTattooService();
            var actorHeadSource = NewActor(15, TotemActorKind.SmartAi, 100f);
            context.Assert(actorHeadService.Equip(actorHeadSource, 1, 1, 1), "Actor-scoped Head tattoo should equip.");
            float actorCritDamage = actorHeadService.ResolveAttackDamage(actorHeadSource, NewActor(16, TotemActorKind.Player, 100f), 10f, out var actorHeadCrit);
            AssertNear(context, 15f, actorCritDamage, "tattoo.head.actorCritDamage");
            context.Assert(actorHeadCrit != null && actorHeadCrit.IsCritical, "Actor-scoped Head tattoo should crit.");
            context.AssertEqual(1, actorHeadService.CaptureSnapshot(actorHeadSource).critTriggeredCount, "tattoo.head.actorSnapshot.count");
            context.AssertEqual(1, actorHeadService.CaptureSnapshot().actorCritTriggeredCount, "tattoo.head.actorAggregate.count");
            context.AssertEqual(0, actorHeadService.CaptureSnapshot().critTriggeredCount, "tattoo.head.actorAggregate.globalCount");

            var enchantDamageService = new TotemTattooService();
            context.Assert(enchantDamageService.Equip(4, 1, 1), "RightArm Red Line should equip for enchant damage diagnostic.");
            context.Assert(enchantDamageService.ApplyEnchant("Common"), "First Common enchant should apply ElementDamageBonus.");
            var enchantSnapshot = enchantDamageService.CaptureSnapshot();
            context.AssertEqual(1, enchantSnapshot.activeEnchantAffixCount, "tattoo.enchant.activeCount");
            AssertNear(context, 0.1f, enchantSnapshot.activeElementDamageBonus, "tattoo.enchant.elementDamageBonus");
            context.Assert(enchantSnapshot.activeEnchantSummary.Contains("ElementDamageBonus", System.StringComparison.Ordinal), "Enchant summary should include ElementDamageBonus.");
            var enchantDamage = enchantDamageService.Trigger("AttackHitEvent", NewActor(17, TotemActorKind.Player, 100f), NewActor(18, TotemActorKind.LightAi, 100f), 10f).FirstOrDefault();
            AssertNear(context, 11f, enchantDamage?.Damage ?? -1f, "tattoo.enchant.elementDamageApplied");

            var healEnchantService = new TotemTattooService();
            context.Assert(healEnchantService.Equip(4, 1, 1), "RightArm Red Line should equip for self-heal enchant diagnostic.");
            for (int i = 0; i < 5; i++)
            {
                context.Assert(healEnchantService.ApplyEnchant("Common"), $"Common enchant {i + 1} should apply.");
            }

            var healSource = NewActor(19, TotemActorKind.Player, 100f);
            healSource.ApplyDamage(20f);
            var healResult = healEnchantService.Trigger("AttackHitEvent", healSource, NewActor(20, TotemActorKind.LightAi, 100f), 10f).FirstOrDefault();
            AssertNear(context, 5f, healResult?.SourceHeal ?? -1f, "tattoo.enchant.selfHealResult");
            AssertNear(context, 85f, healSource.Health, "tattoo.enchant.selfHealHealth");
            context.AssertEqual(5, healEnchantService.CaptureSnapshot().activeEnchantAffixCount, "tattoo.enchant.selfHeal.activeCount");
            AssertNear(context, 5f, healEnchantService.CaptureSnapshot().activeSelfHealOnHit, "tattoo.enchant.selfHeal.snapshot");

            var critEnchantService = new TotemTattooService();
            context.Assert(critEnchantService.Equip(1, 1, 1), "Head Red Line should equip for crit enchant diagnostic.");
            for (int i = 0; i < 8; i++)
            {
                context.Assert(critEnchantService.ApplyEnchant("Common"), $"Crit Common enchant {i + 1} should apply.");
            }

            float enchantedCritDamage = critEnchantService.ResolveAttackDamage(NewActor(21, TotemActorKind.Player, 100f), NewActor(22, TotemActorKind.LightAi, 100f), 10f, out var enchantedCrit);
            AssertNear(context, 16.5f, enchantedCritDamage, "tattoo.enchant.critDamageApplied");
            AssertNear(context, 1.65f, enchantedCrit?.CritMultiplier ?? -1f, "tattoo.enchant.critMultiplier");
            AssertNear(context, 0.05f, critEnchantService.CaptureSnapshot().activeCritChanceBonus, "tattoo.enchant.critChanceSnapshot");
            AssertNear(context, 0.15f, critEnchantService.CaptureSnapshot().activeCritDamageBonus, "tattoo.enchant.critDamageSnapshot");

            var statEnchantService = new TotemTattooService();
            context.Assert(statEnchantService.Equip(4, 1, 1), "RightArm Red Line should equip for stat enchant diagnostic.");
            for (int i = 0; i < 7; i++)
            {
                context.Assert(statEnchantService.ApplyEnchant("Common"), $"Stat Common enchant {i + 1} should apply.");
            }

            var statSource = NewActor(23, TotemActorKind.Player, 100f);
            var statSnapshot = statEnchantService.CaptureSnapshot();
            AssertNear(context, 0.08f, statSnapshot.activeAttackSpeedBonus, "tattoo.enchant.attackSpeedSnapshot");
            AssertNear(context, 0.10f, statSnapshot.activeCooldownReduction, "tattoo.enchant.cooldownSnapshot");
            AssertNear(context, 0.08f, statSnapshot.activeStatusChanceBonus, "tattoo.enchant.statusChanceSnapshot");
            AssertNear(context, 0.10f, statSnapshot.activeRangeBonus, "tattoo.enchant.rangeSnapshot");
            AssertNear(context, 0.9f / 1.08f, statEnchantService.ResolveWeaponCooldownMultiplier(statSource), "tattoo.enchant.weaponCooldownMultiplier");
            AssertNear(context, 0.9f, statEnchantService.ResolveSkillCooldownMultiplier(statSource), "tattoo.enchant.skillCooldownMultiplier");
            AssertNear(context, 1.1f, statEnchantService.ResolveRangeMultiplier(statSource), "tattoo.enchant.rangeMultiplier");
            AssertNear(context, 0.08f, statEnchantService.ResolveStatusChanceBonus(statSource), "tattoo.enchant.statusChanceBonus");
            AssertNear(context, 0.83f, TotemTattooService.ComputeStatusApplyChance(0.75f, 0.08f), "tattoo.enchant.statusChanceFormula.common");
            AssertNear(context, 1f, TotemTattooService.ComputeStatusApplyChance(0.75f, 0.25f), "tattoo.enchant.statusChanceFormula.legendaryClamp");
            context.Assert(TotemTattooService.ShouldApplyStatus(0.83f, 0.82f), "Status chance should pass when roll is below chance.");
            context.Assert(!TotemTattooService.ShouldApplyStatus(0.83f, 0.84f), "Status chance should fail when roll is above chance.");

            var distanceEnchantService = new TotemTattooService();
            context.Assert(distanceEnchantService.Equip(4, 1, 1), "RightArm Red Line should equip for distance enchant diagnostic.");
            for (int i = 0; i < 4; i++)
            {
                context.Assert(distanceEnchantService.ApplyEnchant("Rare"), $"Rare enchant {i + 1} should apply.");
            }

            var distanceSource = NewActor(24, TotemActorKind.Player, 100f);
            var nearTarget = NewActor(25, TotemActorKind.LightAi, 100f);
            nearTarget.Position = Vector3.forward * 4f;
            var farTarget = NewActor(26, TotemActorKind.LightAi, 100f);
            farTarget.Position = Vector3.forward * 10f;
            var nearDamage = distanceEnchantService.Trigger("AttackHitEvent", distanceSource, nearTarget, 10f).FirstOrDefault();
            var farDamage = distanceEnchantService.Trigger("AttackHitEvent", distanceSource, farTarget, 10f).FirstOrDefault();
            AssertNear(context, 12f, nearDamage?.Damage ?? -1f, "tattoo.enchant.distance.nearDamage");
            AssertNear(context, 15f, farDamage?.Damage ?? -1f, "tattoo.enchant.distance.farDamage");

            var afterDodgeEnchantService = new TotemTattooService();
            context.Assert(afterDodgeEnchantService.Equip(4, 1, 1), "RightArm Red Line should equip for AfterDodge enchant diagnostic.");
            for (int i = 0; i < 4; i++)
            {
                context.Assert(afterDodgeEnchantService.ApplyEnchant("Legendary"), $"AfterDodge Legendary enchant {i + 1} should apply.");
            }

            var afterDodgeSource = NewActor(31, TotemActorKind.Player, 100f);
            var beforeDodgeTarget = NewActor(32, TotemActorKind.LightAi, 100f);
            var beforeDodgeDamage = afterDodgeEnchantService.Trigger("AttackHitEvent", afterDodgeSource, beforeDodgeTarget, 10f).FirstOrDefault();
            AssertNear(context, 13.5f, beforeDodgeDamage?.Damage ?? -1f, "tattoo.enchant.afterDodge.beforeDamage");
            context.AssertEqual(0, afterDodgeEnchantService.Trigger("DodgePressedEvent", afterDodgeSource, null, 4f).Length, "tattoo.enchant.afterDodge.readyResultCount");
            var afterDodgeReady = afterDodgeEnchantService.CaptureSnapshot();
            context.Assert(afterDodgeReady.afterDodgeEnchantPending, "AfterDodge enchant should be pending after dodge.");
            context.AssertEqual(1, afterDodgeReady.afterDodgeEnchantCreatedCount, "tattoo.enchant.afterDodge.created");
            context.AssertEqual(0, afterDodgeReady.afterDodgeEnchantConsumedCount, "tattoo.enchant.afterDodge.notConsumedYet");
            context.AssertEqual(afterDodgeSource.ActorId, afterDodgeReady.lastAfterDodgeEnchantActorId, "tattoo.enchant.afterDodge.actorId");

            var afterDodgeTarget = NewActor(33, TotemActorKind.LightAi, 100f);
            var afterDodgeDamage = afterDodgeEnchantService.Trigger("AttackHitEvent", afterDodgeSource, afterDodgeTarget, 10f).FirstOrDefault();
            AssertNear(context, 18f, afterDodgeDamage?.Damage ?? -1f, "tattoo.enchant.afterDodge.firstDamage");
            var afterDodgeConsumed = afterDodgeEnchantService.CaptureSnapshot();
            context.Assert(!afterDodgeConsumed.afterDodgeEnchantPending, "AfterDodge enchant should be consumed by the next hit.");
            context.AssertEqual(1, afterDodgeConsumed.afterDodgeEnchantConsumedCount, "tattoo.enchant.afterDodge.consumed");

            var secondAfterDodgeTarget = NewActor(34, TotemActorKind.LightAi, 100f);
            var secondAfterDodgeDamage = afterDodgeEnchantService.Trigger("AttackHitEvent", afterDodgeSource, secondAfterDodgeTarget, 10f).FirstOrDefault();
            AssertNear(context, 13.5f, secondAfterDodgeDamage?.Damage ?? -1f, "tattoo.enchant.afterDodge.secondDamage");
            context.AssertEqual(1, afterDodgeEnchantService.CaptureSnapshot().afterDodgeEnchantConsumedCount, "tattoo.enchant.afterDodge.singleUse");

            CheckEnchantStatRuntime(context);

            var pendingService = new TotemTattooService();
            context.Assert(pendingService.Equip(5, 1, 1), "LeftLeg Red Line should equip for pending trigger.");
            var pendingPlayer = NewActor(5, TotemActorKind.Player, 100f);
            var pendingTarget = NewActor(6, TotemActorKind.SmartAi, 100f);
            var dodgeResults = pendingService.Trigger("DodgePressedEvent", pendingPlayer, null, 4f);
            context.AssertEqual(1, dodgeResults.Length, "tattoo.pending.dodgeResultCount");
            context.AssertEqual("PendingTrigger", dodgeResults.FirstOrDefault()?.StatusName ?? string.Empty, "tattoo.pending.dodgeStatus");
            AssertNear(context, 100f, pendingTarget.Health, "tattoo.pending.noImmediateDamage");
            var afterDodge = pendingService.CaptureSnapshot();
            context.AssertEqual(1, afterDodge.pendingTriggerCount, "tattoo.pending.afterDodge.count");
            context.AssertEqual(1, afterDodge.pendingTriggerCreatedCount, "tattoo.pending.afterDodge.created");
            context.AssertEqual("LeftLeg", afterDodge.lastPendingTriggerSource, "tattoo.pending.afterDodge.source");
            context.AssertEqual("AttackHitEvent", afterDodge.lastPendingTriggerConsumeEvent, "tattoo.pending.afterDodge.consumeEvent");
            context.Assert(afterDodge.lastPendingTriggerSummary.Contains("LeftLeg/Red/Line", System.StringComparison.Ordinal), "Pending summary should include source/color/pattern.");
            var attackResults = pendingService.Trigger("AttackHitEvent", pendingPlayer, pendingTarget, 10f);
            context.AssertEqual(1, attackResults.Length, "tattoo.pending.attackResultCount");
            AssertNear(context, 4f, attackResults.FirstOrDefault()?.Damage ?? -1f, "tattoo.pending.consumedDamage");
            AssertNear(context, 96f, pendingTarget.Health, "tattoo.pending.targetHealth");
            var afterConsume = pendingService.CaptureSnapshot();
            context.AssertEqual(0, afterConsume.pendingTriggerCount, "tattoo.pending.afterConsume.count");
            context.AssertEqual(1, afterConsume.pendingTriggerConsumedCount, "tattoo.pending.afterConsume.consumed");
            context.AssertEqual("AttackHitEvent", afterConsume.lastPendingTriggerConsumeEvent, "tattoo.pending.afterConsume.consumeEvent");

            var pendingEnchantService = new TotemTattooService();
            context.Assert(pendingEnchantService.Equip(5, 1, 1), "LeftLeg Red Line should equip for pending enchant diagnostic.");
            context.Assert(pendingEnchantService.ApplyEnchant("Common"), "Pending enchant should apply ElementDamageBonus.");
            var pendingEnchantPlayer = NewActor(29, TotemActorKind.Player, 100f);
            var pendingEnchantTarget = NewActor(30, TotemActorKind.LightAi, 100f);
            pendingEnchantService.Trigger("DodgePressedEvent", pendingEnchantPlayer, null, 4f);
            var pendingEnchantAttack = pendingEnchantService
                .Trigger("AttackHitEvent", pendingEnchantPlayer, pendingEnchantTarget, 10f)
                .FirstOrDefault(item => (item.Note ?? string.Empty).Contains("ConsumedPending@AttackHitEvent", System.StringComparison.Ordinal));
            AssertNear(context, 4.4f, pendingEnchantAttack?.Damage ?? -1f, "tattoo.enchant.pendingDamageApplied");
            AssertNear(context, 95.6f, pendingEnchantTarget.Health, "tattoo.enchant.pendingTargetHealth");

            var afterDodgePendingService = new TotemTattooService();
            context.Assert(afterDodgePendingService.Equip(5, 1, 1), "LeftLeg Red Line should equip for AfterDodge pending enchant diagnostic.");
            for (int i = 0; i < 4; i++)
            {
                context.Assert(afterDodgePendingService.ApplyEnchant("Legendary"), $"AfterDodge pending Legendary enchant {i + 1} should apply.");
            }

            var afterDodgePendingPlayer = NewActor(35, TotemActorKind.Player, 100f);
            var afterDodgePendingTarget = NewActor(36, TotemActorKind.LightAi, 100f);
            var afterDodgePendingReadyResults = afterDodgePendingService.Trigger("DodgePressedEvent", afterDodgePendingPlayer, null, 4f);
            context.AssertEqual(1, afterDodgePendingReadyResults.Length, "tattoo.enchant.afterDodgePending.readyResultCount");
            var afterDodgePendingReady = afterDodgePendingService.CaptureSnapshot();
            context.Assert(afterDodgePendingReady.afterDodgeEnchantPending, "AfterDodge pending enchant should be armed by dodge.");
            context.AssertEqual(1, afterDodgePendingReady.pendingTriggerCount, "tattoo.enchant.afterDodgePending.pendingCount");
            var afterDodgePendingAttack = afterDodgePendingService
                .Trigger("AttackHitEvent", afterDodgePendingPlayer, afterDodgePendingTarget, 10f)
                .FirstOrDefault(item => (item.Note ?? string.Empty).Contains("ConsumedPending@AttackHitEvent", System.StringComparison.Ordinal));
            AssertNear(context, 7.2f, afterDodgePendingAttack?.Damage ?? -1f, "tattoo.enchant.afterDodgePending.damageApplied");
            AssertNear(context, 92.8f, afterDodgePendingTarget.Health, "tattoo.enchant.afterDodgePending.targetHealth");
            var afterDodgePendingConsumed = afterDodgePendingService.CaptureSnapshot();
            context.Assert(!afterDodgePendingConsumed.afterDodgeEnchantPending, "AfterDodge pending enchant should be consumed with pending hit.");
            context.AssertEqual(1, afterDodgePendingConsumed.afterDodgeEnchantConsumedCount, "tattoo.enchant.afterDodgePending.consumed");
        }

        private static void CheckEnchantStatRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemEnchantStatDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemDataService());
                runtime.RegisterService(new TotemStatusService());
                runtime.RegisterService(new TotemTattooService());
                runtime.RegisterService(new TotemWeaponService());
                runtime.RegisterService(new TotemSkillService());
                runtime.StartRuntime();

                var tattoo = runtime.GetService<TotemTattooService>();
                var status = runtime.GetService<TotemStatusService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var skill = runtime.GetService<TotemSkillService>();
                var player = NewActor(27, TotemActorKind.Player, 100f);
                var target = NewActor(28, TotemActorKind.LightAi, 100f);

                context.Assert(tattoo.Equip(4, 1, 1), "Runtime stat enchant should equip a player tattoo.");
                for (int i = 0; i < 7; i++)
                {
                    context.Assert(tattoo.ApplyEnchant("Common"), $"Runtime stat enchant {i + 1} should apply.");
                }

                var statusTarget = NewActor(37, TotemActorKind.LightAi, 100f);
                var tattooStatus = tattoo.Trigger("AttackHitEvent", player, statusTarget, 10f).FirstOrDefault();
                context.Assert(tattooStatus != null && tattooStatus.StatusApplied, "Runtime tattoo status should apply through status chance gate.");
                AssertNear(context, 1f, tattooStatus?.StatusChance ?? -1f, "tattoo.enchant.runtime.statusChance");
                AssertNear(context, 0.08f, tattooStatus?.StatusChanceBonus ?? -1f, "tattoo.enchant.runtime.statusChanceBonus");
                AssertNear(context, 0f, tattooStatus?.StatusRoll ?? -1f, "tattoo.enchant.runtime.statusRoll");
                context.Assert(status.HasStatus(statusTarget, TotemStatusService.BurnStatus), "Runtime tattoo status should use existing Burn status, not a standalone StatusChance effect.");

                weapon.EquipWeapon(player, "knife_basic");
                var fire = weapon.FireWeapon(player, target, false, 0f);
                context.Assert(fire.Fired, "Runtime enchanted weapon fire should succeed.");
                float expectedWeaponCooldown = fire.Weapon.Cooldown * TotemTattooService.ComputeWeaponCooldownMultiplier(0.08f, 0.10f);
                float expectedRange = fire.Weapon.Range * TotemTattooService.ComputeRangeMultiplier(0.10f);
                AssertNear(context, expectedWeaponCooldown, weapon.GetOrCreateState(player).CooldownRemaining, "tattoo.enchant.runtime.weaponCooldown");
                AssertNear(context, expectedRange, fire.Range, "tattoo.enchant.runtime.weaponRange");

                context.Assert(skill.EquipSkill(player, 0, "skill_fireball_01"), "Runtime enchanted skill should equip.");
                context.Assert(skill.TryCastSlot(player, 0, out var castSkill), "Runtime enchanted skill should cast.");
                AssertNear(context, (castSkill?.Cooldown ?? -1f) * TotemTattooService.ComputeSkillCooldownMultiplier(0.10f), skill.GetCooldownRemaining(player, 0), "tattoo.enchant.runtime.skillCooldown");
                context.AssertEqual("skill_fireball_01", castSkill?.SkillId ?? string.Empty, "tattoo.enchant.runtime.skillId");
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

        private static void CheckActorScopedTattooRuntime(GFDiagnosticScenarioContext context)
        {
            var service = new TotemTattooService();
            var smart = NewActor(101, TotemActorKind.SmartAi, 100f);
            var otherSmart = NewActor(102, TotemActorKind.SmartAi, 100f);
            var target = NewActor(201, TotemActorKind.Player, 100f);

            context.Assert(service.StartSelfTattoo(smart, 4, 1, 2), "Smart AI should start an actor-scoped self tattoo.");
            var reading = service.CaptureSnapshot(smart);
            context.Assert(reading.selfTattooInProgress, "Actor self tattoo should be in progress after start.");
            context.AssertEqual("Part4/Color1/Pattern2", reading.pendingSelfTattooSummary, "actorTattoo.pending");
            AssertNear(context, 5f, reading.selfTattooRemainingSec, "actorTattoo.readingDuration");
            context.AssertEqual(1, service.CaptureSnapshot().actorStateCount, "actorTattoo.aggregate.stateCount");

            service.Tick(TotemTattooService.GetSelfTattooDuration(4));
            var finished = service.CaptureSnapshot(smart);
            context.Assert(!finished.selfTattooInProgress, "Actor self tattoo should finish after duration.");
            context.AssertEqual(1, finished.equippedCount, "actorTattoo.equippedCount");
            context.AssertEqual("RightArm/Red/Ring", finished.equippedSummary, "actorTattoo.summary");

            var aggregate = service.CaptureSnapshot();
            context.AssertEqual(0, aggregate.equippedCount, "actorTattoo.globalEquippedCount");
            context.AssertEqual(1, aggregate.actorEquippedCount, "actorTattoo.aggregate.equippedCount");

            var results = service.Trigger("AttackHitEvent", smart, target, 10f);
            context.AssertEqual(1, results.Length, "actorTattoo.triggerResultCount");
            context.Assert(target.Health < 100f, "Actor-scoped tattoo trigger should damage the target.");
            context.AssertEqual(1, service.CaptureSnapshot(smart).appliedEffectCount, "actorTattoo.effectLogCount");

            float targetHealth = target.Health;
            var isolatedResults = service.Trigger("AttackHitEvent", otherSmart, target, 10f);
            context.AssertEqual(0, isolatedResults.Length, "actorTattoo.isolatedResultCount");
            AssertNear(context, targetHealth, target.Health, "actorTattoo.isolatedTargetHealth");

            context.Assert(service.Equip(4, 1, 1), "Player/global tattoo should still equip through the public player API.");
            var playerTarget = NewActor(202, TotemActorKind.LightAi, 100f);
            var playerResults = service.Trigger("AttackHitEvent", NewActor(1, TotemActorKind.Player, 100f), playerTarget, 10f);
            context.AssertEqual(1, playerResults.Length, "actorTattoo.playerGlobalTriggerCount");
            context.Assert(playerTarget.Health < 100f, "Player/global tattoo trigger should remain intact.");

            context.Assert(service.Equip(smart, 5, 1, 1), "Smart AI should equip actor-scoped LeftLeg pending tattoo.");
            var pendingTarget = NewActor(203, TotemActorKind.LightAi, 100f);
            var actorDodgeResults = service.Trigger("DodgePressedEvent", smart, null, 3f);
            context.AssertEqual(1, actorDodgeResults.Length, "actorTattoo.pending.dodgeResultCount");
            context.AssertEqual(1, service.CaptureSnapshot(smart).pendingTriggerCount, "actorTattoo.pending.count");
            context.AssertEqual(1, service.CaptureSnapshot().actorPendingTriggerCount, "actorTattoo.pending.aggregateCount");
            var actorAttackResults = service.Trigger("AttackHitEvent", smart, pendingTarget, 10f);
            context.AssertEqual(2, actorAttackResults.Length, "actorTattoo.pending.attackResultCount");
            context.Assert(actorAttackResults.Any(item => (item.Note ?? string.Empty).Contains("ConsumedPending@AttackHitEvent", System.StringComparison.Ordinal)), "Actor pending trigger should be consumed on next attack.");
            context.Assert(service.CaptureSnapshot(smart).pendingTriggerConsumedCount >= 1, "Actor pending trigger consumption should be counted.");
            context.Assert(pendingTarget.Health < 100f, "Actor pending trigger should damage the next attack target.");
        }

        private static void CheckTattooShapeRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemTattooShapeDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemDataService());
                runtime.RegisterService(new TotemActorService());
                runtime.RegisterService(new TotemStatusService());
                runtime.RegisterService(new TotemTattooService());
                runtime.StartRuntime();

                var actor = runtime.GetService<TotemActorService>();
                var tattoo = runtime.GetService<TotemTattooService>();
                actor.SpawnActors(TotemMapService.BuildLayout(seed: 3, themeId: 1), new TotemStartupSelection(), createObjects: false);
                var player = actor.Player;
                var primary = actor.Actors.First(item => item.Kind == TotemActorKind.SmartAi);
                var nearA = actor.Actors.First(item => item.Kind == TotemActorKind.LightAi);
                var nearB = actor.Actors.Last(item => item.Kind == TotemActorKind.LightAi);
                var far = actor.Actors.First(item => item.Kind == TotemActorKind.SmartAi && !ReferenceEquals(item, primary));
                player.Position = Vector3.zero;
                primary.Position = new Vector3(4f, 0f, 0f);
                nearA.Position = new Vector3(6f, 0f, 0f);
                nearB.Position = new Vector3(4f, 0f, 4f);
                far.Position = new Vector3(40f, 0f, 0f);

                for (int i = 0; i < actor.Actors.Count; i++)
                {
                    var item = actor.Actors[i];
                    if (item != null &&
                        !ReferenceEquals(item, player) &&
                        !ReferenceEquals(item, primary) &&
                        !ReferenceEquals(item, nearA) &&
                        !ReferenceEquals(item, nearB) &&
                        !ReferenceEquals(item, far))
                    {
                        item.Position = new Vector3(80f + i, 0f, 80f);
                    }
                }

                float primaryBefore = primary.Health;
                float nearABefore = nearA.Health;
                float nearBBefore = nearB.Health;
                float farBefore = far.Health;
                context.Assert(tattoo.Equip(4, 1, 2), "Runtime AOEBurst tattoo should equip.");
                var results = tattoo.Trigger("AttackHitEvent", player, primary, 10f);
                var aoe = results.FirstOrDefault();
                context.AssertEqual(1, results.Length, "tattoo.shape.runtimeAoe.resultCount");
                context.AssertEqual(3, aoe?.HitCount ?? -1, "tattoo.shape.runtimeAoe.hitCount");
                AssertNear(context, 18f, aoe?.Damage ?? -1f, "tattoo.shape.runtimeAoe.totalDamage");
                AssertNear(context, primaryBefore - 6f, primary.Health, "tattoo.shape.runtimeAoe.primaryHealth");
                AssertNear(context, nearABefore - 6f, nearA.Health, "tattoo.shape.runtimeAoe.nearAHealth");
                AssertNear(context, nearBBefore - 6f, nearB.Health, "tattoo.shape.runtimeAoe.nearBHealth");
                AssertNear(context, farBefore, far.Health, "tattoo.shape.runtimeAoe.farHealth");

                context.Assert(tattoo.Equip(4, 1, 3), "Runtime StackingMark tattoo should equip.");
                var stackTarget = actor.Actors.First(item => item.Kind == TotemActorKind.LightAi && !ReferenceEquals(item, nearA) && !ReferenceEquals(item, nearB));
                stackTarget.Position = new Vector3(10f, 0f, 0f);
                float stackHealth = stackTarget.Health;
                for (int i = 0; i < 4; i++)
                {
                    var stack = tattoo.Trigger("AttackHitEvent", player, stackTarget, 10f).FirstOrDefault();
                    context.AssertEqual(0, stack?.HitCount ?? -1, $"tattoo.shape.stacking.preBurstHit{i}");
                    context.Assert((stack?.Note ?? string.Empty).Contains("StackingMark:Stack", System.StringComparison.Ordinal), "StackingMark should report stack progress.");
                }

                var burst = tattoo.Trigger("AttackHitEvent", player, stackTarget, 10f).FirstOrDefault();
                context.Assert(burst != null && burst.BurstTriggered, "StackingMark should burst at threshold.");
                context.AssertEqual(5, burst.StackThreshold, "tattoo.shape.stacking.threshold");
                AssertNear(context, 40f, burst.Damage, "tattoo.shape.stacking.burstDamage");
                AssertNear(context, stackHealth - 40f, stackTarget.Health, "tattoo.shape.stacking.targetHealth");

                context.Assert(tattoo.Equip(2, 1, 1), "Runtime Torso tattoo should equip.");
                context.Assert(TotemTattooService.TryGetDefinition(2, 1, 1, out var torsoDefinition), "Torso definition should resolve.");
                float attackerBefore = far.Health;
                actor.ApplyDamage(player, 10f, far, "DiagnosticTorsoDamage");
                AssertNear(context, attackerBefore - 10f * torsoDefinition.Magnitude, far.Health, "tattoo.part.torso.reflectDamage");

                context.Assert(tattoo.Equip(6, 1, 1), "Runtime RightLeg tattoo should equip.");
                context.Assert(TotemTattooService.TryGetDefinition(6, 1, 1, out var rightLegDefinition), "RightLeg definition should resolve.");
                for (int i = 0; i < actor.Actors.Count; i++)
                {
                    var item = actor.Actors[i];
                    if (item != null && !ReferenceEquals(item, player) && !ReferenceEquals(item, far))
                    {
                        item.Position = new Vector3(120f + i, 0f, 120f);
                    }
                }

                player.Position = Vector3.zero;
                far.Position = new Vector3(1f, 0f, 0f);
                float moveTargetBefore = far.Health;
                var moveResults = tattoo.Trigger("MoveTickEvent", player, null, 2f);
                context.AssertEqual(1, moveResults.Length, "tattoo.part.rightLeg.moveResultCount");
                AssertNear(context, 2f * rightLegDefinition.Magnitude, moveResults.FirstOrDefault()?.Damage ?? -1f, "tattoo.part.rightLeg.moveDamage");
                AssertNear(context, moveTargetBefore - 2f * rightLegDefinition.Magnitude, far.Health, "tattoo.part.rightLeg.targetHealth");
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

        private static void CheckWeaponCatalogAndUpgrade(GFDiagnosticScenarioContext context)
        {
            string[] requiredWeapons =
            {
                "knife_basic",
                "hammer_heavy",
                "pistol_basic",
                "bow_charge",
                "energy_fist",
            };

            for (int i = 0; i < requiredWeapons.Length; i++)
            {
                context.Assert(TotemWeaponService.TryGetDefinition(requiredWeapons[i], out _), $"Weapon must exist: {requiredWeapons[i]}");
            }

            var l2 = TotemWeaponService.GetMultipliers(2);
            AssertNear(context, 1.2f, l2.DamageMul, "weapon.l2.damageMul");
            AssertNear(context, 0.5f, l2.RangeAdd, "weapon.l2.rangeAdd");
            AssertNear(context, 0.9f, l2.CooldownMul, "weapon.l2.cooldownMul");

            var l3 = TotemWeaponService.GetMultipliers(3);
            AssertNear(context, 1.44f, l3.DamageMul, "weapon.l3.damageMul");
            AssertNear(context, 1.0f, l3.RangeAdd, "weapon.l3.rangeAdd");
            AssertNear(context, 0.81f, l3.CooldownMul, "weapon.l3.cooldownMul");
            context.AssertEqual(25, TotemWeaponService.ComputeConvertGold(50), "weapon.convertGold");

            var service = new TotemWeaponService();
            var player = NewActor(1, TotemActorKind.Player, 100f);
            var target = NewActor(2, TotemActorKind.SmartAi, 100f);
            context.AssertEqual(1, service.GetWeaponLevel(player), "weapon.default.level");
            context.AssertEqual(TotemWeaponService.DefaultWeaponId, service.GetEquippedWeaponId(player), "weapon.default.id");
            context.Assert(service.TryUpgrade(player, "knife_basic", 50, out int firstUpgradeConvertedGold), "Knife should upgrade from level 1 to 2.");
            context.AssertEqual(0, firstUpgradeConvertedGold, "weapon.upgrade.l2.convertedGold");
            context.AssertEqual(2, service.GetWeaponLevel(player), "weapon.upgrade.l2.level");
            context.Assert(service.TryUpgrade(player, "knife_basic", 50, out int secondUpgradeConvertedGold), "Knife should upgrade from level 2 to 3.");
            context.AssertEqual(0, secondUpgradeConvertedGold, "weapon.upgrade.l3.convertedGold");
            context.AssertEqual(3, service.GetWeaponLevel(player), "weapon.upgrade.l3.level");
            context.Assert(!service.TryUpgrade(player, "knife_basic", 50, out int maxLevelConvertedGold), "Max-level knife should convert duplicate upgrade to gold.");
            context.AssertEqual(25, maxLevelConvertedGold, "weapon.upgrade.max.convertedGold");
            context.AssertEqual(3, service.GetWeaponLevel(player), "weapon.upgrade.max.level");
            service.EquipWeapon(player, "knife_basic");
            var fire = service.FireWeapon(player, target, false, 0f);
            context.Assert(fire.Fired, "Equipped knife should fire.");
            AssertNear(context, 18f, fire.Damage, "weapon.knife.damage");
            AssertNear(context, 36f / 60f, fire.Weapon.Cooldown, "weapon.knife.cooldown");
            context.AssertEqual("trait_quickslash", fire.ActiveTrait?.TraitId, "weapon.knife.normalTrait");

            service.EquipWeapon(player, "pistol_basic");
            var pistolFire = service.FireWeapon(player, target, false, 0f);
            context.Assert(pistolFire.Fired, "Equipped pistol should fire.");
            AssertNear(context, 16f, pistolFire.Damage, "weapon.pistol.damage");
            context.AssertEqual("bullet_pistol", pistolFire.Projectile?.ProjectileId, "weapon.pistol.projectile");
            context.AssertEqual("trait_pierce", pistolFire.ActiveTrait?.TraitId, "weapon.pistol.normalTrait");

            service.EquipWeapon(player, "bow_charge");
            var unchargedBow = service.FireWeapon(player, target, false, 0f);
            context.Assert(!unchargedBow.Fired && unchargedBow.Reason == "RequiresCharge", "Bow should preserve requires-charge behavior.");
            var chargedBow = service.FireWeapon(player, target, true, 1f);
            context.Assert(chargedBow.Fired, "Charged bow should fire.");
            AssertNear(context, 72f, chargedBow.Damage, "weapon.bow.chargedDamage");
            context.AssertEqual("arrow_bow", chargedBow.Projectile?.ProjectileId, "weapon.bow.projectile");
            context.AssertEqual("trait_chain", chargedBow.ActiveTrait?.TraitId, "weapon.bow.chargedTrait");
            CheckWeaponTraitRuntime(context);
        }

        private static void CheckWeaponTraitRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemWeaponTraitDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemActorService());
                runtime.RegisterService(new TotemStatusService());
                runtime.RegisterService(new TotemTattooService());
                runtime.RegisterService(new TotemWeaponService());
                runtime.StartRuntime();

                var actor = runtime.GetService<TotemActorService>();
                var status = runtime.GetService<TotemStatusService>();
                var tattoo = runtime.GetService<TotemTattooService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var player = NewActor(11, TotemActorKind.Player, 100f);
                var target = NewActor(12, TotemActorKind.SmartAi, 100f);
                weapon.EquipWeapon(player, "hammer_heavy");
                var fire = weapon.FireWeapon(player, target, false, 0f);
                context.Assert(fire.Fired, "Hammer should fire for stun trait diagnostics.");
                context.AssertEqual("trait_stun", fire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.stun.id");
                bool killed = actor.ApplyDamage(target, fire.Damage, player, "DiagnosticWeaponTrait");
                context.Assert(!killed, "Hammer diagnostic target should survive base damage.");
                var stun = weapon.ApplyTraitEffect(fire, player, target, killed);
                context.Assert(stun.applied, $"Stun trait should apply: {stun.reason}");
                context.AssertEqual("Stun", stun.statusName, "weapon.trait.stun.status");
                AssertNear(context, 0.8f, stun.statusDuration, "weapon.trait.stun.duration");
                context.Assert(stun.statusApplied, "Stun trait should report statusApplied.");
                AssertNear(context, 1f, stun.statusChance, "weapon.trait.stun.statusChance");
                AssertNear(context, 0f, stun.statusChanceBonus, "weapon.trait.stun.statusChanceBonus");
                AssertNear(context, 0f, stun.statusRoll, "weapon.trait.stun.statusRoll");
                context.AssertEqual(1, status.CaptureSnapshot(target).activeCount, "weapon.trait.stun.activeStatusCount");
                context.AssertEqual(1, weapon.TraitEffectAppliedCount, "weapon.trait.appliedCount.afterStun");

                context.Assert(weapon.TryGetRuntimeTraitDefinition("trait_dot_burn", out var burnTrait), "Burn status trait should exist in runtime catalog.");
                var burnTarget = NewActor(13, TotemActorKind.LightAi, 100f);
                var burnFire = new TotemWeaponFireResult
                {
                    Fired = true,
                    Reason = "Diagnostic",
                    Weapon = fire.Weapon,
                    ActiveTrait = burnTrait,
                    Damage = 1f,
                };
                var burn = weapon.ApplyTraitEffect(burnFire, player, burnTarget, false);
                context.Assert(burn.applied, $"Burn status trait should apply: {burn.reason}");
                context.AssertEqual("Burn", burn.statusName, "weapon.trait.status.burnName");
                AssertNear(context, 4f, burn.statusDps, "weapon.trait.status.burnDps");
                AssertNear(context, 2.5f, burn.statusDuration, "weapon.trait.status.burnDuration");
                context.Assert(burn.statusApplied, "Burn status trait should report statusApplied.");
                AssertNear(context, 1f, burn.statusChance, "weapon.trait.status.burnChance");
                AssertNear(context, 0f, burn.statusChanceBonus, "weapon.trait.status.burnChanceBonus");
                context.Assert(TotemStatusService.FormatStatusSummary(status.CaptureSnapshot(burnTarget)).Contains("Burn 2.5s", System.StringComparison.Ordinal), "Burn trait should appear in status summary.");

                context.Assert(weapon.TryGetRuntimeTraitDefinition("trait_dot_poison", out var poisonTrait), "Poison status trait should exist in runtime catalog.");
                var poisonTarget = NewActor(16, TotemActorKind.LightAi, 100f);
                var poisonFire = new TotemWeaponFireResult
                {
                    Fired = true,
                    Reason = "Diagnostic",
                    Weapon = fire.Weapon,
                    ActiveTrait = poisonTrait,
                    Damage = 1f,
                };
                var poison = weapon.ApplyTraitEffect(poisonFire, player, poisonTarget, false);
                context.Assert(poison.applied, $"Poison status trait should apply: {poison.reason}");
                context.AssertEqual(TotemStatusService.PoisonStatus, poison.statusName, "weapon.trait.status.poisonName");
                AssertNear(context, 3f, poison.statusDps, "weapon.trait.status.poisonDps");
                AssertNear(context, 3f, poison.statusDuration, "weapon.trait.status.poisonDuration");
                context.Assert(poison.statusApplied, "Poison status trait should report statusApplied.");
                AssertNear(context, 1f, poison.statusChance, "weapon.trait.status.poisonChance");
                context.Assert(status.HasStatus(poisonTarget, TotemStatusService.PoisonStatus), "Poison trait should apply the existing Poison status.");
                context.Assert(TotemStatusService.FormatStatusSummary(status.CaptureSnapshot(poisonTarget)).Contains("Poison 3.0s", System.StringComparison.Ordinal), "Poison trait should appear in status summary.");

                context.Assert(tattoo.Equip(4, 1, 1), "Weapon status chance diagnostic should equip a tattoo.");
                for (int i = 0; i < 7; i++)
                {
                    context.Assert(tattoo.ApplyEnchant("Common"), $"Weapon status chance Common enchant {i + 1} should apply.");
                }

                var enchantedBurnTarget = NewActor(15, TotemActorKind.LightAi, 100f);
                var enchantedBurn = weapon.ApplyTraitEffect(burnFire, player, enchantedBurnTarget, false);
                context.Assert(enchantedBurn.applied, $"Enchanted burn status trait should apply: {enchantedBurn.reason}");
                AssertNear(context, 0.08f, enchantedBurn.statusChanceBonus, "weapon.trait.status.enchantedBurnChanceBonus");
                AssertNear(context, 1f, enchantedBurn.statusChance, "weapon.trait.status.enchantedBurnChance");
                context.Assert(status.HasStatus(enchantedBurnTarget, TotemStatusService.BurnStatus), "Enchanted weapon trait should still apply the existing Burn status.");

                weapon.EquipWeapon(player, "knife_basic");
                var quickTarget = NewActor(14, TotemActorKind.LightAi, 100f);
                var quickFire = weapon.FireWeapon(player, quickTarget, false, 0f);
                context.Assert(quickFire.Fired, "Knife should fire for QuickSlash diagnostics.");
                context.AssertEqual("trait_quickslash", quickFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.quick.id");
                float cooldownBeforeQuick = weapon.GetOrCreateState(player).CooldownRemaining;
                var quick = weapon.ApplyTraitEffect(quickFire, player, quickTarget, false);
                context.Assert(quick.applied, $"Quick trait should apply: {quick.reason}");
                context.Assert(quick.cooldownRemaining < cooldownBeforeQuick, "Quick trait should reduce weapon cooldown.");
                AssertNear(context, cooldownBeforeQuick * 0.7f, quick.cooldownRemaining, "weapon.trait.quick.cooldownRemaining");

                context.Assert(weapon.TryGetRuntimeTraitDefinition("trait_lifesteal", out var lifestealTrait), "Life Steal trait should exist in runtime catalog.");
                player.ApplyDamage(40f);
                float playerHealthBeforeLifeSteal = player.Health;
                var lifesteal = weapon.ApplyTraitEffect(new TotemWeaponFireResult { Fired = true, ActiveTrait = lifestealTrait, Damage = 100f }, player, burnTarget, false);
                context.Assert(lifesteal.applied, $"Life Steal trait should apply: {lifesteal.reason}");
                AssertNear(context, 8f, lifesteal.sourceHeal, "weapon.trait.lifesteal.heal");
                AssertNear(context, playerHealthBeforeLifeSteal + 8f, player.Health, "weapon.trait.lifesteal.playerHealth");

                CheckComplexWeaponTraitRuntime(context, actor, weapon);
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

        private static void CheckComplexWeaponTraitRuntime(GFDiagnosticScenarioContext context, TotemActorService actor, TotemWeaponService weapon)
        {
            actor.SpawnActors(TotemMapService.BuildLayout(seed: 3107, themeId: 1), new TotemStartupSelection
            {
                CharacterId = 1,
                ColorId = 1,
                WeaponId = "knife_basic",
                PatternIds = new[] { 1 },
            }, createObjects: false);

            var player = actor.Player;
            var enemies = actor.Actors
                .Where(item => item.Kind == TotemActorKind.SmartAi || item.Kind == TotemActorKind.LightAi)
                .Take(6)
                .ToArray();
            context.Assert(player != null, "Complex weapon trait diagnostics need a runtime player.");
            context.Assert(enemies.Length >= 6, "Complex weapon trait diagnostics need at least six runtime enemies.");
            player.Position = Vector3.zero;
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Position = new Vector3(1.5f + i * 0.75f, 0.5f, (i % 2) * 0.5f);
            }

            weapon.EquipWeapon(player, "pistol_basic");
            var pierceFire = weapon.FireWeapon(player, enemies[0], false, 0f);
            context.Assert(pierceFire.Fired, "Pistol should fire for Pierce diagnostics.");
            context.AssertEqual("trait_pierce", pierceFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.pierce.id");
            var pierce = weapon.ApplyTraitEffect(pierceFire, player, enemies[0], false);
            context.Assert(pierce.applied, $"Pierce trait should apply: {pierce.reason}");
            context.AssertEqual(3, pierce.secondaryHitCount, "weapon.trait.pierce.secondaryHitCount");
            AssertNear(context, 28.8f, pierce.secondaryDamage, "weapon.trait.pierce.secondaryDamage");

            actor.SpawnActors(TotemMapService.BuildLayout(seed: 3108, themeId: 1), new TotemStartupSelection(), createObjects: false);
            player = actor.Player;
            enemies = actor.Actors
                .Where(item => item.Kind == TotemActorKind.SmartAi || item.Kind == TotemActorKind.LightAi)
                .Take(6)
                .ToArray();
            player.Position = Vector3.zero;
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Position = new Vector3(1.5f + i * 1.0f, 0.5f, 0f);
            }

            weapon.EquipWeapon(player, "bow_charge");
            var chainFire = weapon.FireWeapon(player, enemies[0], true, 1f);
            context.Assert(chainFire.Fired, "Charged bow should fire for Chain diagnostics.");
            context.AssertEqual("trait_chain", chainFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.chain.id");
            var chain = weapon.ApplyTraitEffect(chainFire, player, enemies[0], false);
            context.Assert(chain.applied, $"Chain trait should apply: {chain.reason}");
            context.AssertEqual(3, chain.secondaryHitCount, "weapon.trait.chain.secondaryHitCount");
            AssertNear(context, 86.4f, chain.secondaryDamage, "weapon.trait.chain.secondaryDamage");

            actor.SpawnActors(TotemMapService.BuildLayout(seed: 3109, themeId: 1), new TotemStartupSelection(), createObjects: false);
            player = actor.Player;
            enemies = actor.Actors
                .Where(item => item.Kind == TotemActorKind.SmartAi || item.Kind == TotemActorKind.LightAi)
                .Take(6)
                .ToArray();
            player.Position = Vector3.zero;
            enemies[0].Position = new Vector3(2f, 0.5f, 0f);
            enemies[1].Position = new Vector3(2.5f, 0.5f, 0.5f);
            enemies[2].Position = new Vector3(3.0f, 0.5f, -0.5f);
            enemies[3].Position = new Vector3(8f, 0.5f, 0f);
            weapon.EquipWeapon(player, "hammer_heavy");
            var explosiveFire = weapon.FireWeapon(player, enemies[0], true, 1f);
            context.Assert(explosiveFire.Fired, "Charged hammer should fire for Explosive diagnostics.");
            context.AssertEqual("trait_explosive", explosiveFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.explosive.id");
            var explosive = weapon.ApplyTraitEffect(explosiveFire, player, enemies[0], false);
            context.Assert(explosive.applied, $"Explosive trait should apply: {explosive.reason}");
            context.AssertEqual(2, explosive.secondaryHitCount, "weapon.trait.explosive.secondaryHitCount");
            AssertNear(context, 115.2f, explosive.secondaryDamage, "weapon.trait.explosive.secondaryDamage");

            actor.SpawnActors(TotemMapService.BuildLayout(seed: 3110, themeId: 1), new TotemStartupSelection(), createObjects: false);
            player = actor.Player;
            enemies = actor.Actors
                .Where(item => item.Kind == TotemActorKind.SmartAi || item.Kind == TotemActorKind.LightAi)
                .Take(6)
                .ToArray();
            player.Position = Vector3.zero;
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Position = new Vector3(2f + i, 0.5f, i % 2 == 0 ? 0.75f : -0.75f);
            }

            weapon.EquipWeapon(player, "pistol_basic");
            var multiShotFire = weapon.FireWeapon(player, enemies[0], true, 1f);
            context.Assert(multiShotFire.Fired, "Charged pistol should fire for MultiShot diagnostics.");
            context.AssertEqual("trait_multishot", multiShotFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.multishot.id");
            var multiShot = weapon.ApplyTraitEffect(multiShotFire, player, enemies[0], false);
            context.Assert(multiShot.applied, $"MultiShot trait should apply: {multiShot.reason}");
            context.AssertEqual(2, multiShot.extraProjectileCount, "weapon.trait.multishot.extraProjectileCount");
            context.AssertEqual(2, multiShot.secondaryHitCount, "weapon.trait.multishot.secondaryHitCount");
            AssertNear(context, 44.8f, multiShot.secondaryDamage, "weapon.trait.multishot.secondaryDamage");

            actor.SpawnActors(TotemMapService.BuildLayout(seed: 3111, themeId: 1), new TotemStartupSelection(), createObjects: false);
            player = actor.Player;
            enemies = actor.Actors
                .Where(item => item.Kind == TotemActorKind.SmartAi || item.Kind == TotemActorKind.LightAi)
                .Take(2)
                .ToArray();
            player.Position = Vector3.zero;
            enemies[0].Position = new Vector3(3f, 0f, 0f);
            weapon.EquipWeapon(player, "energy_fist");
            var pullFire = weapon.FireWeapon(player, enemies[0], true, 1f);
            context.Assert(pullFire.Fired, "Charged energy fist should fire for Pull diagnostics.");
            context.AssertEqual("trait_pull", pullFire.ActiveTrait?.TraitId ?? string.Empty, "weapon.trait.pull.id");
            float beforePullDistance = Vector3.Distance(player.Position, enemies[0].Position);
            var pull = weapon.ApplyTraitEffect(pullFire, player, enemies[0], false);
            context.Assert(pull.applied, $"Pull trait should apply: {pull.reason}");
            AssertNear(context, 1.5f, pull.displacement, "weapon.trait.pull.displacement");
            AssertNear(context, beforePullDistance - 1.5f, Vector3.Distance(player.Position, enemies[0].Position), "weapon.trait.pull.remainingDistance");
        }

        private static void CheckWeaponDropAndPickupRuntime(GFDiagnosticScenarioContext context)
        {
            context.Assert(TotemDataService.TryLoadGameplayCatalogFromFile(TotemDataService.GetGameplayCatalogPath(), out var catalog, out string error), $"Gameplay catalog should load for weapon drop diagnostics: {error}");
            var drops = catalog.CreateWeaponDropDefinitions();
            context.AssertEqual(15, drops.Length, "weaponDrop.count");
            context.AssertEqual(5, drops.Count(drop => drop.DropSource == "Elite"), "weaponDrop.eliteCount");
            context.AssertEqual(5, drops.Count(drop => drop.DropSource == "Chest"), "weaponDrop.chestCount");
            context.AssertEqual(5, drops.Count(drop => drop.DropSource == "Merchant"), "weaponDrop.merchantCount");

            var runtimeObject = new GameObject("[TotemWeaponPickupDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterWeaponPickupDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var map = runtime.GetService<TotemMapService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var interaction = runtime.GetService<TotemInteractionService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                int mapResourcePickupCount = TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.Resource).Length;
                var mapPickupSnapshot = weapon.CapturePickupSnapshot();
                context.AssertEqual(mapResourcePickupCount, mapPickupSnapshot.mapResourcePickupCount, "weaponPickup.mapResourcePickupCount");
                context.AssertEqual(mapResourcePickupCount, mapPickupSnapshot.activePickupCount, "weaponPickup.mapResourceActiveCount");
                context.AssertEqual(15, weapon.GetRuntimeDropCatalog().Count, "weaponDrop.runtimeCount");
                context.AssertEqual(2, weapon.GetRuntimeProjectileCatalog().Count, "weaponProjectile.runtimeCount");
                context.AssertEqual(10, weapon.GetRuntimeTraitCatalog().Count, "weaponTrait.runtimeCount");
                context.Assert(weapon.TrySelectWeaponDrop("Elite", 1, 0, out string eliteWeapon), "Elite room 1 should select a weapon.");
                context.Assert(!string.IsNullOrWhiteSpace(eliteWeapon), "Elite selected weapon should not be empty.");
                context.Assert(weapon.TrySelectWeaponDrop("Chest", 2, 9, out string chestWeapon), "Chest room 2 should select a weapon.");
                context.Assert(!string.IsNullOrWhiteSpace(chestWeapon), "Chest selected weapon should not be empty.");

                var pickup = weapon.SpawnWeaponPickup("knife_basic", "Diagnostic", player.Position + new Vector3(1f, 0f, 0f));
                context.Assert(pickup != null, "Diagnostic weapon pickup should spawn.");
                interaction.Tick(0.1f);
                var focused = interaction.CaptureSnapshot();
                context.Assert(focused.hasWeaponPickup, "Interaction should focus a nearby weapon pickup.");
                context.AssertEqual(pickup.InstanceId, focused.weaponPickupInstanceId, "weaponPickup.focusId");
                context.AssertEqual(TotemInteractionService.BuildWeaponPickupPrompt(pickup), focused.prompt, "weaponPickup.prompt");
                context.Assert(interaction.TryInteractCurrent(), "Interaction should pick up the focused weapon.");
                context.AssertEqual(2, weapon.GetWeaponLevel(player), "weaponPickup.playerLevelAfterPickup");
                context.AssertEqual(mapResourcePickupCount, weapon.CapturePickupSnapshot().activePickupCount, "weaponPickup.activeAfterPick");

                context.Assert(weapon.TryUpgrade(player, "knife_basic", 50, out _), "Knife should upgrade to level 3 before duplicate conversion.");
                var beforeInventory = economy.CaptureInventory(player);
                var duplicate = weapon.SpawnWeaponPickup("knife_basic", "Diagnostic", player.Position + new Vector3(1f, 0f, 0f));
                context.Assert(duplicate != null, "Duplicate weapon pickup should spawn.");
                interaction.Tick(0.1f);
                context.Assert(interaction.TryInteractCurrent(), "Duplicate max-level pickup should still be consumed.");
                var afterInventory = economy.CaptureInventory(player);
                context.AssertEqual(25, afterInventory.coins - beforeInventory.coins, "weaponPickup.duplicateConvertedGold");
                context.AssertEqual(3, weapon.GetWeaponLevel(player), "weaponPickup.levelAfterDuplicate");
                context.AssertEqual($"weapon_pickup_{duplicate.InstanceId}", interaction.CaptureSnapshot().lastInteraction, "weaponPickup.lastInteraction");

                var smart = actor.Actors.First(item => item.Kind == TotemActorKind.SmartAi && item.IsAlive);
                smart.Position = player.Position + new Vector3(TotemActorService.CoverMeleeBypassDistance * 0.5f, 0f, 0f);
                context.Assert(actor.ApplyDamage(smart, smart.Health + 1f, player, "DiagnosticEliteWeaponDrop"), "Smart AI should die for elite weapon drop.");
                var afterKill = weapon.CapturePickupSnapshot();
                context.Assert(afterKill.activePickupCount > mapResourcePickupCount, "Smart AI death should spawn an Elite weapon pickup.");
                context.Assert(afterKill.spawnedPickupCount >= mapResourcePickupCount + 3, "Weapon pickup spawned counter should include map resources, manual and elite pickups.");
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

        private static void RegisterWeaponPickupDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemChoiceService());
            runtime.RegisterService(new TotemInteractionService());
        }

        private static void CheckChestRuntime(GFDiagnosticScenarioContext context)
        {
            context.Assert(TotemDataService.TryLoadGameplayCatalogFromFile(TotemDataService.GetGameplayCatalogPath(), out var catalog, out string error), $"Gameplay catalog should load for chest diagnostics: {error}");
            var rewards = catalog.CreateChestRewardDefinitions();
            context.AssertEqual(6, rewards.Length, "chest.rewardRowCount");
            context.AssertEqual(100, rewards.Where(item => item.ChestId == "chest_common").Sum(item => item.Probability), "chest.common.probability");
            context.AssertEqual(100, rewards.Where(item => item.ChestId == "chest_rare").Sum(item => item.Probability), "chest.rare.probability");

            var runtimeObject = new GameObject("[TotemChestDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterChestDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var chest = runtime.GetService<TotemChestService>();
                var asset = runtime.GetService<TotemAssetService>();
                var interaction = runtime.GetService<TotemInteractionService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                var snapshot = chest.CaptureSnapshot();
                context.AssertEqual(4, snapshot.activeChestCount, "chest.runtime.activeCount");
                context.AssertEqual(2, snapshot.commonChestCount, "chest.runtime.commonCount");
                context.AssertEqual(2, snapshot.rareChestCount, "chest.runtime.rareCount");
                context.AssertEqual(6, chest.GetRuntimeRewardCatalog().Count, "chest.runtime.rewardRowCount");
                context.AssertEqual(0, asset.MissingEntryCount, "chest.asset.missingEntryCount");
                context.AssertEqual(0, asset.FallbackRequiredCount, "chest.asset.fallbackRequiredCount");
                context.Assert(chest.TrySelectReward("chest_common", 0, out var commonWeaponReward), "Common chest should select a weapon reward for seed 0.");
                context.AssertEqual(TotemChestRewardType.Weapon, commonWeaponReward.RewardType, "chest.select.commonWeapon");
                context.Assert(chest.TrySelectReward("chest_common", 50, out var commonGoldReward), "Common chest should select a gold reward for seed 50.");
                context.AssertEqual(TotemChestRewardType.Gold, commonGoldReward.RewardType, "chest.select.commonGold");
                context.Assert(chest.TrySelectReward("chest_rare", 95, out var rarePotionReward), "Rare chest should select a potion reward for seed 95.");
                context.AssertEqual(TotemChestRewardType.Potion, rarePotionReward.RewardType, "chest.select.rarePotion");

                var commonGoldChest = chest.ActiveChests.First(item => item.ChestId == "chest_common");
                var beforeGold = economy.CaptureInventory(player);
                context.Assert(chest.TryOpenChest(player, commonGoldChest, 50, out var goldResult), "Common chest should open with gold reward.");
                context.AssertEqual(TotemChestRewardType.Gold, goldResult.rewardType, "chest.gold.rewardType");
                context.AssertEqual(45, goldResult.coinsAdded, "chest.gold.coinsAdded");
                context.AssertEqual(beforeGold.coins + 45, economy.CaptureInventory(player).coins, "chest.gold.playerCoins");

                var rarePotionChest = chest.ActiveChests.First(item => item.ChestId == "chest_rare");
                actor.ApplyDamage(player, 60f, null, "DiagnosticChestPotionSetup");
                float healthBeforePotion = player.Health;
                context.Assert(chest.TryOpenChest(player, rarePotionChest, 95, out var potionResult), "Rare chest should open with potion reward.");
                context.AssertEqual(TotemChestRewardType.Potion, potionResult.rewardType, "chest.potion.rewardType");
                AssertNear(context, 50f, potionResult.healAmount, "chest.potion.healAmount");
                AssertNear(context, healthBeforePotion + 50f, player.Health, "chest.potion.playerHealth");

                var weaponChest = chest.SpawnChest("chest_common", player.Position + new Vector3(10f, 0f, 0f), false);
                var pickupBefore = weapon.CapturePickupSnapshot();
                context.Assert(chest.TryOpenChest(player, weaponChest, 0, out var weaponResult), "Common chest should open with weapon reward.");
                context.AssertEqual(TotemChestRewardType.Weapon, weaponResult.rewardType, "chest.weapon.rewardType");
                context.Assert(weaponResult.spawnedWeaponPickupId > 0, "Weapon chest should spawn a pickup.");
                context.AssertEqual(pickupBefore.spawnedPickupCount + 1, weapon.CapturePickupSnapshot().spawnedPickupCount, "chest.weapon.spawnedPickupCount");

                var interactiveChest = chest.SpawnChest("chest_common", player.Position + new Vector3(1f, 0f, 0f), false);
                interaction.Tick(0.1f);
                var focused = interaction.CaptureSnapshot();
                context.Assert(focused.hasChest, "Interaction should focus a nearby unopened chest.");
                context.AssertEqual(interactiveChest.InstanceId, focused.chestInstanceId, "chest.interaction.focusId");
                context.AssertEqual(TotemInteractionService.BuildChestPrompt(interactiveChest), focused.prompt, "chest.interaction.prompt");
                context.Assert(interaction.TryInteractCurrent(), "Interaction should open the focused chest.");
                context.AssertEqual($"chest_{interactiveChest.InstanceId}", interaction.CaptureSnapshot().lastInteraction, "chest.interaction.lastInteraction");
                context.Assert(chest.CaptureSnapshot().openedChestCount >= 4, "Opened chest count should include direct and interaction opens.");
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

        private static void RegisterChestDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemChestService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemChoiceService());
            runtime.RegisterService(new TotemInteractionService());
        }

        private static void CheckSkillAndStatusTiming(GFDiagnosticScenarioContext context)
        {
            var player = NewActor(1, TotemActorKind.Player, 100f);
            var skillService = new TotemSkillService();
            context.Assert(skillService.EquipSkill(player, 0, "skill_fireball_01"), "Fireball skill should equip.");
            context.Assert(skillService.TryCastSlot(player, 0, out var skill), "Default slot0 skill should cast.");
            context.AssertEqual("skill_fireball_01", skill?.SkillId ?? string.Empty, "skill.fireball.skillId");
            AssertNear(context, 7f, skill?.Cooldown ?? -1f, "skill.fireball.cooldown");
            AssertNear(context, 8f / 60f, skill?.Startup ?? -1f, "skill.fireball.startupSec");
            AssertNear(context, 2.4f, skill?.DamageMultiplier ?? -1f, "skill.fireball.damageMul");
            context.AssertEqual(TotemSkillChargeModel.Cooldown, skill?.ChargeModel ?? TotemSkillChargeModel.HoldRelease, "skill.fireball.chargeModel");
            var knife = TotemWeaponService.GetCatalog().FirstOrDefault(item => item.WeaponId == "knife_basic");
            AssertNear(context, 43.2f, TotemSkillService.ResolveSkillDamage(skill, knife, 15f), "skill.fireball.resolvedDamage");
            context.Assert(skillService.GetCooldownRemaining(player, 0) > 0f, "Skill cooldown should start after cast.");

            context.Assert(skillService.EquipSkill(player, 1, "skill_chain_lightning_01"), "Chain Lightning skill should equip.");
            context.AssertEqual(3, skillService.GetCurrentCharges(player, 1), "skill.chain.initialCharges");
            context.Assert(skillService.TryCastSlot(player, 1, out var chain), "Chain Lightning should cast.");
            context.AssertEqual(TotemSkillChargeModel.Charges, chain?.ChargeModel ?? TotemSkillChargeModel.Cooldown, "skill.chain.chargeModel");
            context.AssertEqual(2, skillService.GetCurrentCharges(player, 1), "skill.chain.afterCastCharges");
            skillService.Tick(8f);
            context.AssertEqual(3, skillService.GetCurrentCharges(player, 1), "skill.chain.regeneratedCharges");

            context.Assert(skillService.EquipSkill(player, 1, "skill_stealth_01"), "Stealth hold-release skill should equip.");
            context.Assert(skillService.TryCastSlot(player, 1, out var stealth), "Stealth should cast.");
            context.AssertEqual(TotemSkillChargeModel.HoldRelease, stealth?.ChargeModel ?? TotemSkillChargeModel.Cooldown, "skill.stealth.chargeModel");
            AssertNear(context, 1.5f, stealth?.HoldDuration ?? -1f, "skill.stealth.holdDuration");
            AssertNear(context, 2.3f, skillService.GetCooldownRemaining(player, 1), "skill.stealth.holdCooldown");

            var statusService = new TotemStatusService();
            var target = NewActor(2, TotemActorKind.SmartAi, 100f);
            statusService.ApplyStatus(target, "Burn", 10f, 1.5f);
            var burnSnapshot = statusService.CaptureSnapshot(target);
            context.AssertEqual(1, burnSnapshot.activeCount, "status.snapshot.burn.activeCount");
            context.AssertEqual(1, burnSnapshot.appliedCount, "status.snapshot.burn.appliedCount");
            context.AssertEqual("Burn", burnSnapshot.lastStatusName, "status.snapshot.burn.lastStatus");
            context.AssertEqual("Status: Burn 1.5s", burnSnapshot.summary, "status.snapshot.burn.summary");
            AssertNear(context, 10f, burnSnapshot.totalDps, "status.snapshot.burn.totalDps");
            statusService.Tick(0.3f);
            AssertNear(context, 100f, target.Health, "status.beforeHalfSecond.health");
            statusService.Tick(0.2f);
            AssertNear(context, 95f, target.Health, "status.firstTick.health");
            var tickSnapshot = statusService.CaptureSnapshot(target);
            context.AssertEqual(1, tickSnapshot.tickDamageCount, "status.snapshot.tickDamageCount");

            var poisonService = new TotemStatusService();
            var poisonTarget = NewActor(21, TotemActorKind.LightAi, 100f);
            poisonService.ApplyStatus(poisonTarget, TotemStatusService.PoisonStatus, 6f, 1f);
            var poisonSnapshot = poisonService.CaptureSnapshot(poisonTarget);
            context.AssertEqual(1, poisonSnapshot.activeCount, "status.snapshot.poison.activeCount");
            context.AssertEqual(TotemStatusService.PoisonStatus, poisonSnapshot.lastStatusName, "status.snapshot.poison.lastStatus");
            context.Assert(poisonSnapshot.summary.Contains("Poison 1.0s", System.StringComparison.Ordinal), "Poison should appear in status summary.");
            context.Assert(TotemStatusService.IsDamageStatus(TotemStatusService.PoisonStatus), "Poison should be treated as a damage status.");
            AssertNear(context, 3f, TotemStatusService.ComputeTickDamage(TotemStatusService.PoisonStatus, 6f), "status.poison.tickDamageFormula");
            poisonService.Tick(TotemStatusService.TickInterval);
            AssertNear(context, 97f, poisonTarget.Health, "status.poison.firstTick.health");
            context.AssertEqual(1, poisonService.CaptureSnapshot(poisonTarget).tickDamageCount, "status.poison.tickDamageCount");
            poisonService.Tick(0.6f);
            var poisonExpired = poisonService.CaptureSnapshot(poisonTarget);
            context.AssertEqual(0, poisonExpired.activeCount, "status.poison.expired.activeCount");
            context.AssertEqual(TotemStatusService.PoisonStatus, poisonExpired.lastExpiredStatusName, "status.poison.expired.lastStatus");

            statusService.ApplyStatus(target, "Shock", 8f, 3f);
            statusService.ApplyStatus(target, "Shock", 12f, 1f);
            var shock = statusService.GetActiveStatuses(target).FirstOrDefault(item => item.StatusName == "Shock");
            context.Assert(shock != null, "Shock should be active.");
            AssertNear(context, 12f, shock.DPS, "status.shock.mergedDps");
            AssertNear(context, 3f, shock.RemainingSec, "status.shock.mergedDuration");
            context.Assert(TotemStatusService.IsDamageStatus(TotemStatusService.ShockStatus), "Shock should remain a damage status after refresh.");
            var shockSnapshot = statusService.CaptureSnapshot(target);
            context.AssertEqual(3, shockSnapshot.appliedCount, "status.snapshot.shock.appliedCount");
            context.AssertEqual("Shock", shockSnapshot.lastStatusName, "status.snapshot.shock.lastStatus");
            context.Assert(shockSnapshot.summary.Contains("Shock 3.0s", System.StringComparison.Ordinal), "Status snapshot summary should include refreshed Shock.");

            var refreshService = new TotemStatusService();
            var refreshTarget = NewActor(22, TotemActorKind.LightAi, 100f);
            refreshService.ApplyStatus(refreshTarget, TotemStatusService.ShockStatus, 12f, 3f);
            refreshService.ApplyStatus(refreshTarget, TotemStatusService.ShockStatus, 5f, 5f);
            var lowerDpsRefresh = refreshService.GetActiveStatuses(refreshTarget).FirstOrDefault(item => item.StatusName == TotemStatusService.ShockStatus);
            context.Assert(lowerDpsRefresh != null, "Lower-DPS Shock refresh should keep one active status.");
            AssertNear(context, 12f, lowerDpsRefresh?.DPS ?? -1f, "status.shock.lowerRefreshDps");
            AssertNear(context, 5f, lowerDpsRefresh?.RemainingSec ?? -1f, "status.shock.lowerRefreshDuration");
            context.AssertEqual(2, refreshService.CaptureSnapshot(refreshTarget).appliedCount, "status.shock.lowerRefreshAppliedCount");

            var invalidService = new TotemStatusService();
            var invalidTarget = NewActor(23, TotemActorKind.LightAi, 100f);
            invalidService.ApplyStatus(null, TotemStatusService.StunStatus, 5f, 2f);
            invalidService.ApplyStatus(invalidTarget, string.Empty, 5f, 2f);
            invalidService.ApplyStatus(invalidTarget, TotemStatusService.BurnStatus, 5f, 0f);
            invalidService.ClearAllStatuses(null);
            var invalidSnapshot = invalidService.CaptureSnapshot(invalidTarget);
            context.AssertEqual(0, invalidSnapshot.activeCount, "status.invalid.activeCount");
            context.AssertEqual(0, invalidSnapshot.appliedCount, "status.invalid.appliedCount");
            context.AssertEqual(0, invalidSnapshot.expiredCount, "status.invalid.expiredCount");

            statusService.Tick(1.1f);
            context.AssertEqual("Burn", statusService.CaptureSnapshot(target).lastExpiredStatusName, "status.snapshot.burn.expiredLastStatus");
            statusService.Tick(2.1f);
            var expiredSnapshot = statusService.CaptureSnapshot(target);
            context.Assert(expiredSnapshot.expiredCount >= 1, "Status expiration should be counted.");
            context.AssertEqual("Shock", expiredSnapshot.lastExpiredStatusName, "status.snapshot.expired.lastStatus");
            statusService.ClearAllStatuses(target);
            context.AssertEqual(0, statusService.GetActiveStatuses(target).Count, "status.clear.count");
            context.AssertEqual("Status: None", TotemStatusService.FormatStatusSummary(statusService.CaptureSnapshot(target)), "status.snapshot.emptySummary");

            var clearTarget = NewActor(20, TotemActorKind.LightAi, 100f);
            statusService.ApplyStatus(clearTarget, TotemStatusService.SlowStatus, 0.3f, 5f);
            statusService.ApplyStatus(clearTarget, TotemStatusService.BurnStatus, 8f, 3f);
            int clearExpiredBefore = statusService.CaptureSnapshot(clearTarget).expiredCount;
            statusService.ClearAllStatuses(clearTarget);
            var clearedSnapshot = statusService.CaptureSnapshot(clearTarget);
            context.AssertEqual(0, clearedSnapshot.activeCount, "status.clearAll.activeCount");
            context.AssertEqual(clearExpiredBefore + 2, clearedSnapshot.expiredCount, "status.clearAll.expiredCount");
            context.AssertEqual(TotemStatusService.BurnStatus, clearedSnapshot.lastExpiredStatusName, "status.clearAll.lastExpired");

            var controlTarget = NewActor(3, TotemActorKind.LightAi, 100f);
            statusService.ApplyStatus(controlTarget, TotemStatusService.SlowStatus, 0.4f, 1f);
            context.Assert(statusService.HasStatus(controlTarget, TotemStatusService.SlowStatus), "status.control.slow.active");
            AssertNear(context, 0.6f, statusService.GetMoveSpeedMultiplier(controlTarget), "status.control.slow.moveMultiplier");
            int tickDamageBeforeSlow = statusService.CaptureSnapshot(controlTarget).tickDamageCount;
            statusService.Tick(TotemStatusService.TickInterval);
            AssertNear(context, 100f, controlTarget.Health, "status.control.slow.noDamage");
            context.AssertEqual(tickDamageBeforeSlow, statusService.CaptureSnapshot(controlTarget).tickDamageCount, "status.control.slow.noTickDamage");
            statusService.ApplyStatus(controlTarget, TotemStatusService.StunStatus, 0f, 1f);
            context.Assert(!statusService.CanAct(controlTarget), "status.control.stun.blocksAct");
            context.Assert(!statusService.CanMove(controlTarget), "status.control.stun.blocksMove");
            AssertNear(context, 0f, statusService.GetMoveSpeedMultiplier(controlTarget), "status.control.stun.moveMultiplier");
        }

        private static void CheckStatusAndTattooDamageRouting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemDamageRouteDiagnosticRuntime]");
            GameObject statusTargetObject = null;
            GameObject tattooTargetObject = null;
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterDamageRoutingDiagnosticServices(runtime);
                runtime.StartRuntime();

                var actorService = runtime.GetService<TotemActorService>();
                var statusService = runtime.GetService<TotemStatusService>();
                var tattooService = runtime.GetService<TotemTattooService>();
                int eventCount = 0;
                int detailedEventCount = 0;
                int killedCount = 0;
                float lastDamage = 0f;
                TotemDamageRecord lastRecord = default;
                actorService.DamageApplied += (_, damage, killed) =>
                {
                    eventCount++;
                    lastDamage = damage;
                    if (killed)
                    {
                        killedCount++;
                    }
                };
                actorService.DamageResolved += record =>
                {
                    detailedEventCount++;
                    lastRecord = record;
                };

                var statusTarget = NewActor(901, TotemActorKind.LightAi, 10f);
                statusTargetObject = new GameObject("[StatusDamageTarget]");
                statusTarget.GameObject = statusTargetObject;
                statusService.ApplyStatus(statusTarget, "Burn", 20f, 1.5f);
                statusService.Tick(TotemStatusService.TickInterval);
                context.AssertEqual(1, eventCount, "damageRoute.status.eventCount");
                context.AssertEqual(1, detailedEventCount, "damageRoute.status.detailedEventCount");
                context.AssertEqual(1, killedCount, "damageRoute.status.killedCount");
                AssertNear(context, 10f, lastDamage, "damageRoute.status.damage");
                context.AssertEqual(1, lastRecord.Sequence, "damageRoute.status.sequence");
                context.AssertEqual("Status:Burn", lastRecord.Reason, "damageRoute.status.reason");
                context.Assert(ReferenceEquals(statusTarget, lastRecord.Target), "Status damage record should point to the damaged target.");
                context.Assert(lastRecord.Source == null, "Direct status damage should allow a null source.");
                context.Assert(!statusTarget.IsAlive, "Status damage should kill low-health target through actor service.");
                context.Assert(statusTarget.AnimationDead, "Status-killed target should enter actor death animation state.");
                context.Assert(statusTargetObject.activeSelf, "Status-killed target object should remain visible during death animation delay.");
                context.AssertEqual(0, statusService.GetActiveStatuses(statusTarget).Count, "damageRoute.status.remaining");
                actorService.Tick(1f);
                context.Assert(!statusTargetObject.activeSelf, "Status-killed target object should hide after death animation delay.");

                eventCount = 0;
                detailedEventCount = 0;
                killedCount = 0;
                lastDamage = 0f;
                lastRecord = default;
                var source = NewActor(900, TotemActorKind.Player, 100f);
                var tattooTarget = NewActor(902, TotemActorKind.SmartAi, 5f);
                tattooTargetObject = new GameObject("[TattooDamageTarget]");
                tattooTarget.GameObject = tattooTargetObject;
                context.Assert(tattooService.Equip(4, 1, 1), "RightArm Red Line tattoo should equip for damage routing.");
                var results = tattooService.Trigger("AttackHitEvent", source, tattooTarget, 10f);
                context.AssertEqual(1, results.Length, "damageRoute.tattoo.resultCount");
                context.AssertEqual(1, eventCount, "damageRoute.tattoo.eventCount");
                context.AssertEqual(1, detailedEventCount, "damageRoute.tattoo.detailedEventCount");
                context.AssertEqual(1, killedCount, "damageRoute.tattoo.killedCount");
                AssertNear(context, 10f, lastDamage, "damageRoute.tattoo.damage");
                context.AssertEqual(2, lastRecord.Sequence, "damageRoute.tattoo.sequence");
                context.AssertEqual("Tattoo:AttackHitEvent", lastRecord.Reason, "damageRoute.tattoo.reason");
                context.Assert(ReferenceEquals(source, lastRecord.Source), "Tattoo damage record should point to the source actor.");
                context.Assert(ReferenceEquals(tattooTarget, lastRecord.Target), "Tattoo damage record should point to the damaged target.");
                context.Assert(!tattooTarget.IsAlive, "Tattoo damage should kill low-health target through actor service.");
                context.Assert(tattooTarget.AnimationDead, "Tattoo-killed target should enter actor death animation state.");
                context.Assert(tattooTargetObject.activeSelf, "Tattoo-killed target object should remain visible during death animation delay.");
                actorService.Tick(1f);
                context.Assert(!tattooTargetObject.activeSelf, "Tattoo-killed target object should hide after death animation delay.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                if (statusTargetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(statusTargetObject);
                }

                if (tattooTargetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(tattooTargetObject);
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckSelfTattooInterruption(GFDiagnosticScenarioContext context)
        {
            var standalone = new TotemTattooService();
            context.Assert(standalone.StartSelfTattoo(2, 3, 4), "Standalone self tattoo should start before movement interruption.");
            standalone.Trigger("MoveTickEvent", NewActor(501, TotemActorKind.Player, 100f), null, 0.25f);
            var moved = standalone.CaptureSnapshot();
            context.Assert(!moved.selfTattooInProgress, "Player self tattoo should cancel when moving.");
            context.AssertEqual(1, moved.selfTattooCancelledCount, "selfTattoo.interrupt.move.cancelCount");
            context.AssertEqual("Moved", moved.lastSelfTattooCancelReason, "selfTattoo.interrupt.move.reason");

            var actorStandalone = new TotemTattooService();
            var smart = NewActor(502, TotemActorKind.SmartAi, 100f);
            context.Assert(actorStandalone.StartSelfTattoo(smart, 4, 1, 1), "Actor self tattoo should start before movement interruption.");
            actorStandalone.Trigger("MoveTickEvent", smart, null, 0.5f);
            var actorMoved = actorStandalone.CaptureSnapshot(smart);
            context.Assert(!actorMoved.selfTattooInProgress, "Actor self tattoo should cancel when moving.");
            context.AssertEqual(1, actorMoved.selfTattooCancelledCount, "actorSelfTattoo.interrupt.move.cancelCount");
            context.AssertEqual("Moved", actorMoved.lastSelfTattooCancelReason, "actorSelfTattoo.interrupt.move.reason");
            context.AssertEqual(1, actorStandalone.CaptureSnapshot().actorSelfTattooCancelledCount, "actorSelfTattoo.interrupt.move.aggregateCancelCount");

            var runtimeObject = new GameObject("[TotemSelfTattooInterruptDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterDamageRoutingDiagnosticServices(runtime);
                runtime.StartRuntime();

                var actorService = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var tattoo = runtime.GetService<TotemTattooService>();
                var player = NewActor(503, TotemActorKind.Player, 100f);
                var attacker = NewActor(504, TotemActorKind.SmartAi, 100f);
                economy.RegisterActor(player);
                economy.AddCoins(player, 100);
                context.Assert(tattoo.StartSelfTattoo(player, 4, 1, 1), "Runtime actor self tattoo should start before manual cancellation.");
                context.Assert(tattoo.CancelSelfTattoo(player), "Manual actor self tattoo cancellation should succeed.");
                context.AssertEqual(80, economy.CaptureInventory(player).coins, "selfTattoo.cancel.manual.depositCoins");
                context.AssertEqual(1, economy.SelfTattooInterruptPenaltyCount, "selfTattoo.cancel.manual.penaltyCount");
                context.AssertEqual(20, economy.LastSelfTattooInterruptPenaltyAmount, "selfTattoo.cancel.manual.depositAmount");
                context.AssertEqual(player.ActorId, economy.LastSelfTattooInterruptPenaltyActorId, "selfTattoo.cancel.manual.penaltyActor");
                context.AssertEqual("Manual", economy.LastSelfTattooInterruptPenaltyReason, "selfTattoo.cancel.manual.penaltyReason");

                economy.AddCoins(player, 40);
                context.Assert(tattoo.StartSelfTattoo(2, 3, 4), "Runtime player self tattoo should start before damage interruption.");
                actorService.ApplyDamage(player, 5f, attacker, "DiagnosticSelfTattooDamage");
                AssertNear(context, 95f, player.Health, "selfTattoo.interrupt.damage.playerHealth");
                var damaged = tattoo.CaptureSnapshot();
                context.Assert(!damaged.selfTattooInProgress, "Player self tattoo should cancel when damaged through actor service.");
                context.AssertEqual(1, damaged.selfTattooCancelledCount, "selfTattoo.interrupt.damage.cancelCount");
                context.AssertEqual("Damaged", damaged.lastSelfTattooCancelReason, "selfTattoo.interrupt.damage.reason");
                context.AssertEqual(70, economy.CaptureInventory(player).coins, "selfTattoo.interrupt.damage.penaltyCoins");
                context.AssertEqual(2, economy.SelfTattooInterruptPenaltyCount, "selfTattoo.interrupt.damage.penaltyCount");
                context.AssertEqual(TotemEconomyService.SelfTattooInterruptPenalty, economy.LastSelfTattooInterruptPenaltyAmount, "selfTattoo.interrupt.damage.penaltyAmount");
                context.AssertEqual(player.ActorId, economy.LastSelfTattooInterruptPenaltyActorId, "selfTattoo.interrupt.damage.penaltyActor");
                context.AssertEqual("Damaged", economy.LastSelfTattooInterruptPenaltyReason, "selfTattoo.interrupt.damage.penaltyReason");

                var readingAi = NewActor(505, TotemActorKind.SmartAi, 100f);
                economy.RegisterActor(readingAi);
                economy.AddCoins(readingAi, 40);
                context.Assert(tattoo.StartSelfTattoo(readingAi, 4, 1, 1), "Runtime actor self tattoo should start before damage interruption.");
                actorService.ApplyDamage(readingAi, 6f, player, "DiagnosticActorSelfTattooDamage");
                AssertNear(context, 94f, readingAi.Health, "actorSelfTattoo.interrupt.damage.actorHealth");
                var actorDamaged = tattoo.CaptureSnapshot(readingAi);
                context.Assert(!actorDamaged.selfTattooInProgress, "Actor self tattoo should cancel when damaged through actor service.");
                context.AssertEqual(1, actorDamaged.selfTattooCancelledCount, "actorSelfTattoo.interrupt.damage.cancelCount");
                context.AssertEqual("Damaged", actorDamaged.lastSelfTattooCancelReason, "actorSelfTattoo.interrupt.damage.reason");
                context.AssertEqual(0, economy.CaptureInventory(readingAi).coins, "actorSelfTattoo.interrupt.damage.penaltyFloor");
                context.AssertEqual(3, economy.SelfTattooInterruptPenaltyCount, "actorSelfTattoo.interrupt.damage.penaltyCount");
                context.AssertEqual(40, economy.LastSelfTattooInterruptPenaltyAmount, "actorSelfTattoo.interrupt.damage.penaltyAmount");
                context.AssertEqual(readingAi.ActorId, economy.LastSelfTattooInterruptPenaltyActorId, "actorSelfTattoo.interrupt.damage.penaltyActor");
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

        private static void RegisterDamageRoutingDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
        }

        private static void CheckDeathChestRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemDeathChestDiagnosticRuntime]");
            GameObject victimObject = null;
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterDeathChestDiagnosticServices(runtime);
                runtime.StartRuntime();

                var actorService = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var victim = NewActor(920, TotemActorKind.SmartAi, 5f);
                var looter = NewActor(921, TotemActorKind.Player, 100f);
                victimObject = new GameObject("[DeathChestVictim]");
                victim.GameObject = victimObject;

                economy.RegisterActor(victim);
                economy.RegisterActor(looter);
                economy.AddCoins(victim, 100);
                economy.AddInk(victim, 5);
                economy.AddRecipeShards(victim, 3);
                economy.AddEquipment(victim, 2);

                context.Assert(actorService.ApplyDamage(victim, 10f, looter, "DiagnosticKill"), "Death chest victim should be killed through actor service.");
                context.Assert(victim.AnimationDead, "Death chest victim should enter actor death animation state.");
                context.Assert(victimObject.activeSelf, "Death chest victim object should remain visible during death animation delay.");
                context.AssertEqual(1, economy.PendingDeathChestCount, "deathChest.pendingCount.afterKill");
                context.Assert(economy.TryGetPendingDeathChest(victim, out var pending), "Death chest should be pending for the killed actor.");
                actorService.Tick(1f);
                context.Assert(!victimObject.activeSelf, "Death chest victim object should hide after death animation delay.");
                context.AssertEqual(victim.ActorId, pending.deadActorId, "deathChest.deadActorId");
                context.AssertEqual(50, pending.coins, "deathChest.pending.coins");
                context.AssertEqual(2, pending.inkBottleCount, "deathChest.pending.ink");
                context.AssertEqual(1, pending.recipeCopyCount, "deathChest.pending.recipes");
                context.AssertEqual(2, pending.equipmentCount, "deathChest.pending.equipment");

                var victimInventory = economy.CaptureInventory(victim);
                context.AssertEqual(50, victimInventory.coins, "deathChest.victim.remainingCoins");
                context.AssertEqual(3, victimInventory.inkBottleCount, "deathChest.victim.remainingInk");
                context.AssertEqual(2, victimInventory.recipeShardCount, "deathChest.victim.remainingRecipes");
                context.AssertEqual(0, victimInventory.equipmentCount, "deathChest.victim.remainingEquipment");

                context.Assert(economy.TryLootDeathChest(looter, victim, out var looted), "Looter should transfer the pending death chest.");
                context.AssertEqual(0, economy.PendingDeathChestCount, "deathChest.pendingCount.afterLoot");
                context.AssertEqual(50, looted.coins, "deathChest.looted.coins");
                var looterInventory = economy.CaptureInventory(looter);
                context.AssertEqual(50, looterInventory.coins, "deathChest.looter.coins");
                context.AssertEqual(2, looterInventory.inkBottleCount, "deathChest.looter.ink");
                context.AssertEqual(1, looterInventory.recipeShardCount, "deathChest.looter.recipes");
                context.AssertEqual(2, looterInventory.equipmentCount, "deathChest.looter.equipment");
                context.Assert(!economy.TryLootDeathChest(looter, victim, out _), "Death chest should not be lootable twice.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                if (victimObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(victimObject);
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterDeathChestDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
        }

        private static void CheckDeathChestInteractionRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemDeathChestInteractionDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterDeathChestInteractionDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actorService = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var interaction = runtime.GetService<TotemInteractionService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actorService.Player;
                var victim = actorService.Actors.First(actor => actor.Kind == TotemActorKind.LightAi);
                victim.Position = player.Position + new Vector3(1f, 0f, 0f);
                if (victim.GameObject != null)
                {
                    victim.GameObject.transform.position = victim.Position;
                }

                var before = economy.CaptureInventory(player);
                economy.AddCoins(victim, 80);
                economy.AddInk(victim, 4);
                economy.AddRecipeShards(victim, 2);
                economy.AddEquipment(victim, 1);
                context.Assert(actorService.ApplyDamage(victim, victim.Health + 1f, player, "DiagnosticChestInteractKill"), "Interaction chest victim should die.");

                interaction.Tick(0.1f);
                var focused = interaction.CaptureSnapshot();
                context.Assert(focused.hasDeathChest, "Interaction should focus a nearby pending death chest.");
                context.AssertEqual(victim.ActorId, focused.deathChestActorId, "deathChest.interaction.focusActorId");
                context.AssertEqual(TotemInteractionService.BuildDeathChestPrompt(victim), focused.prompt, "deathChest.interaction.prompt");
                context.Assert(interaction.TryInteractCurrent(), "Interaction should loot the focused death chest.");
                context.AssertEqual(0, economy.PendingDeathChestCount, "deathChest.interaction.pendingAfterLoot");

                var after = economy.CaptureInventory(player);
                context.AssertEqual(before.coins + 40, after.coins, "deathChest.interaction.playerCoins");
                context.AssertEqual(before.inkBottleCount + 2, after.inkBottleCount, "deathChest.interaction.playerInk");
                context.AssertEqual(before.recipeShardCount + 1, after.recipeShardCount, "deathChest.interaction.playerRecipes");
                context.AssertEqual(before.equipmentCount + 1, after.equipmentCount, "deathChest.interaction.playerEquipment");

                var looted = interaction.CaptureSnapshot();
                context.AssertEqual($"death_chest_{victim.ActorId}", looted.lastInteraction, "deathChest.interaction.lastInteraction");
                interaction.Tick(0.1f);
                context.Assert(!interaction.CaptureSnapshot().hasDeathChest, "Looted death chest should no longer be focused.");
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

        private static void RegisterDeathChestInteractionDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemChoiceService());
            runtime.RegisterService(new TotemInteractionService());
        }

        private static void CheckZoneAndBossContracts(GFDiagnosticScenarioContext context)
        {
            var phases = TotemZoneService.GetPhases();
            context.AssertEqual(3, phases.Count, "zone.phaseCount");
            context.AssertEqual(0, TotemZoneService.GetPhaseAt(0f).Id, "zone.phaseAt0");
            context.AssertEqual(1, TotemZoneService.GetPhaseAt(180f).Id, "zone.phaseAt180");
            context.AssertEqual(2, TotemZoneService.GetPhaseAt(540f).Id, "zone.phaseAt540");
            AssertNear(context, 200f, TotemZoneService.ComputeRadius(0f, 400f), "zone.radiusAt0");
            AssertNear(context, 65f, TotemZoneService.ComputeRadius(180f, 400f), "zone.radiusAt180");
            AssertNear(context, 35f, TotemZoneService.ComputeRadius(540f, 400f), "zone.radiusAt540");
            AssertNear(context, 5f, TotemZoneService.ComputeRadius(900f, 400f), "zone.radiusAt900");
            AssertNear(context, 12f, phases[2].OutZoneDamage, "zone.phase2.outDamage");
            context.AssertEqual("Fixed", phases[2].CenterOffsetMode, "zone.phase2.offsetMode");

            var bossPhases = TotemBossService.GetPhases();
            context.AssertEqual(3, bossPhases.Count, "boss.phaseCount");
            context.AssertEqual(1, TotemBossService.ResolvePhaseByHpRatio(0.9f), "boss.phaseAt90");
            context.AssertEqual(2, TotemBossService.ResolvePhaseByHpRatio(0.6f), "boss.phaseAt60");
            context.AssertEqual(3, TotemBossService.ResolvePhaseByHpRatio(0.3f), "boss.phaseAt30");
            AssertNear(context, 0.8f, TotemBossService.TransitionDuration, "boss.transitionDuration");
            AssertNear(context, 4f, TotemBossService.SkillCooldown, "boss.skillCooldown");
            context.AssertEqual("enemy_ai_ruins_boss_01", bossPhases[0].BossId, "boss.phase1.bossId");
            context.AssertEqual("skill_stomp,skill_beam", bossPhases[0].SkillIds, "boss.phase1.skills");
            context.AssertEqual("skill_summon", bossPhases[1].SkillIds, "boss.phase2.skills");
            context.AssertEqual("skill_enrage_aoe", bossPhases[2].SkillIds, "boss.phase3.skills");
            context.Assert(TotemSkillService.TryGetDefinition("skill_stomp", out _), "Boss phase skill_stomp should resolve through SkillService.");
            context.Assert(TotemSkillService.TryGetDefinition("skill_beam", out _), "Boss phase skill_beam should resolve through SkillService.");
            context.Assert(TotemSkillService.TryGetDefinition("skill_summon", out _), "Boss phase skill_summon should resolve through SkillService.");
            context.Assert(TotemSkillService.TryGetDefinition("skill_enrage_aoe", out _), "Boss phase skill_enrage_aoe should resolve through SkillService.");
            AssertNear(context, 1.35f, bossPhases[2].EnrageMultiplier, "boss.phase3.enrage");
            context.AssertEqual("vfx_boss_phase3", bossPhases[2].PhaseVFXId, "boss.phase3.vfx");
            context.AssertEqual("bgm_boss_phase3", bossPhases[2].PhaseBGMCueId, "boss.phase3.bgm");
            context.AssertEqual("recipe_ai_ruins_boss", bossPhases[2].DeathPatternRecipeId, "boss.phase3.recipe");
        }

        private static void CheckZoneDamageRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemZoneDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterZoneDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var mapService = runtime.GetService<TotemMapService>();
                var actorService = runtime.GetService<TotemActorService>();
                var zoneService = runtime.GetService<TotemZoneService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var map = mapService.CurrentMap;
                var center = new Vector3(map.InitialZoneCenter.x, 0.5f, map.InitialZoneCenter.y);
                var player = actorService.Player;
                var smart = actorService.Actors.First(actor => actor.Kind == TotemActorKind.SmartAi);
                var light = actorService.Actors.First(actor => actor.Kind == TotemActorKind.LightAi);
                var boss = actorService.Boss;

                player.Position = new Vector3(-100f, 0.5f, -100f);
                smart.Position = center;
                light.Position = new Vector3(-101f, 0.5f, -100f);
                boss.Position = new Vector3(-102f, 0.5f, -100f);
                light.ApplyDamage(light.Health - 0.5f);

                int eventCount = 0;
                int killedCount = 0;
                actorService.DamageApplied += (_, _, killed) =>
                {
                    eventCount++;
                    if (killed)
                    {
                        killedCount++;
                    }
                };

                float playerHpBefore = player.Health;
                float smartHpBefore = smart.Health;
                float bossHpBefore = boss.Health;
                zoneService.Tick(1f);
                var snapshot = zoneService.CaptureSnapshot();

                context.Assert(player.Health < playerHpBefore, "Player outside zone should receive out-zone damage.");
                AssertNear(context, smartHpBefore, smart.Health, "zone.insideSmart.health");
                context.AssertEqual(0, snapshot.currentPhaseId, "zone.runtime.phaseAtStart");
                AssertNear(context, 2f, snapshot.outZoneDamage, "zone.runtime.outDamageAtStart");
                context.Assert(!light.IsAlive, "Low-health Light AI outside zone should be killed by out-zone damage.");
                context.Assert(light.AnimationDead, "Killed out-zone actor should enter actor death animation state.");
                context.Assert(light.GameObject == null || light.GameObject.activeSelf, "Killed out-zone actor object should remain visible during death animation delay.");
                context.Assert(boss.Health < bossHpBefore, "Boss outside zone should follow the same out-zone damage contract.");
                context.AssertEqual(3, snapshot.outZoneAffectedActorCount, "zone.damage.affectedCount");
                context.AssertEqual(1, snapshot.outZoneKilledActorCount, "zone.damage.killedCount");
                context.Assert(snapshot.lastOutZoneDamageTick > 0f, "Zone snapshot should report last tick damage.");
                context.Assert(snapshot.totalOutZoneDamage >= snapshot.lastOutZoneDamageTick, "Zone snapshot should accumulate total damage.");
                context.AssertEqual(3, eventCount, "zone.damage.eventCount");
                context.AssertEqual(1, killedCount, "zone.damage.eventKilledCount");
                actorService.Tick(1f);
                context.Assert(light.GameObject == null || !light.GameObject.activeSelf, "Killed out-zone actor object should hide after death animation delay.");
                context.Assert(!zoneService.IsInsideCurrentZone(player.Position), "Player position should be outside current zone.");
                context.Assert(zoneService.IsInsideCurrentZone(smart.Position), "Smart AI position should be inside current zone.");
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

        private static void RegisterZoneDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemZoneService());
        }

        private static void CheckBossRewardRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemBossRewardDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            string directory = Path.Combine(Path.GetTempPath(), "totem-warrior-diagnostics");
            string statsFileName = Path.Combine(directory, "boss-reward-run-stats.json");
            string statsBackupFile = statsFileName + ".bak";
            string statsTempFile = statsFileName + ".tmp";
            try
            {
                DeleteIfExists(statsFileName);
                DeleteIfExists(statsBackupFile);
                DeleteIfExists(statsTempFile);

                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterBossRewardDiagnosticServices(runtime, statsFileName);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actorService = runtime.GetService<TotemActorService>();
                var bossService = runtime.GetService<TotemBossService>();
                var economyService = runtime.GetService<TotemEconomyService>();
                var combatService = runtime.GetService<TotemCombatService>();
                var skillService = runtime.GetService<TotemSkillService>();
                var runStatsService = runtime.GetService<TotemRunStatsService>();
                var uiService = runtime.GetService<TotemUIService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var boss = actorService.Boss;
                var player = actorService.Player;
                var beforeInventory = economyService.CaptureInventory(player);
                string recipeId = bossService.CaptureSnapshot().deathPatternRecipeId;
                context.Assert(!string.IsNullOrWhiteSpace(recipeId), "Boss reward runtime should expose a death recipe id.");
                context.Assert(bossService.CanUseSkill(out string firstBossSkill), "Boss should expose its first phase skill.");
                context.AssertEqual("skill_stomp", firstBossSkill, "bossReward.phase1.firstSkill");
                context.Assert(skillService.TryGetRuntimeDefinition(firstBossSkill, out _), "Boss first skill should resolve in runtime SkillService.");
                bossService.Tick(TotemBossService.SkillCooldown + 0.1f);
                context.Assert(bossService.CanUseSkill(out string secondBossSkill), "Boss should rotate to its second phase skill after cooldown.");
                context.AssertEqual("skill_beam", secondBossSkill, "bossReward.phase1.secondSkill");
                context.Assert(skillService.TryGetRuntimeDefinition(secondBossSkill, out _), "Boss second skill should resolve in runtime SkillService.");

                actorService.ApplyDamage(boss, boss.MaxHealth * 0.75f);
                bossService.EvaluateBossHealth();
                context.AssertEqual(2, bossService.CurrentPhase, "bossReward.phaseAfterHeavyDamage");
                bossService.Tick(TotemBossService.TransitionDuration + 0.1f);
                bossService.EvaluateBossHealth();
                context.AssertEqual(3, bossService.CurrentPhase, "bossReward.phaseAfterTransition");

                var actors = actorService.Actors.ToArray();
                for (int i = 0; i < actors.Length; i++)
                {
                    var actor = actors[i];
                    if (TotemActorService.IsEnemy(actor) && actor.IsAlive)
                    {
                        actorService.ApplyDamage(actor, actor.Health + actor.MaxHealth + 1f);
                    }
                }

                combatService.Tick(0.1f);
                var result = combatService.LastRunResult;
                context.Assert(result != null, "Combat should build a run result after all enemies are dead.");
                context.Assert(result.win, "Boss reward runtime should end in victory.");
                context.AssertEqual("AllEnemiesDefeated", result.reason, "bossReward.runResult.reason");
                context.Assert(result.bossRewardClaimed, "Victory result should claim the Boss death reward.");
                context.AssertEqual(recipeId, result.bossDeathPatternRecipeId, "bossReward.runResult.recipeId");

                var bossSnapshot = bossService.CaptureSnapshot();
                context.Assert(bossSnapshot.deathRewardClaimed, "Boss snapshot should report claimed death reward.");
                context.AssertEqual(recipeId, bossSnapshot.lastDeathRewardRecipeId, "bossReward.snapshot.recipeId");

                var afterInventory = economyService.CaptureInventory(player);
                context.AssertEqual(beforeInventory.recipeUnlockCount + 1, afterInventory.recipeUnlockCount, "bossReward.inventory.recipeUnlockCount");
                context.Assert(afterInventory.recipeIds.Contains(recipeId), "Boss recipe should be unlocked in player inventory.");
                context.Assert(!bossService.TryClaimDeathReward(out _), "Boss death reward should not be claimable twice.");
                context.Assert(TotemRunResultForm.FormatSummary(result).Contains(recipeId, System.StringComparison.Ordinal), "Run result summary should expose the Boss recipe reward.");
                context.Assert(result.cumulativeStats != null, "Run result should include cumulative stats from RunStatsService.");
                context.AssertEqual(1, result.cumulativeStats.totalRuns, "bossReward.runStats.totalRuns");
                context.AssertEqual(1, result.cumulativeStats.totalWins, "bossReward.runStats.totalWins");
                context.AssertEqual(0, result.cumulativeStats.totalLosses, "bossReward.runStats.totalLosses");
                context.AssertEqual("AllEnemiesDefeated", result.cumulativeStats.lastResultReason, "bossReward.runStats.lastReason");
                context.Assert(runStatsService.LastSaveSucceeded, "RunStatsService should save after a real combat finish.");
                context.Assert(File.Exists(statsFileName), "RunStatsService should write the diagnostic run stats file.");
                context.Assert(TotemRunStatsService.TryReadSnapshotFromFile(statsFileName, out var savedStats, out string readStatsError), $"RunStatsService saved stats should be readable: {readStatsError}");
                context.AssertEqual(1, savedStats.totalRuns, "bossReward.runStats.savedTotalRuns");
                context.AssertEqual(1, savedStats.totalWins, "bossReward.runStats.savedTotalWins");
                context.Assert(ReferenceEquals(result, uiService.ActiveRunResult), "Run result UI context should receive the finished run result.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
                DeleteIfExists(statsFileName);
                DeleteIfExists(statsBackupFile);
                DeleteIfExists(statsTempFile);
            }
        }

        private static void RegisterBossRewardDiagnosticServices(TotemGameRuntime runtime, string statsFileName)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemRunStatsService { FilePathOverride = statsFileName });
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemBossService());
            runtime.RegisterService(new TotemCombatService());
            runtime.RegisterService(new TotemUIService());
        }

        private static void CheckEconomyNpcAndChoices(GFDiagnosticScenarioContext context)
        {
            var player = NewActor(1, TotemActorKind.Player, 100f);
            var economy = new TotemEconomyService();
            var catalog = TotemGameplayCatalog.BuildDefault();
            economy.ReloadItemDefinitions(catalog);
            context.AssertEqual(31, economy.ItemDefinitionCount, "economy.itemCatalog.count");
            context.Assert(economy.TryGetItemDefinition(2703, out var whiteInk) && whiteInk.ItemType == TotemItemType.InkBottle && whiteInk.BasePrice == 220, "Economy should expose White Premium ink item metadata.");
            context.AssertEqual(176, economy.CalculateSellValue(2703, 2), "economy.itemCatalog.sellValue");
            economy.RegisterActor(player);
            economy.AddCoins(player, 121);
            economy.AddInk(player, 7);
            economy.AddRecipeShards(player, 5);
            economy.AddEquipment(player, 2);
            var chest = economy.CalculateDeathChest(player);
            context.AssertEqual(60, chest.coins, "economy.deathChest.coins");
            context.AssertEqual(3, chest.inkBottleCount, "economy.deathChest.ink");
            context.AssertEqual(2, chest.recipeCopyCount, "economy.deathChest.recipes");
            context.AssertEqual(2, chest.equipmentCount, "economy.deathChest.equipment");
            context.Assert(economy.AddConfiguredItem(player, 2101, 2), "Economy should add configured red ink item.");
            context.AssertEqual(9, economy.CaptureInventory(player).inkBottleCount, "economy.itemCatalog.addInk");
            context.Assert(economy.SpendInk(player, 2), "Economy should spend generic GF_X ink.");
            context.AssertEqual(7, economy.CaptureInventory(player).inkBottleCount, "economy.itemCatalog.spendInk");
            context.Assert(!economy.SpendInk(player, 99), "Economy should reject overspending generic ink.");
            context.AssertEqual(7, economy.CaptureInventory(player).inkBottleCount, "economy.itemCatalog.spendInkRejected");

            var enchantBuyer = NewActor(51, TotemActorKind.Player, 100f);
            var enchantEconomy = new TotemEconomyService();
            var enchantTattoo = new TotemTattooService();
            enchantEconomy.ReloadItemDefinitions(catalog);
            enchantEconomy.RegisterActor(enchantBuyer);
            context.Assert(enchantTattoo.Equip(4, 1, 1), "Tattoo enchant transaction should have an equipped tattoo.");
            context.Assert(!TotemNpcService.TryApplyTattooEnchant(enchantBuyer, "Common", enchantEconomy, enchantTattoo, out var noCoinEnchant), "Tattoo enchant should reject missing coins.");
            context.AssertEqual("NotEnoughCoins", noCoinEnchant.reason, "npc.tattooEnchant.noCoins.reason");
            enchantEconomy.AddCoins(enchantBuyer, 200);
            context.Assert(!TotemNpcService.TryApplyTattooEnchant(enchantBuyer, "Common", enchantEconomy, enchantTattoo, out var noInkEnchant), "Tattoo enchant should reject missing ink.");
            context.AssertEqual("NotEnoughInk", noInkEnchant.reason, "npc.tattooEnchant.noInk.reason");
            enchantEconomy.AddInk(enchantBuyer, 1);
            context.Assert(TotemNpcService.TryApplyTattooEnchant(enchantBuyer, "Common", enchantEconomy, enchantTattoo, out var paidEnchant), "Tattoo enchant should apply through tattooist transaction.");
            context.Assert(paidEnchant.succeeded, "Tattoo enchant transaction result should succeed.");
            context.AssertEqual(200, paidEnchant.coinCost, "npc.tattooEnchant.coinCost");
            context.AssertEqual(1, paidEnchant.rarePigmentCost, "npc.tattooEnchant.inkCost");
            context.AssertEqual(0, paidEnchant.coinsAfter, "npc.tattooEnchant.coinsAfter");
            context.AssertEqual(0, paidEnchant.inkAfter, "npc.tattooEnchant.inkAfter");
            context.AssertEqual(1, enchantTattoo.CaptureSnapshot().enchantedCount, "npc.tattooEnchant.enchantedCount");

            var map = TotemMapService.BuildLayout(1, 1);
            var npcs = TotemNpcService.BuildDefaultNpcs(map);
            context.AssertEqual(5, npcs.Length, "npc.count");
            context.AssertEqual(3, npcs.Count(npc => npc.Type == TotemNpcType.Tattooist), "npc.tattooistCount");
            context.AssertEqual(2, npcs.Count(npc => npc.Type == TotemNpcType.Merchant), "npc.merchantCount");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).All(npc => npc.Offers.Length >= 3), "Each merchant should expose at least 3 offers.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).All(offer => offer.RewardType != TotemShopRewardType.Unknown), "Each merchant offer should expose an explicit reward type.");
            var generatedGeneralShop = catalog.CreateShopOffers("general_shop");
            var generatedAlienShop = catalog.CreateShopOffers("alien_shop");
            context.AssertEqual(10, generatedGeneralShop.Length, "shopStock.general.count");
            context.AssertEqual(5, generatedAlienShop.Length, "shopStock.alien.count");
            context.Assert(generatedGeneralShop.Any(offer => offer.ItemId == 401 && offer.RewardType == TotemShopRewardType.StatusCleanse), "ShopStock Antidote should cleanse statuses.");
            context.Assert(generatedAlienShop.Any(offer => offer.ItemId == 501 && offer.RewardType == TotemShopRewardType.Ink && offer.RewardAmount == 2), "ShopStock RareInk should route to ink rewards.");
            var generatedMerchantSlots = catalog.CreateMerchantSlotOffers("merchant_general");
            context.AssertEqual(3, generatedMerchantSlots.Length, "merchantSlot.generated.count");
            context.Assert(generatedMerchantSlots.Any(offer => offer.ItemId == 9002 && offer.RewardId == "energy_fist" && offer.Price == 130), "MerchantConfig slot refresh should preserve energy fist slot 2.");
            var generatedNpcs = catalog.CreateNpcModels(map);
            context.AssertEqual(13, generatedNpcs.FirstOrDefault(npc => npc.NpcId == "merchant_general")?.Offers.Length ?? -1, "shopStock.generatedGeneralOffers");
            context.AssertEqual(8, generatedNpcs.FirstOrDefault(npc => npc.NpcId == "merchant_alien")?.Offers.Length ?? -1, "shopStock.generatedAlienOffers");

            var choices = TotemChoiceService.BuildThreeChoices("diagnostic_choice", 7);
            context.AssertEqual(3, choices.Options.Length, "choice.optionCount");
            context.AssertEqual("diagnostic_choice", choices.EventId, "choice.eventId");
            var eventCatalog = catalog.CreateEvents();
            context.AssertEqual(6, eventCatalog.Length, "event.catalogCount");
            var selectedChoiceEvent = TotemChoiceService.SelectEvent(TotemGameplayEventType.Choice, 7, eventCatalog);
            context.Assert(selectedChoiceEvent != null, "Choice event selection should return a row.");
            context.AssertEqual(TotemGameplayEventType.Choice, selectedChoiceEvent?.EventType ?? TotemGameplayEventType.Unknown, "event.choice.selectedType");
            AssertNear(context, 20f, selectedChoiceEvent?.TimeoutSec ?? -1f, "event.choice.timeout");
        }

        private static void CheckInteractionContracts(GFDiagnosticScenarioContext context)
        {
            var merchant = new TotemNpcModel { NpcId = "merchant_general", Type = TotemNpcType.Merchant };
            var tattooist = new TotemNpcModel { NpcId = "tattooist_default", Type = TotemNpcType.Tattooist };
            context.AssertEqual("F: Shop with merchant_general", TotemInteractionService.BuildPrompt(merchant), "interaction.merchantPrompt");
            context.AssertEqual("F: Tattoo with tattooist_default", TotemInteractionService.BuildPrompt(tattooist), "interaction.tattooPrompt");
            var eventAnchor = new TotemMapAnchor { AnchorId = "event.choice.altar", Kind = TotemMapAnchorKind.Event, PayloadId = "event_choice_001" };
            context.AssertEqual("F: Inspect event_choice_001", TotemInteractionService.BuildMapEventPrompt(eventAnchor), "interaction.mapEventPrompt");
            context.AssertEqual("shop_merchant_general", TotemInteractionService.BuildChoiceEventId(merchant), "interaction.merchantChoiceEvent");
            context.AssertEqual("tattoo_tattooist_default", TotemInteractionService.BuildChoiceEventId(tattooist), "interaction.tattooChoiceEvent");
            context.AssertEqual(
                TotemInteractionService.ComputeStableSeed("tattoo_tattooist_default", 1),
                TotemInteractionService.ComputeStableSeed("tattoo_tattooist_default", 1),
                "interaction.stableSeed");

            var choices = TotemChoiceService.BuildThreeChoices(TotemInteractionService.BuildChoiceEventId(merchant), 11);
            context.AssertEqual(3, choices.Options.Length, "interaction.choiceCount");
            context.AssertEqual("shop_merchant_general", choices.EventId, "interaction.choiceEventId");
        }

        private static void CheckNpcInteractionRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemNpcInteractionDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterNpcInteractionDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var input = runtime.GetService<TotemInputService>();
                var actor = runtime.GetService<TotemActorService>();
                var npc = runtime.GetService<TotemNpcService>();
                var choice = runtime.GetService<TotemChoiceService>();
                var interaction = runtime.GetService<TotemInteractionService>();
                var ui = runtime.GetService<TotemUIService>();
                var provider = new InteractionInputProvider();
                input.SetInputProvider(provider);
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                context.AssertEqual(5, npc.Npcs.Count, "npcInteraction.spawnedCount");
                var player = actor.Player;
                var merchant = npc.Npcs.FirstOrDefault(item => item.Type == TotemNpcType.Merchant);
                var tattooist = npc.Npcs.FirstOrDefault(item => item.Type == TotemNpcType.Tattooist);
                context.Assert(merchant != null, "NPC interaction diagnostic should find a merchant.");
                context.Assert(tattooist != null, "NPC interaction diagnostic should find a tattooist.");

                MoveActorTo(player, merchant.Position);
                interaction.Tick(0.1f);
                var merchantFocus = interaction.CaptureSnapshot();
                context.Assert(merchantFocus.hasNpc, "Interaction should focus a nearby merchant.");
                context.AssertEqual(merchant.NpcId, merchantFocus.npcId, "npcInteraction.merchant.focusNpcId");
                context.AssertEqual(TotemInteractionService.BuildPrompt(merchant), merchantFocus.prompt, "npcInteraction.merchant.prompt");

                provider.PressInteract();
                input.Tick(0.1f);
                interaction.Tick(0.1f);
                provider.ClearPressed();
                var shopSnapshot = interaction.CaptureSnapshot();
                context.AssertEqual(TotemInteractionService.BuildChoiceEventId(merchant), shopSnapshot.lastInteraction, "npcInteraction.shop.lastInteraction");
                context.Assert(ReferenceEquals(merchant, ui.ActiveShopNpc), "Merchant interaction should become the active shop NPC.");
                context.Assert(ui.ActiveTattooNpc == null, "Opening a shop should clear active tattoo NPC context.");
                context.Assert(ui.ActiveChoice == null, "Opening a shop should not leave a stale choice context.");

                MoveActorTo(player, tattooist.Position);
                input.Tick(0.1f);
                interaction.Tick(0.1f);
                var tattooFocus = interaction.CaptureSnapshot();
                context.Assert(tattooFocus.hasNpc, "Interaction should focus a nearby tattooist.");
                context.AssertEqual(tattooist.NpcId, tattooFocus.npcId, "npcInteraction.tattoo.focusNpcId");
                context.AssertEqual(TotemInteractionService.BuildPrompt(tattooist), tattooFocus.prompt, "npcInteraction.tattoo.prompt");

                provider.PressInteract();
                input.Tick(0.1f);
                interaction.Tick(0.1f);
                provider.ClearPressed();
                var tattooSnapshot = interaction.CaptureSnapshot();
                string tattooEventId = TotemInteractionService.BuildChoiceEventId(tattooist);
                context.AssertEqual(tattooEventId, tattooSnapshot.lastInteraction, "npcInteraction.tattoo.lastInteraction");
                context.AssertEqual(tattooEventId, tattooSnapshot.choiceEventId, "npcInteraction.tattoo.choiceEventId");
                context.AssertEqual(3, tattooSnapshot.choiceCount, "npcInteraction.tattoo.choiceCount");
                context.Assert(ReferenceEquals(tattooist, ui.ActiveTattooNpc), "Tattooist interaction should become the active tattoo NPC.");
                context.Assert(ui.ActiveShopNpc == null, "Opening a tattoo studio should clear active shop NPC context.");
                context.Assert(ui.ActiveChoice != null, "Tattooist interaction should expose an active choice snapshot.");
                context.AssertEqual(tattooEventId, ui.ActiveChoice.EventId, "npcInteraction.ui.choiceEventId");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, choice.ChoiceState, "npcInteraction.choice.runtimeState");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, ui.ActiveChoice.State, "npcInteraction.choice.snapshotState");
                AssertNear(context, 0f, Time.timeScale, "npcInteraction.choice.pausedTimeScale");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckMapEventInteractionRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemMapEventInteractionDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterNpcInteractionDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var input = runtime.GetService<TotemInputService>();
                var map = runtime.GetService<TotemMapService>();
                var actor = runtime.GetService<TotemActorService>();
                var choice = runtime.GetService<TotemChoiceService>();
                var interaction = runtime.GetService<TotemInteractionService>();
                var ui = runtime.GetService<TotemUIService>();
                var provider = new InteractionInputProvider();
                input.SetInputProvider(provider);
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var eventAnchor = TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.Event).FirstOrDefault();
                context.Assert(eventAnchor != null, "Map event interaction diagnostic requires an event anchor.");
                var player = actor.Player;
                MoveActorTo(player, eventAnchor.Position);

                interaction.Tick(0.1f);
                var focus = interaction.CaptureSnapshot();
                context.Assert(focus.hasMapEvent, "Interaction should focus a nearby map event anchor.");
                context.AssertEqual(eventAnchor.AnchorId, focus.mapEventAnchorId, "mapEvent.interaction.focusAnchorId");
                context.AssertEqual(eventAnchor.PayloadId, focus.mapEventId, "mapEvent.interaction.focusEventId");
                context.AssertEqual(TotemInteractionService.BuildMapEventPrompt(eventAnchor), focus.prompt, "mapEvent.interaction.prompt");

                provider.PressInteract();
                input.Tick(0.1f);
                interaction.Tick(0.1f);
                provider.ClearPressed();

                var opened = interaction.CaptureSnapshot();
                context.AssertEqual($"map_event_{eventAnchor.AnchorId}", opened.lastInteraction, "mapEvent.interaction.lastInteraction");
                context.AssertEqual(eventAnchor.PayloadId, opened.choiceEventId, "mapEvent.interaction.choiceEventId");
                context.AssertEqual(3, opened.choiceCount, "mapEvent.interaction.choiceCount");
                context.Assert(ui.ActiveChoice != null, "Map event interaction should expose an active ThreeChoice snapshot.");
                context.Assert(ui.ActiveShopNpc == null && ui.ActiveTattooNpc == null, "Map event interaction should not reuse NPC UI context.");
                context.AssertEqual(eventAnchor.PayloadId, ui.ActiveChoice.EventId, "mapEvent.interaction.uiChoiceEventId");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, choice.ChoiceState, "mapEvent.interaction.choiceState");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, ui.ActiveChoice.State, "mapEvent.interaction.uiChoiceState");
                AssertNear(context, 0f, Time.timeScale, "mapEvent.interaction.pausedTimeScale");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckRuntimeShopPurchase(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemShopPurchaseDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterNpcInteractionDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var npc = runtime.GetService<TotemNpcService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                var merchant = npc.Npcs.FirstOrDefault(item =>
                    item.Type == TotemNpcType.Merchant &&
                    (item.Offers ?? System.Array.Empty<TotemShopOffer>()).Any(offer => offer.RewardType == TotemShopRewardType.Ink && offer.Stock > 0));
                var inkOffer = merchant?.Offers?.FirstOrDefault(offer => offer.RewardType == TotemShopRewardType.Ink && offer.Stock > 0);
                context.Assert(player != null, "Runtime shop purchase diagnostic should have a player.");
                context.Assert(merchant != null, "Runtime shop purchase diagnostic should find a merchant with an ink offer.");
                context.Assert(inkOffer != null, "Runtime shop purchase diagnostic should find an ink offer.");
                if (player == null || merchant == null || inkOffer == null)
                {
                    return;
                }

                economy.AddCoins(player, 500);
                int expectedPrice = Mathf.RoundToInt(inkOffer.Price * merchant.ThemePriceMultiplier);
                int stockBefore = inkOffer.Stock;
                var beforePurchase = economy.CaptureInventory(player);
                context.Assert(expectedPrice > 0, "Runtime shop purchase should use a positive actual price.");
                context.Assert(npc.TryPurchase(player, merchant, inkOffer.ItemId, out var purchased), "Runtime shop purchase should succeed for an affordable stocked ink offer.");
                var afterPurchase = economy.CaptureInventory(player);
                int expectedInk = inkOffer.RewardAmount > 0 ? inkOffer.RewardAmount : 1;
                context.Assert(purchased.purchased, "Runtime shop purchase result should mark purchased.");
                context.AssertEqual("Purchased", purchased.reason, "shop.runtime.purchase.reason");
                context.AssertEqual(inkOffer.ItemId, purchased.itemId, "shop.runtime.purchase.itemId");
                context.AssertEqual(expectedPrice, purchased.actualPrice, "shop.runtime.purchase.price");
                context.AssertEqual(stockBefore - 1, purchased.stockLeft, "shop.runtime.purchase.resultStockLeft");
                context.AssertEqual(stockBefore - 1, inkOffer.Stock, "shop.runtime.purchase.offerStockLeft");
                context.AssertEqual(TotemShopRewardType.Ink, purchased.rewardType, "shop.runtime.purchase.rewardType");
                context.AssertEqual($"Ink +{expectedInk}", purchased.rewardSummary, "shop.runtime.purchase.rewardSummary");
                context.AssertEqual(beforePurchase.coins - expectedPrice, afterPurchase.coins, "shop.runtime.purchase.coinsAfter");
                context.AssertEqual(beforePurchase.inkBottleCount + expectedInk, afterPurchase.inkBottleCount, "shop.runtime.purchase.inkAfter");

                var failOffer = new TotemShopOffer
                {
                    ItemId = 9901,
                    DisplayName = "Diagnostic Expensive Ink",
                    Price = expectedPrice + 1000,
                    Stock = 2,
                    RewardType = TotemShopRewardType.Ink,
                    RewardAmount = 1,
                };
                var failMerchant = new TotemNpcModel
                {
                    NpcId = "merchant_diagnostic_fail",
                    Type = TotemNpcType.Merchant,
                    ThemePriceMultiplier = 1f,
                    Offers = new[] { failOffer },
                };

                economy.SpendCoins(player, afterPurchase.coins);
                economy.AddCoins(player, 10);
                int inkBeforeFailure = economy.CaptureInventory(player).inkBottleCount;
                context.Assert(!npc.TryPurchase(player, failMerchant, failOffer.ItemId, out var notEnough), "Runtime shop purchase should reject unaffordable offers.");
                var afterNotEnough = economy.CaptureInventory(player);
                context.Assert(!notEnough.purchased, "NotEnoughCoins result should not mark purchased.");
                context.AssertEqual("NotEnoughCoins", notEnough.reason, "shop.runtime.notEnough.reason");
                context.AssertEqual(failOffer.Price, notEnough.actualPrice, "shop.runtime.notEnough.price");
                context.AssertEqual(2, notEnough.stockLeft, "shop.runtime.notEnough.resultStockLeft");
                context.AssertEqual(2, failOffer.Stock, "shop.runtime.notEnough.offerStockPreserved");
                context.AssertEqual(10, afterNotEnough.coins, "shop.runtime.notEnough.coinsPreserved");
                context.AssertEqual(inkBeforeFailure, afterNotEnough.inkBottleCount, "shop.runtime.notEnough.inkPreserved");

                var unavailableOffer = new TotemShopOffer
                {
                    ItemId = 9902,
                    DisplayName = "Diagnostic Broken Reward",
                    Price = 5,
                    Stock = 1,
                    RewardType = TotemShopRewardType.Unknown,
                    RewardAmount = 1,
                };
                failMerchant.Offers = new[] { unavailableOffer };
                context.Assert(!npc.TryPurchase(player, failMerchant, unavailableOffer.ItemId, out var unavailable), "Runtime shop purchase should reject offers whose reward cannot be applied.");
                var afterUnavailable = economy.CaptureInventory(player);
                context.Assert(!unavailable.purchased, "RewardUnavailable result should not mark purchased.");
                context.AssertEqual("RewardUnavailable", unavailable.reason, "shop.runtime.unavailable.reason");
                context.AssertEqual(5, unavailable.actualPrice, "shop.runtime.unavailable.price");
                context.AssertEqual(1, unavailable.stockLeft, "shop.runtime.unavailable.resultStockLeft");
                context.AssertEqual(1, unavailableOffer.Stock, "shop.runtime.unavailable.offerStockPreserved");
                context.AssertEqual(10, afterUnavailable.coins, "shop.runtime.unavailable.coinsPreserved");
                context.AssertEqual(inkBeforeFailure, afterUnavailable.inkBottleCount, "shop.runtime.unavailable.inkPreserved");

                failMerchant.Offers = new[] { failOffer };
                failOffer.Stock = 0;
                context.Assert(!npc.TryPurchase(player, failMerchant, failOffer.ItemId, out var exhausted), "Runtime shop purchase should reject exhausted offers.");
                context.AssertEqual("StockExhausted", exhausted.reason, "shop.runtime.exhausted.reason");
                context.AssertEqual(0, exhausted.stockLeft, "shop.runtime.exhausted.stockLeft");
                context.AssertEqual(failOffer.Price, exhausted.actualPrice, "shop.runtime.exhausted.price");
                context.AssertEqual(10, economy.CaptureInventory(player).coins, "shop.runtime.exhausted.coinsPreserved");

                context.Assert(!npc.TryPurchase(player, failMerchant, 123456, out var missing), "Runtime shop purchase should reject missing offers.");
                context.AssertEqual("OfferNotFound", missing.reason, "shop.runtime.missing.reason");
                context.AssertEqual(0, missing.actualPrice, "shop.runtime.missing.price");

                var tattooist = new TotemNpcModel { NpcId = "tattooist_diagnostic_invalid", Type = TotemNpcType.Tattooist, Offers = new[] { failOffer } };
                context.Assert(!npc.TryPurchase(player, tattooist, failOffer.ItemId, out var invalid), "Runtime shop purchase should reject non-merchant NPCs.");
                context.AssertEqual("InvalidContext", invalid.reason, "shop.runtime.invalid.reason");
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

        private static void RegisterNpcInteractionDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemInputService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemChoiceService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemInteractionService());
            runtime.RegisterService(new TotemUIService());
        }

        private static void CheckShopAndChoiceRewardRouting(GFDiagnosticScenarioContext context)
        {
            var player = NewActor(1, TotemActorKind.Player, 100f);
            var economy = new TotemEconomyService();
            var weapon = new TotemWeaponService();
            var skill = new TotemSkillService();
            var status = new TotemStatusService();
            var tattoo = new TotemTattooService();

            economy.RegisterActor(player);
            economy.AddCoins(player, 500);
            weapon.EquipWeapon(player, "knife_basic");
            context.Assert(skill.EquipSkill(player, 0, "skill_fireball_01"), "Skill slot 0 should equip before reward routing.");
            context.Assert(skill.TryCastSlot(player, 0, out _), "Skill slot 0 should enter cooldown before reward routing.");
            context.Assert(skill.GetCooldownRemaining(player, 0) > 0f, "Skill cooldown should be active before Skill Core reward.");
            context.Assert(tattoo.Equip(4, 1, 1), "Tattoo should equip before TattooBonus reward.");

            var redInk = new TotemShopOffer { ItemId = 101, DisplayName = "Red Ink", Price = 30, Stock = 1, RewardType = TotemShopRewardType.Ink, RewardId = "red", RewardAmount = 1 };
            context.AssertEqual(TotemShopRewardType.Ink, TotemNpcService.InferRewardType(redInk), "shop.redInk.rewardType");
            context.Assert(TotemNpcService.ApplyPurchasedOfferEffect(redInk, player, economy, weapon, skill, tattoo, out string redInkSummary), "Red Ink purchase effect should apply.");
            context.AssertEqual("Ink +1", redInkSummary, "shop.redInk.summary");
            context.AssertEqual(1, economy.CaptureInventory(player).inkBottleCount, "shop.redInk.inkCount");

            var knifeUpgrade = new TotemShopOffer { ItemId = 201, DisplayName = "Knife Upgrade", Price = 50, Stock = 1, RewardType = TotemShopRewardType.WeaponUpgrade, RewardId = "knife_basic", RewardAmount = 1 };
            context.Assert(TotemNpcService.ApplyPurchasedOfferEffect(knifeUpgrade, player, economy, weapon, skill, tattoo, out _), "Knife Upgrade purchase effect should apply.");
            context.AssertEqual("knife_basic", weapon.GetEquippedWeaponId(player), "shop.weaponUpgrade.weaponId");
            context.AssertEqual(2, weapon.GetWeaponLevel(player), "shop.weaponUpgrade.level");

            var skillCore = new TotemShopOffer { ItemId = 301, DisplayName = "Skill Core", Price = 80, Stock = 1, RewardType = TotemShopRewardType.SkillCore, RewardId = "skill_fireball_01", RewardAmount = 1, RewardSlot = 0 };
            context.Assert(TotemNpcService.ApplyPurchasedOfferEffect(skillCore, player, economy, weapon, skill, tattoo, out string skillSummary), "Skill Core purchase effect should apply.");
            context.AssertEqual("Skill slot 0 refreshed", skillSummary, "shop.skillCore.summary");
            AssertNear(context, 0f, skill.GetCooldownRemaining(player, 0), "shop.skillCore.cooldown");
            context.AssertEqual(1, skill.GetCurrentCharges(player, 0), "shop.skillCore.charges");

            status.ApplyStatus(player, "Burn", 10f, 2f);
            var antidote = new TotemShopOffer { ItemId = 401, DisplayName = "Antidote", Price = 120, Stock = 1, RewardType = TotemShopRewardType.StatusCleanse, RewardAmount = 1 };
            context.Assert(TotemNpcService.ApplyPurchasedOfferEffect(antidote, player, economy, weapon, skill, status, tattoo, out string antidoteSummary), "Antidote purchase effect should apply.");
            context.AssertEqual("Statuses cleansed", antidoteSummary, "shop.antidote.summary");
            context.AssertEqual(0, status.GetActiveStatuses(player).Count, "shop.antidote.statusCount");

            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "coin_cache", EffectType = TotemChoiceEffectType.CoinReward, Magnitude = 80f }, player, economy, weapon, status, tattoo, out string coinSummary), "Coin reward choice should apply.");
            context.AssertEqual("Coins +80", coinSummary, "choice.coin.summary");
            context.AssertEqual(580, economy.CaptureInventory(player).coins, "choice.coin.total");

            player.ApplyDamage(40f);
            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "opt_heal_moderate", EffectType = TotemChoiceEffectType.Heal, ValueInt = 30 }, player, economy, weapon, skill, status, tattoo, out string healSummary), "Heal choice should apply.");
            context.AssertEqual("Heal +30", healSummary, "choice.heal.summary");
            AssertNear(context, 90f, player.Health, "choice.heal.playerHealth");

            var beforeRecipes = economy.CaptureInventory(player).recipeUnlockCount;
            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "opt_tattoo_recipe_fire_001", EffectType = TotemChoiceEffectType.RecipeUnlock, ContentRef = "recipe_fire_001" }, player, economy, weapon, skill, status, tattoo, out string recipeSummary), "Recipe unlock choice should apply.");
            context.AssertEqual("Recipe recipe_fire_001 unlocked", recipeSummary, "choice.recipe.summary");
            context.AssertEqual(beforeRecipes + 1, economy.CaptureInventory(player).recipeUnlockCount, "choice.recipe.unlockCount");

            context.Assert(skill.TryCastSlot(player, 0, out _), "Skill slot 0 should enter cooldown before choice skill refresh.");
            context.Assert(skill.GetCooldownRemaining(player, 0) > 0f, "Skill slot 0 cooldown should be active before choice skill refresh.");
            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "opt_skill_upgrade_slot0_001", EffectType = TotemChoiceEffectType.SkillRefresh, SkillSlot = 0 }, player, economy, weapon, skill, status, tattoo, out string refreshSummary), "Skill refresh choice should apply.");
            context.AssertEqual("Skill slot 0 refreshed", refreshSummary, "choice.skillRefresh.summary");
            AssertNear(context, 0f, skill.GetCooldownRemaining(player, 0), "choice.skillRefresh.cooldown");

            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "opt_skill_acquire_dash_001", EffectType = TotemChoiceEffectType.SkillAcquire, ContentRef = "skill_phase_dash", SkillSlot = 1 }, player, economy, weapon, skill, status, tattoo, out string dashSummary), "Skill acquire dash choice should apply.");
            context.AssertEqual("Skill skill_phase_dash equipped", dashSummary, "choice.skillAcquire.dash.summary");
            context.Assert(skill.TryCastSlot(player, 1, out var dashSkill), "Acquired dash skill should cast from slot 1.");
            context.AssertEqual("skill_phase_dash", dashSkill?.SkillId ?? string.Empty, "choice.skillAcquire.dash.skillId");
            context.Assert(skill.GetCooldownRemaining(player, 1) > 0f, "Acquired dash skill should enter cooldown.");

            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "opt_skill_acquire_shield_001", EffectType = TotemChoiceEffectType.SkillAcquire, ContentRef = "skill_ink_shield", SkillSlot = 1 }, player, economy, weapon, skill, status, tattoo, out string shieldSummary), "Skill acquire shield choice should apply.");
            context.AssertEqual("Skill skill_ink_shield equipped", shieldSummary, "choice.skillAcquire.shield.summary");
            context.Assert(skill.TryCastSlot(player, 1, out var shieldSkill), "Acquired shield skill should cast from slot 1.");
            context.AssertEqual("skill_ink_shield", shieldSkill?.SkillId ?? string.Empty, "choice.skillAcquire.shield.skillId");

            status.ApplyStatus(player, "Burn", 10f, 2f);
            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "cleanse_status", EffectType = TotemChoiceEffectType.StatusCleanse, Magnitude = 1f }, player, economy, weapon, status, tattoo, out _), "Status cleanse choice should apply.");
            context.AssertEqual(0, status.GetActiveStatuses(player).Count, "choice.cleanse.statusCount");

            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "upgrade_weapon", EffectType = TotemChoiceEffectType.WeaponUpgrade, Magnitude = 1f }, player, economy, weapon, status, tattoo, out _), "Weapon upgrade choice should apply.");
            context.AssertEqual(3, weapon.GetWeaponLevel(player), "choice.weaponUpgrade.level");

            context.Assert(TotemChoiceService.ApplyChoiceEffect(new TotemChoiceOption { OptionId = "tattoo_focus", EffectType = TotemChoiceEffectType.TattooBonus, Magnitude = 0.15f }, player, economy, weapon, status, tattoo, out string tattooSummary), "Tattoo bonus choice should apply.");
            context.AssertEqual("Tattoo enchant applied", tattooSummary, "choice.tattoo.summary");
            context.AssertEqual(1, tattoo.CaptureSnapshot().enchantedCount, "choice.tattoo.enchantedCount");
        }

        private static void CheckSettingsSelfTattooAndRunResult(GFDiagnosticScenarioContext context)
        {
            var settings = new TotemSettingsSnapshot { bgmVolume = 0.75f, sfxVolume = 0.5f, qualityLevel = 1 };
            context.AssertEqual("BGM 0.75  SFX 0.50  Quality 1", TotemSettingsService.FormatSnapshot(settings), "settings.format");
            CheckSettingsPersistence(context, settings);
            CheckSettingsEditLifecycle(context);

            var tattoo = new TotemTattooService();
            context.Assert(tattoo.StartSelfTattoo(2, 3, 4), "Self tattoo should start for a valid combination.");
            var before = tattoo.CaptureSnapshot();
            context.Assert(before.selfTattooInProgress, "Self tattoo should be in progress after start.");
            context.AssertEqual("Part2/Color3/Pattern4", before.pendingSelfTattooSummary, "selfTattoo.pending");
            AssertNear(context, 8f, before.selfTattooRemainingSec, "selfTattoo.readingDuration");
            tattoo.Tick(TotemTattooService.GetSelfTattooDuration(2));
            var after = tattoo.CaptureSnapshot();
            context.Assert(!after.selfTattooInProgress, "Self tattoo should finish after duration.");
            context.AssertEqual(1, after.equippedCount, "selfTattoo.equippedCount");
            context.Assert(tattoo.ApplyMinorEnchant(), "Minor enchant should apply to an equipped tattoo.");
            var enchanted = tattoo.CaptureSnapshot();
            context.AssertEqual(1, enchanted.enchantedCount, "tattoo.enchantedCount");
            context.AssertEqual(1, enchanted.lastEnchantAffixId, "tattoo.enchant.lastAffixId");
            context.AssertEqual("ElementDamageBonus", enchanted.lastEnchantAffixType, "tattoo.enchant.lastAffixType");
            context.AssertEqual("Common", enchanted.lastEnchantColorTier, "tattoo.enchant.lastColorTier");
            context.AssertEqual("ElementDmg", enchanted.lastEnchantStatKey, "tattoo.enchant.lastStatKey");
            AssertNear(context, 0.1f, enchanted.lastEnchantValue, "tattoo.enchant.lastValue");
            context.AssertEqual(200, enchanted.lastEnchantCoinCost, "tattoo.enchant.coinCost");
            context.AssertEqual(1, enchanted.lastEnchantRarePigmentCost, "tattoo.enchant.rarePigmentCost");

            var result = TotemCombatService.BuildRunResult(false, "PlayerDefeated", 9, 0f, 31, 45.6f);
            context.AssertEqual(false, result.win, "runResult.win");
            context.AssertEqual("PlayerDefeated", result.reason, "runResult.reason");
            context.AssertEqual(9, result.killCount, "runResult.killCount");
            CheckRunStatsPersistence(context, result);
        }

        private static void CheckSettingsPersistence(GFDiagnosticScenarioContext context, TotemSettingsSnapshot settings)
        {
            string directory = Path.Combine(Path.GetTempPath(), "totem-warrior-diagnostics");
            string fileName = Path.Combine(directory, "settings-test.json");
            string backupFile = fileName + ".bak";
            string tempFile = fileName + ".tmp";
            try
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);

                context.Assert(TotemSettingsService.TryWriteSnapshotToFile(fileName, settings, out string writeError), $"Settings should save to temp file: {writeError}");
                context.Assert(File.Exists(fileName), "Settings save file should exist.");
                context.Assert(TotemSettingsService.TryReadSnapshotFromFile(fileName, out var loaded, out string readError), $"Settings should load from temp file: {readError}");
                context.AssertEqual("BGM 0.75  SFX 0.50  Quality 1", TotemSettingsService.FormatSnapshot(loaded), "settings.persistence.loaded");

                File.WriteAllText(fileName, "{not json}");
                context.Assert(!TotemSettingsService.TryReadSnapshotFromFile(fileName, out _, out string invalidError), "Invalid settings JSON should fail cleanly.");
                context.Assert(!string.IsNullOrWhiteSpace(invalidError), "Invalid settings JSON should report an error.");
            }
            finally
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);
            }
        }

        private static void CheckSettingsEditLifecycle(GFDiagnosticScenarioContext context)
        {
            string directory = Path.Combine(Path.GetTempPath(), "totem-warrior-diagnostics");
            string fileName = Path.Combine(directory, "settings-lifecycle-test.json");
            string backupFile = fileName + ".bak";
            string tempFile = fileName + ".tmp";
            try
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);

                var service = new TotemSettingsService
                {
                    FilePathOverride = fileName,
                };
                int changeCount = 0;
                TotemSettingsSnapshot lastChanged = null;
                service.SettingsChanged += snapshot =>
                {
                    changeCount++;
                    lastChanged = snapshot;
                };

                var initial = service.CaptureSnapshot();
                context.Assert(!initial.editing, "Settings should start outside edit mode.");
                service.Preview(0.15f, 0.2f, 2);
                var idlePreview = service.CaptureSnapshot();
                AssertNear(context, initial.bgmVolume, idlePreview.bgmVolume, "settings.lifecycle.idlePreviewBgm");
                AssertNear(context, initial.sfxVolume, idlePreview.sfxVolume, "settings.lifecycle.idlePreviewSfx");
                context.AssertEqual(initial.qualityLevel, idlePreview.qualityLevel, "settings.lifecycle.idlePreviewQuality");
                context.Assert(!idlePreview.editing, "Idle Preview should stay outside edit mode.");
                context.AssertEqual(0, changeCount, "settings.lifecycle.idlePreviewChangeCount");
                context.AssertEqual(1, service.IgnoredOperationCount, "settings.lifecycle.idlePreviewIgnoredCount");
                context.AssertEqual("Preview", service.LastIgnoredOperation, "settings.lifecycle.idlePreviewIgnoredOperation");

                service.Commit();
                context.AssertEqual(2, service.IgnoredOperationCount, "settings.lifecycle.idleCommitIgnoredCount");
                context.AssertEqual("Commit", service.LastIgnoredOperation, "settings.lifecycle.idleCommitIgnoredOperation");
                context.Assert(!File.Exists(fileName), "Idle Commit should not write a settings file.");
                context.AssertEqual(0, changeCount, "settings.lifecycle.idleCommitChangeCount");

                service.Rollback();
                context.AssertEqual(3, service.IgnoredOperationCount, "settings.lifecycle.idleRollbackIgnoredCount");
                context.AssertEqual("Rollback", service.LastIgnoredOperation, "settings.lifecycle.idleRollbackIgnoredOperation");
                context.AssertEqual(0, changeCount, "settings.lifecycle.idleRollbackChangeCount");

                service.BeginEdit();
                context.Assert(service.CaptureSnapshot().editing, "BeginEdit should enter edit mode.");
                context.AssertEqual(0, changeCount, "settings.lifecycle.beginEditChangeCount");

                service.Preview(-0.25f, 1.25f, 1);
                var preview = service.CaptureSnapshot();
                AssertNear(context, 0f, preview.bgmVolume, "settings.lifecycle.previewBgmClamped");
                AssertNear(context, 1f, preview.sfxVolume, "settings.lifecycle.previewSfxClamped");
                context.Assert(preview.editing, "Preview should keep edit mode active.");
                context.AssertEqual(1, changeCount, "settings.lifecycle.previewChangeCount");

                service.Rollback();
                var rollback = service.CaptureSnapshot();
                AssertNear(context, initial.bgmVolume, rollback.bgmVolume, "settings.lifecycle.rollbackBgm");
                AssertNear(context, initial.sfxVolume, rollback.sfxVolume, "settings.lifecycle.rollbackSfx");
                context.Assert(!rollback.editing, "Rollback should leave edit mode.");
                context.AssertEqual(2, changeCount, "settings.lifecycle.rollbackChangeCount");

                service.BeginEdit();
                service.Preview(0.25f, 0.35f, 1);
                service.Commit();
                var committed = service.CaptureSnapshot();
                AssertNear(context, 0.25f, committed.bgmVolume, "settings.lifecycle.commitBgm");
                AssertNear(context, 0.35f, committed.sfxVolume, "settings.lifecycle.commitSfx");
                context.Assert(!committed.editing, "Commit should leave edit mode.");
                context.Assert(service.LastSaveSucceeded, $"Commit should save to the temp settings file: {service.LastPersistenceMessage}");
                context.Assert(File.Exists(fileName), "Commit should create the temp settings file.");
                context.AssertEqual(4, changeCount, "settings.lifecycle.commitChangeCount");
                context.Assert(lastChanged != null && !lastChanged.editing, "SettingsChanged should expose a non-editing committed snapshot.");
                context.Assert(TotemSettingsService.TryReadSnapshotFromFile(fileName, out var loaded, out string readError), $"Committed settings should reload: {readError}");
                AssertNear(context, 0.25f, loaded.bgmVolume, "settings.lifecycle.loadedBgm");
                AssertNear(context, 0.35f, loaded.sfxVolume, "settings.lifecycle.loadedSfx");
                context.AssertEqual(1, loaded.qualityLevel, "settings.lifecycle.loadedQuality");

                service.Commit();
                context.AssertEqual(4, service.IgnoredOperationCount, "settings.lifecycle.repeatCommitIgnoredCount");
                context.AssertEqual("Commit", service.LastIgnoredOperation, "settings.lifecycle.repeatCommitIgnoredOperation");
                context.AssertEqual(4, changeCount, "settings.lifecycle.repeatCommitChangeCount");
            }
            finally
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);
            }
        }

        private static void CheckRunStatsPersistence(GFDiagnosticScenarioContext context, TotemRunResultSnapshot defeatResult)
        {
            var victory = TotemCombatService.BuildRunResult(true, "AllEnemiesDefeated", 3, 25f, 0, 20f);
            var stats = TotemRunStatsService.ApplyRunResult(null, victory);
            stats = TotemRunStatsService.ApplyRunResult(stats, defeatResult);
            context.AssertEqual(2, stats.totalRuns, "runStats.totalRuns");
            context.AssertEqual(1, stats.totalWins, "runStats.totalWins");
            context.AssertEqual(1, stats.totalLosses, "runStats.totalLosses");
            context.AssertEqual(12, stats.totalKills, "runStats.totalKills");
            context.AssertEqual(9, stats.bestKills, "runStats.bestKills");
            AssertNear(context, 65.6f, stats.totalPlayTimeSec, "runStats.totalPlayTime");
            AssertNear(context, 20f, stats.bestWinTimeSec, "runStats.bestWinTime");
            context.Assert(TotemRunStatsService.FormatSnapshot(stats).Contains("Total Runs: 2"), "Run stats formatter should include total runs.");

            string directory = Path.Combine(Path.GetTempPath(), "totem-warrior-diagnostics");
            string fileName = Path.Combine(directory, "run-stats-test.json");
            string backupFile = fileName + ".bak";
            string tempFile = fileName + ".tmp";
            try
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);

                context.Assert(TotemRunStatsService.TryWriteSnapshotToFile(fileName, stats, out string writeError), $"Run stats should save to temp file: {writeError}");
                context.Assert(File.Exists(fileName), "Run stats save file should exist.");
                context.Assert(TotemRunStatsService.TryReadSnapshotFromFile(fileName, out var loaded, out string readError), $"Run stats should load from temp file: {readError}");
                context.AssertEqual(2, loaded.totalRuns, "runStats.persistence.totalRuns");
                context.AssertEqual(12, loaded.totalKills, "runStats.persistence.totalKills");

                File.WriteAllText(fileName, "{not json}");
                context.Assert(!TotemRunStatsService.TryReadSnapshotFromFile(fileName, out _, out string invalidError), "Invalid run stats JSON should fail cleanly.");
                context.Assert(!string.IsNullOrWhiteSpace(invalidError), "Invalid run stats JSON should report an error.");
            }
            finally
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);
            }
        }

        private static void DeleteIfExists(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        private static TotemActorModel NewActor(int actorId, TotemActorKind kind, float health)
        {
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = actorId,
                Name = kind.ToString(),
                Kind = kind,
                Position = Vector3.zero,
                MaxHealth = health,
            });
        }

        private static void MoveActorTo(TotemActorModel actor, Vector3 position)
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

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }

        private sealed class InteractionInputProvider : ITotemInputProvider
        {
            private bool interactPressed;

            public float UnscaledTime => Time.unscaledTime;

            public Vector3 MousePosition => new Vector3(float.NaN, float.NaN, float.NaN);

            public void PressInteract()
            {
                interactPressed = true;
            }

            public void ClearPressed()
            {
                interactPressed = false;
            }

            public bool GetKey(KeyCode keyCode)
            {
                return false;
            }

            public bool GetKeyDown(KeyCode keyCode)
            {
                return keyCode == KeyCode.F && interactPressed;
            }

            public bool GetMouseButton(int button)
            {
                return false;
            }

            public bool GetMouseButtonDown(int button)
            {
                return false;
            }
        }
    }
}
#endif
