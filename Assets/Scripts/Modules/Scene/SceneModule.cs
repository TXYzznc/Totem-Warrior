using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Generic Unity scene loading module. It knows scene names, not game meaning.
/// </summary>
public sealed class SceneModule : IGameModule
{
    readonly EventBus _eventBus;
    CancellationTokenSource _lifetimeCts;
    bool _isBusy;

    public int ModuleCategory => 0;

    public SceneModule(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        FrameworkLogger.Info("SceneModule", "Action=Initialized");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
        FrameworkLogger.Info("SceneModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    public IReadOnlyList<string> GetLoadedSceneNames()
    {
        var names = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
                names.Add(scene.name);
        }
        return names;
    }

    public async UniTask LoadSceneAsync(
        string sceneName,
        LoadSceneMode mode = LoadSceneMode.Single,
        bool setActiveScene = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new ArgumentException("场景名不能为空", nameof(sceneName));
        if (_isBusy)
            throw new InvalidOperationException("SceneModule 正在执行场景操作");

        using var linkedCts = CreateLinkedCts(ct);
        var token = linkedCts.Token;
        _isBusy = true;

        _eventBus.Publish(new SceneLoadStartedEvent(sceneName, mode));
        FrameworkLogger.Info("SceneModule",
            $"Action=LoadSceneStarted Scene={sceneName} Mode={mode}");

        try
        {
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
                throw new InvalidOperationException($"无法创建场景加载操作: {sceneName}");

            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                _eventBus.Publish(new SceneLoadProgressEvent(sceneName, op.progress));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (setActiveScene)
            {
                var loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                    SceneManager.SetActiveScene(loadedScene);
            }

            _eventBus.Publish(new SceneLoadProgressEvent(sceneName, 1f));
            _eventBus.Publish(new SceneLoadedEvent(sceneName, mode));
            FrameworkLogger.Info("SceneModule",
                $"Action=LoadSceneCompleted Scene={sceneName} Mode={mode}");
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new SceneLoadFailedEvent(sceneName, ex.Message, ex));
            FrameworkLogger.Error("SceneModule",
                $"Action=LoadSceneFailed Scene={sceneName} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
        finally
        {
            _isBusy = false;
        }
    }

    public async UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new ArgumentException("场景名不能为空", nameof(sceneName));
        if (_isBusy)
            throw new InvalidOperationException("SceneModule 正在执行场景操作");

        using var linkedCts = CreateLinkedCts(ct);
        var token = linkedCts.Token;
        _isBusy = true;

        _eventBus.Publish(new SceneUnloadStartedEvent(sceneName));
        FrameworkLogger.Info("SceneModule",
            $"Action=UnloadSceneStarted Scene={sceneName}");

        try
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                FrameworkLogger.Warn("SceneModule",
                    $"Action=UnloadSceneSkipped Scene={sceneName} Reason=NotLoaded");
                _eventBus.Publish(new SceneUnloadedEvent(sceneName));
                return;
            }

            var op = SceneManager.UnloadSceneAsync(scene);
            if (op == null)
                throw new InvalidOperationException($"无法创建场景卸载操作: {sceneName}");

            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                _eventBus.Publish(new SceneUnloadProgressEvent(sceneName, op.progress));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _eventBus.Publish(new SceneUnloadProgressEvent(sceneName, 1f));
            _eventBus.Publish(new SceneUnloadedEvent(sceneName));
            FrameworkLogger.Info("SceneModule",
                $"Action=UnloadSceneCompleted Scene={sceneName}");
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new SceneUnloadFailedEvent(sceneName, ex.Message, ex));
            FrameworkLogger.Error("SceneModule",
                $"Action=UnloadSceneFailed Scene={sceneName} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
        finally
        {
            _isBusy = false;
        }
    }

    public async UniTask ReloadSceneAsync(string sceneName, CancellationToken ct = default)
    {
        await UnloadSceneAsync(sceneName, ct);
        await LoadSceneAsync(sceneName, LoadSceneMode.Additive, true, ct);
    }

    [EventHandler]
    async UniTask OnSceneLoadRequested(SceneLoadRequestedEvent evt)
    {
        await LoadSceneAsync(evt.SceneName, evt.Mode, evt.SetActiveScene);
    }

    [EventHandler]
    async UniTask OnSceneUnloadRequested(SceneUnloadRequestedEvent evt)
    {
        await UnloadSceneAsync(evt.SceneName);
    }

    CancellationTokenSource CreateLinkedCts(CancellationToken ct)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            _lifetimeCts?.Token ?? CancellationToken.None);
    }
}
