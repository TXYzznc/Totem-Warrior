#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TotemLaunchReloadLifecycleSmokeTests
{
    private const int ReloadCount = 3;
    private const int MaxFrames = 600;

    [UnityTest]
    public IEnumerator Launch_CanReloadRepeatedlyWithoutStaleFrameworkReferences()
    {
        yield return TotemPlayModeTestIsolation.LoadFreshLaunchScene();
        for (int reloadIndex = 0; reloadIndex < ReloadCount; reloadIndex++)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Launch", LoadSceneMode.Single);
            Assert.NotNull(load);
            for (int frame = 0; frame < MaxFrames && !load.isDone; frame++)
            {
                yield return null;
            }

            Assert.That(load.isDone, Is.True, $"Reload {reloadIndex}: Launch load timed out.");
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                if (TotemGameRuntime.Instance != null
                    && TotemGameRuntime.Instance.ServicesReady
                    && GFBuiltin.Instance != null
                    && GFBuiltin.Debugger != null
                    && GFBuiltin.UI != null
                    && GF.DataModel != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.NotNull(GFBuiltin.Instance, $"Reload {reloadIndex}: current GFBuiltin instance.");
            Assert.NotNull(GFBuiltin.Debugger, $"Reload {reloadIndex}: current DebuggerComponent.");
            Assert.NotNull(GFBuiltin.UI, $"Reload {reloadIndex}: current UIComponent.");
            Assert.NotNull(GF.DataModel, $"Reload {reloadIndex}: current DataModelComponent.");
            Assert.That(GFBuiltin.Instance.gameObject.scene.name, Is.EqualTo("Launch"));
            Assert.That(GFBuiltin.Debugger.gameObject.scene.name, Is.EqualTo("Launch"));
            Assert.That(GFBuiltin.UI.gameObject.scene.name, Is.EqualTo("Launch"));
            Assert.That(GF.DataModel.gameObject.scene.name, Is.EqualTo("Launch"));
        }
    }
}
#endif
