using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class TotemDataService : TotemRuntimeServiceBase
{
    public const string GameplayCatalogRelativePath = "GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json";

    public override string ServiceName => "Data";

    public TotemGameplayCatalog GameplayCatalog { get; private set; } = TotemGameplayCatalog.BuildDefault();

    public bool GameplayCatalogLoadedFromFile { get; private set; }

    public bool GameplayCatalogUsingFallback { get; private set; } = true;

    public string GameplayCatalogPath { get; private set; } = string.Empty;

    public string GameplayCatalogMessage { get; private set; } = "NotLoaded";

    public string GameplayCatalogContentHash { get; private set; } = "BuildDefault";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        ReloadGameplayCatalog();
    }

    public void ReloadGameplayCatalog()
    {
        GameplayCatalogPath = GetGameplayCatalogPath();
        GameplayCatalogLoadedFromFile = false;
        GameplayCatalogUsingFallback = false;
        GameplayCatalogContentHash = string.Empty;

        if (TryLoadGameplayCatalogFromFile(GameplayCatalogPath, out var loadedCatalog, out string error))
        {
            var validationErrors = new List<string>();
            if (TotemGameplayCatalogValidator.Validate(loadedCatalog, validationErrors))
            {
                GameplayCatalog = loadedCatalog;
                GameplayCatalogLoadedFromFile = true;
                GameplayCatalogUsingFallback = false;
                GameplayCatalogMessage = "Loaded";
                GameplayCatalogContentHash = ComputeFileHash(GameplayCatalogPath);
                GFTrace.Success("TotemData", "GameplayCatalog.Loaded", null, GFTrace.Data(
                    "path", GameplayCatalogPath,
                    "source", GameplayCatalog.source,
                    "hash", GameplayCatalogContentHash,
                    "weaponCount", GameplayCatalog.weapons.Length.ToString(),
                    "npcCount", GameplayCatalog.npcs.Length.ToString()));
                return;
            }

            GameplayCatalogMessage = string.Join("; ", validationErrors);
            GFTrace.Warning("TotemData", "GameplayCatalog.Invalid", null, GFTrace.Data(
                "path", GameplayCatalogPath,
                "errors", GameplayCatalogMessage));
        }
        else
        {
            GameplayCatalogMessage = error;
            GFTrace.Warning("TotemData", "GameplayCatalog.Missing", null, GFTrace.Data(
                "path", GameplayCatalogPath,
                "error", error));
        }

        GameplayCatalog = TotemGameplayCatalog.BuildDefault();
        GameplayCatalog.Normalize();
        GameplayCatalogUsingFallback = true;
        GameplayCatalogContentHash = "BuildDefault";
        GFTrace.Warning("TotemData", "GameplayCatalog.FallbackDefault", null, GFTrace.Data("reason", GameplayCatalogMessage));
    }

    public static string GetGameplayCatalogPath()
    {
        string projectRoot = GetProjectRoot();
        return Path.GetFullPath(Path.Combine(projectRoot, GameplayCatalogRelativePath));
    }

    public static bool TryLoadGameplayCatalogFromFile(string fileName, out TotemGameplayCatalog catalog, out string error)
    {
        catalog = null;
        error = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "File path is empty.";
                return false;
            }

            if (!File.Exists(fileName))
            {
                error = "File does not exist.";
                return false;
            }

            string json = File.ReadAllText(fileName, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "File is empty.";
                return false;
            }

            catalog = JsonUtility.FromJson<TotemGameplayCatalog>(json);
            if (catalog == null)
            {
                error = "JsonUtility returned null.";
                return false;
            }

            catalog.Normalize();
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            catalog = null;
            return false;
        }
    }

    public static bool TryLoadValidatedGameplayCatalogFromDefaultPath(out TotemGameplayCatalog catalog, out string error)
    {
        if (!TryLoadGameplayCatalogFromFile(GetGameplayCatalogPath(), out catalog, out error))
        {
            return false;
        }

        var validationErrors = new List<string>();
        if (TotemGameplayCatalogValidator.Validate(catalog, validationErrors))
        {
            return true;
        }

        error = string.Join("; ", validationErrors);
        catalog = null;
        return false;
    }

    public static TotemGameplayCatalog LoadGameplayCatalogOrDefault()
    {
        if (TryLoadValidatedGameplayCatalogFromDefaultPath(out var catalog, out _))
        {
            return catalog;
        }

        var fallback = TotemGameplayCatalog.BuildDefault();
        fallback.Normalize();
        return fallback;
    }

    private static string GetProjectRoot()
    {
        var assetsDirectory = Directory.GetParent(Application.dataPath);
        return assetsDirectory == null ? Directory.GetCurrentDirectory() : assetsDirectory.FullName;
    }

    private static string ComputeFileHash(string fileName)
    {
        try
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(File.ReadAllBytes(fileName));
                var builder = new StringBuilder(16);
                for (int i = 0; i < hash.Length && i < 8; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
        catch (Exception exception)
        {
            return $"hash-error:{exception.GetType().Name}";
        }
    }
}
