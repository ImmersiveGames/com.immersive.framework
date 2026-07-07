using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
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

        [Header("Actor Identity Bridge")]
        [SerializeField] private ActorDeclaration sourceActor;

        [Header("Participants")]
        [SerializeField] private UnityResetParticipantDiscoveryMode participantDiscovery = UnityResetParticipantDiscoveryMode.Children;
        [SerializeField] private bool includeInactiveParticipants = true;
        [SerializeField] private bool includeUnityResettableComponents = true;

        private readonly List<ResetRegistrationHandle> _participantHandles = new();
        private readonly List<UnityResettableComponentParticipant> _resettableComponentParticipants = new();
        private ResetRegistrationHandle _subjectHandle;
        private ResetSubject _subject;
        private FrameworkRuntimeHost _registeredHost;
        private bool _registrationAttempted;
        private bool _runtimeUnavailableLogged;
        private bool _ownerUnavailableLogged;
        private string _lastOwnerUnavailableIssue;
        private FrameworkLogger _logger;

        public bool IsRegistered => _subjectHandle.IsSubject && _registeredHost != null;

        public ResetRegistrationHandle SubjectHandle => _subjectHandle;

        public ResetSubject Subject => _subject;

        public ResetSubjectId SubjectId => _subject.SubjectId;

        public int RegisteredParticipantCount => _participantHandles.Count;

        public UnityResetSubjectIdGenerationMode IdGeneration => idGeneration;

        public ResetSubjectScope Scope => scope;

        public ActorDeclaration SourceActor => sourceActor;

        public bool HasSourceActor => sourceActor != null;

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
                LogRuntimeUnavailable(reason);
                return false;
            }

            if (!runtimeHost.TryResolveCurrentResetOwner(scope, out RuntimeContentOwner owner, out string issue))
            {
                LogOwnerUnavailable(reason, issue);
                return false;
            }

            _runtimeUnavailableLogged = false;
            _ownerUnavailableLogged = false;
            _lastOwnerUnavailableIssue = null;

            ResetRegistryOperationResult subjectResult = CreateAndRegisterSubject(runtimeHost, owner, reason);
            if (!subjectResult.Succeeded)
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter registration rejected by ResetRegistry.",
                    LogFields.Field("status", subjectResult.Status.ToString()),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                    LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
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

            Logger.Debug(
                "Unity Reset Subject Adapter registered reset subject.",
                LogFields.Field("status", "Registered"),
                LogFields.Field("subjectId", _subject.SubjectId.StableText),
                LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
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
                _resettableComponentParticipants.Clear();
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
                Logger.Debug(
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
            _resettableComponentParticipants.Clear();
            return unregistered;
        }

        private void LogRuntimeUnavailable(string reason)
        {
            if (IsExpectedDeferredRegistration(reason))
            {
                if (_runtimeUnavailableLogged)
                {
                    return;
                }

                _runtimeUnavailableLogged = true;
                Logger.Trace(
                    "Unity Reset Subject Adapter is waiting for FrameworkRuntimeHost before registering reset subject.",
                    LogFields.Field("status", "WaitingForRuntime"),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                    LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
                    LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("retryUntilRuntimeAvailable", retryUntilRuntimeAvailable.ToString()),
                    LogFields.Field("reason", reason));
                return;
            }

            Logger.Warning(
                "Unity Reset Subject Adapter registration rejected because FrameworkRuntimeHost is not available.",
                LogFields.Field("status", "RejectedNoRuntime"),
                LogFields.Field("idGeneration", idGeneration.ToString()),
                LogFields.Field("subjectId", subjectId),
                LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
                LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                LogFields.Field("scope", scope.ToString()),
                LogFields.Field("retryUntilRuntimeAvailable", retryUntilRuntimeAvailable.ToString()),
                LogFields.Field("reason", reason));
        }

        private void LogOwnerUnavailable(string reason, string issue)
        {
            if (IsExpectedDeferredRegistration(reason))
            {
                if (_ownerUnavailableLogged && string.Equals(_lastOwnerUnavailableIssue, issue, StringComparison.Ordinal))
                {
                    return;
                }

                _ownerUnavailableLogged = true;
                _lastOwnerUnavailableIssue = issue;
                Logger.Trace(
                    "Unity Reset Subject Adapter is waiting for a reset owner before registering reset subject.",
                    LogFields.Field("status", "WaitingForOwner"),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                    LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
                    LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field("retryUntilRuntimeAvailable", retryUntilRuntimeAvailable.ToString()),
                    LogFields.Field("reason", reason));
                return;
            }

            Logger.Warning(
                "Unity Reset Subject Adapter registration rejected.",
                LogFields.Field("status", "RejectedMissingOwner"),
                LogFields.Field("idGeneration", idGeneration.ToString()),
                LogFields.Field("subjectId", subjectId),
                LogFields.Field("subjectIdSource", ResolveSubjectIdSourceLabel()),
                LogFields.Field("sourceActor", ResolveSourceActorDiagnosticText()),
                LogFields.Field("runtimeSubjectIdPrefix", runtimeSubjectIdPrefix),
                LogFields.Field("scope", scope.ToString()),
                LogFields.Field("issue", issue),
                LogFields.Field("retryUntilRuntimeAvailable", retryUntilRuntimeAvailable.ToString()),
                LogFields.Field("reason", reason));
        }

        private bool IsExpectedDeferredRegistration(string reason)
        {
            return retryUntilRuntimeAvailable
                && registerOnEnable
                && (string.Equals(reason, "on-enable", StringComparison.Ordinal)
                    || string.Equals(reason, "start", StringComparison.Ordinal)
                    || string.Equals(reason, "update-retry", StringComparison.Ordinal));
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
            string normalizedSubjectId;
            try
            {
                normalizedSubjectId = ResolveAuthoredStableSubjectId();
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, exception.Message),
                    "Unity reset subject registration rejected because source actor id is invalid.");
            }

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
            _resettableComponentParticipants.Clear();

            RegisterUnityResetParticipantBehaviours(reason);

            if (includeUnityResettableComponents)
            {
                RegisterUnityResettableComponents(reason);
            }
        }

        private void RegisterUnityResetParticipantBehaviours(string reason)
        {
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

                LogParticipantRegistrationRejected(
                    result,
                    participant.GetType().Name,
                    participant.ParticipantIdText,
                    reason);
            }
        }

        private void RegisterUnityResettableComponents(string reason)
        {
            var components = ResolveResettableComponents();
            for (int i = 0; i < components.Count; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null || component is UnityResetParticipantBehaviour)
                {
                    continue;
                }

                if (component is not IUnityResettable resettable)
                {
                    continue;
                }

                var participant = new UnityResettableComponentParticipant(component, resettable);
                ResetRegistryOperationResult result = _registeredHost.RegisterResetParticipant(
                    _subjectHandle,
                    participant,
                    component,
                    nameof(UnityResetSubjectAdapter),
                    reason);
                if (result.Succeeded)
                {
                    _participantHandles.Add(result.Handle);
                    _resettableComponentParticipants.Add(participant);
                    continue;
                }

                LogParticipantRegistrationRejected(
                    result,
                    component.GetType().Name,
                    resettable.ResetParticipantId,
                    reason);
            }
        }

        private void LogParticipantRegistrationRejected(
            ResetRegistryOperationResult result,
            string participantType,
            string participantIdText,
            string reason)
        {
            Logger.Warning(
                "Unity Reset Subject Adapter participant registration rejected.",
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("subjectId", _subject.SubjectId.StableText),
                LogFields.Field("participant", participantType),
                LogFields.Field("participantId", participantIdText.NormalizeText()),
                LogFields.Field("issues", result.Issues.Count.ToString()),
                LogFields.Field("issue", result.Issues.Count > 0 ? result.Issues[0].Message : string.Empty),
                LogFields.Field("reason", reason));
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

        private IReadOnlyList<MonoBehaviour> ResolveResettableComponents()
        {
            switch (participantDiscovery)
            {
                case UnityResetParticipantDiscoveryMode.SameGameObject:
                    return GetComponents<MonoBehaviour>();
                case UnityResetParticipantDiscoveryMode.Children:
                    return GetComponentsInChildren<MonoBehaviour>(includeInactiveParticipants);
                default:
                    return Array.Empty<MonoBehaviour>();
            }
        }

        private string ResolveAuthoredStableSubjectId()
        {
            if (sourceActor != null)
            {
                return sourceActor.ActorId.StableText;
            }

            return subjectId.NormalizeText();
        }

        private string ResolveSubjectIdSourceLabel()
        {
            switch (idGeneration)
            {
                case UnityResetSubjectIdGenerationMode.RuntimeInstanceId:
                    return "RuntimePrefix";
                case UnityResetSubjectIdGenerationMode.AuthoredStableId:
                    return sourceActor != null ? "ActorDeclaration" : "AuthoredText";
                default:
                    return "Unknown";
            }
        }

        private string ResolveSourceActorDiagnosticText()
        {
            if (sourceActor == null)
            {
                return "<none>";
            }

            string actorIdText;
            try
            {
                actorIdText = sourceActor.ActorId.StableText;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                actorIdText = $"<invalid:{exception.Message}>";
            }

            string displayNameText = sourceActor.ActorDisplayName.NormalizeTextOrFallback(sourceActor.name);
            string objectName = sourceActor.gameObject != null ? sourceActor.gameObject.name : "<none>";
            return $"name='{objectName}' displayName='{displayNameText}' actorId='{actorIdText}'";
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
            bool qaIncludeInactiveParticipants,
            bool qaIncludeUnityResettableComponents = true,
            ActorDeclaration qaSourceActor = null)
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
            includeUnityResettableComponents = qaIncludeUnityResettableComponents;
            sourceActor = qaSourceActor;
        }
#endif
    }
}
