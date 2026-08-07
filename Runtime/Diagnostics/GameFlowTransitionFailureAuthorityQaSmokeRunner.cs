using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Transition;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Development tooling smoke for IF-TXN-01 GameFlow Transition Failure Authority.
    /// Validates phase acceptance and typed pre-commit vs committed-target reveal terminals without executing lifecycle.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "IF-TXN-01 GameFlow Transition Failure Authority smoke; synthetic decision/result proof.")]
    internal static class GameFlowTransitionFailureAuthorityQaSmokeRunner
    {
        internal const string SmokeName = "GameFlow Transition Failure Authority Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(GameFlowTransitionFailureAuthorityQaSmokeRunner));

            bool beforeFailedNotAccepted = ValidateBeforeFailedNotAccepted(logger, normalizedSource);
            bool afterFailedNotAccepted = ValidateAfterFailedNotAccepted(logger, normalizedSource);
            bool warningsAccepted = ValidateCompletedWithWarningsAccepted(logger, normalizedSource);
            bool skippedAccepted = ValidateSkippedAccepted(logger, normalizedSource);
            bool typedTerminals = ValidateTypedTerminals(logger);
            bool readinessKindsPreserved = ValidateReadinessKindsPreserved(logger);

            return Task.FromResult(
                beforeFailedNotAccepted &&
                afterFailedNotAccepted &&
                warningsAccepted &&
                skippedAccepted &&
                typedTerminals &&
                readinessKindsPreserved);
        }

        private static bool ValidateBeforeFailedNotAccepted(FrameworkLogger logger, string source)
        {
            TransitionResult failedBefore = TransitionResult.FailedResult(
                TransitionOperationId.From("qa.if-txn-01.before.failed"),
                TransitionKind.RouteSwitch,
                source,
                "before",
                "required surface missing",
                new[]
                {
                    TransitionStep.Failed(
                        0,
                        TransitionPhase.OperationOpened,
                        "before",
                        "required surface missing")
                },
                new[] { "required surface missing" });

            bool passed =
                failedBefore.Failed &&
                !failedBefore.Completed &&
                !GameFlowRuntime.IsAcceptedTransitionPhase(failedBefore) &&
                !GameFlowRuntime.TryAcceptTransitionPhase(failedBefore, "Before", out string issue) &&
                issue.IndexOf("Before", System.StringComparison.Ordinal) >= 0;

            LogStep(logger, "before-failed-not-accepted", passed, failedBefore);
            return passed;
        }

        private static bool ValidateAfterFailedNotAccepted(FrameworkLogger logger, string source)
        {
            TransitionResult failedAfter = TransitionResult.FailedResult(
                TransitionOperationId.From("qa.if-txn-01.after.failed"),
                TransitionKind.RouteSwitch,
                source,
                "after",
                "reveal adapter blocked",
                new[]
                {
                    TransitionStep.Failed(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "reveal adapter blocked")
                },
                new[] { "reveal adapter blocked" });

            bool passed =
                failedAfter.Failed &&
                !failedAfter.Completed &&
                !GameFlowRuntime.IsAcceptedTransitionPhase(failedAfter);

            LogStep(logger, "after-failed-not-accepted", passed, failedAfter);
            return passed;
        }

        private static bool ValidateCompletedWithWarningsAccepted(FrameworkLogger logger, string source)
        {
            TransitionResult warnings = TransitionResult.CompletedWithWarningsResult(
                TransitionOperationId.From("qa.if-txn-01.warnings"),
                TransitionKind.RouteSwitch,
                source,
                "after",
                "completed with warnings",
                new[]
                {
                    TransitionStep.Succeeded(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "ok")
                },
                new[] { "non-blocking" });

            bool passed =
                warnings.CompletedWithWarnings &&
                warnings.Completed &&
                GameFlowRuntime.IsAcceptedTransitionPhase(warnings);

            LogStep(logger, "completed-with-warnings-accepted", passed, warnings);
            return passed;
        }

        private static bool ValidateSkippedAccepted(FrameworkLogger logger, string source)
        {
            TransitionResult skipped = TransitionResult.SkippedResult(
                TransitionOperationId.From("qa.if-txn-01.skipped"),
                TransitionKind.ActivitySwitch,
                source,
                "before",
                "SkippedByActivityPolicy",
                new[]
                {
                    TransitionStep.Skipped(
                        0,
                        TransitionPhase.OperationOpened,
                        "activity-before-policy-skip",
                        "skipped")
                });

            bool passed =
                skipped.Status == TransitionStatus.Skipped &&
                !skipped.Completed &&
                GameFlowRuntime.IsAcceptedTransitionPhase(skipped);

            LogStep(logger, "skipped-accepted", passed, skipped);
            return passed;
        }

        private static bool ValidateTypedTerminals(FrameworkLogger logger)
        {
            var preCommit = FrameworkRouteRequestResult.FailedPreCommitTransition(
                "pre-commit",
                null,
                "qa",
                "before");
            var reveal = FrameworkRouteRequestResult.FailedCommittedTargetReveal(
                "reveal",
                null,
                "qa",
                "after",
                default);
            var activityReveal = FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                "reveal",
                null,
                "qa",
                "after",
                default);
            var startupReveal = FrameworkGameFlowStartResult.FailedCommittedTargetReveal(
                "reveal",
                null,
                default,
                ActivityEntryReadinessExecutionStatus.Ready);

            bool passed =
                preCommit.Kind == FrameworkRouteRequestKind.FailedPreCommitTransition &&
                !preCommit.Succeeded &&
                !preCommit.DestinationAuthoritative &&
                reveal.Kind == FrameworkRouteRequestKind.FailedCommittedTargetReveal &&
                !reveal.Succeeded &&
                reveal.DestinationAuthoritative &&
                activityReveal.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReveal &&
                activityReveal.DestinationAuthoritative &&
                !activityReveal.Succeeded &&
                startupReveal.CommittedTargetRevealFailed &&
                startupReveal.DestinationAuthoritative &&
                !startupReveal.Started &&
                reveal.Kind != FrameworkRouteRequestKind.FailedCommittedTargetNotReady;

            LogBoolStep(logger, "typed-terminals", passed);
            return passed;
        }

        private static bool ValidateReadinessKindsPreserved(FrameworkLogger logger)
        {
            var notReady = FrameworkActivityRequestResult.FailedCommittedTargetNotReady(
                "not ready",
                null,
                "qa",
                "readiness",
                default);
            var cancelled = FrameworkActivityRequestResult.FailedCommittedTargetReadinessCancelled(
                "cancelled",
                null,
                "qa",
                "readiness",
                default);
            var invalidated = FrameworkActivityRequestResult.FailedCommittedTargetReadinessInvalidated(
                "invalidated",
                null,
                "qa",
                "readiness",
                default);
            var superseded = FrameworkActivityRequestResult.SupersededCommittedTargetByRouteReplacement(
                "superseded",
                null,
                "qa",
                "RouteAuthorityReplaced",
                default);

            bool passed =
                notReady.Kind == FrameworkActivityRequestKind.FailedCommittedTargetNotReady &&
                notReady.DestinationAuthoritative &&
                !notReady.Succeeded &&
                cancelled.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReadinessCancelled &&
                cancelled.DestinationAuthoritative &&
                invalidated.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReadinessInvalidated &&
                invalidated.DestinationAuthoritative &&
                superseded.Superseded &&
                !superseded.DestinationAuthoritative &&
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Superseded) ==
                ActivityEntryReadinessExecutionStatus.Superseded;

            LogBoolStep(logger, "readiness-kinds-preserved", passed);
            return passed;
        }

        private static void LogStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            TransitionResult result)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("status", result.IsValid ? result.Status.ToString() : "Invalid"),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("accepted", GameFlowRuntime.IsAcceptedTransitionPhase(result)));

            if (passed)
            {
                logger.Info("QA GameFlow Transition Failure Authority Smoke step completed.", fields);
            }
            else
            {
                logger.Warning("QA GameFlow Transition Failure Authority Smoke step failed.", fields);
            }
        }

        private static void LogBoolStep(FrameworkLogger logger, string step, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));

            if (passed)
            {
                logger.Info("QA GameFlow Transition Failure Authority Smoke step completed.", fields);
            }
            else
            {
                logger.Warning("QA GameFlow Transition Failure Authority Smoke step failed.", fields);
            }
        }
    }
}
#endif
