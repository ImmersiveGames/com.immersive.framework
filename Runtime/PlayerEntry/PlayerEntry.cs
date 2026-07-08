using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerEntry
{
    /// <summary>
    /// API status: Experimental. Immutable passive PlayerEntry model.
    /// It stores PlayerSlot identity, Actor identity, passive entry state and Actor readiness evidence.
    /// It does not coordinate runtime lifecycle, join players, spawn actors, bind views, bind control or move gameplay objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49D passive PlayerEntry model.")]
    public sealed class PlayerEntry : IPlayerEntry, IEquatable<PlayerEntry>
    {
        private readonly PlayerSlotId _playerSlotId;
        private readonly ActorId _actorId;
        private readonly PlayerEntryState _state;
        private readonly ActorReadinessSnapshot _actorReadiness;
        private readonly string _suspensionReason;
        private readonly string _reason;

        public PlayerEntry(PlayerSlotId playerSlotId, ActorId actorId, string reason = null)
            : this(
                playerSlotId,
                actorId,
                PlayerEntryState.Configured,
                new ActorReadinessSnapshot(ActorReadinessState.NotReady, string.Empty),
                string.Empty,
                reason)
        {
        }

        public PlayerEntry(
            PlayerSlotId playerSlotId,
            ActorId actorId,
            PlayerEntryState state,
            ActorReadinessSnapshot actorReadiness,
            string suspensionReason,
            string reason)
        {
            Validate(playerSlotId, actorId, state, actorReadiness, suspensionReason);

            _playerSlotId = playerSlotId;
            _actorId = actorId;
            _state = state;
            _actorReadiness = actorReadiness;
            _suspensionReason = suspensionReason.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId => _playerSlotId;

        public ActorId ActorId => _actorId;

        public PlayerEntryState State => _state;

        public ActorReadinessSnapshot ActorReadiness => _actorReadiness;

        public bool IsActorReadyForView => _actorReadiness.IsReadyForView;

        public bool IsActorReadyForControl => _actorReadiness.IsReadyForControl;

        public string SuspensionReason => _suspensionReason;

        public string Reason => _reason;

        public PlayerEntrySnapshot CreateSnapshot()
        {
            return new PlayerEntrySnapshot(_playerSlotId, _actorId, _state, _actorReadiness, _suspensionReason, _reason);
        }

        public PlayerEntry WithState(PlayerEntryState state, string reason = null)
        {
            return new PlayerEntry(_playerSlotId, _actorId, state, _actorReadiness, _suspensionReason, reason);
        }

        public PlayerEntry WithActorReadiness(ActorReadinessSnapshot actorReadiness, string reason = null)
        {
            return new PlayerEntry(_playerSlotId, _actorId, _state, actorReadiness, _suspensionReason, reason);
        }

        public PlayerEntry WithSuspension(string suspensionReason, string reason = null)
        {
            return new PlayerEntry(_playerSlotId, _actorId, PlayerEntryState.Suspended, _actorReadiness, suspensionReason, reason);
        }

        public PlayerEntry Released(string reason = null)
        {
            return new PlayerEntry(_playerSlotId, _actorId, PlayerEntryState.Released, _actorReadiness, string.Empty, reason);
        }

        public bool Equals(PlayerEntry other)
        {
            return other != null && CreateSnapshot().Equals(other.CreateSnapshot());
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return CreateSnapshot().GetHashCode();
        }

        public override string ToString()
        {
            return CreateSnapshot().ToString();
        }

        internal static void Validate(
            PlayerSlotId playerSlotId,
            ActorId actorId,
            PlayerEntryState state,
            ActorReadinessSnapshot actorReadiness,
            string suspensionReason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerEntry requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            if (!actorId.IsValid)
            {
                throw new ArgumentException("PlayerEntry requires a valid ActorId.", nameof(actorId));
            }

            if (!Enum.IsDefined(typeof(PlayerEntryState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "PlayerEntry state is not defined.");
            }

            if (RequiresActorReadyForView(state) && !actorReadiness.IsReadyForView)
            {
                throw new InvalidOperationException($"PlayerEntry state '{state}' requires Actor readiness for view.");
            }

            if (state == PlayerEntryState.Suspended && string.IsNullOrWhiteSpace(suspensionReason))
            {
                throw new ArgumentException("Suspended PlayerEntry requires an explicit suspension reason.", nameof(suspensionReason));
            }
        }

        private static bool RequiresActorReadyForView(PlayerEntryState state)
        {
            return state == PlayerEntryState.ActorReady
                || state == PlayerEntryState.ViewBound
                || state == PlayerEntryState.Active
                || state == PlayerEntryState.Suspended;
        }
    }
}
