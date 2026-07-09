using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using Obfuz;
using System;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
public class HotfixEntry
{
    public static async void StartHotfixLogic(bool enableHotfix)
    {
        Log.Info<bool>("Hotfix Enable:{0}", enableHotfix);
        GFTrace.Info("Hotfix", "Entry.Start", null, GFTrace.Data("enableHotfix", enableHotfix.ToString()));
        AwaitExtension.SubscribeEvent();

        GFBuiltin.Fsm.DestroyFsm<IProcedureManager>();
        var fsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
        var procManager = GameFrameworkEntry.GetModule<IProcedureManager>();
        var appConfig = await AppConfigs.GetInstanceSync();

        ProcedureBase[] procedures = new ProcedureBase[appConfig.Procedures.Length];
        for (int i = 0; i < appConfig.Procedures.Length; i++)
        {
            procedures[i] = Activator.CreateInstance(Type.GetType(appConfig.Procedures[i])) as ProcedureBase;
            GFTrace.Info("Procedure", "Create", null, GFTrace.Data("procedure", appConfig.Procedures[i], "success", (procedures[i] != null).ToString()));
        }

        procManager.Initialize(fsmManager, procedures);
        GFTrace.Success("Procedure", "Initialize", null, GFTrace.Data("count", procedures.Length.ToString(), "start", nameof(PreloadProcedure)));
        procManager.StartProcedure<PreloadProcedure>();
    }
}