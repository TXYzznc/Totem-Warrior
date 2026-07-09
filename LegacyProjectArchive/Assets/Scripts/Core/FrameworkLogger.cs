using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// AI 友好型结构化日志。
/// 格式：[模块名|级别] message Location=文件名:行号
/// </summary>
public static class FrameworkLogger
{
    public static void Error(string module, string message,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        UnityEngine.Debug.LogError(Format(module, "ERROR", message, file, line));
    }

    public static void Warn(string module, string message,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        UnityEngine.Debug.LogWarning(Format(module, "WARN", message, file, line));
    }

    public static void Info(string module, string message,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        UnityEngine.Debug.Log(Format(module, "INFO", message, file, line));
    }

    public static void Debug(string module, string message,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        UnityEngine.Debug.Log(Format(module, "DEBUG", message, file, line));
    }

    static string Format(string module, string level, string message, string file, int line)
    {
        return $"[{module}|{level}] {message} Location={Path.GetFileName(file)}:{line}";
    }
}