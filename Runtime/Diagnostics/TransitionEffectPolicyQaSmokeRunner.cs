using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F19E required/optional effect policy and authoring guardrails.
    /// It uses an explicit adapter list and does not require saved scene objects, ScriptableObjects, discovery, registries or Transition runtime integration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F19E Transition Effect policy smoke; explicit adapter list only.")]
    internal static class TransitionEffectPolicyQaSmokeRunner
    {
        internal const string SmokeName = "Transition Effect Policy Guardrails Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionEffectPolicyQaSmokeRunner));

            GameObject surfaceRoot = null;
            try
            {
                surfaceRoot = new GameObject("QA_TransitionEffectPolicy_FadeAdapter");
                var adapter = surfaceRoot.AddComponent<UnityFadeCurtainEffectAdapter>();
                var adapters = new ITransitionEffectAdapter[] { adapter };

                bool requiredPresent = ValidateRequiredAdapterPresent(logger, adapters, normalizedSource);
                bool optionalMissing = ValidateOptionalAdapterMissing(logger, adapters, normalizedSource);
                bool requiredMissing = ValidateRequiredAdapterMissing(logger, adapters, normalizedSource);
                bool duplicateId = ValidateDuplicateEffectId(logger, adapters, normalizedSource);
                bool mixedPolicy = ValidateMixedPolicyEvaluation(logger, adapters, normalizedSource);

                return Task.FromResult(requiredPresent
                    && optionalMissing
                    && requiredMissing
                    && duplicateId
                    && mixedPolicy);
            }
            finally
            {
                DestroyQaObject(surfaceRoot);
            }
        }

        private static bool ValidateRequiredAdapterPresent(
            FrameworkLogger logger,
            ITransitionEffectAdapter[] adapters,
            string source)
        {
            var plan = CreatePlan(
                "qa.transition-effect.policy.required-present",
                source,
                TransitionEffectRequest.Required(
                    "qa.transition-effect.policy.fade.required",
                    TransitionEffectKind.Fade,
                    TransitionOperationId.From("qa.transition-effect.policy.required-present"),
                    TransitionKind.RouteSwitch,
                    TransitionPhase.GateBlockApplied,
                    source,
                    "qa.transition-effect.policy.required-present"));

            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(plan, adapters);
            bool passed = evaluation is { IsValid: true, IsAllowed: true, BlocksTransition: false, AdapterCount: 1, MatchedRequestCount: 1, RequiredAdapterMissingCount: 0, OptionalAdapterMissingCount: 0, DuplicateEffectIdCount: 0, IssueCount: 0 };

            LogEvaluationStep(logger, "required-adapter-present", evaluation, passed);
            return passed;
        }

        private static bool ValidateOptionalAdapterMissing(
            FrameworkLogger logger,
            ITransitionEffectAdapter[] adapters,
            string source)
        {
            var plan = CreatePlan(
                "qa.transition-effect.policy.optional-missing",
                source,
                TransitionEffectRequest.Optional(
                    "qa.transition-effect.policy.loading.optional",
                    TransitionEffectKind.LoadingScreen,
                    TransitionOperationId.From("qa.transition-effect.policy.optional-missing"),
                    TransitionKind.RouteSwitch,
                    TransitionPhase.SceneOrContentOperationObserved,
                    source,
                    "qa.transition-effect.policy.optional-missing"));

            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(plan, adapters);
            bool passed = evaluation is { IsValid: true, IsAllowed: true, BlocksTransition: false, AdapterCount: 1, MatchedRequestCount: 0, RequiredAdapterMissingCount: 0, OptionalAdapterMissingCount: 1, DuplicateEffectIdCount: 0, IssueCount: 1, BlockingIssueCount: 0 };

            LogEvaluationStep(logger, "optional-adapter-missing-nonblocking", evaluation, passed);
            return passed;
        }

        private static bool ValidateRequiredAdapterMissing(
            FrameworkLogger logger,
            ITransitionEffectAdapter[] adapters,
            string source)
        {
            var plan = CreatePlan(
                "qa.transition-effect.policy.required-missing",
                source,
                TransitionEffectRequest.Required(
                    "qa.transition-effect.policy.loading.required",
                    TransitionEffectKind.LoadingScreen,
                    TransitionOperationId.From("qa.transition-effect.policy.required-missing"),
                    TransitionKind.RouteSwitch,
                    TransitionPhase.SceneOrContentOperationObserved,
                    source,
                    "qa.transition-effect.policy.required-missing"));

            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(plan, adapters);
            bool passed = evaluation is { IsValid: true, IsAllowed: false, BlocksTransition: true, AdapterCount: 1, MatchedRequestCount: 0, RequiredAdapterMissingCount: 1, OptionalAdapterMissingCount: 0, DuplicateEffectIdCount: 0, IssueCount: 1, BlockingIssueCount: 1 };

            LogEvaluationStep(logger, "required-adapter-missing-blocking", evaluation, passed);
            return passed;
        }

        private static bool ValidateDuplicateEffectId(
            FrameworkLogger logger,
            ITransitionEffectAdapter[] adapters,
            string source)
        {
            var operationId = TransitionOperationId.From("qa.transition-effect.policy.duplicate-id");
            string duplicateId = "qa.transition-effect.policy.duplicate.fade";
            TransitionEffectRequest[] requests = new[]
            {
                TransitionEffectRequest.Required(
                    duplicateId,
                    TransitionEffectKind.Fade,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.GateBlockApplied,
                    source,
                    "qa.transition-effect.policy.duplicate-a"),
                TransitionEffectRequest.Optional(
                    duplicateId,
                    TransitionEffectKind.Fade,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.GateBlockReleased,
                    source,
                    "qa.transition-effect.policy.duplicate-b")
            };

            var plan = TransitionEffectPlan.Create(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition-effect.policy.duplicate-id",
                requests);

            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(plan, adapters);
            bool passed = evaluation is { IsValid: true, IsAllowed: false, BlocksTransition: true, AdapterCount: 1, MatchedRequestCount: 2, RequiredAdapterMissingCount: 0, OptionalAdapterMissingCount: 0, DuplicateEffectIdCount: 1, IssueCount: 1, BlockingIssueCount: 1 };

            LogEvaluationStep(logger, "duplicate-effect-id-blocking", evaluation, passed);
            return passed;
        }

        private static bool ValidateMixedPolicyEvaluation(
            FrameworkLogger logger,
            ITransitionEffectAdapter[] adapters,
            string source)
        {
            var operationId = TransitionOperationId.From("qa.transition-effect.policy.mixed");
            TransitionEffectRequest[] requests = new[]
            {
                TransitionEffectRequest.Required(
                    "qa.transition-effect.policy.mixed.fade.required",
                    TransitionEffectKind.Fade,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.GateBlockApplied,
                    source,
                    "qa.transition-effect.policy.mixed.fade-required"),
                TransitionEffectRequest.Optional(
                    "qa.transition-effect.policy.mixed.loading.optional",
                    TransitionEffectKind.LoadingScreen,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionPhase.SceneOrContentOperationObserved,
                    source,
                    "qa.transition-effect.policy.mixed.loading-optional")
            };

            var plan = TransitionEffectPlan.Create(
                operationId,
                TransitionKind.RouteSwitch,
                source,
                "qa.transition-effect.policy.mixed",
                requests);

            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(plan, adapters);
            bool passed = evaluation is { IsValid: true, IsAllowed: true, BlocksTransition: false, AdapterCount: 1, MatchedRequestCount: 1, RequiredAdapterMissingCount: 0, OptionalAdapterMissingCount: 1, DuplicateEffectIdCount: 0, IssueCount: 1, BlockingIssueCount: 0 };

            LogEvaluationStep(logger, "mixed-policy-evaluation", evaluation, passed);
            return passed;
        }

        private static TransitionEffectPlan CreatePlan(
            string operationValue,
            string source,
            TransitionEffectRequest request)
        {
            return TransitionEffectPlan.Create(
                TransitionOperationId.From(operationValue),
                TransitionKind.RouteSwitch,
                source,
                operationValue,
                new[] { request });
        }

        private static void LogEvaluationStep(
            FrameworkLogger logger,
            string step,
            TransitionEffectPolicyEvaluation evaluation,
            bool passed)
        {
            logger.Info(
                passed
                    ? "QA Transition Effect Policy Guardrails Smoke step completed."
                    : "QA Transition Effect Policy Guardrails Smoke step failed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("passed", passed),
                    LogFields.Field("operation", evaluation.Plan.OperationId.StableText),
                    LogFields.Field("transitionKind", evaluation.Plan.TransitionKind.ToString()),
                    LogFields.Field("policySource", evaluation.PolicySource),
                    LogFields.Field("allowed", evaluation.IsAllowed),
                    LogFields.Field("blocksTransition", evaluation.BlocksTransition),
                    LogFields.Field("adapters", evaluation.AdapterCount),
                    LogFields.Field("requests", evaluation.Plan.RequestCount),
                    LogFields.Field("matched", evaluation.MatchedRequestCount),
                    LogFields.Field("requiredMissing", evaluation.RequiredAdapterMissingCount),
                    LogFields.Field("optionalMissing", evaluation.OptionalAdapterMissingCount),
                    LogFields.Field("duplicates", evaluation.DuplicateEffectIdCount),
                    LogFields.Field("issues", evaluation.IssueCount),
                    LogFields.Field("blockingIssues", evaluation.BlockingIssueCount)));

            logger.Debug("QA Transition Effect Policy Guardrails Smoke evaluation diagnostics.", LogFields.Field("details", evaluation.ToDiagnosticString()));
        }

        private static void DestroyQaObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}
#endif
