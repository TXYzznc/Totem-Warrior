#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemBalanceEnvelopeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        private const float ReferencePlayerHealth = 100f;

        public override string Name => "Totem Balance Envelope";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            string catalogPath = TotemDataService.GetGameplayCatalogPath();
            context.Detail("balance.catalog.path", catalogPath);
            context.Assert(File.Exists(catalogPath), $"Gameplay catalog file must exist for balance envelope: {catalogPath}");
            context.Assert(TotemDataService.TryLoadGameplayCatalogFromFile(catalogPath, out var catalog, out string error), $"Gameplay catalog should parse for balance envelope: {error}");

            if (catalog == null)
            {
                catalog = TotemGameplayCatalog.BuildDefault();
                context.Assert(false, "Balance envelope must run against the external Business gameplay catalog, not BuildDefault fallback.");
            }

            catalog.Normalize();

            var weapons = catalog.CreateWeaponDefinitions();
            var skills = catalog.CreateSkillDefinitions();
            var enemies = catalog.CreateEnemyDefinitions();
            var zones = catalog.CreateZonePhases();
            var bossPhases = catalog.CreateBossPhases();
            var profiles = catalog.CreateBotProfiles();
            var chestRewards = catalog.CreateChestRewardDefinitions();
            var generalShop = catalog.CreateShopOffers("general_shop");
            var alienShop = catalog.CreateShopOffers("alien_shop");

            CheckWeaponEnvelope(context, weapons, enemies);
            CheckSkillEnvelope(context, weapons, skills, enemies);
            CheckAiEnvelope(context, profiles);
            CheckZoneAndBossEnvelope(context, zones, bossPhases, enemies);
            CheckEconomyEnvelope(context, chestRewards, generalShop, alienShop);

            context.Pass("Totem first-round balance envelope is ready.");
        }

        private static void CheckWeaponEnvelope(GFDiagnosticScenarioContext context, TotemWeaponDefinition[] weapons, TotemEnemyDefinition[] enemies)
        {
            context.AssertEqual(5, weapons.Length, "balance.weapons.count");

            var light = RequireEnemy(context, enemies, TotemEnemyTier.Light, "balance.enemy.light");
            var elite = RequireEnemy(context, enemies, TotemEnemyTier.Elite, "balance.enemy.elite");
            var knife = RequireWeapon(context, weapons, "knife_basic");
            var hammer = RequireWeapon(context, weapons, "hammer_heavy");
            var pistol = RequireWeapon(context, weapons, "pistol_basic");
            var bow = RequireWeapon(context, weapons, "bow_charge");
            var energyFist = RequireWeapon(context, weapons, "energy_fist");

            var dpsValues = weapons.Select(GetWeaponDps).Where(value => value > 0f).ToArray();
            context.Assert(dpsValues.Length == weapons.Length, "Every first-round weapon should have positive DPS.");
            float minDps = dpsValues.Length > 0 ? dpsValues.Min() : 0f;
            float maxDps = dpsValues.Length > 0 ? dpsValues.Max() : 0f;
            AssertInRange(context, minDps, 28f, 34f, "balance.weapons.minDps");
            AssertInRange(context, maxDps, 42f, 50f, "balance.weapons.maxDps");
            AssertInRange(context, maxDps / Mathf.Max(0.001f, minDps), 1f, 1.75f, "balance.weapons.dpsSpread");

            foreach (var weapon in weapons)
            {
                float dps = GetWeaponDps(weapon);
                float lightTtk = GetTimeToKill(light.BaseHP, dps);
                float eliteTtk = GetTimeToKill(elite.BaseHP, dps);
                string key = $"balance.weapon.{weapon.WeaponId}";

                context.Detail($"{key}.damage", weapon.BaseDamage);
                context.Detail($"{key}.cooldown", weapon.Cooldown);
                context.Detail($"{key}.dps", dps);
                context.Detail($"{key}.lightTtkSec", lightTtk);
                context.Detail($"{key}.eliteTtkSec", eliteTtk);

                AssertInRange(context, weapon.Cooldown, 0.45f, 1.05f, $"{key}.cooldownEnvelope");
                AssertInRange(context, dps, 28f, 50f, $"{key}.dpsEnvelope");
                AssertInRange(context, lightTtk, 1.0f, 2.1f, $"{key}.lightTtkEnvelope");
                AssertInRange(context, eliteTtk, 3.0f, 5.6f, $"{key}.eliteTtkEnvelope");
            }

            context.Assert(GetWeaponDps(pistol) <= GetWeaponDps(energyFist) + 0.001f, "Pistol should not out-DPS energy fist in the first-round envelope.");
            context.Assert(GetWeaponDps(bow) <= GetWeaponDps(pistol) + 0.001f, "Charge bow base DPS should stay below pistol burst cadence.");
            context.Assert(GetWeaponDps(knife) < GetWeaponDps(hammer), "Heavy hammer should keep a higher sustained DPS than starter knife.");
            context.Assert(hammer.BaseDamage * hammer.ChargedMultiplier < elite.BaseHP * 0.75f, "Hammer charged hit should not remove more than 75% of an elite enemy.");
            context.Assert(bow.BaseDamage * bow.ChargedMultiplier < elite.BaseHP * 0.6f, "Charge bow should not delete an elite enemy with one charged hit.");
        }

        private static void CheckSkillEnvelope(GFDiagnosticScenarioContext context, TotemWeaponDefinition[] weapons, TotemSkillDefinition[] skills, TotemEnemyDefinition[] enemies)
        {
            context.Assert(skills.Length >= 14, "First-round skill catalog should include migrated player, choice, and BossPhaseConfig skills.");

            var light = RequireEnemy(context, enemies, TotemEnemyTier.Light, "balance.skill.enemy.light");
            var knife = RequireWeapon(context, weapons, "knife_basic");
            var fireball = RequireSkill(context, skills, "skill_fireball_01");
            var frost = RequireSkill(context, skills, "skill_frost_field_01");
            var chain = RequireSkill(context, skills, "skill_chain_lightning_01");
            var shield = RequireSkill(context, skills, "skill_ink_shield");

            float referenceDamage = Mathf.Max(1f, knife.BaseDamage);
            float fireballDamage = ResolveSkillBurstDamage(fireball, referenceDamage);
            float frostDamage = ResolveSkillBurstDamage(frost, referenceDamage);
            float chainDamage = ResolveSkillBurstDamage(chain, referenceDamage);
            float fireballDpsSupport = fireballDamage / Mathf.Max(0.001f, fireball.Cooldown);
            float weaponDps = GetWeaponDps(knife);

            context.Detail("balance.skill.fireball.damage", fireballDamage);
            context.Detail("balance.skill.fireball.supportDps", fireballDpsSupport);
            context.Detail("balance.skill.frost.damage", frostDamage);
            context.Detail("balance.skill.chain.damage", chainDamage);

            AssertInRange(context, fireball.Cooldown, 6f, 8f, "balance.skill.fireball.cooldown");
            AssertInRange(context, fireballDamage / light.BaseHP, 0.65f, 0.9f, "balance.skill.fireball.lightHpRatio");
            context.Assert(fireballDpsSupport <= weaponDps * 0.25f, "Fireball should remain burst support instead of replacing weapon DPS.");

            AssertInRange(context, frost.Cooldown, 9f, 11f, "balance.skill.frost.cooldown");
            AssertInRange(context, frostDamage / light.BaseHP, 0.45f, 0.65f, "balance.skill.frost.lightHpRatio");

            context.AssertEqual(TotemSkillChargeModel.Charges, chain.ChargeModel, "balance.skill.chain.chargeModel");
            context.Assert(chain.MaxCharges >= 2 && chain.MaxCharges <= 3, "Chain lightning should keep two or three tactical charges.");
            AssertInRange(context, chain.ChargeRegenTime, 7f, 9f, "balance.skill.chain.regen");
            AssertInRange(context, chainDamage / light.BaseHP, 0.35f, 0.5f, "balance.skill.chain.lightHpRatio");

            context.AssertEqual(0f, shield.DamageMultiplier, "balance.skill.shield.damageMul");
            AssertInRange(context, shield.Cooldown, 8f, 12f, "balance.skill.shield.cooldown");
        }

        private static void CheckAiEnvelope(GFDiagnosticScenarioContext context, TotemBotProfileDefinition[] profiles)
        {
            var smart = profiles.Where(profile => profile.ActorKind == TotemActorKind.SmartAi).ToArray();
            context.AssertEqual(20, smart.Length, "balance.ai.smartCount");

            float minAttackCooldown = smart.Length > 0 ? smart.Min(profile => profile.AttackCooldown) : 0f;
            float maxAttackCooldown = smart.Length > 0 ? smart.Max(profile => profile.AttackCooldown) : 0f;
            float avgAttackCooldown = Average(smart, profile => profile.AttackCooldown);
            context.Detail("balance.ai.attackCooldown.min", minAttackCooldown);
            context.Detail("balance.ai.attackCooldown.max", maxAttackCooldown);
            context.Detail("balance.ai.attackCooldown.avg", avgAttackCooldown);
            AssertInRange(context, minAttackCooldown, 0.82f, 0.9f, "balance.ai.attackCooldown.minEnvelope");
            AssertInRange(context, maxAttackCooldown, 1.05f, 1.2f, "balance.ai.attackCooldown.maxEnvelope");
            AssertInRange(context, avgAttackCooldown, 0.9f, 1.05f, "balance.ai.attackCooldown.avgEnvelope");

            var aggressive = RequirePersonality(context, smart, TotemAIPersonality.Aggressive);
            var conservative = RequirePersonality(context, smart, TotemAIPersonality.Conservative);
            var resource = RequirePersonality(context, smart, TotemAIPersonality.ResourceAcquisition);
            var boss = RequirePersonality(context, smart, TotemAIPersonality.BossPriority);
            var player = RequirePersonality(context, smart, TotemAIPersonality.PlayerPriority);

            context.Detail("balance.ai.aggressive.attackCooldownAvg", Average(aggressive, profile => profile.AttackCooldown));
            context.Detail("balance.ai.conservative.attackCooldownAvg", Average(conservative, profile => profile.AttackCooldown));
            context.Detail("balance.ai.resource.targetResourceWeightAvg", Average(resource, profile => profile.TargetResourceWeight));
            context.Detail("balance.ai.boss.targetBossWeightAvg", Average(boss, profile => profile.TargetBossWeight));
            context.Detail("balance.ai.player.targetPlayerWeightAvg", Average(player, profile => profile.TargetPlayerWeight));
            context.Detail("balance.ai.player.targetHumanoidAiWeightAvg", Average(player, profile => profile.TargetHumanoidAiWeight));

            context.Assert(Average(aggressive, profile => profile.AttackCooldown) < Average(conservative, profile => profile.AttackCooldown), "Aggressive Smart AI should attack more often than Conservative Smart AI.");
            context.Assert(Average(aggressive, profile => profile.RiskTolerance) > Average(conservative, profile => profile.RiskTolerance), "Aggressive Smart AI should tolerate more risk than Conservative Smart AI.");
            context.Assert(Average(conservative, profile => profile.AggroRadius) < Average(aggressive, profile => profile.AggroRadius), "Conservative Smart AI should keep a smaller aggro radius.");

            context.Assert(Average(resource, profile => profile.TargetResourceWeight) >= 1.8f, "Resource-acquisition Smart AI should strongly prefer resources.");
            context.Assert(Average(resource, profile => profile.TargetResourceWeight) > Average(resource, profile => profile.TargetPlayerWeight), "Resource-acquisition Smart AI should prefer resources over player hunting.");
            context.Assert(Average(resource, profile => profile.LootGreedFactor) > Average(aggressive, profile => profile.LootGreedFactor), "Resource-acquisition Smart AI should be greedier than aggressive hunters.");

            context.Assert(Average(boss, profile => profile.TargetBossWeight) >= 2.0f, "Boss-priority Smart AI should strongly prefer the active Boss target.");
            context.Assert(Average(boss, profile => profile.TargetBossWeight) > Average(boss, profile => profile.TargetPlayerWeight), "Boss-priority Smart AI should prefer Boss over player targets.");

            float playerWeight = Average(player, profile => profile.TargetPlayerWeight);
            float humanoidWeight = Average(player, profile => profile.TargetHumanoidAiWeight);
            context.Assert(playerWeight >= 1.2f && humanoidWeight >= 1.2f, "Player-priority Smart AI should hunt both real player and humanoid AI targets.");
            context.Assert(Mathf.Abs(playerWeight - humanoidWeight) <= 0.05f, "Player-priority Smart AI should treat real player and humanoid AI with near-equal target weight.");
        }

        private static void CheckZoneAndBossEnvelope(GFDiagnosticScenarioContext context, TotemZonePhase[] zones, TotemBossPhase[] bossPhases, TotemEnemyDefinition[] enemies)
        {
            context.AssertEqual(3, zones.Length, "balance.zone.phaseCount");
            context.AssertEqual(3, bossPhases.Length, "balance.boss.phaseCount");

            var orderedZones = zones.OrderBy(phase => phase.Id).ToArray();
            for (int i = 1; i < orderedZones.Length; i++)
            {
                context.Assert(orderedZones[i].TargetRadius < orderedZones[i - 1].TargetRadius, $"Zone phase {orderedZones[i].Id} should shrink radius.");
                context.Assert(orderedZones[i].OutZoneDamage > orderedZones[i - 1].OutZoneDamage, $"Zone phase {orderedZones[i].Id} should increase out-zone pressure.");
            }

            var finalZone = orderedZones.LastOrDefault() ?? new TotemZonePhase();
            float finalOutZoneTtk = finalZone.OutZoneDamage > 0f ? ReferencePlayerHealth / finalZone.OutZoneDamage : 0f;
            context.Detail("balance.zone.final.outZoneDamage", finalZone.OutZoneDamage);
            context.Detail("balance.zone.final.playerTtkSec", finalOutZoneTtk);
            AssertInRange(context, finalZone.OutZoneDamage, 10f, 13f, "balance.zone.final.damageEnvelope");
            AssertInRange(context, finalOutZoneTtk, 8f, 12f, "balance.zone.final.playerTtkEnvelope");

            var orderedBossPhases = bossPhases.OrderBy(phase => phase.PhaseIndex).ToArray();
            for (int i = 1; i < orderedBossPhases.Length; i++)
            {
                context.Assert(orderedBossPhases[i].HPThreshold < orderedBossPhases[i - 1].HPThreshold, $"Boss phase {orderedBossPhases[i].PhaseIndex} should trigger at a lower HP threshold.");
                context.Assert(orderedBossPhases[i].EnrageMultiplier >= orderedBossPhases[i - 1].EnrageMultiplier, $"Boss phase {orderedBossPhases[i].PhaseIndex} should not reduce enrage pressure.");
            }

            var boss = RequireEnemy(context, enemies, TotemEnemyTier.Boss, "balance.boss.enemy");
            var finalBossPhase = orderedBossPhases.LastOrDefault() ?? new TotemBossPhase();
            float phaseThreeDamage = boss.BaseDamage * finalBossPhase.EnrageMultiplier;
            context.Detail("balance.boss.phase3.enrage", finalBossPhase.EnrageMultiplier);
            context.Detail("balance.boss.phase3.rawDamage", phaseThreeDamage);
            AssertInRange(context, finalBossPhase.EnrageMultiplier, 1.25f, 1.35f, "balance.boss.phase3.enrageEnvelope");
            AssertInRange(context, phaseThreeDamage, 40f, 50f, "balance.boss.phase3.damageEnvelope");
            context.Assert(phaseThreeDamage < ReferencePlayerHealth * 0.5f, "Boss phase 3 raw hit should remain below half of the reference player HP.");
        }

        private static void CheckEconomyEnvelope(GFDiagnosticScenarioContext context, TotemChestRewardDefinition[] chestRewards, TotemShopOffer[] generalShop, TotemShopOffer[] alienShop)
        {
            var commonGold = RequireChestReward(context, chestRewards, "chest_common", TotemChestRewardType.Gold);
            var rareGold = RequireChestReward(context, chestRewards, "chest_rare", TotemChestRewardType.Gold);

            context.AssertEqual(100, chestRewards.Where(reward => reward.ChestId == "chest_common").Sum(reward => reward.Probability), "balance.economy.commonChest.probability");
            context.AssertEqual(100, chestRewards.Where(reward => reward.ChestId == "chest_rare").Sum(reward => reward.Probability), "balance.economy.rareChest.probability");
            AssertInRange(context, commonGold.RewardAmount, 40f, 55f, "balance.economy.commonGold.amount");
            AssertInRange(context, rareGold.RewardAmount, 85f, 110f, "balance.economy.rareGold.amount");

            context.AssertEqual(10, generalShop.Length, "balance.economy.generalShop.count");
            context.AssertEqual(5, alienShop.Length, "balance.economy.alienShop.count");

            int generalInkPrice = MinPrice(generalShop, offer => string.Equals(offer.Category, "Ink", StringComparison.OrdinalIgnoreCase));
            int generalWeaponPrice = MinPrice(generalShop, offer => offer.RewardType == TotemShopRewardType.WeaponUpgrade);
            int generalSkillPrice = MinPrice(generalShop, offer => offer.RewardType == TotemShopRewardType.SkillCore);
            int alienRareInkPrice = MinPrice(alienShop, offer => string.Equals(offer.Category, "RareInk", StringComparison.OrdinalIgnoreCase));

            context.Detail("balance.economy.general.inkPrice", generalInkPrice);
            context.Detail("balance.economy.general.weaponPrice", generalWeaponPrice);
            context.Detail("balance.economy.general.skillPrice", generalSkillPrice);
            context.Detail("balance.economy.alien.rareInkPrice", alienRareInkPrice);

            AssertInRange(context, generalInkPrice / Mathf.Max(1f, commonGold.RewardAmount), 1.2f, 2.0f, "balance.economy.inkToCommonChest");
            AssertInRange(context, generalWeaponPrice / Mathf.Max(1f, commonGold.RewardAmount), 4.0f, 6.0f, "balance.economy.weaponToCommonChest");
            AssertInRange(context, generalSkillPrice / Mathf.Max(1f, commonGold.RewardAmount), 3.0f, 4.0f, "balance.economy.skillToCommonChest");
            AssertInRange(context, alienRareInkPrice / Mathf.Max(1f, rareGold.RewardAmount), 3.0f, 4.0f, "balance.economy.rareInkToRareChest");
        }

        private static TotemWeaponDefinition RequireWeapon(GFDiagnosticScenarioContext context, TotemWeaponDefinition[] weapons, string weaponId)
        {
            var weapon = weapons.FirstOrDefault(item => item.WeaponId == weaponId);
            context.Assert(weapon != null, $"Balance envelope requires weapon: {weaponId}");
            return weapon ?? new TotemWeaponDefinition { WeaponId = weaponId, BaseDamage = 0f, Cooldown = 999f };
        }

        private static TotemSkillDefinition RequireSkill(GFDiagnosticScenarioContext context, TotemSkillDefinition[] skills, string skillId)
        {
            var skill = skills.FirstOrDefault(item => item.SkillId == skillId);
            context.Assert(skill != null, $"Balance envelope requires skill: {skillId}");
            return skill ?? new TotemSkillDefinition { SkillId = skillId, Cooldown = 999f };
        }

        private static TotemEnemyDefinition RequireEnemy(GFDiagnosticScenarioContext context, TotemEnemyDefinition[] enemies, TotemEnemyTier tier, string key)
        {
            var enemy = enemies.FirstOrDefault(item => item.Tier == tier);
            context.Assert(enemy != null, $"Balance envelope requires enemy tier: {tier}");
            context.Detail($"{key}.hp", enemy?.BaseHP ?? 0f);
            context.Detail($"{key}.damage", enemy?.BaseDamage ?? 0f);
            return enemy ?? new TotemEnemyDefinition { EnemyId = tier.ToString(), Tier = tier, BaseHP = 1f, BaseDamage = 0f };
        }

        private static TotemChestRewardDefinition RequireChestReward(GFDiagnosticScenarioContext context, TotemChestRewardDefinition[] rewards, string chestId, TotemChestRewardType rewardType)
        {
            var reward = rewards.FirstOrDefault(item => item.ChestId == chestId && item.RewardType == rewardType);
            context.Assert(reward != null, $"Balance envelope requires chest reward: {chestId}/{rewardType}");
            context.Detail($"balance.economy.{chestId}.{rewardType}.amount", reward?.RewardAmount ?? 0);
            context.Detail($"balance.economy.{chestId}.{rewardType}.probability", reward?.Probability ?? 0);
            return reward ?? new TotemChestRewardDefinition { ChestId = chestId, RewardType = rewardType, RewardAmount = 0, Probability = 0 };
        }

        private static TotemBotProfileDefinition[] RequirePersonality(GFDiagnosticScenarioContext context, TotemBotProfileDefinition[] profiles, TotemAIPersonality personality)
        {
            var matches = profiles.Where(profile => profile.Personality == personality).ToArray();
            context.Detail($"balance.ai.personality.{personality}.count", matches.Length);
            context.Assert(matches.Length > 0, $"Balance envelope requires Smart AI personality: {personality}");
            return matches;
        }

        private static float Average(TotemBotProfileDefinition[] profiles, Func<TotemBotProfileDefinition, float> selector)
        {
            return profiles.Length == 0 ? 0f : profiles.Average(selector);
        }

        private static float ResolveSkillBurstDamage(TotemSkillDefinition skill, float referenceDamage)
        {
            return skill.Damage > 0f ? skill.Damage : referenceDamage * Mathf.Max(0f, skill.DamageMultiplier);
        }

        private static float GetWeaponDps(TotemWeaponDefinition weapon)
        {
            return weapon.Cooldown <= 0f ? 0f : weapon.BaseDamage / weapon.Cooldown;
        }

        private static float GetTimeToKill(float hp, float dps)
        {
            return dps <= 0f ? 999f : hp / dps;
        }

        private static int MinPrice(TotemShopOffer[] offers, Func<TotemShopOffer, bool> predicate)
        {
            var prices = offers.Where(predicate).Select(offer => offer.Price).Where(price => price > 0).ToArray();
            return prices.Length == 0 ? 0 : prices.Min();
        }

        private static void AssertInRange(GFDiagnosticScenarioContext context, float actual, float minInclusive, float maxInclusive, string key)
        {
            context.Detail($"{key}.actual", actual);
            context.Assert(actual >= minInclusive && actual <= maxInclusive, $"{key}: expected {minInclusive}..{maxInclusive}, actual={actual}");
        }
    }
}
#endif
