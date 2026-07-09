using GameFramework.Fsm;
using GameFramework.Procedure;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class WorkspaceProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GF.BuiltinView.HideLoadingProgress();
        GFTrace.Success("Procedure", "Workspace.Enter", null, GFTrace.Data("next", nameof(TotemGameProcedure)));
        GF.Log("进入干净工作区流程，准备切换到 Totem Warrior 业务运行时。");
        ChangeState<TotemGameProcedure>(procedureOwner);
    }
}
