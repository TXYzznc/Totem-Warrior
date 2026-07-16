using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Artist-facing editor for hand-authoring regular TattooMap regions. The artist chooses the
/// character-semantic part; the window never infers left/right from screen position.
/// </summary>
public sealed class TattooMapRegionMarkerWindow : EditorWindow
{
    private const int FramePixels = 512;
    private const string AuthoringAssetPath = "Assets/Game/Config/TattooVisual/ActorCommonM02TattooRegionAuthoring.asset";
    private const string SpriteDirectory = "Assets/Game/Sprite/Actors/ActorCommonM02";

    private static readonly string[] Actions = { "idle", "walk", "sprint", "hit", "attack", "roll", "death" };
    private static readonly string[] Directions = { "down", "up", "left", "right" };
    private static readonly int[] FrameCounts = { 4, 6, 6, 4, 6, 8, 8 };
    private static readonly string[] MarkingToolLabels = { "矩形区域", "钢笔" };
    private static readonly string[] PartLabels =
    {
        "1  头部（太阳穴 / 面颊）",
        "2  躯干（裸露胸腹）",
        "3  人物左臂（非屏幕左）",
        "4  人物右臂（非屏幕右）",
        "5  人物左腿（非屏幕左）",
        "6  人物右腿（非屏幕右）",
    };
    private static readonly Color[] PartColors =
    {
        new Color(0.96f, 0.34f, 0.35f, 0.8f),
        new Color(0.95f, 0.69f, 0.26f, 0.8f),
        new Color(0.35f, 0.77f, 0.47f, 0.8f),
        new Color(0.29f, 0.61f, 0.90f, 0.8f),
        new Color(0.68f, 0.37f, 0.86f, 0.8f),
        new Color(0.29f, 0.83f, 0.82f, 0.8f),
    };

    private TattooMapRegionAuthoringAsset authoring;
    private int actionIndex;
    private int directionIndex = 3;
    private int frameIndex;
    private int selectedPartId = 1;
    private float limbWidth = 36f;
    private float rectangleRotation;
    private MarkingTool markingTool;
    private float zoom = 1f;
    private Vector2 pan;
    private bool drawing;
    private bool isDrawingPen;
    private Vector2 dragStart;
    private Vector2 dragCurrent;
    private readonly List<Vector2> penPoints = new List<Vector2>();
    private Vector2 penHoverPoint;
    private RegionDragMode regionDragMode;
    private int regionDragHandle;
    private Vector2 lastRegionDragPoint;
    private Texture2D previewTexture;
    private string previewPath;
    private int previewVersion = -1;

    private enum RegionDragMode
    {
        None,
        Move,
        LineStart,
        LineEnd,
        LineWidth,
        RectangleCorner,
        RectangleEdge,
        PolygonVertex,
    }

    private enum MarkingTool
    {
        Rectangle,
        Pen,
    }

    [MenuItem("Game/Totem/Tattoo/Region Marker")]
    public static void Open()
    {
        var window = GetWindow<TattooMapRegionMarkerWindow>("Tattoo Region Marker");
        window.minSize = new Vector2(980f, 640f);
        window.Show();
    }

    void OnEnable()
    {
        authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
    }

    void OnDisable()
    {
        DestroyPreviewTexture();
    }

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawControlPanel();
        DrawCanvasPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("语义左右以角色自身为准，绝不按屏幕左右自动判断。", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("生成所有已标记 TattooMap", EditorStyles.toolbarButton))
        {
            ActorCommonM02TattooMapTool.GenerateAllAuthoredTattooMaps();
        }

        if (GUILayout.Button("生成当前方向 TattooMap", EditorStyles.toolbarButton))
        {
            ActorCommonM02TattooMapTool.GenerateCurrentDirectionTattooMaps(Directions[directionIndex]);
        }

