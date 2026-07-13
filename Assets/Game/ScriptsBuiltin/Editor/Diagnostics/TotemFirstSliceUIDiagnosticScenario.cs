#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UGF.EditorTools
{
    public sealed class TotemFirstSliceUIDiagnosticScenario : GFDiagnosticScenarioBase
    {
        private static readonly PrefabContract[] PrefabContracts =
        {
            new PrefabContract("MainMenu", typeof(TotemMainMenuForm)),
            new PrefabContract("CharacterSelect", typeof(TotemCharacterSelectForm)),
            new PrefabContract("StartupSelect", typeof(TotemStartupSelectForm)),
            new PrefabContract("CombatHUD", typeof(TotemCombatHUDForm)),
            new PrefabContract("Shop", typeof(TotemShopForm)),
            new PrefabContract("ThreeChoice", typeof(TotemThreeChoiceForm)),
            new PrefabContract("TattooStudio", typeof(TotemTattooStudioForm)),
            new PrefabContract("PauseMenu", typeof(TotemPauseMenuForm)),
            new PrefabContract("RunResult", typeof(TotemRunResultForm)),
            new PrefabContract("Settings", typeof(TotemSettingsForm)),
            new PrefabContract("SelfTattoo", typeof(TotemSelfTattooForm)),
            new PrefabContract("TattooEnchant", typeof(TotemTattooEnchantForm)),
        };

        public override string Name => "Totem First Slice UI";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckUIViewEnum(context);
            CheckUITableFiles(context);
            CheckUIPrefabs(context);
            CheckUIIconSources(context);
            CheckCombatHudRuntimeText(context);
            CheckStartupPatternUnlocks(context);
            CheckOverlayRuntimeText(context);
            context.Pass("Totem first slice UI contract is ready.");
        }

        private static void CheckUIViewEnum(GFDiagnosticScenarioContext context)
        {
            AssertUIView(context, "MainMenu", 1);
            AssertUIView(context, "CharacterSelect", 2);
            AssertUIView(context, "StartupSelect", 3);
            AssertUIView(context, "CombatHUD", 4);
            AssertUIView(context, "Shop", 5);
            AssertUIView(context, "ThreeChoice", 6);
            AssertUIView(context, "TattooStudio", 7);
            AssertUIView(context, "PauseMenu", 8);
            AssertUIView(context, "RunResult", 9);
            AssertUIView(context, "Settings", 10);
            AssertUIView(context, "SelfTattoo", 11);
            AssertUIView(context, "TattooEnchant", 12);
        }

        private static void CheckUITableFiles(GFDiagnosticScenarioContext context)
        {
            const string aiJson = "GameData/AIData/DataTables/Core/UITable.json";
            const string tableText = "Assets/Game/DataTable/Core/UITable.txt";
            context.RequireFile(aiJson);
            context.RequireFile(tableText);

            if (File.Exists(aiJson))
            {
                try
                {
                    var root = JObject.Parse(File.ReadAllText(aiJson));
                    var rows = root["rows"] as JArray;
                    context.Detail("UITable.json.rows", rows?.Count ?? 0);
                    context.Assert(rows != null && rows.Count >= 12, "UITable AI json must contain all active Totem UI rows.");
                }
                catch (Exception exception)
                {
                    context.Fail($"UITable AI json can not be parsed: {exception.Message}");
                }
            }

            if (File.Exists(tableText))
            {
                string text = File.ReadAllText(tableText);
                for (int i = 0; i < PrefabContracts.Length; i++)
                {
                    string prefabName = PrefabContracts[i].PrefabName;
                    context.Assert(text.Contains(prefabName, StringComparison.Ordinal), $"UITable.txt must contain UI prefab: {prefabName}");
                }
            }
        }

        private static void CheckUIPrefabs(GFDiagnosticScenarioContext context)
        {
            for (int i = 0; i < PrefabContracts.Length; i++)
            {
                CheckUIPrefab(context, PrefabContracts[i]);
            }
        }

        private static void CheckUIPrefab(GFDiagnosticScenarioContext context, PrefabContract contract)
        {
            string path = TotemFirstSlicePrefabMigrator.GetTargetPath(contract.PrefabName);
            context.RequireFile(path);
            if (!File.Exists(path))
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                context.Assert(root.GetComponent(contract.FormType) != null, $"{path} must use {contract.FormType.Name}.");
                context.Assert(root.GetComponent<Canvas>() != null, $"{path} must have Canvas.");
                context.Assert(root.GetComponent<CanvasGroup>() != null, $"{path} must have CanvasGroup.");
                context.Assert(root.GetComponent<GraphicRaycaster>() != null, $"{path} must have GraphicRaycaster.");
                context.Assert(root.GetComponent<RectTransform>() != null, $"{path} must have RectTransform.");
                if (contract.PrefabName == "CombatHUD")
                {
                    context.Assert(FindImage(root, "WeaponIcon") != null, $"{path} must have WeaponIcon image.");
                    context.Assert(FindImage(root, "SkillSlotE") != null, $"{path} must have SkillSlotE image.");
                    context.Assert(FindImage(root, "SkillSlotQ") != null, $"{path} must have SkillSlotQ image.");
                    context.Assert(FindImage(root, "CdMaskE") != null, $"{path} must keep CdMaskE cooldown image.");
                    context.Assert(FindImage(root, "CdMaskQ") != null, $"{path} must keep CdMaskQ cooldown image.");
                    context.Assert(FindTransform(root, "MinimapImage") != null, $"{path} must keep MinimapImage.");
                    context.Assert(FindTransform(root, "LogListRoot") != null, $"{path} must keep LogListRoot.");
                    context.Assert(FindTransform(root, "LogRowTemplate") != null, $"{path} must keep LogRowTemplate.");
                    context.Assert(FindTransform(root, "BuildListRoot") != null, $"{path} must keep BuildListRoot.");
                }

                int missingScriptCount = CountMissingScripts(root);
                context.Detail($"{contract.PrefabName}.missingScripts", missingScriptCount);
                context.Assert(missingScriptCount == 0, $"{path} must not contain missing scripts after migration.");

                int persistentClickCount = CountButtonPersistentClicks(root);
                context.Detail($"{contract.PrefabName}.persistentButtonClicks", persistentClickCount);
                context.Assert(persistentClickCount == 0, $"{path} must not keep old serialized button callbacks.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CheckUIIconSources(GFDiagnosticScenarioContext context)
        {
            var service = new TotemAssetService();
            service.ReloadRuntimeAssetCatalog();
            context.Assert(service.RuntimeAssetCatalogLoadedFromFile, $"Runtime asset catalog should load from file: {service.RuntimeAssetCatalogMessage}");

            AssertSpriteLoads(context, service, "ui.character.1");
            AssertSpriteLoads(context, service, "ui.character.2");
            AssertSpriteLoads(context, service, "ui.character.3");
            AssertSpriteLoads(context, service, "ui.character.card.unlocked");
            AssertSpriteLoads(context, service, "weapon.knife_basic");
            AssertSpriteLoads(context, service, "weapon.hammer_heavy");
            AssertSpriteLoads(context, service, "weapon.pistol_basic");
            AssertSpriteLoads(context, service, "weapon.bow_charge");
            AssertSpriteLoads(context, service, "weapon.energy_fist");
            AssertSpriteLoads(context, service, "skill.skill_fireball_01");
            AssertSpriteLoads(context, service, "skill.skill_stealth_01");
        }

        private static void CheckCombatHudRuntimeText(GFDiagnosticScenarioContext context)
        {
            context.AssertEqual(
                "Weapon: pistol_basic  Ammo: 12  E:1.5s  Q:2.0s",
                TotemCombatHUDForm.FormatWeaponStatus("pistol_basic", 12, true, 1.5f, 2f),
                "combatHud.weaponAmmoText");
            context.AssertEqual(
                "Weapon: knife_basic  E:0.0s  Q:0.0s",
                TotemCombatHUDForm.FormatWeaponStatus("knife_basic", 0, false, 0f),
                "combatHud.weaponMeleeText");
            context.AssertEqual(
                "Enemies: 49",
                TotemCombatHUDForm.FormatEnemyStatus(49),
                "combatHud.enemyText");
            context.AssertEqual(
                "Zone P2 R15 D8  Enemies: 7",
                TotemCombatHUDForm.FormatZoneStatus(2, 15f, 8f, 7),
                "combatHud.zoneText");
            context.AssertEqual(
                "Enemies: 3  F: Shop with merchant_general",
                TotemCombatHUDForm.AppendPrompt("Enemies: 3", "F: Shop with merchant_general"),
                "combatHud.promptText");
            context.AssertEqual(
                "Enemies: 3  Status: Burn 2.0s",
                TotemCombatHUDForm.AppendStatus("Enemies: 3", "Status: Burn 2.0s"),
                "combatHud.statusText");
            context.AssertEqual(
                "Enemies: 3",
                TotemCombatHUDForm.AppendStatus("Enemies: 3", "Status: None"),
                "combatHud.emptyStatusText");
            context.Assert(TotemCombatHUDForm.GetHpColor(0.8f) == Color.green, "combatHud.hpColor.high");
            context.Assert(TotemCombatHUDForm.GetHpColor(0.4f) == Color.yellow, "combatHud.hpColor.mid");
            context.Assert(TotemCombatHUDForm.GetHpColor(0.1f) == Color.red, "combatHud.hpColor.low");
            context.Assert(Mathf.Abs(0.25f - TotemCombatHUDForm.CalculateCooldownMaskFill(1f, 4f)) <= 0.001f, "combatHud.cooldownMask.fill");
            context.AssertEqual(
                "Build: RightArm/Red/Line",
                TotemCombatHUDForm.FormatBuildSummary(new TotemTattooSnapshot { equippedCount = 1, equippedSummary = "RightArm/Red/Line" }),
                "combatHud.buildSummary");
            context.AssertEqual(
                "Attack: SmartAI01 -18 [knife_basic]",
                TotemCombatHUDForm.FormatCombatLog(new TotemCombatSnapshot { lastAction = "Attack", lastTargetName = "SmartAI01", lastDamage = 18f, lastWeaponId = "knife_basic" }),
                "combatHud.combatLog.attack");
            context.AssertEqual(
                "Skill: Boss -45 KO [skill_fireball_01]",
                TotemCombatHUDForm.FormatCombatLog(new TotemCombatSnapshot { lastAction = "Skill", lastTargetName = "Boss", lastDamage = 45f, lastKilled = true, lastSkillId = "skill_fireball_01" }),
                "combatHud.combatLog.skill");
            var minimapPixels = new Color32[32 * 32];
            var minimapMap = TotemMapService.BuildLayout(seed: 1, themeId: 1);
            var minimapActors = new[]
            {
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 1, Name = "Player", Kind = TotemActorKind.Player, Position = minimapMap.Rooms[0].CenterWorld, MaxHealth = 100f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 2, Name = "LightBot", Kind = TotemActorKind.LightAi, Position = minimapMap.Rooms[1].CenterWorld, MaxHealth = 20f }),
            };
            var minimapEnemies = new[]
            {
                new TotemEnemyModel(1000, "enemy_common_hunter", "Hunter", "common", TotemEnemyTier.Light, 80f, minimapMap.Rooms[2].CenterWorld),
                new TotemEnemyModel(1001, "boss_ai_core_zero", "Core Zero", "ai_ruins", TotemEnemyTier.Boss, 300f, minimapMap.Rooms[3].CenterWorld),
            };
            context.Assert(
                TotemCombatHUDForm.BuildMinimapPixels(
                    minimapPixels,
                    32,
                    minimapMap,
                    minimapActors,
                    new TotemZoneSnapshot { active = true, currentRadius = 30f },
                    minimapEnemies,
                    minimapEnemies.Length),
                "combatHud.minimap.buildPixels");
            context.Assert(TotemCombatHUDForm.CountMinimapPixelsDifferentFromBackground(minimapPixels) > 20, "combatHud.minimap.nonBackgroundPixels");
            context.AssertEqual("skill.skill_phase_dash", TotemCombatHUDForm.GetSkillAssetKey("skill_phase_dash"), "combatHud.dynamicSkillAssetKey");
        }

        private static void CheckStartupPatternUnlocks(GFDiagnosticScenarioContext context)
        {
            context.AssertEqual("ui.character.1", TotemCharacterSelectForm.GetCharacterAssetKey(1), "characterSelect.assetKey.1");
            context.AssertEqual("ui.character.2", TotemCharacterSelectForm.GetCharacterAssetKey(2), "characterSelect.assetKey.2");
            context.AssertEqual("ui.character.3", TotemCharacterSelectForm.GetCharacterAssetKey(3), "characterSelect.assetKey.3");

            var defaultPatternIds = TotemStartupSelectForm.GetUnlockedPatternOptionIds(null);
            context.AssertEqual(2, defaultPatternIds.Length, "startup.patternFallback.count");
            context.Assert(defaultPatternIds[0] == 1 && defaultPatternIds[1] == 2, "Startup pattern fallback should expose Line and Ring.");

            var snapshot = TotemMetaProgressService.CreateDefaultSnapshot();
            snapshot.patternUnlocks = new[]
            {
                new TotemPatternUnlockSnapshot { patternId = "pattern_bolt", slots = new[] { false, true, false, false, false, false } },
                new TotemPatternUnlockSnapshot { patternId = "PATTERN_STAR", slots = new[] { true, false, false, false, false, false } },
                new TotemPatternUnlockSnapshot { patternId = "pattern_line", slots = new[] { false, false, false, false, false, false } },
            };

            var unlockedPatternIds = TotemStartupSelectForm.GetUnlockedPatternOptionIds(snapshot);
            context.AssertEqual(2, unlockedPatternIds.Length, "startup.patternUnlock.count");
            context.Assert(Array.IndexOf(unlockedPatternIds, 5) >= 0, "Startup pattern unlocks should include Bolt.");
            context.Assert(Array.IndexOf(unlockedPatternIds, 6) >= 0, "Startup pattern unlocks should include Star.");
            context.Assert(Array.IndexOf(unlockedPatternIds, 1) < 0, "Startup pattern unlocks should not include locked Line when real unlocks exist.");
        }

        private static void CheckOverlayRuntimeText(GFDiagnosticScenarioContext context)
        {
            var merchant = new TotemNpcModel { NpcId = "merchant_general", Type = TotemNpcType.Merchant, ThemePriceMultiplier = 1.2f };
            var offer = new TotemShopOffer { ItemId = 101, DisplayName = "Red Ink", Price = 30, Stock = 3 };
            context.AssertEqual("Merchant: merchant_general  Price x1.20", TotemShopForm.FormatNpcText(merchant), "shop.npcText");
            context.AssertEqual("Coins: 120", TotemShopForm.FormatInventoryText(120), "shop.inventoryText");
            context.AssertEqual("Red Ink  Price: 36  Stock: 3", TotemShopForm.FormatOfferText(offer, merchant.ThemePriceMultiplier), "shop.offerText");

            var tattooist = new TotemNpcModel { NpcId = "tattooist_default", Type = TotemNpcType.Tattooist, ThemePriceMultiplier = 1.15f };
            var choice = TotemChoiceService.BuildThreeChoices(TotemInteractionService.BuildChoiceEventId(tattooist), 17);
            context.AssertEqual("Tattooist: tattooist_default  Theme x1.15", TotemTattooStudioForm.FormatNpcText(tattooist), "tattoo.npcText");
            context.AssertEqual("Choices: tattoo_tattooist_default  Count: 3", TotemTattooStudioForm.FormatChoiceSummary(choice), "tattoo.choiceSummary");
            context.AssertEqual("Event: tattoo_tattooist_default", TotemThreeChoiceForm.FormatChoiceHeader(choice), "threeChoice.header");
            context.Assert(TotemThreeChoiceForm.FormatChoiceText(choice.Options[0]).Contains(choice.Options[0].DisplayName, StringComparison.Ordinal), "threeChoice.optionText should contain display name.");
            context.Assert(!TotemThreeChoiceForm.AreChoiceButtonsInteractable(2.9f, 0f), "threeChoice.inputGrace.blocked");
            context.Assert(TotemThreeChoiceForm.AreChoiceButtonsInteractable(3.1f, 0f), "threeChoice.inputGrace.unlocked");
            choice.State = TotemChoiceRuntimeState.Showing;
            context.Assert(TotemTattooStudioForm.CanReuseChoice(choice, choice.EventId), "TattooStudio should reuse the current showing choice.");
            choice.State = TotemChoiceRuntimeState.Resolved;
            context.Assert(!TotemTattooStudioForm.CanReuseChoice(choice, choice.EventId), "TattooStudio should not reuse a resolved choice.");
            choice.State = TotemChoiceRuntimeState.Closed;
            context.Assert(!TotemTattooStudioForm.CanReuseChoice(choice, choice.EventId), "TattooStudio should not reuse a closed choice.");
            choice.State = TotemChoiceRuntimeState.Showing;
            context.Assert(!TotemTattooStudioForm.CanReuseChoice(choice, "tattoo_other"), "TattooStudio should not reuse a choice from another event.");

            var combat = new TotemCombatSnapshot { playerHealth = 88f, aliveParticipantCount = 38, aliveEnemyCount = 12, killCount = 4 };
            context.AssertEqual("HP 88  Alive 38  Monsters 12  Kills 4", TotemPauseMenuForm.FormatStatus(combat), "pause.status");

            var runResult = TotemCombatService.BuildRunResult(true, "LastParticipantStanding", 50, 72f, 1, 123.4f);
            context.AssertEqual("Victory", TotemRunResultForm.FormatTitle(runResult), "runResult.title");
            context.Assert(TotemRunResultForm.FormatSummary(runResult).Contains("Kills: 50", StringComparison.Ordinal), "runResult.summary should contain kill count.");

            var settings = new TotemSettingsSnapshot { bgmVolume = 0.7f, sfxVolume = 0.6f, qualityLevel = 2 };
            context.AssertEqual("BGM 0.70  SFX 0.60  Quality 2", TotemSettingsService.FormatSnapshot(settings), "settings.summary");

            context.AssertEqual("Part 2  Color 3  Pattern 4", TotemSelfTattooForm.FormatSelection(2, 3, 4), "selfTattoo.selection");
            var tattooSnapshot = new TotemTattooSnapshot
            {
                equippedCount = 1,
                equippedSummary = "RightArm/Red/Line",
                selfTattooInProgress = true,
                selfTattooRemainingSec = 1.5f,
                pendingSelfTattooSummary = "Part2/Color3/Pattern4",
                enchantedCount = 1,
            };
            context.AssertEqual("Equipped: RightArm/Red/Line", TotemSelfTattooForm.FormatEquipped(tattooSnapshot), "selfTattoo.equipped");
            context.AssertEqual("Reading: Part2/Color3/Pattern4  1.5s", TotemSelfTattooForm.FormatReading(tattooSnapshot), "selfTattoo.reading");
            context.AssertEqual("Equipped: 1  Enchants: 1", TotemTattooEnchantForm.FormatStatus(tattooSnapshot), "tattooEnchant.status");
        }

        private static void AssertUIView(GFDiagnosticScenarioContext context, string viewName, int expectedId)
        {
            if (!Enum.IsDefined(typeof(UIViews), viewName))
            {
                context.Fail($"UIViews must define {viewName}.");
                return;
            }

            int actualId = (int)Enum.Parse(typeof(UIViews), viewName);
            context.AssertEqual(expectedId, actualId, $"UIViews.{viewName}");
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
            }

            return count;
        }

        private static Image FindImage(GameObject root, string childName)
        {
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == childName)
                {
                    return images[i];
                }
            }

            return null;
        }

        private static Transform FindTransform(GameObject root, string childName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == childName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static void AssertSpriteLoads(GFDiagnosticScenarioContext context, TotemAssetService service, string assetKey)
        {
            bool loaded = service.TryLoadSprite(assetKey, out var sprite) && sprite != null;
            context.Detail($"{assetKey}.uiSpriteLoaded", loaded);
            context.Assert(loaded, $"{assetKey} should load for first-slice UI.");
        }

        private static int CountButtonPersistentClicks(GameObject root)
        {
            int count = 0;
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                count += buttons[i].onClick.GetPersistentEventCount();
            }

            return count;
        }

        private readonly struct PrefabContract
        {
            public readonly string PrefabName;
            public readonly Type FormType;

            public PrefabContract(string prefabName, Type formType)
            {
                PrefabName = prefabName;
                FormType = formType;
            }
        }
    }
}
#endif
