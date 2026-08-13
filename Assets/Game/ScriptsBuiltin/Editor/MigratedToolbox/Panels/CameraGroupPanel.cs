using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 已加载场景摄像机的集中查看、层级分组与临时预览工具。
/// 分组约定：CameraGroups/{组名}/{Camera}。
/// </summary>
[ToolHubItem("场景工具/摄像机组", "查看、分组并快速预览当前已加载场景中的全部摄像机", 8)]
public sealed class CameraGroupPanel : IToolHubPanel
{
    internal const string CameraGroupsRootName = "CameraGroups";
    internal const string UngroupedName = "（未分组）";

    internal sealed class CameraRecord
    {
        public Camera Camera;
        public string SceneName;
        public string GroupName;
        public string HierarchyPath;
    }

    private sealed class TemporaryCameraState
    {
        public Camera Camera;
        public bool Enabled;
        public RenderTexture TargetTexture;
    }

    private sealed class CameraListSection
    {
        public string SceneName;
        public string GroupName;
        public string FoldoutKey;
        public readonly List<CameraRecord> Records = new();
    }

    private static readonly GUIContent QuickGameViewContent =
        new("切换", "Game View 临时切换到此摄像机；无需先选中摄像机。");
    private static readonly GUIContent CurrentGameViewContent =
        new("当前", "此摄像机当前正用于临时 Game View 预览。");

    private readonly List<CameraRecord> cameraRecords = new();
    private readonly List<CameraListSection> visibleSections = new();
    private readonly Dictionary<int, TemporaryCameraState> temporaryStates = new();
    private readonly Dictionary<string, bool> groupFoldouts = new();

    private Vector2 cameraListScroll;
    private Vector2 detailsScroll;
    private string searchText = string.Empty;
    private string newGroupName = string.Empty;
    private Camera selectedCamera;
    private UnityEditor.Editor cameraInspector;
    private UnityEditor.Editor transformInspector;
    private Camera temporaryActiveCamera;
    private List<string> selectedSceneGroups = new() { UngroupedName };
    private string[] selectedSceneGroupOptions = { UngroupedName };
    private bool detailsFoldout;
    private bool hierarchyDirty = true;
    private OasisLookDevCatalog lookDevCatalog;
    private OasisLookDevSession lookDevSession;
    private OasisLookDevBakeController bakeController;
    private bool lookDevFoldout = true;

    public void OnEnable()
    {
        lookDevSession ??= new OasisLookDevSession();
        bakeController ??= new OasisLookDevBakeController(lookDevSession);
        lookDevCatalog = OasisLookDevCatalog.Load();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        Undo.undoRedoPerformed += OnHierarchyChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += RestoreTemporaryEditorState;
        RefreshCameraCache();
    }

    public void OnDisable()
    {
        RestoreTemporaryEditorState();
        UnsubscribeEvents();
    }

    public void OnDestroy()
    {
        RestoreTemporaryEditorState();
        DestroyInspectors();
        UnsubscribeEvents();
    }

    public string GetHelpText()
    {
        return "列出所有已加载场景中的 Camera（包含未激活对象）。\n"
             + "分组通过 CameraGroups/组名 的场景层级保存。\n"
             + "每行的“切换”可直接临时切换 Game View，无需先选中相机。\n"
             + "“Scene View 预览”不修改场景；Game View 临时切换会在关闭面板或点击恢复后还原。";
    }

