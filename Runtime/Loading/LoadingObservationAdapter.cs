using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Transition;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Adapter that observes existing SceneLifecycle and Transition diagnostics as canonical Loading progress data.
    /// It does not execute scene operations, mutate transitions, run effects, show UI or own readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F22D SceneLifecycle/Transition Loading observation adapter; observation only.")]
    internal static class LoadingObservationAdapter
    {
        public const string AdapterSource = "F22D.LoadingObservation";

        public static LoadingStep ObserveSceneLoadStep(
            LoadingStepId stepId,
            SceneLifecycleLoadResult result,
            float weight,
            string source,
            string reason)
        {
            if (!stepId.IsValid)
            {
                throw new ArgumentException("Scene load Loading observation requires a valid step id.", nameof(stepId));
            }

            string message = BuildSceneMessage("load", result.SceneName, result.ScenePath, result.LoadMode, result.Message, reason);
            if (result.Loaded)
            {
                return new LoadingStep(
                    stepId,
                    LoadingStepStatus.Completed,
                    LoadingWeightedProgress.From(weight, 1f),
                    ResolveDisplayName("Scene Load", result.SceneName, result.ScenePath),
                    NormalizeSource(source),
                    message);
            }

            return new LoadingStep(
                stepId,
                LoadingStepStatus.Failed,
                LoadingWeightedProgress.From(weight, 0f),
                ResolveDisplayName("Scene Load", result.SceneName, result.ScenePath),
                NormalizeSource(source),
                message);
        }

        public static LoadingStep ObserveSceneUnloadStep(
            LoadingStepId stepId,
            SceneLifecycleUnloadResult result,
            float weight,
            string source,
            string reason)
        {
            if (!stepId.IsValid)
            {
                throw new ArgumentException("Scene unload Loading observation requires a valid step id.", nameof(stepId));
            }

            string message = BuildSceneMessage("unload", result.SceneName, result.ScenePath, string.Empty, result.Message, reason);
            if (result.Unloaded)
            {
                return new LoadingStep(
                    stepId,
                    LoadingStepStatus.Completed,
                    LoadingWeightedProgress.From(weight, 1f),
                    ResolveDisplayName("Scene Unload", result.SceneName, result.ScenePath),
                    NormalizeSource(source),
                    message);
            }

            if (result.Skipped)
            {
                return new LoadingStep(
                    stepId,
                    LoadingStepStatus.Skipped,
                    LoadingWeightedProgress.From(weight, 1f),
                    ResolveDisplayName("Scene Unload", result.SceneName, result.ScenePath),
                    NormalizeSource(source),
                    message);
            }

            return new LoadingStep(
                stepId,
                LoadingStepStatus.Failed,
                LoadingWeightedProgress.From(weight, 0f),
                ResolveDisplayName("Scene Unload", result.SceneName, result.ScenePath),
                NormalizeSource(source),
                message);
        }

        public static LoadingProgressAggregationResult ObserveSceneLifecycleOperation(
            LoadingOperationId operationId,
            IReadOnlyList<LoadingStep> observedSteps,
            string source,
            string reason)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("SceneLifecycle Loading observation requires a valid operation id.", nameof(operationId));
            }

            return LoadingProgressAggregator.Aggregate(
                operationId,
                observedSteps,
                NormalizeSource(source),
                NormalizeReason(reason),
                "Observed SceneLifecycle results as canonical Loading progress. No scene operation was executed by Loading.");
        }

        public static LoadingStep ObserveTransitionStep(
            LoadingStepId stepId,
            TransitionStep transitionStep,
            float weight,
            string source,
            string reason)
        {
            if (!stepId.IsValid)
            {
                throw new ArgumentException("Transition Loading observation requires a valid step id.", nameof(stepId));
            }

            if (!transitionStep.IsValid)
            {
                throw new ArgumentException("Transition Loading observation requires a valid transition step.", nameof(transitionStep));
            }

            var status = MapTransitionStepStatus(transitionStep.Status);
            float normalizedProgress = GetProgressForStepStatus(status);
            string displayName = transitionStep.HasLabel ? transitionStep.Label : transitionStep.Phase.ToString();
            string message = string.IsNullOrWhiteSpace(transitionStep.Message)
                ? NormalizeReason(reason)
                : transitionStep.Message;

            return new LoadingStep(
                stepId,
                status,
                LoadingWeightedProgress.From(weight, normalizedProgress),
                displayName,
                NormalizeSource(source),
                message);
        }

        public static LoadingProgressAggregationResult ObserveTransitionResult(
            LoadingOperationId operationId,
            TransitionResult result,
            string source,
            string reason)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Loading observation requires a valid operation id.", nameof(operationId));
            }

            if (!result.IsValid)
            {
                throw new ArgumentException("Transition Loading observation requires a valid transition result.", nameof(result));
            }

            IReadOnlyList<TransitionStep> transitionSteps = result.ObservedSteps;
            var loadingSteps = new LoadingStep[transitionSteps.Count];
            for (int i = 0; i < transitionSteps.Count; i++)
            {
                var step = transitionSteps[i];
                loadingSteps[i] = ObserveTransitionStep(
                    LoadingStepId.From($"transition.{result.OperationId.Value.Value}.{i:00}.{step.Phase.ToString().ToLowerInvariant()}"),
                    step,
                    1f,
                    source,
                    reason);
            }

            if (loadingSteps.Length == 0)
            {
                var status = MapTransitionResultToSyntheticStepStatus(result.Status);
                loadingSteps = new[]
                {
                    new LoadingStep(
                        LoadingStepId.From($"transition.{result.OperationId.Value.Value}.result"),
                        status,
                        LoadingWeightedProgress.From(1f, GetProgressForStepStatus(status)),
                        result.Kind.ToString(),
                        NormalizeSource(source),
                        string.IsNullOrWhiteSpace(result.Message) ? NormalizeReason(reason) : result.Message)
                };
            }

            return LoadingProgressAggregator.Aggregate(
                operationId,
                loadingSteps,
                NormalizeSource(source),
                NormalizeReason(reason),
                "Observed Transition result as canonical Loading progress. No Transition operation was executed by Loading.");
        }

        public static LoadingOperation ObserveOperationFromAggregation(
            LoadingOperationId operationId,
            LoadingProgressAggregationResult aggregation,
            string displayName,
            string source,
            string message)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading observation operation requires a valid operation id.", nameof(operationId));
            }

            if (!aggregation.OperationId.Equals(operationId))
            {
                throw new ArgumentException("Loading observation operation id must match the aggregation operation id.", nameof(aggregation));
            }

            return new LoadingOperation(
                operationId,
                MapAggregationStatus(aggregation.Status),
                aggregation.Progress,
                displayName,
                NormalizeSource(source),
                message);
        }

        private static LoadingOperationStatus MapAggregationStatus(LoadingProgressAggregationStatus status)
        {
            switch (status)
            {
                case LoadingProgressAggregationStatus.Running:
                    return LoadingOperationStatus.Running;
                case LoadingProgressAggregationStatus.Completed:
                case LoadingProgressAggregationStatus.CompletedWithSkippedSteps:
                case LoadingProgressAggregationStatus.NoSteps:
                    return LoadingOperationStatus.Completed;
                case LoadingProgressAggregationStatus.Canceled:
                    return LoadingOperationStatus.Canceled;
                case LoadingProgressAggregationStatus.Failed:
                    return LoadingOperationStatus.Failed;
                default:
                    return LoadingOperationStatus.Failed;
            }
        }

        private static LoadingStepStatus MapTransitionStepStatus(TransitionStatus status)
        {
            switch (status)
            {
                case TransitionStatus.Planned:
                    return LoadingStepStatus.Pending;
                case TransitionStatus.Running:
                case TransitionStatus.Observed:
                    return LoadingStepStatus.Running;
                case TransitionStatus.Skipped:
                    return LoadingStepStatus.Skipped;
                case TransitionStatus.Succeeded:
                case TransitionStatus.CompletedWithWarnings:
                    return LoadingStepStatus.Completed;
                case TransitionStatus.Cancelled:
                    return LoadingStepStatus.Canceled;
                case TransitionStatus.Failed:
                case TransitionStatus.Rejected:
                    return LoadingStepStatus.Failed;
                default:
                    return LoadingStepStatus.Failed;
            }
        }

        private static LoadingStepStatus MapTransitionResultToSyntheticStepStatus(TransitionStatus status)
        {
            switch (status)
            {
                case TransitionStatus.Succeeded:
                case TransitionStatus.CompletedWithWarnings:
                    return LoadingStepStatus.Completed;
                case TransitionStatus.Cancelled:
                    return LoadingStepStatus.Canceled;
                case TransitionStatus.Failed:
                case TransitionStatus.Rejected:
                    return LoadingStepStatus.Failed;
                case TransitionStatus.Skipped:
                    return LoadingStepStatus.Skipped;
                case TransitionStatus.Planned:
                    return LoadingStepStatus.Pending;
                case TransitionStatus.Running:
                case TransitionStatus.Observed:
                    return LoadingStepStatus.Running;
                default:
                    return LoadingStepStatus.Failed;
            }
        }

        private static float GetProgressForStepStatus(LoadingStepStatus status)
        {
            switch (status)
            {
                case LoadingStepStatus.Completed:
                case LoadingStepStatus.Skipped:
                    return 1f;
                case LoadingStepStatus.Pending:
                case LoadingStepStatus.Failed:
                case LoadingStepStatus.Canceled:
                    return 0f;
                case LoadingStepStatus.Running:
                    return 0.5f;
                default:
                    return 0f;
            }
        }

        private static string BuildSceneMessage(string verb, string sceneName, string scenePath, string loadMode, string resultMessage, string reason)
        {
            string label = ResolveSceneLabel(sceneName, scenePath);
            string normalizedReason = NormalizeReason(reason);
            string normalizedResultMessage = Normalize(resultMessage);
            string normalizedLoadMode = Normalize(loadMode);
            if (!string.IsNullOrWhiteSpace(normalizedLoadMode))
            {
                return $"Observed SceneLifecycle {verb} for '{label}'. loadMode='{normalizedLoadMode}'. reason='{normalizedReason}'. result='{normalizedResultMessage}'.";
            }

            return $"Observed SceneLifecycle {verb} for '{label}'. reason='{normalizedReason}'. result='{normalizedResultMessage}'.";
        }

        private static string ResolveDisplayName(string prefix, string sceneName, string scenePath)
        {
            return $"{prefix}: {ResolveSceneLabel(sceneName, scenePath)}";
        }

        private static string ResolveSceneLabel(string sceneName, string scenePath)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return "<scene>";
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback(AdapterSource);
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("loading.observation");
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
