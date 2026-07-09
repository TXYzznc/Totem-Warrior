using System;

public sealed class FlowPipelineStepStartedEvent
{
    public string PipelineName { get; }
    public string StepName { get; }
    public int StepIndex { get; }
    public int StepCount { get; }
    public float Progress { get; }

    public FlowPipelineStepStartedEvent(string pipelineName, string stepName, int stepIndex, int stepCount, float progress)
    {
        PipelineName = pipelineName;
        StepName = stepName;
        StepIndex = stepIndex;
        StepCount = stepCount;
        Progress = progress;
    }
}

public sealed class FlowPipelineStepCompletedEvent
{
    public string PipelineName { get; }
    public string StepName { get; }
    public int StepIndex { get; }
    public int StepCount { get; }
    public float Progress { get; }

    public FlowPipelineStepCompletedEvent(string pipelineName, string stepName, int stepIndex, int stepCount, float progress)
    {
        PipelineName = pipelineName;
        StepName = stepName;
        StepIndex = stepIndex;
        StepCount = stepCount;
        Progress = progress;
    }
}

public sealed class FlowPipelineCompletedEvent
{
    public string PipelineName { get; }

    public FlowPipelineCompletedEvent(string pipelineName)
    {
        PipelineName = pipelineName;
    }
}

public sealed class FlowPipelineFailedEvent
{
    public string PipelineName { get; }
    public string StepName { get; }
    public int StepIndex { get; }
    public Exception Exception { get; }

    public FlowPipelineFailedEvent(string pipelineName, string stepName, int stepIndex, Exception exception)
    {
        PipelineName = pipelineName;
        StepName = stepName;
        StepIndex = stepIndex;
        Exception = exception;
    }
}
