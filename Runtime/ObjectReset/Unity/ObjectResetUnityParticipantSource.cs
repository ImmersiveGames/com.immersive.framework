using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
namespace Immersive.Framework.ObjectReset.Unity
{
    /// <summary>
    /// API status: Experimental. Explicit Unity-side source for Object Reset participants.
    /// It registers itself with the framework runtime host and returns only serialized participant references that support the resolved logical target.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Unity Participant Source")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F15B/F16 Unity participant source; explicit references only, no scene search.")]
    public sealed class ObjectResetUnityParticipantSource : MonoBehaviour, IObjectResetParticipantSource
    {
        [Header("Registration")]
        [SerializeField] private bool registerOnEnable = true;

        [Header("Participants")]
        [SerializeField] private ObjectResetUnityParticipantBehaviour[] participants = Array.Empty<ObjectResetUnityParticipantBehaviour>();

        private FrameworkRuntimeHost _registeredHost;

        public bool RegisterOnEnable => registerOnEnable;

        public int AuthoredParticipantCount => participants?.Length ?? 0;

        public bool IsRegistered => _registeredHost != null && !ReferenceEquals(_registeredHost, null);

        private void OnEnable()
        {
            if (registerOnEnable)
            {
                RegisterWithCurrentHost();
            }
        }

        private void Start()
        {
            if (registerOnEnable && !IsRegistered)
            {
                RegisterWithCurrentHost();
            }
        }

        private void OnDisable()
        {
            ClearRegistration();
        }

        public bool RegisterWithCurrentHost()
        {
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost) || runtimeHost == null)
            {
                return false;
            }

            runtimeHost.SetObjectResetParticipantSource(this);
            _registeredHost = runtimeHost;
            return true;
        }

        public bool ClearRegistration()
        {
            if (_registeredHost == null)
            {
                return false;
            }

            bool cleared = _registeredHost.ClearObjectResetParticipantSource(this);
            _registeredHost = null;
            return cleared;
        }

        public IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget)
        {
            if (!request.IsValid || participants == null || participants.Length == 0)
            {
                return Array.Empty<IObjectResetParticipant>();
            }

            var resolved = new List<IObjectResetParticipant>(participants.Length);
            for (int i = 0; i < participants.Length; i++)
            {
                var participant = participants[i];
                if (participant == null)
                {
                    continue;
                }

                if (participant.SupportsResolvedTarget(request, resolvedTarget))
                {
                    resolved.Add(participant);
                }
            }

            return resolved;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            bool qaRegisterOnEnable,
            params ObjectResetUnityParticipantBehaviour[] qaParticipants)
        {
            registerOnEnable = qaRegisterOnEnable;
            participants = qaParticipants ?? Array.Empty<ObjectResetUnityParticipantBehaviour>();
        }
#endif
    }
}
