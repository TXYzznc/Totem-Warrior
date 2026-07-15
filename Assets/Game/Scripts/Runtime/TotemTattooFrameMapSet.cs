using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TotemTattooFrameBinding
{
    public Sprite sprite;
    public Texture2D tattooMap;
}

/// <summary>
/// Serialized Sprite-to-TattooMap bindings for the shared M02 sprite set.
/// A binding is intentionally data-only: the Player presenter owns the runtime lookup.
/// The class intentionally lives in a same-named source file so Unity can create a valid
/// MonoScript reference when persisting the ScriptableObject asset.
/// </summary>
public sealed class TotemTattooFrameMapSet : ScriptableObject
{
    [SerializeField] private TotemTattooFrameBinding[] bindings = Array.Empty<TotemTattooFrameBinding>();

    private Dictionary<Sprite, Texture2D> lookup;

    public int Count => bindings == null ? 0 : bindings.Length;

    public bool TryGetTattooMap(Sprite sprite, out Texture2D tattooMap)
    {
        tattooMap = null;
        EnsureLookup();
        return sprite != null && lookup.TryGetValue(sprite, out tattooMap) && tattooMap != null;
    }

    public void SetBindings(TotemTattooFrameBinding[] value)
    {
        bindings = value ?? Array.Empty<TotemTattooFrameBinding>();
        lookup = null;
    }

    /// <summary>Returns a snapshot for editor generators that need to replace only one direction.</summary>
    public TotemTattooFrameBinding[] GetBindings()
    {
        return bindings == null ? Array.Empty<TotemTattooFrameBinding>() : (TotemTattooFrameBinding[])bindings.Clone();
    }

    private void EnsureLookup()
    {
        if (lookup != null)
        {
            return;
        }

        lookup = new Dictionary<Sprite, Texture2D>(bindings == null ? 0 : bindings.Length);
        if (bindings == null)
        {
            return;
        }

        for (int index = 0; index < bindings.Length; index++)
        {
            TotemTattooFrameBinding binding = bindings[index];
            if (binding.sprite != null && binding.tattooMap != null)
            {
                lookup[binding.sprite] = binding.tattooMap;
            }
        }
    }
}
