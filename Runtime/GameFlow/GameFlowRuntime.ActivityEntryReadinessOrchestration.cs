using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Gate;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        private GateSnapshot _activityEntryReadinessRecoveryGateSnapshot;
        private ActivityReadinessOccurrence _activityEntryReadinessRecoveryOccurrence;

        private GateSnapshot CurrentActivityEntryReadinessGateSnapshot =>
            CombineGateSnapshots(
                _transitionGateSnapshot,
                _activityEntryReadinessRecoveryGateSnapshot);

        private static bool TryValidateActivityEntryReadinessConfiguration(
            ActivityAsset activity,
            TransitionGateMode enclosingGateMode,
            bool hasVisualCover,
            out string issue)
        {
            issue = string.Empty;
            if (activity == null || !activity.HasDefinedEntryReadinessPolicy)
            {
                issue = "Activity Entry Readiness Policy is missing or invalid.";
                return false;
            }

            if (!activity.WaitsForEntryReadiness)
            {
                return true;
            }

            if (enclosingGateMode != TransitionGateMode.InputInteractionAndGameplay)
            {
                issue = "Waiting Activity Entry Readiness policies require Transition Gate Mode InputInteractionAndGameplay.";
                return false;
            }

            if (activity.EntryReadinessPolicy == ActivityEntryReadinessPolicy.WaitCovered &&
                !hasVisualCover)
            {
                issue = "WaitCovered requires an explicit visual cover for this Activity entry.";
                return false;
            }

            return true;
        }

        private bool TryPrepareActivityEntryReadinessExecution(
            ActivityAsset activity,
            ActivityFlowStartResult activityFlowResult,
            TransitionGateMode enclosingGateMode,
            bool requiresVisualCover,
            out ActivityEntryReadinessExecutionResult result)
        {
            result = default;
            if (activity == null)
            {
                result = new ActivityEntryReadinessExecutionResult(
                    ActivityEntryReadinessPolicy.ObserveOnly,
                    ActivityEntryReadinessExecutionStatus.ObserveOnly,
                    default,
                    activityFlowResult,
                    "NoStartupActivity",
                    activityFlowResult.Completed);
                return true;
            }

            if (!TryValidateActivityEntryReadinessConfiguration(
                    activity,
                    enclosingGateMode,
                    requiresVisualCover,
                    out string configurationIssue))
            {
                result = CreateConfigurationRejectedResult(
                    activityFlowResult, configurationIssue);
                return false;
            }

            ActivityEntryReadinessPolicy policy = activity.EntryReadinessPolicy;
            if (policy == ActivityEntryReadinessPolicy.ObserveOnly)
            {
                result = new ActivityEntryReadinessExecutionResult(
                    policy,
                    ActivityEntryReadinessExecutionStatus.ObserveOnly,
                    default,
                    activityFlowResult,
                    "ObserveOnly",
                    activityFlowResult.Completed);
                return true;
            }

            if (!activityFlowResult.Completed || !ReferenceEquals(activityFlowResult.Activity, activity))
            {
                result = new ActivityEntryReadinessExecutionResult(
                    policy,
                    ActivityEntryReadinessExecutionStatus.Invalidated,
                    ActivityEntryReadinessWaitResult.Invalidation(
                        default,
                        activityFlowResult.ActivityReadinessState,
                        "CommittedActivityOccurrenceUnavailable",
                        1),
                    activityFlowResult,
                    "CommittedActivityOccurrenceUnavailable",
                    activityFlowResult.Completed);
                return true;
            }

            ActivityReadinessOccurrence occurrence = _routeLifecycleRuntime.CurrentOccurrence;
            if (!occurrence.Matches(activity, occurrence.TransitionSequence))
            {
                result = new ActivityEntryReadinessExecutionResult(
                    policy,
                    ActivityEntryReadinessExecutionStatus.Invalidated,
                    ActivityEntryReadinessWaitResult.Invalidation(
                        occurrence,
                        activityFlowResult.ActivityReadinessState,
                        "InitialOccurrenceUnavailable",
                        1),
                    activityFlowResult,
                    "InitialOccurrenceUnavailable",
                    true);
                return true;
            }

            result = new ActivityEntryReadinessExecutionResult(
                policy,
                ActivityEntryReadinessExecutionStatus.Unknown,
                default,
                activityFlowResult,
                string.Empty,
                true,
                occurrence: occurrence);
            return true;
        }

        private async Task<ActivityEntryReadinessExecutionResult>
            WaitForPreparedActivityEntryReadinessAsync(
                ActivityEntryReadinessExecutionResult prepared,
                CancellationToken cancellationToken)
        {
            if (!prepared.RequiresWait || prepared.Status != ActivityEntryReadinessExecutionStatus.Unknown)
            {
                return prepared;
            }

            ActivityReadinessOccurrence occurrence = prepared.Occurrence;
            ActivityEntryReadinessWaitResult waitResult =
                await WaitForActivityEntryReadinessAsync(occurrence, cancellationToken);
            RefreshCurrentFlowContext();

            ActivityFlowStartResult finalActivityFlowResult = prepared.ActivityFlowResult;
            if (_routeLifecycleRuntime.TryGetCurrentRouteResult(
                    out RouteLifecycleStartResult currentRouteResult) &&
                _routeLifecycleRuntime.CurrentOccurrence.Matches(
                    occurrence.Activity,
                    occurrence.TransitionSequence) &&
                ReferenceEquals(currentRouteResult.ActivityFlowResult.Activity, occurrence.Activity))
            {
                finalActivityFlowResult = currentRouteResult.ActivityFlowResult;
            }
            else if (waitResult.Status == ActivityEntryReadinessWaitStatus.Ready)
            {
                waitResult = ActivityEntryReadinessWaitResult.Invalidation(
                    occurrence,
                    default,
                    "OccurrenceReplacedBeforeFinalProjection",
                    waitResult.Revision);
            }

            return new ActivityEntryReadinessExecutionResult(
                prepared.Policy,
                MapWaitStatus(waitResult.Status),
                waitResult,
                finalActivityFlowResult,
                waitResult.Reason,
                true,
                occurrence: occurrence);
        }

        private void ApplyActivityEntryReadinessRecoveryGate(
            ActivityEntryReadinessExecutionResult execution,
            string source,
            string reason)
        {
            if (!execution.IsFailure || !execution.DestinationAuthoritative ||
                !execution.Occurrence.IsValid)
            {
                return;
            }

            _activityEntryReadinessRecoveryOccurrence = execution.Occurrence;
            _activityEntryReadinessRecoveryGateSnapshot =
                ActivityEntryReadinessRecoveryGatePolicy.Create(
                    execution.Occurrence,
                    source,
                    reason);
        }

        private void ReleaseActivityEntryReadinessRecoveryGate()
        {
            _activityEntryReadinessRecoveryGateSnapshot = GateSnapshot.Empty();
            _activityEntryReadinessRecoveryOccurrence = default;
        }

        private static ActivityEntryReadinessExecutionStatus MapWaitStatus(
            ActivityEntryReadinessWaitStatus status)
        {
            return status switch
            {
                ActivityEntryReadinessWaitStatus.Ready => ActivityEntryReadinessExecutionStatus.Ready,
                ActivityEntryReadinessWaitStatus.Failed => ActivityEntryReadinessExecutionStatus.Failed,
                ActivityEntryReadinessWaitStatus.Invalidated => ActivityEntryReadinessExecutionStatus.Invalidated,
                ActivityEntryReadinessWaitStatus.Cancelled => ActivityEntryReadinessExecutionStatus.Cancelled,
                _ => ActivityEntryReadinessExecutionStatus.Invalidated
            };
        }

        private static FrameworkActivityRequestResult CreateCommittedActivityReadinessResult(
            ActivityEntryReadinessExecutionResult execution,
            ActivityAsset targetActivity,
            string source,
            string reason,
            FrameworkTransitionDiagnostics transitionDiagnostics,
            TransitionGateDiagnostics transitionGateDiagnostics,
            ActivityVisualTransitionMode transitionMode)
        {
            string message = "Activity Request committed the target Activity but entry readiness did not complete. " +
                execution.ToDiagnosticString();
            return execution.Status switch
            {
                ActivityEntryReadinessExecutionStatus.Failed =>
                    FrameworkActivityRequestResult.FailedCommittedTargetNotReady(
                        message, targetActivity, source, reason, execution.ActivityFlowResult,
                        transitionDiagnostics, transitionGateDiagnostics, transitionMode),
                ActivityEntryReadinessExecutionStatus.Cancelled =>
                    FrameworkActivityRequestResult.FailedCommittedTargetReadinessCancelled(
                        message, targetActivity, source, reason, execution.ActivityFlowResult,
                        transitionDiagnostics, transitionGateDiagnostics, transitionMode),
                _ => FrameworkActivityRequestResult.FailedCommittedTargetReadinessInvalidated(
                    message, targetActivity, source, reason, execution.ActivityFlowResult,
                    transitionDiagnostics, transitionGateDiagnostics, transitionMode)
            };
        }

        private static FrameworkRouteRequestResult CreateCommittedRouteReadinessResult(
            ActivityEntryReadinessExecutionResult execution,
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            FrameworkTransitionDiagnostics transitionDiagnostics,
            TransitionGateDiagnostics transitionGateDiagnostics)
        {
            string message = "Route Request committed the target Route but Startup Activity entry readiness did not complete. " +
                execution.ToDiagnosticString();
            FrameworkRouteRequestKind kind = execution.Status switch
            {
                ActivityEntryReadinessExecutionStatus.Failed =>
                    FrameworkRouteRequestKind.FailedCommittedTargetNotReady,
                ActivityEntryReadinessExecutionStatus.Cancelled =>
                    FrameworkRouteRequestKind.FailedCommittedTargetReadinessCancelled,
                _ => FrameworkRouteRequestKind.FailedCommittedTargetReadinessInvalidated
            };
            return FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                kind, message, targetRoute, source, reason, routeLifecycleResult,
                transitionDiagnostics, transitionGateDiagnostics);
        }

        private static ActivityEntryReadinessExecutionResult CreateConfigurationRejectedResult(
            ActivityFlowStartResult activityFlowResult,
            string reason)
        {
            return new ActivityEntryReadinessExecutionResult(
                ActivityEntryReadinessPolicy.ObserveOnly,
                ActivityEntryReadinessExecutionStatus.RejectedInvalidConfiguration,
                default,
                activityFlowResult,
                reason,
                false);
        }

        private static GateSnapshot CombineGateSnapshots(
            GateSnapshot first,
            GateSnapshot second)
        {
            if (!first.HasBlockers && !second.HasBlockers)
            {
                return GateSnapshot.Empty();
            }

            var blockers = new List<GateBlocker>(first.BlockerCount + second.BlockerCount);
            AddGateBlockers(blockers, first);
            AddGateBlockers(blockers, second);
            return new GateSnapshot(blockers);
        }

        private static void AddGateBlockers(List<GateBlocker> target, GateSnapshot source)
        {
            IReadOnlyList<GateBlocker> blockers = source.Blockers;
            for (int index = 0; index < blockers.Count; index++)
            {
                target.Add(blockers[index]);
            }
        }
    }
}
