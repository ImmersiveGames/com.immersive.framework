using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Identity;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        /// <summary>
        /// IF-TXN-01: GameFlow accepts only completed Transition phases as transaction-continue evidence.
        /// <see cref="TransitionStatus.Skipped"/> remains accepted because policy/no-visual skips are intentional phase completion.
        /// <see cref="TransitionResult.CompletedWithWarnings"/> remains accepted via <see cref="TransitionResult.Completed"/>.
        /// </summary>
        internal static bool IsAcceptedTransitionPhase(TransitionResult result)
        {
            if (!result.IsValid)
            {
                return false;
            }

            return result.Completed || result.Status == TransitionStatus.Skipped;
        }

        internal static bool TryAcceptTransitionPhase(
            TransitionResult result,
            string phaseLabel,
            out string issue)
        {
            if (IsAcceptedTransitionPhase(result))
            {
                issue = string.Empty;
                return true;
            }

            string statusText = result.IsValid ? result.Status.ToString() : "Invalid";
            string message = result.IsValid
                ? result.Message.NormalizeTextOrFallback(statusText)
                : "Transition phase returned an invalid result.";
            issue =
                $"Transition {phaseLabel} did not complete. status='{statusText}' message='{message}'.";
            return false;
        }

        private static string BuildPreCommitTransitionFailureMessage(
            string operationLabel,
            string phaseIssue,
            TransitionResult beforeResult)
        {
            string operationId = beforeResult.IsValid
                ? beforeResult.OperationId.StableText
                : "<none>";
            return
                $"{operationLabel} aborted before destination commit. {phaseIssue} " +
                $"transitionOperationId='{operationId}'. Previous Route/Activity authority is preserved.";
        }

        private static string BuildCommittedTargetRevealFailureMessage(
            string operationLabel,
            string phaseIssue,
            TransitionResult afterResult)
        {
            string operationId = afterResult.IsValid
                ? afterResult.OperationId.StableText
                : "<none>";
            return
                $"{operationLabel} committed the destination but Transition After/reveal did not complete. " +
                $"{phaseIssue} transitionOperationId='{operationId}'. " +
                "Committed destination remains authoritative; recovery protection is retained/applied; no blind rollback.";
        }

        private void ApplyCommittedTargetRevealRecoveryGate(
            ActivityEntryReadinessExecutionResult readinessExecution,
            string source,
            string reason)
        {
            if (_activityEntryReadinessOrchestrationDisposed)
            {
                return;
            }

            ActivityReadinessOccurrence occurrence = readinessExecution.Occurrence;
            if (!occurrence.IsValid && CurrentOccurrence.IsValid)
            {
                occurrence = CurrentOccurrence;
            }

            if (!occurrence.IsValid || occurrence.Activity == null ||
                !occurrence.Activity.HasValidActivityId)
            {
                return;
            }

            FrameworkIdentityKey owner = FrameworkIdentityKey.From(
                occurrence.Activity.ActivityId);
            _activityEntryReadinessRecoveryOccurrence = occurrence;
            _activityEntryReadinessRecoveryOwner = owner;
            _activityEntryReadinessRecoveryGateSnapshot =
                CommittedTargetRevealRecoveryGatePolicy.Create(
                    occurrence,
                    owner,
                    source,
                    reason);
        }

        private FrameworkRouteRequestResult CreatePreCommitRouteTransitionFailure(
            string phaseIssue,
            RouteAsset targetRoute,
            string source,
            string reason,
            TransitionResult transitionBefore,
            TransitionGateDiagnostics transitionGateDiagnostics)
        {
            var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                TransitionScope.Route,
                transitionBefore,
                default);
            return FrameworkRouteRequestResult.FailedPreCommitTransition(
                BuildPreCommitTransitionFailureMessage(
                    "Route Request",
                    phaseIssue,
                    transitionBefore),
                targetRoute,
                source,
                reason,
                transitionDiagnostics,
                transitionGateDiagnostics);
        }

        private FrameworkRouteRequestResult CreateCommittedRouteRevealFailure(
            string phaseIssue,
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            TransitionResult transitionBefore,
            TransitionResult transitionAfter,
            TransitionGateDiagnostics transitionGateDiagnostics,
            ActivityEntryReadinessExecutionResult readinessExecution)
        {
            var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                TransitionScope.Route,
                transitionBefore,
                transitionAfter);
            ApplyCommittedTargetRevealRecoveryGate(
                readinessExecution,
                source,
                reason);
            return FrameworkRouteRequestResult.FailedCommittedTargetReveal(
                BuildCommittedTargetRevealFailureMessage(
                    "Route Request",
                    phaseIssue,
                    transitionAfter),
                targetRoute,
                source,
                reason,
                routeLifecycleResult,
                transitionDiagnostics,
                transitionGateDiagnostics);
        }

        private FrameworkActivityRequestResult CreatePreCommitActivityTransitionFailure(
            string phaseIssue,
            ActivityAsset targetActivity,
            string source,
            string reason,
            TransitionResult transitionBefore,
            TransitionGateDiagnostics transitionGateDiagnostics,
            ActivityVisualTransitionMode activityTransitionMode)
        {
            var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                TransitionScope.Activity,
                transitionBefore,
                default);
            return FrameworkActivityRequestResult.FailedPreCommitTransition(
                BuildPreCommitTransitionFailureMessage(
                    "Activity Request",
                    phaseIssue,
                    transitionBefore),
                targetActivity,
                source,
                reason,
                transitionDiagnostics,
                transitionGateDiagnostics,
                activityTransitionMode);
        }

        private FrameworkActivityRequestResult CreateCommittedActivityRevealFailure(
            string phaseIssue,
            ActivityAsset targetActivity,
            string source,
            string reason,
            ActivityFlowStartResult activityFlowResult,
            TransitionResult transitionBefore,
            TransitionResult transitionAfter,
            TransitionGateDiagnostics transitionGateDiagnostics,
            ActivityVisualTransitionMode activityTransitionMode,
            ActivityEntryReadinessExecutionResult readinessExecution)
        {
            var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                TransitionScope.Activity,
                transitionBefore,
                transitionAfter);
            ApplyCommittedTargetRevealRecoveryGate(
                readinessExecution,
                source,
                reason);
            return FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                BuildCommittedTargetRevealFailureMessage(
                    "Activity Request",
                    phaseIssue,
                    transitionAfter),
                targetActivity,
                source,
                reason,
                activityFlowResult,
                transitionDiagnostics,
                transitionGateDiagnostics,
                activityTransitionMode);
        }

        private FrameworkGameFlowStartResult CreatePreCommitStartupTransitionFailure(
            string phaseIssue,
            RouteAsset startupRoute,
            TransitionResult transitionBefore)
        {
            return FrameworkGameFlowStartResult.FailedPreCommitTransition(
                BuildPreCommitTransitionFailureMessage(
                    "Game Flow Startup",
                    phaseIssue,
                    transitionBefore),
                startupRoute);
        }

        private FrameworkGameFlowStartResult CreateCommittedStartupRevealFailure(
            string phaseIssue,
            RouteAsset startupRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            TransitionResult transitionAfter,
            ActivityEntryReadinessExecutionResult readinessExecution)
        {
            ApplyCommittedTargetRevealRecoveryGate(
                readinessExecution,
                "GameApplication",
                "startup");
            return FrameworkGameFlowStartResult.FailedCommittedTargetReveal(
                BuildCommittedTargetRevealFailureMessage(
                    "Game Flow Startup",
                    phaseIssue,
                    transitionAfter),
                startupRoute,
                routeLifecycleResult,
                readinessExecution.Status);
        }
    }
}