        if (GUILayout.Button("验证当前方向", EditorStyles.toolbarButton))
        {
            ActorCommonM02TattooMapTool.ValidateCurrentDirectionTattooMaps(Directions[directionIndex]);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawControlPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(286f));
        GUILayout.Label("帧选择", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        actionIndex = EditorGUILayout.Popup("动作", actionIndex, Actions);
        directionIndex = EditorGUILayout.Popup("方向", directionIndex, Directions);
        frameIndex = EditorGUILayout.IntSlider("帧", frameIndex + 1, 1, FrameCounts[actionIndex]) - 1;
        if (EditorGUI.EndChangeCheck())
        {
            frameIndex = Mathf.Clamp(frameIndex, 0, FrameCounts[actionIndex] - 1);
            CancelActiveDrawing();
            RefreshPreview();
        }

        GUILayout.Space(8f);
        DrawFrameToleranceControls();
        GUILayout.Space(8f);
        GUILayout.Label("选择要标记的语义部位", EditorStyles.boldLabel);
        for (int index = 0; index < PartLabels.Length; index++)
        {
            int partId = index + 1;
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = selectedPartId == partId ? PartColors[index] : new Color(0.78f, 0.78f, 0.78f, 1f);
            if (GUILayout.Button(PartLabels[index], GUILayout.Height(25f)))
            {
                selectedPartId = partId;
                TattooMapRegionAuthoring region = CurrentFrame?.FindRegion(partId);
                if (region != null)
                {
                    limbWidth = region.width;
                    rectangleRotation = region.rotationDegrees;
                }
            }
            GUI.backgroundColor = previous;
        }

        GUILayout.Space(8f);
        GUILayout.Label("标记工具", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        MarkingTool newTool = (MarkingTool)GUILayout.Toolbar((int)markingTool, MarkingToolLabels);
        if (EditorGUI.EndChangeCheck())
        {
            markingTool = newTool;
            CancelActiveDrawing();
        }

        GUILayout.Space(8f);
        bool isLimb = selectedPartId >= 3;
        if (markingTool == MarkingTool.Rectangle)
        {
            GUILayout.Label(isLimb ? "肢体：中心线 + 宽度；拖边框手柄可改长度和宽度" : "头部 / 躯干：拖出规则矩形；拖边框手柄可改大小", EditorStyles.boldLabel);
            if (isLimb)
            {
                limbWidth = EditorGUILayout.Slider("区域宽度", limbWidth, 8f, 180f);
                GUILayout.Label("中心线决定方向，宽度由滑条保证规整。", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                rectangleRotation = EditorGUILayout.Slider("矩形旋转", rectangleRotation, -180f, 180f);
                GUILayout.Label("先拖出范围；需要贴合倾斜姿势时再调旋转。", EditorStyles.wordWrappedMiniLabel);
            }
        }
        else
        {
            GUILayout.Label("钢笔：单击依次放置顶点；单击起点或双击最后一点闭合。", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!isDrawingPen || penPoints.Count < 3))
                {
                    if (GUILayout.Button("完成钢笔区域"))
                    {
                        CommitPenDraft();
                    }
                }

                using (new EditorGUI.DisabledScope(!isDrawingPen || penPoints.Count == 0))
                {
                    if (GUILayout.Button("撤销顶点"))
                    {
                        penPoints.RemoveAt(penPoints.Count - 1);
                        Repaint();
                    }
                }
            }
        }

        DrawSelectedRegionControls();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("复制上一帧的已手工标记"))
        {
            CopyPreviousFrame();
        }

        if (GUILayout.Button("删除当前部位的手工标记"))
        {
            DeleteSelectedRegion();
        }

