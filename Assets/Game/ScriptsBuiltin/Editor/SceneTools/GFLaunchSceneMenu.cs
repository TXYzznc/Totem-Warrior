using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UGF.EditorTools
{
    public static class GFLaunchSceneMenu
    {
        private const string LaunchScenePath = "Assets/Game/Scene/Launch.unity";

        [MenuItem("Game Framework/GameTools/Open Launch Scene", false, 1000)]
        public static void OpenLaunchScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Cannot switch to GF_X launch scene while entering or running Play Mode.");
                return;
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == LaunchScenePath)
            {
                Debug.Log($"GF_X launch scene already open: {LaunchScenePath}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Open GF_X launch scene cancelled because current scene changes were not saved.");
                return;
            }

            Scene launchScene = EditorSceneManager.OpenScene(LaunchScenePath, OpenSceneMode.Single);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(LaunchScenePath);
            Debug.Log($"GF_X launch scene opened: {launchScene.path}");
        }
    }
}
