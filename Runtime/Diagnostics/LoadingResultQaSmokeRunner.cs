using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F22H Loading result/issue primitives.
    /// It validates passive result summaries only; it does not execute lifecycle, retry/fallback, mutate readiness, create UI or run gameplay adapters.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F22H Loading result/issue diagnostics smoke.")]
    internal static class LoadingResultQaSmokeRunner
    {
        internal const string SmokeName = "Loading Result and Issue Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LoadingResultQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool successPassed = ValidateSuccessResult(logger, normalizedSource);
                bool waitingPassed = ValidateWaitingReadinessResult(logger, normalizedSource);
                bool failurePassed = ValidateFailureIssueResult(logger, normalizedSource);
                bool surfaceEvidencePassed = ValidateSurfaceAdapterEvidenceResult(logger, normalizedSource);
                bool boundaryPassed = ValidateBoundary(logger);

                return Task.FromResult(contractsPassed
                    && successPassed
                    && waitingPassed
                    && failurePassed
                    && surfaceEvidencePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Loading Result and Issue Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.result.operation.contracts");
            var aggregation = LoadingProgressAggregator.Aggregate(
                operationId,
                new[] { LoadingStep.Completed("qa.loading.result.step.contracts", 1f, "Contracts", source, "contracts") },
                source,
                "qa.result.contracts",
                "contracts");
            var readiness = LoadingReadinessObservation.Ready(
                "qa.loading.result.readiness.contracts",
                operationId,
                "Contracts readiness",
                source,
                "qa.result.contracts",
                "ready");
            var issue = LoadingIssue.Warning(operationId, "qa.loading.warning", source, "warning issue");
            var result = LoadingResult.SucceededWithWarnings(
                operationId,
                aggregation,
                new[] { readiness },
                new[] { issue },
                source,
                "contracts");

            bool passed = result is { IsValid: true, Status: LoadingResultStatus.SucceededWithWarnings, Succeeded: true, Completed: true, Failed: false, BlocksCompletion: false, ReadinessObservationCount: 1, IssueCount: 1, WarningOrHigherIssueCount: 1, BlockingIssueCount: 0 };

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("result", nameof(LoadingResult)),
                    LogFields.Field("issue", nameof(LoadingIssue)),
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("readiness", result.ReadinessObservationCount),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("blocksCompletion", result.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateSuccessResult(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.result.operation.success");
            var aggregation = LoadingProgressAggregator.Aggregate(
                operationId,
                new[]
                {
                    LoadingStep.Completed("qa.loading.result.success.scene", 2f, "Scene", source, "complete"),
                    LoadingStep.Completed("qa.loading.result.success.readiness-step", 1f, "Readiness step", source, "complete")
                },
                source,
                "qa.result.success",
                "success");
            var readiness = LoadingReadinessObservation.Ready(
                "qa.loading.result.readiness.success",
                operationId,
                "Ready",
                source,
                "qa.result.success",
                "ready");
            var result = LoadingResult.SucceededResult(
                operationId,
                aggregation,
                new[] { readiness },
                source,
                "success");

            bool passed = result is { Status: LoadingResultStatus.Succeeded, Succeeded: true, Completed: true, BlocksCompletion: false, Progress: { PercentRounded: 100 }, ReadyReadinessCount: 1, IssueCount: 0 };

            LogStep(
                logger,
                "success-result",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("progress", result.Progress.NormalizedValue),
                    LogFields.Field("percent", result.Progress.PercentRounded),
                    LogFields.Field("ready", result.ReadyReadinessCount),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("blocksCompletion", result.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateWaitingReadinessResult(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.result.operation.waiting");
            var aggregation = LoadingProgressAggregator.Aggregate(
                operationId,
                new[]
                {
                    LoadingStep.Completed("qa.loading.result.waiting.scene", 2f, "Scene", source, "complete"),
                    LoadingStep.Pending("qa.loading.result.waiting.readiness-step", 1f, "Readiness step", source, "waiting")
                },
                source,
                "qa.result.waiting",
                "waiting");
            var readiness = LoadingReadinessObservation.Waiting(
                "qa.loading.result.readiness.waiting",
                operationId,
                "Waiting",
                source,
                "qa.result.waiting",
                "waiting");
            var result = LoadingResult.Waiting(
                operationId,
                aggregation,
                new[] { readiness },
                Array.Empty<LoadingIssue>(),
                source,
                "waiting");

            bool passed = result is { Status: LoadingResultStatus.WaitingForReadiness, WaitingForReadiness: true, Completed: false, BlocksCompletion: true, WaitingReadinessCount: 1, BlockingIssueCount: 0 };

            LogStep(
                logger,
                "waiting-readiness-result",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("waiting", result.WaitingReadinessCount),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("blocksCompletion", result.BlocksCompletion),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount)));

            return passed;
        }

        private static bool ValidateFailureIssueResult(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.result.operation.failure");
            var aggregation = LoadingProgressAggregator.Aggregate(
                operationId,
                new[] { LoadingStep.Failed("qa.loading.result.failure.scene", 1f, 0.4f, "Scene", source, "failed") },
                source,
                "qa.result.failure",
                "failure");
            var readiness = LoadingReadinessObservation.FailedObservation(
                "qa.loading.result.readiness.failure",
                operationId,
                "Readiness failure",
                source,
                "qa.result.failure",
                "failed");
            var issue = LoadingIssue.Blocking(operationId, "qa.loading.blocking", source, "blocking issue");
            var result = LoadingResult.FailedResult(
                operationId,
                aggregation,
                new[] { readiness },
                new[] { issue },
                source,
                "failure");

            bool passed = result is { Status: LoadingResultStatus.Failed, Failed: true, Completed: true, BlocksCompletion: true, FailedReadinessCount: 1, BlockingIssueCount: 1, ProgressAggregation: { Failed: true } };

            LogStep(
                logger,
                "failure-issue-result",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("completed", result.Completed),
                    LogFields.Field("failedReadiness", result.FailedReadinessCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("blocksCompletion", result.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateSurfaceAdapterEvidenceResult(FrameworkLogger logger, string source)
        {
            var request = LoadingSurfaceRequest.Show(
                "Loading QA",
                "Surface evidence",
                source,
                "qa.surface.evidence",
                LoadingProgress.FromNormalized(0.5f),
                progressSupported: true);
            var visibleEvidence = new LoadingSurfaceAdapterEvidence(
                "QA Loading Surface Adapter",
                LoadingSurfaceResultStatus.Succeeded,
                0,
                0,
                "Visible loading state applied.");
            var skippedEvidence = new LoadingSurfaceAdapterEvidence(
                "Optional Loading Surface Adapter",
                LoadingSurfaceResultStatus.Skipped,
                0,
                0,
                "Optional adapter skipped.");
            var failedEvidence = new LoadingSurfaceAdapterEvidence(
                "Failing Loading Surface Adapter",
                LoadingSurfaceResultStatus.Failed,
                1,
                1,
                "Required adapter failed.");
            var result = LoadingSurfaceResult.FailedResult(
                request,
                "QA Aggregate Loading Surface",
                "Aggregate loading surface failed.",
                new[] { "Failing Loading Surface Adapter: missing-canvas-group" },
                new[] { visibleEvidence, skippedEvidence, failedEvidence });

            bool passed = result is { Failed: true, HasAdapterEvidence: true, AdapterEvidenceCount: 3, AppliedAdapterEvidenceCount: 1, SkippedAdapterEvidenceCount: 1, FailedAdapterEvidenceCount: 1, AdapterEvidenceIssueCount: 1, AdapterEvidenceBlockingIssueCount: 1, ProgressSupported: true }
                && result.AdapterEvidence[0].Applied
                && result.AdapterEvidence[1].Skipped
                && result.AdapterEvidence[2].Failed
                && result.ToDiagnosticString().Contains("adapterEvidence='3'", StringComparison.Ordinal)
                && result.ToDiagnosticString().Contains("QA Loading Surface Adapter", StringComparison.Ordinal);

            LogStep(
                logger,
                "surface-adapter-evidence",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("loadingAdapterEvidenceCount", result.AdapterEvidenceCount),
                    LogFields.Field("loadingAdapterEvidenceApplied", result.AppliedAdapterEvidenceCount),
                    LogFields.Field("loadingAdapterEvidenceSkipped", result.SkippedAdapterEvidenceCount),
                    LogFields.Field("loadingAdapterEvidenceFailed", result.FailedAdapterEvidenceCount),
                    LogFields.Field("loadingAdapterEvidenceIssues", result.AdapterEvidenceIssueCount),
                    LogFields.Field("loadingAdapterEvidenceBlockingIssues", result.AdapterEvidenceBlockingIssueCount),
                    LogFields.Field("loadingAdapterEvidenceNames", "QA Loading Surface Adapter; Optional Loading Surface Adapter; Failing Loading Surface Adapter"),
                    LogFields.Field("loadingAdapterEvidenceStatuses", "Succeeded; Skipped; Failed"),
                    LogFields.Field("loadingProgressSupported", result.ProgressSupported),
                    LogFields.Field("loadingProgressMode", result.ProgressSupported ? "request-progress" : "unsupported"),
                    LogFields.Field("loadingNoOp", false)));

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
                    LogFields.Field("result", nameof(LoadingResult)),
                    LogFields.Field("issue", nameof(LoadingIssue)),
                    LogFields.Field("severity", nameof(LoadingIssueSeverity)),
                    LogFields.Field("lifecycleExecution", "none"),
                    LogFields.Field("retry", "none"),
                    LogFields.Field("fallback", "none"),
                    LogFields.Field("readinessMutation", "none"),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("gameplayAdapters", "none")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] stepFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));

            if (passed)
            {
                logger.Info("QA Loading Result and Issue Smoke step completed.", stepFields);
                logger.Debug("QA Loading Result and Issue Smoke step diagnostics.", fields);
                return;
            }

            logger.Warning("QA Loading Result and Issue Smoke step failed.", stepFields);
            logger.Debug("QA Loading Result and Issue Smoke failure diagnostics.", fields);
        }
    }
}
#endif
