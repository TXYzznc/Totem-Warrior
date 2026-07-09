using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[Serializable]
public sealed class GFDiagnosticReport
{
    public int schemaVersion = 1;
    public string action;
    public string createdAtUtc;
    public int successCount;
    public int failureCount;
    public int warningCount;
    public List<string> warnings = new List<string>();
    public List<GFDiagnosticReportItem> items = new List<GFDiagnosticReportItem>();
    public GFDiagnosticSnapshot snapshot;
    public List<GFTraceEvent> timeline = new List<GFTraceEvent>();

    public GFDiagnosticReport(string action)
    {
        this.action = action;
        createdAtUtc = DateTime.UtcNow.ToString("O");
    }

    public GFDiagnosticReportItem AddItem(string name)
    {
        var item = new GFDiagnosticReportItem
        {
            name = name,
        };
        items.Add(item);
        return item;
    }

    public void RefreshSummary()
    {
        successCount = items.Count(item => item.success);
        failureCount = items.Count(item => !item.success);
        warningCount = warnings.Count + items.Sum(item => item.warnings.Count);
    }

    public void AttachRuntimeContext(string snapshotReason, int maxTraceEvents = 160)
    {
        snapshot = GFDiagnosticSnapshot.Capture(snapshotReason, maxTraceEvents);
        timeline = GFTrace.GetRecentEvents(maxTraceEvents);
    }

    public static void WriteJson(string fileName, GFDiagnosticReport report)
    {
        EnsureFileDirectory(fileName);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(fileName, json, new UTF8Encoding(false));
    }

    private static void EnsureFileDirectory(string fileName)
    {
        string directoryName = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }
}

[Serializable]
public sealed class GFDiagnosticReportItem
{
    public string name;
    public bool success;
    public long durationMs;
    public List<string> warnings = new List<string>();
    public List<string> errors = new List<string>();
    public Dictionary<string, string> details = new Dictionary<string, string>(StringComparer.Ordinal);

    public void Pass(string message = null)
    {
        success = errors.Count <= 0;
        if (!string.IsNullOrWhiteSpace(message))
        {
            details["message"] = message;
        }
    }

    public void Fail(string message)
    {
        success = false;
        if (!string.IsNullOrWhiteSpace(message))
        {
            errors.Add(message);
        }
    }

    public void Warn(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            warnings.Add(message);
        }
    }

    public void Detail(string key, object value)
    {
        details[key] = value == null ? string.Empty : value.ToString();
    }
}
