using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
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
    /// API status: Development Tooling. Synthetic smoke for F19D minimal Unity fade/curtain adapter boundary.
    /// It creates transient QA GameObjects and validates CanvasGroup state mutation without requiring a canonical scene asset,
    /// DOTween, loading screen prefab, adapter registry, Pause, Input or Transition runtime integration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F19D Unity fade/curtain adapter smoke; transient QA surface only.")]
    internal static class TransitionEffectUnityFadeCurtainQaSmokeRunner
    {
        internal const string SmokeName = "Unity Fade Curtain Effect Adapter Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionEffectUnityFadeCurtainQaSmokeRunner));

            GameObject surfaceRoot = null;
            GameObject missingSurfaceRoot = null;

            try
            {
                var operationId = TransitionOperationId.From("qa.transition-effect.unity-fade-curtain.route-switch");
                surfaceRoot = new GameObject("QA_TransitionEffect_UnityFadeCurtain_Surface");
                var canvasGroup = surfaceRoot.AddComponent<CanvasGroup>();
                var adapter = surfaceRoot.AddComponent<UnityFadeCurtainEffectAdapter>();

                bool adapterCreated = ValidateAdapterCreated(logger, adapter, canvasGroup);
                bool visibleApplied = ValidateVisibleState(logger, adapter, canvasGroup, surfaceRoot, operationId, normalizedSource);
                bool hiddenApplied = ValidateHiddenState(logger, adapter, canvasGroup, surfaceRoot, operationId, normalizedSource);

                missingSurfaceRoot = new GameObject("QA_TransitionEffect_UnityFadeCurtain_MissingSurface");
                var missingAdapter = missingSurfaceRoot.AddComponent<UnityFadeCurtainEffectAdapter>();
                bool missingSurface = ValidateMissingSurface(logger, missingAdapter, operationId, normalizedSource);
                bool unsupportedOptional = ValidateUnsupportedOptionalKind(logger, adapter, operationId, normalizedSource);

                return Task.FromResult(adapterCreated && visibleApplied && hiddenApplied && missingSurface && unsupportedOptional);
            }
            finally
            {
                DestroyQaObject(surfaceRoot);
                DestroyQaObject(missingSurfaceRoot);
            }
        }

        private static bool ValidateAdapterCreated(FrameworkLogger logger, UnityFadeCurtainEffectAdapter adapter, CanvasGroup canvasGroup)
        {
            bool passed = adapter != null
                && canvasGroup != null
                && adapter.Supports(TransitionEffectKind.Fade)
                && !adapter.Supports(TransitionEffectKind.LoadingScreen)
                && adapter.HasCanvasGroup;

            logger.Info(
                passed
                    ? "QA Unity Fade Curtain Effect Adapter Smoke step completed."
                    : "QA Unity Fade Curtain Effect Adapter Smoke step failed.",
                LogFields.Of(
                    LogFields.Field("step", "adapter-created"),
                    LogFields.Field("passed", passed),
                    LogFields.Field("adapter", adapter != null ? adapter.AdapterName : "<null>"),
                    LogFields.Field("configuredKind", adapter != null ? adapter.ConfiguredEffectKind.ToString() : "<null>"),
                    LogFields.Field("hasCanvasGroup", adapter != null && adapter.HasCanvasGroup),
                    LogFields.Field("supportsFade", adapter != null && adapter.Supports(TransitionEffectKind.Fade)),
                    LogFields.Field("supportsLoadingScreen", adapter != null && adapter.Supports(TransitionEffectKind.LoadingScreen))));

            return passed;
        }

        private static bool ValidateVisibleState(
            FrameworkLogger logger,
            UnityFadeCurtainEffectAdapter adapter,
            CanvasGroup canvasGroup,
            GameObject surfaceRoot,
            TransitionOperationId operationId,
            string source)
        {
            var request = TransitionEffectRequest.Required(
                "qa.transition-effect.unity-fade-curtain.fade.required",
                TransitionEffectKind.Fade,
                operationId,
                TransitionKind.RouteSwitch,
                TransitionPhase.GateBlockApplied,
                source,
                "qa.transition-effect.unity-fade-curtain.visible");

            var result = adapter.Execute(request);
            bool passed = result is { Succeeded: true, Completed: true, BlocksTransition: false }
                && surfaceRoot.activeSelf
                && Approximately(canvasGroup.alpha, 1f)
                && canvasGroup.blocksRaycasts
                && !canvasGroup.interactable
                && adapter.IsVisible;

            LogResultStep(logger, "visible-state-applied", result, passed, surfaceRoot, canvasGroup, adapter);
            return passed;
        }

        private static bool ValidateHiddenState(
            FrameworkLogger logger,
            UnityFadeCurtainEffectAdapter adapter,
            CanvasGroup canvasGroup,
            GameObject surfaceRoot,
            TransitionOperationId operationId,
            string source)
        {
            var request = TransitionEffectRequest.Required(
                "qa.transition-effect.unity-fade-curtain.fade.required",
                TransitionEffectKind.Fade,
                operationId,
                TransitionKind.RouteSwitch,
                TransitionPhase.GateBlockReleased,
                source,
                "qa.transition-effect.unity-fade-curtain.hidden");

            var result = adapter.Execute(request);
            bool passed = result is { Succeeded: true, Completed: true, BlocksTransition: false }
                && !surfaceRoot.activeSelf
                && Approximately(canvasGroup.alpha, 0f)
                && !canvasGroup.blocksRaycasts
                && !canvasGroup.interactable
                && !adapter.IsVisible;

            LogResultStep(logger, "hidden-state-applied", result, passed, surfaceRoot, canvasGroup, adapter);
            return passed;
        }

        private static bool ValidateMissingSurface(
            FrameworkLogger logger,
            UnityFadeCurtainEffectAdapter adapter,
            TransitionOperationId operationId,
            string source)
        {
            var request = TransitionEffectRequest.Required(
                "qa.transition-effect.unity-fade-curtain.missing.required",
                TransitionEffectKind.Fade,
                operationId,
                TransitionKind.RouteSwitch,
                TransitionPhase.GateBlockApplied,
                source,
                "qa.transition-effect.unity-fade-curtain.missing-surface");

            var result = adapter.Execute(request);
            bool passed = result is { Failed: true, Completed: false, BlocksTransition: true, IssueCount: 1 }
                && !adapter.HasCanvasGroup;

            LogResultStep(logger, "required-missing-surface-blocks", result, passed, adapter.gameObject, null, adapter);
            return passed;
        }

        private static bool ValidateUnsupportedOptionalKind(
            FrameworkLogger logger,
            UnityFadeCurtainEffectAdapter adapter,
            TransitionOperationId operationId,
            string source)
        {
            var request = TransitionEffectRequest.Optional(
                "qa.transition-effect.unity-fade-curtain.loading.optional",
                TransitionEffectKind.LoadingScreen,
                operationId,
                TransitionKind.RouteSwitch,
                TransitionPhase.SceneOrContentOperationObserved,
                source,
                "qa.transition-effect.unity-fade-curtain.unsupported-optional-kind");

            var result = adapter.Execute(request);
            bool passed = result is { Rejected: true, Completed: false, BlocksTransition: false, IssueCount: 1 };

            LogResultStep(logger, "optional-unsupported-kind-nonblocking", result, passed, adapter.gameObject, null, adapter);
            return passed;
        }

        private static void LogResultStep(
            FrameworkLogger logger,
            string step,
            TransitionEffectResult result,
            bool passed,
            GameObject surfaceRoot,
            CanvasGroup canvasGroup,
            UnityFadeCurtainEffectAdapter adapter)
        {
            logger.Info(
                passed
                    ? "QA Unity Fade Curtain Effect Adapter Smoke step completed."
                    : "QA Unity Fade Curtain Effect Adapter Smoke step failed.",
                LogFields.Of(
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
                    LogFields.Field("rejected", result.Rejected),
                    LogFields.Field("failed", result.Failed),
                    LogFields.Field("blocksTransition", result.BlocksTransition),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("surfaceActive", surfaceRoot != null && surfaceRoot.activeSelf),
                    LogFields.Field("hasCanvasGroup", adapter != null && adapter.HasCanvasGroup),
                    LogFields.Field("alpha", canvasGroup != null ? canvasGroup.alpha : -1f),
                    LogFields.Field("visible", adapter != null && adapter.IsVisible)));

            logger.Debug("QA Unity Fade Curtain Effect Adapter Smoke result diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= 0.001f;
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
