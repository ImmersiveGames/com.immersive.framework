using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic snapshot for one passive PlayerView.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49G passive PlayerView snapshot.")]
    public readonly struct PlayerViewSnapshot : IEquatable<PlayerViewSnapshot>
    {
        private readonly string _cameraName;
        private readonly string _targetName;
        private readonly string _reason;

        public PlayerViewSnapshot(
            PlayerSlotId playerSlotId,
            PlayerViewState state,
            bool hasCameraEvidence,
            bool hasTargetEvidence,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            string cameraName,
            string targetName,
            string reason)
        {
            PlayerView.Validate(playerSlotId, state, hasPlayerEntryEvidence, playerEntryState, reason);

            PlayerSlotId = playerSlotId;
            State = state;
            HasCameraEvidence = hasCameraEvidence;
            HasTargetEvidence = hasTargetEvidence;
            HasPlayerEntryEvidence = hasPlayerEntryEvidence;
            PlayerEntryState = playerEntryState;
            _cameraName = cameraName.NormalizeText();
            _targetName = targetName.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerViewState State { get; }

        public bool HasCameraEvidence { get; }

        public bool HasTargetEvidence { get; }

        public bool HasPlayerEntryEvidence { get; }

        public PlayerEntryState PlayerEntryState { get; }

        public string CameraName => _cameraName;

        public string DiagnosticCameraName => _cameraName.ToDiagnosticText();

        public string TargetName => _targetName;

        public string DiagnosticTargetName => _targetName.ToDiagnosticText();

        public string Reason => _reason;

        public string DiagnosticReason => _reason.ToDiagnosticText();

        public bool IsDeclared => State == PlayerViewState.Declared;

        public bool IsBound => State == PlayerViewState.Bound;

        public bool IsActive => State == PlayerViewState.Active;

        public bool IsSuspended => State == PlayerViewState.Suspended;

        public bool IsReleased => State == PlayerViewState.Released;

        public bool IsEligibleForActiveView => State == PlayerViewState.Active && IsViewBoundOrActiveEntry(PlayerEntryState);

        public bool Equals(PlayerViewSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && State == other.State
                && HasCameraEvidence == other.HasCameraEvidence
                && HasTargetEvidence == other.HasTargetEvidence
                && HasPlayerEntryEvidence == other.HasPlayerEntryEvidence
                && PlayerEntryState == other.PlayerEntryState
                && string.Equals(_cameraName, other._cameraName, StringComparison.Ordinal)
                && string.Equals(_targetName, other._targetName, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ HasCameraEvidence.GetHashCode();
                hash = (hash * 397) ^ HasTargetEvidence.GetHashCode();
                hash = (hash * 397) ^ HasPlayerEntryEvidence.GetHashCode();
                hash = (hash * 397) ^ (int)PlayerEntryState;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_targetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"state='{State}' playerSlot='{PlayerSlotId.StableText}' hasCamera='{HasCameraEvidence}' camera='{DiagnosticCameraName}' hasTarget='{HasTargetEvidence}' target='{DiagnosticTargetName}' hasPlayerEntry='{HasPlayerEntryEvidence}' playerEntryState='{PlayerEntryState}' eligibleForActiveView='{IsEligibleForActiveView}' reason='{DiagnosticReason}'";
        }

        public static bool operator ==(PlayerViewSnapshot left, PlayerViewSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerViewSnapshot left, PlayerViewSnapshot right)
        {
            return !left.Equals(right);
        }

        internal static bool IsViewBoundOrActiveEntry(PlayerEntryState state)
        {
            return state == PlayerEntryState.ViewBound || state == PlayerEntryState.Active;
        }
    }
}
