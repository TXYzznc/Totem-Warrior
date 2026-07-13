using UnityEngine;

public static class TotemActorVisualHelper
{
    public const string ShadowName = "TotemActorShadow";

    private const int ShadowTextureSize = 64;
    private const float DefaultShadowYOffset = -0.42f;

    private static Sprite shadowSprite;

    public static TotemActorVisualAttachment AttachActorVisuals(GameObject actorObject, TotemActorKind kind)
    {
        var attachment = new TotemActorVisualAttachment();
        if (actorObject == null)
        {
            return attachment;
        }

        attachment.shadowAttached = AttachShadow(actorObject, GetShadowRadius(kind), DefaultShadowYOffset);
        int totalSpriteRendererCount = CountSpriteRenderers(actorObject, includeShadow: true);
        attachment.spriteRendererCount = CountSpriteRenderers(actorObject, includeShadow: false);
        if (totalSpriteRendererCount > 0)
        {
            var sorter = actorObject.GetComponent<TotemActorDepthSorter>() ?? actorObject.AddComponent<TotemActorDepthSorter>();
            sorter.RefreshRenderers();
            sorter.ForceRecalculate();
            attachment.depthSorterAttached = true;
        }

        if (attachment.spriteRendererCount > 0)
        {
            var billboard = actorObject.GetComponent<TotemActorBillboard>() ?? actorObject.AddComponent<TotemActorBillboard>();
            billboard.ApplyTilt(TotemActorBillboard.ResolveCameraTiltX());
            attachment.billboardAttached = true;
        }

        return attachment;
    }

    private static bool AttachShadow(GameObject actorObject, float radius, float yOffset)
    {
        if (actorObject == null || actorObject.transform.Find(ShadowName) != null)
        {
            return false;
        }

        EnsureShadowSprite();
        var shadow = new GameObject(ShadowName);
        shadow.transform.SetParent(actorObject.transform, false);
        shadow.transform.localPosition = new Vector3(0f, yOffset, 0f);
        shadow.transform.localRotation = Quaternion.identity;
        shadow.transform.localScale = Vector3.one * radius * 2f;

        var renderer = shadow.AddComponent<SpriteRenderer>();
        renderer.sprite = shadowSprite;
        renderer.color = new Color(0f, 0f, 0f, 0.38f);
        renderer.sortingOrder = -1;
        return true;
    }

    private static float GetShadowRadius(TotemActorKind kind)
    {
        switch (kind)
        {
            case TotemActorKind.Player:
                return 0.55f;
            default:
                return 0.50f;
        }
    }

    private static int CountSpriteRenderers(GameObject actorObject, bool includeShadow)
    {
        if (actorObject == null)
        {
            return 0;
        }

        var renderers = actorObject.GetComponentsInChildren<SpriteRenderer>(true);
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!includeShadow && renderers[i].transform.name == ShadowName)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static void EnsureShadowSprite()
    {
        if (shadowSprite != null)
        {
            return;
        }

        var texture = new Texture2D(ShadowTextureSize, ShadowTextureSize, TextureFormat.RGBA32, false)
        {
            name = "TotemActorShadowSprite",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };

        var pixels = new Color32[ShadowTextureSize * ShadowTextureSize];
        float center = (ShadowTextureSize - 1) * 0.5f;
        float maxRadius = ShadowTextureSize * 0.5f;
        for (int y = 0; y < ShadowTextureSize; y++)
        {
            for (int x = 0; x < ShadowTextureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / maxRadius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                pixels[y * ShadowTextureSize + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        shadowSprite = Sprite.Create(
            texture,
            new Rect(0, 0, ShadowTextureSize, ShadowTextureSize),
            new Vector2(0.5f, 0.5f),
            ShadowTextureSize);
        shadowSprite.name = "TotemActorShadowSprite";
        shadowSprite.hideFlags = HideFlags.HideAndDontSave;
    }
}

public sealed class TotemActorVisualAttachment
{
    public bool shadowAttached;
    public bool depthSorterAttached;
    public bool billboardAttached;
    public int spriteRendererCount;
}

[DisallowMultipleComponent]
public sealed class TotemActorDepthSorter : MonoBehaviour
{
    private const float SortScale = 100f;
    private const float DirtyEpsSq = 0.0001f;

    public int BaseOffset;

    private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();
    private SpriteRenderer shadowRenderer;
    private Vector3 lastPosition;
    private bool initialized;

    void Awake()
    {
        RefreshRenderers();
    }

    void OnEnable()
    {
        initialized = false;
        ForceRecalculate();
    }

    void Update()
    {
        Vector3 position = transform.position;
        if (initialized && (position - lastPosition).sqrMagnitude < DirtyEpsSq)
        {
            return;
        }

        ForceRecalculate();
    }

    public void RefreshRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var shadow = transform.Find(TotemActorVisualHelper.ShadowName);
        shadowRenderer = shadow == null ? null : shadow.GetComponent<SpriteRenderer>();
    }

    public void ForceRecalculate()
    {
        int order = BaseOffset + Mathf.RoundToInt(-transform.position.z * SortScale);
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].sortingOrder = order;
                }
            }
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.sortingOrder = order - 1;
        }

        lastPosition = transform.position;
        initialized = true;
    }
}

[DisallowMultipleComponent]
public sealed class TotemActorBillboard : MonoBehaviour
{
    private const float DefaultTiltX = 55f;

    void OnEnable()
    {
        ApplyTilt(ResolveCameraTiltX());
    }

    public void ApplyTilt(float tiltX)
    {
        transform.localEulerAngles = new Vector3(tiltX, 0f, 0f);
    }

    public static float ResolveCameraTiltX()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            float tilt = camera.transform.eulerAngles.x;
            if (tilt > 0f && tilt < 90f)
            {
                return tilt;
            }
        }

        return DefaultTiltX;
    }
}
