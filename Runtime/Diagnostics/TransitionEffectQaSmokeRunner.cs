using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F19C Transition Effect diagnostics.
    /// It validates passive Transition Effect primitives without creating scene objects, ScriptableObjects,
    /// Canvas, fade/loading visuals, adapters, registries, Pause, Input or runtime effect execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F19C Transition Effect diagnostics smoke; synthetic passive primitives only.")]
    internal static class TransitionEffectQaSmokeRunner
    {
        internal const string SmokeName = "Transition Effect Diagnostics Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionEffectQaSmokeRunner));
            var operationId = TransitionOperationId.From("qa.transition-effect.diagnostics.route-switch");
            var plan = CreatePlan(operationId, normalizedSource);

            bool requestPassed = ValidateRequest(logger, plan);
            bool planPassed = ValidatePlan(logger, plan);
            bool succeededPassed = ValidateSucceededResult(logger, plan);
            bool optionalSkippedPassed = ValidateOptionalSkippedResult(logger, plan);
            bool requiredMissingAdapterPassed = ValidateRequiredMissingAdapterResult(logger, plan);
            bool snapshotPassed = ValidateSnapshot(logger, plan);

            return Task.FromResult(requestPassed
                && planPassed
                && succeededPassed
                && optionalSkippedPassed
                && requiredMissingAdapterPassed
                && snapshotPassed);
        }

        private static TransitionEffectPlan CreatePlan(TransitionOperationId operationId, string source)
        {
            TransitionEffectRequest[] requests = new[]
            {
                TransitionEffectRequest.Required(
                    "qa.transition-effect.fade.required",
                    TransitionEffectKind.Fade,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.GateBlockApplied,
                    source,
                    "qa.transition-effect.required-fade"),
                TransitionEffectRequest.Optional(
                    "qa.transition-effect.loading.optional",
                    TransitionEffectKind.LoadingScreen,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.SceneOrContentOperationObserved,
                    source,
                    "qa.transition-effect.optional-loading")
            };

            return TransitionEffectPlan.Create(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition-effect.diagnostics.plan",
                requests);
        }

        private static bool ValidateRequest(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            var request = plan.Requests[0];
            bool passed = request is { IsValid: true, EffectId: { Domain: FrameworkIdentityDomain.TransitionEffect } }
                && request.OperationId == plan.OperationId
                && request.TransitionKind == plan.TransitionKind
                && request is { EffectKind: TransitionEffectKind.Fade, Requiredness: TransitionEffectRequiredness.Required, Phase: TransitionPhase.GateBlockApplied, IsRequired: true, IsOptional: false };

            LogRequestStep(logger, "request", request, passed);
            return passed;
        }

        private static bool ValidatePlan(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            bool passed = plan is { IsValid: true, OperationId: { Domain: FrameworkIdentityDomain.Transition }, TransitionKind: TransitionKind.RouteSwitch, RequestCount: 2, RequiredRequestCount: 1, OptionalRequestCount: 1 }
                && plan.CountByKind(TransitionEffectKind.Fade) == 1
                && plan.CountByKind(TransitionEffectKind.LoadingScreen) == 1;

            LogPlanStep(logger, "plan", plan, passed);
            return passed;
        }

        private static bool ValidateSucceededResult(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            var request = plan.Requests[0];
            var result = TransitionEffectResult.SucceededResult(request, "Required fade effect completed synthetically.");
            bool passed = result is { IsValid: true, Succeeded: true, Completed: true, CompletedWithWarnings: false, Failed: false, MissingAdapter: false, BlocksTransition: false, IssueCount: 0 }
                && result.OperationId == plan.OperationId
                && result.TransitionKind == plan.TransitionKind;

            LogResultStep(logger, "succeeded-result", result, passed);
            return passed;
        }

        private static bool ValidateOptionalSkippedResult(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            var request = plan.Requests[1];
            var result = TransitionEffectResult.SkippedResult(request, "Optional loading screen skipped synthetically.");
            bool passed = result is { IsValid: true, Skipped: true, Completed: true, IsOptional: true, BlocksTransition: false, IssueCount: 0 }
                && result.OperationId == plan.OperationId
                && result.TransitionKind == plan.TransitionKind;

            LogResultStep(logger, "optional-skipped-result", result, passed);
            return passed;
        }

        private static bool ValidateRequiredMissingAdapterResult(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            var request = plan.Requests[0];
            string[] issues = new[]
            {
                "required-transition-effect-adapter-missing"
            };

            var result = TransitionEffectResult.MissingAdapterResult(
                request,
                "Required fade adapter missing is blocking.",
                issues);

            bool passed = result is { IsValid: true, MissingAdapter: true, IsRequired: true, BlocksTransition: true, Completed: false, IssueCount: 1 }
                && result.OperationId == plan.OperationId
                && result.TransitionKind == plan.TransitionKind;

            LogResultStep(logger, "required-missing-adapter-result", result, passed);
            return passed;
        }

        private static bool ValidateSnapshot(FrameworkLogger logger, TransitionEffectPlan plan)
        {
            TransitionEffectResult[] results = new[]
            {
                TransitionEffectResult.SucceededResult(plan.Requests[0], "Required fade effect completed synthetically."),
                TransitionEffectResult.SkippedResult(plan.Requests[1], "Optional loading screen skipped synthetically.")
            };

            string[] facts = new[]
            {
                "transition-effect.diagnostics.snapshot.created"
            };

            var snapshot = TransitionEffectSnapshot.FromResults(
                plan,
                TransitionPhase.GateBlockReleased,
                TransitionEffectStatus.Succeeded,
                results,
                facts);

            bool passed = snapshot.IsValid
                && snapshot.OperationId == plan.OperationId
                && snapshot.TransitionKind == plan.TransitionKind
                && snapshot is { CurrentPhase: TransitionPhase.GateBlockReleased, Status: TransitionEffectStatus.Succeeded, PlannedRequestCount: 2, ObservedResultCount: 2, FactCount: 1, BlockingIssueCount: 0 };

            LogSnapshotStep(logger, "snapshot", snapshot, passed);
            return passed;
        }

        private static void LogRequestStep(FrameworkLogger logger, string step, TransitionEffectRequest request, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("effect", request.EffectId.StableText),
                LogFields.Field("effectKind", request.EffectKind.ToString()),
                LogFields.Field("requiredness", request.Requiredness.ToString()),
                LogFields.Field("operation", request.OperationId.StableText),
                LogFields.Field("transitionKind", request.TransitionKind.ToString()),
                LogFields.Field("phase", request.Phase.ToString()),
                LogFields.Field("isRequired", request.IsRequired));

            if (passed)
            {
                logger.Info("QA Transition Effect Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Effect Diagnostics Smoke request diagnostics.", LogFields.Field("details", request.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Effect Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Effect Diagnostics Smoke request failure diagnostics.", LogFields.Field("details", request.ToDiagnosticString()));
        }

        private static void LogPlanStep(FrameworkLogger logger, string step, TransitionEffectPlan plan, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", plan.OperationId.StableText),
                LogFields.Field("transitionKind", plan.TransitionKind.ToString()),
                LogFields.Field("requests", plan.RequestCount),
                LogFields.Field("required", plan.RequiredRequestCount),
                LogFields.Field("optional", plan.OptionalRequestCount),
                LogFields.Field("fade", plan.CountByKind(TransitionEffectKind.Fade)),
                LogFields.Field("loadingScreen", plan.CountByKind(TransitionEffectKind.LoadingScreen)));

            if (passed)
            {
                logger.Info("QA Transition Effect Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Effect Diagnostics Smoke plan diagnostics.", LogFields.Field("details", plan.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Effect Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Effect Diagnostics Smoke plan failure diagnostics.", LogFields.Field("details", plan.ToDiagnosticString()));
        }

        private static void LogResultStep(FrameworkLogger logger, string step, TransitionEffectResult result, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("effect", result.EffectId.StableText),
                LogFields.Field("effectKind", result.EffectKind.ToString()),
                LogFields.Field("requiredness", result.Requiredness.ToString()),
                LogFields.Field("operation", result.OperationId.StableText),
                LogFields.Field("transitionKind", result.TransitionKind.ToString()),
                LogFields.Field("phase", result.Phase.ToString()),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("succeeded", result.Succeeded),
                LogFields.Field("skipped", result.Skipped),
                LogFields.Field("missingAdapter", result.MissingAdapter),
                LogFields.Field("blocksTransition", result.BlocksTransition),
                LogFields.Field("issues", result.IssueCount));

            if (passed)
            {
                logger.Info("QA Transition Effect Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Effect Diagnostics Smoke result diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Effect Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Effect Diagnostics Smoke result failure diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
        }

        private static void LogSnapshotStep(FrameworkLogger logger, string step, TransitionEffectSnapshot snapshot, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("operation", snapshot.OperationId.StableText),
                LogFields.Field("transitionKind", snapshot.TransitionKind.ToString()),
                LogFields.Field("phase", snapshot.CurrentPhase.ToString()),
                LogFields.Field("status", snapshot.Status.ToString()),
                LogFields.Field("planned", snapshot.PlannedRequestCount),
                LogFields.Field("observed", snapshot.ObservedResultCount),
                LogFields.Field("facts", snapshot.FactCount),
                LogFields.Field("blockingIssues", snapshot.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Transition Effect Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Transition Effect Diagnostics Smoke snapshot diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Transition Effect Diagnostics Smoke step failed.", fields);
            logger.Debug("QA Transition Effect Diagnostics Smoke snapshot failure diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
        }
    }
}
#endif
