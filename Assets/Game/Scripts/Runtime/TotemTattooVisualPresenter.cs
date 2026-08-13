using System;
using UnityEngine;

[Serializable]
public struct TotemTattooVisualPlacement
{
    public Vector2 offset;
    public float scale;

    public static TotemTattooVisualPlacement Default => new TotemTattooVisualPlacement
    {
        offset = new Vector2(0.5f, 0.5f),
        scale = 1f,
    };
}

[Serializable]
public struct TotemTattooPartVisualPlacement
{
    [Range(1, TotemFirstPlayableTattooBuildState.SlotCount)] public int partId;
    public TotemTattooVisualPlacement placement;
}

/// <summary>
/// Player-only visual bridge for runtime tattoos. It updates a reused MaterialPropertyBlock
/// only when the animated Sprite or equipped tattoo summary changes.
/// </summary>
[DisallowMultipleComponent]
public sealed class TotemTattooVisualPresenter : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BodyTexId = Shader.PropertyToID("_BodyTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TattooMapId = Shader.PropertyToID("_TattooMap");
    private static readonly int TattooPatternAtlasId = Shader.PropertyToID("_TattooPatternAtlas");
    private static readonly int TattooPart1Id = Shader.PropertyToID("_TattooPart1");
    private static readonly int TattooPart2Id = Shader.PropertyToID("_TattooPart2");
    private static readonly int TattooPart3Id = Shader.PropertyToID("_TattooPart3");
    private static readonly int TattooPart4Id = Shader.PropertyToID("_TattooPart4");
    private static readonly int TattooPart5Id = Shader.PropertyToID("_TattooPart5");
    private static readonly int TattooPart6Id = Shader.PropertyToID("_TattooPart6");
    private static readonly int TattooTransform1Id = Shader.PropertyToID("_TattooTransform1");
    private static readonly int TattooTransform2Id = Shader.PropertyToID("_TattooTransform2");
    private static readonly int TattooTransform3Id = Shader.PropertyToID("_TattooTransform3");
    private static readonly int TattooTransform4Id = Shader.PropertyToID("_TattooTransform4");
    private static readonly int TattooTransform5Id = Shader.PropertyToID("_TattooTransform5");
    private static readonly int TattooTransform6Id = Shader.PropertyToID("_TattooTransform6");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material tattooMaterial;
    [SerializeField] private Texture2D tattooPatternAtlas;
    [SerializeField] private TotemTattooFrameMapSet frameMapSet;
    [SerializeField] private TotemTattooVisualPlacement defaultPlacement = new TotemTattooVisualPlacement
    {
        offset = new Vector2(0.5f, 0.5f),
        scale = 1f,
    };
    [SerializeField] private TotemTattooPartVisualPlacement[] partPlacements = Array.Empty<TotemTattooPartVisualPlacement>();

    private readonly Vector4[] partDescriptors = new Vector4[TotemFirstPlayableTattooBuildState.SlotCount];
    private readonly Vector4[] partTransforms = new Vector4[TotemFirstPlayableTattooBuildState.SlotCount];
    private MaterialPropertyBlock propertyBlock;
    private Sprite lastSprite;
    private int lastVisualHash = int.MinValue;
    private bool initialized;

    void Awake()
    {
        EnsurePartPlacementSlots();
        Initialize();
    }

    void OnEnable()
    {
        EnsurePartPlacementSlots();
        Initialize();
        lastSprite = null;
        lastVisualHash = int.MinValue;
    }

    /// <summary>
    /// Future UI/save integration point. No player-facing control calls this in the current release.
    /// Offset is normalized inside the already approved body-part region; scale must stay positive.
    /// </summary>
    public bool SetPartPlacement(int partId, Vector2 offset, float scale)
    {
        if (partId < 1 || partId > TotemFirstPlayableTattooBuildState.SlotCount)
        {
            return false;
        }

        EnsurePartPlacementSlots();
        for (int index = 0; index < partPlacements.Length; index++)
        {
            if (partPlacements[index].partId != partId)
            {
                continue;
            }

            var value = partPlacements[index];
            value.placement.offset = new Vector2(Mathf.Clamp01(offset.x), Mathf.Clamp01(offset.y));
            value.placement.scale = Mathf.Max(0.01f, scale);
            partPlacements[index] = value;
            lastVisualHash = int.MinValue;
            return true;
        }

        return false;
    }

    public TotemTattooVisualPlacement GetPartPlacement(int partId)
    {
        return ResolvePlacement(partId);
    }

    void LateUpdate()
    {
        if (!Initialize())
        {
            return;
        }

        TotemGameRuntime runtime = TotemGameRuntime.Instance;
        TotemActorModel player = runtime?.GetService<TotemActorService>()?.Player;
        TotemFirstPlayableTattooBuildState buildState = runtime
            ?.GetService<TotemFirstPlayableTattooBuildService>()
            ?.GetOrCreateState(player);
        Sprite currentSprite = spriteRenderer.sprite;
        int visualHash = ComputeVisualHash(buildState);
        if (currentSprite == lastSprite && visualHash == lastVisualHash)
        {
            return;
        }

        ApplyVisual(currentSprite, buildState);
        lastSprite = currentSprite;
        lastVisualHash = visualHash;
    }

    private bool Initialize()
    {
        if (initialized)
        {
            return spriteRenderer != null && propertyBlock != null;
        }

        spriteRenderer = spriteRenderer == null ? GetComponent<SpriteRenderer>() : spriteRenderer;
        if (spriteRenderer == null || tattooMaterial == null || frameMapSet == null || tattooPatternAtlas == null)
        {
            return false;
        }

        spriteRenderer.sharedMaterial = tattooMaterial;
        propertyBlock = new MaterialPropertyBlock();
        initialized = true;
        return true;
    }

    private void ApplyVisual(Sprite currentSprite, TotemFirstPlayableTattooBuildState buildState)
    {
        // 当前角色帧未必有纹身遮罩；空纹理不能传给 MaterialPropertyBlock。
        // 清空旧属性后必须重新写入当前 Sprite 的贴图；否则会采样纹身材质的默认白图。
        propertyBlock.Clear();
        if (currentSprite != null && currentSprite.texture != null)
        {
            propertyBlock.SetTexture(MainTexId, currentSprite.texture);
            propertyBlock.SetTexture(BodyTexId, currentSprite.texture);
        }

        propertyBlock.SetColor(ColorId, spriteRenderer.color);
        propertyBlock.SetTexture(TattooPatternAtlasId, tattooPatternAtlas);
        if (frameMapSet.TryGetTattooMap(currentSprite, out Texture2D tattooMap) && tattooMap != null)
        {
            propertyBlock.SetTexture(TattooMapId, tattooMap);
        }

        FillDescriptors(buildState);
        propertyBlock.SetVector(TattooPart1Id, partDescriptors[0]);
        propertyBlock.SetVector(TattooPart2Id, partDescriptors[1]);
        propertyBlock.SetVector(TattooPart3Id, partDescriptors[2]);
        propertyBlock.SetVector(TattooPart4Id, partDescriptors[3]);
        propertyBlock.SetVector(TattooPart5Id, partDescriptors[4]);
        propertyBlock.SetVector(TattooPart6Id, partDescriptors[5]);
        propertyBlock.SetVector(TattooTransform1Id, partTransforms[0]);
        propertyBlock.SetVector(TattooTransform2Id, partTransforms[1]);
        propertyBlock.SetVector(TattooTransform3Id, partTransforms[2]);
        propertyBlock.SetVector(TattooTransform4Id, partTransforms[3]);
        propertyBlock.SetVector(TattooTransform5Id, partTransforms[4]);
        propertyBlock.SetVector(TattooTransform6Id, partTransforms[5]);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void FillDescriptors(TotemFirstPlayableTattooBuildState buildState)
    {
        for (int index = 0; index < TotemFirstPlayableTattooBuildState.SlotCount; index++)
        {
            partDescriptors[index] = Vector4.zero;
            TotemTattooVisualPlacement placement = ResolvePlacement(index + 1);
            partTransforms[index] = new Vector4(placement.offset.x, placement.offset.y, placement.scale, 0f);
        }

        for (int index = 0; buildState != null && index < TotemFirstPlayableTattooBuildState.SlotCount; index++)
        {
            TotemTattooLoadoutEntry definition = buildState.GetSlot((TotemTattooSlotId)index);
            int partIndex = (int)definition.Slot;
            if (!definition.IsEquipped || partIndex < 0 || partIndex >= TotemFirstPlayableTattooBuildState.SlotCount)
            {
                continue;
            }

            Color color = ResolveColor(definition.Element);
            partDescriptors[partIndex] = new Vector4(color.r, color.g, color.b, (int)definition.Pattern);
        }
    }

    private TotemTattooVisualPlacement ResolvePlacement(int partId)
    {
        TotemTattooVisualPlacement fallback = NormalizePlacement(defaultPlacement);
        if (partPlacements == null)
        {
            return fallback;
        }

        for (int index = 0; index < partPlacements.Length; index++)
        {
            if (partPlacements[index].partId == partId)
            {
                return NormalizePlacement(partPlacements[index].placement);
            }
        }

        return fallback;
    }

    /// <summary>
    /// Keeps the future public placement API independent of prefab authoring order. This runs
    /// during initialization or an explicit placement write, never from LateUpdate.
    /// </summary>
    private void EnsurePartPlacementSlots()
    {
        if (HasCompletePartPlacementSlots())
        {
            return;
        }

        var normalized = new TotemTattooPartVisualPlacement[TotemFirstPlayableTattooBuildState.SlotCount];
        var assigned = new bool[TotemFirstPlayableTattooBuildState.SlotCount];
        for (int partId = 1; partId <= TotemFirstPlayableTattooBuildState.SlotCount; partId++)
        {
            normalized[partId - 1] = new TotemTattooPartVisualPlacement
            {
                partId = partId,
                placement = NormalizePlacement(defaultPlacement),
            };
        }

        for (int index = 0; partPlacements != null && index < partPlacements.Length; index++)
        {
            TotemTattooPartVisualPlacement existing = partPlacements[index];
            int partIndex = existing.partId - 1;
            if (partIndex < 0 || partIndex >= TotemFirstPlayableTattooBuildState.SlotCount || assigned[partIndex])
            {
                continue;
            }

            existing.placement = NormalizePlacement(existing.placement);
            normalized[partIndex] = existing;
            assigned[partIndex] = true;
        }

        partPlacements = normalized;
    }

    private bool HasCompletePartPlacementSlots()
    {
        if (partPlacements == null || partPlacements.Length != TotemFirstPlayableTattooBuildState.SlotCount)
        {
            return false;
        }

        for (int partId = 1; partId <= TotemFirstPlayableTattooBuildState.SlotCount; partId++)
        {
            bool found = false;
            for (int index = 0; index < partPlacements.Length; index++)
            {
                if (partPlacements[index].partId == partId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static TotemTattooVisualPlacement NormalizePlacement(TotemTattooVisualPlacement value)
    {
        value.offset = new Vector2(Mathf.Clamp01(value.offset.x), Mathf.Clamp01(value.offset.y));
        value.scale = Mathf.Max(0.01f, value.scale);
        return value;
    }

    private static int ComputeVisualHash(TotemFirstPlayableTattooBuildState buildState)
    {
        unchecked
        {
            int hash = 17;
            for (int index = 0; buildState != null && index < TotemFirstPlayableTattooBuildState.SlotCount; index++)
            {
                TotemTattooLoadoutEntry definition = buildState.GetSlot((TotemTattooSlotId)index);
                if (!definition.IsEquipped)
                {
                    continue;
                }

                hash = hash * 31 + (int)definition.Slot;
                hash = hash * 31 + (int)definition.Element;
                hash = hash * 31 + (int)definition.Pattern;
            }

            return hash;
        }
    }

    private static Color ResolveColor(TotemFirstPlayableElement element)
    {
        switch (element)
        {
            case TotemFirstPlayableElement.Fire: return new Color32(0xC9, 0x3D, 0x38, 0xFF);
            case TotemFirstPlayableElement.Ice: return new Color32(0x63, 0xB8, 0xD9, 0xFF);
            case TotemFirstPlayableElement.Lightning: return new Color32(0xD4, 0xA6, 0x2B, 0xFF);
            default: return Color.white;
        }
    }
}
