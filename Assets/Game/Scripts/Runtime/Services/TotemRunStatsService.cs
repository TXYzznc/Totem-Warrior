using System;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class TotemRunStatsService : TotemRuntimeServiceBase
{
    private const int SaveVersion = 1;
    private const string SaveFileName = "totem_run_stats.json";

    private TotemRunStatsSnapshot current = new TotemRunStatsSnapshot();

    public override string ServiceName => "RunStats";

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
            GFTrace.Warning("TotemRunStats", "LoadSkipped", null, GFTrace.Data("reason", loadError));
        }
    }

    public TotemRunStatsSnapshot CaptureSnapshot()
    {
        return Clone(current);
    }

    public TotemRunStatsSnapshot RecordRun(TotemRunResultSnapshot result)
    {
        current = ApplyRunResult(current, result);
        string saveError = string.Empty;
        if (AutoSave)
        {
            LastSaveSucceeded = TrySaveToFile(GetPersistenceFilePath(), out saveError);
        }
        else
        {
            LastSaveSucceeded = false;
            LastPersistenceMessage = "AutoSave disabled.";
            saveError = LastPersistenceMessage;
        }

        GFTrace.Success("TotemRunStats", "RunRecorded", null, GFTrace.Data(
            "totalRuns", current.totalRuns.ToString(),
            "totalWins", current.totalWins.ToString(),
            "totalKills", current.totalKills.ToString(),
            "saved", LastSaveSucceeded.ToString(),
            "error", saveError ?? string.Empty));
        return CaptureSnapshot();
    }

    public static TotemRunStatsSnapshot ApplyRunResult(TotemRunStatsSnapshot source, TotemRunResultSnapshot result)
    {
        var value = Sanitize(source);
        if (result == null)
        {
            return value;
        }

        int kills = Mathf.Max(0, result.killCount);
        float elapsedSec = Mathf.Max(0f, result.elapsedSec);
        value.totalRuns++;
        if (result.win)
        {
            value.totalWins++;
            if (elapsedSec > 0f && (value.bestWinTimeSec <= 0f || elapsedSec < value.bestWinTimeSec))
            {
                value.bestWinTimeSec = elapsedSec;
            }
        }
        else
        {
            value.totalLosses++;
        }

        value.totalKills += kills;
        value.totalPlayTimeSec += elapsedSec;
        value.bestKills = Mathf.Max(value.bestKills, kills);
        value.lastResultReason = string.IsNullOrWhiteSpace(result.reason) ? (result.win ? "Victory" : "Defeat") : result.reason;
        value.lastSavedUtc = DateTime.UtcNow.ToString("O");
        return value;
    }

    public static string FormatSnapshot(TotemRunStatsSnapshot value)
    {
        if (value == null)
        {
            return "Total Runs: 0  Wins: 0  Kills: 0  Time: 0.0s";
        }

        return $"Total Runs: {value.totalRuns}  Wins: {value.totalWins}  Losses: {value.totalLosses}\nTotal Kills: {value.totalKills}  Best Kills: {value.bestKills}  Time: {value.totalPlayTimeSec:F1}s";
    }

    public static string GetDefaultStatsFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private string GetPersistenceFilePath()
    {
        return string.IsNullOrWhiteSpace(FilePathOverride)
            ? GetDefaultStatsFilePath()
            : FilePathOverride;
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
        LoadedFromDisk = true;
        LastPersistenceMessage = $"Loaded {fileName}";
        return true;
    }

    public bool TrySaveToFile(string fileName, out string error)
    {
        bool saved = TryWriteSnapshotToFile(fileName, current, out error);
        LastSaveSucceeded = saved;
        LastPersistenceMessage = saved ? $"Saved {fileName}" : error;
        return saved;
    }

    public static bool TryReadSnapshotFromFile(string fileName, out TotemRunStatsSnapshot value, out string error)
    {
        value = null;
        error = string.Empty;
        try
        {
            string json = File.ReadAllText(fileName, Encoding.UTF8);
            var data = JsonUtility.FromJson<TotemRunStatsSaveData>(json);
            if (data == null || data.stats == null)
            {
                error = "Run stats JSON is empty or invalid.";
                return false;
            }

            value = Sanitize(data.stats);
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public static bool TryWriteSnapshotToFile(string fileName, TotemRunStatsSnapshot value, out string error)
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

            var data = new TotemRunStatsSaveData
            {
                version = SaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                stats = Sanitize(value),
            };
            data.stats.lastSavedUtc = data.savedAtUtc;

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

    private static TotemRunStatsSnapshot Sanitize(TotemRunStatsSnapshot source)
    {
        var value = Clone(source);
        value.totalRuns = Mathf.Max(0, value.totalRuns);
        value.totalWins = Mathf.Max(0, value.totalWins);
        value.totalLosses = Mathf.Max(0, value.totalLosses);
        value.totalKills = Mathf.Max(0, value.totalKills);
        value.totalPlayTimeSec = Mathf.Max(0f, value.totalPlayTimeSec);
        value.bestKills = Mathf.Max(0, value.bestKills);
        value.bestWinTimeSec = Mathf.Max(0f, value.bestWinTimeSec);
        value.lastResultReason = value.lastResultReason ?? string.Empty;
        value.lastSavedUtc = value.lastSavedUtc ?? string.Empty;
        return value;
    }

    private static TotemRunStatsSnapshot Clone(TotemRunStatsSnapshot source)
    {
        if (source == null)
        {
            return new TotemRunStatsSnapshot();
        }

        return new TotemRunStatsSnapshot
        {
            totalRuns = source.totalRuns,
            totalWins = source.totalWins,
            totalLosses = source.totalLosses,
            totalKills = source.totalKills,
            totalPlayTimeSec = source.totalPlayTimeSec,
            bestKills = source.bestKills,
            bestWinTimeSec = source.bestWinTimeSec,
            lastResultReason = source.lastResultReason,
            lastSavedUtc = source.lastSavedUtc,
        };
    }

    [Serializable]
    private sealed class TotemRunStatsSaveData
    {
        public int version = SaveVersion;
        public string savedAtUtc;
        public TotemRunStatsSnapshot stats;
    }
}
