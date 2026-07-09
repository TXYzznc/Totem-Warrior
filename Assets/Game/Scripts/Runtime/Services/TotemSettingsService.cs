using System;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class TotemSettingsService : TotemRuntimeServiceBase
{
    private const int SaveVersion = 1;
    private const string SaveFileName = "totem_settings.json";

    private TotemSettingsSnapshot current = new TotemSettingsSnapshot();
    private TotemSettingsSnapshot snapshot;

    public override string ServiceName => "Settings";

    public string LastPersistenceMessage { get; private set; } = string.Empty;

    public bool LoadedFromDisk { get; private set; }

    public bool LastSaveSucceeded { get; private set; }

    public int IgnoredOperationCount { get; private set; }

    public string LastIgnoredOperation { get; private set; } = string.Empty;

    public string FilePathOverride { get; set; } = string.Empty;

    public event Action<TotemSettingsSnapshot> SettingsChanged;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        LoadedFromDisk = TryLoadFromFile(ResolveSettingsFilePath(), out string loadError);
        if (!LoadedFromDisk && !string.IsNullOrEmpty(loadError))
        {
            GFTrace.Warning("TotemSettings", "LoadSkipped", null, GFTrace.Data("reason", loadError));
        }

        current.qualityLevel = ClampQuality(current.qualityLevel);
        ApplyAndNotify();
    }

    protected override void OnShutdown()
    {
        snapshot = null;
        SettingsChanged = null;
    }

    public TotemSettingsSnapshot CaptureSnapshot()
    {
        return Clone(current);
    }

    public void BeginEdit()
    {
        if (current.editing)
        {
            return;
        }

        snapshot = Clone(current);
        current.editing = true;
        GFTrace.Info("TotemSettings", "BeginEdit");
    }

    public void Preview(float bgmVolume, float sfxVolume, int qualityLevel)
    {
        if (!current.editing)
        {
            RecordIgnoredOperation("Preview");
            return;
        }

        current.bgmVolume = Mathf.Clamp01(bgmVolume);
        current.sfxVolume = Mathf.Clamp01(sfxVolume);
        current.qualityLevel = ClampQuality(qualityLevel);
        ApplyAndNotify();
        GFTrace.Info("TotemSettings", "Preview", null, GFTrace.Data(
            "bgm", current.bgmVolume.ToString("F2"),
            "sfx", current.sfxVolume.ToString("F2"),
            "quality", current.qualityLevel.ToString()));
    }

    public void Commit()
    {
        if (!current.editing)
        {
            RecordIgnoredOperation("Commit");
            return;
        }

        current.editing = false;
        snapshot = null;
        ApplyAndNotify();
        string filePath = ResolveSettingsFilePath();
        LastSaveSucceeded = TrySaveToFile(filePath, out string saveError);
        GFTrace.Success("TotemSettings", "Commit", null, GFTrace.Data(
            "bgm", current.bgmVolume.ToString("F2"),
            "sfx", current.sfxVolume.ToString("F2"),
            "quality", current.qualityLevel.ToString(),
            "saved", LastSaveSucceeded.ToString(),
            "path", filePath,
            "error", saveError ?? string.Empty));
    }

    public void Rollback()
    {
        if (!current.editing)
        {
            RecordIgnoredOperation("Rollback");
            return;
        }

        if (snapshot != null)
        {
            current = Clone(snapshot);
        }

        current.editing = false;
        snapshot = null;
        ApplyAndNotify();
        GFTrace.Info("TotemSettings", "Rollback");
    }

    private void RecordIgnoredOperation(string operation)
    {
        IgnoredOperationCount++;
        LastIgnoredOperation = operation;
        GFTrace.Info("TotemSettings", operation + ".Ignored", null, GFTrace.Data("reason", "NotEditing"));
    }

    public static string FormatSnapshot(TotemSettingsSnapshot value)
    {
        if (value == null)
        {
            return "BGM 0.80  SFX 0.80  Quality 1";
        }

        return $"BGM {value.bgmVolume:F2}  SFX {value.sfxVolume:F2}  Quality {value.qualityLevel}";
    }

    public static string GetDefaultSettingsFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private string ResolveSettingsFilePath()
    {
        return string.IsNullOrWhiteSpace(FilePathOverride) ? GetDefaultSettingsFilePath() : FilePathOverride;
    }

    public bool TryLoadFromFile(string fileName, out string error)
    {
        error = string.Empty;
        LoadedFromDisk = false;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "Path is empty.";
            LastPersistenceMessage = error;
            return false;
        }

        if (!File.Exists(fileName))
        {
            error = "File not found.";
            LastPersistenceMessage = error;
            return false;
        }

        if (!TryReadSnapshotFromFile(fileName, out var loaded, out error))
        {
            LastPersistenceMessage = error;
            return false;
        }

        current = Sanitize(loaded);
        snapshot = null;
        current.editing = false;
        LoadedFromDisk = true;
        LastPersistenceMessage = $"Loaded {fileName}";
        ApplyAndNotify();
        return true;
    }

    public bool TrySaveToFile(string fileName, out string error)
    {
        bool saved = TryWriteSnapshotToFile(fileName, current, out error);
        LastSaveSucceeded = saved;
        LastPersistenceMessage = saved ? $"Saved {fileName}" : error;
        return saved;
    }

    public static bool TryReadSnapshotFromFile(string fileName, out TotemSettingsSnapshot value, out string error)
    {
        value = null;
        error = string.Empty;
        try
        {
            string json = File.ReadAllText(fileName, Encoding.UTF8);
            var data = JsonUtility.FromJson<TotemSettingsSaveData>(json);
            if (data == null || data.settings == null)
            {
                error = "Settings JSON is empty or invalid.";
                return false;
            }

            value = Sanitize(data.settings);
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public static bool TryWriteSnapshotToFile(string fileName, TotemSettingsSnapshot value, out string error)
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

            var data = new TotemSettingsSaveData
            {
                version = SaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                settings = Sanitize(value),
            };
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

    private static void Apply(TotemSettingsSnapshot value)
    {
        AudioListener.volume = Mathf.Clamp01((value.bgmVolume + value.sfxVolume) * 0.5f);
        int quality = ClampQuality(value.qualityLevel);
        if (QualitySettings.names != null && QualitySettings.names.Length > 0)
        {
            QualitySettings.SetQualityLevel(quality, applyExpensiveChanges: false);
        }
    }

    private void ApplyAndNotify()
    {
        Apply(current);
        SettingsChanged?.Invoke(Clone(current));
    }

    private static int ClampQuality(int qualityLevel)
    {
        int max = QualitySettings.names == null || QualitySettings.names.Length == 0
            ? 2
            : QualitySettings.names.Length - 1;
        return Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, max));
    }

    private static TotemSettingsSnapshot Sanitize(TotemSettingsSnapshot source)
    {
        var value = Clone(source);
        value.bgmVolume = Mathf.Clamp01(value.bgmVolume);
        value.sfxVolume = Mathf.Clamp01(value.sfxVolume);
        value.qualityLevel = ClampQuality(value.qualityLevel);
        value.editing = false;
        return value;
    }

    private static TotemSettingsSnapshot Clone(TotemSettingsSnapshot source)
    {
        if (source == null)
        {
            return new TotemSettingsSnapshot();
        }

        return new TotemSettingsSnapshot
        {
            bgmVolume = source.bgmVolume,
            sfxVolume = source.sfxVolume,
            qualityLevel = source.qualityLevel,
            editing = source.editing,
        };
    }

    [Serializable]
    private sealed class TotemSettingsSaveData
    {
        public int version = SaveVersion;
        public string savedAtUtc;
        public TotemSettingsSnapshot settings;
    }
}
