using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F18E Route/Activity Transition orchestration observations.
    /// It validates passive Transition narration of existing lifecycle concepts without executing Route, Activity, scenes or effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F18E Transition orchestration observation smoke; synthetic passive observations only.")]
    internal static class TransitionOrchestrationObservationQaSmokeRunner
    {
        internal const string SmokeName = "Transition Orchestration Observation Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionOrchestrationObservationQaSmokeRunner));

            bool routePassed = ValidateRouteSwitchObservation(logger, normalizedSource);
            bool activitySwitchPassed = ValidateActivitySwitchObservation(logger, normalizedSource);
            bool activityClearPassed = ValidateActivityClearObservation(logger, normalizedSource);
            bool snapshotPassed = ValidateObservationSnapshot(logger, normalizedSource);

            return Task.FromResult(routePassed && activitySwitchPassed && activityClearPassed && snapshotPassed);
        }

        private static bool ValidateRouteSwitchObservation(FrameworkLogger logger, string source)
        {
            var plan = TransitionOrchestrationObservationPolicy.CreateRouteSwitchPlan(
                TransitionOperationId.From("qa.transition.observation.route-switch"),
                source,
                "qa.transition.observation.route-switch.plan");

            TransitionStep[] observed = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "Route request admitted by Gate."),
                TransitionStep.Observed(10, TransitionPhase.OperationOpened, "operation-opened", "Route transition operation opened."),
                TransitionStep.Succeeded(20, TransitionPhase.GateBlockApplied, "gate-block-applied", "Transition Gate blocker relationship observed."),
                TransitionStep.Observed(30, TransitionPhase.PreviousScopeExitObserved, "previous-route-exit-observed", "Previous Route exit result observed."),
                TransitionStep.Observed(40, TransitionPhase.ContentReleaseObserved, "route-content-release-observed", "Route content release result observed."),
                TransitionStep.Observed(50, TransitionPhase.SceneOrContentOperationObserved, "route-scene-operation-observed", "Route scene composition/load result observed."),
                TransitionStep.Observed(60, TransitionPhase.NextScopeEnterObserved, "next-route-enter-observed", "Next Route enter/content callbacks observed."),
                TransitionStep.Observed(70, TransitionPhase.ReadinessObserved, "startup-activity-readiness-observed", "Startup Activity readiness observed."),
                TransitionStep.Succeeded(80, TransitionPhase.GateBlockReleased, "gate-block-released", "Transition Gate blocker release observed."),
                TransitionStep.Succeeded(90, TransitionPhase.OperationClosed, "operation-closed", "Route transition operation closed.")
            };

            var result = TransitionOrchestrationObservationPolicy.ObserveSucceeded(
                plan,
                "Route switch orchestration observation succeeded.",
                observed);

            bool passed = plan is { IsValid: true, Kind: TransitionKind.RouteSwitch, StepCount: 10 }
                && result is { Succeeded: true, Completed: true }
                && result.ObservedStepCount == plan.StepCount
                && result.BlockingIssueCount == 0;

            LogObservationStep(logger, "route-switch-observed", passed, plan, result);
            return passed;
        }

        private static bool ValidateActivitySwitchObservation(FrameworkLogger logger, string source)
        {
            var plan = TransitionOrchestrationObservationPolicy.CreateActivitySwitchPlan(
                TransitionOperationId.From("qa.transition.observation.activity-switch"),
                source,
                "qa.transition.observation.activity-switch.plan");

            TransitionStep[] observed = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "Activity request admitted by Gate."),
                TransitionStep.Observed(10, TransitionPhase.OperationOpened, "operation-opened", "Activity transition operation opened."),
                TransitionStep.Succeeded(20, TransitionPhase.GateBlockApplied, "gate-block-applied", "Transition Gate blocker relationship observed."),
                TransitionStep.Observed(30, TransitionPhase.PreviousScopeExitObserved, "previous-activity-exit-observed", "Previous Activity exit/content result observed."),
                TransitionStep.Observed(40, TransitionPhase.ContentReleaseObserved, "activity-binding-cleanup-observed", "Activity binding cleanup result observed."),
                TransitionStep.Observed(50, TransitionPhase.SceneOrContentOperationObserved, "activity-content-operation-observed", "Activity content operation observed."),
                TransitionStep.Observed(60, TransitionPhase.NextScopeEnterObserved, "next-activity-enter-observed", "Next Activity enter/content result observed."),
                TransitionStep.Observed(70, TransitionPhase.ReadinessObserved, "activity-readiness-observed", "Activity readiness observed."),
                TransitionStep.Succeeded(80, TransitionPhase.GateBlockReleased, "gate-block-released", "Transition Gate blocker release observed."),
                TransitionStep.Succeeded(90, TransitionPhase.OperationClosed, "operation-closed", "Activity transition operation closed.")
            };

            var result = TransitionOrchestrationObservationPolicy.ObserveSucceeded(
                plan,
                "Activity switch orchestration observation succeeded.",
                observed);

            bool passed = plan is { IsValid: true, Kind: TransitionKind.ActivitySwitch, StepCount: 10 }
                && result is { Succeeded: true, Completed: true }
                && result.ObservedStepCount == plan.StepCount
                && result.BlockingIssueCount == 0;

            LogObservationStep(logger, "activity-switch-observed", passed, plan, result);
            return passed;
        }

        private static bool ValidateActivityClearObservation(FrameworkLogger logger, string source)
        {
            var plan = TransitionOrchestrationObservationPolicy.CreateActivityClearPlan(
                TransitionOperationId.From("qa.transition.observation.activity-clear"),
                source,
                "qa.transition.observation.activity-clear.plan");

            TransitionStep[] observed = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "Activity clear request admitted by Gate."),
                TransitionStep.Observed(10, TransitionPhase.OperationOpened, "operation-opened", "Activity clear transition operation opened."),
                TransitionStep.Succeeded(20, TransitionPhase.GateBlockApplied, "gate-block-applied", "Transition Gate blocker relationship observed."),
                TransitionStep.Observed(30, TransitionPhase.PreviousScopeExitObserved, "previous-activity-exit-observed", "Previous Activity exit/content result observed."),
                TransitionStep.Observed(40, TransitionPhase.ContentReleaseObserved, "activity-binding-cleanup-observed", "Activity binding cleanup result observed."),
                TransitionStep.Observed(50, TransitionPhase.SceneOrContentOperationObserved, "activity-clear-content-operation-observed", "Activity clear content operation observed."),
                TransitionStep.Observed(60, TransitionPhase.ReadinessObserved, "no-active-activity-readiness-observed", "No-active-Activity readiness observed."),
                TransitionStep.Succeeded(70, TransitionPhase.GateBlockReleased, "gate-block-released", "Transition Gate blocker release observed."),
                TransitionStep.Succeeded(80, TransitionPhase.OperationClosed, "operation-closed", "Activity clear transition operation closed.")
            };

            var result = TransitionOrchestrationObservationPolicy.ObserveSucceeded(
                plan,
                "Activity clear orchestration observation succeeded.",
                observed);

            bool passed = plan is { IsValid: true, Kind: TransitionKind.ActivityClear, StepCount: 9 }
                && result is { Succeeded: true, Completed: true }
                && result.ObservedStepCount == plan.StepCount
                && result.BlockingIssueCount == 0;

            LogObservationStep(logger, "activity-clear-observed", passed, plan, result);
            return passed;
        }

        private static bool ValidateObservationSnapshot(FrameworkLogger logger, string source)
        {
            var plan = TransitionOrchestrationObservationPolicy.CreateActivitySwitchPlan(
                TransitionOperationId.From("qa.transition.observation.snapshot"),
                source,
                "qa.transition.observation.snapshot.plan");

            TransitionStep[] observed = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "Activity request admitted by Gate."),
                TransitionStep.Observed(10, TransitionPhase.OperationOpened, "operation-opened", "Activity transition operation opened."),
                TransitionStep.Observed(20, TransitionPhase.NextScopeEnterObserved, "next-activity-enter-observed", "Next Activity enter/content result observed."),
                TransitionStep.Succeeded(30, TransitionPhase.OperationClosed, "operation-closed", "Activity transition operation closed.")
            };

            var result = TransitionOrchestrationObservationPolicy.ObserveSucceeded(
                plan,
                "Activity switch snapshot observation succeeded.",
                observed);

            var snapshot = TransitionOrchestrationObservationPolicy.SnapshotFromObservation(
                result,
                TransitionPhase.OperationClosed,
                new[]
                {
                    "transition.observation.route-activity.narrative"
                });

            bool passed = snapshot.IsValid
                && snapshot.OperationId == result.OperationId
                && snapshot.Kind == result.Kind
                && snapshot is { CurrentPhase: TransitionPhase.OperationClosed, Status: TransitionStatus.Succeeded }
                && snapshot.ObservedStepCount == result.ObservedStepCount
                && snapshot is { FactCount: 1, BlockingIssueCount: 0 };

            LogSnapshotStep(logger, "observation-snapshot", passed, snapshot);
            return passed;
        }

        private static void LogObservationStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            TransitionPlan plan,
            TransitionResult result)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", plan.OperationId.StableText),
                LogFields.Field("kind", plan.Kind.ToString()),
                LogFields.Field("policySource", TransitionOrchestrationObservationPolicy.PolicySource),
                LogFields.Field("planSteps", plan.StepCount),
                LogFields.Field("observedSteps", result.ObservedStepCount),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("blockingIssues", result.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Transition Orchestration Observation Smoke step completed.", fields);
                logger.Debug("QA Transition Orchestration Observation Smoke diagnostics.", LogFields.Of(
                    LogFields.Field("plan", plan.ToDiagnosticString()),
                    LogFields.Field("result", result.ToDiagnosticString())));
                return;
            }

            logger.Warning("QA Transition Orchestration Observation Smoke step failed.", fields);
            logger.Debug("QA Transition Orchestration Observation Smoke failure diagnostics.", LogFields.Of(
                LogFields.Field("plan", plan.ToDiagnosticString()),
                LogFields.Field("result", result.ToDiagnosticString())));
        }

        private static void LogSnapshotStep(FrameworkLogger logger, string step, bool passed, TransitionSnapshot snapshot)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", snapshot.OperationId.StableText),
                LogFields.Field("kind", snapshot.Kind.ToString()),
                LogFields.Field("phase", snapshot.CurrentPhase.ToString()),
                LogFields.Field("status", snapshot.Status.ToString()),
                LogFields.Field("observedSteps", snapshot.ObservedStepCount),
                LogFields.Field("facts", snapshot.FactCount),
                LogFields.Field("blockingIssues", snapshot.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Transition Orchestration Observation Smoke step completed.", fields);
                logger.Debug("QA Transition Orchestration Observation Smoke snapshot diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Orchestration Observation Smoke step failed.", fields);
            logger.Debug("QA Transition Orchestration Observation Smoke snapshot failure diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
        }
    }
}
#endif
