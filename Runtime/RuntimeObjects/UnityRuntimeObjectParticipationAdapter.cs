using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.ObjectReset.Unity;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Experimental. Unity adapter that lets a runtime-instantiated or runtime-enabled object participate in ObjectEntry and ObjectReset flows.
    /// This is not a spawner, PlayerActor, pooling hook, save identity, camera binding or input owner.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Runtime Objects/Unity Runtime Object Participation Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F44 runtime object participation adapter; registers spawned/enabled objects into ObjectEntry and ObjectReset context.")]
    public sealed class UnityRuntimeObjectParticipationAdapter : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool unregisterOnDisable = true;
        [SerializeField] private bool retryUntilRuntimeAvailable = true;

        [Header("Object Entry")]
        [SerializeField] private string objectEntryId;
        [SerializeField] private ObjectEntryScope scope = ObjectEntryScope.Activity;
        [SerializeField] private ObjectEntryRequiredness requiredness = ObjectEntryRequiredness.Required;
        [SerializeField] private string displayName;

        [Header("Object Reset")]
        [SerializeField] private ObjectResetUnityParticipantBehaviour[] resetParticipants = Array.Empty<ObjectResetUnityParticipantBehaviour>();

        private RuntimeObjectParticipationHandle _handle;
        private FrameworkRuntimeHost _registeredHost;
        private bool _registrationAttempted;
        private FrameworkLogger _logger;

        public bool IsRegistered => _handle.IsValid && _registeredHost != null;

        public RuntimeObjectParticipationHandle Handle => _handle;

        public string ObjectEntryIdText => objectEntryId;

        public ObjectEntryScope Scope => scope;

        private FrameworkLogger Logger => _logger ??= FrameworkLogger.Create<UnityRuntimeObjectParticipationAdapter>();

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

        public bool ClearRegistration()
        {
            return ClearRegistration("manual");
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
                    "Unity Runtime Object Participation Adapter skipped because FrameworkRuntimeHost is not available. The adapter will retry if retry is enabled.",
                    LogFields.Field("status", "SkippedNoRuntime"),
                    LogFields.Field("objectEntry", objectEntryId),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("reason", reason));
                return false;
            }

            if (!TryCreateDescriptor(runtimeHost, out ObjectEntryDescriptor descriptor, out string issue))
            {
                Logger.Warning(
                    "Unity Runtime Object Participation Adapter registration rejected.",
                    LogFields.Field("status", "RejectedInvalidDescriptor"),
                    LogFields.Field("objectEntry", objectEntryId),
                    LogFields.Field("scope", scope.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field("reason", reason));
                return false;
            }

            IReadOnlyList<IObjectResetParticipant> participants = ResolveResetParticipants();
            if (!runtimeHost.RegisterRuntimeObjectParticipation(
                    descriptor,
                    participants,
                    this,
                    nameof(UnityRuntimeObjectParticipationAdapter),
                    reason,
                    out RuntimeObjectParticipationHandle handle,
                    out issue))
            {
                Logger.Warning(
                    "Unity Runtime Object Participation Adapter registration rejected by runtime host.",
                    LogFields.Field("status", "RejectedByRuntimeHost"),
                    LogFields.Field("objectEntry", descriptor.Id.StableText),
                    LogFields.Field("scope", descriptor.Scope.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field("reason", reason));
                return false;
            }

            _registeredHost = runtimeHost;
            _handle = handle;
            Logger.Info(
                "Unity Runtime Object Participation Adapter registered runtime object.",
                LogFields.Field("status", "Registered"),
                LogFields.Field("objectEntry", descriptor.Id.StableText),
                LogFields.Field("scope", descriptor.Scope.ToString()),
                LogFields.Field("sourceKind", descriptor.SourceKind.ToString()),
                LogFields.Field("participants", participants.Count.ToString()),
                LogFields.Field("handle", handle.Value.ToString()),
                LogFields.Field("reason", reason));
            return true;
        }

        public bool ClearRegistration(string reason)
        {
            if (!IsRegistered)
            {
                _handle = default;
                _registeredHost = null;
                return false;
            }

            bool unregistered = _registeredHost.UnregisterRuntimeObjectParticipation(
                _handle,
                this,
                nameof(UnityRuntimeObjectParticipationAdapter),
                reason,
                out string issue);

            if (unregistered)
            {
                Logger.Info(
                    "Unity Runtime Object Participation Adapter unregistered runtime object.",
                    LogFields.Field("status", "Unregistered"),
                    LogFields.Field("objectEntry", objectEntryId),
                    LogFields.Field("handle", _handle.Value.ToString()),
                    LogFields.Field("reason", reason));
            }
            else
            {
                Logger.Warning(
                    "Unity Runtime Object Participation Adapter unregister failed.",
                    LogFields.Field("status", "UnregisterFailed"),
                    LogFields.Field("objectEntry", objectEntryId),
                    LogFields.Field("handle", _handle.Value.ToString()),
                    LogFields.Field("issue", issue),
                    LogFields.Field("reason", reason));
            }

            _handle = default;
            _registeredHost = null;
            return unregistered;
        }

        private bool TryCreateDescriptor(
            FrameworkRuntimeHost runtimeHost,
            out ObjectEntryDescriptor descriptor,
            out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (string.IsNullOrWhiteSpace(objectEntryId))
            {
                issue = "Object Entry Id is missing.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                issue = "Object Entry Scope must be explicit.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryRequiredness), requiredness) || requiredness == ObjectEntryRequiredness.Unspecified)
            {
                issue = "Object Entry Requiredness must be explicit.";
                return false;
            }

            if (!runtimeHost.TryResolveCurrentObjectEntryOwnerIdentity(scope, out FrameworkIdentityKey ownerIdentity))
            {
                issue = $"No active runtime owner identity is available for scope '{scope}'.";
                return false;
            }

            try
            {
                descriptor = new ObjectEntryDescriptor(
                    ObjectEntryId.From(objectEntryId.Trim()),
                    scope,
                    ObjectEntrySourceKind.RuntimeRegistered,
                    requiredness,
                    ResolveDisplayName(),
                    ownerIdentity);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

        private IReadOnlyList<IObjectResetParticipant> ResolveResetParticipants()
        {
            if (resetParticipants == null || resetParticipants.Length == 0)
            {
                return Array.Empty<IObjectResetParticipant>();
            }

            var participants = new List<IObjectResetParticipant>(resetParticipants.Length);
            for (int i = 0; i < resetParticipants.Length; i++)
            {
                if (resetParticipants[i] != null)
                {
                    participants.Add(resetParticipants[i]);
                }
            }

            return participants;
        }

        private string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return gameObject != null && !string.IsNullOrWhiteSpace(gameObject.name)
                ? gameObject.name.Trim()
                : objectEntryId.Trim();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            bool qaRegisterOnEnable,
            string qaObjectEntryId,
            ObjectEntryScope qaScope,
            ObjectEntryRequiredness qaRequiredness,
            string qaDisplayName,
            params ObjectResetUnityParticipantBehaviour[] qaResetParticipants)
        {
            registerOnEnable = qaRegisterOnEnable;
            objectEntryId = qaObjectEntryId;
            scope = qaScope;
            requiredness = qaRequiredness;
            displayName = qaDisplayName;
            resetParticipants = qaResetParticipants ?? Array.Empty<ObjectResetUnityParticipantBehaviour>();
        }
#endif
    }
}
