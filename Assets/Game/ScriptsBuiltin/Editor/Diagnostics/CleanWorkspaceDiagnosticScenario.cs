#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace UGF.EditorTools
{
    public sealed class CleanWorkspaceDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Clean Workspace Contract";

        public override string Category => "Core";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            context.TraceInfo("Arrange", "Check active workspace and removed GF_X examples.");

            RequireDirectory(context, "Assets/Game/Scene");
            RequireDirectory(context, "Assets/Game/Prefabs/Entity");
            RequireDirectory(context, "Assets/Game/Prefabs/UI");
            RequireFile(context, "Assets/Game/Scene/Launch.unity");
            RequireFile(context, "Assets/Game/Scripts/Procedures/WorkspaceProcedure.cs");
            RequireFile(context, "Assets/Game/ScriptableAssets/Core/AppConfigs.asset");
            RequireFile(context, "GameData/DataTables/Core/UITable.xlsx");
            RequireFile(context, "GameData/DataTables/Core/LanguagesTable.xlsx");
            RequireFile(context, "GameData/Languages/English.xlsx");

            ForbidDirectory(context, "Assets/Game/Examples/DemoGame", "GF_X DemoGame example assets must not remain in the active project.");
            ForbidDirectory(context, "GameData/Examples/DemoGame", "GF_X DemoGame example data must not remain in the active project.");
            ForbidFile(context, "Assets/Game/Scene/Game.unity", "Demo scene must not be present in the active scene folder.");
            ForbidFile(context, "Assets/Game/Scripts/Procedures/MenuProcedure.cs", "Demo MenuProcedure must not be compiled in active workspace.");
            ForbidFile(context, "Assets/Game/Scripts/Procedures/GameProcedure.cs", "Demo GameProcedure must not be compiled in active workspace.");
            ForbidFile(context, "Assets/Game/Scripts/Procedures/GameOverProcedure.cs", "Demo GameOverProcedure must not be compiled in active workspace.");
            ForbidFile(context, "Assets/Readme.asset", "Unity template readme must stay archived with TutorialInfo.");
            ForbidDirectory(context, "Assets/Scenes", "Legacy scenes must stay under LegacyProjectArchive/Assets/Scenes. Active startup scene is Assets/Game/Scene/Launch.unity.");
            ForbidDirectory(context, "Assets/Editor", "Legacy editor scripts must stay under LegacyProjectArchive/Assets/Editor or be migrated into Assets/Game/ScriptsBuiltin/Editor.");
            ForbidDirectory(context, "Assets/Tools", "Legacy ToolHub scripts must stay under LegacyProjectArchive/Assets/Tools. GF_X project tools live outside active runtime assets or under ScriptsBuiltin/Editor.");
            ForbidDirectory(context, "Assets/TutorialInfo", "Unity template tutorial assets must stay under LegacyProjectArchive/Assets/TutorialInfo.");
            ForbidDirectory(context, "Assets/Screenshots", "Playtest screenshots must stay under tools/playtest/screenshots, not active Unity assets.");
            ForbidDirectory(context, "Assets/TestResults", "Playtest results must stay under tools/playtest/test-results, not active Unity assets.");
            ForbidDirectory(context, "Assets/Resources/Character", "Confirmed obsolete Character art must stay out of active Resources.");
            ForbidDirectory(context, "Assets/Resources/Characters", "Confirmed obsolete Characters art must stay out of active Resources.");
            ForbidDirectory(context, "Assets/Resources/Environments", "Confirmed obsolete Environments art must stay out of active Resources.");
            ForbidDirectory(context, "Assets/Resources/Recipes", "Confirmed obsolete Recipes art must stay out of active Resources.");
            ForbidDirectory(context, "Assets/Resources/Tattoo", "Confirmed obsolete Tattoo art must stay out of active Resources.");
            ForbidUISpriteSidecarFiles(context);
            ForbidRuntimeResidues(context);
            ForbidPrefabMissingScripts(context);

            context.Detail("contract", "Active workspace starts from Launch -> Preload -> Workspace; GF_X DemoGame examples are removed.");
        }

        private static void RequireDirectory(GFDiagnosticScenarioContext context, string directoryName)
        {
            context.Detail($"directory.{directoryName}", Directory.Exists(directoryName));
            context.RequireDirectory(directoryName);
        }

        private static void RequireFile(GFDiagnosticScenarioContext context, string fileName)
        {
            context.Detail($"file.{fileName}", File.Exists(fileName));
            context.RequireFile(fileName);
        }

        private static void ForbidFile(GFDiagnosticScenarioContext context, string fileName, string message)
        {
            bool exists = File.Exists(fileName);
            context.Detail($"forbidden.{fileName}", exists);
            context.Assert(!exists, $"{message} Path: {fileName}");
        }

        private static void ForbidDirectory(GFDiagnosticScenarioContext context, string directoryName, string message)
        {
            bool exists = Directory.Exists(directoryName);
            context.Detail($"forbidden.{directoryName}", exists);
            context.Assert(!exists, $"{message} Path: {directoryName}");
        }

        private static void ForbidUISpriteSidecarFiles(GFDiagnosticScenarioContext context)
        {
            const string uiSpriteRoot = "Assets/Resources/Sprite/UI";
            string[] forbiddenExtensions =
            {
                ".json",
                ".log",
                ".txt",
            };

            var hits = new List<string>();
            if (Directory.Exists(uiSpriteRoot))
            {
                foreach (string file in Directory.GetFiles(uiSpriteRoot, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file);
                    if (forbiddenExtensions.Contains(extension, System.StringComparer.OrdinalIgnoreCase))
                    {
                        hits.Add(file.Replace('\\', '/'));
                    }
                }
            }

            context.Detail("forbidden.uiSpriteSidecarFiles", hits.Count);
            context.Detail("forbidden.uiSpriteSidecarExamples", string.Join(", ", hits.Take(10)));
            context.Assert(hits.Count == 0, "UI sprite Resources should contain art assets only. Move README/log/json sidecars to LegacyProjectArchive/Assets/Resources/Sprite/UI_Docs or 项目知识库（AI自行维护）/raw.");
        }

        private static void ForbidRuntimeResidues(GFDiagnosticScenarioContext context)
        {
            var residues = TotemRuntimeResidueCleaner.FindRuntimeResiduals();
            context.Detail("runtimeResidue.count", residues.Count);
            context.Detail("runtimeResidue.names", string.Join(", ", residues.Take(10).Select(item => item.name)));
            context.Assert(residues.Count == 0, "Runtime temporary VFX objects must not remain in the active editor scene. Run Game Framework/GameTools/Diagnostics/Cleanup Runtime Residuals.");
        }

        private static void ForbidPrefabMissingScripts(GFDiagnosticScenarioContext context)
        {
            var records = TotemPrefabMissingScriptCleaner.FindMissingScriptRecords();
            int missingCount = records.Sum(record => record.MissingCount);
            context.Detail("prefabMissingScripts.prefabObjectCount", records.Count);
            context.Detail("prefabMissingScripts.componentCount", missingCount);
            context.Detail("prefabMissingScripts.examples", string.Join(", ", records.Take(10).Select(record => $"{record.PrefabPath}:{record.ObjectPath}")));
            context.Assert(missingCount == 0, "Project prefabs must not contain missing script components. Run Game Framework/GameTools/Diagnostics/Cleanup Prefab Missing Scripts.");
        }
    }
}
#endif
