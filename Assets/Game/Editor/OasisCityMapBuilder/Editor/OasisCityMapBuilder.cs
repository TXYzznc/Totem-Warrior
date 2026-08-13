using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.EditorTools.OasisCity
{
    internal static class OasisCityMapBuilder
    {
        private const string LayoutPath = "Assets/Game/Editor/OasisCityMapBuilder/Data/OasisCityLayout.json";
        private const string ScenePath = "Assets/Game/Scene/OasisCity.unity";
        private const string MapAnchorAuthoringScriptPath = "Assets/Game/Scripts/Runtime/TotemMapAnchorAuthoring.cs";
        private const string DataRoot = "Assets/Game/Scene/OasisCityData";
        private const string MeshRoot = DataRoot + "/Meshes";
        private const string MaterialRoot = DataRoot + "/Materials";
        private const string TerrainRoot = DataRoot + "/Terrain";
        private const string BuildingRoot = "Assets/Game/Prefabs/Environment/OasisCity/Buildings";
        private const string DecorationRoot = "Assets/Game/Prefabs/Environment/OasisCity/Decorations";
        private const string TerrainSandTexturePath = "Assets/Game/Textures/Environment/OasisCity/Terrain/T_Oasis_Terrain_Sand_BaseColor.png";

        private const float GroundY = 2f;
        private const float RoadY = 2.035f;
        private const float BuildingY = 2.04f;
        private const float WaterY = 0.35f;

        private static readonly StaticEditorFlags EnvironmentStaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.NavigationStatic;

        private static readonly StaticEditorFlags NonGiEnvironmentStaticFlags =
            EnvironmentStaticFlags & ~StaticEditorFlags.ContributeGI;

        private static readonly Vector3[] WestWallFallback =
        {
            new(-52f, 0f, 376f), new(-110f, 0f, 370f), new(-188f, 0f, 330f),
            new(-232f, 0f, 255f), new(-245f, 0f, 165f), new(-240f, 0f, 40f),
            new(-227f, 0f, -100f), new(-235f, 0f, -205f), new(-210f, 0f, -285f),
            new(-172f, 0f, -345f), new(-96f, 0f, -374f), new(-45f, 0f, -378f),
        };

        private static readonly Vector3[] EastWallFallback =
        {
            new(12f, 0f, 376f), new(120f, 0f, 365f), new(190f, 0f, 326f),
            new(230f, 0f, 252f), new(247f, 0f, 165f), new(241f, 0f, 45f),
            new(230f, 0f, -85f), new(241f, 0f, -218f), new(220f, 0f, -300f),
            new(176f, 0f, -350f), new(100f, 0f, -371f), new(12f, 0f, -377f),
        };

        [MenuItem("Game Framework/GameTools/Oasis City/Build Complete Map")]
        public static void BuildCompleteMap()
        {
            OasisCityLayoutData layout = LoadLayout();
            ValidateLayout(layout);

            // Folder creation must be visible to the AssetDatabase before the
            // generated materials are written. Wrapping both operations in a
            // StartAssetEditing block leaves the new folders undiscoverable.
            RecreateGeneratedDataFolders();
            CreateMaterialsAndTerrainLayers();
            CreateBuildingCollisionMeshes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "OasisCity";

            GameObject root = NewNode("ENV_OasisCity", null);
            GameObject world = NewNode("00_World", root.transform);
            GameObject terrainRoot = NewNode("10_Terrain", world.transform);
            GameObject waterRoot = NewNode("20_Water", world.transform);
            GameObject roadRoot = NewNode("30_Roads", world.transform);
            GameObject wallRoot = NewNode("40_CityWalls", world.transform);
            GameObject bridgeRoot = NewNode("50_Bridges", world.transform);
            GameObject buildingRoot = NewNode("60_Buildings", root.transform);
            GameObject decorationRoot = NewNode("70_Decorations", root.transform);
            GameObject placeholderRoot = NewNode("75_DecorationPlaceholders", root.transform);
            GameObject gameplayRoot = NewNode("80_GameplayMarkers", root.transform);
            GameObject lightingRoot = NewNode("90_Lighting", root.transform);
            GameObject reviewRoot = NewNode("99_Review", root.transform);

            BuildTerrain(layout, terrainRoot.transform);
            BuildRiver(layout, waterRoot.transform);
            BuildRoadNetwork(layout, roadRoot.transform);
            BuildWalls(wallRoot.transform);
            BuildBridges(layout, bridgeRoot.transform);
            BuildBuildings(layout, buildingRoot.transform);
            BuildAvailableDecorations(layout, decorationRoot.transform);
            BuildMissingDecorationPlaceholders(layout, placeholderRoot.transform);
            BuildGameplayMarkers(layout, gameplayRoot.transform);
            BuildLighting(lightingRoot.transform);
            BuildReviewObjects(reviewRoot.transform);
            ConfigureBakedGi(root);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save scene: {ScenePath}");
            }
            EnsureSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                $"[OasisCityMapBuilder] Built {layout.buildings.Length} buildings, " +
                $"{layout.spawns.Length} spawn markers and the complete city skeleton: {ScenePath}");
        }

        [MenuItem("Game Framework/GameTools/Oasis City/Validate Generated Map")]
        public static void ValidateGeneratedMap()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject root = GameObject.Find("ENV_OasisCity");
            if (scene.path != ScenePath || root == null)
            {
                Debug.LogError($"[OasisCityMapBuilder] Open {ScenePath} before validation.");
                return;
            }

            int buildings = root.transform.Find("60_Buildings")?.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name.StartsWith("BF-", StringComparison.Ordinal)) ?? 0;
            int spawns = root.transform.Find("80_GameplayMarkers")?.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name.StartsWith("SP", StringComparison.Ordinal)) ?? 0;
            int placeholders = root.transform.Find("75_DecorationPlaceholders")?.childCount ?? 0;
            Transform buildingContainer = root.transform.Find("60_Buildings");
            MeshCollider[] buildingColliders = buildingContainer != null
                ? buildingContainer.GetComponentsInChildren<MeshCollider>(true)
                    .Where(item => item.transform.name.StartsWith("BF-", StringComparison.Ordinal))
                    .ToArray()
                : Array.Empty<MeshCollider>();
            int validBuildingColliders = buildingColliders.Count(item => item.sharedMesh != null && !item.convex);
            int missingMeshes = root.GetComponentsInChildren<MeshFilter>(true)
                .Count(item => item.sharedMesh == null);
            int missingMaterials = root.GetComponentsInChildren<MeshRenderer>(true)
                .Count(item => item.sharedMaterial == null);
            Transform decorationContainer = root.transform.Find("70_Decorations");
            bool allAvailableDecorationsPresent = Enumerable.Range(1, 12).All(type =>
                decorationContainer != null && decorationContainer.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.StartsWith($"DE-{type:00}-", StringComparison.Ordinal)));
            bool allMissingTypesRepresented = Enumerable.Range(13, 20).All(type =>
                root.transform.Find("75_DecorationPlaceholders")?.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.StartsWith($"DE-{type:00}-占位-", StringComparison.Ordinal)) == true);

            bool valid = buildings == 152 && spawns == 20 && placeholders > 0 &&
                         validBuildingColliders == 152 &&
                         allAvailableDecorationsPresent && allMissingTypesRepresented &&
                         missingMeshes == 0 && missingMaterials == 0;
            string message =
                $"buildings={buildings}/152, spawns={spawns}/20, placeholders={placeholders}, " +
                $"buildingColliders={validBuildingColliders}/152, " +
                $"availableDecorations={allAvailableDecorationsPresent}, missingTypes={allMissingTypesRepresented}, " +
                $"missingMeshes={missingMeshes}, missingMaterials={missingMaterials}";
            if (valid)
            {
                Debug.Log("[OasisCityMapBuilder] Validation passed: " + message);
            }
            else
            {
                Debug.LogError("[OasisCityMapBuilder] Validation failed: " + message);
            }
        }

        [MenuItem("Game Framework/GameTools/Oasis City/Upgrade Gameplay Anchors")]
        public static void UpgradeGameplayAnchors()
        {
            OasisCityLayoutData layout = LoadLayout();
            ValidateLayout(layout);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForUpgrade = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpgrade)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "ENV_OasisCity");
            Transform gameplayRoot = root != null ? root.transform.Find("80_GameplayMarkers") : null;
            if (root == null || gameplayRoot == null)
            {
                throw new InvalidOperationException($"OasisCity scene contract is missing ENV_OasisCity/80_GameplayMarkers: {ScenePath}");
            }

            foreach (Transform item in gameplayRoot.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
            }
            foreach (OasisSpawnData spawn in layout.spawns)
            {
                Transform marker = gameplayRoot.Find(spawn.id);
                if (marker != null)
                {
                    UnityEngine.Object.DestroyImmediate(marker.gameObject);
                }
            }
            foreach (string markerRootName in new[] { "MapResourceAnchors", "ExtractionAnchors" })
            {
                Transform markerRoot = gameplayRoot.Find(markerRootName);
                if (markerRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(markerRoot.gameObject);
                }
            }
            foreach (OasisSpawnData spawn in layout.spawns)
            {
                FindOrCreateMarker(
                    gameplayRoot,
                    spawn.id,
                    new Vector3(spawn.x, GroundY + 0.12f, spawn.z));
            }
            EnsureGameplayAuthoring(layout, root, gameplayRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save upgraded gameplay anchors: {ScenePath}");
            }

            if (openedForUpgrade)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("[OasisCityMapBuilder] Gameplay anchor authoring upgraded: 20 player, 20 resource, 7 extraction anchors.");
        }

        [MenuItem("Game Framework/GameTools/Oasis City/Audit Building Mesh Structure")]
        public static void AuditBuildingMeshStructure()
        {
            foreach (int type in new[] { 1, 17, 24 })
            {
                string path = $"{BuildingRoot}/PF_Oasis_BF{type:00}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MeshFilter[] filters = prefab != null ? prefab.GetComponentsInChildren<MeshFilter>(true) : Array.Empty<MeshFilter>();
                string details = string.Join("\n", filters.Take(80).Select(filter =>
                {
                    Bounds bounds = filter.sharedMesh != null ? filter.sharedMesh.bounds : default;
                    return $"{GetHierarchyPath(filter.transform, prefab.transform)} | size={bounds.size}";
                }));
                Debug.Log($"[OasisCityMapBuilder] BF-{type:00}: meshFilters={filters.Length}\n{details}");
            }
        }

        [MenuItem("Game Framework/GameTools/Oasis City/Validate All Building Entrances")]
        public static void ValidateAllBuildingEntrances()
        {
            OasisCityLayoutData layout = LoadLayout();
            GameObject root = GameObject.Find("ENV_OasisCity");
            Transform buildingRoot = root != null ? root.transform.Find("60_Buildings") : null;
            if (buildingRoot == null)
            {
                Debug.LogError("[OasisCityMapBuilder] Building root is missing from the active OasisCity scene.");
                return;
            }

            Dictionary<string, Transform> instances = buildingRoot.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.StartsWith("BF-", StringComparison.Ordinal))
                .GroupBy(item => item.name)
                .ToDictionary(group => group.Key, group => group.First());
            List<string> failures = new();
            foreach (OasisBuildingData item in layout.buildings)
            {
                if (!instances.TryGetValue(item.id, out Transform instance))
                {
                    failures.Add(item.id + ": instance missing");
                    continue;
                }

                Transform outsideMarker = instance.Find("_Navigation_D01/_DoorOutside");
                Transform insideMarker = instance.Find("_Navigation_D01/_DoorInside");
                if (outsideMarker == null || insideMarker == null)
                {
                    failures.Add(item.id + ": D01 navigation link missing");
                    continue;
                }

                bool foundOutside = NavMesh.SamplePosition(outsideMarker.position, out NavMeshHit outsideHit, 1.25f, NavMesh.AllAreas);
                bool foundInside = NavMesh.SamplePosition(insideMarker.position, out NavMeshHit insideHit, 1.25f, NavMesh.AllAreas);
                if (!foundOutside || !foundInside)
                {
                    failures.Add($"{item.id}: sample outside={foundOutside}, inside={foundInside}");
                    continue;
                }

                NavMeshPath path = new();
                bool calculated = NavMesh.CalculatePath(outsideHit.position, insideHit.position, NavMesh.AllAreas, path);
                if (!calculated || path.status != NavMeshPathStatus.PathComplete)
                {
                    failures.Add($"{item.id}: {path.status}");
                    continue;
                }

                Vector3 torsoOffset = Vector3.up * 0.85f;
                if (Physics.Linecast(
                        outsideMarker.position + torsoOffset,
                        insideMarker.position + torsoOffset,
                        out RaycastHit obstruction,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore) &&
                    (obstruction.collider.transform == instance || obstruction.collider.transform.IsChildOf(instance)))
                {
                    failures.Add($"{item.id}: physical doorway blocked by {obstruction.collider.name}");
                }
            }

            if (failures.Count == 0)
            {
                Debug.Log("[OasisCityMapBuilder] Entrance validation passed: 152/152 buildings have a complete door-crossing NavMesh path.");
            }
            else
            {
                Debug.LogError($"[OasisCityMapBuilder] Entrance validation failed: {failures.Count}/152\n{string.Join("\n", failures)}");
            }
        }

        [MenuItem("Game Framework/GameTools/Oasis City/Rebuild Review Cameras")]
        public static void RebuildReviewCameras()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject root = GameObject.Find("ENV_OasisCity");
            Transform reviewRoot = root != null ? root.transform.Find("99_Review") : null;
            if (scene.path != ScenePath || reviewRoot == null)
            {
                Debug.LogError($"[OasisCityMapBuilder] Open {ScenePath} before rebuilding review cameras.");
                return;
            }

            Undo.SetCurrentGroupName("重建 OasisCity 审阅摄像机");
            int undoGroup = Undo.GetCurrentGroup();
            for (int index = reviewRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = reviewRoot.GetChild(index);
                if (child.name == "ReviewCamera" || child.name == "CameraGroups")
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            Transform groupsRoot = BuildReviewObjects(reviewRoot);
            Undo.RegisterCreatedObjectUndo(groupsRoot.gameObject, "创建 OasisCity 审阅摄像机");
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = groupsRoot.gameObject;
            Debug.Log("[OasisCityMapBuilder] Rebuilt 14 review cameras in 4 groups.");
        }

        private static bool TryGetPrimaryDoorProbe(Transform instance, out Vector3 center, out Vector3 outward)
        {
            MeshFilter[] frames = instance.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null &&
                                 filter.name.IndexOf("STR_D01_", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (frames.Length == 0)
            {
                center = default;
                outward = default;
                return false;
            }

            bool initialized = false;
            Bounds aggregate = default;
            foreach (MeshFilter frame in frames)
            {
                Bounds meshBounds = frame.sharedMesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 localCorner = new(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);
                    Vector3 instanceLocal = instance.InverseTransformPoint(frame.transform.TransformPoint(localCorner));
                    if (!initialized)
                    {
                        aggregate = new Bounds(instanceLocal, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        aggregate.Encapsulate(instanceLocal);
                    }
                }
            }

            center = new Vector3(aggregate.center.x, 0.18f, aggregate.center.z);
            if (aggregate.size.x >= aggregate.size.z)
            {
                outward = new Vector3(0f, 0f, Mathf.Sign(Mathf.Approximately(center.z, 0f) ? 1f : center.z));
            }
            else
            {
                outward = new Vector3(Mathf.Sign(Mathf.Approximately(center.x, 0f) ? 1f : center.x), 0f, 0f);
            }
            return true;
        }

        private static OasisCityLayoutData LoadLayout()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (asset == null)
            {
                throw new FileNotFoundException("Oasis layout manifest is missing.", LayoutPath);
            }

            OasisCityLayoutData layout = JsonUtility.FromJson<OasisCityLayoutData>(asset.text);
            if (layout == null)
            {
                throw new InvalidDataException("Oasis layout manifest could not be parsed.");
            }

            return layout;
        }

        private static void ValidateLayout(OasisCityLayoutData layout)
        {
            if (layout.schemaVersion != 1 || layout.buildings == null || layout.buildings.Length != 152)
            {
                throw new InvalidDataException("Oasis layout must contain schema v1 and exactly 152 buildings.");
            }

            if (layout.spawns == null || layout.spawns.Length != 20)
            {
                throw new InvalidDataException("Oasis layout must contain exactly 20 spawn points.");
            }

            int[] expected = { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 6, 6, 6, 7, 30, 24, 15, 17, 12, 10, 5, 2 };
            for (int type = 1; type <= 24; type++)
            {
                int actual = layout.buildings.Count(item => item.type == type);
                if (actual != expected[type])
                {
                    throw new InvalidDataException($"BF-{type:00} allocation mismatch: {actual}/{expected[type]}");
                }
            }
        }

        private static void RecreateGeneratedDataFolders()
        {
            if (AssetDatabase.IsValidFolder(DataRoot))
            {
                AssetDatabase.DeleteAsset(DataRoot);
            }

            EnsureFolder("Assets/Game/Scene", "OasisCityData");
            EnsureFolder(DataRoot, "Meshes");
            EnsureFolder(DataRoot, "Materials");
            EnsureFolder(DataRoot, "Terrain");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateMaterialsAndTerrainLayers()
        {
            CreateMaterial("MAT_Oasis_Sand", new Color(0.55f, 0.40f, 0.23f), 0.08f, 0f);
            CreateMaterial("MAT_Oasis_Road", new Color(0.46f, 0.36f, 0.25f), 0.22f, 0f);
            CreateMaterial("MAT_Oasis_Wall", new Color(0.58f, 0.43f, 0.27f), 0.16f, 0f);
            CreateMaterial("MAT_Oasis_Bridge", new Color(0.35f, 0.20f, 0.12f), 0.18f, 0f);
            CreateMaterial("MAT_Oasis_Water", new Color(0.02f, 0.42f, 0.48f, 1f), 0.76f, 0.05f);
            CreateMaterial("MAT_Oasis_Placeholder", Color.white, 0.1f, 0f);
            CreateMaterial("MAT_Oasis_Review", new Color(0.06f, 0.55f, 0.95f), 0.25f, 0f);

            ConfigureTerrainTextureImport(TerrainSandTexturePath);
            Texture2D sandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainSandTexturePath) ??
                                    CreateColorTexture("T_Oasis_Sand_Fallback", new Color(0.55f, 0.41f, 0.25f));
            TerrainLayer sandLayer = new()
            {
                name = "TL_Oasis_Sand",
                diffuseTexture = sandTexture,
                tileSize = new Vector2(9f, 9f),
                smoothness = 0.05f,
                metallic = 0f,
            };
            AssetDatabase.CreateAsset(sandLayer, TerrainRoot + "/TL_Oasis_Sand.terrainlayer");
        }

        private static void ConfigureTerrainTextureImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            bool dirty = importer.wrapMode != TextureWrapMode.Repeat ||
                         importer.npotScale != TextureImporterNPOTScale.None ||
                         !importer.mipmapEnabled || importer.maxTextureSize != 2048 ||
                         importer.textureCompression != TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (dirty) importer.SaveAndReimport();
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            AssetDatabase.CreateAsset(material, MaterialRoot + "/" + name + ".mat");
            return material;
        }

        private static Texture2D CreateColorTexture(string name, Color color)
        {
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, true) { name = name };
            Color[] pixels = Enumerable.Repeat(color, 16).ToArray();
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            AssetDatabase.CreateAsset(texture, TerrainRoot + "/" + name + ".asset");
            return texture;
        }

        private static void CreateBuildingCollisionMeshes()
        {
            string[] structuralTokens =
            {
                "FLOOR", "WALL", "CEILING", "ROOF", "STAIR", "STEP",
                "LANDING", "RAIL", "PARAPET", "COLUMN", "PILLAR",
            };

            string[] modelPaths = Enumerable.Range(1, 24)
                .SelectMany(type => AssetDatabase.GetDependencies($"{BuildingRoot}/PF_Oasis_BF{type:00}.prefab", true))
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            SetModelReadability(modelPaths, true);
            try
            {
                for (int type = 1; type <= 24; type++)
                {
                    string prefabPath = $"{BuildingRoot}/PF_Oasis_BF{type:00}.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null) throw new FileNotFoundException($"Building prefab missing: {prefabPath}");

                    CombineInstance[] combines = prefab.GetComponentsInChildren<MeshFilter>(true)
                        .Where(filter => filter.sharedMesh != null &&
                                         structuralTokens.Any(filter.name.ToUpperInvariant().Contains))
                        .Select(filter => new CombineInstance
                        {
                            mesh = filter.sharedMesh,
                            transform = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                        })
                        .ToArray();

                    if (combines.Length == 0)
                    {
                        throw new InvalidDataException($"BF-{type:00} produced no structural collision geometry.");
                    }

                    Mesh mesh = new()
                    {
                        name = $"M_Collision_BF{type:00}",
                        indexFormat = IndexFormat.UInt32,
                    };
                    mesh.CombineMeshes(combines, true, true, false);
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, $"{MeshRoot}/M_Collision_BF{type:00}.asset");
                }
            }
            finally
            {
                SetModelReadability(modelPaths, false);
            }
        }

        private static void SetModelReadability(IEnumerable<string> modelPaths, bool readable)
        {
            foreach (string modelPath in modelPaths)
            {
                if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer || importer.isReadable == readable)
                    continue;
                importer.isReadable = readable;
                importer.SaveAndReimport();
            }
        }

        private static void AppendBoundsBox(Bounds bounds, Matrix4x4 matrix, List<Vector3> vertices, List<int> triangles)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners =
            {
                new(min.x, min.y, min.z), new(max.x, min.y, min.z),
                new(min.x, max.y, min.z), new(max.x, max.y, min.z),
                new(min.x, min.y, max.z), new(max.x, min.y, max.z),
                new(min.x, max.y, max.z), new(max.x, max.y, max.z),
            };
            int vertex = vertices.Count;
            vertices.AddRange(corners.Select(matrix.MultiplyPoint3x4));
            AddBoxTriangles(triangles, vertex);
        }

        private static void BuildTerrain(OasisCityLayoutData layout, Transform parent)
        {
            const int resolution = 513;
            TerrainData data = new()
            {
                name = "TD_OasisCity",
                heightmapResolution = resolution,
                alphamapResolution = 512,
                baseMapResolution = 1024,
                size = new Vector3(layout.mapWidth, 20f, layout.mapLength),
                terrainLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainRoot + "/TL_Oasis_Sand.terrainlayer") },
            };

            float[,] heights = new float[resolution, resolution];
            for (int zIndex = 0; zIndex < resolution; zIndex++)
            {
                float worldZ = Mathf.Lerp(-layout.mapLength * 0.5f, layout.mapLength * 0.5f, zIndex / (resolution - 1f));
                OasisRiverSample sample = NearestRiverSample(layout.river, worldZ);
                for (int xIndex = 0; xIndex < resolution; xIndex++)
                {
                    float worldX = Mathf.Lerp(-layout.mapWidth * 0.5f, layout.mapWidth * 0.5f, xIndex / (resolution - 1f));
                    float border = Mathf.Min(layout.mapWidth * 0.5f - Mathf.Abs(worldX), layout.mapLength * 0.5f - Mathf.Abs(worldZ));
                    float edgeDune = Mathf.Clamp01((30f - border) / 30f) * 2.1f;
                    float subtle = (Mathf.PerlinNoise(worldX * 0.018f + 41f, worldZ * 0.018f + 17f) - 0.5f) * 0.12f;
                    float absoluteHeight = GroundY + edgeDune + subtle;
                    if (sample != null)
                    {
                        float center = (sample.leftX + sample.rightX) * 0.5f;
                        float halfWidth = Mathf.Max(4f, (sample.rightX - sample.leftX) * 0.5f);
                        // A short bank transition keeps the river disconnected
                        // from the walkable terrain during NavMesh baking; only
                        // the six authored bridge decks should connect both banks.
                        float riverFactor = 1f - Mathf.Clamp01((Mathf.Abs(worldX - center) - halfWidth) / 2.5f);
                        absoluteHeight = Mathf.Lerp(absoluteHeight, -1.5f, riverFactor);
                    }
                    heights[zIndex, xIndex] = Mathf.Clamp01((absoluteHeight + 2f) / 20f);
                }
            }
            data.SetHeights(0, 0, heights);
            AssetDatabase.CreateAsset(data, TerrainRoot + "/TD_OasisCity.asset");

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terrain_OasisCity_512x768";
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.position = new Vector3(-layout.mapWidth * 0.5f, -2f, -layout.mapLength * 0.5f);
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 500f;
            terrain.shadowCastingMode = ShadowCastingMode.On;
            GameObjectUtility.SetStaticEditorFlags(terrainObject, EnvironmentStaticFlags);
        }

        private static OasisRiverSample NearestRiverSample(OasisRiverSample[] samples, float z)
        {
            if (samples == null || samples.Length == 0) return null;
            OasisRiverSample nearest = samples[0];
            float distance = Mathf.Abs(nearest.z - z);
            for (int i = 1; i < samples.Length; i++)
            {
                float candidate = Mathf.Abs(samples[i].z - z);
                if (candidate < distance)
                {
                    distance = candidate;
                    nearest = samples[i];
                }
            }
            return nearest;
        }

        private static void BuildRiver(OasisCityLayoutData layout, Transform parent)
        {
            List<Vector3> left = new();
            List<Vector3> right = new();
            foreach (OasisRiverSample sample in layout.river.OrderBy(item => item.z))
            {
                left.Add(new Vector3(sample.leftX - 0.8f, WaterY, sample.z));
                right.Add(new Vector3(sample.rightX + 0.8f, WaterY, sample.z));
            }
            GameObject river = CreateStripObject("River_Main", left, right, GetMaterial("MAT_Oasis_Water"), parent, false);
            river.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void BuildRoadNetwork(OasisCityLayoutData layout, Transform parent)
        {
            List<(Vector3[] points, float width)> roads = new()
            {
                (P((-210,310),(-165,340),(-105,342),(-62,320)), 7f),
                (P((-220,255),(-175,285),(-120,290),(-75,270)), 6f),
                (P((25,320),(80,345),(145,325),(205,275)), 7f),
                (P((25,260),(85,285),(150,265),(218,210)), 6f),
                (P((-225,95),(-170,125),(-110,105),(-70,55)), 7f),
                (P((10,130),(75,155),(155,125),(220,75)), 7f),
                (P((-230,-110),(-170,-85),(-105,-105),(-65,-145)), 7f),
                (P((10,-75),(80,-55),(160,-70),(220,-130)), 7f),
                (P((-220,-260),(-160,-225),(-100,-250),(-55,-290)), 7f),
                (P((20,-235),(80,-210),(155,-235),(210,-285)), 7f),
                (P((65,345),(75,245),(70,140),(65,25),(70,-95),(75,-220),(90,-330)), 8f),
                (P((-70,330),(-75,220),(-70,105),(-65,-20),(-70,-145),(-60,-275)), 7f),
                (P((-110,5),(-45,25),(15,20),(85,0)), 8f),
                (P((-110,-205),(-50,-220),(10,-215),(80,-190)), 7f),
            };

            roads.AddRange(new[]
            {
                (ClearanceRing(layout, -148f, 282f, 82f, 62f, false), 5.5f),
                (ClearanceRing(layout, 128f, 275f, 88f, 72f, true), 5.5f),
                (ClearanceRing(layout, -153f, 105f, 80f, 72f, false), 5.5f),
                (ClearanceRing(layout, 137f, 92f, 86f, 72f, true), 5.5f),
                (ClearanceRing(layout, -150f, -78f, 82f, 70f, false), 5.5f),
                (ClearanceRing(layout, 135f, -88f, 84f, 70f, true), 5.5f),
                (ClearanceRing(layout, -145f, -265f, 80f, 66f, false), 5.5f),
                (ClearanceRing(layout, 105f, -285f, 74f, 68f, true), 5.5f),
                (P((-223,338),(-175,362),(-105,370),(-54,360)), 6f),
                (P((18,360),(95,370),(168,350),(220,310)), 6f),
                (P((-215,-332),(-150,-360),(-75,-370),(-42,-360)), 6f),
                (P((18,-360),(90,-368),(165,-345),(210,-315)), 6f),
            });

            Material material = GetMaterial("MAT_Oasis_Road");
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();
            foreach ((Vector3[] points, float width) in roads)
            {
                AppendPolylineStrip(layout, points, width, RoadY, vertices, triangles, uvs);
            }
            Mesh mesh = SaveMesh("M_Oasis_RoadNetwork", vertices, triangles, uvs);
            GameObject road = NewMeshObject("RoadNetwork_Combined", mesh, material, parent);
            GameObjectUtility.SetStaticEditorFlags(road, EnvironmentStaticFlags);
        }

        private static Vector3[] P(params (float x, float z)[] points)
        {
            return points.Select(item => new Vector3(item.x, 0f, item.z)).ToArray();
        }

        private static Vector3[] EllipsePoints(float centerX, float centerZ, float radiusX, float radiusZ, int segments)
        {
            Vector3[] points = new Vector3[segments + 1];
            for (int index = 0; index <= segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                points[index] = new Vector3(
                    centerX + Mathf.Cos(angle) * radiusX,
                    0f,
                    centerZ + Mathf.Sin(angle) * radiusZ);
            }
            return points;
        }

        private static Vector3[] ClearanceRing(
            OasisCityLayoutData layout,
            float centerX,
            float centerZ,
            float radiusX,
            float radiusZ,
            bool eastDistrict)
        {
            const int segments = 64;
            const int stateCount = 19;
            const float firstScale = 0.5f;
            const float scaleStep = 0.05f;
            const float infinity = 1e20f;
            float[,] nodeCost = new float[segments, stateCount];
            Vector3[,] candidates = new Vector3[segments, stateCount];

            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                for (int state = 0; state < stateCount; state++)
                {
                    float scale = firstScale + state * scaleStep;
                    Vector3 point = new(
                        centerX + Mathf.Cos(angle) * radiusX * scale,
                        0f,
                        centerZ + Mathf.Sin(angle) * radiusZ * scale);
                    candidates[segment, state] = point;
                    float clearance = MinimumBuildingClearance(layout.buildings, point.x, point.z);
                    OasisRiverSample river = NearestRiverSample(layout.river, point.z);
                    bool clearOfRiver = eastDistrict
                        ? point.x > river.rightX + 3.5f
                        : point.x < river.leftX - 3.5f;
                    bool insideCity = Mathf.Abs(point.x) < 232f && Mathf.Abs(point.z) < 358f;
                    nodeCost[segment, state] = clearance >= 3.55f && clearOfRiver && insideCity
                        ? Mathf.Pow(scale - 1f, 2f) * 4f
                        : infinity;
                }
            }

            float bestCost = infinity;
            int[] bestStates = null;
            for (int start = 0; start < stateCount; start++)
            {
                if (nodeCost[0, start] >= infinity) continue;
                float[,] costs = new float[segments, stateCount];
                int[,] parents = new int[segments, stateCount];
                for (int segment = 0; segment < segments; segment++)
                for (int state = 0; state < stateCount; state++)
                    costs[segment, state] = infinity;
                costs[0, start] = nodeCost[0, start];

                for (int segment = 1; segment < segments; segment++)
                {
                    for (int state = 0; state < stateCount; state++)
                    {
                        if (nodeCost[segment, state] >= infinity) continue;
                        for (int previous = 0; previous < stateCount; previous++)
                        {
                            if (costs[segment - 1, previous] >= infinity) continue;
                            float smoothness = Mathf.Pow((state - previous) * scaleStep, 2f) * 32f;
                            float candidateCost = costs[segment - 1, previous] + nodeCost[segment, state] + smoothness;
                            if (candidateCost >= costs[segment, state]) continue;
                            costs[segment, state] = candidateCost;
                            parents[segment, state] = previous;
                        }
                    }
                }

                for (int end = 0; end < stateCount; end++)
                {
                    float closure = Mathf.Pow((end - start) * scaleStep, 2f) * 32f;
                    float total = costs[segments - 1, end] + closure;
                    if (total >= bestCost) continue;
                    bestCost = total;
                    bestStates = new int[segments];
                    bestStates[segments - 1] = end;
                    for (int segment = segments - 1; segment > 0; segment--)
                        bestStates[segment - 1] = parents[segment, bestStates[segment]];
                }
            }

            if (bestStates == null)
                return EllipsePoints(centerX, centerZ, radiusX, radiusZ, segments);

            Vector3[] result = new Vector3[segments + 1];
            for (int segment = 0; segment < segments; segment++)
                result[segment] = candidates[segment, bestStates[segment]];
            result[segments] = result[0];
            return result;
        }

        private static float MinimumBuildingClearance(OasisBuildingData[] buildings, float x, float z)
        {
            float minimum = float.PositiveInfinity;
            foreach (OasisBuildingData building in buildings)
            {
                float radians = building.yaw * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);
                float dx = x - building.x;
                float dz = z - building.z;
                float localX = dx * cos - dz * sin;
                float localZ = dx * sin + dz * cos;
                float outsideX = Mathf.Max(Mathf.Abs(localX) - building.sizeX * 0.5f, 0f);
                float outsideZ = Mathf.Max(Mathf.Abs(localZ) - building.sizeZ * 0.5f, 0f);
                minimum = Mathf.Min(minimum, Mathf.Sqrt(outsideX * outsideX + outsideZ * outsideZ));
            }
            return minimum;
        }

        private static void BuildWalls(Transform parent)
        {
            Material wallMaterial = GetMaterial("MAT_Oasis_Wall");
            CreateWallMesh("CityWall_West", WestWallFallback, 2.2f, 5.5f, wallMaterial, parent);
            CreateWallMesh("CityWall_East", EastWallFallback, 2.2f, 5.5f, wallMaterial, parent);
            CreateWallMesh("CityWall_NorthWest", new[] { WestWallFallback[0], WestWallFallback[1] }, 2.2f, 5.5f, wallMaterial, parent);
            CreateWallMesh("CityWall_NorthEast", new[] { EastWallFallback[0], EastWallFallback[1] }, 2.2f, 5.5f, wallMaterial, parent);
            CreateWallMesh("CityWall_SouthWest", new[] { WestWallFallback[^2], WestWallFallback[^1] }, 2.2f, 5.5f, wallMaterial, parent);
            CreateWallMesh("CityWall_SouthEast", new[] { EastWallFallback[^2], EastWallFallback[^1] }, 2.2f, 5.5f, wallMaterial, parent);
        }

        private static void CreateWallMesh(string name, IReadOnlyList<Vector3> points, float width, float height, Material material, Transform parent)
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();
            for (int index = 0; index < points.Count - 1; index++)
            {
                Vector3 start = points[index];
                Vector3 end = points[index + 1];
                Vector3 direction = (end - start).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
                int vertex = vertices.Count;
                float bottom = GroundY - 0.2f;
                float top = GroundY + height;
                vertices.Add(new Vector3(start.x, bottom, start.z) - side);
                vertices.Add(new Vector3(start.x, bottom, start.z) + side);
                vertices.Add(new Vector3(start.x, top, start.z) - side);
                vertices.Add(new Vector3(start.x, top, start.z) + side);
                vertices.Add(new Vector3(end.x, bottom, end.z) - side);
                vertices.Add(new Vector3(end.x, bottom, end.z) + side);
                vertices.Add(new Vector3(end.x, top, end.z) - side);
                vertices.Add(new Vector3(end.x, top, end.z) + side);
                AddBoxTriangles(triangles, vertex);
                for (int i = 0; i < 8; i++) uvs.Add(Vector2.zero);
            }
            Mesh mesh = SaveMesh("M_" + name, vertices, triangles, uvs);
            GameObject wall = NewMeshObject(name, mesh, material, parent);
            MeshCollider collider = wall.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(wall, EnvironmentStaticFlags);
        }

        private static void AddBoxTriangles(List<int> triangles, int v)
        {
            int[] indices =
            {
                0,2,1, 1,2,3, 4,5,6, 5,7,6,
                0,4,2, 4,6,2, 1,3,5, 5,3,7,
                2,6,3, 3,6,7, 0,1,4, 1,5,4,
            };
            triangles.AddRange(indices.Select(index => v + index));
        }

        private static void BuildBridges(OasisCityLayoutData layout, Transform parent)
        {
            float[] bridgeZ = { 260f, 150f, 30f, -90f, -220f, -335f };
            Material material = GetMaterial("MAT_Oasis_Bridge");
            for (int index = 0; index < bridgeZ.Length; index++)
            {
                OasisRiverSample sample = NearestRiverSample(layout.river, bridgeZ[index]);
                float center = (sample.leftX + sample.rightX) * 0.5f;
                float length = sample.rightX - sample.leftX + 8f;
                GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = $"Bridge_{index + 1:00}";
                bridge.transform.SetParent(parent, false);
                bridge.transform.position = new Vector3(center, GroundY + 0.15f, bridgeZ[index]);
                bridge.transform.localScale = new Vector3(length, 0.3f, 7f);
                bridge.GetComponent<MeshRenderer>().sharedMaterial = material;
                GameObjectUtility.SetStaticEditorFlags(bridge, EnvironmentStaticFlags);
            }

            CreateSpawnDock(layout, "SP02", material, parent);
            CreateSpawnDock(layout, "SP13", material, parent);
            OasisBuildingData riverService = layout.buildings.First(item => item.id == "BF-12-01");
            GameObject entrancePlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entrancePlatform.name = "EntrancePlatform_BF-12-01";
            entrancePlatform.transform.SetParent(parent, false);
            entrancePlatform.transform.position = new Vector3(riverService.x - 5.8f, GroundY + 0.075f, riverService.z + 0.32f);
            entrancePlatform.transform.localScale = new Vector3(4.8f, 0.15f, 2.4f);
            entrancePlatform.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(entrancePlatform, EnvironmentStaticFlags);

            OasisSpawnData eastSpawn = layout.spawns.First(item => item.id == "SP08");
            GameObject spawnAccess = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnAccess.name = "SpawnAccess_SP08";
            spawnAccess.transform.SetParent(parent, false);
            spawnAccess.transform.position = new Vector3(eastSpawn.x - 5.8f, GroundY + 0.35f, eastSpawn.z);
            spawnAccess.transform.localScale = new Vector3(14f, 0.2f, 4f);
            spawnAccess.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(spawnAccess, EnvironmentStaticFlags);
        }

        private static void CreateSpawnDock(OasisCityLayoutData layout, string spawnId, Material material, Transform parent)
        {
            OasisSpawnData spawn = layout.spawns.First(item => item.id == spawnId);
            OasisRiverSample river = NearestRiverSample(layout.river, spawn.z);
            float bankX = river.leftX - 2.2f;
            float centerX = (spawn.x + bankX) * 0.5f;
            float length = Mathf.Abs(spawn.x - bankX) + 3.5f;
            GameObject dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dock.name = $"SpawnDock_{spawnId}";
            dock.transform.SetParent(parent, false);
            dock.transform.position = new Vector3(centerX, GroundY + 0.15f, spawn.z);
            dock.transform.localScale = new Vector3(length, 0.3f, 5f);
            dock.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(dock, EnvironmentStaticFlags);
        }

        private static void BuildBuildings(OasisCityLayoutData layout, Transform parent)
        {
            Dictionary<string, Transform> districts = new();
            foreach (string district in new[] { "North", "CentralNorth", "Central", "CentralSouth", "South" })
            {
                districts[district] = NewNode(district, parent).transform;
            }

            Dictionary<int, GameObject> prefabs = new();
            for (int type = 1; type <= 24; type++)
            {
                string path = $"{BuildingRoot}/PF_Oasis_BF{type:00}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new FileNotFoundException($"Building prefab missing: {path}");
                prefabs[type] = prefab;
            }

            foreach (OasisBuildingData item in layout.buildings.OrderBy(item => item.id))
            {
                string district = item.z switch
                {
                    > 230f => "North",
                    > 75f => "CentralNorth",
                    > -75f => "Central",
                    > -230f => "CentralSouth",
                    _ => "South",
                };
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[item.type], districts[district]);
                instance.name = item.id;
                instance.transform.SetPositionAndRotation(
                    new Vector3(item.x, BuildingY, item.z),
                    Quaternion.Euler(0f, item.yaw, 0f));
                instance.transform.localScale = Vector3.one;
                Mesh collisionMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshRoot}/M_Collision_BF{item.type:00}.asset");
                MeshCollider collider = instance.AddComponent<MeshCollider>();
                collider.sharedMesh = collisionMesh;
                collider.convex = false;
                ConfigureBuildingLod(instance, item);
                ConfigureBuildingEntranceLink(instance.transform);
                SetStaticRecursively(instance, EnvironmentStaticFlags);
            }
        }

        private static void ConfigureBuildingLod(GameObject instance, OasisBuildingData item)
        {
            Renderer[] detailRenderers = instance.GetComponentsInChildren<Renderer>(true);
            GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proxy.name = "_LOD1_Proxy";
            proxy.transform.SetParent(instance.transform, false);
            proxy.transform.localPosition = new Vector3(0f, item.sizeY * 0.5f, 0f);
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = new Vector3(item.sizeX, item.sizeY, item.sizeZ);
            UnityEngine.Object.DestroyImmediate(proxy.GetComponent<Collider>());
            MeshRenderer proxyRenderer = proxy.GetComponent<MeshRenderer>();
            proxyRenderer.sharedMaterial = GetMaterial("MAT_Oasis_Wall");
            proxyRenderer.shadowCastingMode = ShadowCastingMode.On;
            proxyRenderer.receiveShadows = true;

            LODGroup group = instance.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(new[]
            {
                new LOD(0.065f, detailRenderers),
                new LOD(0.008f, new Renderer[] { proxyRenderer }),
            });
            group.RecalculateBounds();
        }

        private static void ConfigureBuildingEntranceLink(Transform instance)
        {
            if (!TryGetPrimaryDoorProbe(instance, out Vector3 doorCenterLocal, out Vector3 outwardLocal)) return;
            GameObject navigation = NewNode("_Navigation_D01", instance);
            Transform outside = NewNode("_DoorOutside", navigation.transform).transform;
            Transform inside = NewNode("_DoorInside", navigation.transform).transform;
            outside.localPosition = doorCenterLocal + outwardLocal * 1.65f;
            inside.localPosition = doorCenterLocal - outwardLocal * 1.65f;
            OffMeshLink link = navigation.AddComponent<OffMeshLink>();
            link.startTransform = outside;
            link.endTransform = inside;
            link.biDirectional = true;
            link.activated = true;
            link.autoUpdatePositions = false;
            link.costOverride = -1f;
        }

        private static void BuildAvailableDecorations(OasisCityLayoutData layout, Transform parent)
        {
            Dictionary<int, GameObject[]> prefabs = new();
            for (int type = 1; type <= 12; type++)
            {
                string[] guids = AssetDatabase.FindAssets($"PF_Oasis_DE{type:00} t:Prefab", new[] { DecorationRoot });
                GameObject[] found = guids.Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                    .Where(item => item != null)
                    .ToArray();
                if (found.Length == 0) throw new FileNotFoundException($"No DE-{type:00} prefabs found under {DecorationRoot}");
                prefabs[type] = found;
            }

            System.Random random = new(20260805);
            int sequence = 0;
            void Place(int type, Vector3 position, float yaw, float scale = 1f)
            {
                GameObject[] variants = prefabs[type];
                GameObject prefab = variants[sequence++ % variants.Length];
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = $"DE-{type:00}-{sequence:000}";
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                instance.transform.localScale = Vector3.one * scale;
                SetStaticRecursively(instance, EnvironmentStaticFlags);
            }

            // Every available family is represented. Repetition is deliberately
            // irregular to avoid a costly, visibly tiled city-wide pattern.
            for (int i = 0; i < 48; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 22f + (i % 7) * 3.1f;
                Place(1, new Vector3(Mathf.Cos(angle) * radius, RoadY + 0.04f, Mathf.Sin(angle) * radius), i * 29f);
            }
            for (int i = 0; i < 12; i++) Place(2, new Vector3(-12f + i * 2.1f, RoadY + 0.03f, 18f), 90f);
            for (int i = 0; i < 8; i++) Place(3, new Vector3(24f + i * 2.2f, GroundY + 0.08f, 20f + (i % 2) * 1.4f), i * 12f);
            for (int i = 0; i < 18; i++) Place(4, new Vector3(-55f + (i % 3) * 3f, GroundY + 0.1f, 315f - i * 35f), 0f);
            foreach (Vector3 p in P((-115,260),(125,255),(-145,65),(135,45),(-130,-150),(135,-165))) Place(5, p + Vector3.up * GroundY, 0f);

            OasisBuildingData[] ordered = layout.buildings.OrderBy(item => item.id).ToArray();
            for (int i = 0; i < 36; i++)
            {
                OasisBuildingData b = ordered[(i * 17) % ordered.Length];
                Vector3 side = Quaternion.Euler(0f, b.yaw, 0f) * new Vector3((i % 2 == 0 ? 1 : -1) * (b.sizeX * 0.5f + 1.1f), 0f, 0f);
                Place(6, new Vector3(b.x, GroundY, b.z) + side, b.yaw + (i % 2 == 0 ? 0f : 180f), 0.92f + (float)random.NextDouble() * 0.14f);
            }
            for (int i = 0; i < 22; i++) Place(7, new Vector3(-205f + (i % 2) * 410f, GroundY, 300f - i * 29f), i * 47f, 0.9f + (i % 4) * 0.06f);
            for (int i = 0; i < 16; i++)
            {
                OasisBuildingData b = ordered[(i * 29 + 7) % ordered.Length];
                Place(8, new Vector3(b.x, BuildingY + Mathf.Max(3f, b.sizeY * 0.55f), b.z), b.yaw + 90f);
            }
            for (int i = 0; i < 42; i++) Place(9, new Vector3((i % 2 == 0 ? -1 : 1) * (215f - i % 5 * 3f), GroundY, 330f - i * 16f), i * 31f, 0.85f + (i % 5) * 0.06f);
            for (int i = 0; i < 10; i++) Place(10, new Vector3(-145f + (i % 5) * 72f, GroundY + 3.25f, 112f - (i / 5) * 215f), i * 19f);
            for (int i = 0; i < 14; i++)
            {
                OasisBuildingData b = ordered[(i * 23 + 3) % ordered.Length];
                Place(11, new Vector3(b.x, GroundY + 1.3f, b.z) + Quaternion.Euler(0f, b.yaw, 0f) * new Vector3(0f, 0f, b.sizeZ * 0.5f + 0.05f), b.yaw);
            }
            for (int i = 0; i < 16; i++) Place(12, new Vector3(-205f + (i % 2) * 410f, GroundY, 305f - i * 40f), i % 2 == 0 ? 90f : -90f);
        }

        private static void BuildMissingDecorationPlaceholders(OasisCityLayoutData layout, Transform parent)
        {
            Dictionary<int, Vector3> sizes = new()
            {
                [13]=new(.25f,.8f,.25f), [14]=new(.35f,1.5f,.35f), [15]=new(.18f,2.5f,.18f),
                [16]=new(.2f,.2f,1.5f), [17]=new(.8f,1.2f,.2f), [18]=new(2f,1.5f,.04f),
                [19]=new(.7f,.5f,.08f), [20]=new(.45f,.8f,.45f), [21]=new(.8f,1.1f,.4f),
                [22]=new(.8f,.8f,.8f), [23]=new(1.6f,.45f,.45f), [24]=new(1.8f,.03f,1.2f),
                [25]=new(2f,2.2f,1f), [26]=new(1.2f,2.4f,.12f), [27]=new(2f,1.1f,.15f),
                [28]=new(1.4f,.18f,2.2f), [29]=new(.12f,2.5f,.12f), [30]=new(.8f,1f,.8f),
                [31]=new(1.5f,.25f,1f), [32]=new(.5f,.9f,.5f),
            };
            Dictionary<int, int> counts = new()
            {
                [13]=16,[14]=6,[15]=6,[16]=6,[17]=5,[18]=8,[19]=14,[20]=18,[21]=4,[22]=10,
                [23]=10,[24]=6,[25]=6,[26]=6,[27]=6,[28]=4,[29]=6,[30]=7,[31]=10,[32]=6,
            };
            Material material = GetMaterial("MAT_Oasis_Placeholder");
            OasisBuildingData[] buildings = layout.buildings.OrderBy(item => item.id).ToArray();
            foreach ((int type, int count) in counts)
            {
                for (int index = 0; index < count; index++)
                {
                    OasisBuildingData building = buildings[(type * 19 + index * 31) % buildings.Length];
                    Vector3 size = sizes[type];
                    float sideSign = index % 2 == 0 ? 1f : -1f;
                    Vector3 offset = Quaternion.Euler(0f, building.yaw, 0f) *
                                     new Vector3(sideSign * (building.sizeX * 0.5f + 1.3f), 0f, -building.sizeZ * 0.18f);
                    GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    placeholder.name = $"DE-{type:00}-占位-{index + 1:000}";
                    placeholder.transform.SetParent(parent, false);
                    placeholder.transform.position = new Vector3(building.x, GroundY + size.y * 0.5f, building.z) + offset;
                    placeholder.transform.rotation = Quaternion.Euler(0f, building.yaw, 0f);
                    placeholder.transform.localScale = size;
                    placeholder.GetComponent<MeshRenderer>().sharedMaterial = material;
                    GameObjectUtility.SetStaticEditorFlags(placeholder, NonGiEnvironmentStaticFlags);
                }
            }
        }

        private static void BuildGameplayMarkers(OasisCityLayoutData layout, Transform parent)
        {
            Material material = GetMaterial("MAT_Oasis_Review");
            foreach (OasisSpawnData spawn in layout.spawns.OrderBy(item => item.id))
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = spawn.id;
                marker.transform.SetParent(parent, false);
                marker.transform.SetPositionAndRotation(new Vector3(spawn.x, GroundY + 0.08f, spawn.z), Quaternion.Euler(0f, spawn.yaw, 0f));
                marker.transform.localScale = new Vector3(1.1f, 0.08f, 1.1f);
                marker.GetComponent<MeshRenderer>().sharedMaterial = material;
                marker.GetComponent<Collider>().enabled = false;
            }

            EnsureGameplayAuthoring(layout, parent.root.gameObject, parent);

            OasisSpawnData eastSpawn = layout.spawns.First(item => item.id == "SP08");
            GameObject accessLinkObject = NewNode("_Navigation_SP08", parent);
            Transform citySide = NewNode("_CitySide", accessLinkObject.transform).transform;
            Transform spawnSide = NewNode("_SpawnSide", accessLinkObject.transform).transform;
            citySide.position = new Vector3(eastSpawn.x - 12f, GroundY + 0.25f, eastSpawn.z);
            spawnSide.position = new Vector3(eastSpawn.x, GroundY + 0.5f, eastSpawn.z);
            OffMeshLink accessLink = accessLinkObject.AddComponent<OffMeshLink>();
            accessLink.startTransform = citySide;
            accessLink.endTransform = spawnSide;
            accessLink.biDirectional = true;
            accessLink.activated = true;
            accessLink.autoUpdatePositions = false;
            accessLink.costOverride = -1f;
        }

        private static void EnsureGameplayAuthoring(OasisCityLayoutData layout, GameObject root, Transform gameplayRoot)
        {
            TotemMapSceneAuthoring sceneAuthoring = root.GetComponent<TotemMapSceneAuthoring>() ?? root.AddComponent<TotemMapSceneAuthoring>();
            sceneAuthoring.Configure(
                new Vector2(-layout.mapWidth * 0.5f, -layout.mapLength * 0.5f),
                new Vector2(layout.mapWidth * 0.5f, layout.mapLength * 0.5f));

            Transform resourceRoot = FindOrCreateMarkerRoot(gameplayRoot, "MapResourceAnchors");
            Transform extractionRoot = FindOrCreateMarkerRoot(gameplayRoot, "ExtractionAnchors");
            foreach (OasisSpawnData spawn in layout.spawns.OrderBy(item => item.id))
            {
                Transform spawnMarker = gameplayRoot.Find(spawn.id);
                if (spawnMarker == null)
                {
                    throw new InvalidOperationException($"Missing authored player spawn marker: {spawn.id}");
                }

                EnsureAnchor(spawnMarker.gameObject, spawn.id, 1, 8f);

                int index = int.Parse(spawn.id.Substring(2));
                string resourceId = $"RS{index:00}";
                Vector3 source = new Vector3(spawn.x, GroundY + 0.12f, spawn.z);
                Vector3 inward = Vector3.zero - source;
                inward.y = 0f;
                Vector3 resourcePosition = source + inward.normalized * 14f;
                Transform resource = FindOrCreateMarker(resourceRoot, resourceId, resourcePosition);
                EnsureAnchor(resource.gameObject, resourceId, 7, 5f);

                if ((index - 1) % 3 == 0)
                {
                    int extractionIndex = (index - 1) / 3 + 1;
                    string extractionId = $"EX{extractionIndex:00}";
                    Vector3 extractionPosition = source + inward.normalized * 6f;
                    Transform extraction = FindOrCreateMarker(extractionRoot, extractionId, extractionPosition);
                    EnsureAnchor(extraction.gameObject, extractionId, 6, 7f);
                }
            }
        }

        private static Transform FindOrCreateMarkerRoot(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing : NewNode(name, parent).transform;
        }

        private static Transform FindOrCreateMarker(Transform parent, string name, Vector3 position)
        {
            Transform marker = parent.Find(name);
            if (marker == null)
            {
                marker = NewNode(name, parent).transform;
            }
            marker.position = position;
            return marker;
        }

        private static void EnsureAnchor(GameObject gameObject, string id, int kindValue, float radius)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(MapAnchorAuthoringScriptPath);
            if (script == null)
            {
                throw new InvalidOperationException($"Missing map anchor authoring script: {MapAnchorAuthoringScriptPath}");
            }

            Type componentType = script.GetClass();
            if (componentType == null)
            {
                throw new InvalidOperationException($"Unable to resolve runtime type from {MapAnchorAuthoringScriptPath}.");
            }

            Component component = gameObject.AddComponent(componentType);
            if (component is not TotemMapAnchorAuthoring authoring)
            {
                throw new InvalidOperationException($"Unable to bind {MapAnchorAuthoringScriptPath} to {gameObject.name}.");
            }
            authoring.Configure(id, (TotemMapAnchorKind)kindValue, radius, true);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject sun = NewNode("Sun_Main", parent);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.63f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.82f;
            light.lightmapBakeType = LightmapBakeType.Baked;

            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.31f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.23f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.09f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.54f, 0.43f, 0.31f);
            RenderSettings.fogStartDistance = 420f;
            RenderSettings.fogEndDistance = 1350f;

            BuildLightProbes(parent);
            BuildReflectionProbes(parent);
        }

        private static void BuildLightProbes(Transform parent)
        {
            GameObject probeObject = NewNode("LightProbes_CityGrid", parent);
            LightProbeGroup group = probeObject.AddComponent<LightProbeGroup>();
            List<Vector3> probes = new(585);
            float[] heights = { 2.2f, 7.5f, 18f };
            for (int zIndex = 0; zIndex < 13; zIndex++)
            {
                float z = Mathf.Lerp(-330f, 330f, zIndex / 12f);
                for (int xIndex = 0; xIndex < 15; xIndex++)
                {
                    float x = Mathf.Lerp(-210f, 210f, xIndex / 14f);
                    for (int heightIndex = 0; heightIndex < heights.Length; heightIndex++)
                        probes.Add(new Vector3(x, heights[heightIndex], z));
                }
            }
            group.probePositions = probes.ToArray();
        }

        private static void BuildReflectionProbes(Transform parent)
        {
            (string name, Vector3 position, Vector3 size)[] definitions =
            {
                ("ReflectionProbe_North", new Vector3(0f, 28f, 250f), new Vector3(440f, 70f, 230f)),
                ("ReflectionProbe_Central", new Vector3(0f, 25f, 0f), new Vector3(420f, 65f, 250f)),
                ("ReflectionProbe_South", new Vector3(0f, 26f, -255f), new Vector3(440f, 70f, 230f)),
                ("ReflectionProbe_River", new Vector3(0f, 16f, 25f), new Vector3(130f, 45f, 680f)),
                ("ReflectionProbe_WestBoundary", new Vector3(-190f, 24f, 0f), new Vector3(150f, 65f, 700f)),
                ("ReflectionProbe_EastBoundary", new Vector3(190f, 24f, 0f), new Vector3(150f, 65f, 700f)),
            };
            foreach ((string probeName, Vector3 position, Vector3 size) in definitions)
            {
                GameObject probeObject = NewNode(probeName, parent);
                probeObject.transform.position = position;
                ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Baked;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.resolution = 256;
                probe.size = size;
                probe.boxProjection = true;
                probe.blendDistance = 18f;
            }
        }

        internal static Transform BuildReviewObjects(Transform parent)
        {
            GameObject cameraGroups = NewNode("CameraGroups", parent);
            Transform overviewGroup = NewNode("城市全景", cameraGroups.transform).transform;
            Transform districtGroup = NewNode("街区构图", cameraGroups.transform).transform;
            Transform waterBoundaryGroup = NewNode("水体与边界", cameraGroups.transform).transform;
            Transform buildingCloseupGroup = NewNode("建筑特写", cameraGroups.transform).transform;

            CreateReviewCamera(
                "CAM_Overview_SouthEast", overviewGroup,
                new Vector3(410f, 510f, -500f), new Vector3(0f, 8f, 0f), 42f, true);
            CreateReviewCamera(
                "CAM_Overview_NorthWest", overviewGroup,
                new Vector3(-430f, 390f, 520f), new Vector3(0f, 8f, 20f), 45f, false);
            CreateReviewCamera(
                "CAM_Overview_Top", overviewGroup,
                new Vector3(0f, 650f, -80f), new Vector3(0f, 0f, 0f), 38f, false);

            CreateReviewCamera(
                "CAM_District_North", districtGroup,
                new Vector3(245f, 92f, 385f), new Vector3(118f, 8f, 275f), 48f, false);
            CreateReviewCamera(
                "CAM_District_Central", districtGroup,
                new Vector3(-175f, 58f, 145f), new Vector3(-35f, 7f, 25f), 50f, false);
            CreateReviewCamera(
                "CAM_District_South", districtGroup,
                new Vector3(-235f, 72f, -360f), new Vector3(-90f, 7f, -255f), 48f, false);

            CreateReviewCamera(
                "CAM_River_Bridge03", waterBoundaryGroup,
                new Vector3(135f, 46f, 105f), new Vector3(-8f, 2f, 30f), 44f, false);
            CreateReviewCamera(
                "CAM_Boundary_WestWall", waterBoundaryGroup,
                new Vector3(-335f, 82f, 5f), new Vector3(-225f, 15f, 5f), 46f, false);

            OasisCityLayoutData layout = LoadLayout();
            CreateBuildingCloseupCamera(
                "CAM_Building_Tower_BF01", buildingCloseupGroup, layout, "BF-01-01", new Vector3(-1f, 0f, -1f), 36f);
            CreateBuildingCloseupCamera(
                "CAM_Building_Civic_BF06", buildingCloseupGroup, layout, "BF-06-01", new Vector3(1f, 0f, -1f), 38f);
            CreateBuildingCloseupCamera(
                "CAM_Building_RiverService_BF12", buildingCloseupGroup, layout, "BF-12-01", new Vector3(-1f, 0f, 1f), 34f);
            CreateBuildingCloseupCamera(
                "CAM_Building_Courtyard_BF07", buildingCloseupGroup, layout, "BF-07-01", new Vector3(1f, 0f, 1f), 38f);
            CreateBuildingCloseupCamera(
                "CAM_Building_Bazaar_BF24", buildingCloseupGroup, layout, "BF-24-01", new Vector3(-1f, 0f, 1f), 34f);
            CreateBuildingCloseupCamera(
                "CAM_Building_Residential_BF18", buildingCloseupGroup, layout, "BF-18-01", new Vector3(1f, 0f, -1f), 36f);
            return cameraGroups.transform;
        }

        private static Camera CreateBuildingCloseupCamera(
            string cameraName,
            Transform parent,
            OasisCityLayoutData layout,
            string buildingId,
            Vector3 viewingDirection,
            float fieldOfView)
        {
            OasisBuildingData building = null;
            for (int index = 0; index < layout.buildings.Length; index++)
            {
                if (layout.buildings[index].id == buildingId)
                {
                    building = layout.buildings[index];
                    break;
                }
            }

            if (building == null)
                throw new InvalidOperationException($"Review-camera building is missing from layout: {buildingId}");

            Vector3 target = new(
                building.x,
                BuildingY + building.sizeY * 0.55f,
                building.z);
            Vector3 horizontalDirection = new Vector3(viewingDirection.x, 0f, viewingDirection.z).normalized;
            float footprint = Mathf.Max(building.sizeX, building.sizeZ);
            float distance = Mathf.Max(22f, footprint * 1.45f);
            float height = Mathf.Max(8f, building.sizeY * 0.45f);
            Vector3 position = target + horizontalDirection * distance + Vector3.up * height;
            return CreateReviewCamera(cameraName, parent, position, target, fieldOfView, false);
        }

        private static Camera CreateReviewCamera(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 lookAt,
            float fieldOfView,
            bool enabled)
        {
            GameObject cameraObject = NewNode(name, parent);
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt, Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1400f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.enabled = enabled;
            UniversalAdditionalCameraData additionalData = camera.GetUniversalAdditionalCameraData();
            additionalData.renderPostProcessing = true;
            if (enabled)
                camera.tag = "MainCamera";
            return camera;
        }

        private static GameObject CreateStripObject(string name, List<Vector3> left, List<Vector3> right, Material material, Transform parent, bool collider)
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();
            for (int index = 0; index < left.Count; index++)
            {
                vertices.Add(left[index]);
                vertices.Add(right[index]);
                float v = index / Mathf.Max(1f, left.Count - 1f);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
                if (index == left.Count - 1) continue;
                int vertex = index * 2;
                triangles.AddRange(new[] { vertex, vertex + 2, vertex + 1, vertex + 1, vertex + 2, vertex + 3 });
            }
            Mesh mesh = SaveMesh("M_" + name, vertices, triangles, uvs);
            GameObject result = NewMeshObject(name, mesh, material, parent);
            if (collider)
            {
                MeshCollider meshCollider = result.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
            GameObjectUtility.SetStaticEditorFlags(result, EnvironmentStaticFlags);
            return result;
        }

        private static void AppendPolylineStrip(
            OasisCityLayoutData layout,
            IReadOnlyList<Vector3> points,
            float width,
            float y,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            for (int index = 0; index < points.Count - 1; index++)
            {
                Vector3 start = points[index];
                Vector3 end = points[index + 1];
                Vector3 midpoint = (start + end) * 0.5f;
                float requiredClearance = width * 0.5f + 0.6f;
                if (MinimumBuildingClearance(layout.buildings, start.x, start.z) < requiredClearance ||
                    MinimumBuildingClearance(layout.buildings, midpoint.x, midpoint.z) < requiredClearance ||
                    MinimumBuildingClearance(layout.buildings, end.x, end.z) < requiredClearance)
                {
                    continue;
                }
                Vector3 direction = (end - start).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
                int vertex = vertices.Count;
                vertices.Add(new Vector3(start.x, y, start.z) - side);
                vertices.Add(new Vector3(start.x, y, start.z) + side);
                vertices.Add(new Vector3(end.x, y, end.z) - side);
                vertices.Add(new Vector3(end.x, y, end.z) + side);
                uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
                triangles.AddRange(new[] { vertex, vertex + 2, vertex + 1, vertex + 1, vertex + 2, vertex + 3 });
            }
        }

        private static Mesh SaveMesh(string name, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            Mesh mesh = new() { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            AssetDatabase.CreateAsset(mesh, MeshRoot + "/" + name + ".asset");
            return mesh;
        }

        private static GameObject NewMeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject result = NewNode(name, parent);
            result.AddComponent<MeshFilter>().sharedMesh = mesh;
            result.AddComponent<MeshRenderer>().sharedMaterial = material;
            return result;
        }

        private static Material GetMaterial(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{name}.mat");
            if (material == null) throw new FileNotFoundException($"Generated material missing: {name}");
            return material;
        }

        private static GameObject NewNode(string name, Transform parent)
        {
            GameObject node = new(name);
            if (parent != null) node.transform.SetParent(parent, false);
            return node;
        }

        private static void EnsureSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            int existing = scenes.FindIndex(item => string.Equals(item.path, ScenePath, StringComparison.Ordinal));
            if (existing >= 0)
            {
                scenes[existing] = new EditorBuildSettingsScene(ScenePath, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string GetHierarchyPath(Transform current, Transform root)
        {
            List<string> parts = new();
            while (current != null)
            {
                parts.Add(current.name);
                if (current == root) break;
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void SetStaticRecursively(GameObject root, StaticEditorFlags flags)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            }
        }

        private static void ConfigureBakedGi(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEligibleForBakedGi(renderer.transform, root.transform))
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                        GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~StaticEditorFlags.ContributeGI);
                    continue;
                }

                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI);
                SerializedObject serializedRenderer = new(renderer);
                SerializedProperty receiveGi = serializedRenderer.FindProperty("m_ReceiveGI");
                SerializedProperty scaleInLightmap = serializedRenderer.FindProperty("m_ScaleInLightmap");
                if (receiveGi != null) receiveGi.intValue = (int)ReceiveGI.Lightmaps;
                if (scaleInLightmap != null) scaleInLightmap.floatValue = GetLightmapScale(renderer);
                serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool IsEligibleForBakedGi(Transform current, Transform root)
        {
            while (current != null && current != root)
            {
                if (current.name == "20_Water" || current.name == "75_DecorationPlaceholders" ||
                    current.name == "80_GameplayMarkers" || current.name == "90_Lighting" || current.name == "99_Review" ||
                    current.name.StartsWith("_Navigation", StringComparison.Ordinal))
                    return false;
                current = current.parent;
            }
            return true;
        }

        private static float GetLightmapScale(Renderer renderer)
        {
            string name = renderer.transform.root.name + "/" + renderer.name;
            if (name.Contains("BF-01", StringComparison.Ordinal) || name.Contains("BF-06", StringComparison.Ordinal) ||
                name.Contains("BF-12", StringComparison.Ordinal) || name.Contains("Bridge03", StringComparison.Ordinal))
                return 1.25f;
            if (name.Contains("Terrain", StringComparison.Ordinal) || name.Contains("Road", StringComparison.Ordinal) ||
                name.Contains("Wall", StringComparison.Ordinal) || name.Contains("_LOD1_Proxy", StringComparison.Ordinal))
                return 0.18f;
            if (name.StartsWith("DE-", StringComparison.Ordinal))
                return 0.32f;
            return 0.55f;
        }
    }
}
