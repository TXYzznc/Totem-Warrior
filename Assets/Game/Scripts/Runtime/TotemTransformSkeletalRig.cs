using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stable body-part identifiers shared by the skeletal character preview and a future tattoo visual layer.
/// These identifiers are deliberately visual-only; they do not alter tattoo gameplay data or effects.
/// </summary>
public enum TotemSkeletalBodyPart
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
}

/// <summary>
/// A fixed local area on a body bone that is allowed to receive a tattoo decal.
/// </summary>
[DisallowMultipleComponent]
public sealed class TotemSkeletalTattooAnchor : MonoBehaviour
{
    [SerializeField] private TotemSkeletalBodyPart bodyPart;
    [SerializeField] private Rect localMaskBounds = new Rect(-0.25f, -0.25f, 0.5f, 0.5f);
    [SerializeField] private Vector2 defaultOffset = new Vector2(0.5f, 0.5f);
    [SerializeField, Min(0.01f)] private float defaultScale = 1f;

    public TotemSkeletalBodyPart BodyPart => bodyPart;
    public Rect LocalMaskBounds => localMaskBounds;
    public Vector2 DefaultOffset => defaultOffset;
    public float DefaultScale => defaultScale;

    public void Configure(TotemSkeletalBodyPart value, Rect bounds, Vector2 offset, float scale)
    {
        bodyPart = value;
        localMaskBounds = bounds;
        defaultOffset = offset;
        defaultScale = Mathf.Max(0.01f, scale);
    }
}

/// <summary>
/// Runtime lookup for a Transform-cutout character rig. It owns no gameplay state and allocates only once
/// while building its anchor lookup.
/// </summary>
[DisallowMultipleComponent]
public sealed class TotemTransformSkeletalRig : MonoBehaviour
{
    [SerializeField] private Transform rigRoot;
    [SerializeField] private TotemSkeletalTattooAnchor[] tattooAnchors = Array.Empty<TotemSkeletalTattooAnchor>();

    private Dictionary<TotemSkeletalBodyPart, TotemSkeletalTattooAnchor> anchorsByPart;

    public Transform RigRoot => rigRoot == null ? transform : rigRoot;

    void Awake()
    {
        EnsureAnchorLookup();
    }

    public bool TryGetTattooAnchor(TotemSkeletalBodyPart part, out TotemSkeletalTattooAnchor anchor)
    {
        EnsureAnchorLookup();
        return anchorsByPart.TryGetValue(part, out anchor) && anchor != null;
    }

    public void Configure(Transform value, TotemSkeletalTattooAnchor[] anchors)
    {
        rigRoot = value == null ? transform : value;
        tattooAnchors = anchors ?? Array.Empty<TotemSkeletalTattooAnchor>();
        anchorsByPart = null;
    }

    private void EnsureAnchorLookup()
    {
        if (anchorsByPart != null)
        {
            return;
        }

        anchorsByPart = new Dictionary<TotemSkeletalBodyPart, TotemSkeletalTattooAnchor>();
        for (int index = 0; index < tattooAnchors.Length; index++)
        {
            TotemSkeletalTattooAnchor anchor = tattooAnchors[index];
            if (anchor != null)
            {
                anchorsByPart[anchor.BodyPart] = anchor;
            }
        }
    }
}
