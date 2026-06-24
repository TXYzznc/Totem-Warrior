using System;
using System.Collections.Generic;

/// <summary>
/// 通用对象池。预创建固定容量，耗尽时按需扩容（factory按需调用）。
/// 适用于Enemy（纯C#类）和Bullet（MonoBehaviour）等需要复用的对象。
/// </summary>
public class GenericObjectPool<T> where T : class
{
    readonly Func<T> _factory;
    readonly Stack<T> _free;

    public GenericObjectPool(Func<T> factory, int capacity)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _free = new Stack<T>(capacity);
        for (int i = 0; i < capacity; i++)
            _free.Push(_factory());
    }

    public T Get() => _free.Count > 0 ? _free.Pop() : _factory();

    public void Return(T item) => _free.Push(item);

    public int FreeCount => _free.Count;
}
