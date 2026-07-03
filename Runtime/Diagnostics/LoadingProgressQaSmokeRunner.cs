using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Loading;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F22C Loading progress aggregation.
    /// It validates pure weighted progress aggregation only; it does not execute SceneLifecycle, Transition, UI or readiness mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F22C Loading progress aggregation diagnostics smoke.")]
    internal static class LoadingProgressQaSmokeRunner
    {
        internal const string SmokeName = "Loading Progress Aggregation Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LoadingProgressQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool weightedRunningPassed = ValidateWeightedRunningAggregation(logger, normalizedSource);
                bool completedSkippedPassed = ValidateCompletedWithSkippedAggregation(logger, normalizedSource);
                bool failedPassed = ValidateFailedAggregation(logger, normalizedSource);
                bool emptyPassed = ValidateNoStepsAggregation(logger, normalizedSource);
                bool boundaryPassed = ValidateBoundary(logger);

                return Task.FromResult(contractsPassed
                    && weightedRunningPassed
                    && completedSkippedPassed
                    && failedPassed
                    && emptyPassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Loading Progress Aggregation Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.operation.contracts");
            var stepId = LoadingStepId.From("qa.loading.step.contracts");
            var progress = LoadingProgress.FromPercent(25f);
            var weight = LoadingStepWeight.From(4f);
            var weightedProgress = new LoadingWeightedProgress(weight, progress);
            var step = new LoadingStep(
                stepId,
                LoadingStepStatus.Running,
                weightedProgress,
                "QA Loading Contract Step",
                source,
                "contracts");
            var operation = new LoadingOperation(
                operationId,
                LoadingOperationStatus.Running,
                progress,
                "QA Loading Contract Operation",
                source,
                "contracts");
            var result = LoadingProgressAggregator.Aggregate(
                operationId,
                new[] { step },
                source,
                "qa.loading.contracts",
                "contracts");

            bool passed = operationId.Domain == FrameworkIdentityDomain.Loading
                && stepId.Domain == FrameworkIdentityDomain.Loading
                && operation.IsValid
                && step.IsValid
                && weightedProgress.WeightedCompleted.Equals(1f)
                && weightedProgress.WeightedTotal.Equals(4f)
                && result.OperationId == operationId
                && result is { Status: LoadingProgressAggregationStatus.Running, StepCount: 1, Progress: { PercentRounded: 25 } };

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("operation", operationId.StableText),
                    LogFields.Field("step", stepId.StableText),
                    LogFields.Field("domain", operationId.Domain.ToString()),
                    LogFields.Field("aggregator", nameof(LoadingProgressAggregator)),
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("progress", result.Progress.NormalizedValue),
                    LogFields.Field("percent", result.Progress.PercentRounded)));

            return passed;
        }

        private static bool ValidateWeightedRunningAggregation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.operation.weighted-running");
            LoadingStep[] steps = new[]
            {
                LoadingStep.Completed("qa.loading.step.bootstrap", 2f, "Bootstrap", source, "synthetic-complete"),
                LoadingStep.Running("qa.loading.step.scene", 3f, 0.5f, "Scene", source, "synthetic-running"),
                LoadingStep.Pending("qa.loading.step.readiness", 5f, "Readiness", source, "synthetic-pending")
            };

            var result = LoadingProgressAggregator.Aggregate(
                operationId,
                steps,
                source,
                "qa.loading.weighted-running",
                "weighted-running");

            float expectedProgress = 0.35f;
            bool passed = result is { Status: LoadingProgressAggregationStatus.Running, IsRunning: true, Completed: false, Failed: false, BlocksCompletion: true, StepCount: 3, CompletedCount: 1, RunningCount: 1, PendingCount: 1 }
                && NearlyEqual(result.WeightedCompleted, 3.5f)
                && NearlyEqual(result.WeightedTotal, 10f)
                && NearlyEqual(result.Progress.NormalizedValue, expectedProgress)
                && result.Progress.PercentRounded == 35;

            LogStep(
                logger,
                "weighted-running",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("steps", result.StepCount),
                    LogFields.Field("completed", result.CompletedCount),
                    LogFields.Field("running", result.RunningCount),
                    LogFields.Field("pending", result.PendingCount),
                    LogFields.Field("weightedCompleted", result.WeightedCompleted),
                    LogFields.Field("weightedTotal", result.WeightedTotal),
                    LogFields.Field("progress", result.Progress.NormalizedValue),
                    LogFields.Field("percent", result.Progress.PercentRounded),
                    LogFields.Field("blocksCompletion", result.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateCompletedWithSkippedAggregation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.operation.completed-skipped");
            LoadingStep[] steps = new[]
            {
                LoadingStep.Completed("qa.loading.step.scene", 2f, "Scene", source, "synthetic-complete"),
                CreateSkippedStep("qa.loading.step.optional-ui", 1f, source),
                LoadingStep.Completed("qa.loading.step.readiness", 1f, "Readiness", source, "synthetic-complete")
            };

            var result = LoadingProgressAggregator.Aggregate(
                operationId,
                steps,
                source,
                "qa.loading.completed-skipped",
                "completed-with-skipped");

            bool passed = result is { Status: LoadingProgressAggregationStatus.CompletedWithSkippedSteps, Completed: true, IsTerminal: true, Failed: false, BlocksCompletion: false, StepCount: 3, CompletedCount: 2, SkippedCount: 1, HasSkippedSteps: true, Progress: { IsComplete: true, PercentRounded: 100 } };

            LogStep(
                logger,
                "completed-with-skipped",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("steps", result.StepCount),
                    LogFields.Field("completed", result.CompletedCount),
                    LogFields.Field("skipped", result.SkippedCount),
                    LogFields.Field("progress", result.Progress.NormalizedValue),
                    LogFields.Field("percent", result.Progress.PercentRounded),
                    LogFields.Field("terminal", result.IsTerminal)));

            return passed;
        }

        private static bool ValidateFailedAggregation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.operation.failed");
            LoadingStep[] steps = new[]
            {
                LoadingStep.Completed("qa.loading.step.bootstrap", 1f, "Bootstrap", source, "synthetic-complete"),
                LoadingStep.Failed("qa.loading.step.scene", 4f, 0.25f, "Scene", source, "synthetic-failed")
            };

            var result = LoadingProgressAggregator.Aggregate(
                operationId,
                steps,
                source,
                "qa.loading.failed",
                "failed");

            bool passed = result is { Status: LoadingProgressAggregationStatus.Failed, Failed: true, IsTerminal: true, Completed: false, FailedCount: 1, CompletedCount: 1 }
                && NearlyEqual(result.WeightedCompleted, 2f)
                && NearlyEqual(result.WeightedTotal, 5f)
                && NearlyEqual(result.Progress.NormalizedValue, 0.4f);

            LogStep(
                logger,
                "failed-aggregation",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("terminal", result.IsTerminal),
                    LogFields.Field("failedSteps", result.FailedCount),
                    LogFields.Field("weightedCompleted", result.WeightedCompleted),
                    LogFields.Field("weightedTotal", result.WeightedTotal),
                    LogFields.Field("progress", result.Progress.NormalizedValue)));

            return passed;
        }

        private static bool ValidateNoStepsAggregation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.operation.empty");
            var result = LoadingProgressAggregator.Aggregate(
                operationId,
                Array.Empty<LoadingStep>(),
                source,
                "qa.loading.empty",
                "empty");

            bool passed = result is { Status: LoadingProgressAggregationStatus.NoSteps, Completed: true, IsTerminal: true, HasSteps: false, StepCount: 0 }
                && result.Progress == LoadingProgress.Zero
                && result.WeightedCompleted.Equals(0f)
                && result.WeightedTotal.Equals(0f);

            LogStep(
                logger,
                "no-steps",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("terminal", result.IsTerminal),
                    LogFields.Field("steps", result.StepCount),
                    LogFields.Field("progress", result.Progress.NormalizedValue),
                    LogFields.Field("weightedCompleted", result.WeightedCompleted),
                    LogFields.Field("weightedTotal", result.WeightedTotal)));

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
                    LogFields.Field("aggregator", nameof(LoadingProgressAggregator)),
                    LogFields.Field("result", nameof(LoadingProgressAggregationResult)),
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

        private static LoadingStep CreateSkippedStep(string stepId, float weight, string source)
        {
            return new LoadingStep(
                LoadingStepId.From(stepId),
                LoadingStepStatus.Skipped,
                LoadingWeightedProgress.From(weight, 1f),
                "Optional skipped step",
                source,
                "synthetic-skipped");
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] stepFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));

            if (passed)
            {
                logger.Info("QA Loading Progress Aggregation Smoke step completed.", stepFields);
                logger.Debug("QA Loading Progress Aggregation Smoke step diagnostics.", fields);
                return;
            }

            logger.Warning("QA Loading Progress Aggregation Smoke step failed.", stepFields);
            logger.Debug("QA Loading Progress Aggregation Smoke failure diagnostics.", fields);
        }
    }
}
#endif
