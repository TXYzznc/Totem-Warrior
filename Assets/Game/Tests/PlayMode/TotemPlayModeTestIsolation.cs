#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class TotemPlayModeTestIsolation
{
    private const string LaunchSceneName = "Launch";
    private const int MaxFrames = 600;

    public static IEnumerator LoadFreshLaunchScene()
    {
        TotemGameRuntime runtime = TotemGameRuntime.Instance;
        if (runtime == null || !runtime.ServicesReady)
        {
            if (SceneManager.GetActiveScene().name != LaunchSceneName)
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(LaunchSceneName, LoadSceneMode.Single);
                Assert.NotNull(operation, "Launch scene should be available from build settings.");
                for (int frame = 0; frame < MaxFrames && !operation.isDone; frame++)
                {
                    yield return null;
                }

                Assert.IsTrue(operation.isDone, "Launch scene load timed out.");
            }

            for (int frame = 0; frame < MaxFrames; frame++)
            {
                runtime = TotemGameRuntime.Instance;
                if (runtime != null && runtime.ServicesReady)
                {
                    break;
                }

                yield return null;
            }
        }

        Assert.NotNull(runtime, "Launch must create the Totem runtime.");
        Assert.IsTrue(runtime.ServicesReady, "Totem runtime initialization timed out.");

        TotemInputService input = runtime.GetService<TotemInputService>();
        input?.SetInputProvider(null);
        TotemGameFlowService flow = runtime.GetService<TotemGameFlowService>();
        TotemUIService ui = runtime.GetService<TotemUIService>();
        Assert.NotNull(flow, "GameFlow service must be registered.");
        Assert.NotNull(ui, "UI service must be registered.");
        flow?.EnterMainMenu();
        ui?.OpenMainMenu();

        for (int frame = 0; frame < MaxFrames; frame++)
        {
            TotemUISnapshot snapshot = ui.CaptureSnapshot();
            if (!SceneManager.GetSceneByName("OasisCity").isLoaded
                && flow?.CurrentState == TotemGameFlowState.MainMenu
                && snapshot.lastExclusiveView == UIViews.MainMenu.ToString()
                && snapshot.currentFormId > 0)
            {
                yield break;
            }

            if (frame > 0 && frame % 60 == 0 && GFBuiltin.UI != null)
            {
                ui.OpenMainMenu();
            }

            yield return null;
        }

        TotemUISnapshot failedSnapshot = ui.CaptureSnapshot();
        Assert.Fail(
            $"Timed out normalizing the runtime to MainMenu. " +
            $"flow={flow.CurrentState}, view={failedSnapshot.lastExclusiveView}, " +
            $"formId={failedSnapshot.currentFormId}, canUseGFUI={failedSnapshot.canUseGFUI}, " +
            $"oasisLoaded={SceneManager.GetSceneByName("OasisCity").isLoaded}.");
    }
}
#endif
