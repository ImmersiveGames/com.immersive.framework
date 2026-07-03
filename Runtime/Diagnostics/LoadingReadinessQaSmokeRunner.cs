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
    /// API status: Development Tooling. Synthetic smoke for F22G Loading readiness observation primitives.
    /// It validates passive readiness records only; it does not wait, execute lifecycle, mutate readiness, create UI or run gameplay adapters.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F22G Loading readiness observation diagnostics smoke.")]
    internal static class LoadingReadinessQaSmokeRunner
    {
        internal const string SmokeName = "Loading Readiness Observation Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LoadingReadinessQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool waitingPassed = ValidateWaitingObservation(logger, normalizedSource);
                bool readyPassed = ValidateReadyObservation(logger, normalizedSource);
                bool blockedPassed = ValidateBlockedObservation(logger, normalizedSource);
                bool failedPassed = ValidateFailedObservation(logger, normalizedSource);
                bool boundaryPassed = ValidateBoundary(logger);

                return Task.FromResult(contractsPassed
                    && waitingPassed
                    && readyPassed
                    && blockedPassed
                    && failedPassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Loading Readiness Observation Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.readiness.operation.contracts");
            var observationId = LoadingReadinessObservationId.From("qa.loading.readiness.observation.contracts");
            var observation = new LoadingReadinessObservation(
                observationId,
                operationId,
                LoadingReadinessStatus.Waiting,
                "QA Readiness Contracts",
                source,
                "contracts",
                "passive readiness contract");

            bool passed = operationId.Domain == FrameworkIdentityDomain.Loading
                && observationId.Domain == FrameworkIdentityDomain.Loading
                && observation is { IsValid: true, IsWaiting: true, BlocksCompletion: true, IsReady: false }
                && observation.OperationId == operationId;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("operation", operationId.StableText),
                    LogFields.Field("observation", observationId.StableText),
                    LogFields.Field("domain", observationId.Domain.ToString()),
                    LogFields.Field("status", observation.Status.ToString()),
                    LogFields.Field("blocksCompletion", observation.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateWaitingObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.readiness.operation.waiting");
            var observation = LoadingReadinessObservation.Waiting(
                "qa.loading.readiness.waiting",
                operationId,
                "Scene readiness",
                source,
                "qa.readiness.waiting",
                "Waiting for content readiness observation.");

            bool passed = observation is { Status: LoadingReadinessStatus.Waiting, IsWaiting: true, BlocksCompletion: true, IsTerminal: false, IsReady: false };

            LogStep(
                logger,
                "waiting-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("status", observation.Status.ToString()),
                    LogFields.Field("waiting", observation.IsWaiting),
                    LogFields.Field("ready", observation.IsReady),
                    LogFields.Field("terminal", observation.IsTerminal),
                    LogFields.Field("blocksCompletion", observation.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateReadyObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.readiness.operation.ready");
            var observation = LoadingReadinessObservation.Ready(
                "qa.loading.readiness.ready",
                operationId,
                "Activity readiness",
                source,
                "qa.readiness.ready",
                "Readiness observed as ready.");

            bool passed = observation is { Status: LoadingReadinessStatus.Ready, IsReady: true, IsTerminal: true, BlocksCompletion: false, Failed: false };

            LogStep(
                logger,
                "ready-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("status", observation.Status.ToString()),
                    LogFields.Field("ready", observation.IsReady),
                    LogFields.Field("terminal", observation.IsTerminal),
                    LogFields.Field("blocksCompletion", observation.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateBlockedObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.readiness.operation.blocked");
            var observation = LoadingReadinessObservation.Blocked(
                "qa.loading.readiness.blocked",
                operationId,
                "Required readiness",
                source,
                "qa.readiness.blocked",
                "Required readiness is blocked.");

            bool passed = observation is { Status: LoadingReadinessStatus.Blocked, IsBlocked: true, BlocksCompletion: true, IsTerminal: true, IsReady: false };

            LogStep(
                logger,
                "blocked-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("status", observation.Status.ToString()),
                    LogFields.Field("blocked", observation.IsBlocked),
                    LogFields.Field("ready", observation.IsReady),
                    LogFields.Field("terminal", observation.IsTerminal),
                    LogFields.Field("blocksCompletion", observation.BlocksCompletion)));

            return passed;
        }

        private static bool ValidateFailedObservation(FrameworkLogger logger, string source)
        {
            var operationId = LoadingOperationId.From("qa.loading.readiness.operation.failed");
            var observation = LoadingReadinessObservation.FailedObservation(
                "qa.loading.readiness.failed",
                operationId,
                "Readiness failure",
                source,
                "qa.readiness.failed",
                "Readiness observation failed.");

            bool passed = observation is { Status: LoadingReadinessStatus.Failed, Failed: true, IsTerminal: true, IsReady: false, BlocksCompletion: false };

            LogStep(
                logger,
                "failed-observation",
                passed,
                LogFields.Of(
                    LogFields.Field("status", observation.Status.ToString()),
                    LogFields.Field("failed", observation.Failed),
                    LogFields.Field("ready", observation.IsReady),
                    LogFields.Field("terminal", observation.IsTerminal),
                    LogFields.Field("blocksCompletion", observation.BlocksCompletion)));

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
                    LogFields.Field("readinessObservation", nameof(LoadingReadinessObservation)),
                    LogFields.Field("readinessStatus", nameof(LoadingReadinessStatus)),
                    LogFields.Field("lifecycleExecution", "none"),
                    LogFields.Field("readinessMutation", "none"),
                    LogFields.Field("sceneLifecycle", "observed-only"),
                    LogFields.Field("transition", "observed-only"),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("gameplayAdapters", "none")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, params LogField[] fields)
        {
            logger.Info(
                "QA Loading Readiness Observation Smoke step completed.",
                Merge(
                    LogFields.Field("step", step),
                    LogFields.Field("passed", passed),
                    fields));
        }

        private static LogField[] Merge(LogField first, LogField second, params LogField[] rest)
        {
            var merged = new LogField[rest.Length + 2];
            merged[0] = first;
            merged[1] = second;
            for (int i = 0; i < rest.Length; i++)
            {
                merged[i + 2] = rest[i];
            }

            return merged;
        }
    }
}
#endif
