using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Unity authoring adapter for ResetSubject registration.
    /// This adapter is the resetability facet of an object; it does not require ObjectEntryDeclaration.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Unity Reset Subject Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity reset subject adapter independent from ObjectEntryDeclaration.")]
    public sealed class UnityResetSubjectAdapter : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool unregisterOnDisable = true;
        [SerializeField] private bool retryUntilRuntimeAvailable = true;

        [Header("Subject")]
        [SerializeField] private UnityResetSubjectIdGenerationMode idGeneration = UnityResetSubjectIdGenerationMode.AuthoredStableId;
        [SerializeField] private string subjectId;
        [SerializeField] private string runtimeSubjectIdPrefix;
        [SerializeField] private ResetSubjectScope scope = ResetSubjectScope.Activity;
        [SerializeField] private string displayName;
        [SerializeField] private string diagnosticTag;

        [Header("Participants")]
        [SerializeField] private UnityResetParticipantDiscoveryMode participantDiscovery = UnityResetParticipantDiscoveryMode.Children;
        [SerializeField] private bool includeInactiveParticipants = true;

        private readonly List<ResetRegistrationHandle> _participantHandles = new();
        private ResetRegistrationHandle _subjectHandle;
        private ResetSubject _subject;
        private FrameworkRuntimeHost _registeredHost;
        private bool _registrationAttempted;
        private FrameworkLogger _logger;

        public bool IsRegistered => _subjectHandle.IsSubject && _registeredHost != null;

        public ResetRegistrationHandle SubjectHandle => _subjectHandle;

        public ResetSubject Subject => _subject;

        public ResetSubjectId SubjectId => _subject.SubjectId;

        public int RegisteredParticipantCount => _participantHandles.Count;

        public UnityResetSubjectIdGenerationMode IdGeneration => idGeneration;

        public ResetSubjectScope Scope => scope;

        private FrameworkLogger Logger => _logger ??= FrameworkLogger.Create<UnityResetSubjectAdapter>();

        private void OnEnable()
        {
            if (registerOnEnable)
            {
                RegisterWithCurrentHost("on-enable");
            }
        }

        private void Start()
        {
            if (registerOnEnable && !IsRegistered)
            {
                RegisterWithCurrentHost("start");
            }
        }

        private void Update()
        {
            if (!retryUntilRuntimeAvailable || !registerOnEnable || IsRegistered || !_registrationAttempted)
            {
                return;
            }

            RegisterWithCurrentHost("update-retry");
        }

        private void OnDisable()
        {
            if (unregisterOnDisable)
            {
                ClearRegistration("on-disable");
            }
        }

        private void OnDestroy()
        {
            ClearRegistration("on-destroy");
        }

        public bool RegisterWithCurrentHost()
        {
            return RegisterWithCurrentHost("manual");
        }

        public bool RegisterWithCurrentHost(string reason)
        {
            _registrationAttempted = true;
            if (IsRegistered)
            {
                return true;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost) || runtimeHost == null)
            {
                Logger.Info(
                    "Unity Reset Subject Adapter skipped because FrameworkRuntimeHost is not available. The adapter will retry if retry is enabled.",
                    LogFields.Field("status", "SkippedNoRuntime"),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("reason", reason));
                return false;
            }

            if (!runtimeHost.TryResolveCurrentResetOwner(scope, out RuntimeContentOwner owner, out string issue))
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter registration rejected.",
                    LogFields.Field("status", "RejectedMissingOwner"),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field("reason", reason));
                return false;
            }

            ResetRegistryOperationResult subjectResult = CreateAndRegisterSubject(runtimeHost, owner, reason);
            if (!subjectResult.Succeeded)
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter registration rejected by ResetRegistry.",
                    LogFields.Field("status", subjectResult.Status.ToString()),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("issues", subjectResult.Issues.Count.ToString()),
                    LogFields.Field("issue", subjectResult.Issues.Count > 0 ? subjectResult.Issues[0].Message : string.Empty),
                    LogFields.Field("reason", reason));
                return false;
            }

            _registeredHost = runtimeHost;
            _subjectHandle = subjectResult.Handle;
            _subject = subjectResult.Subject;
            RegisterParticipants(reason);

            Logger.Info(
                "Unity Reset Subject Adapter registered reset subject.",
                LogFields.Field("status", "Registered"),
                LogFields.Field("subjectId", _subject.SubjectId.StableText),
                LogFields.Field("scope", _subject.Scope.ToString()),
                LogFields.Field("origin", _subject.Origin.ToString()),
                LogFields.Field("owner", _subject.OwnerStableText),
                LogFields.Field("participants", _participantHandles.Count.ToString()),
                LogFields.Field("handle", _subjectHandle.Value.ToString()),
                LogFields.Field("reason", reason));
            return true;
        }

        public bool ClearRegistration()
        {
            return ClearRegistration("manual");
        }

        public bool ClearRegistration(string reason)
        {
            if (!IsRegistered)
            {
                _subjectHandle = default;
                _subject = default;
                _registeredHost = null;
                _participantHandles.Clear();
                return false;
            }

            ResetRegistrationHandle handle = _subjectHandle;
            ResetSubject subject = _subject;
            ResetRegistryOperationResult result = _registeredHost.UnregisterResetRegistration(
                handle,
                this,
                nameof(UnityResetSubjectAdapter),
                reason);

            bool unregistered = result.Succeeded || result.Status == ResetRegistryOperationStatus.AlreadyUnregistered;
            if (unregistered)
            {
                Logger.Info(
                    "Unity Reset Subject Adapter unregistered reset subject.",
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("subjectId", subject.SubjectId.StableText),
                    LogFields.Field("handle", handle.Value.ToString()),
                    LogFields.Field("reason", reason));
            }
            else
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter unregister failed.",
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("subjectId", subject.SubjectId.StableText),
                    LogFields.Field("handle", handle.Value.ToString()),
                    LogFields.Field("issues", result.Issues.Count.ToString()),
                    LogFields.Field("issue", result.Issues.Count > 0 ? result.Issues[0].Message : string.Empty),
                    LogFields.Field("reason", reason));
            }

            _subjectHandle = default;
            _subject = default;
            _registeredHost = null;
            _participantHandles.Clear();
            return unregistered;
        }

        private ResetRegistryOperationResult CreateAndRegisterSubject(
            FrameworkRuntimeHost runtimeHost,
            RuntimeContentOwner owner,
            string reason)
        {
            switch (idGeneration)
            {
                case UnityResetSubjectIdGenerationMode.AuthoredStableId:
                    return RegisterAuthoredStableSubject(runtimeHost, owner, reason);
                case UnityResetSubjectIdGenerationMode.RuntimeInstanceId:
                    return runtimeHost.RegisterRuntimeResetSubject(
                        runtimeSubjectIdPrefix,
                        scope,
                        owner,
                        this,
                        ResolveDisplayName(runtimeSubjectIdPrefix),
                        ResolveDiagnosticTag("UnityResetSubjectAdapter:Runtime"),
                        nameof(UnityResetSubjectAdapter),
                        reason);
                default:
                    return ResetRegistryOperationResult.Rejected(
                        ResetRegistryOperationStatus.RejectedInvalidSubject,
                        ResetIssue.Error(ResetIssueKind.InvalidSubject, "Unity Reset Subject Adapter requires an explicit id generation mode."),
                        "Unity reset subject registration rejected because id generation mode is invalid.");
            }
        }

        private ResetRegistryOperationResult RegisterAuthoredStableSubject(
            FrameworkRuntimeHost runtimeHost,
            RuntimeContentOwner owner,
            string reason)
        {
            string normalizedSubjectId = subjectId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedSubjectId))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, "Unity Reset Subject Adapter authored stable id is missing."),
                    "Unity reset subject registration rejected because authored stable id is missing.");
            }

            ResetSubject subject;
            try
            {
                subject = new ResetSubject(
                    ResetSubjectId.From(normalizedSubjectId),
                    scope,
                    ResetSubjectOrigin.SceneAuthored,
                    owner,
                    ResolveDisplayName(normalizedSubjectId),
                    ResolveDiagnosticTag("UnityResetSubjectAdapter:SceneAuthored"));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, exception.Message),
                    "Unity reset subject registration rejected because subject descriptor creation failed.");
            }

            return runtimeHost.RegisterResetSubject(
                subject,
                this,
                nameof(UnityResetSubjectAdapter),
                reason);
        }

        private void RegisterParticipants(string reason)
        {
            _participantHandles.Clear();
            var participants = ResolveParticipants();
            for (int i = 0; i < participants.Count; i++)
            {
                UnityResetParticipantBehaviour participant = participants[i];
                if (participant == null)
                {
                    continue;
                }

                ResetRegistryOperationResult result = _registeredHost.RegisterResetParticipant(
                    _subjectHandle,
                    participant,
                    participant,
                    nameof(UnityResetSubjectAdapter),
                    reason);
                if (result.Succeeded)
                {
                    _participantHandles.Add(result.Handle);
                    continue;
                }

                Logger.Warning(
                    "Unity Reset Subject Adapter participant registration rejected.",
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("subjectId", _subject.SubjectId.StableText),
                    LogFields.Field("participant", participant.GetType().Name),
                    LogFields.Field("issues", result.Issues.Count.ToString()),
                    LogFields.Field("issue", result.Issues.Count > 0 ? result.Issues[0].Message : string.Empty),
                    LogFields.Field("reason", reason));
            }
        }

        private IReadOnlyList<UnityResetParticipantBehaviour> ResolveParticipants()
        {
            switch (participantDiscovery)
            {
                case UnityResetParticipantDiscoveryMode.SameGameObject:
                    return GetComponents<UnityResetParticipantBehaviour>();
                case UnityResetParticipantDiscoveryMode.Children:
                    return GetComponentsInChildren<UnityResetParticipantBehaviour>(includeInactiveParticipants);
                default:
                    return Array.Empty<UnityResetParticipantBehaviour>();
            }
        }

        private string ResolveDisplayName(string fallback)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (gameObject != null && !string.IsNullOrWhiteSpace(gameObject.name))
            {
                return gameObject.name.Trim();
            }

            return fallback.NormalizeTextOrFallback("Reset Subject");
        }

        private string ResolveDiagnosticTag(string fallback)
        {
            return diagnosticTag.NormalizeTextOrFallback(fallback);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            bool qaRegisterOnEnable,
            bool qaUnregisterOnDisable,
            bool qaRetryUntilRuntimeAvailable,
            UnityResetSubjectIdGenerationMode qaIdGeneration,
            string qaSubjectId,
            string qaRuntimeSubjectIdPrefix,
            ResetSubjectScope qaScope,
            string qaDisplayName,
            string qaDiagnosticTag,
            UnityResetParticipantDiscoveryMode qaParticipantDiscovery,
            bool qaIncludeInactiveParticipants)
        {
            registerOnEnable = qaRegisterOnEnable;
            unregisterOnDisable = qaUnregisterOnDisable;
            retryUntilRuntimeAvailable = qaRetryUntilRuntimeAvailable;
            idGeneration = qaIdGeneration;
            subjectId = qaSubjectId;
            runtimeSubjectIdPrefix = qaRuntimeSubjectIdPrefix;
            scope = qaScope;
            displayName = qaDisplayName;
            diagnosticTag = qaDiagnosticTag;
            participantDiscovery = qaParticipantDiscovery;
            includeInactiveParticipants = qaIncludeInactiveParticipants;
        }
#endif
    }
}
