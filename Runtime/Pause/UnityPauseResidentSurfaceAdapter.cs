using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Canonical Unity adapter for a resident Pause surface authored in UIGlobal.
    /// The adapter shows/hides an existing GameObject/CanvasGroup from Pause snapshots. It does not instantiate,
    /// bind ContentAnchors, own input, change Time.timeScale, or control Route/Activity lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Unity Pause Resident Surface Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10G canonical resident UIGlobal Pause surface adapter; no materialization, input, timeScale or lifecycle ownership.")]
    public sealed class UnityPauseResidentSurfaceAdapter : MonoBehaviour, IPauseSurfaceAdapter
    {
        [HideInInspector]
        [SerializeField] private string adapterName = "Unity Pause Resident Surface Adapter";

        [Header("Resident Surface")]
        [SerializeField] private GameObject surfaceRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visibility")]
        [SerializeField] private bool setSurfaceRootActive = true;
        [SerializeField] private bool applyHiddenStateOnAwake = true;
        [Range(0f, 1f)]
        [SerializeField] private float hiddenAlpha = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float visibleAlpha = 1f;
        [SerializeField] private bool blockRaycastsWhenPaused = true;
        [SerializeField] private bool interactableWhenPaused = true;

        [HideInInspector]
        [SerializeField] private PauseState lastAppliedState = PauseState.Unknown;
        [HideInInspector]
        [SerializeField] private bool lastVisibleState;
        [HideInInspector]
        [SerializeField] private string lastMessage = string.Empty;

        public string AdapterName => adapterName.NormalizeTextOrFallback(nameof(UnityPauseResidentSurfaceAdapter));

        public PauseState LastAppliedState => lastAppliedState;

        public bool LastVisibleState => lastVisibleState;

        public string LastMessage => lastMessage.NormalizeText();

        public bool IsVisible => lastVisibleState;

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool HasSurfaceRoot => TryResolveSurfaceRoot(out _);

        public float CurrentAlpha => TryResolveCanvasGroup(out var group) ? group.alpha : 0f;

        public bool CurrentBlocksRaycasts => TryResolveCanvasGroup(out var group) && group.blocksRaycasts;

        public bool CurrentInteractable => TryResolveCanvasGroup(out var group) && group.interactable;

        public bool Supports(PauseSnapshot snapshot)
        {
            return snapshot.IsValid;
        }

        public void Apply(PauseSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                lastAppliedState = PauseState.Unknown;
                lastVisibleState = false;
                lastMessage = "Unity Pause resident surface adapter rejected an invalid Pause snapshot.";
                return;
            }

            bool visible = snapshot.IsPaused;
            if (visible)
            {
                ApplyVisibleState();
            }
            else
            {
                ApplyHiddenState();
            }

            lastAppliedState = snapshot.State;
            lastVisibleState = visible;
            lastMessage = visible
                ? "Resident Pause surface applied visible state from Pause snapshot."
                : "Resident Pause surface applied hidden state from Pause snapshot.";
        }

        [ContextMenu("Immersive Framework/QA Apply Paused Visible State")]
        private void QaApplyPausedVisibleState()
        {
            Apply(PauseSnapshot.FromState(
                PauseState.Paused,
                nameof(UnityPauseResidentSurfaceAdapter),
                "qa.pause-resident-surface.visible",
                new[] { "QA visible Pause resident surface state." }));
        }

        [ContextMenu("Immersive Framework/QA Apply Running Hidden State")]
        private void QaApplyRunningHiddenState()
        {
            Apply(PauseSnapshot.FromState(
                PauseState.Running,
                nameof(UnityPauseResidentSurfaceAdapter),
                "qa.pause-resident-surface.hidden",
                new[] { "QA hidden Pause resident surface state." }));
        }

        private void Awake()
        {
            NormalizeConfiguration();

            if (applyHiddenStateOnAwake)
            {
                ApplyHiddenState();
                lastAppliedState = PauseState.Running;
                lastVisibleState = false;
                lastMessage = "Initial resident Pause surface hidden state applied on Awake.";
            }
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

        private void ApplyVisibleState()
        {
            if (setSurfaceRootActive && TryResolveSurfaceRoot(out var root))
            {
                root.SetActive(true);
            }

            if (TryResolveCanvasGroup(out var group))
            {
                group.alpha = visibleAlpha;
                group.blocksRaycasts = blockRaycastsWhenPaused;
                group.interactable = interactableWhenPaused;
            }
        }

        private void ApplyHiddenState()
        {
            if (TryResolveCanvasGroup(out var group))
            {
                group.alpha = hiddenAlpha;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            if (setSurfaceRootActive && TryResolveSurfaceRoot(out var root))
            {
                root.SetActive(false);
            }
        }

        private bool TryResolveSurfaceRoot(out GameObject root)
        {
            root = surfaceRoot != null ? surfaceRoot : gameObject;
            return root != null;
        }

        private bool TryResolveCanvasGroup(out CanvasGroup group)
        {
            group = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            return group != null;
        }

        private void NormalizeConfiguration()
        {
            adapterName = adapterName.NormalizeTextOrFallback(nameof(UnityPauseResidentSurfaceAdapter));
            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);

            if (surfaceRoot == null)
            {
                surfaceRoot = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
