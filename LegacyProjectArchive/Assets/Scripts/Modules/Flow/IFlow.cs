using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Generic flow contract. A flow id is project-defined data, not framework semantics.
/// </summary>
public interface IFlow
{
    string Id { get; }

    UniTask EnterAsync(FlowContext context, CancellationToken ct = default);

    UniTask ExitAsync(FlowContext context, CancellationToken ct = default);
}
