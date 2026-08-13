using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TotemCombatHUDForm : TotemUIFormBase
{
    private const float RefreshIntervalSeconds = 0.25f;
    private const int MaxLogRows = 8;
    private const int MinimapSize = 96;

    private static readonly Color32 MinimapBackground = new Color32(16, 18, 22, 220);
    private static readonly Color32 MinimapRoom = new Color32(70, 76, 88, 255);
    private static readonly Color32 MinimapZone = new Color32(77, 180, 255, 255);
    private static readonly Color32 MinimapPlayer = new Color32(94, 224, 94, 255);
    private static readonly Color32 MinimapOpponent = new Color32(230, 82, 72, 255);

    [SerializeField] private Image hpBar;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Component ammoText;
    [SerializeField] private Component zoneTimerText;
    [SerializeField] private Transform logListRoot;
    [SerializeField] private TMP_Text logRowTemplate;
    [SerializeField] private RawImage minimapImage;

    private readonly WaitForSeconds refreshWait = new WaitForSeconds(RefreshIntervalSeconds);
    private readonly List<TMP_Text> logRows = new List<TMP_Text>(MaxLogRows);
    private Coroutine refreshCoroutine;
    private Coroutine startupProtectionReleaseCoroutine;
    private string lastCombatLogSignature = string.Empty;
    private Texture2D minimapTexture;
    private Color32[] minimapPixels;
    private TotemFirstPlayableHudPresenter firstPlayablePresenter;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (hpBar == null)
        {
            hpBar = FindChildComponent<Image>("HpBar");
        }

        if (weaponIcon == null)
        {
            weaponIcon = FindChildComponent<Image>("WeaponIcon");
        }

        if (ammoText == null)
        {
            ammoText = FindChildComponent<TMPro.TMP_Text>("AmmoText");
        }

        if (ammoText == null)
        {
            ammoText = FindChildComponent<Text>("AmmoText");
        }

        if (zoneTimerText == null)
        {
            zoneTimerText = FindChildComponent<TMPro.TMP_Text>("ZoneTimerText");
        }

        if (zoneTimerText == null)
        {
            zoneTimerText = FindChildComponent<Text>("ZoneTimerText");
        }

        if (logListRoot == null)
        {
            logListRoot = FindChildTransform("LogListRoot");
        }

        if (logRowTemplate == null)
        {
            logRowTemplate = FindChildComponent<TMP_Text>("LogRowTemplate");
        }

        if (logRowTemplate != null)
        {
            logRowTemplate.gameObject.SetActive(false);
        }

        if (minimapImage == null)
        {
            minimapImage = FindChildComponent<RawImage>("MinimapImage");
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ResetDynamicHudRows();
        ApplyInitialHudState();
        firstPlayablePresenter = GetComponent<TotemFirstPlayableHudPresenter>();
        if (firstPlayablePresenter == null)
        {
            firstPlayablePresenter = gameObject.AddComponent<TotemFirstPlayableHudPresenter>();
        }
        firstPlayablePresenter.Initialize(Runtime);
        refreshCoroutine = StartCoroutine(RefreshHudLoop());
        GFTrace.Success("TotemUI", "CombatHUD.Open");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (startupProtectionReleaseCoroutine != null)
        {
            StopCoroutine(startupProtectionReleaseCoroutine);
            startupProtectionReleaseCoroutine = null;
        }

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        if (firstPlayablePresenter != null)
        {
            firstPlayablePresenter.Shutdown();
            Destroy(firstPlayablePresenter);
            firstPlayablePresenter = null;
        }

        ResetDynamicHudRows();
        base.OnClose(isShutdown, userData);
    }

    protected override void OnOpenAnimationComplete()
    {
        base.OnOpenAnimationComplete();
        if (startupProtectionReleaseCoroutine != null)
        {
            StopCoroutine(startupProtectionReleaseCoroutine);
        }

        startupProtectionReleaseCoroutine = StartCoroutine(ReleaseStartupProtectionAfterControllableFrame());
    }

    private IEnumerator ReleaseStartupProtectionAfterControllableFrame()
    {
        var actorService = ActorService;
        var expectedPlayer = actorService?.Player;
        yield return null;

        while (isActiveAndEnabled && actorService != null && expectedPlayer != null)
        {
            if (Camera.main != null
                && InputService != null
                && ReadinessService != null
                && ReadinessService.NotifyLocalClientReady(expectedPlayer, "CombatHUD.Interactable"))
            {
                break;
            }

            yield return null;
        }

        startupProtectionReleaseCoroutine = null;
    }

    private void ApplyInitialHudState()
    {
        string weaponId = TotemWeaponService.DefaultWeaponId;

        ApplyIcon(weaponIcon, GetWeaponAssetKey(weaponId));
        RefreshRuntimeState();

        GFTrace.Info("TotemUI", "CombatHUD.StateApplied", null, GFTrace.Data(
            "characterId", "1",
            "weaponId", weaponId));
    }

    private IEnumerator RefreshHudLoop()
    {
        while (true)
        {
            yield return refreshWait;
            RefreshRuntimeState();
        }
    }

    private void RefreshRuntimeState()
    {
        RefreshPlayerHp();
        RefreshWeaponText();
        RefreshCombatLog();
        RefreshMinimap();
        RefreshZoneText();
        firstPlayablePresenter?.Refresh();
    }

    private void RefreshPlayerHp()
    {
        float playerHealth = ActorService?.Player?.Health ?? 0f;
        float playerMaxHealth = ActorService?.Player?.MaxHealth ?? 1f;
        if (hpBar != null && playerMaxHealth > 0f)
        {
            float ratio = Mathf.Clamp01(playerHealth / playerMaxHealth);
            hpBar.fillAmount = ratio;
            hpBar.color = GetHpColor(ratio);
        }
    }

    private void RefreshWeaponText()
    {
        string weaponId = TotemWeaponService.DefaultWeaponId;
        SetText(ammoText, FormatWeaponStatus(weaponId, 0, false, 0f, 0f));
    }

    private void RefreshCombatLog()
    {
        var snapshot = CombatService?.CaptureCombatSnapshot();
        string line = FormatCombatLog(snapshot);
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string signature = BuildCombatLogSignature(snapshot);
        if (string.Equals(signature, lastCombatLogSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastCombatLogSignature = signature;
        AppendCombatLog(line);
    }

    private void RefreshMinimap()
    {
        if (minimapImage == null || MapService?.CurrentMap == null)
        {
            return;
        }

        EnsureMinimapTexture();
        if (!BuildMinimapPixels(minimapPixels, MinimapSize, MapService.CurrentMap, ActorService?.Actors, ZoneService?.CaptureSnapshot()))
        {
            return;
        }

        minimapTexture.SetPixels32(minimapPixels);
        minimapTexture.Apply(false);
        minimapImage.texture = minimapTexture;
        minimapImage.color = Color.white;
    }

    private void RefreshZoneText()
    {
        var interaction = InteractionService?.CaptureSnapshot();
        string prompt = interaction?.prompt ?? string.Empty;
        string statusSummary = TotemStatusService.FormatStatusSummary(StatusService?.CaptureSnapshot(ActorService?.Player));
        string matchStatus = FormatMatchFlowStatus(MatchFlowService);
        var zone = ZoneService?.CaptureSnapshot();
        if (zone == null || !zone.active)
        {
            SetText(zoneTimerText, AppendStatus(AppendPrompt(matchStatus, prompt), statusSummary));
            return;
        }

        SetText(zoneTimerText, AppendStatus(AppendPrompt(AppendMatchStatus(matchStatus, FormatZoneStatus(zone.currentPhaseId, zone.currentRadius, zone.outZoneDamage)), prompt), statusSummary));
    }

    public static string FormatMatchFlowStatus(TotemMatchFlowService flow)
    {
        if (flow == null || !flow.IsRunning)
        {
            return string.Empty;
        }

        int seconds = Mathf.CeilToInt(flow.ActivityRemaining);
        switch (flow.CurrentPhase)
        {
            case TotemMatchPhase.OpeningBuild: return $"开局构筑  {seconds}s";
            case TotemMatchPhase.Round1Combat: return $"第1轮战斗  {seconds}s";
            case TotemMatchPhase.Build2: return $"第2轮构筑  {seconds}s  · 预告第1次缩圈";
            case TotemMatchPhase.Round2Combat:
                return flow.CurrentActivity == TotemMatchActivity.ZoneShrink
                    ? $"第1次缩圈  {seconds}s"
                    : $"第2轮战斗  {seconds}s";
            case TotemMatchPhase.Build3: return $"第3轮构筑  {seconds}s  · 预告第2次缩圈";
            case TotemMatchPhase.Round3Combat:
                return flow.CurrentActivity == TotemMatchActivity.ZoneShrink
                    ? $"第2次缩圈  {seconds}s"
                    : $"第3轮战斗  {seconds}s";
            case TotemMatchPhase.Build4: return $"第4轮构筑  {seconds}s  · 预告第3次缩圈";
            case TotemMatchPhase.Round4Combat:
                return flow.CurrentActivity == TotemMatchActivity.ZoneShrink
                    ? $"第3次缩圈  {seconds}s"
                    : $"第4轮战斗  {seconds}s";
            case TotemMatchPhase.Build5: return $"第5轮构筑  {seconds}s  · 预告第4次缩圈";
            case TotemMatchPhase.Round5Combat:
                return flow.CurrentActivity == TotemMatchActivity.ZoneShrink
                    ? $"第4次缩圈  {seconds}s"
                    : $"第5轮最终战斗与撤离  {seconds}s";
            case TotemMatchPhase.Result: return "本局结果";
            default: return string.Empty;
        }
    }

    private static string AppendMatchStatus(string matchStatus, string detail)
    {
        if (string.IsNullOrWhiteSpace(matchStatus))
        {
            return detail ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(detail) ? matchStatus : $"{matchStatus}  |  {detail}";
    }

    public static string FormatWeaponStatus(string weaponId, int ammo, bool showAmmo, float skillECooldown, float skillQCooldown = 0f)
    {
        if (showAmmo)
        {
            return $"Weapon: {weaponId}  Ammo: {ammo}";
        }

        return $"Weapon: {weaponId}";
    }

    public static string FormatZoneStatus(int phaseId, float radius, float outZoneDamage)
    {
        return $"Zone P{phaseId} R{radius:F0} D{outZoneDamage:F0}";
    }

    public static string AppendPrompt(string status, string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return status ?? string.Empty;
        }

        return $"{status}  {prompt}";
    }

    public static string AppendStatus(string status, string statusSummary)
    {
        if (string.IsNullOrWhiteSpace(statusSummary) || string.Equals(statusSummary, "Status: None", System.StringComparison.Ordinal))
        {
            return status ?? string.Empty;
        }

        return $"{status}  {statusSummary}";
    }

    public static Color GetHpColor(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        if (ratio > 0.5f)
        {
            return Color.green;
        }

        if (ratio > 0.25f)
        {
            return Color.yellow;
        }

        return Color.red;
    }

    [Obsolete("Legacy GF_X diagnostic compatibility only. First playable has no player skill cooldown UI.")]
    public static float CalculateCooldownMaskFill(float remaining, float cooldownWindow)
    {
        return remaining <= 0f || cooldownWindow <= 0f ? 0f : Mathf.Clamp01(remaining / cooldownWindow);
    }

    public static string FormatCombatLog(TotemCombatSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.lastAction))
        {
            return string.Empty;
        }

        if (string.Equals(snapshot.lastAction, "CombatStarted", StringComparison.Ordinal))
        {
            return "Combat started";
        }

        string target = !string.IsNullOrWhiteSpace(snapshot.lastTargetName)
            ? snapshot.lastTargetName
            : snapshot.lastTargetActorId > 0 ? $"Actor {snapshot.lastTargetActorId}" : string.Empty;
        string suffix = snapshot.lastKilled ? " KO" : string.Empty;
        string detail = snapshot.lastDamage > 0f ? $" -{snapshot.lastDamage:F0}{suffix}" : suffix;
        string source = snapshot.lastWeaponId;

        if (!string.IsNullOrWhiteSpace(target))
        {
            return string.IsNullOrWhiteSpace(source)
                ? $"{snapshot.lastAction}: {target}{detail}"
                : $"{snapshot.lastAction}: {target}{detail} [{source}]";
        }

        return string.IsNullOrWhiteSpace(snapshot.lastReason)
            ? snapshot.lastAction
            : $"{snapshot.lastAction}: {snapshot.lastReason}";
    }

    public static bool BuildMinimapPixels(Color32[] pixels, int size, TotemMapSnapshot map, IReadOnlyList<TotemActorModel> actors, TotemZoneSnapshot zone)
    {
        if (pixels == null || size <= 0 || pixels.Length < size * size || map == null || map.MapSize <= 0f)
        {
            return false;
        }

        for (int i = 0; i < size * size; i++)
        {
            pixels[i] = MinimapBackground;
        }

        var rooms = map.Rooms;
        for (int i = 0; rooms != null && i < rooms.Length; i++)
        {
            DrawRoom(pixels, size, map, rooms[i]);
        }

        float radius = zone != null && zone.active ? zone.currentRadius : TotemMapService.GetInitialZoneRadius(map);
        DrawCircleOutline(pixels, size, map, new Vector2(map.InitialZoneCenter.x, map.InitialZoneCenter.y), radius, MinimapZone);

        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            Color32 color = actor.ControllerKind == TotemParticipantControllerKind.Human ? MinimapPlayer : MinimapOpponent;
            int radiusPx = actor.ControllerKind == TotemParticipantControllerKind.Human ? 2 : 1;
            DrawDot(pixels, size, map, actor.Position, radiusPx, color);
        }

        return true;
    }

    public static int CountMinimapPixelsDifferentFromBackground(Color32[] pixels)
    {
        if (pixels == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!EqualsColor(pixels[i], MinimapBackground))
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyIcon(Image image, string assetKey)
    {
        if (image == null)
        {
            return;
        }

        if (!TryLoadRuntimeSprite(assetKey, out var sprite))
        {
            GFTrace.Warning("TotemUI", "CombatHUD.IconMissing", null, GFTrace.Data("assetKey", assetKey));
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private void AppendCombatLog(string line)
    {
        if (logListRoot == null || logRowTemplate == null)
        {
            return;
        }

        var row = Instantiate(logRowTemplate, logListRoot);
        row.gameObject.SetActive(true);
        row.SetText(line);
        logRows.Add(row);
        while (logRows.Count > MaxLogRows)
        {
            var first = logRows[0];
            logRows.RemoveAt(0);
            if (first != null)
            {
                Destroy(first.gameObject);
            }
        }
    }

    private void ResetDynamicHudRows()
    {
        for (int i = logRows.Count - 1; i >= 0; i--)
        {
            if (logRows[i] != null)
            {
                Destroy(logRows[i].gameObject);
            }
        }

        logRows.Clear();
        lastCombatLogSignature = string.Empty;
    }

    private void EnsureMinimapTexture()
    {
        if (minimapTexture != null && minimapPixels != null && minimapPixels.Length == MinimapSize * MinimapSize)
        {
            return;
        }

        minimapPixels = new Color32[MinimapSize * MinimapSize];
        minimapTexture = new Texture2D(MinimapSize, MinimapSize, TextureFormat.RGBA32, false)
        {
            name = "TotemCombatHUD_Minimap",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
    }

    private static void DrawRoom(Color32[] pixels, int size, TotemMapSnapshot map, TotemRoomInfo room)
    {
        if (room == null)
        {
            return;
        }

        Vector2 min = TotemMapService.GetWorldMin(map);
        Vector2 max = TotemMapService.GetWorldMax(map);
        int minX = WorldToMinimapPixel(room.Bounds.xMin, min.x, max.x, size);
        int maxX = WorldToMinimapPixel(room.Bounds.xMax, min.x, max.x, size);
        int minY = WorldToMinimapPixel(room.Bounds.yMin, min.y, max.y, size);
        int maxY = WorldToMinimapPixel(room.Bounds.yMax, min.y, max.y, size);
        for (int y = Mathf.Min(minY, maxY); y <= Mathf.Max(minY, maxY); y++)
        {
            for (int x = Mathf.Min(minX, maxX); x <= Mathf.Max(minX, maxX); x++)
            {
                SetMinimapPixel(pixels, size, x, y, MinimapRoom);
            }
        }
    }

    private static void DrawCircleOutline(Color32[] pixels, int size, TotemMapSnapshot map, Vector2 center, float radius, Color32 color)
    {
        if (radius <= 0f)
        {
            return;
        }

        Vector2 min = TotemMapService.GetWorldMin(map);
        Vector2 max = TotemMapService.GetWorldMax(map);
        int centerX = WorldToMinimapPixel(center.x, min.x, max.x, size);
        int centerY = WorldToMinimapPixel(center.y, min.y, max.y, size);
        float radiusPx = Mathf.Max(1f, radius / Mathf.Max(max.x - min.x, max.y - min.y) * (size - 1));
        float radiusSqr = radiusPx * radiusPx;
        float innerSqr = Mathf.Max(0f, radiusPx - 1.25f) * Mathf.Max(0f, radiusPx - 1.25f);
        int bound = Mathf.CeilToInt(radiusPx) + 1;
        for (int y = centerY - bound; y <= centerY + bound; y++)
        {
            for (int x = centerX - bound; x <= centerX + bound; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distanceSqr = dx * dx + dy * dy;
                if (distanceSqr <= radiusSqr && distanceSqr >= innerSqr)
                {
                    SetMinimapPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static void DrawDot(Color32[] pixels, int size, TotemMapSnapshot map, Vector3 worldPosition, int radiusPx, Color32 color)
    {
        Vector2 min = TotemMapService.GetWorldMin(map);
        Vector2 max = TotemMapService.GetWorldMax(map);
        int centerX = WorldToMinimapPixel(worldPosition.x, min.x, max.x, size);
        int centerY = WorldToMinimapPixel(worldPosition.z, min.y, max.y, size);
        radiusPx = Mathf.Max(1, radiusPx);
        for (int y = centerY - radiusPx; y <= centerY + radiusPx; y++)
        {
            for (int x = centerX - radiusPx; x <= centerX + radiusPx; x++)
            {
                if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radiusPx * radiusPx)
                {
                    SetMinimapPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static int WorldToMinimapPixel(float value, float min, float max, int size)
    {
        float normalized = max <= min ? 0f : Mathf.InverseLerp(min, max, value);
        return Mathf.Clamp(Mathf.RoundToInt(normalized * (size - 1)), 0, size - 1);
    }

    private static void SetMinimapPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (pixels == null || x < 0 || x >= size || y < 0 || y >= size)
        {
            return;
        }

        pixels[y * size + x] = color;
    }

    private static bool EqualsColor(Color32 left, Color32 right)
    {
        return left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
    }

    private static string BuildCombatLogSignature(TotemCombatSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return string.Empty;
        }

        return string.Join("|",
            snapshot.lastAction ?? string.Empty,
            snapshot.lastReason ?? string.Empty,
            snapshot.lastTargetActorId.ToString(),
            snapshot.lastDamage.ToString("F2"),
            snapshot.lastKilled.ToString(),
            snapshot.lastWeaponId ?? string.Empty);
    }

    private static string GetWeaponAssetKey(string weaponId)
    {
        return string.Equals(weaponId, TotemWeaponService.DefaultWeaponId, System.StringComparison.Ordinal)
            ? TotemFirstPlayableArtHandoff.WeaponKey
            : string.Empty;
    }

}
