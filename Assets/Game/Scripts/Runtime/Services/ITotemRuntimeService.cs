using System;

public enum TotemRuntimeServiceState
{
    Created,
    Initializing,
    Ready,
    Failed,
    Shutdown,
}

public interface ITotemRuntimeService
{
    string ServiceName { get; }
    TotemRuntimeServiceState State { get; }
    string LastMessage { get; }
    void Initialize(TotemGameRuntime runtime);
    void Shutdown();
    TotemRuntimeServiceStatus CaptureStatus();
}

public interface ITotemRuntimeTickService
{
    void Tick(float deltaTime);
}

public interface ITotemRuntimeLateTickService
{
    void LateTick(float deltaTime);
}

/// <summary>
/// 标记只允许在 Match gameplay 未暂停时推进的模拟服务。
/// Input、UI、Audio、VFX 表现和 Match 时钟不得实现此接口。
/// </summary>
public interface ITotemGameplaySimulationService
{
}

[Serializable]
public sealed class TotemRuntimeServiceStatus
{
    public string serviceName;
    public string state;
    public string lastMessage;
}
