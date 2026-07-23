using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;
using Immersive.Framework.Common;

namespace Immersive.Framework.Bootstrap
{
    /// <summary>
    /// Internal runtime bootstrap for the Immersive Framework.
    /// It resolves and validates the active Game Application, then hands off to the first lifecycle owner.
    /// Activity, Actor, Input, Camera, Save and Pooling lifecycles are not owned here.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal static class ImmersiveFrameworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void BootAfterSceneLoad()
        {
            var logger = FrameworkLogger.Create(typeof(ImmersiveFrameworkBootstrap));

            try
            {
                var settings = LoadSettings();

#if UNITY_EDITOR
                if (ShouldSkipFrameworkStartupInEditor(settings))
                {
                    logger.Info("Boot skipped.", LogFields.Field("editorPlayModeStartup", "CurrentSceneOnly"));
                    return;
                }
#endif

                var result = FrameworkBootValidator.Validate(settings);

                if (!result.Succeeded)
                {
                    logger.Error("Boot failed.", LogFields.Field("reason", result.Message));
                    return;
                }

                var runtimeHost = FrameworkRuntimeHost.Create(result.GameApplication);
                PlayerParticipationRuntimeHostModule.Attach(
                    runtimeHost,
                    result.GameApplication,
                    "ImmersiveFrameworkBootstrap",
                    "session-start",
                    out PlayerParticipationOperationResult playerParticipationInitialization);

                if (!playerParticipationInitialization.Succeeded)
                {
                    logger.Error(
                        "Player participation Session runtime initialization failed.",
                        BuildPlayerParticipationRuntimeFields(playerParticipationInitialization));
                    UnityEngine.Object.Destroy(runtimeHost.gameObject);
                    return;
                }

                logger.Debug(
                    "Player participation Session runtime initialized.",
                    BuildPlayerParticipationRuntimeFields(playerParticipationInitialization));

                var gameFlowResult = await runtimeHost.StartAsync();
                if (!gameFlowResult.Started)
                {
                    logger.Error("Game Flow failed.", LogFields.Field("reason", gameFlowResult.Message));
                    return;
                }

                if (!TryInitializeLocalPlayerProvisioning(runtimeHost, logger))
                {
                    UnityEngine.Object.Destroy(runtimeHost.gameObject);
                    return;
                }

                logger.Info(
                    "Boot succeeded. Application Runtime started.",
                    BuildBootSummaryFields(result, gameFlowResult));
                logger.Debug("Boot diagnostics. " + gameFlowResult.Message, BuildBootDiagnosticFields(result, gameFlowResult, runtimeHost));
                LogActivityContentObservability(logger, gameFlowResult.RouteLifecycleResult.ActivityFlowResult.ActivityContentResult);
            }
            catch (Exception exception)
            {
                logger.Error("Boot failed.", exception);
            }
        }

        internal static FrameworkBootResult Boot()
        {
            return FrameworkBootValidator.Validate(LoadSettings());
        }

