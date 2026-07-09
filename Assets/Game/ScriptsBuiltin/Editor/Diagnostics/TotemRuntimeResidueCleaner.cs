#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UGF.EditorTools
{
    public static class TotemRuntimeResidueCleaner
    {
        private static readonly string[] RuntimeResiduePrefixes =
        {
            "[TotemVFX]",
            "TotemDamageFloat_",
            "TotemVFX_",
            "[TotemVFXVignette]",
        };

        [MenuItem("Game Framework/GameTools/Diagnostics/Cleanup Runtime Residuals", false, 1020)]
        public static void CleanupRuntimeResidualsMenu()
        {
            int removed = CleanupRuntimeResiduals();
            Debug.Log($"Totem runtime residual cleanup removed {removed} object(s).");
        }

        public static int CleanupRuntimeResiduals()
        {
            var objects = FindRuntimeResiduals();
            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null)
                {
                    continue;
                }

                Object.DestroyImmediate(go);
            }

            MarkOpenScenesDirty(objects);
            return objects.Count;
        }

        public static List<GameObject> FindRuntimeResiduals()
        {
            var result = new List<GameObject>();
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                var go = objects[i];
                if (go == null || EditorUtility.IsPersistent(go))
                {
                    continue;
                }

                var scene = go.scene;
                if (!scene.IsValid() || !scene.isLoaded || !IsRuntimeResidueName(go.name) || HasRuntimeResidueAncestor(go.transform))
                {
                    continue;
                }

                result.Add(go);
            }

            return result;
        }

        public static bool IsRuntimeResidueName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            for (int i = 0; i < RuntimeResiduePrefixes.Length; i++)
            {
                if (objectName.StartsWith(RuntimeResiduePrefixes[i], System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRuntimeResidueAncestor(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            var parent = transform.parent;
            while (parent != null)
            {
                if (IsRuntimeResidueName(parent.name))
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        private static void MarkOpenScenesDirty(List<GameObject> removedObjects)
        {
            if (removedObjects == null || removedObjects.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }
    }
}
#endif
