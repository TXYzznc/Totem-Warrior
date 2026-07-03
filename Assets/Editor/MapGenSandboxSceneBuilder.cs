using MapGen.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TotemWarrior.EditorTools
{
    public static class MapGenSandboxSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MapGenSandbox.unity";

        [MenuItem("Tools/MapGen/Create Sandbox Scene")]
        public static void CreateSandboxScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("MapGenSandbox");
            root.AddComponent<MapGenSandboxDriver>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(50f, 95f, -65f);
            cameraGo.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.10f);

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[MapGenSandboxSceneBuilder] Saved {ScenePath}");
        }
    }
}
