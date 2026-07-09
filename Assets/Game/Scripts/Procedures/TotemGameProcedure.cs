using GameFramework.Fsm;
using GameFramework.Procedure;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public sealed class TotemGameProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GFTrace.Info("Procedure", "TotemGame.Enter");

        GF.BuiltinView.HideLoadingProgress();
        var runtime = TotemGameRuntime.EnsureCreated();
        runtime.MarkProcedureEntered(GetType().Name);
        runtime.StartRuntime();

        GFTrace.Success("Procedure", "TotemGame.RuntimeReady", null, GFTrace.Data(
            "runtime", runtime.name,
            "servicesReady", runtime.ServicesReady.ToString()));
        GF.Log("进入 Totem Warrior GF_X 业务运行时。");
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        TotemGameRuntime.Instance?.MarkProcedureLeaving(GetType().Name, isShutdown);
        GFTrace.Info("Procedure", "TotemGame.Leave", null, GFTrace.Data("isShutdown", isShutdown.ToString()));
        base.OnLeave(procedureOwner, isShutdown);
    }
}
