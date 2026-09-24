using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Transition;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F18C Transition diagnostics.
    /// It validates the passive Transition primitives without changing scenes, Route, Activity, Gate, Pause or visual effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F18C Transition diagnostics smoke; synthetic passive primitives only.")]
    internal static class TransitionQaSmokeRunner
    {
        internal const string SmokeName = "Transition Diagnostics Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionQaSmokeRunner));

            var operationId = TransitionOperationId.From("qa.transition.diagnostics.route-switch");
            var plan = CreateRouteSwitchPlan(operationId, normalizedSource);

            bool planPassed = ValidatePlan(logger, plan);
            bool succeededPassed = ValidateSucceededResult(logger, operationId, normalizedSource, plan);
            bool warningsPassed = ValidateWarningsResult(logger, operationId, normalizedSource);
            bool failedPassed = ValidateFailedResult(logger, operationId, normalizedSource);
            bool snapshotPassed = ValidateSnapshot(logger, plan);

            return Task.FromResult(planPassed && succeededPassed && warningsPassed && failedPassed && snapshotPassed);
        }

        private static TransitionPlan CreateRouteSwitchPlan(TransitionOperationId operationId, string source)
        {
            TransitionStep[] steps = new[]
            {
                TransitionStep.Planned(0, TransitionPhase.RequestAdmitted, "request-admitted"),
                TransitionStep.Planned(10, TransitionPhase.OperationOpened, "operation-opened"),
                TransitionStep.Planned(20, TransitionPhase.GateBlockApplied, "gate-block-applied"),
                TransitionStep.Planned(30, TransitionPhase.PreviousScopeExitObserved, "previous-scope-exit-observed"),
                TransitionStep.Planned(40, TransitionPhase.ContentReleaseObserved, "content-release-observed"),
                TransitionStep.Planned(50, TransitionPhase.SceneOrContentOperationObserved, "scene-or-content-operation-observed"),
                TransitionStep.Planned(60, TransitionPhase.NextScopeEnterObserved, "next-scope-enter-observed"),
                TransitionStep.Planned(70, TransitionPhase.ReadinessObserved, "readiness-observed"),
                TransitionStep.Planned(80, TransitionPhase.GateBlockReleased, "gate-block-released"),
                TransitionStep.Planned(90, TransitionPhase.OperationClosed, "operation-closed")
            };

            return TransitionPlan.Create(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition.diagnostics.plan",
                steps);
        }

        private static bool ValidatePlan(FrameworkLogger logger, TransitionPlan plan)
        {
            bool passed = plan is { IsValid: true, Kind: TransitionKind.RouteSwitch, StepCount: 10, HasOwner: false, OperationId: { Domain: FrameworkIdentityDomain.Transition } };

            LogPlanStep(logger, "plan", plan, passed);
            return passed;
        }

        private static bool ValidateSucceededResult(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            string source,
            TransitionPlan plan)
        {
            TransitionStep[] observedSteps = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "request admitted by Gate."),
                TransitionStep.Observed(10, TransitionPhase.OperationOpened, "operation-opened", "transition operation opened."),
                TransitionStep.Succeeded(20, TransitionPhase.GateBlockApplied, "gate-block-applied", "transition blocker applied."),
                TransitionStep.Succeeded(30, TransitionPhase.OperationClosed, "operation-closed", "transition operation closed.")
            };

            var result = TransitionResult.SucceededResult(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition.diagnostics.succeeded",
                "Transition diagnostics succeeded.",
                observedSteps);

            bool passed = result is { IsValid: true, Succeeded: true, Completed: true, CompletedWithWarnings: false, Failed: false }
                && result.Kind == plan.Kind
                && result.OperationId == plan.OperationId
                && result is { ObservedStepCount: 4, IssueCount: 0, BlockingIssueCount: 0 };

            LogResultStep(logger, "succeeded-result", result, passed);
            return passed;
        }

        private static bool ValidateWarningsResult(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            string source)
        {
            TransitionStep[] observedSteps = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "request admitted by Gate."),
                TransitionStep.Skipped(10, TransitionPhase.ContentReleaseObserved, "content-release-skipped", "no content release required.")
            };

            string[] issues = new[]
            {
                "optional-transition-observation-skipped"
            };

            var result = TransitionResult.CompletedWithWarningsResult(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition.diagnostics.warnings",
                "Transition diagnostics completed with warnings.",
                observedSteps,
                issues);

            bool passed = result is { IsValid: true, CompletedWithWarnings: true, Completed: true, Succeeded: false, Failed: false, ObservedStepCount: 2, IssueCount: 1, BlockingIssueCount: 0 };

            LogResultStep(logger, "warnings-result", result, passed);
            return passed;
        }

        private static bool ValidateFailedResult(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            string source)
        {
            TransitionStep[] observedSteps = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.RequestAdmitted, "request-admitted", "request admitted by Gate."),
                TransitionStep.Failed(10, TransitionPhase.SceneOrContentOperationObserved, "scene-operation-failed", "synthetic blocking issue.")
            };

            string[] issues = new[]
            {
                "synthetic-transition-blocking-issue"
            };

            var result = TransitionResult.FailedResult(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition.diagnostics.failed",
                "Transition diagnostics failed as expected for synthetic negative shape.",
                observedSteps,
                issues);

            bool passed = result is { IsValid: true, Failed: true, Completed: false, Succeeded: false, ObservedStepCount: 2, IssueCount: 1, BlockingIssueCount: 1 };

            LogResultStep(logger, "failed-result", result, passed);
            return passed;
        }

        private static bool ValidateSnapshot(FrameworkLogger logger, TransitionPlan plan)
        {
            string[] facts = new[]
            {
                "transition.diagnostics.snapshot.created"
            };

            var snapshot = TransitionSnapshot.FromPlan(
                plan,
                TransitionPhase.OperationOpened,
                TransitionStatus.Running,
                facts);

            bool passed = snapshot.IsValid
                && snapshot.OperationId == plan.OperationId
                && snapshot.Kind == plan.Kind
                && snapshot is { CurrentPhase: TransitionPhase.OperationOpened, Status: TransitionStatus.Running }
                && snapshot.ObservedStepCount == plan.StepCount
                && snapshot is { FactCount: 1, BlockingIssueCount: 0 };

            LogSnapshotStep(logger, "snapshot", snapshot, passed);
            return passed;
        }

        private static void LogPlanStep(FrameworkLogger logger, string step, TransitionPlan plan, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", plan.OperationId.StableText),
                LogFields.Field("kind", plan.Kind.ToString()),
                LogFields.Field("steps", plan.StepCount),
                LogFields.Field("owner", plan.HasOwner ? plan.Owner.StableText : "<none>"));

            if (passed)
            {
                logger.Info("QA Transition Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Diagnostics Smoke plan diagnostics.", LogFields.Field("details", plan.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Diagnostics Smoke plan failure diagnostics.", LogFields.Field("details", plan.ToDiagnosticString()));
        }

        private static void LogResultStep(FrameworkLogger logger, string step, TransitionResult result, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", result.OperationId.StableText),
                LogFields.Field("kind", result.Kind.ToString()),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("succeeded", result.Succeeded),
                LogFields.Field("completedWithWarnings", result.CompletedWithWarnings),
                LogFields.Field("failed", result.Failed),
                LogFields.Field("observedSteps", result.ObservedStepCount),
                LogFields.Field("issues", result.IssueCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Transition Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Diagnostics Smoke result diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Diagnostics Smoke result failure diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
        }

        private static void LogSnapshotStep(FrameworkLogger logger, string step, TransitionSnapshot snapshot, bool passed)
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
                logger.Info("QA Transition Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Diagnostics Smoke snapshot diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Diagnostics Smoke snapshot failure diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
        }
    }
}
#endif
