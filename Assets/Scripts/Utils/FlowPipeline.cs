using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Ordered async step runner for project-defined flows.
/// </summary>
public sealed class FlowPipeline
{
    readonly List<Step> _steps = new();
    readonly EventBus _eventBus;
    readonly string _pipelineName;

    public FlowPipeline(string pipelineName, EventBus eventBus = null)
    {
        _pipelineName = string.IsNullOrEmpty(pipelineName) ? "FlowPipeline" : pipelineName;
        _eventBus = eventBus;
    }

    public int StepCount => _steps.Count;

    public FlowPipeline AddStep(string name, Func<CancellationToken, UniTask> action)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("步骤名不能为空", nameof(name));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _steps.Add(new Step(name, action));
        return this;
    }

    public async UniTask RunAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("FlowPipeline",
            $"Pipeline={_pipelineName} Action=Start Steps={_steps.Count}");

        for (int i = 0; i < _steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var step = _steps[i];
            var progress = _steps.Count == 0 ? 1f : i / (float)_steps.Count;
            _eventBus?.Publish(new FlowPipelineStepStartedEvent(_pipelineName, step.Name, i, _steps.Count, progress));
            FrameworkLogger.Info("FlowPipeline",
                $"Pipeline={_pipelineName} Step={step.Name} Index={i} Action=StepStarted");

            try
            {
                await step.Action(ct);
            }
            catch (Exception ex)
            {
                _eventBus?.Publish(new FlowPipelineFailedEvent(_pipelineName, step.Name, i, ex));
                FrameworkLogger.Error("FlowPipeline",
                    $"Pipeline={_pipelineName} Step={step.Name} Index={i} Action=StepFailed Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
                throw;
            }

            var completedProgress = (i + 1) / (float)_steps.Count;
            _eventBus?.Publish(new FlowPipelineStepCompletedEvent(_pipelineName, step.Name, i, _steps.Count, completedProgress));
            FrameworkLogger.Info("FlowPipeline",
                $"Pipeline={_pipelineName} Step={step.Name} Index={i} Action=StepCompleted");
        }

        _eventBus?.Publish(new FlowPipelineCompletedEvent(_pipelineName));
        FrameworkLogger.Info("FlowPipeline",
            $"Pipeline={_pipelineName} Action=Completed");
    }

    readonly struct Step
    {
        public readonly string Name;
        public readonly Func<CancellationToken, UniTask> Action;

        public Step(string name, Func<CancellationToken, UniTask> action)
        {
            Name = name;
            Action = action;
        }
    }
}