        EditorGUILayout.HelpBox("矩形工具可拖动现有手柄；钢笔工具可绘制任意多边形，切回矩形工具后可拖动其顶点或整体移动。\n\n预览使用和导出一致的皮肤识别裁剪。色块越界到衣物或透明背景时不会写入 TattooMap。\n\n“生成所有已标记 TattooMap”会遍历所有有手工标记的帧；右向保留已审核的保守默认区域，其它方向只导出手工标记。", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void DrawFrameToleranceControls()
    {
        TattooMapFrameAuthoring frame = CurrentFrame;
        GUILayout.Label("当前帧皮肤识别", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float tolerance = EditorGUILayout.Slider(
            new GUIContent("肤色容差", "仅影响当前动作、方向、帧。正值放宽肤色筛选，负值收紧；透明背景始终排除。"),
            frame.skinTolerance,
            -0.12f,
            0.12f
        );
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(EnsureAuthoring(), "Adjust Tattoo Skin Tolerance");
            frame.skinTolerance = tolerance;
            SaveAuthoring();
            RefreshPreview(true);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("默认值：0（当前帧独立保存）", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("重置", EditorStyles.miniButton, GUILayout.Width(48f)) && !Mathf.Approximately(frame.skinTolerance, 0f))
            {
                Undo.RecordObject(EnsureAuthoring(), "Reset Tattoo Skin Tolerance");
                frame.skinTolerance = 0f;
                SaveAuthoring();
                RefreshPreview(true);
            }
        }
    }

    private void DrawSelectedRegionControls()
    {
        TattooMapRegionAuthoring region = CurrentFrame?.FindRegion(selectedPartId);
        if (region == null)
        {
            return;
        }

        GUILayout.Space(6f);
        GUILayout.Label("当前部位微调", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        if (region.shape == TattooMapRegionShape.CenterLine)
        {
            Vector2 start = EditorGUILayout.Vector2Field("中心线起点", region.start);
            Vector2 end = EditorGUILayout.Vector2Field("中心线终点", region.end);
            float width = EditorGUILayout.Slider("区域宽度", region.width, 8f, 180f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(EnsureAuthoring(), "Adjust Tattoo Region");
                region.start = start;
                region.end = end;
                region.width = width;
                limbWidth = width;
                SaveAuthoring();
                RefreshPreview(true);
            }
        }
        else if (region.shape == TattooMapRegionShape.OrientedRectangle)
        {
            Vector2 center = EditorGUILayout.Vector2Field("中心", region.center);
            Vector2 size = EditorGUILayout.Vector2Field("尺寸", region.size);
            float rotation = EditorGUILayout.Slider("矩形旋转", region.rotationDegrees, -180f, 180f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(EnsureAuthoring(), "Adjust Tattoo Region");
                region.center = center;
                region.size = new Vector2(Mathf.Max(4f, size.x), Mathf.Max(4f, size.y));
                region.rotationDegrees = rotation;
                rectangleRotation = rotation;
                SaveAuthoring();
                RefreshPreview(true);
            }
        }
        else
        {
            int pointCount = region.points == null ? 0 : region.points.Count;
            EditorGUILayout.LabelField("钢笔顶点", pointCount + " 个");
            EditorGUILayout.LabelField("提示", "切回矩形区域工具后，可拖动顶点或区域内部来编辑。", EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawCanvasPanel()
    {
        EditorGUILayout.BeginVertical();
        string sourcePath = CurrentSourcePath;
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            EditorGUILayout.HelpBox("未找到源帧：" + sourcePath, MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        if (regionDragMode == RegionDragMode.None && (previewTexture == null || previewPath != sourcePath || previewVersion != GetPreviewVersion()))
        {
            RefreshPreview();
        }

        Rect canvas = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(520f));
        EditorGUI.DrawRect(canvas, new Color(0.10f, 0.10f, 0.10f, 1f));
        Rect imageRect = GetImageRect(canvas);
        if (previewTexture != null)
        {
            GUI.DrawTexture(imageRect, previewTexture, ScaleMode.StretchToFill, true);
        }

        DrawRegionOutlines(imageRect);
        DrawCanvasInput(canvas, imageRect);
        EditorGUILayout.EndVertical();
    }

    private void DrawRegionOutlines(Rect imageRect)
    {
        Handles.BeginGUI();
        TattooMapFrameAuthoring frame = CurrentFrame;
        if (frame != null)
        {
            for (int index = 0; index < frame.regions.Count; index++)
            {
                TattooMapRegionAuthoring region = frame.regions[index];
                if (region == null)
                {
                    continue;
                }

                DrawOutline(imageRect, TattooMapRegionAuthoringGeometry.GetCorners(region), PartColors[region.partId - 1], region.partId == selectedPartId ? 3f : 1.5f, true);
                if (region.partId == selectedPartId)
                {
                    DrawRegionHandles(imageRect, region);
                }
            }
        }
        if (drawing)
        {
            TattooMapRegionAuthoring draft = BuildDraftRegion();
            DrawOutline(imageRect, TattooMapRegionAuthoringGeometry.GetCorners(draft), Color.white, 2f, true);
        }

        if (isDrawingPen && penPoints.Count > 0)
        {
            var points = new List<Vector2>(penPoints) { penHoverPoint };
            DrawOutline(imageRect, points.ToArray(), Color.white, 2f, false);
            for (int index = 0; index < penPoints.Count; index++)
            {
                DrawHandle(imageRect, penPoints[index], Color.white, true);
            }
        }
        Handles.EndGUI();
    }

    private void DrawOutline(Rect imageRect, Vector2[] points, Color color, float width, bool closed)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        int count = points.Length + (closed ? 1 : 0);
        var guiPoints = new Vector3[count];
        for (int index = 0; index < points.Length; index++)
        {
            Vector2 point = ImageToGui(points[index], imageRect);
            guiPoints[index] = new Vector3(point.x, point.y, 0f);
        }
        if (closed)
        {
            guiPoints[guiPoints.Length - 1] = guiPoints[0];
        }
        Handles.color = color;
        Handles.DrawAAPolyLine(width, guiPoints);
    }

    private void DrawRegionHandles(Rect imageRect, TattooMapRegionAuthoring region)
    {
        Vector2[] corners = TattooMapRegionAuthoringGeometry.GetCorners(region);
        Color color = PartColors[region.partId - 1];
        if (region.shape == TattooMapRegionShape.CenterLine)
        {
            DrawHandle(imageRect, (corners[0] + corners[1]) * 0.5f, color, true);
            DrawHandle(imageRect, (corners[2] + corners[3]) * 0.5f, color, true);
            DrawHandle(imageRect, (corners[1] + corners[2]) * 0.5f, color, false);
            DrawHandle(imageRect, (corners[0] + corners[3]) * 0.5f, color, false);
        }
        else if (region.shape == TattooMapRegionShape.OrientedRectangle)
        {
            for (int index = 0; index < 4; index++)
            {
                DrawHandle(imageRect, corners[index], color, true);
                DrawHandle(imageRect, (corners[index] + corners[(index + 1) % 4]) * 0.5f, color, false);
            }
        }
        else
        {
            for (int index = 0; index < corners.Length; index++)
            {
                DrawHandle(imageRect, corners[index], color, true);
            }
        }

        DrawHandle(imageRect, GetRegionCenter(region), Color.white, false);
    }

    private static void DrawHandle(Rect imageRect, Vector2 point, Color color, bool square)
    {
        Vector2 gui = ImageToGui(point, imageRect);
        const float size = 9f;
        Rect rect = new Rect(gui.x - size * 0.5f, gui.y - size * 0.5f, size, size);
        if (square)
        {
            EditorGUI.DrawRect(rect, color);
            EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Color.black);
        }
        else
        {
            Handles.color = color;
            Handles.DrawSolidDisc(gui, Vector3.forward, size * 0.5f);
        }
    }

    private void DrawCanvasInput(Rect canvas, Rect imageRect)
    {
        Event current = Event.current;
        if (!canvas.Contains(current.mousePosition))
        {
            return;
        }

        if (current.type == EventType.ScrollWheel)
        {
            zoom = Mathf.Clamp(zoom * (current.delta.y > 0f ? 0.9f : 1.1f), 0.35f, 4f);
            current.Use();
            Repaint();
            return;
        }

        if (current.button == 2 && current.type == EventType.MouseDrag)
        {
            pan += current.delta;
            current.Use();
            Repaint();
            return;
        }

        if (current.button != 0)
        {
            return;
        }

        if (markingTool == MarkingTool.Pen)
        {
            if (current.type == EventType.MouseMove && imageRect.Contains(current.mousePosition))
            {
                penHoverPoint = ClampImagePoint(GuiToImage(current.mousePosition, imageRect));
                Repaint();
            }

            if (current.type == EventType.MouseDown && imageRect.Contains(current.mousePosition))
            {
                Vector2 point = ClampImagePoint(GuiToImage(current.mousePosition, imageRect));
                float closeRadius = Mathf.Max(6f, 11f * FramePixels / imageRect.width);
                if (isDrawingPen && penPoints.Count >= 3 &&
                    (current.clickCount >= 2 || Near(point, penPoints[0], closeRadius)))
                {
                    CommitPenDraft();
                }
                else
                {
                    isDrawingPen = true;
                    penPoints.Add(point);
                    penHoverPoint = point;
                }

                current.Use();
                Repaint();
            }

            return;
        }

        if (regionDragMode != RegionDragMode.None && current.type == EventType.MouseDrag)
        {
            ApplyRegionDrag(ClampImagePoint(GuiToImage(current.mousePosition, imageRect)));
            current.Use();
            Repaint();
            return;
        }

        if (regionDragMode != RegionDragMode.None && current.type == EventType.MouseUp)
        {
            regionDragMode = RegionDragMode.None;
            regionDragHandle = -1;
            SaveAuthoring();
            RefreshPreview(true);
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseDown && imageRect.Contains(current.mousePosition))
        {
            Vector2 mouseImage = GuiToImage(current.mousePosition, imageRect);
            if (TryBeginRegionDrag(mouseImage, imageRect))
            {
                current.Use();
                Repaint();
                return;
            }

            dragStart = mouseImage;
            dragCurrent = dragStart;
            drawing = true;
            current.Use();
            Repaint();
            return;
        }

        if (drawing && current.type == EventType.MouseDrag)
        {
            dragCurrent = ClampImagePoint(GuiToImage(current.mousePosition, imageRect));
            current.Use();
            Repaint();
            return;
        }

        if (drawing && current.type == EventType.MouseUp)
        {
            dragCurrent = ClampImagePoint(GuiToImage(current.mousePosition, imageRect));
            CommitDraft();
            drawing = false;
            current.Use();
            Repaint();
        }
    }

    private void CommitDraft()
    {
        if ((dragCurrent - dragStart).sqrMagnitude < 16f)
        {
            return;
        }

        TattooMapRegionAuthoringAsset target = EnsureAuthoring();
        Undo.RecordObject(target, "Draw Tattoo Region");
        CurrentFrame.ReplaceRegion(BuildDraftRegion());
        SaveAuthoring(target);
        RefreshPreview(true);
    }

    private void CommitPenDraft()
    {
        if (penPoints.Count < 3)
        {
            ShowNotification(new GUIContent("钢笔区域至少需要三个顶点"));
            return;
        }

        TattooMapRegionAuthoringAsset target = EnsureAuthoring();
        Undo.RecordObject(target, "Draw Tattoo Pen Region");
        CurrentFrame.ReplaceRegion(new TattooMapRegionAuthoring
        {
            partId = selectedPartId,
            shape = TattooMapRegionShape.Polygon,
            points = new List<Vector2>(penPoints),
        });
        SaveAuthoring(target);
        CancelActiveDrawing();
        RefreshPreview(true);
    }

    private void CancelActiveDrawing()
    {
        drawing = false;
        isDrawingPen = false;
        penPoints.Clear();
    }

    private bool TryBeginRegionDrag(Vector2 mouseImage, Rect imageRect)
    {
        TattooMapRegionAuthoring region = CurrentFrame.FindRegion(selectedPartId);
        if (region == null)
        {
            return false;
        }

        Vector2[] corners = TattooMapRegionAuthoringGeometry.GetCorners(region);
        float radius = Mathf.Max(6f, 11f * FramePixels / imageRect.width);
        RegionDragMode mode = RegionDragMode.None;
        int handle = -1;
        if (region.shape == TattooMapRegionShape.CenterLine)
        {
            Vector2 startHandle = (corners[0] + corners[1]) * 0.5f;
            Vector2 endHandle = (corners[2] + corners[3]) * 0.5f;
            Vector2 positiveWidthHandle = (corners[1] + corners[2]) * 0.5f;
            Vector2 negativeWidthHandle = (corners[0] + corners[3]) * 0.5f;
            if (Near(mouseImage, startHandle, radius)) mode = RegionDragMode.LineStart;
            else if (Near(mouseImage, endHandle, radius)) mode = RegionDragMode.LineEnd;
            else if (Near(mouseImage, positiveWidthHandle, radius) || Near(mouseImage, negativeWidthHandle, radius)) mode = RegionDragMode.LineWidth;
        }
        else if (region.shape == TattooMapRegionShape.OrientedRectangle)
        {
            for (int index = 0; index < 4 && mode == RegionDragMode.None; index++)
            {
                if (Near(mouseImage, corners[index], radius))
                {
                    mode = RegionDragMode.RectangleCorner;
                    handle = index;
                }
            }

            for (int index = 0; index < 4 && mode == RegionDragMode.None; index++)
            {
                if (Near(mouseImage, (corners[index] + corners[(index + 1) % 4]) * 0.5f, radius))
                {
                    mode = RegionDragMode.RectangleEdge;
                    handle = index;
                }
            }
        }
        else
        {
            for (int index = 0; index < corners.Length && mode == RegionDragMode.None; index++)
            {
                if (Near(mouseImage, corners[index], radius))
                {
                    mode = RegionDragMode.PolygonVertex;
                    handle = index;
                }
            }
        }

        if (mode == RegionDragMode.None && TattooMapRegionAuthoringGeometry.Contains(region, mouseImage.x, mouseImage.y, out _, out _))
        {
            mode = RegionDragMode.Move;
        }

        if (mode == RegionDragMode.None)
        {
            return false;
        }

        Undo.RecordObject(EnsureAuthoring(), "Manipulate Tattoo Region");
        regionDragMode = mode;
        regionDragHandle = handle;
        lastRegionDragPoint = mouseImage;
        return true;
    }

    private void ApplyRegionDrag(Vector2 mouseImage)
    {
        TattooMapRegionAuthoring region = CurrentFrame.FindRegion(selectedPartId);
        if (region == null)
        {
            regionDragMode = RegionDragMode.None;
            return;
        }

        if (regionDragMode == RegionDragMode.Move)
        {
            Vector2 delta = mouseImage - lastRegionDragPoint;
            if (region.shape == TattooMapRegionShape.CenterLine)
            {
                region.start += delta;
                region.end += delta;
            }
            else if (region.shape == TattooMapRegionShape.OrientedRectangle)
            {
                region.center += delta;
            }
            else if (region.points != null)
            {
                for (int index = 0; index < region.points.Count; index++)
                {
                    region.points[index] += delta;
                }
            }
        }
        else if (region.shape == TattooMapRegionShape.CenterLine)
        {
            if (regionDragMode == RegionDragMode.LineStart)
            {
                region.start = mouseImage;
            }
            else if (regionDragMode == RegionDragMode.LineEnd)
            {
                region.end = mouseImage;
            }
            else if (regionDragMode == RegionDragMode.LineWidth)
            {
                Vector2 direction = region.end - region.start;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector2.down;
                }
                direction.Normalize();
                Vector2 normal = new Vector2(-direction.y, direction.x);
                region.width = Mathf.Max(8f, Mathf.Abs(Vector2.Dot(mouseImage - (region.start + region.end) * 0.5f, normal)) * 2f);
                limbWidth = region.width;
            }
        }
        else if (regionDragMode == RegionDragMode.RectangleCorner)
        {
            Vector2[] corners = TattooMapRegionAuthoringGeometry.GetCorners(region);
            Vector2 fixedCorner = corners[(regionDragHandle + 2) % 4];
            ResizeRectangleFromOppositeCorner(region, fixedCorner, mouseImage);
        }
        else if (regionDragMode == RegionDragMode.RectangleEdge)
        {
            ResizeRectangleFromOppositeEdge(region, regionDragHandle, mouseImage);
        }
        else if (regionDragMode == RegionDragMode.PolygonVertex && region.points != null && regionDragHandle >= 0 && regionDragHandle < region.points.Count)
        {
            region.points[regionDragHandle] = mouseImage;
        }

        lastRegionDragPoint = mouseImage;
    }

    private static void ResizeRectangleFromOppositeCorner(TattooMapRegionAuthoring region, Vector2 fixedCorner, Vector2 movingCorner)
    {
        GetRectangleAxes(region, out Vector2 right, out Vector2 down);
        Vector2 delta = movingCorner - fixedCorner;
        region.center = (fixedCorner + movingCorner) * 0.5f;
        region.size = new Vector2(Mathf.Max(4f, Mathf.Abs(Vector2.Dot(delta, right))), Mathf.Max(4f, Mathf.Abs(Vector2.Dot(delta, down))));
    }

    private static void ResizeRectangleFromOppositeEdge(TattooMapRegionAuthoring region, int edgeIndex, Vector2 movingPoint)
    {
        Vector2[] corners = TattooMapRegionAuthoringGeometry.GetCorners(region);
        Vector2 fixedEdge = (corners[(edgeIndex + 2) % 4] + corners[(edgeIndex + 3) % 4]) * 0.5f;
        GetRectangleAxes(region, out Vector2 right, out Vector2 down);
        bool horizontalEdge = edgeIndex == 0 || edgeIndex == 2;
        Vector2 axis = horizontalEdge ? down : right;
        float distance = Vector2.Dot(movingPoint - fixedEdge, axis);
        region.center = fixedEdge + axis * (distance * 0.5f);
        if (horizontalEdge)
        {
            region.size.y = Mathf.Max(4f, Mathf.Abs(distance));
        }
        else
        {
            region.size.x = Mathf.Max(4f, Mathf.Abs(distance));
        }
    }

    private static void GetRectangleAxes(TattooMapRegionAuthoring region, out Vector2 right, out Vector2 down)
    {
        float radians = region.rotationDegrees * Mathf.Deg2Rad;
        right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        down = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
    }

    private static Vector2 GetRegionCenter(TattooMapRegionAuthoring region)
    {
        if (region.shape == TattooMapRegionShape.CenterLine)
        {
            return (region.start + region.end) * 0.5f;
        }

        if (region.shape == TattooMapRegionShape.OrientedRectangle)
        {
            return region.center;
        }

        if (region.points == null || region.points.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 sum = Vector2.zero;
        for (int index = 0; index < region.points.Count; index++)
        {
            sum += region.points[index];
        }
        return sum / region.points.Count;
    }

    private static bool Near(Vector2 first, Vector2 second, float radius)
    {
        return (first - second).sqrMagnitude <= radius * radius;
    }

    private TattooMapRegionAuthoring BuildDraftRegion()
    {
        if (selectedPartId >= 3)
        {
            return new TattooMapRegionAuthoring
            {
                partId = selectedPartId,
                shape = TattooMapRegionShape.CenterLine,
                start = dragStart,
                end = dragCurrent,
                width = limbWidth,
            };
        }

        return new TattooMapRegionAuthoring
        {
            partId = selectedPartId,
            shape = TattooMapRegionShape.OrientedRectangle,
            center = (dragStart + dragCurrent) * 0.5f,
            size = new Vector2(Mathf.Abs(dragCurrent.x - dragStart.x), Mathf.Abs(dragCurrent.y - dragStart.y)),
            rotationDegrees = rectangleRotation,
        };
    }

    private void CopyPreviousFrame()
    {
        if (frameIndex <= 0)
        {
            return;
        }

        TattooMapFrameAuthoring previous = EnsureAuthoring().FindFrame(Actions[actionIndex], Directions[directionIndex], frameIndex);
        if (previous == null || previous.regions.Count == 0)
        {
            ShowNotification(new GUIContent("上一帧没有手工标记"));
            return;
        }

        TattooMapRegionAuthoringAsset target = EnsureAuthoring();
        Undo.RecordObject(target, "Copy Previous Tattoo Frame");
        TattooMapFrameAuthoring current = CurrentFrame;
        current.regions.Clear();
        for (int index = 0; index < previous.regions.Count; index++)
        {
            current.regions.Add(Clone(previous.regions[index]));
        }
        SaveAuthoring(target);
        RefreshPreview(true);
    }

    private void DeleteSelectedRegion()
    {
        TattooMapFrameAuthoring frame = CurrentFrame;
        if (frame == null)
        {
            return;
        }

        for (int index = frame.regions.Count - 1; index >= 0; index--)
        {
            if (frame.regions[index] != null && frame.regions[index].partId == selectedPartId)
            {
                Undo.RecordObject(EnsureAuthoring(), "Delete Tattoo Region");
                frame.regions.RemoveAt(index);
                SaveAuthoring();
                RefreshPreview(true);
                return;
            }
        }
    }

    private TattooMapRegionAuthoringAsset EnsureAuthoring()
    {
        if (authoring != null)
        {
            return authoring;
        }

        authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        if (authoring == null)
        {
            const string folder = "Assets/Game/Config/TattooVisual";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Game/Config", "TattooVisual");
            }
            authoring = CreateInstance<TattooMapRegionAuthoringAsset>();
            AssetDatabase.CreateAsset(authoring, AuthoringAssetPath);
            AssetDatabase.SaveAssets();
        }

        return authoring;
    }

    private void SaveAuthoring(TattooMapRegionAuthoringAsset target = null)
    {
        TattooMapRegionAuthoringAsset assetToSave = target ?? authoring;
        if (assetToSave == null)
        {
            return;
        }

        EditorUtility.SetDirty(assetToSave);
        AssetDatabase.SaveAssets();
    }

    private TattooMapFrameAuthoring CurrentFrame => EnsureAuthoring().GetOrCreateFrame(Actions[actionIndex], Directions[directionIndex], frameIndex + 1);

    private string CurrentSourcePath => SpriteDirectory + "/actor_common_m02_" + Actions[actionIndex] + "_" + Directions[directionIndex] + "_" + (frameIndex + 1).ToString("00") + ".png";

    private void RefreshPreview(bool force = false)
    {
        string sourcePath = CurrentSourcePath;
        int version = GetPreviewVersion();
        if (!force && previewTexture != null && previewPath == sourcePath && previewVersion == version)
        {
            return;
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            return;
        }

        Color32[] pixels = ReadPixelsWithoutImporterMutation(source);
        TattooMapFrameAuthoring frame = CurrentFrame;
        for (int imageY = 0; imageY < FramePixels; imageY++)
        {
            int textureY = FramePixels - 1 - imageY;
            for (int x = 0; x < FramePixels; x++)
            {
                int pixelIndex = textureY * FramePixels + x;
                Color32 sourcePixel = pixels[pixelIndex];
                if (!IsSkinPixel(sourcePixel, frame.skinTolerance))
                {
                    continue;
                }

                for (int regionIndex = 0; regionIndex < frame.regions.Count; regionIndex++)
                {
                    TattooMapRegionAuthoring region = frame.regions[regionIndex];
                    if (region != null && TattooMapRegionAuthoringGeometry.Contains(region, x, imageY, out _, out _))
                    {
                        pixels[pixelIndex] = Blend(sourcePixel, PartColors[region.partId - 1], 0.80f);
                        break;
                    }
                }
            }
        }

        DestroyPreviewTexture();
        previewTexture = new Texture2D(FramePixels, FramePixels, TextureFormat.RGBA32, false, true);
        previewTexture.SetPixels32(pixels);
        previewTexture.Apply(false, false);
        previewPath = sourcePath;
        previewVersion = version;
    }

    private static Color32[] ReadPixelsWithoutImporterMutation(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(FramePixels, FramePixels, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;
        var copy = new Texture2D(FramePixels, FramePixels, TextureFormat.RGBA32, false, true);
        copy.ReadPixels(new Rect(0f, 0f, FramePixels, FramePixels), 0, 0, false);
        copy.Apply(false, false);
        Color32[] result = copy.GetPixels32();
        DestroyImmediate(copy);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return result;
    }

    private int GetPreviewVersion()
    {
        TattooMapFrameAuthoring frame = CurrentFrame;
        unchecked
        {
            int hash = (actionIndex * 397) ^ (directionIndex * 31) ^ frameIndex;
            hash = hash * 31 + frame.skinTolerance.GetHashCode();
            for (int index = 0; index < frame.regions.Count; index++)
            {
                TattooMapRegionAuthoring region = frame.regions[index];
                if (region == null)
                {
                    continue;
                }
                hash = hash * 31 + region.partId;
                hash = hash * 31 + region.shape.GetHashCode();
                hash = hash * 31 + region.start.GetHashCode();
                hash = hash * 31 + region.end.GetHashCode();
                hash = hash * 31 + region.width.GetHashCode();
                hash = hash * 31 + region.center.GetHashCode();
                hash = hash * 31 + region.size.GetHashCode();
                hash = hash * 31 + region.rotationDegrees.GetHashCode();
                if (region.points != null)
                {
                    for (int pointIndex = 0; pointIndex < region.points.Count; pointIndex++)
                    {
                        hash = hash * 31 + region.points[pointIndex].GetHashCode();
                    }
                }
            }
            return hash;
        }
    }

    private Rect GetImageRect(Rect canvas)
    {
        float scale = Mathf.Min(canvas.width / FramePixels, canvas.height / FramePixels) * zoom;
        Vector2 size = new Vector2(FramePixels * scale, FramePixels * scale);
        return new Rect(canvas.center - size * 0.5f + pan, size);
    }

    private static Vector2 GuiToImage(Vector2 point, Rect imageRect)
    {
        return ClampImagePoint((point - imageRect.position) * (FramePixels / imageRect.width));
    }

    private static Vector2 ImageToGui(Vector2 point, Rect imageRect)
    {
        return imageRect.position + point * (imageRect.width / FramePixels);
    }

    private static Vector2 ClampImagePoint(Vector2 value)
    {
        return new Vector2(Mathf.Clamp(value.x, 0f, FramePixels - 1f), Mathf.Clamp(value.y, 0f, FramePixels - 1f));
    }

    private static bool IsSkinPixel(Color32 color, float tolerance)
    {
        if (color.a < 16)
        {
            return false;
        }

        tolerance = Mathf.Clamp(tolerance, -0.12f, 0.12f);
        float red = color.r / 255f;
        float green = color.g / 255f;
        float blue = color.b / 255f;
        return red > 0.30f - tolerance &&
               green > 0.16f - tolerance &&
               red > green * (1.10f - tolerance * 0.5f) &&
               green > blue * (1.08f - tolerance * 0.4f) &&
               red - blue > 0.20f - tolerance;
    }

    private static Color32 Blend(Color32 source, Color tint, float amount)
    {
        return new Color32(
            (byte)Mathf.Lerp(source.r, tint.r * 255f, amount),
            (byte)Mathf.Lerp(source.g, tint.g * 255f, amount),
            (byte)Mathf.Lerp(source.b, tint.b * 255f, amount),
            source.a);
    }

    private static TattooMapRegionAuthoring Clone(TattooMapRegionAuthoring value)
    {
        return new TattooMapRegionAuthoring
        {
            partId = value.partId,
            shape = value.shape,
            start = value.start,
            end = value.end,
            width = value.width,
            center = value.center,
            size = value.size,
            rotationDegrees = value.rotationDegrees,
            points = value.points == null ? new List<Vector2>() : new List<Vector2>(value.points),
        };
    }

    private void DestroyPreviewTexture()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
}
