using UnityEditor;

[InitializeOnLoad]
internal static class OasisLookDevPreviewBakeBootstrap
{
    private static OasisLookDevSession session;
    private static OasisLookDevBakeController controller;

    static OasisLookDevPreviewBakeBootstrap() => EditorApplication.delayCall += Begin;

    private static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!activeScene.IsValid()
            || !activeScene.isLoaded
            || activeScene.path != "Assets/Game/Scene/OasisCity.unity")
            return;

        OasisLookDevCatalog catalog = OasisLookDevCatalog.Load();
        if (catalog == null || Lightmapping.isRunning)
            return;
        OasisLookDevAssetUtility.AssignNeutralPreviewBaseline();
        session = new OasisLookDevSession();
        controller = new OasisLookDevBakeController(session);
        controller.Start(catalog, OasisBakeTier.Preview);
    }
}
