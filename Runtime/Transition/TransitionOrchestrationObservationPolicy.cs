using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive policy for describing Route/Activity lifecycle observations as Transition plans/results.
    /// This does not execute Route, Activity, SceneLifecycle, Gate, effects, input or UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18E passive Route/Activity Transition observation policy; no lifecycle execution or flow mutation.")]
    internal static class TransitionOrchestrationObservationPolicy
    {
        public const string PolicySource = "F18E.TransitionOrchestrationObservation";

        public static TransitionPlan CreateRouteSwitchPlan(
            TransitionOperationId operationId,
            string source,
            string reason)
        {
            return TransitionPlan.Create(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                reason,
                new[]
                {
                    TransitionStep.Planned(0, TransitionPhase.RequestAdmitted, "request-admitted"),
                    TransitionStep.Planned(10, TransitionPhase.OperationOpened, "operation-opened"),
                    TransitionStep.Planned(20, TransitionPhase.GateBlockApplied, "gate-block-applied"),
                    TransitionStep.Planned(30, TransitionPhase.PreviousScopeExitObserved, "previous-route-exit-observed"),
                    TransitionStep.Planned(40, TransitionPhase.ContentReleaseObserved, "route-content-release-observed"),
                    TransitionStep.Planned(50, TransitionPhase.SceneOrContentOperationObserved, "route-scene-operation-observed"),
                    TransitionStep.Planned(60, TransitionPhase.NextScopeEnterObserved, "next-route-enter-observed"),
                    TransitionStep.Planned(70, TransitionPhase.ReadinessObserved, "startup-activity-readiness-observed"),
                    TransitionStep.Planned(80, TransitionPhase.GateBlockReleased, "gate-block-released"),
                    TransitionStep.Planned(90, TransitionPhase.OperationClosed, "operation-closed")
                });
        }

        public static TransitionPlan CreateActivitySwitchPlan(
            TransitionOperationId operationId,
            string source,
            string reason)
        {
            return TransitionPlan.Create(
                operationId,
                TransitionKind.ActivitySwitch,
                source,
                reason,
                new[]
                {
                    TransitionStep.Planned(0, TransitionPhase.RequestAdmitted, "request-admitted"),
                    TransitionStep.Planned(10, TransitionPhase.OperationOpened, "operation-opened"),
                    TransitionStep.Planned(20, TransitionPhase.GateBlockApplied, "gate-block-applied"),
                    TransitionStep.Planned(30, TransitionPhase.PreviousScopeExitObserved, "previous-activity-exit-observed"),
                    TransitionStep.Planned(40, TransitionPhase.ContentReleaseObserved, "activity-binding-cleanup-observed"),
                    TransitionStep.Planned(50, TransitionPhase.SceneOrContentOperationObserved, "activity-content-operation-observed"),
                    TransitionStep.Planned(60, TransitionPhase.NextScopeEnterObserved, "next-activity-enter-observed"),
                    TransitionStep.Planned(70, TransitionPhase.ReadinessObserved, "activity-readiness-observed"),
                    TransitionStep.Planned(80, TransitionPhase.GateBlockReleased, "gate-block-released"),
                    TransitionStep.Planned(90, TransitionPhase.OperationClosed, "operation-closed")
                });
        }

        public static TransitionPlan CreateActivityClearPlan(
            TransitionOperationId operationId,
            string source,
            string reason)
        {
            return TransitionPlan.Create(
                operationId,
                TransitionKind.ActivityClear,
                source,
                reason,
                new[]
                {
                    TransitionStep.Planned(0, TransitionPhase.RequestAdmitted, "request-admitted"),
                    TransitionStep.Planned(10, TransitionPhase.OperationOpened, "operation-opened"),
                    TransitionStep.Planned(20, TransitionPhase.GateBlockApplied, "gate-block-applied"),
                    TransitionStep.Planned(30, TransitionPhase.PreviousScopeExitObserved, "previous-activity-exit-observed"),
                    TransitionStep.Planned(40, TransitionPhase.ContentReleaseObserved, "activity-binding-cleanup-observed"),
                    TransitionStep.Planned(50, TransitionPhase.SceneOrContentOperationObserved, "activity-clear-content-operation-observed"),
                    TransitionStep.Planned(60, TransitionPhase.ReadinessObserved, "no-active-activity-readiness-observed"),
                    TransitionStep.Planned(70, TransitionPhase.GateBlockReleased, "gate-block-released"),
                    TransitionStep.Planned(80, TransitionPhase.OperationClosed, "operation-closed")
                });
        }

        public static TransitionResult ObserveSucceeded(
            TransitionPlan plan,
            string message,
            IReadOnlyList<TransitionStep> observedSteps)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition observation requires a valid plan.", nameof(plan));
            }

            return TransitionResult.SucceededResult(
                plan.OperationId,
                plan.Kind,
                plan.Source,
                plan.Reason,
                message,
                observedSteps);
        }

        public static TransitionSnapshot SnapshotFromObservation(
            TransitionResult result,
            TransitionPhase phase,
            IReadOnlyList<string> facts)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Transition observation snapshot requires a valid result.", nameof(result));
            }

            return TransitionSnapshot.FromResult(result, phase, facts);
        }
    }
}
