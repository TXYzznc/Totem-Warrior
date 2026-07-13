#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;

namespace UGF.EditorTools
{
    public sealed class DependencySourceDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "GF_X Dependency Source Contract";

        public override string Category => "Migration";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckPackages(context);
            CheckOutPackages(context);
            CheckDOTween(context);
            CheckUniTask(context);
            context.Pass("GF_X dependency source contract is configured.");
        }

        private static void CheckPackages(GFDiagnosticScenarioContext context)
        {
            const string manifestPath = "Packages/manifest.json";
            const string lockPath = "Packages/packages-lock.json";

            context.RequireFile(manifestPath);
            context.RequireFile(lockPath);

            string manifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : string.Empty;
            string packageLock = File.Exists(lockPath) ? File.ReadAllText(lockPath) : string.Empty;
            string combined = manifest + "\n" + packageLock;

            AssertNotContains(context, combined, "com.cysharp.unitask", "Package Manager UniTask must not be a root or locked dependency. Use GF_X Assets/Plugins/UniTask.");
            AssertNotContains(context, combined, "com.demigiant.dotween", "Package Manager DOTween must not be a root or locked dependency. Use GF_X Assets/Plugins/DOTween.");

            context.Detail("package.com.cysharp.unitask", combined.Contains("com.cysharp.unitask", StringComparison.Ordinal));
            context.Detail("package.com.demigiant.dotween", combined.Contains("com.demigiant.dotween", StringComparison.Ordinal));
        }

        private static void CheckOutPackages(GFDiagnosticScenarioContext context)
        {
            const string unitySkillsPackage = "OutPackages/Unity-Skills-main/SkillsForUnity";
            const string oldUniTaskClone = "OutPackages/UniTask";

            context.Detail("outpackages.unitySkills.exists", Directory.Exists(unitySkillsPackage));
            context.Detail("forbidden.OutPackages/UniTask", Directory.Exists(oldUniTaskClone));

            context.Assert(Directory.Exists(unitySkillsPackage), "Unity Skills local package must remain available for Packages/manifest.json.");
            context.Assert(!Directory.Exists(oldUniTaskClone), "Archived UniTask upstream clone must not return under OutPackages; use Assets/Plugins/UniTask as the GF_X source.");
        }

        private static void CheckDOTween(GFDiagnosticScenarioContext context)
        {
            const string oldDemigiantPath = "Assets/Demigiant";
            const string dotweenRoot = "Assets/Plugins/DOTween";
            const string dotweenAsmdef = "Assets/Plugins/DOTween/Modules/DOTween.Extension.asmdef";
            const string dotweenSettings = "Assets/Game/DOTweenSettings.asset";

            context.Detail("forbidden.Assets/Demigiant", Directory.Exists(oldDemigiantPath));
            context.Assert(!Directory.Exists(oldDemigiantPath), "Old Demigiant DOTween/DOTweenPro must stay outside the active Unity assets path.");

            context.RequireDirectory(dotweenRoot);
            context.RequireFile(dotweenAsmdef);
            context.RequireFile(dotweenSettings);

            string asmdef = File.Exists(dotweenAsmdef) ? File.ReadAllText(dotweenAsmdef) : string.Empty;
            context.Assert(asmdef.Contains("\"name\": \"DOTween.Extension\"", StringComparison.Ordinal) || asmdef.Contains("\"name\":\"DOTween.Extension\"", StringComparison.Ordinal), "GF_X DOTween module asmdef must be named DOTween.Extension.");

            int dotweenFileCount = CountNonMetaFiles(dotweenRoot);
            context.Detail("dotween.fileCount", dotweenFileCount);
            context.Assert(dotweenFileCount > 0, "GF_X DOTween plugin folder must contain plugin files.");
        }

        private static void CheckUniTask(GFDiagnosticScenarioContext context)
        {
            const string uniTaskRoot = "Assets/Plugins/UniTask";
            const string uniTaskPackage = "Assets/Plugins/UniTask/package.json";
            const string uniTaskRuntimeAsmdef = "Assets/Plugins/UniTask/Runtime/UniTask.asmdef";
            const string uniTaskDOTweenAsmdef = "Assets/Plugins/UniTask/Runtime/External/DOTween/UniTask.DOTween.asmdef";
            const string uniTaskAddressablesAsmdef = "Assets/Plugins/UniTask/Runtime/External/Addressables/UniTask.Addressables.asmdef";

            context.RequireDirectory(uniTaskRoot);
            context.RequireFile(uniTaskPackage);
            context.RequireFile(uniTaskRuntimeAsmdef);
            context.RequireFile(uniTaskDOTweenAsmdef);
            context.RequireFile(uniTaskAddressablesAsmdef);

            string package = File.Exists(uniTaskPackage) ? File.ReadAllText(uniTaskPackage) : string.Empty;
            context.Assert(package.Contains("\"name\": \"com.cysharp.unitask\"", StringComparison.Ordinal) || package.Contains("\"name\":\"com.cysharp.unitask\"", StringComparison.Ordinal), "Embedded GF_X UniTask package metadata must remain present under Assets/Plugins/UniTask.");
            context.Assert(package.Contains("\"version\": \"2.5.10\"", StringComparison.Ordinal) || package.Contains("\"version\":\"2.5.10\"", StringComparison.Ordinal), "Embedded GF_X UniTask version must remain 2.5.10 unless the framework dependency is intentionally upgraded.");

            string dotweenAsmdef = File.Exists(uniTaskDOTweenAsmdef) ? File.ReadAllText(uniTaskDOTweenAsmdef) : string.Empty;
            context.Assert(dotweenAsmdef.Contains("\"DOTween.Extension\"", StringComparison.Ordinal), "UniTask DOTween bridge must reference the active GF_X DOTween.Extension asmdef.");
            context.Assert(!dotweenAsmdef.Contains("\"DOTween.Modules\"", StringComparison.Ordinal), "UniTask DOTween bridge must not reference stale DOTween.Modules asmdef.");

            string addressablesAsmdef = File.Exists(uniTaskAddressablesAsmdef) ? File.ReadAllText(uniTaskAddressablesAsmdef) : string.Empty;
            context.Assert(addressablesAsmdef.Contains("\"UNITASK_ADDRESSABLE_SUPPORT\"", StringComparison.Ordinal), "UniTask Addressables bridge must be guarded by UNITASK_ADDRESSABLE_SUPPORT while Addressables is not a root package.");

            int uniTaskFileCount = CountNonMetaFiles(uniTaskRoot);
            context.Detail("unitask.fileCount", uniTaskFileCount);
            context.Detail("unitask.version", "2.5.10");
            context.Assert(uniTaskFileCount > 0, "GF_X UniTask plugin folder must contain plugin files.");
        }

        private static void AssertNotContains(GFDiagnosticScenarioContext context, string text, string token, string message)
        {
            context.Assert(!text.Contains(token, StringComparison.Ordinal), message);
        }

        private static int CountNonMetaFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Count(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
        }
    }
}
#endif
