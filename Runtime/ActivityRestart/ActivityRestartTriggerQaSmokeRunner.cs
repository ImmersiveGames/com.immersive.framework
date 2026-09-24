using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for preview.12E ActivityRestartTrigger integration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12E ActivityRestartTrigger integration smoke.")]
    internal static class ActivityRestartTriggerQaSmokeRunner
    {
        internal const string SmokeName = "Activity Restart Trigger Reset Executor Synthetic Smoke";

        internal static async Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ActivityRestartTriggerQaSmokeRunner));
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            bool restartPassed = await ValidateRestartTriggerAsync(runtimeHost, logger, normalizedSource);

            runtimeHost.ResetRegistry.CleanupStaleOwners();
            bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;

            bool passed = restartPassed && cleanupPassed;
            logger.Info(
                "QA Activity Restart Trigger Reset Executor Synthetic Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("restartTrigger", restartPassed),
                    LogFields.Field("cleanup", cleanupPassed),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
            return passed;
        }

        private static async Task<bool> ValidateRestartTriggerAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "activity-restart-current-route-and-activity-subjects";
            QaSubjectFixture fixture = null;
            GameObject triggerObject = null;
            try
            {
                fixture = CreateSubjectFixture(
                    "QA 12E Activity Restart Subject",
                    "qa.reset.12e.activity.subject",
                    ResetSubjectScope.Activity,
                    source,
                    "qa.reset.12e.subject.setup");
                if (!fixture.Adapter.RegisterWithCurrentHost("qa.reset.12e.subject.register"))
                {
                    return LogStep(logger, step, false, "register-failed", runtimeHost, "adapter registration failed");
                }

                triggerObject = new GameObject("QA 12E Activity Restart Trigger");
                var trigger = triggerObject.AddComponent<ActivityRestartTrigger>();
                IActivityRestartRuntimePort activityRestartRuntime = runtimeHost;
                if (!trigger.TryBindActivityRestartRuntime(activityRestartRuntime, out string bindingIssue))
                {
                    return LogStep(logger, step, false, "binding-failed", runtimeHost, bindingIssue);
                }
                trigger.ConfigureForQa(
                    qaTargetActivity: null,
                    qaUseCurrentActivityWhenTargetMissing: true,
                    qaRequireTargetActivityIsCurrent: true,
                    qaReason: "qa.reset.12e.restart",
                    qaSelectionMode: ResetSelectionMode.CurrentRouteAndActivitySubjects,
                    qaExplicitSubjects: Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                fixture.Target.localPosition = new Vector3(21f, 22f, 23f);
                ActivityRestartResult result = await trigger.RequestActivityRestartAsync();
                bool positionReset = Approximately(fixture.Target.localPosition, fixture.BaselinePosition);
                bool passed = result != null
                    && result.Succeeded
                    && trigger.LastRequestSucceeded
                    && result.HasResetExecutionResult
                    && result.ResetExecutionResult.Status == ResetExecutionStatus.Succeeded
                    && result.ResetSubjectCount >= 1
                    && result.ResetSubjectSucceededCount >= 1
                    && result.ResetParticipantCount >= 1
                    && result.ResetParticipantSucceededCount >= 1
                    && string.Equals(result.ClearStatus, "Succeeded", StringComparison.Ordinal)
                    && string.Equals(result.ReenterStatus, "Succeeded", StringComparison.Ordinal)
                    && positionReset;

                return LogStep(
                    logger,
                    step,
                    passed,
                    result != null ? result.Status.ToString() : "NullResult",
                    runtimeHost,
                    result != null
                        ? $"resetStatus='{result.ResetStatus}' resetSubjects='{result.ResetSubjectCount}' resetSubjectSucceeded='{result.ResetSubjectSucceededCount}' resetParticipants='{result.ResetParticipantCount}' resetParticipantSucceeded='{result.ResetParticipantSucceededCount}' clearStatus='{result.ClearStatus}' reenterStatus='{result.ReenterStatus}' positionReset='{positionReset}'"
                        : "result='null'");
            }
            finally
            {
                DestroyObject(triggerObject);
                fixture?.Dispose();
            }
        }

        private static QaSubjectFixture CreateSubjectFixture(
            string objectName,
            string subjectId,
            ResetSubjectScope scope,
            string source,
            string reason)
        {
            var go = new GameObject(objectName);
            go.SetActive(false);
            var participant = go.AddComponent<UnityTransformResetParticipant>();
            participant.ConfigureForQa(
                "transform",
                ResetParticipantRequiredness.Required,
                0,
                "Transform",
                source,
                reason);
            var adapter = go.AddComponent<UnityResetSubjectAdapter>();
            adapter.ConfigureForQa(
                qaRegisterOnEnable: false,
                qaUnregisterOnDisable: true,
                qaRetryUntilRuntimeAvailable: false,
                UnityResetSubjectIdGenerationMode.AuthoredStableId,
                subjectId,
                string.Empty,
                scope,
                objectName,
                $"QA:{subjectId}",
                UnityResetParticipantDiscoveryMode.Children,
                qaIncludeInactiveParticipants: true);
            go.SetActive(true);
            go.transform.localPosition = Vector3.zero;
            participant.ConfigureTransformForQa(go.transform, qaCaptureOnEnable: false, qaResetPosition: true, qaResetRotation: true, qaResetScale: true);
            return new QaSubjectFixture(go, go.transform, adapter, Vector3.zero);
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return Vector3.SqrMagnitude(left - right) <= 0.0001f;
        }

        private static bool LogStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            string status,
            FrameworkRuntimeHost runtimeHost,
            string detail)
        {
            logger.Info(
                "QA Activity Restart Trigger Reset Executor Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("restartStatus", status),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString()),
                    LogFields.Field("detail", detail ?? string.Empty)));
            return passed;
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private sealed class QaSubjectFixture : IDisposable
        {
            internal QaSubjectFixture(
                GameObject gameObject,
                Transform target,
                UnityResetSubjectAdapter adapter,
                Vector3 baselinePosition)
            {
                GameObject = gameObject;
                Target = target;
                Adapter = adapter;
                BaselinePosition = baselinePosition;
            }

            internal GameObject GameObject { get; }

            internal Transform Target { get; }

            internal UnityResetSubjectAdapter Adapter { get; }

            internal Vector3 BaselinePosition { get; }

            public void Dispose()
            {
                if (Adapter != null)
                {
                    Adapter.ClearRegistration("qa.reset.12e.cleanup");
                }

                DestroyObject(GameObject);
            }
        }
    }
}
#endif
