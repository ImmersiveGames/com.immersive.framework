using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Immutable passive PlayerView model.
    /// It stores PlayerSlot identity, passive view state and optional PlayerEntry state evidence.
    /// It does not activate cameras, drive Cinemachine, bind control, process input or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49G passive PlayerView model.")]
    public sealed class PlayerView : IPlayerView, IEquatable<PlayerView>
    {
        private readonly PlayerSlotId _playerSlotId;
        private readonly PlayerViewState _state;
        private readonly bool _hasCameraEvidence;
        private readonly bool _hasTargetEvidence;
        private readonly bool _hasPlayerEntryEvidence;
        private readonly PlayerEntryState _playerEntryState;
        private readonly string _cameraName;
        private readonly string _targetName;
        private readonly string _reason;

        public PlayerView(PlayerSlotId playerSlotId, string reason = null)
            : this(
                playerSlotId,
                PlayerViewState.Declared,
                false,
                false,
                false,
                PlayerEntryState.Configured,
                string.Empty,
                string.Empty,
                reason)
        {
        }

        public PlayerView(
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
            Validate(playerSlotId, state, hasPlayerEntryEvidence, playerEntryState, reason);

            _playerSlotId = playerSlotId;
            _state = state;
            _hasCameraEvidence = hasCameraEvidence;
            _hasTargetEvidence = hasTargetEvidence;
            _hasPlayerEntryEvidence = hasPlayerEntryEvidence;
            _playerEntryState = playerEntryState;
            _cameraName = cameraName.NormalizeText();
            _targetName = targetName.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId => _playerSlotId;

        public PlayerViewState State => _state;

        public bool HasCameraEvidence => _hasCameraEvidence;

        public bool HasTargetEvidence => _hasTargetEvidence;

        public bool HasPlayerEntryEvidence => _hasPlayerEntryEvidence;

        public PlayerEntryState PlayerEntryState => _playerEntryState;

        public string CameraName => _cameraName;

        public string TargetName => _targetName;

        public string Reason => _reason;

        public bool IsEligibleForActiveView => _state == PlayerViewState.Active && PlayerViewSnapshot.IsViewBoundOrActiveEntry(_playerEntryState);

        public PlayerViewSnapshot CreateSnapshot()
        {
            return new PlayerViewSnapshot(
                _playerSlotId,
                _state,
                _hasCameraEvidence,
                _hasTargetEvidence,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _cameraName,
                _targetName,
                _reason);
        }

        public PlayerView WithState(PlayerViewState state, string reason = null)
        {
            return new PlayerView(
                _playerSlotId,
                state,
                _hasCameraEvidence,
                _hasTargetEvidence,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _cameraName,
                _targetName,
                reason);
        }

        public PlayerView WithPlayerEntryEvidence(PlayerEntrySnapshot playerEntrySnapshot, string reason = null)
        {
            if (playerEntrySnapshot.PlayerSlotId != _playerSlotId)
            {
                throw new InvalidOperationException(
                    $"PlayerView PlayerEntry evidence must match PlayerSlotId. PlayerView='{_playerSlotId.StableText}' PlayerEntry='{playerEntrySnapshot.PlayerSlotId.StableText}'.");
            }

            return new PlayerView(
                _playerSlotId,
                _state,
                _hasCameraEvidence,
                _hasTargetEvidence,
                true,
                playerEntrySnapshot.State,
                _cameraName,
                _targetName,
                reason);
        }

        public PlayerView Released(string reason = null)
        {
            return new PlayerView(
                _playerSlotId,
                PlayerViewState.Released,
                _hasCameraEvidence,
                _hasTargetEvidence,
                false,
                PlayerEntryState.Released,
                _cameraName,
                _targetName,
                reason);
        }

        public bool Equals(PlayerView other)
        {
            return other != null && CreateSnapshot().Equals(other.CreateSnapshot());
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerView other && Equals(other);
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
            PlayerViewState state,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerView requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            if (!Enum.IsDefined(typeof(PlayerViewState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "PlayerView state is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerEntryState), playerEntryState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerEntryState), playerEntryState, "PlayerEntry state evidence is not defined.");
            }

            if (RequiresViewBoundEntry(state) && (!hasPlayerEntryEvidence || !PlayerViewSnapshot.IsViewBoundOrActiveEntry(playerEntryState)))
            {
                throw new InvalidOperationException($"PlayerView state '{state}' requires PlayerEntry evidence in ViewBound or Active state.");
            }

            if (state == PlayerViewState.Suspended && string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Suspended PlayerView requires an explicit diagnostic reason.", nameof(reason));
            }
        }

        private static bool RequiresViewBoundEntry(PlayerViewState state)
        {
            return state == PlayerViewState.Bound || state == PlayerViewState.Active;
        }
    }
}
