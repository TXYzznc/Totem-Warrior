using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TotemRuntimeAssetCatalog
{
    public int schemaVersion = 1;
    public string source = string.Empty;
    public TotemRuntimeAssetEntry[] entries = Array.Empty<TotemRuntimeAssetEntry>();
    public void Normalize()
    {
        if (entries == null)
        {
            entries = Array.Empty<TotemRuntimeAssetEntry>();
        }
    }

    public bool TryGetEntry(string key, out TotemRuntimeAssetEntry entry)
    {
        Normalize();
        for (int i = 0; i < entries.Length; i++)
        {
            if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
            {
                entry = entries[i];
                return true;
            }
        }

        entry = null;
        return false;
    }

    public static TotemRuntimeAssetCatalog BuildDefault()
    {
        return new TotemRuntimeAssetCatalog
        {
            schemaVersion = 1,
            source = "runtime-default",
            entries = Array.Empty<TotemRuntimeAssetEntry>(),
        };
    }
}

[Serializable]
public sealed class TotemRuntimeAssetEntry
{
    public string key;
    public string assetKind;
    public string role;
    public string legacySourcePath;
    public string activeAssetPath;
    public string loadMode;
    public string fallbackKey;
    public string fallbackPrimitive;
    public string tint;
    public float scaleX = 1f;
    public float scaleY = 1f;
    public float scaleZ = 1f;
    public string notes;

    public Vector3 Scale => new Vector3(scaleX <= 0f ? 1f : scaleX, scaleY <= 0f ? 1f : scaleY, scaleZ <= 0f ? 1f : scaleZ);
}

public static class TotemRuntimeAssetCatalogValidator
{
    private static readonly string[] RequiredKeys =
    {
        "actor.player",
        TotemFirstPlayableArtHandoff.WeaponKey,
        "effect.attack.hit",
    };

    public static bool Validate(TotemRuntimeAssetCatalog catalog, IList<string> errors)
    {
        if (catalog == null)
        {
            errors?.Add("Runtime asset catalog is null.");
            return false;
        }

        catalog.Normalize();
        Require(catalog.schemaVersion > 0, errors, "schemaVersion must be positive.");
        for (int i = 0; i < RequiredKeys.Length; i++)
        {
            Require(catalog.TryGetEntry(RequiredKeys[i], out _), errors, $"Required runtime asset key is missing: {RequiredKeys[i]}");
        }

        for (int i = 0; i < catalog.entries.Length; i++)
        {
            var entry = catalog.entries[i];
            Require(!string.IsNullOrWhiteSpace(entry.key), errors, $"Entry[{i}] key is empty.");
            Require(!string.IsNullOrWhiteSpace(entry.assetKind), errors, $"Entry[{i}] assetKind is empty.");
            Require(
                !string.IsNullOrWhiteSpace(entry.activeAssetPath) || !string.IsNullOrWhiteSpace(entry.fallbackPrimitive),
                errors,
                $"Entry[{i}] must provide an activeAssetPath or fallbackPrimitive.");
        }

        return errors == null || errors.Count <= 0;
    }

    private static void Require(bool condition, IList<string> errors, string message)
    {
        if (!condition)
        {
            errors?.Add(message);
        }
    }
}
