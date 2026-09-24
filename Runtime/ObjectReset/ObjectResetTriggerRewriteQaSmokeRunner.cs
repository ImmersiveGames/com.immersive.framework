using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for preview.12D trigger rewrite.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12D ObjectResetTrigger/ObjectResetGroupTrigger rewrite smoke.")]
    internal static class ObjectResetTriggerRewriteQaSmokeRunner
    {
        internal const string SmokeName = "Object Reset Trigger Rewrite Synthetic Smoke";
        internal const string ExpectedFailureSmokeName = "Object Reset Trigger Rewrite Expected Failure Smoke";

        internal static async Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ObjectResetTriggerRewriteQaSmokeRunner));
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            bool singlePassed = await ValidateSingleTriggerAsync(runtimeHost, logger, normalizedSource);
            bool explicitGroupPassed = await ValidateExplicitGroupTriggerAsync(runtimeHost, logger, normalizedSource);
            bool scopedGroupPassed = await ValidateScopedGroupTriggerAsync(runtimeHost, logger, normalizedSource);

            runtimeHost.ResetRegistry.CleanupStaleOwners();
            bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;

            bool passed = singlePassed
                && explicitGroupPassed
                && scopedGroupPassed
                && cleanupPassed;

            logger.Info(
                "QA Object Reset Trigger Rewrite Synthetic Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("singleTrigger", singlePassed),
                    LogFields.Field("explicitGroup", explicitGroupPassed),
                    LogFields.Field("scopedGroup", scopedGroupPassed),
                    LogFields.Field("cleanup", cleanupPassed),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
            return passed;
        }

        internal static async Task<bool> RunExpectedFailureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ObjectResetTriggerRewriteQaSmokeRunner));
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            bool blockedEmptyPassed = await ValidateBlockedEmptySelectionAsync(runtimeHost, logger, normalizedSource);

            runtimeHost.ResetRegistry.CleanupStaleOwners();
            bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;
            bool passed = blockedEmptyPassed && cleanupPassed;

            logger.Info(
                "QA Object Reset Trigger Rewrite Expected Failure Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("blockedEmptySelection", blockedEmptyPassed),
                    LogFields.Field("cleanup", cleanupPassed),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
            return passed;
        }

        private static async Task<bool> ValidateSingleTriggerAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "single-trigger-explicit-subject";
            QaSubjectFixture fixture = null;
            GameObject triggerObject = null;
            try
            {
                fixture = CreateSubjectFixture(
                    "QA 12D Single Subject",
                    "qa.reset.12d.single",
                    ResetSubjectScope.Activity,
                    source,
                    "qa.reset.12d.single.setup");
                if (!fixture.Adapter.RegisterWithCurrentHost("qa.reset.12d.single.register"))
                {
                    return LogStep(logger, step, false, "register-failed", runtimeHost, "adapter registration failed");
                }

                triggerObject = new GameObject("QA 12D Object Reset Trigger");
                var trigger = triggerObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    fixture.Adapter,
                    string.Empty,
                    "qa.reset.12d.single.trigger",
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true);

                fixture.Target.localPosition = new Vector3(7f, 8f, 9f);
                trigger.RequestObjectReset();
                bool completed = await WaitForTriggerAsync(trigger);
                bool positionReset = Approximately(fixture.Target.localPosition, fixture.BaselinePosition);
                bool passed = completed
                    && trigger.LastRequestSucceeded
                    && trigger.LastExecutionStatus == ResetExecutionStatus.Succeeded
                    && trigger.LastParticipantCount == 1
                    && trigger.LastSucceededParticipantCount == 1
                    && positionReset;

                return LogStep(
                    logger,
                    step,
                    passed,
                    trigger.LastExecutionStatus.ToString(),
                    runtimeHost,
                    $"completed='{completed}' positionReset='{positionReset}' participants='{trigger.LastParticipantCount}' succeeded='{trigger.LastSucceededParticipantCount}'");
            }
            finally
            {
                DestroyObject(triggerObject);
                fixture?.Dispose();
            }
        }

        private static async Task<bool> ValidateExplicitGroupTriggerAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "group-trigger-explicit-subjects";
            QaSubjectFixture first = null;
            QaSubjectFixture second = null;
            GameObject triggerObject = null;
            try
            {
                first = CreateSubjectFixture("QA 12D Explicit First", "qa.reset.12d.explicit.first", ResetSubjectScope.Activity, source, "qa.reset.12d.explicit.first.setup");
                second = CreateSubjectFixture("QA 12D Explicit Second", "qa.reset.12d.explicit.second", ResetSubjectScope.Activity, source, "qa.reset.12d.explicit.second.setup");
                bool registered = first.Adapter.RegisterWithCurrentHost("qa.reset.12d.explicit.first.register")
                    && second.Adapter.RegisterWithCurrentHost("qa.reset.12d.explicit.second.register");
                if (!registered)
                {
                    return LogStep(logger, step, false, "register-failed", runtimeHost, "adapter registration failed");
                }

                triggerObject = new GameObject("QA 12D Object Reset Group Trigger Explicit");
                var trigger = triggerObject.AddComponent<ObjectResetGroupTrigger>();
                trigger.ConfigureForQa(
                    "qa.reset.12d.group.explicit",
                    "qa.reset.12d.group.explicit.trigger",
                    ResetSelectionMode.ExplicitSubjects,
                    new[]
                    {
                        CreateReference(first.Adapter),
                        CreateReference(second.Adapter)
                    },
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                first.Target.localPosition = new Vector3(1f, 2f, 3f);
                second.Target.localPosition = new Vector3(4f, 5f, 6f);
                ResetExecutionResult groupResult = await trigger.RequestObjectResetGroupAsync();
                bool firstReset = Approximately(first.Target.localPosition, first.BaselinePosition);
                bool secondReset = Approximately(second.Target.localPosition, second.BaselinePosition);
                bool passed = groupResult.Status != ResetExecutionStatus.Unknown
                    && trigger.LastRequestSucceeded
                    && trigger.LastExecutionStatus == ResetExecutionStatus.Succeeded
                    && trigger.LastTargetCount == 2
                    && trigger.LastSucceededTargetCount == 2
                    && trigger.LastParticipantCount == 2
                    && trigger.LastSucceededParticipantCount == 2
                    && firstReset
                    && secondReset;

                return LogStep(
                    logger,
                    step,
                    passed,
                    trigger.LastExecutionStatus.ToString(),
                    runtimeHost,
                    $"firstReset='{firstReset}' secondReset='{secondReset}' subjects='{trigger.LastTargetCount}' participants='{trigger.LastParticipantCount}'");
            }
            finally
            {
                DestroyObject(triggerObject);
                first?.Dispose();
                second?.Dispose();
            }
        }

        private static async Task<bool> ValidateScopedGroupTriggerAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "group-trigger-current-route-and-activity-subjects";
            QaSubjectFixture routeSubject = null;
            QaSubjectFixture activitySubject = null;
            GameObject triggerObject = null;
            try
            {
                routeSubject = CreateSubjectFixture("QA 12D Route Subject", "qa.reset.12d.scoped.route", ResetSubjectScope.Route, source, "qa.reset.12d.scoped.route.setup");
                activitySubject = CreateSubjectFixture("QA 12D Activity Subject", "qa.reset.12d.scoped.activity", ResetSubjectScope.Activity, source, "qa.reset.12d.scoped.activity.setup");
                bool registered = routeSubject.Adapter.RegisterWithCurrentHost("qa.reset.12d.scoped.route.register")
                    && activitySubject.Adapter.RegisterWithCurrentHost("qa.reset.12d.scoped.activity.register");
                if (!registered)
                {
                    return LogStep(logger, step, false, "register-failed", runtimeHost, "adapter registration failed");
                }

                triggerObject = new GameObject("QA 12D Object Reset Group Trigger Scoped");
                var trigger = triggerObject.AddComponent<ObjectResetGroupTrigger>();
                trigger.ConfigureForQa(
                    "qa.reset.12d.group.scoped",
                    "qa.reset.12d.group.scoped.trigger",
                    ResetSelectionMode.CurrentRouteAndActivitySubjects,
                    Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                routeSubject.Target.localPosition = new Vector3(11f, 12f, 13f);
                activitySubject.Target.localPosition = new Vector3(14f, 15f, 16f);
                await trigger.RequestObjectResetGroupAsync();
                bool routeReset = Approximately(routeSubject.Target.localPosition, routeSubject.BaselinePosition);
                bool activityReset = Approximately(activitySubject.Target.localPosition, activitySubject.BaselinePosition);
                bool passed = trigger.LastRequestSucceeded
                    && trigger.LastExecutionStatus == ResetExecutionStatus.Succeeded
                    && trigger.LastTargetCount >= 2
                    && trigger.LastSucceededTargetCount >= 2
                    && routeReset
                    && activityReset;

                return LogStep(
                    logger,
                    step,
                    passed,
                    trigger.LastExecutionStatus.ToString(),
                    runtimeHost,
                    $"routeReset='{routeReset}' activityReset='{activityReset}' subjects='{trigger.LastTargetCount}' participants='{trigger.LastParticipantCount}'");
            }
            finally
            {
                DestroyObject(triggerObject);
                routeSubject?.Dispose();
                activitySubject?.Dispose();
            }
        }

        private static async Task<bool> ValidateBlockedEmptySelectionAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "group-trigger-empty-explicit-blocked";
            GameObject triggerObject = null;
            try
            {
                triggerObject = new GameObject("QA 12D Object Reset Group Trigger Empty");
                var trigger = triggerObject.AddComponent<ObjectResetGroupTrigger>();
                trigger.ConfigureForQa(
                    "qa.reset.12d.group.empty",
                    "qa.reset.12d.group.empty.trigger",
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                await trigger.RequestObjectResetGroupAsync();
                bool passed = trigger.LastRequestFailed
                    && trigger.LastExecutionStatus == ResetExecutionStatus.FailedNoSubjects
                    && trigger.LastBlockingIssueCount == 1;

                return LogStep(
                    logger,
                    step,
                    passed,
                    trigger.LastExecutionStatus.ToString(),
                    runtimeHost,
                    $"blockingIssues='{trigger.LastBlockingIssueCount}' subjects='{trigger.LastTargetCount}'");
            }
            finally
            {
                DestroyObject(triggerObject);
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

        private static ResetSubjectReference CreateReference(UnityResetSubjectAdapter adapter)
        {
            var reference = new ResetSubjectReference();
            reference.ConfigureForQa(adapter, string.Empty);
            return reference;
        }

        private static async Task<bool> WaitForTriggerAsync(ObjectResetTrigger trigger)
        {
            for (int i = 0; i < 120; i++)
            {
                if (trigger != null && !trigger.IsRequestInFlight && trigger.HasLastResult)
                {
                    return true;
                }

                await Task.Yield();
            }

            return trigger != null && !trigger.IsRequestInFlight && trigger.HasLastResult;
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
                "QA Object Reset Trigger Rewrite Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("executionStatus", status),
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
                    Adapter.ClearRegistration("qa.reset.12d.cleanup");
                }

                DestroyObject(GameObject);
            }
        }
    }
}
#endif
