using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.LocalContribution;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Minimal development QA surface for exercising framework runtime requests.
    /// API status: Development Tooling. Compiled only in the Unity Editor or development builds.
    /// Uses IMGUI to avoid adding a UGUI/package dependency to the framework runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Framework QA Canvas")]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "QA smoke UI for Editor/development builds; not product API.")]
    public sealed class FrameworkQaCanvas : MonoBehaviour
    {
        private const string QaSource = nameof(FrameworkQaCanvas);

        private static FrameworkQaCanvas _current;
        private static int _nextWindowId = 36000;

        private FrameworkLogger _logger;
        private int _windowId;
        private bool _requestInFlight;
        private bool _showAdvancedControls;
        private Vector2 _scroll;

        [Header("Display")]
        [SerializeField] private bool showCanvas = true;
        [SerializeField] private bool persistAcrossSceneLoads = true;
        [SerializeField] private bool allowInPlayerBuild;
        [SerializeField] private Rect windowRect = new Rect(16f, 16f, 420f, 500f);

        [Header("Routes")]
        [SerializeField] private RouteAsset[] routeTargets = Array.Empty<RouteAsset>();
        [SerializeField] private string routeReason = "qa.route";

        [Header("Smoke Scenario Routes")]
        [FormerlySerializedAs("startupRoute")]
        [SerializeField] private RouteAsset canonicalRoute;
        [FormerlySerializedAs("secondaryRoute")]
        [SerializeField] private RouteAsset alternateRoute;
        [SerializeField] private RouteAsset noActivityRoute;

        [Header("Route Scene Composition Smoke")]
        [SerializeField] private RouteAsset routeSceneCompositionRoute;
        [SerializeField] private int expectedRouteSceneLoadedCount = 2;
        [SerializeField] private int expectedRouteSceneOwnedLoadedCount = 2;

        [Header("Route Release Smoke")]
        [SerializeField] private int expectedRouteReleaseReleasedCount = 1;

        [Header("Content Anchor Diagnostics Smoke")]
        [SerializeField] private int expectedContentAnchorCount = 1;
        [SerializeField] private int expectedContentAnchorCandidateCount = 1;
        [SerializeField] private int expectedContentAnchorIssueCount = 0;
        [SerializeField] private int expectedContentAnchorInvalidCount = 0;
        [SerializeField] private int expectedContentAnchorRouteMismatchCount = 0;
        [SerializeField] private int expectedContentAnchorRequiredCount = -1;
        [SerializeField] private int expectedContentAnchorOptionalCount = -1;
        [SerializeField] private int expectedContentAnchorRootCount = -1;
        [SerializeField] private int expectedContentAnchorSlotCount = -1;
        [SerializeField] private int expectedContentAnchorPointCount = -1;

        [Header("Activity Content Anchor Diagnostics Smoke")]
        [SerializeField] private int expectedActivityContentAnchorCount = 0;
        [SerializeField] private int expectedActivityContentAnchorCandidateCount = 0;
        [SerializeField] private int expectedActivityContentAnchorIssueCount = 0;
        [SerializeField] private int expectedActivityContentAnchorInvalidCount = 0;
        [SerializeField] private int expectedActivityContentAnchorActivityMismatchCount = 0;

        [Header("Activity Content Anchor Positive Smoke")]
        [SerializeField] private string positiveActivityContentAnchorId = "qa.activity.anchor.positive";
        [SerializeField] private ContentAnchorKind positiveActivityContentAnchorKind = ContentAnchorKind.Slot;
        [SerializeField] private ContentAnchorRequiredness positiveActivityContentAnchorRequiredness = ContentAnchorRequiredness.Optional;

        [Header("Smoke Scenario Activities")]
        [FormerlySerializedAs("activity01")]
        [SerializeField] private ActivityAsset primaryActivity;
        [FormerlySerializedAs("activity02")]
        [SerializeField] private ActivityAsset secondaryActivity;
        [SerializeField] private ActivityAsset noContentActivity;

        [Header("Baseline Reset")]
        [SerializeField] private RouteAsset resetRoute;
        [SerializeField] private ActivityAsset resetActivity;
        [SerializeField] private string resetReason = "qa.reset";

        [Header("Activities")]
        [SerializeField] private ActivityAsset[] activityTargets = Array.Empty<ActivityAsset>();
        [SerializeField] private string activityReason = "qa.activity";
        [SerializeField] private string clearActivityReason = "qa.activity.clear";

        private void Awake()
        {
            _logger = FrameworkLogger.Create<FrameworkQaCanvas>();
            EnsureWindowId();

            if (!persistAcrossSceneLoads)
            {
                return;
            }

            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }

            _current = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            if (!CanRenderInCurrentBuild())
            {
                return;
            }

            if (!showCanvas)
            {
                if (GUI.Button(new Rect(16f, 16f, 160f, 28f), "Immersive QA"))
                {
                    showCanvas = true;
                }

                return;
            }

            EnsureWindowId();
            windowRect = GUILayout.Window(_windowId, windowRect, DrawWindow, "Immersive Framework QA");
        }

        private void EnsureWindowId()
        {
            if (_windowId != 0)
            {
                return;
            }

            _windowId = _nextWindowId++;
        }

        private bool CanRenderInCurrentBuild()
        {
#if UNITY_EDITOR
            return true;
#else
            return allowInPlayerBuild || Debug.isDebugBuild;
#endif
        }

        private void DrawWindow(int windowId)
        {
            using (new GUILayout.VerticalScope())
            {
                DrawToolbar();

                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
                DrawRuntimeStatus();
                DrawQaScenarioSummary();
                DrawCoreSmokeControls();
                DrawDiagnosticSmokeControls();
                DrawAdvancedControls();
                GUILayout.EndScrollView();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("QA smoke control", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Hide", GUILayout.Width(64f)))
                {
                    showCanvas = false;
                }
            }
        }

        private void DrawRuntimeStatus()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Runtime", GUI.skin.box);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                GUILayout.Label("Application Runtime: unavailable");
                return;
            }

            var state = runtimeHost.State;
            GUILayout.Label("Application Runtime: available");
            GUILayout.Label($"Game Application: {GetName(state.GameApplication != null ? state.GameApplication.ApplicationName : string.Empty, "<none>")}");
            GUILayout.Label($"Active Route: {GetName(state.CurrentRouteName, "<none>")}");
            GUILayout.Label($"Active Activity: {GetName(state.CurrentActivityName, "<none>")}");
            GUILayout.Label($"Content Anchor Bindings: {runtimeHost.ContentAnchorBindingCount}");
            if (runtimeHost.TryGetPauseSnapshot(out var pauseSnapshot))
            {
                GUILayout.Label($"Pause State: {pauseSnapshot.State} / Gate blockers: {runtimeHost.PauseGateSnapshot.BlockerCount}");
            }
        }

        private void DrawQaScenarioSummary()
        {
            GUILayout.Space(8f);
            GUILayout.Label("QA Scenario", GUI.skin.box);
            GUILayout.Label($"Canonical Route: {GetAssetName(canonicalRoute, "<missing>")}");
            GUILayout.Label($"Alternate Route: {GetAssetName(alternateRoute, "<missing>")}");
            GUILayout.Label($"Composition Route: {GetAssetName(routeSceneCompositionRoute, "<missing>")}");
            GUILayout.Label($"Expected Route Scenes: {Math.Max(1, expectedRouteSceneLoadedCount)} loaded / {Math.Max(1, expectedRouteSceneOwnedLoadedCount)} owned");
            GUILayout.Label($"Expected Route Release: {Math.Max(0, expectedRouteReleaseReleasedCount)} released");
            GUILayout.Label($"Expected Content Anchors: {Math.Max(0, expectedContentAnchorCount)} anchors / {Math.Max(0, expectedContentAnchorCandidateCount)} candidates");
            GUILayout.Label($"Expected Activity Content Anchors: {Math.Max(0, expectedActivityContentAnchorCount)} anchors / {Math.Max(0, expectedActivityContentAnchorCandidateCount)} candidates");
            GUILayout.Label($"Primary Activity: {GetAssetName(primaryActivity, "<missing>")}");
            GUILayout.Label($"Secondary Activity: {GetAssetName(secondaryActivity, "<missing>")}");
            GUILayout.Label(GetCoreQaAssetsStatus());
        }

        private void DrawCoreSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Core Smokes", GUI.skin.box);
            GUILayout.Label("Default QA path. Phase diagnostics are collapsed below to keep the panel compact.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Standard Smoke"))
                {
                    RunStandardSmoke();
                }

                if (GUILayout.Button("Run Activity Baseline Smoke"))
                {
                    RunActivityBaselineSmoke();
                }

                if (GUILayout.Button("Validate Loaded Authoring"))
                {
                    ValidateLoadedLocalContributionsAuthoring();
                }

                if (GUILayout.Button("Reset QA Scenario"))
                {
                    ResetQaScenario();
                }
            }
        }

        private void DrawDiagnosticSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Current Diagnostics", GUI.skin.box);
            GUILayout.Label("Curated QA surface. Deprecated, intermediate and superseded smoke buttons were removed from the panel; their runner methods remain available in code history/tests if needed.");

            DrawLifecycleTailSmokeControls();
            DrawParticipantConsolidationSmokeControls();
            DrawRouteContentSmokeControls();
            DrawFoundationDiagnosticSmokeControls();
            DrawPauseConsumerSmokeControls();
        }

        private void DrawLifecycleTailSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Lifecycle Tail Diagnostics", GUI.skin.box);
            GUILayout.Label("Internal scope tail shell smoke for the Route/Activity lifecycle seam.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Scope Tail Operation Synthetic Smoke"))
                {
                    RunScopeTailOperationSyntheticSmoke();
                }
            }
        }


        private void DrawParticipantConsolidationSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Participant / CONS Diagnostics", GUI.skin.box);
            GUILayout.Label("Validation surface for the ParticipantExecutor pilot. Use before closing CONS-F.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Participant Executor Synthetic Smoke"))
                {
                    RunParticipantExecutorSyntheticSmoke();
                }

                if (GUILayout.Button("Run Cycle Reset Runtime Host Smoke"))
                {
                    RunCycleResetRuntimeHostSmoke();
                }

                if (GUILayout.Button("Run Cycle Reset Trigger Smoke"))
                {
                    RunCycleResetTriggerSmoke();
                }

                if (GUILayout.Button("Run Cycle Reset Bridge Smoke"))
                {
                    RunCycleResetBridgeSmoke();
                }

                if (GUILayout.Button("Run Object Reset Runtime Executor Smoke"))
                {
                    RunObjectResetRuntimeExecutorSmoke();
                }

                if (GUILayout.Button("Run Object Reset Runtime Host Integration Smoke"))
                {
                    RunObjectResetRuntimeHostIntegrationSmoke();
                }

                if (GUILayout.Button("Run Object Reset Trigger Smoke"))
                {
                    RunObjectResetTriggerSmoke();
                }

                if (GUILayout.Button("Run Object Reset Bridge Smoke"))
                {
                    RunObjectResetBridgeSmoke();
                }

                if (GUILayout.Button("Run Object Reset Foundation Closure Smoke"))
                {
                    RunObjectResetFoundationClosureSmoke();
                }
            }
        }






        private void DrawFoundationDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Foundation / F9R Diagnostics", GUI.skin.box);
            GUILayout.Label("Only current terminal proofs are exposed. Earlier F9R contract, placement, pipeline, bridge, preflight, gate and registry proof buttons were removed after being superseded.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Local Contribution Smoke"))
                {
                    RunLocalContributionSmoke();
                }

                if (GUILayout.Button("Run Runtime Content Smoke"))
                {
                    RunRuntimeContentSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Diagnostics Snapshot Smoke"))
                {
                    RunContentAnchorMaterializationDiagnosticsSnapshotSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Bridge Set Rollback Smoke"))
                {
                    RunContentAnchorMaterializationBridgeSetRollbackSmoke();
                }

                if (GUILayout.Button("Run Composite Lifecycle Release Smoke"))
                {
                    RunCompositeLifecycleReleaseSmoke();
                }
            }
        }


        private void DrawPauseConsumerSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Pause / F10 Diagnostics", GUI.skin.box);
            GUILayout.Label("Canonical Pause path: resident UIGlobal surface. Current Pause/InputMode bridge proofs are exposed below for QA alignment.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Pause Logical Toggle Resident Surface Smoke"))
                {
                    RunPauseLogicalToggleResidentSurfaceSmoke();
                }

                if (GUILayout.Button("Run Pause Runtime PlayerInput Bridge Smoke"))
                {
                    RunPauseInputModeUnityPlayerInputRuntimeBridgeSmoke();
                }

                if (GUILayout.Button("Run Pause InputAction Bridge Trigger Smoke"))
                {
                    RunPauseInputActionRuntimeBridgeTriggerSmoke();
                }
            }
        }


        private void DrawRouteContentSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Route / Content Diagnostics", GUI.skin.box);
            GUILayout.Label("Current route/content validation path only. Positive, binding-only and cleanup-only intermediate buttons were removed from the panel.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Route Scene Composition Smoke"))
                {
                    RunRouteSceneCompositionSmoke();
                }

                if (GUILayout.Button("Run Route Release Smoke"))
                {
                    RunRouteReleaseSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Diagnostics Smoke"))
                {
                    RunContentAnchorDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Activity Content Anchor Diagnostics Smoke"))
                {
                    RunActivityContentAnchorDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Activity Content Execution Participant Source Smoke"))
                {
                    RunActivityContentExecutionParticipantSourceSmoke();
                }
            }
        }

        private void DrawAdvancedControls()
        {
            GUILayout.Space(8f);
            _showAdvancedControls = GUILayout.Toggle(_showAdvancedControls, "Show advanced/manual controls");
            if (!_showAdvancedControls)
            {
                return;
            }

            DrawBaselineResetDiagnostics();
            DrawRouteRequests();
            DrawActivityRequests();
        }

        private void DrawBaselineResetDiagnostics()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Baseline Reset Diagnostics", GUI.skin.box);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                GUILayout.Label("Reset unavailable: Application Runtime is unavailable.");
                return;
            }

            var effectiveRoute = ResolveResetRoute(runtimeHost);
            var effectiveActivity = ResolveResetActivity(effectiveRoute);
            GUILayout.Label($"Reset Route: {GetAssetName(effectiveRoute, "<none>")}");
            GUILayout.Label($"Reset Activity: {GetAssetName(effectiveActivity, "<none>")}");
            GUILayout.Label($"Reset Reason: {ResolveReason(resetReason, "qa.reset")}");
        }

        private void DrawRouteRequests()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Route Requests", GUI.skin.box);
            GUILayout.Label($"Reason: {ResolveReason(routeReason, "qa.route")}");

            if (routeTargets == null || routeTargets.Length == 0)
            {
                GUILayout.Label("No Route targets configured.");
                return;
            }

            foreach (var route in routeTargets)
            {
                if (route == null)
                {
                    GUILayout.Label("Missing Route target.");
                    continue;
                }

                using (new EditorDisabledScope(_requestInFlight))
                {
                    if (GUILayout.Button($"Request Route: {GetAssetName(route, "<unnamed>")} "))
                    {
                        RequestRoute(route);
                    }
                }
            }
        }

        private void DrawActivityRequests()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Activity Requests", GUI.skin.box);
            GUILayout.Label($"Reason: {ResolveReason(activityReason, "qa.activity")}");

            if (activityTargets == null || activityTargets.Length == 0)
            {
                GUILayout.Label("No Activity targets configured.");
            }
            else
            {
                foreach (var activity in activityTargets)
                {
                    if (activity == null)
                    {
                        GUILayout.Label("Missing Activity target.");
                        continue;
                    }

                    using (new EditorDisabledScope(_requestInFlight))
                    {
                        if (GUILayout.Button($"Request Activity: {GetAssetName(activity, "<unnamed>")} "))
                        {
                            RequestActivity(activity);
                        }
                    }
                }
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Clear Reason: {ResolveReason(clearActivityReason, "qa.activity.clear")}");
            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Clear Active Activity"))
                {
                    ClearActivity();
                }
            }
        }

        private async void RequestRoute(RouteAsset route)
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Route Request ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (route == null)
            {
                EndOperation();
                _logger.Error("QA Route Request failed. Target Route is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Route Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await RequestRouteCoreAsync(runtimeHost, route, ResolveReason(routeReason, route.RouteName));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void RequestActivity(ActivityAsset activity)
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Activity Request ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (activity == null)
            {
                EndOperation();
                _logger.Error("QA Activity Request failed. Target Activity is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await RequestActivityCoreAsync(runtimeHost, activity, ResolveReason(activityReason, activity.ActivityName));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void ClearActivity()
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Activity Clear ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Clear failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void ResetQaScenario()
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Scenario reset ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Scenario reset failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                var targetRoute = ResolveResetRoute(runtimeHost);
                if (targetRoute == null)
                {
                    _logger.Warning("QA Scenario reset aborted. reason='Missing Reset Route'.");
                    return;
                }

                var targetActivity = ResolveResetActivity(targetRoute);
                string routeName = GetAssetName(targetRoute, "<none>");
                string activityName = GetAssetName(targetActivity, "<none>");
                _logger.Info($"QA Scenario reset started. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}'.");

                if (!await RequestRouteCoreAsync(runtimeHost, targetRoute, ResolveResetStepReason("route"), allowAlreadyActive: true))
                {
                    _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}' reason='Route request failed'.");
                    return;
                }

                await Task.Yield();
                if (targetActivity != null)
                {
                    if (!await RequestActivityCoreAsync(runtimeHost, targetActivity, ResolveResetStepReason("activity"), allowAlreadyActive: true))
                    {
                        _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}' reason='Activity request failed'.");
                        return;
                    }
                }
                else if (!await ClearActivityCoreAsync(runtimeHost, ResolveResetStepReason("clear"), allowNoActiveActivity: true))
                {
                    _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='<none>' reason='Activity clear failed'.");
                    return;
                }

                _logger.Info($"QA Scenario reset completed. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}'.");
            }
            catch (Exception exception)
            {
                _logger.Error($"QA Scenario reset failed. reason='{FormatValue(exception.Message)}'.");
            }
            finally
            {
                EndOperation();
            }
        }


        private async void RunParticipantExecutorSyntheticSmoke()
        {
            await RunSmokeAsync(ParticipantExecutorSyntheticSmokeRunner.SmokeName, runtimeHost =>
                ParticipantExecutorSyntheticSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunScopeTailOperationSyntheticSmoke()
        {
            await RunSmokeAsync(ScopeTailOperationSyntheticSmokeRunner.SmokeName, runtimeHost =>
                ScopeTailOperationSyntheticSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunGateAdmissionDiagnosticsSmoke()
        {
            await RunSmokeAsync(GateAdmissionQaSmokeRunner.SmokeName, runtimeHost =>
                GateAdmissionQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionDiagnosticsSmoke()
        {
            await RunSmokeAsync(TransitionQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionGateBlockerSmoke()
        {
            await RunSmokeAsync(TransitionGateBlockerQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionGateBlockerQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionOrchestrationObservationSmoke()
        {
            await RunSmokeAsync(TransitionOrchestrationObservationQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionOrchestrationObservationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionEffectDiagnosticsSmoke()
        {
            await RunSmokeAsync(TransitionEffectQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunUnityFadeCurtainEffectAdapterSmoke()
        {
            await RunSmokeAsync(TransitionEffectUnityFadeCurtainQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectUnityFadeCurtainQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionEffectPolicyGuardrailsSmoke()
        {
            await RunSmokeAsync(TransitionEffectPolicyQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectPolicyQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseDiagnosticsSmoke()
        {
            await RunSmokeAsync(PauseQaSmokeRunner.SmokeName, runtimeHost =>
                PauseQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseGateBlockerSmoke()
        {
            await RunSmokeAsync(PauseGateBlockerQaSmokeRunner.SmokeName, runtimeHost =>
                PauseGateBlockerQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseRuntimeRequestSmoke()
        {
            await RunSmokeAsync(PauseRuntimeRequestQaSmokeRunner.SmokeName, runtimeHost =>
                PauseRuntimeRequestQaSmokeRunner.RunRuntimeRequestSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunPauseBoundaryIntentSmoke()
        {
            await RunSmokeAsync(PauseBoundaryIntentQaSmokeRunner.SmokeName, runtimeHost =>
                PauseBoundaryIntentQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunUnityInputTargetOwnershipSmoke()
        {
            await RunSmokeAsync(UnityInputTargetOwnershipQaSmokeRunner.SmokeName, runtimeHost =>
                UnityInputTargetOwnershipQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeContractSmoke()
        {
            await RunSmokeAsync(InputModeContractQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeContractQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }


        private async void RunUnityInputOfficialComponentEvidenceSmoke()
        {
            await RunSmokeAsync(UnityInputOfficialComponentEvidenceQaSmokeRunner.SmokeName, runtimeHost =>
                UnityInputOfficialComponentEvidenceQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseInputModeRequestBoundarySmoke()
        {
            await RunSmokeAsync(PauseInputModeRequestBoundaryQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeRequestBoundaryQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunSessionPlayerInputManagerBoundarySmoke()
        {
            await RunSmokeAsync(SessionPlayerInputManagerBoundaryQaSmokeRunner.SmokeName, runtimeHost =>
                SessionPlayerInputManagerBoundaryQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPlayerActorIdentitySmoke()
        {
            await RunSmokeAsync(PlayerActorIdentityQaSmokeRunner.SmokeName, runtimeHost =>
                PlayerActorIdentityQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityApplicationPreviewSmoke()
        {
            await RunSmokeAsync(InputModeUnityApplicationPreviewQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityApplicationPreviewQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityActionMapPreviewSmoke()
        {
            await RunSmokeAsync(InputModeUnityActionMapPreviewQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityActionMapPreviewQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityApplicationPlanSmoke()
        {
            await RunSmokeAsync(InputModeUnityApplicationPlanQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityApplicationPlanQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputAdapterSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputAdapterQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputAdapterQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputApplicationSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputRequestApplicationSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputRequestApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputRequestApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }


        private async void RunPauseInputModeUnityPlayerInputApplicationSmoke()
        {
            await RunSmokeAsync(PauseInputModeUnityPlayerInputApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeUnityPlayerInputApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseInputModeUnityPlayerInputRuntimeBridgeSmoke()
        {
            await RunSmokeAsync(PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner.RunRuntimeBridgeSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunPauseInputActionRuntimeBridgeTriggerSmoke()
        {
            await RunSmokeAsync(PauseInputActionRuntimeBridgeTriggerQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputActionRuntimeBridgeTriggerQaSmokeRunner.RunTriggerSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunSnapshotParticipantDiagnosticsSmoke()
        {
            await RunSmokeAsync(SnapshotParticipantQaSmokeRunner.SmokeName, runtimeHost =>
                SnapshotParticipantQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPreferencesStoreDiagnosticsSmoke()
        {
            await RunSmokeAsync(PreferencesQaSmokeRunner.SmokeName, runtimeHost =>
                PreferencesQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunProgressionSaveJsonBackendSmoke()
        {
            await RunSmokeAsync(ProgressionSaveQaSmokeRunner.SmokeName, runtimeHost =>
                ProgressionSaveQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunProgressionSaveRuntimeRequestSmoke()
        {
            await RunSmokeAsync(ProgressionSaveRuntimeQaSmokeRunner.SmokeName, runtimeHost =>
                ProgressionSaveRuntimeQaSmokeRunner.RunRuntimeRequestSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingProgressAggregationSmoke()
        {
            await RunSmokeAsync(LoadingProgressQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingProgressQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingObservationAdapterSmoke()
        {
            await RunSmokeAsync(LoadingObservationQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingObservationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingReadinessObservationSmoke()
        {
            await RunSmokeAsync(LoadingReadinessQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingReadinessQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingResultAndIssueSmoke()
        {
            await RunSmokeAsync(LoadingResultQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingResultQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingScreenAdapterBoundarySmoke()
        {
            await RunSmokeAsync(LoadingScreenAdapterQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingScreenAdapterQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunCycleResetRuntimeHostSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.SmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunRuntimeHostSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    logParticipantDetails: false,
                    emitSmokeEnvelope: false));
        }

        private async void RunCycleResetTriggerSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.TriggerSmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunTriggerSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    emitSmokeEnvelope: false));
        }

        private async void RunCycleResetBridgeSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.BridgeSmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunBridgeSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    emitSmokeEnvelope: false));
        }

        private async void RunObjectEntryFoundationClosureSmoke()
        {
            await RunSmokeAsync(ObjectEntryQaSmokeRunner.FoundationClosureSmokeName, runtimeHost =>
                ObjectEntryQaSmokeRunner.RunFoundationClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }


        private async void RunObjectResetRuntimeExecutorSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.RuntimeExecutorSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunRuntimeExecutorSmokeAsync(_logger, QaSource));
        }

        private async void RunObjectResetRuntimeHostIntegrationSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.RuntimeHostIntegrationSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunRuntimeHostIntegrationSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetTriggerSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.TriggerSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunTriggerSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetBridgeSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.BridgeSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunBridgeSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetFoundationClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.FoundationClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunFoundationClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetUnityAdaptersClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.UnityAdaptersClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunUnityAdaptersClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetGameObjectActiveClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.GameObjectActiveClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunGameObjectActiveClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunStandardSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingTargets, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Standard Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Standard Smoke", async runtimeHost =>
            {
                if (!await RequestRouteCoreAsync(runtimeHost, alternateRoute, ResolveReason(routeReason, GetAssetName(alternateRoute, "QA Alternate Route"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestRouteCoreAsync(runtimeHost, canonicalRoute, ResolveReason(routeReason, GetAssetName(canonicalRoute, "QA Canonical Route"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestActivityCoreAsync(runtimeHost, secondaryActivity, ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestActivityCoreAsync(runtimeHost, primaryActivity, ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear")))
                {
                    return false;
                }

                await Task.Yield();
                return await RequestActivityCoreAsync(runtimeHost, primaryActivity, ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
            });
        }

        private async void RunActivityBaselineSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Activity Baseline Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Baseline Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity"))))
                    {
                        return false;
                    }

                    await Task.Yield();
                }


                var secondaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    secondaryActivity,
                    ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity")));
                if (!ValidateActivityBaselineStartedStep(secondaryResult, "secondary", secondaryActivity, requireActivityContent: true))
                {
                    return false;
                }

                await Task.Yield();

                var primaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                if (!ValidateActivityBaselineStartedStep(primaryResult, "primary", primaryActivity, requireActivityContent: false))
                {
                    return false;
                }

                await Task.Yield();

                var clearResult = await ClearActivityWithResultCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"));
                if (!ValidateActivityBaselineClearedStep(clearResult, "clear"))
                {
                    return false;
                }

                await Task.Yield();

                var restoreResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                return ValidateActivityBaselineStartedStep(restoreResult, "restore-primary", primaryActivity, requireActivityContent: false);
            });
        }

private async void RunLocalContributionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Local Contribution Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Local Contribution Smoke", async runtimeHost =>
            {
                if (!ValidateLocalContributionSmokeStep("loaded"))
                {
                    return false;
                }

                var secondaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    secondaryActivity,
                    ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity")));
                if (!secondaryResult.Succeeded)
                {
                    _logger.Warning($"QA Local Contribution Smoke step failed. step='secondary' reason='Activity request did not succeed' status='{FormatValue(secondaryResult.Kind.ToString())}'.");
                    return false;
                }

                await Task.Yield();
                if (!ValidateLocalContributionSmokeStep("secondary"))
                {
                    return false;
                }

                var primaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                if (!primaryResult.Succeeded)
                {
                    _logger.Warning($"QA Local Contribution Smoke step failed. step='primary' reason='Activity restore request did not succeed' status='{FormatValue(primaryResult.Kind.ToString())}'.");
                    return false;
                }

                await Task.Yield();
                return ValidateLocalContributionSmokeStep("primary");
            });
        }

        private bool ValidateLocalContributionSmokeStep(string stepName)
        {
            EnsureLogger();

            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            RouteContentBinding[] routeBindings = FindObjectsByType<RouteContentBinding>(
                FindObjectsInactive.Include);
            ActivityLocalVisibilityAdapter[] activityAdapters = FindObjectsByType<ActivityLocalVisibilityAdapter>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings?.Length ?? 0;
            int activityAdapterCount = activityAdapters?.Length ?? 0;
            if (routeBindingCount == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No RouteContentBinding found in loaded scenes' routeBindings='0' activityAdapters='{activityAdapterCount}'.");
                return false;
            }

            if (activityAdapterCount == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No ActivityLocalVisibilityAdapter found in loaded scenes' routeBindings='{routeBindingCount}' activityAdapters='0'.");
                return false;
            }

            var validationResult = LocalContributionValidator.ValidateLoadedSceneAuthored();
            var contributionSet = validationResult.ContributionSet;

            if (!validationResult.Succeeded)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Local contribution validation has blocking issues'. {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.Count == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No valid local contribution handles were discovered'. routeBindings='{routeBindingCount}' activityAdapters='{activityAdapterCount}' {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.RouteContentBindingCount != routeBindingCount
                || contributionSet.ActivityLocalVisibilityAdapterCount != activityAdapterCount)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Discovered contribution count does not match loaded authoring component count'. routeBindings='{routeBindingCount}' activityAdapters='{activityAdapterCount}' {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.RequiredCount + contributionSet.OptionalCount != contributionSet.Count)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Requiredness count does not match contribution count'. {validationResult.ToDiagnosticString()}");
                return false;
            }

            _logger.Info(
                "QA Local Contribution Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("routeBindings", routeBindingCount),
                    LogFields.Field("activityAdapters", activityAdapterCount),
                    LogFields.Field("localContributions", validationResult.ContributionCount),
                    LogFields.Field("validationIssues", validationResult.IssueCount),
                    LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                    LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                    LogFields.Field("handles", contributionSet.Count),
                    LogFields.Field("route", contributionSet.RouteCount),
                    LogFields.Field("activity", contributionSet.ActivityCount),
                    LogFields.Field("required", contributionSet.RequiredCount),
                    LogFields.Field("optional", contributionSet.OptionalCount)));
            _logger.Debug(
                "QA Local Contribution Smoke diagnostics.",
                LogFields.Field("details", validationResult.ToDiagnosticString()));
            return true;
        }

        private async void RunRuntimeContentSmoke()
        {
            await RunSmokeAsync("Runtime Content Smoke", runtimeHost =>
                Task.FromResult(RunRuntimeContentSmokeCore(runtimeHost)));
        }

        private bool RunRuntimeContentSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Runtime Content Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            string ownerId = "qa.runtime-content.smoke." + Guid.NewGuid().ToString("N");
            var owner = RuntimeContentOwner.Transient(ownerId, "QA Runtime Content Smoke");
            var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(owner, QaSource, "qa.runtime-content.root.create");
            if (!rootCreateResult.Applied && rootCreateResult.Status != RuntimeRootRegistryOperationStatus.RootAlreadyExists)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                return false;
            }

            if (!runtimeContentRuntime.TryCreateScopeContext(owner, QaSource, "qa.runtime-content.context", out var context))
            {
                _logger.Warning("QA Runtime Content Smoke failed. step='context' reason='Runtime scope context was not created'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.missing-context");
                return false;
            }

            var resource = RuntimeMaterializationResource.From(
                "QA",
                "qa.runtime-content.resource",
                "QA Runtime Content Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    "qa.runtime-content.handle",
                    resource,
                    QaSource,
                    "qa.runtime-content.materialization.request",
                    out var request,
                    out var requestGuardResult))
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.request-failed");
                return false;
            }

            var handle = RuntimeContentHandle.Declared(
                request.Identity,
                QaSource,
                "qa.runtime-content.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                request,
                handle,
                QaSource,
                "qa.runtime-content.materialization.synthetic-result",
                "Synthetic runtime content smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.runtime-content.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='materialization-apply' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.materialization-failed");
                return false;
            }

            if (!runtimeContentRuntime.TryGetHandle(context, request.Identity, out var registeredHandle) || registeredHandle == null || !registeredHandle.IsMaterialized)
            {
                _logger.Warning("QA Runtime Content Smoke failed. step='registered-handle' reason='Materialized handle was not registered in the runtime scope root'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.unregistered-handle");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.unregistered-handle");
                return false;
            }

            if (!runtimeContentRuntime.IsMaterializationRequestCurrent(
                    request,
                    QaSource,
                    "qa.runtime-content.guard.active-check",
                    out var activeGuardResult))
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='guard-active' guard='{activeGuardResult.Status}' message='{FormatValue(activeGuardResult.Message)}'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.guard-active-failed");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.guard-active-failed");
                return false;
            }

            var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                context,
                request.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.runtime-content.release.logical");
            if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='release' status='{releaseResult.Status}' unregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.release-failed");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.release-failed");
                return false;
            }

            var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.root.remove");
            if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                return false;
            }

            bool staleRequestAllowed = runtimeContentRuntime.IsMaterializationRequestCurrent(
                request,
                QaSource,
                "qa.runtime-content.guard.stale-check",
                out var staleGuardResult);
            if (staleRequestAllowed || !staleGuardResult.Rejected)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='guard-stale' allowed='{staleRequestAllowed}' guard='{staleGuardResult.Status}' message='{FormatValue(staleGuardResult.Message)}'.");
                return false;
            }

            _logger.Info($"QA Runtime Content Smoke step completed. rootCreate='{rootCreateResult.Status}' materialization='{appliedMaterializationResult.Status}' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' rootRemove='{rootRemoveResult.Status}' staleGuard='{staleGuardResult.Status}' runtimeRootCount='{runtimeContentRuntime.RootCount}'.");
            return true;
        }


        private async void RunLifecycleMaterializationRegistryContractSmoke()
        {
            await RunSmokeAsync("Lifecycle Materialization Registry Contract Smoke", runtimeHost =>
                Task.FromResult(RunLifecycleMaterializationRegistryContractSmokeCore(runtimeHost)));
        }

        private bool RunLifecycleMaterializationRegistryContractSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var registry = new LifecycleMaterializationRegistry();
            var owner = RuntimeContentOwner.Transient(
                "qa.lifecycle-materialization-registry.owner",
                "QA Lifecycle Materialization Registry Contract Smoke");
            var identity = RuntimeContentIdentity.From(
                owner,
                "qa.lifecycle-materialization-registry.content");
            var handle = RuntimeContentHandle.Materialized(
                identity,
                QaSource,
                "qa.lifecycle-materialization-registry.handle.materialized");

            var registerResult = registry.Register(
                handle,
                QaSource,
                "qa.lifecycle-materialization-registry.register");
            bool registered = registerResult.Succeeded
                && registerResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
                && registerResult.HasEntry
                && registry.Count == 1
                && registry.ActiveCount == 1
                && registry.ReleaseRequestedCount == 0
                && registry.ReleasedCount == 0
                && registry.ReleaseFailedCount == 0
                && registry.Snapshot(owner).Length == 1
                && registry.Snapshot(RuntimeContentScope.Transient).Length == 1;
            if (!registered)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='register' diagnostics='{registerResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var duplicateResult = registry.Register(
                handle,
                QaSource,
                "qa.lifecycle-materialization-registry.duplicate");
            bool duplicateStable = duplicateResult.Succeeded
                && duplicateResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededAlreadyRegistered
                && registry.Count == 1
                && registry.ActiveCount == 1;
            if (!duplicateStable)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var conflictingHandle = RuntimeContentHandle.Materialized(
                identity,
                QaSource,
                "qa.lifecycle-materialization-registry.handle.conflict");
            var conflictingDuplicateResult = registry.Register(
                conflictingHandle,
                QaSource,
                "qa.lifecycle-materialization-registry.duplicate-conflict");
            bool conflictingDuplicateRejected = conflictingDuplicateResult.Failed
                && conflictingDuplicateResult.Status == LifecycleMaterializationRegistryOperationStatus.RejectedDuplicateEntry
                && registry.Count == 1
                && registry.ActiveCount == 1;
            if (!conflictingDuplicateRejected)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='duplicate-conflict' diagnostics='{conflictingDuplicateResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var releaseRequestResult = registry.RequestRelease(
                identity,
                QaSource,
                "qa.lifecycle-materialization-registry.release.request");
            bool releaseRequested = releaseRequestResult.Succeeded
                && releaseRequestResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededReleaseRequested
                && registry.ActiveCount == 0
                && registry.ReleaseRequestedCount == 1
                && registry.ReleasedCount == 0;
            if (!releaseRequested)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='release-request' diagnostics='{releaseRequestResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var releasedResult = registry.MarkReleased(
                identity,
                QaSource,
                "qa.lifecycle-materialization-registry.release.mark-released");
            bool released = releasedResult.Succeeded
                && releasedResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededReleased
                && registry.ActiveCount == 0
                && registry.ReleaseRequestedCount == 0
                && registry.ReleasedCount == 1
                && registry.ReleaseFailedCount == 0;
            if (!released)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='released' diagnostics='{releasedResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var missingIdentity = RuntimeContentIdentity.From(
                owner,
                "qa.lifecycle-materialization-registry.missing");
            var missingReleaseResult = registry.RequestRelease(
                missingIdentity,
                QaSource,
                "qa.lifecycle-materialization-registry.release.missing");
            bool missingRejected = missingReleaseResult.Failed
                && missingReleaseResult.Status == LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry
                && registry.Count == 1
                && registry.ReleasedCount == 1;
            if (!missingRejected)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='missing-release' diagnostics='{missingReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            bool registryOwnsEvidenceOnly = registry.Count == 1
                && registry.HasEntries
                && registry.Snapshot().Length == 1
                && registry.Snapshot(owner).Length == 1
                && registry.Snapshot(RuntimeContentScope.Route).Length == 0
                && handle.IsMaterialized;
            if (!registryOwnsEvidenceOnly)
            {
                _logger.Warning($"QA Lifecycle Materialization Registry Contract Smoke step failed. step='evidence-only' registry='{registry.ToDiagnosticString()}' handle='{handle.ToDiagnosticString()}'.");
                return false;
            }

            _logger.Info(
                "QA Lifecycle Materialization Registry Contract Smoke step completed. "
                + $"step='lifecycle-materialization-registry-contract' passed='True' register='{registerResult.Status}' duplicate='{duplicateResult.Status}' duplicateConflict='{conflictingDuplicateResult.Status}' releaseRequest='{releaseRequestResult.Status}' releaseComplete='{releasedResult.Status}' missingRelease='{missingReleaseResult.Status}' entries='{registry.Count}' active='{registry.ActiveCount}' releaseRequested='{registry.ReleaseRequestedCount}' released='{registry.ReleasedCount}' releaseFailed='{registry.ReleaseFailedCount}' typedIdentity='True' registryOwnsEvidenceOnly='{registryOwnsEvidenceOnly}' physicalRelease='False' logicalRuntimeContentRelease='False' contentAnchorBindingCleanup='False' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
            return true;
        }

        private async void RunPauseLogicalToggleResidentSurfaceSmoke()
        {
            await RunSmokeAsync("Pause Logical Toggle Resident Surface Smoke", runtimeHost =>
            {
                GameObject surfaceObject = null;

                try
                {
                    surfaceObject = new GameObject("QA Pause Logical Toggle Resident Surface");
                    var canvasGroup = surfaceObject.AddComponent<CanvasGroup>();
                    var adapter = surfaceObject.AddComponent<UnityPauseResidentSurfaceAdapter>();
                    var surfaceRuntime = PauseSurfaceRuntime.Create(
                        _logger,
                        new IPauseSurfaceAdapter[] { adapter },
                        "QA Pause Logical Toggle Resident Surface");

                    var initialResumeResult = runtimeHost.RequestPause(
                        PauseRequestKind.Resume,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.initial-resume");
                    if (!runtimeHost.TryGetPauseSnapshot(out var initialSnapshot))
                    {
                        _logger.Warning("QA Pause Logical Toggle Resident Surface Smoke step failed. step='snapshot-initial' reason='Pause snapshot unavailable'.");
                        return Task.FromResult(false);
                    }

                    var initialSurfaceResult = surfaceRuntime.ApplySnapshot(
                        initialSnapshot,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.initial-hidden");
                    bool initialHidden = initialSnapshot.IsRunning
                        && !surfaceObject.activeSelf
                        && Math.Abs(canvasGroup.alpha) <= 0.0001f
                        && !canvasGroup.blocksRaycasts
                        && !canvasGroup.interactable;

                    var pauseResult = runtimeHost.RequestPause(
                        PauseRequestKind.Toggle,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.toggle-pause");
                    if (!runtimeHost.TryGetPauseSnapshot(out var pausedSnapshot))
                    {
                        _logger.Warning("QA Pause Logical Toggle Resident Surface Smoke step failed. step='snapshot-paused' reason='Pause snapshot unavailable'.");
                        return Task.FromResult(false);
                    }

                    var pausedSurfaceResult = surfaceRuntime.ApplySnapshot(
                        pausedSnapshot,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.show");
                    bool pausedVisible = pauseResult.Applied
                        && pausedSnapshot.IsPaused
                        && surfaceObject.activeSelf
                        && adapter.IsVisible
                        && adapter.LastAppliedState == PauseState.Paused
                        && Math.Abs(canvasGroup.alpha - 1f) <= 0.0001f
                        && canvasGroup.blocksRaycasts
                        && canvasGroup.interactable;

                    bool rootActiveWhenPaused = surfaceObject.activeSelf;
                    float pausedAlpha = canvasGroup.alpha;
                    bool pausedBlocksRaycasts = canvasGroup.blocksRaycasts;
                    bool pausedInteractable = canvasGroup.interactable;

                    var resumeResult = runtimeHost.RequestPause(
                        PauseRequestKind.Toggle,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.toggle-resume");
                    if (!runtimeHost.TryGetPauseSnapshot(out var resumedSnapshot))
                    {
                        _logger.Warning("QA Pause Logical Toggle Resident Surface Smoke step failed. step='snapshot-resumed' reason='Pause snapshot unavailable'.");
                        return Task.FromResult(false);
                    }

                    var resumedSurfaceResult = surfaceRuntime.ApplySnapshot(
                        resumedSnapshot,
                        QaSource,
                        "qa.pause-logical-toggle-resident-surface.hide");
                    bool resumedHidden = resumeResult.Applied
                        && resumedSnapshot.IsRunning
                        && !surfaceObject.activeSelf
                        && !adapter.IsVisible
                        && adapter.LastAppliedState == PauseState.Running
                        && Math.Abs(canvasGroup.alpha) <= 0.0001f
                        && !canvasGroup.blocksRaycasts
                        && !canvasGroup.interactable;

                    bool logicalToggleApplied = pauseResult.Applied
                        && resumeResult.Applied
                        && pauseResult.CurrentState == PauseState.Paused
                        && resumeResult.CurrentState == PauseState.Running;
                    bool residentSurfaceAppliedFromPauseSnapshot = pausedSurfaceResult.Succeeded
                        && resumedSurfaceResult.Succeeded
                        && pausedSurfaceResult.Snapshot.IsPaused
                        && resumedSurfaceResult.Snapshot.IsRunning;
                    bool passed = surfaceRuntime.HasVisibleSurface
                        && surfaceRuntime.AdapterCount == 1
                        && initialSurfaceResult.Succeeded
                        && initialHidden
                        && pausedVisible
                        && resumedHidden
                        && logicalToggleApplied
                        && residentSurfaceAppliedFromPauseSnapshot;

                    if (!passed)
                    {
                        _logger.Warning(
                            $"QA Pause Logical Toggle Resident Surface Smoke step failed. step='pause-logical-toggle-resident-surface' initialResume='{initialResumeResult.Status}' pause='{pauseResult.Status}' resume='{resumeResult.Status}' initialSurface='{initialSurfaceResult.StatusText}' pausedSurface='{pausedSurfaceResult.StatusText}' resumedSurface='{resumedSurfaceResult.StatusText}' initialHidden='{initialHidden}' pausedVisible='{pausedVisible}' resumedHidden='{resumedHidden}' logicalToggleApplied='{logicalToggleApplied}' residentSurfaceAppliedFromPauseSnapshot='{residentSurfaceAppliedFromPauseSnapshot}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause Logical Toggle Resident Surface Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-logical-toggle-resident-surface"),
                            LogFields.Field("passed", true),
                            LogFields.Field("initialResume", initialResumeResult.Status.ToString()),
                            LogFields.Field("pauseRequest", pauseResult.Status.ToString()),
                            LogFields.Field("resumeRequest", resumeResult.Status.ToString()),
                            LogFields.Field("previousState", pauseResult.PreviousState.ToString()),
                            LogFields.Field("pausedState", pauseResult.CurrentState.ToString()),
                            LogFields.Field("resumedState", resumeResult.CurrentState.ToString()),
                            LogFields.Field("surfaceRuntime", pausedSurfaceResult.StatusText),
                            LogFields.Field("adapterCount", surfaceRuntime.AdapterCount),
                            LogFields.Field("supportedAdapters", pausedSurfaceResult.SupportedAdapterCount),
                            LogFields.Field("appliedAdapters", pausedSurfaceResult.AppliedAdapterCount),
                            LogFields.Field("initialHidden", initialHidden),
                            LogFields.Field("pausedVisible", pausedVisible),
                            LogFields.Field("resumedHidden", resumedHidden),
                            LogFields.Field("logicalToggleApplied", logicalToggleApplied),
                            LogFields.Field("residentSurfaceAppliedFromPauseSnapshot", residentSurfaceAppliedFromPauseSnapshot),
                            LogFields.Field("rootActiveWhenPaused", pausedVisible && rootActiveWhenPaused),
                            LogFields.Field("rootInactiveWhenRunning", resumedHidden && !surfaceObject.activeSelf),
                            LogFields.Field("canvasAlphaPaused", pausedAlpha.ToString("0.00")),
                            LogFields.Field("blocksRaycastsWhenPaused", pausedVisible && pausedBlocksRaycasts),
                            LogFields.Field("interactableWhenPaused", pausedVisible && pausedInteractable),
                            LogFields.Field("canonicalResidentUIGlobalSurface", true),
                            LogFields.Field("materialization", false),
                            LogFields.Field("contentAnchorBinding", false),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("explicitSubmit", true),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    try
                    {
                        runtimeHost.RequestPause(
                            PauseRequestKind.Resume,
                            QaSource,
                            "qa.pause-logical-toggle-resident-surface.cleanup-resume");
                    }
                    catch (Exception exception)
                    {
                        _logger.Warning($"QA Pause Logical Toggle Resident Surface Smoke cleanup warning. reason='{FormatValue(exception.Message)}'.");
                    }

                    if (surfaceObject != null)
                    {
                        Object.Destroy(surfaceObject);
                    }
                }
            });
        }

        private async void RunPauseUIGlobalResidentSurfaceSmoke()
        {
            await RunSmokeAsync("Pause UIGlobal Resident Surface Smoke", runtimeHost =>
            {
                GameObject surfaceObject = null;

                try
                {
                    surfaceObject = new GameObject("QA Pause UIGlobal Resident Surface");
                    var canvasGroup = surfaceObject.AddComponent<CanvasGroup>();
                    var adapter = surfaceObject.AddComponent<UnityPauseResidentSurfaceAdapter>();
                    var surfaceRuntime = PauseSurfaceRuntime.Create(
                        _logger,
                        new IPauseSurfaceAdapter[] { adapter },
                        "QA UIGlobal Resident Surface");

                    var runningSnapshot = PauseSnapshot.FromState(
                        PauseState.Running,
                        QaSource,
                        "qa.pause-resident-surface.running",
                        new[] { "QA running snapshot for resident Pause surface." });
                    var pausedSnapshot = PauseSnapshot.FromState(
                        PauseState.Paused,
                        QaSource,
                        "qa.pause-resident-surface.paused",
                        new[] { "QA paused snapshot for resident Pause surface." });
                    var resumeSnapshot = PauseSnapshot.FromState(
                        PauseState.Running,
                        QaSource,
                        "qa.pause-resident-surface.resume",
                        new[] { "QA resume snapshot for resident Pause surface." });

                    var initialResult = surfaceRuntime.ApplySnapshot(runningSnapshot, QaSource, "qa.pause-resident-surface.initial-hidden");
                    bool initialHidden = !surfaceObject.activeSelf
                        && Math.Abs(canvasGroup.alpha) <= 0.0001f
                        && !canvasGroup.blocksRaycasts
                        && !canvasGroup.interactable;

                    var pausedResult = surfaceRuntime.ApplySnapshot(pausedSnapshot, QaSource, "qa.pause-resident-surface.show");
                    bool pausedVisible = surfaceObject.activeSelf
                        && adapter.IsVisible
                        && adapter.LastAppliedState == PauseState.Paused
                        && Math.Abs(canvasGroup.alpha - 1f) <= 0.0001f
                        && canvasGroup.blocksRaycasts
                        && canvasGroup.interactable;

                    bool rootActiveWhenPaused = surfaceObject.activeSelf;
                    float pausedAlpha = canvasGroup.alpha;
                    bool pausedBlocksRaycasts = canvasGroup.blocksRaycasts;
                    bool pausedInteractable = canvasGroup.interactable;

                    var resumeResult = surfaceRuntime.ApplySnapshot(resumeSnapshot, QaSource, "qa.pause-resident-surface.hide");
                    bool resumedHidden = !surfaceObject.activeSelf
                        && !adapter.IsVisible
                        && adapter.LastAppliedState == PauseState.Running
                        && Math.Abs(canvasGroup.alpha) <= 0.0001f
                        && !canvasGroup.blocksRaycasts
                        && !canvasGroup.interactable;

                    bool passed = surfaceRuntime.HasVisibleSurface
                        && surfaceRuntime.AdapterCount == 1
                        && initialResult.Succeeded
                        && pausedResult.Succeeded
                        && resumeResult.Succeeded
                        && initialHidden
                        && pausedVisible
                        && resumedHidden;

                    if (!passed)
                    {
                        _logger.Warning(
                            $"QA Pause UIGlobal Resident Surface Smoke step failed. step='pause-uiglobal-resident-surface' initial='{initialResult.StatusText}' paused='{pausedResult.StatusText}' resume='{resumeResult.StatusText}' initialHidden='{initialHidden}' pausedVisible='{pausedVisible}' resumedHidden='{resumedHidden}' adapterCount='{surfaceRuntime.AdapterCount}' hasSurface='{surfaceRuntime.HasVisibleSurface}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause UIGlobal Resident Surface Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-uiglobal-resident-surface"),
                            LogFields.Field("passed", true),
                            LogFields.Field("surfaceRuntime", pausedResult.StatusText),
                            LogFields.Field("adapterCount", surfaceRuntime.AdapterCount),
                            LogFields.Field("supportedAdapters", pausedResult.SupportedAdapterCount),
                            LogFields.Field("appliedAdapters", pausedResult.AppliedAdapterCount),
                            LogFields.Field("initialHidden", initialHidden),
                            LogFields.Field("pausedVisible", pausedVisible),
                            LogFields.Field("resumedHidden", resumedHidden),
                            LogFields.Field("rootActiveWhenPaused", pausedVisible && rootActiveWhenPaused),
                            LogFields.Field("rootInactiveWhenRunning", resumedHidden && !surfaceObject.activeSelf),
                            LogFields.Field("canvasAlphaPaused", pausedAlpha.ToString("0.00")),
                            LogFields.Field("blocksRaycastsWhenPaused", pausedVisible && pausedBlocksRaycasts),
                            LogFields.Field("interactableWhenPaused", pausedVisible && pausedInteractable),
                            LogFields.Field("canonicalResidentUIGlobalSurface", true),
                            LogFields.Field("materialization", false),
                            LogFields.Field("contentAnchorBinding", false),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("explicitSubmit", true),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    if (surfaceObject != null)
                    {
                        Object.Destroy(surfaceObject);
                    }
                }
            });
        }

        private async void RunPauseVisualSurfaceAuthoringContractSmoke()
        {
            await RunSmokeAsync("Pause Visual Surface Authoring Contract Smoke", runtimeHost =>
            {
                int initialBindingCount = runtimeHost.ContentAnchorBindingCount;
                int initialRuntimeRootCount = runtimeHost.RuntimeContentRuntime != null ? runtimeHost.RuntimeContentRuntime.RootCount : -1;
                GameObject validObject = null;
                GameObject invalidObject = null;
                GameObject visualPrefab = null;

                try
                {
                    visualPrefab = new GameObject("QA Pause Visual Surface Prefab");
                    validObject = new GameObject("QA Pause Visual Surface Authoring");
                    invalidObject = new GameObject("QA Invalid Pause Visual Surface Authoring");

                    var validAuthoring = validObject.AddComponent<PauseVisualSurfaceAuthoring>();
                    validAuthoring.ConfigureForDiagnostics(
                        visualPrefab,
                        PauseVisualSurfaceKind.OverlayRoot,
                        PauseState.Paused,
                        RuntimeContentScope.Transient,
                        "qa.pause.visual.owner",
                        "QA Pause Visual Surface Owner",
                        ContentAnchorScope.Local,
                        ContentAnchorKind.Root,
                        ContentAnchorRequiredness.Required,
                        "qa.pause.visual.anchor-owner",
                        "qa.pause.visual.overlay",
                        "qa.pause.visual.content",
                        "qa.pause.visual.prefab",
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        true);

                    var invalidAuthoring = invalidObject.AddComponent<PauseVisualSurfaceAuthoring>();
                    invalidAuthoring.ConfigureForDiagnostics(
                        null,
                        PauseVisualSurfaceKind.OverlayRoot,
                        PauseState.Paused,
                        RuntimeContentScope.Transient,
                        "qa.pause.visual.owner.invalid",
                        "QA Invalid Pause Visual Surface Owner",
                        ContentAnchorScope.Local,
                        ContentAnchorKind.Root,
                        ContentAnchorRequiredness.Required,
                        "qa.pause.visual.anchor-owner.invalid",
                        "qa.pause.visual.overlay.invalid",
                        "qa.pause.visual.content.invalid",
                        "qa.pause.visual.prefab.invalid",
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        true);

                    bool validCreated = validAuthoring.TryCreateContract(out var contract, out var validMessage);
                    bool invalidRejected = !invalidAuthoring.TryCreateContract(out _, out var invalidMessage);
                    bool contractValid = validCreated && contract.IsValid;
                    bool pauseRequirementValid = contractValid
                        && contract.ContentRequirement.IsValid
                        && contract.ContentRequirement.Purpose == PauseContentRequirementPurpose.PresentationRoot
                        && contract.ContentRequirement.PauseState == PauseState.Paused
                        && contract.ContentRequirement.Requiredness == ContentAnchorRequiredness.Required;
                    bool runtimeOwnerValid = contractValid
                        && contract.RuntimeOwner.IsValid
                        && contract.RuntimeScope == RuntimeContentScope.Transient
                        && contract.RuntimeContentId.IsValid;
                    bool anchorRequirementValid = contractValid
                        && contract.ContentRequirement.AnchorScope == ContentAnchorScope.Local
                        && contract.ContentRequirement.AnchorKind == ContentAnchorKind.Root
                        && contract.ContentRequirement.AnchorId.IsValid;
                    bool prefabRecorded = contractValid && contract.HasVisualPrefab && ReferenceEquals(contract.VisualPrefab, visualPrefab);
                    bool resourceRecorded = contractValid && contract.Resource.IsValid && string.Equals(contract.Resource.ResourceKey, "qa.pause.visual.prefab", StringComparison.Ordinal);
                    int finalBindingCount = runtimeHost.ContentAnchorBindingCount;
                    int finalRuntimeRootCount = runtimeHost.RuntimeContentRuntime != null ? runtimeHost.RuntimeContentRuntime.RootCount : -1;
                    bool noRuntimeSideEffects = finalBindingCount == initialBindingCount && finalRuntimeRootCount == initialRuntimeRootCount;
                    bool passed = validCreated
                        && invalidRejected
                        && contractValid
                        && pauseRequirementValid
                        && runtimeOwnerValid
                        && anchorRequirementValid
                        && prefabRecorded
                        && resourceRecorded
                        && noRuntimeSideEffects;

                    if (!passed)
                    {
                        _logger.Warning(
                            $"QA Pause Visual Surface Authoring Contract Smoke step failed. step='pause-visual-surface-authoring-contract' validCreated='{validCreated}' invalidRejected='{invalidRejected}' contractValid='{contractValid}' pauseRequirementValid='{pauseRequirementValid}' runtimeOwnerValid='{runtimeOwnerValid}' anchorRequirementValid='{anchorRequirementValid}' prefabRecorded='{prefabRecorded}' resourceRecorded='{resourceRecorded}' noRuntimeSideEffects='{noRuntimeSideEffects}' validMessage='{FormatValue(validMessage)}' invalidMessage='{FormatValue(invalidMessage)}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause Visual Surface Authoring Contract Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-visual-surface-authoring-contract"),
                            LogFields.Field("passed", true),
                            LogFields.Field("validContract", validCreated),
                            LogFields.Field("invalidRejected", invalidRejected),
                            LogFields.Field("surfaceKind", contract.SurfaceKind.ToString()),
                            LogFields.Field("pauseState", contract.ContentRequirement.PauseState.ToString()),
                            LogFields.Field("requirementPurpose", contract.ContentRequirement.Purpose.ToString()),
                            LogFields.Field("runtimeScope", contract.RuntimeScope.ToString()),
                            LogFields.Field("anchorScope", contract.ContentRequirement.AnchorScope.ToString()),
                            LogFields.Field("anchorKind", contract.ContentRequirement.AnchorKind.ToString()),
                            LogFields.Field("requiredness", contract.ContentRequirement.Requiredness.ToString()),
                            LogFields.Field("prefabRecorded", prefabRecorded),
                            LogFields.Field("resourceRecorded", resourceRecorded),
                            LogFields.Field("passiveAuthoringOnly", true),
                            LogFields.Field("pauseConsumerSelected", true),
                            LogFields.Field("materialization", false),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    if (validObject != null)
                    {
                        Object.Destroy(validObject);
                    }

                    if (invalidObject != null)
                    {
                        Object.Destroy(invalidObject);
                    }

                    if (visualPrefab != null)
                    {
                        Object.Destroy(visualPrefab);
                    }
                }
            });
        }

        private async void RunPauseContentAnchorBindingRequestSmoke()
        {
            await RunSmokeAsync("Pause Content Anchor Binding Request Smoke", runtimeHost =>
            {
                int initialBindingCount = runtimeHost.ContentAnchorBindingCount;
                int initialRuntimeRootCount = runtimeHost.RuntimeContentRuntime != null ? runtimeHost.RuntimeContentRuntime.RootCount : -1;
                GameObject authoringObject = null;
                GameObject visualPrefab = null;

                try
                {
                    visualPrefab = new GameObject("QA Pause Binding Request Prefab");
                    authoringObject = new GameObject("QA Pause Binding Request Authoring");

                    var authoring = authoringObject.AddComponent<PauseVisualSurfaceAuthoring>();
                    authoring.ConfigureForDiagnostics(
                        visualPrefab,
                        PauseVisualSurfaceKind.OverlayRoot,
                        PauseState.Paused,
                        RuntimeContentScope.Transient,
                        "qa.pause.binding-request.owner",
                        "QA Pause Binding Request Owner",
                        ContentAnchorScope.Local,
                        ContentAnchorKind.Root,
                        ContentAnchorRequiredness.Required,
                        "qa.pause.binding-request.anchor-owner",
                        "qa.pause.binding-request.overlay",
                        "qa.pause.binding-request.content",
                        "qa.pause.binding-request.prefab",
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        true);

                    bool contractCreated = authoring.TryCreateContract(out var contract, out var contractMessage);
                    var requestResult = contractCreated
                        ? PauseVisualSurfaceBindingRequestFactory.Create(
                            contract,
                            QaSource,
                            "qa.pause.content-anchor.binding-request")
                        : PauseVisualSurfaceBindingRequestResult.Failure(
                            PauseVisualSurfaceBindingRequestStatus.RejectedInvalidContract,
                            contract,
                            QaSource,
                            "qa.pause.content-anchor.binding-request",
                            contractMessage);

                    var mismatchedContext = new RuntimeScopeContext(
                        RuntimeContentOwner.Transient(
                            "qa.pause.binding-request.other-owner",
                            "QA Pause Binding Request Other Owner"),
                        QaSource,
                        "qa.pause.content-anchor.binding-request.mismatch");
                    var mismatchedResult = contractCreated
                        ? PauseVisualSurfaceBindingRequestFactory.Create(
                            contract,
                            mismatchedContext,
                            QaSource,
                            "qa.pause.content-anchor.binding-request.mismatch")
                        : PauseVisualSurfaceBindingRequestResult.Failure(
                            PauseVisualSurfaceBindingRequestStatus.RejectedInvalidContract,
                            contract,
                            QaSource,
                            "qa.pause.content-anchor.binding-request.mismatch",
                            contractMessage);

                    bool requestCreated = requestResult.Succeeded && requestResult.HasRequest;
                    var request = requestResult.Request;
                    bool requestMatchesPauseContract = requestCreated
                        && request.RuntimeOwner == contract.RuntimeOwner
                        && request.RuntimeScope == contract.RuntimeScope
                        && request.RuntimeContentId == contract.RuntimeContentId
                        && request.RuntimeIdentity == contract.RuntimeIdentity
                        && request.Resource == contract.Resource;
                    bool requestMatchesAnchorRequirement = requestCreated
                        && request.AnchorScope == contract.AnchorScope
                        && request.AnchorOwner == contract.AnchorOwner
                        && request.AnchorKind == contract.AnchorKind
                        && request.AnchorId == contract.AnchorId;
                    bool anchorOwnerRecorded = contractCreated
                        && contract.AnchorOwner.IsValid
                        && requestCreated
                        && request.AnchorOwner == contract.AnchorOwner;
                    bool mismatchedContextRejected = mismatchedResult.Status == PauseVisualSurfaceBindingRequestStatus.RejectedMismatchedRuntimeOwner;
                    int finalBindingCount = runtimeHost.ContentAnchorBindingCount;
                    int finalRuntimeRootCount = runtimeHost.RuntimeContentRuntime != null ? runtimeHost.RuntimeContentRuntime.RootCount : -1;
                    bool noRuntimeSideEffects = finalBindingCount == initialBindingCount
                        && finalRuntimeRootCount == initialRuntimeRootCount;
                    bool passed = contractCreated
                        && requestCreated
                        && requestMatchesPauseContract
                        && requestMatchesAnchorRequirement
                        && anchorOwnerRecorded
                        && mismatchedContextRejected
                        && noRuntimeSideEffects;

                    if (!passed)
                    {
                        _logger.Warning(
                            $"QA Pause Content Anchor Binding Request Smoke step failed. step='pause-content-anchor-binding-request' contractCreated='{contractCreated}' request='{requestResult.ToDiagnosticString()}' mismatch='{mismatchedResult.ToDiagnosticString()}' requestMatchesPauseContract='{requestMatchesPauseContract}' requestMatchesAnchorRequirement='{requestMatchesAnchorRequirement}' anchorOwnerRecorded='{anchorOwnerRecorded}' mismatchedContextRejected='{mismatchedContextRejected}' noRuntimeSideEffects='{noRuntimeSideEffects}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause Content Anchor Binding Request Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-content-anchor-binding-request"),
                            LogFields.Field("passed", true),
                            LogFields.Field("contract", contractCreated),
                            LogFields.Field("bindingRequest", requestResult.Status.ToString()),
                            LogFields.Field("mismatchedContext", mismatchedResult.Status.ToString()),
                            LogFields.Field("runtimeScope", request.RuntimeScope.ToString()),
                            LogFields.Field("runtimeOwner", request.RuntimeOwner.StableText),
                            LogFields.Field("runtimeContentId", request.RuntimeContentId.StableText),
                            LogFields.Field("runtimeIdentity", request.RuntimeIdentity.StableText),
                            LogFields.Field("anchorScope", request.AnchorScope.ToString()),
                            LogFields.Field("anchorOwner", request.AnchorOwner.StableText),
                            LogFields.Field("anchorKind", request.AnchorKind.ToString()),
                            LogFields.Field("anchorId", request.AnchorId.StableText),
                            LogFields.Field("resourceRecorded", request.Resource.IsValid),
                            LogFields.Field("requestMatchesPauseContract", requestMatchesPauseContract),
                            LogFields.Field("requestMatchesAnchorRequirement", requestMatchesAnchorRequirement),
                            LogFields.Field("anchorOwnerRecorded", anchorOwnerRecorded),
                            LogFields.Field("mismatchedContextRejected", mismatchedContextRejected),
                            LogFields.Field("requestOnly", true),
                            LogFields.Field("bindingExecution", false),
                            LogFields.Field("materialization", false),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("explicitSubmit", true),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    if (authoringObject != null)
                    {
                        Object.Destroy(authoringObject);
                    }

                    if (visualPrefab != null)
                    {
                        Object.Destroy(visualPrefab);
                    }
                }
            });
        }


        private async void RunPauseContentAnchorBindingExecutionSmoke()
        {
            await RunSmokeAsync("Pause Content Anchor Binding Execution Smoke", runtimeHost =>
            {
                var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                if (runtimeContentRuntime == null)
                {
                    _logger.Warning("QA Pause Content Anchor Binding Execution Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                    return Task.FromResult(false);
                }

                int initialBindingCount = runtimeHost.ContentAnchorBindingCount;
                int initialRuntimeRootCount = runtimeContentRuntime.RootCount;
                GameObject authoringObject = null;
                GameObject visualPrefab = null;
                RuntimeContentOwner runtimeOwner = default(RuntimeContentOwner);
                RuntimeScopeContext runtimeContext = default(RuntimeScopeContext);
                bool rootCreated = false;

                try
                {
                    string unique = Guid.NewGuid().ToString("N");
                    visualPrefab = new GameObject("QA Pause Binding Execution Prefab");
                    authoringObject = new GameObject("QA Pause Binding Execution Authoring");

                    var authoring = authoringObject.AddComponent<PauseVisualSurfaceAuthoring>();
                    authoring.ConfigureForDiagnostics(
                        visualPrefab,
                        PauseVisualSurfaceKind.OverlayRoot,
                        PauseState.Paused,
                        RuntimeContentScope.Transient,
                        "qa.pause.binding-execution.owner." + unique,
                        "QA Pause Binding Execution Owner",
                        ContentAnchorScope.Local,
                        ContentAnchorKind.Root,
                        ContentAnchorRequiredness.Required,
                        "qa.pause.binding-execution.anchor-owner." + unique,
                        "qa.pause.binding-execution.overlay." + unique,
                        "qa.pause.binding-execution.content." + unique,
                        "qa.pause.binding-execution.prefab." + unique,
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        true);

                    bool contractCreated = authoring.TryCreateContract(out var contract, out var contractMessage);
                    if (!contractCreated)
                    {
                        _logger.Warning($"QA Pause Content Anchor Binding Execution Smoke step failed. step='contract' reason='{FormatValue(contractMessage)}'.");
                        return Task.FromResult(false);
                    }

                    runtimeOwner = contract.RuntimeOwner;
                    var rootResult = runtimeContentRuntime.CreateScopeRoot(
                        runtimeOwner,
                        QaSource,
                        "qa.pause.content-anchor.binding-execution.root");
                    rootCreated = rootResult.Applied || rootResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                    if (!rootCreated || !runtimeContentRuntime.TryCreateScopeContext(
                            runtimeOwner,
                            QaSource,
                            "qa.pause.content-anchor.binding-execution.context",
                            out runtimeContext))
                    {
                        _logger.Warning($"QA Pause Content Anchor Binding Execution Smoke step failed. step='root' root='{rootResult.Status}' rootCreated='{rootCreated}'.");
                        return Task.FromResult(false);
                    }

                    var anchor = new ContentAnchorDeclaration(
                        contract.AnchorOwner,
                        contract.AnchorScope,
                        contract.AnchorKind,
                        contract.AnchorId,
                        contract.ContentRequirement.Requiredness,
                        "QA Pause Binding Execution Anchor",
                        "Synthetic anchor for Pause ContentAnchor binding execution smoke.",
                        "qa.pause.binding-execution.anchor",
                        string.Empty);
                    var anchorSet = new ContentAnchorSet(new[] { anchor });

                    var executionResult = PauseVisualSurfaceBindingExecutor.Execute(
                        contract,
                        anchorSet,
                        runtimeContext,
                        runtimeHost,
                        QaSource,
                        "qa.pause.content-anchor.binding-execution.execute");

                    bool bindingExecuted = executionResult.Succeeded
                        && executionResult.BindingResult.Succeeded
                        && executionResult.BindingResult.HasHandle;
                    bool handleDeclared = executionResult.RuntimeHandleDeclared
                        && executionResult.RuntimeHandleDeclaration != null
                        && executionResult.RuntimeHandleDeclaration.Status == RuntimeRootRegistryOperationStatus.HandleRegistered;
                    bool bindingCountIncreased = runtimeHost.ContentAnchorBindingCount == initialBindingCount + 1;
                    bool runtimeHandleRegistered = runtimeContentRuntime.TryGetHandle(
                        runtimeContext,
                        contract.RuntimeIdentity,
                        out var registeredHandle)
                        && registeredHandle != null
                        && registeredHandle.Identity == contract.RuntimeIdentity
                        && !registeredHandle.IsReleased;
                    bool requestMatchesPauseContract = executionResult.RequestCreated
                        && executionResult.Request.RuntimeOwner == contract.RuntimeOwner
                        && executionResult.Request.RuntimeScope == contract.RuntimeScope
                        && executionResult.Request.RuntimeContentId == contract.RuntimeContentId
                        && executionResult.Request.RuntimeIdentity == contract.RuntimeIdentity
                        && executionResult.Request.Resource == contract.Resource;
                    bool requestMatchesAnchorRequirement = executionResult.RequestCreated
                        && executionResult.Request.AnchorScope == contract.AnchorScope
                        && executionResult.Request.AnchorOwner == contract.AnchorOwner
                        && executionResult.Request.AnchorKind == contract.AnchorKind
                        && executionResult.Request.AnchorId == contract.AnchorId;
                    bool bindingMatchesAnchor = bindingExecuted
                        && executionResult.BindingResult.Anchor == anchor
                        && executionResult.BindingResult.Handle.Anchor == anchor;
                    bool passed = bindingExecuted
                        && handleDeclared
                        && bindingCountIncreased
                        && runtimeHandleRegistered
                        && requestMatchesPauseContract
                        && requestMatchesAnchorRequirement
                        && bindingMatchesAnchor;

                    bool unbound = false;
                    bool logicalReleased = false;
                    if (bindingExecuted)
                    {
                        unbound = runtimeHost.UnbindContentAnchor(executionResult.BindingResult.Handle);
                        var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                            runtimeContext,
                            contract.RuntimeIdentity,
                            contract.ReleasePolicy,
                            QaSource,
                            "qa.pause.content-anchor.binding-execution.cleanup.logical-release");
                        logicalReleased = releaseResult.Succeeded && releaseResult.HandleUnregistered;
                    }

                    if (!passed || !unbound || !logicalReleased)
                    {
                        _logger.Warning(
                            $"QA Pause Content Anchor Binding Execution Smoke step failed. step='pause-content-anchor-binding-execution' execution='{executionResult.ToDiagnosticString()}' bindingExecuted='{bindingExecuted}' handleDeclared='{handleDeclared}' bindingCountIncreased='{bindingCountIncreased}' runtimeHandleRegistered='{runtimeHandleRegistered}' requestMatchesPauseContract='{requestMatchesPauseContract}' requestMatchesAnchorRequirement='{requestMatchesAnchorRequirement}' bindingMatchesAnchor='{bindingMatchesAnchor}' unbound='{unbound}' logicalReleased='{logicalReleased}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause Content Anchor Binding Execution Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-content-anchor-binding-execution"),
                            LogFields.Field("passed", true),
                            LogFields.Field("contract", contractCreated),
                            LogFields.Field("root", rootResult.Status.ToString()),
                            LogFields.Field("bindingExecution", executionResult.Status.ToString()),
                            LogFields.Field("binding", executionResult.BindingResult.Status.ToString()),
                            LogFields.Field("runtimeHandleDeclaration", executionResult.RuntimeHandleDeclaration.Status.ToString()),
                            LogFields.Field("runtimeScope", executionResult.Request.RuntimeScope.ToString()),
                            LogFields.Field("runtimeOwner", executionResult.Request.RuntimeOwner.StableText),
                            LogFields.Field("runtimeContentId", executionResult.Request.RuntimeContentId.StableText),
                            LogFields.Field("runtimeIdentity", executionResult.Request.RuntimeIdentity.StableText),
                            LogFields.Field("anchorScope", executionResult.Request.AnchorScope.ToString()),
                            LogFields.Field("anchorOwner", executionResult.Request.AnchorOwner.StableText),
                            LogFields.Field("anchorKind", executionResult.Request.AnchorKind.ToString()),
                            LogFields.Field("anchorId", executionResult.Request.AnchorId.StableText),
                            LogFields.Field("bindingCountIncreased", bindingCountIncreased),
                            LogFields.Field("runtimeHandleRegistered", runtimeHandleRegistered),
                            LogFields.Field("requestMatchesPauseContract", requestMatchesPauseContract),
                            LogFields.Field("requestMatchesAnchorRequirement", requestMatchesAnchorRequirement),
                            LogFields.Field("bindingMatchesAnchor", bindingMatchesAnchor),
                            LogFields.Field("explicitSubmit", true),
                            LogFields.Field("requestOnly", false),
                            LogFields.Field("bindingExecutionOnly", true),
                            LogFields.Field("bindingCleanup", unbound),
                            LogFields.Field("smokeCleanupLogicalRuntimeContentRelease", logicalReleased),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("materialization", false),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    if (rootCreated && runtimeOwner.IsValid)
                    {
                        RuntimeContentHandle[] remainingHandles = Array.Empty<RuntimeContentHandle>();
                        if (runtimeContext.IsValid)
                        {
                            remainingHandles = runtimeContentRuntime.SnapshotHandles(runtimeContext);
                            for (int i = 0; i < remainingHandles.Length; i++)
                            {
                                runtimeContentRuntime.ReleaseHandleLogically(
                                    runtimeContext,
                                    remainingHandles[i].Identity,
                                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                                    QaSource,
                                    "qa.pause.content-anchor.binding-execution.cleanup.remaining");
                            }
                        }

                        runtimeContentRuntime.RemoveScopeRoot(
                            runtimeOwner,
                            QaSource,
                            "qa.pause.content-anchor.binding-execution.cleanup.root");
                    }

                    if (authoringObject != null)
                    {
                        Object.Destroy(authoringObject);
                    }

                    if (visualPrefab != null)
                    {
                        Object.Destroy(visualPrefab);
                    }
                }
            });
        }


        private async void RunPauseVisualMaterializationSmoke()
        {
            await RunSmokeAsync("Pause Visual Materialization Smoke", runtimeHost =>
            {
                var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                if (runtimeContentRuntime == null)
                {
                    _logger.Warning("QA Pause Visual Materialization Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                    return Task.FromResult(false);
                }

                int initialBindingCount = runtimeHost.ContentAnchorBindingCount;
                var physicalRegistry = new UnityRuntimeMaterializedObjectRegistry();
                GameObject authoringObject = null;
                GameObject visualPrefab = null;
                GameObject anchorObject = null;
                RuntimeContentOwner runtimeOwner = default;
                RuntimeScopeContext runtimeContext = default;
                bool rootCreated = false;
                bool cleanupExecuted = false;

                try
                {
                    string unique = Guid.NewGuid().ToString("N");
                    visualPrefab = new GameObject("QA Pause Visual Materialization Prefab");
                    visualPrefab.hideFlags = HideFlags.DontSave;
                    authoringObject = new GameObject("QA Pause Visual Materialization Authoring");
                    authoringObject.hideFlags = HideFlags.DontSave;
                    anchorObject = new GameObject("QA Pause Visual Materialization Anchor");
                    anchorObject.hideFlags = HideFlags.DontSave;

                    var authoring = authoringObject.AddComponent<PauseVisualSurfaceAuthoring>();
                    authoring.ConfigureForDiagnostics(
                        visualPrefab,
                        PauseVisualSurfaceKind.OverlayRoot,
                        PauseState.Paused,
                        RuntimeContentScope.Transient,
                        "qa.pause.visual-materialization.owner." + unique,
                        "QA Pause Visual Materialization Owner",
                        ContentAnchorScope.Local,
                        ContentAnchorKind.Root,
                        ContentAnchorRequiredness.Required,
                        "qa.pause.visual-materialization.anchor-owner." + unique,
                        "qa.pause.visual-materialization.overlay." + unique,
                        "qa.pause.visual-materialization.content." + unique,
                        "qa.pause.visual-materialization.prefab." + unique,
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        true);

                    bool contractCreated = authoring.TryCreateContract(out var contract, out var contractMessage);
                    if (!contractCreated)
                    {
                        _logger.Warning($"QA Pause Visual Materialization Smoke step failed. step='contract' reason='{FormatValue(contractMessage)}'.");
                        return Task.FromResult(false);
                    }

                    runtimeOwner = contract.RuntimeOwner;
                    var rootResult = runtimeContentRuntime.CreateScopeRoot(
                        runtimeOwner,
                        QaSource,
                        "qa.pause.visual-materialization.root");
                    rootCreated = rootResult.Applied || rootResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                    if (!rootCreated || !runtimeContentRuntime.TryCreateScopeContext(
                            runtimeOwner,
                            QaSource,
                            "qa.pause.visual-materialization.context",
                            out runtimeContext))
                    {
                        _logger.Warning($"QA Pause Visual Materialization Smoke step failed. step='root' root='{rootResult.Status}' rootCreated='{rootCreated}'.");
                        return Task.FromResult(false);
                    }

                    var anchor = new ContentAnchorDeclaration(
                        contract.AnchorOwner,
                        contract.AnchorScope,
                        contract.AnchorKind,
                        contract.AnchorId,
                        contract.ContentRequirement.Requiredness,
                        "QA Pause Visual Materialization Anchor",
                        "Synthetic anchor for Pause visual materialization smoke.",
                        "qa.pause.visual-materialization.anchor",
                        string.Empty);
                    var anchorSet = new ContentAnchorSet(new[] { anchor });
                    if (!anchorSet.HasAnchors || anchorSet.HasIssues)
                    {
                        _logger.Warning($"QA Pause Visual Materialization Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                        return Task.FromResult(false);
                    }

                    var materializationResult = PauseVisualSurfaceMaterializationExecutor.Execute(
                        contract,
                        anchorSet,
                        anchorObject.transform,
                        runtimeContext,
                        runtimeHost,
                        physicalRegistry,
                        QaSource,
                        "qa.pause.visual-materialization.execute");

                    bool pipelineSucceeded = materializationResult.Succeeded
                        && materializationResult.PipelineResult.Succeeded;
                    bool logicalBinding = pipelineSucceeded
                        && materializationResult.PipelineResult.BindingResult.Succeeded
                        && materializationResult.PipelineResult.BindingResult.HasHandle;
                    bool physicalPlacementApplied = materializationResult.PhysicalPlacementApplied;
                    bool physicalEvidenceRecorded = physicalRegistry.TryGet(contract.RuntimeIdentity, out var evidence)
                        && evidence != null
                        && evidence.HasLiveInstance
                        && !evidence.PhysicalReleaseRequested;
                    bool visualInstanceParented = physicalEvidenceRecorded
                        && evidence.Instance.transform.parent == anchorObject.transform;
                    bool runtimeHandleMaterialized = runtimeContentRuntime.TryGetHandle(
                            runtimeContext,
                            contract.RuntimeIdentity,
                            out var registeredHandle)
                        && registeredHandle != null
                        && registeredHandle.IsMaterialized
                        && !registeredHandle.IsReleased;
                    bool bindingCountIncreased = runtimeHost.ContentAnchorBindingCount == initialBindingCount + 1;
                    bool requestMatchesPauseContract = materializationResult.RequestCreated
                        && materializationResult.Request.RuntimeOwner == contract.RuntimeOwner
                        && materializationResult.Request.RuntimeScope == contract.RuntimeScope
                        && materializationResult.Request.RuntimeContentId == contract.RuntimeContentId
                        && materializationResult.Request.RuntimeIdentity == contract.RuntimeIdentity
                        && materializationResult.Request.Resource == contract.Resource;
                    bool bindingMatchesAnchor = logicalBinding
                        && materializationResult.PipelineResult.BindingResult.Anchor == anchor
                        && materializationResult.PipelineResult.BindingResult.Handle.Anchor == anchor;

                    var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(
                        physicalRegistry,
                        QaSource);
                    var releasePipeline = new UnityContentAnchorMaterializationScopeReleasePipeline(
                        releaseAdapter,
                        QaSource);
                    var cleanupResult = releasePipeline.ReleaseScope(
                        runtimeHost,
                        runtimeContext,
                        contract.ReleasePolicy,
                        "qa.pause.visual-materialization.cleanup.explicit-release");
                    cleanupExecuted = true;

                    bool cleanupSucceeded = cleanupResult.Succeeded
                        && cleanupResult.PhysicalReleaseRequests == 1
                        && cleanupResult.LogicalReleaseResults == 1
                        && cleanupResult.BindingRemovedCount == 1
                        && physicalRegistry.ActiveCount == 0
                        && runtimeHost.ContentAnchorBindingCount == initialBindingCount
                        && runtimeContentRuntime.SnapshotHandles(runtimeContext).Length == 0;
                    bool passed = pipelineSucceeded
                        && logicalBinding
                        && physicalPlacementApplied
                        && physicalEvidenceRecorded
                        && visualInstanceParented
                        && runtimeHandleMaterialized
                        && bindingCountIncreased
                        && requestMatchesPauseContract
                        && bindingMatchesAnchor
                        && cleanupSucceeded;

                    if (!passed)
                    {
                        _logger.Warning(
                            $"QA Pause Visual Materialization Smoke step failed. step='pause-visual-materialization' materialization='{materializationResult.ToDiagnosticString()}' cleanup='{cleanupResult.ToDiagnosticString()}' physicalEvidenceRecorded='{physicalEvidenceRecorded}' visualInstanceParented='{visualInstanceParented}' runtimeHandleMaterialized='{runtimeHandleMaterialized}' bindingCountIncreased='{bindingCountIncreased}' requestMatchesPauseContract='{requestMatchesPauseContract}' bindingMatchesAnchor='{bindingMatchesAnchor}' cleanupSucceeded='{cleanupSucceeded}'.");
                        return Task.FromResult(false);
                    }

                    _logger.Info(
                        "QA Pause Visual Materialization Smoke step completed. ",
                        LogFields.Of(
                            LogFields.Field("step", "pause-visual-materialization"),
                            LogFields.Field("passed", true),
                            LogFields.Field("contract", contractCreated),
                            LogFields.Field("root", rootResult.Status.ToString()),
                            LogFields.Field("materialization", materializationResult.Status.ToString()),
                            LogFields.Field("pipeline", materializationResult.PipelineResult.Status.ToString()),
                            LogFields.Field("binding", materializationResult.PipelineResult.BindingResult.Status.ToString()),
                            LogFields.Field("materialized", materializationResult.PipelineResult.MaterializationResult.Status.ToString()),
                            LogFields.Field("appliedMaterialization", materializationResult.PipelineResult.AppliedMaterializationResult.Status.ToString()),
                            LogFields.Field("runtimeScope", materializationResult.Request.RuntimeScope.ToString()),
                            LogFields.Field("runtimeOwner", materializationResult.Request.RuntimeOwner.StableText),
                            LogFields.Field("runtimeContentId", materializationResult.Request.RuntimeContentId.StableText),
                            LogFields.Field("runtimeIdentity", materializationResult.Request.RuntimeIdentity.StableText),
                            LogFields.Field("anchorScope", materializationResult.Request.AnchorScope.ToString()),
                            LogFields.Field("anchorOwner", materializationResult.Request.AnchorOwner.StableText),
                            LogFields.Field("anchorKind", materializationResult.Request.AnchorKind.ToString()),
                            LogFields.Field("anchorId", materializationResult.Request.AnchorId.StableText),
                            LogFields.Field("bindingCountIncreased", bindingCountIncreased),
                            LogFields.Field("runtimeHandleMaterialized", runtimeHandleMaterialized),
                            LogFields.Field("requestMatchesPauseContract", requestMatchesPauseContract),
                            LogFields.Field("bindingMatchesAnchor", bindingMatchesAnchor),
                            LogFields.Field("physicalEvidenceRecorded", physicalEvidenceRecorded),
                            LogFields.Field("physicalPlacementApplied", physicalPlacementApplied),
                            LogFields.Field("visualInstanceParented", visualInstanceParented),
                            LogFields.Field("physicalRegistryEntries", physicalRegistry.Count),
                            LogFields.Field("physicalRegistryActive", physicalRegistry.ActiveCount),
                            LogFields.Field("physicalReleaseRequested", physicalRegistry.PhysicalReleaseRequestedCount),
                            LogFields.Field("contentAnchorBindings", runtimeHost.ContentAnchorBindingCount),
                            LogFields.Field("runtimeHandles", runtimeContentRuntime.SnapshotHandles(runtimeContext).Length),
                            LogFields.Field("smokeCleanup", cleanupResult.Status.ToString()),
                            LogFields.Field("smokeCleanupPhysicalRelease", cleanupResult.PhysicalReleaseRequests == 1),
                            LogFields.Field("smokeCleanupLogicalRuntimeContentRelease", cleanupResult.LogicalReleaseResults == 1),
                            LogFields.Field("smokeCleanupContentAnchorBindingCleanup", cleanupResult.BindingRemovedCount == 1),
                            LogFields.Field("explicitSubmit", true),
                            LogFields.Field("requestOnly", false),
                            LogFields.Field("bindingExecutionOnly", false),
                            LogFields.Field("visualMaterialization", true),
                            LogFields.Field("materializationExplicit", true),
                            LogFields.Field("physicalRelease", false),
                            LogFields.Field("logicalRuntimeContentRelease", false),
                            LogFields.Field("contentAnchorBindingCleanup", false),
                            LogFields.Field("inputModeChange", false),
                            LogFields.Field("timeScalePolicy", false),
                            LogFields.Field("automaticLifecycleWiring", false),
                            LogFields.Field("routeActivityAutoMaterialization", false),
                            LogFields.Field("routeActivityAutoRelease", false),
                            LogFields.Field("addressables", false),
                            LogFields.Field("pooling", false),
                            LogFields.Field("actorSpawn", false),
                            LogFields.Field("playerJoin", false),
                            LogFields.Field("gameplayConsumer", false),
                            LogFields.Field("cameraConsumer", false),
                            LogFields.Field("audioConsumer", false),
                            LogFields.Field("saveConsumer", false)));
                    return Task.FromResult(true);
                }
                finally
                {
                    if (!cleanupExecuted)
                    {
                        foreach (var entry in physicalRegistry.Snapshot())
                        {
                            if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                            {
                                Object.Destroy(entry.Instance);
                            }
                        }

                        if (runtimeContext.IsValid)
                        {
                            runtimeHost.UnbindContentAnchorRuntimeOwner(
                                runtimeContext.Owner,
                                QaSource,
                                "qa.pause.visual-materialization.cleanup.remaining-bindings");

                            RuntimeContentHandle[] remainingHandles = runtimeContentRuntime.SnapshotHandles(runtimeContext);
                            for (int i = 0; i < remainingHandles.Length; i++)
                            {
                                runtimeContentRuntime.ReleaseHandleLogically(
                                    runtimeContext,
                                    remainingHandles[i].Identity,
                                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                                    QaSource,
                                    "qa.pause.visual-materialization.cleanup.remaining-handles");
                            }
                        }
                    }

                    if (rootCreated && runtimeOwner.IsValid)
                    {
                        runtimeContentRuntime.RemoveScopeRoot(
                            runtimeOwner,
                            QaSource,
                            "qa.pause.visual-materialization.cleanup.root");
                    }

                    if (authoringObject != null)
                    {
                        Object.Destroy(authoringObject);
                    }

                    if (visualPrefab != null)
                    {
                        Object.Destroy(visualPrefab);
                    }

                    if (anchorObject != null)
                    {
                        Object.Destroy(anchorObject);
                    }
                }
            });
        }


        private async void RunBridgeLifecycleRegistryRegistrationSmoke()
        {
            await RunSmokeAsync("Bridge Lifecycle Registry Registration Smoke", runtimeHost =>
                Task.FromResult(RunBridgeLifecycleRegistryRegistrationSmokeCore(runtimeHost)));
        }

        private bool RunBridgeLifecycleRegistryRegistrationSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Bridge Lifecycle Registry Registration Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var lifecycleRegistry = new LifecycleMaterializationRegistry();
            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            RuntimeContentOwner firstOwner = default(RuntimeContentOwner);
            RuntimeContentOwner secondOwner = default(RuntimeContentOwner);
            bool firstRootCreated = false;
            bool secondRootCreated = false;

            string unique = Guid.NewGuid().ToString("N");
            firstOwner = RuntimeContentOwner.Transient(
                "qa.bridge-lifecycle-registry-registration.owner.1." + unique,
                "QA Bridge Lifecycle Registry Registration Smoke 1");
            secondOwner = RuntimeContentOwner.Transient(
                "qa.bridge-lifecycle-registry-registration.owner.2." + unique,
                "QA Bridge Lifecycle Registry Registration Smoke 2");

            try
            {
                setObject = new GameObject("QA Bridge Lifecycle Registry Registration Bridge Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                firstBridgeObject = new GameObject("QA Bridge Lifecycle Registry Registration Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Bridge Lifecycle Registry Registration Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Bridge Lifecycle Registry Registration Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Bridge Lifecycle Registry Registration Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Bridge Lifecycle Registry Registration Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Bridge Lifecycle Registry Registration Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    firstOwner.OwnerId,
                    firstOwner.OwnerName,
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.bridge-lifecycle-registry-registration.route.1." + unique,
                    "qa.bridge-lifecycle-registry-registration.anchor.1." + unique,
                    "qa.bridge-lifecycle-registry-registration.content.1." + unique,
                    "qa.bridge-lifecycle-registry-registration.prefab.1." + unique,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    secondOwner.OwnerId,
                    secondOwner.OwnerName,
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.bridge-lifecycle-registry-registration.route.2." + unique,
                    "qa.bridge-lifecycle-registry-registration.anchor.2." + unique,
                    "qa.bridge-lifecycle-registry-registration.content.2." + unique,
                    "qa.bridge-lifecycle-registry-registration.prefab.2." + unique,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.context.1",
                    out var firstContext);
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.context.2",
                    out var secondContext);

                bool firstEvidenceFound = TryGetSingleActiveMaterializedEvidence(firstBridge, out var firstEvidence);
                bool secondEvidenceFound = TryGetSingleActiveMaterializedEvidence(secondBridge, out var secondEvidence);
                bool materializedExplicitly = materializeAllResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.MaterializedCount == 2
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && firstRootCreated
                    && secondRootCreated
                    && firstContext.IsValid
                    && secondContext.IsValid
                    && firstEvidenceFound
                    && secondEvidenceFound
                    && firstEvidence.Handle != null
                    && secondEvidence.Handle != null
                    && firstEvidence.Handle.IsMaterialized
                    && secondEvidence.Handle.IsMaterialized
                    && HasParentedLiveInstance(firstBridge, firstAnchorObject.transform)
                    && HasParentedLiveInstance(secondBridge, secondAnchorObject.transform)
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!materializedExplicitly)
                {
                    _logger.Warning($"QA Bridge Lifecycle Registry Registration Smoke step failed. step='materialize-all' diagnostics='{materializeAllResult.ToDiagnosticString()}' firstRegistry='{firstBridge.Registry.ToDiagnosticString()}' secondRegistry='{secondBridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var firstRegisterResult = lifecycleRegistry.Register(
                    firstEvidence.Handle,
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.register.1");
                var secondRegisterResult = lifecycleRegistry.Register(
                    secondEvidence.Handle,
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.register.2");
                bool registeredIntoLifecycleRegistry = firstRegisterResult.Succeeded
                    && firstRegisterResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
                    && secondRegisterResult.Succeeded
                    && secondRegisterResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
                    && lifecycleRegistry.Count == 2
                    && lifecycleRegistry.ActiveCount == 2
                    && lifecycleRegistry.ReleaseRequestedCount == 0
                    && lifecycleRegistry.ReleasedCount == 0
                    && lifecycleRegistry.ReleaseFailedCount == 0
                    && lifecycleRegistry.Snapshot(firstOwner).Length == 1
                    && lifecycleRegistry.Snapshot(secondOwner).Length == 1
                    && lifecycleRegistry.Snapshot(RuntimeContentScope.Transient).Length == 2;
                if (!registeredIntoLifecycleRegistry)
                {
                    _logger.Warning($"QA Bridge Lifecycle Registry Registration Smoke step failed. step='lifecycle-register' first='{firstRegisterResult.ToDiagnosticString()}' second='{secondRegisterResult.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}'.");
                    return false;
                }

                var duplicateRegisterResult = lifecycleRegistry.Register(
                    firstEvidence.Handle,
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.register.duplicate");
                bool duplicateRegistrationStable = duplicateRegisterResult.Succeeded
                    && duplicateRegisterResult.Status == LifecycleMaterializationRegistryOperationStatus.SucceededAlreadyRegistered
                    && lifecycleRegistry.Count == 2
                    && lifecycleRegistry.ActiveCount == 2;
                if (!duplicateRegistrationStable)
                {
                    _logger.Warning($"QA Bridge Lifecycle Registry Registration Smoke step failed. step='duplicate-lifecycle-register' duplicate='{duplicateRegisterResult.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.bridge-lifecycle-registry-registration.release-all");
                int contentHandlesAfterRelease = runtimeContentRuntime.SnapshotHandles(firstContext).Length
                    + runtimeContentRuntime.SnapshotHandles(secondContext).Length;
                bool explicitBridgeReleaseSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 0
                    && bridgeSet.PhysicalReleaseRequestedCount == 2
                    && contentHandlesAfterRelease == 0;
                if (!explicitBridgeReleaseSucceeded)
                {
                    _logger.Warning($"QA Bridge Lifecycle Registry Registration Smoke step failed. step='explicit-bridge-release' diagnostics='{releaseAllResult.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}'.");
                    return false;
                }

                bool lifecycleRegistryEvidenceOnly = lifecycleRegistry.Count == 2
                    && lifecycleRegistry.ActiveCount == 2
                    && lifecycleRegistry.ReleaseRequestedCount == 0
                    && lifecycleRegistry.ReleasedCount == 0
                    && lifecycleRegistry.ReleaseFailedCount == 0
                    && firstEvidence.Handle.IsReleased
                    && secondEvidence.Handle.IsReleased;
                if (!lifecycleRegistryEvidenceOnly)
                {
                    _logger.Warning($"QA Bridge Lifecycle Registry Registration Smoke step failed. step='registry-evidence-only' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}' firstHandle='{firstEvidence.Handle.ToDiagnosticString()}' secondHandle='{secondEvidence.Handle.ToDiagnosticString()}'.");
                    return false;
                }

                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "lifecycle-registry-registration.1");
                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "lifecycle-registry-registration.2");
                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Bridge Lifecycle Registry Registration Smoke step completed. "
                    + $"step='unity-content-anchor-bridge-lifecycle-registry-registration' passed='True' materializeAll='{materializeAllResult.Status}' materialized='{materializeAllResult.MaterializedCount}' lifecycleRegisterFirst='{firstRegisterResult.Status}' lifecycleRegisterSecond='{secondRegisterResult.Status}' duplicateRegister='{duplicateRegisterResult.Status}' lifecycleEntries='{lifecycleRegistry.Count}' lifecycleActive='{lifecycleRegistry.ActiveCount}' lifecycleReleaseRequested='{lifecycleRegistry.ReleaseRequestedCount}' lifecycleReleased='{lifecycleRegistry.ReleasedCount}' bridgeReleaseAll='{releaseAllResult.Status}' bridgeReleased='{releaseAllResult.ReleasedCount}' bridgeRegistryEntries='{bridgeSet.RegistryEntries}' bridgeRegistryActive='{bridgeSet.RegistryActive}' bridgePhysicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{contentHandlesAfterRelease}' bridgeSetMaterializationExplicit='{materializedExplicitly}' lifecycleRegistrationExplicit='{registeredIntoLifecycleRegistry}' duplicateRegistrationStable='{duplicateRegistrationStable}' lifecycleRegistryEvidenceOnly='{lifecycleRegistryEvidenceOnly}' lifecycleRegistryPhysicalRelease='False' lifecycleRegistryLogicalRuntimeContentRelease='False' lifecycleRegistryContentAnchorBindingCleanup='False' bridgeExplicitRelease='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "lifecycle-registry-registration.1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "lifecycle-registry-registration.2");
                }
            }
        }


        private async void RunLifecycleRegistryReleasePlanSmoke()
        {
            await RunSmokeAsync("Lifecycle Registry Release Plan Smoke", runtimeHost =>
                Task.FromResult(RunLifecycleRegistryReleasePlanSmokeCore(runtimeHost)));
        }

        private bool RunLifecycleRegistryReleasePlanSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var registry = new LifecycleMaterializationRegistry();
            var owner = RuntimeContentOwner.Transient(
                "qa.lifecycle-release-plan.owner",
                "QA Lifecycle Registry Release Plan Smoke Owner");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.lifecycle-release-plan.owner.secondary",
                "QA Lifecycle Registry Release Plan Smoke Secondary Owner");

            var activeHandle = RuntimeContentHandle.Materialized(
                RuntimeContentIdentity.From(owner, "qa.lifecycle-release-plan.active"),
                QaSource,
                "qa.lifecycle-release-plan.handle.active");
            var releaseRequestedHandle = RuntimeContentHandle.Materialized(
                RuntimeContentIdentity.From(owner, "qa.lifecycle-release-plan.release-requested"),
                QaSource,
                "qa.lifecycle-release-plan.handle.release-requested");
            var releasedHandle = RuntimeContentHandle.Materialized(
                RuntimeContentIdentity.From(owner, "qa.lifecycle-release-plan.released"),
                QaSource,
                "qa.lifecycle-release-plan.handle.released");
            var releaseFailedHandle = RuntimeContentHandle.Materialized(
                RuntimeContentIdentity.From(owner, "qa.lifecycle-release-plan.release-failed"),
                QaSource,
                "qa.lifecycle-release-plan.handle.release-failed");
            var secondaryActiveHandle = RuntimeContentHandle.Materialized(
                RuntimeContentIdentity.From(secondOwner, "qa.lifecycle-release-plan.secondary-active"),
                QaSource,
                "qa.lifecycle-release-plan.handle.secondary-active");

            var activeRegister = registry.Register(
                activeHandle,
                QaSource,
                "qa.lifecycle-release-plan.register.active");
            var releaseRequestedRegister = registry.Register(
                releaseRequestedHandle,
                QaSource,
                "qa.lifecycle-release-plan.register.release-requested");
            var releasedRegister = registry.Register(
                releasedHandle,
                QaSource,
                "qa.lifecycle-release-plan.register.released");
            var releaseFailedRegister = registry.Register(
                releaseFailedHandle,
                QaSource,
                "qa.lifecycle-release-plan.register.release-failed");
            var secondaryActiveRegister = registry.Register(
                secondaryActiveHandle,
                QaSource,
                "qa.lifecycle-release-plan.register.secondary-active");

            bool registered = activeRegister.Succeeded
                && releaseRequestedRegister.Succeeded
                && releasedRegister.Succeeded
                && releaseFailedRegister.Succeeded
                && secondaryActiveRegister.Succeeded
                && registry.Count == 5
                && registry.ActiveCount == 5
                && registry.ReleaseRequestedCount == 0
                && registry.ReleasedCount == 0
                && registry.ReleaseFailedCount == 0;
            if (!registered)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='register' active='{activeRegister.ToDiagnosticString()}' releaseRequested='{releaseRequestedRegister.ToDiagnosticString()}' released='{releasedRegister.ToDiagnosticString()}' releaseFailed='{releaseFailedRegister.ToDiagnosticString()}' secondary='{secondaryActiveRegister.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var markReleaseRequested = registry.RequestRelease(
                releaseRequestedHandle.Identity,
                QaSource,
                "qa.lifecycle-release-plan.state.release-requested");
            var markReleasedRequest = registry.RequestRelease(
                releasedHandle.Identity,
                QaSource,
                "qa.lifecycle-release-plan.state.released.request");
            var markReleased = registry.MarkReleased(
                releasedHandle.Identity,
                QaSource,
                "qa.lifecycle-release-plan.state.released.complete");
            var markReleaseFailedRequest = registry.RequestRelease(
                releaseFailedHandle.Identity,
                QaSource,
                "qa.lifecycle-release-plan.state.release-failed.request");
            var markReleaseFailed = registry.MarkReleaseFailed(
                releaseFailedHandle.Identity,
                QaSource,
                "qa.lifecycle-release-plan.state.release-failed.record",
                "Synthetic release failure for release plan candidate proof.");

            bool statePrepared = markReleaseRequested.Succeeded
                && markReleasedRequest.Succeeded
                && markReleased.Succeeded
                && markReleaseFailedRequest.Succeeded
                && markReleaseFailed.Succeeded
                && registry.Count == 5
                && registry.ActiveCount == 2
                && registry.ReleaseRequestedCount == 1
                && registry.ReleasedCount == 1
                && registry.ReleaseFailedCount == 1;
            if (!statePrepared)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='state-prepare' releaseRequested='{markReleaseRequested.ToDiagnosticString()}' releasedRequest='{markReleasedRequest.ToDiagnosticString()}' released='{markReleased.ToDiagnosticString()}' releaseFailedRequest='{markReleaseFailedRequest.ToDiagnosticString()}' releaseFailed='{markReleaseFailed.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            var ownerPlan = registry.CreateReleasePlan(
                owner,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.lifecycle-release-plan.plan.owner");
            var repeatedOwnerPlan = registry.CreateReleasePlan(
                owner,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.lifecycle-release-plan.plan.owner");
            var scopePlan = registry.CreateReleasePlan(
                RuntimeContentScope.Transient,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.lifecycle-release-plan.plan.scope");
            var emptyOwner = RuntimeContentOwner.Transient(
                "qa.lifecycle-release-plan.owner.empty",
                "QA Lifecycle Registry Release Plan Smoke Empty Owner");
            var emptyPlan = registry.CreateReleasePlan(
                emptyOwner,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.lifecycle-release-plan.plan.empty");

            bool ownerPlanValid = ownerPlan.Succeeded
                && ownerPlan.Status == LifecycleMaterializationReleasePlanStatus.SucceededPlanned
                && ownerPlan.IsOwnerTargeted
                && ownerPlan.Scope == RuntimeContentScope.Transient
                && ownerPlan.Owner == owner
                && ownerPlan.Policy == RuntimeReleasePolicy.MarkReleasedAndUnregister
                && ownerPlan.TotalEntries == 4
                && ownerPlan.RequestCount == 2
                && ownerPlan.ActiveCandidates == 1
                && ownerPlan.ReleaseFailedCandidates == 1
                && ownerPlan.SkippedReleaseRequested == 1
                && ownerPlan.SkippedReleased == 1
                && ownerPlan.Requests[0].Owner == owner
                && ownerPlan.Requests[1].Owner == owner
                && ownerPlan.Requests[0].Policy == RuntimeReleasePolicy.MarkReleasedAndUnregister
                && ownerPlan.Requests[1].Policy == RuntimeReleasePolicy.MarkReleasedAndUnregister;
            if (!ownerPlanValid)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='owner-plan' plan='{ownerPlan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            bool repeatedPlanStable = ownerPlan.Equals(repeatedOwnerPlan)
                && registry.Count == 5
                && registry.ActiveCount == 2
                && registry.ReleaseRequestedCount == 1
                && registry.ReleasedCount == 1
                && registry.ReleaseFailedCount == 1;
            if (!repeatedPlanStable)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='repeated-plan' first='{ownerPlan.ToDiagnosticString()}' repeated='{repeatedOwnerPlan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            bool scopePlanValid = scopePlan.Succeeded
                && scopePlan.Status == LifecycleMaterializationReleasePlanStatus.SucceededPlanned
                && scopePlan.IsScopeTargeted
                && scopePlan.Scope == RuntimeContentScope.Transient
                && scopePlan.Policy == RuntimeReleasePolicy.MarkReleasedAndUnregister
                && scopePlan.TotalEntries == 5
                && scopePlan.RequestCount == 3
                && scopePlan.ActiveCandidates == 2
                && scopePlan.ReleaseFailedCandidates == 1
                && scopePlan.SkippedReleaseRequested == 1
                && scopePlan.SkippedReleased == 1;
            if (!scopePlanValid)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='scope-plan' plan='{scopePlan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            bool emptyPlanValid = emptyPlan.Succeeded
                && emptyPlan.Status == LifecycleMaterializationReleasePlanStatus.SucceededEmpty
                && emptyPlan.IsOwnerTargeted
                && emptyPlan.Owner == emptyOwner
                && emptyPlan.TotalEntries == 0
                && emptyPlan.RequestCount == 0
                && emptyPlan.ActiveCandidates == 0
                && emptyPlan.ReleaseFailedCandidates == 0
                && emptyPlan.SkippedReleaseRequested == 0
                && emptyPlan.SkippedReleased == 0;
            if (!emptyPlanValid)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='empty-plan' plan='{emptyPlan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            bool planningOnly = registry.Count == 5
                && registry.ActiveCount == 2
                && registry.ReleaseRequestedCount == 1
                && registry.ReleasedCount == 1
                && registry.ReleaseFailedCount == 1
                && ownerPlan.ExecutesRelease == false
                && ownerPlan.PerformsPhysicalRelease == false
                && ownerPlan.PerformsLogicalRuntimeContentRelease == false
                && ownerPlan.PerformsContentAnchorBindingCleanup == false
                && scopePlan.ExecutesRelease == false
                && emptyPlan.ExecutesRelease == false;
            if (!planningOnly)
            {
                _logger.Warning($"QA Lifecycle Registry Release Plan Smoke step failed. step='planning-only' ownerPlan='{ownerPlan.ToDiagnosticString()}' scopePlan='{scopePlan.ToDiagnosticString()}' emptyPlan='{emptyPlan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                return false;
            }

            _logger.Info(
                "QA Lifecycle Registry Release Plan Smoke step completed. "
                + $"step='lifecycle-materialization-registry-release-plan' passed='True' ownerPlan='{ownerPlan.Status}' ownerRequests='{ownerPlan.RequestCount}' ownerTotalEntries='{ownerPlan.TotalEntries}' ownerActiveCandidates='{ownerPlan.ActiveCandidates}' ownerReleaseFailedCandidates='{ownerPlan.ReleaseFailedCandidates}' ownerSkippedReleaseRequested='{ownerPlan.SkippedReleaseRequested}' ownerSkippedReleased='{ownerPlan.SkippedReleased}' repeatedPlanStable='{repeatedPlanStable}' scopePlan='{scopePlan.Status}' scopeRequests='{scopePlan.RequestCount}' scopeTotalEntries='{scopePlan.TotalEntries}' scopeActiveCandidates='{scopePlan.ActiveCandidates}' scopeReleaseFailedCandidates='{scopePlan.ReleaseFailedCandidates}' scopeSkippedReleaseRequested='{scopePlan.SkippedReleaseRequested}' scopeSkippedReleased='{scopePlan.SkippedReleased}' emptyPlan='{emptyPlan.Status}' emptyRequests='{emptyPlan.RequestCount}' entries='{registry.Count}' active='{registry.ActiveCount}' releaseRequested='{registry.ReleaseRequestedCount}' released='{registry.ReleasedCount}' releaseFailed='{registry.ReleaseFailedCount}' releasePlanQueryOnly='True' releaseExecution='False' physicalRelease='False' logicalRuntimeContentRelease='False' contentAnchorBindingCleanup='False' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
            return true;
        }

        private async void RunLifecycleRegistryReleaseExecutionSmoke()
        {
            await RunSmokeAsync("Lifecycle Registry Release Execution Smoke", runtimeHost =>
                Task.FromResult(RunLifecycleRegistryReleaseExecutionSmokeCore(runtimeHost)));
        }

        private bool RunLifecycleRegistryReleaseExecutionSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Lifecycle Registry Release Execution Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new LifecycleMaterializationRegistry();
            RuntimeContentOwner owner = default(RuntimeContentOwner);
            RuntimeScopeContext context = default(RuntimeScopeContext);
            bool rootCreated = false;

            string unique = Guid.NewGuid().ToString("N");
            owner = RuntimeContentOwner.Transient(
                "qa.lifecycle-release-execution.owner." + unique,
                "QA Lifecycle Registry Release Execution Smoke Owner");

            try
            {
                var createRoot = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.lifecycle-release-execution.root.create");
                rootCreated = createRoot.Applied || createRoot.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                bool contextCreated = runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.lifecycle-release-execution.context",
                    out context);
                if (!rootCreated || !contextCreated || !context.IsValid)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='scope' root='{createRoot.ToDiagnosticString()}' contextValid='{context.IsValid}'.");
                    return false;
                }

                var firstIdentity = context.CreateIdentity(RuntimeContentId.From("qa.lifecycle-release-execution.content.1"));
                var secondIdentity = context.CreateIdentity(RuntimeContentId.From("qa.lifecycle-release-execution.content.2"));
                var firstHandle = RuntimeContentHandle.Materialized(
                    firstIdentity,
                    QaSource,
                    "qa.lifecycle-release-execution.handle.1");
                var secondHandle = RuntimeContentHandle.Materialized(
                    secondIdentity,
                    QaSource,
                    "qa.lifecycle-release-execution.handle.2");

                var firstRuntimeRegister = runtimeContentRuntime.RegisterHandle(
                    context,
                    firstHandle,
                    QaSource,
                    "qa.lifecycle-release-execution.runtime-register.1");
                var secondRuntimeRegister = runtimeContentRuntime.RegisterHandle(
                    context,
                    secondHandle,
                    QaSource,
                    "qa.lifecycle-release-execution.runtime-register.2");
                var firstLifecycleRegister = registry.Register(
                    firstHandle,
                    QaSource,
                    "qa.lifecycle-release-execution.lifecycle-register.1");
                var secondLifecycleRegister = registry.Register(
                    secondHandle,
                    QaSource,
                    "qa.lifecycle-release-execution.lifecycle-register.2");
                bool registered = firstRuntimeRegister.Applied
                    && secondRuntimeRegister.Applied
                    && firstLifecycleRegister.Succeeded
                    && secondLifecycleRegister.Succeeded
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 2
                    && registry.Count == 2
                    && registry.ActiveCount == 2
                    && firstHandle.IsMaterialized
                    && secondHandle.IsMaterialized;
                if (!registered)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='register' firstRuntime='{firstRuntimeRegister.ToDiagnosticString()}' secondRuntime='{secondRuntimeRegister.ToDiagnosticString()}' firstLifecycle='{firstLifecycleRegister.ToDiagnosticString()}' secondLifecycle='{secondLifecycleRegister.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var plan = registry.CreateReleasePlan(
                    owner,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.lifecycle-release-execution.plan");
                bool planReady = plan.Succeeded
                    && plan.Status == LifecycleMaterializationReleasePlanStatus.SucceededPlanned
                    && plan.RequestCount == 2
                    && plan.ActiveCandidates == 2
                    && plan.ReleaseFailedCandidates == 0
                    && plan.SkippedReleaseRequested == 0
                    && plan.SkippedReleased == 0
                    && plan.ExecutesRelease == false;
                if (!planReady)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='plan' plan='{plan.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var execution = registry.ExecuteReleasePlan(
                    plan,
                    request => runtimeContentRuntime.ReleaseHandleLogically(
                        request,
                        QaSource,
                        "qa.lifecycle-release-execution.execute.logical-release"),
                    QaSource,
                    "qa.lifecycle-release-execution.execute");
                bool executed = execution.Succeeded
                    && execution.Status == LifecycleMaterializationReleaseExecutionStatus.SucceededReleasedAll
                    && execution.RequestCount == 2
                    && execution.ReleaseRequested == 2
                    && execution.Released == 2
                    && execution.ReleaseFailed == 0
                    && execution.MissingEntries == 0
                    && execution.ReleaseResultCount == 2
                    && execution.RegistryResultCount == 4
                    && execution.ExecutesRelease
                    && execution.PerformsLogicalRuntimeContentRelease
                    && !execution.PerformsPhysicalRelease
                    && !execution.PerformsContentAnchorBindingCleanup
                    && registry.Count == 2
                    && registry.ActiveCount == 0
                    && registry.ReleaseRequestedCount == 0
                    && registry.ReleasedCount == 2
                    && registry.ReleaseFailedCount == 0
                    && firstHandle.IsReleased
                    && secondHandle.IsReleased
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!executed)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='execute' execution='{execution.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}' firstHandle='{firstHandle.ToDiagnosticString()}' secondHandle='{secondHandle.ToDiagnosticString()}'.");
                    return false;
                }

                var repeatedPlan = registry.CreateReleasePlan(
                    owner,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.lifecycle-release-execution.plan.repeated");
                var repeatedExecution = registry.ExecuteReleasePlan(
                    repeatedPlan,
                    request => runtimeContentRuntime.ReleaseHandleLogically(
                        request,
                        QaSource,
                        "qa.lifecycle-release-execution.execute.repeated"),
                    QaSource,
                    "qa.lifecycle-release-execution.execute.repeated");
                bool repeatedEmpty = repeatedPlan.Succeeded
                    && repeatedPlan.Status == LifecycleMaterializationReleasePlanStatus.SucceededEmpty
                    && repeatedPlan.RequestCount == 0
                    && repeatedExecution.Succeeded
                    && repeatedExecution.Status == LifecycleMaterializationReleaseExecutionStatus.SucceededNoRequests
                    && repeatedExecution.RequestCount == 0
                    && repeatedExecution.ReleaseResultCount == 0
                    && registry.ReleasedCount == 2
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!repeatedEmpty)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='repeated-empty' plan='{repeatedPlan.ToDiagnosticString()}' execution='{repeatedExecution.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                bool boundariesPreserved = execution.PerformsPhysicalRelease == false
                    && execution.PerformsContentAnchorBindingCleanup == false
                    && repeatedExecution.PerformsPhysicalRelease == false
                    && repeatedExecution.PerformsContentAnchorBindingCleanup == false;
                if (!boundariesPreserved)
                {
                    _logger.Warning($"QA Lifecycle Registry Release Execution Smoke step failed. step='boundaries' execution='{execution.ToDiagnosticString()}' repeated='{repeatedExecution.ToDiagnosticString()}'.");
                    return false;
                }

                _logger.Info(
                    "QA Lifecycle Registry Release Execution Smoke step completed. "
                    + $"step='lifecycle-materialization-registry-release-execution' passed='True' plan='{plan.Status}' planRequests='{plan.RequestCount}' execution='{execution.Status}' executedRequests='{execution.RequestCount}' releaseRequested='{execution.ReleaseRequested}' released='{execution.Released}' releaseFailed='{execution.ReleaseFailed}' missingEntries='{execution.MissingEntries}' releaseResults='{execution.ReleaseResultCount}' registryResults='{execution.RegistryResultCount}' repeatedPlan='{repeatedPlan.Status}' repeatedExecution='{repeatedExecution.Status}' repeatedRequests='{repeatedExecution.RequestCount}' entries='{registry.Count}' active='{registry.ActiveCount}' registryReleaseRequested='{registry.ReleaseRequestedCount}' registryReleased='{registry.ReleasedCount}' registryReleaseFailed='{registry.ReleaseFailedCount}' runtimeHandles='{runtimeContentRuntime.SnapshotHandles(context).Length}' firstHandleReleased='{firstHandle.IsReleased}' secondHandleReleased='{secondHandle.IsReleased}' explicitSubmit='True' releaseExecution='True' physicalRelease='False' logicalRuntimeContentRelease='True' contentAnchorBindingCleanup='False' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                if (rootCreated && owner.IsValid)
                {
                    CleanupBridgeSetSmokeOwner(
                        runtimeHost,
                        runtimeContentRuntime,
                        owner,
                        "lifecycle-release-execution");
                }
            }
        }


        private async void RunCompositeLifecycleReleaseSmoke()
        {
            await RunSmokeAsync("Composite Lifecycle Release Smoke", runtimeHost =>
                Task.FromResult(RunCompositeLifecycleReleaseSmokeCore(runtimeHost)));
        }

        private bool RunCompositeLifecycleReleaseSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Composite Lifecycle Release Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var lifecycleRegistry = new LifecycleMaterializationRegistry();
            GameObject bridgeObject = null;
            GameObject template = null;
            GameObject anchorObject = null;
            RuntimeContentOwner owner = default(RuntimeContentOwner);
            RuntimeScopeContext context = default(RuntimeScopeContext);
            bool rootCreated = false;

            string unique = Guid.NewGuid().ToString("N");
            owner = RuntimeContentOwner.Transient(
                "qa.composite-lifecycle-release.owner." + unique,
                "QA Composite Lifecycle Release Smoke Owner");

            try
            {
                bridgeObject = new GameObject("QA Composite Lifecycle Release Bridge");
                bridgeObject.hideFlags = HideFlags.DontSave;
                var bridge = bridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                template = new GameObject("QA Composite Lifecycle Release Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Composite Lifecycle Release Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                bridge.ConfigureForDiagnostics(
                    template,
                    anchorObject.transform,
                    RuntimeContentScope.Transient,
                    owner.OwnerId,
                    owner.OwnerName,
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.composite-lifecycle-release.route." + unique,
                    "qa.composite-lifecycle-release.anchor." + unique,
                    "qa.composite-lifecycle-release.content." + unique,
                    "qa.composite-lifecycle-release.prefab." + unique,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var materialize = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.composite-lifecycle-release.materialize");
                rootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.composite-lifecycle-release.context",
                    out context);
                bool evidenceFound = TryGetSingleActiveMaterializedEvidence(bridge, out var evidence);
                bool materialized = materialize.Succeeded
                    && materialize.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized
                    && rootCreated
                    && context.IsValid
                    && evidenceFound
                    && evidence.Handle != null
                    && evidence.Handle.IsMaterialized
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 1
                    && bridge.Registry.PhysicalReleaseRequestedCount == 0
                    && HasParentedLiveInstance(bridge, anchorObject.transform)
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 1
                    && runtimeHost.SnapshotContentAnchorBindingsForRuntimeContent(evidence.Handle.Identity).Length == 1;
                if (!materialized)
                {
                    _logger.Warning($"QA Composite Lifecycle Release Smoke step failed. step='materialize' result='{materialize.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var lifecycleRegister = lifecycleRegistry.Register(
                    evidence.Handle,
                    QaSource,
                    "qa.composite-lifecycle-release.lifecycle-register");
                bool registered = lifecycleRegister.Succeeded
                    && lifecycleRegister.Status == LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
                    && lifecycleRegistry.Count == 1
                    && lifecycleRegistry.ActiveCount == 1;
                if (!registered)
                {
                    _logger.Warning($"QA Composite Lifecycle Release Smoke step failed. step='lifecycle-register' result='{lifecycleRegister.ToDiagnosticString()}' registry='{lifecycleRegistry.ToDiagnosticString()}'.");
                    return false;
                }

                var plan = lifecycleRegistry.CreateReleasePlan(
                    owner,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.composite-lifecycle-release.plan");
                bool planReady = plan.Succeeded
                    && plan.Status == LifecycleMaterializationReleasePlanStatus.SucceededPlanned
                    && plan.RequestCount == 1
                    && plan.ActiveCandidates == 1
                    && plan.ExecutesRelease == false;
                if (!planReady)
                {
                    _logger.Warning($"QA Composite Lifecycle Release Smoke step failed. step='plan' plan='{plan.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(bridge.Registry, QaSource);
                var executor = new UnityContentAnchorCompositeLifecycleReleaseExecutor(
                    releaseAdapter,
                    QaSource);
                var execution = executor.Execute(
                    runtimeHost,
                    lifecycleRegistry,
                    plan,
                    "qa.composite-lifecycle-release.execute");

                int bindingsAfterRelease = runtimeHost.SnapshotContentAnchorBindingsForRuntimeContent(evidence.Handle.Identity).Length;
                int handlesAfterRelease = runtimeContentRuntime.SnapshotHandles(context).Length;
                bool executed = execution.Succeeded
                    && execution.Status == UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededReleasedAll
                    && execution.RequestCount == 1
                    && execution.PhysicalReleaseRequests == 1
                    && execution.LogicalRuntimeReleaseResults == 1
                    && execution.BindingCleanupResults == 1
                    && execution.BindingRemoved == 1
                    && execution.LifecycleReleaseRequested == 1
                    && execution.LifecycleReleased == 1
                    && execution.LifecycleReleaseFailed == 0
                    && execution.MissingEntries == 0
                    && execution.PerformsPhysicalRelease
                    && execution.PerformsLogicalRuntimeContentRelease
                    && execution.PerformsContentAnchorBindingCleanup
                    && lifecycleRegistry.Count == 1
                    && lifecycleRegistry.ActiveCount == 0
                    && lifecycleRegistry.ReleaseRequestedCount == 0
                    && lifecycleRegistry.ReleasedCount == 1
                    && lifecycleRegistry.ReleaseFailedCount == 0
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 0
                    && bridge.Registry.PhysicalReleaseRequestedCount == 1
                    && evidence.Handle.IsReleased
                    && handlesAfterRelease == 0
                    && bindingsAfterRelease == 0;
                if (!executed)
                {
                    _logger.Warning($"QA Composite Lifecycle Release Smoke step failed. step='execute' execution='{execution.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}' physicalRegistry='{bridge.Registry.ToDiagnosticString()}' handle='{evidence.Handle.ToDiagnosticString()}' bindingsAfterRelease='{bindingsAfterRelease}' handlesAfterRelease='{handlesAfterRelease}'.");
                    return false;
                }

                var repeatedPlan = lifecycleRegistry.CreateReleasePlan(
                    owner,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.composite-lifecycle-release.plan.repeated");
                var repeatedExecution = executor.Execute(
                    runtimeHost,
                    lifecycleRegistry,
                    repeatedPlan,
                    "qa.composite-lifecycle-release.execute.repeated");
                bool repeatedEmpty = repeatedPlan.Succeeded
                    && repeatedPlan.Status == LifecycleMaterializationReleasePlanStatus.SucceededEmpty
                    && repeatedPlan.RequestCount == 0
                    && repeatedExecution.Succeeded
                    && repeatedExecution.Status == UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededNoRequests
                    && repeatedExecution.RequestCount == 0
                    && lifecycleRegistry.ReleasedCount == 1
                    && bridge.Registry.ActiveCount == 0
                    && handlesAfterRelease == 0
                    && bindingsAfterRelease == 0;
                if (!repeatedEmpty)
                {
                    _logger.Warning($"QA Composite Lifecycle Release Smoke step failed. step='repeated-empty' plan='{repeatedPlan.ToDiagnosticString()}' execution='{repeatedExecution.ToDiagnosticString()}' lifecycleRegistry='{lifecycleRegistry.ToDiagnosticString()}' physicalRegistry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                _logger.Info(
                    "QA Composite Lifecycle Release Smoke step completed. "
                    + $"step='unity-content-anchor-composite-lifecycle-release' passed='True' materialize='{materialize.Status}' lifecycleRegister='{lifecycleRegister.Status}' plan='{plan.Status}' planRequests='{plan.RequestCount}' execution='{execution.Status}' executedRequests='{execution.RequestCount}' physicalReleaseRequests='{execution.PhysicalReleaseRequests}' logicalRuntimeReleaseResults='{execution.LogicalRuntimeReleaseResults}' bindingCleanupResults='{execution.BindingCleanupResults}' bindingRemoved='{execution.BindingRemoved}' lifecycleReleaseRequested='{execution.LifecycleReleaseRequested}' lifecycleReleased='{execution.LifecycleReleased}' lifecycleReleaseFailed='{execution.LifecycleReleaseFailed}' repeatedPlan='{repeatedPlan.Status}' repeatedExecution='{repeatedExecution.Status}' lifecycleEntries='{lifecycleRegistry.Count}' lifecycleActive='{lifecycleRegistry.ActiveCount}' lifecycleReleasedEntries='{lifecycleRegistry.ReleasedCount}' physicalRegistryEntries='{bridge.Registry.Count}' physicalRegistryActive='{bridge.Registry.ActiveCount}' physicalReleaseRequested='{bridge.Registry.PhysicalReleaseRequestedCount}' runtimeHandles='{handlesAfterRelease}' contentAnchorBindings='{bindingsAfterRelease}' physicalRelease='True' logicalRuntimeContentRelease='True' contentAnchorBindingCleanup='True' compositeReleaseExplicit='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' routeActivityAutoRelease='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, owner, "composite-lifecycle-release");
                rootCreated = false;
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(bridgeObject);

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, owner, "composite-lifecycle-release");
                }
            }
        }

        private async void RunRuntimePrefabMaterializationSmoke()
        {
            await RunSmokeAsync("Runtime Prefab Materialization Smoke", runtimeHost =>
                Task.FromResult(RunRuntimePrefabMaterializationSmokeCore(runtimeHost)));
        }

        private bool RunRuntimePrefabMaterializationSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Runtime Prefab Materialization Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject template = null;
            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default(RuntimeScopeContext);
            RuntimeMaterializationRequest request = default(RuntimeMaterializationRequest);
            bool rootCreated = false;

            string ownerId = "qa.runtime-prefab-materialization.smoke." + Guid.NewGuid().ToString("N");
            var owner = RuntimeContentOwner.Transient(ownerId, "QA Runtime Prefab Materialization Smoke");

            try
            {
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.runtime-prefab-materialization.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.runtime-prefab-materialization.context",
                        out context))
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var resource = RuntimeMaterializationResource.From(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.runtime-prefab-materialization.prefab",
                    "QA Runtime Prefab Materialization Template",
                    string.Empty);

                if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                        context,
                        "qa.runtime-prefab-materialization.handle",
                        resource,
                        QaSource,
                        "qa.runtime-prefab-materialization.request",
                        out request,
                        out var requestGuardResult))
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                    return false;
                }

                var missingPrefabAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    null,
                    registry,
                    null,
                    QaSource);
                var missingPrefabResult = missingPrefabAdapter.Materialize(request);
                bool missingPrefabBlocked = missingPrefabResult.Failed
                    && missingPrefabResult.Status == RuntimeMaterializationStatus.FailedMaterializer
                    && registry.Count == 0;
                if (!missingPrefabBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='missing-prefab' status='{missingPrefabResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(missingPrefabResult.Message)}'.");
                    return false;
                }

                template = new GameObject("QA Runtime Prefab Materialization Template");
                template.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var materializationResult = materializationAdapter.Materialize(request);
                if (!materializationResult.Succeeded || !materializationResult.HasHandle || registry.ActiveCount != 1)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='materialize' status='{materializationResult.Status}' hasHandle='{materializationResult.HasHandle}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(materializationResult.Message)}'.");
                    return false;
                }

                if (!registry.TryGet(request.Identity, out var evidence) || evidence == null || !evidence.HasLiveInstance)
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='physical-evidence' reason='Unity physical evidence was not registered'.");
                    return false;
                }

                var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                    materializationResult,
                    QaSource,
                    "qa.runtime-prefab-materialization.apply");
                if (!appliedMaterializationResult.Succeeded)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='apply-materialization' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryGetHandle(context, request.Identity, out var registeredHandle)
                    || registeredHandle == null
                    || !registeredHandle.IsMaterialized)
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='registered-handle' reason='Materialized handle was not registered in the logical runtime scope root'.");
                    return false;
                }

                var duplicateMaterializationResult = materializationAdapter.Materialize(request);
                bool duplicateBlocked = duplicateMaterializationResult.Failed
                    && registry.Count == 1
                    && registry.ActiveCount == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='duplicate-materialization' status='{duplicateMaterializationResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(duplicateMaterializationResult.Message)}'.");
                    return false;
                }

                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    request.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.runtime-prefab-materialization.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.runtime-prefab-materialization.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var doubleReleaseResult = releaseAdapter.Release(releaseRequest);
                bool doubleReleaseBlocked = doubleReleaseResult.Failed
                    && doubleReleaseResult.Status == RuntimeReleaseStatus.FailedReleaseAdapter;
                if (!doubleReleaseBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='double-release' status='{doubleReleaseResult.Status}' message='{FormatValue(doubleReleaseResult.Message)}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.runtime-prefab-materialization.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Runtime Prefab Materialization Smoke step completed. "
                    + $"step='unity-prefab-materialization' passed='True' missingPrefabBlocked='{missingPrefabBlocked}' materialization='{appliedMaterializationResult.Status}' duplicateBlocked='{duplicateBlocked}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' doubleReleaseBlocked='{doubleReleaseBlocked}' registry='{registry.ToDiagnosticString()}' contentAnchorPlacement='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.runtime-prefab-materialization.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.runtime-prefab-materialization.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorPhysicalPlacementSmoke()
        {
            await RunSmokeAsync("Content Anchor Physical Placement Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorPhysicalPlacementSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorPhysicalPlacementSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Physical Placement Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            RuntimeMaterializationRequest materializationRequest = default;
            ContentAnchorBindingRequest bindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-placement.owner",
                    "QA Content Anchor Physical Placement Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-placement.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-placement.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Local,
                    "qa.content-anchor-placement.anchor-owner");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Local,
                    "qa.content-anchor-placement.slot",
                    ContentAnchorRequiredness.Optional,
                    "QA Content Anchor Physical Placement Slot",
                    "QA smoke anchor for explicit physical placement adapter proof.",
                    "QA Content Anchor Physical Placement Anchor",
                    "qa.content-anchor-placement.anchor");
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var resource = RuntimeMaterializationResource.From(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-placement.prefab",
                    "QA Content Anchor Physical Placement Template",
                    string.Empty);

                if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                        context,
                        "qa.content-anchor-placement.content",
                        resource,
                        QaSource,
                        "qa.content-anchor-placement.materialization.request",
                        out materializationRequest,
                        out var requestGuardResult))
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                    return false;
                }

                template = new GameObject("QA Content Anchor Physical Placement Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Physical Placement Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var materializationResult = materializationAdapter.Materialize(materializationRequest);
                if (!materializationResult.Succeeded || !materializationResult.HasHandle || registry.ActiveCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='materialize' status='{materializationResult.Status}' hasHandle='{materializationResult.HasHandle}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(materializationResult.Message)}'.");
                    return false;
                }

                var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                    materializationResult,
                    QaSource,
                    "qa.content-anchor-placement.materialization.apply");
                if (!appliedMaterializationResult.Succeeded)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='apply-materialization' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                    return false;
                }

                if (!registry.TryGet(materializationRequest.Identity, out var evidence)
                    || evidence == null
                    || !evidence.HasLiveInstance)
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='physical-evidence' reason='Unity physical evidence was not registered'.");
                    return false;
                }

                bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    materializationRequest.ContentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-placement.binding.request");
                var bindingResult = runtimeHost.BindContentAnchor(
                    anchorSet,
                    bindingRequest,
                    QaSource,
                    "qa.content-anchor-placement.binding.logical");
                if (!bindingResult.Succeeded || !bindingResult.HasHandle)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='logical-binding' status='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                    return false;
                }

                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var missingAnchorTransformResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    null,
                    true,
                    "qa.content-anchor-placement.missing-anchor-transform");
                bool missingAnchorTransformBlocked = missingAnchorTransformResult.Failed
                    && missingAnchorTransformResult.Status == UnityContentAnchorPlacementStatus.FailedMissingAnchorTransform
                    && evidence.Instance.transform.parent == null;
                if (!missingAnchorTransformBlocked)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='missing-anchor-transform' status='{missingAnchorTransformResult.Status}' parent='{FormatValue(evidence.Instance.transform.parent != null ? evidence.Instance.transform.parent.name : string.Empty)}' message='{FormatValue(missingAnchorTransformResult.Message)}'.");
                    return false;
                }

                var placementResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.place");
                bool placed = placementResult.Succeeded
                    && placementResult.Status == UnityContentAnchorPlacementStatus.Succeeded
                    && placementResult.ParentApplied
                    && evidence.Instance.transform.parent == anchorObject.transform
                    && evidence.Instance.transform.localPosition == Vector3.zero
                    && evidence.Instance.transform.localRotation == Quaternion.identity
                    && evidence.Instance.transform.localScale == Vector3.one;
                if (!placed)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='place' status='{placementResult.Status}' diagnostics='{placementResult.ToDiagnosticString()}'.");
                    return false;
                }

                var alreadyPlacedResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.already-placed");
                bool alreadyPlaced = alreadyPlacedResult.Succeeded
                    && alreadyPlacedResult.Status == UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced
                    && !alreadyPlacedResult.ParentApplied;
                if (!alreadyPlaced)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='already-placed' status='{alreadyPlacedResult.Status}' diagnostics='{alreadyPlacedResult.ToDiagnosticString()}'.");
                    return false;
                }

                bool logicalUnbind = runtimeHost.UnbindContentAnchor(bindingResult.Handle);
                if (!logicalUnbind)
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='logical-unbind' reason='Logical Content Anchor binding was not removed'.");
                    return false;
                }

                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-placement.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var placementAfterReleaseResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.place-after-release");
                bool placementAfterReleaseBlocked = placementAfterReleaseResult.Failed
                    && placementAfterReleaseResult.Status == UnityContentAnchorPlacementStatus.RejectedReleasedPhysicalEvidence;
                if (!placementAfterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='place-after-release' status='{placementAfterReleaseResult.Status}' diagnostics='{placementAfterReleaseResult.ToDiagnosticString()}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.content-anchor-placement.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-placement.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Physical Placement Smoke step completed. "
                    + $"step='unity-content-anchor-placement' passed='True' missingAnchorTransformBlocked='{missingAnchorTransformBlocked}' logicalBinding='{bindingResult.Status}' placement='{placementResult.Status}' alreadyPlaced='{alreadyPlaced}' logicalUnbind='{logicalUnbind}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' placementAfterReleaseBlocked='{placementAfterReleaseBlocked}' registry='{registry.ToDiagnosticString()}' materialization='{appliedMaterializationResult.Status}' contentAnchorPhysicalPlacement='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (bindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(bindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-placement.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-placement.cleanup.root-remove");
                }
            }
        }

        private async void RunContentAnchorMaterializationPipelineSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Pipeline Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationPipelineSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationPipelineSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Pipeline Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            ContentAnchorBindingRequest bindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-pipeline.owner",
                    "QA Content Anchor Materialization Pipeline Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-pipeline.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-pipeline.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Route,
                    "qa.content-anchor-pipeline.route");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Route,
                    "qa.content-anchor-pipeline.anchor",
                    ContentAnchorRequiredness.Required,
                    "QA Content Anchor Materialization Pipeline Anchor",
                    "qa.content-anchor-pipeline.anchor",
                    string.Empty,
                    string.Empty);
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.Count != 1 || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var resource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-pipeline.prefab",
                    "QA Content Anchor Materialization Pipeline Template",
                    string.Empty);
                bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-pipeline.content",
                    resource,
                    QaSource,
                    "qa.content-anchor-pipeline.binding.request");

                template = new GameObject("QA Content Anchor Materialization Pipeline Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Materialization Pipeline Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var pipeline = new UnityContentAnchorMaterializationPipeline(
                    materializationAdapter,
                    placementAdapter,
                    releaseAdapter,
                    QaSource);

                var missingAnchorTransformResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    null,
                    true,
                    "qa.content-anchor-pipeline.missing-anchor-transform");
                bool missingAnchorTransformBlocked = missingAnchorTransformResult.Failed
                    && missingAnchorTransformResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMissingAnchorTransform
                    && registry.Count == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!missingAnchorTransformBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='missing-anchor-transform' diagnostics='{missingAnchorTransformResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var pipelineResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.execute");
                bool pipelineSucceeded = pipelineResult.Succeeded
                    && pipelineResult.MaterializationResult.Succeeded
                    && pipelineResult.AppliedMaterializationResult.Succeeded
                    && pipelineResult.BindingResult.Succeeded
                    && pipelineResult.PlacementResult.Succeeded
                    && pipelineResult.PlacementResult.ParentApplied;
                if (!pipelineSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='pipeline' diagnostics='{pipelineResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                if (!registry.TryGet(bindingRequest.RuntimeIdentity, out var evidence)
                    || evidence == null
                    || !evidence.HasLiveInstance
                    || evidence.Instance.transform.parent != anchorObject.transform)
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='physical-evidence' reason='Materialized evidence was not parented under the explicit anchor Transform'.");
                    return false;
                }

                var duplicateResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization
                    && registry.Count == 1
                    && registry.ActiveCount == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                bool logicalUnbind = runtimeHost.UnbindContentAnchor(pipelineResult.BindingResult.Handle);
                if (!logicalUnbind)
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='logical-unbind' reason='Logical Content Anchor binding was not removed'.");
                    return false;
                }

                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    bindingRequest.RuntimeIdentity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-pipeline.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.content-anchor-pipeline.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var afterReleaseResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization
                    && registry.Count == 1
                    && registry.ActiveCount == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-pipeline.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Pipeline Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-pipeline' passed='True' missingAnchorTransformBlocked='{missingAnchorTransformBlocked}' pipeline='{pipelineResult.Status}' materialization='{pipelineResult.AppliedMaterializationResult.Status}' logicalBinding='{pipelineResult.BindingResult.Status}' placement='{pipelineResult.PlacementResult.Status}' duplicateBlocked='{duplicateBlocked}' logicalUnbind='{logicalUnbind}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' afterReleaseBlocked='{afterReleaseBlocked}' registry='{registry.ToDiagnosticString()}' pipelineMaterializesObject='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' contentAnchorPhysicalPlacement='True' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (bindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(bindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-pipeline.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-pipeline.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationScopeReleaseSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Scope Release Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationScopeReleaseSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationScopeReleaseSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Scope Release Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            ContentAnchorBindingRequest firstBindingRequest = default;
            ContentAnchorBindingRequest secondBindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-scope-release.owner",
                    "QA Content Anchor Materialization Scope Release Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-scope-release.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-scope-release.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Materialization Scope Release Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Route,
                    "qa.content-anchor-scope-release.route");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Route,
                    "qa.content-anchor-scope-release.anchor",
                    ContentAnchorRequiredness.Required,
                    "QA Content Anchor Materialization Scope Release Anchor",
                    "qa.content-anchor-scope-release.anchor",
                    string.Empty,
                    string.Empty);
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.Count != 1 || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var firstResource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-scope-release.prefab.1",
                    "QA Content Anchor Scope Release Template 1",
                    string.Empty);
                firstBindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-scope-release.content.1",
                    firstResource,
                    QaSource,
                    "qa.content-anchor-scope-release.binding.request.1");

                var secondResource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-scope-release.prefab.2",
                    "QA Content Anchor Scope Release Template 2",
                    string.Empty);
                secondBindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-scope-release.content.2",
                    secondResource,
                    QaSource,
                    "qa.content-anchor-scope-release.binding.request.2");

                template = new GameObject("QA Content Anchor Scope Release Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Scope Release Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var materializationPipeline = new UnityContentAnchorMaterializationPipeline(
                    materializationAdapter,
                    placementAdapter,
                    releaseAdapter,
                    QaSource);
                var scopeReleasePipeline = new UnityContentAnchorMaterializationScopeReleasePipeline(
                    releaseAdapter,
                    QaSource);

                var missingRuntimeHostResult = scopeReleasePipeline.ReleaseScope(
                    null,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.missing-host");
                bool missingRuntimeHostBlocked = missingRuntimeHostResult.Failed
                    && missingRuntimeHostResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedMissingRuntimeHost
                    && registry.Count == 0;
                if (!missingRuntimeHostBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='missing-host' diagnostics='{missingRuntimeHostResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var firstPipelineResult = materializationPipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    firstBindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-scope-release.materialize.1");
                var secondPipelineResult = materializationPipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    secondBindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-scope-release.materialize.2");
                bool materializedScope = firstPipelineResult.Succeeded
                    && secondPipelineResult.Succeeded
                    && registry.Count == 2
                    && registry.ActiveCount == 2
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 2;
                if (!materializedScope)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='materialize-scope' first='{firstPipelineResult.ToDiagnosticString()}' second='{secondPipelineResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var scopeReleaseResult = scopeReleasePipeline.ReleaseScope(
                    runtimeHost,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.release");
                bool scopeReleaseSucceeded = scopeReleaseResult.Succeeded
                    && scopeReleaseResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.Succeeded
                    && scopeReleaseResult.MatchedPhysicalEntries == 2
                    && scopeReleaseResult.PhysicalReleaseRequests == 2
                    && scopeReleaseResult.LogicalReleaseResults == 2
                    && scopeReleaseResult.BindingRemovedCount == 2
                    && registry.Count == 2
                    && registry.ActiveCount == 0
                    && registry.PhysicalReleaseRequestedCount == 2
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!scopeReleaseSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='scope-release' diagnostics='{scopeReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var secondScopeReleaseResult = scopeReleasePipeline.ReleaseScope(
                    runtimeHost,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.release-again");
                bool secondScopeReleaseIdempotent = secondScopeReleaseResult.Succeeded
                    && secondScopeReleaseResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.SucceededNoContent
                    && secondScopeReleaseResult.MatchedPhysicalEntries == 2
                    && secondScopeReleaseResult.PhysicalReleaseRequests == 0
                    && secondScopeReleaseResult.LogicalReleaseResults == 0
                    && secondScopeReleaseResult.BindingRemovedCount == 0
                    && registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!secondScopeReleaseIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='release-again' diagnostics='{secondScopeReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-scope-release.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Scope Release Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-scope-release' passed='True' missingRuntimeHostBlocked='{missingRuntimeHostBlocked}' materializedScope='{materializedScope}' scopeRelease='{scopeReleaseResult.Status}' physicalReleaseRequests='{scopeReleaseResult.PhysicalReleaseRequests}' logicalReleaseResults='{scopeReleaseResult.LogicalReleaseResults}' bindingRemoved='{scopeReleaseResult.BindingRemovedCount}' releaseAgain='{secondScopeReleaseResult.Status}' releaseAgainIdempotent='{secondScopeReleaseIdempotent}' registry='{registry.ToDiagnosticString()}' contentHandles='{runtimeContentRuntime.SnapshotHandles(context).Length}' contentAnchorPhysicalPlacement='True' pipelineMaterializesObject='True' scopeReleasePhysical='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (firstBindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(firstBindingRequest);
                        }

                        if (secondBindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(secondBindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-scope-release.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-scope-release.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationBridgeSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject bridgeObject = null;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;
            var owner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge.owner",
                "QA Content Anchor Materialization Bridge Smoke");

            try
            {
                bridgeObject = new GameObject("QA Content Anchor Materialization Bridge");
                bridgeObject.hideFlags = HideFlags.DontSave;
                var bridge = bridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                anchorObject = new GameObject("QA Content Anchor Materialization Bridge Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                bridge.ConfigureForDiagnostics(
                    null,
                    anchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge.owner",
                    "QA Content Anchor Materialization Bridge Smoke",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge.route",
                    "qa.content-anchor-materialization-bridge.anchor",
                    "qa.content-anchor-materialization-bridge.content",
                    "qa.content-anchor-materialization-bridge.prefab",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var missingPrefabResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.missing-prefab");
                bool missingPrefabBlocked = missingPrefabResult.Failed
                    && missingPrefabResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedConfiguration
                    && bridge.Registry.Count == 0;
                if (!missingPrefabBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='missing-prefab' diagnostics='{missingPrefabResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                template = new GameObject("QA Content Anchor Materialization Bridge Template");
                template.hideFlags = HideFlags.DontSave;
                bridge.ConfigureForDiagnostics(
                    template,
                    anchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge.owner",
                    "QA Content Anchor Materialization Bridge Smoke",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge.route",
                    "qa.content-anchor-materialization-bridge.anchor",
                    "qa.content-anchor-materialization-bridge.content",
                    "qa.content-anchor-materialization-bridge.prefab",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var materializeResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.materialize");
                rootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge.root-check",
                    out var context);
                bool physicalParentApplied = false;
                foreach (var evidence in bridge.Registry.Snapshot())
                {
                    if (evidence != null && evidence.HasLiveInstance && evidence.Instance.transform.parent == anchorObject.transform)
                    {
                        physicalParentApplied = true;
                        break;
                    }
                }

                bool materializeSucceeded = materializeResult.Succeeded
                    && materializeResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized
                    && materializeResult.PhysicalPlacementApplied
                    && physicalParentApplied
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 1
                    && context.IsValid
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 1;
                if (!materializeSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='materialize' diagnostics='{materializeResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var duplicateResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedMaterializationPipeline
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 1
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseResult = bridge.SubmitScopeReleaseForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.release");
                bool releaseSucceeded = releaseResult.Succeeded
                    && releaseResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased
                    && releaseResult.PhysicalReleaseRequests == 1
                    && releaseResult.LogicalReleaseResults == 1
                    && releaseResult.BindingRemovedCount == 1
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 0
                    && bridge.Registry.PhysicalReleaseRequestedCount == 1
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!releaseSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='release' diagnostics='{releaseResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAgainResult = bridge.SubmitScopeReleaseForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.release-again");
                bool releaseAgainIdempotent = releaseAgainResult.Succeeded
                    && releaseAgainResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent
                    && releaseAgainResult.PhysicalReleaseRequests == 0
                    && releaseAgainResult.LogicalReleaseResults == 0
                    && releaseAgainResult.BindingRemovedCount == 0
                    && bridge.Registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!releaseAgainIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='release-again' diagnostics='{releaseAgainResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var afterReleaseResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedMaterializationPipeline
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge' passed='True' missingPrefabBlocked='{missingPrefabBlocked}' materialize='{materializeResult.Status}' duplicateBlocked='{duplicateBlocked}' release='{releaseResult.Status}' releaseAgain='{releaseAgainResult.Status}' releaseAgainIdempotent='{releaseAgainIdempotent}' afterReleaseBlocked='{afterReleaseBlocked}' registry='{bridge.Registry.ToDiagnosticString()}' contentHandles='{runtimeContentRuntime.SnapshotHandles(context).Length}' authoredBridge='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' pipelineMaterializesObject='True' contentAnchorPhysicalPlacement='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                if (bridgeObject != null)
                {
                    var bridge = bridgeObject.GetComponent<UnityContentAnchorMaterializationBridge>();
                    if (bridge != null)
                    {
                        foreach (var entry in bridge.Registry.Snapshot())
                        {
                            if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                            {
                                Object.Destroy(entry.Instance);
                            }
                        }
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (bridgeObject != null)
                {
                    Object.Destroy(bridgeObject);
                }

                if (rootCreated)
                {
                    if (runtimeContentRuntime.TryCreateScopeContext(
                            owner,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.context",
                            out var cleanupContext))
                    {
                        runtimeHost.UnbindContentAnchorRuntimeOwner(
                            owner,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.unbind-owner");
                        runtimeContentRuntime.ReleaseScopeLogically(
                            cleanupContext,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.logical-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-materialization-bridge.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationBridgeSetSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Set Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSetSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSetSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Set Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            bool firstRootCreated = false;
            bool secondRootCreated = false;
            var firstOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set.owner.1",
                "QA Content Anchor Materialization Bridge Set Smoke 1");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set.owner.2",
                "QA Content Anchor Materialization Bridge Set Smoke 2");

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Bridge Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                var missingBridgesResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.missing-bridges");
                bool missingBridgesBlocked = missingBridgesResult.Failed
                    && missingBridgesResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedNoBridges
                    && missingBridgesResult.RegistryEntries == 0
                    && missingBridgesResult.RegistryActive == 0;
                if (!missingBridgesBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='missing-bridges' diagnostics='{missingBridgesResult.ToDiagnosticString()}'.");
                    return false;
                }

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set.owner.1",
                    "QA Content Anchor Materialization Bridge Set Smoke 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set.route.1",
                    "qa.content-anchor-materialization-bridge-set.anchor.1",
                    "qa.content-anchor-materialization-bridge-set.content.1",
                    "qa.content-anchor-materialization-bridge-set.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set.owner.2",
                    "QA Content Anchor Materialization Bridge Set Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set.route.2",
                    "qa.content-anchor-materialization-bridge-set.anchor.2",
                    "qa.content-anchor-materialization-bridge-set.content.2",
                    "qa.content-anchor-materialization-bridge-set.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root-check.1",
                    out var firstContext);
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root-check.2",
                    out var secondContext);

                bool firstParentApplied = HasParentedLiveInstance(firstBridge, firstAnchorObject.transform);
                bool secondParentApplied = HasParentedLiveInstance(secondBridge, secondAnchorObject.transform);
                bool materializedAll = materializeAllResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.BridgeCount == 2
                    && materializeAllResult.MaterializedCount == 2
                    && materializeAllResult.RegistryEntries == 2
                    && materializeAllResult.RegistryActive == 2
                    && firstParentApplied
                    && secondParentApplied
                    && firstContext.IsValid
                    && secondContext.IsValid
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!materializedAll)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='materialize-all' diagnostics='{materializeAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var duplicateResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterialization
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.release-all");
                bool releaseAllSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && releaseAllResult.PhysicalReleaseRequests == 2
                    && releaseAllResult.LogicalReleaseResults == 2
                    && releaseAllResult.BindingRemovedCount == 2
                    && releaseAllResult.RegistryEntries == 2
                    && releaseAllResult.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAllSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAgainResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.release-again");
                bool releaseAgainIdempotent = releaseAgainResult.Succeeded
                    && releaseAgainResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent
                    && releaseAgainResult.ReleasedCount == 0
                    && releaseAgainResult.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAgainIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='release-again' diagnostics='{releaseAgainResult.ToDiagnosticString()}'.");
                    return false;
                }

                var afterReleaseResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterialization
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}'.");
                    return false;
                }

                var firstRootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root.remove.1");
                if (!firstRootRemoveResult.Applied && firstRootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='root-remove-1' status='{firstRootRemoveResult.Status}' message='{FormatValue(firstRootRemoveResult.Message)}'.");
                    return false;
                }

                var secondRootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root.remove.2");
                if (!secondRootRemoveResult.Applied && secondRootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='root-remove-2' status='{secondRootRemoveResult.Status}' message='{FormatValue(secondRootRemoveResult.Message)}'.");
                    return false;
                }

                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Set Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge-set' passed='True' missingBridgesBlocked='{missingBridgesBlocked}' materializeAll='{materializeAllResult.Status}' bridgeCount='{materializeAllResult.BridgeCount}' materialized='{materializeAllResult.MaterializedCount}' duplicateBlocked='{duplicateBlocked}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' releaseAgain='{releaseAgainResult.Status}' releaseAgainIdempotent='{releaseAgainIdempotent}' afterReleaseBlocked='{afterReleaseBlocked}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{runtimeContentRuntime.SnapshotHandles(firstContext).Length + runtimeContentRuntime.SnapshotHandles(secondContext).Length}' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "2");
                }
            }
        }

        private async void RunContentAnchorMaterializationBridgeSetPreflightSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Set Preflight Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSetPreflightSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSetPreflightSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Set Preflight Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            bool firstRootCreated = false;
            bool secondRootCreated = false;
            var firstOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                "QA Content Anchor Materialization Bridge Set Preflight Smoke 1");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                "QA Content Anchor Materialization Bridge Set Preflight Smoke 2");

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();
                firstBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                firstTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    null,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var missingPrefabPreflightResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.missing-prefab");
                bool missingPrefabBlockedBeforeSideEffects = missingPrefabPreflightResult.Failed
                    && missingPrefabPreflightResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationPreflight
                    && missingPrefabPreflightResult.MaterializedCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!missingPrefabBlockedBeforeSideEffects)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='missing-prefab-preflight' diagnostics='{missingPrefabPreflightResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke Duplicate",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.duplicate",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.duplicate",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.duplicate",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                var duplicateKeyPreflightResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.duplicate-key");
                bool duplicateKeyBlockedBeforeSideEffects = duplicateKeyPreflightResult.Failed
                    && duplicateKeyPreflightResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedDuplicateMaterializationKey
                    && duplicateKeyPreflightResult.MaterializedCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!duplicateKeyBlockedBeforeSideEffects)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='duplicate-key-preflight' diagnostics='{duplicateKeyPreflightResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.root-check.1",
                    out var firstContext);
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.root-check.2",
                    out var secondContext);
                bool materializedAllAfterPreflight = materializeAllResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.MaterializedCount == 2
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!materializedAllAfterPreflight)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='materialize-all-after-preflight' diagnostics='{materializeAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.release-all");
                bool releaseAllSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && releaseAllResult.PhysicalReleaseRequests == 2
                    && releaseAllResult.LogicalReleaseResults == 2
                    && releaseAllResult.BindingRemovedCount == 2
                    && bridgeSet.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAllSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "preflight.1");
                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "preflight.2");
                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge-set-preflight' passed='True' missingPrefabBlockedBeforeSideEffects='{missingPrefabBlockedBeforeSideEffects}' duplicateKeyBlockedBeforeSideEffects='{duplicateKeyBlockedBeforeSideEffects}' materializeAll='{materializeAllResult.Status}' materialized='{materializeAllResult.MaterializedCount}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{runtimeContentRuntime.SnapshotHandles(firstContext).Length + runtimeContentRuntime.SnapshotHandles(secondContext).Length}' batchPreflight='True' partialMaterializationBlocked='True' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "preflight.1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "preflight.2");
                }
            }
        }

        private async void RunContentAnchorMaterializationBridgeSetRollbackSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Set Rollback Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSetRollbackSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSetRollbackSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Set Rollback Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            bool firstRootCreated = false;
            bool secondRootCreated = false;
            var firstOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-rollback.owner.1",
                "QA Content Anchor Materialization Bridge Set Rollback Smoke 1");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-rollback.owner.2",
                "QA Content Anchor Materialization Bridge Set Rollback Smoke 2");

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Bridge Set Rollback");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();
                firstBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                firstTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Rollback Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-rollback.owner.1",
                    "QA Content Anchor Materialization Bridge Set Rollback Smoke 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-rollback.route.1",
                    "qa.content-anchor-materialization-bridge-set-rollback.anchor.1",
                    "qa.content-anchor-materialization-bridge-set-rollback.content.1",
                    "qa.content-anchor-materialization-bridge-set-rollback.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-rollback.owner.2",
                    "QA Content Anchor Materialization Bridge Set Rollback Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-rollback.route.2",
                    "qa.content-anchor-materialization-bridge-set-rollback.anchor.2",
                    "qa.content-anchor-materialization-bridge-set-rollback.content.2",
                    "qa.content-anchor-materialization-bridge-set-rollback.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var preExistingMaterializationResult = secondBridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.preexisting-materialize");
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.context.preexisting",
                    out var secondContext);
                bool preExistingPrepared = preExistingMaterializationResult.Succeeded
                    && preExistingMaterializationResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized
                    && secondContext.IsValid
                    && secondBridge.RegistryActiveCount == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1
                    && HasParentedLiveInstance(secondBridge, secondAnchorObject.transform);
                if (!preExistingPrepared)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Rollback Smoke step failed. step='preexisting-materialize' diagnostics='{preExistingMaterializationResult.ToDiagnosticString()}'.");
                    return false;
                }

                var rollbackResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.context.1",
                    out var firstContext);
                runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.context.2",
                    out secondContext);

                int firstHandleCountAfterRollback = firstContext.IsValid
                    ? runtimeContentRuntime.SnapshotHandles(firstContext).Length
                    : -1;
                int secondHandleCountAfterRollback = secondContext.IsValid
                    ? runtimeContentRuntime.SnapshotHandles(secondContext).Length
                    : -1;
                bool rollbackRequestedPhysicalRelease = firstBridge.PhysicalReleaseRequestedCount == 1;
                bool firstLogicalStateRolledBack = firstContext.IsValid
                    && firstBridge.RegistryCount == 1
                    && firstBridge.RegistryActiveCount == 0
                    && firstHandleCountAfterRollback == 0;
                bool preExistingPreserved = secondContext.IsValid
                    && secondBridge.RegistryCount == 1
                    && secondBridge.RegistryActiveCount == 1
                    && HasParentedLiveInstance(secondBridge, secondAnchorObject.transform)
                    && secondHandleCountAfterRollback == 1;
                bool partialMaterializationRolledBack = rollbackResult.Failed
                    && rollbackResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationRolledBack
                    && rollbackResult.MaterializedCount == 1
                    && rollbackResult.ReleasedCount == 1
                    && rollbackResult.FailedCount == 1
                    && rollbackRequestedPhysicalRelease
                    && firstLogicalStateRolledBack
                    && preExistingPreserved;
                if (!partialMaterializationRolledBack)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Rollback Smoke step failed. step='rollback' diagnostics='{rollbackResult.ToDiagnosticString()}' firstRegistry='{firstBridge.Registry.ToDiagnosticString()}' secondRegistry='{secondBridge.Registry.ToDiagnosticString()}' firstHandleCount='{firstHandleCountAfterRollback}' secondHandleCount='{secondHandleCountAfterRollback}' rollbackRequestedPhysicalRelease='{rollbackRequestedPhysicalRelease}' firstLogicalStateRolledBack='{firstLogicalStateRolledBack}' preExistingPreserved='{preExistingPreserved}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-rollback.release-all");
                bool releaseAllSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 1
                    && firstBridge.RegistryActiveCount == 0
                    && secondBridge.RegistryActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAllSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Rollback Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "rollback.1");
                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "rollback.2");
                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Set Rollback Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge-set-rollback' passed='True' preExisting='{preExistingMaterializationResult.Status}' materializeAll='{rollbackResult.Status}' materialized='{rollbackResult.MaterializedCount}' rollbackReleased='{rollbackResult.ReleasedCount}' failed='{rollbackResult.FailedCount}' firstRegistryEntries='{firstBridge.RegistryCount}' firstRegistryActive='{firstBridge.RegistryActiveCount}' secondRegistryEntries='{secondBridge.RegistryCount}' secondRegistryActive='{secondBridge.RegistryActiveCount}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' contentHandles='{runtimeContentRuntime.SnapshotHandles(firstContext).Length + runtimeContentRuntime.SnapshotHandles(secondContext).Length}' rollbackAttempted='True' rollbackRequestedPhysicalRelease='{rollbackRequestedPhysicalRelease}' firstLogicalStateRolledBack='{firstLogicalStateRolledBack}' partialMaterializationRolledBack='{partialMaterializationRolledBack}' preExistingPreserved='{preExistingPreserved}' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "rollback.1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "rollback.2");
                }
            }
        }

        private async void RunContentAnchorMaterializationAuthoringValidationSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Authoring Validation Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationAuthoringValidationSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationAuthoringValidationSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Authoring Validation Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Authoring Validation Bridge 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                secondBridgeObject = new GameObject("QA Content Anchor Materialization Authoring Validation Bridge 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Authoring Validation Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Authoring Validation Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Authoring Validation Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Authoring Validation Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                var invalidBridgeResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridge(
                    firstBridge,
                    "qa.content-anchor-authoring.invalid-bridge");
                bool invalidBridgeBlocked = invalidBridgeResult.Failed
                    && invalidBridgeResult.Status == UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeConfiguration
                    && invalidBridgeResult.MissingPrefabCount == 1
                    && invalidBridgeResult.MissingAnchorTransformCount == 1
                    && firstBridge.RegistryCount == 0;
                if (!invalidBridgeBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='invalid-bridge' diagnostics='{invalidBridgeResult.ToDiagnosticString()}'.");
                    return false;
                }

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.duplicate",
                    "QA Content Anchor Materialization Authoring Validation Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.1",
                    "qa.content-anchor-authoring.anchor.1",
                    "qa.content-anchor-authoring.content.duplicate",
                    "qa.content-anchor-authoring.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.duplicate",
                    "QA Content Anchor Materialization Authoring Validation Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.2",
                    "qa.content-anchor-authoring.anchor.2",
                    "qa.content-anchor-authoring.content.duplicate",
                    "qa.content-anchor-authoring.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var duplicateKeyResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                    bridgeSet,
                    "qa.content-anchor-authoring.duplicate-key");
                bool duplicateKeyBlocked = duplicateKeyResult.Failed
                    && duplicateKeyResult.Status == UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeSetConfiguration
                    && duplicateKeyResult.DuplicateMaterializationKeyCount == 1
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!duplicateKeyBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='duplicate-key' diagnostics='{duplicateKeyResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.2",
                    "QA Content Anchor Materialization Authoring Validation Owner 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.2",
                    "qa.content-anchor-authoring.anchor.2",
                    "qa.content-anchor-authoring.content.2",
                    "qa.content-anchor-authoring.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var validFirstBridgeResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridge(
                    firstBridge,
                    "qa.content-anchor-authoring.valid-bridge.1");
                var validSetResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                    bridgeSet,
                    "qa.content-anchor-authoring.valid-set");
                bool validSetSucceeded = validFirstBridgeResult.Succeeded
                    && validSetResult.Succeeded
                    && validSetResult.BridgeCount == 2
                    && validSetResult.BlockingIssueCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0
                    && bridgeSet.PhysicalReleaseRequestedCount == 0;
                if (!validSetSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='valid-set' bridge='{validFirstBridgeResult.ToDiagnosticString()}' set='{validSetResult.ToDiagnosticString()}'.");
                    return false;
                }

                _logger.Info(
                    "QA Content Anchor Materialization Authoring Validation Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-authoring-validation' passed='True' invalidBridgeBlocked='{invalidBridgeBlocked}' duplicateKeyBlocked='{duplicateKeyBlocked}' validBridge='{validFirstBridgeResult.Status}' validSet='{validSetResult.Status}' bridgeCount='{validSetResult.BridgeCount}' blockingIssues='{validSetResult.BlockingIssueCount}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' authoringValidation='True' batchPreflight='True' noRuntimeSideEffects='True' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }
            }
        }

        public void RunContentAnchorMaterializationRuntimeAuthoringGateSmoke()
        {
            _ = RunContentAnchorMaterializationRuntimeAuthoringGateSmokeAsync();
        }

        private async Task RunContentAnchorMaterializationRuntimeAuthoringGateSmokeAsync()
        {
            await RunSmokeAsync("Content Anchor Materialization Runtime Authoring Gate Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationRuntimeAuthoringGateSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationRuntimeAuthoringGateSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Runtime Authoring Gate Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Bridge 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                secondBridgeObject = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Bridge 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Runtime Authoring Gate Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);
                var invalidAuthoringGateResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-runtime-authoring-gate.invalid");
                bool invalidBridgeBlockedByRuntimeGate = invalidAuthoringGateResult.Failed
                    && invalidAuthoringGateResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedAuthoringValidation
                    && invalidAuthoringGateResult.RegistryEntries == 0
                    && invalidAuthoringGateResult.RegistryActive == 0
                    && invalidAuthoringGateResult.PhysicalReleaseRequests == 0;
                if (!invalidBridgeBlockedByRuntimeGate)
                {
                    _logger.Warning($"QA Content Anchor Materialization Runtime Authoring Gate Smoke step failed. step='invalid-authoring-gate' diagnostics='{invalidAuthoringGateResult.ToDiagnosticString()}'.");
                    return false;
                }

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-runtime-authoring-gate.owner.duplicate",
                    "QA Content Anchor Materialization Runtime Authoring Gate Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-runtime-authoring-gate.route.1",
                    "qa.content-anchor-runtime-authoring-gate.anchor.1",
                    "qa.content-anchor-runtime-authoring-gate.content.duplicate",
                    "qa.content-anchor-runtime-authoring-gate.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-runtime-authoring-gate.owner.duplicate",
                    "QA Content Anchor Materialization Runtime Authoring Gate Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-runtime-authoring-gate.route.2",
                    "qa.content-anchor-runtime-authoring-gate.anchor.2",
                    "qa.content-anchor-runtime-authoring-gate.content.duplicate",
                    "qa.content-anchor-runtime-authoring-gate.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var duplicateKeyRuntimeGateResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-runtime-authoring-gate.duplicate-key");
                bool duplicateKeyBlockedByRuntimeGate = duplicateKeyRuntimeGateResult.Failed
                    && duplicateKeyRuntimeGateResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedAuthoringValidation
                    && duplicateKeyRuntimeGateResult.RegistryEntries == 0
                    && duplicateKeyRuntimeGateResult.RegistryActive == 0
                    && duplicateKeyRuntimeGateResult.PhysicalReleaseRequests == 0;
                if (!duplicateKeyBlockedByRuntimeGate)
                {
                    _logger.Warning($"QA Content Anchor Materialization Runtime Authoring Gate Smoke step failed. step='duplicate-key-runtime-gate' diagnostics='{duplicateKeyRuntimeGateResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-runtime-authoring-gate.owner.2",
                    "QA Content Anchor Materialization Runtime Authoring Gate Owner 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-runtime-authoring-gate.route.2",
                    "qa.content-anchor-runtime-authoring-gate.anchor.2",
                    "qa.content-anchor-runtime-authoring-gate.content.2",
                    "qa.content-anchor-runtime-authoring-gate.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var validAuthoringResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                    bridgeSet,
                    "qa.content-anchor-runtime-authoring-gate.valid-authoring");
                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-runtime-authoring-gate.materialize-all");
                bool validGateMaterialized = validAuthoringResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.MaterializedCount == 2
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && HasParentedLiveInstance(firstBridge, firstAnchorObject.transform)
                    && HasParentedLiveInstance(secondBridge, secondAnchorObject.transform);
                if (!validGateMaterialized)
                {
                    _logger.Warning($"QA Content Anchor Materialization Runtime Authoring Gate Smoke step failed. step='valid-gate-materialize' authoring='{validAuthoringResult.ToDiagnosticString()}' diagnostics='{materializeAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-runtime-authoring-gate.release-all");
                bool releaseSucceeded = releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && bridgeSet.RegistryActive == 0
                    && bridgeSet.PhysicalReleaseRequestedCount == 2;
                if (!releaseSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Runtime Authoring Gate Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                int contentHandles = releaseAllResult.ContentHandleCount;

                _logger.Info(
                    "QA Content Anchor Materialization Runtime Authoring Gate Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-runtime-authoring-gate' passed='True' invalidBridgeBlockedByRuntimeGate='{invalidBridgeBlockedByRuntimeGate}' duplicateKeyBlockedByRuntimeGate='{duplicateKeyBlockedByRuntimeGate}' authoringGateStatus='{validAuthoringResult.Status}' materializeAll='{materializeAllResult.Status}' materialized='{materializeAllResult.MaterializedCount}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{contentHandles}' runtimeUsesAuthoringValidation='True' batchPreflight='True' noPartialMaterialization='True' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }
            }
        }


        public void RunContentAnchorMaterializationDiagnosticsSnapshotSmoke()
        {
            _ = RunContentAnchorMaterializationDiagnosticsSnapshotSmokeAsync();
        }

        private async Task RunContentAnchorMaterializationDiagnosticsSnapshotSmokeAsync()
        {
            await RunSmokeAsync("Content Anchor Materialization Diagnostics Snapshot Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationDiagnosticsSnapshotSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationDiagnosticsSnapshotSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Diagnostics Snapshot Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Bridge 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                secondBridgeObject = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Bridge 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Diagnostics Snapshot Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-diagnostics-snapshot.owner.1",
                    "QA Content Anchor Materialization Diagnostics Snapshot Owner 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-diagnostics-snapshot.route.1",
                    "qa.content-anchor-diagnostics-snapshot.anchor.1",
                    "qa.content-anchor-diagnostics-snapshot.content.1",
                    "qa.content-anchor-diagnostics-snapshot.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-diagnostics-snapshot.owner.2",
                    "QA Content Anchor Materialization Diagnostics Snapshot Owner 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-diagnostics-snapshot.route.2",
                    "qa.content-anchor-diagnostics-snapshot.anchor.2",
                    "qa.content-anchor-diagnostics-snapshot.content.2",
                    "qa.content-anchor-diagnostics-snapshot.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var initialSnapshot = bridgeSet.CreateDiagnosticsSnapshotForDiagnostics(QaSource);
                var repeatedInitialSnapshot = bridgeSet.CreateDiagnosticsSnapshotForDiagnostics(QaSource);
                bool initialSnapshotValid = initialSnapshot.AuthoringValid
                    && initialSnapshot.BridgeCount == 2
                    && initialSnapshot.RegistryEntries == 0
                    && initialSnapshot.RegistryActive == 0
                    && initialSnapshot.PhysicalReleaseRequests == 0
                    && initialSnapshot.ContentHandleCount == 0
                    && initialSnapshot.LastMaterializeAllStatus == UnityContentAnchorMaterializationBridgeSetStatus.Unknown
                    && initialSnapshot.LastReleaseAllStatus == UnityContentAnchorMaterializationBridgeSetStatus.Unknown
                    && repeatedInitialSnapshot.RegistryEntries == initialSnapshot.RegistryEntries
                    && repeatedInitialSnapshot.PhysicalReleaseRequests == initialSnapshot.PhysicalReleaseRequests
                    && initialSnapshot.NoRuntimeSideEffects;
                if (!initialSnapshotValid)
                {
                    _logger.Warning($"QA Content Anchor Materialization Diagnostics Snapshot Smoke step failed. step='initial-snapshot' diagnostics='{initialSnapshot.ToDiagnosticString()}'.");
                    return false;
                }

                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-diagnostics-snapshot.materialize-all");
                var activeSnapshot = bridgeSet.CreateDiagnosticsSnapshotForDiagnostics(QaSource);
                bool activeSnapshotValid = materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && activeSnapshot.HasLastMaterializeAllResult
                    && !activeSnapshot.HasLastReleaseAllResult
                    && activeSnapshot.LastMaterializeAllStatus == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && activeSnapshot.LastMaterializedCount == 2
                    && activeSnapshot.RegistryEntries == 2
                    && activeSnapshot.RegistryActive == 2
                    && activeSnapshot.PhysicalReleaseRequests == 0
                    && HasParentedLiveInstance(firstBridge, firstAnchorObject.transform)
                    && HasParentedLiveInstance(secondBridge, secondAnchorObject.transform);
                if (!activeSnapshotValid)
                {
                    _logger.Warning($"QA Content Anchor Materialization Diagnostics Snapshot Smoke step failed. step='active-snapshot' materialize='{materializeAllResult.ToDiagnosticString()}' diagnostics='{activeSnapshot.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-diagnostics-snapshot.release-all");
                var releasedSnapshot = bridgeSet.CreateDiagnosticsSnapshotForDiagnostics(QaSource);
                var repeatedReleasedSnapshot = bridgeSet.CreateDiagnosticsSnapshotForDiagnostics(QaSource);
                bool releasedSnapshotValid = releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releasedSnapshot.HasLastMaterializeAllResult
                    && releasedSnapshot.HasLastReleaseAllResult
                    && releasedSnapshot.LastReleaseAllStatus == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releasedSnapshot.LastReleasedCount == 2
                    && releasedSnapshot.RegistryEntries == 2
                    && releasedSnapshot.RegistryActive == 0
                    && releasedSnapshot.PhysicalReleaseRequests == 2
                    && releasedSnapshot.ContentHandleCount == 0
                    && repeatedReleasedSnapshot.RegistryEntries == releasedSnapshot.RegistryEntries
                    && repeatedReleasedSnapshot.RegistryActive == releasedSnapshot.RegistryActive
                    && repeatedReleasedSnapshot.PhysicalReleaseRequests == releasedSnapshot.PhysicalReleaseRequests
                    && repeatedReleasedSnapshot.ContentHandleCount == releasedSnapshot.ContentHandleCount
                    && releasedSnapshot.NoRuntimeSideEffects;
                if (!releasedSnapshotValid)
                {
                    _logger.Warning($"QA Content Anchor Materialization Diagnostics Snapshot Smoke step failed. step='released-snapshot' release='{releaseAllResult.ToDiagnosticString()}' diagnostics='{releasedSnapshot.ToDiagnosticString()}'.");
                    return false;
                }

                _logger.Info(
                    "QA Content Anchor Materialization Diagnostics Snapshot Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-diagnostics-snapshot' passed='True' initialAuthoring='{initialSnapshot.AuthoringStatus}' initialRegistryEntries='{initialSnapshot.RegistryEntries}' materializeAll='{materializeAllResult.Status}' activeRegistryEntries='{activeSnapshot.RegistryEntries}' activeRegistryActive='{activeSnapshot.RegistryActive}' releaseAll='{releaseAllResult.Status}' releasedRegistryEntries='{releasedSnapshot.RegistryEntries}' releasedRegistryActive='{releasedSnapshot.RegistryActive}' physicalReleaseRequests='{releasedSnapshot.PhysicalReleaseRequests}' contentHandles='{releasedSnapshot.ContentHandleCount}' snapshotQueryOnly='True' repeatedSnapshotStable='True' runtimeUsesAuthoringValidation='{releasedSnapshot.RuntimeUsesAuthoringValidation}' batchPreflight='{releasedSnapshot.BatchPreflight}' noRuntimeSideEffects='{releasedSnapshot.NoRuntimeSideEffects}' authoredBridgeSet='{releasedSnapshot.AuthoredBridgeSet}' explicitSubmit='{releasedSnapshot.ExplicitSubmit}' automaticLifecycleWiring='{releasedSnapshot.AutomaticLifecycleWiring}' routeActivityAutoMaterialization='{releasedSnapshot.RouteActivityAutoMaterialization}' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='{releasedSnapshot.Addressables}' pooling='{releasedSnapshot.Pooling}' actorSpawn='{releasedSnapshot.ActorSpawn}' playerJoin='{releasedSnapshot.PlayerJoin}' gameplayConsumer='{releasedSnapshot.GameplayConsumer}' cameraConsumer='{releasedSnapshot.CameraConsumer}' audioConsumer='{releasedSnapshot.AudioConsumer}' saveConsumer='{releasedSnapshot.SaveConsumer}'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }
            }
        }

        private static bool TryGetSingleActiveMaterializedEvidence(
            UnityContentAnchorMaterializationBridge bridge,
            out UnityRuntimeMaterializedObjectEvidence evidence)
        {
            evidence = null;
            if (bridge == null)
            {
                return false;
            }

            int activeCount = 0;
            foreach (var entry in bridge.Registry.Snapshot())
            {
                if (entry == null || !entry.HasLiveInstance || entry.PhysicalReleaseRequested || entry.Handle == null || !entry.Handle.IsMaterialized)
                {
                    continue;
                }

                evidence = entry;
                activeCount++;
            }

            return activeCount == 1 && evidence != null;
        }

        private static bool HasParentedLiveInstance(UnityContentAnchorMaterializationBridge bridge, Transform expectedParent)
        {
            if (bridge == null || expectedParent == null)
            {
                return false;
            }

            foreach (var evidence in bridge.Registry.Snapshot())
            {
                if (evidence != null && evidence.HasLiveInstance && evidence.Instance.transform.parent == expectedParent)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyBridgeSetSmokeObject(GameObject bridgeObject)
        {
            if (bridgeObject == null)
            {
                return;
            }

            var bridge = bridgeObject.GetComponent<UnityContentAnchorMaterializationBridge>();
            if (bridge != null)
            {
                foreach (var entry in bridge.Registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }
            }

            Object.Destroy(bridgeObject);
        }

        private static void CleanupBridgeSetSmokeOwner(
            FrameworkRuntimeHost runtimeHost,
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentOwner owner,
            string suffix)
        {
            if (runtimeHost == null || runtimeContentRuntime == null)
            {
                return;
            }

            if (runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.context." + suffix,
                    out var cleanupContext))
            {
                runtimeHost.UnbindContentAnchorRuntimeOwner(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.unbind-owner." + suffix);
                runtimeContentRuntime.ReleaseScopeLogically(
                    cleanupContext,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.logical-release." + suffix);
            }

            runtimeContentRuntime.RemoveScopeRoot(
                owner,
                QaSource,
                "qa.content-anchor-materialization-bridge-set.cleanup.root-remove." + suffix);
        }

        private async void RunRouteSceneCompositionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Route Scene Composition Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (routeSceneCompositionRoute != null)
            {
                AppendMissingQaAsset(ref missingTargets, !routeSceneCompositionRoute.HasRouteContentProfile, "Route Content Profile on Composition Route");
                if (routeSceneCompositionRoute.HasRouteContentProfile)
                {
                    AppendMissingQaAsset(
                        ref missingTargets,
                        !routeSceneCompositionRoute.RouteContentProfile.HasAdditionalScenes,
                        "Additional Scene in Route Content Profile");
                }
            }

            if (!TryValidateSmokeTargets("Route Scene Composition Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Scene Composition Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Route Scene Composition Smoke step failed. step='prepare' reason='Composition Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.route.composition.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Route Scene Composition Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var compositionResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.composition")));
                return ValidateRouteSceneCompositionSmokeStep(compositionResult, "composition");
            });
        }

        private async void RunRouteReleaseSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Route Scene Composition Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Release Target Route");

            if (routeSceneCompositionRoute != null)
            {
                AppendMissingQaAsset(ref missingTargets, !routeSceneCompositionRoute.HasRouteContentProfile, "Route Content Profile on Composition Route");
                if (routeSceneCompositionRoute.HasRouteContentProfile)
                {
                    AppendMissingQaAsset(
                        ref missingTargets,
                        !routeSceneCompositionRoute.RouteContentProfile.HasAdditionalScenes,
                        "Additional Scene in Route Content Profile");
                }
            }

            if (!TryValidateSmokeTargets("Route Release Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Release Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    var prepareCompositionResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        routeSceneCompositionRoute,
                        ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.release.prepare")));
                    if (!ValidateRouteSceneCompositionSmokeStep(prepareCompositionResult, "prepare-composition"))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                {
                    _logger.Warning("QA Route Release Smoke step failed. step='release' reason='Alternate Route points to the same Route as the release source'.");
                    return false;
                }

                var releaseResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    alternateRoute,
                    ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.route.release")));
                if (!ValidateRouteReleaseSmokeStep(releaseResult, "release"))
                {
                    return false;
                }

                await Task.Yield();

                var restoreCompositionResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.release.restore")));
                return ValidateRouteSceneCompositionSmokeStep(restoreCompositionResult, "restore-composition");
            });
        }

        private async void RunContentAnchorDiagnosticsSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Content Anchor Diagnostics Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Diagnostics Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Content Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor.diagnostics")));
                return ValidateContentAnchorDiagnosticsSmokeStep(anchorResult, "anchors");
            });
        }

        private async void RunActivityContentAnchorDiagnosticsSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Activity Content Anchor Diagnostics Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Diagnostics Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Activity Content Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor.diagnostics")));
                return ValidateActivityContentAnchorDiagnosticsSmokeStep(anchorResult, "activity-anchors");
            });
        }

        private async void RunActivityContentAnchorPositiveSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Positive Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity / Positive Activity Anchor Owner");

            if (!TryValidateSmokeTargets("Activity Content Anchor Positive Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Positive Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='prepare' reason='Positive Activity Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor-positive.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var routeResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor-positive.route")));
                if (!routeResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='route' reason='Route request did not succeed' status='{FormatValue(routeResult.Kind.ToString())}'.");
                    return false;
                }

                GameObject fixtureObject = null;
                try
                {
                    if (!TryCreatePositiveActivityContentAnchorFixture(primaryActivity, out fixtureObject, out var fixtureAnchor))
                    {
                        return false;
                    }

                    var clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-positive.clear"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='clear' reason='Activity clear did not succeed before positive rediscovery' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();

                    var activityResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-positive.activity")));
                    return ValidateActivityContentAnchorPositiveSmokeStep(
                        activityResult,
                        "activity-anchor-positive",
                        fixtureAnchor);
                }
                finally
                {
                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                    }
                }
            });
        }


        private async void RunActivityContentExecutionRuntimeSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Activity Content Execution Runtime Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Runtime Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")),
                        allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                var activityFlow = runtimeHost.State.ActivityFlowResult;
                var activity = runtimeHost.State.CurrentActivity;
                if (activity == null)
                {
                    _logger.Warning("QA Activity Content Execution Runtime Smoke step failed. step='activity' reason='No active Activity is available'.");
                    return false;
                }

                var runtimeScope = activityFlow.RuntimeActivityScopeResult;
                if (!runtimeScope.HasContext)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='context' reason='Activity runtime scope context unavailable' runtimeActivityScope='{FormatValue(runtimeScope.DiagnosticStatus)}' runtimeActivityContext='{FormatValue(runtimeScope.ContextStatus)}'.");
                    return false;
                }

                var enterRequiredParticipant = new SyntheticActivityContentExecutionParticipant(
                    ActivityContentExecutionParticipantDescriptor.Required(
                        RuntimeContentId.From("qa.activity.execution.required"),
                        supportsEnter: true,
                        supportsExit: true,
                        order: 10,
                        displayName: "QA Required Execution Participant",
                        source: QaSource,
                        reason: "qa.activity-content-execution.required"),
                    SyntheticActivityContentExecutionParticipantMode.Success);

                var enterOptionalParticipant = new SyntheticActivityContentExecutionParticipant(
                    ActivityContentExecutionParticipantDescriptor.Optional(
                        RuntimeContentId.From("qa.activity.execution.optional.enter"),
                        supportsEnter: true,
                        supportsExit: false,
                        order: 20,
                        displayName: "QA Optional Enter Execution Participant",
                        source: QaSource,
                        reason: "qa.activity-content-execution.optional-enter"),
                    SyntheticActivityContentExecutionParticipantMode.NoOp);

                var participants = ActivityContentExecutionParticipantCollection.FromParticipants(
                    new IActivityContentExecutionParticipant[]
                    {
                        enterRequiredParticipant,
                        enterOptionalParticipant
                    });

                if (participants.HasIssues || participants.Count != 2 || participants.EnterCount != 2 || participants.ExitCount != 1)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='collection' count='{participants.Count}' enter='{participants.EnterCount}' exit='{participants.ExitCount}' issues='{participants.IssueCount}' details='{FormatValue(participants.ToDiagnosticString())}'.");
                    return false;
                }

                var runtime = new ActivityContentExecutionRuntime();
                var enterPlan = ActivityContentExecutionRequestFactory.CreateEnterPlan(
                    activity,
                    activityFlow.PreviousActivity,
                    runtimeScope.Context,
                    participants,
                    QaSource,
                    "qa.activity-content-execution.enter");

                if (!enterPlan.Planned || enterPlan.RequestCount != 2 || enterPlan.RequiredCount != 1 || enterPlan.OptionalCount != 1)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='enter-plan' status='{enterPlan.Status}' requests='{enterPlan.RequestCount}' required='{enterPlan.RequiredCount}' optional='{enterPlan.OptionalCount}' message='{FormatValue(enterPlan.Message)}'.");
                    return false;
                }

                var enterResult = runtime.ExecutePhasePlan(
                    enterPlan,
                    QaSource,
                    "qa.activity-content-execution.enter");

                if (!enterResult.Succeeded || enterResult.Status != ActivityContentExecutionAggregateStatus.Succeeded || enterResult.ResultCount != 2 || enterResult.BlocksReadiness || enterResult.BlockingIssueCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='enter-execute' status='{enterResult.Status}' results='{enterResult.ResultCount}' blockingIssues='{enterResult.BlockingIssueCount}' nonBlockingIssues='{enterResult.NonBlockingIssueCount}' blocksReadiness='{enterResult.BlocksReadiness}' details='{FormatValue(enterResult.ToDiagnosticString())}'.");
                    return false;
                }

                var exitPlan = ActivityContentExecutionRequestFactory.CreateExitPlan(
                    activity,
                    null,
                    runtimeScope.Context,
                    participants,
                    QaSource,
                    "qa.activity-content-execution.exit");

                if (!exitPlan.Planned || exitPlan.RequestCount != 1 || exitPlan.RequiredCount != 1 || exitPlan.OptionalCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='exit-plan' status='{exitPlan.Status}' requests='{exitPlan.RequestCount}' required='{exitPlan.RequiredCount}' optional='{exitPlan.OptionalCount}' message='{FormatValue(exitPlan.Message)}'.");
                    return false;
                }

                var exitResult = runtime.ExecutePhasePlan(
                    exitPlan,
                    QaSource,
                    "qa.activity-content-execution.exit");

                if (!exitResult.Succeeded || exitResult.Status != ActivityContentExecutionAggregateStatus.Succeeded || exitResult.ResultCount != 1 || exitResult.BlocksReadiness || exitResult.BlockingIssueCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='exit-execute' status='{exitResult.Status}' results='{exitResult.ResultCount}' blockingIssues='{exitResult.BlockingIssueCount}' nonBlockingIssues='{exitResult.NonBlockingIssueCount}' blocksReadiness='{exitResult.BlocksReadiness}' details='{FormatValue(exitResult.ToDiagnosticString())}'.");
                    return false;
                }

                _logger.Info(
                    "QA Activity Content Execution Runtime Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-execution"),
                        LogFields.Field("activity", GetAssetName(activity, "<none>")),
                        LogFields.Field("participants", participants.Count),
                        LogFields.Field("collectionIssues", participants.IssueCount),
                        LogFields.Field("enterPlan", enterPlan.Status.ToString()),
                        LogFields.Field("enterRequests", enterPlan.RequestCount),
                        LogFields.Field("enterStatus", enterResult.Status.ToString()),
                        LogFields.Field("enterResults", enterResult.ResultCount),
                        LogFields.Field("enterRequired", enterResult.RequiredCount),
                        LogFields.Field("enterOptional", enterResult.OptionalCount),
                        LogFields.Field("enterBlockingIssues", enterResult.BlockingIssueCount),
                        LogFields.Field("enterNonBlockingIssues", enterResult.NonBlockingIssueCount),
                        LogFields.Field("enterBlocksReadiness", enterResult.BlocksReadiness),
                        LogFields.Field("exitPlan", exitPlan.Status.ToString()),
                        LogFields.Field("exitRequests", exitPlan.RequestCount),
                        LogFields.Field("exitStatus", exitResult.Status.ToString()),
                        LogFields.Field("exitResults", exitResult.ResultCount),
                        LogFields.Field("exitRequired", exitResult.RequiredCount),
                        LogFields.Field("exitOptional", exitResult.OptionalCount),
                        LogFields.Field("exitBlockingIssues", exitResult.BlockingIssueCount),
                        LogFields.Field("exitNonBlockingIssues", exitResult.NonBlockingIssueCount),
                        LogFields.Field("exitBlocksReadiness", exitResult.BlocksReadiness)));

                _logger.Debug(
                    "QA Activity Content Execution Runtime Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("collection", participants.ToDiagnosticString()),
                        LogFields.Field("enter", enterResult.ToDiagnosticString()),
                        LogFields.Field("exit", exitResult.ToDiagnosticString())));

                return true;
            });
        }

        private async void RunActivityContentExecutionLifecycleTransitionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");

            if (!TryValidateSmokeTargets("Activity Content Execution Lifecycle Transition Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Lifecycle Transition Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-lifecycle.prepare")),
                            allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    _logger.Warning("QA Activity Content Execution Lifecycle Transition Smoke step failed. step='prepare' reason='Primary Activity is not active before clear'.");
                    return false;
                }

                var clearResult = await ClearActivityWithResultCoreAsync(
                    runtimeHost,
                    ResolveReason(clearActivityReason, "qa.activity-content-execution-lifecycle.clear"));
                if (!clearResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='clear' reason='Activity clear did not succeed' status='{FormatValue(clearResult.Kind.ToString())}'.");
                    return false;
                }

                var clearExecution = clearResult.ActivityFlowResult.ActivityContentExecutionResult;
                if (!clearExecution.Executed
                    || !clearExecution.Succeeded
                    || clearExecution.Status != ActivityContentExecutionLifecycleStatus.SucceededNoContent
                    || clearExecution.ExitResult.Status != ActivityContentExecutionAggregateStatus.SkippedNoContent
                    || clearExecution.ExitRequestCount != 0
                    || clearExecution.ExitResultCount != 0
                    || clearExecution.BlocksReadiness)
                {
                    _logger.Warning(
                        $"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='clear-execution' reason='Activity clear execution diagnostics did not match empty exit expectation' execution='{FormatValue(clearExecution.DiagnosticStatus)}' exit='{FormatValue(clearExecution.ExitResult.Status.ToString())}' exitRequests='{clearExecution.ExitRequestCount}' exitResults='{clearExecution.ExitResultCount}' blocksReadiness='{clearExecution.BlocksReadiness}' details='{FormatValue(clearExecution.ToDiagnosticString())}'.");
                    return false;
                }

                await Task.Yield();

                var restoreResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-lifecycle.restore")));
                if (!restoreResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                    return false;
                }

                var restoreExecution = restoreResult.ActivityFlowResult.ActivityContentExecutionResult;
                if (!restoreExecution.Executed
                    || !restoreExecution.Succeeded
                    || restoreExecution.Status != ActivityContentExecutionLifecycleStatus.SucceededNoContent
                    || restoreExecution.EnterResult.Status != ActivityContentExecutionAggregateStatus.SkippedNoContent
                    || restoreExecution.EnterRequestCount != 0
                    || restoreExecution.EnterResultCount != 0
                    || restoreExecution.BlocksReadiness)
                {
                    _logger.Warning(
                        $"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='restore-execution' reason='Activity restore execution diagnostics did not match empty enter expectation' execution='{FormatValue(restoreExecution.DiagnosticStatus)}' enter='{FormatValue(restoreExecution.EnterResult.Status.ToString())}' enterRequests='{restoreExecution.EnterRequestCount}' enterResults='{restoreExecution.EnterResultCount}' blocksReadiness='{restoreExecution.BlocksReadiness}' details='{FormatValue(restoreExecution.ToDiagnosticString())}'.");
                    return false;
                }

                _logger.Info(
                    "QA Activity Content Execution Lifecycle Transition Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "activity-clear-restore"),
                        LogFields.Field("activity", GetAssetName(primaryActivity, "<none>")),
                        LogFields.Field("clearExecution", clearExecution.Status.ToString()),
                        LogFields.Field("clearEnter", clearExecution.EnterResult.Status.ToString()),
                        LogFields.Field("clearEnterRequests", clearExecution.EnterRequestCount),
                        LogFields.Field("clearExit", clearExecution.ExitResult.Status.ToString()),
                        LogFields.Field("clearExitRequests", clearExecution.ExitRequestCount),
                        LogFields.Field("clearExitResults", clearExecution.ExitResultCount),
                        LogFields.Field("clearBlocksReadiness", clearExecution.BlocksReadiness),
                        LogFields.Field("restoreExecution", restoreExecution.Status.ToString()),
                        LogFields.Field("restoreEnter", restoreExecution.EnterResult.Status.ToString()),
                        LogFields.Field("restoreEnterRequests", restoreExecution.EnterRequestCount),
                        LogFields.Field("restoreEnterResults", restoreExecution.EnterResultCount),
                        LogFields.Field("restoreExit", restoreExecution.ExitResult.Status.ToString()),
                        LogFields.Field("restoreExitRequests", restoreExecution.ExitRequestCount),
                        LogFields.Field("restoreBlocksReadiness", restoreExecution.BlocksReadiness),
                        LogFields.Field("runtimeRootCount", runtimeHost.RuntimeContentRuntime.RootCount)));

                _logger.Debug(
                    "QA Activity Content Execution Lifecycle Transition Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("clearExecution", clearExecution.ToDiagnosticString()),
                        LogFields.Field("restoreExecution", restoreExecution.ToDiagnosticString())));

                return true;
            });
        }

        private async void RunActivityContentExecutionParticipantSourceSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");

            if (!TryValidateSmokeTargets("Activity Content Execution Participant Source Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Participant Source Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.prepare")),
                            allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    _logger.Warning("QA Activity Content Execution Participant Source Smoke step failed. step='prepare' reason='Primary Activity is not active before source injection'.");
                    return false;
                }

                var source = new SyntheticActivityContentExecutionParticipantSource(
                    new IActivityContentExecutionParticipant[]
                    {
                        new SyntheticActivityContentExecutionParticipant(
                            ActivityContentExecutionParticipantDescriptor.Required(
                                RuntimeContentId.From("qa.activity.execution.source.required"),
                                supportsEnter: true,
                                supportsExit: true,
                                order: 10,
                                displayName: "QA Source Required Execution Participant",
                                source: QaSource,
                                reason: "qa.activity-content-execution-source.required"),
                            SyntheticActivityContentExecutionParticipantMode.Success),
                        new SyntheticActivityContentExecutionParticipant(
                            ActivityContentExecutionParticipantDescriptor.Optional(
                                RuntimeContentId.From("qa.activity.execution.source.optional.enter"),
                                supportsEnter: true,
                                supportsExit: false,
                                order: 20,
                                displayName: "QA Source Optional Enter Execution Participant",
                                source: QaSource,
                                reason: "qa.activity-content-execution-source.optional-enter"),
                            SyntheticActivityContentExecutionParticipantMode.NoOp)
                    });

                FrameworkActivityRequestResult clearResult = default;
                FrameworkActivityRequestResult restoreResult = default;
                bool shouldRestore = false;

                runtimeHost.SetActivityContentExecutionParticipantSource(source);
                try
                {
                    clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-execution-source.clear"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Execution Participant Source Smoke step failed. step='clear' reason='Activity clear did not succeed' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    shouldRestore = true;
                    var clearExecution = clearResult.ActivityFlowResult.ActivityContentExecutionResult;
                    bool clearValid = clearExecution is { Executed: true, Succeeded: true, Status: ActivityContentExecutionLifecycleStatus.Succeeded }
                        && clearExecution.ParticipantSourceStatus == ActivityContentExecutionParticipantSourceStatus.Succeeded.ToString()
                        && clearExecution is { ParticipantCount: 2, ExitResult: { Status: ActivityContentExecutionAggregateStatus.Succeeded, RequiredCount: 1, OptionalCount: 0, BlockingIssueCount: 0 }, ExitRequestCount: 1, ExitResultCount: 1, BlocksReadiness: false };

                    if (!clearValid)
                    {
                        _logger.Warning(
                            $"QA Activity Content Execution Participant Source Smoke step failed. step='clear-execution' reason='Activity clear did not execute sourced exit participant as expected' execution='{FormatValue(clearExecution.DiagnosticStatus)}' source='{FormatValue(clearExecution.ParticipantSourceStatus)}' participants='{clearExecution.ParticipantCount}' exit='{FormatValue(clearExecution.ExitResult.Status.ToString())}' exitRequests='{clearExecution.ExitRequestCount}' exitResults='{clearExecution.ExitResultCount}' blockingIssues='{clearExecution.ExitResult.BlockingIssueCount}' blocksReadiness='{clearExecution.BlocksReadiness}' details='{FormatValue(clearExecution.ToDiagnosticString())}'.");
                    }

                    await Task.Yield();

                    restoreResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.restore")));
                    shouldRestore = false;
                    if (!restoreResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Execution Participant Source Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                        return false;
                    }

                    var restoreExecution = restoreResult.ActivityFlowResult.ActivityContentExecutionResult;
                    bool restoreValid = restoreExecution is { Executed: true, Succeeded: true, Status: ActivityContentExecutionLifecycleStatus.Succeeded }
                        && restoreExecution.ParticipantSourceStatus == ActivityContentExecutionParticipantSourceStatus.Succeeded.ToString()
                        && restoreExecution is { ParticipantCount: 2, EnterResult: { Status: ActivityContentExecutionAggregateStatus.Succeeded, RequiredCount: 1, OptionalCount: 1, BlockingIssueCount: 0 }, EnterRequestCount: 2, EnterResultCount: 2, BlocksReadiness: false };

                    if (!restoreValid)
                    {
                        _logger.Warning(
                            $"QA Activity Content Execution Participant Source Smoke step failed. step='restore-execution' reason='Activity restore did not execute sourced enter participants as expected' execution='{FormatValue(restoreExecution.DiagnosticStatus)}' source='{FormatValue(restoreExecution.ParticipantSourceStatus)}' participants='{restoreExecution.ParticipantCount}' enter='{FormatValue(restoreExecution.EnterResult.Status.ToString())}' enterRequests='{restoreExecution.EnterRequestCount}' enterResults='{restoreExecution.EnterResultCount}' blockingIssues='{restoreExecution.EnterResult.BlockingIssueCount}' blocksReadiness='{restoreExecution.BlocksReadiness}' details='{FormatValue(restoreExecution.ToDiagnosticString())}'.");
                    }

                    if (!clearValid || !restoreValid)
                    {
                        return false;
                    }

                    _logger.Info(
                        "QA Activity Content Execution Participant Source Smoke step completed.",
                        LogFields.Of(
                            LogFields.Field("step", "explicit-source-lifecycle"),
                            LogFields.Field("activity", GetAssetName(primaryActivity, "<none>")),
                            LogFields.Field("sourceResolutions", source.ResolveCount),
                            LogFields.Field("clearExecution", clearExecution.Status.ToString()),
                            LogFields.Field("clearSource", clearExecution.ParticipantSourceStatus),
                            LogFields.Field("clearParticipants", clearExecution.ParticipantCount),
                            LogFields.Field("clearExit", clearExecution.ExitResult.Status.ToString()),
                            LogFields.Field("clearExitRequests", clearExecution.ExitRequestCount),
                            LogFields.Field("clearExitResults", clearExecution.ExitResultCount),
                            LogFields.Field("clearExitRequired", clearExecution.ExitResult.RequiredCount),
                            LogFields.Field("clearBlockingIssues", clearExecution.ExitResult.BlockingIssueCount),
                            LogFields.Field("clearBlocksReadiness", clearExecution.BlocksReadiness),
                            LogFields.Field("restoreExecution", restoreExecution.Status.ToString()),
                            LogFields.Field("restoreSource", restoreExecution.ParticipantSourceStatus),
                            LogFields.Field("restoreParticipants", restoreExecution.ParticipantCount),
                            LogFields.Field("restoreEnter", restoreExecution.EnterResult.Status.ToString()),
                            LogFields.Field("restoreEnterRequests", restoreExecution.EnterRequestCount),
                            LogFields.Field("restoreEnterResults", restoreExecution.EnterResultCount),
                            LogFields.Field("restoreEnterRequired", restoreExecution.EnterResult.RequiredCount),
                            LogFields.Field("restoreEnterOptional", restoreExecution.EnterResult.OptionalCount),
                            LogFields.Field("restoreBlockingIssues", restoreExecution.EnterResult.BlockingIssueCount),
                            LogFields.Field("restoreBlocksReadiness", restoreExecution.BlocksReadiness),
                            LogFields.Field("runtimeRootCount", runtimeHost.RuntimeContentRuntime.RootCount)));

                    _logger.Debug(
                        "QA Activity Content Execution Participant Source Smoke diagnostics.",
                        LogFields.Of(
                            LogFields.Field("clearExecution", clearExecution.ToDiagnosticString()),
                            LogFields.Field("restoreExecution", restoreExecution.ToDiagnosticString())));

                    return true;
                }
                finally
                {
                    runtimeHost.SetActivityContentExecutionParticipantSource(EmptyActivityContentExecutionParticipantSource.Instance);
                    if (shouldRestore && !ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                    {
                        await RequestActivityWithResultCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.restore-after-failure")));
                    }
                }
            });
        }

        private async void RunActivityContentAnchorBindingSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity / Activity Anchor Binding Owner");

            if (!TryValidateSmokeTargets("Activity Content Anchor Binding Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Binding Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Binding Smoke step failed. step='prepare' reason='Activity Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor-binding.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var routeResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor-binding.route")));
                if (!routeResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='route' reason='Route request did not succeed' status='{FormatValue(routeResult.Kind.ToString())}'.");
                    return false;
                }

                GameObject fixtureObject = null;
                try
                {
                    if (!TryCreatePositiveActivityContentAnchorFixture(primaryActivity, out fixtureObject, out _))
                    {
                        return false;
                    }

                    var clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-binding.clear-before-discovery"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='clear-before-discovery' reason='Activity clear did not succeed before binding discovery' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();

                    var activityResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-binding.activity")));

                    if (!TryCreateActivityContentAnchorBindingForCleanupSmoke(
                            activityResult,
                            runtimeHost,
                            out var context,
                            out var materializationIdentity,
                            out var bindingResult,
                            out var idempotentBindingResult,
                            out int bindingCountBefore))
                    {
                        return false;
                    }

                    var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                    var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                        context,
                        materializationIdentity,
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        QaSource,
                        "qa.activity-content-anchor-binding.release-before-activity-exit");
                    if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='release' reason='Runtime handle release before Activity exit failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                        return false;
                    }

                    int boundCount = runtimeHost.ContentAnchorBindingCount;
                    if (boundCount <= bindingCountBefore)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bound' reason='Binding count did not increase before Activity exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{boundCount}'.");
                        return false;
                    }

                    var exitResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-binding.exit"));
                    if (!exitResult.Succeeded)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='activity-exit' reason='Activity clear did not succeed' status='{FormatValue(exitResult.Kind.ToString())}'.");
                        return false;
                    }

                    var cleanupResult = exitResult.ActivityFlowResult.ActivityContentAnchorBindingCleanupResult;
                    if (!cleanupResult.Executed || !cleanupResult.Succeeded || cleanupResult.RemovedCount < 1)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='cleanup' reason='Activity binding cleanup did not remove the test binding' cleanup='{cleanupResult.DiagnosticStatus}' removed='{cleanupResult.RemovedCount}' before='{cleanupResult.BindingCountBefore}' after='{cleanupResult.BindingCountAfter}'.");
                        return false;
                    }

                    if (runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='cleanup' reason='Binding count did not return to baseline after Activity exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                        return false;
                    }

                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                        fixtureObject = null;
                    }

                    var restoreResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-binding.restore")));
                    if (!restoreResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                        return false;
                    }

                    _logger.Info(
                        "QA Activity Content Anchor Binding Smoke step completed.",
                        LogFields.Of(
                            LogFields.Field("step", "activity-anchor-binding"),
                            LogFields.Field("activity", GetAssetName(primaryActivity, "<unnamed>")),
                            LogFields.Field("anchor", bindingResult.Anchor.StableText),
                            LogFields.Field("binding", bindingResult.Status.ToString()),
                            LogFields.Field("idempotentBinding", idempotentBindingResult.Status.ToString()),
                            LogFields.Field("release", releaseResult.Status.ToString()),
                            LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                            LogFields.Field("activityCleanup", cleanupResult.DiagnosticStatus),
                            LogFields.Field("activityCleanupRemoved", cleanupResult.RemovedCount),
                            LogFields.Field("activityCleanupBefore", cleanupResult.BindingCountBefore),
                            LogFields.Field("activityCleanupAfter", cleanupResult.BindingCountAfter),
                            LogFields.Field("bindingCountBefore", bindingCountBefore),
                            LogFields.Field("bindingCountBound", boundCount),
                            LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
                    _logger.Debug(
                        "QA Activity Content Anchor Binding Smoke diagnostics.",
                        LogFields.Field("details", bindingResult.ToDiagnosticString()));
                    return true;
                }
                finally
                {
                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                    }
                }
            });
        }

        private async void RunContentAnchorBindingSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Content Anchor Binding Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Binding Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Binding Smoke step failed. step='prepare' reason='Content Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor-binding")));
                return ValidateContentAnchorBindingSmokeStep(anchorResult, "binding", runtimeHost);
            });
        }

        private async void RunContentAnchorBindingCleanupSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Cleanup Exit Route");

            if (!TryValidateSmokeTargets("Content Anchor Binding Cleanup Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Binding Cleanup Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Binding Cleanup Smoke step failed. step='prepare' reason='Content Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding-cleanup.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor-binding-cleanup.bind")));

                if (!TryCreatePersistentContentAnchorBindingForCleanupSmoke(
                        anchorResult,
                        runtimeHost,
                        out var context,
                        out var materializationIdentity,
                        out var bindingResult,
                        out int bindingCountBefore))
                {
                    return false;
                }

                var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationIdentity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.release-before-route-exit");
                if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='release' reason='Runtime handle release before route exit failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                    return false;
                }

                int boundCount = runtimeHost.ContentAnchorBindingCount;
                if (boundCount <= bindingCountBefore)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bound' reason='Binding count did not increase before Route exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{boundCount}'.");
                    return false;
                }

                var exitResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    alternateRoute,
                    ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding-cleanup.exit")));
                if (!exitResult.Succeeded)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='route-exit' reason='Exit Route request did not succeed' status='{FormatValue(exitResult.Kind.ToString())}'.");
                    return false;
                }

                var cleanupResult = exitResult.RouteLifecycleResult.RouteContentAnchorBindingCleanupResult;
                if (!cleanupResult.Executed || !cleanupResult.Succeeded || cleanupResult.RemovedCount < 1)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='cleanup' reason='Route binding cleanup did not remove the test binding' cleanup='{cleanupResult.DiagnosticStatus}' removed='{cleanupResult.RemovedCount}' before='{cleanupResult.BindingCountBefore}' after='{cleanupResult.BindingCountAfter}'.");
                    return false;
                }

                if (runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
                {
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='cleanup' reason='Binding count did not return to baseline after Route exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                    return false;
                }

                _logger.Info(
                    "QA Content Anchor Binding Cleanup Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "route-exit-cleanup"),
                        LogFields.Field("binding", bindingResult.Status.ToString()),
                        LogFields.Field("release", releaseResult.Status.ToString()),
                        LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                        LogFields.Field("routeCleanup", cleanupResult.DiagnosticStatus),
                        LogFields.Field("routeCleanupRemoved", cleanupResult.RemovedCount),
                        LogFields.Field("routeCleanupBefore", cleanupResult.BindingCountBefore),
                        LogFields.Field("routeCleanupAfter", cleanupResult.BindingCountAfter),
                        LogFields.Field("bindingCountBefore", bindingCountBefore),
                        LogFields.Field("bindingCountBound", boundCount),
                        LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
                return true;
            });
        }

