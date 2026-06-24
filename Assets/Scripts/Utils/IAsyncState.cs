using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Generic asynchronous state contract. Framework code does not define concrete states.
/// </summary>
public interface IAsyncState<TStateId, TContext>
{
    TStateId Id { get; }

    UniTask EnterAsync(TContext context, CancellationToken ct = default);

    UniTask ExitAsync(TContext context, CancellationToken ct = default);

    void Tick(TContext context, float deltaTime);
}
