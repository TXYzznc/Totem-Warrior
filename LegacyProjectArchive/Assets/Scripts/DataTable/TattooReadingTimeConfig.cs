// v2.1 新增：纹身读条时长配置
// 参考 TattooPartConfig 风格手写（非 generator 产出）

using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class TattooReadingTimeConfigRow
{
    /// <summary>主键，对齐 TattooPartConfig.Id（1-6）</summary>
    public int PartId { get; set; }
    /// <summary>部位名（仅作可读冗余）</summary>
    public string PartName { get; set; }
    /// <summary>自纹身读条秒数</summary>
    public float DurationSec { get; set; }
}

/// <summary>v2.1：自纹身读条时长。按部位查询 → TattooModule.StartSelfTattoo 替换 Magic Number。</summary>
public sealed class TattooReadingTimeConfig : IDataTable
{
    readonly Dictionary<int, TattooReadingTimeConfigRow> _rows = new();

    public void Load(string json)
    {
        var file = JsonConvert.DeserializeObject<DataTableFile<TattooReadingTimeConfigRow>>(json);
        _rows.Clear();
        foreach (var row in file.rows)
        {
            _rows[row.PartId] = row;
        }
    }

    public TattooReadingTimeConfigRow GetById(int partId)
    {
        if (_rows.TryGetValue(partId, out var row))
            return row;
        throw new KeyNotFoundException($"TattooReadingTimeConfig 未找到 PartId={partId}");
    }

    public bool TryGetById(int partId, out TattooReadingTimeConfigRow row) => _rows.TryGetValue(partId, out row);

    public IReadOnlyDictionary<int, TattooReadingTimeConfigRow> All => _rows;
}
