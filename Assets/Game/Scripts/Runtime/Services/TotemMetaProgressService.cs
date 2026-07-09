using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class TotemMetaProgressService : TotemRuntimeServiceBase
{
    public const int CharacterSlotCount = 9;
    public const int PatternSlotCount = 6;

    private const int SaveVersion = 1;
    private const string SaveFileName = "totem_meta_progress.json";

    private TotemMetaProgressSnapshot current = CreateDefaultSnapshot();

    public override string ServiceName => "MetaProgress";

    public string LastPersistenceMessage { get; private set; } = string.Empty;

    public bool LoadedFromDisk { get; private set; }

    public bool LastSaveSucceeded { get; private set; }

    public bool AutoSave { get; set; } = true;

    public string FilePathOverride { get; set; } = string.Empty;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        LoadedFromDisk = TryLoadFromFile(GetPersistenceFilePath(), out string loadError);
        if (!LoadedFromDisk && !string.IsNullOrEmpty(loadError))
        {
            GFTrace.Warning("TotemMetaProgress", "LoadSkipped", null, GFTrace.Data("reason", loadError));
        }
    }

    public TotemMetaProgressSnapshot CaptureSnapshot()
    {
        current = Sanitize(current);
        return Clone(current);
    }

    public bool IsCharacterUnlocked(int slotIndex)
    {
        current = Sanitize(current);
        return IsValidCharacterSlot(slotIndex) && current.characterSlots[slotIndex];
    }

    public bool SetCharacterUnlocked(int slotIndex, bool unlocked)
    {
        if (!IsValidCharacterSlot(slotIndex) || (slotIndex == 0 && !unlocked))
        {
            return false;
        }

        current = Sanitize(current);
        if (current.characterSlots[slotIndex] == unlocked)
        {
            return true;
        }

        current.characterSlots[slotIndex] = unlocked;
        PersistMutation("CharacterSlotChanged", GFTrace.Data(
            "slot", slotIndex.ToString(),
            "unlocked", unlocked.ToString()));
        return true;
    }

    public bool IsPatternUnlocked(string patternId, int slotIndex)
    {
        if (!IsValidPatternSlot(slotIndex) || string.IsNullOrWhiteSpace(patternId))
        {
            return false;
        }

        current = Sanitize(current);
        string normalizedId = NormalizeId(patternId);
        var entry = current.patternUnlocks.FirstOrDefault(item => item.patternId == normalizedId);
        return entry != null && entry.slots != null && entry.slots.Length > slotIndex && entry.slots[slotIndex];
    }

    public bool SetPatternUnlocked(string patternId, int slotIndex, bool unlocked)
    {
        if (!IsValidPatternSlot(slotIndex) || string.IsNullOrWhiteSpace(patternId))
        {
            return false;
        }

        current = Sanitize(current);
        string normalizedId = NormalizeId(patternId);
        var entries = new List<TotemPatternUnlockSnapshot>(current.patternUnlocks);
        var entry = entries.FirstOrDefault(item => item.patternId == normalizedId);
        if (entry == null)
        {
            if (!unlocked)
            {
                return true;
            }

            entry = new TotemPatternUnlockSnapshot
            {
                patternId = normalizedId,
                slots = new bool[PatternSlotCount],
            };
            entries.Add(entry);
        }

        entry.slots = EnsureBoolArray(entry.slots, PatternSlotCount);
        if (entry.slots[slotIndex] == unlocked)
        {
            return true;
        }

        entry.slots[slotIndex] = unlocked;
        current.patternUnlocks = entries.Where(HasAnyUnlockedSlot).ToArray();
        current = Sanitize(current);
        PersistMutation("PatternSlotChanged", GFTrace.Data(
            "patternId", normalizedId,
            "slot", slotIndex.ToString(),
            "unlocked", unlocked.ToString()));
        return true;
    }

    public bool SetDecorationUnlocked(string decorationId, bool unlocked = true)
    {
        return SetStringFlag(MetaStringSetKind.Decoration, decorationId, unlocked, "DecorationChanged");
    }

    public bool SetTitleUnlocked(string titleId, bool unlocked = true)
    {
        return SetStringFlag(MetaStringSetKind.Title, titleId, unlocked, "TitleChanged");
    }

    public bool SetGalleryUnlocked(string galleryId, bool unlocked = true)
    {
        return SetStringFlag(MetaStringSetKind.Gallery, galleryId, unlocked, "GalleryChanged");
    }

    public bool SetAchievementCompleted(string achievementId, bool completed = true)
    {
        return SetStringFlag(MetaStringSetKind.Achievement, achievementId, completed, "AchievementChanged");
    }

    public bool SaveCurrent(out string error)
    {
        current = Sanitize(current);
        bool saved = TryWriteSnapshotToFile(GetPersistenceFilePath(), current, out error);
        LastSaveSucceeded = saved;
        LastPersistenceMessage = saved ? $"Saved {GetPersistenceFilePath()}" : error;
        return saved;
    }

    public bool TryLoadFromFile(string fileName, out string error)
    {
        error = string.Empty;
        LoadedFromDisk = false;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "Path is empty.";
            LastPersistenceMessage = error;
            current = CreateDefaultSnapshot();
            return false;
        }

        if (!File.Exists(fileName))
        {
            error = "File not found.";
            LastPersistenceMessage = error;
            current = CreateDefaultSnapshot();
            return false;
        }

        if (!TryReadSnapshotFromFile(fileName, out var loaded, out error))
        {
            LastPersistenceMessage = error;
            current = CreateDefaultSnapshot();
            return false;
        }

        current = Sanitize(loaded);
        LoadedFromDisk = true;
        LastPersistenceMessage = $"Loaded {fileName}";
        return true;
    }

    public bool TrySaveToFile(string fileName, out string error)
    {
        current = Sanitize(current);
        bool saved = TryWriteSnapshotToFile(fileName, current, out error);
        LastSaveSucceeded = saved;
        LastPersistenceMessage = saved ? $"Saved {fileName}" : error;
        return saved;
    }

    public static TotemMetaProgressSnapshot CreateDefaultSnapshot()
    {
        var snapshot = new TotemMetaProgressSnapshot
        {
            characterSlots = new bool[CharacterSlotCount],
            patternUnlocks = Array.Empty<TotemPatternUnlockSnapshot>(),
            unlockedDecorations = Array.Empty<string>(),
            unlockedTitles = Array.Empty<string>(),
            unlockedGallery = Array.Empty<string>(),
            completedAchievements = Array.Empty<string>(),
            lastSavedUtc = string.Empty,
        };
        snapshot.characterSlots[0] = true;
        return snapshot;
    }

    public static string FormatSnapshot(TotemMetaProgressSnapshot value)
    {
        var snapshot = Sanitize(value);
        return $"Characters: {CountUnlockedCharacters(snapshot)}/{CharacterSlotCount}  Patterns: {CountUnlockedPatternSlots(snapshot)}  Decorations: {snapshot.unlockedDecorations.Length}  Titles: {snapshot.unlockedTitles.Length}  Gallery: {snapshot.unlockedGallery.Length}  Achievements: {snapshot.completedAchievements.Length}";
    }

    public static int CountUnlockedCharacters(TotemMetaProgressSnapshot value)
    {
        var slots = EnsureCharacterSlots(value?.characterSlots);
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i])
            {
                count++;
            }
        }

        return count;
    }

    public static int CountUnlockedPatternSlots(TotemMetaProgressSnapshot value)
    {
        var entries = NormalizePatternUnlocks(value?.patternUnlocks);
        int count = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            for (int j = 0; j < entries[i].slots.Length; j++)
            {
                if (entries[i].slots[j])
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static string GetDefaultMetaProgressFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    public static bool TryReadSnapshotFromFile(string fileName, out TotemMetaProgressSnapshot value, out string error)
    {
        value = null;
        error = string.Empty;
        try
        {
            string json = File.ReadAllText(fileName, Encoding.UTF8);
            var data = JsonUtility.FromJson<TotemMetaProgressSaveData>(json);
            if (data == null || data.progress == null)
            {
                error = "Meta progress JSON is empty or invalid.";
                return false;
            }

            value = Sanitize(data.progress);
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public static bool TryWriteSnapshotToFile(string fileName, TotemMetaProgressSnapshot value, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "Path is empty.";
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = new TotemMetaProgressSaveData
            {
                version = SaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                progress = Sanitize(value),
            };
            data.progress.lastSavedUtc = data.savedAtUtc;

            string json = JsonUtility.ToJson(data, true);
            string tempFile = fileName + ".tmp";
            string backupFile = fileName + ".bak";
            File.WriteAllText(tempFile, json, new UTF8Encoding(false));
            if (File.Exists(fileName))
            {
                File.Copy(fileName, backupFile, overwrite: true);
                File.Delete(fileName);
            }

            File.Move(tempFile, fileName);
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private bool SetStringFlag(MetaStringSetKind kind, string rawId, bool enabled, string traceAction)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return false;
        }

        current = Sanitize(current);
        string id = NormalizeId(rawId);
        var values = new List<string>(GetStringArray(kind));
        bool contains = values.Contains(id, StringComparer.Ordinal);
        if (contains == enabled)
        {
            return true;
        }

        if (enabled)
        {
            values.Add(id);
        }
        else
        {
            values.RemoveAll(item => item == id);
        }

        SetStringArray(kind, NormalizeStringSet(values));
        PersistMutation(traceAction, GFTrace.Data("id", id, "enabled", enabled.ToString()));
        return true;
    }

    private string[] GetStringArray(MetaStringSetKind kind)
    {
        return kind switch
        {
            MetaStringSetKind.Decoration => current.unlockedDecorations,
            MetaStringSetKind.Title => current.unlockedTitles,
            MetaStringSetKind.Gallery => current.unlockedGallery,
            MetaStringSetKind.Achievement => current.completedAchievements,
            _ => Array.Empty<string>(),
        };
    }

    private void SetStringArray(MetaStringSetKind kind, string[] values)
    {
        switch (kind)
        {
            case MetaStringSetKind.Decoration:
                current.unlockedDecorations = values;
                break;
            case MetaStringSetKind.Title:
                current.unlockedTitles = values;
                break;
            case MetaStringSetKind.Gallery:
                current.unlockedGallery = values;
                break;
            case MetaStringSetKind.Achievement:
                current.completedAchievements = values;
                break;
        }
    }

    private void PersistMutation(string action, Dictionary<string, string> data)
    {
        if (!AutoSave)
        {
            LastPersistenceMessage = "AutoSave disabled.";
            GFTrace.Info("TotemMetaProgress", action, null, data);
            return;
        }

        LastSaveSucceeded = SaveCurrent(out string saveError);
        if (LastSaveSucceeded)
        {
            GFTrace.Success("TotemMetaProgress", action, null, data);
        }
        else
        {
            var traceData = data == null
                ? GFTrace.Data("error", saveError ?? string.Empty)
                : new Dictionary<string, string>(data) { ["error"] = saveError ?? string.Empty };
            GFTrace.Warning("TotemMetaProgress", action, null, traceData);
        }
    }

    private string GetPersistenceFilePath()
    {
        return string.IsNullOrWhiteSpace(FilePathOverride)
            ? GetDefaultMetaProgressFilePath()
            : FilePathOverride;
    }

    private static TotemMetaProgressSnapshot Sanitize(TotemMetaProgressSnapshot source)
    {
        if (source == null)
        {
            return CreateDefaultSnapshot();
        }

        return new TotemMetaProgressSnapshot
        {
            characterSlots = EnsureCharacterSlots(source.characterSlots),
            patternUnlocks = NormalizePatternUnlocks(source.patternUnlocks),
            unlockedDecorations = NormalizeStringSet(source.unlockedDecorations),
            unlockedTitles = NormalizeStringSet(source.unlockedTitles),
            unlockedGallery = NormalizeStringSet(source.unlockedGallery),
            completedAchievements = NormalizeStringSet(source.completedAchievements),
            lastSavedUtc = source.lastSavedUtc ?? string.Empty,
        };
    }

    private static TotemMetaProgressSnapshot Clone(TotemMetaProgressSnapshot source)
    {
        var snapshot = Sanitize(source);
        return new TotemMetaProgressSnapshot
        {
            characterSlots = EnsureCharacterSlots(snapshot.characterSlots),
            patternUnlocks = snapshot.patternUnlocks
                .Select(item => new TotemPatternUnlockSnapshot
                {
                    patternId = item.patternId,
                    slots = EnsureBoolArray(item.slots, PatternSlotCount),
                })
                .ToArray(),
            unlockedDecorations = NormalizeStringSet(snapshot.unlockedDecorations),
            unlockedTitles = NormalizeStringSet(snapshot.unlockedTitles),
            unlockedGallery = NormalizeStringSet(snapshot.unlockedGallery),
            completedAchievements = NormalizeStringSet(snapshot.completedAchievements),
            lastSavedUtc = snapshot.lastSavedUtc ?? string.Empty,
        };
    }

    private static bool[] EnsureCharacterSlots(bool[] source)
    {
        var slots = EnsureBoolArray(source, CharacterSlotCount);
        slots[0] = true;
        return slots;
    }

    private static bool[] EnsureBoolArray(bool[] source, int length)
    {
        var slots = new bool[length];
        if (source != null)
        {
            Array.Copy(source, slots, Mathf.Min(source.Length, length));
        }

        return slots;
    }

    private static TotemPatternUnlockSnapshot[] NormalizePatternUnlocks(TotemPatternUnlockSnapshot[] source)
    {
        if (source == null || source.Length <= 0)
        {
            return Array.Empty<TotemPatternUnlockSnapshot>();
        }

        var merged = new SortedDictionary<string, bool[]>(StringComparer.Ordinal);
        for (int i = 0; i < source.Length; i++)
        {
            var entry = source[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.patternId))
            {
                continue;
            }

            string patternId = NormalizeId(entry.patternId);
            if (!merged.TryGetValue(patternId, out var targetSlots))
            {
                targetSlots = new bool[PatternSlotCount];
                merged.Add(patternId, targetSlots);
            }

            var sourceSlots = EnsureBoolArray(entry.slots, PatternSlotCount);
            for (int slot = 0; slot < PatternSlotCount; slot++)
            {
                targetSlots[slot] |= sourceSlots[slot];
            }
        }

        return merged
            .Where(pair => HasAnyUnlockedSlot(pair.Value))
            .Select(pair => new TotemPatternUnlockSnapshot
            {
                patternId = pair.Key,
                slots = EnsureBoolArray(pair.Value, PatternSlotCount),
            })
            .ToArray();
    }

    private static string[] NormalizeStringSet(IEnumerable<string> source)
    {
        if (source == null)
        {
            return Array.Empty<string>();
        }

        return source
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(NormalizeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeId(string value)
    {
        return value == null ? string.Empty : value.Trim();
    }

    private static bool HasAnyUnlockedSlot(TotemPatternUnlockSnapshot entry)
    {
        return entry != null && HasAnyUnlockedSlot(entry.slots);
    }

    private static bool HasAnyUnlockedSlot(bool[] slots)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidCharacterSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < CharacterSlotCount;
    }

    private static bool IsValidPatternSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < PatternSlotCount;
    }

    private enum MetaStringSetKind
    {
        Decoration,
        Title,
        Gallery,
        Achievement,
    }

    [Serializable]
    private sealed class TotemMetaProgressSaveData
    {
        public int version = SaveVersion;
        public string savedAtUtc;
        public TotemMetaProgressSnapshot progress;
    }
}
