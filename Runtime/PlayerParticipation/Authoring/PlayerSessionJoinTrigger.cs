using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Requests one Manager-Provisioned local Player Join through the current
    /// Route or Activity binding. A joined Player is not Actor/gameplay admission.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Join (Manager-Provisioned)")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Bounded Manager-Provisioned Join boundary under IF-ADR-046.")]
    public sealed class PlayerSessionJoinTrigger : MonoBehaviour
    {
        private const string Source = nameof(PlayerSessionJoinTrigger);

        [SerializeField]
        [Tooltip("Route or Activity lifecycle occurrence that owns this Join trigger.")]
        private LocalPlayerProvisioningConsumerScope scope =
            LocalPlayerProvisioningConsumerScope.Unspecified;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional diagnostic reason. Join is used when left empty.")]
        private string reason;

        [SerializeField]
        [Tooltip("Optional Unity Input System control scheme hint.")]
        private string controlScheme;

        [NonSerialized]
        private IPlayerSessionJoinPort _joinPort;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public LocalPlayerJoinOutcome LastOutcome { get; private set; }

        /// <summary>Requests a join with the supplied intent through the active scoped binding.</summary>
        public LocalPlayerJoinOutcome RequestJoin(LocalPlayerJoinRequest request)
        {
            if (this == null)
                return LocalPlayerJoinOutcomeProjection.RejectedUnavailable();

            if (!TryValidateBindingScope(out _))
                return SetOutcome(LocalPlayerJoinOutcomeProjection.RejectedConfiguration());

            if (!request.IsValid ||
                (request.PairWithDevice != null && !request.PairWithDevice.added))
            {
                return SetOutcome(LocalPlayerJoinOutcomeProjection.RejectedInvalidRequest());
            }

            IPlayerSessionJoinPort joinPort = _joinPort;
            if (joinPort == null)
                return SetOutcome(LocalPlayerJoinOutcomeProjection.RejectedUnavailable());

            try
            {
                LocalPlayerJoinOutcome outcome = joinPort.RequestJoin(request) ??
                    LocalPlayerJoinOutcomeProjection.FailedUnknown();
                return SetOutcome(outcome);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return SetOutcome(LocalPlayerJoinOutcomeProjection.FailedUnknown());
            }
        }

        /// <summary>Invokes Join using the authored reason and control scheme.</summary>
        public void Invoke()
        {
            RequestJoin(CreateAuthoredRequest(pairWithDevice: null));
        }

        /// <summary>Invokes Join while preserving the exact explicit InputDevice reference.</summary>
        public void InvokeFromDevice(InputDevice device)
        {
            if (device == null || !device.added)
            {
                SetOutcome(LocalPlayerJoinOutcomeProjection.RejectedInvalidRequest());
                return;
            }

            RequestJoin(CreateAuthoredRequest(device));
        }

        internal bool TryValidateBindingScope(out string issue)
        {
            if (scope.IsDefinedScope())
            {
                issue = string.Empty;
                return true;
            }

            issue = "Player Session Join trigger requires an explicit Route or Activity scope.";
            return false;
        }

        internal bool TryBindJoinPort(
            IPlayerSessionJoinPort joinPort,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (joinPort == null)
            {
                issue = "Player Session Join trigger received no internal Join port.";
                return false;
            }

            if (!TryValidateBindingScope(out issue))
                return false;

            if (scope != actualScope)
            {
                issue = $"Player Session Join trigger scope '{scope}' does not match active '{actualScope}' scope.";
                return false;
            }

            if (_joinPort != null && !ReferenceEquals(_joinPort, joinPort))
            {
                issue = "Player Session Join trigger is already bound to a different live occurrence.";
                return false;
            }

            _joinPort = joinPort;
            issue = string.Empty;
            return true;
        }

        internal void ReleaseJoinPort(string releaseReason, bool isStale)
        {
            _joinPort = null;
        }

        private LocalPlayerJoinRequest CreateAuthoredRequest(InputDevice pairWithDevice)
        {
            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? "Join"
                : reason.Trim();
            return new LocalPlayerJoinRequest(Source, resolvedReason, pairWithDevice, controlScheme);
        }

        private LocalPlayerJoinOutcome SetOutcome(LocalPlayerJoinOutcome outcome)
        {
            LastOutcome = outcome;
            return outcome;
        }
    }
}
