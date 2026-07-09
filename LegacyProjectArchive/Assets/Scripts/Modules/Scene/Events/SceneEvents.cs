using System;
using UnityEngine.SceneManagement;

public sealed class SceneLoadRequestedEvent
{
    public string SceneName { get; }
    public LoadSceneMode Mode { get; }
    public bool SetActiveScene { get; }

    public SceneLoadRequestedEvent(
        string sceneName,
        LoadSceneMode mode = LoadSceneMode.Single,
        bool setActiveScene = true)
    {
        SceneName = sceneName;
        Mode = mode;
        SetActiveScene = setActiveScene;
    }
}

public sealed class SceneLoadStartedEvent
{
    public string SceneName { get; }
    public LoadSceneMode Mode { get; }

    public SceneLoadStartedEvent(string sceneName, LoadSceneMode mode)
    {
        SceneName = sceneName;
        Mode = mode;
    }
}

public sealed class SceneLoadProgressEvent
{
    public string SceneName { get; }
    public float Progress { get; }

    public SceneLoadProgressEvent(string sceneName, float progress)
    {
        SceneName = sceneName;
        Progress = progress;
    }
}

public sealed class SceneLoadedEvent
{
    public string SceneName { get; }
    public LoadSceneMode Mode { get; }

    public SceneLoadedEvent(string sceneName, LoadSceneMode mode)
    {
        SceneName = sceneName;
        Mode = mode;
    }
}

public sealed class SceneLoadFailedEvent
{
    public string SceneName { get; }
    public string ErrorMessage { get; }
    public Exception Exception { get; }

    public SceneLoadFailedEvent(string sceneName, string errorMessage, Exception exception = null)
    {
        SceneName = sceneName;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}

public sealed class SceneUnloadRequestedEvent
{
    public string SceneName { get; }

    public SceneUnloadRequestedEvent(string sceneName)
    {
        SceneName = sceneName;
    }
}

public sealed class SceneUnloadStartedEvent
{
    public string SceneName { get; }

    public SceneUnloadStartedEvent(string sceneName)
    {
        SceneName = sceneName;
    }
}

public sealed class SceneUnloadProgressEvent
{
    public string SceneName { get; }
    public float Progress { get; }

    public SceneUnloadProgressEvent(string sceneName, float progress)
    {
        SceneName = sceneName;
        Progress = progress;
    }
}

public sealed class SceneUnloadedEvent
{
    public string SceneName { get; }

    public SceneUnloadedEvent(string sceneName)
    {
        SceneName = sceneName;
    }
}

public sealed class SceneUnloadFailedEvent
{
    public string SceneName { get; }
    public string ErrorMessage { get; }
    public Exception Exception { get; }

    public SceneUnloadFailedEvent(string sceneName, string errorMessage, Exception exception = null)
    {
        SceneName = sceneName;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}
