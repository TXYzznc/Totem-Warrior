using UnityEngine;

public sealed class TotemCameraService : TotemRuntimeServiceBase, ITotemRuntimeLateTickService
{
    private const float CameraTiltX = 55f;
    private const float OrthographicSize = 9f;
    private const float SmoothTime = 0.15f;
    private const float BoundaryMargin = 10f;
    private const float ShakeFrequency = 72f;

    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemMapService mapService;
    private Camera mainCamera;
    private Vector3 basePosition;
    private Vector3 lastFocusPosition;
    private Vector3 lastRawFocusPosition;
    private Vector3 followVelocity;
    private Vector3 lastShakeOffset;
    private float cameraDistance;
    private float shakeRemainingSec;
    private float shakeDurationSec;
    private float shakeAmplitude;
    private float lastShakeAmplitude;
    private float lastShakeDuration;
    private int shakeRequestCount;
    private int shakeSkippedCount;
    private int focusClampCount;
    private bool lastFocusClamped;
    private bool following;

    public override string ServiceName => "Camera";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        mapService = runtime.GetService<TotemMapService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        following = false;
        mainCamera = null;
        followVelocity = Vector3.zero;
        basePosition = Vector3.zero;
        lastFocusPosition = Vector3.zero;
        lastRawFocusPosition = Vector3.zero;
        lastShakeOffset = Vector3.zero;
        shakeRemainingSec = 0f;
        shakeDurationSec = 0f;
        shakeAmplitude = 0f;
        lastShakeAmplitude = 0f;
        lastShakeDuration = 0f;
        shakeRequestCount = 0;
        shakeSkippedCount = 0;
        focusClampCount = 0;
        lastFocusClamped = false;
    }

    public void LateTick(float deltaTime)
    {
        bool hasShake = shakeRemainingSec > 0f;
        if ((!following || actorService?.Player == null) && !hasShake)
        {
            return;
        }

        EnsureCamera();
        if (mainCamera == null)
        {
            if (hasShake)
            {
                shakeSkippedCount++;
            }

            return;
        }

        if (following && actorService?.Player != null)
        {
            Vector3 focus = actorService.Player.Position;
            focus.y = 0f;
            lastRawFocusPosition = focus;
            lastFocusClamped = ClampFocus(ref focus);
            if (lastFocusClamped)
            {
                focusClampCount++;
            }
            lastFocusPosition = focus;
            Vector3 targetPosition = FocusToCamera(focus);
            basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref followVelocity, SmoothTime);
        }

        mainCamera.transform.position = basePosition + AdvanceShake(deltaTime);
    }

    public void ActivateCombatCamera()
    {
        EnsureCamera();
        ConfigureCamera();
        var player = actorService?.Player;
        Vector3 focus = player != null ? player.Position : new Vector3(37.5f, 0f, 37.5f);
        focus.y = 0f;
        lastRawFocusPosition = focus;
        lastFocusClamped = ClampFocus(ref focus);
        if (lastFocusClamped)
        {
            focusClampCount++;
        }
        lastFocusPosition = focus;
        basePosition = FocusToCamera(focus);
        followVelocity = Vector3.zero;
        mainCamera.transform.position = basePosition;
        following = true;
        GFTrace.Success("TotemCamera", "CombatCamera.Activated", null, GFTrace.Data(
            "orthographicSize", OrthographicSize.ToString("F1"),
            "tiltX", CameraTiltX.ToString("F1")));
    }

    public void DeactivateCombatCamera()
    {
        following = false;
        followVelocity = Vector3.zero;
        lastShakeOffset = Vector3.zero;
        shakeRemainingSec = 0f;
        shakeDurationSec = 0f;
        shakeAmplitude = 0f;
        GFTrace.Info("TotemCamera", "CombatCamera.Deactivated");
    }

    public bool RequestShake(float amplitude, float durationSec)
    {
        amplitude = Mathf.Max(0f, amplitude);
        durationSec = Mathf.Max(0f, durationSec);
        if (amplitude <= 0f || durationSec <= 0f)
        {
            shakeSkippedCount++;
            return false;
        }

        EnsureCamera();
        if (mainCamera == null)
        {
            shakeSkippedCount++;
            return false;
        }

        if (!following)
        {
            basePosition = mainCamera.transform.position;
        }

        shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
        shakeDurationSec = Mathf.Max(shakeDurationSec, durationSec);
        shakeRemainingSec = Mathf.Max(shakeRemainingSec, durationSec);
        lastShakeAmplitude = amplitude;
        lastShakeDuration = durationSec;
        shakeRequestCount++;
        GFTrace.Info("TotemCamera", "Shake.Requested", null, GFTrace.Data(
            "amplitude", amplitude.ToString("F3"),
            "duration", durationSec.ToString("F3")));
        return true;
    }

    public TotemCameraSnapshot CaptureSnapshot()
    {
        return new TotemCameraSnapshot
        {
            hasCamera = mainCamera != null || Camera.main != null,
            following = following,
            focusPosition = lastFocusPosition,
            rawFocusPosition = lastRawFocusPosition,
            focusClamped = lastFocusClamped,
            focusClampCount = focusClampCount,
            basePosition = basePosition,
            cameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero,
            orthographicSize = mainCamera != null ? mainCamera.orthographicSize : 0f,
            tiltX = mainCamera != null ? mainCamera.transform.eulerAngles.x : 0f,
            cameraDistance = cameraDistance,
            mapSize = mapService?.CurrentMap?.MapSize ?? TotemMapService.DefaultMapSize,
            shakeRequestCount = shakeRequestCount,
            shakeSkippedCount = shakeSkippedCount,
            shakeRemainingSec = shakeRemainingSec,
            lastShakeAmplitude = lastShakeAmplitude,
            lastShakeDuration = lastShakeDuration,
            lastShakeOffset = lastShakeOffset,
        };
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ActivateCombatCamera();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            DeactivateCombatCamera();
        }
    }

    private void EnsureCamera()
    {
        if (mainCamera != null)
        {
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return;
        }

        var go = new GameObject("MainCamera");
        go.tag = "MainCamera";
        mainCamera = go.AddComponent<Camera>();
    }

    private void ConfigureCamera()
    {
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = OrthographicSize;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
        mainCamera.transform.eulerAngles = new Vector3(CameraTiltX, 0f, 0f);

        float tiltRad = CameraTiltX * Mathf.Deg2Rad;
        const float targetHeight = 18f;
        cameraDistance = targetHeight / Mathf.Sin(tiltRad);
    }

    private bool ClampFocus(ref Vector3 focus)
    {
        float mapSize = mapService?.CurrentMap?.MapSize ?? TotemMapService.DefaultMapSize;
        Vector3 before = focus;
        focus.x = Mathf.Clamp(focus.x, BoundaryMargin, mapSize - BoundaryMargin);
        focus.z = Mathf.Clamp(focus.z, BoundaryMargin, mapSize - BoundaryMargin);
        return (focus - before).sqrMagnitude > 0.0001f;
    }

    private Vector3 FocusToCamera(Vector3 focus)
    {
        float tiltRad = CameraTiltX * Mathf.Deg2Rad;
        float sinT = Mathf.Sin(tiltRad);
        float cosT = Mathf.Cos(tiltRad);
        return new Vector3(
            focus.x,
            focus.y + sinT * cameraDistance,
            focus.z - cosT * cameraDistance);
    }

    private Vector3 AdvanceShake(float deltaTime)
    {
        if (shakeRemainingSec <= 0f || shakeDurationSec <= 0f || shakeAmplitude <= 0f)
        {
            lastShakeOffset = Vector3.zero;
            return Vector3.zero;
        }

        shakeRemainingSec = Mathf.Max(0f, shakeRemainingSec - Mathf.Max(0f, deltaTime));
        if (shakeRemainingSec <= 0f)
        {
            shakeAmplitude = 0f;
            shakeDurationSec = 0f;
            lastShakeOffset = Vector3.zero;
            return Vector3.zero;
        }

        float elapsed = shakeDurationSec - shakeRemainingSec;
        float damp = Mathf.Clamp01(shakeRemainingSec / shakeDurationSec);
        lastShakeOffset = new Vector3(
            Mathf.Sin(elapsed * ShakeFrequency) * shakeAmplitude * damp,
            Mathf.Cos(elapsed * (ShakeFrequency * 1.37f)) * shakeAmplitude * 0.25f * damp,
            Mathf.Cos(elapsed * (ShakeFrequency * 0.83f)) * shakeAmplitude * 0.5f * damp);
        return lastShakeOffset;
    }
}
