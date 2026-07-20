#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Development-only read-only runtime diagnostics panel.
    /// QA execution and technical smokes live in QAFramework.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Framework QA Canvas")]
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "H2.2.13 read-only runtime diagnostics panel. Technical QA execution lives in QAFramework.")]
    public sealed class FrameworkQaCanvas : MonoBehaviour
    {
        private const string MissingRuntimeBindingDiagnostic =
            "Framework runtime diagnostics port is not bound.";

        [Header("Display")]
        [SerializeField] private bool showCanvas = true;

        [SerializeField]
        [Tooltip(
            "Legacy field retained for serialized compatibility. H2.2.13 keeps this diagnostic panel scene-scoped and ignores persistence.")]
        private bool persistAcrossSceneLoads;

        [SerializeField] private bool allowInPlayerBuild;

        [SerializeField]
        private Rect windowRect =
            new Rect(16f, 16f, 420f, 300f);

        [NonSerialized]
        private IFrameworkRuntimeDiagnosticsPort runtimeDiagnostics;

        [NonSerialized]
        private string runtimeDiagnosticsBindingDiagnostic =
            MissingRuntimeBindingDiagnostic;

        [NonSerialized]
        private int windowId;

        private static int nextWindowId = 36000;

        public bool HasFrameworkRuntimeDiagnosticsBinding =>
            runtimeDiagnostics != null;

        public string FrameworkRuntimeDiagnosticsBindingStatus =>
            HasFrameworkRuntimeDiagnosticsBinding
                ? "Bound"
                : "Missing";

        public string FrameworkRuntimeDiagnosticsBindingDiagnostic =>
            runtimeDiagnosticsBindingDiagnostic;

        public bool LegacyPersistenceRequested =>
            persistAcrossSceneLoads;

        private void Awake()
        {
            windowId = nextWindowId++;
        }

        private void OnGUI()
        {
            if (!CanRenderInCurrentBuild())
            {
                return;
            }

            if (!showCanvas)
            {
                if (GUI.Button(
                        new Rect(16f, 16f, 160f, 28f),
                        "Immersive Diagnostics"))
                {
                    showCanvas = true;
                }

                return;
            }

            windowRect = GUILayout.Window(
                windowId,
                windowRect,
                DrawWindow,
                "Immersive Framework Diagnostics");
        }

        internal bool TryBindFrameworkRuntimeDiagnostics(
            IFrameworkRuntimeDiagnosticsPort diagnosticsRuntime,
            out string issue)
        {
            if (diagnosticsRuntime == null)
            {
                issue = MissingRuntimeBindingDiagnostic;
                runtimeDiagnosticsBindingDiagnostic = issue;
                return false;
            }

            if (runtimeDiagnostics == null)
            {
                runtimeDiagnostics = diagnosticsRuntime;
                issue = string.Empty;
                runtimeDiagnosticsBindingDiagnostic =
                    "Framework runtime diagnostics port is bound.";
                return true;
            }

            if (ReferenceEquals(
                    runtimeDiagnostics,
                    diagnosticsRuntime))
            {
                issue = string.Empty;
                runtimeDiagnosticsBindingDiagnostic =
                    "Framework runtime diagnostics port binding is already applied.";
                return true;
            }

            issue =
                "Framework QA Canvas is already bound to a different diagnostics runtime for the current component lifetime.";
            runtimeDiagnosticsBindingDiagnostic = issue;
            return false;
        }

        private void DrawWindow(int id)
        {
            using (new GUILayout.VerticalScope())
            {
                DrawToolbar();
                DrawRuntimeStatus();
                DrawQaBoundary();
            }

            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    10000f,
                    24f));
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    "Runtime diagnostics",
                    GUILayout.ExpandWidth(true));
                if (GUILayout.Button(
                        "Hide",
                        GUILayout.Width(64f)))
                {
                    showCanvas = false;
                }
            }
        }

        private void DrawRuntimeStatus()
        {
            GUILayout.Space(8f);
            GUILayout.Label(
                "Application Runtime",
                GUI.skin.box);

            if (runtimeDiagnostics == null)
            {
                GUILayout.Label("Status: unavailable");
                GUILayout.Label(runtimeDiagnosticsBindingDiagnostic);
                return;
            }

            FrameworkRuntimeDiagnosticsSnapshot snapshot =
                runtimeDiagnostics.CreateFrameworkRuntimeDiagnosticsSnapshot();

            GUILayout.Label("Status: available");
            GUILayout.Label(
                $"Game Application: {Display(snapshot.ApplicationName)}");
            GUILayout.Label(
                $"Startup Route: {DisplayAsset(snapshot.StartupRoute)}");
            GUILayout.Label(
                $"Active Route: {Display(snapshot.CurrentRouteName)}");
            GUILayout.Label(
                $"Active Activity: {Display(snapshot.CurrentActivityName)}");
            GUILayout.Label(
                $"Content Anchor Bindings: {Math.Max(0, snapshot.ContentAnchorBindingCount)}");

            if (snapshot.HasPauseSnapshot)
            {
                GUILayout.Label(
                    $"Pause: {snapshot.PauseState} / Gate blockers: {Math.Max(0, snapshot.PauseGateBlockerCount)}");
            }
            else
            {
                GUILayout.Label("Pause: unavailable");
            }
        }

        private void DrawQaBoundary()
        {
            GUILayout.Space(8f);
            GUILayout.Label(
                "QA Boundary",
                GUI.skin.box);
            GUILayout.Label(
                "Technical smokes and synthetic QA run from QAFramework.");
            GUILayout.Label(
                "This package component is read-only and executes no Route, Activity, Reset or gameplay request.");

            if (persistAcrossSceneLoads)
            {
                GUILayout.Label(
                    "Legacy persistence was configured but is intentionally ignored. The panel is scene-scoped.");
            }
        }

        private bool CanRenderInCurrentBuild()
        {
#if UNITY_EDITOR
            return true;
#else
            return allowInPlayerBuild ||
                Debug.isDebugBuild;
#endif
        }

        private static string Display(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<none>"
                : value.Trim();
        }

        private static string DisplayAsset(UnityEngine.Object value)
        {
            return value != null
                ? value.name
                : "<none>";
        }
    }
}
#endif
