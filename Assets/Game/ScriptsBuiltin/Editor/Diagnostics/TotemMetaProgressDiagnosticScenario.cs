#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemMetaProgressDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Meta Progress";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckMutationContract(context);
            CheckPersistenceContract(context);
            CheckRuntimeRegistration(context);
            context.Pass("Totem meta progress contract is ready.");
        }

        private static void CheckMutationContract(GFDiagnosticScenarioContext context)
        {
            var service = new TotemMetaProgressService { AutoSave = false };
            var initial = service.CaptureSnapshot();
            context.AssertEqual(TotemMetaProgressService.CharacterSlotCount, initial.characterSlots.Length, "meta.characterSlot.length");
            context.Assert(initial.characterSlots[0], "Starter character slot should be unlocked.");
            context.Assert(!initial.characterSlots[8], "Last character slot should start locked.");
            context.AssertEqual(1, TotemMetaProgressService.CountUnlockedCharacters(initial), "meta.characterSlot.defaultUnlockedCount");

            context.Assert(service.SetCharacterUnlocked(3, true), "Character slot 3 should unlock.");
            context.Assert(service.IsCharacterUnlocked(3), "Character slot 3 should report unlocked.");
            context.Assert(!service.SetCharacterUnlocked(-1, true), "Negative character slot should be rejected.");
            context.Assert(!service.SetCharacterUnlocked(0, false), "Starter character slot should not be locked.");
            context.Assert(service.SetCharacterUnlocked(3, false), "Character slot 3 should lock again.");
            context.Assert(!service.IsCharacterUnlocked(3), "Character slot 3 should report locked.");

            context.Assert(service.SetPatternUnlocked(" pattern_line ", 2, true), "Pattern slot should unlock.");
            context.Assert(service.IsPatternUnlocked("pattern_line", 2), "Pattern slot should report unlocked.");
            context.Assert(!service.SetPatternUnlocked("pattern_line", TotemMetaProgressService.PatternSlotCount, true), "Out-of-range pattern slot should be rejected.");
            context.Assert(service.SetPatternUnlocked("pattern_line", 2, false), "Pattern slot should lock again.");
            context.AssertEqual(0, TotemMetaProgressService.CountUnlockedPatternSlots(service.CaptureSnapshot()), "meta.patternSlot.countAfterLock");

            context.Assert(service.SetPatternUnlocked("pattern_line", 2, true), "Pattern slot should unlock for set tests.");
            context.Assert(service.SetDecorationUnlocked("mask_bone_001"), "Decoration should unlock.");
            context.Assert(service.SetDecorationUnlocked("mask_bone_001"), "Duplicate decoration unlock should be accepted as no-op.");
            context.Assert(service.SetTitleUnlocked("title_trial_victor"), "Title should unlock.");
            context.Assert(service.SetGalleryUnlocked("gallery_intro_totem"), "Gallery entry should unlock.");
            context.Assert(service.SetAchievementCompleted("achievement_first_blood"), "Achievement should complete.");
            context.Assert(service.SetAchievementCompleted("achievement_first_blood"), "Duplicate achievement completion should be accepted as no-op.");

            var snapshot = service.CaptureSnapshot();
            context.AssertEqual(1, snapshot.unlockedDecorations.Length, "meta.decorations.uniqueCount");
            context.AssertEqual(1, snapshot.unlockedTitles.Length, "meta.titles.uniqueCount");
            context.AssertEqual(1, snapshot.unlockedGallery.Length, "meta.gallery.uniqueCount");
            context.AssertEqual(1, snapshot.completedAchievements.Length, "meta.achievements.uniqueCount");
            context.AssertEqual(1, TotemMetaProgressService.CountUnlockedPatternSlots(snapshot), "meta.patternSlot.unlockedCount");
            context.Assert(TotemMetaProgressService.FormatSnapshot(snapshot).Contains("Achievements: 1"), "Meta formatter should include achievement count.");
        }

        private static void CheckPersistenceContract(GFDiagnosticScenarioContext context)
        {
            string directory = Path.Combine(Path.GetTempPath(), "totem-warrior-diagnostics");
            string fileName = Path.Combine(directory, "meta-progress-test.json");
            string backupFile = fileName + ".bak";
            string tempFile = fileName + ".tmp";
            try
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);

                var snapshot = TotemMetaProgressService.CreateDefaultSnapshot();
                snapshot.characterSlots[4] = true;
                snapshot.patternUnlocks = new[]
                {
                    new TotemPatternUnlockSnapshot { patternId = "pattern_line", slots = new[] { false, true, false, false, false, false } },
                    new TotemPatternUnlockSnapshot { patternId = "pattern_line", slots = new[] { false, false, true, false, false, false } },
                };
                snapshot.unlockedDecorations = new[] { "mask_bone_001", "mask_bone_001" };
                snapshot.unlockedTitles = new[] { "title_trial_victor" };
                snapshot.unlockedGallery = new[] { "gallery_intro_totem" };
                snapshot.completedAchievements = new[] { "achievement_first_blood" };

                context.Assert(TotemMetaProgressService.TryWriteSnapshotToFile(fileName, snapshot, out string writeError), $"Meta progress should save to temp file: {writeError}");
                context.Assert(File.Exists(fileName), "Meta progress file should exist.");
                context.Assert(TotemMetaProgressService.TryReadSnapshotFromFile(fileName, out var loaded, out string readError), $"Meta progress should load from temp file: {readError}");
                context.AssertEqual(2, TotemMetaProgressService.CountUnlockedCharacters(loaded), "meta.persistence.characterCount");
                context.AssertEqual(2, TotemMetaProgressService.CountUnlockedPatternSlots(loaded), "meta.persistence.patternSlotCount");
                context.AssertEqual(1, loaded.unlockedDecorations.Length, "meta.persistence.decorationUniqueCount");
                context.Assert(!string.IsNullOrWhiteSpace(loaded.lastSavedUtc), "Meta progress should record last saved UTC.");

                context.Assert(TotemMetaProgressService.TryWriteSnapshotToFile(fileName, loaded, out string rewriteError), $"Meta progress should rewrite with backup: {rewriteError}");
                context.Assert(File.Exists(backupFile), "Meta progress rewrite should create backup file.");

                File.WriteAllText(fileName, "{not json}");
                context.Assert(!TotemMetaProgressService.TryReadSnapshotFromFile(fileName, out _, out string invalidError), "Invalid meta progress JSON should fail cleanly.");
                context.Assert(!string.IsNullOrWhiteSpace(invalidError), "Invalid meta progress JSON should report an error.");
            }
            finally
            {
                DeleteIfExists(fileName);
                DeleteIfExists(backupFile);
                DeleteIfExists(tempFile);
            }
        }

        private static void CheckRuntimeRegistration(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemMetaProgressDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                var service = new TotemMetaProgressService { AutoSave = false };
                runtime.RegisterService(service);
                runtime.StartRuntime();
                context.Assert(runtime.GetService<TotemMetaProgressService>() != null, "Runtime should resolve TotemMetaProgressService.");
                context.Assert(runtime.GetService<TotemMetaProgressService>().IsCharacterUnlocked(0), "Runtime meta progress should keep starter character unlocked.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void DeleteIfExists(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
#endif
