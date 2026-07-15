using System;
using UnityEngine;

/// <summary>
/// Test-scene-only visualizer for verifying that a Sprite and its persisted TattooMap material
/// advance together. It deliberately has no gameplay-service dependency.
/// </summary>
[DisallowMultipleComponent]
public sealed class TotemTattooRollPreviewLoop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
    [SerializeField] private Material[] frameMaterials = Array.Empty<Material>();
    [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;

    private int displayedFrame = -1;

    void OnEnable()
    {
        displayedFrame = -1;
        ApplyFrame(0);
    }

    void Update()
    {
        int frameCount = Mathf.Min(frames?.Length ?? 0, frameMaterials?.Length ?? 0);
        if (frameCount == 0)
        {
            return;
        }

        int nextFrame = Mathf.FloorToInt(Time.unscaledTime * framesPerSecond) % frameCount;
        ApplyFrame(nextFrame);
    }

    private void ApplyFrame(int frameIndex)
    {
        int frameCount = Mathf.Min(frames?.Length ?? 0, frameMaterials?.Length ?? 0);
        if (spriteRenderer == null || frameIndex < 0 || frameIndex >= frameCount || displayedFrame == frameIndex)
        {
            return;
        }

        spriteRenderer.sprite = frames[frameIndex];
        spriteRenderer.sharedMaterial = frameMaterials[frameIndex];
        displayedFrame = frameIndex;
    }
}
