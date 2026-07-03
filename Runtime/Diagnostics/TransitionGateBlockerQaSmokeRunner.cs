using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Transition;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F18D Transition/Gate relationship.
    /// It validates that Transition can describe Gate blockers without registering runtime Gate state or mutating flow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F18D Transition Gate blocker relationship smoke; synthetic passive policy only.")]
    internal static class TransitionGateBlockerQaSmokeRunner
    {
        internal const string SmokeName = "Transition Gate Blocker Relationship Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionGateBlockerQaSmokeRunner));

            var operationId = TransitionOperationId.From("qa.transition.gate-blocker.route-switch");
            const TransitionKind kind = TransitionKind.RouteSwitch;

            var blocker = TransitionGateBlockerPolicy.CreateLifecycleRequestBlocker(
                operationId,
                kind,
                normalizedSource,
                "qa.transition.gate-blocker.running");

            bool blockerCreatedPassed = ValidateBlockerCreated(logger, blocker, operationId, kind);
            bool runningBlocksPassed = ValidateRunningBlocksLifecycleRequest(logger, operationId, kind, normalizedSource);
            bool completedReleasesPassed = ValidateCompletedReleasesBlocker(logger, operationId, kind, normalizedSource);
            bool failedReleasesPassed = ValidateFailedReleasesBlocker(logger, operationId, kind, normalizedSource);

            return Task.FromResult(blockerCreatedPassed
                && runningBlocksPassed
                && completedReleasesPassed
                && failedReleasesPassed);
        }

        private static bool ValidateBlockerCreated(
            FrameworkLogger logger,
            GateBlocker blocker,
            TransitionOperationId operationId,
            TransitionKind kind)
        {
            bool passed = blocker is { IsValid: true, BlockerId: { Value: TransitionGateBlockerPolicy.LifecycleRequestBlockerId }, Scope: GateScope.GameFlow, Domain: GateDomain.LifecycleRequest, HasOwner: false, PolicySource: TransitionGateBlockerPolicy.PolicySource }
                && blocker.Blocks(GateScope.GameFlow, GateDomain.LifecycleRequest);

            LogBlockerStep(logger, "blocker-created", passed, blocker, operationId, kind);
            return passed;
        }

        private static bool ValidateRunningBlocksLifecycleRequest(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            TransitionKind kind,
            string source)
        {
            var snapshot = TransitionGateBlockerPolicy.CreateRunningSnapshot(
                operationId,
                kind,
                source,
                "qa.transition.gate-blocker.running");

            var evaluation = snapshot.Evaluate(
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                default,
                "RouteRequest",
                source,
                "qa.transition.gate-blocker.evaluate-running",
                TransitionGateBlockerPolicy.PolicySource);

            bool passed = snapshot is { HasBlockers: true, BlockerCount: 1 }
                && evaluation is { IsBlocked: true, BlockingBlockerCount: 1, Decision: { Status: GateDecisionStatus.Blocked, Scope: GateScope.GameFlow, Domain: GateDomain.LifecycleRequest, PolicySource: TransitionGateBlockerPolicy.PolicySource } };

            LogEvaluationStep(logger, "running-blocks-lifecycle", passed, operationId, kind, evaluation, snapshot.BlockerCount);
            return passed;
        }

        private static bool ValidateCompletedReleasesBlocker(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            TransitionKind kind,
            string source)
        {
            TransitionStep[] observedSteps = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.OperationOpened, "operation-opened", "transition operation opened."),
                TransitionStep.Succeeded(10, TransitionPhase.GateBlockApplied, "gate-block-applied", "transition Gate blocker applied."),
                TransitionStep.Succeeded(20, TransitionPhase.GateBlockReleased, "gate-block-released", "transition Gate blocker released."),
                TransitionStep.Succeeded(30, TransitionPhase.OperationClosed, "operation-closed", "transition operation closed.")
            };

            var result = TransitionResult.SucceededResult(
                operationId,
                kind,
                source,
                "qa.transition.gate-blocker.completed",
                "Transition completed and Gate blocker release was observed.",
                observedSteps);

            var releasedEvaluation = TransitionGateBlockerPolicy.CreateReleasedSnapshot().Evaluate(
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                default,
                "RouteRequest",
                source,
                "qa.transition.gate-blocker.evaluate-completed-release",
                TransitionGateBlockerPolicy.PolicySource);

            bool passed = result is { Succeeded: true, Completed: true, ObservedStepCount: 4, BlockingIssueCount: 0 }
                && releasedEvaluation is { IsAllowed: true, BlockingBlockerCount: 0 };

            LogReleaseStep(logger, "completed-releases-blocker", passed, operationId, kind, result, releasedEvaluation);
            return passed;
        }

        private static bool ValidateFailedReleasesBlocker(
            FrameworkLogger logger,
            TransitionOperationId operationId,
            TransitionKind kind,
            string source)
        {
            TransitionStep[] observedSteps = new[]
            {
                TransitionStep.Observed(0, TransitionPhase.OperationOpened, "operation-opened", "transition operation opened."),
                TransitionStep.Succeeded(10, TransitionPhase.GateBlockApplied, "gate-block-applied", "transition Gate blocker applied."),
                TransitionStep.Failed(20, TransitionPhase.SceneOrContentOperationObserved, "operation-failed", "synthetic transition failure."),
                TransitionStep.Succeeded(30, TransitionPhase.GateBlockReleased, "gate-block-released", "transition Gate blocker released after failure.")
            };

            string[] issues = new[]
            {
                "synthetic-transition-failure"
            };

            var result = TransitionResult.FailedResult(
                operationId,
                kind,
                source,
                "qa.transition.gate-blocker.failed",
                "Transition failed and Gate blocker release was still observed.",
                observedSteps,
                issues);

            var releasedEvaluation = TransitionGateBlockerPolicy.CreateReleasedSnapshot().Evaluate(
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                default,
                "ActivityRequest",
                source,
                "qa.transition.gate-blocker.evaluate-failed-release",
                TransitionGateBlockerPolicy.PolicySource);

            bool passed = result is { Failed: true, Completed: false, ObservedStepCount: 4, IssueCount: 1, BlockingIssueCount: 1 }
                && releasedEvaluation is { IsAllowed: true, BlockingBlockerCount: 0 };

            LogReleaseStep(logger, "failed-releases-blocker", passed, operationId, kind, result, releasedEvaluation);
            return passed;
        }

        private static void LogBlockerStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            GateBlocker blocker,
            TransitionOperationId operationId,
            TransitionKind kind)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", operationId.StableText),
                LogFields.Field("kind", kind.ToString()),
                LogFields.Field("blocker", blocker.BlockerId.Value),
                LogFields.Field("scope", blocker.Scope.ToString()),
                LogFields.Field("domain", blocker.Domain.ToString()),
                LogFields.Field("policySource", blocker.PolicySource),
                LogFields.Field("blocksLifecycle", blocker.Blocks(GateScope.GameFlow, GateDomain.LifecycleRequest)));

            if (passed)
            {
                logger.Info("QA Transition Gate Blocker Smoke step completed.", fields);
                logger.Debug("QA Transition Gate Blocker Smoke blocker diagnostics.", LogFields.Field("details", blocker.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Gate Blocker Smoke step failed.", fields);
            logger.Debug("QA Transition Gate Blocker Smoke blocker failure diagnostics.", LogFields.Field("details", blocker.ToDiagnosticString()));
        }

        private static void LogEvaluationStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            TransitionOperationId operationId,
            TransitionKind kind,
            GateEvaluationResult evaluation,
            int snapshotBlockers)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", operationId.StableText),
                LogFields.Field("kind", kind.ToString()),
                LogFields.Field("status", evaluation.Decision.Status.ToString()),
                LogFields.Field("scope", evaluation.Decision.Scope.ToString()),
                LogFields.Field("domain", evaluation.Decision.Domain.ToString()),
                LogFields.Field("snapshotBlockers", snapshotBlockers),
                LogFields.Field("blockers", evaluation.BlockingBlockerCount),
                LogFields.Field("facts", evaluation.FactCount));

            if (passed)
            {
                logger.Info("QA Transition Gate Blocker Smoke step completed.", fields);
                logger.Debug("QA Transition Gate Blocker Smoke evaluation diagnostics.", LogFields.Field("details", evaluation.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Gate Blocker Smoke step failed.", fields);
            logger.Debug("QA Transition Gate Blocker Smoke evaluation failure diagnostics.", LogFields.Field("details", evaluation.ToDiagnosticString()));
        }

        private static void LogReleaseStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionResult result,
            GateEvaluationResult releasedEvaluation)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", operationId.StableText),
                LogFields.Field("kind", kind.ToString()),
                LogFields.Field("transitionStatus", result.Status.ToString()),
                LogFields.Field("transitionCompleted", result.Completed),
                LogFields.Field("transitionSucceeded", result.Succeeded),
                LogFields.Field("transitionFailed", result.Failed),
                LogFields.Field("observedSteps", result.ObservedStepCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("releaseDecision", releasedEvaluation.Decision.Status.ToString()),
                LogFields.Field("releaseBlockers", releasedEvaluation.BlockingBlockerCount));

            if (passed)
            {
                logger.Info("QA Transition Gate Blocker Smoke step completed.", fields);
                logger.Debug("QA Transition Gate Blocker Smoke release diagnostics.", LogFields.Of(
                    LogFields.Field("transition", result.ToDiagnosticString()),
                    LogFields.Field("gate", releasedEvaluation.ToDiagnosticString())));
                return;
            }

            logger.Warning("QA Transition Gate Blocker Smoke step failed.", fields);
            logger.Debug("QA Transition Gate Blocker Smoke release failure diagnostics.", LogFields.Of(
                LogFields.Field("transition", result.ToDiagnosticString()),
                LogFields.Field("gate", releasedEvaluation.ToDiagnosticString())));
        }
    }
}
#endif
