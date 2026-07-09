using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 本地文件存档 Provider。
/// 写盘流程：写 .tmp → File.Replace(.tmp, .json, .bak) 原子重命名，防写一半断电损坏。
/// 读盘流程：读 .json → 失败时 fallback .bak → 两者都失败时返回 null（新档）。
/// </summary>
public sealed class LocalFileSaveProvider : ISaveProvider
{
    readonly string _mainPath;
    readonly string _tmpPath;
    readonly string _bakPath;

    public LocalFileSaveProvider(string fileName = "save.json")
    {
        string dir  = Application.persistentDataPath;
        _mainPath   = Path.Combine(dir, fileName);
        _tmpPath    = _mainPath + ".tmp";
        _bakPath    = _mainPath + ".bak";
    }

    /// <summary>主文件路径（供测试验证）。</summary>
    public string MainPath => _mainPath;

    public async UniTask<string> LoadJsonAsync(CancellationToken ct = default)
    {
        // 主文件优先
        string result = await TryReadFileAsync(_mainPath, ct);
        if (result != null)
        {
            FrameworkLogger.Info("LocalFileSaveProvider",
                $"Action=LoadSuccess Path={_mainPath}");
            return result;
        }

        // Fallback 到备份
        result = await TryReadFileAsync(_bakPath, ct);
        if (result != null)
        {
            FrameworkLogger.Warn("LocalFileSaveProvider",
                $"Action=LoadFallbackBak Path={_bakPath}");
            return result;
        }

        FrameworkLogger.Info("LocalFileSaveProvider", "Action=NoSaveFile ReturnNull");
        return null;
    }

    public async UniTask WriteJsonAsync(string json, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("SaveData JSON 不能为空", nameof(json));

        byte[] bytes = Encoding.UTF8.GetBytes(json);

        // 1. 写到 .tmp
        await File.WriteAllBytesAsync(_tmpPath, bytes, ct);

        // 2. 原子重命名（File.Replace：tmp→main，旧 main→bak）
        if (File.Exists(_mainPath))
        {
            File.Replace(_tmpPath, _mainPath, _bakPath);
        }
        else
        {
            File.Move(_tmpPath, _mainPath);
        }

        FrameworkLogger.Info("LocalFileSaveProvider",
            $"Action=WriteSuccess Path={_mainPath} Size={bytes.Length}B");
    }

    // ── 内部 ────────────────────────────────────────────────────────

    static async UniTask<string> TryReadFileAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return null;
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(path, ct);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            FrameworkLogger.Warn("LocalFileSaveProvider",
                $"Action=ReadFailed Path={path} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            return null;
        }
    }
}
