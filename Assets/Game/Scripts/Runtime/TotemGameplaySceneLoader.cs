using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class TotemGameplaySceneLoader : MonoBehaviour
{
    private const string GameplaySceneName = "TotemGame";
    private static TotemGameplaySceneLoader instance;
    private bool isLoading;

    public static void Begin(TotemGameRuntime runtime)
    {
        if (runtime == null)
        {
            GFTrace.Failure("TotemLoading", "Begin.Failed", "Runtime is unavailable.");
            return;
        }

        if (instance == null)
        {
            var go = new GameObject("[TotemGameplaySceneLoader]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TotemGameplaySceneLoader>();
        }

        if (!instance.isLoading)
        {
            instance.StartCoroutine(instance.LoadAndEnter(runtime));
        }
    }

    private IEnumerator LoadAndEnter(TotemGameRuntime runtime)
    {
        isLoading = true;
        GF.BuiltinView.ShowLoadingProgress(0f);
        SetStage("正在加载游戏场景", 0.05f);

        Scene gameplayScene = SceneManager.GetSceneByName(GameplaySceneName);
        if (!gameplayScene.isLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Additive);
            while (!operation.isDone)
            {
                SetStage("正在加载游戏场景", 0.05f + operation.progress * 0.35f);
                yield return null;
            }

            gameplayScene = SceneManager.GetSceneByName(GameplaySceneName);
        }

        if (!gameplayScene.IsValid() || !gameplayScene.isLoaded)
        {
            SetStage("游戏场景加载失败", 0f);
            GFTrace.Failure("TotemLoading", "Scene.Failed", GameplaySceneName);
            isLoading = false;
            yield break;
        }

        SceneManager.SetActiveScene(gameplayScene);
        SetStage("正在初始化游戏场景", 0.45f);
        yield return null;

        SetStage("正在生成 PCG 地图", 0.60f);
        runtime.GetService<TotemGameFlowService>()?.EnterCombatHud();
        yield return null;

        SetStage("正在生成角色与世界对象", 0.82f);
        yield return null;

        SetStage("正在初始化游戏界面", 0.95f);
        runtime.GetService<TotemUIService>()?.OpenCombatHud();
        yield return null;

        SetStage("进入游戏", 1f);
        yield return null;
        GF.BuiltinView.HideLoadingProgress();
        GFTrace.Success("TotemLoading", "Completed", null, GFTrace.Data("scene", GameplaySceneName));
        isLoading = false;
    }

    private static void SetStage(string stage, float progress)
    {
        GF.BuiltinView.SetLoadingStage(stage);
        GF.BuiltinView.SetLoadingProgress(Mathf.Clamp01(progress));
        GFTrace.Info("TotemLoading", "Stage", null, GFTrace.Data("stage", stage, "progress", progress.ToString("0.00")));
    }
}
