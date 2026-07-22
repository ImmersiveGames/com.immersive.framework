using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// Unity authoring adapter for ResetSubject registration.
    /// This adapter is the resetability facet of an object; it does not require ObjectEntryDeclaration.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Unity Reset Subject Adapter")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "H2.2.10 explicit Reset registration runtime binding independent from ObjectEntryDeclaration.")]
    public sealed class UnityResetSubjectAdapter : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityResetSubjectAdapter);

        [Header("Registration")]
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool unregisterOnDisable = true;
        [SerializeField] private bool retryUntilRuntimeAvailable = true;

        [Header("Subject")]
        [SerializeField] private UnityResetSubjectIdGenerationMode idGeneration =
            UnityResetSubjectIdGenerationMode.AuthoredStableId;
        [SerializeField] private string subjectId;
        [SerializeField] private string runtimeSubjectIdPrefix;
        [SerializeField] private ResetSubjectScope scope = ResetSubjectScope.Activity;
        [SerializeField] private string displayName;
        [SerializeField] private string diagnosticTag;

        [Header("Actor Identity Bridge")]
        [SerializeField] private ActorDeclaration sourceActor;
        [SerializeField] private PlayerActorDeclaration sourcePlayerActor;

        [Header("Participants")]
        [SerializeField] private UnityResetParticipantDiscoveryMode participantDiscovery =
            UnityResetParticipantDiscoveryMode.Children;
        [SerializeField] private bool includeInactiveParticipants = true;
        [SerializeField] private bool includeUnityResettableComponents = true;

        private readonly List<ResetRegistrationHandle> _participantHandles = new();
        private readonly List<UnityResettableComponentParticipant>
            _resettableComponentParticipants = new();

        private IResetRegistrationRuntimePort _resetRegistrationRuntime;
        private IResetRegistrationRuntimePort _registeredRuntime;
        private string _resetRegistrationRuntimeBindingDiagnostic =
            "Reset registration runtime port is not bound.";
        private ResetRegistrationHandle _subjectHandle;
        private ResetSubject _subject;
        private bool _registrationAttempted;
        private bool _runtimeUnavailableLogged;
        private bool _ownerUnavailableLogged;
        private string _lastOwnerUnavailableIssue;
        private FrameworkLogger _logger;

        public bool IsRegistered =>
            _subjectHandle.IsSubject && _registeredRuntime != null;

        public ResetRegistrationHandle SubjectHandle => _subjectHandle;

        public ResetSubject Subject => _subject;

        public ResetSubjectId SubjectId => _subject.SubjectId;

        public int RegisteredParticipantCount => _participantHandles.Count;

        public UnityResetSubjectIdGenerationMode IdGeneration => idGeneration;

        public ResetSubjectScope Scope => scope;

        public ActorDeclaration SourceActor => sourceActor;

        public bool HasSourceActor => sourceActor != null;

        public PlayerActorDeclaration SourcePlayerActor => sourcePlayerActor;

        public bool HasSourcePlayerActor => sourcePlayerActor != null;

        public bool HasResetRegistrationRuntimeBinding =>
            _resetRegistrationRuntime != null;

        public string ResetRegistrationRuntimeBindingStatus =>
            HasResetRegistrationRuntimeBinding ? "Bound" : "Missing";

        public string ResetRegistrationRuntimeBindingDiagnostic =>
            _resetRegistrationRuntimeBindingDiagnostic.NormalizeText();

        private FrameworkLogger Logger =>
            _logger ??= FrameworkLogger.Create<UnityResetSubjectAdapter>();

        internal bool TryBindResetRegistrationRuntime(
            IResetRegistrationRuntimePort resetRegistrationRuntime,
            out string issue)
        {
            if (resetRegistrationRuntime == null)
            {
                issue =
                    "Reset registration runtime port binding requires a non-null port.";
                _resetRegistrationRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_resetRegistrationRuntime == null)
            {
                _resetRegistrationRuntime = resetRegistrationRuntime;
                _runtimeUnavailableLogged = false;
                issue = string.Empty;
                _resetRegistrationRuntimeBindingDiagnostic =
                    $"Bound '{resetRegistrationRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(
                    _resetRegistrationRuntime,
                    resetRegistrationRuntime))
            {
                issue = string.Empty;
                _resetRegistrationRuntimeBindingDiagnostic =
                    $"Bound '{resetRegistrationRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue =
                "Reset registration runtime port binding rejected a different port for the current lifetime.";
            _resetRegistrationRuntimeBindingDiagnostic = issue;
            return false;
        }

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
            if (!retryUntilRuntimeAvailable ||
                !registerOnEnable ||
                IsRegistered ||
                !_registrationAttempted)
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

        private void Reset()
        {
            sourceActor = GetComponent<ActorDeclaration>();
            sourcePlayerActor = GetComponent<PlayerActorDeclaration>();
        }

        /// <summary>
        /// Attempts registration through the explicitly bound Reset registration runtime port.
        /// </summary>
        public bool RegisterWithCurrentHost(string reason)
        {
            _registrationAttempted = true;
            if (IsRegistered)
            {
                return true;
            }

            IResetRegistrationRuntimePort runtime = _resetRegistrationRuntime;
            if (runtime == null)
            {
                LogRuntimeUnavailable(reason);
                return false;
            }

            if (!runtime.TryResolveCurrentResetOwner(
                    scope,
                    out RuntimeContentOwner owner,
                    out string issue))
            {
                LogOwnerUnavailable(reason, issue);
                return false;
            }

            _runtimeUnavailableLogged = false;
            _ownerUnavailableLogged = false;
            _lastOwnerUnavailableIssue = null;

            ResetRegistryOperationResult subjectResult =
                CreateAndRegisterSubject(runtime, owner, reason);
            if (!subjectResult.Succeeded)
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter registration rejected by ResetRegistry.",
                    LogFields.Field("status", subjectResult.Status.ToString()),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field(
                        "subjectIdSource",
                        ResolveSubjectIdSourceLabel()),
                    LogFields.Field(
                        "sourceActor",
                        ResolveSourceActorDiagnosticText()),
                    LogFields.Field(
                        "runtimeSubjectIdPrefix",
                        runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field(
                        "issues",
                        subjectResult.Issues.Count.ToString()),
                    LogFields.Field(
                        "issue",
                        subjectResult.Issues.Count > 0
                            ? subjectResult.Issues[0].Message
                            : string.Empty),
                    LogFields.Field("reason", reason));
                return false;
            }

            _registeredRuntime = runtime;
            _subjectHandle = subjectResult.Handle;
            _subject = subjectResult.Subject;
            RegisterParticipants(reason);

            Logger.Debug(
                "Unity Reset Subject Adapter registered reset subject.",
                LogFields.Field("status", "Registered"),
                LogFields.Field("subjectId", _subject.SubjectId.StableText),
                LogFields.Field(
                    "subjectIdSource",
                    ResolveSubjectIdSourceLabel()),
                LogFields.Field(
                    "sourceActor",
                    ResolveSourceActorDiagnosticText()),
                LogFields.Field("scope", _subject.Scope.ToString()),
                LogFields.Field("origin", _subject.Origin.ToString()),
                LogFields.Field("owner", _subject.OwnerStableText),
                LogFields.Field(
                    "participants",
                    _participantHandles.Count.ToString()),
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
                ClearRegistrationState();
                return false;
            }

            IResetRegistrationRuntimePort registeredRuntime =
                _registeredRuntime;
            ResetRegistrationHandle handle = _subjectHandle;
            ResetSubject subject = _subject;
            ResetRegistryOperationResult result =
                registeredRuntime.UnregisterResetRegistration(
                    handle,
                    this,
                    DefaultSource,
                    reason);

            bool unregistered =
                result.Succeeded ||
                result.Status ==
                ResetRegistryOperationStatus.AlreadyUnregistered;
            if (unregistered)
            {
                Logger.Debug(
                    "Unity Reset Subject Adapter unregistered reset subject.",
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field(
                        "subjectId",
                        subject.SubjectId.StableText),
                    LogFields.Field("handle", handle.Value.ToString()),
                    LogFields.Field("reason", reason));
            }
            else
            {
                Logger.Warning(
                    "Unity Reset Subject Adapter unregister failed.",
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field(
                        "subjectId",
                        subject.SubjectId.StableText),
                    LogFields.Field("handle", handle.Value.ToString()),
                    LogFields.Field(
                        "issues",
                        result.Issues.Count.ToString()),
                    LogFields.Field(
                        "issue",
                        result.Issues.Count > 0
                            ? result.Issues[0].Message
                            : string.Empty),
                    LogFields.Field("reason", reason));
            }

            ClearRegistrationState();
            return unregistered;
        }

        private void ClearRegistrationState()
        {
            _subjectHandle = default;
            _subject = default;
            _registeredRuntime = null;
            _participantHandles.Clear();
            _resettableComponentParticipants.Clear();
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
                    "Unity Reset Subject Adapter is waiting for an explicit Reset registration runtime binding before registering reset subject.",
                    LogFields.Field("status", "WaitingForRuntimeBinding"),
                    LogFields.Field("idGeneration", idGeneration.ToString()),
                    LogFields.Field("subjectId", subjectId),
                    LogFields.Field(
                        "subjectIdSource",
                        ResolveSubjectIdSourceLabel()),
                    LogFields.Field(
                        "sourceActor",
                        ResolveSourceActorDiagnosticText()),
                    LogFields.Field(
                        "runtimeSubjectIdPrefix",
                        runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field(
                        "retryUntilRuntimeAvailable",
                        retryUntilRuntimeAvailable.ToString()),
                    LogFields.Field(
                        "bindingStatus",
                        ResetRegistrationRuntimeBindingStatus),
                    LogFields.Field(
                        "bindingDiagnostic",
                        ResetRegistrationRuntimeBindingDiagnostic),
                    LogFields.Field("reason", reason));
                return;
            }

            Logger.Warning(
                "Unity Reset Subject Adapter registration rejected because the Reset registration runtime port is not bound.",
                LogFields.Field("status", "RejectedMissingRuntimeBinding"),
                LogFields.Field("idGeneration", idGeneration.ToString()),
                LogFields.Field("subjectId", subjectId),
                LogFields.Field(
                    "subjectIdSource",
                    ResolveSubjectIdSourceLabel()),
                LogFields.Field(
                    "sourceActor",
                    ResolveSourceActorDiagnosticText()),
                LogFields.Field(
                    "runtimeSubjectIdPrefix",
                    runtimeSubjectIdPrefix),
                LogFields.Field("scope", scope.ToString()),
                LogFields.Field(
                    "retryUntilRuntimeAvailable",
                    retryUntilRuntimeAvailable.ToString()),
                LogFields.Field(
                    "bindingStatus",
                    ResetRegistrationRuntimeBindingStatus),
                LogFields.Field(
                    "bindingDiagnostic",
                    ResetRegistrationRuntimeBindingDiagnostic),
                LogFields.Field("reason", reason));
        }

        private void LogOwnerUnavailable(string reason, string issue)
        {
            if (IsExpectedDeferredRegistration(reason))
            {
                if (_ownerUnavailableLogged &&
                    string.Equals(
                        _lastOwnerUnavailableIssue,
                        issue,
                        StringComparison.Ordinal))
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
                    LogFields.Field(
                        "subjectIdSource",
                        ResolveSubjectIdSourceLabel()),
                    LogFields.Field(
                        "sourceActor",
                        ResolveSourceActorDiagnosticText()),
                    LogFields.Field(
                        "runtimeSubjectIdPrefix",
                        runtimeSubjectIdPrefix),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field(
                        "retryUntilRuntimeAvailable",
                        retryUntilRuntimeAvailable.ToString()),
                    LogFields.Field("reason", reason));
                return;
            }

            Logger.Warning(
                "Unity Reset Subject Adapter registration rejected.",
                LogFields.Field("status", "RejectedMissingOwner"),
                LogFields.Field("idGeneration", idGeneration.ToString()),
                LogFields.Field("subjectId", subjectId),
                LogFields.Field(
                    "subjectIdSource",
                    ResolveSubjectIdSourceLabel()),
                LogFields.Field(
                    "sourceActor",
                    ResolveSourceActorDiagnosticText()),
                LogFields.Field(
                    "runtimeSubjectIdPrefix",
                    runtimeSubjectIdPrefix),
                LogFields.Field("scope", scope.ToString()),
                LogFields.Field("issue", issue),
                LogFields.Field(
                    "retryUntilRuntimeAvailable",
                    retryUntilRuntimeAvailable.ToString()),
                LogFields.Field("reason", reason));
        }

        private bool IsExpectedDeferredRegistration(string reason)
        {
            return retryUntilRuntimeAvailable &&
                registerOnEnable &&
                (string.Equals(
                     reason,
                     "on-enable",
                     StringComparison.Ordinal) ||
                 string.Equals(
                     reason,
                     "start",
                     StringComparison.Ordinal) ||
                 string.Equals(
                     reason,
                     "update-retry",
                     StringComparison.Ordinal));
        }

        private ResetRegistryOperationResult CreateAndRegisterSubject(
            IResetRegistrationRuntimePort runtime,
            RuntimeContentOwner owner,
            string reason)
        {
            switch (idGeneration)
            {
                case UnityResetSubjectIdGenerationMode.AuthoredStableId:
                    return RegisterAuthoredStableSubject(
                        runtime,
                        owner,
                        reason);

                case UnityResetSubjectIdGenerationMode.RuntimeInstanceId:
                    return runtime.RegisterRuntimeResetSubject(
                        runtimeSubjectIdPrefix,
                        scope,
                        owner,
                        this,
                        ResolveDisplayName(runtimeSubjectIdPrefix),
                        ResolveDiagnosticTag(
                            "UnityResetSubjectAdapter:Runtime"),
                        DefaultSource,
                        reason);

                default:
                    return ResetRegistryOperationResult.Rejected(
                        ResetRegistryOperationStatus.RejectedInvalidSubject,
                        ResetIssue.Error(
                            ResetIssueKind.InvalidSubject,
                            "Unity Reset Subject Adapter requires an explicit id generation mode."),
                        "Unity reset subject registration rejected because id generation mode is invalid.");
            }
        }

        private ResetRegistryOperationResult RegisterAuthoredStableSubject(
            IResetRegistrationRuntimePort runtime,
            RuntimeContentOwner owner,
            string reason)
        {
            string normalizedSubjectId;
            try
            {
                normalizedSubjectId = ResolveAuthoredStableSubjectId();
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      ArgumentOutOfRangeException)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(
                        ResetIssueKind.InvalidSubject,
                        exception.Message),
                    "Unity reset subject registration rejected because source actor identity is invalid or conflicting.");
            }

            if (string.IsNullOrWhiteSpace(normalizedSubjectId))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(
                        ResetIssueKind.InvalidSubject,
                        "Unity Reset Subject Adapter authored stable id is missing."),
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
                    ResolveDiagnosticTag(
                        "UnityResetSubjectAdapter:SceneAuthored"));
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      ArgumentOutOfRangeException)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(
                        ResetIssueKind.InvalidSubject,
                        exception.Message),
                    "Unity reset subject registration rejected because subject descriptor creation failed.");
            }

            return runtime.RegisterResetSubject(
                subject,
                this,
                DefaultSource,
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
            IReadOnlyList<UnityResetParticipantBehaviour> participants =
                ResolveParticipants();
            for (int index = 0; index < participants.Count; index++)
            {
                UnityResetParticipantBehaviour participant =
                    participants[index];
                if (participant == null)
                {
                    continue;
                }

                ResetRegistryOperationResult result =
                    _registeredRuntime.RegisterResetParticipant(
                        _subjectHandle,
                        participant,
                        participant,
                        DefaultSource,
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
            IReadOnlyList<MonoBehaviour> components =
                ResolveResettableComponents();
            for (int index = 0; index < components.Count; index++)
            {
                MonoBehaviour component = components[index];
                if (component == null ||
                    component is UnityResetParticipantBehaviour)
                {
                    continue;
                }

                if (component is not IUnityResettable resettable)
                {
                    continue;
                }

                var participant =
                    new UnityResettableComponentParticipant(
                        component,
                        resettable);
                ResetRegistryOperationResult result =
                    _registeredRuntime.RegisterResetParticipant(
                        _subjectHandle,
                        participant,
                        component,
                        DefaultSource,
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
                LogFields.Field(
                    "participantId",
                    participantIdText.NormalizeText()),
                LogFields.Field(
                    "issues",
                    result.Issues.Count.ToString()),
                LogFields.Field(
                    "issue",
                    result.Issues.Count > 0
                        ? result.Issues[0].Message
                        : string.Empty),
                LogFields.Field("reason", reason));
        }

        private IReadOnlyList<UnityResetParticipantBehaviour>
            ResolveParticipants()
        {
            switch (participantDiscovery)
            {
                case UnityResetParticipantDiscoveryMode.SameGameObject:
                    return GetComponents<UnityResetParticipantBehaviour>();

                case UnityResetParticipantDiscoveryMode.Children:
                    return GetComponentsInChildren<
                        UnityResetParticipantBehaviour>(
                        includeInactiveParticipants);

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
                    return GetComponentsInChildren<MonoBehaviour>(
                        includeInactiveParticipants);

                default:
                    return Array.Empty<MonoBehaviour>();
            }
        }

        private string ResolveAuthoredStableSubjectId()
        {
            IActor actor = ResolveSourceActorIdentity();
            if (actor != null)
            {
                return actor.ActorId.StableText;
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
                    if (sourceActor != null &&
                        sourcePlayerActor != null)
                    {
                        return
                            "ActorDeclaration+PlayerActorDeclaration";
                    }

                    if (sourcePlayerActor != null)
                    {
                        return "PlayerActorDeclaration";
                    }

                    return sourceActor != null
                        ? "ActorDeclaration"
                        : "AuthoredText";

                default:
                    return "Unknown";
            }
        }

        private string ResolveSourceActorDiagnosticText()
        {
            if (sourceActor == null && sourcePlayerActor == null)
            {
                return "<none>";
            }

            string actorDeclarationText = ResolveActorDiagnosticText(
                sourceActor,
                nameof(ActorDeclaration));
            string playerActorDeclarationText =
                ResolveActorDiagnosticText(
                    sourcePlayerActor,
                    nameof(PlayerActorDeclaration));
            return
                $"actorDeclaration='{actorDeclarationText}' playerActorDeclaration='{playerActorDeclarationText}'";
        }

        private IActor ResolveSourceActorIdentity()
        {
            IActor resolvedActor = sourceActor;
            if (sourcePlayerActor == null)
            {
                return resolvedActor;
            }

            if (resolvedActor == null)
            {
                return sourcePlayerActor;
            }

            ActorId actorId = resolvedActor.ActorId;
            ActorId playerActorId = sourcePlayerActor.ActorId;
            if (actorId != playerActorId)
            {
                throw new ArgumentException(
                    $"Unity Reset Subject Adapter has conflicting actor identity sources. ActorDeclaration='{actorId.StableText}' PlayerActorDeclaration='{playerActorId.StableText}'.");
            }

            return resolvedActor;
        }

        private static string ResolveActorDiagnosticText(
            IActor actor,
            string sourceLabel)
        {
            if (actor == null)
            {
                return "<none>";
            }

            string actorIdText;
            try
            {
                actorIdText = actor.ActorId.StableText;
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      ArgumentOutOfRangeException)
            {
                actorIdText = $"<invalid:{exception.Message}>";
            }

            var behaviour = actor as MonoBehaviour;
            string displayNameText =
                actor.ActorDisplayName.NormalizeTextOrFallback(
                    behaviour != null
                        ? behaviour.name
                        : sourceLabel);
            string objectName =
                behaviour != null && behaviour.gameObject != null
                    ? behaviour.gameObject.name
                    : "<none>";
            return
                $"name='{objectName}' displayName='{displayNameText}' actorId='{actorIdText}'";
        }

        private string ResolveDisplayName(string fallback)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (gameObject != null &&
                !string.IsNullOrWhiteSpace(gameObject.name))
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
            ActorDeclaration qaSourceActor = null,
            PlayerActorDeclaration qaSourcePlayerActor = null)
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
            includeUnityResettableComponents =
                qaIncludeUnityResettableComponents;
            sourceActor = qaSourceActor;
            sourcePlayerActor = qaSourcePlayerActor;
        }
#endif
    }
}
