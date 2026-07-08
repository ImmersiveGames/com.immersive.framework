using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Immutable passive PlayerControl model.
    /// It stores PlayerSlot identity, passive control state and PlayerEntry evidence.
    /// It does not bind InputActions, enable movement, route PlayerInput, drive gameplay or activate ControlBinding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49I passive PlayerControl model.")]
    public sealed class PlayerControl : IPlayerControl, IEquatable<PlayerControl>
    {
        private readonly PlayerSlotId _playerSlotId;
        private readonly PlayerControlState _state;
        private readonly bool _hasPlayerEntryEvidence;
        private readonly PlayerEntryState _playerEntryState;
        private readonly bool _isPlayerEntryReadyForControl;
        private readonly bool _hasControlTarget;
        private readonly string _controlTargetName;
        private readonly string _inputSourceId;
        private readonly string _suspensionReason;
        private readonly string _reason;

        public PlayerControl(PlayerSlotId playerSlotId, string reason = null)
            : this(
                playerSlotId,
                PlayerControlState.Declared,
                false,
                PlayerEntryState.Configured,
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                reason)
        {
        }

        public PlayerControl(
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
            Validate(
                playerSlotId,
                state,
                hasPlayerEntryEvidence,
                playerEntryState,
                isPlayerEntryReadyForControl,
                suspensionReason);

            _playerSlotId = playerSlotId;
            _state = state;
            _hasPlayerEntryEvidence = hasPlayerEntryEvidence;
            _playerEntryState = playerEntryState;
            _isPlayerEntryReadyForControl = isPlayerEntryReadyForControl;
            _hasControlTarget = hasControlTarget;
            _controlTargetName = controlTargetName.NormalizeText();
            _inputSourceId = inputSourceId.NormalizeText();
            _suspensionReason = suspensionReason.NormalizeText();
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId => _playerSlotId;

        public PlayerControlState State => _state;

        public bool HasPlayerEntryEvidence => _hasPlayerEntryEvidence;

        public PlayerEntryState PlayerEntryState => _playerEntryState;

        public bool IsPlayerEntryReadyForControl => _isPlayerEntryReadyForControl;

        public bool HasControlTarget => _hasControlTarget;

        public string ControlTargetName => _controlTargetName;

        public string InputSourceId => _inputSourceId;

        public bool IsEligibleForBoundControl => _hasPlayerEntryEvidence && _playerEntryState == PlayerEntryState.Active;

        public bool IsEligibleForActiveControl => IsEligibleForBoundControl && _isPlayerEntryReadyForControl;

        public string SuspensionReason => _suspensionReason;

        public string Reason => _reason;

        public PlayerControlSnapshot CreateSnapshot()
        {
            return new PlayerControlSnapshot(
                _playerSlotId,
                _state,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _isPlayerEntryReadyForControl,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                _suspensionReason,
                _reason);
        }

        public PlayerControl WithState(PlayerControlState state, string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                state,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _isPlayerEntryReadyForControl,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                _suspensionReason,
                reason);
        }

        public PlayerControl WithPlayerEntryEvidence(PlayerEntrySnapshot playerEntry, string reason = null)
        {
            if (playerEntry.PlayerSlotId != _playerSlotId)
            {
                throw new InvalidOperationException($"PlayerControl PlayerEntry evidence has a different PlayerSlot. Control='{_playerSlotId.StableText}' PlayerEntry='{playerEntry.PlayerSlotId.StableText}'.");
            }

            return new PlayerControl(
                _playerSlotId,
                _state,
                true,
                playerEntry.State,
                playerEntry.IsActorReadyForControl,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                _suspensionReason,
                reason);
        }

        public PlayerControl WithoutPlayerEntryEvidence(string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                _state,
                false,
                PlayerEntryState.Configured,
                false,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                _suspensionReason,
                reason);
        }

        public PlayerControl WithControlTarget(bool hasControlTarget, string controlTargetName, string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                _state,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _isPlayerEntryReadyForControl,
                hasControlTarget,
                controlTargetName,
                _inputSourceId,
                _suspensionReason,
                reason);
        }

        public PlayerControl WithInputSource(string inputSourceId, string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                _state,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _isPlayerEntryReadyForControl,
                _hasControlTarget,
                _controlTargetName,
                inputSourceId,
                _suspensionReason,
                reason);
        }

        public PlayerControl WithSuspension(string suspensionReason, string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                PlayerControlState.Suspended,
                _hasPlayerEntryEvidence,
                _playerEntryState,
                _isPlayerEntryReadyForControl,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                suspensionReason,
                reason);
        }

        public PlayerControl Released(string reason = null)
        {
            return new PlayerControl(
                _playerSlotId,
                PlayerControlState.Released,
                false,
                PlayerEntryState.Released,
                false,
                _hasControlTarget,
                _controlTargetName,
                _inputSourceId,
                string.Empty,
                reason);
        }

        public bool Equals(PlayerControl other)
        {
            return other != null && CreateSnapshot().Equals(other.CreateSnapshot());
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControl other && Equals(other);
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
            PlayerControlState state,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            bool isPlayerEntryReadyForControl,
            string suspensionReason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerControl requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            if (!Enum.IsDefined(typeof(PlayerControlState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "PlayerControl state is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerEntryState), playerEntryState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerEntryState), playerEntryState, "PlayerControl PlayerEntry state is not defined.");
            }

            if (state == PlayerControlState.Bound && (!hasPlayerEntryEvidence || playerEntryState != PlayerEntryState.Active))
            {
                throw new InvalidOperationException("Bound PlayerControl requires PlayerEntry evidence in Active state.");
            }

            if (state == PlayerControlState.Active && (!hasPlayerEntryEvidence || playerEntryState != PlayerEntryState.Active || !isPlayerEntryReadyForControl))
            {
                throw new InvalidOperationException("Active PlayerControl requires PlayerEntry evidence in Active state with Actor readiness for control.");
            }

            if (state == PlayerControlState.Suspended && string.IsNullOrWhiteSpace(suspensionReason))
            {
                throw new ArgumentException("Suspended PlayerControl requires an explicit suspension reason.", nameof(suspensionReason));
            }
        }
    }
}
