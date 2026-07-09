using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that a PlayerControl binding was applied to a target.
    /// This is not InputAction routing, PlayerInput action-map switching, movement, camera activation or spawning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A PlayerControl binding target snapshot.")]
    public readonly struct PlayerControlBindingSnapshot : IEquatable<PlayerControlBindingSnapshot>
    {
        private readonly string _controlTargetName;
        private readonly string _inputSourceId;
        private readonly string _bindingTargetName;
        private readonly string _source;
        private readonly string _reason;

        public PlayerControlBindingSnapshot(
            PlayerSlotId playerSlotId,
            PlayerControlState playerControlState,
            PlayerEntryState playerEntryState,
            bool isPlayerEntryReadyForControl,
            string controlTargetName,
            string inputSourceId,
            string bindingTargetName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerControl binding snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            if (!Enum.IsDefined(typeof(PlayerControlState), playerControlState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerControlState), playerControlState, "PlayerControl state is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerEntryState), playerEntryState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerEntryState), playerEntryState, "PlayerEntry state is not defined.");
            }

            PlayerSlotId = playerSlotId;
            PlayerControlState = playerControlState;
            PlayerEntryState = playerEntryState;
            IsPlayerEntryReadyForControl = isPlayerEntryReadyForControl;
            _controlTargetName = controlTargetName.NormalizeText();
            _inputSourceId = inputSourceId.NormalizeText();
            _bindingTargetName = bindingTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerControlBindingSnapshot));
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerControlState PlayerControlState { get; }

        public PlayerEntryState PlayerEntryState { get; }

        public bool IsPlayerEntryReadyForControl { get; }

        public string ControlTargetName => _controlTargetName;

        public string InputSourceId => _inputSourceId;

        public string BindingTargetName => _bindingTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public bool IsActivePlayerControl => PlayerControlState == PlayerControlState.Active;

        public bool BindsView => false;

        public bool BindsControl => true;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(PlayerControlBindingSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && PlayerControlState == other.PlayerControlState
                && PlayerEntryState == other.PlayerEntryState
                && IsPlayerEntryReadyForControl == other.IsPlayerEntryReadyForControl
                && string.Equals(_controlTargetName, other._controlTargetName, StringComparison.Ordinal)
                && string.Equals(_inputSourceId, other._inputSourceId, StringComparison.Ordinal)
                && string.Equals(_bindingTargetName, other._bindingTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControlBindingSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ (int)PlayerControlState;
                hash = (hash * 397) ^ (int)PlayerEntryState;
                hash = (hash * 397) ^ IsPlayerEntryReadyForControl.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_controlTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_inputSourceId ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' playerControlState='{PlayerControlState}' playerEntryState='{PlayerEntryState}' playerEntryReadyForControl='{IsPlayerEntryReadyForControl}' controlTarget='{_controlTargetName.ToDiagnosticText()}' inputSource='{_inputSourceId.ToDiagnosticText()}' bindingTarget='{_bindingTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static PlayerControlBindingSnapshot FromPlayerControl(
            PlayerControlSnapshot playerControl,
            string bindingTargetName,
            string source,
            string reason)
        {
            return new PlayerControlBindingSnapshot(
                playerControl.PlayerSlotId,
                playerControl.State,
                playerControl.PlayerEntryState,
                playerControl.IsPlayerEntryReadyForControl,
                playerControl.ControlTargetName,
                playerControl.InputSourceId,
                bindingTargetName,
                source,
                reason);
        }
    }
}
