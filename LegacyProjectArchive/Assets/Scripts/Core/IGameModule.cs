using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 模块接口。每个游戏功能模块必须实现此接口。
/// </summary>
public interface IGameModule
{
    /// <summary>
    /// 模块功能类别。
    /// 0 = 启动基础层（必须最早可用的底层能力）
    /// 1 = 运行服务层（可被多个上层模块复用的服务）
    /// 2 = 应用协调层（流程、场景、状态等协调能力）
    /// 3 = 项目扩展层（项目自定义模块的默认位置）
    /// 4 = 后台辅助层（不阻塞主要启动路径的辅助能力，默认值）
    /// </summary>
    int ModuleCategory => 4;

    /// <summary>
    /// 硬依赖。必须放具体的 Module 类型，不支持接口。
    /// ModuleRunner 保证 Dependencies 全部 Initialized 后才开始本模块的 InitializeAsync。
    /// </summary>
    Type[] Dependencies => Type.EmptyTypes;

    /// <summary>
    /// 初始化。Dependencies 保证已完成。
    /// ct 在启动失败、超时或退出 Play Mode 时被取消，用于协作取消。
    /// </summary>
    UniTask InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// 关闭。按初始化完成序的逆序调用。
    /// 注销在 ShutdownAsync 完成后，清理逻辑中可继续发事件。
    /// ct 在退出 Play Mode 时被取消。
    /// </summary>
    UniTask ShutdownAsync(CancellationToken ct = default);
}
