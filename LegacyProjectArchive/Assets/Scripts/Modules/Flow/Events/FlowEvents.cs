using System;
using System.Collections.Generic;

public sealed class FlowChangeRequestedEvent
{
    public string FlowId { get; }
    public IReadOnlyDictionary<string, object> Parameters { get; }

    public FlowChangeRequestedEvent(string flowId, IReadOnlyDictionary<string, object> parameters = null)
    {
        FlowId = flowId;
        Parameters = parameters;
    }
}

public sealed class FlowChangingEvent
{
    public string FromFlowId { get; }
    public string ToFlowId { get; }
    public IReadOnlyDictionary<string, object> Parameters { get; }

    public FlowChangingEvent(string fromFlowId, string toFlowId, IReadOnlyDictionary<string, object> parameters = null)
    {
        FromFlowId = fromFlowId;
        ToFlowId = toFlowId;
        Parameters = parameters;
    }
}

public sealed class FlowChangedEvent
{
    public string FromFlowId { get; }
    public string ToFlowId { get; }
    public IReadOnlyDictionary<string, object> Parameters { get; }

    public FlowChangedEvent(string fromFlowId, string toFlowId, IReadOnlyDictionary<string, object> parameters = null)
    {
        FromFlowId = fromFlowId;
        ToFlowId = toFlowId;
        Parameters = parameters;
    }
}

public sealed class FlowExitedEvent
{
    public string FlowId { get; }

    public FlowExitedEvent(string flowId)
    {
        FlowId = flowId;
    }
}

public sealed class FlowChangeFailedEvent
{
    public string FromFlowId { get; }
    public string ToFlowId { get; }
    public Exception Exception { get; }
    public IReadOnlyDictionary<string, object> Parameters { get; }

    public FlowChangeFailedEvent(
        string fromFlowId,
        string toFlowId,
        Exception exception,
        IReadOnlyDictionary<string, object> parameters = null)
    {
        FromFlowId = fromFlowId;
        ToFlowId = toFlowId;
        Exception = exception;
        Parameters = parameters;
    }
}
