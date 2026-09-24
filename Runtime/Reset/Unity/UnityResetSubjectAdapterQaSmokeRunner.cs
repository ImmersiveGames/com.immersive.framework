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
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for preview.12B Unity ResetSubject adapters and participants.
    /// It validates Unity authoring registration without ObjectEntryDeclaration and without legacy runtime participation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12B Unity reset subject adapter synthetic smoke.")]
    internal static class UnityResetSubjectAdapterQaSmokeRunner
    {
        internal const string SmokeName = "Unity Reset Subject Adapter Synthetic Smoke";

        internal static async Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityResetSubjectAdapterQaSmokeRunner));
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            bool sceneAuthoredPassed = ValidateSceneAuthoredSubjectWithParticipants(runtimeHost, logger, normalizedSource);
            bool resettableComponentPassed = await ValidateUnityResettableComponentParticipantAsync(runtimeHost, logger, normalizedSource);
            bool runtimeInstancesPassed = await ValidateRuntimeInstanceSubjectsAsync(runtimeHost, logger, normalizedSource);
            bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;

            if (!cleanupPassed)
            {
                runtimeHost.ResetRegistry.CleanupStaleOwners();
                cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                    && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;
            }

            bool passed = sceneAuthoredPassed && resettableComponentPassed && runtimeInstancesPassed && cleanupPassed;
            logger.Info(
                "QA Unity Reset Subject Adapter Synthetic Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("sceneAuthored", sceneAuthoredPassed),
                    LogFields.Field("resettableComponent", resettableComponentPassed),
                    LogFields.Field("runtimeInstances", runtimeInstancesPassed),
                    LogFields.Field("cleanup", cleanupPassed),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
            return passed;
        }

        private static bool ValidateSceneAuthoredSubjectWithParticipants(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            GameObject root = null;
            GameObject activeTarget = null;
            UnityResetSubjectAdapter adapter = null;
            try
            {
                root = new GameObject("QA_UnityReset_SceneSubject");
                root.SetActive(false);

                adapter = root.AddComponent<UnityResetSubjectAdapter>();
                var transformParticipant = root.AddComponent<UnityTransformResetParticipant>();
                transformParticipant.ConfigureForQa(
                    "transform",
                    ResetParticipantRequiredness.Required,
                    0,
                    "Transform",
                    nameof(UnityResetSubjectAdapterQaSmokeRunner),
                    "qa.unity-reset.scene.transform");
                transformParticipant.ConfigureTransformForQa(root.transform, false, true, true, true);

                activeTarget = new GameObject("QA_UnityReset_ActiveTarget");
                activeTarget.transform.SetParent(root.transform, false);
                var activeParticipant = activeTarget.AddComponent<UnityGameObjectActiveResetParticipant>();
                activeParticipant.ConfigureForQa(
                    "active-state",
                    ResetParticipantRequiredness.Optional,
                    10,
                    "Active State",
                    nameof(UnityResetSubjectAdapterQaSmokeRunner),
                    "qa.unity-reset.scene.active");
                activeParticipant.ConfigureActiveStateForQa(activeTarget, false, true);

                adapter.ConfigureForQa(
                    false,
                    true,
                    false,
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.reset.unity.scene.subject",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    "QA Unity Scene Subject",
                    "Smoke:UnityResetSubjectAdapter:SceneAuthored",
                    UnityResetParticipantDiscoveryMode.Children,
                    true);

                root.SetActive(true);
                bool registered = adapter.RegisterWithCurrentHost("qa.unity-reset.scene-register");
                var participants = runtimeHost.ResetRegistry.GetParticipants(adapter.SubjectHandle);

                root.transform.localPosition = new Vector3(12f, 3f, -4f);
                root.transform.localEulerAngles = new Vector3(0f, 45f, 0f);
                root.transform.localScale = new Vector3(2f, 2f, 2f);
                activeTarget.SetActive(false);

                bool transformResetPassed = false;
                bool activeResetPassed = false;
                for (int i = 0; i < participants.Count; i++)
                {
                    ResetParticipantDescriptor descriptor = participants[i];
                    var context = new ResetContext(adapter.Subject, descriptor, source, "qa.unity-reset.scene-participant-reset");
                    if (descriptor.ParticipantId == ResetParticipantId.From("transform"))
                    {
                        var result = transformParticipant.Reset(context);
                        transformResetPassed = result.Succeeded
                            && root.transform.localPosition == Vector3.zero
                            && root.transform.localEulerAngles == Vector3.zero
                            && root.transform.localScale == Vector3.one;
                    }
                    else if (descriptor.ParticipantId == ResetParticipantId.From("active-state"))
                    {
                        var result = activeParticipant.Reset(context);
                        activeResetPassed = result.Succeeded && activeTarget.activeSelf;
                    }
                }

                bool subjectFound = runtimeHost.ResetRegistry.TryGetSubject(adapter.SubjectId, out ResetSubject foundSubject)
                    && foundSubject.SubjectId == adapter.SubjectId;
                bool passed = registered
                    && adapter.IsRegistered
                    && adapter.Subject.Scope == ResetSubjectScope.Activity
                    && adapter.Subject.Origin == ResetSubjectOrigin.SceneAuthored
                    && adapter.RegisteredParticipantCount == 2
                    && participants.Count == 2
                    && subjectFound
                    && transformResetPassed
                    && activeResetPassed;

                LogUnityStep(
                    logger,
                    "scene-authored-adapter",
                    passed,
                    runtimeHost,
                    registered ? "Registered" : "Rejected",
                    $"subjectId='{adapter.SubjectId.StableText}' participants='{participants.Count}' transformReset='{transformResetPassed}' activeReset='{activeResetPassed}'");

                adapter.ClearRegistration("qa.unity-reset.scene-cleanup");
                return passed;
            }
            finally
            {
                if (adapter != null && adapter.IsRegistered)
                {
                    adapter.ClearRegistration("qa.unity-reset.scene-finally-cleanup");
                }

                DestroyIfNeeded(root);
            }
        }


        private static async Task<bool> ValidateUnityResettableComponentParticipantAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            GameObject root = null;
            UnityResetSubjectAdapter adapter = null;
            QaUnityResettableComponent resettable = null;
            try
            {
                root = new GameObject("QA_UnityReset_ResettableComponentSubject");
                root.SetActive(false);

                adapter = root.AddComponent<UnityResetSubjectAdapter>();
                resettable = root.AddComponent<QaUnityResettableComponent>();

                adapter.ConfigureForQa(
                    false,
                    true,
                    false,
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.reset.unity.resettable-component.subject",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    "QA Unity Resettable Component Subject",
                    "Smoke:UnityResetSubjectAdapter:ResettableComponent",
                    UnityResetParticipantDiscoveryMode.SameGameObject,
                    true,
                    true);

                root.SetActive(true);
                bool registered = adapter.RegisterWithCurrentHost("qa.unity-reset.resettable-component.register");
                IReadOnlyList<ResetParticipantDescriptor> participants = runtimeHost.ResetRegistry.GetParticipants(adapter.SubjectHandle);
                bool participantDescriptorFound = participants.Count == 1
                    && participants[0].ParticipantId == ResetParticipantId.From(QaUnityResettableComponent.ParticipantIdText)
                    && participants[0].Requiredness == ResetParticipantRequiredness.Required
                    && participants[0].Order == QaUnityResettableComponent.ExpectedOrder;

                var executor = new ResetExecutor(runtimeHost.ResetRegistry);
                ResetExecutionResult execution = await executor.ExecuteAsync(
                    ResetExecutionRequest.ForSingleSubject(
                        adapter.SubjectId,
                        allowNoParticipants: false,
                        source,
                        "qa.unity-reset.resettable-component.execute"));

                bool resetExecuted = resettable.ResetCalls == 1
                    && string.Equals(resettable.LastReason, "qa.unity-reset.resettable-component.execute", StringComparison.Ordinal)
                    && resettable.LastSubjectId == adapter.SubjectId
                    && resettable.LastParticipantId == ResetParticipantId.From(QaUnityResettableComponent.ParticipantIdText);

                bool passed = registered
                    && adapter.IsRegistered
                    && adapter.RegisteredParticipantCount == 1
                    && participantDescriptorFound
                    && execution.Succeeded
                    && execution.SubjectCount == 1
                    && execution.ParticipantCount == 1
                    && execution.ParticipantSucceeded == 1
                    && resetExecuted;

                LogUnityStep(
                    logger,
                    "interface-resettable-component",
                    passed,
                    runtimeHost,
                    registered ? execution.Status.ToString() : "Rejected",
                    $"participants='{participants.Count}' resetCalls='{resettable.ResetCalls}' executionStatus='{execution.Status}' participantSucceeded='{execution.ParticipantSucceeded}' participantId='{QaUnityResettableComponent.ParticipantIdText}'");

                adapter.ClearRegistration("qa.unity-reset.resettable-component.cleanup");
                await Task.Yield();
                return passed;
            }
            finally
            {
                if (adapter != null && adapter.IsRegistered)
                {
                    adapter.ClearRegistration("qa.unity-reset.resettable-component.finally-cleanup");
                }

                DestroyIfNeeded(root);
            }
        }

        private static async Task<bool> ValidateRuntimeInstanceSubjectsAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            GameObject first = null;
            GameObject second = null;
            UnityResetSubjectAdapter firstAdapter = null;
            UnityResetSubjectAdapter secondAdapter = null;
            try
            {
                first = CreateRuntimeSubjectObject("QA_UnityReset_RuntimeSubject_A", "qa.reset.unity.runtime.box");
                second = CreateRuntimeSubjectObject("QA_UnityReset_RuntimeSubject_B", "qa.reset.unity.runtime.box");

                first.SetActive(true);
                second.SetActive(true);

                firstAdapter = first.GetComponent<UnityResetSubjectAdapter>();
                secondAdapter = second.GetComponent<UnityResetSubjectAdapter>();
                bool firstRegistered = firstAdapter.RegisterWithCurrentHost("qa.unity-reset.runtime-first");
                bool secondRegistered = secondAdapter.RegisterWithCurrentHost("qa.unity-reset.runtime-second");

                string firstId = firstAdapter.SubjectId.StableText;
                string secondId = secondAdapter.SubjectId.StableText;
                bool idsAreDistinct = !string.Equals(firstId, secondId, StringComparison.Ordinal)
                    && firstId.StartsWith("qa.reset.unity.runtime.box#", StringComparison.Ordinal)
                    && secondId.StartsWith("qa.reset.unity.runtime.box#", StringComparison.Ordinal);
                bool bothHaveParticipants = firstAdapter.RegisteredParticipantCount == 1
                    && secondAdapter.RegisteredParticipantCount == 1;

                first.SetActive(false);
                await Task.Yield();
                runtimeHost.ResetRegistry.CleanupStaleOwners();

                bool firstRemoved = !firstAdapter.IsRegistered
                    && runtimeHost.ResetRegistry.GetParticipants(firstAdapter.SubjectHandle).Count == 0;
                bool secondStillRegistered = secondAdapter.IsRegistered
                    && runtimeHost.ResetRegistry.TryGetSubject(secondAdapter.SubjectId, out _)
                    && runtimeHost.ResetRegistry.GetParticipants(secondAdapter.SubjectHandle).Count == 1;

                secondAdapter.ClearRegistration("qa.unity-reset.runtime-second-cleanup");
                await Task.Yield();

                bool secondRemoved = !secondAdapter.IsRegistered
                    && runtimeHost.ResetRegistry.GetParticipants(secondAdapter.SubjectHandle).Count == 0;

                bool passed = firstRegistered
                    && secondRegistered
                    && idsAreDistinct
                    && bothHaveParticipants
                    && firstRemoved
                    && secondStillRegistered
                    && secondRemoved;

                LogUnityStep(
                    logger,
                    "runtime-instance-adapters",
                    passed,
                    runtimeHost,
                    $"{(firstRegistered ? "Registered" : "Rejected")}/{(secondRegistered ? "Registered" : "Rejected")}",
                    $"first='{firstId}' second='{secondId}' distinct='{idsAreDistinct}' firstRemoved='{firstRemoved}' secondStillRegistered='{secondStillRegistered}' secondRemoved='{secondRemoved}'");

                return passed;
            }
            finally
            {
                if (firstAdapter != null && firstAdapter.IsRegistered)
                {
                    firstAdapter.ClearRegistration("qa.unity-reset.runtime-first-finally-cleanup");
                }

                if (secondAdapter != null && secondAdapter.IsRegistered)
                {
                    secondAdapter.ClearRegistration("qa.unity-reset.runtime-second-finally-cleanup");
                }

                DestroyIfNeeded(first);
                DestroyIfNeeded(second);
            }
        }

        private static GameObject CreateRuntimeSubjectObject(string name, string prefix)
        {
            var root = new GameObject(name);
            root.SetActive(false);

            var adapter = root.AddComponent<UnityResetSubjectAdapter>();
            var participant = root.AddComponent<UnityTransformResetParticipant>();
            participant.ConfigureForQa(
                "transform",
                ResetParticipantRequiredness.Required,
                0,
                "Transform",
                nameof(UnityResetSubjectAdapterQaSmokeRunner),
                "qa.unity-reset.runtime.transform");
            participant.ConfigureTransformForQa(root.transform, false, true, true, true);

            adapter.ConfigureForQa(
                false,
                true,
                false,
                UnityResetSubjectIdGenerationMode.RuntimeInstanceId,
                string.Empty,
                prefix,
                ResetSubjectScope.Runtime,
                name,
                "Smoke:UnityResetSubjectAdapter:RuntimeInstance",
                UnityResetParticipantDiscoveryMode.Children,
                true);
            return root;
        }


        private sealed class QaUnityResettableComponent : MonoBehaviour, IUnityResettable, IUnityResettableMetadata
        {
            internal const string ParticipantIdText = "qa.reset.unity.resettable-component";
            internal const int ExpectedOrder = 25;

            public string ResetParticipantId => ParticipantIdText;

            public ResetParticipantRequiredness ResetRequiredness => ResetParticipantRequiredness.Required;

            public int ResetOrder => ExpectedOrder;

            public string ResetDisplayName => "QA Unity Resettable Component";

            public string ResetSource => nameof(QaUnityResettableComponent);

            public string ResetReason => "qa.unity-reset.resettable-component";

            public int ResetCalls { get; private set; }

            public string LastReason { get; private set; }

            public ResetSubjectId LastSubjectId { get; private set; }

            public ResetParticipantId LastParticipantId { get; private set; }

            public ResetParticipantResult Reset(ResetContext context)
            {
                ResetCalls++;
                LastReason = context.Reason;
                LastSubjectId = context.Subject.SubjectId;
                LastParticipantId = context.Participant.ParticipantId;

                return ResetParticipantResult.CreateSucceeded(
                    context.Participant,
                    nameof(QaUnityResettableComponent),
                    context.Reason,
                    $"QA unity resettable component executed. resetCalls='{ResetCalls}'.");
            }
        }

        private static void LogUnityStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            FrameworkRuntimeHost runtimeHost,
            string operationStatus,
            string detail)
        {
            logger.Info(
                "QA Unity Reset Subject Adapter Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString()),
                    LogFields.Field("operationStatus", operationStatus),
                    LogFields.Field("detail", detail)));
        }

        private static void DestroyIfNeeded(Object target)
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
    }
}
#endif
