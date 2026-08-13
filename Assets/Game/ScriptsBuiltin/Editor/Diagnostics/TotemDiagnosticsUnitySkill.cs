#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnitySkills;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class TotemDiagnosticsUnitySkill
    {
        [UnitySkill("totem_diagnostics_run_all", "Run GF_X Totem diagnostics without routing through Unity menu execution.",
            Category = SkillCategory.Validation,
            Operation = SkillOperation.Analyze,
            Tags = new[] { "totem", "diagnostics", "gf-x", "test" },
            Outputs = new[] { "reportFile", "successCount", "failureCount", "warningCount" },
            ReadOnly = true,
            SupportsDryRun = false)]
        public static object RunAll()
        {
            GFDiagnosticReport report = GFDiagnosticRunner.RunAll();
            string reportFile = Directory.GetFiles(ConstEditor.DiagnosticReportPath, "gf-diagnostics-run-all_*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (report.failureCount > 0)
            {
                return new
                {
                    error = $"GF_X diagnostics failed: {report.failureCount}",
                    reportFile,
                    report.successCount,
                    report.failureCount,
                    report.warningCount,
                };
            }

            return new
            {
                success = true,
                reportFile,
                report.successCount,
                report.failureCount,
                report.warningCount,
            };
        }

        [UnitySkill("totem_cleanup_runtime_residuals", "Clean GF_X Totem transient runtime objects from the loaded editor scene without routing through Unity menu execution.",
            Category = SkillCategory.Validation,
            Operation = SkillOperation.Execute,
            Tags = new[] { "totem", "diagnostics", "cleanup", "runtime-residue" },
            Outputs = new[] { "removedCount", "remainingCount" },
            ReadOnly = false,
            SupportsDryRun = false)]
        public static object CleanupRuntimeResiduals()
        {
            if (Application.isPlaying)
            {
                return new
                {
                    success = false,
                    error = "Runtime residual cleanup is edit-mode only. Exit PlayMode before running this skill.",
                    removedCount = 0,
                    remainingCount = TotemRuntimeResidueCleaner.FindRuntimeResiduals().Count,
                };
            }

            int removed = TotemRuntimeResidueCleaner.CleanupRuntimeResiduals();
            int remaining = TotemRuntimeResidueCleaner.FindRuntimeResiduals().Count;
            return new
            {
                success = remaining == 0,
                removedCount = removed,
                remainingCount = remaining,
            };
        }

    }
}
#endif
