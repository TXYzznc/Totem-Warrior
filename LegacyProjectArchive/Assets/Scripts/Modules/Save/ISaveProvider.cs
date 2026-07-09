using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 存档 I/O 抽象接口。
/// 本期实现：LocalFileSaveProvider（写到 Application.persistentDataPath/save.json）。
/// 未来扩展：SteamCloudSaveProvider / CustomServerSaveProvider（无需改 SaveModule 内部逻辑）。
/// </summary>
public interface ISaveProvider
{
    /// <summary>从存储加载存档 JSON 字符串。文件不存在时返回 null。</summary>
    UniTask<string> LoadJsonAsync(CancellationToken ct = default);

    /// <summary>将 JSON 字符串原子写入存储。</summary>
    UniTask WriteJsonAsync(string json, CancellationToken ct = default);
}
