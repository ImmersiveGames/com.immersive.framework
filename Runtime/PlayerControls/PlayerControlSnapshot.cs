using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic snapshot for one passive PlayerControl evidence entry.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49I passive PlayerControl snapshot.")]
    public readonly struct PlayerControlSnapshot : IEquatable<PlayerControlSnapshot>
    {
        private readonly string _controlTargetName;
        private readonly string _inputSourceId;
        private readonly string _suspensionReason;
        private readonly string _reason;

        public PlayerControlSnapshot(
            PlayerSlotId playerSlotId,
            PlayerControlState state,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            bool isPlayerEntryReadyForControl,
            bool hasControlTarget,
            string controlTargetName,
            string inputSourceId,
            string suspensionReason,
            string reason)
        {
            PlayerControl.Validate(
                playerSlotId,
                state,
                hasPlayerEntryEvidence,
                playerEntryState,
                isPlayerEntryReadyForControl,
                suspensionReason);

            PlayerSlotId = playerSlotId;
            State = state;
            HasPlayerEntryEvidence = hasPlayerEntryEvidence;
            PlayerEntryState = playerEntryState;
            IsPlayerEntryReadyForControl = isPlayerEntryReadyForControl;
            HasControlTarget = hasControlTarget;
            _controlTargetName = controlTargetName.NormalizeText();
            _inputSourceId = inputSourceId.NormalizeText();
            _suspensionReason = suspensionReason.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerControlState State { get; }

        public bool HasPlayerEntryEvidence { get; }

        public PlayerEntryState PlayerEntryState { get; }

        public bool IsPlayerEntryReadyForControl { get; }

        public bool HasControlTarget { get; }

        public string ControlTargetName => _controlTargetName;

        public string DiagnosticControlTargetName => _controlTargetName.ToDiagnosticText();

        public string InputSourceId => _inputSourceId;

        public string DiagnosticInputSourceId => _inputSourceId.ToDiagnosticText();

        public bool HasInputSource => !string.IsNullOrWhiteSpace(_inputSourceId);

        public bool IsDeclared => State == PlayerControlState.Declared;

        public bool IsBound => State == PlayerControlState.Bound;

        public bool IsActive => State == PlayerControlState.Active;

        public bool IsSuspended => State == PlayerControlState.Suspended;

        public bool IsReleased => State == PlayerControlState.Released;

        public bool IsParticipating => State != PlayerControlState.Released;

        public bool IsEligibleForBoundControl => HasPlayerEntryEvidence && PlayerEntryState == PlayerEntryState.Active;

        public bool IsEligibleForActiveControl => IsEligibleForBoundControl && IsPlayerEntryReadyForControl;

        public string SuspensionReason => _suspensionReason;

        public string DiagnosticSuspensionReason => _suspensionReason.ToDiagnosticText();

        public string Reason => _reason;

        public string DiagnosticReason => _reason.ToDiagnosticText();

        public string DiagnosticPlayerEntryState => HasPlayerEntryEvidence ? PlayerEntryState.ToString() : "<none>";

        public bool Equals(PlayerControlSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && State == other.State
                && HasPlayerEntryEvidence == other.HasPlayerEntryEvidence
                && PlayerEntryState == other.PlayerEntryState
                && IsPlayerEntryReadyForControl == other.IsPlayerEntryReadyForControl
                && HasControlTarget == other.HasControlTarget
                && string.Equals(_controlTargetName, other._controlTargetName, StringComparison.Ordinal)
                && string.Equals(_inputSourceId, other._inputSourceId, StringComparison.Ordinal)
                && string.Equals(_suspensionReason, other._suspensionReason, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControlSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ HasPlayerEntryEvidence.GetHashCode();
                hash = (hash * 397) ^ (int)PlayerEntryState;
                hash = (hash * 397) ^ IsPlayerEntryReadyForControl.GetHashCode();
                hash = (hash * 397) ^ HasControlTarget.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_controlTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_inputSourceId ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_suspensionReason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"state='{State}' playerSlot='{PlayerSlotId.StableText}' hasPlayerEntry='{HasPlayerEntryEvidence}' playerEntryState='{DiagnosticPlayerEntryState}' playerEntryReadyForControl='{IsPlayerEntryReadyForControl}' hasControlTarget='{HasControlTarget}' controlTarget='{DiagnosticControlTargetName}' inputSource='{DiagnosticInputSourceId}' eligibleForBoundControl='{IsEligibleForBoundControl}' eligibleForActiveControl='{IsEligibleForActiveControl}' suspensionReason='{DiagnosticSuspensionReason}' reason='{DiagnosticReason}'";
        }

        public static bool operator ==(PlayerControlSnapshot left, PlayerControlSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerControlSnapshot left, PlayerControlSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
