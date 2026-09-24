using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Minimal built-in Unity adapter for fade/curtain/blackout surfaces.
    /// It mutates only a configured CanvasGroup and optional surface root active state.
    /// It does not use DOTween, own Transition lifecycle, register itself globally or discover other adapters.
    /// Async execution completes only after the requested visual phase has settled.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Transition Effects/Unity Fade Curtain Effect Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D3 Unity fade/curtain adapter with explicit visual settle phases.")]
    public sealed class UnityFadeCurtainEffectAdapter : MonoBehaviour, IAsyncTransitionEffectAdapter
    {
        private const string MissingCanvasGroupIssue = "unity-fade-curtain-canvas-group-missing";
        private const string UnsupportedKindIssue = "unity-fade-curtain-kind-unsupported";
        private const string UnsupportedPhaseIssue = "unity-fade-curtain-phase-unsupported";

        private static readonly AnimationCurve LinearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [HideInInspector]
        [SerializeField] private string adapterName = "Unity Fade Curtain Effect Adapter";
        [HideInInspector]
        [SerializeField] private TransitionEffectKind effectKind = TransitionEffectKind.Fade;

        [Header("Surface")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject surfaceRoot;

        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField] private float hiddenAlpha = 0f;
        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField] private float visibleAlpha = 1f;
        [HideInInspector]
        [SerializeField] private bool setSurfaceRootActive = true;
        [HideInInspector]
        [SerializeField] private bool blockRaycastsWhenVisible = true;
        [HideInInspector]
        [SerializeField] private bool interactableWhenVisible;
        [HideInInspector]
        [SerializeField] private bool applyHiddenStateOnAwake = true;

        [Header("Timing")]
        [Tooltip("When enabled, async transition execution waits until the fade phase has visually settled.")]
        [HideInInspector]
        [SerializeField] private bool animateAsyncExecution = true;
        [Min(0f)]
        [SerializeField] private float fadeInSeconds = 0.25f;
        [Min(0f)]
        [SerializeField] private float fadeOutSeconds = 0.25f;
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [HideInInspector]
        [SerializeField] private TransitionEffectStatus lastStatus = TransitionEffectStatus.Unknown;
        [HideInInspector]
        [SerializeField] private string lastMessage = string.Empty;
        [HideInInspector]
        [SerializeField] private bool lastVisibleState;

        public string AdapterName => adapterName.NormalizeTextOrFallback(nameof(UnityFadeCurtainEffectAdapter));

        public TransitionEffectKind ConfiguredEffectKind => effectKind;

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool IsVisible => lastVisibleState;

        public TransitionEffectStatus LastStatus => lastStatus;

        public string LastMessage => string.IsNullOrWhiteSpace(lastMessage) ? string.Empty : lastMessage;

        public float CurrentAlpha => TryResolveCanvasGroup(out var group) ? group.alpha : 0f;

        public bool Supports(TransitionEffectKind candidateKind)
        {
            return IsSupportedKind(effectKind) && candidateKind == effectKind;
        }

        public TransitionEffectResult Execute(TransitionEffectRequest request)
        {
            if (!TryValidateRequest(request, out var failureResult, out var resolvedCanvasGroup, out bool shouldShow))
            {
                return Record(failureResult);
            }

            if (shouldShow)
            {
                ApplyVisibleStateImmediate(resolvedCanvasGroup);
                return Record(TransitionEffectResult.SucceededResult(
                    request,
                    $"Adapter '{AdapterName}' applied visible fade/curtain state immediately."));
            }

            ApplyHiddenStateImmediate(resolvedCanvasGroup);
            return Record(TransitionEffectResult.SucceededResult(
                request,
                $"Adapter '{AdapterName}' applied hidden fade/curtain state immediately."));
        }

        public async Awaitable<TransitionEffectResult> ExecuteAsync(TransitionEffectRequest request)
        {
            if (!TryValidateRequest(request, out var failureResult, out var resolvedCanvasGroup, out bool shouldShow))
            {
                return Record(failureResult);
            }

            await ApplyStateAsync(resolvedCanvasGroup, shouldShow);

            return Record(TransitionEffectResult.SucceededResult(
                request,
                shouldShow
                    ? $"Adapter '{AdapterName}' completed visible fade/curtain phase."
                    : $"Adapter '{AdapterName}' completed hidden fade/curtain phase."));
        }

        [ContextMenu("Immersive Framework/QA Apply Visible Curtain State")]
        private void QaApplyVisibleState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyVisibleStateImmediate(resolvedCanvasGroup);
                lastStatus = TransitionEffectStatus.Succeeded;
                lastMessage = "QA visible curtain state applied.";
            }
        }

        [ContextMenu("Immersive Framework/QA Apply Hidden Curtain State")]
        private void QaApplyHiddenState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyHiddenStateImmediate(resolvedCanvasGroup);
                lastStatus = TransitionEffectStatus.Succeeded;
                lastMessage = "QA hidden curtain state applied.";
            }
        }

        private void Awake()
        {
            NormalizeConfiguration();

            if (!applyHiddenStateOnAwake || !TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                return;
            }

            ApplyHiddenStateImmediate(resolvedCanvasGroup);
            lastStatus = TransitionEffectStatus.Skipped;
            lastMessage = "Initial hidden state applied on Awake.";
        }

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            surfaceRoot = gameObject;
            NormalizeConfiguration();
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        private bool TryValidateRequest(
            TransitionEffectRequest request,
            out TransitionEffectResult failureResult,
            out CanvasGroup resolvedCanvasGroup,
            out bool shouldShow)
        {
            failureResult = default;
            resolvedCanvasGroup = null;
            shouldShow = false;

            if (!request.IsValid)
            {
                throw new ArgumentException("Unity fade/curtain adapter requires a valid Transition Effect request.", nameof(request));
            }

            if (!Supports(request.EffectKind))
            {
                failureResult = TransitionEffectResult.RejectedResult(
                    request,
                    $"Adapter '{AdapterName}' does not support effect kind '{request.EffectKind}'.",
                    new[] { UnsupportedKindIssue });
                return false;
            }

            if (!TryResolveCanvasGroup(out resolvedCanvasGroup))
            {
                failureResult = TransitionEffectResult.FailedResult(
                    request,
                    $"Adapter '{AdapterName}' requires a CanvasGroup surface.",
                    new[] { MissingCanvasGroupIssue });
                return false;
            }

            if (ShouldApplyVisibleState(request.Phase))
            {
                shouldShow = true;
                return true;
            }

            if (ShouldApplyHiddenState(request.Phase))
            {
                shouldShow = false;
                return true;
            }

            failureResult = TransitionEffectResult.RejectedResult(
                request,
                $"Adapter '{AdapterName}' does not handle transition phase '{request.Phase}'.",
                new[] { UnsupportedPhaseIssue });
            return false;
        }

        private TransitionEffectResult Record(TransitionEffectResult result)
        {
            lastStatus = result.Status;
            lastMessage = result.Message;
            lastVisibleState = HasCanvasGroup && CurrentAlpha > 0.001f;
            return result;
        }

        private async Awaitable ApplyStateAsync(CanvasGroup resolvedCanvasGroup, bool visible)
        {
            if (!animateAsyncExecution)
            {
                if (visible)
                {
                    ApplyVisibleStateImmediate(resolvedCanvasGroup);
                }
                else
                {
                    ApplyHiddenStateImmediate(resolvedCanvasGroup);
                }

                return;
            }

            var root = ResolveSurfaceRoot();
            if (visible && setSurfaceRootActive && root != null && !root.activeSelf)
            {
                root.SetActive(true);
                await Awaitable.NextFrameAsync();
            }

            float targetAlpha = visible ? visibleAlpha : hiddenAlpha;
            float duration = visible ? fadeInSeconds : fadeOutSeconds;
            var curve = visible ? fadeInCurve : fadeOutCurve;

            if (visible)
            {
                resolvedCanvasGroup.blocksRaycasts = blockRaycastsWhenVisible;
                resolvedCanvasGroup.interactable = interactableWhenVisible;
            }

            await AnimateAlphaAsync(resolvedCanvasGroup, targetAlpha, duration, curve);

            if (!visible)
            {
                resolvedCanvasGroup.blocksRaycasts = false;
                resolvedCanvasGroup.interactable = false;

                if (setSurfaceRootActive && root != null && root.activeSelf)
                {
                    root.SetActive(false);
                }
            }

            lastVisibleState = visible;
        }

        private async Awaitable AnimateAlphaAsync(
            CanvasGroup resolvedCanvasGroup,
            float targetAlpha,
            float duration,
            AnimationCurve curve)
        {
            if (resolvedCanvasGroup == null)
            {
                return;
            }

            float startAlpha = resolvedCanvasGroup.alpha;
            if (duration <= 0f)
            {
                resolvedCanvasGroup.alpha = targetAlpha;
                Canvas.ForceUpdateCanvases();
                await Awaitable.NextFrameAsync();
                return;
            }

            curve = IsCurveUsable(curve) ? curve : LinearCurve;
            float elapsed = 0f;

            while (resolvedCanvasGroup != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float evaluated = Mathf.Clamp01(curve.Evaluate(normalized));
                resolvedCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, evaluated);
                Canvas.ForceUpdateCanvases();
                await Awaitable.NextFrameAsync();
            }

            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = targetAlpha;
                Canvas.ForceUpdateCanvases();
                await Awaitable.NextFrameAsync();
            }
        }

        private void ApplyVisibleStateImmediate(CanvasGroup resolvedCanvasGroup)
        {
            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            resolvedCanvasGroup.alpha = visibleAlpha;
            resolvedCanvasGroup.blocksRaycasts = blockRaycastsWhenVisible;
            resolvedCanvasGroup.interactable = interactableWhenVisible;
            Canvas.ForceUpdateCanvases();
            lastVisibleState = true;
        }

        private void ApplyHiddenStateImmediate(CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup.alpha = hiddenAlpha;
            resolvedCanvasGroup.blocksRaycasts = false;
            resolvedCanvasGroup.interactable = false;

            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && root.activeSelf)
            {
                root.SetActive(false);
            }

            Canvas.ForceUpdateCanvases();
            lastVisibleState = false;
        }

        private bool TryResolveCanvasGroup(out CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup = canvasGroup;
            if (resolvedCanvasGroup != null)
            {
                return true;
            }

            resolvedCanvasGroup = GetComponent<CanvasGroup>();
            if (resolvedCanvasGroup == null)
            {
                return false;
            }

            canvasGroup = resolvedCanvasGroup;
            return true;
        }

        private GameObject ResolveSurfaceRoot()
        {
            if (surfaceRoot != null)
            {
                return surfaceRoot;
            }

            surfaceRoot = gameObject;
            return surfaceRoot;
        }

        private void NormalizeConfiguration()
        {
            if (!IsSupportedKind(effectKind))
            {
                effectKind = TransitionEffectKind.Fade;
            }

            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);
            fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (!IsCurveUsable(fadeInCurve))
            {
                fadeInCurve = LinearCurve;
            }

            if (!IsCurveUsable(fadeOutCurve))
            {
                fadeOutCurve = LinearCurve;
            }

            if (surfaceRoot == null)
            {
                surfaceRoot = gameObject;
            }
        }

        private static bool IsSupportedKind(TransitionEffectKind candidateKind)
        {
            return candidateKind is TransitionEffectKind.Fade or TransitionEffectKind.Curtain or TransitionEffectKind.Blackout;
        }

        private static bool ShouldApplyVisibleState(TransitionPhase phase)
        {
            return phase is TransitionPhase.OperationOpened or TransitionPhase.GateBlockApplied or TransitionPhase.PreviousScopeExitObserved;
        }

        private static bool ShouldApplyHiddenState(TransitionPhase phase)
        {
            return phase is TransitionPhase.ReadinessObserved or TransitionPhase.GateBlockReleased or TransitionPhase.OperationClosed;
        }

        private static bool IsCurveUsable(AnimationCurve curve)
        {
            return curve is { keys: { Length: > 0 } };
        }
    }
}
