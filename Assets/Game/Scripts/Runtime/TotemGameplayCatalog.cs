using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TotemGameplayCatalog
{
    public int schemaVersion = 1;
    public string source = string.Empty;
    public TotemGameplayCatalogGenerationInfo generation = new TotemGameplayCatalogGenerationInfo();
    public TotemItemCatalogEntry[] items = Array.Empty<TotemItemCatalogEntry>();
    public TotemResourceCatalogEntry[] resources = Array.Empty<TotemResourceCatalogEntry>();
    public TotemWeaponCatalogEntry[] weapons = Array.Empty<TotemWeaponCatalogEntry>();
    public TotemProjectileCatalogEntry[] projectiles = Array.Empty<TotemProjectileCatalogEntry>();
    public TotemWeaponTraitCatalogEntry[] weaponTraits = Array.Empty<TotemWeaponTraitCatalogEntry>();
    public TotemWeaponDropCatalogEntry[] weaponDrops = Array.Empty<TotemWeaponDropCatalogEntry>();
    public TotemChestRewardCatalogEntry[] chestRewards = Array.Empty<TotemChestRewardCatalogEntry>();
    public TotemMapTemplateCatalogEntry[] mapTemplates = Array.Empty<TotemMapTemplateCatalogEntry>();
    public TotemTattooPartCatalogEntry[] tattooParts = Array.Empty<TotemTattooPartCatalogEntry>();
    public TotemTattooColorCatalogEntry[] tattooColors = Array.Empty<TotemTattooColorCatalogEntry>();
    public TotemTattooElementCatalogEntry[] tattooElements = Array.Empty<TotemTattooElementCatalogEntry>();
    public TotemTattooPatternCatalogEntry[] tattooPatterns = Array.Empty<TotemTattooPatternCatalogEntry>();
    public TotemTattooShapeCatalogEntry[] tattooShapes = Array.Empty<TotemTattooShapeCatalogEntry>();
    public TotemTattooReadingTimeCatalogEntry[] tattooReadingTimes = Array.Empty<TotemTattooReadingTimeCatalogEntry>();
    public TotemTattooEnchantAffixCatalogEntry[] tattooEnchantAffixes = Array.Empty<TotemTattooEnchantAffixCatalogEntry>();
    public TotemTattooEnchantRecipeCatalogEntry[] tattooEnchantRecipes = Array.Empty<TotemTattooEnchantRecipeCatalogEntry>();
    public TotemSkillCatalogEntry[] skills = Array.Empty<TotemSkillCatalogEntry>();
    public TotemEnemyCatalogEntry[] enemies = Array.Empty<TotemEnemyCatalogEntry>();
    public TotemEnemyAbilityCatalogEntry[] enemyAbilities = Array.Empty<TotemEnemyAbilityCatalogEntry>();
    public TotemEncounterSpawnCatalogEntry[] encounterSpawns = Array.Empty<TotemEncounterSpawnCatalogEntry>();
    public TotemEnemyLootCatalogEntry[] enemyLoot = Array.Empty<TotemEnemyLootCatalogEntry>();
    public TotemZonePhaseCatalogEntry[] zonePhases = Array.Empty<TotemZonePhaseCatalogEntry>();
    public TotemBossPhaseCatalogEntry[] bossPhases = Array.Empty<TotemBossPhaseCatalogEntry>();
    public TotemAudioCueCatalogEntry[] audioCues = Array.Empty<TotemAudioCueCatalogEntry>();
    public TotemNpcCatalogEntry[] npcs = Array.Empty<TotemNpcCatalogEntry>();
    public TotemShopStockCatalogEntry[] shopStocks = Array.Empty<TotemShopStockCatalogEntry>();
    public TotemMerchantSlotCatalogEntry[] merchantSlots = Array.Empty<TotemMerchantSlotCatalogEntry>();
    public TotemGameplayEventCatalogEntry[] events = Array.Empty<TotemGameplayEventCatalogEntry>();
    public TotemChoiceCatalogEntry[] choiceOptions = Array.Empty<TotemChoiceCatalogEntry>();
    public TotemBotProfileCatalogEntry[] botProfiles = Array.Empty<TotemBotProfileCatalogEntry>();
    public TotemBotBuildPresetCatalogEntry[] botBuildPresets = Array.Empty<TotemBotBuildPresetCatalogEntry>();
    public TotemAITuningDefinition aiTuning = TotemAITuningDefinition.Default;

    public void Normalize()
    {
        if (generation == null) generation = new TotemGameplayCatalogGenerationInfo();
        if (weapons == null) weapons = Array.Empty<TotemWeaponCatalogEntry>();
        if (items == null) items = Array.Empty<TotemItemCatalogEntry>();
        if (resources == null) resources = Array.Empty<TotemResourceCatalogEntry>();
        if (projectiles == null) projectiles = Array.Empty<TotemProjectileCatalogEntry>();
        if (weaponTraits == null) weaponTraits = Array.Empty<TotemWeaponTraitCatalogEntry>();
        if (weaponDrops == null) weaponDrops = Array.Empty<TotemWeaponDropCatalogEntry>();
        if (chestRewards == null) chestRewards = Array.Empty<TotemChestRewardCatalogEntry>();
        if (mapTemplates == null) mapTemplates = Array.Empty<TotemMapTemplateCatalogEntry>();
        if (tattooParts == null) tattooParts = Array.Empty<TotemTattooPartCatalogEntry>();
        if (tattooColors == null) tattooColors = Array.Empty<TotemTattooColorCatalogEntry>();
        if (tattooElements == null) tattooElements = Array.Empty<TotemTattooElementCatalogEntry>();
        if (tattooPatterns == null) tattooPatterns = Array.Empty<TotemTattooPatternCatalogEntry>();
        if (tattooShapes == null) tattooShapes = Array.Empty<TotemTattooShapeCatalogEntry>();
        if (tattooReadingTimes == null) tattooReadingTimes = Array.Empty<TotemTattooReadingTimeCatalogEntry>();
        if (tattooEnchantAffixes == null) tattooEnchantAffixes = Array.Empty<TotemTattooEnchantAffixCatalogEntry>();
        if (tattooEnchantRecipes == null) tattooEnchantRecipes = Array.Empty<TotemTattooEnchantRecipeCatalogEntry>();
        if (skills == null) skills = Array.Empty<TotemSkillCatalogEntry>();
        if (enemies == null) enemies = Array.Empty<TotemEnemyCatalogEntry>();
        if (enemyAbilities == null) enemyAbilities = Array.Empty<TotemEnemyAbilityCatalogEntry>();
        if (encounterSpawns == null) encounterSpawns = Array.Empty<TotemEncounterSpawnCatalogEntry>();
        if (enemyLoot == null) enemyLoot = Array.Empty<TotemEnemyLootCatalogEntry>();
        if (zonePhases == null) zonePhases = Array.Empty<TotemZonePhaseCatalogEntry>();
        if (bossPhases == null) bossPhases = Array.Empty<TotemBossPhaseCatalogEntry>();
        if (audioCues == null) audioCues = Array.Empty<TotemAudioCueCatalogEntry>();
        if (npcs == null) npcs = Array.Empty<TotemNpcCatalogEntry>();
        if (shopStocks == null) shopStocks = Array.Empty<TotemShopStockCatalogEntry>();
        if (merchantSlots == null) merchantSlots = Array.Empty<TotemMerchantSlotCatalogEntry>();
        if (events == null) events = Array.Empty<TotemGameplayEventCatalogEntry>();
        if (choiceOptions == null) choiceOptions = Array.Empty<TotemChoiceCatalogEntry>();
        if (botProfiles == null) botProfiles = Array.Empty<TotemBotProfileCatalogEntry>();
        if (botBuildPresets == null) botBuildPresets = Array.Empty<TotemBotBuildPresetCatalogEntry>();
        if (aiTuning == null) aiTuning = TotemAITuningDefinition.Default;
    }

    public TotemItemDefinition[] CreateItemDefinitions()
    {
        var result = new TotemItemDefinition[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            result[i] = items[i].ToDefinition();
        }

        return result;
    }

    public TotemResourceDefinition[] CreateResourceDefinitions()
    {
        var result = new TotemResourceDefinition[resources.Length];
        for (int i = 0; i < resources.Length; i++)
        {
            result[i] = resources[i].ToDefinition();
        }

        return result;
    }

    public TotemMerchantSlotDefinition[] CreateMerchantSlotDefinitions()
    {
        var result = new TotemMerchantSlotDefinition[merchantSlots.Length];
        for (int i = 0; i < merchantSlots.Length; i++)
        {
            result[i] = merchantSlots[i].ToDefinition();
        }

        return result;
    }

    public TotemWeaponDefinition[] CreateWeaponDefinitions()
    {
        var result = new TotemWeaponDefinition[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            result[i] = weapons[i].ToDefinition();
        }

        return result;
    }

    public TotemWeaponDropDefinition[] CreateWeaponDropDefinitions()
    {
        var result = new TotemWeaponDropDefinition[weaponDrops.Length];
        for (int i = 0; i < weaponDrops.Length; i++)
        {
            result[i] = weaponDrops[i].ToDefinition();
        }

        return result;
    }

    public TotemProjectileDefinition[] CreateProjectileDefinitions()
    {
        var result = new TotemProjectileDefinition[projectiles.Length];
        for (int i = 0; i < projectiles.Length; i++)
        {
            result[i] = projectiles[i].ToDefinition();
        }

        return result;
    }

    public TotemWeaponTraitDefinition[] CreateWeaponTraitDefinitions()
    {
        var result = new TotemWeaponTraitDefinition[weaponTraits.Length];
        for (int i = 0; i < weaponTraits.Length; i++)
        {
            result[i] = weaponTraits[i].ToDefinition();
        }

        return result;
    }

    public TotemChestRewardDefinition[] CreateChestRewardDefinitions()
    {
        var result = new TotemChestRewardDefinition[chestRewards.Length];
        for (int i = 0; i < chestRewards.Length; i++)
        {
            result[i] = chestRewards[i].ToDefinition();
        }

        return result;
    }

    public TotemMapTemplateDefinition[] CreateMapTemplates()
    {
        var result = new TotemMapTemplateDefinition[mapTemplates.Length];
        for (int i = 0; i < mapTemplates.Length; i++)
        {
            result[i] = mapTemplates[i].ToDefinition();
        }

        return result;
    }

    public TotemTattooDefinition[] CreateTattooDefinitions()
    {
        var result = new TotemTattooDefinition[tattooParts.Length * tattooColors.Length * tattooPatterns.Length];
        int cursor = 0;
        for (int part = 0; part < tattooParts.Length; part++)
        {
            for (int color = 0; color < tattooColors.Length; color++)
            {
                for (int pattern = 0; pattern < tattooPatterns.Length; pattern++)
                {
                    var partEntry = tattooParts[part];
                    var colorEntry = tattooColors[color];
                    var patternEntry = tattooPatterns[pattern];
                    var elementEntry = FindTattooElement(colorEntry.element);
                    var shapeEntry = FindTattooShape(patternEntry.shape);
                    float colorMultiplier = colorEntry.multiplier <= 0f ? 1f : colorEntry.multiplier;
                    float patternMultiplier = patternEntry.multiplier <= 0f ? 1f : patternEntry.multiplier;
                    float elementMultiplier = elementEntry == null || elementEntry.baseMultiplier <= 0f ? 1f : elementEntry.baseMultiplier;
                    result[cursor++] = new TotemTattooDefinition
                    {
                        PartId = partEntry.id,
                        PartName = partEntry.name,
                        TriggerEvent = partEntry.triggerEvent,
                        ScaleStat = partEntry.scaleStat ?? string.Empty,
                        SymmetryGroup = partEntry.symmetryGroup ?? string.Empty,
                        ScaleFactor = partEntry.scaleFactor,
                        PassiveDimension = partEntry.passiveDimension ?? string.Empty,
                        ColorId = colorEntry.id,
                        ColorName = colorEntry.name,
                        Element = TotemCatalogEnum.Parse(colorEntry.element, TotemTattooElement.Fire),
                        ColorMultiplier = colorMultiplier,
                        ElementBaseMultiplier = elementMultiplier,
                        ElementParam1 = elementEntry?.param1 ?? 0f,
                        ElementParam2 = elementEntry?.param2 ?? 0f,
                        ElementParam3 = elementEntry?.param3 ?? 0f,
                        PatternId = patternEntry.id,
                        PatternName = patternEntry.name,
                        Shape = TotemCatalogEnum.Parse(patternEntry.shape, TotemTattooShape.SingleHit),
                        PatternMultiplier = patternMultiplier,
                        ShapeParam1 = shapeEntry?.param1 ?? 0f,
                        ShapeParam2 = shapeEntry?.param2 ?? 0f,
                        ShapeParam3 = shapeEntry?.param3 ?? 0f,
                        Magnitude = colorMultiplier * patternMultiplier * elementMultiplier,
                    };
                }
            }
        }

        return result;
    }

    public TotemTattooElementDefinition[] CreateTattooElementDefinitions()
    {
        var result = new TotemTattooElementDefinition[tattooElements.Length];
        for (int i = 0; i < tattooElements.Length; i++)
        {
            result[i] = tattooElements[i].ToDefinition();
        }

        return result;
    }

    public TotemTattooShapeDefinition[] CreateTattooShapeDefinitions()
    {
        var result = new TotemTattooShapeDefinition[tattooShapes.Length];
        for (int i = 0; i < tattooShapes.Length; i++)
        {
            result[i] = tattooShapes[i].ToDefinition();
        }

        return result;
    }

    private TotemTattooElementCatalogEntry FindTattooElement(string elementName)
    {
        for (int i = 0; i < tattooElements.Length; i++)
        {
            var element = tattooElements[i];
            if (element != null && string.Equals(element.name, elementName, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
        }

        return null;
    }

    private TotemTattooShapeCatalogEntry FindTattooShape(string shapeName)
    {
        for (int i = 0; i < tattooShapes.Length; i++)
        {
            var shape = tattooShapes[i];
            if (shape != null && string.Equals(shape.name, shapeName, StringComparison.OrdinalIgnoreCase))
            {
                return shape;
            }
        }

        return null;
    }

    public TotemTattooReadingTimeDefinition[] CreateTattooReadingTimeDefinitions()
    {
        var result = new TotemTattooReadingTimeDefinition[tattooReadingTimes.Length];
        for (int i = 0; i < tattooReadingTimes.Length; i++)
        {
            result[i] = tattooReadingTimes[i].ToDefinition();
        }

        return result;
    }

    public TotemTattooEnchantAffixDefinition[] CreateTattooEnchantAffixDefinitions()
    {
        var result = new TotemTattooEnchantAffixDefinition[tattooEnchantAffixes.Length];
        for (int i = 0; i < tattooEnchantAffixes.Length; i++)
        {
            result[i] = tattooEnchantAffixes[i].ToDefinition();
        }

        return result;
    }

    public TotemTattooEnchantRecipeDefinition[] CreateTattooEnchantRecipeDefinitions()
    {
        var result = new TotemTattooEnchantRecipeDefinition[tattooEnchantRecipes.Length];
        for (int i = 0; i < tattooEnchantRecipes.Length; i++)
        {
            result[i] = tattooEnchantRecipes[i].ToDefinition();
        }

        return result;
    }

    public TotemSkillDefinition[] CreateSkillDefinitions()
    {
        var result = new TotemSkillDefinition[skills.Length];
        for (int i = 0; i < skills.Length; i++)
        {
            result[i] = skills[i].ToDefinition();
        }

        return result;
    }

    public TotemZonePhase[] CreateZonePhases()
    {
        var result = new TotemZonePhase[zonePhases.Length];
        for (int i = 0; i < zonePhases.Length; i++)
        {
            result[i] = zonePhases[i].ToPhase();
        }

        return result;
    }

    public TotemEnemyDefinition[] CreateEnemyDefinitions()
    {
        var result = new TotemEnemyDefinition[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            result[i] = enemies[i].ToDefinition();
        }

        return result;
    }

    public TotemEnemyAbilityDefinition[] CreateEnemyAbilityDefinitions()
    {
        var result = new TotemEnemyAbilityDefinition[enemyAbilities.Length];
        for (int i = 0; i < enemyAbilities.Length; i++)
        {
            result[i] = enemyAbilities[i].ToDefinition();
        }

        return result;
    }

    public TotemEncounterSpawnDefinition[] CreateEncounterSpawnDefinitions()
    {
        var result = new TotemEncounterSpawnDefinition[encounterSpawns.Length];
        for (int i = 0; i < encounterSpawns.Length; i++)
        {
            result[i] = encounterSpawns[i].ToDefinition();
        }

        return result;
    }

    public TotemEnemyLootDefinition[] CreateEnemyLootDefinitions()
    {
        var result = new TotemEnemyLootDefinition[enemyLoot.Length];
        for (int i = 0; i < enemyLoot.Length; i++)
        {
            result[i] = enemyLoot[i].ToDefinition();
        }

        return result;
    }

    public TotemBossPhase[] CreateBossPhases()
    {
        var result = new TotemBossPhase[bossPhases.Length];
        for (int i = 0; i < bossPhases.Length; i++)
        {
            result[i] = bossPhases[i].ToPhase();
        }

        return result;
    }

    public TotemAudioCueDefinition[] CreateAudioCueDefinitions()
    {
        var result = new TotemAudioCueDefinition[audioCues.Length];
        for (int i = 0; i < audioCues.Length; i++)
        {
            result[i] = audioCues[i].ToDefinition();
        }

        return result;
    }

    public TotemNpcModel[] CreateNpcModels(TotemMapSnapshot map)
    {
        var result = new TotemNpcModel[npcs.Length];
        for (int i = 0; i < npcs.Length; i++)
        {
            result[i] = npcs[i].ToModel(map, shopStocks, merchantSlots);
        }

        return result;
    }

    public TotemShopOffer[] CreateShopOffers(string tableId)
    {
        return TotemShopStockCatalogEntry.CreateOffers(shopStocks, tableId);
    }

    public TotemShopOffer[] CreateMerchantSlotOffers(string merchantId)
    {
        return TotemMerchantSlotCatalogEntry.CreateOffers(merchantSlots, merchantId);
    }

    public TotemChoiceOption[] CreateChoiceOptions()
    {
        var result = new TotemChoiceOption[choiceOptions.Length];
        for (int i = 0; i < choiceOptions.Length; i++)
        {
            result[i] = choiceOptions[i].ToOption();
        }

        return result;
    }

    public TotemGameplayEventDefinition[] CreateEvents()
    {
        var result = new TotemGameplayEventDefinition[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            result[i] = events[i].ToDefinition();
        }

        return result;
    }

    public TotemBotProfileDefinition[] CreateBotProfiles()
    {
        var result = new TotemBotProfileDefinition[botProfiles.Length];
        for (int i = 0; i < botProfiles.Length; i++)
        {
            result[i] = botProfiles[i].ToDefinition();
        }

        return result;
    }

    public TotemBotBuildPresetDefinition[] CreateBotBuildPresets()
    {
        var result = new TotemBotBuildPresetDefinition[botBuildPresets.Length];
        for (int i = 0; i < botBuildPresets.Length; i++)
        {
            result[i] = botBuildPresets[i].ToDefinition();
        }

        return result;
    }

    public static TotemGameplayCatalog BuildDefault()
    {
        return new TotemGameplayCatalog
        {
            schemaVersion = 1,
            source = "runtime-default",
            generation = new TotemGameplayCatalogGenerationInfo
            {
                generatedBy = "runtime-default",
                sourceRoot = "runtime-default",
                sourceFileCount = 0,
                sourceContentHash = "BuildDefault",
            },
            items = BuildDefaultItems(),
            resources = BuildDefaultResources(),
            weapons = new[]
            {
                new TotemWeaponCatalogEntry { weaponId = "knife_basic", displayName = "Knife", className = "Melee", baseDamage = 18f, cooldown = 36f / 60f, range = 1.2f, attackSpeed = 0.05f, chargedMul = 1.4f, projectileId = string.Empty, rarity = 0, maxAmmo = -1, baseStartup = 6, baseActive = 4, baseRecovery = 26, requiresCharge = false, aimSpreadHalfDeg = 120f, normalTraitId = "trait_quickslash", chargedTraitId = "trait_pierce", weaponPrefabPath = "Prefab/Weapon/Knife" },
                new TotemWeaponCatalogEntry { weaponId = "hammer_heavy", displayName = "Hammer", className = "Melee", baseDamage = 48f, cooldown = 60f / 60f, range = 1.8f, attackSpeed = -0.15f, chargedMul = 2.0f, projectileId = string.Empty, rarity = 1, maxAmmo = -1, baseStartup = 14, baseActive = 6, baseRecovery = 40, requiresCharge = false, aimSpreadHalfDeg = 90f, normalTraitId = "trait_stun", chargedTraitId = "trait_explosive", weaponPrefabPath = "Prefab/Weapon/Hammer" },
                new TotemWeaponCatalogEntry { weaponId = "pistol_basic", displayName = "Pistol", className = "Ranged", baseDamage = 16f, cooldown = 30f / 60f, range = 20f, attackSpeed = 0.0f, chargedMul = 1.4f, projectileId = "bullet_pistol", rarity = 1, maxAmmo = 18, baseStartup = 6, baseActive = 2, baseRecovery = 22, requiresCharge = false, aimSpreadHalfDeg = 12f, normalTraitId = "trait_pierce", chargedTraitId = "trait_multishot", weaponPrefabPath = "Prefab/Weapon/Pistol" },
                new TotemWeaponCatalogEntry { weaponId = "bow_charge", displayName = "Bow", className = "Ranged", baseDamage = 30f, cooldown = 60f / 60f, range = 30f, attackSpeed = -0.05f, chargedMul = 2.4f, projectileId = "arrow_bow", rarity = 2, maxAmmo = 24, baseStartup = 16, baseActive = 4, baseRecovery = 40, requiresCharge = true, aimSpreadHalfDeg = 4f, normalTraitId = "trait_pierce", chargedTraitId = "trait_chain", weaponPrefabPath = "Prefab/Weapon/Bow" },
                new TotemWeaponCatalogEntry { weaponId = "energy_fist", displayName = "Energy Fist", className = "Special", baseDamage = 24f, cooldown = 40f / 60f, range = 2.5f, attackSpeed = 0.1f, chargedMul = 1.7f, projectileId = string.Empty, rarity = 2, maxAmmo = -1, baseStartup = 8, baseActive = 5, baseRecovery = 27, requiresCharge = false, aimSpreadHalfDeg = 35f, normalTraitId = "trait_quickslash", chargedTraitId = "trait_pull", weaponPrefabPath = "Prefab/Weapon/Fist" },
            },
            projectiles = BuildDefaultProjectiles(),
            weaponTraits = BuildDefaultWeaponTraits(),
            weaponDrops = BuildDefaultWeaponDrops(),
            chestRewards = BuildDefaultChestRewards(),
            mapTemplates = BuildDefaultMapTemplates(),
            tattooParts = new[]
            {
                new TotemTattooPartCatalogEntry { id = 1, name = "Head", triggerEvent = "CritHitEvent", scaleStat = "CritMultiplier", symmetryGroup = "None", scaleFactor = 10.0f, passiveDimension = "Critical" },
                new TotemTattooPartCatalogEntry { id = 2, name = "Torso", triggerEvent = "DamagedEvent", scaleStat = "MaxHealth", symmetryGroup = "None", scaleFactor = 0.12f, passiveDimension = "Defense" },
                new TotemTattooPartCatalogEntry { id = 3, name = "LeftArm", triggerEvent = "SkillCastEvent", scaleStat = "SkillPower", symmetryGroup = "Arms", scaleFactor = 0.6f, passiveDimension = "Skill" },
                new TotemTattooPartCatalogEntry { id = 4, name = "RightArm", triggerEvent = "AttackHitEvent", scaleStat = "WeaponDamage", symmetryGroup = "Arms", scaleFactor = 0.8f, passiveDimension = "Weapon" },
                new TotemTattooPartCatalogEntry { id = 5, name = "LeftLeg", triggerEvent = "DodgePressedEvent", scaleStat = "DodgeFrames", symmetryGroup = "Legs", scaleFactor = 40.0f, passiveDimension = "Dodge" },
                new TotemTattooPartCatalogEntry { id = 6, name = "RightLeg", triggerEvent = "MoveTickEvent", scaleStat = "MoveSpeed", symmetryGroup = "Legs", scaleFactor = 1.6f, passiveDimension = "Move" },
            },
            tattooColors = new[]
            {
                new TotemTattooColorCatalogEntry { id = 1, name = "Red", element = "Fire", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 2, name = "Yellow", element = "Lightning", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 3, name = "Green", element = "Nature", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 4, name = "Blue", element = "Frost", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 5, name = "Purple", element = "Mutation", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 6, name = "Gold", element = "Holy", multiplier = 1.00f },
                new TotemTattooColorCatalogEntry { id = 7, name = "White", element = "Pure", multiplier = 1.00f },
            },
            tattooElements = BuildDefaultTattooElements(),
            tattooPatterns = new[]
            {
                new TotemTattooPatternCatalogEntry { id = 1, name = "Line", shape = "SingleHit", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 2, name = "Ring", shape = "AOEBurst", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 3, name = "Spiral", shape = "StackingMark", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 4, name = "Zigzag", shape = "MultiHit", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 5, name = "Bolt", shape = "ChainJump", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 6, name = "Star", shape = "ProbBurst", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 7, name = "Stream", shape = "TrailZone", multiplier = 1.00f },
                new TotemTattooPatternCatalogEntry { id = 8, name = "Beast", shape = "SummonForm", multiplier = 1.00f },
            },
            tattooShapes = BuildDefaultTattooShapes(),
            tattooReadingTimes = BuildDefaultTattooReadingTimes(),
            tattooEnchantAffixes = BuildDefaultTattooEnchantAffixes(),
            tattooEnchantRecipes = BuildDefaultTattooEnchantRecipes(),
            skills = BuildDefaultSkills(),
            enemies = BuildDefaultEnemies(),
            enemyAbilities = BuildDefaultEnemyAbilities(),
            encounterSpawns = BuildDefaultEncounterSpawns(),
            enemyLoot = BuildDefaultEnemyLoot(),
            zonePhases = new[]
            {
                new TotemZonePhaseCatalogEntry { id = 0, phaseName = "Phase0_Slow", startTime = 0f, duration = 180f, targetRadius = 65f, outZoneDamage = 2f, centerOffsetMode = "None" },
                new TotemZonePhaseCatalogEntry { id = 1, phaseName = "Phase1_Offset", startTime = 180f, duration = 360f, targetRadius = 35f, outZoneDamage = 5f, centerOffsetMode = "Drift" },
                new TotemZonePhaseCatalogEntry { id = 2, phaseName = "Phase2_Rush", startTime = 540f, duration = 360f, targetRadius = 5f, outZoneDamage = 12f, centerOffsetMode = "Fixed" },
            },
            bossPhases = BuildDefaultBossPhases(),
            audioCues = BuildDefaultAudioCues(),
            npcs = BuildDefaultNpcs(),
            shopStocks = BuildDefaultShopStocks(),
            merchantSlots = BuildDefaultMerchantSlots(),
            events = BuildDefaultEvents(),
            choiceOptions = BuildDefaultChoiceOptions(),
            botProfiles = BuildDefaultBotProfiles(),
            botBuildPresets = BuildDefaultBotBuildPresets(),
            aiTuning = TotemAITuningDefinition.Default,
        };
    }

    private static TotemItemCatalogEntry[] BuildDefaultItems()
    {
        return new[]
        {
            new TotemItemCatalogEntry { itemId = 1, itemType = "Coin", subType = string.Empty, tier = 0, displayName = "item.coin", rarity = "Common", maxStack = 9999, basePrice = 1, sellRatio = 0f },
            new TotemItemCatalogEntry { itemId = 2101, itemType = "InkBottle", subType = "1", tier = 1, displayName = "item.ink.red.basic", rarity = "Common", maxStack = 99, basePrice = 40, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2102, itemType = "InkBottle", subType = "1", tier = 2, displayName = "item.ink.red.standard", rarity = "Uncommon", maxStack = 99, basePrice = 60, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2103, itemType = "InkBottle", subType = "1", tier = 3, displayName = "item.ink.red.premium", rarity = "Rare", maxStack = 99, basePrice = 100, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2201, itemType = "InkBottle", subType = "2", tier = 1, displayName = "item.ink.yellow.basic", rarity = "Common", maxStack = 99, basePrice = 40, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2202, itemType = "InkBottle", subType = "2", tier = 2, displayName = "item.ink.yellow.standard", rarity = "Uncommon", maxStack = 99, basePrice = 60, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2203, itemType = "InkBottle", subType = "2", tier = 3, displayName = "item.ink.yellow.premium", rarity = "Rare", maxStack = 99, basePrice = 100, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2301, itemType = "InkBottle", subType = "3", tier = 1, displayName = "item.ink.green.basic", rarity = "Common", maxStack = 99, basePrice = 40, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2302, itemType = "InkBottle", subType = "3", tier = 2, displayName = "item.ink.green.standard", rarity = "Uncommon", maxStack = 99, basePrice = 60, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2303, itemType = "InkBottle", subType = "3", tier = 3, displayName = "item.ink.green.premium", rarity = "Rare", maxStack = 99, basePrice = 100, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2401, itemType = "InkBottle", subType = "4", tier = 1, displayName = "item.ink.blue.basic", rarity = "Common", maxStack = 99, basePrice = 50, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2402, itemType = "InkBottle", subType = "4", tier = 2, displayName = "item.ink.blue.standard", rarity = "Uncommon", maxStack = 99, basePrice = 70, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2403, itemType = "InkBottle", subType = "4", tier = 3, displayName = "item.ink.blue.premium", rarity = "Rare", maxStack = 99, basePrice = 120, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2501, itemType = "InkBottle", subType = "5", tier = 1, displayName = "item.ink.purple.basic", rarity = "Uncommon", maxStack = 99, basePrice = 60, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2502, itemType = "InkBottle", subType = "5", tier = 2, displayName = "item.ink.purple.standard", rarity = "Rare", maxStack = 99, basePrice = 90, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2503, itemType = "InkBottle", subType = "5", tier = 3, displayName = "item.ink.purple.premium", rarity = "Epic", maxStack = 99, basePrice = 150, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2601, itemType = "InkBottle", subType = "6", tier = 1, displayName = "item.ink.gold.basic", rarity = "Uncommon", maxStack = 99, basePrice = 70, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2602, itemType = "InkBottle", subType = "6", tier = 2, displayName = "item.ink.gold.standard", rarity = "Rare", maxStack = 99, basePrice = 110, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2603, itemType = "InkBottle", subType = "6", tier = 3, displayName = "item.ink.gold.premium", rarity = "Epic", maxStack = 99, basePrice = 180, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2701, itemType = "InkBottle", subType = "7", tier = 1, displayName = "item.ink.white.basic", rarity = "Rare", maxStack = 99, basePrice = 100, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2702, itemType = "InkBottle", subType = "7", tier = 2, displayName = "item.ink.white.standard", rarity = "Epic", maxStack = 99, basePrice = 150, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 2703, itemType = "InkBottle", subType = "7", tier = 3, displayName = "item.ink.white.premium", rarity = "Legendary", maxStack = 99, basePrice = 220, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 3001, itemType = "RecipeShard", subType = string.Empty, tier = 0, displayName = "item.recipe.shard", rarity = "Uncommon", maxStack = 99, basePrice = 60, sellRatio = 0.3f },
            new TotemItemCatalogEntry { itemId = 3100, itemType = "RecipeFull", subType = string.Empty, tier = 0, displayName = "item.recipe.full", rarity = "Rare", maxStack = 10, basePrice = 200, sellRatio = 0.3f },
            new TotemItemCatalogEntry { itemId = 4001, itemType = "Equipment", subType = "Common", tier = 0, displayName = "item.equipment.common_weapon", rarity = "Common", maxStack = 1, basePrice = 80, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 4002, itemType = "Equipment", subType = "Uncommon", tier = 0, displayName = "item.equipment.uncommon_weapon", rarity = "Uncommon", maxStack = 1, basePrice = 150, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 4003, itemType = "Equipment", subType = "Rare", tier = 0, displayName = "item.equipment.rare_weapon", rarity = "Rare", maxStack = 1, basePrice = 250, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 4004, itemType = "Equipment", subType = "Legendary", tier = 0, displayName = "item.equipment.legendary_weapon", rarity = "Legendary", maxStack = 1, basePrice = 500, sellRatio = 0.4f },
            new TotemItemCatalogEntry { itemId = 5001, itemType = "Antidote", subType = "Detox", tier = 0, displayName = "item.antidote.detox", rarity = "Common", maxStack = 5, basePrice = 70, sellRatio = 0.3f },
            new TotemItemCatalogEntry { itemId = 5002, itemType = "Antidote", subType = "Thaw", tier = 0, displayName = "item.antidote.thaw", rarity = "Common", maxStack = 5, basePrice = 70, sellRatio = 0.3f },
            new TotemItemCatalogEntry { itemId = 5003, itemType = "Antidote", subType = "Unstun", tier = 0, displayName = "item.antidote.unstun", rarity = "Common", maxStack = 5, basePrice = 70, sellRatio = 0.3f },
        };
    }

    private static TotemResourceCatalogEntry[] BuildDefaultResources()
    {
        // Tattoo sprite bindings were retired when the old Assets/Game/Sprite/Tattoo art was
        // deleted. Keep Tattoo gameplay tables active, but leave visual ResourceConfig rows
        // empty until the replacement art pass produces approved sprites.
        return Array.Empty<TotemResourceCatalogEntry>();
    }

    private static TotemMerchantSlotCatalogEntry[] BuildDefaultMerchantSlots()
    {
        return new[]
        {
            new TotemMerchantSlotCatalogEntry { slotIndex = 0, weaponId = "knife_basic", goldCost = 50, refreshWeight = 40 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 0, weaponId = "pistol_basic", goldCost = 75, refreshWeight = 35 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 0, weaponId = "energy_fist", goldCost = 90, refreshWeight = 25 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 1, weaponId = "hammer_heavy", goldCost = 80, refreshWeight = 40 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 1, weaponId = "pistol_basic", goldCost = 85, refreshWeight = 35 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 1, weaponId = "bow_charge", goldCost = 120, refreshWeight = 25 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 2, weaponId = "bow_charge", goldCost = 140, refreshWeight = 35 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 2, weaponId = "energy_fist", goldCost = 130, refreshWeight = 35 },
            new TotemMerchantSlotCatalogEntry { slotIndex = 2, weaponId = "hammer_heavy", goldCost = 100, refreshWeight = 30 },
        };
    }

    private static TotemWeaponDropCatalogEntry[] BuildDefaultWeaponDrops()
    {
        return new[]
        {
            new TotemWeaponDropCatalogEntry { dropId = "drop_elite_001", weaponId = "knife_basic", dropSource = "Elite", weight = 35, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_elite_002", weaponId = "hammer_heavy", dropSource = "Elite", weight = 25, minRoomIndex = 2, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_elite_003", weaponId = "pistol_basic", dropSource = "Elite", weight = 20, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_elite_004", weaponId = "bow_charge", dropSource = "Elite", weight = 10, minRoomIndex = 3, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_elite_005", weaponId = "energy_fist", dropSource = "Elite", weight = 10, minRoomIndex = 3, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_chest_001", weaponId = "knife_basic", dropSource = "Chest", weight = 30, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_chest_002", weaponId = "hammer_heavy", dropSource = "Chest", weight = 20, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_chest_003", weaponId = "pistol_basic", dropSource = "Chest", weight = 25, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_chest_004", weaponId = "bow_charge", dropSource = "Chest", weight = 15, minRoomIndex = 2, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_chest_005", weaponId = "energy_fist", dropSource = "Chest", weight = 10, minRoomIndex = 2, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_merchant_001", weaponId = "knife_basic", dropSource = "Merchant", weight = 20, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_merchant_002", weaponId = "hammer_heavy", dropSource = "Merchant", weight = 25, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_merchant_003", weaponId = "pistol_basic", dropSource = "Merchant", weight = 30, minRoomIndex = 1, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_merchant_004", weaponId = "bow_charge", dropSource = "Merchant", weight = 15, minRoomIndex = 2, maxRoomIndex = 10 },
            new TotemWeaponDropCatalogEntry { dropId = "drop_merchant_005", weaponId = "energy_fist", dropSource = "Merchant", weight = 10, minRoomIndex = 2, maxRoomIndex = 10 },
        };
    }

    public static TotemTattooReadingTimeCatalogEntry[] BuildDefaultTattooReadingTimes()
    {
        return new[]
        {
            new TotemTattooReadingTimeCatalogEntry { partId = 1, partName = "Head", durationSec = 8.0f },
            new TotemTattooReadingTimeCatalogEntry { partId = 2, partName = "Torso", durationSec = 8.0f },
            new TotemTattooReadingTimeCatalogEntry { partId = 3, partName = "LeftArm", durationSec = 5.0f },
            new TotemTattooReadingTimeCatalogEntry { partId = 4, partName = "RightArm", durationSec = 5.0f },
            new TotemTattooReadingTimeCatalogEntry { partId = 5, partName = "LeftLeg", durationSec = 3.0f },
            new TotemTattooReadingTimeCatalogEntry { partId = 6, partName = "RightLeg", durationSec = 3.0f },
        };
    }

    public static TotemTattooElementCatalogEntry[] BuildDefaultTattooElements()
    {
        return new[]
        {
            new TotemTattooElementCatalogEntry { id = 1, name = "Fire", baseMultiplier = 1.0f, param1 = 2.0f, param2 = 3.0f, param3 = 0.0f },
            new TotemTattooElementCatalogEntry { id = 2, name = "Lightning", baseMultiplier = 1.0f, param1 = 1.0f, param2 = 0.0f, param3 = 0.0f },
            new TotemTattooElementCatalogEntry { id = 3, name = "Nature", baseMultiplier = 1.0f, param1 = 1.5f, param2 = 4.0f, param3 = 5.0f },
            new TotemTattooElementCatalogEntry { id = 4, name = "Frost", baseMultiplier = 1.0f, param1 = 0.30f, param2 = 2.0f, param3 = 5.0f },
            new TotemTattooElementCatalogEntry { id = 5, name = "Mutation", baseMultiplier = 1.0f, param1 = 0.0f, param2 = 0.0f, param3 = 42.0f },
            new TotemTattooElementCatalogEntry { id = 6, name = "Holy", baseMultiplier = 1.0f, param1 = 0.15f, param2 = 0.0f, param3 = 0.0f },
            new TotemTattooElementCatalogEntry { id = 7, name = "Pure", baseMultiplier = 1.0f, param1 = 0.20f, param2 = 0.01f, param3 = 5.0f },
        };
    }

    public static TotemTattooShapeCatalogEntry[] BuildDefaultTattooShapes()
    {
        return new[]
        {
            new TotemTattooShapeCatalogEntry { id = 1, name = "SingleHit", param1 = 0.0f, param2 = 0.0f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 2, name = "AOEBurst", param1 = 0.6f, param2 = 5.0f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 3, name = "StackingMark", param1 = 5.0f, param2 = 4.0f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 4, name = "MultiHit", param1 = 4.0f, param2 = 0.0f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 5, name = "ChainJump", param1 = 3.0f, param2 = 0.7f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 6, name = "ProbBurst", param1 = 1.0f, param2 = 2.0f, param3 = 12345.0f },
            new TotemTattooShapeCatalogEntry { id = 7, name = "TrailZone", param1 = 0.4f, param2 = 3.0f, param3 = 0.0f },
            new TotemTattooShapeCatalogEntry { id = 8, name = "SummonForm", param1 = 2.5f, param2 = 0.0f, param3 = 0.0f },
        };
    }

    public static TotemTattooEnchantAffixCatalogEntry[] BuildDefaultTattooEnchantAffixes()
    {
        return new[]
        {
            new TotemTattooEnchantAffixCatalogEntry { id = 1, partId = 0, colorTier = "Common", affixType = "ElementDamageBonus", statKey = "ElementDmg", value = 0.10f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Element damage +10%", weight = 5.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 2, partId = 0, colorTier = "Common", affixType = "AttackSpeed", statKey = "AttackSpeed", value = 0.08f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Attack speed +8%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 3, partId = 0, colorTier = "Common", affixType = "CritChance", statKey = "CritRate", value = 0.05f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit chance +5%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 4, partId = 0, colorTier = "Common", affixType = "CooldownReduction", statKey = "CooldownPct", value = 0.10f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Cooldown reduction +10%", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 5, partId = 0, colorTier = "Common", affixType = "SelfHealOnHit", statKey = "HealOnHit", value = 5.0f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Heal on hit +5", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 6, partId = 0, colorTier = "Common", affixType = "StatusChance", statKey = "StatusChance", value = 0.08f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Status chance +8%", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 7, partId = 0, colorTier = "Common", affixType = "RangeBonus", statKey = "Range", value = 0.10f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Range +10%", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 8, partId = 0, colorTier = "Common", affixType = "CritDamage", statKey = "CritDmg", value = 0.15f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit damage +15%", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 9, partId = 0, colorTier = "Rare", affixType = "ElementDamageBonus", statKey = "ElementDmg", value = 0.20f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Element damage +20%", weight = 5.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 10, partId = 0, colorTier = "Rare", affixType = "CritChance", statKey = "CritRate", value = 0.10f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit chance +10%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 11, partId = 0, colorTier = "Rare", affixType = "CritDamage", statKey = "CritDmg", value = 0.25f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit damage +25%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 12, partId = 0, colorTier = "Rare", affixType = "ElementDamageBonus", statKey = "ElementDmg", value = 0.30f, conditionKey = "DistanceGt8m", conditionVal = 8f, displayText = "Element damage +30% beyond 8m", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 13, partId = 0, colorTier = "Rare", affixType = "AttackSpeed", statKey = "AttackSpeed", value = 0.15f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Attack speed +15%", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 14, partId = 0, colorTier = "Rare", affixType = "SelfHealOnHit", statKey = "HealOnHit", value = 12.0f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Heal on hit +12", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 15, partId = 0, colorTier = "Rare", affixType = "CooldownReduction", statKey = "CooldownPct", value = 0.18f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Cooldown reduction +18%", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 16, partId = 0, colorTier = "Rare", affixType = "StatusChance", statKey = "StatusChance", value = 0.15f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Status chance +15%", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 17, partId = 0, colorTier = "Legendary", affixType = "ElementDamageBonus", statKey = "ElementDmg", value = 0.35f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Element damage +35%", weight = 5.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 18, partId = 0, colorTier = "Legendary", affixType = "CritChance", statKey = "CritRate", value = 0.18f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit chance +18%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 19, partId = 0, colorTier = "Legendary", affixType = "CritDamage", statKey = "CritDmg", value = 0.40f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Crit damage +40%", weight = 4.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 20, partId = 0, colorTier = "Legendary", affixType = "ElementDamageBonus", statKey = "ElementDmg", value = 0.45f, conditionKey = "AfterDodge", conditionVal = 0f, displayText = "Element damage +45% after dodge", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 21, partId = 0, colorTier = "Legendary", affixType = "AttackSpeed", statKey = "AttackSpeed", value = 0.25f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Attack speed +25%", weight = 3.5f },
            new TotemTattooEnchantAffixCatalogEntry { id = 22, partId = 0, colorTier = "Legendary", affixType = "CooldownReduction", statKey = "CooldownPct", value = 0.30f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Cooldown reduction +30%", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 23, partId = 0, colorTier = "Legendary", affixType = "SelfHealOnHit", statKey = "HealOnHit", value = 25.0f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Heal on hit +25", weight = 3.0f },
            new TotemTattooEnchantAffixCatalogEntry { id = 24, partId = 0, colorTier = "Legendary", affixType = "StatusChance", statKey = "StatusChance", value = 0.25f, conditionKey = string.Empty, conditionVal = 0f, displayText = "Status chance +25%", weight = 3.0f },
        };
    }

    public static TotemTattooEnchantRecipeCatalogEntry[] BuildDefaultTattooEnchantRecipes()
    {
        return new[]
        {
            new TotemTattooEnchantRecipeCatalogEntry { id = 1, colorTier = "Common", coinCost = 200, rarePigmentCost = 1, maxAffixPerSlot = 2 },
            new TotemTattooEnchantRecipeCatalogEntry { id = 2, colorTier = "Rare", coinCost = 350, rarePigmentCost = 1, maxAffixPerSlot = 2 },
            new TotemTattooEnchantRecipeCatalogEntry { id = 3, colorTier = "Legendary", coinCost = 500, rarePigmentCost = 1, maxAffixPerSlot = 2 },
        };
    }

    private static TotemProjectileCatalogEntry[] BuildDefaultProjectiles()
    {
        return new[]
        {
            new TotemProjectileCatalogEntry { projectileId = "bullet_pistol", speed = 30f, maxRange = 20f, piercing = false, aoeRadius = 0f, visualPrefabPath = "BulletPistol", poolSize = 60 },
            new TotemProjectileCatalogEntry { projectileId = "arrow_bow", speed = 22f, maxRange = 30f, piercing = true, aoeRadius = 0f, visualPrefabPath = "ArrowBow", poolSize = 30 },
        };
    }

    private static TotemWeaponTraitCatalogEntry[] BuildDefaultWeaponTraits()
    {
        return new[]
        {
            new TotemWeaponTraitCatalogEntry { traitId = "trait_quickslash", displayName = "Quick Slash", description = "Shorter recovery and small burn pressure after repeated hits.", effectType = "Quick", effectParam1 = 0.30f, effectParam2 = 4.0f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_pierce", displayName = "Pierce", description = "Can pierce multiple targets with per-pierce damage falloff.", effectType = "Pierce", effectParam1 = 3.0f, effectParam2 = 0.20f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_stun", displayName = "Stun", description = "Hit target is stunned briefly and takes bonus damage while stunned.", effectType = "Stun", effectParam1 = 0.8f, effectParam2 = 0.25f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_chain", displayName = "Chain", description = "Hit can jump to nearby targets with damage falloff.", effectType = "Chain", effectParam1 = 3.0f, effectParam2 = 0.30f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_explosive", displayName = "Explosive", description = "Charged hit can explode around the target.", effectType = "Explosive", effectParam1 = 2.5f, effectParam2 = 0.60f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_multishot", displayName = "Multi Shot", description = "Charged shot can fire a fan of projectiles.", effectType = "MultiShot", effectParam1 = 3.0f, effectParam2 = 18.0f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_pull", displayName = "Pull", description = "Charged hit can pull the target toward the attacker.", effectType = "Pull", effectParam1 = 1.5f, effectParam2 = 5.0f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_dot_burn", displayName = "Burn", description = "Applies burn damage over time.", effectType = "Status", effectParam1 = 4.0f, effectParam2 = 2.5f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_dot_poison", displayName = "Poison", description = "Applies poison damage over time.", effectType = "Status", effectParam1 = 3.0f, effectParam2 = 3.0f },
            new TotemWeaponTraitCatalogEntry { traitId = "trait_lifesteal", displayName = "Life Steal", description = "Heals the source for 8% of dealt weapon damage, capped at 12 HP per hit.", effectType = "Quick", effectParam1 = 0.08f, effectParam2 = 12.0f },
        };
    }

    private static TotemChestRewardCatalogEntry[] BuildDefaultChestRewards()
    {
        return new[]
        {
            new TotemChestRewardCatalogEntry { chestId = "chest_common", rewardType = "Weapon", rewardId = string.Empty, rewardAmount = 1, probability = 45 },
            new TotemChestRewardCatalogEntry { chestId = "chest_common", rewardType = "Gold", rewardId = string.Empty, rewardAmount = 45, probability = 40 },
            new TotemChestRewardCatalogEntry { chestId = "chest_common", rewardType = "Potion", rewardId = string.Empty, rewardAmount = 1, probability = 15 },
            new TotemChestRewardCatalogEntry { chestId = "chest_rare", rewardType = "Weapon", rewardId = string.Empty, rewardAmount = 1, probability = 60 },
            new TotemChestRewardCatalogEntry { chestId = "chest_rare", rewardType = "Gold", rewardId = string.Empty, rewardAmount = 95, probability = 30 },
            new TotemChestRewardCatalogEntry { chestId = "chest_rare", rewardType = "Potion", rewardId = string.Empty, rewardAmount = 2, probability = 10 },
        };
    }

    public static TotemMapTemplateCatalogEntry[] BuildDefaultMapTemplates()
    {
        return new[]
        {
            new TotemMapTemplateCatalogEntry { id = 1, themeName = "AI_RUINS", mapSize = 400f, minRoomSize = 40f, terrainPoolId = 101, prefabPath = string.Empty, hudAccentColor = "#66CCFF", dominantColor = "#3A4858" },
            new TotemMapTemplateCatalogEntry { id = 2, themeName = "ALIEN_HIVE", mapSize = 400f, minRoomSize = 40f, terrainPoolId = 102, prefabPath = string.Empty, hudAccentColor = "#7DFF88", dominantColor = "#273A22" },
            new TotemMapTemplateCatalogEntry { id = 3, themeName = "VIRUS_SWAMP", mapSize = 400f, minRoomSize = 40f, terrainPoolId = 103, prefabPath = string.Empty, hudAccentColor = "#B6FF3C", dominantColor = "#233A35" },
        };
    }

    private static TotemSkillCatalogEntry[] BuildDefaultSkills()
    {
        return new[]
        {
            new TotemSkillCatalogEntry { skillId = "skill_fireball_01", displayName = "Fireball", chargeModel = 0, cooldown = 7f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 8, activeFrames = 4, recoveryFrames = 12, damageMul = 2.4f, hitShape = "circle", hitRadius = 3f, element = "Fire", cancelableByDodge = false, itemId = 1001 },
            new TotemSkillCatalogEntry { skillId = "skill_frost_field_01", displayName = "Frost Field", chargeModel = 0, cooldown = 10f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 12, activeFrames = 6, recoveryFrames = 18, damageMul = 1.6f, hitShape = "circle", hitRadius = 4f, element = "Frost", cancelableByDodge = false, itemId = 1002 },
            new TotemSkillCatalogEntry { skillId = "skill_chain_lightning_01", displayName = "Chain Lightning", chargeModel = 1, cooldown = 0f, maxCharges = 3, chargeRegenTime = 8f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 6, activeFrames = 3, recoveryFrames = 9, damageMul = 1.35f, hitShape = "single", hitRadius = 0f, element = "Lightning", cancelableByDodge = true, itemId = 1003 },
            new TotemSkillCatalogEntry { skillId = "skill_heal_aura_01", displayName = "Heal Aura", chargeModel = 0, cooldown = 12f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 6, activeFrames = 60, recoveryFrames = 6, damageMul = 0f, hitShape = "circle", hitRadius = 5f, element = "Holy", cancelableByDodge = true, itemId = 1004 },
            new TotemSkillCatalogEntry { skillId = "skill_shield_01", displayName = "Shield", chargeModel = 0, cooldown = 12f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 4, activeFrames = 120, recoveryFrames = 6, damageMul = 0f, hitShape = "single", hitRadius = 0f, element = "None", cancelableByDodge = false, itemId = 1005 },
            new TotemSkillCatalogEntry { skillId = "skill_stealth_01", displayName = "Stealth", chargeModel = 2, cooldown = 0f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 1.5f, overchargeWindow = 0.8f, startupFrames = 3, activeFrames = 180, recoveryFrames = 6, damageMul = 0f, hitShape = "single", hitRadius = 0f, element = "None", cancelableByDodge = true, itemId = 1006 },
            new TotemSkillCatalogEntry { skillId = "skill_summon_01", displayName = "Summon", chargeModel = 0, cooldown = 15f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 18, activeFrames = 600, recoveryFrames = 12, damageMul = 1.2f, hitShape = "single", hitRadius = 0f, element = "Nature", cancelableByDodge = false, itemId = 1007 },
            new TotemSkillCatalogEntry { skillId = "skill_time_slow_01", displayName = "Time Slow", chargeModel = 0, cooldown = 20f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 6, activeFrames = 300, recoveryFrames = 6, damageMul = 0f, hitShape = "circle", hitRadius = 8f, element = "None", cancelableByDodge = false, itemId = 1008 },
            new TotemSkillCatalogEntry { skillId = "skill_phase_dash", displayName = "Phase Dash", chargeModel = 0, cooldown = 5f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 3, activeFrames = 8, recoveryFrames = 10, damageMul = 0.8f, hitShape = "single", hitRadius = 0f, element = "Pure", cancelableByDodge = true, itemId = 1009 },
            new TotemSkillCatalogEntry { skillId = "skill_ink_shield", displayName = "Ink Shield", chargeModel = 0, cooldown = 10f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 4, activeFrames = 120, recoveryFrames = 8, damageMul = 0f, hitShape = "circle", hitRadius = 4f, element = "Pure", cancelableByDodge = true, itemId = 1010 },
            new TotemSkillCatalogEntry { skillId = "skill_stomp", displayName = "Boss Stomp", chargeModel = 0, cooldown = 4f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 10, activeFrames = 8, recoveryFrames = 18, damageMul = 0f, damage = 26f, hitShape = "circle", hitRadius = 4.5f, element = "Pure", cancelableByDodge = false, itemId = 0 },
            new TotemSkillCatalogEntry { skillId = "skill_beam", displayName = "Boss Beam", chargeModel = 0, cooldown = 4f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 14, activeFrames = 12, recoveryFrames = 20, damageMul = 0f, damage = 22f, hitShape = "single", hitRadius = 8f, element = "Lightning", cancelableByDodge = false, itemId = 0 },
            new TotemSkillCatalogEntry { skillId = "skill_summon", displayName = "Boss Summon", chargeModel = 0, cooldown = 6f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 18, activeFrames = 60, recoveryFrames = 18, damageMul = 0f, damage = 14f, hitShape = "circle", hitRadius = 5f, element = "Nature", cancelableByDodge = false, itemId = 0 },
            new TotemSkillCatalogEntry { skillId = "skill_enrage_aoe", displayName = "Boss Enrage AOE", chargeModel = 0, cooldown = 4f, maxCharges = 1, chargeRegenTime = 0f, holdDuration = 0f, overchargeWindow = 0f, startupFrames = 16, activeFrames = 14, recoveryFrames = 24, damageMul = 0f, damage = 30f, hitShape = "circle", hitRadius = 7f, element = "Fire", cancelableByDodge = false, itemId = 0 },
        };
    }

    private static TotemEnemyCatalogEntry[] BuildDefaultEnemies()
    {
        return new[]
        {
            Enemy("enemy_common_hunter", "common", "Light", "light_hunter", "ability_melee_claw", 64f, 8f, 3.8f, 1.7f, 18f, 32f, "loot_light_common", "loot_light_common_coin", 1, "pool_common_light"),
            Enemy("enemy_common_shooter", "common", "Light", "light_kiter", "ability_projectile_bolt", 50f, 7f, 3.4f, 10f, 22f, 36f, "loot_light_common", "loot_light_common_coin", 1, "pool_common_light"),
            Enemy("enemy_common_guardian", "common", "Elite", "elite_support", "ability_shield_guardian,ability_area_guardian", 240f, 16f, 2.8f, 5f, 24f, 40f, "loot_elite_common", "loot_elite_common_coin,loot_elite_common_paint", 4, "pool_common_elite"),
            Enemy("enemy_ai_servo", "ai_ruins", "Light", "light_pack_alert", "ability_melee_shock", 70f, 9f, 4f, 1.8f, 20f, 34f, "loot_light_ai_ruins", "loot_light_ai_ruins_coin", 1, "pool_ai_ruins_light"),
            Enemy("enemy_ai_arc_drone", "ai_ruins", "Light", "light_strafer", "ability_beam_arc", 54f, 8f, 4.3f, 11f, 24f, 38f, "loot_light_ai_ruins", "loot_light_ai_ruins_coin", 2, "pool_ai_ruins_light"),
            Enemy("enemy_ai_manager", "ai_ruins", "Elite", "elite_commander", "ability_shield_manager,ability_area_emp,ability_summon_servo", 220f, 17f, 3.1f, 8f, 28f, 44f, "loot_elite_ai_ruins", "loot_elite_ai_ruins_coin,loot_elite_ai_ruins_paint", 5, "pool_ai_ruins_elite"),
            Enemy("boss_ai_core_zero", "ai_ruins", "Boss", "boss_phase_controller", "ability_area_emp,ability_beam_core,ability_summon_servo,ability_hazard_overload,ability_phase_transition", 1250f, 32f, 3f, 14f, 45f, 60f, "loot_boss_ai_ruins", "loot_boss_ai_ruins_recipe,loot_boss_ai_ruins_paint,loot_boss_ai_ruins_coin", 12, "pool_ai_ruins_boss", "skill_stomp,skill_beam,skill_summon"),
            Enemy("enemy_alien_crawler", "alien_hive", "Light", "light_flanker", "ability_leap_crawler,ability_melee_claw", 76f, 10f, 4.6f, 2f, 20f, 35f, "loot_light_alien_hive", "loot_light_alien_hive_coin", 2, "pool_alien_hive_light"),
            Enemy("enemy_alien_spitter", "alien_hive", "Light", "light_kiter", "ability_projectile_acid,ability_hazard_acid", 58f, 8f, 3.5f, 12f, 25f, 40f, "loot_light_alien_hive", "loot_light_alien_hive_coin", 2, "pool_alien_hive_light"),
            Enemy("enemy_alien_guard", "alien_hive", "Elite", "elite_zone_control", "ability_cone_alien,ability_summon_crawler,ability_hazard_acid", 270f, 19f, 3.2f, 7f, 28f, 45f, "loot_elite_alien_hive", "loot_elite_alien_hive_coin,loot_elite_alien_hive_paint", 5, "pool_alien_hive_elite"),
            Enemy("boss_alien_hive_mother", "alien_hive", "Boss", "boss_phase_controller", "ability_cone_alien,ability_summon_crawler,ability_hazard_acid,ability_phase_transition", 1380f, 34f, 3.4f, 10f, 46f, 62f, "loot_boss_alien_hive", "loot_boss_alien_hive_recipe,loot_boss_alien_hive_paint,loot_boss_alien_hive_coin", 12, "pool_alien_hive_boss", "skill_stomp,skill_summon,skill_enrage_aoe"),
            Enemy("enemy_virus_mutant", "virus_swamp", "Light", "light_berserker", "ability_charge_mutant", 82f, 11f, 4.1f, 5f, 21f, 36f, "loot_light_virus_swamp", "loot_light_virus_swamp_coin", 2, "pool_virus_swamp_light"),
            Enemy("enemy_virus_spore_carrier", "virus_swamp", "Light", "light_suicide_zoner", "ability_projectile_spore,ability_death_burst_spore,ability_hazard_virus", 62f, 9f, 3.6f, 10f, 24f, 38f, "loot_light_virus_swamp", "loot_light_virus_swamp_coin", 2, "pool_virus_swamp_light"),
            Enemy("enemy_virus_spore_host", "virus_swamp", "Elite", "elite_regenerator", "ability_area_spore,ability_regenerate_spore,ability_summon_spore", 290f, 18f, 2.9f, 7f, 28f, 46f, "loot_elite_virus_swamp", "loot_elite_virus_swamp_coin,loot_elite_virus_swamp_paint", 5, "pool_virus_swamp_elite"),
            Enemy("boss_virus_terminus", "virus_swamp", "Boss", "boss_split_controller", "ability_charge_boss,ability_hazard_virus,ability_summon_spore,ability_regenerate_spore,ability_phase_transition", 1500f, 36f, 3.6f, 8f, 48f, 65f, "loot_boss_virus_swamp", "loot_boss_virus_swamp_recipe,loot_boss_virus_swamp_paint,loot_boss_virus_swamp_coin", 13, "pool_virus_swamp_boss", "skill_stomp,skill_summon,skill_enrage_aoe"),
        };
    }

    private static TotemEnemyCatalogEntry Enemy(string id, string theme, string tier, string behavior, string abilities,
        float hp, float damage, float speed, float attackRange, float detectRange, float leashRange,
        string lootTable, string guaranteedLoot, int spawnCost, string pools, string legacySkills = "")
    {
        string assetKey = "enemy." + id;
        return new TotemEnemyCatalogEntry
        {
            enemyId = id,
            displayName = id.Replace('_', '.'),
            themeId = theme,
            tier = tier,
            runtimeAssetKey = assetKey,
            fallbackRuntimeAssetKey = "enemy.fallback." + theme + "." + tier.ToLowerInvariant(),
            behaviorProfileId = behavior,
            abilityIds = abilities,
            baseHP = hp,
            hpCurveK = tier == "Boss" ? 0f : 0.1f,
            baseDamage = damage,
            damageCurveK = tier == "Boss" ? 0f : 0.05f,
            moveSpeed = speed,
            attackRange = attackRange,
            detectRange = detectRange,
            leashRange = leashRange,
            lootTableId = lootTable,
            guaranteedLootIds = guaranteedLoot,
            spawnCost = spawnCost,
            poolIds = pools,
            skillIds = legacySkills,
            elitePaintDropRare = tier == "Elite" ? 1 : 0,
            xpReward = tier == "Boss" ? 220 : tier == "Elite" ? 28 : 8,
            coinReward = tier == "Boss" ? "90-150" : tier == "Elite" ? "12-28" : "2-9",
        };
    }

    private static TotemEnemyAbilityCatalogEntry[] BuildDefaultEnemyAbilities()
    {
        return new[]
        {
            Ability("ability_melee_claw", "Melee"), Ability("ability_melee_shock", "Melee", statusId: "Shock", statusChance: 0.35f),
            Ability("ability_projectile_bolt", "Projectile"), Ability("ability_projectile_acid", "Projectile", statusId: "Poison", statusChance: 0.4f), Ability("ability_projectile_spore", "Projectile", statusId: "Slow", statusChance: 0.35f),
            Ability("ability_charge_mutant", "Charge"), Ability("ability_charge_boss", "Charge"), Ability("ability_leap_crawler", "Leap"),
            Ability("ability_beam_arc", "Beam"), Ability("ability_beam_core", "Beam", statusId: "Shock", statusChance: 0.5f), Ability("ability_cone_alien", "ConeSweep"),
            Ability("ability_area_guardian", "AreaPulse"), Ability("ability_area_emp", "AreaPulse"), Ability("ability_area_spore", "AreaPulse"),
            Ability("ability_hazard_overload", "HazardZone"), Ability("ability_hazard_acid", "HazardZone"), Ability("ability_hazard_virus", "HazardZone", statusId: "Poison", statusChance: 0.6f),
            Ability("ability_shield_guardian", "Shield", 0f), Ability("ability_shield_manager", "Shield", 0f),
            Ability("ability_summon_servo", "Summon", 0f, "enemy_ai_servo", 2),
            Ability("ability_summon_crawler", "Summon", 0f, "enemy_alien_crawler", 2),
            Ability("ability_summon_spore", "Summon", 0f, "enemy_virus_spore_carrier", 2),
            Ability("ability_regenerate_spore", "Regenerate", 0f), Ability("ability_death_burst_spore", "DeathBurst"),
            Ability("ability_phase_transition", "PhaseTransition", 0f),
        };
    }

    private static TotemEnemyAbilityCatalogEntry Ability(string id, string type, float damageMultiplier = 1f, string summonEnemyId = "", int summonCount = 0,
        string statusId = "", float statusChance = 0f)
    {
        return new TotemEnemyAbilityCatalogEntry
        {
            abilityId = id,
            abilityType = type,
            range = type == "Melee" ? 2f : 12f,
            radius = 1f,
            cooldown = type == "DeathBurst" || type == "PhaseTransition" ? 0f : 4f,
            windup = type == "DeathBurst" ? 0f : 0.5f,
            active = 0.1f,
            recovery = type == "DeathBurst" ? 0f : 0.6f,
            damageMultiplier = damageMultiplier,
            statusId = statusId,
            statusChance = statusChance,
            summonEnemyId = summonEnemyId,
            summonCount = summonCount,
            audioCueId = type == "Projectile" || type == "Beam" ? "sfx_hit_ranged" : "sfx_hit_special",
            parametersJson = "{}",
        };
    }

    private static TotemEncounterSpawnCatalogEntry[] BuildDefaultEncounterSpawns()
    {
        return new[]
        {
            Encounter("ai_ruins", "light", "pool_common_light,pool_ai_ruins_light", 0f, 18, 30, 60, false),
            Encounter("ai_ruins", "elite", "pool_common_elite,pool_ai_ruins_elite", 240f, 5, 8, 8, false),
            Encounter("ai_ruins", "boss", "pool_ai_ruins_boss", 600f, 1, 1, 1, true),
            Encounter("alien_hive", "light", "pool_common_light,pool_alien_hive_light", 0f, 18, 30, 60, false),
            Encounter("alien_hive", "elite", "pool_common_elite,pool_alien_hive_elite", 240f, 5, 8, 8, false),
            Encounter("alien_hive", "boss", "pool_alien_hive_boss", 600f, 1, 1, 1, true),
            Encounter("virus_swamp", "light", "pool_common_light,pool_virus_swamp_light", 0f, 18, 30, 60, false),
            Encounter("virus_swamp", "elite", "pool_common_elite,pool_virus_swamp_elite", 240f, 5, 8, 8, false),
            Encounter("virus_swamp", "boss", "pool_virus_swamp_boss", 600f, 1, 1, 1, true),
        };
    }

    private static TotemEncounterSpawnCatalogEntry Encounter(string theme, string tier, string pools, float startTime,
        int initialCount, int activeCap, int totalCap, bool unique)
    {
        bool isBoss = tier == "boss";
        bool isElite = tier == "elite";
        return new TotemEncounterSpawnCatalogEntry
        {
            encounterId = "encounter_" + theme + "." + tier,
            themeId = theme,
            zoneRoles = isBoss ? "BossSpawn" : isElite ? "EliteSpawn" : "EnemySpawn",
            enemyPoolIds = pools,
            startTime = startTime,
            endTime = isElite ? 599f : 0f,
            initialCount = initialCount,
            activeCap = activeCap,
            totalCap = totalCap,
            waveMin = isBoss ? 1 : isElite ? 1 : 4,
            waveMax = isBoss ? 1 : isElite ? 2 : 6,
            waveInterval = isBoss ? 0f : isElite ? 60f : 45f,
            minParticipantDistance = isBoss ? 45f : isElite ? 35f : 25f,
            minSpacing = isBoss ? 12f : isElite ? 8f : 4f,
            weight = 100,
            unique = unique,
        };
    }

    private static TotemEnemyLootCatalogEntry[] BuildDefaultEnemyLoot()
    {
        var result = new List<TotemEnemyLootCatalogEntry>(37);
        AddLightLoot(result, "common", "2101", "5001", "Item");
        AddLightLoot(result, "ai_ruins", "2401", "5003", "Item");
        AddLightLoot(result, "alien_hive", "2601", "4001", "Equipment");
        AddLightLoot(result, "virus_swamp", "2501", "5001", "Item");
        AddEliteLoot(result, "common", "2303", "knife_basic", "4002");
        AddEliteLoot(result, "ai_ruins", "2403", "pistol_basic", "4003");
        AddEliteLoot(result, "alien_hive", "2603", "bow_charge", "4003");
        AddEliteLoot(result, "virus_swamp", "2503", "energy_fist", "4003");
        AddBossLoot(result, "ai_ruins", "recipe_ai_ruins_boss", "2403");
        AddBossLoot(result, "alien_hive", "recipe_alien_hive_boss", "2603");
        AddBossLoot(result, "virus_swamp", "recipe_virus_swamp_boss", "2503");
        return result.ToArray();
    }

    private static void AddLightLoot(List<TotemEnemyLootCatalogEntry> rows, string theme, string paintId, string supplyId, string supplyType)
    {
        string table = "loot_light_" + theme;
        rows.Add(Loot(table + "_coin", table, "1", "Coin", 2, 9, 0, true, "Light", theme));
        rows.Add(Loot(table + "_paint", table, paintId, "Paint", 1, 1, 28, false, "Light", theme));
        rows.Add(Loot(table + "_supply", table, supplyId, supplyType, 1, 1, 12, false, "Light", theme));
    }

    private static void AddEliteLoot(List<TotemEnemyLootCatalogEntry> rows, string theme, string paintId, string weaponId, string equipmentId)
    {
        string table = "loot_elite_" + theme;
        rows.Add(Loot(table + "_coin", table, "1", "Coin", 12, 28, 0, true, "Elite", theme));
        rows.Add(Loot(table + "_paint", table, paintId, "Paint", 1, 1, 0, true, "Elite", theme));
        rows.Add(Loot(table + "_weapon", table, weaponId, "Weapon", 1, 1, 55, false, "Elite", theme));
        rows.Add(Loot(table + "_equipment", table, equipmentId, "Equipment", 1, 1, 45, false, "Elite", theme));
    }

    private static void AddBossLoot(List<TotemEnemyLootCatalogEntry> rows, string theme, string recipeId, string paintId)
    {
        string table = "loot_boss_" + theme;
        rows.Add(Loot(table + "_recipe", table, recipeId, "Recipe", 1, 1, 0, true, "Boss", theme));
        rows.Add(Loot(table + "_paint", table, paintId, "Paint", 2, 3, 0, true, "Boss", theme));
        rows.Add(Loot(table + "_coin", table, "1", "Coin", 90, 150, 0, true, "Boss", theme));
    }

    private static TotemEnemyLootCatalogEntry Loot(string entryId, string tableId, string itemId, string rewardType,
        int minCount, int maxCount, int weight, bool guaranteed, string tier, string theme)
    {
        return new TotemEnemyLootCatalogEntry
        {
            lootEntryId = entryId,
            lootTableId = tableId,
            itemId = itemId,
            rewardType = rewardType,
            minCount = minCount,
            maxCount = maxCount,
            weight = weight,
            guaranteed = guaranteed,
            tierFilter = tier,
            themeId = theme,
        };
    }

    private static TotemBossPhaseCatalogEntry[] BuildDefaultBossPhases()
    {
        return new[]
        {
            BossPhase("boss_ai_core_zero", 1, "ability_area_emp,ability_beam_core", "skill_stomp,skill_beam", 1f, "ai", ""),
            BossPhase("boss_ai_core_zero", 2, "ability_summon_servo,ability_hazard_overload", "skill_summon", 1.18f, "ai", ""),
            BossPhase("boss_ai_core_zero", 3, "ability_phase_transition,ability_beam_core,ability_hazard_overload", "skill_enrage_aoe", 1.38f, "ai", "recipe_ai_ruins_boss"),
            BossPhase("boss_alien_hive_mother", 1, "ability_cone_alien,ability_hazard_acid", "skill_stomp", 1f, "alien", ""),
            BossPhase("boss_alien_hive_mother", 2, "ability_summon_crawler,ability_hazard_acid", "skill_summon", 1.2f, "alien", ""),
            BossPhase("boss_alien_hive_mother", 3, "ability_phase_transition,ability_cone_alien,ability_summon_crawler", "skill_enrage_aoe", 1.4f, "alien", "recipe_alien_hive_boss"),
            BossPhase("boss_virus_terminus", 1, "ability_charge_boss,ability_hazard_virus", "skill_stomp", 1f, "virus", ""),
            BossPhase("boss_virus_terminus", 2, "ability_summon_spore,ability_regenerate_spore", "skill_summon", 1.22f, "virus", ""),
            BossPhase("boss_virus_terminus", 3, "ability_phase_transition,ability_charge_boss,ability_death_burst_spore", "skill_enrage_aoe", 1.42f, "virus", "recipe_virus_swamp_boss"),
        };
    }

    private static TotemBossPhaseCatalogEntry BossPhase(string bossId, int phaseIndex, string abilities, string skills,
        float enrageMultiplier, string theme, string recipeId)
    {
        return new TotemBossPhaseCatalogEntry
        {
            bossId = bossId,
            phaseIndex = phaseIndex,
            hpThreshold = phaseIndex == 1 ? 1f : phaseIndex == 2 ? 0.6f : 0.3f,
            abilityIds = abilities,
            skillIds = skills,
            enrageMultiplier = enrageMultiplier,
            phaseVFXId = "vfx_boss_" + theme + "_phase" + phaseIndex,
            phaseBGMCueId = "bgm_boss_phase" + phaseIndex,
            deathPatternRecipeId = recipeId,
        };
    }

    private static TotemAudioCueCatalogEntry[] BuildDefaultAudioCues()
    {
        return new[]
        {
            new TotemAudioCueCatalogEntry { cueId = "bgm_main_menu", kind = "Bgm", assetName = "BGM/main_menu.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Main menu and non-combat front-end flow.", legacySource = "GameState.MainMenu" },
            new TotemAudioCueCatalogEntry { cueId = "bgm_in_game", kind = "Bgm", assetName = "BGM/in_game.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Combat baseline BGM before Boss phase override.", legacySource = "GameState.InGame" },
            new TotemAudioCueCatalogEntry { cueId = "bgm_boss_phase1", kind = "Bgm", assetName = "BGM/boss_phase1.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Boss phase 1 BGM.", legacySource = "BossPhaseConfig.phaseBGMCueId" },
            new TotemAudioCueCatalogEntry { cueId = "bgm_boss_phase2", kind = "Bgm", assetName = "BGM/boss_phase2.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Boss phase 2 BGM.", legacySource = "BossPhaseConfig.phaseBGMCueId" },
            new TotemAudioCueCatalogEntry { cueId = "bgm_boss_phase3", kind = "Bgm", assetName = "BGM/boss_phase3.ogg", volume = 1f, loop = true, minIntervalSec = 0f, usage = "Boss phase 3 BGM.", legacySource = "BossPhaseConfig.phaseBGMCueId" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_hit_melee", kind = "Sfx", assetName = "SFX/hit_melee.wav", volume = 1f, loop = false, minIntervalSec = 0.05f, usage = "Melee weapon hit.", legacySource = "WeaponClass.Melee" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_hit_ranged", kind = "Sfx", assetName = "SFX/hit_ranged.wav", volume = 1f, loop = false, minIntervalSec = 0.05f, usage = "Ranged weapon hit.", legacySource = "WeaponClass.Ranged" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_hit_special", kind = "Sfx", assetName = "SFX/hit_special.wav", volume = 1f, loop = false, minIntervalSec = 0.05f, usage = "Special weapon hit.", legacySource = "WeaponClass.Special" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_hit_default", kind = "Sfx", assetName = "SFX/hit_default.wav", volume = 1f, loop = false, minIntervalSec = 0.05f, usage = "Fallback hit cue when weapon class is unknown.", legacySource = "WeaponClass.Unknown" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_skill_cast", kind = "Sfx", assetName = "SFX/skill_cast.wav", volume = 1f, loop = false, minIntervalSec = 0.08f, usage = "Player or AI skill cast.", legacySource = "SkillCastEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_kill", kind = "Sfx", assetName = "SFX/kill.wav", volume = 1f, loop = false, minIntervalSec = 0.08f, usage = "Enemy killed.", legacySource = "TargetKilledEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_player_died", kind = "Sfx", assetName = "SFX/player_died.wav", volume = 1f, loop = false, minIntervalSec = 0f, usage = "Player death.", legacySource = "PlayerDiedEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_dodge", kind = "Sfx", assetName = "SFX/dodge.wav", volume = 0.8f, loop = false, minIntervalSec = 0.12f, usage = "Dodge input feedback.", legacySource = "DodgePressedEvent" },
            new TotemAudioCueCatalogEntry { cueId = "sfx_ui_click", kind = "Sfx", assetName = "ui/ui_click.wav", volume = 0.8f, loop = false, minIntervalSec = 0.05f, usage = "Shared UI click feedback.", legacySource = "GF UIFormBase" },
        };
    }

    private static TotemNpcCatalogEntry[] BuildDefaultNpcs()
    {
        return new[]
        {
            new TotemNpcCatalogEntry { configId = 1, npcId = "tattooist_default", type = "Tattooist", mapTheme = "All", roomType = "TattooStudio", offsetX = -2f, offsetY = 0f, offsetZ = 0f, interactRadius = 2.5f, themePriceMultiplier = 1f, shopStockTable = string.Empty, guardRadius = 8f, serviceCooldown = 30f, guardSpawnId = "guard_basic", guardCount1 = 1, guardCount2 = 2, offers = Array.Empty<TotemShopOfferCatalogEntry>() },
            new TotemNpcCatalogEntry { configId = 2, npcId = "tattooist_advanced", type = "Tattooist", mapTheme = "Lab", roomType = "TattooStudio", offsetX = 2f, offsetY = 0f, offsetZ = 0f, interactRadius = 2.5f, themePriceMultiplier = 1.1f, shopStockTable = string.Empty, guardRadius = 8f, serviceCooldown = 30f, guardSpawnId = "guard_lab", guardCount1 = 2, guardCount2 = 3, offers = Array.Empty<TotemShopOfferCatalogEntry>() },
            new TotemNpcCatalogEntry { configId = 3, npcId = "tattooist_alien", type = "Tattooist", mapTheme = "Alien", roomType = "TattooStudio", offsetX = 0f, offsetY = 0f, offsetZ = 2f, interactRadius = 2.5f, themePriceMultiplier = 1.2f, shopStockTable = string.Empty, guardRadius = 8f, serviceCooldown = 30f, guardSpawnId = "guard_alien", guardCount1 = 2, guardCount2 = 4, offers = Array.Empty<TotemShopOfferCatalogEntry>() },
            new TotemNpcCatalogEntry
            {
                configId = 4,
                npcId = "merchant_general",
                type = "Merchant",
                mapTheme = "All",
                roomType = "Merchant",
                offsetX = -2f,
                offsetY = 0f,
                offsetZ = 0f,
                interactRadius = 2.5f,
                themePriceMultiplier = 1f,
                shopStockTable = "general_shop",
                guardRadius = 6f,
                serviceCooldown = 20f,
                guardSpawnId = "guard_basic",
                guardCount1 = 1,
                guardCount2 = 2,
                offers = new[]
                {
                    new TotemShopOfferCatalogEntry { itemId = 101, displayName = "Red Ink", price = 30, stock = 3, weight = 40, rewardType = "Ink", rewardId = "red", rewardAmount = 1, rewardSlot = -1 },
                    new TotemShopOfferCatalogEntry { itemId = 201, displayName = "Knife Upgrade", price = 50, stock = 1, weight = 30, rewardType = "WeaponUpgrade", rewardId = "knife_basic", rewardAmount = 1, rewardSlot = -1 },
                    new TotemShopOfferCatalogEntry { itemId = 301, displayName = "Skill Core", price = 80, stock = 1, weight = 20, rewardType = "SkillCore", rewardId = "skill_fireball_01", rewardAmount = 1, rewardSlot = 0 },
                },
            },
            new TotemNpcCatalogEntry
            {
                configId = 5,
                npcId = "merchant_alien",
                type = "Merchant",
                mapTheme = "Alien",
                roomType = "Merchant",
                offsetX = 2f,
                offsetY = 0f,
                offsetZ = 0f,
                interactRadius = 2.5f,
                themePriceMultiplier = 1.2f,
                shopStockTable = "alien_shop",
                guardRadius = 6f,
                serviceCooldown = 20f,
                guardSpawnId = "guard_alien",
                guardCount1 = 2,
                guardCount2 = 3,
                offers = new[]
                {
                    new TotemShopOfferCatalogEntry { itemId = 102, displayName = "Pure Ink", price = 90, stock = 2, weight = 20, rewardType = "Ink", rewardId = "pure", rewardAmount = 2, rewardSlot = -1 },
                    new TotemShopOfferCatalogEntry { itemId = 202, displayName = "Bow Upgrade", price = 120, stock = 1, weight = 20, rewardType = "WeaponUpgrade", rewardId = "bow_charge", rewardAmount = 1, rewardSlot = -1 },
                    new TotemShopOfferCatalogEntry { itemId = 302, displayName = "Boss Charm", price = 160, stock = 1, weight = 10, rewardType = "SkillCore", rewardId = "skill_stealth_01", rewardAmount = 1, rewardSlot = 1 },
                },
            },
        };
    }

    private static TotemShopStockCatalogEntry[] BuildDefaultShopStocks()
    {
        return new[]
        {
            new TotemShopStockCatalogEntry { id = 1, tableId = "general_shop", itemId = 101, category = "Ink", weight = 3.0f, minCount = 1, maxCount = 3, basePrice = 70, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 2, tableId = "general_shop", itemId = 102, category = "Ink", weight = 3.0f, minCount = 1, maxCount = 3, basePrice = 70, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 3, tableId = "general_shop", itemId = 103, category = "Ink", weight = 3.0f, minCount = 1, maxCount = 3, basePrice = 70, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 4, tableId = "general_shop", itemId = 104, category = "Ink", weight = 3.0f, minCount = 1, maxCount = 3, basePrice = 70, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 5, tableId = "general_shop", itemId = 201, category = "Weapon", weight = 2.0f, minCount = 1, maxCount = 1, basePrice = 260, sellRatio = 0.4f },
            new TotemShopStockCatalogEntry { id = 6, tableId = "general_shop", itemId = 202, category = "Weapon", weight = 2.0f, minCount = 1, maxCount = 1, basePrice = 260, sellRatio = 0.4f },
            new TotemShopStockCatalogEntry { id = 7, tableId = "general_shop", itemId = 301, category = "Skill", weight = 1.5f, minCount = 1, maxCount = 1, basePrice = 180, sellRatio = 0.4f },
            new TotemShopStockCatalogEntry { id = 8, tableId = "general_shop", itemId = 302, category = "Skill", weight = 1.5f, minCount = 1, maxCount = 1, basePrice = 180, sellRatio = 0.4f },
            new TotemShopStockCatalogEntry { id = 9, tableId = "general_shop", itemId = 401, category = "Antidote", weight = 2.0f, minCount = 1, maxCount = 2, basePrice = 90, sellRatio = 0.3f },
            new TotemShopStockCatalogEntry { id = 10, tableId = "general_shop", itemId = 402, category = "Remover", weight = 1.5f, minCount = 1, maxCount = 2, basePrice = 140, sellRatio = 0.3f },
            new TotemShopStockCatalogEntry { id = 11, tableId = "alien_shop", itemId = 501, category = "RareInk", weight = 5.0f, minCount = 1, maxCount = 2, basePrice = 360, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 12, tableId = "alien_shop", itemId = 502, category = "RareInk", weight = 3.0f, minCount = 1, maxCount = 1, basePrice = 520, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 13, tableId = "alien_shop", itemId = 503, category = "RareInk", weight = 1.0f, minCount = 1, maxCount = 1, basePrice = 720, sellRatio = 0.5f },
            new TotemShopStockCatalogEntry { id = 14, tableId = "alien_shop", itemId = 201, category = "Weapon", weight = 1.5f, minCount = 1, maxCount = 1, basePrice = 310, sellRatio = 0.4f },
            new TotemShopStockCatalogEntry { id = 15, tableId = "alien_shop", itemId = 301, category = "Skill", weight = 1.5f, minCount = 1, maxCount = 1, basePrice = 220, sellRatio = 0.4f },
        };
    }

    private static TotemGameplayEventCatalogEntry[] BuildDefaultEvents()
    {
        return new[]
        {
            new TotemGameplayEventCatalogEntry { eventId = "event_choice_001", eventType = "choice_event", displayName = "event.choice.mysterious_altar", triggerCondition = string.Empty, baseRewardCoin = 0, rewardPoolId = string.Empty, timeoutSec = 20f, curseDebuffId = string.Empty, weightBase = 40, isRepeatAllowed = true },
            new TotemGameplayEventCatalogEntry { eventId = "event_choice_002", eventType = "choice_event", displayName = "event.choice.old_forge", triggerCondition = "{\"minElapsedSec\":120}", baseRewardCoin = 0, rewardPoolId = string.Empty, timeoutSec = 20f, curseDebuffId = string.Empty, weightBase = 30, isRepeatAllowed = true },
            new TotemGameplayEventCatalogEntry { eventId = "event_combat_001", eventType = "combat_event", displayName = "event.combat.ambush", triggerCondition = string.Empty, baseRewardCoin = 50, rewardPoolId = "pool_combat_basic", timeoutSec = -1f, curseDebuffId = string.Empty, weightBase = 35, isRepeatAllowed = true },
            new TotemGameplayEventCatalogEntry { eventId = "event_merchant_001", eventType = "merchant_event", displayName = "event.merchant.wandering_trader", triggerCondition = string.Empty, baseRewardCoin = 0, rewardPoolId = string.Empty, timeoutSec = -1f, curseDebuffId = string.Empty, weightBase = 20, isRepeatAllowed = true },
            new TotemGameplayEventCatalogEntry { eventId = "event_curse_001", eventType = "curse_event", displayName = "event.curse.ink_corruption", triggerCondition = "{\"minElapsedSec\":180}", baseRewardCoin = 120, rewardPoolId = "pool_curse_reward", timeoutSec = 20f, curseDebuffId = "debuff_ink_slow", weightBase = 15, isRepeatAllowed = false },
            new TotemGameplayEventCatalogEntry { eventId = "event_lore_001", eventType = "lore_event", displayName = "event.lore.ancient_inscription", triggerCondition = string.Empty, baseRewardCoin = 20, rewardPoolId = string.Empty, timeoutSec = -1f, curseDebuffId = string.Empty, weightBase = 10, isRepeatAllowed = false },
        };
    }

    private static TotemChoiceCatalogEntry[] BuildDefaultChoiceOptions()
    {
        return new[]
        {
            new TotemChoiceCatalogEntry { optionId = "opt_tattoo_recipe_fire_001", optionType = "tattoo_recipe", displayName = "option.tattoo_recipe.fire_brand", descKey = "option.tattoo_recipe.fire_brand.desc", contentRef = "recipe_fire_001", skillSlot = -1, valueInt = 0, weightBase = 20, weightBuildBonus = "{\"fire\":15}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_pattern_recipe_straight_001", optionType = "pattern_recipe", displayName = "option.pattern_recipe.straight_line", descKey = "option.pattern_recipe.straight_line.desc", contentRef = "pattern_straight", skillSlot = -1, valueInt = 0, weightBase = 12, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = true },
            new TotemChoiceCatalogEntry { optionId = "opt_pattern_recipe_circle_001", optionType = "pattern_recipe", displayName = "option.pattern_recipe.circle", descKey = "option.pattern_recipe.circle.desc", contentRef = "pattern_circle", skillSlot = -1, valueInt = 0, weightBase = 10, weightBuildBonus = "{}", minRunElapsedSec = 60f, isUnique = true },
            new TotemChoiceCatalogEntry { optionId = "opt_weapon_upgrade_damage_001", optionType = "weapon_upgrade", displayName = "option.weapon_upgrade.sharpen", descKey = "option.weapon_upgrade.sharpen.desc", contentRef = "wupgrade_sharpen_001", skillSlot = -1, valueInt = 0, weightBase = 18, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_skill_upgrade_slot0_001", optionType = "skill_upgrade", displayName = "option.skill_upgrade.enhance_slot0", descKey = "option.skill_upgrade.enhance_slot0.desc", contentRef = "supgrade_power_001", skillSlot = 0, valueInt = 0, weightBase = 16, weightBuildBonus = "{}", minRunElapsedSec = 60f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_skill_upgrade_slot1_001", optionType = "skill_upgrade", displayName = "option.skill_upgrade.enhance_slot1", descKey = "option.skill_upgrade.enhance_slot1.desc", contentRef = "supgrade_power_002", skillSlot = 1, valueInt = 0, weightBase = 16, weightBuildBonus = "{}", minRunElapsedSec = 60f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_skill_acquire_dash_001", optionType = "skill_acquire", displayName = "option.skill_acquire.phase_dash", descKey = "option.skill_acquire.phase_dash.desc", contentRef = "skill_phase_dash", skillSlot = -1, valueInt = 0, weightBase = 16, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_skill_acquire_shield_001", optionType = "skill_acquire", displayName = "option.skill_acquire.ink_shield", descKey = "option.skill_acquire.ink_shield.desc", contentRef = "skill_ink_shield", skillSlot = -1, valueInt = 0, weightBase = 14, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_coin_bonus_small", optionType = "coin_bonus", displayName = "option.coin_bonus.small", descKey = "option.coin_bonus.small.desc", contentRef = string.Empty, skillSlot = -1, valueInt = 80, weightBase = 22, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_heal_moderate", optionType = "heal", displayName = "option.heal.moderate", descKey = "option.heal.moderate.desc", contentRef = string.Empty, skillSlot = -1, valueInt = 30, weightBase = 20, weightBuildBonus = "{}", minRunElapsedSec = 0f, isUnique = false },
            new TotemChoiceCatalogEntry { optionId = "opt_one_time_scroll_001", optionType = "one_time_scroll", displayName = "option.scroll.ink_burst", descKey = "option.scroll.ink_burst.desc", contentRef = "scroll_ink_burst", skillSlot = -1, valueInt = 0, weightBase = 12, weightBuildBonus = "{}", minRunElapsedSec = 120f, isUnique = false },
        };
    }

    private static TotemBotProfileCatalogEntry[] BuildDefaultBotProfiles()
    {
        return new[]
        {
            NewBotProfile(1, "Smart", "Smart Aggressive Fire", "Aggressive", 1, 20f, 18f, 210, 0.88f, 0.95f, 0.62f, 0.65f),
            NewBotProfile(2, "Smart", "Smart Aggressive Lightning", "Aggressive", 2, 20f, 18f, 220, 0.86f, 1.0f, 0.58f, 0.6f),
            NewBotProfile(3, "Smart", "Smart Aggressive Mutation", "Aggressive", 5, 20f, 18f, 240, 0.82f, 0.9f, 0.64f, 0.58f),
            NewBotProfile(4, "Smart", "Smart Aggressive Pure", "Aggressive", 7, 20f, 18f, 215, 0.87f, 0.85f, 0.6f, 0.62f),
            NewBotProfile(5, "Smart", "Smart Aggressive Flanker", "Aggressive", 1, 21f, 18f, 230, 0.84f, 1.05f, 0.55f, 0.7f),
            NewBotProfile(6, "Smart", "Smart Conservative Nature", "Conservative", 3, 22f, 15f, 320, 0.72f, 0.45f, 0.35f, 0.35f, attackCooldown: 0.85f),
            NewBotProfile(7, "Smart", "Smart Conservative Frost", "Conservative", 4, 22f, 14f, 300, 0.75f, 0.4f, 0.3f, 0.4f, attackCooldown: 0.85f),
            NewBotProfile(8, "Smart", "Smart Conservative Holy", "Conservative", 6, 24f, 16f, 310, 0.78f, 0.5f, 0.38f, 0.45f, attackCooldown: 0.9f),
            NewBotProfile(9, "Smart", "Smart Resource Lightning", "ResourceAcquisition", 2, 21f, 16f, 260, 0.78f, 1.65f, 0.48f, 0.75f, attackCooldown: 0.75f),
            NewBotProfile(10, "Smart", "Smart Resource Nature", "ResourceAcquisition", 3, 21f, 16f, 280, 0.74f, 1.75f, 0.44f, 0.8f, attackCooldown: 0.8f),
            NewBotProfile(11, "Smart", "Smart Resource Mutation", "ResourceAcquisition", 5, 22f, 17f, 270, 0.76f, 1.85f, 0.5f, 0.85f, attackCooldown: 0.75f),
            NewBotProfile(12, "Smart", "Smart Resource Holy", "ResourceAcquisition", 6, 22f, 17f, 265, 0.79f, 1.7f, 0.46f, 0.9f, attackCooldown: 0.8f),
            NewBotProfile(13, "Smart", "Smart Boss Fire", "BossPriority", 1, 24f, 18f, 250, 0.82f, 0.55f, 0.5f, 0.6f, attackCooldown: 0.75f),
            NewBotProfile(14, "Smart", "Smart Boss Frost", "BossPriority", 4, 24f, 18f, 255, 0.8f, 0.5f, 0.5f, 0.62f, attackCooldown: 0.8f),
            NewBotProfile(15, "Smart", "Smart Boss Pure", "BossPriority", 7, 25f, 19f, 245, 0.84f, 0.45f, 0.52f, 0.65f, attackCooldown: 0.75f),
            NewBotProfile(16, "Smart", "Smart Boss Mutation", "BossPriority", 5, 24f, 18f, 265, 0.78f, 0.6f, 0.48f, 0.58f, attackCooldown: 0.8f),
            NewBotProfile(17, "Smart", "Smart Player Lightning", "PlayerPriority", 2, 22f, 20f, 220, 0.86f, 0.7f, 0.55f, 0.65f),
            NewBotProfile(18, "Smart", "Smart Player Pure", "PlayerPriority", 7, 22f, 20f, 225, 0.88f, 0.65f, 0.58f, 0.62f),
            NewBotProfile(19, "Smart", "Smart Player Fire", "PlayerPriority", 1, 22f, 20f, 215, 0.87f, 0.68f, 0.56f, 0.66f),
            NewBotProfile(20, "Smart", "Smart Player Mutation", "PlayerPriority", 5, 23f, 20f, 235, 0.83f, 0.72f, 0.54f, 0.68f),
            NewBotProfile(101, "Light", "Light Scout A", "Hybrid", 1, 14f, 12f, 350, 0.45f, 0.3f, 0f, 0f, rethinkInterval: 45f, attackCooldown: 1.0f),
            NewBotProfile(102, "Light", "Light Scout B", "Hybrid", 2, 14f, 12f, 380, 0.4f, 0.3f, 0f, 0f, rethinkInterval: 45f, attackCooldown: 1.0f),
            NewBotProfile(103, "Light", "Light Scout C", "Hybrid", 3, 14f, 12f, 400, 0.4f, 0.3f, 0f, 0f, rethinkInterval: 45f, attackCooldown: 1.1f),
        };
    }

    private static TotemBotProfileCatalogEntry NewBotProfile(
        int botId,
        string type,
        string displayName,
        string personality,
        int preferredPreset,
        float visionRadius,
        float aggroRadius,
        int dodgeReactionMs,
        float confidence,
        float lootGreedFactor,
        float selfTattooBoldness,
        float enchantGreed,
        float rethinkInterval = 20f,
        float attackCooldown = 0.7f)
    {
        return new TotemBotProfileCatalogEntry
        {
            botId = botId,
            type = type,
            displayName = displayName,
            personality = personality,
            rethinkInterval = rethinkInterval,
            attackCooldown = attackCooldown,
            visionRadius = visionRadius,
            aggroRadius = aggroRadius,
            dodgeReactionMs = dodgeReactionMs,
            confidence = confidence,
            preferredPreset = preferredPreset,
            lootGreedFactor = lootGreedFactor,
            selfTattooBoldness = selfTattooBoldness,
            enchantGreed = enchantGreed,
        };
    }

    private static TotemBotBuildPresetCatalogEntry[] BuildDefaultBotBuildPresets()
    {
        return new[]
        {
            NewPreset(1, "Fire Burst Arm", new[] { 0.55f, 0.05f, 0.05f, 0.05f, 0.05f, 0.10f, 0.15f }, new[] { 4, 1, 2, 5, 6, 3 }, "Rush", 1, 2, 101, 102, NewSlot(4, 1, 2), NewSlot(1, 1, 6), NewSlot(2, 1, 1)),
            NewPreset(2, "Lightning Chain", new[] { 0.05f, 0.55f, 0.05f, 0.05f, 0.05f, 0.10f, 0.15f }, new[] { 3, 4, 1, 5, 6, 2 }, "Pivot", 3, 4, 103, 0, NewSlot(3, 2, 5), NewSlot(4, 2, 2)),
            NewPreset(3, "Nature Endure", new[] { 0.05f, 0.05f, 0.55f, 0.05f, 0.05f, 0.10f, 0.15f }, new[] { 6, 2, 4, 5, 1, 3 }, "Camp", 5, 6, 104, 0, NewSlot(6, 3, 7), NewSlot(2, 3, 3)),
            NewPreset(4, "Frost Guard", new[] { 0.05f, 0.05f, 0.05f, 0.55f, 0.05f, 0.10f, 0.15f }, new[] { 2, 5, 4, 6, 1, 3 }, "Camp", 7, 8, 105, 106, NewSlot(2, 4, 3), NewSlot(5, 4, 7)),
            NewPreset(5, "Mutation Brawl", new[] { 0.05f, 0.05f, 0.05f, 0.05f, 0.55f, 0.10f, 0.15f }, new[] { 1, 4, 3, 5, 6, 2 }, "Hybrid", 9, 10, 107, 0, NewSlot(1, 5, 6), NewSlot(4, 5, 5)),
            NewPreset(6, "Holy Support", new[] { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.55f, 0.20f }, new[] { 2, 3, 1, 6, 5, 4 }, "Camp", 11, 12, 108, 0, NewSlot(2, 6, 4), NewSlot(3, 6, 1)),
            NewPreset(7, "Pure Control", new[] { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.20f, 0.55f }, new[] { 3, 1, 4, 5, 6, 2 }, "Pivot", 13, 14, 109, 0, NewSlot(3, 7, 4), NewSlot(1, 7, 1)),
        };
    }

    private static TotemBotBuildPresetCatalogEntry NewPreset(int presetId, string name, float[] tendency, int[] preferredParts, string behaviorMacro, int preferredSkillQ, int preferredSkillE, int affixA, int affixB, params TotemBotBuildSlot[] recommendedSeq)
    {
        int[] affixes = affixB > 0 ? new[] { affixA, affixB } : new[] { affixA };
        return new TotemBotBuildPresetCatalogEntry
        {
            presetId = presetId,
            name = name,
            tendency = tendency,
            preferredParts = preferredParts,
            recommendedSeq = recommendedSeq,
            earlyGameWeapon = 1,
            behaviorMacro = behaviorMacro,
            preferredSkillQ = preferredSkillQ,
            preferredSkillE = preferredSkillE,
            targetEnchantAffixes = affixes,
        };
    }

    private static TotemBotBuildSlot NewSlot(int partId, int colorId, int patternId)
    {
        return new TotemBotBuildSlot { partId = partId, colorId = colorId, patternId = patternId };
    }
}

[Serializable]
public sealed class TotemGameplayCatalogGenerationInfo
{
    public string generatedBy = string.Empty;
    public string sourceRoot = string.Empty;
    public int sourceFileCount;
    public string sourceContentHash = string.Empty;
}

[Serializable]
public sealed class TotemItemCatalogEntry
{
    public int itemId;
    public string itemType;
    public string subType;
    public int tier;
    public string displayName;
    public string rarity;
    public int maxStack;
    public int basePrice;
    public float sellRatio;

    public TotemItemDefinition ToDefinition()
    {
        return new TotemItemDefinition
        {
            ItemId = itemId,
            ItemType = TotemCatalogEnum.Parse(itemType, TotemItemType.Unknown),
            SubType = subType ?? string.Empty,
            Tier = Mathf.Max(0, tier),
            DisplayName = displayName ?? string.Empty,
            Rarity = rarity ?? string.Empty,
            MaxStack = Mathf.Max(1, maxStack),
            BasePrice = Mathf.Max(0, basePrice),
            SellRatio = Mathf.Clamp01(sellRatio),
        };
    }
}

[Serializable]
public sealed class TotemResourceCatalogEntry
{
    public int id;
    public string name;
    public string resourceType;
    public string loadPath;
    public string assetKey;
    public string activeAssetPath;

    public TotemResourceDefinition ToDefinition()
    {
        return new TotemResourceDefinition
        {
            Id = id,
            Name = name ?? string.Empty,
            ResourceType = resourceType ?? string.Empty,
            LoadPath = loadPath ?? string.Empty,
            AssetKey = assetKey ?? string.Empty,
            ActiveAssetPath = activeAssetPath ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemMerchantSlotCatalogEntry
{
    public int slotIndex;
    public string weaponId;
    public int goldCost;
    public int refreshWeight;

    public TotemMerchantSlotDefinition ToDefinition()
    {
        return new TotemMerchantSlotDefinition
        {
            SlotIndex = Mathf.Max(0, slotIndex),
            WeaponId = weaponId ?? string.Empty,
            GoldCost = Mathf.Max(0, goldCost),
            RefreshWeight = Mathf.Max(0, refreshWeight),
        };
    }

    public TotemShopOffer ToOffer()
    {
        return new TotemShopOffer
        {
            ItemId = 9000 + Mathf.Max(0, slotIndex),
            Category = "MerchantWeapon",
            DisplayName = $"merchant.weapon.{weaponId}",
            Price = Mathf.Max(0, goldCost),
            Stock = 1,
            Weight = Mathf.Max(0, refreshWeight),
            RewardType = TotemShopRewardType.WeaponUpgrade,
            RewardId = weaponId ?? string.Empty,
            RewardAmount = 1,
            RewardSlot = -1,
        };
    }

    public static TotemShopOffer[] CreateOffers(TotemMerchantSlotCatalogEntry[] rows, string merchantId)
    {
        if (rows == null || rows.Length <= 0)
        {
            return Array.Empty<TotemShopOffer>();
        }

        int maxSlot = -1;
        for (int i = 0; i < rows.Length; i++)
        {
            if (IsValid(rows[i]) && rows[i].slotIndex > maxSlot)
            {
                maxSlot = rows[i].slotIndex;
            }
        }

        if (maxSlot < 0)
        {
            return Array.Empty<TotemShopOffer>();
        }

        var offers = new List<TotemShopOffer>(maxSlot + 1);
        for (int slot = 0; slot <= maxSlot; slot++)
        {
            var selected = SelectWeighted(rows, slot, merchantId);
            if (selected != null)
            {
                offers.Add(selected.ToOffer());
            }
        }

        return offers.ToArray();
    }

    private static TotemMerchantSlotCatalogEntry SelectWeighted(TotemMerchantSlotCatalogEntry[] rows, int slotIndex, string merchantId)
    {
        int totalWeight = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (IsValid(row) && row.slotIndex == slotIndex)
            {
                totalWeight += Mathf.Max(0, row.refreshWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int cursor = PositiveHash($"{merchantId}:{slotIndex}") % totalWeight;
        int running = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (!IsValid(row) || row.slotIndex != slotIndex)
            {
                continue;
            }

            running += Mathf.Max(0, row.refreshWeight);
            if (cursor < running)
            {
                return row;
            }
        }

        return null;
    }

    private static bool IsValid(TotemMerchantSlotCatalogEntry row)
    {
        return row != null && row.slotIndex >= 0 && !string.IsNullOrWhiteSpace(row.weaponId) && row.goldCost > 0 && row.refreshWeight > 0;
    }

    private static int PositiveHash(string value)
    {
        unchecked
        {
            int hash = 23;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }
            }

            return hash == int.MinValue ? int.MaxValue : Mathf.Abs(hash);
        }
    }
}

[Serializable]
public sealed class TotemWeaponCatalogEntry
{
    public string weaponId;
    public string displayName;
    public string className;
    public float baseDamage;
    public float cooldown;
    public float range;
    public float attackSpeed;
    public float chargedMul = 1.5f;
    public string projectileId;
    public int rarity;
    public bool requiresCharge;
    public int maxAmmo;
    public int baseStartup;
    public int baseActive;
    public int baseRecovery;
    public float aimSpreadHalfDeg;
    public string normalTraitId;
    public string chargedTraitId;
    public string weaponPrefabPath;

    public TotemWeaponDefinition ToDefinition()
    {
        return new TotemWeaponDefinition
        {
            WeaponId = weaponId,
            DisplayName = displayName,
            Class = TotemCatalogEnum.Parse(className, TotemWeaponClass.Melee),
            BaseDamage = baseDamage,
            Cooldown = cooldown > 0f ? cooldown : ComputeCooldownFromFrames(baseStartup, baseActive, baseRecovery),
            Range = range,
            AttackSpeedModifier = attackSpeed,
            ChargedMultiplier = chargedMul <= 0f ? 1.5f : chargedMul,
            ProjectileId = projectileId ?? string.Empty,
            Rarity = Mathf.Max(0, rarity),
            RequiresCharge = requiresCharge,
            MaxAmmo = maxAmmo,
            StartupFrames = Mathf.Max(0, baseStartup),
            ActiveFrames = Mathf.Max(0, baseActive),
            RecoveryFrames = Mathf.Max(0, baseRecovery),
            AimSpreadHalfDegrees = aimSpreadHalfDeg,
            NormalTraitId = normalTraitId ?? string.Empty,
            ChargedTraitId = chargedTraitId ?? string.Empty,
            WeaponPrefabPath = weaponPrefabPath ?? string.Empty,
        };
    }

    public static float ComputeCooldownFromFrames(int startupFrames, int activeFrames, int recoveryFrames)
    {
        int totalFrames = Mathf.Max(0, startupFrames) + Mathf.Max(0, activeFrames) + Mathf.Max(0, recoveryFrames);
        return totalFrames <= 0 ? 0.35f : totalFrames / 60f;
    }
}

[Serializable]
public sealed class TotemProjectileCatalogEntry
{
    public string projectileId;
    public float speed;
    public float maxRange;
    public bool piercing;
    public float aoeRadius;
    public string visualPrefabPath;
    public int poolSize;

    public TotemProjectileDefinition ToDefinition()
    {
        return new TotemProjectileDefinition
        {
            ProjectileId = projectileId ?? string.Empty,
            Speed = Mathf.Max(0f, speed),
            MaxRange = Mathf.Max(0f, maxRange),
            Piercing = piercing,
            AoeRadius = Mathf.Max(0f, aoeRadius),
            VisualPrefabPath = visualPrefabPath ?? string.Empty,
            PoolSize = Mathf.Max(0, poolSize),
        };
    }
}

[Serializable]
public sealed class TotemWeaponTraitCatalogEntry
{
    public string traitId;
    public string displayName;
    public string description;
    public string effectType;
    public float effectParam1;
    public float effectParam2;

    public TotemWeaponTraitDefinition ToDefinition()
    {
        return new TotemWeaponTraitDefinition
        {
            TraitId = traitId ?? string.Empty,
            DisplayName = displayName ?? string.Empty,
            Description = description ?? string.Empty,
            EffectType = TotemCatalogEnum.Parse(effectType, TotemWeaponTraitEffectType.Unknown),
            EffectParam1 = effectParam1,
            EffectParam2 = effectParam2,
        };
    }
}

[Serializable]
public sealed class TotemWeaponDropCatalogEntry
{
    public string dropId;
    public string weaponId;
    public string dropSource;
    public int weight;
    public int minRoomIndex;
    public int maxRoomIndex;

    public TotemWeaponDropDefinition ToDefinition()
    {
        return new TotemWeaponDropDefinition
        {
            DropId = dropId,
            WeaponId = weaponId,
            DropSource = string.IsNullOrWhiteSpace(dropSource) ? "Elite" : dropSource,
            Weight = Mathf.Max(0, weight),
            MinRoomIndex = Mathf.Max(0, minRoomIndex),
            MaxRoomIndex = maxRoomIndex <= 0 ? 10 : maxRoomIndex,
        };
    }
}

[Serializable]
public sealed class TotemChestRewardCatalogEntry
{
    public string chestId;
    public string rewardType;
    public string rewardId;
    public int rewardAmount;
    public int probability;

    public TotemChestRewardDefinition ToDefinition()
    {
        return new TotemChestRewardDefinition
        {
            ChestId = string.IsNullOrWhiteSpace(chestId) ? "chest_common" : chestId,
            RewardType = TotemCatalogEnum.Parse(rewardType, TotemChestRewardType.Unknown),
            RewardId = rewardId ?? string.Empty,
            RewardAmount = Mathf.Max(0, rewardAmount),
            Probability = Mathf.Max(0, probability),
        };
    }
}

[Serializable]
public sealed class TotemMapTemplateCatalogEntry
{
    public int id;
    public string themeName;
    public float mapSize;
    public float minRoomSize;
    public int terrainPoolId;
    public string prefabPath;
    public string hudAccentColor;
    public string dominantColor;

    public TotemMapTemplateDefinition ToDefinition()
    {
        return new TotemMapTemplateDefinition
        {
            Id = id,
            ThemeName = themeName ?? string.Empty,
            MapSize = mapSize > 0f ? mapSize : TotemMapService.DefaultMapSize,
            MinRoomSize = minRoomSize > 0f ? minRoomSize : 15f,
            TerrainPoolId = terrainPoolId,
            PrefabPath = prefabPath ?? string.Empty,
            HudAccentColor = hudAccentColor ?? string.Empty,
            DominantColor = dominantColor ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemTattooPartCatalogEntry
{
    public int id;
    public string name;
    public string triggerEvent;
    public string scaleStat;
    public string symmetryGroup;
    public float scaleFactor;
    public string passiveDimension;
}

[Serializable]
public sealed class TotemTattooColorCatalogEntry
{
    public int id;
    public string name;
    public string element;
    public float multiplier;
}

[Serializable]
public sealed class TotemTattooPatternCatalogEntry
{
    public int id;
    public string name;
    public string shape;
    public float multiplier;
}

[Serializable]
public sealed class TotemTattooElementCatalogEntry
{
    public int id;
    public string name;
    public float baseMultiplier;
    public float param1;
    public float param2;
    public float param3;

    public TotemTattooElementDefinition ToDefinition()
    {
        return new TotemTattooElementDefinition
        {
            Id = id,
            Name = name ?? string.Empty,
            Element = TotemCatalogEnum.Parse(name, TotemTattooElement.Fire),
            BaseMultiplier = baseMultiplier <= 0f ? 1f : baseMultiplier,
            Param1 = param1,
            Param2 = param2,
            Param3 = param3,
        };
    }
}

[Serializable]
public sealed class TotemTattooShapeCatalogEntry
{
    public int id;
    public string name;
    public float param1;
    public float param2;
    public float param3;

    public TotemTattooShapeDefinition ToDefinition()
    {
        return new TotemTattooShapeDefinition
        {
            Id = id,
            Name = name ?? string.Empty,
            Shape = TotemCatalogEnum.Parse(name, TotemTattooShape.SingleHit),
            Param1 = param1,
            Param2 = param2,
            Param3 = param3,
        };
    }
}

[Serializable]
public sealed class TotemTattooReadingTimeCatalogEntry
{
    public int partId;
    public string partName;
    public float durationSec;

    public TotemTattooReadingTimeDefinition ToDefinition()
    {
        return new TotemTattooReadingTimeDefinition
        {
            PartId = partId,
            PartName = partName ?? string.Empty,
            DurationSec = Mathf.Max(0f, durationSec),
        };
    }
}

[Serializable]
public sealed class TotemTattooEnchantAffixCatalogEntry
{
    public int id;
    public int partId;
    public string colorTier;
    public string affixType;
    public string statKey;
    public float value;
    public string conditionKey;
    public float conditionVal;
    public string displayText;
    public float weight;

    public TotemTattooEnchantAffixDefinition ToDefinition()
    {
        return new TotemTattooEnchantAffixDefinition
        {
            Id = id,
            PartId = Mathf.Clamp(partId, 0, TotemTattooService.PartCount),
            ColorTier = string.IsNullOrWhiteSpace(colorTier) ? "Common" : colorTier,
            AffixType = TotemCatalogEnum.Parse(affixType, TotemTattooEnchantAffixType.Unknown),
            StatKey = statKey ?? string.Empty,
            Value = value,
            ConditionKey = conditionKey ?? string.Empty,
            ConditionVal = conditionVal,
            DisplayText = displayText ?? string.Empty,
            Weight = Mathf.Max(0f, weight),
        };
    }
}

[Serializable]
public sealed class TotemTattooEnchantRecipeCatalogEntry
{
    public int id;
    public string colorTier;
    public int coinCost;
    public int rarePigmentCost;
    public int maxAffixPerSlot;

    public TotemTattooEnchantRecipeDefinition ToDefinition()
    {
        return new TotemTattooEnchantRecipeDefinition
        {
            Id = id,
            ColorTier = string.IsNullOrWhiteSpace(colorTier) ? "Common" : colorTier,
            CoinCost = Mathf.Max(0, coinCost),
            RarePigmentCost = Mathf.Max(0, rarePigmentCost),
            MaxAffixPerSlot = Mathf.Max(1, maxAffixPerSlot),
        };
    }
}

[Serializable]
public sealed class TotemSkillCatalogEntry
{
    public string skillId;
    public string displayName;
    public int chargeModel;
    public float cooldown;
    public int maxCharges;
    public float chargeRegenTime;
    public float holdDuration;
    public float overchargeWindow;
    public int startupFrames;
    public int activeFrames;
    public int recoveryFrames;
    public float startup;
    public float active;
    public float recovery;
    public float damage;
    public float damageMul;
    public string hitShape;
    public float radius;
    public float hitRadius;
    public string element;
    public bool cancelableByDodge;
    public int itemId;

    public TotemSkillDefinition ToDefinition()
    {
        float startupSec = startup > 0f ? startup : startupFrames / 60f;
        float activeSec = active > 0f ? active : activeFrames / 60f;
        float recoverySec = recovery > 0f ? recovery : recoveryFrames / 60f;
        float resolvedRadius = hitRadius > 0f ? hitRadius : radius;
        return new TotemSkillDefinition
        {
            SkillId = skillId,
            DisplayName = displayName,
            ChargeModel = (TotemSkillChargeModel)Mathf.Clamp(chargeModel, 0, 2),
            Cooldown = cooldown,
            MaxCharges = Mathf.Max(1, maxCharges),
            ChargeRegenTime = Mathf.Max(0f, chargeRegenTime),
            HoldDuration = Mathf.Max(0f, holdDuration),
            OverchargeWindow = Mathf.Max(0f, overchargeWindow),
            StartupFrames = Mathf.Max(0, startupFrames),
            ActiveFrames = Mathf.Max(0, activeFrames),
            RecoveryFrames = Mathf.Max(0, recoveryFrames),
            Startup = startupSec,
            Active = activeSec,
            Recovery = recoverySec,
            Damage = damage,
            DamageMultiplier = damageMul,
            HitShape = TotemCatalogEnum.Parse(hitShape, TotemSkillHitShape.Single),
            Radius = resolvedRadius,
            Element = ParseSkillElement(element),
            CancelableByDodge = cancelableByDodge,
            ItemId = itemId,
        };
    }

    private static TotemTattooElement ParseSkillElement(string value)
    {
        if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
        {
            return TotemTattooElement.Pure;
        }

        return TotemCatalogEnum.Parse(value, TotemTattooElement.Pure);
    }
}

[Serializable]
public sealed class TotemEnemyCatalogEntry
{
    public string enemyId;
    public string displayName;
    public string themeId;
    public string tier;
    public string runtimeAssetKey;
    public string fallbackRuntimeAssetKey;
    public string behaviorProfileId;
    public string abilityIds;
    public float baseHP;
    public float hpCurveK;
    public float baseDamage;
    public float damageCurveK;
    public float moveSpeed;
    public float attackRange;
    public float detectRange;
    public float leashRange;
    public string skillIds;
    public string lootTableId;
    public string guaranteedLootIds;
    public int spawnCost;
    public int elitePaintDropRare;
    public int xpReward;
    public string coinReward;
    public string poolIds;

    public TotemEnemyDefinition ToDefinition()
    {
        ParseCoinReward(coinReward, out int coinMin, out int coinMax);
        return new TotemEnemyDefinition
        {
            EnemyId = enemyId ?? string.Empty,
            DisplayName = displayName ?? string.Empty,
            ThemeId = themeId ?? string.Empty,
            Tier = TotemCatalogEnum.Parse(tier, TotemEnemyTier.Unknown),
            RuntimeAssetKey = runtimeAssetKey ?? string.Empty,
            FallbackRuntimeAssetKey = fallbackRuntimeAssetKey ?? string.Empty,
            BehaviorProfileId = behaviorProfileId ?? string.Empty,
            AbilityIds = abilityIds ?? string.Empty,
            BaseHP = Mathf.Max(1f, baseHP),
            HPCurveK = Mathf.Max(0f, hpCurveK),
            BaseDamage = Mathf.Max(0f, baseDamage),
            DamageCurveK = Mathf.Max(0f, damageCurveK),
            MoveSpeed = Mathf.Max(0f, moveSpeed),
            AttackRange = Mathf.Max(0f, attackRange),
            DetectRange = Mathf.Max(0f, detectRange),
            LeashRange = Mathf.Max(0f, leashRange),
            SkillIds = skillIds ?? string.Empty,
            LootTableId = lootTableId ?? string.Empty,
            GuaranteedLootIds = guaranteedLootIds ?? string.Empty,
            SpawnCost = Mathf.Max(1, spawnCost),
            ElitePaintDropRare = elitePaintDropRare != 0,
            XPReward = Mathf.Max(0, xpReward),
            CoinRewardMin = coinMin,
            CoinRewardMax = coinMax,
            PoolIds = poolIds ?? string.Empty,
        };
    }

    private static void ParseCoinReward(string value, out int coinMin, out int coinMax)
    {
        coinMin = 0;
        coinMax = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int dash = value.IndexOf('-');
        if (dash < 0)
        {
            if (int.TryParse(value, out int single))
            {
                coinMin = Mathf.Max(0, single);
                coinMax = coinMin;
            }

            return;
        }

        if (int.TryParse(value.Substring(0, dash), out int min))
        {
            coinMin = Mathf.Max(0, min);
        }

        if (int.TryParse(value.Substring(dash + 1), out int max))
        {
            coinMax = Mathf.Max(coinMin, max);
        }
    }
}

public sealed class TotemEnemyDefinition
{
    public string EnemyId;
    public string DisplayName;
    public string ThemeId;
    public TotemEnemyTier Tier;
    public string RuntimeAssetKey;
    public string FallbackRuntimeAssetKey;
    public string BehaviorProfileId;
    public string AbilityIds;
    public float BaseHP;
    public float HPCurveK;
    public float BaseDamage;
    public float DamageCurveK;
    public float MoveSpeed;
    public float AttackRange;
    public float DetectRange;
    public float LeashRange;
    public string SkillIds;
    public string LootTableId;
    public string GuaranteedLootIds;
    public int SpawnCost;
    public bool ElitePaintDropRare;
    public int XPReward;
    public int CoinRewardMin;
    public int CoinRewardMax;
    public string PoolIds;
}

public enum TotemEnemyAbilityType
{
    Unknown = 0,
    Melee,
    Projectile,
    Charge,
    Leap,
    Beam,
    ConeSweep,
    AreaPulse,
    HazardZone,
    Shield,
    Summon,
    Regenerate,
    DeathBurst,
    PhaseTransition,
}

[Serializable]
public sealed class TotemEnemyAbilityCatalogEntry
{
    public string abilityId;
    public string abilityType;
    public float range;
    public float radius;
    public float cooldown;
    public float windup;
    public float active;
    public float recovery;
    public float damageMultiplier;
    public string statusId;
    public float statusChance;
    public string summonEnemyId;
    public int summonCount;
    public string vfxId;
    public string audioCueId;
    public string parametersJson;

    public TotemEnemyAbilityDefinition ToDefinition()
    {
        return new TotemEnemyAbilityDefinition
        {
            AbilityId = abilityId ?? string.Empty,
            AbilityType = TotemCatalogEnum.Parse(abilityType, TotemEnemyAbilityType.Unknown),
            Range = Mathf.Max(0f, range),
            Radius = Mathf.Max(0f, radius),
            Cooldown = Mathf.Max(0f, cooldown),
            Windup = Mathf.Max(0f, windup),
            Active = Mathf.Max(0f, active),
            Recovery = Mathf.Max(0f, recovery),
            DamageMultiplier = Mathf.Max(0f, damageMultiplier),
            StatusId = statusId ?? string.Empty,
            StatusChance = Mathf.Clamp01(statusChance),
            SummonEnemyId = summonEnemyId ?? string.Empty,
            SummonCount = Mathf.Max(0, summonCount),
            VfxId = vfxId ?? string.Empty,
            AudioCueId = audioCueId ?? string.Empty,
            ParametersJson = parametersJson ?? "{}",
        };
    }
}

public sealed class TotemEnemyAbilityDefinition
{
    public string AbilityId;
    public TotemEnemyAbilityType AbilityType;
    public float Range;
    public float Radius;
    public float Cooldown;
    public float Windup;
    public float Active;
    public float Recovery;
    public float DamageMultiplier;
    public string StatusId;
    public float StatusChance;
    public string SummonEnemyId;
    public int SummonCount;
    public string VfxId;
    public string AudioCueId;
    public string ParametersJson;
}

[Serializable]
public sealed class TotemEncounterSpawnCatalogEntry
{
    public string encounterId;
    public string themeId;
    public string zoneRoles;
    public string enemyPoolIds;
    public float startTime;
    public float endTime;
    public int initialCount;
    public int activeCap;
    public int totalCap;
    public int waveMin;
    public int waveMax;
    public float waveInterval;
    public float minParticipantDistance;
    public float minSpacing;
    public int weight;
    public bool unique;

    public TotemEncounterSpawnDefinition ToDefinition()
    {
        return new TotemEncounterSpawnDefinition
        {
            EncounterId = encounterId ?? string.Empty,
            ThemeId = themeId ?? string.Empty,
            ZoneRoles = zoneRoles ?? string.Empty,
            EnemyPoolIds = enemyPoolIds ?? string.Empty,
            StartTime = Mathf.Max(0f, startTime),
            EndTime = Mathf.Max(0f, endTime),
            InitialCount = Mathf.Max(0, initialCount),
            ActiveCap = Mathf.Max(0, activeCap),
            TotalCap = Mathf.Max(0, totalCap),
            WaveMin = Mathf.Max(0, waveMin),
            WaveMax = Mathf.Max(0, waveMax),
            WaveInterval = Mathf.Max(0f, waveInterval),
            MinParticipantDistance = Mathf.Max(0f, minParticipantDistance),
            MinSpacing = Mathf.Max(0f, minSpacing),
            Weight = Mathf.Max(0, weight),
            Unique = unique,
        };
    }
}

public sealed class TotemEncounterSpawnDefinition
{
    public string EncounterId;
    public string ThemeId;
    public string ZoneRoles;
    public string EnemyPoolIds;
    public float StartTime;
    public float EndTime;
    public int InitialCount;
    public int ActiveCap;
    public int TotalCap;
    public int WaveMin;
    public int WaveMax;
    public float WaveInterval;
    public float MinParticipantDistance;
    public float MinSpacing;
    public int Weight;
    public bool Unique;
}

public enum TotemEnemyLootRewardType
{
    Unknown = 0,
    Coin,
    Item,
    Paint,
    Weapon,
    Equipment,
    Recipe,
}

[Serializable]
public sealed class TotemEnemyLootCatalogEntry
{
    public string lootEntryId;
    public string lootTableId;
    public string itemId;
    public string rewardType;
    public int minCount;
    public int maxCount;
    public int weight;
    public bool guaranteed;
    public string tierFilter;
    public string themeId;

    public TotemEnemyLootDefinition ToDefinition()
    {
        return new TotemEnemyLootDefinition
        {
            LootEntryId = lootEntryId ?? string.Empty,
            LootTableId = lootTableId ?? string.Empty,
            ItemId = itemId ?? string.Empty,
            RewardType = TotemCatalogEnum.Parse(rewardType, TotemEnemyLootRewardType.Unknown),
            MinCount = Mathf.Max(0, minCount),
            MaxCount = Mathf.Max(0, maxCount),
            Weight = Mathf.Max(0, weight),
            Guaranteed = guaranteed,
            TierFilter = TotemCatalogEnum.Parse(tierFilter, TotemEnemyTier.Unknown),
            ThemeId = themeId ?? string.Empty,
        };
    }
}

public sealed class TotemEnemyLootDefinition
{
    public string LootEntryId;
    public string LootTableId;
    public string ItemId;
    public TotemEnemyLootRewardType RewardType;
    public int MinCount;
    public int MaxCount;
    public int Weight;
    public bool Guaranteed;
    public TotemEnemyTier TierFilter;
    public string ThemeId;
}

[Serializable]
public sealed class TotemZonePhaseCatalogEntry
{
    public int id;
    public string phaseName;
    public float startTime;
    public float duration;
    public float targetRadius;
    public float outZoneDamage;
    public string centerOffsetMode;

    public TotemZonePhase ToPhase()
    {
        return new TotemZonePhase
        {
            Id = id,
            PhaseName = phaseName,
            StartTime = startTime,
            Duration = duration,
            TargetRadius = targetRadius,
            OutZoneDamage = outZoneDamage,
            CenterOffsetMode = centerOffsetMode,
        };
    }
}

[Serializable]
public sealed class TotemBossPhaseCatalogEntry
{
    public string bossId;
    public int phaseIndex;
    public float hpThreshold;
    public string abilityIds;
    public string skillIds;
    public float enrageMultiplier;
    public string phaseVFXId;
    public string phaseBGMCueId;
    public string deathPatternRecipeId;

    public TotemBossPhase ToPhase()
    {
        return new TotemBossPhase
        {
            BossId = bossId ?? string.Empty,
            PhaseIndex = phaseIndex,
            HPThreshold = hpThreshold,
            SkillIds = skillIds ?? string.Empty,
            EnrageMultiplier = enrageMultiplier,
            PhaseVFXId = phaseVFXId ?? string.Empty,
            PhaseBGMCueId = phaseBGMCueId ?? string.Empty,
            DeathPatternRecipeId = deathPatternRecipeId ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemAudioCueCatalogEntry
{
    public string cueId;
    public string kind;
    public string assetName;
    public float volume = 1f;
    public bool loop;
    public float minIntervalSec;
    public string usage;
    public string legacySource;

    public TotemAudioCueDefinition ToDefinition()
    {
        return new TotemAudioCueDefinition
        {
            CueId = cueId ?? string.Empty,
            Kind = TotemCatalogEnum.Parse(kind, TotemAudioCueKind.Unknown),
            AssetName = assetName ?? string.Empty,
            Volume = volume <= 0f ? 1f : Mathf.Clamp01(volume),
            Loop = loop,
            MinIntervalSec = Mathf.Max(0f, minIntervalSec),
            Usage = usage ?? string.Empty,
            LegacySource = legacySource ?? string.Empty,
        };
    }
}

[Serializable]
public sealed class TotemNpcCatalogEntry
{
    public int configId;
    public string npcId;
    public string type;
    public string mapTheme;
    public string roomType;
    public string shopStockTable;
    public float offsetX;
    public float offsetY;
    public float offsetZ;
    public float interactRadius;
    public float themePriceMultiplier;
    public float guardRadius;
    public float serviceCooldown;
    public string guardSpawnId;
    public int guardCount1;
    public int guardCount2;
    public TotemShopOfferCatalogEntry[] offers = Array.Empty<TotemShopOfferCatalogEntry>();

    public TotemNpcModel ToModel(TotemMapSnapshot map)
    {
        return ToModel(map, Array.Empty<TotemShopStockCatalogEntry>(), Array.Empty<TotemMerchantSlotCatalogEntry>());
    }

    public TotemNpcModel ToModel(TotemMapSnapshot map, TotemShopStockCatalogEntry[] shopStocks, TotemMerchantSlotCatalogEntry[] merchantSlots)
    {
        if (offers == null)
        {
            offers = Array.Empty<TotemShopOfferCatalogEntry>();
        }

        var npcType = TotemCatalogEnum.Parse(type, TotemNpcType.Tattooist);
        return new TotemNpcModel
        {
            ConfigId = configId,
            NpcId = npcId,
            Type = npcType,
            MapTheme = mapTheme ?? string.Empty,
            ShopStockTable = shopStockTable ?? string.Empty,
            Position = ResolveNpcAnchorPosition(map, roomType, npcType) + new Vector3(offsetX, offsetY, offsetZ),
            InteractRadius = interactRadius > 0f ? interactRadius : 3f,
            ThemePriceMultiplier = themePriceMultiplier > 0f ? themePriceMultiplier : 1f,
            GuardRadius = guardRadius,
            ServiceCooldown = serviceCooldown,
            GuardSpawnId = guardSpawnId ?? string.Empty,
            GuardCount1 = guardCount1,
            GuardCount2 = guardCount2,
            Offers = CreateOffers(shopStocks, merchantSlots),
        };
    }

    private TotemShopOffer[] CreateOffers(TotemShopStockCatalogEntry[] shopStocks, TotemMerchantSlotCatalogEntry[] merchantSlots)
    {
        TotemShopOffer[] baseOffers;
        if (!string.IsNullOrWhiteSpace(shopStockTable))
        {
            var stockOffers = TotemShopStockCatalogEntry.CreateOffers(shopStocks, shopStockTable);
            if (stockOffers.Length > 0)
            {
                baseOffers = stockOffers;
                return AppendMerchantSlotOffers(baseOffers, merchantSlots);
            }
        }

        baseOffers = new TotemShopOffer[offers.Length];
        for (int i = 0; i < offers.Length; i++)
        {
            baseOffers[i] = offers[i].ToOffer();
        }

        return AppendMerchantSlotOffers(baseOffers, merchantSlots);
    }

    private TotemShopOffer[] AppendMerchantSlotOffers(TotemShopOffer[] baseOffers, TotemMerchantSlotCatalogEntry[] merchantSlots)
    {
        if (!string.Equals(type, "Merchant", StringComparison.OrdinalIgnoreCase))
        {
            return baseOffers ?? Array.Empty<TotemShopOffer>();
        }

        var slotOffers = TotemMerchantSlotCatalogEntry.CreateOffers(merchantSlots, npcId ?? string.Empty);
        if (slotOffers.Length <= 0)
        {
            return baseOffers ?? Array.Empty<TotemShopOffer>();
        }

        int baseCount = baseOffers?.Length ?? 0;
        var result = new TotemShopOffer[baseCount + slotOffers.Length];
        for (int i = 0; i < baseCount; i++)
        {
            result[i] = baseOffers[i];
        }

        for (int i = 0; i < slotOffers.Length; i++)
        {
            result[baseCount + i] = slotOffers[i];
        }

        return result;
    }

    private static Vector3 ResolveNpcAnchorPosition(TotemMapSnapshot map, string roomTypeName, TotemNpcType npcType)
    {
        var fallback = ResolveRoomCenter(map, roomTypeName);
        if (npcType == TotemNpcType.Merchant)
        {
            return TotemMapService.ResolveAnchorPosition(map, TotemMapAnchorKind.Merchant, fallback);
        }

        return TotemMapService.ResolveAnchorPosition(map, TotemMapAnchorKind.Tattooist, fallback);
    }

    private static Vector3 ResolveRoomCenter(TotemMapSnapshot map, string roomTypeName)
    {
        var roomType = TotemCatalogEnum.Parse(roomTypeName, TotemRoomType.SpawnRoom);
        var rooms = map?.Rooms;
        if (rooms != null)
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].RoomType == roomType)
                {
                    return rooms[i].CenterWorld;
                }
            }
        }

        switch (roomType)
        {
            case TotemRoomType.TattooStudio:
                return new Vector3(37.5f, 0f, 112.5f);
            case TotemRoomType.Merchant:
                return new Vector3(112.5f, 0f, 112.5f);
            case TotemRoomType.BossRoom:
                return new Vector3(112.5f, 0f, 37.5f);
            default:
                return Vector3.zero;
        }
    }
}

[Serializable]
public sealed class TotemShopStockCatalogEntry
{
    public int id;
    public string tableId;
    public int itemId;
    public string category;
    public float weight;
    public int minCount;
    public int maxCount;
    public int basePrice;
    public float sellRatio;

    public TotemShopOffer ToOffer()
    {
        return new TotemShopOffer
        {
            ItemId = itemId,
            Category = category ?? string.Empty,
            DisplayName = BuildDisplayName(category, itemId),
            Price = Mathf.Max(0, basePrice),
            Stock = Mathf.Max(1, maxCount > 0 ? maxCount : minCount),
            Weight = Mathf.Max(0, Mathf.RoundToInt(weight)),
            RewardType = ResolveRewardType(category),
            RewardId = ResolveRewardId(category, itemId),
            RewardAmount = ResolveRewardAmount(category, itemId),
            RewardSlot = ResolveRewardSlot(category, itemId),
        };
    }

    public static TotemShopOffer[] CreateOffers(TotemShopStockCatalogEntry[] rows, string tableId)
    {
        if (rows == null || string.IsNullOrWhiteSpace(tableId))
        {
            return Array.Empty<TotemShopOffer>();
        }

        int count = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (IsInTable(rows[i], tableId))
            {
                count++;
            }
        }

        if (count <= 0)
        {
            return Array.Empty<TotemShopOffer>();
        }

        var result = new TotemShopOffer[count];
        int cursor = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (IsInTable(rows[i], tableId))
            {
                result[cursor++] = rows[i].ToOffer();
            }
        }

        return result;
    }

    private static bool IsInTable(TotemShopStockCatalogEntry row, string tableId)
    {
        return row != null && string.Equals(row.tableId, tableId, StringComparison.Ordinal);
    }

    private static string BuildDisplayName(string category, int itemId)
    {
        switch (itemId)
        {
            case 101: return "Red Ink";
            case 102: return "Yellow Ink";
            case 103: return "Green Ink";
            case 104: return "Blue Ink";
            case 201: return "Knife Upgrade";
            case 202: return "Bow Upgrade";
            case 301: return "Fireball Core";
            case 302: return "Stealth Core";
            case 401: return "Antidote";
            case 402: return "Remover";
            case 501: return "Rare Ink I";
            case 502: return "Rare Ink II";
            case 503: return "Rare Ink III";
            default: return $"{category} {itemId}";
        }
    }

    private static TotemShopRewardType ResolveRewardType(string category)
    {
        if (string.Equals(category, "Ink", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, "RareInk", StringComparison.OrdinalIgnoreCase))
        {
            return TotemShopRewardType.Ink;
        }

        if (string.Equals(category, "Weapon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, "MerchantWeapon", StringComparison.OrdinalIgnoreCase))
        {
            return TotemShopRewardType.WeaponUpgrade;
        }

        if (string.Equals(category, "Skill", StringComparison.OrdinalIgnoreCase))
        {
            return TotemShopRewardType.SkillCore;
        }

        if (string.Equals(category, "Antidote", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(category, "Remover", StringComparison.OrdinalIgnoreCase))
        {
            return TotemShopRewardType.StatusCleanse;
        }

        return TotemShopRewardType.Unknown;
    }

    private static string ResolveRewardId(string category, int itemId)
    {
        if (string.Equals(category, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            return itemId == 202 ? "bow_charge" : "knife_basic";
        }

        if (string.Equals(category, "Skill", StringComparison.OrdinalIgnoreCase))
        {
            return itemId == 302 ? "skill_stealth_01" : "skill_fireball_01";
        }

        if (string.Equals(category, "RareInk", StringComparison.OrdinalIgnoreCase))
        {
            return $"rare_ink_{itemId}";
        }

        if (string.Equals(category, "Ink", StringComparison.OrdinalIgnoreCase))
        {
            return $"ink_{itemId}";
        }

        return string.Empty;
    }

    private static int ResolveRewardAmount(string category, int itemId)
    {
        if (string.Equals(category, "RareInk", StringComparison.OrdinalIgnoreCase))
        {
            return itemId == 501 ? 2 : 1;
        }

        return 1;
    }

    private static int ResolveRewardSlot(string category, int itemId)
    {
        return string.Equals(category, "Skill", StringComparison.OrdinalIgnoreCase) && itemId == 302 ? 1 : -1;
    }
}

[Serializable]
public sealed class TotemShopOfferCatalogEntry
{
    public int itemId;
    public string category;
    public string displayName;
    public int price;
    public int stock;
    public int weight;
    public string rewardType;
    public string rewardId;
    public int rewardAmount;
    public int rewardSlot = -1;

    public TotemShopOffer ToOffer()
    {
        return new TotemShopOffer
        {
            ItemId = itemId,
            Category = category ?? string.Empty,
            DisplayName = displayName,
            Price = price,
            Stock = stock,
            Weight = weight,
            RewardType = TotemCatalogEnum.Parse(rewardType, TotemShopRewardType.Unknown),
            RewardId = rewardId,
            RewardAmount = rewardAmount,
            RewardSlot = rewardSlot,
        };
    }
}

[Serializable]
public sealed class TotemGameplayEventCatalogEntry
{
    public string eventId;
    public string eventType;
    public string displayName;
    public string triggerCondition;
    public int baseRewardCoin;
    public string rewardPoolId;
    public float timeoutSec;
    public string curseDebuffId;
    public int weightBase;
    public bool isRepeatAllowed;

    public TotemGameplayEventDefinition ToDefinition()
    {
        return new TotemGameplayEventDefinition
        {
            EventId = eventId ?? string.Empty,
            EventType = ParseEventType(eventType),
            DisplayName = displayName ?? string.Empty,
            TriggerCondition = triggerCondition ?? string.Empty,
            BaseRewardCoin = Mathf.Max(0, baseRewardCoin),
            RewardPoolId = rewardPoolId ?? string.Empty,
            TimeoutSec = timeoutSec,
            CurseDebuffId = curseDebuffId ?? string.Empty,
            WeightBase = Mathf.Max(0, weightBase),
            IsRepeatAllowed = isRepeatAllowed,
        };
    }

    private static TotemGameplayEventType ParseEventType(string value)
    {
        if (string.Equals(value, "combat_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Combat;
        }

        if (string.Equals(value, "choice_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Choice;
        }

        if (string.Equals(value, "puzzle_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Puzzle;
        }

        if (string.Equals(value, "merchant_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Merchant;
        }

        if (string.Equals(value, "boss_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Boss;
        }

        if (string.Equals(value, "lore_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Lore;
        }

        if (string.Equals(value, "curse_event", StringComparison.OrdinalIgnoreCase))
        {
            return TotemGameplayEventType.Curse;
        }

        return TotemCatalogEnum.Parse(value, TotemGameplayEventType.Unknown);
    }
}

[Serializable]
public sealed class TotemChoiceCatalogEntry
{
    public string optionId;
    public string optionType;
    public string displayName;
    public string descKey;
    public string contentRef;
    public int skillSlot;
    public int valueInt;
    public int weightBase;
    public string weightBuildBonus;
    public float minRunElapsedSec;
    public bool isUnique;
    public string effectType;
    public float magnitude;

    public TotemChoiceOption ToOption()
    {
        var parsedOptionType = ParseOptionType(optionType);
        var resolvedEffectType = string.IsNullOrWhiteSpace(effectType)
            ? InferEffectType(parsedOptionType)
            : TotemCatalogEnum.Parse(effectType, TotemChoiceEffectType.CoinReward);
        float resolvedMagnitude = magnitude > 0f ? magnitude : valueInt;
        return new TotemChoiceOption
        {
            OptionId = optionId ?? string.Empty,
            OptionType = parsedOptionType,
            DisplayName = displayName ?? string.Empty,
            DescKey = descKey ?? string.Empty,
            ContentRef = contentRef ?? string.Empty,
            SkillSlot = skillSlot,
            ValueInt = valueInt,
            WeightBase = Mathf.Max(0, weightBase),
            WeightBuildBonus = weightBuildBonus ?? string.Empty,
            MinRunElapsedSec = Mathf.Max(0f, minRunElapsedSec),
            IsUnique = isUnique,
            EffectType = resolvedEffectType,
            Magnitude = resolvedMagnitude,
        };
    }

    private static TotemChoiceOptionType ParseOptionType(string value)
    {
        if (string.Equals(value, "tattoo_recipe", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.TattooRecipe;
        if (string.Equals(value, "pattern_recipe", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.PatternRecipe;
        if (string.Equals(value, "weapon_upgrade", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.WeaponUpgrade;
        if (string.Equals(value, "skill_upgrade", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.SkillUpgrade;
        if (string.Equals(value, "skill_acquire", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.SkillAcquire;
        if (string.Equals(value, "coin_bonus", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.CoinBonus;
        if (string.Equals(value, "heal", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.Heal;
        if (string.Equals(value, "one_time_scroll", StringComparison.OrdinalIgnoreCase)) return TotemChoiceOptionType.OneTimeScroll;
        return TotemCatalogEnum.Parse(value, TotemChoiceOptionType.Unknown);
    }

    private static TotemChoiceEffectType InferEffectType(TotemChoiceOptionType optionType)
    {
        switch (optionType)
        {
            case TotemChoiceOptionType.WeaponUpgrade:
                return TotemChoiceEffectType.WeaponUpgrade;
            case TotemChoiceOptionType.SkillUpgrade:
                return TotemChoiceEffectType.SkillRefresh;
            case TotemChoiceOptionType.SkillAcquire:
                return TotemChoiceEffectType.SkillAcquire;
            case TotemChoiceOptionType.CoinBonus:
                return TotemChoiceEffectType.CoinReward;
            case TotemChoiceOptionType.Heal:
                return TotemChoiceEffectType.Heal;
            case TotemChoiceOptionType.TattooRecipe:
            case TotemChoiceOptionType.PatternRecipe:
            case TotemChoiceOptionType.OneTimeScroll:
                return TotemChoiceEffectType.RecipeUnlock;
            default:
                return TotemChoiceEffectType.CoinReward;
        }
    }
}

[Serializable]
public sealed class TotemBotProfileCatalogEntry
{
    public int botId;
    public string type;
    public string displayName;
    public float rethinkInterval;
    public float attackCooldown;
    public float visionRadius;
    public float aggroRadius;
    public int dodgeReactionMs;
    public float confidence;
    public int preferredPreset;
    public float lootGreedFactor;
    public float selfTattooBoldness;
    public float enchantGreed;
    public string personality;
    public float targetPlayerWeight;
    public float targetHumanoidAiWeight;
    public float targetBossWeight;
    public float targetResourceWeight;
    public float readingTargetWeight;
    public float shopPreference;
    public float riskTolerance;

    public TotemBotProfileDefinition ToDefinition()
    {
        var resolvedPersonality = TotemCatalogEnum.Parse(personality, TotemAIPersonality.Hybrid);
        return new TotemBotProfileDefinition
        {
            BotId = botId,
            ActorKind = ParseBotKind(type),
            DisplayName = displayName,
            RethinkInterval = rethinkInterval,
            AttackCooldown = attackCooldown,
            VisionRadius = visionRadius,
            AggroRadius = aggroRadius,
            DodgeReactionSec = Mathf.Max(0f, dodgeReactionMs * 0.001f),
            Confidence = Mathf.Clamp01(confidence),
            PreferredPreset = preferredPreset,
            LootGreedFactor = lootGreedFactor,
            SelfTattooBoldness = Mathf.Clamp01(selfTattooBoldness),
            EnchantGreed = Mathf.Clamp01(enchantGreed),
            Personality = resolvedPersonality,
            TargetPlayerWeight = ResolveWeight(targetPlayerWeight, DefaultTargetPlayerWeight(resolvedPersonality)),
            TargetHumanoidAiWeight = ResolveWeight(targetHumanoidAiWeight, DefaultTargetHumanoidAiWeight(resolvedPersonality)),
            TargetBossWeight = ResolveWeight(targetBossWeight, DefaultTargetBossWeight(resolvedPersonality)),
            TargetResourceWeight = ResolveWeight(targetResourceWeight, DefaultTargetResourceWeight(resolvedPersonality)),
            ReadingTargetWeight = ResolveWeight(readingTargetWeight, DefaultReadingTargetWeight(resolvedPersonality)),
            ShopPreference = ResolveWeight(shopPreference, DefaultShopPreference(resolvedPersonality)),
            RiskTolerance = ResolveWeight(riskTolerance, DefaultRiskTolerance(resolvedPersonality)),
        };
    }

    private static float ResolveWeight(float value, float fallback)
    {
        return value > 0f ? value : fallback;
    }

    private static float DefaultTargetPlayerWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.PlayerPriority:
                return 1.1f;
            case TotemAIPersonality.Conservative:
                return 0.45f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.35f;
            case TotemAIPersonality.BossPriority:
                return 0.25f;
            default:
                return 0.9f;
        }
    }

    private static float DefaultTargetHumanoidAiWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.PlayerPriority:
                return 1.1f;
            case TotemAIPersonality.Conservative:
                return 0.35f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.25f;
            case TotemAIPersonality.BossPriority:
                return 0.25f;
            default:
                return 0.9f;
        }
    }

    private static float DefaultTargetBossWeight(TotemAIPersonality personality)
    {
        return personality == TotemAIPersonality.BossPriority ? 2.0f : 0.2f;
    }

    private static float DefaultTargetResourceWeight(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.ResourceAcquisition:
                return 1.6f;
            case TotemAIPersonality.BossPriority:
                return 0.45f;
            case TotemAIPersonality.Conservative:
                return 0.25f;
            default:
                return 0.4f;
        }
    }

    private static float DefaultReadingTargetWeight(TotemAIPersonality personality)
    {
        return personality == TotemAIPersonality.PlayerPriority ? 1.5f : 0.8f;
    }

    private static float DefaultShopPreference(TotemAIPersonality personality)
    {
        return personality == TotemAIPersonality.ResourceAcquisition ? 1.2f : 0.4f;
    }

    private static float DefaultRiskTolerance(TotemAIPersonality personality)
    {
        switch (personality)
        {
            case TotemAIPersonality.Aggressive:
            case TotemAIPersonality.PlayerPriority:
                return 0.85f;
            case TotemAIPersonality.Conservative:
                return 0.35f;
            case TotemAIPersonality.ResourceAcquisition:
                return 0.55f;
            case TotemAIPersonality.BossPriority:
                return 0.7f;
            default:
                return 0.6f;
        }
    }

    private static TotemActorKind ParseBotKind(string value)
    {
        if (string.Equals(value, "Smart", StringComparison.OrdinalIgnoreCase))
        {
            return TotemActorKind.SmartAi;
        }

        if (string.Equals(value, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return TotemActorKind.LightAi;
        }

        return TotemCatalogEnum.Parse(value, TotemActorKind.LightAi);
    }
}

[Serializable]
public sealed class TotemBotProfileDefinition
{
    public int BotId;
    public TotemActorKind ActorKind;
    public string DisplayName;
    public float RethinkInterval;
    public float AttackCooldown;
    public float VisionRadius;
    public float AggroRadius;
    public float DodgeReactionSec;
    public float Confidence;
    public int PreferredPreset;
    public float LootGreedFactor;
    public float SelfTattooBoldness;
    public float EnchantGreed;
    public TotemAIPersonality Personality;
    public float TargetPlayerWeight;
    public float TargetHumanoidAiWeight;
    public float TargetBossWeight;
    public float TargetResourceWeight;
    public float ReadingTargetWeight;
    public float ShopPreference;
    public float RiskTolerance;
}

[Serializable]
public sealed class TotemBotBuildPresetCatalogEntry
{
    public int presetId;
    public string name;
    public float[] tendency = Array.Empty<float>();
    public int[] preferredParts = Array.Empty<int>();
    public TotemBotBuildSlot[] recommendedSeq = Array.Empty<TotemBotBuildSlot>();
    public int earlyGameWeapon;
    public string behaviorMacro;
    public int preferredSkillQ;
    public int preferredSkillE;
    public int[] targetEnchantAffixes = Array.Empty<int>();

    public TotemBotBuildPresetDefinition ToDefinition()
    {
        return new TotemBotBuildPresetDefinition
        {
            PresetId = presetId,
            Name = name,
            Tendency = tendency ?? Array.Empty<float>(),
            PreferredParts = preferredParts ?? Array.Empty<int>(),
            RecommendedSeq = recommendedSeq ?? Array.Empty<TotemBotBuildSlot>(),
            EarlyGameWeapon = earlyGameWeapon,
            BehaviorMacro = TotemCatalogEnum.Parse(behaviorMacro, TotemAIBehaviorMacro.Hybrid),
            PreferredSkillQ = preferredSkillQ,
            PreferredSkillE = preferredSkillE,
            TargetEnchantAffixes = targetEnchantAffixes ?? Array.Empty<int>(),
        };
    }
}

[Serializable]
public sealed class TotemBotBuildPresetDefinition
{
    public int PresetId;
    public string Name;
    public float[] Tendency = Array.Empty<float>();
    public int[] PreferredParts = Array.Empty<int>();
    public TotemBotBuildSlot[] RecommendedSeq = Array.Empty<TotemBotBuildSlot>();
    public int EarlyGameWeapon;
    public TotemAIBehaviorMacro BehaviorMacro;
    public int PreferredSkillQ;
    public int PreferredSkillE;
    public int[] TargetEnchantAffixes = Array.Empty<int>();
}

[Serializable]
public sealed class TotemBotBuildSlot
{
    public int partId;
    public int colorId;
    public int patternId;

    public string Format()
    {
        return $"Part{partId}/Color{colorId}/Pattern{patternId}";
    }
}

[Serializable]
public sealed class TotemAITuningDefinition
{
    public float lodRadius;
    public float lodScanInterval;
    public float smartColdInterval;
    public float lightHotInterval;
    public float lightColdInterval;
    public float smartAttackRange;
    public float lightAttackRange;
    public float bossAttackRange;
    public float smartMoveSpeed;
    public float lightMoveSpeed;
    public float bossMoveSpeed;
    public float smartAttackCooldown;
    public float lightAttackCooldown;
    public float bossAttackCooldown;
    public float smartVisionRadius;
    public float lightVisionRadius;
    public float smartSkillRadius;
    public float smartSkillCooldown;
    public float dodgeReactionSec;
    public float smartDamage;
    public float lightDamage;
    public float bossDamage;

    public static TotemAITuningDefinition Default => new TotemAITuningDefinition
    {
        lodRadius = TotemAIService.LodRadius,
        lodScanInterval = TotemAIService.LodScanInterval,
        smartColdInterval = TotemAIService.SmartColdInterval,
        lightHotInterval = TotemAIService.LightHotInterval,
        lightColdInterval = TotemAIService.LightColdInterval,
        smartAttackRange = TotemAIService.SmartAttackRange,
        lightAttackRange = TotemAIService.LightAttackRange,
        bossAttackRange = 5f,
        smartMoveSpeed = 4.2f,
        lightMoveSpeed = 2.4f,
        bossMoveSpeed = 3.0f,
        smartAttackCooldown = 1.0f,
        lightAttackCooldown = 1.5f,
        bossAttackCooldown = 1.5f,
        smartVisionRadius = 22f,
        lightVisionRadius = 16f,
        smartSkillRadius = 8f,
        smartSkillCooldown = 3f,
        dodgeReactionSec = 0.18f,
        smartDamage = 8f,
        lightDamage = 5f,
        bossDamage = 18f,
    };
}

public static class TotemGameplayCatalogValidator
{
    public static bool Validate(TotemGameplayCatalog catalog, IList<string> errors)
    {
        if (catalog == null)
        {
            errors?.Add("Catalog is null.");
            return false;
        }

        catalog.Normalize();
        Require(catalog.schemaVersion > 0, errors, "schemaVersion must be positive.");
        Require(catalog.items.Length >= 31, errors, "At least 31 ItemConfig rows are required.");
        Require(ItemsAreValid(catalog), errors, "Items must preserve old ItemConfig ids, types, stack limits and prices.");
        Require(ResourcesAreValid(catalog), errors, "ResourceConfig rows must be valid and must not reference retired sprite folders.");
        Require(catalog.weapons.Length >= 5, errors, "At least 5 weapons are required.");
        Require(catalog.projectiles.Length >= 2, errors, "At least 2 projectile rows are required.");
        Require(catalog.weaponTraits.Length >= 10, errors, "At least 10 weapon trait rows are required.");
        Require(WeaponMetadataIsValid(catalog), errors, "Weapons must preserve valid frame timing, projectile refs and trait refs.");
        Require(ProjectilesAreValid(catalog), errors, "Projectiles must define ids, speed, range and pool size.");
        Require(WeaponTraitsAreValid(catalog), errors, "Weapon traits must define ids and known effect types.");
        Require(catalog.weaponDrops.Length >= 15, errors, "At least 15 weapon drop rows are required.");
        Require(WeaponDropsAreValid(catalog), errors, "Weapon drops must reference existing weapons and carry positive weights.");
        Require(catalog.chestRewards.Length >= 6, errors, "At least 6 chest reward rows are required.");
        Require(ChestRewardsAreValid(catalog), errors, "Chest rewards must define valid reward types and positive probabilities.");
        Require(ChestProbabilitiesSumTo100(catalog), errors, "Chest reward probabilities must sum to 100 per ChestId.");
        Require(catalog.mapTemplates.Length == 3, errors, "Map template count must be 3.");
        Require(MapTemplatesAreValid(catalog), errors, "Map templates must define ids, theme names, map size and colors.");
        Require(catalog.tattooParts.Length == TotemTattooService.PartCount, errors, "Tattoo part count must be 6.");
        Require(catalog.tattooColors.Length == TotemTattooService.ColorCount, errors, "Tattoo color count must be 7.");
        Require(catalog.tattooElements.Length == TotemTattooService.ColorCount, errors, "Tattoo element count must be 7.");
        Require(catalog.tattooPatterns.Length == TotemTattooService.PatternCount, errors, "Tattoo pattern count must be 8.");
        Require(catalog.tattooShapes.Length == TotemTattooService.PatternCount, errors, "Tattoo shape count must be 8.");
        Require(TattooCoreTablesAreValid(catalog), errors, "Tattoo part/color/element/pattern/shape tables must preserve ids, refs and old parameters.");
        Require(catalog.tattooReadingTimes.Length == TotemTattooService.PartCount, errors, "Tattoo reading time count must be 6.");
        Require(TattooReadingTimesAreValid(catalog), errors, "Tattoo reading times must cover all 6 parts with positive durations.");
        Require(catalog.tattooEnchantRecipes.Length >= 3, errors, "At least 3 tattoo enchant recipes are required.");
        Require(TattooEnchantRecipesAreValid(catalog), errors, "Tattoo enchant recipes must cover Common/Rare/Legendary tiers.");
        Require(catalog.tattooEnchantAffixes.Length >= 24, errors, "At least 24 tattoo enchant affix rows are required.");
        Require(TattooEnchantAffixesAreValid(catalog), errors, "Tattoo enchant affixes must define unique ids, known types, valid tiers and weights.");
        Require(catalog.skills.Length >= 8, errors, "At least 8 migrated SkillConfig rows are required.");
        Require(SkillsAreValid(catalog), errors, "Skills must preserve valid SkillConfig timing, charge model and hit metadata.");
        Require(catalog.enemies.Length == 15, errors, "EnemyConfig must contain the 15 confirmed enemy definitions.");
        Require(catalog.enemyAbilities.Length >= 13, errors, "EnemyAbilityConfig must contain all reusable ability types.");
        Require(catalog.encounterSpawns.Length == 9, errors, "EncounterSpawnConfig must contain three schedules per map theme.");
        Require(catalog.enemyLoot.Length >= 37, errors, "EnemyLootConfig must contain Light, Elite and Boss loot rows.");
        Require(EnemyDomainIsValid(catalog), errors, "Enemy data must preserve ids, abilities, pools, loot, assets and foreign keys.");
        Require(catalog.zonePhases.Length == 3, errors, "Zone phase count must be 3.");
        Require(ZonePhasesAreValid(catalog), errors, "Zone phases must preserve tuned ZoneShrinkConfig timing, radii, damage and offset modes.");
        Require(catalog.bossPhases.Length == 9, errors, "Boss phase count must be 9 across three bosses.");
        Require(BossPhasesAreValid(catalog), errors, "Each Boss must preserve three ability phases, thresholds, VFX/BGM cues and death recipe.");
        Require(catalog.audioCues.Length >= 14, errors, "At least 14 audio cue rows are required.");
        Require(AudioCuesAreValid(catalog), errors, "Audio cues must define BGM/SFX ids, GF_X asset names and Boss phase BGM refs.");
        Require(catalog.npcs.Length == 5, errors, "NPC count must be 5.");
        Require(catalog.shopStocks.Length >= 15, errors, "At least 15 shop stock rows are required.");
        Require(ShopStocksAreValid(catalog), errors, "Shop stock rows must define table ids, item ids, prices, stock counts and known reward categories.");
        Require(catalog.merchantSlots.Length >= 9, errors, "At least 9 MerchantConfig slot rows are required.");
        Require(MerchantSlotsAreValid(catalog), errors, "Merchant slots must define slot indexes, valid weapon refs, prices and weights.");
        Require(MerchantOffersHaveRewardTypes(catalog), errors, "Merchant shop offers must define rewardType.");
        Require(catalog.events.Length >= 6, errors, "At least 6 event rows are required.");
        Require(EventsAreValid(catalog), errors, "Events must define ids, known event types, weights and valid choice timeouts.");
        Require(catalog.choiceOptions.Length >= 11, errors, "At least 11 migrated ThreeChoiceOptionConfig rows are required.");
        Require(ChoiceOptionsAreValid(catalog), errors, "Choice options must define known option types, weights, timing and content references.");
        Require(CountBotProfiles(catalog, "Smart") >= 20, errors, "At least 20 Smart bot profiles are required.");
        Require(CountBotProfiles(catalog, "Light") >= 3, errors, "At least 3 Light bot profiles are required.");
        Require(catalog.botBuildPresets.Length >= 7, errors, "At least 7 bot build presets are required.");
        Require(BotProfilesHaveValidPresets(catalog), errors, "Bot profiles must reference existing build presets.");
        Require(BotProfilePersonalitiesAreValid(catalog), errors, "Smart bot profiles must preserve the confirmed five-personality distribution and target weights.");
        Require(catalog.aiTuning != null && catalog.aiTuning.lodRadius > 0f, errors, "AI tuning lodRadius must be positive.");

        return errors == null || errors.Count <= 0;
    }

    private static void Require(bool condition, IList<string> errors, string message)
    {
        if (!condition)
        {
            errors?.Add(message);
        }
    }

    private static bool ItemsAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        var inkTiers = new bool[8, 4];
        int inkCount = 0;
        bool hasCoin = false;
        bool hasRecipeShard = false;
        bool hasRecipeFull = false;
        bool hasLegendaryEquipment = false;
        bool hasAntidote = false;
        for (int i = 0; i < catalog.items.Length; i++)
        {
            var item = catalog.items[i];
            if (item == null || item.itemId <= 0 || !ids.Add(item.itemId) ||
                string.IsNullOrWhiteSpace(item.itemType) || string.IsNullOrWhiteSpace(item.displayName) ||
                string.IsNullOrWhiteSpace(item.rarity) || item.maxStack <= 0 || item.basePrice < 0 ||
                item.sellRatio < 0f || item.sellRatio > 1f)
            {
                return false;
            }

            var definition = item.ToDefinition();
            if (definition.ItemType == TotemItemType.Unknown)
            {
                return false;
            }

            if (definition.ItemType == TotemItemType.InkBottle)
            {
                if (!int.TryParse(definition.SubType, out int colorId) || colorId < 1 || colorId > 7 || definition.Tier < 1 || definition.Tier > 3)
                {
                    return false;
                }

                inkTiers[colorId, definition.Tier] = true;
                inkCount++;
            }

            hasCoin |= item.itemId == 1 && definition.ItemType == TotemItemType.Coin && definition.MaxStack == 9999;
            hasRecipeShard |= item.itemId == 3001 && definition.ItemType == TotemItemType.RecipeShard;
            hasRecipeFull |= item.itemId == 3100 && definition.ItemType == TotemItemType.RecipeFull;
            hasLegendaryEquipment |= item.itemId == 4004 && definition.ItemType == TotemItemType.Equipment && definition.BasePrice == 500;
            hasAntidote |= item.itemId == 5001 && definition.ItemType == TotemItemType.Antidote;
        }

        if (inkCount != 21)
        {
            return false;
        }

        for (int colorId = 1; colorId <= 7; colorId++)
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                if (!inkTiers[colorId, tier])
                {
                    return false;
                }
            }
        }

        return hasCoin && hasRecipeShard && hasRecipeFull && hasLegendaryEquipment && hasAntidote;
    }

    private static bool ResourcesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        for (int i = 0; i < catalog.resources.Length; i++)
        {
            var resource = catalog.resources[i];
            if (resource == null || resource.id <= 0 || !ids.Add(resource.id) ||
                string.IsNullOrWhiteSpace(resource.name) || string.IsNullOrWhiteSpace(resource.resourceType) ||
                string.IsNullOrWhiteSpace(resource.loadPath) || string.IsNullOrWhiteSpace(resource.assetKey) ||
                string.IsNullOrWhiteSpace(resource.activeAssetPath))
            {
                return false;
            }

            if (ReferencesRetiredSpriteFolder(resource.loadPath) || ReferencesRetiredSpriteFolder(resource.activeAssetPath))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReferencesRetiredSpriteFolder(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return false;
        }

        return assetPath.StartsWith("Assets/Game/Sprite/Character/", StringComparison.Ordinal) ||
               assetPath.StartsWith("Assets/Game/Sprite/Characters/", StringComparison.Ordinal) ||
               assetPath.StartsWith("Assets/Game/Sprite/Environments/", StringComparison.Ordinal) ||
               assetPath.StartsWith("Assets/Game/Sprite/Recipes/", StringComparison.Ordinal) ||
               assetPath.StartsWith("Assets/Game/Sprite/Tattoo/", StringComparison.Ordinal);
    }

    private static bool MerchantSlotsAreValid(TotemGameplayCatalog catalog)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        int[] slotCounts = new int[3];
        for (int i = 0; i < catalog.merchantSlots.Length; i++)
        {
            var row = catalog.merchantSlots[i];
            if (row == null || row.slotIndex < 0 || row.slotIndex > 2 ||
                string.IsNullOrWhiteSpace(row.weaponId) || row.goldCost < 50 || row.goldCost > 200 ||
                row.refreshWeight <= 0 || !keys.Add($"{row.slotIndex}:{row.weaponId}"))
            {
                return false;
            }

            if (!WeaponExists(catalog, row.weaponId))
            {
                return false;
            }

            slotCounts[row.slotIndex]++;
        }

        for (int slot = 0; slot < slotCounts.Length; slot++)
        {
            if (slotCounts[slot] != 3)
            {
                return false;
            }
        }

        return TotemMerchantSlotCatalogEntry.CreateOffers(catalog.merchantSlots, "merchant_general").Length == 3;
    }

    private static bool TattooCoreTablesAreValid(TotemGameplayCatalog catalog)
    {
        if (!HasTattooPart(catalog, 1, "Head", "CritHitEvent", "CritMultiplier", "None", 10.0f) ||
            !HasTattooPart(catalog, 4, "RightArm", "AttackHitEvent", "WeaponDamage", "Arms", 0.8f) ||
            !HasTattooPart(catalog, 6, "RightLeg", "MoveTickEvent", "MoveSpeed", "Legs", 1.6f))
        {
            return false;
        }

        var elementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < catalog.tattooElements.Length; i++)
        {
            var element = catalog.tattooElements[i];
            if (element == null || element.id < 1 || element.id > TotemTattooService.ColorCount ||
                string.IsNullOrWhiteSpace(element.name) || !elementNames.Add(element.name) || element.baseMultiplier <= 0f)
            {
                return false;
            }
        }

        var shapeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < catalog.tattooShapes.Length; i++)
        {
            var shape = catalog.tattooShapes[i];
            if (shape == null || shape.id < 1 || shape.id > TotemTattooService.PatternCount ||
                string.IsNullOrWhiteSpace(shape.name) || !shapeNames.Add(shape.name))
            {
                return false;
            }
        }

        for (int i = 0; i < catalog.tattooColors.Length; i++)
        {
            var color = catalog.tattooColors[i];
            if (color == null || color.id < 1 || color.id > TotemTattooService.ColorCount ||
                string.IsNullOrWhiteSpace(color.name) || string.IsNullOrWhiteSpace(color.element) ||
                color.multiplier <= 0f || !elementNames.Contains(color.element))
            {
                return false;
            }
        }

        for (int i = 0; i < catalog.tattooPatterns.Length; i++)
        {
            var pattern = catalog.tattooPatterns[i];
            if (pattern == null || pattern.id < 1 || pattern.id > TotemTattooService.PatternCount ||
                string.IsNullOrWhiteSpace(pattern.name) || string.IsNullOrWhiteSpace(pattern.shape) ||
                pattern.multiplier <= 0f || !shapeNames.Contains(pattern.shape))
            {
                return false;
            }
        }

        var fire = GetTattooElement(catalog, "Fire");
        var pure = GetTattooElement(catalog, "Pure");
        var aoe = GetTattooShape(catalog, "AOEBurst");
        var prob = GetTattooShape(catalog, "ProbBurst");
        return fire != null && Mathf.Abs(fire.param1 - 2f) <= 0.001f && Mathf.Abs(fire.param2 - 3f) <= 0.001f &&
            pure != null && Mathf.Abs(pure.param1 - 0.20f) <= 0.001f && Mathf.Abs(pure.param2 - 0.01f) <= 0.001f &&
            aoe != null && Mathf.Abs(aoe.param1 - 0.6f) <= 0.001f && Mathf.Abs(aoe.param2 - 5f) <= 0.001f &&
            prob != null && Mathf.Abs(prob.param3 - 12345f) <= 0.001f;
    }

    private static bool HasTattooPart(TotemGameplayCatalog catalog, int id, string name, string triggerEvent, string scaleStat, string symmetryGroup, float scaleFactor)
    {
        for (int i = 0; i < catalog.tattooParts.Length; i++)
        {
            var part = catalog.tattooParts[i];
            if (part != null && part.id == id &&
                string.Equals(part.name, name, StringComparison.Ordinal) &&
                string.Equals(part.triggerEvent, triggerEvent, StringComparison.Ordinal) &&
                string.Equals(part.scaleStat, scaleStat, StringComparison.Ordinal) &&
                string.Equals(part.symmetryGroup, symmetryGroup, StringComparison.Ordinal) &&
                Mathf.Abs(part.scaleFactor - scaleFactor) <= 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    private static TotemTattooElementCatalogEntry GetTattooElement(TotemGameplayCatalog catalog, string name)
    {
        for (int i = 0; i < catalog.tattooElements.Length; i++)
        {
            var element = catalog.tattooElements[i];
            if (element != null && string.Equals(element.name, name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
        }

        return null;
    }

    private static TotemTattooShapeCatalogEntry GetTattooShape(TotemGameplayCatalog catalog, string name)
    {
        for (int i = 0; i < catalog.tattooShapes.Length; i++)
        {
            var shape = catalog.tattooShapes[i];
            if (shape != null && string.Equals(shape.name, name, StringComparison.OrdinalIgnoreCase))
            {
                return shape;
            }
        }

        return null;
    }

    private static bool TattooReadingTimesAreValid(TotemGameplayCatalog catalog)
    {
        var seen = new bool[TotemTattooService.PartCount + 1];
        for (int i = 0; i < catalog.tattooReadingTimes.Length; i++)
        {
            var row = catalog.tattooReadingTimes[i];
            if (row == null || row.partId < 1 || row.partId > TotemTattooService.PartCount || row.durationSec <= 0f || seen[row.partId])
            {
                return false;
            }

            seen[row.partId] = true;
        }

        for (int partId = 1; partId <= TotemTattooService.PartCount; partId++)
        {
            if (!seen[partId])
            {
                return false;
            }
        }

        return true;
    }

    private static bool TattooEnchantRecipesAreValid(TotemGameplayCatalog catalog)
    {
        bool hasCommon = false;
        bool hasRare = false;
        bool hasLegendary = false;
        var ids = new HashSet<int>();
        for (int i = 0; i < catalog.tattooEnchantRecipes.Length; i++)
        {
            var recipe = catalog.tattooEnchantRecipes[i];
            if (recipe == null || recipe.id <= 0 || !ids.Add(recipe.id) ||
                string.IsNullOrWhiteSpace(recipe.colorTier) || recipe.coinCost <= 0 || recipe.rarePigmentCost < 0 || recipe.maxAffixPerSlot <= 0)
            {
                return false;
            }

            if (string.Equals(recipe.colorTier, "Common", StringComparison.OrdinalIgnoreCase))
            {
                hasCommon = true;
            }
            else if (string.Equals(recipe.colorTier, "Rare", StringComparison.OrdinalIgnoreCase))
            {
                hasRare = true;
            }
            else if (string.Equals(recipe.colorTier, "Legendary", StringComparison.OrdinalIgnoreCase))
            {
                hasLegendary = true;
            }
        }

        return hasCommon && hasRare && hasLegendary;
    }

    private static bool TattooEnchantAffixesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        for (int i = 0; i < catalog.tattooEnchantAffixes.Length; i++)
        {
            var affix = catalog.tattooEnchantAffixes[i];
            if (affix == null || affix.id <= 0 || !ids.Add(affix.id) ||
                affix.partId < 0 || affix.partId > TotemTattooService.PartCount ||
                string.IsNullOrWhiteSpace(affix.colorTier) || string.IsNullOrWhiteSpace(affix.statKey) ||
                affix.value <= 0f || affix.weight <= 0f)
            {
                return false;
            }

            if (TotemCatalogEnum.Parse(affix.affixType, TotemTattooEnchantAffixType.Unknown) == TotemTattooEnchantAffixType.Unknown)
            {
                return false;
            }

            if (!RecipeTierExists(catalog, affix.colorTier))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EnemyDomainIsValid(TotemGameplayCatalog catalog)
    {
        var expectedEnemies = new HashSet<string>(StringComparer.Ordinal)
        {
            "enemy_common_hunter", "enemy_common_shooter", "enemy_common_guardian",
            "enemy_ai_servo", "enemy_ai_arc_drone", "enemy_ai_manager", "boss_ai_core_zero",
            "enemy_alien_crawler", "enemy_alien_spitter", "enemy_alien_guard", "boss_alien_hive_mother",
            "enemy_virus_mutant", "enemy_virus_spore_carrier", "enemy_virus_spore_host", "boss_virus_terminus",
        };
        var enemyIds = new HashSet<string>(StringComparer.Ordinal);
        var abilityIds = new HashSet<string>(StringComparer.Ordinal);
        var poolIds = new HashSet<string>(StringComparer.Ordinal);
        var lootTables = new HashSet<string>(StringComparer.Ordinal);
        var lootIds = new HashSet<string>(StringComparer.Ordinal);
        var recipeIds = new HashSet<string>(StringComparer.Ordinal);
        var audioCueIds = new HashSet<string>(StringComparer.Ordinal);
        var abilityTypes = new HashSet<TotemEnemyAbilityType>();

        for (int i = 0; i < catalog.audioCues.Length; i++)
        {
            if (catalog.audioCues[i] != null) audioCueIds.Add(catalog.audioCues[i].cueId);
        }
        for (int i = 0; i < catalog.bossPhases.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(catalog.bossPhases[i]?.deathPatternRecipeId)) recipeIds.Add(catalog.bossPhases[i].deathPatternRecipeId);
        }
        for (int i = 0; i < catalog.enemyLoot.Length; i++)
        {
            var loot = catalog.enemyLoot[i];
            if (loot == null || string.IsNullOrWhiteSpace(loot.lootEntryId) || !lootIds.Add(loot.lootEntryId) ||
                string.IsNullOrWhiteSpace(loot.lootTableId) || string.IsNullOrWhiteSpace(loot.itemId) ||
                loot.minCount <= 0 || loot.maxCount < loot.minCount || (!loot.guaranteed && loot.weight <= 0))
            {
                return false;
            }
            lootTables.Add(loot.lootTableId);
            var rewardType = TotemCatalogEnum.Parse(loot.rewardType, TotemEnemyLootRewardType.Unknown);
            if (rewardType == TotemEnemyLootRewardType.Unknown || TotemCatalogEnum.Parse(loot.tierFilter, TotemEnemyTier.Unknown) == TotemEnemyTier.Unknown)
            {
                return false;
            }
            if (rewardType == TotemEnemyLootRewardType.Weapon)
            {
                if (!WeaponExists(catalog, loot.itemId)) return false;
            }
            else if (rewardType == TotemEnemyLootRewardType.Recipe)
            {
                if (!recipeIds.Contains(loot.itemId)) return false;
            }
            else if (!int.TryParse(loot.itemId, out int itemId) || !ItemExists(catalog, itemId))
            {
                return false;
            }
        }

        for (int i = 0; i < catalog.enemyAbilities.Length; i++)
        {
            var ability = catalog.enemyAbilities[i];
            if (ability == null || string.IsNullOrWhiteSpace(ability.abilityId) || !abilityIds.Add(ability.abilityId) ||
                ability.cooldown < 0f || ability.windup < 0f || ability.active < 0f || ability.recovery < 0f ||
                ability.statusChance < 0f || ability.statusChance > 1f || string.IsNullOrWhiteSpace(ability.parametersJson))
            {
                return false;
            }
            var abilityType = TotemCatalogEnum.Parse(ability.abilityType, TotemEnemyAbilityType.Unknown);
            if (abilityType == TotemEnemyAbilityType.Unknown ||
                (!string.IsNullOrWhiteSpace(ability.audioCueId) && !audioCueIds.Contains(ability.audioCueId)))
            {
                return false;
            }
            abilityTypes.Add(abilityType);
        }
        for (int type = (int)TotemEnemyAbilityType.Melee; type <= (int)TotemEnemyAbilityType.PhaseTransition; type++)
        {
            if (!abilityTypes.Contains((TotemEnemyAbilityType)type)) return false;
        }

        for (int i = 0; i < catalog.enemies.Length; i++)
        {
            var enemy = catalog.enemies[i];
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId) || !enemyIds.Add(enemy.enemyId) ||
                string.IsNullOrWhiteSpace(enemy.displayName) || string.IsNullOrWhiteSpace(enemy.themeId) ||
                string.IsNullOrWhiteSpace(enemy.runtimeAssetKey) || string.IsNullOrWhiteSpace(enemy.fallbackRuntimeAssetKey) ||
                string.IsNullOrWhiteSpace(enemy.behaviorProfileId) || string.IsNullOrWhiteSpace(enemy.abilityIds) ||
                enemy.baseHP <= 0f || enemy.baseDamage < 0f || enemy.moveSpeed <= 0f ||
                enemy.attackRange <= 0f || enemy.detectRange < enemy.attackRange || enemy.leashRange < enemy.detectRange ||
                enemy.spawnCost <= 0 || !lootTables.Contains(enemy.lootTableId) || string.IsNullOrWhiteSpace(enemy.poolIds))
            {
                return false;
            }
            if (!AllDelimitedIdsExist(enemy.abilityIds, abilityIds)) return false;
            AddDelimitedIds(enemy.poolIds, poolIds);
            var guaranteed = new HashSet<string>(StringComparer.Ordinal);
            AddDelimitedIds(enemy.guaranteedLootIds, guaranteed);
            foreach (string lootId in guaranteed)
            {
                if (!lootIds.Contains(lootId) || !GuaranteedLootMatches(catalog, lootId, enemy.lootTableId)) return false;
            }
        }
        if (!enemyIds.SetEquals(expectedEnemies)) return false;

        for (int i = 0; i < catalog.enemyAbilities.Length; i++)
        {
            var ability = catalog.enemyAbilities[i];
            if (string.Equals(ability.abilityType, "Summon", StringComparison.OrdinalIgnoreCase) &&
                (ability.summonCount <= 0 || !enemyIds.Contains(ability.summonEnemyId))) return false;
        }
        for (int i = 0; i < catalog.encounterSpawns.Length; i++)
        {
            var encounter = catalog.encounterSpawns[i];
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.encounterId) || string.IsNullOrWhiteSpace(encounter.themeId) ||
                string.IsNullOrWhiteSpace(encounter.zoneRoles) || !AllDelimitedIdsExist(encounter.enemyPoolIds, poolIds) ||
                encounter.initialCount > encounter.activeCap || encounter.activeCap > encounter.totalCap ||
                encounter.waveMin > encounter.waveMax || encounter.minParticipantDistance <= 0f || encounter.minSpacing <= 0f)
            {
                return false;
            }
        }
        return true;
    }

    private static bool ItemExists(TotemGameplayCatalog catalog, int itemId)
    {
        for (int i = 0; i < catalog.items.Length; i++)
        {
            if (catalog.items[i] != null && catalog.items[i].itemId == itemId) return true;
        }
        return false;
    }

    private static bool GuaranteedLootMatches(TotemGameplayCatalog catalog, string lootEntryId, string lootTableId)
    {
        for (int i = 0; i < catalog.enemyLoot.Length; i++)
        {
            var loot = catalog.enemyLoot[i];
            if (loot != null && loot.guaranteed && string.Equals(loot.lootEntryId, lootEntryId, StringComparison.Ordinal) &&
                string.Equals(loot.lootTableId, lootTableId, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static bool AllDelimitedIdsExist(string value, HashSet<string> ids)
    {
        var parsed = new HashSet<string>(StringComparer.Ordinal);
        AddDelimitedIds(value, parsed);
        if (parsed.Count == 0) return false;
        foreach (string id in parsed)
        {
            if (!ids.Contains(id)) return false;
        }
        return true;
    }

    private static void AddDelimitedIds(string value, HashSet<string> target)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string[] parts = value.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string id = parts[i].Trim();
            if (!string.IsNullOrWhiteSpace(id)) target.Add(id);
        }
    }

    private static bool ZonePhasesAreValid(TotemGameplayCatalog catalog)
    {
        if (catalog.zonePhases == null || catalog.zonePhases.Length != 3)
        {
            return false;
        }

        return ZonePhaseMatches(catalog.zonePhases, 0, "Phase0_Slow", 0f, 180f, 65f, 2f, "None") &&
            ZonePhaseMatches(catalog.zonePhases, 1, "Phase1_Offset", 180f, 360f, 35f, 5f, "Drift") &&
            ZonePhaseMatches(catalog.zonePhases, 2, "Phase2_Rush", 540f, 360f, 5f, 12f, "Fixed");
    }

    private static bool ZonePhaseMatches(
        TotemZonePhaseCatalogEntry[] phases,
        int index,
        string phaseName,
        float startTime,
        float duration,
        float targetRadius,
        float outZoneDamage,
        string centerOffsetMode)
    {
        var phase = phases[index];
        return phase != null &&
            phase.id == index &&
            string.Equals(phase.phaseName, phaseName, StringComparison.Ordinal) &&
            Mathf.Abs(phase.startTime - startTime) <= 0.001f &&
            Mathf.Abs(phase.duration - duration) <= 0.001f &&
            Mathf.Abs(phase.targetRadius - targetRadius) <= 0.001f &&
            Mathf.Abs(phase.outZoneDamage - outZoneDamage) <= 0.001f &&
            string.Equals(phase.centerOffsetMode, centerOffsetMode, StringComparison.Ordinal);
    }

    private static bool BossPhasesAreValid(TotemGameplayCatalog catalog)
    {
        var abilityIds = new HashSet<string>(StringComparer.Ordinal);
        var bossIds = new HashSet<string>(StringComparer.Ordinal);
        var phaseKeys = new HashSet<string>(StringComparer.Ordinal);
        var finalRecipes = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < catalog.enemyAbilities.Length; i++)
        {
            if (catalog.enemyAbilities[i] != null) abilityIds.Add(catalog.enemyAbilities[i].abilityId);
        }
        for (int i = 0; i < catalog.enemies.Length; i++)
        {
            var enemy = catalog.enemies[i];
            if (enemy != null && string.Equals(enemy.tier, "Boss", StringComparison.OrdinalIgnoreCase)) bossIds.Add(enemy.enemyId);
        }
        for (int i = 0; i < catalog.bossPhases.Length; i++)
        {
            var phase = catalog.bossPhases[i];
            if (phase == null || phase.phaseIndex < 1 || phase.phaseIndex > 3 ||
                string.IsNullOrWhiteSpace(phase.bossId) || !bossIds.Contains(phase.bossId) ||
                !phaseKeys.Add(phase.bossId + ":" + phase.phaseIndex) ||
                string.IsNullOrWhiteSpace(phase.abilityIds) || string.IsNullOrWhiteSpace(phase.skillIds) ||
                string.IsNullOrWhiteSpace(phase.phaseVFXId) || string.IsNullOrWhiteSpace(phase.phaseBGMCueId) ||
                phase.hpThreshold <= 0f || phase.hpThreshold > 1f || phase.enrageMultiplier <= 0f)
            {
                return false;
            }
            float expectedThreshold = phase.phaseIndex == 1 ? 1f : phase.phaseIndex == 2 ? 0.6f : 0.3f;
            if (Mathf.Abs(phase.hpThreshold - expectedThreshold) > 0.001f ||
                !AllDelimitedIdsExist(phase.abilityIds, abilityIds) || !AllSkillIdsExist(catalog, phase.skillIds))
            {
                return false;
            }
            if (phase.phaseIndex == 3)
            {
                if (string.IsNullOrWhiteSpace(phase.deathPatternRecipeId)) return false;
                finalRecipes.Add(phase.bossId);
            }
        }
        return bossIds.Count == 3 && phaseKeys.Count == 9 && finalRecipes.SetEquals(bossIds);
    }

    private static bool AudioCuesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool hasMenuBgm = false;
        bool hasInGameBgm = false;
        bool hasKill = false;
        bool hasPlayerDied = false;
        for (int i = 0; i < catalog.audioCues.Length; i++)
        {
            var cue = catalog.audioCues[i];
            if (cue == null || string.IsNullOrWhiteSpace(cue.cueId) || !ids.Add(cue.cueId) ||
                string.IsNullOrWhiteSpace(cue.assetName) || cue.volume <= 0f || cue.volume > 1f ||
                cue.minIntervalSec < 0f)
            {
                return false;
            }

            var definition = cue.ToDefinition();
            if (definition.Kind == TotemAudioCueKind.Unknown)
            {
                return false;
            }

            if (definition.Kind == TotemAudioCueKind.Bgm && !definition.Loop)
            {
                return false;
            }

            hasMenuBgm |= string.Equals(cue.cueId, "bgm_main_menu", StringComparison.Ordinal);
            hasInGameBgm |= string.Equals(cue.cueId, "bgm_in_game", StringComparison.Ordinal);
            hasKill |= string.Equals(cue.cueId, "sfx_kill", StringComparison.Ordinal);
            hasPlayerDied |= string.Equals(cue.cueId, "sfx_player_died", StringComparison.Ordinal);
        }

        if (!hasMenuBgm || !hasInGameBgm || !hasKill || !hasPlayerDied)
        {
            return false;
        }

        for (int i = 0; i < catalog.bossPhases.Length; i++)
        {
            string cueId = catalog.bossPhases[i]?.phaseBGMCueId;
            if (string.IsNullOrWhiteSpace(cueId) || !ids.Contains(cueId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ChoiceOptionsAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool hasCoin = false;
        bool hasHeal = false;
        bool hasWeaponUpgrade = false;
        bool hasRecipe = false;
        bool hasSkill = false;
        for (int i = 0; i < catalog.choiceOptions.Length; i++)
        {
            var row = catalog.choiceOptions[i];
            if (row == null || string.IsNullOrWhiteSpace(row.optionId) || !ids.Add(row.optionId) ||
                string.IsNullOrWhiteSpace(row.displayName) || string.IsNullOrWhiteSpace(row.descKey) ||
                row.weightBase <= 0 || row.skillSlot < -1 || row.minRunElapsedSec < 0f)
            {
                return false;
            }

            var option = row.ToOption();
            if (option.OptionType == TotemChoiceOptionType.Unknown)
            {
                return false;
            }

            if ((option.OptionType == TotemChoiceOptionType.TattooRecipe ||
                option.OptionType == TotemChoiceOptionType.PatternRecipe ||
                option.OptionType == TotemChoiceOptionType.OneTimeScroll ||
                option.OptionType == TotemChoiceOptionType.SkillAcquire ||
                option.OptionType == TotemChoiceOptionType.SkillUpgrade ||
                option.OptionType == TotemChoiceOptionType.WeaponUpgrade) &&
                string.IsNullOrWhiteSpace(option.ContentRef))
            {
                return false;
            }

            if ((option.OptionType == TotemChoiceOptionType.CoinBonus || option.OptionType == TotemChoiceOptionType.Heal) &&
                option.ValueInt <= 0)
            {
                return false;
            }

            if (option.OptionType == TotemChoiceOptionType.SkillAcquire &&
                !SkillExists(catalog, option.ContentRef))
            {
                return false;
            }

            hasCoin |= option.OptionType == TotemChoiceOptionType.CoinBonus;
            hasHeal |= option.OptionType == TotemChoiceOptionType.Heal;
            hasWeaponUpgrade |= option.OptionType == TotemChoiceOptionType.WeaponUpgrade;
            hasRecipe |= option.OptionType == TotemChoiceOptionType.TattooRecipe || option.OptionType == TotemChoiceOptionType.PatternRecipe;
            hasSkill |= option.OptionType == TotemChoiceOptionType.SkillAcquire || option.OptionType == TotemChoiceOptionType.SkillUpgrade;
        }

        return hasCoin && hasHeal && hasWeaponUpgrade && hasRecipe && hasSkill;
    }

    private static bool RecipeTierExists(TotemGameplayCatalog catalog, string colorTier)
    {
        for (int i = 0; i < catalog.tattooEnchantRecipes.Length; i++)
        {
            var recipe = catalog.tattooEnchantRecipes[i];
            if (recipe != null && string.Equals(recipe.colorTier, colorTier, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MerchantOffersHaveRewardTypes(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.npcs.Length; i++)
        {
            var npc = catalog.npcs[i];
            if (!string.Equals(npc.type, "Merchant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(npc.shopStockTable))
            {
                var stockOffers = TotemShopStockCatalogEntry.CreateOffers(catalog.shopStocks, npc.shopStockTable);
                if (stockOffers.Length <= 0)
                {
                    return false;
                }

                for (int offerIndex = 0; offerIndex < stockOffers.Length; offerIndex++)
                {
                    if (stockOffers[offerIndex].RewardType == TotemShopRewardType.Unknown)
                    {
                        return false;
                    }
                }

                continue;
            }

            var offers = npc.offers;
            if (offers == null || offers.Length <= 0)
            {
                return false;
            }

            for (int offerIndex = 0; offerIndex < offers.Length; offerIndex++)
            {
                if (TotemCatalogEnum.Parse(offers[offerIndex].rewardType, TotemShopRewardType.Unknown) == TotemShopRewardType.Unknown)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ShopStocksAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        for (int i = 0; i < catalog.shopStocks.Length; i++)
        {
            var row = catalog.shopStocks[i];
            if (row == null || row.id <= 0 || !ids.Add(row.id) ||
                string.IsNullOrWhiteSpace(row.tableId) || row.itemId <= 0 ||
                string.IsNullOrWhiteSpace(row.category) || row.weight <= 0f ||
                row.minCount <= 0 || row.maxCount < row.minCount || row.basePrice <= 0)
            {
                return false;
            }

            if (row.ToOffer().RewardType == TotemShopRewardType.Unknown)
            {
                return false;
            }
        }

        return ShopStockTableHasRows(catalog, "general_shop") && ShopStockTableHasRows(catalog, "alien_shop");
    }

    private static bool ShopStockTableHasRows(TotemGameplayCatalog catalog, string tableId)
    {
        return TotemShopStockCatalogEntry.CreateOffers(catalog.shopStocks, tableId).Length > 0;
    }

    private static bool EventsAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool hasChoice = false;
        bool hasCombat = false;
        bool hasCurse = false;
        for (int i = 0; i < catalog.events.Length; i++)
        {
            var row = catalog.events[i];
            if (row == null || string.IsNullOrWhiteSpace(row.eventId) || !ids.Add(row.eventId) ||
                string.IsNullOrWhiteSpace(row.displayName) || row.weightBase <= 0)
            {
                return false;
            }

            var definition = row.ToDefinition();
            if (definition.EventType == TotemGameplayEventType.Unknown)
            {
                return false;
            }

            if (definition.EventType == TotemGameplayEventType.Choice && Mathf.Abs(definition.TimeoutSec - 20f) > 0.001f)
            {
                return false;
            }

            if (definition.EventType == TotemGameplayEventType.Choice)
            {
                hasChoice = true;
            }
            else if (definition.EventType == TotemGameplayEventType.Combat)
            {
                hasCombat = true;
            }
            else if (definition.EventType == TotemGameplayEventType.Curse)
            {
                hasCurse = true;
            }
        }

        return hasChoice && hasCombat && hasCurse;
    }

    private static bool WeaponDropsAreValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.weaponDrops.Length; i++)
        {
            var drop = catalog.weaponDrops[i];
            if (drop == null || string.IsNullOrWhiteSpace(drop.dropId) || string.IsNullOrWhiteSpace(drop.weaponId) || drop.weight <= 0)
            {
                return false;
            }

            if (!WeaponExists(catalog, drop.weaponId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool WeaponMetadataIsValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.weapons.Length; i++)
        {
            var weapon = catalog.weapons[i];
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.weaponId) || weapon.baseDamage <= 0f || weapon.range <= 0f)
            {
                return false;
            }

            if (weapon.baseStartup + weapon.baseActive + weapon.baseRecovery <= 0)
            {
                return false;
            }

            float expectedCooldown = TotemWeaponCatalogEntry.ComputeCooldownFromFrames(weapon.baseStartup, weapon.baseActive, weapon.baseRecovery);
            if (weapon.cooldown > 0f && Mathf.Abs(weapon.cooldown - expectedCooldown) > 0.002f)
            {
                return false;
            }

            var weaponClass = TotemCatalogEnum.Parse(weapon.className, TotemWeaponClass.Melee);
            if (weaponClass == TotemWeaponClass.Ranged && !ProjectileExists(catalog, weapon.projectileId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(weapon.normalTraitId) && !WeaponTraitExists(catalog, weapon.normalTraitId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(weapon.chargedTraitId) && !WeaponTraitExists(catalog, weapon.chargedTraitId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ProjectilesAreValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.projectiles.Length; i++)
        {
            var projectile = catalog.projectiles[i];
            if (projectile == null || string.IsNullOrWhiteSpace(projectile.projectileId) ||
                projectile.speed <= 0f || projectile.maxRange <= 0f || projectile.poolSize <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool WeaponTraitsAreValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.weaponTraits.Length; i++)
        {
            var trait = catalog.weaponTraits[i];
            if (trait == null || string.IsNullOrWhiteSpace(trait.traitId))
            {
                return false;
            }

            if (TotemCatalogEnum.Parse(trait.effectType, TotemWeaponTraitEffectType.Unknown) == TotemWeaponTraitEffectType.Unknown)
            {
                return false;
            }
        }

        return true;
    }

    private static bool WeaponExists(TotemGameplayCatalog catalog, string weaponId)
    {
        for (int i = 0; i < catalog.weapons.Length; i++)
        {
            var weapon = catalog.weapons[i];
            if (weapon != null && string.Equals(weapon.weaponId, weaponId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ProjectileExists(TotemGameplayCatalog catalog, string projectileId)
    {
        for (int i = 0; i < catalog.projectiles.Length; i++)
        {
            var projectile = catalog.projectiles[i];
            if (projectile != null && string.Equals(projectile.projectileId, projectileId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool WeaponTraitExists(TotemGameplayCatalog catalog, string traitId)
    {
        for (int i = 0; i < catalog.weaponTraits.Length; i++)
        {
            var trait = catalog.weaponTraits[i];
            if (trait != null && string.Equals(trait.traitId, traitId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ChestRewardsAreValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.chestRewards.Length; i++)
        {
            var reward = catalog.chestRewards[i];
            if (reward == null || string.IsNullOrWhiteSpace(reward.chestId) || reward.probability <= 0)
            {
                return false;
            }

            var rewardType = TotemCatalogEnum.Parse(reward.rewardType, TotemChestRewardType.Unknown);
            if (rewardType == TotemChestRewardType.Unknown)
            {
                return false;
            }

            if (rewardType == TotemChestRewardType.Weapon && !string.IsNullOrWhiteSpace(reward.rewardId) && !WeaponExists(catalog, reward.rewardId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ChestProbabilitiesSumTo100(TotemGameplayCatalog catalog)
    {
        var sums = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < catalog.chestRewards.Length; i++)
        {
            var reward = catalog.chestRewards[i];
            if (reward == null || string.IsNullOrWhiteSpace(reward.chestId))
            {
                return false;
            }

            if (!sums.ContainsKey(reward.chestId))
            {
                sums[reward.chestId] = 0;
            }

            sums[reward.chestId] += Mathf.Max(0, reward.probability);
        }

        foreach (var pair in sums)
        {
            if (pair.Value != 100)
            {
                return false;
            }
        }

        return sums.Count > 0;
    }

    private static bool MapTemplatesAreValid(TotemGameplayCatalog catalog)
    {
        var ids = new HashSet<int>();
        bool hasRuins = false;
        bool hasAlienHive = false;
        bool hasVirusSwamp = false;
        for (int i = 0; i < catalog.mapTemplates.Length; i++)
        {
            var row = catalog.mapTemplates[i];
            if (row == null || row.id <= 0 || !ids.Add(row.id) ||
                string.IsNullOrWhiteSpace(row.themeName) || Mathf.Abs(row.mapSize - TotemMapService.DefaultMapSize) > 0.001f ||
                row.minRoomSize <= 0f ||
                string.IsNullOrWhiteSpace(row.hudAccentColor) || string.IsNullOrWhiteSpace(row.dominantColor))
            {
                return false;
            }

            if (string.Equals(row.themeName, "AI_RUINS", StringComparison.Ordinal))
            {
                hasRuins = true;
            }
            else if (string.Equals(row.themeName, "ALIEN_HIVE", StringComparison.Ordinal))
            {
                hasAlienHive = true;
            }
            else if (string.Equals(row.themeName, "VIRUS_SWAMP", StringComparison.Ordinal))
            {
                hasVirusSwamp = true;
            }
        }

        return hasRuins && hasAlienHive && hasVirusSwamp;
    }

    private static bool SkillsAreValid(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.skills.Length; i++)
        {
            var skill = catalog.skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.skillId) || skill.maxCharges <= 0)
            {
                return false;
            }

            if (skill.startupFrames + skill.activeFrames + skill.recoveryFrames <= 0)
            {
                return false;
            }

            if (skill.chargeModel < 0 || skill.chargeModel > 2)
            {
                return false;
            }

            if (skill.chargeModel == 0 && skill.cooldown <= 0f)
            {
                return false;
            }

            if (skill.chargeModel == 1 && skill.chargeRegenTime <= 0f)
            {
                return false;
            }

            if (skill.chargeModel == 2 && skill.holdDuration <= 0f)
            {
                return false;
            }

            var hitShape = TotemCatalogEnum.Parse(skill.hitShape, TotemSkillHitShape.Single);
            if (hitShape == TotemSkillHitShape.Circle && skill.hitRadius <= 0f && skill.radius <= 0f)
            {
                return false;
            }
        }

        return SkillExists(catalog, "skill_fireball_01") &&
               SkillExists(catalog, "skill_chain_lightning_01") &&
               SkillExists(catalog, "skill_stealth_01") &&
               SkillExists(catalog, "skill_phase_dash") &&
               SkillExists(catalog, "skill_ink_shield") &&
               SkillExists(catalog, "skill_stomp") &&
               SkillExists(catalog, "skill_beam") &&
               SkillExists(catalog, "skill_summon") &&
               SkillExists(catalog, "skill_enrage_aoe");
    }

    private static bool AllSkillIdsExist(TotemGameplayCatalog catalog, string skillIds)
    {
        if (string.IsNullOrWhiteSpace(skillIds))
        {
            return false;
        }

        string[] ids = skillIds.Split(',');
        bool any = false;
        for (int i = 0; i < ids.Length; i++)
        {
            string skillId = ids[i]?.Trim();
            if (string.IsNullOrWhiteSpace(skillId))
            {
                continue;
            }

            any = true;
            if (!SkillExists(catalog, skillId))
            {
                return false;
            }
        }

        return any;
    }

    private static bool SkillExists(TotemGameplayCatalog catalog, string skillId)
    {
        for (int i = 0; i < catalog.skills.Length; i++)
        {
            var skill = catalog.skills[i];
            if (skill != null && string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountBotProfiles(TotemGameplayCatalog catalog, string profileType)
    {
        int count = 0;
        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            if (string.Equals(catalog.botProfiles[i].type, profileType, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static bool BotProfilesHaveValidPresets(TotemGameplayCatalog catalog)
    {
        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            int presetId = catalog.botProfiles[i].preferredPreset;
            bool found = false;
            for (int presetIndex = 0; presetIndex < catalog.botBuildPresets.Length; presetIndex++)
            {
                if (catalog.botBuildPresets[presetIndex].presetId == presetId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool BotProfilePersonalitiesAreValid(TotemGameplayCatalog catalog)
    {
        int aggressive = 0;
        int conservative = 0;
        int resource = 0;
        int boss = 0;
        int player = 0;
        bool bossWeightsValid = false;
        bool playerWeightsValid = false;
        bool resourceWeightsValid = false;

        for (int i = 0; i < catalog.botProfiles.Length; i++)
        {
            var row = catalog.botProfiles[i];
            if (row == null || !string.Equals(row.type, "Smart", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var profile = row.ToDefinition();
            switch (profile.Personality)
            {
                case TotemAIPersonality.Aggressive:
                    aggressive++;
                    break;
                case TotemAIPersonality.Conservative:
                    conservative++;
                    break;
                case TotemAIPersonality.ResourceAcquisition:
                    resource++;
                    resourceWeightsValid |= profile.TargetResourceWeight > profile.TargetPlayerWeight && profile.ShopPreference > 0.5f;
                    break;
                case TotemAIPersonality.BossPriority:
                    boss++;
                    bossWeightsValid |= profile.TargetBossWeight > profile.TargetResourceWeight && profile.TargetBossWeight > profile.TargetPlayerWeight;
                    break;
                case TotemAIPersonality.PlayerPriority:
                    player++;
                    playerWeightsValid |= Mathf.Abs(profile.TargetPlayerWeight - profile.TargetHumanoidAiWeight) <= 0.001f;
                    break;
            }
        }

        return aggressive == 5 &&
               conservative == 3 &&
               resource == 4 &&
               boss == 4 &&
               player == 4 &&
               bossWeightsValid &&
               playerWeightsValid &&
               resourceWeightsValid;
    }
}

public static class TotemCatalogEnum
{
    public static TEnum Parse<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out TEnum parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
