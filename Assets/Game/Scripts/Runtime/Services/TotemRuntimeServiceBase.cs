using System;

public abstract class TotemRuntimeServiceBase : ITotemRuntimeService
{
    public abstract string ServiceName { get; }

    public TotemRuntimeServiceState State { get; private set; } = TotemRuntimeServiceState.Created;

    public string LastMessage { get; private set; } = string.Empty;

    protected TotemGameRuntime Runtime { get; private set; }

    public void Initialize(TotemGameRuntime runtime)
    {
        if (State == TotemRuntimeServiceState.Ready)
        {
            return;
        }

        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        State = TotemRuntimeServiceState.Initializing;
        LastMessage = "Initializing";
        GFTrace.Info("TotemRuntime", "Service.Initialize.Begin", null, GFTrace.Data("service", ServiceName));

        try
        {
            OnInitialize(runtime);
            State = TotemRuntimeServiceState.Ready;
            LastMessage = "Ready";
            GFTrace.Success("TotemRuntime", "Service.Initialize.Success", null, GFTrace.Data("service", ServiceName));
        }
        catch (Exception exception)
        {
            State = TotemRuntimeServiceState.Failed;
            LastMessage = exception.GetType().Name;
            GFTrace.Exception("TotemRuntime", "Service.Initialize.Exception", exception, GFTrace.Data("service", ServiceName));
            throw;
        }
    }

    public void Shutdown()
    {
        if (State == TotemRuntimeServiceState.Shutdown)
        {
            return;
        }

        try
        {
            OnShutdown();
            State = TotemRuntimeServiceState.Shutdown;
            LastMessage = "Shutdown";
            GFTrace.Info("TotemRuntime", "Service.Shutdown", null, GFTrace.Data("service", ServiceName));
        }
        catch (Exception exception)
        {
            State = TotemRuntimeServiceState.Failed;
            LastMessage = exception.GetType().Name;
            GFTrace.Exception("TotemRuntime", "Service.Shutdown.Exception", exception, GFTrace.Data("service", ServiceName));
        }
    }

    public TotemRuntimeServiceStatus CaptureStatus()
    {
        return new TotemRuntimeServiceStatus
        {
            serviceName = ServiceName,
            state = State.ToString(),
            lastMessage = LastMessage,
        };
    }

    protected virtual void OnInitialize(TotemGameRuntime runtime)
    {
    }

    protected virtual void OnShutdown()
    {
    }
}
