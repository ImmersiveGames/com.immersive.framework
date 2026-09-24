using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.UI;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Minimal built-in Unity adapter for app/session-scoped loading surfaces.
    /// It mutates only configured UI references and optional surface/progress root active state.
    /// It does not animate over time, use DOTween, own loading lifecycle or discover other adapters.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Loading/Unity Loading Surface Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D/F26C minimal Unity loading surface adapter boundary; no registry, tweening or lifecycle ownership.")]
    public sealed class UnityLoadingSurfaceAdapter : MonoBehaviour, ILoadingSurfaceProgressPresentationAdapter
    {
        private const string MissingCanvasGroupIssue = "unity-loading-surface-canvas-group-missing";
        private const string MissingImageIssue = "unity-loading-surface-image-missing";
        private const string UnsupportedActionIssue = "unity-loading-surface-action-unsupported";
        private const string InvalidRequestIssue = "unity-loading-surface-request-invalid";
        private const string UnsupportedProgressMessage = "Loading surface request does not carry a supported progress source.";

        [HideInInspector]
        [SerializeField] private string adapterName = "Unity Loading Surface Adapter";

        [Header("Surface")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject surfaceRoot;

        [Header("Presentation")]
        [SerializeField] private Image surfaceImage;

        [Header("Progress Presentation")]
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Slider progressSlider;
        [Range(0f, 1f)]
        [SerializeField] private float initialProgressValue = 0f;
        [SerializeField] private bool showProgressRootWhenVisible = true;
        [SerializeField] private bool hideProgressRootWhenHidden = true;
        [SerializeField] private bool resetProgressOnHide = true;

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

        [HideInInspector]
        [SerializeField] private LoadingSurfaceResultStatus lastStatus = LoadingSurfaceResultStatus.Unknown;
        [HideInInspector]
        [SerializeField] private string lastMessage = string.Empty;
        [HideInInspector]
        [SerializeField] private bool lastVisibleState;
        [HideInInspector]
        [SerializeField] private bool lastProgressSupported;
        [HideInInspector]
        [SerializeField] private float lastProgressValue01;
        [HideInInspector]
        [SerializeField] private int lastProgressPercent;
        [HideInInspector]
        [SerializeField] private string lastProgressMessage = string.Empty;

        public string AdapterName => adapterName.NormalizeTextOrFallback(nameof(UnityLoadingSurfaceAdapter));

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool HasSurfaceImage => TryResolveSurfaceImage(out _);

        public bool HasProgressPresentation => progressRoot != null || progressFillImage != null || progressSlider != null;

        public bool IsVisible => lastVisibleState;

        public LoadingSurfaceResultStatus LastStatus => lastStatus;

        public string LastMessage => string.IsNullOrWhiteSpace(lastMessage) ? string.Empty : lastMessage;

        public bool LastProgressSupported => lastProgressSupported;

        public float LastProgressValue01 => lastProgressValue01;

        public int LastProgressPercent => lastProgressPercent;

        public string LastProgressMessage => string.IsNullOrWhiteSpace(lastProgressMessage) ? string.Empty : lastProgressMessage;

        public float CurrentAlpha => TryResolveCanvasGroup(out var group) ? group.alpha : 0f;

        public bool Supports(LoadingSurfaceRequest request)
        {
            return request.IsValid;
        }

        public LoadingSurfaceResult Show(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Show, true);
        }

        LoadingSurfaceResult ILoadingSurfaceAdapter.Update(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Update, request.ShouldBeVisible);
        }

        public LoadingSurfaceResult Hide(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Hide, false);
        }

        [ContextMenu("Immersive Framework/QA Apply Visible Loading State")]
        private void QaApplyVisibleState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyVisibleState(resolvedCanvasGroup);
                lastStatus = LoadingSurfaceResultStatus.Succeeded;
                lastMessage = "QA visible loading state applied.";
            }
        }

        [ContextMenu("Immersive Framework/QA Apply Hidden Loading State")]
        private void QaApplyHiddenState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyHiddenState(resolvedCanvasGroup);
                lastStatus = LoadingSurfaceResultStatus.Succeeded;
                lastMessage = "QA hidden loading state applied.";
            }
        }

        private void Awake()
        {
            NormalizeConfiguration();

            if (!applyHiddenStateOnAwake || !TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                return;
            }

            ApplyHiddenState(resolvedCanvasGroup);
            lastStatus = LoadingSurfaceResultStatus.Skipped;
            lastMessage = "Initial hidden state applied on Awake.";
        }

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            surfaceImage = GetComponent<Image>();
            surfaceRoot = gameObject;
            NormalizeConfiguration();
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        private LoadingSurfaceResult Apply(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction,
            bool visibleState)
        {
            if (!request.IsValid)
            {
                return Record(LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    "Unity loading surface adapter requires a valid request.",
                    new[] { InvalidRequestIssue }));
            }

            if (request.Action != expectedAction)
            {
                return Record(LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' expected action '{expectedAction}' but received '{request.Action}'.",
                    new[] { UnsupportedActionIssue }));
            }

            if (!TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                return Record(LoadingSurfaceResult.FailedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' requires a CanvasGroup surface.",
                    new[] { MissingCanvasGroupIssue }));
            }

            if (!TryResolveSurfaceImage(out _))
            {
                return Record(LoadingSurfaceResult.FailedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' requires a surface Image for the loading panel.",
                    new[] { MissingImageIssue }));
            }

            if (visibleState)
            {
                ApplyVisibleState(resolvedCanvasGroup, request);
                return Record(LoadingSurfaceResult.SucceededResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' applied visible loading state."));
            }

            ApplyHiddenState(resolvedCanvasGroup);
            return Record(LoadingSurfaceResult.SucceededResult(
                request,
                AdapterName,
                $"Adapter '{AdapterName}' applied hidden loading state."));
        }

        private void ApplyVisibleState(CanvasGroup resolvedCanvasGroup)
        {
            ApplyVisibleState(resolvedCanvasGroup, null);
        }

        private void ApplyVisibleState(CanvasGroup resolvedCanvasGroup, LoadingSurfaceRequest? request)
        {
            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            resolvedCanvasGroup.alpha = visibleAlpha;
            resolvedCanvasGroup.blocksRaycasts = blockRaycastsWhenVisible;
            resolvedCanvasGroup.interactable = interactableWhenVisible;
            lastVisibleState = true;
            ApplyProgressVisibleState(request);
        }

        private void ApplyHiddenState(CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup.alpha = hiddenAlpha;
            resolvedCanvasGroup.blocksRaycasts = false;
            resolvedCanvasGroup.interactable = false;
            ApplyProgressHiddenState();

            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && root.activeSelf)
            {
                root.SetActive(false);
            }

            lastVisibleState = false;
        }

        private void ApplyProgressVisibleState(LoadingSurfaceRequest? request)
        {
            if (!HasProgressPresentation)
            {
                return;
            }

            SetProgressRootVisible(showProgressRootWhenVisible);

            if (request is { ProgressSupported: true })
            {
                ApplyProgressValue(request.Value.Progress.NormalizedValue);
                lastProgressSupported = true;
                lastProgressValue01 = request.Value.Progress.NormalizedValue;
                lastProgressPercent = request.Value.Progress.PercentRounded;
                lastProgressMessage = $"Loading progress consumed from request percent='{lastProgressPercent}'.";
                return;
            }

            ApplyProgressValue(initialProgressValue);
            lastProgressSupported = false;
            lastProgressValue01 = initialProgressValue;
            lastProgressPercent = Mathf.RoundToInt(initialProgressValue * 100f);
            lastProgressMessage = UnsupportedProgressMessage;
        }

        private void ApplyProgressHiddenState()
        {
            if (!HasProgressPresentation)
            {
                return;
            }

            if (resetProgressOnHide)
            {
                ApplyProgressValue(0f);
                lastProgressValue01 = 0f;
                lastProgressPercent = 0;
            }

            lastProgressSupported = false;
            lastProgressMessage = "Loading progress hidden.";

            if (hideProgressRootWhenHidden)
            {
                SetProgressRootVisible(false);
            }
        }

        private void ApplyProgressValue(float value01)
        {
            value01 = NormalizeProgressValue(value01);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = value01;
            }

            if (progressSlider != null)
            {
                progressSlider.normalizedValue = value01;
            }
        }

        private void SetProgressRootVisible(bool visible)
        {
            var resolvedProgressRoot = ResolveProgressRoot();
            if (resolvedProgressRoot != null && resolvedProgressRoot.activeSelf != visible)
            {
                resolvedProgressRoot.SetActive(visible);
            }
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

        private bool TryResolveSurfaceImage(out Image resolvedSurfaceImage)
        {
            resolvedSurfaceImage = surfaceImage;
            if (resolvedSurfaceImage != null)
            {
                return true;
            }

            resolvedSurfaceImage = GetComponent<Image>();
            if (resolvedSurfaceImage == null)
            {
                return false;
            }

            surfaceImage = resolvedSurfaceImage;
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

        private GameObject ResolveProgressRoot()
        {
            if (progressRoot != null)
            {
                return progressRoot;
            }

            if (progressSlider != null)
            {
                progressRoot = progressSlider.gameObject;
                return progressRoot;
            }

            if (progressFillImage != null)
            {
                progressRoot = progressFillImage.gameObject;
                return progressRoot;
            }

            return null;
        }

        private void NormalizeConfiguration()
        {
            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);
            initialProgressValue = NormalizeProgressValue(initialProgressValue);
            lastProgressValue01 = NormalizeProgressValue(lastProgressValue01);
            lastProgressPercent = Mathf.Clamp(lastProgressPercent, 0, 100);

            if (surfaceRoot == null)
            {
                surfaceRoot = gameObject;
            }
        }

        private static float NormalizeProgressValue(float value01)
        {
            return float.IsNaN(value01) ? 0f : Mathf.Clamp01(value01);
        }

        private LoadingSurfaceResult Record(LoadingSurfaceResult result)
        {
            lastStatus = result.Status;
            lastMessage = result.Message;
            lastVisibleState = HasCanvasGroup && CurrentAlpha > 0.001f;
            return result;
        }
    }
}