private async void RunNoActivityRouteSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, noActivityRoute == null, "No-Activity Route");
            if (!TryValidateSmokeTargets("No-Activity Route Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("No-Activity Route Smoke", runtimeHost =>
                RequestRouteCoreAsync(runtimeHost, noActivityRoute, ResolveReason(routeReason, GetAssetName(noActivityRoute, "QA No Activity Route"))));
        }

        private async void RunNoContentActivitySmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, noContentActivity == null, "No-Content Activity");
            if (!TryValidateSmokeTargets("No-Content Activity Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("No-Content Activity Smoke", runtimeHost =>
                RequestActivityCoreAsync(runtimeHost, noContentActivity, ResolveReason(activityReason, GetAssetName(noContentActivity, "QA No Content Activity"))));
        }

        private async void RunNegativeSmoke()
        {
            await RunSmokeAsync("Negative Smoke", async runtimeHost =>
            {
                if (!await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"), allowNoActiveActivity: true))
                {
                    return false;
                }

                await Task.Yield();
                return await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"), requireNoActiveActivity: true);
            });
        }

        private async Task RunSmokeAsync(string smokeName, Func<FrameworkRuntimeHost, Task<bool>> sequence)
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Smoke ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Smoke failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            _logger.Info($"QA Smoke started. name='{smokeName}'.");
            try
            {
                bool completed = await sequence(runtimeHost);
                if (completed)
                {
                    _logger.Info($"QA Smoke completed. name='{smokeName}'.");
                }
                else
                {
                    _logger.Warning($"QA Smoke aborted. name='{smokeName}'. reason='Step failed'.");
                }
            }
            catch (Exception exception)
            {
                _logger.Error($"QA Smoke failed. name='{smokeName}'. reason='{FormatValue(exception.Message)}'.");
            }
            finally
            {
                EndOperation();
            }
        }

        private async Task<bool> RequestRouteCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            RouteAsset route,
            string reason,
            bool allowAlreadyActive = false)
        {
            var result = await RequestRouteWithResultCoreAsync(runtimeHost, route, reason);
            return result.Succeeded || allowAlreadyActive && result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive;
        }

        private async Task<FrameworkRouteRequestResult> RequestRouteWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            RouteAsset route,
            string reason)
        {
            if (route == null)
            {
                _logger.Error("QA Route Request failed. Target Route is missing.");
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    "QA Route Request failed. Target Route is missing.",
                    route,
                    QaSource,
                    reason);
            }

            return await runtimeHost.RequestRouteAsync(route, QaSource, reason);
        }

        private bool ValidateRouteCallbackSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var enterResult = lifecycleResult.RouteContentEnterResult;
            var exitResult = lifecycleResult.RouteContentExitResult;
            string enterRouteName = GetAssetName(enterResult.Route, routeName);
            string exitRouteName = GetAssetName(exitResult.Route, "<none>");

            if (!IsValidRouteCallbackDispatch(enterResult))
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' phase='Enter' callbackRoute='{FormatValue(enterRouteName)}' executed='{enterResult.Executed}' bindings='{enterResult.BindingCount}' receivers='{enterResult.ReceiverCount}' failed='{enterResult.FailedReceiverCount}'.");
                return false;
            }

            if (!IsValidRouteCallbackDispatch(exitResult))
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' phase='Exit' callbackRoute='{FormatValue(exitRouteName)}' executed='{exitResult.Executed}' bindings='{exitResult.BindingCount}' receivers='{exitResult.ReceiverCount}' failed='{exitResult.FailedReceiverCount}'.");
                return false;
            }

            _logger.Info($"QA Route Callback Smoke step completed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' routeContentEnterRoute='{FormatValue(enterRouteName)}' routeContentEnterReceivers='{enterResult.ReceiverCount}' routeContentExitRoute='{FormatValue(exitRouteName)}' routeContentExitReceivers='{exitResult.ReceiverCount}'.");
            return true;
        }

        private bool ValidateActivityBaselineStartedStep(
            FrameworkActivityRequestResult result,
            string stepName,
            ActivityAsset expectedActivity,
            bool requireActivityContent)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string expectedActivityName = GetAssetName(expectedActivity, "<missing>");

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlowResult = result.ActivityFlowResult;
            if (!activityFlowResult.Started)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity flow did not start'.");
                return false;
            }

            if (!ReferenceEquals(activityFlowResult.Activity, expectedActivity))
            {
                string actualActivityName = activityFlowResult.Activity != null
                    ? GetAssetName(activityFlowResult.Activity, "<unnamed>")
                    : "<none>";
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' expectedActivity='{FormatValue(expectedActivityName)}' actualActivity='{FormatValue(actualActivityName)}' reason='Unexpected active Activity'.");
                return false;
            }

            var readinessState = activityFlowResult.ActivityReadinessState;
            if (!readinessState.IsReady || readinessState.HasBlockingIssues)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}'.");
                return false;
            }

            var activityContentResult = activityFlowResult.ActivityContentResult;
            if (activityContentResult.HasLifecycleFailures)
            {
                var lifecycleResult = activityContentResult.LifecycleResult;
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity Content lifecycle failure' activityContentEnterFailed='{lifecycleResult.EnterFailedReceiverCount}' activityContentExitFailed='{lifecycleResult.ExitFailedReceiverCount}'.");
                return false;
            }

            if (requireActivityContent && !activityFlowResult.HasActivityContent)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Expected Activity Content Set handle was not registered' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}'.");
                return false;
            }

            var lifecycle = activityContentResult.LifecycleResult;
            _logger.Info($"QA Activity Baseline Smoke step completed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}' activityContentLifecycle='{FormatValue(lifecycle.DiagnosticStatus)}' activityContentEnterFailed='{lifecycle.EnterFailedReceiverCount}' activityContentExitFailed='{lifecycle.ExitFailedReceiverCount}'.");
            return true;
        }

        private bool ValidateActivityBaselineClearedStep(FrameworkActivityRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Clear Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlowResult = result.ActivityFlowResult;
            if (!activityFlowResult.Cleared || !activityFlowResult.ActivityState.IsNone)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Activity flow did not clear to None' activityState='{FormatValue(activityFlowResult.ActivityState.DiagnosticStatus)}'.");
                return false;
            }

            var readinessState = activityFlowResult.ActivityReadinessState;
            if (!readinessState.IsNone || readinessState.HasBlockingIssues)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' reason='Expected no active Activity readiness'.");
                return false;
            }

            _logger.Info($"QA Activity Baseline Smoke step completed. step='{FormatValue(normalizedStepName)}' activityState='{FormatValue(activityFlowResult.ActivityState.DiagnosticStatus)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}'.");
            return true;
        }

        private bool ValidateRouteSceneCompositionSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var composition = lifecycleResult.RouteSceneCompositionResult;
            if (!composition.Succeeded || composition.HasBlockingIssues)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route scene composition did not succeed' status='{FormatValue(composition.Status.ToString())}' loaded='{composition.LoadedCount}' failed='{composition.FailedCount}' blockingIssues='{composition.BlockingIssueCount}'.");
                return false;
            }

            int expectedLoaded = Math.Max(1, expectedRouteSceneLoadedCount);
            if (composition.LoadedCount < expectedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Loaded scene count below expectation' expectedLoaded='{expectedLoaded}' actualLoaded='{composition.LoadedCount}' failed='{composition.FailedCount}' blockingIssues='{composition.BlockingIssueCount}'.");
                return false;
            }

            int expectedOwnedLoaded = Math.Max(1, expectedRouteSceneOwnedLoadedCount);
            if (composition.OwnedLoadedCount < expectedOwnedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Owned loaded scene count below expectation' expectedOwnedLoaded='{expectedOwnedLoaded}' actualOwnedLoaded='{composition.OwnedLoadedCount}' loaded='{composition.LoadedCount}'.");
                return false;
            }

            var contentSet = lifecycleResult.RouteContentSet;
            if (contentSet.Count < expectedOwnedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route Content Set handle count below expectation' expectedHandles='{expectedOwnedLoaded}' actualHandles='{contentSet.Count}' loaded='{composition.LoadedCount}' ownedLoaded='{composition.OwnedLoadedCount}'.");
                return false;
            }

            _logger.Info(
                "QA Route Scene Composition Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("routeSceneComposition", composition.Status.ToString()),
                    LogFields.Field("routeSceneEntries", composition.EntryCount),
                    LogFields.Field("routeSceneLoaded", composition.LoadedCount),
                    LogFields.Field("routeSceneOwnedLoaded", composition.OwnedLoadedCount),
                    LogFields.Field("routeSceneAlreadyLoaded", composition.AlreadyLoadedCount),
                    LogFields.Field("routeSceneFailed", composition.FailedCount),
                    LogFields.Field("routeSceneIssues", composition.IssueCount),
                    LogFields.Field("routeSceneBlockingIssues", composition.BlockingIssueCount),
                    LogFields.Field("routeContentHandles", contentSet.Count),
                    LogFields.Field("routeContentOwned", contentSet.OwnedCount),
                    LogFields.Field("routeContentDiagnosticOnly", contentSet.DiagnosticOnlyCount)));
            _logger.Debug(
                "QA Route Scene Composition Smoke diagnostics.",
                LogFields.Field("details", composition.ToDiagnosticString()));
            return true;
        }

        private bool ValidateRouteReleaseSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var release = lifecycleResult.ContentReleaseResult;
            if (release.Failed || release.HasBlockingIssues)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route content release failed' status='{FormatValue(release.Status.ToString())}' released='{release.ReleasedCount}' skipped='{release.SkippedCount}' failed='{release.FailedCount}' blockingIssues='{release.BlockingIssueCount}'.");
                return false;
            }

            int expectedReleased = Math.Max(0, expectedRouteReleaseReleasedCount);
            if (release.ReleasedCount < expectedReleased)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Released count below expectation' expectedReleased='{expectedReleased}' actualReleased='{release.ReleasedCount}' skipped='{release.SkippedCount}' failed='{release.FailedCount}'.");
                return false;
            }

            _logger.Info(
                "QA Route Release Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("routeRelease", release.Status.ToString()),
                    LogFields.Field("routeReleasePlanned", release.PlannedCount),
                    LogFields.Field("routeReleaseReleased", release.ReleasedCount),
                    LogFields.Field("routeReleaseSkipped", release.SkippedCount),
                    LogFields.Field("routeReleaseFailed", release.FailedCount),
                    LogFields.Field("routeReleaseIssues", release.IssueCount),
                    LogFields.Field("routeReleaseBlockingIssues", release.BlockingIssueCount),
                    LogFields.Field("routeSceneComposition", lifecycleResult.RouteSceneCompositionResult.Status.ToString()),
                    LogFields.Field("routeSceneLoaded", lifecycleResult.RouteSceneCompositionResult.LoadedCount),
                    LogFields.Field("routeContentHandles", lifecycleResult.RouteContentSet.Count)));
            _logger.Debug(
                "QA Route Release Smoke diagnostics.",
                LogFields.Field("details", release.ToDiagnosticString()));
            return true;
        }

        private bool TryCreatePersistentContentAnchorBindingForCleanupSmoke(
            FrameworkRouteRequestResult result,
            FrameworkRuntimeHost runtimeHost,
            out RuntimeScopeContext context,
            out RuntimeContentIdentity materializationIdentity,
            out ContentAnchorBindingResult bindingResult,
            out int bindingCountBefore)
        {
            context = default(RuntimeScopeContext);
            materializationIdentity = default(RuntimeContentIdentity);
            bindingResult = default(ContentAnchorBindingResult);
            bindingCountBefore = 0;

            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            if (!result.RouteLifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var anchorSet = result.RouteLifecycleResult.ContentAnchorSet;
            if (!anchorSet.HasAnchors)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = result.RouteLifecycleResult.RuntimeRouteScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route runtime scope context unavailable' runtimeRouteScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeRouteContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.content-anchor.binding-cleanup." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ContentAnchorBindingCleanup",
                "qa.content-anchor.binding-cleanup.resource",
                "QA Content Anchor Binding Cleanup Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.content-anchor-binding-cleanup.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.content-anchor-binding-cleanup.materialization.synthetic-result",
                "Synthetic Content Anchor binding cleanup smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.content-anchor-binding-cleanup.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.content-anchor-binding-cleanup.request");
            bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding-cleanup.bind");

            if (!bindingResult.Succeeded)
            {
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.cleanup.binding-failed");
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                return false;
            }

            materializationIdentity = materializationRequest.Identity;
            return true;
        }

        private bool ValidateContentAnchorBindingSmokeStep(
            FrameworkRouteRequestResult result,
            string stepName,
            FrameworkRuntimeHost runtimeHost)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var anchorSet = lifecycleResult.ContentAnchorSet;
            if (!anchorSet.HasAnchors || anchorSet.Declarations.Count == 0)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = lifecycleResult.RuntimeRouteScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route runtime scope context unavailable' runtimeRouteScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeRouteContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            var context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.content-anchor.binding." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ContentAnchorBinding",
                "qa.content-anchor.binding.resource",
                "QA Content Anchor Binding Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-binding.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.content-anchor-binding.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.content-anchor-binding.materialization.synthetic-result",
                "Synthetic Content Anchor binding smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.content-anchor-binding.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.content-anchor-binding.request");
            int bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            var bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding.bind");

            if (!bindingResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.binding-failed");
                return false;
            }

            var idempotentBindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding.bind.idempotent");

            if (idempotentBindingResult.Status != ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Idempotent binding did not return already-bound' binding='{idempotentBindingResult.Status}' message='{FormatValue(idempotentBindingResult.Message)}'.");
                runtimeHost.UnbindContentAnchor(bindingRequest);
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.idempotent-failed");
                return false;
            }

            bool unbound = runtimeHost.UnbindContentAnchor(bindingRequest);
            if (!unbound || runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Logical binding was not removed from host runtime' unbound='{unbound}' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.unbind-failed");
                return false;
            }

            var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                context,
                materializationRequest.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.content-anchor-binding.release.logical");

            if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Runtime handle release failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                return false;
            }

            if (runtimeContentRuntime.TryGetHandle(context, materializationRequest.Identity, out _))
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Runtime handle still registered after release/unregister'.");
                return false;
            }

            _logger.Info(
                "QA Content Anchor Binding Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("anchor", anchor.StableText),
                    LogFields.Field("binding", bindingResult.Status.ToString()),
                    LogFields.Field("idempotentBinding", idempotentBindingResult.Status.ToString()),
                    LogFields.Field("unbound", unbound),
                    LogFields.Field("release", releaseResult.Status.ToString()),
                    LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                    LogFields.Field("runtimeRootCount", runtimeContentRuntime.RootCount),
                    LogFields.Field("bindingCountBefore", bindingCountBefore),
                    LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
            _logger.Debug(
                "QA Content Anchor Binding Smoke diagnostics.",
                LogFields.Field("details", bindingResult.ToDiagnosticString()));
            return true;
        }

        private bool TryCreateActivityContentAnchorBindingForCleanupSmoke(
            FrameworkActivityRequestResult result,
            FrameworkRuntimeHost runtimeHost,
            out RuntimeScopeContext context,
            out RuntimeContentIdentity materializationIdentity,
            out ContentAnchorBindingResult bindingResult,
            out ContentAnchorBindingResult idempotentBindingResult,
            out int bindingCountBefore)
        {
            context = default(RuntimeScopeContext);
            materializationIdentity = default(RuntimeContentIdentity);
            bindingResult = default(ContentAnchorBindingResult);
            idempotentBindingResult = default(ContentAnchorBindingResult);
            bindingCountBefore = 0;

            string activityName = result.TargetActivity != null ? GetAssetName(result.TargetActivity, "<unnamed>") : "<missing>";

            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity flow did not complete'.");
                return false;
            }

            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (!anchorSet.HasAnchors || anchorSet.Declarations.Count == 0)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = activityFlow.RuntimeActivityScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity runtime scope context unavailable' runtimeActivityScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeActivityContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            if (discovery.IssueCount != 0 || discovery.InvalidAuthoringCount != 0 || discovery.SkippedActivityMismatchCount != 0)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity Content Anchor discovery produced issues' issues='{discovery.IssueCount}' invalid='{discovery.InvalidAuthoringCount}' mismatch='{discovery.SkippedActivityMismatchCount}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.activity-content-anchor.binding." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ActivityContentAnchorBinding",
                "qa.activity-content-anchor.binding.resource",
                "QA Activity Content Anchor Binding Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.activity-content-anchor-binding.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.activity-content-anchor-binding.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.activity-content-anchor-binding.materialization.synthetic-result",
                "Synthetic Activity Content Anchor binding smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.activity-content-anchor-binding.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.activity-content-anchor-binding.request");
            bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.activity-content-anchor-binding.bind");

            if (!bindingResult.Succeeded)
            {
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.activity-content-anchor-binding.cleanup.binding-failed");
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                return false;
            }

            idempotentBindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.activity-content-anchor-binding.bind.idempotent");

            if (idempotentBindingResult.Status != ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                runtimeHost.UnbindContentAnchor(bindingRequest);
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.activity-content-anchor-binding.cleanup.idempotent-failed");
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Idempotent binding did not return already-bound' binding='{idempotentBindingResult.Status}' message='{FormatValue(idempotentBindingResult.Message)}'.");
                return false;
            }

            materializationIdentity = materializationRequest.Identity;
            return true;
        }

        private bool TryCreatePositiveActivityContentAnchorFixture(
            ActivityAsset activity,
            out GameObject fixtureObject,
            out ActivityContentAnchor fixtureAnchor)
        {
            fixtureObject = null;
            fixtureAnchor = null;

            if (activity == null)
            {
                _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='fixture' reason='Primary Activity is missing'.");
                return false;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='fixture' reason='Active Scene is invalid or not loaded'.");
                return false;
            }

            string anchorId = ResolveReason(positiveActivityContentAnchorId, "qa.activity.anchor.positive");
            var anchorKind = positiveActivityContentAnchorKind == ContentAnchorKind.Unknown
                ? ContentAnchorKind.Slot
                : positiveActivityContentAnchorKind;
            var requiredness = Enum.IsDefined(typeof(ContentAnchorRequiredness), positiveActivityContentAnchorRequiredness)
                ? positiveActivityContentAnchorRequiredness
                : ContentAnchorRequiredness.Optional;

            fixtureObject = new GameObject("QA Activity Content Anchor Positive Fixture");
            SceneManager.MoveGameObjectToScene(fixtureObject, activeScene);
            fixtureAnchor = fixtureObject.AddComponent<ActivityContentAnchor>();
            fixtureAnchor.ConfigureForDiagnostics(
                activity,
                anchorId,
                anchorKind,
                requiredness,
                "QA Activity Content Anchor Positive Fixture",
                "Temporary QA fixture created by FrameworkQaCanvas for F9H positive-path discovery smoke.");
            return true;
        }

        private bool ValidateActivityContentAnchorPositiveSmokeStep(
            FrameworkActivityRequestResult result,
            string stepName,
            ActivityContentAnchor fixtureAnchor)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string activityName = result.TargetActivity != null ? GetAssetName(result.TargetActivity, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Activity flow did not complete'.");
                return false;
            }

            if (fixtureAnchor == null || !fixtureAnchor.TryCreateDeclaration(out var declaration))
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive fixture declaration is invalid'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (discovery.AnchorCount < 1 || discovery.CandidateCount < 1 || discovery.AcceptedCount < 1)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive Activity Content Anchor was not discovered' anchors='{discovery.AnchorCount}' candidates='{discovery.CandidateCount}' accepted='{discovery.AcceptedCount}'.");
                return false;
            }

            if (!anchorSet.Contains(declaration))
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Discovered Activity Content Anchor Set does not contain the positive fixture declaration' anchor='{FormatValue(declaration.StableText)}'.");
                return false;
            }

            if (discovery.IssueCount != 0 || discovery.InvalidAuthoringCount != 0 || discovery.SkippedActivityMismatchCount != 0)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive discovery produced issues' issues='{discovery.IssueCount}' invalid='{discovery.InvalidAuthoringCount}' mismatch='{discovery.SkippedActivityMismatchCount}'.");
                return false;
            }

            _logger.Info(
                "QA Activity Content Anchor Positive Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("activity", activityName),
                    LogFields.Field("anchor", declaration.StableText),
                    LogFields.Field("activityContentAnchors", discovery.AnchorCount),
                    LogFields.Field("activityContentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("activityContentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("activityContentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("activityContentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("activityContentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("activityContentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("activityContentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("activityContentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("activityContentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("activityContentAnchorActivityMismatch", discovery.SkippedActivityMismatchCount)));
            _logger.Debug(
                "QA Activity Content Anchor Positive Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateActivityContentAnchorDiagnosticsSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.RouteLifecycleResult.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Activity flow did not complete'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (!ValidateMinimum("activityContentAnchors", Math.Max(0, expectedActivityContentAnchorCount), discovery.AnchorCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateMinimum("activityContentAnchorCandidates", Math.Max(0, expectedActivityContentAnchorCandidateCount), discovery.CandidateCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorIssues", Math.Max(0, expectedActivityContentAnchorIssueCount), discovery.IssueCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorInvalid", Math.Max(0, expectedActivityContentAnchorInvalidCount), discovery.InvalidAuthoringCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorActivityMismatch", Math.Max(0, expectedActivityContentAnchorActivityMismatchCount), discovery.SkippedActivityMismatchCount, normalizedStepName, routeName))
            {
                return false;
            }

            _logger.Info(
                "QA Activity Content Anchor Diagnostics Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("activity", GetAssetName(activityFlow.Activity, "<none>")),
                    LogFields.Field("activityContentAnchors", discovery.AnchorCount),
                    LogFields.Field("activityContentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("activityContentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("activityContentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("activityContentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("activityContentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("activityContentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("activityContentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("activityContentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("activityContentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("activityContentAnchorActivityMismatch", discovery.SkippedActivityMismatchCount),
                    LogFields.Field("activityContentAnchorDuplicateIdentity", anchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("activityContentAnchorDuplicateId", anchorSet.DuplicateAnchorIdIssueCount)));
            _logger.Debug(
                "QA Activity Content Anchor Diagnostics Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateContentAnchorDiagnosticsSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var discovery = lifecycleResult.ContentAnchorDiscoveryResult;
            var anchorSet = lifecycleResult.ContentAnchorSet;
            if (!ValidateMinimum("contentAnchors", Math.Max(0, expectedContentAnchorCount), discovery.AnchorCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateMinimum("contentAnchorCandidates", Math.Max(0, expectedContentAnchorCandidateCount), discovery.CandidateCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorIssues", Math.Max(0, expectedContentAnchorIssueCount), discovery.IssueCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorInvalid", Math.Max(0, expectedContentAnchorInvalidCount), discovery.InvalidAuthoringCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorRouteMismatch", Math.Max(0, expectedContentAnchorRouteMismatchCount), discovery.SkippedRouteMismatchCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorRequired", expectedContentAnchorRequiredCount, anchorSet.RequiredCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorOptional", expectedContentAnchorOptionalCount, anchorSet.OptionalCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorRoot", expectedContentAnchorRootCount, anchorSet.RootCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorSlot", expectedContentAnchorSlotCount, anchorSet.SlotCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorPoint", expectedContentAnchorPointCount, anchorSet.PointCount, normalizedStepName, routeName))
            {
                return false;
            }

            _logger.Info(
                "QA Content Anchor Diagnostics Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("contentAnchors", discovery.AnchorCount),
                    LogFields.Field("contentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("contentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("contentAnchorRoute", anchorSet.RouteCount),
                    LogFields.Field("contentAnchorActivity", anchorSet.ActivityCount),
                    LogFields.Field("contentAnchorLocal", anchorSet.LocalCount),
                    LogFields.Field("contentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("contentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("contentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("contentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("contentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("contentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("contentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("contentAnchorRouteMismatch", discovery.SkippedRouteMismatchCount),
                    LogFields.Field("contentAnchorDuplicateIdentity", anchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("contentAnchorDuplicateId", anchorSet.DuplicateAnchorIdIssueCount)));
            _logger.Debug(
                "QA Content Anchor Diagnostics Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateMinimum(string metricName, int expectedMinimum, int actual, string stepName, string routeName)
        {
            if (actual >= expectedMinimum)
            {
                return true;
            }

            _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(stepName)}' route='{FormatValue(routeName)}' metric='{FormatValue(metricName)}' reason='Metric below expectation' expectedMinimum='{expectedMinimum}' actual='{actual}'.");
            return false;
        }

        private bool ValidateExact(string metricName, int expected, int actual, string stepName, string routeName)
        {
            if (actual == expected)
            {
                return true;
            }

            _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(stepName)}' route='{FormatValue(routeName)}' metric='{FormatValue(metricName)}' reason='Metric did not match expectation' expected='{expected}' actual='{actual}'.");
            return false;
        }

        private bool ValidateOptionalExact(string metricName, int expected, int actual, string stepName, string routeName)
        {
            return expected < 0 || ValidateExact(metricName, expected, actual, stepName, routeName);
        }

        private static bool IsValidRouteCallbackDispatch(RouteContentLifecycleDispatchResult result)
        {
            return result is { Executed: true, BindingCount: > 0, ReceiverCount: > 0, FailedReceiverCount: 0 };
        }


        private void ValidateLoadedRouteContentAuthoring()
        {
            ValidateLoadedLocalContributionsAuthoring();
        }

        private void ValidateLoadedLocalContributionsAuthoring()
        {
            EnsureLogger();

            RouteContentBinding[] routeBindings = FindObjectsByType<RouteContentBinding>(
                FindObjectsInactive.Include);
            ActivityLocalVisibilityAdapter[] activityAdapters = FindObjectsByType<ActivityLocalVisibilityAdapter>(
                FindObjectsInactive.Include);
            RouteContentAnchor[] routeContentAnchors = FindObjectsByType<RouteContentAnchor>(
                FindObjectsInactive.Include);
            ActivityContentAnchor[] activityContentAnchors = FindObjectsByType<ActivityContentAnchor>(
                FindObjectsInactive.Include);
            RouteCycleResetTrigger[] routeCycleResetTriggers = FindObjectsByType<RouteCycleResetTrigger>(
                FindObjectsInactive.Include);
            ActivityCycleResetTrigger[] activityCycleResetTriggers = FindObjectsByType<ActivityCycleResetTrigger>(
                FindObjectsInactive.Include);
            ObjectEntryDeclaration[] objectEntryDeclarations = FindObjectsByType<ObjectEntryDeclaration>(
                FindObjectsInactive.Include);
            ObjectResetTrigger[] objectResetTriggers = FindObjectsByType<ObjectResetTrigger>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings?.Length ?? 0;
            int activityAdapterCount = activityAdapters?.Length ?? 0;
            int routeContentAnchorCount = routeContentAnchors?.Length ?? 0;
            int activityContentAnchorCount = activityContentAnchors?.Length ?? 0;
            int routeCycleResetTriggerCount = routeCycleResetTriggers?.Length ?? 0;
            int activityCycleResetTriggerCount = activityCycleResetTriggers?.Length ?? 0;
            int objectEntryDeclarationCount = objectEntryDeclarations?.Length ?? 0;
            int objectResetTriggerCount = objectResetTriggers?.Length ?? 0;

            if (routeBindingCount == 0
                && activityAdapterCount == 0
                && routeContentAnchorCount == 0
                && activityContentAnchorCount == 0
                && routeCycleResetTriggerCount == 0
                && activityCycleResetTriggerCount == 0
                && objectEntryDeclarationCount == 0
                && objectResetTriggerCount == 0)
            {
                _logger.Warning("QA Authoring Validation completed. scope='Loaded Authoring' routeBindings='0' activityAdapters='0' routeContentAnchors='0' activityContentAnchors='0' routeCycleResetTriggers='0' activityCycleResetTriggers='0' objectEntryDeclarations='0' objectResetTriggers='0' localContributions='0' contentAnchors='0' objectEntries='0' issues='1' reason='No framework authoring components found in loaded scenes'.");
                return;
            }

            int issueCount = 0;
            int errorCount = 0;
            int warningCount = 0;
            var contentAnchorDeclarations = new List<ContentAnchorDeclaration>(Math.Max(0, routeContentAnchorCount + activityContentAnchorCount));
            var objectEntryDescriptors = new List<ObjectEntryDescriptor>(Math.Max(0, objectEntryDeclarationCount));

            if (routeBindings != null)
            {
                for (int i = 0; i < routeBindings.Length; i++)
                {
                    ValidateLoadedRouteContentBinding(routeBindings[i], ref issueCount, ref errorCount, ref warningCount);
                }
            }

            if (routeContentAnchors != null)
            {
                for (int i = 0; i < routeContentAnchors.Length; i++)
                {
                    ValidateLoadedRouteContentAnchor(
                        routeContentAnchors[i],
                        contentAnchorDeclarations,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            if (activityContentAnchors != null)
            {
                for (int i = 0; i < activityContentAnchors.Length; i++)
                {
                    ValidateLoadedActivityContentAnchor(
                        activityContentAnchors[i],
                        contentAnchorDeclarations,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            ValidateLoadedCycleResetTriggers(
                routeCycleResetTriggers,
                activityCycleResetTriggers,
                ref issueCount,
                ref errorCount,
                ref warningCount);

            if (objectEntryDeclarations != null)
            {
                for (int i = 0; i < objectEntryDeclarations.Length; i++)
                {
                    ValidateLoadedObjectEntryDeclaration(
                        objectEntryDeclarations[i],
                        objectEntryDescriptors,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            ValidateLoadedObjectResetTriggers(
                objectResetTriggers,
                objectEntryDescriptors,
                ref issueCount,
                ref errorCount,
                ref warningCount);

            var objectEntrySet = CreateLoadedObjectEntrySet(
                objectEntryDescriptors,
                ref issueCount,
                ref errorCount);

            var contentAnchorSet = ContentAnchorSet.FromDeclarations(contentAnchorDeclarations);
            AddLoadedRouteContentAnchorSetIssues(contentAnchorSet, ref issueCount, ref errorCount, ref warningCount);

            var validationResult = LocalContributionValidator.ValidateLoadedSceneAuthored();
            AddLocalContributionValidationIssues(validationResult, ref issueCount, ref errorCount, ref warningCount);

            if (issueCount == 0)
            {
                _logger.Info(
                    "QA Authoring Validation completed.",
                    LogFields.Of(
                        LogFields.Field("scope", "Loaded Authoring"),
                        LogFields.Field("routeBindings", routeBindingCount),
                        LogFields.Field("activityAdapters", activityAdapterCount),
                        LogFields.Field("routeContentAnchors", routeContentAnchorCount),
                        LogFields.Field("activityContentAnchors", activityContentAnchorCount),
                        LogFields.Field("routeCycleResetTriggers", routeCycleResetTriggerCount),
                        LogFields.Field("activityCycleResetTriggers", activityCycleResetTriggerCount),
                        LogFields.Field("objectEntryDeclarations", objectEntryDeclarationCount),
                        LogFields.Field("objectResetTriggers", objectResetTriggerCount),
                        LogFields.Field("issues", 0),
                        LogFields.Field("localContributions", validationResult.ContributionCount),
                        LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                        LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                        LogFields.Field("contentAnchors", contentAnchorSet.Count),
                        LogFields.Field("contentAnchorIssues", contentAnchorSet.IssueCount),
                        LogFields.Field("contentAnchorDuplicateIdentity", contentAnchorSet.DuplicateIdentityIssueCount),
                        LogFields.Field("contentAnchorDuplicateId", contentAnchorSet.DuplicateAnchorIdIssueCount),
                        LogFields.Field("objectEntries", objectEntrySet.Count),
                        LogFields.Field("objectEntryRequired", objectEntrySet.RequiredCount),
                        LogFields.Field("objectEntryOptional", objectEntrySet.OptionalCount),
                        LogFields.Field("objectEntryRoute", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Route)),
                        LogFields.Field("objectEntryActivity", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Activity)),
                        LogFields.Field("objectEntrySession", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Session))));
                _logger.Debug(
                    "QA Authoring Validation diagnostics.",
                    LogFields.Of(
                        LogFields.Field("localContributions", validationResult.ToDiagnosticString()),
                        LogFields.Field("contentAnchors", contentAnchorSet.ToDiagnosticString()),
                        LogFields.Field("objectEntries", BuildObjectEntryDiagnostics(objectEntrySet))));
                return;
            }

            _logger.Warning(
                "QA Authoring Validation completed with issues.",
                LogFields.Of(
                    LogFields.Field("scope", "Loaded Authoring"),
                    LogFields.Field("routeBindings", routeBindingCount),
                    LogFields.Field("activityAdapters", activityAdapterCount),
                    LogFields.Field("routeContentAnchors", routeContentAnchorCount),
                    LogFields.Field("activityContentAnchors", activityContentAnchorCount),
                    LogFields.Field("routeCycleResetTriggers", routeCycleResetTriggerCount),
                    LogFields.Field("activityCycleResetTriggers", activityCycleResetTriggerCount),
                    LogFields.Field("objectEntryDeclarations", objectEntryDeclarationCount),
                    LogFields.Field("objectResetTriggers", objectResetTriggerCount),
                    LogFields.Field("issues", issueCount),
                    LogFields.Field("errors", errorCount),
                    LogFields.Field("warnings", warningCount),
                    LogFields.Field("localContributions", validationResult.ContributionCount),
                    LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                    LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                    LogFields.Field("contentAnchors", contentAnchorSet.Count),
                    LogFields.Field("contentAnchorIssues", contentAnchorSet.IssueCount),
                    LogFields.Field("contentAnchorDuplicateIdentity", contentAnchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("contentAnchorDuplicateId", contentAnchorSet.DuplicateAnchorIdIssueCount),
                    LogFields.Field("objectEntries", objectEntrySet.Count),
                    LogFields.Field("objectEntryRequired", objectEntrySet.RequiredCount),
                    LogFields.Field("objectEntryOptional", objectEntrySet.OptionalCount),
                    LogFields.Field("objectEntryRoute", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Route)),
                    LogFields.Field("objectEntryActivity", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Activity)),
                    LogFields.Field("objectEntrySession", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Session)),
                    LogFields.Field("localContributionDetails", validationResult.ToDiagnosticString()),
                    LogFields.Field("contentAnchorDetails", contentAnchorSet.ToDiagnosticString()),
                    LogFields.Field("objectEntryDetails", BuildObjectEntryDiagnostics(objectEntrySet))));
        }

        private void ValidateLoadedCycleResetTriggers(
            RouteCycleResetTrigger[] routeTriggers,
            ActivityCycleResetTrigger[] activityTriggers,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            var sharedObjects = new HashSet<GameObject>();

            if (routeTriggers != null)
            {
                for (int i = 0; i < routeTriggers.Length; i++)
                {
                    ValidateLoadedCycleResetTriggerCommon(routeTriggers[i], nameof(RouteCycleResetTrigger), ref issueCount, ref warningCount);

                    var trigger = routeTriggers[i];
                    if (trigger != null && trigger.gameObject != null && trigger.GetComponent<ActivityCycleResetTrigger>() != null)
                    {
                        if (sharedObjects.Add(trigger.gameObject))
                        {
                            string objectName = trigger.gameObject.name;
                            string sceneName = trigger.gameObject.scene.IsValid() ? trigger.gameObject.scene.name : "<no-scene>";
                            AddQaAuthoringWarning(
                                ref issueCount,
                                ref warningCount,
                                $"CycleResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Route and Activity Cycle Reset Triggers share the same GameObject'.");
                        }
                    }
                }
            }

            if (activityTriggers != null)
            {
                for (int i = 0; i < activityTriggers.Length; i++)
                {
                    ValidateLoadedCycleResetTriggerCommon(activityTriggers[i], nameof(ActivityCycleResetTrigger), ref issueCount, ref warningCount);
                }
            }
        }

        private void ValidateLoadedCycleResetTriggerCommon(
            MonoBehaviour trigger,
            string triggerType,
            ref int issueCount,
            ref int warningCount)
        {
            if (trigger == null)
            {
                AddQaAuthoringWarning(ref issueCount, ref warningCount, $"{triggerType} is missing.");
                return;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            string sceneName = trigger.gameObject != null && trigger.gameObject.scene.IsValid()
                ? trigger.gameObject.scene.name
                : "<no-scene>";

            if (trigger.gameObject != null && !trigger.gameObject.activeInHierarchy)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"{triggerType} object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Trigger GameObject is inactive in hierarchy'.");
            }

            string reason = string.Empty;
            if (trigger is RouteCycleResetTrigger { HasCustomReason: true } routeTrigger)
            {
                reason = routeTrigger.AuthoringReason;
            }
            else if (trigger is ActivityCycleResetTrigger { HasCustomReason: true } activityTrigger)
            {
                reason = activityTrigger.AuthoringReason;
            }

            if (!string.IsNullOrWhiteSpace(reason) && ContainsFutureResetVocabulary(reason))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"{triggerType} object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' reason='{FormatValue(reason)}' issue='Reason uses object/component/player/actor/pool/save/reload vocabulary reserved for later reset phases'.");
            }
        }

        private void ValidateLoadedObjectResetTriggers(
            ObjectResetTrigger[] objectResetTriggers,
            List<ObjectEntryDescriptor> objectEntryDescriptors,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (objectResetTriggers == null)
            {
                return;
            }

            for (int i = 0; i < objectResetTriggers.Length; i++)
            {
                var trigger = objectResetTriggers[i];
                if (trigger == null)
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        "ObjectResetTrigger is missing.");
                    continue;
                }

                string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
                string sceneName = trigger.gameObject != null && trigger.gameObject.scene.IsValid()
                    ? trigger.gameObject.scene.name
                    : "<no-scene>";

                if (trigger.gameObject != null && !trigger.gameObject.activeInHierarchy)
                {
                    AddQaAuthoringWarning(
                        ref issueCount,
                        ref warningCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Trigger GameObject is inactive in hierarchy'.");
                }

                string targetId = trigger.ResolvedAuthoringObjectEntryId;
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Target Object Entry Id is missing'.");
                    continue;
                }

                bool targetDeclared = false;
                if (objectEntryDescriptors != null)
                {
                    for (int descriptorIndex = 0; descriptorIndex < objectEntryDescriptors.Count; descriptorIndex++)
                    {
                        var descriptor = objectEntryDescriptors[descriptorIndex];
                        if (descriptor.Id.IsValid
                            && string.Equals(descriptor.Id.Value.Value, targetId.Trim(), StringComparison.Ordinal))
                        {
                            targetDeclared = true;
                            break;
                        }
                    }
                }

                if (!targetDeclared)
                {
                    AddQaAuthoringWarning(
                        ref issueCount,
                        ref warningCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' objectEntry='{FormatValue(targetId)}' issue='Target Object Entry Id is not declared in loaded Object Entry authoring'.");
                }
            }
        }

        private void ValidateLoadedObjectEntryDeclaration(
            ObjectEntryDeclaration declaration,
            List<ObjectEntryDescriptor> descriptors,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (declaration == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "ObjectEntryDeclaration is missing.");
                return;
            }

            string objectName = declaration.gameObject != null ? declaration.gameObject.name : "<missing>";
            string sceneName = declaration.gameObject != null && declaration.gameObject.scene.IsValid()
                ? declaration.gameObject.scene.name
                : "<no-scene>";

            if (declaration.gameObject != null && !declaration.gameObject.activeInHierarchy)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration GameObject is inactive in hierarchy'.");
            }

            if (!declaration.HasRequiredAuthoredOwner)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' scope='{declaration.Scope}' issue='Explicit owner is required for Route/Activity Object Entry'.");
                return;
            }

            if (!declaration.TryCreateDescriptor(out var descriptor, out string issue))
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='{FormatValue(issue)}'.");
                return;
            }

            descriptors.Add(descriptor);
        }

        private ObjectEntrySet CreateLoadedObjectEntrySet(
            List<ObjectEntryDescriptor> descriptors,
            ref int issueCount,
            ref int errorCount)
        {
            try
            {
                return new ObjectEntrySet(descriptors);
            }
            catch (ArgumentException exception)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntrySet issue='Duplicate Object Entry Id' message='{FormatValue(exception.Message)}'.");
                return ObjectEntrySet.Empty();
            }
        }

        private static int CountObjectEntriesByScope(ObjectEntrySet set, ObjectEntryScope scope)
        {
            if (set == null || set.IsEmpty)
            {
                return 0;
            }

            IReadOnlyList<ObjectEntryDescriptor> entries = set.Entries;
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Scope == scope)
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildObjectEntryDiagnostics(ObjectEntrySet set)
        {
            if (set == null || set.IsEmpty)
            {
                return "objectEntries='0'";
            }

            IReadOnlyList<ObjectEntryDescriptor> entries = set.Entries;
            var details = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                details.Add($"id='{entry.Id.StableText}' scope='{entry.Scope}' sourceKind='{entry.SourceKind}' requiredness='{entry.Requiredness}' displayName='{entry.DisplayName}'");
            }

            return $"{set.Summary} details=[{string.Join(" | ", details)}]";
        }

        private static bool ContainsFutureResetVocabulary(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] reservedTerms =
            {
                "object",
                "component",
                "player",
                "actor",
                "transform",
                "rigidbody",
                "animator",
                "pool",
                "snapshot",
                "save",
                "reload",
                "checkpoint"
            };

            for (int i = 0; i < reservedTerms.Length; i++)
            {
                if (value.IndexOf(reservedTerms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddLocalContributionValidationIssues(
            LocalContributionValidationResult validationResult,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (!validationResult.HasIssues)
            {
                return;
            }

            IReadOnlyList<LocalContributionValidationIssue> issues = validationResult.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Blocking)
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        $"LocalContributionValidation {issues[i].ToDiagnosticString()}.");
                    continue;
                }

                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"LocalContributionValidation {issues[i].ToDiagnosticString()}.");
            }
        }

        private void ValidateLoadedRouteContentAnchor(
            RouteContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (anchor == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "RouteContentAnchor is missing.");
                return;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";
            string sceneName = anchor.gameObject != null && anchor.gameObject.scene.IsValid()
                ? anchor.gameObject.scene.name
                : "<no-scene>";
            bool hasBlockingAuthoringIssue = false;

            if (anchor.Route == null)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Route'.");
            }

            if (!anchor.HasExplicitAnchorId)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Anchor Id'.");
            }

            if (!anchor.HasExplicitKind)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Unknown Kind'.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Invalid Requiredness'.");
            }

            if (anchor.Route != null && !DoesAnchorSceneMatchRoute(anchor))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(anchor.Route, "<unnamed>"))}' issue='Route does not declare this scene'.");
            }

            if (hasBlockingAuthoringIssue)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration creation failed' message='{FormatValue(exception.Message)}'.");
            }
        }

        private void ValidateLoadedActivityContentAnchor(
            ActivityContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (anchor == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "ActivityContentAnchor is missing.");
                return;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";
            string sceneName = anchor.gameObject != null && anchor.gameObject.scene.IsValid()
                ? anchor.gameObject.scene.name
                : "<no-scene>";
            bool hasBlockingAuthoringIssue = false;

            if (anchor.Activity == null)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Activity'.");
            }

            if (!anchor.HasExplicitAnchorId)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Anchor Id'.");
            }

            if (!anchor.HasExplicitKind)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Unknown Kind'.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Invalid Requiredness'.");
            }

            if (hasBlockingAuthoringIssue)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration creation failed' message='{FormatValue(exception.Message)}'.");
            }
        }

        private void AddLoadedRouteContentAnchorSetIssues(
            ContentAnchorSet contentAnchorSet,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (!contentAnchorSet.HasIssues)
            {
                return;
            }

            IReadOnlyList<ContentAnchorSetIssue> issues = contentAnchorSet.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                switch (issue.Kind)
                {
                    case ContentAnchorSetIssueKind.DuplicateIdentity:
                    case ContentAnchorSetIssueKind.DuplicateAnchorId:
                    case ContentAnchorSetIssueKind.InvalidDeclaration:
                        AddQaAuthoringIssue(
                            ref issueCount,
                            ref errorCount,
                            $"ContentAnchorSet {issue.ToDiagnosticString()}.");
                        break;
                    default:
                        AddQaAuthoringWarning(
                            ref issueCount,
                            ref warningCount,
                            $"ContentAnchorSet {issue.ToDiagnosticString()}.");
                        break;
                }
            }
        }

        private void ValidateLoadedRouteContentBinding(
            RouteContentBinding binding,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (binding == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "RouteContentBinding is missing.");
                return;
            }

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";
            string sceneName = binding.gameObject != null && binding.gameObject.scene.IsValid()
                ? binding.gameObject.scene.name
                : "<no-scene>";

            if (binding.Route == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Route'.");
                return;
            }

            if (!DoesBindingSceneMatchRoute(binding))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(binding.Route, "<unnamed>"))}' issue='Route does not own this scene'. expectedPrimaryScene='{FormatValue(binding.Route.PrimaryScenePath)}'.");
            }

            int receiverCount = CountRouteContentLifecycleReceivers(binding);
            if (receiverCount == 0)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(binding.Route, "<unnamed>"))}' issue='No IRouteContentLifecycleReceiver under binding'.");
            }
        }

        private void AddQaAuthoringIssue(
            ref int issueCount,
            ref int errorCount,
            string message)
        {
            issueCount++;
            errorCount++;
            _logger.Error($"QA Authoring Validation issue. severity='Error' {message}");
        }

        private void AddQaAuthoringWarning(
            ref int issueCount,
            ref int warningCount,
            string message)
        {
            issueCount++;
            warningCount++;
            _logger.Warning($"QA Authoring Validation issue. severity='Warning' {message}");
        }

        private static bool DoesAnchorSceneMatchRoute(RouteContentAnchor anchor)
        {
            if (anchor == null || anchor.Route == null || anchor.gameObject == null)
            {
                return false;
            }

            var scene = anchor.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }

            return DoesRouteDeclareScene(anchor.Route, scene.path, scene.name);
        }

        private static bool DoesRouteDeclareScene(RouteAsset route, string scenePath, string sceneName)
        {
            if (route == null)
            {
                return false;
            }

            if (MatchesScene(route.PrimaryScenePath, route.PrimarySceneName, scenePath, sceneName))
            {
                return true;
            }

            var profile = route.RouteContentProfile;
            if (profile == null || profile.AdditionalScenes == null)
            {
                return false;
            }

            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                var entry = profile.AdditionalScenes[i];
                if (entry != null && MatchesScene(entry.ScenePath, entry.SceneName, scenePath, sceneName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesScene(string declaredPath, string declaredName, string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(declaredPath)
                && !string.IsNullOrWhiteSpace(scenePath)
                && string.Equals(declaredPath.Trim(), scenePath.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(declaredName)
                && !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(declaredName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool DoesBindingSceneMatchRoute(RouteContentBinding binding)
        {
            if (binding == null || binding.Route == null || binding.gameObject == null)
            {
                return false;
            }

            var scene = binding.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(binding.Route.PrimaryScenePath)
                && !string.IsNullOrWhiteSpace(scene.path))
            {
                return string.Equals(scene.path, binding.Route.PrimaryScenePath, StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(binding.Route.PrimarySceneName)
                && string.Equals(scene.name, binding.Route.PrimarySceneName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountRouteContentLifecycleReceivers(RouteContentBinding binding)
        {
            if (binding == null)
            {
                return 0;
            }

            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRouteContentLifecycleReceiver)
                {
                    count++;
                }
            }

            return count;
        }

        private async Task<bool> RequestActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            ActivityAsset activity,
            string reason,
            bool allowAlreadyActive = false)
        {
            var result = await RequestActivityWithResultCoreAsync(runtimeHost, activity, reason);
            return result.Succeeded || allowAlreadyActive && result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive;
        }

        private async Task<FrameworkActivityRequestResult> RequestActivityWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            ActivityAsset activity,
            string reason)
        {
            if (activity == null)
            {
                _logger.Error("QA Activity Request failed. Target Activity is missing.");
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "QA Activity Request failed. Target Activity is missing.",
                    activity,
                    QaSource,
                    reason);
            }

            return await runtimeHost.RequestActivityAsync(activity, QaSource, reason);
        }

        private async Task<bool> ClearActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            string reason,
            bool allowNoActiveActivity = false,
            bool requireNoActiveActivity = false)
        {
            var result = await ClearActivityWithResultCoreAsync(runtimeHost, reason);
            if (requireNoActiveActivity)
            {
                return result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity;
            }

            return result.Succeeded || allowNoActiveActivity && result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity;
        }

        private async Task<FrameworkActivityRequestResult> ClearActivityWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            string reason)
        {
            return await runtimeHost.ClearActivityAsync(QaSource, reason);
        }

        private bool TryGetRuntimeHost(string message, out FrameworkRuntimeHost runtimeHost)
        {
            if (FrameworkRuntimeHost.TryGetCurrent(out runtimeHost))
            {
                return true;
            }

            _logger.Error(message);
            return false;
        }

        private bool TryBeginOperation(string busyMessage)
        {
            if (_requestInFlight)
            {
                _logger.Warning(busyMessage);
                return false;
            }

            _requestInFlight = true;
            return true;
        }

        private void EndOperation()
        {
            _requestInFlight = false;
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<FrameworkQaCanvas>();
        }

        private bool TryValidateSmokeTargets(string smokeName, string missingTargets)
        {
            EnsureLogger();
            if (string.IsNullOrEmpty(missingTargets))
            {
                return true;
            }

            _logger.Warning($"QA Smoke skipped. name='{smokeName}'. reason='Missing QA target(s): {FormatValue(missingTargets)}'.");
            return false;
        }

        private RouteAsset ResolveResetRoute(FrameworkRuntimeHost runtimeHost)
        {
            if (resetRoute != null)
            {
                return resetRoute;
            }

            var state = runtimeHost.State;
            return state.GameApplication != null ? state.GameApplication.StartupRoute : null;
        }

        private ActivityAsset ResolveResetActivity(RouteAsset effectiveRoute)
        {
            if (resetActivity != null)
            {
                return resetActivity;
            }

            return effectiveRoute != null ? effectiveRoute.StartupActivity : null;
        }

        private string ResolveResetStepReason(string suffix)
        {
            string baseReason = ResolveReason(resetReason, "qa.reset");
            return $"{baseReason}.{suffix}";
        }

        private static string ResolveReason(string configuredReason, string fallback)
        {
            return configuredReason.NormalizeTextOrFallback(fallback);
        }

        private static string GetAssetName(Object asset, string fallback)
        {
            if (asset is RouteAsset route)
            {
                return GetName(route.RouteName, fallback);
            }

            if (asset is ActivityAsset activity)
            {
                return GetName(activity.ActivityName, fallback);
            }

            return fallback;
        }

        private string GetCoreQaAssetsStatus()
        {
            string missingCore = string.Empty;
            AppendMissingQaAsset(ref missingCore, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingCore, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingCore, primaryActivity == null, "Primary Activity");
            AppendMissingQaAsset(ref missingCore, secondaryActivity == null, "Secondary Activity");

            return string.IsNullOrEmpty(missingCore)
                ? "Core QA targets configured."
                : $"Core QA targets missing: {missingCore}";
        }

        private string GetQaAssetsStatus()
        {
            string missingCore = string.Empty;
            AppendMissingQaAsset(ref missingCore, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingCore, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingCore, primaryActivity == null, "Primary Activity");
            AppendMissingQaAsset(ref missingCore, secondaryActivity == null, "Secondary Activity");

            string missingOptional = string.Empty;
            AppendMissingQaAsset(ref missingOptional, noActivityRoute == null, "No-Activity Route");
            AppendMissingQaAsset(ref missingOptional, noContentActivity == null, "No-Content Activity");
            AppendMissingQaAsset(ref missingOptional, routeSceneCompositionRoute == null, "Route Scene Composition Route");

            if (string.IsNullOrEmpty(missingCore) && string.IsNullOrEmpty(missingOptional))
            {
                return "QA scenario targets configured.";
            }

            if (string.IsNullOrEmpty(missingCore))
            {
                return $"QA core targets configured. Optional targets missing: {missingOptional}";
            }

            if (string.IsNullOrEmpty(missingOptional))
            {
                return $"QA core targets missing: {missingCore}";
            }

            return $"QA core targets missing: {missingCore}. Optional targets missing: {missingOptional}";
        }

        private static void AppendMissingQaAsset(ref string missing, bool isMissing, string label)
        {
            if (!isMissing)
            {
                return;
            }

            missing = string.IsNullOrEmpty(missing) ? label : $"{missing}, {label}";
        }

        private static string GetName(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
        }


        private enum SyntheticActivityContentExecutionParticipantMode
        {
            Success,
            NoOp
        }

        private sealed class SyntheticActivityContentExecutionParticipantSource : IActivityContentExecutionParticipantSource
        {
            private readonly ActivityContentExecutionParticipantCollection _collection;

            public SyntheticActivityContentExecutionParticipantSource(IReadOnlyList<IActivityContentExecutionParticipant> participants)
            {
                _collection = ActivityContentExecutionParticipantCollection.FromParticipants(participants);
            }

            public int ResolveCount { get; private set; }

            public ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(ActivityContentExecutionParticipantSourceRequest request)
            {
                ResolveCount++;
                if (!request.IsValid)
                {
                    return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                        request,
                        QaSource,
                        "qa.activity-content-execution-source.synthetic",
                        "Synthetic Activity content execution participant source rejected an invalid request.");
                }

                return ActivityContentExecutionParticipantSourceResult.FromCollection(
                    request,
                    _collection,
                    QaSource,
                    "qa.activity-content-execution-source.synthetic",
                    "Synthetic Activity content execution participant source returned explicit QA participants.");
            }
        }

        private sealed class SyntheticActivityContentExecutionParticipant : IActivityContentExecutionParticipant
        {
            private readonly ActivityContentExecutionParticipantDescriptor _descriptor;
            private readonly SyntheticActivityContentExecutionParticipantMode _mode;

            public SyntheticActivityContentExecutionParticipant(
                ActivityContentExecutionParticipantDescriptor descriptor,
                SyntheticActivityContentExecutionParticipantMode mode)
            {
                _descriptor = descriptor;
                _mode = mode;
            }

            public ActivityContentExecutionParticipantDescriptor GetActivityContentExecutionDescriptor()
            {
                return _descriptor;
            }

            public ActivityContentExecutionResult ExecuteActivityContent(ActivityContentExecutionRequest request)
            {
                if (!request.ContentId.Equals(_descriptor.ContentId))
                {
                    return request.IsRequired
                        ? ActivityContentExecutionResult.BlockingFailure(
                            request,
                            1,
                            QaSource,
                            "qa.activity-content-execution.synthetic",
                            "Synthetic Activity content execution participant received a request for a different content id.")
                        : ActivityContentExecutionResult.NonBlockingFailure(
                            request,
                            1,
                            QaSource,
                            "qa.activity-content-execution.synthetic",
                            "Synthetic Activity content execution participant received a request for a different content id.");
                }

                if (!_descriptor.SupportsPhase(request.Phase))
                {
                    return ActivityContentExecutionResult.SkippedResult(
                        request,
                        ActivityContentExecutionStatus.SkippedNotApplicable,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant does not support the requested phase.");
                }

                return _mode == SyntheticActivityContentExecutionParticipantMode.NoOp
                    ? ActivityContentExecutionResult.SucceededNoOp(
                        request,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant completed with no operation.")
                    : ActivityContentExecutionResult.Success(
                        request,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant succeeded.");
            }
        }

        private readonly struct EditorDisabledScope : IDisposable
        {
            private readonly bool _previousEnabled;

            public EditorDisabledScope(bool disabled)
            {
                _previousEnabled = GUI.enabled;
                GUI.enabled = !disabled && _previousEnabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previousEnabled;
            }
        }
    }
}
#endif
