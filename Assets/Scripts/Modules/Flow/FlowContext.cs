using System.Collections.Generic;

/// <summary>
/// Generic context passed into flow enter/exit methods.
/// </summary>
public sealed class FlowContext
{
    static readonly IReadOnlyDictionary<string, object> EmptyParameters =
        new Dictionary<string, object>();

    public EventBus EventBus { get; }
    public IReadOnlyDictionary<string, object> Parameters { get; }

    public FlowContext(EventBus eventBus, IReadOnlyDictionary<string, object> parameters = null)
    {
        EventBus = eventBus;
        Parameters = parameters ?? EmptyParameters;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (Parameters.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}
