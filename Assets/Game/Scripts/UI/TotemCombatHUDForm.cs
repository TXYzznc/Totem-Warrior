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
    private static readonly Color32 MinimapEnemy = new Color32(230, 82, 72, 255);
    private static readonly Color32 MinimapBoss = new Color32(216, 86, 255, 255);

    [SerializeField] private Image hpBar;
    [SerializeField] private Image bossHpBar;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image skillSlotEIcon;
    [SerializeField] private Image skillSlotQIcon;
    [SerializeField] private Image skillSlotECooldownMask;
    [SerializeField] private Image skillSlotQCooldownMask;
    [SerializeField] private GameObject bossHpRoot;
    [SerializeField] private Component ammoText;
    [SerializeField] private Component zoneTimerText;
    [SerializeField] private Transform logListRoot;
    [SerializeField] private TMP_Text logRowTemplate;
    [SerializeField] private Transform buildListRoot;
    [SerializeField] private RawImage minimapImage;

    private readonly WaitForSeconds refreshWait = new WaitForSeconds(RefreshIntervalSeconds);
    private readonly List<TMP_Text> logRows = new List<TMP_Text>(MaxLogRows);
    private Coroutine refreshCoroutine;
    private Coroutine startupProtectionReleaseCoroutine;
    private string lastSkillSlotEAssetKey = string.Empty;
    private string lastSkillSlotQAssetKey = string.Empty;
    private string lastCombatLogSignature = string.Empty;
    private string lastBuildSummary = string.Empty;
    private TMP_Text buildSummaryText;
    private Texture2D minimapTexture;
    private Color32[] minimapPixels;
    private readonly TotemEnemyModel[] minimapEnemyBuffer = new TotemEnemyModel[TotemEnemyService.DefaultEnemyCapacity];

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (hpBar == null)
        {
            hpBar = FindChildComponent<Image>("HpBar");
        }

        if (bossHpBar == null)
        {
            bossHpBar = FindChildComponent<Image>("BossHpBar");
        }

        if (weaponIcon == null)
        {
            weaponIcon = FindChildComponent<Image>("WeaponIcon");
        }

        if (skillSlotEIcon == null)
        {
            skillSlotEIcon = FindChildComponent<Image>("SkillSlotE");
        }

        if (skillSlotQIcon == null)
        {
            skillSlotQIcon = FindChildComponent<Image>("SkillSlotQ");
        }

        if (skillSlotECooldownMask == null)
        {
            skillSlotECooldownMask = FindChildComponent<Image>("CdMaskE");
        }

        if (skillSlotQCooldownMask == null)
        {
            skillSlotQCooldownMask = FindChildComponent<Image>("CdMaskQ");
        }

        if (bossHpRoot == null)
        {
            bossHpRoot = FindChildTransform("BossHpRoot")?.gameObject;
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

        if (buildListRoot == null)
        {
            buildListRoot = FindChildTransform("BuildListRoot");
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
        var selection = FlowService?.StartupSelection;
        string weaponId = selection == null || string.IsNullOrWhiteSpace(selection.WeaponId)
            ? "knife_basic"
            : selection.WeaponId;

        ApplyIcon(weaponIcon, GetWeaponAssetKey(weaponId));
        RefreshSkillIcons(force: true);
        RefreshRuntimeState();

        GFTrace.Info("TotemUI", "CombatHUD.StateApplied", null, GFTrace.Data(
            "characterId", (selection?.CharacterId ?? 1).ToString(),
            "weaponId", weaponId,
            "colorId", (selection?.ColorId ?? 1).ToString()));
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
        RefreshBossHp();
        RefreshWeaponText();
        RefreshSkillIcons(force: false);
        RefreshSkillCooldownMasks();
        RefreshBuildList();
        RefreshCombatLog();
        RefreshMinimap();
        RefreshZoneText();
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

    private void RefreshBossHp()
    {
        var boss = EnemyService?.FindClosestAliveEnemy(Vector3.zero, 0f, TotemEnemyTier.Boss);
        bool showBoss = boss != null && boss.IsAlive;
        if (bossHpRoot != null && bossHpRoot.activeSelf != showBoss)
        {
            bossHpRoot.SetActive(showBoss);
        }

        if (bossHpBar != null)
        {
            bossHpBar.fillAmount = showBoss ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f;
        }
    }

    private void RefreshWeaponText()
    {
        var selection = FlowService?.StartupSelection;
        string weaponId = selection == null || string.IsNullOrWhiteSpace(selection.WeaponId)
            ? "knife_basic"
            : selection.WeaponId;
        var weaponState = WeaponService?.GetOrCreateState(ActorService?.Player);
        float skillECooldown = SkillService?.GetCooldownRemaining(ActorService?.Player, 0) ?? 0f;
        float skillQCooldown = SkillService?.GetCooldownRemaining(ActorService?.Player, 1) ?? 0f;

        if (weaponState?.Weapon != null && weaponState.Weapon.MaxAmmo > 0)
        {
            SetText(ammoText, FormatWeaponStatus(weaponId, weaponState.CurrentAmmo, true, skillECooldown, skillQCooldown));
            return;
        }

        SetText(ammoText, FormatWeaponStatus(weaponId, 0, false, skillECooldown, skillQCooldown));
    }

    private void RefreshSkillCooldownMasks()
    {
        var player = ActorService?.Player;
        ApplyCooldownMask(skillSlotECooldownMask, player, 0);
        ApplyCooldownMask(skillSlotQCooldownMask, player, 1);
    }

    private void ApplyCooldownMask(Image mask, TotemActorModel player, int slot)
    {
        if (mask == null)
        {
            return;
        }

        string skillId = SkillService?.GetEquippedSkillId(player, slot);
        float remaining = SkillService?.GetCooldownRemaining(player, slot) ?? 0f;
        float window = ResolveSkillCooldownWindow(skillId);
        mask.fillAmount = CalculateCooldownMaskFill(remaining, window);
    }

    private float ResolveSkillCooldownWindow(string skillId)
    {
        if (SkillService == null || string.IsNullOrWhiteSpace(skillId) || !SkillService.TryGetRuntimeDefinition(skillId, out var skill))
        {
            return 0f;
        }

        return ResolveSkillCooldownWindow(skill);
    }

    private void RefreshBuildList()
    {
        if (buildListRoot == null || logRowTemplate == null)
        {
            return;
        }

        string summary = FormatBuildSummary(TattooService?.CaptureSnapshot());
        if (string.Equals(summary, lastBuildSummary, StringComparison.Ordinal))
        {
            return;
        }

        lastBuildSummary = summary;
        if (buildSummaryText == null)
        {
            buildSummaryText = Instantiate(logRowTemplate, buildListRoot);
            buildSummaryText.name = "BuildSummaryRuntime";
            buildSummaryText.gameObject.SetActive(true);
        }

        buildSummaryText.SetText(summary);
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
        int enemyCount = EnemyService?.CopyAliveEnemies(minimapEnemyBuffer) ?? 0;
        if (!BuildMinimapPixels(minimapPixels, MinimapSize, MapService.CurrentMap, ActorService?.Actors, ZoneService?.CaptureSnapshot(), minimapEnemyBuffer, enemyCount))
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
        int aliveEnemyCount = EnemyService?.CaptureSnapshot().aliveEnemyCount ?? 0;
        var interaction = InteractionService?.CaptureSnapshot();
        string prompt = interaction?.prompt ?? string.Empty;
        string statusSummary = TotemStatusService.FormatStatusSummary(StatusService?.CaptureSnapshot(ActorService?.Player));
        var zone = ZoneService?.CaptureSnapshot();
        if (zone == null || !zone.active)
        {
            SetText(zoneTimerText, AppendStatus(AppendPrompt(FormatEnemyStatus(aliveEnemyCount), prompt), statusSummary));
            return;
        }

        SetText(zoneTimerText, AppendStatus(AppendPrompt(FormatZoneStatus(zone.currentPhaseId, zone.currentRadius, zone.outZoneDamage, aliveEnemyCount), prompt), statusSummary));
    }

    public static string FormatWeaponStatus(string weaponId, int ammo, bool showAmmo, float skillECooldown, float skillQCooldown = 0f)
    {
        if (showAmmo)
        {
            return $"Weapon: {weaponId}  Ammo: {ammo}  E:{skillECooldown:F1}s  Q:{skillQCooldown:F1}s";
        }

        return $"Weapon: {weaponId}  E:{skillECooldown:F1}s  Q:{skillQCooldown:F1}s";
    }

    public static string FormatEnemyStatus(int aliveEnemyCount)
    {
        return $"Enemies: {aliveEnemyCount}";
    }

    public static string FormatZoneStatus(int phaseId, float radius, float outZoneDamage, int aliveEnemyCount)
    {
        return $"Zone P{phaseId} R{radius:F0} D{outZoneDamage:F0}  Enemies: {aliveEnemyCount}";
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

    public static float CalculateCooldownMaskFill(float remaining, float cooldownWindow)
    {
        if (remaining <= 0f || cooldownWindow <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(remaining / cooldownWindow);
    }

    public static float ResolveSkillCooldownWindow(TotemSkillDefinition skill)
    {
        if (skill == null)
        {
            return 0f;
        }

        switch (skill.ChargeModel)
        {
            case TotemSkillChargeModel.Charges:
                return Mathf.Max(0f, skill.ChargeRegenTime);
            case TotemSkillChargeModel.HoldRelease:
                return Mathf.Max(0f, skill.HoldDuration + skill.OverchargeWindow);
            default:
                return Mathf.Max(0f, skill.Cooldown);
        }
    }

    public static string FormatBuildSummary(TotemTattooSnapshot snapshot)
    {
        if (snapshot == null || snapshot.equippedCount <= 0 || string.IsNullOrWhiteSpace(snapshot.equippedSummary))
        {
            return "Build: none";
        }

        return $"Build: {snapshot.equippedSummary}";
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
        string source = !string.IsNullOrWhiteSpace(snapshot.lastSkillId)
            ? snapshot.lastSkillId
            : !string.IsNullOrWhiteSpace(snapshot.lastWeaponId) ? snapshot.lastWeaponId : snapshot.lastTraitId;

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
        return BuildMinimapPixels(pixels, size, map, actors, zone, null, 0);
    }

    public static bool BuildMinimapPixels(
        Color32[] pixels,
        int size,
        TotemMapSnapshot map,
        IReadOnlyList<TotemActorModel> actors,
        TotemZoneSnapshot zone,
        TotemEnemyModel[] enemies,
        int enemyCount)
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
            DrawRoom(pixels, size, map.MapSize, rooms[i]);
        }

        float radius = zone != null && zone.active ? zone.currentRadius : map.MapSize * 0.5f;
        DrawCircleOutline(pixels, size, map.MapSize, new Vector2(map.InitialZoneCenter.x, map.InitialZoneCenter.y), radius, MinimapZone);

        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            Color32 color = actor.ControllerKind == TotemParticipantControllerKind.Human ? MinimapPlayer : MinimapEnemy;
            int radiusPx = actor.ControllerKind == TotemParticipantControllerKind.Human ? 2 : 1;
            DrawDot(pixels, size, map.MapSize, actor.Position, radiusPx, color);
        }

        for (int i = 0; enemies != null && i < enemyCount && i < enemies.Length; i++)
        {
            TotemEnemyModel enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            bool boss = enemy.Tier == TotemEnemyTier.Boss;
            DrawDot(pixels, size, map.MapSize, enemy.Position, boss ? 2 : 1, boss ? MinimapBoss : new Color32(255, 55, 55, 255));
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

    private void RefreshSkillIcons(bool force)
    {
        var player = ActorService?.Player;
        string slotE = SkillService?.GetEquippedSkillId(player, 0);
        string slotQ = SkillService?.GetEquippedSkillId(player, 1);
        if (string.IsNullOrWhiteSpace(slotE))
        {
            slotE = "skill_fireball_01";
        }

        if (string.IsNullOrWhiteSpace(slotQ))
        {
            slotQ = "skill_stealth_01";
        }

        ApplySkillIconIfChanged(skillSlotEIcon, GetSkillAssetKey(slotE), ref lastSkillSlotEAssetKey, force);
        ApplySkillIconIfChanged(skillSlotQIcon, GetSkillAssetKey(slotQ), ref lastSkillSlotQAssetKey, force);
    }

    private void ApplySkillIconIfChanged(Image image, string assetKey, ref string lastAssetKey, bool force)
    {
        if (!force && string.Equals(lastAssetKey, assetKey, System.StringComparison.Ordinal))
        {
            return;
        }

        lastAssetKey = assetKey;
        ApplyIcon(image, assetKey);
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
        if (buildSummaryText != null)
        {
            Destroy(buildSummaryText.gameObject);
            buildSummaryText = null;
        }

        lastCombatLogSignature = string.Empty;
        lastBuildSummary = string.Empty;
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

    private static void DrawRoom(Color32[] pixels, int size, float mapSize, TotemRoomInfo room)
    {
        if (room == null)
        {
            return;
        }

        int minX = WorldToMinimapPixel(room.Bounds.xMin, mapSize, size);
        int maxX = WorldToMinimapPixel(room.Bounds.xMax, mapSize, size);
        int minY = WorldToMinimapPixel(room.Bounds.yMin, mapSize, size);
        int maxY = WorldToMinimapPixel(room.Bounds.yMax, mapSize, size);
        for (int y = Mathf.Min(minY, maxY); y <= Mathf.Max(minY, maxY); y++)
        {
            for (int x = Mathf.Min(minX, maxX); x <= Mathf.Max(minX, maxX); x++)
            {
                SetMinimapPixel(pixels, size, x, y, MinimapRoom);
            }
        }
    }

    private static void DrawCircleOutline(Color32[] pixels, int size, float mapSize, Vector2 center, float radius, Color32 color)
    {
        if (radius <= 0f)
        {
            return;
        }

        int centerX = WorldToMinimapPixel(center.x, mapSize, size);
        int centerY = WorldToMinimapPixel(center.y, mapSize, size);
        float radiusPx = Mathf.Max(1f, radius / mapSize * (size - 1));
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

    private static void DrawDot(Color32[] pixels, int size, float mapSize, Vector3 worldPosition, int radiusPx, Color32 color)
    {
        int centerX = WorldToMinimapPixel(worldPosition.x, mapSize, size);
        int centerY = WorldToMinimapPixel(worldPosition.z, mapSize, size);
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

    private static int WorldToMinimapPixel(float value, float mapSize, int size)
    {
        float normalized = mapSize <= 0f ? 0f : Mathf.Clamp01(value / mapSize);
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
            snapshot.lastWeaponId ?? string.Empty,
            snapshot.lastTraitId ?? string.Empty,
            snapshot.lastSkillId ?? string.Empty,
            snapshot.lastHitCount.ToString());
    }

    private static string GetWeaponAssetKey(string weaponId)
    {
        return string.IsNullOrWhiteSpace(weaponId) ? string.Empty : $"weapon.{weaponId}";
    }

    public static string GetSkillAssetKey(string skillId)
    {
        return string.IsNullOrWhiteSpace(skillId) ? string.Empty : $"skill.{skillId}";
    }
}
