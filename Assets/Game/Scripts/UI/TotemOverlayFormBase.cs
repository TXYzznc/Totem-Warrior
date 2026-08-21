using UnityEngine;

public abstract class TotemOverlayFormBase : TotemUIFormBase
{
    private const string RuntimeRootName = "TotemRuntimeOverlayRoot";

    protected void ClearRuntimeOverlay()
    {
        var existing = transform.Find(RuntimeRootName);
        if (existing == null)
        {
            return;
        }

        Destroy(existing.gameObject);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearRuntimeOverlay();
        UIService?.ForgetOverlay(Id);
        base.OnClose(isShutdown, userData);
    }

}
