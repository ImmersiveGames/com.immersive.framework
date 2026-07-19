using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.ObjectReset;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for preview.12F runtime-prefab reset behavior.
    /// It creates an inactive prototype and instantiates runtime clones to validate RuntimeInstanceId, group reset,
    /// unregister-on-destroy and ActivityRestart integration without requiring ObjectEntryDeclaration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12F runtime prefab reset smoke over UnityResetSubjectAdapter + ResetExecutor.")]
    internal static class UnityResetRuntimePrefabQaSmokeRunner
    {
        internal const string SmokeName = "Runtime Prefab Reset Synthetic Smoke";

        private const string RuntimePrefix = "qa.reset.12f.runtime.box";

        internal static async Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityResetRuntimePrefabQaSmokeRunner));
            runtimeHost.ResetRegistry.CleanupStaleOwners();
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            RuntimePrefabFixture fixture = null;
            try
            {
                fixture = RuntimePrefabFixture.Create(RuntimePrefix);

                bool spawnPassed = fixture.SpawnAndRegister(runtimeHost, logger, normalizedSource);
                bool groupResetPassed = spawnPassed && await ValidateRuntimeGroupResetAsync(fixture, runtimeHost, logger, normalizedSource);
                bool destroyPassed = groupResetPassed && await ValidateDestroyUnregisterAndSecondResetAsync(fixture, runtimeHost, logger, normalizedSource);
                bool restartPassed = destroyPassed && await ValidateActivityRestartWithRemainingRuntimeSubjectAsync(fixture, runtimeHost, logger, normalizedSource);

                fixture.Dispose();
                runtimeHost.ResetRegistry.CleanupStaleOwners();
                bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                    && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;

                bool passed = spawnPassed && groupResetPassed && destroyPassed && restartPassed && cleanupPassed;
                logger.Info(
                    "QA Runtime Prefab Reset Synthetic Smoke completed.",
                    LogFields.Of(
                        LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("spawn", spawnPassed),
                        LogFields.Field("runtimeGroupReset", groupResetPassed),
                        LogFields.Field("destroyUnregister", destroyPassed),
                        LogFields.Field("activityRestart", restartPassed),
                        LogFields.Field("cleanup", cleanupPassed),
                        LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                        LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
                return passed;
            }
            finally
            {
                fixture?.Dispose();
                runtimeHost.ResetRegistry.CleanupStaleOwners();
            }
        }

        private static async Task<bool> ValidateRuntimeGroupResetAsync(
            RuntimePrefabFixture fixture,
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "runtime-prefab-group-reset";
            GameObject triggerObject = null;
            try
            {
                fixture.First.Target.localPosition = new Vector3(10f, 0f, 0f);
                fixture.Second.Target.localPosition = new Vector3(-10f, 0f, 0f);

                triggerObject = new GameObject("QA_12F_RuntimeOnly_GroupTrigger");
                var trigger = triggerObject.AddComponent<ObjectResetGroupTrigger>();
                trigger.ConfigureForQa(
                    "qa.reset.12f.runtime-group",
                    "qa.reset.12f.runtime-group.reset",
                    ResetSelectionMode.RuntimeOnlySubjects,
                    Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                ResetExecutionResult result = await trigger.RequestObjectResetGroupAsync();
                bool firstReset = Approximately(fixture.First.Target.localPosition, fixture.First.BaselinePosition);
                bool secondReset = Approximately(fixture.Second.Target.localPosition, fixture.Second.BaselinePosition);
                bool passed = FrameworkEnumValidation.IsDefinedAndNot(result.Status, ResetExecutionStatus.Unknown)
                    && result.Succeeded
                    && trigger.LastRequestSucceeded
                    && trigger.LastExecutionStatus == ResetExecutionStatus.Succeeded
                    && trigger.LastTargetCount == 2
                    && trigger.LastSucceededTargetCount == 2
                    && trigger.LastFailedTargetCount == 0
                    && trigger.LastParticipantCount == 2
                    && trigger.LastSucceededParticipantCount == 2
                    && firstReset
                    && secondReset;

                LogStep(
                    logger,
                    step,
                    passed,
                    runtimeHost,
                    result.Status.ToString(),
                    $"firstReset='{firstReset}' secondReset='{secondReset}' subjects='{trigger.LastTargetCount}' participants='{trigger.LastParticipantCount}'");
                return passed;
            }
            finally
            {
                DestroyObject(triggerObject);
            }
        }

        private static async Task<bool> ValidateDestroyUnregisterAndSecondResetAsync(
            RuntimePrefabFixture fixture,
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "runtime-prefab-destroy-unregister";
            GameObject triggerObject = null;
            try
            {
                ResetSubjectId firstSubjectId = fixture.First.Adapter.SubjectId;
                ResetSubjectId secondSubjectId = fixture.Second.Adapter.SubjectId;

                fixture.DestroyFirst();
                await Task.Yield();
                runtimeHost.ResetRegistry.CleanupStaleOwners();

                bool firstRemoved = !runtimeHost.ResetRegistry.TryGetSubject(firstSubjectId, out _);
                bool secondStillRegistered = fixture.Second.Adapter != null
                    && fixture.Second.Adapter.IsRegistered
                    && runtimeHost.ResetRegistry.TryGetSubject(secondSubjectId, out _);

                fixture.Second.Target.localPosition = new Vector3(0f, 14f, 0f);

                triggerObject = new GameObject("QA_12F_RuntimeOnly_GroupTrigger_AfterDestroy");
                var trigger = triggerObject.AddComponent<ObjectResetGroupTrigger>();
                trigger.ConfigureForQa(
                    "qa.reset.12f.runtime-group-after-destroy",
                    "qa.reset.12f.runtime-group.after-destroy",
                    ResetSelectionMode.RuntimeOnlySubjects,
                    Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                ResetExecutionResult result = await trigger.RequestObjectResetGroupAsync();
                bool secondReset = Approximately(fixture.Second.Target.localPosition, fixture.Second.BaselinePosition);
                bool noStaleExecution = trigger.LastFailedTargetCount == 0
                    && trigger.LastFailedParticipantCount == 0
                    && trigger.LastBlockingIssueCount == 0;

                bool passed = firstRemoved
                    && secondStillRegistered
                    && FrameworkEnumValidation.IsDefinedAndNot(result.Status, ResetExecutionStatus.Unknown)
                    && result.Succeeded
                    && trigger.LastRequestSucceeded
                    && trigger.LastExecutionStatus == ResetExecutionStatus.Succeeded
                    && trigger.LastTargetCount == 1
                    && trigger.LastSucceededTargetCount == 1
                    && trigger.LastParticipantCount == 1
                    && trigger.LastSucceededParticipantCount == 1
                    && secondReset
                    && noStaleExecution;

                LogStep(
                    logger,
                    step,
                    passed,
                    runtimeHost,
                    result.Status.ToString(),
                    $"firstRemoved='{firstRemoved}' secondStillRegistered='{secondStillRegistered}' secondReset='{secondReset}' subjects='{trigger.LastTargetCount}' participants='{trigger.LastParticipantCount}' staleFailed='{!noStaleExecution}'");
                return passed;
            }
            finally
            {
                DestroyObject(triggerObject);
            }
        }

        private static async Task<bool> ValidateActivityRestartWithRemainingRuntimeSubjectAsync(
            RuntimePrefabFixture fixture,
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            const string step = "runtime-prefab-activity-restart";
            GameObject triggerObject = null;
            try
            {
                fixture.Second.Target.localPosition = new Vector3(0f, -17f, 0f);

                triggerObject = new GameObject("QA_12F_Runtime_ActivityRestartTrigger");
                var trigger = triggerObject.AddComponent<ActivityRestartTrigger>();
                IActivityRestartRuntimePort activityRestartRuntime = runtimeHost;
                if (!trigger.TryBindActivityRestartRuntime(activityRestartRuntime, out string bindingIssue))
                {
                    LogStep(logger, step, false, runtimeHost, "binding-failed", bindingIssue);
                    return false;
                }
                trigger.ConfigureForQa(
                    qaTargetActivity: null,
                    qaUseCurrentActivityWhenTargetMissing: true,
                    qaRequireTargetActivityIsCurrent: true,
                    qaReason: "qa.reset.12f.activity-restart",
                    qaSelectionMode: ResetSelectionMode.CurrentRouteAndActivitySubjects,
                    qaExplicitSubjects: Array.Empty<ResetSubjectReference>(),
                    qaAllowNoSubjects: false,
                    qaAllowNoParticipants: false,
                    qaStopOnFailure: true,
                    qaYieldBetweenSubjects: false);

                ActivityRestartResult result = await trigger.RequestActivityRestartAsync();
                bool secondReset = Approximately(fixture.Second.Target.localPosition, fixture.Second.BaselinePosition);
                bool passed = result != null
                    && FrameworkEnumValidation.IsDefinedAndNot(result.Status, ActivityRestartResultStatus.Unknown)
                    && result.Succeeded
                    && trigger.LastRequestSucceeded
                    && result.HasResetExecutionResult
                    && result.ResetExecutionResult.Status == ResetExecutionStatus.Succeeded
                    && result.ResetSubjectCount == 1
                    && result.ResetSubjectSucceededCount == 1
                    && result.ResetParticipantCount == 1
                    && result.ResetParticipantSucceededCount == 1
                    && string.Equals(result.ClearStatus, "Succeeded", StringComparison.Ordinal)
                    && string.Equals(result.ReenterStatus, "Succeeded", StringComparison.Ordinal)
                    && secondReset;

                LogStep(
                    logger,
                    step,
                    passed,
                    runtimeHost,
                    result != null ? result.Status.ToString() : "NullResult",
                    result != null
                        ? $"resetStatus='{result.ResetStatus}' resetSubjects='{result.ResetSubjectCount}' resetParticipants='{result.ResetParticipantCount}' clearStatus='{result.ClearStatus}' reenterStatus='{result.ReenterStatus}' secondReset='{secondReset}'"
                        : "result='null'");
                return passed;
            }
            finally
            {
                DestroyObject(triggerObject);
            }
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return Vector3.SqrMagnitude(left - right) <= 0.0001f;
        }

        private static void LogStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            FrameworkRuntimeHost runtimeHost,
            string status,
            string detail)
        {
            logger.Info(
                "QA Runtime Prefab Reset Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("operationStatus", status),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString()),
                    LogFields.Field("detail", detail ?? string.Empty)));
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

        private sealed class RuntimePrefabFixture : IDisposable
        {
            private readonly GameObject _prototype;
            private RuntimePrefabInstance _first;
            private RuntimePrefabInstance _second;
            private bool _disposed;

            private RuntimePrefabFixture(GameObject prototype)
            {
                _prototype = prototype;
            }

            internal RuntimePrefabInstance First => _first;

            internal RuntimePrefabInstance Second => _second;

            internal static RuntimePrefabFixture Create(string runtimePrefix)
            {
                GameObject prototype = CreatePrototype(runtimePrefix);
                return new RuntimePrefabFixture(prototype);
            }

            internal bool SpawnAndRegister(
                FrameworkRuntimeHost runtimeHost,
                FrameworkLogger logger,
                string source)
            {
                const string step = "runtime-prefab-spawn-register";
                _first = InstantiateRuntimeBox("QA_12F_RuntimeBox_A", new Vector3(1f, 2f, 3f));
                _second = InstantiateRuntimeBox("QA_12F_RuntimeBox_B", new Vector3(-1f, 2f, -3f));

                bool firstRegistered = _first.Register("qa.reset.12f.runtime-first.register");
                bool secondRegistered = _second.Register("qa.reset.12f.runtime-second.register");

                string firstId = _first.Adapter.SubjectId.StableText;
                string secondId = _second.Adapter.SubjectId.StableText;
                bool idsDistinct = !string.Equals(firstId, secondId, StringComparison.Ordinal)
                    && firstId.StartsWith(RuntimePrefix + "#", StringComparison.Ordinal)
                    && secondId.StartsWith(RuntimePrefix + "#", StringComparison.Ordinal);
                bool bothHaveParticipants = _first.Adapter.RegisteredParticipantCount == 1
                    && _second.Adapter.RegisteredParticipantCount == 1;
                bool bothRuntimeOrigin = _first.Adapter.Subject.Origin == ResetSubjectOrigin.RuntimeRegistered
                    && _second.Adapter.Subject.Origin == ResetSubjectOrigin.RuntimeRegistered;
                bool bothRuntimeScope = _first.Adapter.Subject.Scope == ResetSubjectScope.Runtime
                    && _second.Adapter.Subject.Scope == ResetSubjectScope.Runtime;

                bool passed = firstRegistered
                    && secondRegistered
                    && idsDistinct
                    && bothHaveParticipants
                    && bothRuntimeOrigin
                    && bothRuntimeScope
                    && runtimeHost.ResetRegistry.TryGetSubject(_first.Adapter.SubjectId, out _)
                    && runtimeHost.ResetRegistry.TryGetSubject(_second.Adapter.SubjectId, out _);

                LogStep(
                    logger,
                    step,
                    passed,
                    runtimeHost,
                    $"{(firstRegistered ? "Registered" : "Rejected")}/{(secondRegistered ? "Registered" : "Rejected")}",
                    $"first='{firstId}' second='{secondId}' distinct='{idsDistinct}' participants='{_first.Adapter.RegisteredParticipantCount + _second.Adapter.RegisteredParticipantCount}' originRuntime='{bothRuntimeOrigin}' scopeRuntime='{bothRuntimeScope}'");
                return passed;
            }

            internal void DestroyFirst()
            {
                if (_first == null)
                {
                    return;
                }

                _first.DisposeByDestroy("qa.reset.12f.runtime-first.destroy");
                _first = null;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _first?.Dispose();
                _second?.Dispose();
                DestroyObject(_prototype);
            }

            private RuntimePrefabInstance InstantiateRuntimeBox(string name, Vector3 baselinePosition)
            {
                GameObject instance = Object.Instantiate(_prototype);
                instance.name = name;
                instance.transform.localPosition = baselinePosition;
                instance.transform.localEulerAngles = Vector3.zero;
                instance.transform.localScale = Vector3.one;

                var adapter = instance.GetComponent<UnityResetSubjectAdapter>();
                var participant = instance.GetComponent<UnityTransformResetParticipant>();
                participant.ConfigureForQa(
                    "transform",
                    ResetParticipantRequiredness.Required,
                    0,
                    "Transform",
                    nameof(UnityResetRuntimePrefabQaSmokeRunner),
                    "qa.reset.12f.runtime.transform");
                participant.ConfigureTransformForQa(instance.transform, qaCaptureOnEnable: false, qaResetPosition: true, qaResetRotation: true, qaResetScale: true);

                instance.SetActive(true);
                return new RuntimePrefabInstance(instance, instance.transform, adapter, baselinePosition);
            }

            private static GameObject CreatePrototype(string runtimePrefix)
            {
                var prototype = new GameObject("QA_12F_RuntimeResetBox_Prototype");
                prototype.SetActive(false);

                var adapter = prototype.AddComponent<UnityResetSubjectAdapter>();
                var participant = prototype.AddComponent<UnityTransformResetParticipant>();
                participant.ConfigureForQa(
                    "transform",
                    ResetParticipantRequiredness.Required,
                    0,
                    "Transform",
                    nameof(UnityResetRuntimePrefabQaSmokeRunner),
                    "qa.reset.12f.runtime.prototype.transform");
                participant.ConfigureTransformForQa(prototype.transform, qaCaptureOnEnable: false, qaResetPosition: true, qaResetRotation: true, qaResetScale: true);

                adapter.ConfigureForQa(
                    qaRegisterOnEnable: false,
                    qaUnregisterOnDisable: true,
                    qaRetryUntilRuntimeAvailable: false,
                    qaIdGeneration: UnityResetSubjectIdGenerationMode.RuntimeInstanceId,
                    qaSubjectId: string.Empty,
                    qaRuntimeSubjectIdPrefix: runtimePrefix,
                    qaScope: ResetSubjectScope.Runtime,
                    qaDisplayName: "QA Runtime Reset Box",
                    qaDiagnosticTag: "Smoke:RuntimePrefabReset:Prototype",
                    qaParticipantDiscovery: UnityResetParticipantDiscoveryMode.Children,
                    qaIncludeInactiveParticipants: true);
                return prototype;
            }
        }

        private sealed class RuntimePrefabInstance : IDisposable
        {
            private bool _disposed;

            internal RuntimePrefabInstance(
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

            internal bool Register(string reason)
            {
                return Adapter != null && Adapter.RegisterWithCurrentHost(reason);
            }

            internal void DisposeByDestroy(string reason)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (GameObject != null)
                {
                    GameObject.SetActive(false);
                    DestroyObject(GameObject);
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (Adapter != null && Adapter.IsRegistered)
                {
                    Adapter.ClearRegistration("qa.reset.12f.runtime.cleanup");
                }

                DestroyObject(GameObject);
            }
        }
    }
}
#endif
