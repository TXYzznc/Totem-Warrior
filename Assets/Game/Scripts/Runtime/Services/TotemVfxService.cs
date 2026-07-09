using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class TotemVfxService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private const float AttackHitLifetime = 0.35f;
    private const float SkillBurstLifetime = 0.60f;
    private const float NormalShakeAmplitude = 0.05f;
    private const float NormalShakeDuration = 0.12f;
    private const float StrongShakeAmplitude = 0.10f;
    private const float StrongShakeDuration = 0.18f;
    private const float DangerHealthRatio = 0.30f;
    private const float VignetteIntensityMax = 0.45f;
    private const float VignettePulseDuration = 1.0f;
    private const float VignetteFadeOutSpeed = 2.5f;
    private const float DamageFloatLifetime = 0.70f;
    private const float DamageFloatRise = 1.0f;
    private const float StrongDamageFloatRise = 1.25f;

    private readonly List<TotemVfxInstance> instances = new List<TotemVfxInstance>(32);
    private readonly List<TotemFloatingTextInstance> floatingTexts = new List<TotemFloatingTextInstance>(16);
    private TotemAssetService assetService;
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemCameraService cameraService;
    private GameObject transientRoot;
    private GameObject vignetteRoot;
    private Image vignetteImage;
    private int spawnedCount;
    private int projectileSpawnedCount;
    private int spriteRequestCount;
    private int spriteMissingCount;
    private int cameraShakeRequestCount;
    private int cameraShakeSkippedCount;
    private int vignettePulseCount;
    private int floatingTextSpawnedCount;
    private float lastCameraShakeAmplitude;
    private float lastCameraShakeDuration;
    private float vignetteIntensity;
    private float vignettePulseTimer;
    private float playerHealthRatio = 1f;
    private bool vignettePulsing;
    private bool lastFloatingTextStrong;
    private string lastAssetKey = string.Empty;
    private string lastMissingAssetKey = string.Empty;
    private string lastProjectileId = string.Empty;
    private string lastFloatingText = string.Empty;

    public override string ServiceName => "VFX";

    public int ActiveCount => instances.Count;

    public int SpawnedCount => spawnedCount;

    public int ProjectileSpawnedCount => projectileSpawnedCount;

    public string LastAssetKey => lastAssetKey;

    public string LastProjectileId => lastProjectileId;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        assetService = runtime.GetService<TotemAssetService>();
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        cameraService = runtime.GetService<TotemCameraService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        if (actorService != null)
        {
            actorService.DamageResolved += OnDamageResolved;
        }
    }

    protected override void OnShutdown()
    {
        if (actorService != null)
        {
            actorService.DamageResolved -= OnDamageResolved;
        }

        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        ClearRunVisuals(resetCounters: true);
        assetService = null;
        actorService = null;
        cameraService = null;
    }

    public void Tick(float deltaTime)
    {
        RefreshPlayerDangerFeedback();
        UpdateVignette(deltaTime);
        UpdateFloatingTexts(deltaTime);

        if (deltaTime <= 0f || instances.Count <= 0)
        {
            ReleaseTransientRootIfEmpty();
            return;
        }

        for (int i = instances.Count - 1; i >= 0; i--)
        {
            var instance = instances[i];
            if (instance.GameObject == null)
            {
                instances.RemoveAt(i);
                continue;
            }

            instance.Remaining -= deltaTime;
            ApplyFade(instance);
            if (instance.Remaining <= 0f)
            {
                DestroyObject(instance.GameObject);
                instances.RemoveAt(i);
            }
        }

        ReleaseTransientRootIfEmpty();
    }

    public TotemVfxSnapshot CaptureSnapshot()
    {
        return new TotemVfxSnapshot
        {
            activeCount = instances.Count,
            spawnedCount = spawnedCount,
            projectileSpawnedCount = projectileSpawnedCount,
            spriteRequestCount = spriteRequestCount,
            spriteMissingCount = spriteMissingCount,
            lastAssetKey = lastAssetKey,
            lastMissingAssetKey = lastMissingAssetKey,
            lastProjectileId = lastProjectileId,
            cameraShakeRequestCount = cameraShakeRequestCount,
            cameraShakeSkippedCount = cameraShakeSkippedCount,
            lastCameraShakeAmplitude = lastCameraShakeAmplitude,
            lastCameraShakeDuration = lastCameraShakeDuration,
            vignettePulsing = vignettePulsing,
            vignetteOverlayActive = vignetteRoot != null,
            vignettePulseCount = vignettePulseCount,
            vignetteIntensity = vignetteIntensity,
            playerHealthRatio = playerHealthRatio,
            floatingTextActiveCount = floatingTexts.Count,
            floatingTextSpawnedCount = floatingTextSpawnedCount,
            lastFloatingText = lastFloatingText,
            lastFloatingTextStrong = lastFloatingTextStrong,
        };
    }

    public bool SpawnAttackHit(Vector3 position, string weaponId, bool charged)
    {
        string assetKey = ResolveAttackHitKey(weaponId);
        float scale = charged ? 1.15f : 0.85f;
        return SpawnSprite(assetKey, position + Vector3.up * 0.16f, scale, AttackHitLifetime, Color.white);
    }

    public bool SpawnSkillBurst(Vector3 position, string skillId, float radius)
    {
        string assetKey = ResolveSkillEffectKey(skillId);
        float scale = Mathf.Clamp(radius * 0.28f, 1.2f, 3.2f);
        return SpawnSprite(assetKey, position + Vector3.up * 0.18f, scale, SkillBurstLifetime, Color.white);
    }

    public bool SpawnProjectileTrail(Vector3 start, Vector3 target, TotemProjectileDefinition projectile, bool isPlayer, bool charged)
    {
        if (projectile == null || string.IsNullOrWhiteSpace(projectile.ProjectileId))
        {
            return false;
        }

        Vector3 midpoint = Vector3.Lerp(start, target, 0.5f) + Vector3.up * 0.22f;
        float scale = Mathf.Clamp(projectile.AoeRadius > 0f ? projectile.AoeRadius : 0.35f, 0.25f, 1.5f);
        float lifetime = Mathf.Clamp(projectile.MaxRange / Mathf.Max(1f, projectile.Speed), 0.15f, 0.9f);
        Color tint = isPlayer
            ? (charged ? new Color(1f, 0.75f, 0.2f, 1f) : new Color(0.35f, 0.85f, 1f, 1f))
            : new Color(1f, 0.2f, 0.2f, 1f);

        bool spawned = SpawnSprite(ResolveProjectileEffectKey(projectile.ProjectileId), midpoint, scale, lifetime, tint);
        if (!spawned)
        {
            return false;
        }

        projectileSpawnedCount++;
        lastProjectileId = projectile.ProjectileId;
        GFTrace.Info("TotemVFX", "Projectile.Spawned", null, GFTrace.Data(
            "projectileId", projectile.ProjectileId,
            "speed", projectile.Speed.ToString("F1"),
            "maxRange", projectile.MaxRange.ToString("F1")));
        return true;
    }

    public bool RequestCombatFeedback(TotemDamageRecord record)
    {
        if (record.Target == null)
        {
            return false;
        }

        bool requestedShake = false;
        if (ShouldShake(record))
        {
            requestedShake = RequestCameraShake(record);
        }

        if (record.Target.Kind == TotemActorKind.Player)
        {
            UpdateDangerFeedback(record.Target);
        }

        if (ShouldSpawnDamageFloat(record))
        {
            SpawnDamageFloat(record);
        }

        return requestedShake;
    }

    public static string ResolveAttackHitKey(string weaponId)
    {
        return "effect.attack.hit";
    }

    public static string ResolveSkillEffectKey(string skillId)
    {
        return string.Equals(skillId, "skill_beam", StringComparison.Ordinal)
            ? "effect.boss.bolt"
            : "effect.skill.burst";
    }

    public static string ResolveProjectileEffectKey(string projectileId)
    {
        if (string.Equals(projectileId, "bullet_pistol", StringComparison.Ordinal))
        {
            return "effect.projectile.bullet_pistol";
        }

        if (string.Equals(projectileId, "arrow_bow", StringComparison.Ordinal))
        {
            return "effect.projectile.arrow_bow";
        }

        return "effect.attack.hit";
    }

    private bool SpawnSprite(string assetKey, Vector3 position, float scale, float lifetime, Color tint)
    {
        spriteRequestCount++;
        if (assetService == null || !assetService.TryLoadSprite(assetKey, out var sprite) || sprite == null)
        {
            spriteMissingCount++;
            lastMissingAssetKey = assetKey ?? string.Empty;
            GFTrace.Warning("TotemVFX", "Sprite.Missing", null, GFTrace.Data("assetKey", assetKey));
            return false;
        }

        var go = new GameObject($"TotemVFX_{assetKey}");
        go.transform.SetParent(EnsureTransientRoot(), false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = 120;

        instances.Add(new TotemVfxInstance
        {
            GameObject = go,
            Renderer = renderer,
            Duration = Mathf.Max(0.01f, lifetime),
            Remaining = Mathf.Max(0.01f, lifetime),
            Tint = tint,
        });

        spawnedCount++;
        lastAssetKey = assetKey;
        GFTrace.Info("TotemVFX", "Sprite.Spawned", null, GFTrace.Data(
            "assetKey", assetKey,
            "position", $"{position.x:F1},{position.y:F1},{position.z:F1}",
            "scale", scale.ToString("F2")));
        return true;
    }

    private void OnDamageResolved(TotemDamageRecord record)
    {
        RequestCombatFeedback(record);
    }

    private bool SpawnDamageFloat(TotemDamageRecord record)
    {
        if (record.Target == null || record.Amount <= 0f)
        {
            return false;
        }

        bool strong = IsStrongHit(record);
        string text = Mathf.RoundToInt(record.Amount).ToString();
        Vector3 position = record.Target.Position + Vector3.up * (record.Target.Kind == TotemActorKind.Boss ? 2.2f : 1.35f);
        position.x += Mathf.Sin((record.Sequence + 1) * 12.9898f) * 0.22f;

        var go = new GameObject($"TotemDamageFloat_{text}");
        go.transform.SetParent(EnsureTransientRoot(), false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(TotemActorBillboard.ResolveCameraTiltX(), 0f, 0f);

        var textMesh = go.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = strong ? 48 : 34;
        textMesh.characterSize = strong ? 0.10f : 0.085f;
        textMesh.color = strong ? new Color(1f, 0.13f, 0.13f, 1f) : Color.white;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 240;
        }

        floatingTexts.Add(new TotemFloatingTextInstance
        {
            GameObject = go,
            TextMesh = textMesh,
            StartPosition = position,
            Duration = DamageFloatLifetime,
            Remaining = DamageFloatLifetime,
            Rise = strong ? StrongDamageFloatRise : DamageFloatRise,
            BaseColor = textMesh.color,
        });

        floatingTextSpawnedCount++;
        lastFloatingText = text;
        lastFloatingTextStrong = strong;
        GFTrace.Info("TotemVFX", "DamageFloat.Spawned", null, GFTrace.Data(
            "text", text,
            "strong", strong.ToString(),
            "target", record.Target.Name));
        return true;
    }

    private bool RequestCameraShake(TotemDamageRecord record)
    {
        bool strong = IsStrongHit(record);
        float amplitude = strong ? StrongShakeAmplitude : NormalShakeAmplitude;
        float duration = strong ? StrongShakeDuration : NormalShakeDuration;
        lastCameraShakeAmplitude = amplitude;
        lastCameraShakeDuration = duration;

        if (cameraService == null || !cameraService.RequestShake(amplitude, duration))
        {
            cameraShakeSkippedCount++;
            GFTrace.Warning("TotemVFX", "CameraShake.Skipped", null, GFTrace.Data(
                "reason", record.Reason ?? string.Empty,
                "target", record.Target?.Name ?? string.Empty));
            return false;
        }

        cameraShakeRequestCount++;
        GFTrace.Info("TotemVFX", "CameraShake.Requested", null, GFTrace.Data(
            "reason", record.Reason ?? string.Empty,
            "amplitude", amplitude.ToString("F3"),
            "duration", duration.ToString("F3")));
        return true;
    }

    private void RefreshPlayerDangerFeedback()
    {
        if (actorService?.Player == null)
        {
            return;
        }

        UpdateDangerFeedback(actorService.Player);
    }

    private void UpdateDangerFeedback(TotemActorModel player)
    {
        if (player == null || player.MaxHealth <= 0f)
        {
            return;
        }

        playerHealthRatio = Mathf.Clamp01(player.Health / player.MaxHealth);
        if (playerHealthRatio > 0f && playerHealthRatio < DangerHealthRatio)
        {
            StartVignettePulse();
        }
        else
        {
            StopVignettePulse();
        }
    }

    private void StartVignettePulse()
    {
        EnsureVignetteOverlay();
        if (vignettePulsing)
        {
            return;
        }

        vignettePulsing = true;
        vignettePulseTimer = 0f;
        vignettePulseCount++;
        GFTrace.Info("TotemVFX", "Vignette.Start", null, GFTrace.Data("healthRatio", playerHealthRatio.ToString("F2")));
    }

    private void StopVignettePulse()
    {
        if (!vignettePulsing)
        {
            return;
        }

        vignettePulsing = false;
        GFTrace.Info("TotemVFX", "Vignette.Stop", null, GFTrace.Data("healthRatio", playerHealthRatio.ToString("F2")));
    }

    private void UpdateVignette(float deltaTime)
    {
        float dt = Mathf.Max(0f, deltaTime);
        if (vignettePulsing)
        {
            EnsureVignetteOverlay();
            vignettePulseTimer += dt;
            float wave = 0.5f + 0.5f * Mathf.Sin((vignettePulseTimer / VignettePulseDuration) * Mathf.PI * 2f - Mathf.PI * 0.5f);
            vignetteIntensity = Mathf.Lerp(0.12f, VignetteIntensityMax, wave);
        }
        else if (vignetteIntensity > 0f)
        {
            vignetteIntensity = Mathf.MoveTowards(vignetteIntensity, 0f, VignetteFadeOutSpeed * dt);
        }

        ApplyVignetteColor();
    }

    private void EnsureVignetteOverlay()
    {
        if (vignetteRoot != null && vignetteImage != null)
        {
            return;
        }

        vignetteRoot = new GameObject("[TotemVFXVignette]");
        var canvas = vignetteRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;

        var imageGo = new GameObject("Vignette");
        imageGo.transform.SetParent(vignetteRoot.transform, false);
        vignetteImage = imageGo.AddComponent<Image>();
        vignetteImage.raycastTarget = false;

        var rect = imageGo.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        ApplyVignetteColor();
    }

    private void ApplyVignetteColor()
    {
        if (vignetteImage == null)
        {
            return;
        }

        vignetteImage.color = new Color(0.8f, 0.05f, 0.05f, Mathf.Clamp01(vignetteIntensity));
    }

    private void UpdateFloatingTexts(float deltaTime)
    {
        if (deltaTime <= 0f || floatingTexts.Count <= 0)
        {
            ReleaseTransientRootIfEmpty();
            return;
        }

        for (int i = floatingTexts.Count - 1; i >= 0; i--)
        {
            var instance = floatingTexts[i];
            if (instance.GameObject == null || instance.TextMesh == null)
            {
                floatingTexts.RemoveAt(i);
                continue;
            }

            instance.Remaining = Mathf.Max(0f, instance.Remaining - deltaTime);
            float progress = 1f - Mathf.Clamp01(instance.Remaining / instance.Duration);
            instance.GameObject.transform.position = instance.StartPosition + Vector3.up * (instance.Rise * progress);

            Color color = instance.BaseColor;
            color.a = Mathf.Clamp01(instance.Remaining / instance.Duration);
            instance.TextMesh.color = color;

            if (instance.Remaining <= 0f)
            {
                DestroyObject(instance.GameObject);
                floatingTexts.RemoveAt(i);
            }
        }

        ReleaseTransientRootIfEmpty();
    }

    private void DestroyAll()
    {
        if (transientRoot != null)
        {
            DestroyObject(transientRoot);
            transientRoot = null;
        }
        else
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                DestroyObject(instances[i].GameObject);
            }

            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                DestroyObject(floatingTexts[i].GameObject);
            }
        }

        instances.Clear();
        floatingTexts.Clear();
    }

    private Transform EnsureTransientRoot()
    {
        if (transientRoot != null)
        {
            return transientRoot.transform;
        }

        transientRoot = new GameObject("[TotemVFX]");
        return transientRoot.transform;
    }

    private void ReleaseTransientRootIfEmpty()
    {
        if (transientRoot == null || instances.Count > 0 || floatingTexts.Count > 0 || transientRoot.transform.childCount > 0)
        {
            return;
        }

        DestroyObject(transientRoot);
        transientRoot = null;
    }

    private void DestroyVignetteOverlay()
    {
        if (vignetteRoot != null)
        {
            DestroyObject(vignetteRoot);
        }

        vignetteRoot = null;
        vignetteImage = null;
    }

    private void ClearRunVisuals(bool resetCounters)
    {
        DestroyAll();
        DestroyVignetteOverlay();
        vignetteIntensity = 0f;
        vignettePulseTimer = 0f;
        playerHealthRatio = 1f;
        vignettePulsing = false;
        lastFloatingTextStrong = false;
        if (resetCounters)
        {
            spawnedCount = 0;
            projectileSpawnedCount = 0;
            spriteRequestCount = 0;
            spriteMissingCount = 0;
            cameraShakeRequestCount = 0;
            cameraShakeSkippedCount = 0;
            vignettePulseCount = 0;
            floatingTextSpawnedCount = 0;
            lastCameraShakeAmplitude = 0f;
            lastCameraShakeDuration = 0f;
            lastAssetKey = string.Empty;
            lastMissingAssetKey = string.Empty;
            lastProjectileId = string.Empty;
            lastFloatingText = string.Empty;
        }
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ClearRunVisuals(resetCounters: true);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ClearRunVisuals(resetCounters: true);
            GFTrace.Info("TotemVFX", "Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private static void ApplyFade(TotemVfxInstance instance)
    {
        if (instance.Renderer == null)
        {
            return;
        }

        Color color = instance.Tint;
        color.a *= Mathf.Clamp01(instance.Remaining / instance.Duration);
        instance.Renderer.color = color;
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static bool ShouldShake(TotemDamageRecord record)
    {
        string reason = record.Reason ?? string.Empty;
        if (reason.StartsWith("Status:", StringComparison.Ordinal) || string.Equals(reason, "ShrinkZone", StringComparison.Ordinal))
        {
            return false;
        }

        return record.Target?.Kind == TotemActorKind.Player
            || record.Source?.Kind == TotemActorKind.Player
            || reason.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
            || reason.IndexOf("Skill", StringComparison.OrdinalIgnoreCase) >= 0
            || reason.IndexOf("Tattoo", StringComparison.OrdinalIgnoreCase) >= 0
            || reason.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ShouldSpawnDamageFloat(TotemDamageRecord record)
    {
        string reason = record.Reason ?? string.Empty;
        if (reason.StartsWith("Status:", StringComparison.Ordinal) || string.Equals(reason, "ShrinkZone", StringComparison.Ordinal))
        {
            return false;
        }

        return record.Amount > 0f && record.Target != null;
    }

    private static bool IsStrongHit(TotemDamageRecord record)
    {
        string reason = record.Reason ?? string.Empty;
        return record.Killed
            || record.Amount >= 45f
            || reason.IndexOf("Charged", StringComparison.OrdinalIgnoreCase) >= 0
            || reason.IndexOf("Crit", StringComparison.OrdinalIgnoreCase) >= 0
            || reason.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private sealed class TotemVfxInstance
    {
        public GameObject GameObject;
        public SpriteRenderer Renderer;
        public float Duration;
        public float Remaining;
        public Color Tint;
    }

    private sealed class TotemFloatingTextInstance
    {
        public GameObject GameObject;
        public TextMesh TextMesh;
        public Vector3 StartPosition;
        public float Duration;
        public float Remaining;
        public float Rise;
        public Color BaseColor;
    }
}
