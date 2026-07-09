using UnityEngine;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Globalization;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class LaunchProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GFTrace.Info("Procedure", "Launch.Enter", null, GFTrace.Data("editorResourceMode", GFBuiltin.Base.EditorResourceMode.ToString()));
        InitSettings();
        GFTrace.Info("Procedure", "Launch.ChangeState", null, GFTrace.Data("next", GFBuiltin.Base.EditorResourceMode ? nameof(LoadHotfixDllProcedure) : nameof(UpdateResourcesProcedure)));
        ChangeState(procedureOwner, GFBuiltin.Base.EditorResourceMode ? typeof(LoadHotfixDllProcedure) : typeof(UpdateResourcesProcedure));
    }

    private void InitSettings()
    {
        CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
        GFBuiltin.Debugger.ActiveWindow = AppSettings.Instance.DebugMode;
        GFBuiltin.Debugger.WindowScale = 1.4f;
    }
}