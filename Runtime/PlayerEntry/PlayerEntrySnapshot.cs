using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerEntry
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic snapshot for one passive PlayerEntry.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49D passive PlayerEntry snapshot.")]
    public readonly struct PlayerEntrySnapshot : IEquatable<PlayerEntrySnapshot>
    {
        private readonly string _suspensionReason;
        private readonly string _reason;

        public PlayerEntrySnapshot(
            PlayerSlotId playerSlotId,
            ActorId actorId,
            PlayerEntryState state,
            ActorReadinessSnapshot actorReadiness,
            string suspensionReason,
            string reason)
        {
            PlayerEntry.Validate(playerSlotId, actorId, state, actorReadiness, suspensionReason);

            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            State = state;
            ActorReadiness = actorReadiness;
            _suspensionReason = suspensionReason.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public ActorId ActorId { get; }

        public PlayerEntryState State { get; }

        public ActorReadinessSnapshot ActorReadiness { get; }

        public bool IsActorReadyForView => ActorReadiness.IsReadyForView;

        public bool IsActorReadyForControl => ActorReadiness.IsReadyForControl;

        public bool IsConfigured => State >= PlayerEntryState.Configured && State != PlayerEntryState.Released;

        public bool IsJoined => State >= PlayerEntryState.Joined && State != PlayerEntryState.Released;

        public bool IsAssigned => State >= PlayerEntryState.Assigned && State != PlayerEntryState.Released;

        public bool IsInstantiated => State >= PlayerEntryState.Instantiated && State != PlayerEntryState.Released;

        public bool IsActorReady => State >= PlayerEntryState.ActorReady && State != PlayerEntryState.Released;

        public bool IsViewBound => State >= PlayerEntryState.ViewBound && State != PlayerEntryState.Released;

        public bool IsActive => State == PlayerEntryState.Active;

        public bool IsSuspended => State == PlayerEntryState.Suspended;

        public bool IsReleased => State == PlayerEntryState.Released;

        public string SuspensionReason => _suspensionReason;

        public string DiagnosticSuspensionReason => _suspensionReason.ToDiagnosticText();

        public string Reason => _reason;

        public string DiagnosticReason => _reason.ToDiagnosticText();

        public bool Equals(PlayerEntrySnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && ActorId.Equals(other.ActorId)
                && State == other.State
                && ActorReadiness.Equals(other.ActorReadiness)
                && string.Equals(_suspensionReason, other._suspensionReason, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerEntrySnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ ActorId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ ActorReadiness.GetHashCode();
                hash = (hash * 397) ^ (_suspensionReason != null ? _suspensionReason.GetHashCode() : 0);
                hash = (hash * 397) ^ (_reason != null ? _reason.GetHashCode() : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"state='{State}' playerSlot='{PlayerSlotId.StableText}' actor='{ActorId.StableText}' actorReadiness=\"{ActorReadiness}\" suspensionReason='{DiagnosticSuspensionReason}' reason='{DiagnosticReason}'";
        }

        public static bool operator ==(PlayerEntrySnapshot left, PlayerEntrySnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerEntrySnapshot left, PlayerEntrySnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