    public void OnGUI()
    {
        if (hierarchyDirty)
            RefreshCameraCache();

        DrawToolbar();
        DrawTemporarySwitchNotice();
        DrawLookDevControls();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawCameraList();
            DrawSelectedCameraDetails();
        }
    }

    internal IReadOnlyList<CameraRecord> RefreshCameraCache()
    {
        cameraRecords.Clear();
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera camera in cameras)
        {
            if (!IsLoadedSceneCamera(camera))
                continue;

            cameraRecords.Add(new CameraRecord
            {
                Camera = camera,
                SceneName = camera.gameObject.scene.name,
                GroupName = GetGroupName(camera),
                HierarchyPath = GetHierarchyPath(camera.transform),
            });
        }

        cameraRecords.Sort(CompareRecords);
        bool selectionExists = false;
        for (int index = 0; index < cameraRecords.Count; index++)
        {
            if (cameraRecords[index].Camera == selectedCamera)
            {
                selectionExists = true;
                break;
            }
        }

        if (!selectionExists)
            SetSelectedCamera(cameraRecords.Count > 0 ? cameraRecords[0].Camera : null);

        RebuildVisibleSections();
        RefreshSelectedGroupOptions();
        hierarchyDirty = false;
        return cameraRecords;
    }

    internal static bool IsLoadedSceneCamera(Camera camera)
    {
        return camera != null
            && !EditorUtility.IsPersistent(camera)
            && camera.gameObject.scene.IsValid()
            && camera.gameObject.scene.isLoaded;
    }

    internal static string GetGroupName(Camera camera)
    {
        if (camera == null)
            return UngroupedName;

        Transform childUnderRoot = camera.transform;
        Transform current = camera.transform.parent;
        while (current != null)
        {
            if (current.name == CameraGroupsRootName)
                return childUnderRoot == camera.transform ? UngroupedName : childUnderRoot.name;

            childUnderRoot = current;
            current = current.parent;
        }

        return UngroupedName;
    }

    internal static Transform MoveToGroup(Camera camera, string groupName)
    {
        if (!IsLoadedSceneCamera(camera))
            throw new ArgumentException("Camera must belong to a loaded scene.", nameof(camera));
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be empty.", nameof(groupName));

        string normalizedGroupName = groupName.Trim();
        Scene scene = camera.gameObject.scene;
        Transform root = FindCameraGroupsRoot(scene);
        if (root == null)
        {
            GameObject rootObject = new(CameraGroupsRootName);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            Undo.RegisterCreatedObjectUndo(rootObject, "创建摄像机组根节点");
            root = rootObject.transform;
        }

        Transform group = FindDirectChild(root, normalizedGroupName);
        if (group == null)
        {
            GameObject groupObject = new(normalizedGroupName);
            SceneManager.MoveGameObjectToScene(groupObject, scene);
            Undo.RegisterCreatedObjectUndo(groupObject, "创建摄像机组");
            Undo.SetTransformParent(groupObject.transform, root, "设置摄像机组层级");
            group = groupObject.transform;
        }

        Undo.SetTransformParent(camera.transform, group, "移动摄像机到分组");
        EditorSceneManager.MarkSceneDirty(scene);
        return group;
    }

    internal void BeginTemporaryGameViewSwitch(Camera target)
    {
        if (!IsLoadedSceneCamera(target))
            throw new ArgumentException("Camera must belong to a loaded scene.", nameof(target));

        if (temporaryStates.Count == 0)
        {
            RefreshCameraCache();
            foreach (CameraRecord record in cameraRecords)
            {
                Camera camera = record.Camera;
                temporaryStates[camera.GetInstanceID()] = new TemporaryCameraState
                {
                    Camera = camera,
                    Enabled = camera.enabled,
                    TargetTexture = camera.targetTexture,
                };
            }
        }

        foreach (TemporaryCameraState state in temporaryStates.Values)
        {
            if (state.Camera == null)
                continue;

            state.Camera.enabled = state.Camera == target;
            state.Camera.targetTexture = state.Camera == target ? null : state.TargetTexture;
        }

        temporaryActiveCamera = target;
        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();
    }

    internal void RestoreTemporaryGameViewSwitch()
    {
        if (temporaryStates.Count == 0)
            return;

        foreach (TemporaryCameraState state in temporaryStates.Values)
        {
            if (state.Camera == null)
                continue;

            state.Camera.enabled = state.Enabled;
            state.Camera.targetTexture = state.TargetTexture;
        }

        temporaryStates.Clear();
        temporaryActiveCamera = null;
        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label($"摄像机 {cameraRecords.Count}", EditorStyles.boldLabel, GUILayout.Width(84f));
            EditorGUI.BeginChangeCheck();
            searchText = GUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.MinWidth(120f));
            if (EditorGUI.EndChangeCheck())
                RebuildVisibleSections();
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                RefreshCameraCache();
            using (new EditorGUI.DisabledScope(temporaryStates.Count == 0))
            {
                if (GUILayout.Button("恢复 Game View", EditorStyles.toolbarButton, GUILayout.Width(112f)))
                    RestoreTemporaryGameViewSwitch();
            }
        }
    }

    private void DrawTemporarySwitchNotice()
    {
        if (temporaryStates.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "Game View 正处于临时摄像机切换状态。关闭此工具、保存场景、切换播放模式或点击“恢复 Game View”都会还原原始状态。",
                MessageType.Warning);
        }
    }

    private void DrawLookDevControls()
    {
        EditorGUILayout.Space(3f);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            lookDevFoldout = EditorGUILayout.Foldout(lookDevFoldout, "OasisCity LookDev / 光照烘焙", true);
            if (!lookDevFoldout)
                return;

            if (lookDevCatalog == null)
            {
                EditorGUILayout.HelpBox("尚未生成 OasisCity LookDev 配置资产。", MessageType.Info);
                if (GUILayout.Button("创建三套风格配置"))
                {
                    OasisLookDevAssetUtility.CreateOrRefreshAssets();
                    lookDevCatalog = OasisLookDevCatalog.Load();
                }
                return;
            }

            IReadOnlyList<string> errors = lookDevCatalog.ValidateCatalog();
            if (errors.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Error);

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (OasisLookPreset preset in lookDevCatalog.Presets)
                {
                    bool active = lookDevSession != null && lookDevSession.ActiveLookId == preset.Id;
                    Color previous = GUI.backgroundColor;
                    if (active) GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
                    if (GUILayout.Button(active ? preset.DisplayName + "（当前）" : preset.DisplayName, GUILayout.Height(28f)))
                    {
                        if (!lookDevSession.Apply(preset, OasisBakeTier.Preview, out string error))
                            Debug.LogError("[OasisLookDev] " + error);
                    }
                    GUI.backgroundColor = previous;
                }
            }

            string activeStatus = lookDevSession != null && lookDevSession.IsActive
                ? $"当前：{lookDevSession.ActiveLookId} / {lookDevSession.ActiveTier}"
                : "当前：未启用临时 LookDev";
            EditorGUILayout.LabelField(activeStatus, EditorStyles.miniLabel);
            if (bakeController != null)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, 18f);
                EditorGUI.ProgressBar(progressRect, bakeController.Progress, bakeController.Status);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool running = bakeController != null && bakeController.IsRunning;
                using (new EditorGUI.DisabledScope(running || errors.Count > 0))
                {
                    if (GUILayout.Button("依次预览烘焙"))
                        bakeController.Start(lookDevCatalog, OasisBakeTier.Preview);
                    using (new EditorGUI.DisabledScope(!lookDevCatalog.PreviewApproved))
                    {
                        if (GUILayout.Button("依次最终烘焙"))
                            bakeController.Start(lookDevCatalog, OasisBakeTier.Final);
                    }
                }
                using (new EditorGUI.DisabledScope(!running))
                {
                    if (GUILayout.Button("取消", GUILayout.Width(58f)))
                        bakeController.Cancel();
                }
                if (GUILayout.Button("恢复原状态", GUILayout.Width(88f)))
                    lookDevSession.Restore();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(bakeController == null || bakeController.IsRunning))
                {
                    if (GUILayout.Button("从未完成项继续"))
                        bakeController.Resume(lookDevCatalog);
                    if (GUILayout.Button("生成 12 张预览对比"))
                    {
                        string output = OasisLookDevCapture.CaptureMatrix(lookDevCatalog, lookDevSession, OasisBakeTier.Preview);
                        EditorUtility.RevealInFinder(output);
                    }
                }
            }
            EditorGUILayout.HelpBox("最终烘焙需先完成三套预览、生成 12 张对比并获得人工批准。所有切换均为编辑器临时状态。", MessageType.None);
        }
    }

    private void DrawCameraList()
    {
        float listWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.44f, 320f, 480f);
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(listWidth)))
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(20f);
                GUILayout.Label("相机", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label("镜头", EditorStyles.miniLabel, GUILayout.Width(38f));
                GUILayout.Label("Game", EditorStyles.miniLabel, GUILayout.Width(46f));
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(cameraListScroll))
            {
                cameraListScroll = scroll.scrollPosition;
                string lastSceneName = null;
                for (int sectionIndex = 0; sectionIndex < visibleSections.Count; sectionIndex++)
                {
                    CameraListSection section = visibleSections[sectionIndex];
                    if (!string.Equals(lastSceneName, section.SceneName, StringComparison.Ordinal))
                    {
                        EditorGUILayout.LabelField(section.SceneName, EditorStyles.boldLabel);
                        lastSceneName = section.SceneName;
                    }

                    bool expanded = !groupFoldouts.TryGetValue(section.FoldoutKey, out bool value) || value;
                    expanded = EditorGUILayout.Foldout(expanded, $"{section.GroupName} ({section.Records.Count})", true);
                    groupFoldouts[section.FoldoutKey] = expanded;
                    if (!expanded)
                        continue;

                    EditorGUI.indentLevel++;
                    for (int recordIndex = 0; recordIndex < section.Records.Count; recordIndex++)
                        DrawCameraListRow(section.Records[recordIndex]);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    private void DrawCameraListRow(CameraRecord record)
    {
        Camera camera = record.Camera;
        if (camera == null)
            return;

        using (new EditorGUILayout.HorizontalScope(selectedCamera == camera ? "SelectionRect" : GUIStyle.none))
        {
            GUIContent state = EditorGUIUtility.IconContent(camera.enabled && camera.gameObject.activeInHierarchy ? "d_scenevis_visible_hover" : "d_scenevis_hidden_hover");
            GUILayout.Label(state, GUILayout.Width(20f));
            if (GUILayout.Button(camera.name, EditorStyles.label, GUILayout.MinWidth(80f), GUILayout.ExpandWidth(true)))
                SetSelectedCamera(camera);
            GUILayout.Label(camera.orthographic ? "正交" : $"{camera.fieldOfView:0.#}°", EditorStyles.miniLabel, GUILayout.Width(38f));
            bool isTemporaryActive = temporaryActiveCamera == camera && temporaryStates.Count > 0;
            Color previousBackground = GUI.backgroundColor;
            if (isTemporaryActive)
                GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
            if (GUILayout.Button(isTemporaryActive ? CurrentGameViewContent : QuickGameViewContent, EditorStyles.miniButton, GUILayout.Width(46f)))
                BeginTemporaryGameViewSwitch(camera);
            GUI.backgroundColor = previousBackground;
        }
    }

    private void DrawSelectedCameraDetails()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true)))
        {
            if (selectedCamera == null)
            {
                EditorGUILayout.HelpBox("当前没有可用摄像机。", MessageType.Info);
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(detailsScroll))
            {
                detailsScroll = scroll.scrollPosition;
                DrawSelectedHeader();
                DrawSelectedSummary();
                DrawGroupControls();
                DrawPreviewControls();

                detailsFoldout = EditorGUILayout.Foldout(detailsFoldout, "摄像机与 Transform 配置", true);
                if (detailsFoldout)
                {
                    UnityEditor.Editor.CreateCachedEditor(selectedCamera.transform, null, ref transformInspector);
                    transformInspector.OnInspectorGUI();
                    EditorGUILayout.Space(4f);
                    UnityEditor.Editor.CreateCachedEditor(selectedCamera, null, ref cameraInspector);
                    cameraInspector.OnInspectorGUI();
                }
            }
        }
    }

    private void DrawSelectedHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(selectedCamera.name, EditorStyles.boldLabel);
            if (GUILayout.Button("选中", GUILayout.Width(58f)))
            {
                Selection.activeGameObject = selectedCamera.gameObject;
                EditorGUIUtility.PingObject(selectedCamera.gameObject);
            }
        }
    }

    private void DrawSelectedSummary()
    {
        Transform transform = selectedCamera.transform;
        EditorGUILayout.LabelField("场景", selectedCamera.gameObject.scene.name);
        EditorGUILayout.LabelField("层级", GetHierarchyPath(transform));
        EditorGUILayout.LabelField("位置", FormatVector(transform.position));
        EditorGUILayout.LabelField("旋转", FormatVector(transform.eulerAngles));
        EditorGUILayout.LabelField("投影", selectedCamera.orthographic ? $"正交 / Size {selectedCamera.orthographicSize:0.##}" : $"透视 / FOV {selectedCamera.fieldOfView:0.##}°");
        EditorGUILayout.LabelField("裁剪", $"{selectedCamera.nearClipPlane:0.###} — {selectedCamera.farClipPlane:0.###}");
        EditorGUILayout.LabelField("Depth / Display", $"{selectedCamera.depth:0.##} / Display {selectedCamera.targetDisplay + 1}");
        EditorGUILayout.LabelField("状态", $"GameObject={(selectedCamera.gameObject.activeInHierarchy ? "Active" : "Inactive")}, Camera={(selectedCamera.enabled ? "Enabled" : "Disabled")}");
    }

    private void DrawGroupControls()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("分组", EditorStyles.boldLabel);
        string currentGroup = GetGroupName(selectedCamera);
        int currentIndex = Mathf.Max(0, selectedSceneGroups.IndexOf(currentGroup));
        int nextIndex = EditorGUILayout.Popup("所属组", currentIndex, selectedSceneGroupOptions);
        if (nextIndex != currentIndex && selectedSceneGroups[nextIndex] != UngroupedName)
        {
            MoveToGroup(selectedCamera, selectedSceneGroups[nextIndex]);
            RefreshCameraCache();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            newGroupName = EditorGUILayout.TextField("新组", newGroupName);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newGroupName)))
            {
                if (GUILayout.Button("创建并移入", GUILayout.Width(92f)))
                {
                    MoveToGroup(selectedCamera, newGroupName);
                    newGroupName = string.Empty;
                    RefreshCameraCache();
                }
            }
        }
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("快速预览", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scene View 预览", GUILayout.Height(30f)))
                PreviewInSceneView(selectedCamera);
            if (GUILayout.Button("Game View 临时切换", GUILayout.Height(30f)))
                BeginTemporaryGameViewSwitch(selectedCamera);
        }

        if (GUILayout.Button("相机匹配当前 Scene View"))
            MatchCameraToSceneView(selectedCamera);
    }

    private static void PreviewInSceneView(Camera camera)
    {
        SceneView sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        if (sceneView == null || camera == null)
            return;

        sceneView.AlignViewToObject(camera.transform);
        sceneView.orthographic = camera.orthographic;
        if (camera.orthographic)
            sceneView.size = camera.orthographicSize;
        else
            sceneView.cameraSettings.fieldOfView = camera.fieldOfView;
        sceneView.Repaint();
    }

    private static void MatchCameraToSceneView(Camera camera)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null || camera == null)
            return;

        Undo.RecordObjects(new UnityEngine.Object[] { camera.transform, camera }, "相机匹配 Scene View");
        camera.transform.SetPositionAndRotation(sceneView.camera.transform.position, sceneView.camera.transform.rotation);
        camera.orthographic = sceneView.orthographic;
        if (sceneView.orthographic)
            camera.orthographicSize = sceneView.size;
        else
            camera.fieldOfView = sceneView.cameraSettings.fieldOfView;
        EditorUtility.SetDirty(camera);
        EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
    }

    private bool MatchesSearch(CameraRecord record)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return record.Camera.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            || record.SceneName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            || record.GroupName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            || record.HierarchyPath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RebuildVisibleSections()
    {
        visibleSections.Clear();
        CameraListSection currentSection = null;
        for (int index = 0; index < cameraRecords.Count; index++)
        {
            CameraRecord record = cameraRecords[index];
            if (!MatchesSearch(record))
                continue;

            if (currentSection == null ||
                !string.Equals(currentSection.SceneName, record.SceneName, StringComparison.Ordinal) ||
                !string.Equals(currentSection.GroupName, record.GroupName, StringComparison.Ordinal))
            {
                currentSection = new CameraListSection
                {
                    SceneName = record.SceneName,
                    GroupName = record.GroupName,
                    FoldoutKey = record.SceneName + "/" + record.GroupName,
                };
                visibleSections.Add(currentSection);
            }

            currentSection.Records.Add(record);
        }
    }

    private static int CompareRecords(CameraRecord left, CameraRecord right)
    {
        int scene = string.Compare(left.SceneName, right.SceneName, StringComparison.OrdinalIgnoreCase);
        if (scene != 0) return scene;
        int group = string.Compare(left.GroupName, right.GroupName, StringComparison.OrdinalIgnoreCase);
        return group != 0 ? group : string.Compare(left.HierarchyPath, right.HierarchyPath, StringComparison.OrdinalIgnoreCase);
    }

    private void SetSelectedCamera(Camera camera)
    {
        if (selectedCamera == camera)
            return;
        selectedCamera = camera;
        DestroyInspectors();
        RefreshSelectedGroupOptions();
    }

    private void RefreshSelectedGroupOptions()
    {
        selectedSceneGroups = selectedCamera != null
            ? GetGroups(selectedCamera.gameObject.scene)
            : new List<string> { UngroupedName };
        selectedSceneGroupOptions = selectedSceneGroups.ToArray();
    }

    private void DestroyInspectors()
    {
        if (cameraInspector != null)
            UnityEngine.Object.DestroyImmediate(cameraInspector);
        if (transformInspector != null)
            UnityEngine.Object.DestroyImmediate(transformInspector);
        cameraInspector = null;
        transformInspector = null;
    }

    private void OnHierarchyChanged() => hierarchyDirty = true;
    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => hierarchyDirty = true;
    private void OnSceneClosed(Scene scene) => hierarchyDirty = true;
    private void OnSceneSaving(Scene scene, string path) => RestoreTemporaryEditorState();

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            RestoreTemporaryEditorState();
        hierarchyDirty = true;
    }

    private void UnsubscribeEvents()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        Undo.undoRedoPerformed -= OnHierarchyChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= RestoreTemporaryEditorState;
    }

    private void RestoreTemporaryEditorState()
    {
        RestoreTemporaryGameViewSwitch();
        bakeController?.Shutdown();
        lookDevSession?.Restore();
    }

    private static List<string> GetGroups(Scene scene)
    {
        List<string> result = new() { UngroupedName };
        Transform root = FindCameraGroupsRoot(scene);
        if (root == null)
            return result;

        for (int i = 0; i < root.childCount; i++)
            result.Add(root.GetChild(i).name);
        if (result.Count > 1)
            result.Sort(1, result.Count - 1, StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static Transform FindCameraGroupsRoot(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == CameraGroupsRootName)
                    return transforms[i];
            }
        }
        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static string FormatVector(Vector3 value) => $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
}
