using Immersive.Framework.Common; 
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// Development-only runner for the F11 Cycle Reset runtime-host smoke.
    /// It installs synthetic participants temporarily and validates the canonical runtime request path.
    /// It does not reset Unity objects, reload scenes, release content, restore snapshots, return pooled objects or touch gameplay state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Shared QA runner for F11 Cycle Reset runtime-host smoke; no physical reset.")]
    internal static class CycleResetQaSmokeRunner
    {
        internal const string SmokeName = "Cycle Reset Runtime Host Smoke";

        internal const string TriggerSmokeName = "Cycle Reset Trigger Smoke";

        internal const string BridgeSmokeName = "Cycle Reset Bridge Smoke";

        internal static async Task<bool> RunRuntimeHostSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool runRouteCycleReset = true,
            bool runActivityCycleReset = true,
            bool logParticipantDetails = false,
            bool emitSmokeEnvelope = false)
        {
            if (logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(CycleResetQaSmokeRunner) : source;

            if (emitSmokeEnvelope)
            {
                logger.Info($"QA Smoke started. name='{SmokeName}'.");
            }

            if (runtimeHost == null)
            {
                logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Framework Runtime Host is missing'.");
                return false;
            }

            if (runtimeHost.State.CurrentRoute == null)
            {
                logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Active Route is missing'.");
                return false;
            }

            var participantSource = new SyntheticCycleResetParticipantSource(source);
            runtimeHost.SetCycleResetParticipantSource(participantSource);

            try
            {
                bool completed = true;

                if (runRouteCycleReset)
                {
                    completed &= await RunRouteCycleResetStep(runtimeHost, logger, source, logParticipantDetails);
                }

                if (runActivityCycleReset)
                {
                    if (runtimeHost.State.CurrentActivity == null)
                    {
                        logger.Warning("QA Cycle Reset Smoke step failed. step='activity' reason='Activity Cycle Reset requires an active Activity'.");
                        completed = false;
                    }
                    else
                    {
                        completed &= await RunActivityCycleResetStep(runtimeHost, logger, source, logParticipantDetails);
                    }
                }

                if (emitSmokeEnvelope)
                {
                    if (completed)
                    {
                        logger.Info($"QA Smoke completed. name='{SmokeName}'.");
                    }
                    else
                    {
                        logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Step failed'.");
                    }
                }

                return completed;
            }
            finally
            {
                runtimeHost.SetCycleResetParticipantSource(null);
            }
        }



        internal static async Task<bool> RunTriggerSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool runRouteCycleReset = true,
            bool runActivityCycleReset = true,
            bool emitSmokeEnvelope = false)
        {
            if (logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(CycleResetQaSmokeRunner) : source;

            if (emitSmokeEnvelope)
            {
                logger.Info($"QA Smoke started. name='{TriggerSmokeName}'.");
            }

            if (runtimeHost == null)
            {
                logger.Warning($"QA Smoke aborted. name='{TriggerSmokeName}'. reason='Framework Runtime Host is missing'.");
                return false;
            }

            if (runtimeHost.State.CurrentRoute == null)
            {
                logger.Warning($"QA Smoke aborted. name='{TriggerSmokeName}'. reason='Active Route is missing'.");
                return false;
            }

            IRouteCycleResetRuntimePort routeCycleResetRuntime = runtimeHost;

            bool completed = true;

            if (runRouteCycleReset)
            {
                completed &= await RunRouteTriggerStep(logger, source, routeCycleResetRuntime);
            }

            if (runActivityCycleReset)
            {
                if (runtimeHost.State.CurrentActivity == null)
                {
                    logger.Warning("QA Cycle Reset Trigger Smoke step failed. step='activity-trigger' reason='Activity Cycle Reset requires an active Activity'.");
                    completed = false;
                }
                else
                {
                    completed &= await RunActivityTriggerStep(logger, source);
                }
            }

            if (emitSmokeEnvelope)
            {
                if (completed)
                {
                    logger.Info($"QA Smoke completed. name='{TriggerSmokeName}'.");
                }
                else
                {
                    logger.Warning($"QA Smoke aborted. name='{TriggerSmokeName}'. reason='Step failed'.");
                }
            }

            return completed;
        }

        internal static async Task<bool> RunBridgeSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool runRouteCycleReset = true,
            bool runActivityCycleReset = true,
            bool emitSmokeEnvelope = false)
        {
            if (logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(CycleResetQaSmokeRunner) : source;

            if (emitSmokeEnvelope)
            {
                logger.Info($"QA Smoke started. name='{BridgeSmokeName}'.");
            }

            if (runtimeHost == null)
            {
                logger.Warning($"QA Smoke aborted. name='{BridgeSmokeName}'. reason='Framework Runtime Host is missing'.");
                return false;
            }

            if (runtimeHost.State.CurrentRoute == null)
            {
                logger.Warning($"QA Smoke aborted. name='{BridgeSmokeName}'. reason='Active Route is missing'.");
                return false;
            }

            IRouteCycleResetRuntimePort routeCycleResetRuntime = runtimeHost;

            bool completed = true;

            if (runRouteCycleReset)
            {
                completed &= await RunRouteBridgeStep(logger, source, routeCycleResetRuntime);
            }

            if (runActivityCycleReset)
            {
                if (runtimeHost.State.CurrentActivity == null)
                {
                    logger.Warning("QA Cycle Reset Bridge Smoke step failed. step='activity-bridge' reason='Activity Cycle Reset requires an active Activity'.");
                    completed = false;
                }
                else
                {
                    completed &= await RunActivityBridgeStep(logger, source);
                }
            }

            if (emitSmokeEnvelope)
            {
                if (completed)
                {
                    logger.Info($"QA Smoke completed. name='{BridgeSmokeName}'.");
                }
                else
                {
                    logger.Warning($"QA Smoke aborted. name='{BridgeSmokeName}'. reason='Step failed'.");
                }
            }

            return completed;
        }

        private static async Task<bool> RunRouteCycleResetStep(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool logParticipantDetails)
        {
            var result = await runtimeHost.RequestRouteCycleResetAsync(source, "qa.cycle-reset.runtime-host.route");
            return ValidateRuntimeHostResult(logger, "route", result, ExpectedParticipantCountFor(result.Request), logParticipantDetails);
        }

        private static async Task<bool> RunActivityCycleResetStep(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool logParticipantDetails)
        {
            var result = await runtimeHost.RequestActivityCycleResetAsync(source, "qa.cycle-reset.runtime-host.activity");
            return ValidateRuntimeHostResult(logger, "activity", result, ExpectedParticipantCountFor(result.Request), logParticipantDetails);
        }

        private static bool ValidateRuntimeHostResult(
            FrameworkLogger logger,
            string step,
            CycleResetResult result,
            int expectedParticipantCount,
            bool logParticipantDetails)
        {
            if (!result.Succeeded)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Runtime host request did not succeed'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.Status == CycleResetStatus.SucceededNoParticipants)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Runtime host request completed without participants, but synthetic participant source was installed'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.ParticipantCount != expectedParticipantCount)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Unexpected participant result count' expected='{expectedParticipantCount}' actual='{result.ParticipantCount}'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.SucceededCount != expectedParticipantCount)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Not all synthetic participants succeeded' expected='{expectedParticipantCount}' actual='{result.SucceededCount}'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.BlockingIssueCount > 0 || result.BlockingFailureCount > 0)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Blocking issue or failure was produced'. {result.ToDiagnosticString()}");
                return false;
            }

            logger.Info(
                "QA Cycle Reset Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("scope", result.Request.Scope.ToString()),
                    LogFields.Field("route", result.Request.ActiveRouteName),
                    LogFields.Field("activity", result.Request.ActiveActivityName.ToDiagnosticText()),
                    LogFields.Field("includeActiveActivity", result.Request.IncludesActiveActivity),
                    LogFields.Field("resultStatus", result.Status.ToString()),
                    LogFields.Field("participants", result.ParticipantCount),
                    LogFields.Field("participantSucceeded", result.SucceededCount),
                    LogFields.Field("participantSkipped", result.SkippedCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount)));

            if (logParticipantDetails)
            {
                logger.Debug(
                    "QA Cycle Reset Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("step", step),
                        LogFields.Field("result", result.ToDiagnosticString())));
            }

            return true;
        }



        private static async Task<bool> RunRouteTriggerStep(FrameworkLogger logger, string source, IRouteCycleResetRuntimePort routeCycleResetRuntime)
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("QA_CycleReset_RouteTrigger_Smoke");
                var trigger = gameObject.AddComponent<RouteCycleResetTrigger>();
                if (!trigger.TryBindRouteCycleResetRuntime(routeCycleResetRuntime, out string bindingIssue))
                {
                    logger.Warning($"QA Cycle Reset Trigger Smoke step failed. step='route-trigger' reason='Route Cycle Reset runtime binding was rejected' issue='{bindingIssue}'.");
                    return false;
                }
                trigger.RequestRouteCycleReset();

                if (!await WaitForTriggerCompletion(trigger))
                {
                    logger.Warning("QA Cycle Reset Trigger Smoke step failed. step='route-trigger' reason='Route trigger did not complete in the expected editor frame window'.");
                    return false;
                }

                return ValidateRouteTriggerResult(logger, trigger, source);
            }
            finally
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        private static async Task<bool> RunActivityTriggerStep(FrameworkLogger logger, string source)
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("QA_CycleReset_ActivityTrigger_Smoke");
                var trigger = gameObject.AddComponent<ActivityCycleResetTrigger>();
                trigger.RequestActivityCycleReset();

                if (!await WaitForTriggerCompletion(trigger))
                {
                    logger.Warning("QA Cycle Reset Trigger Smoke step failed. step='activity-trigger' reason='Activity trigger did not complete in the expected editor frame window'.");
                    return false;
                }

                return ValidateActivityTriggerResult(logger, trigger, source);
            }
            finally
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        private static async Task<bool> WaitForTriggerCompletion(RouteCycleResetTrigger trigger)
        {
            const int maxYields = 256;
            for (int i = 0; i < maxYields; i++)
            {
                await Task.Yield();
                if (trigger != null && !trigger.IsRequestInFlight && trigger.HasLastResult)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<bool> WaitForTriggerCompletion(ActivityCycleResetTrigger trigger)
        {
            const int maxYields = 256;
            for (int i = 0; i < maxYields; i++)
            {
                await Task.Yield();
                if (trigger != null && !trigger.IsRequestInFlight && trigger.HasLastResult)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidateRouteTriggerResult(FrameworkLogger logger, RouteCycleResetTrigger trigger, string source)
        {
            if (trigger.LastRequestFailed || !trigger.HasLastResult || !trigger.LastResult.Succeeded)
            {
                logger.Warning($"QA Cycle Reset Trigger Smoke step failed. step='route-trigger' reason='Route trigger request failed'. {trigger.LastResultSummary}");
                return false;
            }

            if (trigger.LastBlockingIssueCount > 0)
            {
                logger.Warning($"QA Cycle Reset Trigger Smoke step failed. step='route-trigger' reason='Route trigger result has blocking issues'. {trigger.LastResultSummary}");
                return false;
            }

            LogTriggerStepCompleted(logger, "route-trigger", CycleResetScope.Route, nameof(RouteCycleResetTrigger), source, trigger.LastResult, trigger.LastResultSummary);
            return true;
        }

        private static bool ValidateActivityTriggerResult(FrameworkLogger logger, ActivityCycleResetTrigger trigger, string source)
        {
            if (trigger.LastRequestFailed || !trigger.HasLastResult || !trigger.LastResult.Succeeded)
            {
                logger.Warning($"QA Cycle Reset Trigger Smoke step failed. step='activity-trigger' reason='Activity trigger request failed'. {trigger.LastResultSummary}");
                return false;
            }

            if (trigger.LastBlockingIssueCount > 0)
            {
                logger.Warning($"QA Cycle Reset Trigger Smoke step failed. step='activity-trigger' reason='Activity trigger result has blocking issues'. {trigger.LastResultSummary}");
                return false;
            }

            LogTriggerStepCompleted(logger, "activity-trigger", CycleResetScope.Activity, nameof(ActivityCycleResetTrigger), source, trigger.LastResult, trigger.LastResultSummary);
            return true;
        }

        private static void LogTriggerStepCompleted(
            FrameworkLogger logger,
            string step,
            CycleResetScope scope,
            string trigger,
            string source,
            CycleResetResult result,
            string resultSummary)
        {
            logger.Info(
                "QA Cycle Reset Trigger Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("trigger", trigger),
                    LogFields.Field("source", source),
                    LogFields.Field("resultStatus", result.Status.ToString()),
                    LogFields.Field("participants", result.ParticipantCount),
                    LogFields.Field("participantSucceeded", result.SucceededCount),
                    LogFields.Field("participantSkipped", result.SkippedCount),
                    LogFields.Field("participantFailed", result.FailedCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                    LogFields.Field("resultSummary", resultSummary)));
        }


        private static async Task<bool> RunRouteBridgeStep(FrameworkLogger logger, string source, IRouteCycleResetRuntimePort routeCycleResetRuntime)
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("QA_CycleReset_RouteBridge_Smoke");
                var trigger = gameObject.AddComponent<RouteCycleResetTrigger>();
                if (!trigger.TryBindRouteCycleResetRuntime(routeCycleResetRuntime, out string bindingIssue))
                {
                    logger.Warning($"QA Cycle Reset Bridge Smoke step failed. step='route-bridge' reason='Route Cycle Reset runtime binding was rejected' issue='{bindingIssue}'.");
                    return false;
                }
                var bridge = gameObject.AddComponent<RouteCycleResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                trigger.RequestRouteCycleReset();

                if (!await WaitForTriggerCompletion(trigger))
                {
                    logger.Warning("QA Cycle Reset Bridge Smoke step failed. step='route-bridge' reason='Route trigger did not complete in the expected editor frame window'.");
                    return false;
                }

                return ValidateRouteBridgeResult(logger, trigger, counters, source);
            }
            finally
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        private static async Task<bool> RunActivityBridgeStep(FrameworkLogger logger, string source)
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("QA_CycleReset_ActivityBridge_Smoke");
                var trigger = gameObject.AddComponent<ActivityCycleResetTrigger>();
                var bridge = gameObject.AddComponent<ActivityCycleResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                trigger.RequestActivityCycleReset();

                if (!await WaitForTriggerCompletion(trigger))
                {
                    logger.Warning("QA Cycle Reset Bridge Smoke step failed. step='activity-bridge' reason='Activity trigger did not complete in the expected editor frame window'.");
                    return false;
                }

                return ValidateActivityBridgeResult(logger, trigger, counters, source);
            }
            finally
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        private static void AttachBridgeCounters(RouteCycleResetTriggerUnityEventBridge bridge, BridgeEventCounters counters)
        {
            bridge.RequestSubmitted.AddListener(() => counters.submitted++);
            bridge.RequestSucceeded.AddListener(() => counters.succeeded++);
            bridge.RequestSucceededWithParticipants.AddListener(() => counters.succeededWithParticipants++);
            bridge.RequestSucceededNoParticipants.AddListener(() => counters.succeededNoParticipants++);
            bridge.RequestCompletedWithWarnings.AddListener(() => counters.completedWithWarnings++);
            bridge.RequestIgnored.AddListener(() => counters.ignored++);
            bridge.RequestFailed.AddListener(() => counters.failed++);
            bridge.RequestCompleted.AddListener(() => counters.completed++);
        }

        private static void AttachBridgeCounters(ActivityCycleResetTriggerUnityEventBridge bridge, BridgeEventCounters counters)
        {
            bridge.RequestSubmitted.AddListener(() => counters.submitted++);
            bridge.RequestSucceeded.AddListener(() => counters.succeeded++);
            bridge.RequestSucceededWithParticipants.AddListener(() => counters.succeededWithParticipants++);
            bridge.RequestSucceededNoParticipants.AddListener(() => counters.succeededNoParticipants++);
            bridge.RequestCompletedWithWarnings.AddListener(() => counters.completedWithWarnings++);
            bridge.RequestIgnored.AddListener(() => counters.ignored++);
            bridge.RequestFailed.AddListener(() => counters.failed++);
            bridge.RequestCompleted.AddListener(() => counters.completed++);
        }

        private static bool ValidateRouteBridgeResult(FrameworkLogger logger, RouteCycleResetTrigger trigger, BridgeEventCounters counters, string source)
        {
            if (!ValidateBridgeTriggerResult(logger, "route-bridge", trigger.LastRequestFailed, trigger.HasLastResult, trigger.LastResult, trigger.LastResultSummary))
            {
                return false;
            }

            return ValidateBridgeCounters(logger, "route-bridge", CycleResetScope.Route, nameof(RouteCycleResetTriggerUnityEventBridge), source, trigger.LastResult, trigger.LastResultSummary, counters);
        }

        private static bool ValidateActivityBridgeResult(FrameworkLogger logger, ActivityCycleResetTrigger trigger, BridgeEventCounters counters, string source)
        {
            if (!ValidateBridgeTriggerResult(logger, "activity-bridge", trigger.LastRequestFailed, trigger.HasLastResult, trigger.LastResult, trigger.LastResultSummary))
            {
                return false;
            }

            return ValidateBridgeCounters(logger, "activity-bridge", CycleResetScope.Activity, nameof(ActivityCycleResetTriggerUnityEventBridge), source, trigger.LastResult, trigger.LastResultSummary, counters);
        }

        private static bool ValidateBridgeTriggerResult(
            FrameworkLogger logger,
            string step,
            bool requestFailed,
            bool hasLastResult,
            CycleResetResult result,
            string resultSummary)
        {
            if (requestFailed || !hasLastResult || !result.Succeeded)
            {
                logger.Warning($"QA Cycle Reset Bridge Smoke step failed. step='{step}' reason='Trigger request failed'. {resultSummary}");
                return false;
            }

            if (result.BlockingIssueCount > 0)
            {
                logger.Warning($"QA Cycle Reset Bridge Smoke step failed. step='{step}' reason='Trigger result has blocking issues'. {resultSummary}");
                return false;
            }

            return true;
        }

        private static bool ValidateBridgeCounters(
            FrameworkLogger logger,
            string step,
            CycleResetScope scope,
            string bridge,
            string source,
            CycleResetResult result,
            string resultSummary,
            BridgeEventCounters counters)
        {
            if (counters.submitted != 1 || counters.succeeded != 1 || counters.completed != 1)
            {
                logger.Warning($"QA Cycle Reset Bridge Smoke step failed. step='{step}' reason='Bridge did not receive required UnityEvent callbacks' submitted='{counters.submitted}' succeeded='{counters.succeeded}' completed='{counters.completed}'. {resultSummary}");
                return false;
            }

            int expectedSucceededNoParticipants = result.Status == CycleResetStatus.SucceededNoParticipants ? 1 : 0;
            int expectedSucceededWithParticipants = result.Status == CycleResetStatus.Succeeded ? 1 : 0;
            int expectedCompletedWithWarnings = result.CompletedWithWarnings ? 1 : 0;

            if (counters.succeededNoParticipants != expectedSucceededNoParticipants ||
                counters.succeededWithParticipants != expectedSucceededWithParticipants ||
                counters.completedWithWarnings != expectedCompletedWithWarnings ||
                counters.ignored != 0 ||
                counters.failed != 0)
            {
                logger.Warning(
                    $"QA Cycle Reset Bridge Smoke step failed. step='{step}' reason='Bridge UnityEvent callback routing is inconsistent' " +
                    $"succeededNoParticipants='{counters.succeededNoParticipants}' expectedSucceededNoParticipants='{expectedSucceededNoParticipants}' " +
                    $"succeededWithParticipants='{counters.succeededWithParticipants}' expectedSucceededWithParticipants='{expectedSucceededWithParticipants}' " +
                    $"completedWithWarnings='{counters.completedWithWarnings}' expectedCompletedWithWarnings='{expectedCompletedWithWarnings}' " +
                    $"ignored='{counters.ignored}' failed='{counters.failed}'. {resultSummary}");
                return false;
            }

            logger.Info(
                "QA Cycle Reset Bridge Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("bridge", bridge),
                    LogFields.Field("source", source),
                    LogFields.Field("resultStatus", result.Status.ToString()),
                    LogFields.Field("participants", result.ParticipantCount),
                    LogFields.Field("submittedEvents", counters.submitted),
                    LogFields.Field("succeededEvents", counters.succeeded),
                    LogFields.Field("succeededWithParticipantsEvents", counters.succeededWithParticipants),
                    LogFields.Field("succeededNoParticipantsEvents", counters.succeededNoParticipants),
                    LogFields.Field("completedWithWarningsEvents", counters.completedWithWarnings),
                    LogFields.Field("ignoredEvents", counters.ignored),
                    LogFields.Field("failedEvents", counters.failed),
                    LogFields.Field("completedEvents", counters.completed),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                    LogFields.Field("resultSummary", resultSummary)));

            return true;
        }

        private static int ExpectedParticipantCountFor(CycleResetRequest request)
        {
            if (!request.IsValid)
            {
                return 0;
            }

            if (request.IsActivityReset)
            {
                return 2;
            }

            return request.IncludesActiveActivity ? 3 : 2;
        }

        private static List<ICycleResetParticipant> CreateRouteSyntheticParticipants(CycleResetRequest request, string source)
        {
            var participants = new List<ICycleResetParticipant>
            {
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Required(
                        CycleResetParticipantId.From("qa.cycle-reset.route.required"),
                        CycleResetScope.Route,
                        order: 10,
                        displayName: "QA Route Required Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.route.required")),
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Optional(
                        CycleResetParticipantId.From("qa.cycle-reset.route.optional"),
                        CycleResetScope.Route,
                        order: 20,
                        displayName: "QA Route Optional Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.route.optional"))
            };

            if (request.IncludesActiveActivity)
            {
                participants.Add(
                    SyntheticCycleResetParticipant.Success(
                        CycleResetParticipantDescriptor.Required(
                            CycleResetParticipantId.From("qa.cycle-reset.route.active-activity.required"),
                            CycleResetScope.Activity,
                            order: 10,
                            displayName: "QA Route Included Activity Cycle Reset Participant",
                            source: source,
                            reason: "qa.cycle-reset.route.active-activity.required")));
            }

            return participants;
        }

        private static List<ICycleResetParticipant> CreateActivitySyntheticParticipants(string source)
        {
            return new List<ICycleResetParticipant>
            {
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Required(
                        CycleResetParticipantId.From("qa.cycle-reset.activity.required"),
                        CycleResetScope.Activity,
                        order: 10,
                        displayName: "QA Activity Required Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.activity.required")),
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Optional(
                        CycleResetParticipantId.From("qa.cycle-reset.activity.optional"),
                        CycleResetScope.Activity,
                        order: 20,
                        displayName: "QA Activity Optional Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.activity.optional"))
            };
        }

        private sealed class BridgeEventCounters
        {
            public int submitted;
            public int succeeded;
            public int succeededWithParticipants;
            public int succeededNoParticipants;
            public int completedWithWarnings;
            public int ignored;
            public int failed;
            public int completed;
        }

        private sealed class SyntheticCycleResetParticipantSource : ICycleResetParticipantSource
        {
            private readonly string _source;

            public SyntheticCycleResetParticipantSource(string source)
            {
                _source = source;
            }

            public IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request)
            {
                if (!request.IsValid)
                {
                    return Array.Empty<ICycleResetParticipant>();
                }

                if (request.IsRouteReset)
                {
                    return CreateRouteSyntheticParticipants(request, _source);
                }

                if (request.IsActivityReset)
                {
                    return CreateActivitySyntheticParticipants(_source);
                }

                return Array.Empty<ICycleResetParticipant>();
            }
        }

        private sealed class SyntheticCycleResetParticipant : ICycleResetParticipant
        {
            private readonly CycleResetParticipantDescriptor _descriptor;
            private readonly SyntheticCycleResetParticipantMode _mode;

            private SyntheticCycleResetParticipant(
                CycleResetParticipantDescriptor descriptor,
                SyntheticCycleResetParticipantMode mode)
            {
                _descriptor = descriptor;
                _mode = mode;
            }

            public CycleResetParticipantDescriptor GetCycleResetDescriptor()
            {
                return _descriptor;
            }

            public CycleResetParticipantResult ResetCycle(CycleResetContext context)
            {
                switch (_mode)
                {
                    case SyntheticCycleResetParticipantMode.Success:
                        return CycleResetParticipantResult.Success(
                            context,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Cycle Reset participant succeeded.");

                    case SyntheticCycleResetParticipantMode.SkippedOptional:
                        return CycleResetParticipantResult.Skipped(
                            context,
                            CycleResetParticipantResultStatus.SkippedOptional,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Cycle Reset participant skipped as optional.");

                    case SyntheticCycleResetParticipantMode.Failure:
                        return CycleResetParticipantResult.Failure(
                            context,
                            1,
                            _descriptor.Source,
                            _descriptor.Reason,
                            "Synthetic Cycle Reset participant failed.");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(_mode), _mode, "Unsupported synthetic Cycle Reset participant mode.");
                }
            }

            public static SyntheticCycleResetParticipant Success(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.Success);
            }

            public static SyntheticCycleResetParticipant SkippedOptional(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.SkippedOptional);
            }

            public static SyntheticCycleResetParticipant Failure(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.Failure);
            }
        }

        private enum SyntheticCycleResetParticipantMode
        {
            Success = 0,
            SkippedOptional = 1,
            Failure = 2
        }
    }
}
#endif
