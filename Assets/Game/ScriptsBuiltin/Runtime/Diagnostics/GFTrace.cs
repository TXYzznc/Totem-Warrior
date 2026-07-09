using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GFTraceEvent
{
    public int seq;
    public string utc;
    public int frame;
    public float time;
    public string traceId;
    public string system;
    public string action;
    public string result;
    public string message;
    public Dictionary<string, string> data;
}

public static class GFTrace
{
    public const string ResultInfo = "Info";
    public const string ResultSuccess = "Success";
    public const string ResultWarning = "Warning";
    public const string ResultFailure = "Failure";

    private const int MaxEvents = 500;
    private static readonly List<GFTraceEvent> Events = new List<GFTraceEvent>(MaxEvents);
    private static int nextSeq;
    private static string currentTraceId;
    private static bool unityLogHooked;
    private static bool handlingUnityLog;

    public static string CurrentTraceId => currentTraceId;

    public static string BeginTrace(string name = null)
    {
        currentTraceId = string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString("N") : $"{name}-{Guid.NewGuid():N}";
        Info("Trace", "Begin", currentTraceId);
        return currentTraceId;
    }

    public static void Clear()
    {
        Events.Clear();
        nextSeq = 0;
    }

    public static Dictionary<string, string> Data(params string[] keyValues)
    {
        if (keyValues == null || keyValues.Length <= 0)
        {
            return null;
        }

        var data = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i + 1 < keyValues.Length; i += 2)
        {
            data[keyValues[i] ?? string.Empty] = keyValues[i + 1] ?? string.Empty;
        }

        return data;
    }

    public static void Info(string system, string action, string message = null, Dictionary<string, string> data = null, string traceId = null)
    {
        Record(system, action, ResultInfo, message, data, traceId);
    }

    public static void Success(string system, string action, string message = null, Dictionary<string, string> data = null, string traceId = null)
    {
        Record(system, action, ResultSuccess, message, data, traceId);
    }

    public static void Warning(string system, string action, string message = null, Dictionary<string, string> data = null, string traceId = null)
    {
        Record(system, action, ResultWarning, message, data, traceId);
    }

    public static void Failure(string system, string action, string message = null, Dictionary<string, string> data = null, string traceId = null)
    {
        Record(system, action, ResultFailure, message, data, traceId);
    }

    public static void Exception(string system, string action, Exception exception, Dictionary<string, string> data = null, string traceId = null)
    {
        var message = exception == null ? null : exception.ToString();
        Record(system, action, ResultFailure, message, data, traceId);
    }

    public static void Record(string system, string action, string result, string message = null, Dictionary<string, string> data = null, string traceId = null)
    {
        if (Events.Count >= MaxEvents)
        {
            Events.RemoveAt(0);
        }

        Events.Add(new GFTraceEvent
        {
            seq = ++nextSeq,
            utc = DateTime.UtcNow.ToString("O"),
            frame = Application.isPlaying ? Time.frameCount : 0,
            time = Application.isPlaying ? Time.realtimeSinceStartup : 0f,
            traceId = traceId ?? currentTraceId,
            system = system,
            action = action,
            result = string.IsNullOrWhiteSpace(result) ? ResultInfo : result,
            message = message,
            data = data,
        });
    }

    public static List<GFTraceEvent> GetRecentEvents(int maxCount = 120)
    {
        int count = Mathf.Clamp(maxCount, 0, Events.Count);
        int start = Events.Count - count;
        var result = new List<GFTraceEvent>(count);
        for (int i = start; i < Events.Count; i++)
        {
            result.Add(Events[i]);
        }

        return result;
    }

    public static void EnableUnityLogCapture()
    {
        if (unityLogHooked)
        {
            return;
        }

        Application.logMessageReceived += OnUnityLogMessageReceived;
        unityLogHooked = true;
    }

    public static void DisableUnityLogCapture()
    {
        if (!unityLogHooked)
        {
            return;
        }

        Application.logMessageReceived -= OnUnityLogMessageReceived;
        unityLogHooked = false;
    }

    private static void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (handlingUnityLog)
        {
            return;
        }

        if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

        handlingUnityLog = true;
        try
        {
            string result = type == LogType.Warning ? ResultWarning : ResultFailure;
            Record("UnityLog", type.ToString(), result, condition, Data("stackTrace", stackTrace));
        }
        finally
        {
            handlingUnityLog = false;
        }
    }
}
