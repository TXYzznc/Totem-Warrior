using System;
using System.Collections.Generic;

/// <summary>
/// 聚合 IDisposable。管理多个临时订阅的生命周期。
/// </summary>
public class CompositeDisposable : IDisposable
{
    readonly List<IDisposable> _list = new();
    bool _disposed;

    /// <summary>
    /// 添加一个 disposable。已 Dispose 后添加会抛出 ObjectDisposedException。
    /// </summary>
    public void Add(IDisposable d)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CompositeDisposable));
        _list.Add(d);
    }

    /// <summary>
    /// 释放所有已添加的 disposable。幂等。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var d in _list)
            d.Dispose();

        _list.Clear();
    }
}