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

[Serializable]
public sealed class TotemRuntimeServiceStatus
{
    public string serviceName;
    public string state;
    public string lastMessage;
}
