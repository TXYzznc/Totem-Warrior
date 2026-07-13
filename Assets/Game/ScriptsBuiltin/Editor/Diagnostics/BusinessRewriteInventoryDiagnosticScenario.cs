#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UGF.EditorTools
{
    public sealed class BusinessRewriteInventoryDiagnosticScenario : GFDiagnosticScenarioBase
    {
        private const string RequirementsInventoryPath = "openspec/changes/gf-x-business-runtime-refactor/REQUIREMENTS_INVENTORY.md";
        private const string LegacyEffectCoveragePath = "openspec/changes/gf-x-business-runtime-refactor/LEGACY_EFFECT_COVERAGE.md";
        private const string LegacyOpenSpecStatusPath = "openspec/changes/gf-x-business-runtime-refactor/LEGACY_OPENSPEC_SPEC_STATUS.md";
        private const string CompletionAuditPath = "openspec/changes/gf-x-business-runtime-refactor/COMPLETION_AUDIT.md";
        private const string DecisionsNeededPath = "openspec/changes/gf-x-business-runtime-refactor/DECISIONS_NEEDED.md";
        private const string RefactorTasksPath = "openspec/changes/gf-x-business-runtime-refactor/tasks.md";
        private const string ReadmePath = "README.md";
        private const string ClaudeGuidePath = ".claude/CLAUDE.md";
        private const string CodexGuidePath = "AGENTS.md";
        private const string PlaytestDriverSkillPath = ".claude/skills/playtest-driver/SKILL.md";
        private const string PlaytestReportTemplatePath = "tools/playtest/reports/_TEMPLATE.md";
        private const string ActivePlaytestReportsPath = "tools/playtest/reports";
        private const string ArchivedLegacyPlaytestReportsPath = "LegacyProjectArchive/tools/playtest/reports/old-runtime";
        private const string RefactorTestPlanPath = "openspec/changes/gf-x-business-runtime-refactor/tests/plan.md";
        private const string ProjectMapPath = "项目知识库（AI自行维护）/wiki/PROJECT_MAP.md";
        private const string ActiveContextPath = "项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md";
        private const string DataTableMigrationManifestPath = "openspec/changes/gf-x-business-runtime-refactor/DATATABLE_MIGRATION_MANIFEST.md";
        private const string ToolMigrationManifestPath = "openspec/changes/gf-x-business-runtime-refactor/GF_X_TOOL_MIGRATION_MANIFEST.md";
        private const string GameplayRuntimeSlicePath = "openspec/changes/gf-x-business-runtime-refactor/GAMEPLAY_RUNTIME_SLICE.md";
        private const string PlayModeLaunchSmokeReportPath = "tools/playtest/reports/2026-07-08-0404-PM-04-gf-x-launch-after-prefab-cleanup.md";
        private const string PlayModeCombatHudSmokeReportPath = "tools/playtest/reports/2026-07-09-1225-PM-05-combathud-input-playmode.md";
        private const string PlayModeCombatHudSmokeXmlPath = "tools/playtest/test-results/2026-07-09-1225-PM-05-combathud-input-playmode.xml";
        private const string ArtAssetsManifestPath = "项目知识库（AI自行维护）/wiki/manifests/art_assets.json";
        private const string DataTablesManifestPath = "项目知识库（AI自行维护）/wiki/manifests/datatables.json";
        private const string FeatureSlicesManifestPath = "项目知识库（AI自行维护）/wiki/manifests/feature_slices.json";
        private const string DiagnosticTriageManifestPath = "项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json";
        private const string RuntimeAssetCatalogPath = "GameData/AIData/GameplayCatalogs/totem_runtime_assets.json";
        private const string ActiveLegacyScriptPath = "Assets/Scripts";
        private const string ArchivedLegacyScriptPath = "LegacyProjectArchive/Assets/Scripts";
        private const string ArchivedLegacyModulesPath = "LegacyProjectArchive/Assets/Scripts/Modules";
        private const string ActiveLegacyTestsPath = "Assets/Tests";
        private const string ArchivedLegacyTestsPath = "LegacyProjectArchive/Assets/Tests";
        private const string ActiveLegacyPlaytestEditorPath = "Assets/Editor/Playtest";
        private const string ArchivedLegacyPlaytestEditorPath = "LegacyProjectArchive/Assets/Editor/Playtest";
        private const string NativePlaytestDriverPath = "Assets/Game/ScriptsBuiltin/Editor/Playtest/TotemPlaytestDriverEditor.cs";
        private const string ActiveLegacyCharacterEditorPath = "Assets/Editor/Character";
        private const string ArchivedLegacyCharacterEditorPath = "LegacyProjectArchive/Assets/Editor/Character";
        private const string ActiveLegacyDataTablePath = "Assets/Resources/DataTable";
        private const string ArchivedLegacyDataTablePath = "LegacyProjectArchive/Assets/Resources/DataTable";
        private const string BusinessAIDataTablePath = "GameData/AIData/DataTables/Business";
        private const string BusinessXlsxDataTablePath = "GameData/DataTables/Business";
        private const string LegacyUIPrefabPath = "Assets/Resources/Prefab/UI";

        public override string Name => "GF_X Rewrite Inventory Contract";
        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckDocuments(context);
            CheckLegacyEvidenceInventory(context);
            CheckNativeRuntimeSkeleton(context);
            context.Pass("GF_X rewrite inventory contract is recorded.");
        }

        private static void CheckDocuments(GFDiagnosticScenarioContext context)
        {
            context.Detail("requirementsInventory", File.Exists(RequirementsInventoryPath));
            context.Detail("legacyEffectCoverage", File.Exists(LegacyEffectCoveragePath));
            context.Detail("legacyOpenSpecStatus", File.Exists(LegacyOpenSpecStatusPath));
            context.Detail("completionAudit", File.Exists(CompletionAuditPath));
            context.Detail("decisionsNeeded", File.Exists(DecisionsNeededPath));
            context.Detail("refactorTasks", File.Exists(RefactorTasksPath));
            context.Detail("readme", File.Exists(ReadmePath));
            context.Detail("claudeGuide", File.Exists(ClaudeGuidePath));
            context.Detail("codexGuide", File.Exists(CodexGuidePath));
            context.Detail("playtestDriverSkill", File.Exists(PlaytestDriverSkillPath));
            context.Detail("refactorTestPlan", File.Exists(RefactorTestPlanPath));
            context.Detail("projectMap", File.Exists(ProjectMapPath));
            context.Detail("activeContext", File.Exists(ActiveContextPath));
            context.Detail("dataTableMigrationManifest", File.Exists(DataTableMigrationManifestPath));
            context.Detail("toolMigrationManifest", File.Exists(ToolMigrationManifestPath));
            context.Detail("gameplayRuntimeSlice", File.Exists(GameplayRuntimeSlicePath));
            context.Detail("playModeLaunchSmokeReport", File.Exists(PlayModeLaunchSmokeReportPath));
            context.Detail("playModeCombatHudSmokeReport", File.Exists(PlayModeCombatHudSmokeReportPath));
            context.Detail("playModeCombatHudSmokeXml", File.Exists(PlayModeCombatHudSmokeXmlPath));
            context.Detail("artAssetsManifest", File.Exists(ArtAssetsManifestPath));
            context.Detail("dataTablesManifest", File.Exists(DataTablesManifestPath));
            context.Detail("featureSlicesManifest", File.Exists(FeatureSlicesManifestPath));
            context.Detail("diagnosticTriageManifest", File.Exists(DiagnosticTriageManifestPath));
            context.Detail("runtimeAssetCatalog", File.Exists(RuntimeAssetCatalogPath));
            context.RequireFile(RequirementsInventoryPath);
            context.RequireFile(LegacyEffectCoveragePath);
            context.RequireFile(LegacyOpenSpecStatusPath);
            context.RequireFile(CompletionAuditPath);
            context.RequireFile(DecisionsNeededPath);
            context.RequireFile(RefactorTasksPath);
            context.RequireFile(ReadmePath);
            context.RequireFile(ClaudeGuidePath);
            context.RequireFile(CodexGuidePath);
            context.RequireFile(PlaytestDriverSkillPath);
            context.RequireFile(RefactorTestPlanPath);
            context.RequireFile(ProjectMapPath);
            context.RequireFile(ActiveContextPath);
            context.RequireFile(DataTableMigrationManifestPath);
            context.RequireFile(ToolMigrationManifestPath);
            context.RequireFile(GameplayRuntimeSlicePath);
            context.RequireFile(PlayModeLaunchSmokeReportPath);
            context.RequireFile(PlayModeCombatHudSmokeReportPath);
            context.RequireFile(PlayModeCombatHudSmokeXmlPath);
            context.RequireFile(ArtAssetsManifestPath);
            context.RequireFile(DataTablesManifestPath);
            context.RequireFile(FeatureSlicesManifestPath);
            context.RequireFile(DiagnosticTriageManifestPath);
            context.RequireFile(RuntimeAssetCatalogPath);
            context.RequireFile(NativePlaytestDriverPath);

            string requirementsText = File.ReadAllText(RequirementsInventoryPath);
            context.Assert(requirementsText.Contains("Status: current confirmed inventory", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must be the current confirmed inventory, not an early draft.");
            context.Assert(requirementsText.Contains("LegacyProjectArchive/Assets/Scripts/**", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must point old code evidence at LegacyProjectArchive.");
            context.Assert(!requirementsText.Contains("Status: draft", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must not remain in draft state.");
            context.Assert(!requirementsText.Contains("Old implementation evidence: `Assets/Scripts/**`", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must not point old code evidence at active Assets/Scripts.");
            context.Assert(!requirementsText.Contains("Needs user confirmation", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must record current decisions instead of stale pending-confirmation columns.");
            context.Assert(requirementsText.Contains("T9f7", System.StringComparison.Ordinal), "REQUIREMENTS_INVENTORY.md must record the deferred final tuning boundary.");

            string decisionsText = File.ReadAllText(DecisionsNeededPath);
            int decisionCount = decisionsText.Split('\n').Count(line => line.StartsWith("## D"));
            context.Detail("decisionEntryCount", decisionCount);
            context.Assert(decisionCount >= 9, "DECISIONS_NEEDED.md should list the current uncertainty set.");
            context.Assert(!decisionsText.Contains("For non-UI tests, what should count as enough", System.StringComparison.Ordinal), "DECISIONS_NEEDED.md must not keep stale open testing questions.");
            context.Assert(!decisionsText.Contains("runtime implementation is next", System.StringComparison.Ordinal), "DECISIONS_NEEDED.md must not say Smart AI runtime implementation is still next.");
            context.Assert(decisionsText.Contains("Status: confirmed and executed for the current migration pass.", System.StringComparison.Ordinal), "DECISIONS_NEEDED.md must record the current testing acceptance decision.");
            context.Assert(decisionsText.Contains("Status: confirmed and implemented for the current first-round runtime.", System.StringComparison.Ordinal), "DECISIONS_NEEDED.md must record the implemented Smart AI decision.");

            CheckDiagnosticsWorkflowEntryPoints(context);
            CheckGameplayRuntimeSliceFreshness(context);
            CheckActiveChangeDocumentEncoding(context);
            CheckActivePlaytestReportArchive(context);
            CheckArtAssetRuntimeUsageIndex(context);
            CheckFeatureSliceManifest(context);
            CheckDiagnosticTriageManifest(context);

            string completionAuditText = File.ReadAllText(CompletionAuditPath);
            context.Assert(completionAuditText.Contains("Requirement Audit"), "Completion audit must include a requirement audit section.");
            context.Assert(completionAuditText.Contains("Accepted Later Boundaries"), "Completion audit must state accepted later boundaries.");
            context.Assert(completionAuditText.Contains("T9f7"), "Completion audit must reference the deferred final tuning boundary.");
            context.Assert(completionAuditText.Contains("First-round objective closed", StringComparison.Ordinal), "Completion audit must state the current first-round closure vocabulary.");
            context.Assert(!completionAuditText.Contains("full objective remains active", StringComparison.Ordinal), "Completion audit must not keep the old active-objective wording after closure.");
            context.Assert(!completionAuditText.Contains("gf-diagnostics-run-all_20260708_163725.json", System.StringComparison.Ordinal), "Completion audit must not use the old dependency-boundary report as the current non-UI test proof.");

            string auditDiagnosticsReportPath = GetCompletionAuditDiagnosticsReportPath(completionAuditText);
            context.Detail("completionAudit.editModeDiagnosticsReport", auditDiagnosticsReportPath);
            context.Assert(!string.IsNullOrWhiteSpace(auditDiagnosticsReportPath), "Completion audit must reference an EditMode diagnostics report path.");
            context.RequireFile(auditDiagnosticsReportPath);
            string latestEditModeDiagnosticsText = File.ReadAllText(auditDiagnosticsReportPath);
            int editModeSuccessCount = ReadDiagnosticCounter(latestEditModeDiagnosticsText, "successCount");
            int editModeFailureCount = ReadDiagnosticCounter(latestEditModeDiagnosticsText, "failureCount");
            int editModeWarningCount = ReadDiagnosticCounter(latestEditModeDiagnosticsText, "warningCount");
            context.Detail("completionAudit.editModeDiagnostics.successCount", editModeSuccessCount);
            context.Detail("completionAudit.editModeDiagnostics.failureCount", editModeFailureCount);
            context.Detail("completionAudit.editModeDiagnostics.warningCount", editModeWarningCount);
            context.Assert(editModeSuccessCount >= 27, "Latest EditMode diagnostics report must keep at least 27 successful scenarios.");
            context.AssertEqual(0, editModeFailureCount, "completionAudit.editModeDiagnostics.failureCount");
            context.AssertEqual(0, editModeWarningCount, "completionAudit.editModeDiagnostics.warningCount");

            string coverageText = File.ReadAllText(LegacyEffectCoveragePath);
            string[] legacyModules =
            {
                "Audio",
                "Bot",
                "Camera",
                "Combat",
                "DataTable",
                "Economy",
                "Enemy",
                "Event",
                "Flow",
                "GameState",
                "Input",
                "MapGen",
                "NPC",
                "Resource",
                "Save",
                "Scene",
                "Settings",
                "Skill",
                "Spawner",
                "Status",
                "Tattoo",
                "UI",
                "VFX",
                "Weapon",
            };

            int coveredModuleCount = 0;
            foreach (string module in legacyModules)
            {
                if (coverageText.Contains($"| {module} |"))
                {
                    coveredModuleCount++;
                    continue;
                }

                context.Fail($"LEGACY_EFFECT_COVERAGE.md must include legacy module row: {module}");
            }

            context.Detail("legacyEffectCoverage.moduleCount", coveredModuleCount);
            context.Assert(coverageText.Contains("Accepted later boundaries"), "Legacy effect coverage must state accepted later boundaries instead of implying complete parity.");
            context.Assert(!coverageText.Contains("full objective remains active", StringComparison.Ordinal), "Legacy effect coverage must not keep the old active-objective wording after closure.");
            CheckCoverageMatrixStateConsistency(context, coverageText);
            CheckCoverageEvidenceReferences(context, coverageText);
            CheckPlayModeLaunchSmokeEvidence(context, coverageText);
            CheckPlayModeCombatHudSmokeEvidence(context, coverageText);
            CheckCoverageBoundaryClassification(context, coverageText, legacyModules);
            CheckLegacyOpenSpecStatus(context);
        }

        private static void CheckDiagnosticsWorkflowEntryPoints(GFDiagnosticScenarioContext context)
        {
            string readmeText = File.ReadAllText(ReadmePath);
            string claudeGuideText = File.ReadAllText(ClaudeGuidePath);
            string codexGuideText = File.ReadAllText(CodexGuidePath);
            string playtestDriverText = File.ReadAllText(PlaytestDriverSkillPath);
            string playtestReportTemplateText = File.ReadAllText(PlaytestReportTemplatePath);
            string refactorTestPlanText = File.ReadAllText(RefactorTestPlanPath);
            string projectMapText = File.ReadAllText(ProjectMapPath);
            string activeContextText = File.ReadAllText(ActiveContextPath);

            context.Assert(readmeText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "README.md must point AI diagnostics at totem_diagnostics_run_all.");
            context.Assert(readmeText.Contains("GameData/AIData/DataTables/Business", System.StringComparison.Ordinal), "README.md must document Business AI JSON as the editable config source.");
            context.Assert(readmeText.Contains("GameData/DataTables/Business", System.StringComparison.Ordinal), "README.md must document Business xlsx as the planner-readable config table path.");
            context.Assert(readmeText.Contains("totem_gameplay_catalog.json", System.StringComparison.Ordinal), "README.md must document the runtime gameplay catalog path.");
            context.Assert(claudeGuideText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), ".claude/CLAUDE.md must point AI diagnostics at totem_diagnostics_run_all.");
            context.Assert(codexGuideText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "AGENTS.md must point Codex diagnostics at totem_diagnostics_run_all.");
            context.Assert(playtestDriverText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "playtest-driver skill must document the stable Totem diagnostics UnitySkill.");
            context.Assert(playtestDriverText.Contains("ExternalAudioDeviceNoise", System.StringComparison.Ordinal), "playtest-driver skill must classify known FMOD output-device noise separately from project errors.");
            context.Assert(playtestDriverText.Contains("FMOD failed to switch back to normal output", System.StringComparison.Ordinal), "playtest-driver skill must name the raw FMOD output-device noise signature.");
            context.Assert(playtestDriverText.Contains("filtered project Error/Exception", System.StringComparison.Ordinal), "playtest-driver skill must distinguish raw Console errors from filtered project errors.");
            context.Assert(playtestReportTemplateText.Contains("ExternalAudioDeviceNoise", System.StringComparison.Ordinal), "Playtest report template must include the FMOD external audio noise classification.");
            context.Assert(playtestReportTemplateText.Contains("Filtered project Error/Exception count", System.StringComparison.Ordinal), "Playtest report template must require filtered project error counts.");
            context.Assert(refactorTestPlanText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "Refactor test plan must use the stable Totem diagnostics UnitySkill.");
            context.Assert(projectMapText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "PROJECT_MAP.md must point AI diagnostics at totem_diagnostics_run_all.");
            context.Assert(activeContextText.Contains("totem_diagnostics_run_all", System.StringComparison.Ordinal), "ACTIVE_CONTEXT.md must point AI diagnostics at totem_diagnostics_run_all.");
            context.Assert(!readmeText.Contains("Unity 内诊断菜单：", System.StringComparison.Ordinal), "README.md must not present the Unity menu as the AI diagnostics entry.");
            context.Assert(!readmeText.Contains("正式 xlsx/GF_X DataTable 工作流后续再定", System.StringComparison.Ordinal), "README.md must not keep stale wording that the xlsx/GF_X DataTable workflow is undecided.");
            context.Assert(!readmeText.Contains("当前先使用 AI 友好的 JSON catalog 驱动玩法", System.StringComparison.Ordinal), "README.md must not keep stale catalog-only workflow wording.");
            context.Assert(!playtestDriverText.Contains("Game Framework/GameTools/Diagnostics/Run All\"}' | us editor_execute_menu", System.StringComparison.Ordinal), "playtest-driver skill must not route GF_X full diagnostics through editor_execute_menu.");
            context.Assert(!refactorTestPlanText.Contains("UGF.EditorTools.GFDiagnosticRunner.RunAll", System.StringComparison.Ordinal), "Refactor test plan must not ask AI verification to call GFDiagnosticRunner.RunAll directly.");
            context.Assert(!refactorTestPlanText.Contains("执行 `Game Framework/GameTools/Diagnostics/Run All`", System.StringComparison.Ordinal), "Refactor test plan must not route GF_X full diagnostics through the Unity menu path.");
        }

        private static void CheckGameplayRuntimeSliceFreshness(GFDiagnosticScenarioContext context)
        {
            string text = File.ReadAllText(GameplayRuntimeSlicePath);
            context.Assert(!text.Contains("xlsx/DataTable promotion is still pending", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must not say Business xlsx/DataTable promotion is still pending.");
            context.Assert(!text.Contains("pending table-scope confirmation", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must not keep stale table-scope confirmation wording.");
            context.Assert(!text.Contains("## Remaining Work", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must not keep the old remaining-work section after closure.");
            context.Assert(text.Contains("## Closed Baseline And Accepted Later Work", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must state the closed baseline and accepted later work section.");
            context.Assert(!text.Contains("map size is 150", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must not keep the old 150m map-size verification wording.");
            context.Assert(!text.Contains("7 Smart profiles + 3 Light profiles", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must not keep the old 10-profile AI wording.");
            context.Assert(text.Contains("map size is 400", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must describe the current fixed 400m map-size verification.");
            context.Assert(text.Contains("20 Smart profiles + 3 Light profiles", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must describe the current confirmed 20 Smart + 3 Light profile set.");
            context.Assert(text.Contains("28 Business AI DataTable manifests", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must describe the current Business AI DataTable source.");
            context.Assert(text.Contains("Business xlsx files are synchronized", StringComparison.Ordinal), "GAMEPLAY_RUNTIME_SLICE.md must describe the current JSON -> xlsx sync state.");
        }

        private static void CheckArtAssetRuntimeUsageIndex(GFDiagnosticScenarioContext context)
        {
            string artAssetsText = File.ReadAllText(ArtAssetsManifestPath);
            context.Detail("artAssetsIndexed", artAssetsText.Contains("\"assets\""));
            context.Assert(artAssetsText.Contains("\"runtime_asset_catalog\"", StringComparison.Ordinal), "art_assets.json must include runtime asset catalog linkage metadata.");
            context.Assert(artAssetsText.Contains("\"runtime_usages\"", StringComparison.Ordinal), "art_assets.json must include per-asset runtime_usages.");
            context.Assert(artAssetsText.Contains("\"ui_form_usage_summary\"", StringComparison.Ordinal), "art_assets.json must include UI form usage summary metadata.");
            context.Assert(artAssetsText.Contains("\"ui_form_usages\"", StringComparison.Ordinal), "art_assets.json must include per-asset ui_form_usages.");
            context.Assert(artAssetsText.Contains("\"usage_guidance\"", StringComparison.Ordinal), "art_assets.json must include per-asset usage_guidance for AI asset decisions.");
            context.Assert(artAssetsText.Contains("\"usage_status\"", StringComparison.Ordinal), "art_assets.json must include per-asset usage_status for AI asset decisions.");

            var root = JObject.Parse(artAssetsText);
            var assets = root["assets"] as JArray;
            var runtimeCatalog = root["runtime_asset_catalog"] as JObject;
            var uiFormUsageSummary = root["ui_form_usage_summary"] as JObject;
            var usageStatusCounts = root["usage_status_counts"] as JObject;
            var usageStatusLegend = root["usage_status_legend"] as JObject;
            var reviewStateCounts = root["review_state_counts"] as JObject;
            var systemCounts = root["system_counts"] as JObject;
            context.Assert(assets != null, "art_assets.json assets must be a JSON array.");
            context.Assert(runtimeCatalog != null, "art_assets.json runtime_asset_catalog must be a JSON object.");
            context.Assert(uiFormUsageSummary != null, "art_assets.json ui_form_usage_summary must be a JSON object.");
            context.Assert(usageStatusCounts != null, "art_assets.json usage_status_counts must be a JSON object.");
            context.Assert(usageStatusLegend != null, "art_assets.json usage_status_legend must be a JSON object.");
            context.Assert(systemCounts != null, "art_assets.json system_counts must be a JSON object.");

            int assetCount = assets?.Count ?? 0;
            int manifestRuntimeBoundAssetCount = root.Value<int?>("runtime_bound_asset_count") ?? -1;
            int manifestUiFormBoundAssetCount = root.Value<int?>("ui_form_bound_asset_count") ?? -1;
            int runtimeBoundAssetCount = 0;
            int uiFormBoundAssetCount = 0;
            int runtimeUsageRecordCount = 0;
            int uiFormUsageRecordCount = 0;
            int missingSchemaCount = 0;
            int missingUiFormUsageSchemaCount = 0;
            int missingUsageStatusSchemaCount = 0;
            int usageStatusAssetCount = 0;
            int runtimeBoundUsageStatusAssetCount = 0;
            int uiFormBoundUsageStatusAssetCount = 0;

            if (assets != null)
            {
                foreach (var token in assets)
                {
                    var asset = token as JObject;
                    if (asset == null)
                    {
                        continue;
                    }

                    if (asset["runtime_usage_count"] == null || asset["runtime_keys"] == null || asset["runtime_usages"] == null || asset["usage_guidance"] == null)
                    {
                        missingSchemaCount++;
                    }

                    if (asset["ui_form_usage_count"] == null || asset["ui_form_names"] == null || asset["ui_form_prefab_paths"] == null || asset["ui_form_usages"] == null)
                    {
                        missingUiFormUsageSchemaCount++;
                    }

                    if (asset["usage_status"] == null || asset["usage_status_reason"] == null)
                    {
                        missingUsageStatusSchemaCount++;
                    }

                    string usageStatus = asset.Value<string>("usage_status") ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(usageStatus))
                    {
                        usageStatusAssetCount++;
                        if (usageStatus.StartsWith("runtime_bound", StringComparison.Ordinal))
                        {
                            runtimeBoundUsageStatusAssetCount++;
                        }

                        if (usageStatus.StartsWith("ui_form_bound", StringComparison.Ordinal))
                        {
                            uiFormBoundUsageStatusAssetCount++;
                        }
                    }

                    int usageCount = asset.Value<int?>("runtime_usage_count") ?? 0;
                    var usages = asset["runtime_usages"] as JArray;
                    runtimeUsageRecordCount += usages?.Count ?? 0;
                    if (usageCount > 0)
                    {
                        runtimeBoundAssetCount++;
                        context.Assert(usages != null && usages.Count == usageCount, "Asset runtime_usage_count must match runtime_usages length.");
                    }

                    int uiFormUsageCount = asset.Value<int?>("ui_form_usage_count") ?? 0;
                    var uiFormUsages = asset["ui_form_usages"] as JArray;
                    uiFormUsageRecordCount += uiFormUsages?.Count ?? 0;
                    if (uiFormUsageCount > 0)
                    {
                        uiFormBoundAssetCount++;
                        context.Assert(uiFormUsages != null && uiFormUsages.Count == uiFormUsageCount, "Asset ui_form_usage_count must match ui_form_usages length.");
                    }
                }
            }

            int runtimeEntryCount = runtimeCatalog?.Value<int?>("entry_count") ?? -1;
            int activeUsageCount = runtimeCatalog?.Value<int?>("active_usage_count") ?? -1;
            int indexedActiveUsageCount = runtimeCatalog?.Value<int?>("indexed_active_usage_count") ?? -1;
            int missingActivePathCount = (runtimeCatalog?["missing_active_asset_paths"] as JArray)?.Count ?? -1;
            int missingLegacyPathCount = (runtimeCatalog?["missing_legacy_source_paths"] as JArray)?.Count ?? -1;
            int uiFormRowCount = uiFormUsageSummary?.Value<int?>("row_count") ?? -1;
            int uiFormEnabledRowCount = uiFormUsageSummary?.Value<int?>("enabled_row_count") ?? -1;
            int uiFormIndexedAssetPathCount = uiFormUsageSummary?.Value<int?>("indexed_asset_path_count") ?? -1;
            int uiFormIndexedUsageCount = uiFormUsageSummary?.Value<int?>("indexed_usage_count") ?? -1;
            int uiFormMissingActivePathCount = (uiFormUsageSummary?["missing_active_asset_paths"] as JArray)?.Count ?? -1;
            int usageStatusCountTotal = usageStatusCounts?.Properties().Sum(property => property.Value.Value<int?>() ?? 0) ?? -1;
            int usageStatusLegendCount = usageStatusLegend?.Properties().Count() ?? -1;
            int runtimeBoundUsageStatusCount =
                (usageStatusCounts?.Value<int?>("runtime_bound") ?? 0) +
                (usageStatusCounts?.Value<int?>("runtime_bound_placeholder") ?? 0);
            int uiFormBoundUsageStatusCount =
                (usageStatusCounts?.Value<int?>("ui_form_bound") ?? 0) +
                (usageStatusCounts?.Value<int?>("ui_form_bound_placeholder") ?? 0);
            int placeholderReviewStateCount = reviewStateCounts?.Value<int?>("placeholder") ?? -1;
            int placeholderUsageStatusCount =
                (usageStatusCounts?.Value<int?>("placeholder") ?? 0) +
                (usageStatusCounts?.Value<int?>("runtime_bound_placeholder") ?? 0) +
                (usageStatusCounts?.Value<int?>("ui_form_bound_placeholder") ?? 0);
            int unclassifiedSystemCount = systemCounts?.Value<int?>("Unclassified") ?? 0;
            int classificationNeededUsageStatusCount = usageStatusCounts?.Value<int?>("classification_needed") ?? 0;

            context.Detail("artAssets.count", assetCount);
            context.Detail("artAssets.unclassifiedSystemCount", unclassifiedSystemCount);
            context.Detail("artAssets.classificationNeededUsageStatusCount", classificationNeededUsageStatusCount);
            context.Detail("artAssets.runtimeBoundAssetCount", runtimeBoundAssetCount);
            context.Detail("artAssets.runtimeBoundAssetCount.manifest", manifestRuntimeBoundAssetCount);
            context.Detail("artAssets.uiFormBoundAssetCount", uiFormBoundAssetCount);
            context.Detail("artAssets.uiFormBoundAssetCount.manifest", manifestUiFormBoundAssetCount);
            context.Detail("artAssets.runtimeUsageRecordCount", runtimeUsageRecordCount);
            context.Detail("artAssets.uiFormUsageRecordCount", uiFormUsageRecordCount);
            context.Detail("artAssets.runtimeUsageSchemaMissingCount", missingSchemaCount);
            context.Detail("artAssets.uiFormUsageSchemaMissingCount", missingUiFormUsageSchemaCount);
            context.Detail("artAssets.usageStatusSchemaMissingCount", missingUsageStatusSchemaCount);
            context.Detail("artAssets.usageStatusAssetCount", usageStatusAssetCount);
            context.Detail("artAssets.usageStatusCountTotal", usageStatusCountTotal);
            context.Detail("artAssets.usageStatusLegendCount", usageStatusLegendCount);
            context.Detail("artAssets.runtimeBoundUsageStatusAssetCount", runtimeBoundUsageStatusAssetCount);
            context.Detail("artAssets.runtimeBoundUsageStatusCount", runtimeBoundUsageStatusCount);
            context.Detail("artAssets.uiFormBoundUsageStatusAssetCount", uiFormBoundUsageStatusAssetCount);
            context.Detail("artAssets.uiFormBoundUsageStatusCount", uiFormBoundUsageStatusCount);
            context.Detail("artAssets.placeholderReviewStateCount", placeholderReviewStateCount);
            context.Detail("artAssets.placeholderUsageStatusCount", placeholderUsageStatusCount);
            context.Detail("runtimeAssetCatalog.entryCountFromArtIndex", runtimeEntryCount);
            context.Detail("runtimeAssetCatalog.activeUsageCountFromArtIndex", activeUsageCount);
            context.Detail("runtimeAssetCatalog.indexedActiveUsageCountFromArtIndex", indexedActiveUsageCount);
            context.Detail("runtimeAssetCatalog.missingActiveAssetPathCountFromArtIndex", missingActivePathCount);
            context.Detail("runtimeAssetCatalog.missingLegacySourcePathCountFromArtIndex", missingLegacyPathCount);
            context.Detail("uiFormUsage.rowCountFromArtIndex", uiFormRowCount);
            context.Detail("uiFormUsage.enabledRowCountFromArtIndex", uiFormEnabledRowCount);
            context.Detail("uiFormUsage.indexedAssetPathCountFromArtIndex", uiFormIndexedAssetPathCount);
            context.Detail("uiFormUsage.indexedUsageCountFromArtIndex", uiFormIndexedUsageCount);
            context.Detail("uiFormUsage.missingActiveAssetPathCountFromArtIndex", uiFormMissingActivePathCount);

            context.AssertEqual(0, missingSchemaCount, "artAssets.runtimeUsageSchemaMissingCount");
            context.AssertEqual(0, missingUiFormUsageSchemaCount, "artAssets.uiFormUsageSchemaMissingCount");
            context.AssertEqual(0, missingUsageStatusSchemaCount, "artAssets.usageStatusSchemaMissingCount");
            context.AssertEqual(0, unclassifiedSystemCount, "artAssets.unclassifiedSystemCount");
            context.AssertEqual(0, classificationNeededUsageStatusCount, "artAssets.classificationNeededUsageStatusCount");
            context.AssertEqual(assetCount, usageStatusAssetCount, "artAssets.usageStatusAssetCount");
            context.AssertEqual(assetCount, usageStatusCountTotal, "artAssets.usageStatusCountTotal");
            context.AssertEqual(usageStatusCounts?.Properties().Count() ?? -1, usageStatusLegendCount, "artAssets.usageStatusLegendCount");
            context.AssertEqual(runtimeBoundAssetCount, manifestRuntimeBoundAssetCount, "artAssets.runtimeBoundAssetCount");
            context.AssertEqual(uiFormBoundAssetCount, manifestUiFormBoundAssetCount, "artAssets.uiFormBoundAssetCount");
            context.AssertEqual(manifestRuntimeBoundAssetCount, runtimeBoundUsageStatusAssetCount, "artAssets.runtimeBoundUsageStatusAssetCount");
            context.AssertEqual(manifestRuntimeBoundAssetCount, runtimeBoundUsageStatusCount, "artAssets.runtimeBoundUsageStatusCount");
            context.AssertEqual(manifestUiFormBoundAssetCount, uiFormBoundUsageStatusAssetCount, "artAssets.uiFormBoundUsageStatusAssetCount");
            context.AssertEqual(manifestUiFormBoundAssetCount, uiFormBoundUsageStatusCount, "artAssets.uiFormBoundUsageStatusCount");
            context.AssertEqual(placeholderReviewStateCount, placeholderUsageStatusCount, "artAssets.placeholderUsageStatusCount");
            context.Assert(runtimeEntryCount >= 36, "art_assets.json must link against the current first-round runtime asset catalog.");
            context.Assert(activeUsageCount >= runtimeEntryCount, "Runtime asset catalog active usage count must cover every entry.");
            context.AssertEqual(activeUsageCount, indexedActiveUsageCount, "runtimeAssetCatalog.indexedActiveUsageCountFromArtIndex");
            context.AssertEqual(0, missingActivePathCount, "runtimeAssetCatalog.missingActiveAssetPathCountFromArtIndex");
            context.AssertEqual(12, uiFormEnabledRowCount, "uiFormUsage.enabledRowCountFromArtIndex");
            context.AssertEqual(uiFormEnabledRowCount, uiFormIndexedUsageCount, "uiFormUsage.indexedUsageCountFromArtIndex");
            context.AssertEqual(uiFormIndexedUsageCount, uiFormUsageRecordCount, "artAssets.uiFormUsageRecordCount");
            context.AssertEqual(uiFormIndexedAssetPathCount, uiFormBoundAssetCount, "uiFormUsage.indexedAssetPathCountFromArtIndex");
            context.AssertEqual(0, uiFormMissingActivePathCount, "uiFormUsage.missingActiveAssetPathCountFromArtIndex");
            context.Assert(runtimeBoundAssetCount > 0, "art_assets.json must expose at least one runtime-bound art asset.");
            context.Assert(uiFormBoundAssetCount >= 12, "art_assets.json must expose all first-round GF_X UI form prefab bindings.");
            context.Assert(runtimeUsageRecordCount >= runtimeEntryCount, "art_assets.json should expose runtime usage records for catalog entries.");
        }

        private static void CheckFeatureSliceManifest(GFDiagnosticScenarioContext context)
        {
            string text = File.ReadAllText(FeatureSlicesManifestPath);
            var root = JObject.Parse(text);
            var slices = root["slices"] as JArray;
            var validation = root["validation"] as JObject;
            context.Assert(slices != null, "feature_slices.json slices must be a JSON array.");
            context.Assert(validation != null, "feature_slices.json validation must be a JSON object.");

            int manifestCount = root.Value<int?>("count") ?? -1;
            int sliceCount = slices?.Count ?? -1;
            int legacyModuleCoverageCount = root.Value<int?>("legacy_module_coverage_count") ?? -1;
            int businessTableCoverageCount = root.Value<int?>("business_table_coverage_count") ?? -1;
            int runtimeServiceCoverageCount = root.Value<int?>("runtime_service_coverage_count") ?? -1;
            int runtimeAssetKeyCoverageCount = root.Value<int?>("runtime_asset_key_coverage_count") ?? -1;
            int diagnosticScenarioCoverageCount = root.Value<int?>("diagnostic_scenario_coverage_count") ?? -1;
            bool validationValid = validation?.Value<bool?>("valid") ?? false;
            int missingModules = CountObjectProperties(validation, "missing_modules");
            int uncoveredLegacyModuleCount = (validation?["uncovered_legacy_modules"] as JArray)?.Count ?? -1;
            int missingTables = CountObjectProperties(validation, "missing_business_tables");
            int missingRuntimeServices = CountObjectProperties(validation, "missing_runtime_services");
            int uncoveredRuntimeServiceCount = (validation?["uncovered_runtime_services"] as JArray)?.Count ?? -1;
            int missingRuntimeKeys = CountObjectProperties(validation, "missing_runtime_asset_keys");
            int missingFields = CountObjectProperties(validation, "missing_required_fields");
            int activeRuntimeServiceCount = Directory.Exists("Assets/Game/Scripts/Runtime/Services")
                ? Directory.GetFiles("Assets/Game/Scripts/Runtime/Services", "Totem*Service.cs").Length
                : 0;
            int completeHandoffCount = 0;
            int diagnosticScenarioReferenceCount = 0;
            int registeredDiagnosticScenarioReferenceCount = 0;
            var registeredScenarios = BuildRegisteredScenarioNames();

            var requiredIds = new[]
            {
                "ui_entry_flow",
                "first_round_population",
                "combat_weapon_skill",
                "tattoo_builds",
                "smart_ai_roster",
                "economy_shop_chest",
                "npc_interactions",
                "three_choice_events",
                "map_zone",
                "boss_phase",
            };

            var seenIds = new HashSet<string>();
            if (slices != null)
            {
                foreach (var token in slices)
                {
                    var slice = token as JObject;
                    if (slice == null)
                    {
                        continue;
                    }

                    string id = slice.Value<string>("id") ?? string.Empty;
                    seenIds.Add(id);
                    var handoff = slice["discipline_handoff"] as JObject;
                    if (handoff != null
                        && !string.IsNullOrWhiteSpace(handoff.Value<string>("design"))
                        && !string.IsNullOrWhiteSpace(handoff.Value<string>("art"))
                        && !string.IsNullOrWhiteSpace(handoff.Value<string>("program"))
                        && !string.IsNullOrWhiteSpace(handoff.Value<string>("qa")))
                    {
                        completeHandoffCount++;
                    }

                    var diagnosticScenarios = slice["diagnostic_scenarios"] as JArray;
                    if (diagnosticScenarios == null)
                    {
                        continue;
                    }

                    foreach (var scenarioToken in diagnosticScenarios)
                    {
                        string scenario = scenarioToken?.Value<string>() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(scenario))
                        {
                            continue;
                        }

                        diagnosticScenarioReferenceCount++;
                        if (IsRegisteredOrGuardedScenario(registeredScenarios, scenario))
                        {
                            registeredDiagnosticScenarioReferenceCount++;
                        }
                        else
                        {
                            context.Fail($"feature_slices.json references a diagnostic scenario that is not registered or guarded: {scenario}");
                        }
                    }
                }
            }

            int requiredIdHitCount = requiredIds.Count(id => seenIds.Contains(id));
            context.Detail("featureSlices.count", sliceCount);
            context.Detail("featureSlices.manifestCount", manifestCount);
            context.Detail("featureSlices.legacyModuleCoverageCount", legacyModuleCoverageCount);
            context.Detail("featureSlices.uncoveredLegacyModuleCount", uncoveredLegacyModuleCount);
            context.Detail("featureSlices.businessTableCoverageCount", businessTableCoverageCount);
            context.Detail("featureSlices.runtimeServiceCoverageCount", runtimeServiceCoverageCount);
            context.Detail("featureSlices.activeRuntimeServiceCount", activeRuntimeServiceCount);
            context.Detail("featureSlices.uncoveredRuntimeServiceCount", uncoveredRuntimeServiceCount);
            context.Detail("featureSlices.runtimeAssetKeyCoverageCount", runtimeAssetKeyCoverageCount);
            context.Detail("featureSlices.diagnosticScenarioCoverageCount", diagnosticScenarioCoverageCount);
            context.Detail("featureSlices.validation.valid", validationValid);
            context.Detail("featureSlices.validation.missingModuleFeatureCount", missingModules);
            context.Detail("featureSlices.validation.missingTableFeatureCount", missingTables);
            context.Detail("featureSlices.validation.missingRuntimeServiceFeatureCount", missingRuntimeServices);
            context.Detail("featureSlices.validation.missingRuntimeKeyFeatureCount", missingRuntimeKeys);
            context.Detail("featureSlices.validation.missingFieldFeatureCount", missingFields);
            context.Detail("featureSlices.completeHandoffCount", completeHandoffCount);
            context.Detail("featureSlices.requiredIdHitCount", requiredIdHitCount);
            context.Detail("featureSlices.diagnosticScenarioReferenceCount", diagnosticScenarioReferenceCount);
            context.Detail("featureSlices.registeredDiagnosticScenarioReferenceCount", registeredDiagnosticScenarioReferenceCount);

            context.Assert(sliceCount >= 10, "feature_slices.json must cover first-round cross-discipline feature slices.");
            context.AssertEqual(sliceCount, manifestCount, "featureSlices.count");
            context.AssertEqual(24, legacyModuleCoverageCount, "featureSlices.legacyModuleCoverageCount");
            context.AssertEqual(0, uncoveredLegacyModuleCount, "featureSlices.uncoveredLegacyModuleCount");
            context.AssertEqual(28, businessTableCoverageCount, "featureSlices.businessTableCoverageCount");
            context.AssertEqual(activeRuntimeServiceCount, runtimeServiceCoverageCount, "featureSlices.runtimeServiceCoverageCount");
            context.AssertEqual(0, uncoveredRuntimeServiceCount, "featureSlices.uncoveredRuntimeServiceCount");
            context.Assert(runtimeAssetKeyCoverageCount >= 25, "feature_slices.json should cover first-round runtime art keys.");
            context.Assert(diagnosticScenarioCoverageCount >= 10, "feature_slices.json should link feature work to GF_X diagnostics.");
            context.Assert(validationValid, "feature_slices.json validation must be true.");
            context.AssertEqual(0, missingModules, "featureSlices.validation.missingModuleFeatureCount");
            context.AssertEqual(0, missingTables, "featureSlices.validation.missingTableFeatureCount");
            context.AssertEqual(0, missingRuntimeServices, "featureSlices.validation.missingRuntimeServiceFeatureCount");
            context.AssertEqual(0, missingRuntimeKeys, "featureSlices.validation.missingRuntimeKeyFeatureCount");
            context.AssertEqual(0, missingFields, "featureSlices.validation.missingFieldFeatureCount");
            context.AssertEqual(sliceCount, completeHandoffCount, "featureSlices.completeHandoffCount");
            context.AssertEqual(requiredIds.Length, requiredIdHitCount, "featureSlices.requiredIdHitCount");
            context.AssertEqual(diagnosticScenarioReferenceCount, registeredDiagnosticScenarioReferenceCount, "featureSlices.registeredDiagnosticScenarioReferenceCount");
        }

        private static int CountObjectProperties(JObject root, string propertyName)
        {
            var child = root?[propertyName] as JObject;
            return child?.Properties().Count() ?? -1;
        }

        private static void CheckDiagnosticTriageManifest(GFDiagnosticScenarioContext context)
        {
            string text = File.ReadAllText(DiagnosticTriageManifestPath);
            var root = JObject.Parse(text);
            var records = root["records"] as JArray;
            var validation = root["validation"] as JObject;
            context.Assert(records != null, "diagnostic_triage.json records must be a JSON array.");
            context.Assert(validation != null, "diagnostic_triage.json validation must be a JSON object.");

            int scenarioCount = root.Value<int?>("count") ?? -1;
            int recordCount = records?.Count ?? -1;
            int featureLinkCount = root.Value<int?>("feature_link_count") ?? -1;
            bool validationValid = validation?.Value<bool?>("valid") ?? false;
            int missingFeatureLinkCount = (validation?["missing_feature_links"] as JArray)?.Count ?? -1;
            int completeRecordCount = 0;
            int registeredScenarioCount = 0;
            var registeredScenarios = BuildRegisteredScenarioNames();

            var requiredScenarios = new[]
            {
                "Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
                "Scenario/BusinessRuntime/Totem First Slice UI",
                "Scenario/BusinessRuntime/Totem Gameplay Runtime",
                "Scenario/BusinessRuntime/Totem AI Runtime",
                "Scenario/BusinessRuntime/Totem Runtime Catalog Binding",
            };

            var seenScenarios = new HashSet<string>();
            if (records != null)
            {
                foreach (var token in records)
                {
                    var record = token as JObject;
                    if (record == null)
                    {
                        continue;
                    }

                    string scenario = record.Value<string>("diagnostic_scenario") ?? string.Empty;
                    seenScenarios.Add(scenario);
                    if (IsRegisteredOrGuardedScenario(registeredScenarios, scenario))
                    {
                        registeredScenarioCount++;
                    }
                    else
                    {
                        context.Fail($"diagnostic_triage.json references a diagnostic scenario that is not registered or guarded: {scenario}");
                    }

                    int featureIds = (record["feature_ids"] as JArray)?.Count ?? 0;
                    int triageSteps = (record["triage_steps"] as JArray)?.Count ?? 0;
                    if (featureIds > 0 && triageSteps >= 4)
                    {
                        completeRecordCount++;
                    }
                }
            }

            int requiredScenarioHitCount = requiredScenarios.Count(item => seenScenarios.Contains(item));
            context.Detail("diagnosticTriage.count", recordCount);
            context.Detail("diagnosticTriage.manifestCount", scenarioCount);
            context.Detail("diagnosticTriage.featureLinkCount", featureLinkCount);
            context.Detail("diagnosticTriage.validation.valid", validationValid);
            context.Detail("diagnosticTriage.validation.missingFeatureLinkCount", missingFeatureLinkCount);
            context.Detail("diagnosticTriage.completeRecordCount", completeRecordCount);
            context.Detail("diagnosticTriage.requiredScenarioHitCount", requiredScenarioHitCount);
            context.Detail("diagnosticTriage.registeredScenarioCount", registeredScenarioCount);

            context.Assert(recordCount >= 10, "diagnostic_triage.json must cover the current GF_X business diagnostic scenarios.");
            context.AssertEqual(recordCount, scenarioCount, "diagnosticTriage.count");
            context.Assert(featureLinkCount >= recordCount, "diagnostic_triage.json should link every diagnostic scenario to at least one feature slice.");
            context.Assert(validationValid, "diagnostic_triage.json validation must be true.");
            context.AssertEqual(0, missingFeatureLinkCount, "diagnosticTriage.validation.missingFeatureLinkCount");
            context.AssertEqual(recordCount, completeRecordCount, "diagnosticTriage.completeRecordCount");
            context.AssertEqual(requiredScenarios.Length, requiredScenarioHitCount, "diagnosticTriage.requiredScenarioHitCount");
            context.AssertEqual(recordCount, registeredScenarioCount, "diagnosticTriage.registeredScenarioCount");
        }

        private static bool IsRegisteredOrGuardedScenario(HashSet<string> registeredScenarios, string scenario)
        {
            if (registeredScenarios.Contains(scenario))
            {
                return true;
            }

            return string.Equals(scenario, "Scenario/Startup/Launch To Totem Runtime Smoke", StringComparison.Ordinal);
        }

        private static string GetCompletionAuditDiagnosticsReportPath(string completionAuditText)
        {
            var match = Regex.Match(completionAuditText, @"GameData/Diagnostics/Reports/gf-diagnostics-run-all_\d{8}_\d{6}\.json", RegexOptions.CultureInvariant);
            if (match.Success && File.Exists(match.Value))
            {
                return match.Value;
            }

            return GetLatestSuccessfulDiagnosticsReportPath();
        }

        private static string GetLatestSuccessfulDiagnosticsReportPath()
        {
            const string diagnosticsReportDirectory = "GameData/Diagnostics/Reports";
            if (!Directory.Exists(diagnosticsReportDirectory))
            {
                return string.Empty;
            }

            foreach (string file in Directory.GetFiles(diagnosticsReportDirectory, "gf-diagnostics-run-all_*.json", SearchOption.TopDirectoryOnly)
                         .OrderByDescending(File.GetLastWriteTimeUtc))
            {
                try
                {
                    string text = File.ReadAllText(file);
                    if (ReadDiagnosticCounter(text, "successCount") >= 27 &&
                        ReadDiagnosticCounter(text, "failureCount") == 0 &&
                        ReadDiagnosticCounter(text, "warningCount") == 0)
                    {
                        return NormalizePath(file);
                    }
                }
                catch
                {
                    // Ignore incomplete report files while a diagnostics run is still writing.
                }
            }

            return string.Empty;
        }

        private static int ReadDiagnosticCounter(string jsonText, string key)
        {
            var match = Regex.Match(jsonText, $"\"{Regex.Escape(key)}\"\\s*:\\s*(\\d+)", RegexOptions.CultureInvariant);
            return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : -1;
        }

        private static void CheckActiveChangeDocumentEncoding(GFDiagnosticScenarioContext context)
        {
            string[] paths =
            {
                RequirementsInventoryPath,
                LegacyEffectCoveragePath,
                LegacyOpenSpecStatusPath,
                CompletionAuditPath,
                DecisionsNeededPath,
                RefactorTasksPath,
                DataTableMigrationManifestPath,
                ToolMigrationManifestPath,
            };

            string[] mojibakeSignatures =
            {
                "�",
                "璇诲",
                "鐩樼",
                "椤圭洰",
                "锛",
                "銆",
                "€",
                "歚",
                "乁I",
                "丟F",
                "泂ervice",
                "姝ｅ紡",
                "褰撳",
                "鍐呰",
            };

            int checkedDocumentCount = 0;
            foreach (string path in paths)
            {
                string text = File.ReadAllText(path);
                checkedDocumentCount++;
                foreach (string signature in mojibakeSignatures)
                {
                    context.Assert(!text.Contains(signature, System.StringComparison.Ordinal), $"{path} must not contain mojibake signature: {signature}");
                }
            }

            context.Detail("activeChangeEncoding.checkedDocumentCount", checkedDocumentCount);
        }

        private static void CheckActivePlaytestReportArchive(GFDiagnosticScenarioContext context)
        {
            context.RequireDirectory(ActivePlaytestReportsPath);
            context.RequireDirectory(ArchivedLegacyPlaytestReportsPath);

            string[] activeReportDirectories = Directory.GetDirectories(ActivePlaytestReportsPath, "*", SearchOption.AllDirectories)
                .Select(NormalizePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            context.Detail("activePlaytestReports.directoryCount", activeReportDirectories.Length);
            context.Assert(activeReportDirectories.Length == 0, $"Active playtest reports folder must not contain screenshot/output subdirectories; archive them or use tools/playtest/screenshots: {string.Join(",", activeReportDirectories)}");

            string[] legacyReportSignatures =
            {
                "[GameApp]",
                "GameApp 日志",
                "所有模块初始化完成",
                "Action=AllFormsLoaded",
                "UIModule Register",
                "EventBus.Subscribe",
                "Assets/Scripts/",
                "Assets\\Scripts\\",
            };

            int activeReportFilesChecked = 0;
            string[] activeReportFiles = Directory.GetFiles(ActivePlaytestReportsPath, "*", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Equals(".gitkeep", StringComparison.Ordinal))
                .OrderBy(NormalizePath, StringComparer.Ordinal)
                .ToArray();

            foreach (string reportPath in activeReportFiles)
            {
                string normalizedPath = NormalizePath(reportPath);
                string text = File.ReadAllText(reportPath);
                activeReportFilesChecked++;
                foreach (string signature in legacyReportSignatures)
                {
                    context.Assert(!text.Contains(signature, StringComparison.Ordinal), $"Active playtest report must not contain old-runtime signature '{signature}': {normalizedPath}");
                }
            }

            int archivedReportFileCount = Directory.GetFiles(ArchivedLegacyPlaytestReportsPath, "*", SearchOption.AllDirectories).Length;
            context.Detail("activePlaytestReports.filesChecked", activeReportFilesChecked);
            context.Detail("archivedLegacyPlaytestReports.fileCount", archivedReportFileCount);
            context.Assert(archivedReportFileCount >= 13, "Archived legacy playtest reports should remain available outside the active reports folder.");
        }

        private static void CheckPlayModeLaunchSmokeEvidence(GFDiagnosticScenarioContext context, string coverageText)
        {
            context.Assert(coverageText.Contains("[PlayMode] Scenario/Startup/Launch To Totem Runtime Smoke", System.StringComparison.Ordinal), "Legacy effect coverage must reference the PlayMode startup smoke report item.");

            string reportText = File.Exists(PlayModeLaunchSmokeReportPath) ? File.ReadAllText(PlayModeLaunchSmokeReportPath) : string.Empty;
            context.Assert(reportText.Contains("Play Mode diagnostic result: `success=8`, `failure=0`, `warning=0`", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record success=8/failure=0/warning=0.");
            context.Assert(reportText.Contains("StartupChainDiagnosticScenario", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must name the startup chain diagnostic.");
            context.Assert(reportText.Contains("Launch -> LoadHotfixDll -> HotfixEntry -> Preload -> Workspace -> TotemGame -> RuntimeReady", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record the GF_X startup chain.");
            context.Assert(reportText.Contains("currentProcedure=TotemGameProcedure", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record TotemGameProcedure.");
            context.Assert(reportText.Contains("serviceCount=26", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record the default service count.");
            context.Assert(reportText.Contains("readyServiceCount=26", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record all services ready.");
            context.Assert(reportText.Contains("failedServiceCount=0", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record zero failed services.");
            context.Assert(reportText.Contains("preloadFailures=0", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record zero preload failures.");
            context.Assert(reportText.Contains("filteredProjectErrorCount=0", System.StringComparison.Ordinal), "PM-04 PlayMode launch smoke report must record zero filtered project errors after exit.");

            context.Detail("playModeLaunchSmokeJsonRetention", "Pruned by report retention policy; markdown report keeps the required PlayMode evidence.");
        }

        private static void CheckPlayModeCombatHudSmokeEvidence(GFDiagnosticScenarioContext context, string coverageText)
        {
            const string evidenceName = "[PlayMode] Test/Totem CombatHUD Input Smoke";
            context.Assert(coverageText.Contains(evidenceName, System.StringComparison.Ordinal), "Legacy effect coverage must reference the PlayMode CombatHUD input smoke test.");

            string reportText = File.Exists(PlayModeCombatHudSmokeReportPath) ? File.ReadAllText(PlayModeCombatHudSmokeReportPath) : string.Empty;
            context.Assert(reportText.Contains("TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must name the PlayMode test.");
            context.Assert(reportText.Contains("Result: `Passed`", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record Passed.");
            context.Assert(reportText.Contains("Mode: PlayMode", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record PlayMode.");
            context.Assert(reportText.Contains("Total: `1`", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record total=1.");
            context.Assert(reportText.Contains("Passed: `1`", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record passed=1.");
            context.Assert(reportText.Contains("Failed: `0`", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record failed=0.");
            context.Assert(reportText.Contains("TotemInputService", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record TotemInputService routing.");
            context.Assert(reportText.Contains("ITotemInputProvider", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke report must record ITotemInputProvider routing.");

            string xmlText = File.Exists(PlayModeCombatHudSmokeXmlPath) ? File.ReadAllText(PlayModeCombatHudSmokeXmlPath) : string.Empty;
            context.Assert(xmlText.Contains("testcasecount=\"1\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record testcasecount=1.");
            context.Assert(xmlText.Contains("result=\"Passed\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record Passed.");
            context.Assert(xmlText.Contains("total=\"1\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record total=1.");
            context.Assert(xmlText.Contains("passed=\"1\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record passed=1.");
            context.Assert(xmlText.Contains("failed=\"0\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record failed=0.");
            context.Assert(xmlText.Contains("platform\" value=\"PlayMode\"", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must record PlayMode platform.");
            context.Assert(xmlText.Contains("TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must name the target test.");
            context.Assert(!xmlText.Contains("Expected: CombatHud", System.StringComparison.Ordinal), "PM-05 CombatHUD smoke XML must not be the earlier failing result.");
        }

        private static string GetCoverageMatrixSection(string coverageText)
        {
            int matrixStart = coverageText.IndexOf("## Matrix", System.StringComparison.Ordinal);
            int matrixEnd = coverageText.IndexOf("## Boundary Classification", System.StringComparison.Ordinal);
            return matrixStart >= 0 && matrixEnd > matrixStart
                ? coverageText.Substring(matrixStart, matrixEnd - matrixStart)
                : string.Empty;
        }

        private static void CheckCoverageMatrixStateConsistency(GFDiagnosticScenarioContext context, string coverageText)
        {
            string matrixSection = GetCoverageMatrixSection(coverageText);

            int checkedRows = 0;
            foreach (string line in matrixSection.Split('\n'))
            {
                if (!line.StartsWith("| ", System.StringComparison.Ordinal) ||
                    line.StartsWith("| Legacy module", System.StringComparison.Ordinal) ||
                    line.StartsWith("|---", System.StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Trim().Trim('|').Split('|').Select(part => part.Trim()).ToArray();
                if (parts.Length < 6)
                {
                    continue;
                }

                string module = parts[0];
                string state = parts[4];
                string boundary = parts[5];
                checkedRows++;

                if (state == "Covered")
                {
                    context.Assert(boundary.StartsWith("No remaining first-round boundary", System.StringComparison.Ordinal), $"Legacy effect row {module} is Covered but still describes a remaining boundary.");
                    continue;
                }

                if (state == "Covered with boundary")
                {
                    context.Assert(!boundary.StartsWith("No remaining first-round boundary", System.StringComparison.Ordinal), $"Legacy effect row {module} is Covered with boundary but says no boundary remains.");
                    continue;
                }

                if (state == "Evidence only")
                {
                    continue;
                }

                context.Fail($"Legacy effect row {module} has unknown coverage state: {state}");
            }

            context.Detail("legacyEffectCoverage.matrixStateRowsChecked", checkedRows);
        }

        private static void CheckCoverageEvidenceReferences(GFDiagnosticScenarioContext context, string coverageText)
        {
            string matrixSection = GetCoverageMatrixSection(coverageText);
            context.Assert(!matrixSection.Contains("DiagnosticScenario", System.StringComparison.Ordinal), "Legacy effect coverage evidence must use report item names, not C# DiagnosticScenario class names.");
            string[] allowedEvidenceItems =
            {
                "[EditMode] AI DataTable json",
                "[EditMode] Scenario/BusinessRuntime/GF_X Rewrite Inventory Contract",
                "[EditMode] Scenario/BusinessRuntime/Totem AI Runtime",
                "[EditMode] Scenario/BusinessRuntime/Totem Audio Runtime",
                "[EditMode] Scenario/BusinessRuntime/Totem Choice Runtime",
                "[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay",
                "[EditMode] Scenario/BusinessRuntime/Totem First Round Contract",
                "[EditMode] Scenario/BusinessRuntime/Totem First Slice UI",
                "[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog",
                "[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime",
                "[EditMode] Scenario/BusinessRuntime/Totem Meta Progress",
                "[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding",
                "[EditMode] Scenario/BusinessRuntime/Totem Runtime Assets",
                "[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke",
                "[EditMode] Scenario/BusinessRuntime/Totem VFX Runtime",
                "[EditMode] Scenario/Core/Clean Workspace Contract",
                "[EditMode] Scenario/Migration/Migration Path Contract",
                "[PlayMode] Scenario/Startup/Launch To Totem Runtime Smoke",
                "[PlayMode] Test/Totem CombatHUD Input Smoke",
            };

            int checkedRows = 0;
            int checkedEvidenceItems = 0;
            int checkedScenarioEvidenceItems = 0;
            var currentEditModeScenarioEvidenceItems = BuildCurrentEditModeScenarioEvidenceItems();
            foreach (string line in matrixSection.Split('\n'))
            {
                if (!line.StartsWith("| ", System.StringComparison.Ordinal) ||
                    line.StartsWith("| Legacy module", System.StringComparison.Ordinal) ||
                    line.StartsWith("|---", System.StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Trim().Trim('|').Split('|').Select(part => part.Trim()).ToArray();
                if (parts.Length < 6)
                {
                    continue;
                }

                string module = parts[0];
                string evidence = parts[3];
                checkedRows++;
                context.Assert(!evidence.Contains(" checks", System.StringComparison.Ordinal), $"Legacy effect row {module} evidence must not use vague 'checks' wording; name the report item.");
                context.Assert(
                    evidence.Contains("[EditMode]", System.StringComparison.Ordinal) ||
                    evidence.Contains("[PlayMode]", System.StringComparison.Ordinal) ||
                    evidence.Contains("[Static]", System.StringComparison.Ordinal),
                    $"Legacy effect row {module} must tag evidence source as EditMode, PlayMode or Static.");

                foreach (string rawEvidenceItem in evidence.Split(';'))
                {
                    string evidenceItem = rawEvidenceItem.Trim().Trim('`');
                    if (string.IsNullOrWhiteSpace(evidenceItem))
                    {
                        continue;
                    }

                    checkedEvidenceItems++;
                    context.Assert(allowedEvidenceItems.Contains(evidenceItem), $"Legacy effect row {module} references unknown automated evidence item: {evidenceItem}");

                    if (evidenceItem.StartsWith("[EditMode] Scenario/", System.StringComparison.Ordinal))
                    {
                        checkedScenarioEvidenceItems++;
                        context.Assert(currentEditModeScenarioEvidenceItems.Contains(evidenceItem), $"Legacy effect row {module} references an EditMode scenario evidence item that is not registered: {evidenceItem}");
                    }
                }
            }

            context.Detail("legacyEffectCoverage.evidenceRowsChecked", checkedRows);
            context.Detail("legacyEffectCoverage.evidenceItemsChecked", checkedEvidenceItems);
            context.Detail("legacyEffectCoverage.editModeScenarioEvidenceItemsChecked", checkedScenarioEvidenceItems);
        }

        private static HashSet<string> BuildCurrentEditModeScenarioEvidenceItems()
        {
            var items = new HashSet<string>();
            foreach (Type scenarioType in TypeCache.GetTypesDerivedFrom<IGFDiagnosticScenario>()
                         .Where(type => type != null && !type.IsAbstract && !type.IsInterface))
            {
                IGFDiagnosticScenario scenario = null;
                try
                {
                    scenario = (IGFDiagnosticScenario)Activator.CreateInstance(scenarioType);
                }
                catch
                {
                    continue;
                }

                if (scenario.Mode == GFDiagnosticScenarioMode.PlayMode)
                {
                    continue;
                }

                string category = string.IsNullOrWhiteSpace(scenario.Category) ? "General" : scenario.Category;
                string name = string.IsNullOrWhiteSpace(scenario.Name) ? scenarioType.Name : scenario.Name;
                items.Add($"[EditMode] Scenario/{category}/{name}");
            }

            return items;
        }

        private static HashSet<string> BuildRegisteredScenarioNames()
        {
            var items = new HashSet<string>(StringComparer.Ordinal);
            foreach (Type scenarioType in TypeCache.GetTypesDerivedFrom<IGFDiagnosticScenario>()
                         .Where(type => type != null && !type.IsAbstract && !type.IsInterface))
            {
                IGFDiagnosticScenario scenario = null;
                try
                {
                    scenario = (IGFDiagnosticScenario)Activator.CreateInstance(scenarioType);
                }
                catch
                {
                    continue;
                }

                string category = string.IsNullOrWhiteSpace(scenario.Category) ? "General" : scenario.Category;
                string name = string.IsNullOrWhiteSpace(scenario.Name) ? scenarioType.Name : scenario.Name;
                items.Add($"Scenario/{category}/{name}");
            }

            return items;
        }

        private static void CheckCoverageBoundaryClassification(GFDiagnosticScenarioContext context, string coverageText, string[] legacyModules)
        {
            context.Assert(coverageText.Contains("## Boundary Classification", System.StringComparison.Ordinal), "Legacy effect coverage must classify every remaining boundary.");
            int sectionStart = coverageText.IndexOf("## Boundary Classification", System.StringComparison.Ordinal);
            int sectionEnd = coverageText.IndexOf("## Accepted later boundaries", System.StringComparison.Ordinal);
            string classificationSection = sectionStart >= 0 && sectionEnd > sectionStart
                ? coverageText.Substring(sectionStart, sectionEnd - sectionStart)
                : string.Empty;

            context.Assert(classificationSection.Contains("T9f7 production tuning", System.StringComparison.Ordinal), "Boundary classification must include the deferred T9f7 production tuning class.");
            context.Assert(classificationSection.Contains("Manual UI visual judgement", System.StringComparison.Ordinal), "Boundary classification must include the manual UI visual judgement class.");
            context.Assert(classificationSection.Contains("Explicit out-of-scope", System.StringComparison.Ordinal), "Boundary classification must include explicit out-of-scope decisions.");
            context.Assert(classificationSection.Contains("Placeholder art accepted", System.StringComparison.Ordinal), "Boundary classification must include the accepted placeholder-art class.");
            context.Assert(!classificationSection.Contains("Unclassified", System.StringComparison.Ordinal), "Legacy effect coverage must not leave any boundary unclassified.");

            int classifiedModuleCount = 0;
            foreach (string module in legacyModules)
            {
                if (classificationSection.Contains($"| {module} |", System.StringComparison.Ordinal))
                {
                    classifiedModuleCount++;
                    continue;
                }

                context.Fail($"Boundary classification must include legacy module row: {module}");
            }

            context.Detail("legacyEffectCoverage.classifiedBoundaryModuleCount", classifiedModuleCount);
        }

        private static void CheckLegacyOpenSpecStatus(GFDiagnosticScenarioContext context)
        {
            const string specsRoot = "openspec/specs";
            string statusText = File.ReadAllText(LegacyOpenSpecStatusPath);
            context.Assert(statusText.Contains("Historical evidence"), "Legacy OpenSpec status must mark old specs as historical evidence.");
            context.Assert(statusText.Contains("GF_X active replacement"), "Legacy OpenSpec status must point to GF_X active replacements.");

            string[] legacyTerms =
            {
                "GameApp",
                "EventBus",
                "ModuleRunner",
                "UIModule",
                "DataTableModule",
                "Assets/Scripts",
            };

            string[] legacySpecFiles = Directory.Exists(specsRoot)
                ? Directory.GetFiles(specsRoot, "spec.md", SearchOption.AllDirectories)
                    .Where(path => legacyTerms.Any(term => File.ReadAllText(path).Contains(term)))
                    .Select(NormalizePath)
                    .OrderBy(path => path)
                    .ToArray()
                : new string[0];

            context.Detail("legacyOpenSpecStatus.fileCount", legacySpecFiles.Length);
            foreach (string specFile in legacySpecFiles)
            {
                context.Assert(statusText.Contains(specFile), $"Legacy OpenSpec status must list old-runtime spec file: {specFile}");
            }
        }

        private static void CheckLegacyEvidenceInventory(GFDiagnosticScenarioContext context)
        {
            context.RequireDirectory(ArchivedLegacyDataTablePath);
            context.RequireDirectory(LegacyUIPrefabPath);
            context.RequireDirectory(ArchivedLegacyScriptPath);
            context.RequireDirectory(ArchivedLegacyModulesPath);
            context.RequireDirectory(BusinessAIDataTablePath);
            context.RequireDirectory(BusinessXlsxDataTablePath);
            context.RequireDirectory(ArchivedLegacyTestsPath);
            context.RequireDirectory(ArchivedLegacyPlaytestEditorPath);
            context.RequireDirectory(ArchivedLegacyCharacterEditorPath);
            context.Assert(!Directory.Exists(ActiveLegacyScriptPath), "Old Assets/Scripts must stay outside the active Unity compile path.");
            context.Assert(!Directory.Exists(ActiveLegacyTestsPath), "Old Assets/Tests must stay outside the active Unity compile path.");
            context.Assert(!Directory.Exists(ActiveLegacyPlaytestEditorPath), "Old Playtest editor tools must stay outside the active Unity compile path.");
            context.Assert(!Directory.Exists(ActiveLegacyCharacterEditorPath), "Old Character editor generator must stay outside the active Unity compile path.");
            context.Assert(!Directory.Exists(ActiveLegacyDataTablePath), "Old Resources/DataTable json must stay outside the active Unity Resources path.");
            CheckRootGeneratedProjectFiles(context);

            int archivedLegacyDataTableCount = Directory.Exists(ArchivedLegacyDataTablePath)
                ? Directory.GetFiles(ArchivedLegacyDataTablePath, "*.json", SearchOption.TopDirectoryOnly).Length
                : 0;
            int businessAIDataTableCount = Directory.Exists(BusinessAIDataTablePath)
                ? Directory.GetFiles(BusinessAIDataTablePath, "*.json", SearchOption.TopDirectoryOnly).Length
                : 0;
            int businessXlsxDataTableCount = Directory.Exists(BusinessXlsxDataTablePath)
                ? Directory.GetFiles(BusinessXlsxDataTablePath, "*.xlsx", SearchOption.TopDirectoryOnly).Length
                : 0;
            int legacyUIPrefabCount = Directory.Exists(LegacyUIPrefabPath)
                ? Directory.GetFiles(LegacyUIPrefabPath, "*.prefab", SearchOption.TopDirectoryOnly).Length
                : 0;
            int archivedLegacyScriptCount = Directory.Exists(ArchivedLegacyScriptPath)
                ? Directory.GetFiles(ArchivedLegacyScriptPath, "*.cs", SearchOption.AllDirectories).Length
                : 0;
            int archivedLegacyModuleDirectoryCount = Directory.Exists(ArchivedLegacyModulesPath)
                ? Directory.GetDirectories(ArchivedLegacyModulesPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0;
            int archivedLegacyTestCount = Directory.Exists(ArchivedLegacyTestsPath)
                ? Directory.GetFiles(ArchivedLegacyTestsPath, "*.cs", SearchOption.AllDirectories).Length
                : 0;
            int archivedLegacyEditorToolCount = 0;
            if (Directory.Exists(ArchivedLegacyPlaytestEditorPath))
            {
                archivedLegacyEditorToolCount += Directory.GetFiles(ArchivedLegacyPlaytestEditorPath, "*.cs", SearchOption.AllDirectories).Length;
            }

            if (Directory.Exists(ArchivedLegacyCharacterEditorPath))
            {
                archivedLegacyEditorToolCount += Directory.GetFiles(ArchivedLegacyCharacterEditorPath, "*.cs", SearchOption.AllDirectories).Length;
            }

            context.Detail("archivedLegacyDataTableCount", archivedLegacyDataTableCount);
            context.Detail("businessAIDataTableCount", businessAIDataTableCount);
            context.Detail("businessXlsxDataTableCount", businessXlsxDataTableCount);
            context.Detail("legacyUIPrefabCount", legacyUIPrefabCount);
            context.Detail("archivedLegacyScriptCount", archivedLegacyScriptCount);
            context.Detail("archivedLegacyModuleDirectoryCount", archivedLegacyModuleDirectoryCount);
            context.Detail("archivedLegacyTestCount", archivedLegacyTestCount);
            context.Detail("archivedLegacyEditorToolCount", archivedLegacyEditorToolCount);
            context.Assert(archivedLegacyDataTableCount == 28, $"Expected 28 archived legacy DataTable json files as evidence, actual {archivedLegacyDataTableCount}.");
            context.Assert(businessAIDataTableCount == 28, $"Expected 28 Business AI DataTable json files, actual {businessAIDataTableCount}.");
            context.Assert(businessXlsxDataTableCount == 28, $"Expected 28 Business xlsx DataTable files, actual {businessXlsxDataTableCount}.");
            CheckBusinessDataTableBridgeMapping(context);
            context.Assert(legacyUIPrefabCount == 12, $"Expected 12 legacy UI prefabs as evidence, actual {legacyUIPrefabCount}.");
            CheckLegacyUIPrefabInventory(context);
            context.Assert(archivedLegacyScriptCount > 0, "Archived legacy scripts should remain available as rewrite evidence.");
            context.Assert(archivedLegacyModuleDirectoryCount == 24, $"Expected 24 archived legacy module directories as behavior evidence, actual {archivedLegacyModuleDirectoryCount}.");
            CheckLegacyModuleAnalysisCards(context);
            context.Assert(archivedLegacyTestCount > 0, "Archived legacy tests should remain available as rewrite evidence.");
            context.Assert(archivedLegacyEditorToolCount > 0, "Archived legacy editor tools should remain available as rewrite evidence.");
        }

        private static void CheckRootGeneratedProjectFiles(GFDiagnosticScenarioContext context)
        {
            string[] projectFiles = Directory.GetFiles(".", "*.csproj", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(".", "*.sln", SearchOption.TopDirectoryOnly))
                .ToArray();
            string[] staleSignatures =
            {
                @"Assets\Scripts",
                "Assets/Scripts",
                @"Assets\Tests",
                "Assets/Tests",
                "Combat.Tests",
                "Tattoo.Tests",
                "WeaponUpgrade.Tests",
            };

            List<string> staleReferences = new List<string>();
            foreach (string path in projectFiles)
            {
                string fileName = Path.GetFileName(path);
                if (string.Equals(fileName, "GameDesinger.sln", StringComparison.OrdinalIgnoreCase))
                {
                    staleReferences.Add(fileName);
                    continue;
                }

                string text = File.ReadAllText(path);
                foreach (string signature in staleSignatures)
                {
                    if (text.Contains(signature, StringComparison.Ordinal))
                    {
                        staleReferences.Add($"{fileName}:{signature}");
                    }
                }
            }

            context.Detail("rootGeneratedProjectFile.count", projectFiles.Length);
            context.Detail("rootGeneratedProjectFile.staleReferenceCount", staleReferences.Count);
            context.Detail("rootGeneratedProjectFile.staleReferences", string.Join(", ", staleReferences));
            context.Assert(staleReferences.Count == 0, "Root generated .csproj/.sln files must not reference archived Assets/Scripts, old tests, or stale GameDesinger.sln.");
        }

        private static void CheckLegacyUIPrefabInventory(GFDiagnosticScenarioContext context)
        {
            string[] expected =
            {
                "CharacterSelect",
                "CombatHUD",
                "MainMenu",
                "PauseMenu",
                "RunResult",
                "SelfTattoo",
                "Settings",
                "Shop",
                "StartupSelect",
                "TattooEnchant",
                "TattooStudio",
                "ThreeChoice",
            };

            string[] actual = GetFileBaseNames(LegacyUIPrefabPath, "*.prefab");
            context.Detail("legacyUIPrefabNames", string.Join(",", actual));
            AssertSameNames(context, expected, actual, "Legacy UI prefab evidence");
        }

        private static void CheckBusinessDataTableBridgeMapping(GFDiagnosticScenarioContext context)
        {
            string[] archivedTableNames = GetFileBaseNames(ArchivedLegacyDataTablePath, "*.json");
            string[] businessJsonTableNames = GetFileBaseNames(BusinessAIDataTablePath, "*.json");
            string[] businessXlsxTableNames = GetFileBaseNames(BusinessXlsxDataTablePath, "*.xlsx");

            context.Detail("businessDataTableBridge.archivedNames", string.Join(",", archivedTableNames));
            context.Detail("businessDataTableBridge.jsonNames", string.Join(",", businessJsonTableNames));
            context.Detail("businessDataTableBridge.xlsxNames", string.Join(",", businessXlsxTableNames));

            AssertSameNames(context, archivedTableNames, businessJsonTableNames, "Business AI JSON DataTables");
            AssertSameNames(context, archivedTableNames, businessXlsxTableNames, "Business xlsx DataTables");
            CheckBusinessDataTableSchemaBridge(context);
        }

        private static void CheckBusinessDataTableSchemaBridge(GFDiagnosticScenarioContext context)
        {
            string text = File.ReadAllText(DataTablesManifestPath);
            var root = JObject.Parse(text);
            var bridge = root["ai_datatable_bridge"] as JObject;
            context.Assert(bridge != null, "datatables.json must include ai_datatable_bridge.");

            int schemaBridgeTableCount = bridge?.Value<int?>("schema_bridge_table_count") ?? -1;
            int schemaBridgeValidTableCount = bridge?.Value<int?>("schema_bridge_valid_table_count") ?? -1;
            int missingLegacyFieldTableCount = bridge?.Value<int?>("schema_bridge_missing_legacy_field_table_count") ?? -1;
            int addedBusinessFieldTableCount = bridge?.Value<int?>("schema_bridge_added_business_field_table_count") ?? -1;
            var missingFields = bridge?["schema_bridge_missing_legacy_fields"] as JObject;
            var addedFields = bridge?["schema_bridge_added_business_fields"] as JObject;
            int missingFieldEntryCount = missingFields?.Properties().Count() ?? -1;
            int addedFieldEntryCount = addedFields?.Properties().Count() ?? -1;

            context.Detail("businessDataTableBridge.schemaBridgeTableCount", schemaBridgeTableCount);
            context.Detail("businessDataTableBridge.schemaBridgeValidTableCount", schemaBridgeValidTableCount);
            context.Detail("businessDataTableBridge.missingLegacyFieldTableCount", missingLegacyFieldTableCount);
            context.Detail("businessDataTableBridge.addedBusinessFieldTableCount", addedBusinessFieldTableCount);
            context.Detail("businessDataTableBridge.missingLegacyFieldEntryCount", missingFieldEntryCount);
            context.Detail("businessDataTableBridge.addedBusinessFieldEntryCount", addedFieldEntryCount);

            context.AssertEqual(28, schemaBridgeTableCount, "businessDataTableBridge.schemaBridgeTableCount");
            context.AssertEqual(28, schemaBridgeValidTableCount, "businessDataTableBridge.schemaBridgeValidTableCount");
            context.AssertEqual(0, missingLegacyFieldTableCount, "businessDataTableBridge.missingLegacyFieldTableCount");
            context.AssertEqual(0, missingFieldEntryCount, "businessDataTableBridge.missingLegacyFieldEntryCount");
            context.AssertEqual(1, addedBusinessFieldTableCount, "businessDataTableBridge.addedBusinessFieldTableCount");
            context.AssertEqual(1, addedFieldEntryCount, "businessDataTableBridge.addedBusinessFieldEntryCount");

            string[] addedFieldTables = addedFields?.Properties().Select(property => property.Name).ToArray() ?? Array.Empty<string>();
            AssertSameNames(context, new[] { "BotConfig" }, addedFieldTables, "Business schema added-field table");

            string[] expectedBotConfigFields =
            {
                "Personality",
                "ReadingTargetWeight",
                "RiskTolerance",
                "ShopPreference",
                "TargetBossWeight",
                "TargetHumanoidAiWeight",
                "TargetPlayerWeight",
                "TargetResourceWeight",
            };
            string[] actualBotConfigFields = (addedFields?["BotConfig"] as JArray)?
                .Select(token => token.Value<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();
            AssertSameNames(context, expectedBotConfigFields, actualBotConfigFields, "BotConfig GF_X schema extension fields");
        }

        private static string[] GetFileBaseNames(string directory, string pattern)
        {
            if (!Directory.Exists(directory))
            {
                return new string[0];
            }

            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AssertSameNames(GFDiagnosticScenarioContext context, string[] expected, string[] actual, string label)
        {
            var expectedSet = new HashSet<string>(expected);
            var actualSet = new HashSet<string>(actual);
            string[] missing = expectedSet.Except(actualSet).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] extra = actualSet.Except(expectedSet).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            context.Assert(missing.Length == 0, $"{label} missing migrated table(s): {string.Join(",", missing)}");
            context.Assert(extra.Length == 0, $"{label} has unexpected table(s): {string.Join(",", extra)}");
        }

        private static void CheckLegacyModuleAnalysisCards(GFDiagnosticScenarioContext context)
        {
            string[] legacyModules =
            {
                "Audio",
                "Bot",
                "Camera",
                "Combat",
                "DataTable",
                "Economy",
                "Enemy",
                "Event",
                "Flow",
                "GameState",
                "Input",
                "MapGen",
                "NPC",
                "Resource",
                "Save",
                "Scene",
                "Settings",
                "Skill",
                "Spawner",
                "Status",
                "Tattoo",
                "UI",
                "VFX",
                "Weapon",
            };

            int moduleCardCount = 0;
            int moduleSourceDirectoryCount = 0;
            foreach (string module in legacyModules)
            {
                string moduleDirectory = $"{ArchivedLegacyModulesPath}/{module}";
                string moduleCardPath = $"{moduleDirectory}/MODULE.md";
                context.Assert(Directory.Exists(moduleDirectory), $"Archived legacy module directory is missing: {moduleDirectory}");
                if (Directory.Exists(moduleDirectory))
                {
                    int sourceFileCount = Directory.GetFiles(moduleDirectory, "*.cs", SearchOption.AllDirectories).Length;
                    context.Assert(sourceFileCount > 0, $"Archived legacy module directory has no C# source evidence: {moduleDirectory}");
                    moduleSourceDirectoryCount++;
                }

                context.Assert(File.Exists(moduleCardPath), $"Archived legacy module analysis card is missing: {moduleCardPath}");
                if (!File.Exists(moduleCardPath))
                {
                    continue;
                }

                moduleCardCount++;
                string moduleCardText = File.ReadAllText(moduleCardPath);
                context.Assert(moduleCardText.Contains($"module: {module}", StringComparison.Ordinal), $"Legacy module analysis card front matter must name module {module}: {moduleCardPath}");
                context.Assert(moduleCardText.Contains("source: tools/ai_index/build_ai_manifests.py", StringComparison.Ordinal), $"Legacy module analysis card must record its generator source: {moduleCardPath}");
                context.Assert(moduleCardText.Contains($"{ArchivedLegacyModulesPath}/{module}/", StringComparison.Ordinal), $"Legacy module analysis card must reference archived source files for module {module}: {moduleCardPath}");
            }

            context.Detail("archivedLegacyModuleCardCount", moduleCardCount);
            context.Detail("archivedLegacyModuleSourceDirectoryCount", moduleSourceDirectoryCount);
        }

        private static void CheckNativeRuntimeSkeleton(GFDiagnosticScenarioContext context)
        {
            string[] requiredTypes =
            {
                "TotemGameRuntime",
                "ITotemRuntimeService",
                "TotemRuntimeServiceBase",
                "TotemGameFlowService",
                "TotemInputService",
                "TotemDataService",
                "TotemAssetService",
                "TotemInteractionService",
                "TotemChestService",
                "TotemVfxService",
                "TotemUIService",
                "UGF.EditorTools.TotemPlaytestDriverEditor",
            };

            foreach (string typeName in requiredTypes)
            {
                context.Assert(ResolveType(typeName) != null, $"Type can not be resolved: {typeName}");
            }
        }

        private static System.Type ResolveType(string typeName)
        {
            System.Type type = System.Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            return System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(candidate => candidate != null);
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
#endif
