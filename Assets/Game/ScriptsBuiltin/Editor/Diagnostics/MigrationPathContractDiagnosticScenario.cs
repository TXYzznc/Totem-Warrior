#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UGF.EditorTools
{
    public sealed class MigrationPathContractDiagnosticScenario : GFDiagnosticScenarioBase
    {
        private static readonly string[] RequiredDirectories =
        {
            "Assets/Game",
            "Assets/Game/Scripts",
            "Assets/Game/ScriptsBuiltin",
            "Assets/Game/ScriptableAssets/Core",
            "GameData",
            "GameData/DataTables",
            "GameData/AIData",
            "GameData/AIData/DataTables",
            "GameData/Diagnostics",
            "GameData/Diagnostics/Reports",
            "项目知识库（AI自行维护）",
            "项目知识库（AI自行维护）/raw",
            "项目知识库（AI自行维护）/outputs",
            "项目知识库（AI自行维护）/wiki",
            "项目知识库（AI自行维护）/wiki/manifests",
        };

        private static readonly string[] ScanRoots =
        {
            "Assets/Game",
            "GameData",
            "项目知识库（AI自行维护）/wiki",
        };

        private static readonly string[] TextExtensions =
        {
            ".asmdef",
            ".asset",
            ".cs",
            ".json",
            ".md",
            ".prefab",
            ".txt",
            ".unity",
            ".xml",
            ".yaml",
            ".yml",
        };

        private static readonly Regex AbsolutePathPattern = new Regex(@"(?<![A-Za-z])[A-Za-z]:[\\/]|/Users/|\\\\\\\\[A-Za-z0-9_$.-]+\\\\", RegexOptions.Compiled);

        public override string Name => "Migration Path Contract";

        public override string Category => "Migration";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            context.TraceInfo("Scan", "Check migration path contract.");

            foreach (string directory in RequiredDirectories)
            {
                context.Detail($"directory.{directory}", Directory.Exists(directory));
                context.RequireDirectory(directory);
            }

            List<string> files = GetScannableFiles().ToList();
            context.Detail("scannedFiles", files.Count);

            var staleNameHits = new List<string>();
            var absolutePathHits = new List<string>();
            foreach (string file in files)
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception)
                {
                    context.Warn($"Could not read migration scan file: {file}. {exception.Message}");
                    continue;
                }

                if (text.Contains("AAAGame", StringComparison.Ordinal) || text.Contains("AAAGameData", StringComparison.Ordinal))
                {
                    staleNameHits.Add(file);
                }

                if (text.Contains("GF_X-master", StringComparison.Ordinal) || AbsolutePathPattern.IsMatch(text))
                {
                    absolutePathHits.Add(file);
                }
            }

            context.Detail("staleNameHits", staleNameHits.Count);
            context.Detail("absolutePathHits", absolutePathHits.Count);
            ReportHits(context, staleNameHits, "staleName");
            ReportHits(context, absolutePathHits, "absolutePath");

            context.Assert(staleNameHits.Count == 0, "Active framework files still contain AAAGame/AAAGameData.");
            context.Assert(absolutePathHits.Count == 0, "Active framework files still contain absolute or machine-specific paths.");
        }

        private static IEnumerable<string> GetScannableFiles()
        {
            foreach (string root in ScanRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string normalized = file.Replace('\\', '/');
                    if (IsExcluded(normalized))
                    {
                        continue;
                    }

                    string extension = Path.GetExtension(file);
                    if (TextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return normalized;
                    }
                }
            }
        }

        private static bool IsExcluded(string normalizedPath)
        {
            return normalizedPath.Contains("/Examples/", StringComparison.Ordinal) ||
                   normalizedPath.Contains("/Diagnostics/Reports/", StringComparison.Ordinal) ||
                   normalizedPath.Contains("/AIData/Reports/", StringComparison.Ordinal) ||
                   normalizedPath.Contains("/AIData/Backups/", StringComparison.Ordinal) ||
                   normalizedPath.EndsWith("/MigrationPathContractDiagnosticScenario.cs", StringComparison.Ordinal);
        }

        private static void ReportHits(GFDiagnosticScenarioContext context, List<string> hits, string key)
        {
            for (int i = 0; i < hits.Count && i < 20; i++)
            {
                context.Detail($"{key}.{i}", hits[i]);
            }
        }
    }
}
#endif
