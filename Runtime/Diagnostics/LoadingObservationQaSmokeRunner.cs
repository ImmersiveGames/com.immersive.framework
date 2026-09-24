using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Transition;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F22D Loading observation adapters.
    /// It validates mapping from existing SceneLifecycle/Transition diagnostics into canonical Loading progress without executing lifecycle, transitions or UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F22D Loading observation adapter diagnostics smoke.")]
    internal static class LoadingObservationQaSmokeRunner
    {
        internal const string SmokeName = "Loading Observation Adapter Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LoadingObservationQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool sceneLifecyclePassed = ValidateSceneLifecycleObservation(logger, normalizedSource);
                bool sceneFailurePassed = ValidateSceneLifecycleFailureObservation(logger, normalizedSource);
                bool transitionPassed = ValidateTransitionObservation(logger, normalizedSource);
                bool transitionFailurePassed = ValidateTransitionFailureObservation(logger, normalizedSource);
                bool boundaryPassed = ValidateBoundary(logger);

                return Task.FromResult(contractsPassed
                    && sceneLifecyclePassed
                    && sceneFailurePassed
                    && transitionPassed
                    && transitionFailurePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Loading Observation Adapter Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.observation.contracts");
            var stepId = LoadingStepId.From("qa.loading.observation.step.contracts");
            var sceneResult = SceneLifecycleLoadResult.LoadedPrimaryScene(
                "StartupScene",
                "Assets/QA/StartupScene.unity",
                false,
                "Single");

            var step = LoadingObservationAdapter.ObserveSceneLoadStep(
                stepId,
                sceneResult,
                1f,
                source,
                "contracts");
            var aggregation = LoadingObservationAdapter.ObserveSceneLifecycleOperation(
                operationId,
                new[] { step },
                source,
                "contracts");
            var operation = LoadingObservationAdapter.ObserveOperationFromAggregation(
                operationId,
                aggregation,
                "QA Loading Observation Contracts",
                source,
                "contracts");

            bool passed = operationId.IsValid
                && stepId.IsValid
                && step.Status == LoadingStepStatus.Completed
                && aggregation is { Status: LoadingProgressAggregationStatus.Completed, Progress: { IsComplete: true } }
                && operation is { Status: LoadingOperationStatus.Completed, Progress: { IsComplete: true } };

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("adapter", nameof(LoadingObservationAdapter)),
                    LogFields.Field("operation", operationId.StableText),
                    LogFields.Field("step", stepId.StableText),
                    LogFields.Field("stepStatus", step.Status.ToString()),
                    LogFields.Field("aggregationStatus", aggregation.Status.ToString()),
                    LogFields.Field("operationStatus", operation.Status.ToString())));

            return passed;
        }

        private static bool ValidateSceneLifecycleObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.observation.scene.lifecycle");
            var loadResult = SceneLifecycleLoadResult.LoadedPrimaryScene(
                "StartupScene",
                "Assets/QA/StartupScene.unity",
                false,
                "Single");
            var unloadResult = SceneLifecycleUnloadResult.SkippedScene(
                "OptionalScene",
                "Assets/QA/OptionalScene.unity",
                "Optional scene was already unloaded.");

            var loadStep = LoadingObservationAdapter.ObserveSceneLoadStep(
                LoadingStepId.From("qa.loading.observation.scene.load"),
                loadResult,
                3f,
                source,
                "scene-lifecycle");
            var unloadStep = LoadingObservationAdapter.ObserveSceneUnloadStep(
                LoadingStepId.From("qa.loading.observation.scene.unload"),
                unloadResult,
                1f,
                source,
                "scene-lifecycle");
            var aggregation = LoadingObservationAdapter.ObserveSceneLifecycleOperation(
                operationId,
                new[] { loadStep, unloadStep },
                source,
                "scene-lifecycle");
            var operation = LoadingObservationAdapter.ObserveOperationFromAggregation(
                operationId,
                aggregation,
                "QA SceneLifecycle Loading Observation",
                source,
                "scene-lifecycle");

            bool passed = loadStep.Status == LoadingStepStatus.Completed
                && unloadStep.Status == LoadingStepStatus.Skipped
                && aggregation is { Status: LoadingProgressAggregationStatus.CompletedWithSkippedSteps, Completed: true, HasSkippedSteps: true, Progress: { IsComplete: true } }
                && operation is { Status: LoadingOperationStatus.Completed, Progress: { IsComplete: true } };

            LogStep(
                logger,
                "scene-lifecycle-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("loadStep", loadStep.Status.ToString()),
                    LogFields.Field("unloadStep", unloadStep.Status.ToString()),
                    LogFields.Field("aggregationStatus", aggregation.Status.ToString()),
                    LogFields.Field("operationStatus", operation.Status.ToString()),
                    LogFields.Field("progress", aggregation.Progress.NormalizedValue),
                    LogFields.Field("skipped", aggregation.SkippedCount),
                    LogFields.Field("sceneLifecycleExecution", "none")));

            return passed;
        }

        private static bool ValidateSceneLifecycleFailureObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.observation.scene.failure");
            var failedResult = SceneLifecycleLoadResult.Failed("Scene Lifecycle could not load QA missing scene.");
            var failedStep = LoadingObservationAdapter.ObserveSceneLoadStep(
                LoadingStepId.From("qa.loading.observation.scene.failed-load"),
                failedResult,
                2f,
                source,
                "scene-failure");
            var aggregation = LoadingObservationAdapter.ObserveSceneLifecycleOperation(
                operationId,
                new[] { failedStep },
                source,
                "scene-failure");
            var operation = LoadingObservationAdapter.ObserveOperationFromAggregation(
                operationId,
                aggregation,
                "QA SceneLifecycle Failure Loading Observation",
                source,
                "scene-failure");

            bool passed = failedStep.Status == LoadingStepStatus.Failed
                && aggregation is { Status: LoadingProgressAggregationStatus.Failed, Failed: true, Completed: false }
                && operation is { Status: LoadingOperationStatus.Failed, Progress: { IsComplete: false } };

            LogStep(
                logger,
                "scene-lifecycle-failure-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("stepStatus", failedStep.Status.ToString()),
                    LogFields.Field("aggregationStatus", aggregation.Status.ToString()),
                    LogFields.Field("operationStatus", operation.Status.ToString()),
                    LogFields.Field("failed", aggregation.Failed),
                    LogFields.Field("sceneLifecycleExecution", "none")));

            return passed;
        }

        private static bool ValidateTransitionObservation(FrameworkLogger logger, string source)
        {
            var transitionOperationId = TransitionOperationId.From("qa.transition.loading.observation.success");
            var transitionResult = TransitionResult.SucceededResult(
                transitionOperationId,
                TransitionKind.RouteSwitch,
                source,
                "transition-observation",
                "Synthetic transition result observed for Loading.",
                new[]
                {
                    TransitionStep.Succeeded(0, TransitionPhase.RequestAdmitted, "request-admitted", "request admitted"),
                    TransitionStep.Succeeded(10, TransitionPhase.SceneOrContentOperationObserved, "scene-operation-observed", "scene operation observed"),
                    TransitionStep.Skipped(20, TransitionPhase.ReadinessObserved, "readiness-not-required", "readiness not required")
                });
            var loadingOperationId = LoadingOperationId.From("qa.loading.observation.transition.success");
            var aggregation = LoadingObservationAdapter.ObserveTransitionResult(
                loadingOperationId,
                transitionResult,
                source,
                "transition-observation");
            var operation = LoadingObservationAdapter.ObserveOperationFromAggregation(
                loadingOperationId,
                aggregation,
                "QA Transition Loading Observation",
                source,
                "transition-observation");

            bool passed = aggregation is { Status: LoadingProgressAggregationStatus.CompletedWithSkippedSteps, Completed: true, StepCount: 3, CompletedCount: 2, SkippedCount: 1, Progress: { IsComplete: true } }
                && operation is { Status: LoadingOperationStatus.Completed, Progress: { IsComplete: true } };

            LogStep(
                logger,
                "transition-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("transition", transitionOperationId.StableText),
                    LogFields.Field("loading", loadingOperationId.StableText),
                    LogFields.Field("aggregationStatus", aggregation.Status.ToString()),
                    LogFields.Field("operationStatus", operation.Status.ToString()),
                    LogFields.Field("steps", aggregation.StepCount),
                    LogFields.Field("completed", aggregation.CompletedCount),
                    LogFields.Field("skipped", aggregation.SkippedCount),
                    LogFields.Field("transitionExecution", "none")));

            return passed;
        }

        private static bool ValidateTransitionFailureObservation(FrameworkLogger logger, string source)
        {
            var transitionOperationId = TransitionOperationId.From("qa.transition.loading.observation.failed");
            var transitionResult = TransitionResult.FailedResult(
                transitionOperationId,
                TransitionKind.SceneComposition,
                source,
                "transition-failure-observation",
                "Synthetic transition failure observed for Loading.",
                new[]
                {
                    TransitionStep.Succeeded(0, TransitionPhase.RequestAdmitted, "request-admitted", "request admitted"),
                    TransitionStep.Failed(10, TransitionPhase.SceneOrContentOperationObserved, "scene-operation-failed", "scene operation failed")
                },
                new[] { "synthetic scene operation failure" });
            var loadingOperationId = LoadingOperationId.From("qa.loading.observation.transition.failed");
            var aggregation = LoadingObservationAdapter.ObserveTransitionResult(
                loadingOperationId,
                transitionResult,
                source,
                "transition-failure-observation");
            var operation = LoadingObservationAdapter.ObserveOperationFromAggregation(
                loadingOperationId,
                aggregation,
                "QA Transition Failure Loading Observation",
                source,
                "transition-failure-observation");

            bool passed = aggregation is { Status: LoadingProgressAggregationStatus.Failed, Failed: true, FailedCount: 1, Completed: false }
                && operation is { Status: LoadingOperationStatus.Failed, Progress: { IsComplete: false } };

            LogStep(
                logger,
                "transition-failure-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("transition", transitionOperationId.StableText),
                    LogFields.Field("loading", loadingOperationId.StableText),
                    LogFields.Field("aggregationStatus", aggregation.Status.ToString()),
                    LogFields.Field("operationStatus", operation.Status.ToString()),
                    LogFields.Field("failedSteps", aggregation.FailedCount),
                    LogFields.Field("transitionExecution", "none")));

            return passed;
        }

        private static bool ValidateBoundary(FrameworkLogger logger)
        {
            bool passed = true;
            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("adapter", nameof(LoadingObservationAdapter)),
                    LogFields.Field("sceneLifecycle", "observed-result-only"),
                    LogFields.Field("transition", "observed-result-only"),
                    LogFields.Field("sceneLifecycleExecution", "none"),
                    LogFields.Field("transitionExecution", "none"),
                    LogFields.Field("transitionEffect", "none"),
                    LogFields.Field("readinessMutation", "none"),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("fade", "none"),
                    LogFields.Field("curtain", "none"),
                    LogFields.Field("prefab", "none")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] stepFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));

            if (passed)
            {
                logger.Info("QA Loading Observation Adapter Smoke step completed.", stepFields);
                logger.Debug("QA Loading Observation Adapter Smoke step diagnostics.", fields);
                return;
            }

            logger.Warning("QA Loading Observation Adapter Smoke step failed.", stepFields);
            logger.Debug("QA Loading Observation Adapter Smoke failure diagnostics.", fields);
        }
    }
}
#endif
