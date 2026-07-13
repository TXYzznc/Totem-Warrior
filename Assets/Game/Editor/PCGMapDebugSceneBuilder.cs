using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PCGMapDebugSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scene/PCGMapDebug.unity";

    [MenuItem("Game/Debug Scenes/Create PCG Map Debug Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f, 1f);

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(32f, 64f, 31.99f);
        cameraGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 38f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraGo.AddComponent<AudioListener>();

        var lightGo = new GameObject("Directional Light");
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;

        var controllerGo = new GameObject("PCG Map Debug Scene Controller");
        controllerGo.AddComponent<PCGMapDebugSceneController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PCG map debug scene created: {ScenePath}");
    }
}