        private static LogField[] BuildBootSummaryFields(
            FrameworkBootResult result,
            FrameworkGameFlowStartResult gameFlowResult)
        {
            RouteLifecycleStartResult routeLifecycleResult = gameFlowResult.RouteLifecycleResult;
            ActivityFlowStartResult activityFlowResult = routeLifecycleResult.ActivityFlowResult;
            return LogFields.Of(
                LogFields.Field("gameApplication", result.GameApplication != null ? result.GameApplication.ApplicationName : null),
                LogFields.Field("startupRoute", result.StartupRoute != null ? result.StartupRoute.RouteName : null),
                LogFields.Field("primaryScene", result.StartupRoute != null ? result.StartupRoute.PrimarySceneName : null),
                LogFields.Field("validationMode", result.ValidationMode),
                LogFields.Field("routeSceneComposition", routeLifecycleResult.RouteSceneCompositionResult.Status),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlowResult.ActivityState.ActivityName)),
                LogFields.Field("activityReadiness", activityFlowResult.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("blockingIssues", routeLifecycleResult.RouteSceneCompositionResult.BlockingIssueCount + activityFlowResult.ActivityReadinessState.BlockingIssueCount));
        }

        private static LogField[] BuildBootDiagnosticFields(
            FrameworkBootResult result,
            FrameworkGameFlowStartResult gameFlowResult,
            FrameworkRuntimeHost runtimeHost)
        {
            RouteLifecycleStartResult routeLifecycleResult = gameFlowResult.RouteLifecycleResult;
            ActivityFlowStartResult activityFlowResult = routeLifecycleResult.ActivityFlowResult;
            ActivityContentApplyResult activityContentResult = activityFlowResult.ActivityContentResult;
            PlayerParticipationSnapshot playerParticipation = null;
            bool hasPlayerParticipation = runtimeHost != null &&
                runtimeHost.TryGetPlayerParticipationSnapshot(out playerParticipation);
            LocalPlayerProvisioningRuntimeHostModule localPlayerProvisioning = null;
            bool hasLocalPlayerProvisioning = runtimeHost != null &&
                runtimeHost.TryGetLocalPlayerProvisioningRuntime(
                    out localPlayerProvisioning);

            return LogFields.Of(
                LogFields.Field("gameApplication", result.GameApplication != null ? result.GameApplication.ApplicationName : null),
                LogFields.Field("startupRoute", result.StartupRoute != null ? result.StartupRoute.RouteName : null),
                LogFields.Field("primaryScene", result.StartupRoute != null ? result.StartupRoute.PrimarySceneName : null),
                LogFields.Field("validationMode", result.ValidationMode),
                LogFields.Field("alreadyLoaded", routeLifecycleResult.SceneLifecycleResult.AlreadyLoaded),
                LogFields.Field("loadMode", routeLifecycleResult.SceneLifecycleResult.LoadMode),
                LogFields.Field("routeSceneComposition", routeLifecycleResult.RouteSceneCompositionResult.Status),
                LogFields.Field("routeSceneLoaded", routeLifecycleResult.RouteSceneCompositionResult.LoadedCount),
                LogFields.Field("routeSceneFailed", routeLifecycleResult.RouteSceneCompositionResult.FailedCount),
                LogFields.Field("routeSceneBlockingIssues", routeLifecycleResult.RouteSceneCompositionResult.BlockingIssueCount),
                LogFields.Field("routeRelease", routeLifecycleResult.ContentReleaseResult.Status),
                LogFields.Field("routeReleaseReleased", routeLifecycleResult.ContentReleaseResult.ReleasedCount),
                LogFields.Field("routeReleaseSkipped", routeLifecycleResult.ContentReleaseResult.SkippedCount),
                LogFields.Field("routeReleaseFailed", routeLifecycleResult.ContentReleaseResult.FailedCount),
                LogFields.Field("routeReleaseBlockingIssues", routeLifecycleResult.ContentReleaseResult.BlockingIssueCount),
                LogFields.Field("runtimeRouteScope", routeLifecycleResult.RuntimeRouteScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeRouteRootEnter", routeLifecycleResult.RuntimeRouteScopeResult.EnterStatus),
                LogFields.Field("runtimeRouteRootExit", routeLifecycleResult.RuntimeRouteScopeResult.ExitStatus),
                LogFields.Field("runtimeRouteContext", routeLifecycleResult.RuntimeRouteScopeResult.ContextStatus),
                LogFields.Field("runtimeRootCount", routeLifecycleResult.RuntimeRouteScopeResult.RootCount),
                LogFields.Field("routeContentHandles", routeLifecycleResult.RouteContentSet.Count),
                LogFields.Field("contentAnchors", routeLifecycleResult.ContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("contentAnchorCandidates", routeLifecycleResult.ContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("contentAnchorIssues", routeLifecycleResult.ContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("contentAnchorInvalid", routeLifecycleResult.ContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("contentAnchorRouteMismatch", routeLifecycleResult.ContentAnchorDiscoveryResult.SkippedRouteMismatchCount),
                LogFields.Field("contentAnchorBindings", runtimeHost != null ? runtimeHost.ContentAnchorBindingCount : 0),
                LogFields.Field("playerParticipationInitialized", hasPlayerParticipation),
                LogFields.Field("playerParticipationContext", hasPlayerParticipation ? playerParticipation.ContextId : string.Empty),
                LogFields.Field("playerParticipationRevision", hasPlayerParticipation ? playerParticipation.Revision : 0),
                LogFields.Field("playerParticipationSlots", hasPlayerParticipation ? playerParticipation.ConfiguredSlotCount : 0),
                LogFields.Field("playerParticipationCapacity", hasPlayerParticipation ? playerParticipation.DynamicCapacity : 0),
                LogFields.Field("playerParticipationJoiningOpen", hasPlayerParticipation && playerParticipation.JoiningOpen),
                LogFields.Field("localPlayerProvisioningReady", hasLocalPlayerProvisioning),
                LogFields.Field("localPlayerProvisioningAuthoring", hasLocalPlayerProvisioning ? localPlayerProvisioning.Authoring.name : string.Empty),
                LogFields.Field("localPlayerProvisioningManager", hasLocalPlayerProvisioning && localPlayerProvisioning.Authoring.PlayerInputManager != null ? localPlayerProvisioning.Authoring.PlayerInputManager.name : string.Empty),
                LogFields.Field("localPlayerProvisioningRequests", hasLocalPlayerProvisioning ? localPlayerProvisioning.RequestCount : 0),
                LogFields.Field("localPlayerProvisioningDiagnostic", hasLocalPlayerProvisioning ? localPlayerProvisioning.Diagnostic : "NotConfigured"),
                LogFields.Field("activityContentExecution", activityFlowResult.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", activityFlowResult.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", activityFlowResult.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", activityFlowResult.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", activityFlowResult.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentExecutionEnterRequests", activityFlowResult.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", activityFlowResult.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", activityFlowResult.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", activityFlowResult.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", activityFlowResult.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentParticipantExecution", activityFlowResult.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentParticipantSource", activityFlowResult.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentParticipantSourceIssues", activityFlowResult.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentParticipantCount", activityFlowResult.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentParticipantEnter", activityFlowResult.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentParticipantEnterRequests", activityFlowResult.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentParticipantExit", activityFlowResult.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentParticipantExitRequests", activityFlowResult.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentParticipantBlockingIssues", activityFlowResult.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentParticipantBlocksReadiness", activityFlowResult.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentAnchors", activityFlowResult.ActivityContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("activityContentAnchorCandidates", activityFlowResult.ActivityContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("activityContentDiscoverySceneRoots", activityFlowResult.ActivityContentAnchorDiscoveryResult.DiscoverySceneRootCount),
                LogFields.Field("activityContentAnchorIssues", activityFlowResult.ActivityContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("activityContentAnchorInvalid", activityFlowResult.ActivityContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("activityContentAnchorActivityMismatch", activityFlowResult.ActivityContentAnchorDiscoveryResult.SkippedActivityMismatchCount),
                LogFields.Field("routeContentEnterReceivers", routeLifecycleResult.RouteContentEnterResult.ReceiverCount),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlowResult.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlowResult.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlowResult.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("runtimeActivityScope", activityFlowResult.RuntimeActivityScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeActivityRootEnter", activityFlowResult.RuntimeActivityScopeResult.EnterStatus),
                LogFields.Field("runtimeActivityRootExit", activityFlowResult.RuntimeActivityScopeResult.ExitStatus),
                LogFields.Field("runtimeActivityContext", activityFlowResult.RuntimeActivityScopeResult.ContextStatus),
                LogFields.Field("activityContentHandles", activityContentResult.ActivityContentCount),
                LogFields.Field("activitySceneLedger", activityFlowResult.ActivitySceneLedgerSnapshot.DiagnosticStatus),
                LogFields.Field("activitySceneLedgerEntries", activityFlowResult.ActivitySceneLedgerSnapshot.EntryCount),
                LogFields.Field("activitySceneLedgerLoaded", activityFlowResult.ActivitySceneLedgerSnapshot.LoadedCount),
                LogFields.Field("activitySceneLedgerReleased", activityFlowResult.ActivitySceneLedgerSnapshot.ReleasedCount),
                LogFields.Field("activitySceneLedgerStale", activityFlowResult.ActivitySceneLedgerSnapshot.StaleCount));
        }

        private static bool TryInitializeLocalPlayerProvisioning(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger)
        {
            if (!runtimeHost.TryResolveLocalPlayerProvisioningAuthoring(
                    out LocalPlayerProvisioningAuthoring authoring,
                    out bool isConfigured,
                    out string registrationDiagnostic))
            {
                logger.Error(
                    "Local Player provisioning Session runtime initialization failed.",
                    LogFields.Of(
                        LogFields.Field("status", "RejectedHostRegistration"),
                        LogFields.Field("message", registrationDiagnostic)));
                return false;
            }

            if (!isConfigured)
            {
                logger.Debug(
                    "Local Player provisioning is not configured.",
                    LogFields.Of(
                        LogFields.Field("status", "NotConfigured"),
                        LogFields.Field("message", registrationDiagnostic)));
                return true;
            }

            if (!LocalPlayerProvisioningRuntimeHostModule.TryAttach(
                    runtimeHost,
                    authoring,
                    out LocalPlayerProvisioningRuntimeHostModule module,
                    out string issue))
            {
                logger.Error(
                    "Local Player provisioning Session runtime initialization failed.",
                    LogFields.Of(
                        LogFields.Field("status", "RejectedInvalidConfiguration"),
                        LogFields.Field("authoring", authoring.name),
                        LogFields.Field("manager", authoring.PlayerInputManager != null ? authoring.PlayerInputManager.name : string.Empty),
                        LogFields.Field("message", issue)));
                return false;
            }

            PlayerParticipationSnapshot snapshot = null;
            module.TryGetSnapshot(out snapshot);
            logger.Debug(
                "Local Player provisioning Session runtime initialized.",
                LogFields.Of(
                    LogFields.Field("status", "Ready"),
                    LogFields.Field("authoring", authoring.name),
                    LogFields.Field("manager", authoring.PlayerInputManager.name),
                    LogFields.Field("context", snapshot != null ? snapshot.ContextId : string.Empty),
                    LogFields.Field("slots", snapshot != null ? snapshot.ConfiguredSlotCount : 0),
                    LogFields.Field("capacity", snapshot != null ? snapshot.DynamicCapacity : 0),
                    LogFields.Field("joiningOpen", snapshot != null && snapshot.JoiningOpen),
                    LogFields.Field("message", module.Diagnostic)));
            return true;
        }

        private static LogField[] BuildPlayerParticipationRuntimeFields(
            PlayerParticipationOperationResult initializationResult)
        {
            PlayerParticipationSnapshot snapshot = initializationResult?.Snapshot;
            return LogFields.Of(
                LogFields.Field("operation", initializationResult != null ? initializationResult.Operation : string.Empty),
                LogFields.Field("status", initializationResult != null ? initializationResult.Status.ToString() : "Missing"),
                LogFields.Field("configuredSlots", snapshot != null ? snapshot.ConfiguredSlotCount : 0),
                LogFields.Field("dynamicCapacity", snapshot != null ? snapshot.DynamicCapacity : 0),
                LogFields.Field("joiningOpen", snapshot != null && snapshot.JoiningOpen),
                LogFields.Field("revision", snapshot != null ? snapshot.Revision : 0),
                LogFields.Field("message", initializationResult != null ? initializationResult.Message : "Initialization result is missing."));
        }

        private static string FormatDiagnosticValue(string value)
        {
            return value.NormalizeTextOrFallback("<none>");
        }

        private static void LogActivityContentObservability(FrameworkLogger logger, ActivityContentApplyResult activityContentResult)
        {
            if (activityContentResult.HasDetailMessage)
            {
                logger.Debug(activityContentResult.DetailMessage);
            }

            if (activityContentResult.HasWarningMessage)
            {
                logger.Warning(activityContentResult.WarningMessage);
            }
        }

        private static ImmersiveFrameworkSettingsAsset LoadSettings()
        {
            return Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath);
        }

#if UNITY_EDITOR
        private static bool ShouldSkipFrameworkStartupInEditor(ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null &&
                   settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly;
        }
#endif
    }
}
