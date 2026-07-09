using System;

public sealed class StateChangingEvent<TStateId>
{
    public string MachineName { get; }
    public TStateId From { get; }
    public TStateId To { get; }
    public bool HasPreviousState { get; }

    public StateChangingEvent(string machineName, TStateId from, TStateId to, bool hasPreviousState)
    {
        MachineName = machineName;
        From = from;
        To = to;
        HasPreviousState = hasPreviousState;
    }
}

public sealed class StateChangedEvent<TStateId>
{
    public string MachineName { get; }
    public TStateId From { get; }
    public TStateId To { get; }
    public bool HasPreviousState { get; }

    public StateChangedEvent(string machineName, TStateId from, TStateId to, bool hasPreviousState)
    {
        MachineName = machineName;
        From = from;
        To = to;
        HasPreviousState = hasPreviousState;
    }
}

public sealed class StateChangeFailedEvent<TStateId>
{
    public string MachineName { get; }
    public TStateId From { get; }
    public TStateId To { get; }
    public bool HasPreviousState { get; }
    public Exception Exception { get; }

    public StateChangeFailedEvent(
        string machineName,
        TStateId from,
        TStateId to,
        bool hasPreviousState,
        Exception exception)
    {
        MachineName = machineName;
        From = from;
        To = to;
        HasPreviousState = hasPreviousState;
        Exception = exception;
    }
}
