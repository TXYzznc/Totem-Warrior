using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 资源管理模块（框架通用）。通过ResourceConfig表将资源ID映射为Resources加载路径，
/// 按ID缓存已加载资源，避免重复Resources.Load。
///
/// 加载路径规则："{ResourceConfig.Type}/{ResourceConfig.LoadPath}"，
/// Type需与Resources下子文件夹名一致（Animation/Audio/Effect/Font/Material/Model/Prefab/Sprite/Texture）。
/// </summary>
public sealed class ResourceModule : IGameModule
{
    readonly ModuleRunner _runner;
    readonly Dictionary<int, UnityEngine.Object> _cache = new();

    ResourceConfig _resourceConfig;

    public int ModuleCategory => 1;
    public Type[] Dependencies => new[] { typeof(DataTableModule) };

    public ResourceModule(ModuleRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _resourceConfig = _runner.GetModule<DataTableModule>().GetTable<ResourceConfig>();
        FrameworkLogger.Info("ResourceModule", "Action=Initialized");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _cache.Clear();
        FrameworkLogger.Info("ResourceModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 按资源ID加载资源，结果按ID缓存。资源不存在或类型不匹配时记Error并返回null。
    /// </summary>
    public T Load<T>(int resourceId) where T : UnityEngine.Object
    {
        if (_cache.TryGetValue(resourceId, out var cached))
            return cached as T;

        var row = _resourceConfig.GetById(resourceId);
        var path = $"{row.Type}/{row.LoadPath}";
        var asset = Resources.Load<T>(path);

        if (asset == null)
        {
            FrameworkLogger.Error("ResourceModule",
                $"Action=Load Id={resourceId} Name={row.Name} Path={path} Result=NotFound");
            return null;
        }

        _cache[resourceId] = asset;
        return asset;
    }
}
