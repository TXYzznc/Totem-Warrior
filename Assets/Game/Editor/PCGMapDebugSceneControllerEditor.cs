using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PCGMapDebugSceneController))]
public sealed class PCGMapDebugSceneControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        var controller = (PCGMapDebugSceneController)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("PCG 测试界面", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("地图操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清理地图", GUILayout.Height(28f)))
                {
                    Execute(controller, controller.ClearDebugMap);
                }

                if (GUILayout.Button("生成地图", GUILayout.Height(28f)))
                {
                    Execute(controller, controller.GenerateAndRender);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新当前种子", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.GenerateWithCurrentSeed);
                }

                if (GUILayout.Button("随机种子生成", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.RandomizeSeedAndGenerate);
                }
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("种子与预设", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("上一种子", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.GeneratePreviousSeed);
                }

                if (GUILayout.Button("下一种子", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.GenerateNextSeed);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fast 32x32", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.UseDiagnosticSize);
                }

                if (GUILayout.Button("Full 64x64", GUILayout.Height(24f)))
                {
                    Execute(controller, controller.UseFullSize);
                }
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("运行状态", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(controller.LastStatus, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(74f));
        }
    }

    private static void Execute(PCGMapDebugSceneController controller, System.Action action)
    {
        Undo.RecordObject(controller, "PCG Map Debug Action");
        action();
        EditorUtility.SetDirty(controller);
        if (!Application.isPlaying && controller.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
    }
}
