using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class DataTableModule : IGameModule
{
    public int ModuleCategory => 0;
    public Type[] Dependencies => Type.EmptyTypes;

    readonly Dictionary<Type, IDataTable> _tables = new();

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        // 同步加载：JSON 体积小（每张 < 10KB），同步加载开销可忽略。
        // 关键好处：EditMode 测试无 frame loop 也能驱动初始化完成（LoadAsync 的 ResourceRequest 在 EditMode 中无法被 yield 驱动到完成）。
        foreach (var entry in DataTableRegistry.Entries)
        {
            ct.ThrowIfCancellationRequested();

            var asset = Resources.Load<TextAsset>($"DataTable/{entry.FileName}");
            if (asset == null)
                throw new InvalidOperationException($"配置表资源未找到: DataTable/{entry.FileName}");

            var table = (IDataTable)Activator.CreateInstance(entry.TableType);
            table.Load(asset.text);
            _tables[entry.TableType] = table;

            FrameworkLogger.Info("DataTableModule",
                $"Action=TableLoaded Table={entry.TableType.Name} File={entry.FileName}");
        }
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _tables.Clear();
        return UniTask.CompletedTask;
    }

    public T GetTable<T>() where T : IDataTable
    {
        if (_tables.TryGetValue(typeof(T), out var table))
            return (T)table;
        throw new KeyNotFoundException($"配置表 {typeof(T).Name} 未加载");
    }
}
