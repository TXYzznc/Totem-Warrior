using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>通用游戏状态。具体项目按需细分。</summary>
public enum GameState
{
    MainMenu,
    Loading,
    InGame,
    Paused,
    GameOver,
}

/// <summary>游戏状态变更事件。任何模块可订阅以响应状态切换。</summary>
public sealed class GameStateChangedEvent
{
    public GameState OldState;
    public GameState NewState;

    public GameStateChangedEvent(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// 游戏状态枢纽。零依赖，提供状态切换 API + 广播 GameStateChangedEvent。
/// 不绑定任何业务（关卡 / 评分 等）。
/// </summary>
public sealed class GameStateModule : IGameModule
{
    readonly EventBus _eventBus;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    public int ModuleCategory => 1;
    public Type[] Dependencies => Type.EmptyTypes;

    public GameStateModule(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("GameStateModule", $"Action=Initialized State={CurrentState}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("GameStateModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    public void GoToMainMenu() => SetState(GameState.MainMenu);
    public void GoToLoading() => SetState(GameState.Loading);
    public void StartGame() => SetState(GameState.InGame);
    public void Pause()    => SetState(GameState.Paused);
    public void Resume()   => SetState(GameState.InGame);
    public void GameOver() => SetState(GameState.GameOver);

    void SetState(GameState newState)
    {
        if (newState == CurrentState) return;
        var old = CurrentState;
        CurrentState = newState;
        _eventBus.Publish(new GameStateChangedEvent(old, newState));
        FrameworkLogger.Info("GameStateModule", $"Action=StateChanged Old={old} New={newState}");
    }
}
