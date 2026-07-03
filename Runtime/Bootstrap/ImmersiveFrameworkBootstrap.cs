using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
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
                var gameFlowResult = await runtimeHost.StartAsync();
                if (!gameFlowResult.Started)
                {
                    logger.Error("Game Flow failed.", LogFields.Field("reason", gameFlowResult.Message));
                    return;
                }

                logger.Info(
                    "Boot succeeded. Application Runtime started.",
                    BuildBootFields(result, gameFlowResult, runtimeHost));
                logger.Debug("Boot diagnostics. " + gameFlowResult.Message);
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

        private static LogField[] BuildBootFields(
            FrameworkBootResult result,
            FrameworkGameFlowStartResult gameFlowResult,
            FrameworkRuntimeHost runtimeHost)
        {
            RouteLifecycleStartResult routeLifecycleResult = gameFlowResult.RouteLifecycleResult;
            ActivityFlowStartResult activityFlowResult = routeLifecycleResult.ActivityFlowResult;
            ActivityContentApplyResult activityContentResult = activityFlowResult.ActivityContentResult;

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
