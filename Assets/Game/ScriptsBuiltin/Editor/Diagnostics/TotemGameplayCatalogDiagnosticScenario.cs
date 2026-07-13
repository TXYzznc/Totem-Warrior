#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemGameplayCatalogDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Gameplay Catalog";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckGameplayCatalogFile(context);
            CheckGeneratedContracts(context);
            CheckDataServiceLoadPath(context);
            context.Pass("Totem gameplay catalog contract is ready.");
        }

        private static void CheckGameplayCatalogFile(GFDiagnosticScenarioContext context)
        {
            string path = TotemDataService.GetGameplayCatalogPath();
            context.Detail("catalog.path", path);
            context.Assert(File.Exists(path), $"Gameplay catalog file must exist: {path}");
            context.Assert(TotemDataService.TryLoadGameplayCatalogFromFile(path, out var catalog, out string error), $"Gameplay catalog should parse: {error}");

            var errors = new List<string>();
            context.Assert(TotemGameplayCatalogValidator.Validate(catalog, errors), $"Gameplay catalog validation failed: {string.Join("; ", errors)}");
            context.Detail("catalog.source", catalog.source);
            context.AssertEqual("GameData/AIData/DataTables/Business", catalog.source, "catalog.source.businessDataTables");
            CheckGenerationFingerprint(context, catalog);
            context.Detail("catalog.schemaVersion", catalog.schemaVersion);
            context.Detail("catalog.itemCount", catalog.items.Length);
            context.Detail("catalog.resourceCount", catalog.resources.Length);
            context.Detail("catalog.weaponCount", catalog.weapons.Length);
            context.Detail("catalog.projectileCount", catalog.projectiles.Length);
            context.Detail("catalog.weaponTraitCount", catalog.weaponTraits.Length);
            context.Detail("catalog.weaponDropCount", catalog.weaponDrops.Length);
            context.Detail("catalog.chestRewardCount", catalog.chestRewards.Length);
            context.Detail("catalog.mapTemplateCount", catalog.mapTemplates.Length);
            context.Detail("catalog.tattooReadingTimeCount", catalog.tattooReadingTimes.Length);
            context.Detail("catalog.tattooElementCount", catalog.tattooElements.Length);
            context.Detail("catalog.tattooShapeCount", catalog.tattooShapes.Length);
            context.Detail("catalog.tattooEnchantAffixCount", catalog.tattooEnchantAffixes.Length);
            context.Detail("catalog.tattooEnchantRecipeCount", catalog.tattooEnchantRecipes.Length);
            context.Detail("catalog.enemyCount", catalog.enemies.Length);
            context.Detail("catalog.enemyAbilityCount", catalog.enemyAbilities.Length);
            context.Detail("catalog.encounterSpawnCount", catalog.encounterSpawns.Length);
            context.Detail("catalog.enemyLootCount", catalog.enemyLoot.Length);
            context.Detail("catalog.audioCueCount", catalog.audioCues.Length);
            context.Detail("catalog.npcCount", catalog.npcs.Length);
            context.Detail("catalog.shopStockCount", catalog.shopStocks.Length);
            context.Detail("catalog.merchantSlotCount", catalog.merchantSlots.Length);
            context.Detail("catalog.eventCount", catalog.events.Length);
            context.Detail("catalog.choiceOptionCount", catalog.choiceOptions.Length);
            context.Detail("catalog.botProfileCount", catalog.botProfiles.Length);
            context.Detail("catalog.botBuildPresetCount", catalog.botBuildPresets.Length);
        }

        private static void CheckGeneratedContracts(GFDiagnosticScenarioContext context)
        {
            TotemDataService.TryLoadGameplayCatalogFromFile(TotemDataService.GetGameplayCatalogPath(), out var catalog, out _);
            if (catalog == null)
            {
                catalog = TotemGameplayCatalog.BuildDefault();
            }

            catalog.Normalize();

            var items = catalog.CreateItemDefinitions();
            context.AssertEqual(31, items.Length, "catalog.items.count");
            context.Assert(items.Count(item => item.ItemType == TotemItemType.InkBottle) == 21, "Catalog must preserve 21 ink bottle item rows.");
            context.Assert(items.Any(item => item.ItemId == 1 && item.ItemType == TotemItemType.Coin && item.MaxStack == 9999), "Catalog must preserve Coin item.");
            context.Assert(items.Any(item => item.ItemId == 2703 && item.ItemType == TotemItemType.InkBottle && item.Rarity == "Legendary" && item.BasePrice == 220), "Catalog must preserve White Premium ink price/rarity.");
            context.Assert(items.Any(item => item.ItemId == 4004 && item.ItemType == TotemItemType.Equipment && item.BasePrice == 500), "Catalog must preserve Legendary equipment price.");
            context.Assert(items.Any(item => item.ItemId == 5001 && item.ItemType == TotemItemType.Antidote && item.SubType == "Detox"), "Catalog must preserve Detox antidote.");

            var resources = catalog.CreateResourceDefinitions();
            context.AssertEqual(14, resources.Length, "catalog.resources.count");
            context.Assert(resources.Any(item => item.Id == 1001 && item.AssetKey == "tattoo.part.head" && item.ActiveAssetPath.EndsWith("Head.png")), "Catalog must preserve Head tattoo resource.");
            context.Assert(resources.Any(item => item.Id == 1208 && item.AssetKey == "tattoo.pattern.beast" && item.ActiveAssetPath.EndsWith("Beast.png")), "Catalog must preserve Beast pattern resource.");

            var weapons = catalog.CreateWeaponDefinitions();
            context.AssertEqual(5, weapons.Length, "catalog.weapons.count");
            context.Assert(weapons.Any(item => item.WeaponId == "bow_charge" && item.RequiresCharge), "Catalog must include charge bow.");
            var pistol = weapons.FirstOrDefault(item => item.WeaponId == "pistol_basic");
            context.Assert(pistol != null, "Catalog must include pistol.");
            AssertNear(context, 16f, pistol.BaseDamage, "catalog.weapon.pistol.damage");
            AssertNear(context, 30f / 60f, pistol.Cooldown, "catalog.weapon.pistol.cooldown");
            context.AssertEqual("bullet_pistol", pistol.ProjectileId, "catalog.weapon.pistol.projectile");
            context.AssertEqual("trait_pierce", pistol.NormalTraitId, "catalog.weapon.pistol.normalTrait");
            var bow = weapons.FirstOrDefault(item => item.WeaponId == "bow_charge");
            context.Assert(bow != null && bow.ChargedMultiplier >= 2.4f, "Catalog bow should preserve tuned charged multiplier.");
            context.AssertEqual("trait_chain", bow.ChargedTraitId, "catalog.weapon.bow.chargedTrait");

            var projectiles = catalog.CreateProjectileDefinitions();
            context.AssertEqual(2, projectiles.Length, "catalog.projectiles.count");
            context.Assert(projectiles.Any(item => item.ProjectileId == "bullet_pistol" && !item.Piercing && item.PoolSize == 60), "Catalog must preserve pistol projectile.");
            context.Assert(projectiles.Any(item => item.ProjectileId == "arrow_bow" && item.Piercing && item.PoolSize == 30), "Catalog must preserve bow projectile.");

            var weaponTraits = catalog.CreateWeaponTraitDefinitions();
            context.AssertEqual(10, weaponTraits.Length, "catalog.weaponTraits.count");
            context.Assert(weaponTraits.Any(item => item.TraitId == "trait_multishot" && item.EffectType == TotemWeaponTraitEffectType.MultiShot), "Catalog must preserve multishot trait.");
            context.Assert(weaponTraits.Any(item => item.TraitId == "trait_pull" && item.EffectType == TotemWeaponTraitEffectType.Pull), "Catalog must preserve pull trait.");
            var lifesteal = weaponTraits.FirstOrDefault(item => item.TraitId == "trait_lifesteal");
            context.Assert(lifesteal != null, "Catalog must preserve Life Steal trait.");
            context.AssertEqual(TotemWeaponTraitEffectType.Quick, lifesteal?.EffectType ?? TotemWeaponTraitEffectType.Unknown, "catalog.weaponTraits.lifesteal.effectType");
            AssertNear(context, 0.08f, lifesteal?.EffectParam1 ?? -1f, "catalog.weaponTraits.lifesteal.ratio");
            AssertNear(context, 12f, lifesteal?.EffectParam2 ?? -1f, "catalog.weaponTraits.lifesteal.cap");
            context.Assert(!(lifesteal?.Description ?? string.Empty).Contains("Reserved", StringComparison.OrdinalIgnoreCase), "Life Steal trait description must describe active behavior, not reserved placeholder wording.");

            var weaponDrops = catalog.CreateWeaponDropDefinitions();
            context.AssertEqual(15, weaponDrops.Length, "catalog.weaponDrops.count");
            context.AssertEqual(5, weaponDrops.Count(item => item.DropSource == "Elite"), "catalog.weaponDrops.eliteCount");
            context.AssertEqual(5, weaponDrops.Count(item => item.DropSource == "Chest"), "catalog.weaponDrops.chestCount");
            context.AssertEqual(5, weaponDrops.Count(item => item.DropSource == "Merchant"), "catalog.weaponDrops.merchantCount");
            context.Assert(weaponDrops.Any(item => item.WeaponId == "bow_charge" && item.MinRoomIndex == 3 && item.DropSource == "Elite"), "Catalog elite drops should preserve Bow room gate.");

            var chestRewards = catalog.CreateChestRewardDefinitions();
            context.AssertEqual(6, chestRewards.Length, "catalog.chestRewards.count");
            context.AssertEqual(100, chestRewards.Where(item => item.ChestId == "chest_common").Sum(item => item.Probability), "catalog.chestRewards.commonProbability");
            context.AssertEqual(100, chestRewards.Where(item => item.ChestId == "chest_rare").Sum(item => item.Probability), "catalog.chestRewards.rareProbability");
            context.Assert(chestRewards.Any(item => item.ChestId == "chest_common" && item.RewardType == TotemChestRewardType.Gold && item.RewardAmount == 45), "Catalog common chest should preserve tuned Gold 45 reward.");
            context.Assert(chestRewards.Any(item => item.ChestId == "chest_rare" && item.RewardType == TotemChestRewardType.Potion && item.RewardAmount == 2), "Catalog rare chest should preserve Potion x2 reward.");

            var mapTemplates = catalog.CreateMapTemplates();
            context.AssertEqual(3, mapTemplates.Length, "catalog.mapTemplates.count");
            var ruins = mapTemplates.FirstOrDefault(item => item.Id == 1);
            var alienHive = mapTemplates.FirstOrDefault(item => item.Id == 2);
            var virusSwamp = mapTemplates.FirstOrDefault(item => item.Id == 3);
            context.AssertEqual("AI_RUINS", ruins?.ThemeName ?? string.Empty, "catalog.mapTemplates.ruins.theme");
            AssertNear(context, 400f, ruins?.MapSize ?? -1f, "catalog.mapTemplates.ruins.mapSize");
            AssertNear(context, 40f, ruins?.MinRoomSize ?? -1f, "catalog.mapTemplates.ruins.minRoomSize");
            context.AssertEqual("ALIEN_HIVE", alienHive?.ThemeName ?? string.Empty, "catalog.mapTemplates.alienHive.theme");
            context.AssertEqual("#7DFF88", alienHive?.HudAccentColor ?? string.Empty, "catalog.mapTemplates.alienHive.accent");
            context.AssertEqual("VIRUS_SWAMP", virusSwamp?.ThemeName ?? string.Empty, "catalog.mapTemplates.virusSwamp.theme");
            context.AssertEqual("#233A35", virusSwamp?.DominantColor ?? string.Empty, "catalog.mapTemplates.virusSwamp.dominant");

            var tattoos = catalog.CreateTattooDefinitions();
            context.Detail("catalog.firstRoundContent.tattooCombinations", tattoos.Length);
            context.AssertEqual(336, tattoos.Length, "catalog.tattoos.combinationCount");
            context.Assert(tattoos.Any(item => item.PartId == 4 && item.ColorId == 1 && item.PatternId == 1 && item.TriggerEvent == "AttackHitEvent"), "Catalog tattoo mapping should preserve RightArm/AttackHitEvent.");
            var rightArmRedLine = tattoos.FirstOrDefault(item => item.PartId == 4 && item.ColorId == 1 && item.PatternId == 1);
            context.AssertEqual("WeaponDamage", rightArmRedLine?.ScaleStat ?? string.Empty, "catalog.tattoos.rightArm.scaleStat");
            AssertNear(context, 0.8f, rightArmRedLine?.ScaleFactor ?? -1f, "catalog.tattoos.rightArm.scaleFactor");
            AssertNear(context, 1f, rightArmRedLine?.Magnitude ?? -1f, "catalog.tattoos.oldMultiplier.rightArmRedLine");
            var whiteBeast = tattoos.FirstOrDefault(item => item.ColorId == 7 && item.PatternId == 8);
            AssertNear(context, 1f, whiteBeast?.ColorMultiplier ?? -1f, "catalog.tattoos.white.colorMultiplier");
            AssertNear(context, 1f, whiteBeast?.PatternMultiplier ?? -1f, "catalog.tattoos.beast.patternMultiplier");
            var elements = catalog.CreateTattooElementDefinitions();
            context.AssertEqual(7, elements.Length, "catalog.tattoos.elementCount");
            context.Assert(elements.Any(item => item.Element == TotemTattooElement.Fire && Mathf.Abs(item.Param1 - 2f) <= 0.001f && Mathf.Abs(item.Param2 - 3f) <= 0.001f), "Catalog must preserve Fire element params.");
            context.Assert(elements.Any(item => item.Element == TotemTattooElement.Pure && Mathf.Abs(item.Param1 - 0.2f) <= 0.001f && Mathf.Abs(item.Param3 - 5f) <= 0.001f), "Catalog must preserve Pure element params.");
            var shapes = catalog.CreateTattooShapeDefinitions();
            context.AssertEqual(8, shapes.Length, "catalog.tattoos.shapeCount");
            context.Assert(shapes.Any(item => item.Shape == TotemTattooShape.AOEBurst && Mathf.Abs(item.Param1 - 0.6f) <= 0.001f && Mathf.Abs(item.Param2 - 5f) <= 0.001f), "Catalog must preserve AOEBurst shape params.");
            context.Assert(shapes.Any(item => item.Shape == TotemTattooShape.ProbBurst && Mathf.Abs(item.Param3 - 12345f) <= 0.001f), "Catalog must preserve ProbBurst seed param.");
            var readingTimes = catalog.CreateTattooReadingTimeDefinitions();
            context.AssertEqual(6, readingTimes.Length, "catalog.tattoos.readingTimeCount");
            AssertNear(context, 8f, readingTimes.FirstOrDefault(item => item.PartId == 1)?.DurationSec ?? -1f, "catalog.tattoos.headReadingSec");
            AssertNear(context, 5f, readingTimes.FirstOrDefault(item => item.PartId == 4)?.DurationSec ?? -1f, "catalog.tattoos.rightArmReadingSec");
            AssertNear(context, 3f, readingTimes.FirstOrDefault(item => item.PartId == 6)?.DurationSec ?? -1f, "catalog.tattoos.rightLegReadingSec");
            var enchantAffixes = catalog.CreateTattooEnchantAffixDefinitions();
            context.AssertEqual(24, enchantAffixes.Length, "catalog.tattoos.enchantAffixCount");
            context.Assert(enchantAffixes.Any(item => item.Id == 1 && item.ColorTier == "Common" && item.AffixType == TotemTattooEnchantAffixType.ElementDamageBonus && item.StatKey == "ElementDmg"), "Catalog must preserve Common ElementDmg affix.");
            context.Assert(enchantAffixes.Any(item => item.Id == 12 && item.ConditionKey == "DistanceGt8m" && item.ConditionVal >= 8f), "Catalog must preserve Rare distance-gated affix.");
            context.Assert(enchantAffixes.Any(item => item.Id == 20 && item.ConditionKey == "AfterDodge"), "Catalog must preserve Legendary dodge-gated affix.");
            var enchantRecipes = catalog.CreateTattooEnchantRecipeDefinitions();
            context.AssertEqual(3, enchantRecipes.Length, "catalog.tattoos.enchantRecipeCount");
            context.Assert(enchantRecipes.Any(item => item.ColorTier == "Common" && item.CoinCost == 200 && item.MaxAffixPerSlot == 2), "Catalog must preserve Common enchant recipe.");
            context.Assert(enchantRecipes.Any(item => item.ColorTier == "Legendary" && item.CoinCost == 500), "Catalog must preserve Legendary enchant recipe.");

            var skills = catalog.CreateSkillDefinitions();
            context.AssertEqual(14, skills.Length, "catalog.skills.count");
            var fireball = skills.FirstOrDefault(item => item.SkillId == "skill_fireball_01");
            context.Assert(fireball != null, "Catalog must include migrated Fireball skill.");
            AssertNear(context, 7f, fireball?.Cooldown ?? -1f, "catalog.skill.fireball.cooldown");
            AssertNear(context, 2.4f, fireball?.DamageMultiplier ?? -1f, "catalog.skill.fireball.damageMul");
            AssertNear(context, 3f, fireball?.Radius ?? -1f, "catalog.skill.fireball.hitRadius");
            context.AssertEqual(TotemSkillChargeModel.Cooldown, fireball?.ChargeModel ?? TotemSkillChargeModel.HoldRelease, "catalog.skill.fireball.chargeModel");
            var chain = skills.FirstOrDefault(item => item.SkillId == "skill_chain_lightning_01");
            context.Assert(chain != null, "Catalog must include migrated Chain Lightning skill.");
            context.AssertEqual(TotemSkillChargeModel.Charges, chain?.ChargeModel ?? TotemSkillChargeModel.Cooldown, "catalog.skill.chain.chargeModel");
            context.AssertEqual(3, chain?.MaxCharges ?? -1, "catalog.skill.chain.maxCharges");
            AssertNear(context, 8f, chain?.ChargeRegenTime ?? -1f, "catalog.skill.chain.regen");
            var stealth = skills.FirstOrDefault(item => item.SkillId == "skill_stealth_01");
            context.Assert(stealth != null, "Catalog must include migrated Stealth skill.");
            context.AssertEqual(TotemSkillChargeModel.HoldRelease, stealth?.ChargeModel ?? TotemSkillChargeModel.Cooldown, "catalog.skill.stealth.chargeModel");
            AssertNear(context, 1.5f, stealth?.HoldDuration ?? -1f, "catalog.skill.stealth.holdDuration");
            var phaseDash = skills.FirstOrDefault(item => item.SkillId == "skill_phase_dash");
            context.Assert(phaseDash != null, "Catalog must include SkillAcquire target skill_phase_dash.");
            AssertNear(context, 5f, phaseDash?.Cooldown ?? -1f, "catalog.skill.phaseDash.cooldown");
            var inkShield = skills.FirstOrDefault(item => item.SkillId == "skill_ink_shield");
            context.Assert(inkShield != null, "Catalog must include SkillAcquire target skill_ink_shield.");
            AssertNear(context, 4f, inkShield?.Radius ?? -1f, "catalog.skill.inkShield.radius");
            context.Assert(skills.Any(item => item.SkillId == "skill_stomp"), "Catalog must include BossPhase target skill_stomp.");
            context.Assert(skills.Any(item => item.SkillId == "skill_beam"), "Catalog must include BossPhase target skill_beam.");
            context.Assert(skills.Any(item => item.SkillId == "skill_summon"), "Catalog must include BossPhase target skill_summon.");
            context.Assert(skills.Any(item => item.SkillId == "skill_enrage_aoe"), "Catalog must include BossPhase target skill_enrage_aoe.");
            context.Assert(!skills.Any(item => item.SkillId == "boss_phase_bolt"), "Catalog must not keep stale boss_phase_bolt after BossPhaseConfig skill closure.");

            var zonePhases = catalog.CreateZonePhases();
            context.AssertEqual(3, zonePhases.Length, "catalog.zonePhases.count");
            context.Assert(zonePhases[2].TargetRadius < zonePhases[0].TargetRadius, "Zone phases should shrink radius.");
            context.AssertEqual("Phase0_Slow", zonePhases[0].PhaseName, "catalog.zonePhases.phase0.name");
            AssertNear(context, 180f, zonePhases[0].Duration, "catalog.zonePhases.phase0.duration");
            AssertNear(context, 65f, zonePhases[0].TargetRadius, "catalog.zonePhases.phase0.radius");
            AssertNear(context, 2f, zonePhases[0].OutZoneDamage, "catalog.zonePhases.phase0.damage");
            context.AssertEqual("None", zonePhases[0].CenterOffsetMode, "catalog.zonePhases.phase0.offset");
            AssertNear(context, 180f, zonePhases[1].StartTime, "catalog.zonePhases.phase1.start");
            AssertNear(context, 360f, zonePhases[1].Duration, "catalog.zonePhases.phase1.duration");
            AssertNear(context, 35f, zonePhases[1].TargetRadius, "catalog.zonePhases.phase1.radius");
            AssertNear(context, 5f, zonePhases[1].OutZoneDamage, "catalog.zonePhases.phase1.damage");
            context.AssertEqual("Drift", zonePhases[1].CenterOffsetMode, "catalog.zonePhases.phase1.offset");
            AssertNear(context, 540f, zonePhases[2].StartTime, "catalog.zonePhases.phase2.start");
            AssertNear(context, 360f, zonePhases[2].Duration, "catalog.zonePhases.phase2.duration");
            AssertNear(context, 5f, zonePhases[2].TargetRadius, "catalog.zonePhases.phase2.radius");
            AssertNear(context, 12f, zonePhases[2].OutZoneDamage, "catalog.zonePhases.phase2.damage");
            context.AssertEqual("Fixed", zonePhases[2].CenterOffsetMode, "catalog.zonePhases.phase2.offset");

            var enemies = catalog.CreateEnemyDefinitions();
            context.Detail("catalog.firstRoundContent.enemyBodyRows", enemies.Length);
            context.AssertEqual(15, enemies.Length, "catalog.enemies.count");
            context.AssertEqual(8, enemies.Count(item => item.Tier == TotemEnemyTier.Light), "catalog.enemies.lightCount");
            context.AssertEqual(4, enemies.Count(item => item.Tier == TotemEnemyTier.Elite), "catalog.enemies.eliteCount");
            context.AssertEqual(3, enemies.Count(item => item.Tier == TotemEnemyTier.Boss), "catalog.enemies.bossCount");

            var enemyAbilities = catalog.CreateEnemyAbilityDefinitions();
            var encounters = catalog.CreateEncounterSpawnDefinitions();
            var enemyLoot = catalog.CreateEnemyLootDefinitions();
            context.AssertEqual(25, enemyAbilities.Length, "catalog.enemyAbilities.count");
            context.AssertEqual(9, encounters.Length, "catalog.encounters.count");
            context.AssertEqual(37, enemyLoot.Length, "catalog.enemyLoot.count");

            var enemyIds = enemies.Select(item => item.EnemyId).ToHashSet(StringComparer.Ordinal);
            var abilityIds = enemyAbilities.Select(item => item.AbilityId).ToHashSet(StringComparer.Ordinal);
            var lootEntryIds = enemyLoot.Select(item => item.LootEntryId).ToHashSet(StringComparer.Ordinal);
            var lootTableIds = enemyLoot.Select(item => item.LootTableId).ToHashSet(StringComparer.Ordinal);
            var poolIds = enemies.SelectMany(item => SplitIds(item.PoolIds)).ToHashSet(StringComparer.Ordinal);
            context.AssertEqual(enemies.Length, enemyIds.Count, "catalog.enemies.uniqueIds");
            context.AssertEqual(enemyAbilities.Length, abilityIds.Count, "catalog.enemyAbilities.uniqueIds");
            context.AssertEqual(enemyLoot.Length, lootEntryIds.Count, "catalog.enemyLoot.uniqueIds");
            context.Assert(enemies.All(item => !string.IsNullOrWhiteSpace(item.RuntimeAssetKey)), "Every EnemyConfig row must bind a RuntimeAssetKey.");
            context.Assert(enemies.All(item => !string.IsNullOrWhiteSpace(item.BehaviorProfileId)), "Every EnemyConfig row must bind a behavior profile.");
            context.Assert(enemies.All(item => SplitIds(item.AbilityIds).All(abilityIds.Contains)), "Every EnemyConfig AbilityIds foreign key must resolve.");
            context.Assert(enemies.All(item => lootTableIds.Contains(item.LootTableId)), "Every EnemyConfig LootTableId foreign key must resolve.");
            context.Assert(enemies.All(item => SplitIds(item.GuaranteedLootIds).All(lootEntryIds.Contains)), "Every EnemyConfig GuaranteedLootIds foreign key must resolve.");
            context.Assert(enemyAbilities.Where(item => item.AbilityType == TotemEnemyAbilityType.Summon).All(item => enemyIds.Contains(item.SummonEnemyId)), "Every summon ability must resolve its EnemyConfig foreign key.");
            context.Assert(encounters.All(item => SplitIds(item.EnemyPoolIds).All(poolIds.Contains)), "Every EncounterSpawnConfig pool foreign key must resolve.");
            context.AssertEqual(3, encounters.Count(item => item.Unique), "catalog.encounters.uniqueBossSchedules");

            var bossPhases = catalog.CreateBossPhases();
            context.Detail("catalog.firstRoundContent.bossPhaseRows", bossPhases.Length);
            context.AssertEqual(9, bossPhases.Length, "catalog.bossPhases.count");
            var bossIds = enemies.Where(item => item.Tier == TotemEnemyTier.Boss).Select(item => item.EnemyId).ToHashSet(StringComparer.Ordinal);
            var phaseGroups = bossPhases.GroupBy(phase => phase.BossId).ToArray();
            context.AssertEqual(3, phaseGroups.Length, "catalog.bossPhases.bossGroupCount");
            context.Assert(phaseGroups.All(group => bossIds.Contains(group.Key)), "Every BossPhaseConfig BossId foreign key must resolve.");
            context.Assert(phaseGroups.All(group => group.Count() == 3), "Each Boss must own exactly three phase rows.");
            context.Assert(phaseGroups.All(group => group.Select(phase => phase.PhaseIndex).OrderBy(index => index).SequenceEqual(new[] { 1, 2, 3 })), "Each Boss must define phases 1, 2, and 3 exactly once.");
            context.Assert(phaseGroups.All(group => !string.IsNullOrWhiteSpace(group.Single(phase => phase.PhaseIndex == 3).DeathPatternRecipeId)), "Each Boss phase 3 must bind a death recipe.");

            var audioCues = catalog.CreateAudioCueDefinitions();
            context.AssertEqual(14, audioCues.Length, "catalog.audioCues.count");
            context.Assert(audioCues.Any(item => item.CueId == "bgm_main_menu" && item.Kind == TotemAudioCueKind.Bgm && item.Loop), "Catalog audio should preserve main-menu BGM cue.");
            context.Assert(audioCues.Any(item => item.CueId == "sfx_hit_melee" && item.Kind == TotemAudioCueKind.Sfx && item.AssetName == "SFX/hit_melee.wav"), "Catalog audio should preserve melee hit SFX cue.");
            context.Assert(audioCues.Any(item => item.CueId == "sfx_player_died" && item.Kind == TotemAudioCueKind.Sfx), "Catalog audio should preserve player death SFX cue.");
            var audioCueIds = audioCues.Select(item => item.CueId).ToHashSet();
            context.Assert(bossPhases.All(phase => audioCueIds.Contains(phase.PhaseBGMCueId)), "All Boss phase BGM cue ids should exist in audioCues.");

            var map = TotemMapService.BuildLayout(1, 1);
            var npcs = catalog.CreateNpcModels(map);
            context.Detail("catalog.firstRoundContent.npcDefinitions", npcs.Length);
            context.AssertEqual(5, npcs.Length, "catalog.npcs.count");
            context.AssertEqual(3, npcs.Count(npc => npc.Type == TotemNpcType.Tattooist), "catalog.npcs.tattooists");
            context.AssertEqual(2, npcs.Count(npc => npc.Type == TotemNpcType.Merchant), "catalog.npcs.merchants");
            context.Detail("catalog.firstRoundContent.shopStockRows", catalog.shopStocks.Length);
            context.AssertEqual(15, catalog.shopStocks.Length, "catalog.shopStocks.count");
            context.AssertEqual(10, catalog.CreateShopOffers("general_shop").Length, "catalog.shopStocks.generalCount");
            context.AssertEqual(5, catalog.CreateShopOffers("alien_shop").Length, "catalog.shopStocks.alienCount");
            var merchantSlots = catalog.CreateMerchantSlotDefinitions();
            context.AssertEqual(9, merchantSlots.Length, "catalog.merchantSlots.count");
            context.AssertEqual(3, merchantSlots.Count(item => item.SlotIndex == 0), "catalog.merchantSlots.slot0Count");
            context.Assert(merchantSlots.Any(item => item.SlotIndex == 2 && item.WeaponId == "energy_fist" && item.GoldCost == 130), "Catalog must preserve merchant slot 2 energy fist price.");
            var merchantSlotOffers = catalog.CreateMerchantSlotOffers("merchant_general");
            context.AssertEqual(3, merchantSlotOffers.Length, "catalog.merchantSlots.generatedOfferCount");
            context.Assert(merchantSlotOffers.Any(item => item.ItemId == 9002 && item.RewardId == "energy_fist" && item.Price == 130), "Merchant slot offers should preserve deterministic slot 2 energy fist offer.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).All(npc => npc.Offers.Length >= 3), "Catalog merchants should have shop offers.");
            var generalMerchant = npcs.FirstOrDefault(npc => npc.NpcId == "merchant_general");
            var alienMerchant = npcs.FirstOrDefault(npc => npc.NpcId == "merchant_alien");
            context.AssertEqual("general_shop", generalMerchant?.ShopStockTable ?? string.Empty, "catalog.npcs.general.shopStockTable");
            context.AssertEqual(13, generalMerchant?.Offers.Length ?? -1, "catalog.npcs.general.offerCount");
            context.AssertEqual(8, alienMerchant?.Offers.Length ?? -1, "catalog.npcs.alien.offerCount");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).All(offer => offer.RewardType != TotemShopRewardType.Unknown), "Catalog shop offers should expose explicit reward types.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).Any(offer => offer.RewardType == TotemShopRewardType.WeaponUpgrade && offer.RewardId == "bow_charge"), "Catalog should expose Bow Upgrade reward id.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).Any(offer => offer.RewardType == TotemShopRewardType.SkillCore && offer.RewardId == "skill_fireball_01"), "Catalog should expose Fireball Skill Core reward id.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).Any(offer => offer.ItemId == 401 && offer.RewardType == TotemShopRewardType.StatusCleanse), "Catalog should expose Antidote as StatusCleanse.");
            context.Assert(npcs.Where(npc => npc.Type == TotemNpcType.Merchant).SelectMany(npc => npc.Offers).Any(offer => offer.ItemId == 501 && offer.RewardType == TotemShopRewardType.Ink && offer.RewardAmount == 2), "Catalog should expose RareInk reward amount.");

            var events = catalog.CreateEvents();
            context.Detail("catalog.firstRoundContent.eventRows", events.Length);
            context.AssertEqual(6, events.Length, "catalog.events.count");
            context.AssertEqual(2, events.Count(item => item.EventType == TotemGameplayEventType.Choice), "catalog.events.choiceCount");
            context.Assert(events.Where(item => item.EventType == TotemGameplayEventType.Choice).All(item => Mathf.Abs(item.TimeoutSec - 20f) <= 0.001f), "Choice events should preserve 20s timeout.");
            context.Assert(events.Any(item => item.EventId == "event_combat_001" && item.BaseRewardCoin == 50 && item.RewardPoolId == "pool_combat_basic"), "Combat event should preserve base coin and reward pool.");
            context.Assert(events.Any(item => item.EventId == "event_curse_001" && item.CurseDebuffId == "debuff_ink_slow" && !item.IsRepeatAllowed), "Curse event should preserve debuff and repeat flag.");

            var choiceCatalog = catalog.CreateChoiceOptions();
            context.Detail("catalog.firstRoundContent.threeChoiceOptionRows", choiceCatalog.Length);
            context.AssertEqual(11, choiceCatalog.Length, "catalog.choices.optionCount");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_tattoo_recipe_fire_001" && item.OptionType == TotemChoiceOptionType.TattooRecipe && item.EffectType == TotemChoiceEffectType.RecipeUnlock && item.ContentRef == "recipe_fire_001"), "Choice catalog should preserve tattoo recipe option.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_pattern_recipe_straight_001" && item.IsUnique), "Choice catalog should preserve unique pattern recipe option.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_weapon_upgrade_damage_001" && item.EffectType == TotemChoiceEffectType.WeaponUpgrade), "Choice catalog should preserve weapon upgrade option.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_skill_upgrade_slot0_001" && item.SkillSlot == 0 && item.EffectType == TotemChoiceEffectType.SkillRefresh), "Choice catalog should preserve skill slot 0 upgrade option.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_coin_bonus_small" && item.ValueInt == 80 && item.WeightBase == 22), "Choice catalog should preserve coin bonus value and weight.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_heal_moderate" && item.EffectType == TotemChoiceEffectType.Heal && item.ValueInt == 30), "Choice catalog should preserve heal option.");
            context.Assert(choiceCatalog.Any(item => item.OptionId == "opt_one_time_scroll_001" && Mathf.Abs(item.MinRunElapsedSec - 120f) <= 0.001f), "Choice catalog should preserve one-time scroll timing.");
            var choices = TotemChoiceService.BuildThreeChoices("catalog_choice", 7, choiceCatalog);
            context.AssertEqual(3, choices.Options.Length, "catalog.choices.rollCount");
            context.Assert(choices.Options.All(item => item.MinRunElapsedSec <= 0f), "Initial choice roll should only include immediately available options.");
            var selectedChoiceEvent = TotemChoiceService.SelectEvent(TotemGameplayEventType.Choice, 7, events);
            context.Assert(selectedChoiceEvent != null && selectedChoiceEvent.EventType == TotemGameplayEventType.Choice, "Choice event selection should return a choice event.");

            context.Assert(catalog.aiTuning.lodRadius > 0f, "AI tuning lod radius must be positive.");
            AssertNear(context, 20f, catalog.aiTuning.lodRadius, "catalog.ai.lodRadius");
            AssertNear(context, 0.2f, catalog.aiTuning.lightHotInterval, "catalog.ai.lightHotInterval");

            var botProfiles = catalog.CreateBotProfiles();
            var botBuildPresets = catalog.CreateBotBuildPresets();
            int smartProfileCount = botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi);
            int lightProfileCount = botProfiles.Count(profile => profile.ActorKind == TotemActorKind.LightAi);
            var firstRoundRoster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection());
            int humanParticipants = firstRoundRoster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.Human);
            int smartBotParticipants = firstRoundRoster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightBotParticipants = firstRoundRoster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.LightBot);
            context.Detail("catalog.firstRoundContent.participantCount", firstRoundRoster.Length);
            context.Detail("catalog.firstRoundContent.humanParticipants", humanParticipants);
            context.Detail("catalog.firstRoundContent.smartBotParticipants", smartBotParticipants);
            context.Detail("catalog.firstRoundContent.lightBotParticipants", lightBotParticipants);
            context.Detail("catalog.firstRoundContent.smartAiProfileRows", smartProfileCount);
            context.Detail("catalog.firstRoundContent.lightAiProfileRowsReusedByRuntimeActors", lightProfileCount);
            context.AssertEqual(23, botProfiles.Length, "catalog.ai.profileCount");
            context.AssertEqual(20, smartProfileCount, "catalog.ai.smartProfileCount");
            context.AssertEqual(3, lightProfileCount, "catalog.ai.lightProfileCount.reusedBy29RuntimeActors");
            context.AssertEqual(50, firstRoundRoster.Length, "catalog.firstRoundContent.participantCount");
            context.AssertEqual(1, humanParticipants, "catalog.firstRoundContent.humanParticipants");
            context.AssertEqual(20, smartBotParticipants, "catalog.firstRoundContent.smartBotParticipants");
            context.AssertEqual(29, lightBotParticipants, "catalog.firstRoundContent.lightBotParticipants");
            context.Assert(firstRoundRoster.All(actor => TotemActorService.IsParticipantKind(actor.Kind)),
                "Catalog Actor roster must contain Participant kinds only.");
            context.AssertEqual(5, botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == TotemAIPersonality.Aggressive), "catalog.ai.personality.aggressive");
            context.AssertEqual(3, botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == TotemAIPersonality.Conservative), "catalog.ai.personality.conservative");
            context.AssertEqual(4, botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == TotemAIPersonality.ResourceAcquisition), "catalog.ai.personality.resource");
            context.AssertEqual(4, botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == TotemAIPersonality.BossPriority), "catalog.ai.personality.bossPriority");
            context.AssertEqual(4, botProfiles.Count(profile => profile.ActorKind == TotemActorKind.SmartAi && profile.Personality == TotemAIPersonality.PlayerPriority), "catalog.ai.personality.playerPriority");
            var bossPriorityProfile = botProfiles.First(profile => profile.Personality == TotemAIPersonality.BossPriority);
            var playerPriorityProfile = botProfiles.First(profile => profile.Personality == TotemAIPersonality.PlayerPriority);
            context.Assert(bossPriorityProfile.TargetBossWeight > bossPriorityProfile.TargetResourceWeight, "Boss-priority profile must keep Boss weight above resources.");
            AssertNear(context, playerPriorityProfile.TargetPlayerWeight, playerPriorityProfile.TargetHumanoidAiWeight, "catalog.ai.playerPriority.samePlayerAiWeight");
            context.AssertEqual(7, botBuildPresets.Length, "catalog.ai.buildPresetCount");
            context.Assert(botBuildPresets.Any(preset => preset.BehaviorMacro == TotemAIBehaviorMacro.Rush), "Catalog should include Rush bot macro.");
            context.Assert(botBuildPresets.Any(preset => preset.BehaviorMacro == TotemAIBehaviorMacro.Camp), "Catalog should include Camp bot macro.");
        }

        private static void CheckDataServiceLoadPath(GFDiagnosticScenarioContext context)
        {
            var dataService = new TotemDataService();
            dataService.ReloadGameplayCatalog();
            context.Detail("dataService.catalog.path", dataService.GameplayCatalogPath);
            context.Detail("dataService.catalog.hash", dataService.GameplayCatalogContentHash);
            context.Detail("dataService.catalog.usingFallback", dataService.GameplayCatalogUsingFallback);
            context.Detail("dataService.catalog.source", dataService.GameplayCatalog?.source ?? string.Empty);
            context.Assert(dataService.GameplayCatalogLoadedFromFile, $"Data service should load external catalog: {dataService.GameplayCatalogMessage}");
            context.Assert(!dataService.GameplayCatalogUsingFallback, "Data service must not use BuildDefault fallback when gameplay catalog exists.");
            context.Assert(!string.IsNullOrWhiteSpace(dataService.GameplayCatalogContentHash), "Data service should expose catalog content hash.");
            context.Assert(dataService.GameplayCatalog != null, "Data service should expose gameplay catalog.");
            context.AssertEqual("GameData/AIData/DataTables/Business", dataService.GameplayCatalog?.source ?? string.Empty, "dataService.catalog.source.businessDataTables");
            context.AssertEqual(5, dataService.GameplayCatalog.weapons.Length, "dataService.catalog.weaponCount");
            context.AssertEqual(2, dataService.GameplayCatalog.projectiles.Length, "dataService.catalog.projectileCount");
            context.AssertEqual(10, dataService.GameplayCatalog.weaponTraits.Length, "dataService.catalog.weaponTraitCount");
            context.AssertEqual(15, dataService.GameplayCatalog.weaponDrops.Length, "dataService.catalog.weaponDropCount");
            context.AssertEqual(6, dataService.GameplayCatalog.chestRewards.Length, "dataService.catalog.chestRewardCount");
            context.AssertEqual(3, dataService.GameplayCatalog.mapTemplates.Length, "dataService.catalog.mapTemplateCount");
            context.AssertEqual(31, dataService.GameplayCatalog.items.Length, "dataService.catalog.itemCount");
            context.AssertEqual(14, dataService.GameplayCatalog.resources.Length, "dataService.catalog.resourceCount");
            context.AssertEqual(6, dataService.GameplayCatalog.tattooReadingTimes.Length, "dataService.catalog.tattooReadingTimeCount");
            context.AssertEqual(7, dataService.GameplayCatalog.tattooElements.Length, "dataService.catalog.tattooElementCount");
            context.AssertEqual(8, dataService.GameplayCatalog.tattooShapes.Length, "dataService.catalog.tattooShapeCount");
            context.AssertEqual(24, dataService.GameplayCatalog.tattooEnchantAffixes.Length, "dataService.catalog.tattooEnchantAffixCount");
            context.AssertEqual(3, dataService.GameplayCatalog.tattooEnchantRecipes.Length, "dataService.catalog.tattooEnchantRecipeCount");
            context.AssertEqual(15, dataService.GameplayCatalog.enemies.Length, "dataService.catalog.enemyCount");
            context.AssertEqual(25, dataService.GameplayCatalog.enemyAbilities.Length, "dataService.catalog.enemyAbilityCount");
            context.AssertEqual(9, dataService.GameplayCatalog.encounterSpawns.Length, "dataService.catalog.encounterSpawnCount");
            context.AssertEqual(37, dataService.GameplayCatalog.enemyLoot.Length, "dataService.catalog.enemyLootCount");
            context.AssertEqual(9, dataService.GameplayCatalog.bossPhases.Length, "dataService.catalog.bossPhaseCount");
            context.AssertEqual(14, dataService.GameplayCatalog.audioCues.Length, "dataService.catalog.audioCueCount");
            context.AssertEqual(15, dataService.GameplayCatalog.shopStocks.Length, "dataService.catalog.shopStockCount");
            context.AssertEqual(9, dataService.GameplayCatalog.merchantSlots.Length, "dataService.catalog.merchantSlotCount");
            context.AssertEqual(6, dataService.GameplayCatalog.events.Length, "dataService.catalog.eventCount");
            context.AssertEqual(11, dataService.GameplayCatalog.choiceOptions.Length, "dataService.catalog.choiceOptionCount");
            context.AssertEqual(14, dataService.GameplayCatalog.skills.Length, "dataService.catalog.skillCount");
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }

        private static void CheckGenerationFingerprint(GFDiagnosticScenarioContext context, TotemGameplayCatalog catalog)
        {
            context.Assert(catalog.generation != null, "Gameplay catalog must include deterministic generation metadata.");
            var generation = catalog.generation;
            context.Detail("catalog.generation.generatedBy", generation.generatedBy ?? string.Empty);
            context.Detail("catalog.generation.sourceRoot", generation.sourceRoot ?? string.Empty);
            context.Detail("catalog.generation.sourceFileCount", generation.sourceFileCount);
            context.Detail("catalog.generation.sourceContentHash", generation.sourceContentHash ?? string.Empty);
            context.AssertEqual("tools/ai_index/build_gameplay_catalog_from_business_tables.py", generation.generatedBy ?? string.Empty, "catalog.generation.generatedBy");
            context.AssertEqual("GameData/AIData/DataTables/Business", generation.sourceRoot ?? string.Empty, "catalog.generation.sourceRoot");

            string sourceDirectory = Path.Combine(GetProjectRoot(), generation.sourceRoot ?? string.Empty);
            context.Assert(Directory.Exists(sourceDirectory), $"Gameplay catalog source directory must exist: {sourceDirectory}");
            string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.json", SearchOption.TopDirectoryOnly);
            context.Detail("catalog.generation.actualSourceFileCount", sourceFiles.Length);
            context.AssertEqual(31, sourceFiles.Length, "catalog.generation.actualSourceFileCount");
            context.AssertEqual(sourceFiles.Length, generation.sourceFileCount, "catalog.generation.sourceFileCount.matchesActual");

            string actualHash = ComputeSourceFingerprint(sourceFiles);
            context.Detail("catalog.generation.actualSourceContentHash", actualHash);
            context.AssertEqual(actualHash, generation.sourceContentHash ?? string.Empty, "catalog.generation.sourceContentHash");
        }

        private static string GetProjectRoot()
        {
            var assetsDirectory = Directory.GetParent(Application.dataPath);
            return assetsDirectory == null ? Directory.GetCurrentDirectory() : assetsDirectory.FullName;
        }

        private static IEnumerable<string> SplitIds(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        private static string ComputeSourceFingerprint(string[] sourceFiles)
        {
            using (var sha = SHA256.Create())
            {
                var separator = new byte[] { 0 };
                foreach (string fileName in sourceFiles.OrderBy(Path.GetFileName, StringComparer.Ordinal))
                {
                    byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(fileName));
                    sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
                    sha.TransformBlock(separator, 0, separator.Length, null, 0);

                    byte[] contentBytes = File.ReadAllBytes(fileName);
                    sha.TransformBlock(contentBytes, 0, contentBytes.Length, null, 0);
                    sha.TransformBlock(separator, 0, separator.Length, null, 0);
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var builder = new StringBuilder(sha.Hash.Length * 2);
                for (int i = 0; i < sha.Hash.Length; i++)
                {
                    builder.Append(sha.Hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
#endif
