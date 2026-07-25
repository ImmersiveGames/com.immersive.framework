using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Minimal built-in Unity presentation adapter for the application-scoped
    /// Persistent Content Pause surface.
    ///
    /// It only projects a logical PauseSnapshot onto explicit CanvasGroup and
    /// surface-root references. It does not read input, own Pause state, mutate
    /// Time.timeScale, discover scene objects or create presentation content.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Pause/Unity Pause Surface Adapter")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Persistent Content Pause presentation adapter.")]
    public sealed class UnityPauseSurfaceAdapter :
        MonoBehaviour,
        IPauseSurfaceAdapter
    {
        [HideInInspector]
        [SerializeField]
        private string adapterName =
            "Unity Pause Surface Adapter";

        [Header("Surface")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private GameObject surfaceRoot;

        [Header("Initial Presentation")]
        [SerializeField]
        private bool applyRunningStateOnAwake = true;

        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField]
        private float runningAlpha;

        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField]
        private float pausedAlpha = 1f;

        [HideInInspector]
        [SerializeField]
        private bool setSurfaceRootActive = true;

        [HideInInspector]
        [SerializeField]
        private bool blockRaycastsWhenPaused = true;

        [HideInInspector]
        [SerializeField]
        private bool interactableWhenPaused = true;

        [Header("Runtime Diagnostics")]
        [SerializeField]
        [HideInInspector]
        private PauseState lastAppliedState =
            PauseState.Unknown;

        [SerializeField]
        [HideInInspector]
        private bool lastVisibleState;

        [SerializeField]
        [HideInInspector]
        private string lastDiagnostic =
            "Pause surface has not received a snapshot.";

        public string AdapterName =>
            adapterName.NormalizeTextOrFallback(
                nameof(UnityPauseSurfaceAdapter));

        public CanvasGroup CanvasGroup =>
            canvasGroup;

        public GameObject SurfaceRoot =>
            surfaceRoot;

        public bool ApplyRunningStateOnAwake =>
            applyRunningStateOnAwake;

        public PauseState LastAppliedState =>
            lastAppliedState;

        public bool IsVisible =>
            lastVisibleState;

        public string LastDiagnostic =>
            lastDiagnostic.NormalizeText();

        public bool Supports(
            PauseSnapshot snapshot)
        {
            return snapshot.IsValid;
        }

        public void Apply(
            PauseSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException(
                    "Unity Pause Surface Adapter requires a valid Pause snapshot.",
                    nameof(snapshot));
            }

            ValidateConfiguration();

            if (snapshot.IsPaused)
            {
                ApplyPausedState();
            }
            else
            {
                ApplyRunningState();
            }

            lastAppliedState =
                snapshot.State;
            lastDiagnostic =
                $"Pause surface applied state='{snapshot.State}' visible='{lastVisibleState}'.";
        }

        private void Awake()
        {
            NormalizeConfiguration();

            if (!applyRunningStateOnAwake)
            {
                return;
            }

            ValidateConfiguration();
            ApplyRunningState();
            lastAppliedState =
                PauseState.Running;
            lastDiagnostic =
                "Initial Running presentation applied on Awake.";
        }

        private void Reset()
        {
            canvasGroup =
                GetComponentInChildren<CanvasGroup>(
                    true);
            surfaceRoot =
                canvasGroup != null
                    ? canvasGroup.gameObject
                    : gameObject;
            NormalizeConfiguration();
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        private void ApplyPausedState()
        {
            if (setSurfaceRootActive &&
                !surfaceRoot.activeSelf)
            {
                surfaceRoot.SetActive(
                    true);
            }

            canvasGroup.alpha =
                pausedAlpha;
            canvasGroup.blocksRaycasts =
                blockRaycastsWhenPaused;
            canvasGroup.interactable =
                interactableWhenPaused;
            lastVisibleState =
                true;
        }

        private void ApplyRunningState()
        {
            canvasGroup.alpha =
                runningAlpha;
            canvasGroup.blocksRaycasts =
                false;
            canvasGroup.interactable =
                false;

            if (setSurfaceRootActive &&
                surfaceRoot.activeSelf)
            {
                surfaceRoot.SetActive(
                    false);
            }

            lastVisibleState =
                false;
        }

        private void ValidateConfiguration()
        {
            if (canvasGroup == null)
            {
                throw new InvalidOperationException(
                    "Unity Pause Surface Adapter requires an explicit CanvasGroup.");
            }

            if (surfaceRoot == null)
            {
                throw new InvalidOperationException(
                    "Unity Pause Surface Adapter requires an explicit Surface Root.");
            }

            if (canvasGroup.gameObject !=
                surfaceRoot)
            {
                throw new InvalidOperationException(
                    "Unity Pause Surface Adapter requires the CanvasGroup on the configured Surface Root.");
            }
        }

        private void NormalizeConfiguration()
        {
            runningAlpha =
                Mathf.Clamp01(
                    runningAlpha);
            pausedAlpha =
                Mathf.Clamp01(
                    pausedAlpha);
        }
    }
}
