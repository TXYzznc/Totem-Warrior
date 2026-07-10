using System;
using System.Collections.Generic;
using PCGMap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class PCGMapDebugSceneController : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private int seed = 1001;
    [SerializeField] private bool lockSeed = true;
    [SerializeField] private bool randomizeSeedWhenUnlocked = true;
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 64;
    [SerializeField] private int objectBudget = 160;
    [SerializeField] private int stampBudget = 24;
    [SerializeField] private int decalBudget = 180;
    [SerializeField, Range(0.02f, 0.45f)] private float edgeMatchTolerance = 0.18f;
    [SerializeField] private bool autoGenerateOnStart = true;

    [Header("Zone Weights")]
    [SerializeField] private int teamSpawnZoneWeight = 100;
    [SerializeField] private int lootZoneWeight = 100;
    [SerializeField] private int combatZoneWeight = 100;
    [SerializeField] private int dangerZoneWeight = 100;

    [Header("Rendering")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool renderUnderlay = true;
    [SerializeField] private int maxVisualSprites = 1200;
    [SerializeField] private Camera sceneCamera;

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(512);
    private readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>(512);
    private GameObject mapRoot;
    private Text statusText;
    private InputField seedInput;
    private Toggle lockSeedToggle;
    private Toggle randomizeSeedToggle;
    private Toggle renderUnderlayToggle;
    private Slider edgeToleranceSlider;
    private Text edgeToleranceValueText;
    private InputField widthInput;
    private InputField heightInput;
    private InputField objectBudgetInput;
    private InputField stampBudgetInput;
    private InputField decalBudgetInput;
    private InputField maxVisualSpritesInput;
    private InputField teamSpawnWeightInput;
    private InputField lootWeightInput;
    private InputField combatWeightInput;
    private InputField dangerWeightInput;
    private string lastStatus = "PCG debug scene ready.";
    private int missingSpriteCount;
    private int tileSpriteCount;
    private int visualSpriteCount;

    public string LastStatus => lastStatus;

    public bool HasRenderedMap => mapRoot != null;

    private void Start()
    {
        EnsureSceneCamera();
        EnsureEventSystem();
        EnsureUi();

        if (autoGenerateOnStart)
        {
            GenerateAndRender();
        }
    }

    public void GenerateAndRender()
    {
        GenerateAndRender(true);
    }

    public void GenerateWithCurrentSeed()
    {
        GenerateAndRender(false);
    }

    private void GenerateAndRender(bool allowUnlockedSeedRandomization)
    {
        NormalizeSettings();
        if (allowUnlockedSeedRandomization && !lockSeed && randomizeSeedWhenUnlocked)
        {
            seed = CreateRandomSeed();
        }

        EnsureSceneCamera();
        if (Application.isPlaying)
        {
            EnsureEventSystem();
            EnsureUi();
        }

        ClearMap();
        missingSpriteCount = 0;
        tileSpriteCount = 0;
        visualSpriteCount = 0;
        RefreshControls();

        try
        {
            var assetIndex = PCGAssetIndex.LoadFromResources();
            var generator = new PCGMapGenerator(assetIndex);
            var map = generator.Generate(new PCGMapGenerateRequest
            {
                Seed = seed,
                Width = Mathf.Clamp(width, 8, 128),
                Height = Mathf.Clamp(height, 8, 128),
                ObjectBudget = Mathf.Max(0, objectBudget),
                StampBudget = Mathf.Max(0, stampBudget),
                DecalBudget = Mathf.Max(0, decalBudget),
                TeamSpawnZoneWeight = Mathf.Max(0, teamSpawnZoneWeight),
                LootZoneWeight = Mathf.Max(0, lootZoneWeight),
                CombatZoneWeight = Mathf.Max(0, combatZoneWeight),
                DangerZoneWeight = Mathf.Max(0, dangerZoneWeight),
                EdgeMatchTolerance = edgeMatchTolerance,
            });

            RenderMap(map);
            RefreshStatus(map, null);
        }
        catch (Exception ex)
        {
            RefreshStatus(null, $"PCG generation failed\nseed={seed}\n{ex.Message}");
            Debug.LogException(ex, this);
        }
    }

    public void GenerateNextSeed()
    {
        seed++;
        GenerateWithCurrentSeed();
    }

    public void GeneratePreviousSeed()
    {
        seed--;
        GenerateWithCurrentSeed();
    }

    public void RandomizeSeedAndGenerate()
    {
        seed = CreateRandomSeed();
        GenerateWithCurrentSeed();
    }

    public void ClearDebugMap()
    {
        ClearMap();
        missingSpriteCount = 0;
        tileSpriteCount = 0;
        visualSpriteCount = 0;
        RefreshStatus(null, "Map cleared.");
    }

    public void UseFullSize()
    {
        width = TotemMapService.PcgMapWidth;
        height = TotemMapService.PcgMapHeight;
        objectBudget = 160;
        stampBudget = 24;
        decalBudget = 180;
        maxVisualSprites = 1200;
        GenerateWithCurrentSeed();
    }

    public void UseDiagnosticSize()
    {
        width = TotemMapService.DiagnosticPcgMapWidth;
        height = TotemMapService.DiagnosticPcgMapHeight;
        objectBudget = 36;
        stampBudget = 8;
        decalBudget = 48;
        maxVisualSprites = 128;
        GenerateWithCurrentSeed();
    }

    public void SetSeedLocked(bool value)
    {
        lockSeed = value;
        RefreshControls();
    }

    private void NormalizeSettings()
    {
        width = Mathf.Clamp(width, 8, 128);
        height = Mathf.Clamp(height, 8, 128);
        objectBudget = Mathf.Max(0, objectBudget);
        stampBudget = Mathf.Max(0, stampBudget);
        decalBudget = Mathf.Max(0, decalBudget);
        maxVisualSprites = Mathf.Max(0, maxVisualSprites);
        cellSize = Mathf.Max(0.1f, cellSize);
        edgeMatchTolerance = Mathf.Clamp(edgeMatchTolerance, 0.02f, 0.45f);
        teamSpawnZoneWeight = Mathf.Max(0, teamSpawnZoneWeight);
        lootZoneWeight = Mathf.Max(0, lootZoneWeight);
        combatZoneWeight = Mathf.Max(0, combatZoneWeight);
        dangerZoneWeight = Mathf.Max(0, dangerZoneWeight);
    }

    private static int CreateRandomSeed()
    {
        unchecked
        {
            int timeHash = Environment.TickCount;
            int frameHash = Time.frameCount * 73856093;
            int randomHash = UnityEngine.Random.Range(-1000000000, 1000000000);
            return timeHash ^ frameHash ^ randomHash;
        }
    }

    private void RenderMap(PCGMapData map)
    {
        mapRoot = new GameObject("[PCG Debug Map]");
        var tileRoot = CreateTileRoot();
        var underlayTilemap = CreateTilemap(tileRoot, "PCG_Debug_UnderlayTilemap", -5);
        var groundTilemap = CreateTilemap(tileRoot, "PCG_Debug_GroundTilemap", 0);

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var cell = map.GetCell(x, y);
                if (renderUnderlay && !string.IsNullOrEmpty(cell.UnderlayAsset))
                {
                    SetTile(underlayTilemap, cell.UnderlayAsset, x, y, 0f, false, false);
                }

                bool useEdgeBase = !string.IsNullOrEmpty(cell.EdgeBaseAsset);
                if (SetTile(
                    groundTilemap,
                    useEdgeBase ? cell.EdgeBaseAsset : cell.BaseAsset,
                    x,
                    y,
                    useEdgeBase ? 0f : cell.BaseRotationDegrees,
                    !useEdgeBase && cell.BaseFlipX,
                    true))
                {
                    tileSpriteCount++;
                }
            }
        }

        int visualLimit = Mathf.Min(Mathf.Max(0, maxVisualSprites), map.Visuals.Count);
        for (int i = 0; i < visualLimit; i++)
        {
            RenderVisual(map.Visuals[i]);
        }

        PositionCamera(map.Width, map.Height);
    }

    private Transform CreateTileRoot()
    {
        var go = new GameObject("PCG_Debug_TileRoot");
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3(0f, 0.02f, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var grid = go.AddComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 1f);
        return go.transform;
    }

    private static Tilemap CreateTilemap(Transform parent, string objectName, int sortingOrder)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        var tilemap = go.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    private bool SetTile(Tilemap tilemap, string assetPath, int x, int y, float rotationDegrees, bool flipX, bool countMissing)
    {
        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0.5f), countMissing);
        if (sprite == null)
        {
            return false;
        }

        var tile = GetTile(assetPath, sprite);
        var position = new Vector3Int(x, y, 0);
        tilemap.SetTile(position, tile);

        if (Mathf.Abs(rotationDegrees) > 0.01f || flipX)
        {
            var scale = flipX ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            tilemap.SetTransformMatrix(position, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, rotationDegrees), scale));
        }

        return true;
    }

    private Tile GetTile(string assetPath, Sprite sprite)
    {
        string key = assetPath ?? string.Empty;
        if (tileCache.TryGetValue(key, out var tile) && tile != null)
        {
            return tile;
        }

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        tileCache[key] = tile;
        return tile;
    }

    private void RenderVisual(PCGPlacedVisual visual)
    {
        if (visual == null)
        {
            return;
        }

        int sortingOrder = GetSortingOrder(visual);
        string safeId = string.IsNullOrEmpty(visual.Id) ? "unnamed" : visual.Id;
        if (visual.Kind == PCGPlacedVisualKind.Object || visual.Kind == PCGPlacedVisualKind.Poi)
        {
            RenderStandingSprite(
                $"PCG_Debug_{visual.Kind}_{safeId}_{visual.X}_{visual.Y}",
                visual.Asset,
                visual.X,
                visual.Y,
                Mathf.Max(1, visual.Width),
                sortingOrder,
                visual.Kind == PCGPlacedVisualKind.Poi ? 1f : 1.35f);
            return;
        }

        float widthCells = Mathf.Max(1, visual.Width);
        float heightCells = Mathf.Max(1, visual.Height);
        RenderGroundSprite(
            $"PCG_Debug_{visual.Kind}_{safeId}_{visual.X}_{visual.Y}",
            visual.Asset,
            visual.X + widthCells * 0.5f - 0.5f,
            visual.Y + heightCells * 0.5f - 0.5f,
            widthCells,
            heightCells,
            sortingOrder,
            visual.RotationDegrees);
    }

    private static int GetSortingOrder(PCGPlacedVisual visual)
    {
        if (visual.HasSortingOrder)
        {
            return visual.SortingOrder;
        }

        switch (visual.Kind)
        {
            case PCGPlacedVisualKind.TransitionMask:
                return 40;
            case PCGPlacedVisualKind.TransitionDetail:
                return 50;
            case PCGPlacedVisualKind.Stamp:
                return 20;
            case PCGPlacedVisualKind.Decal:
                return 30;
            case PCGPlacedVisualKind.Poi:
                return 9000 - visual.Y * 10;
            case PCGPlacedVisualKind.Object:
                return 10000 - visual.Y * 10;
            default:
                return 100;
        }
    }

    private void RenderGroundSprite(
        string objectName,
        string assetPath,
        float cellX,
        float cellY,
        float widthCells,
        float heightCells,
        int sortingOrder,
        float rotationDegrees)
    {
        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0.5f), false);
        if (sprite == null)
        {
            return;
        }

        var go = new GameObject(objectName);
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3((cellX + 0.5f) * cellSize, 0.02f, (cellY + 0.5f) * cellSize);
        go.transform.rotation = Quaternion.Euler(90f, 0f, rotationDegrees);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        Vector2 size = sprite.bounds.size;
        if (size.x > 0f && size.y > 0f)
        {
            go.transform.localScale = new Vector3(widthCells * cellSize / size.x, heightCells * cellSize / size.y, 1f);
        }

        visualSpriteCount++;
    }

    private void RenderStandingSprite(
        string objectName,
        string assetPath,
        float cellX,
        float cellY,
        float footprintWidth,
        int sortingOrder,
        float scaleMultiplier)
    {
        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0f), true);
        if (sprite == null)
        {
            return;
        }

        var go = new GameObject(objectName);
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3((cellX + footprintWidth * 0.5f - 0.5f) * cellSize, 0.08f, cellY * cellSize);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        Vector2 size = sprite.bounds.size;
        if (size.x > 0f)
        {
            float scale = footprintWidth * cellSize / size.x * scaleMultiplier;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        visualSpriteCount++;
    }

    private Sprite GetPcgSprite(string assetPath, Vector2 pivot, bool countMissing)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            if (countMissing)
            {
                missingSpriteCount++;
            }

            return null;
        }

        string cacheKey = $"{assetPath}|{pivot.x:0.###},{pivot.y:0.###}";
        if (spriteCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(assetPath);
        if (sprite != null)
        {
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        var texture = Resources.Load<Texture2D>(assetPath);
        if (texture != null)
        {
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 128f, 0, SpriteMeshType.FullRect);
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        if (countMissing)
        {
            missingSpriteCount++;
        }

        spriteCache[cacheKey] = null;
        return null;
    }

    private void EnsureSceneCamera()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (sceneCamera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            sceneCamera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
        }

        sceneCamera.orthographic = true;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void EnsureUi()
    {
        if (statusText != null)
        {
            return;
        }

        var canvasGo = new GameObject("PCG Debug Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(430f, 620f);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.62f);

        statusText = CreateText(panel.transform, "StatusText", new Vector2(398f, 122f), new Vector2(16f, -12f), 13, TextAnchor.UpperLeft);
        statusText.text = lastStatus;

        CreateButton(panel.transform, "ClearButton", "Clear", new Vector2(76f, 30f), new Vector2(16f, -146f), ClearDebugMap);
        CreateButton(panel.transform, "GenerateButton", "Generate", new Vector2(96f, 30f), new Vector2(100f, -146f), GenerateAndRender);
        CreateButton(panel.transform, "CurrentSeedButton", "Refresh", new Vector2(86f, 30f), new Vector2(204f, -146f), GenerateWithCurrentSeed);
        CreateButton(panel.transform, "RandomSeedButton", "Random", new Vector2(86f, 30f), new Vector2(298f, -146f), RandomizeSeedAndGenerate);

        CreateButton(panel.transform, "PrevSeedButton", "Seed -", new Vector2(76f, 30f), new Vector2(16f, -184f), GeneratePreviousSeed);
        CreateButton(panel.transform, "NextSeedButton", "Seed +", new Vector2(76f, 30f), new Vector2(100f, -184f), GenerateNextSeed);
        CreateButton(panel.transform, "DiagnosticButton", "Fast", new Vector2(76f, 30f), new Vector2(184f, -184f), UseDiagnosticSize);
        CreateButton(panel.transform, "FullButton", "Full", new Vector2(76f, 30f), new Vector2(268f, -184f), UseFullSize);

        float y = -230f;
        seedInput = CreateIntInput(panel.transform, "SeedInput", "Seed", seed, new Vector2(16f, y), value => seed = value);
        y -= 34f;
        widthInput = CreateIntInput(panel.transform, "WidthInput", "Width", width, new Vector2(16f, y), value => width = value);
        heightInput = CreateIntInput(panel.transform, "HeightInput", "Height", height, new Vector2(218f, y), value => height = value);
        y -= 34f;
        objectBudgetInput = CreateIntInput(panel.transform, "ObjectBudgetInput", "Objects", objectBudget, new Vector2(16f, y), value => objectBudget = value);
        stampBudgetInput = CreateIntInput(panel.transform, "StampBudgetInput", "Stamps", stampBudget, new Vector2(218f, y), value => stampBudget = value);
        y -= 34f;
        decalBudgetInput = CreateIntInput(panel.transform, "DecalBudgetInput", "Decals", decalBudget, new Vector2(16f, y), value => decalBudget = value);
        maxVisualSpritesInput = CreateIntInput(panel.transform, "MaxVisualSpritesInput", "Max Visuals", maxVisualSprites, new Vector2(218f, y), value => maxVisualSprites = value);
        y -= 38f;

        CreateText(panel.transform, "EdgeToleranceLabel", new Vector2(122f, 24f), new Vector2(16f, y), 13, TextAnchor.MiddleLeft).text = "Edge Tolerance";
        edgeToleranceValueText = CreateText(panel.transform, "EdgeToleranceValue", new Vector2(50f, 24f), new Vector2(348f, y), 13, TextAnchor.MiddleRight);
        edgeToleranceSlider = CreateSlider(panel.transform, "EdgeToleranceSlider", new Vector2(190f, 20f), new Vector2(146f, y - 2f), 0.02f, 0.45f, edgeMatchTolerance, value =>
        {
            edgeMatchTolerance = value;
            RefreshControls();
        });
        y -= 36f;

        teamSpawnWeightInput = CreateIntInput(panel.transform, "TeamSpawnWeightInput", "Spawn W", teamSpawnZoneWeight, new Vector2(16f, y), value => teamSpawnZoneWeight = value);
        lootWeightInput = CreateIntInput(panel.transform, "LootWeightInput", "Loot W", lootZoneWeight, new Vector2(218f, y), value => lootZoneWeight = value);
        y -= 34f;
        combatWeightInput = CreateIntInput(panel.transform, "CombatWeightInput", "Combat W", combatZoneWeight, new Vector2(16f, y), value => combatZoneWeight = value);
        dangerWeightInput = CreateIntInput(panel.transform, "DangerWeightInput", "Danger W", dangerZoneWeight, new Vector2(218f, y), value => dangerZoneWeight = value);
        y -= 40f;

        lockSeedToggle = CreateToggle(panel.transform, "LockSeedToggle", "Lock Seed", lockSeed, new Vector2(16f, y), value => lockSeed = value);
        randomizeSeedToggle = CreateToggle(panel.transform, "RandomizeSeedToggle", "Randomize When Unlocked", randomizeSeedWhenUnlocked, new Vector2(150f, y), value => randomizeSeedWhenUnlocked = value);
        y -= 32f;
        renderUnderlayToggle = CreateToggle(panel.transform, "RenderUnderlayToggle", "Render Underlay", renderUnderlay, new Vector2(16f, y), value => renderUnderlay = value);

        RefreshControls();
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Text CreateText(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, int fontSize, TextAnchor alignment)
    {
        var go = CreateUiObject(objectName, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static InputField CreateIntInput(
        Transform parent,
        string objectName,
        string label,
        int value,
        Vector2 anchoredPosition,
        Action<int> onValueChanged)
    {
        CreateText(parent, $"{objectName}_Label", new Vector2(82f, 24f), anchoredPosition, 13, TextAnchor.MiddleLeft).text = label;

        var go = CreateUiObject(objectName, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(106f, 24f);
        rect.anchoredPosition = anchoredPosition + new Vector2(88f, 0f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);
        var input = go.AddComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;

        var text = CreateText(go.transform, "Text", new Vector2(94f, 22f), new Vector2(6f, -1f), 13, TextAnchor.MiddleLeft);
        text.color = Color.white;
        input.textComponent = text;

        var placeholder = CreateText(go.transform, "Placeholder", new Vector2(94f, 22f), new Vector2(6f, -1f), 13, TextAnchor.MiddleLeft);
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        placeholder.text = "0";
        input.placeholder = placeholder;

        input.text = value.ToString();
        input.onEndEdit.AddListener(textValue =>
        {
            if (int.TryParse(textValue, out int parsed))
            {
                onValueChanged(parsed);
            }
        });
        return input;
    }

    private static Slider CreateSlider(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 anchoredPosition,
        float minValue,
        float maxValue,
        float value,
        UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        var go = CreateUiObject(objectName, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var background = CreateUiObject("Background", go.transform);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 4f);
        backgroundRect.anchoredPosition = Vector2.zero;
        background.AddComponent<Image>().color = new Color(0.2f, 0.24f, 0.28f, 1f);

        var fillArea = CreateUiObject("Fill Area", go.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-5f, 0f);

        var fill = CreateUiObject("Fill", fillArea.transform);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.42f, 0.64f, 0.9f, 1f);

        var handleArea = CreateUiObject("Handle Slide Area", go.transform);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(5f, 0f);
        handleAreaRect.offsetMax = new Vector2(-5f, 0f);

        var handle = CreateUiObject("Handle", handleArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 14f);
        handle.AddComponent<Image>().color = new Color(0.9f, 0.92f, 0.95f, 1f);

        var slider = go.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }

    private static Toggle CreateToggle(
        Transform parent,
        string objectName,
        string label,
        bool value,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        var go = CreateUiObject(objectName, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(230f, 24f);
        rect.anchoredPosition = anchoredPosition;

        var box = CreateUiObject("Box", go.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.sizeDelta = new Vector2(18f, 18f);
        boxRect.anchoredPosition = Vector2.zero;
        var boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);

        var check = CreateUiObject("Checkmark", box.transform);
        var checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(10f, 10f);
        checkRect.anchoredPosition = Vector2.zero;
        var checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.42f, 0.9f, 0.58f, 1f);

        CreateText(go.transform, "Label", new Vector2(200f, 24f), new Vector2(26f, 0f), 13, TextAnchor.MiddleLeft).text = label;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(onValueChanged);
        return toggle;
    }

    private static void CreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 size,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateUiObject(objectName, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.27f, 0.95f);
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var labelText = CreateText(go.transform, "Label", size, Vector2.zero, 13, TextAnchor.MiddleCenter);
        labelText.text = label;
        labelText.raycastTarget = false;
    }

    private void PositionCamera(int mapWidth, int mapHeight)
    {
        if (sceneCamera == null)
        {
            return;
        }

        float centerX = mapWidth * cellSize * 0.5f;
        float centerZ = mapHeight * cellSize * 0.5f;
        sceneCamera.transform.position = new Vector3(centerX, 64f, centerZ - 0.01f);
        sceneCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sceneCamera.orthographicSize = Mathf.Max(mapWidth, mapHeight) * cellSize * 0.58f;
    }

    private void RefreshStatus(PCGMapData map, string error)
    {
        if (map == null)
        {
            lastStatus = string.IsNullOrEmpty(error) ? "No map rendered." : error;
            if (statusText != null)
            {
                statusText.text = lastStatus;
            }

            return;
        }

        var validation = map.Validation;
        lastStatus =
            $"PCG Debug Scene\n" +
            $"seed={seed} locked={lockSeed} size={map.Width}x{map.Height} hash={map.ContentHash}\n" +
            $"valid={validation.IsValid} walkable={validation.WalkableCells} reachable={validation.ReachableCells} unreachable={validation.UnreachableCells}\n" +
            $"visuals={map.Visuals.Count} rendered={visualSpriteCount} tiles={tileSpriteCount} missingSprites={missingSpriteCount}\n" +
            $"time={map.Diagnostics.TotalMs}ms steps={map.Diagnostics.Steps.Count}\n" +
            $"warnings={validation.Warnings.Count}";

        if (statusText != null)
        {
            statusText.text = lastStatus;
        }
    }

    private void ClearMap()
    {
        if (mapRoot != null)
        {
            DestroyUnityObject(mapRoot);
            mapRoot = null;
        }
    }

    private void RefreshControls()
    {
        if (seedInput != null)
        {
            seedInput.SetTextWithoutNotify(seed.ToString());
        }

        if (lockSeedToggle != null)
        {
            lockSeedToggle.SetIsOnWithoutNotify(lockSeed);
        }

        if (randomizeSeedToggle != null)
        {
            randomizeSeedToggle.SetIsOnWithoutNotify(randomizeSeedWhenUnlocked);
        }

        if (renderUnderlayToggle != null)
        {
            renderUnderlayToggle.SetIsOnWithoutNotify(renderUnderlay);
        }

        if (edgeToleranceSlider != null)
        {
            edgeToleranceSlider.SetValueWithoutNotify(edgeMatchTolerance);
        }

        if (edgeToleranceValueText != null)
        {
            edgeToleranceValueText.text = edgeMatchTolerance.ToString("0.###");
        }

        SetInputText(widthInput, width);
        SetInputText(heightInput, height);
        SetInputText(objectBudgetInput, objectBudget);
        SetInputText(stampBudgetInput, stampBudget);
        SetInputText(decalBudgetInput, decalBudget);
        SetInputText(maxVisualSpritesInput, maxVisualSprites);
        SetInputText(teamSpawnWeightInput, teamSpawnZoneWeight);
        SetInputText(lootWeightInput, lootZoneWeight);
        SetInputText(combatWeightInput, combatZoneWeight);
        SetInputText(dangerWeightInput, dangerZoneWeight);
    }

    private static void SetInputText(InputField input, int value)
    {
        if (input != null)
        {
            input.SetTextWithoutNotify(value.ToString());
        }
    }

    private static void DestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void OnDestroy()
    {
        ClearMap();
        foreach (var pair in tileCache)
        {
            if (pair.Value != null)
            {
                DestroyUnityObject(pair.Value);
            }
        }

        tileCache.Clear();
        spriteCache.Clear();
    }
}
