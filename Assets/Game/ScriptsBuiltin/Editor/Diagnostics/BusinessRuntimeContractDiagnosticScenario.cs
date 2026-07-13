#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UGF.EditorTools
{
    public sealed class BusinessRuntimeContractDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "GF_X Business Runtime Contract";
        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckBuildSettings(context);
            CheckAppConfigs(context);
            CheckRuntimeTypes(context);
            CheckNoOldRuntimeHosts(context);
            CheckNoDirectInputBypass(context);
            CheckWeaponCatalogBoundary(context);
            CheckPhaseCatalogBoundary(context);
            CheckServiceCatalogCacheBoundary(context);
            CheckNoServiceOwnedShopFallbacks(context);
            CheckNoBossSkillFallbackInAiService(context);
            CheckThreeChoiceUiRejectKeepsChoice(context);
            CheckRuntimeObservabilityDebt(context);
            context.Pass("GF_X business runtime contract is configured.");
        }

        private static void CheckBuildSettings(GFDiagnosticScenarioContext context)
        {
            const string gfLaunchScene = "Assets/Game/Scene/Launch.unity";
            string[] legacyScenes =
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Launch.unity",
                "Assets/Scenes/SampleScene.unity",
            };

            var scenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            bool hasGFLaunch = scenes.Any(scene => scene.enabled && scene.path == gfLaunchScene);
            if (!hasGFLaunch)
            {
                context.Fail($"BuildSettings must enable {gfLaunchScene}.");
            }

            foreach (string legacyScene in legacyScenes)
            {
                if (scenes.Any(scene => scene.enabled && scene.path == legacyScene))
                {
                    context.Fail($"Legacy scene must not be enabled in BuildSettings: {legacyScene}");
                }
            }

            context.Detail("buildScenes", string.Join(",", scenes.Select(scene => $"{scene.path}|enabled={scene.enabled}")));
        }

        private static void CheckAppConfigs(GFDiagnosticScenarioContext context)
        {
            var appConfig = AppConfigs.ReloadInstanceEditor();
            if (appConfig == null)
            {
                context.Fail("AppConfigs asset can not be loaded.");
                return;
            }

            string[] procedures = appConfig.Procedures ?? Array.Empty<string>();
            string[] required =
            {
                "PreloadProcedure",
                "WorkspaceProcedure",
                "TotemGameProcedure",
            };

            foreach (string procedure in required)
            {
                if (!procedures.Contains(procedure, StringComparer.Ordinal))
                {
                    context.Fail($"AppConfigs must include procedure: {procedure}");
                }
            }

            context.Detail("procedures", string.Join(",", procedures));
        }

        private static void CheckRuntimeTypes(GFDiagnosticScenarioContext context)
        {
            string[] requiredTypes =
            {
                "WorkspaceProcedure",
                "TotemGameProcedure",
                "TotemGameRuntime",
                "ITotemRuntimeService",
                "TotemRuntimeServiceBase",
                "TotemGameFlowService",
                "TotemMatchClockService",
                "ITotemInputProvider",
                "TotemInputService",
                "TotemDataService",
                "TotemAssetService",
                "TotemSettingsService",
                "TotemAudioService",
                "TotemMetaProgressService",
                "TotemMapService",
                "TotemCombatRelationshipService",
                "TotemActorService",
                "TotemParticipantReadinessService",
                "TotemEconomyService",
                "TotemStatusService",
                "TotemTattooService",
                "TotemWeaponService",
                "TotemChestService",
                "TotemSkillService",
                "TotemZoneService",
                "TotemAIService",
                "TotemNpcService",
                "TotemChoiceService",
                "TotemInteractionService",
                "TotemCameraService",
                "TotemVfxService",
                "TotemEnemyWorldService",
                "TotemEnemyService",
                "TotemEnemyLootService",
                "TotemCombatService",
                "TotemUIService",
            };

            foreach (string typeName in requiredTypes)
            {
                if (ResolveType(typeName) == null)
                {
                    context.Fail($"Type can not be resolved: {typeName}");
                }
            }
        }

        private static void CheckNoOldRuntimeHosts(GFDiagnosticScenarioContext context)
        {
            const string gfRuntimeRoot = "Assets/Game/Scripts";
            string[] forbiddenTokens =
            {
                "GameApp",
                "ModuleRunner",
                "EventBus",
                "UIModule",
                "DataTableModule",
            };

            if (!Directory.Exists(gfRuntimeRoot))
            {
                context.Fail($"GF_X runtime script root does not exist: {gfRuntimeRoot}");
                return;
            }

            var hits = new List<string>();
            foreach (string file in Directory.GetFiles(gfRuntimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = StripCSharpComments(File.ReadAllText(file));
                foreach (string forbiddenToken in forbiddenTokens)
                {
                    if (text.Contains(forbiddenToken, StringComparison.Ordinal))
                    {
                        hits.Add($"{file.Replace('\\', '/')}:{forbiddenToken}");
                    }
                }
            }

            context.Detail("oldRuntimeHostHits", hits.Count);
            if (hits.Count > 0)
            {
                context.Fail("GF_X runtime scripts must not directly reference old runtime hosts: " + string.Join(",", hits));
            }
        }

        private static string StripCSharpComments(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"^\s*//.*$", string.Empty, RegexOptions.Multiline);
        }

        private static void CheckNoDirectInputBypass(GFDiagnosticScenarioContext context)
        {
            const string gfRuntimeRoot = "Assets/Game/Scripts";
            const string allowedInputProviderFile = "Assets/Game/Scripts/Runtime/Services/TotemInputService.cs";

            if (!Directory.Exists(gfRuntimeRoot))
            {
                context.Fail($"GF_X runtime script root does not exist: {gfRuntimeRoot}");
                return;
            }

            var hits = new List<string>();
            foreach (string file in Directory.GetFiles(gfRuntimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                string text = File.ReadAllText(file);
                if (!text.Contains("Input.Get", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(normalized, allowedInputProviderFile, StringComparison.Ordinal))
                {
                    continue;
                }

                hits.Add(normalized);
            }

            context.Detail("directInputBypassHits", hits.Count);
            if (hits.Count > 0)
            {
                context.Fail("Input.Get* calls must stay behind TotemInputService provider boundary: " + string.Join(",", hits));
            }
        }

        private static void CheckWeaponCatalogBoundary(GFDiagnosticScenarioContext context)
        {
            const string weaponServiceFile = "Assets/Game/Scripts/Runtime/Services/TotemWeaponService.cs";

            if (!File.Exists(weaponServiceFile))
            {
                context.Fail($"Weapon service file does not exist: {weaponServiceFile}");
                return;
            }

            string text = File.ReadAllText(weaponServiceFile);
            string[] forbiddenSnippets =
            {
                "private static readonly TotemWeaponDefinition[]",
                "private static readonly TotemProjectileDefinition[]",
                "private static readonly TotemWeaponTraitDefinition[]",
            };

            var hits = forbiddenSnippets
                .Where(snippet => text.Contains(snippet, StringComparison.Ordinal))
                .ToArray();

            context.Detail("weaponServiceStaticCatalogHits", hits.Length);
            if (hits.Length > 0)
            {
                context.Fail("TotemWeaponService must load weapon/projectile/trait catalogs through TotemDataService instead of keeping hidden static tables: " + string.Join(",", hits));
            }
        }

        private static void CheckPhaseCatalogBoundary(GFDiagnosticScenarioContext context)
        {
            var checks = new[]
            {
                new
                {
                    File = "Assets/Game/Scripts/Runtime/Services/TotemZoneService.cs",
                    Snippet = "private static readonly TotemZonePhase[]",
                },
            };

            var hits = new List<string>();
            foreach (var check in checks)
            {
                if (!File.Exists(check.File))
                {
                    context.Fail($"Runtime service file does not exist: {check.File}");
                    continue;
                }

                string text = File.ReadAllText(check.File);
                if (text.Contains(check.Snippet, StringComparison.Ordinal))
                {
                    hits.Add($"{check.File}:{check.Snippet}");
                }
            }

            context.Detail("phaseServiceStaticCatalogHits", hits.Count);
            if (hits.Count > 0)
            {
                context.Fail("Zone/Boss phase services must load migrated phase catalogs through TotemDataService instead of keeping hidden static phase tables: " + string.Join(",", hits));
            }
        }

        private static void CheckServiceCatalogCacheBoundary(GFDiagnosticScenarioContext context)
        {
            var checks = new[]
            {
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemActorService.cs", Snippet = "private static readonly TotemEnemyDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemSkillService.cs", Snippet = "private static readonly TotemSkillDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemChoiceService.cs", Snippet = "private static readonly TotemChoiceOption[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemChoiceService.cs", Snippet = "private static readonly TotemGameplayEventDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemMapService.cs", Snippet = "private static readonly TotemMapTemplateDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemTattooService.cs", Snippet = "private static readonly TotemTattooDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemTattooService.cs", Snippet = "private static readonly TotemTattooReadingTimeDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemTattooService.cs", Snippet = "private static readonly TotemTattooEnchantAffixDefinition[]" },
                new { File = "Assets/Game/Scripts/Runtime/Services/TotemTattooService.cs", Snippet = "private static readonly TotemTattooEnchantRecipeDefinition[]" },
            };

            var hits = new List<string>();
            foreach (var check in checks)
            {
                if (!File.Exists(check.File))
                {
                    context.Fail($"Runtime service file does not exist: {check.File}");
                    continue;
                }

                string text = File.ReadAllText(check.File);
                if (text.Contains(check.Snippet, StringComparison.Ordinal))
                {
                    hits.Add($"{check.File}:{check.Snippet}");
                }
            }

            context.Detail("serviceStaticCatalogCacheHits", hits.Count);
            if (hits.Count > 0)
            {
                context.Fail("Runtime services must not cache gameplay catalogs in private static readonly arrays; load through TotemDataService or runtime-injected catalog instead: " + string.Join(",", hits));
            }
        }

        private static void CheckNoServiceOwnedShopFallbacks(GFDiagnosticScenarioContext context)
        {
            const string npcServiceFile = "Assets/Game/Scripts/Runtime/Services/TotemNpcService.cs";

            if (!File.Exists(npcServiceFile))
            {
                context.Fail($"NPC service file does not exist: {npcServiceFile}");
                return;
            }

            string text = File.ReadAllText(npcServiceFile);
            string[] forbiddenSnippets =
            {
                "BuildGeneralShop",
                "BuildRareShop",
                "ItemId ==",
                "offer.ItemId ==",
            };

            var hits = forbiddenSnippets
                .Where(snippet => text.Contains(snippet, StringComparison.Ordinal))
                .ToArray();

            context.Detail("npcServiceShopFallbackHits", hits.Length);
            if (hits.Length > 0)
            {
                context.Fail("TotemNpcService must route shop reward metadata through gameplay catalog fields instead of service-owned shop tables or item-id inference: " + string.Join(",", hits));
            }
        }

        private static void CheckNoBossSkillFallbackInAiService(GFDiagnosticScenarioContext context)
        {
            const string aiServiceFile = "Assets/Game/Scripts/Runtime/Services/TotemAIService.cs";

            if (!File.Exists(aiServiceFile))
            {
                context.Fail($"AI service file does not exist: {aiServiceFile}");
                return;
            }

            string text = File.ReadAllText(aiServiceFile);
            bool hasFallback = text.Contains("boss_phase_bolt", StringComparison.Ordinal);
            context.Detail("aiServiceBossSkillFallbackHits", hasFallback ? 1 : 0);
            if (hasFallback)
            {
                context.Fail("TotemAIService must use EnemyService Boss definitions instead of hardcoding a fallback Boss skill.");
            }
        }

        private static void CheckThreeChoiceUiRejectKeepsChoice(GFDiagnosticScenarioContext context)
        {
            const string threeChoiceFormFile = "Assets/Game/Scripts/UI/TotemThreeChoiceForm.cs";

            if (!File.Exists(threeChoiceFormFile))
            {
                context.Fail($"ThreeChoice form file does not exist: {threeChoiceFormFile}");
                return;
            }

            string text = File.ReadAllText(threeChoiceFormFile);
            bool hasRejectedRefresh = text.Contains("ThreeChoice.ApplyRejected", StringComparison.Ordinal) &&
                text.Contains("BuildView();", StringComparison.Ordinal);
            bool hasUnconditionalCloseAfterRejectedBranch = text.Contains("BuildView();\r\n        }\r\n\r\n        OnClickClose();", StringComparison.Ordinal) ||
                text.Contains("BuildView();\n        }\n\n        OnClickClose();", StringComparison.Ordinal);

            context.Detail("threeChoiceUiRejectedRefresh", hasRejectedRefresh);
            context.Detail("threeChoiceUiUnconditionalCloseAfterReject", hasUnconditionalCloseAfterRejectedBranch);
            if (!hasRejectedRefresh || hasUnconditionalCloseAfterRejectedBranch)
            {
                context.Fail("ThreeChoice UI must keep the current choice visible when ApplyChoice rejects a selected option.");
            }
        }

        private static void CheckRuntimeObservabilityDebt(GFDiagnosticScenarioContext context)
        {
            const string runtimeRoot = "Assets/Game/Scripts";
            const string changeSceneProcedureFile = "Assets/Game/Scripts/Procedures/ChangeSceneProcedure.cs";
            const string soundExtensionFile = "Assets/Game/Scripts/Extension/SoundExtension.cs";

            var todoHits = new List<string>();
            foreach (string file in Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                string text = File.ReadAllText(file);
                if (text.Contains("TODO", StringComparison.Ordinal) || text.Contains("FIXME", StringComparison.Ordinal))
                {
                    todoHits.Add(normalized);
                }
            }

            context.Detail("runtimeTodoHits", todoHits.Count);
            if (todoHits.Count > 0)
            {
                context.Fail("Active GF_X runtime scripts must not keep TODO/FIXME debt: " + string.Join(",", todoHits));
            }

            if (!File.Exists(changeSceneProcedureFile))
            {
                context.Fail($"ChangeSceneProcedure file does not exist: {changeSceneProcedureFile}");
            }
            else
            {
                string text = File.ReadAllText(changeSceneProcedureFile);
                context.Assert(text.Contains("ShowLoadingProgress(0f)", StringComparison.Ordinal), "ChangeSceneProcedure must show loading progress before loading a scene.");
                context.Assert(text.Contains("SetLoadingProgress(arg.Progress)", StringComparison.Ordinal), "ChangeSceneProcedure must update loading progress from LoadSceneUpdateEventArgs.");
                context.Assert(text.Contains("\"Load.Progress\"", StringComparison.Ordinal), "ChangeSceneProcedure must trace scene loading progress for diagnostics.");
            }

            if (!File.Exists(soundExtensionFile))
            {
                context.Fail($"SoundExtension file does not exist: {soundExtensionFile}");
            }
            else
            {
                string text = File.ReadAllText(soundExtensionFile);
                context.Assert(text.Contains("GFBuiltin.Resource.HasAsset(assetName)", StringComparison.Ordinal), "SoundExtension must guard missing sound assets before PlaySound.");
                context.Assert(text.Contains("\"PlaySound.MissingAsset\"", StringComparison.Ordinal), "SoundExtension must trace missing sound asset attempts for diagnostics.");
            }
        }

        private static Type ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(candidate => candidate != null);
        }
    }
}
#endif
