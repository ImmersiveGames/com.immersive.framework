using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that a PlayerControl binding was bridged to an explicit Unity PlayerInput.
    /// This is not input activation, InputAction routing, action-map switching, movement, gameplay execution or spawning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity PlayerInput bridge snapshot.")]
    public readonly struct UnityPlayerInputBridgeSnapshot : IEquatable<UnityPlayerInputBridgeSnapshot>
    {
        private readonly string _controlTargetName;
        private readonly string _inputSourceId;
        private readonly string _controlBindingTargetName;
        private readonly string _unityPlayerInputName;
        private readonly string _bridgeTargetName;
        private readonly string _source;
        private readonly string _reason;

        public UnityPlayerInputBridgeSnapshot(
            PlayerSlotId playerSlotId,
            PlayerControlState playerControlState,
            PlayerEntryState playerEntryState,
            bool isPlayerEntryReadyForControl,
            string controlTargetName,
            string inputSourceId,
            string controlBindingTargetName,
            string unityPlayerInputName,
            string bridgeTargetName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("Unity PlayerInput bridge snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
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
            _controlBindingTargetName = controlBindingTargetName.NormalizeText();
            _unityPlayerInputName = unityPlayerInputName.NormalizeText();
            _bridgeTargetName = bridgeTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeSnapshot));
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerControlState PlayerControlState { get; }

        public PlayerEntryState PlayerEntryState { get; }

        public bool IsPlayerEntryReadyForControl { get; }

        public string ControlTargetName => _controlTargetName;

        public string InputSourceId => _inputSourceId;

        public string ControlBindingTargetName => _controlBindingTargetName;

        public string UnityPlayerInputName => _unityPlayerInputName;

        public string BridgeTargetName => _bridgeTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public bool IsActivePlayerControl => PlayerControlState == PlayerControlState.Active;

        public bool BindsView => false;

        public bool BindsControl => true;

        public bool BridgesUnityPlayerInput => true;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(UnityPlayerInputBridgeSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && PlayerControlState == other.PlayerControlState
                && PlayerEntryState == other.PlayerEntryState
                && IsPlayerEntryReadyForControl == other.IsPlayerEntryReadyForControl
                && string.Equals(_controlTargetName, other._controlTargetName, StringComparison.Ordinal)
                && string.Equals(_inputSourceId, other._inputSourceId, StringComparison.Ordinal)
                && string.Equals(_controlBindingTargetName, other._controlBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_unityPlayerInputName, other._unityPlayerInputName, StringComparison.Ordinal)
                && string.Equals(_bridgeTargetName, other._bridgeTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityPlayerInputBridgeSnapshot other && Equals(other);
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
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_controlBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_unityPlayerInputName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bridgeTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' playerControlState='{PlayerControlState}' playerEntryState='{PlayerEntryState}' playerEntryReadyForControl='{IsPlayerEntryReadyForControl}' controlTarget='{_controlTargetName.ToDiagnosticText()}' inputSource='{_inputSourceId.ToDiagnosticText()}' controlBindingTarget='{_controlBindingTargetName.ToDiagnosticText()}' playerInput='{_unityPlayerInputName.ToDiagnosticText()}' bridgeTarget='{_bridgeTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' unityPlayerInputBridge='{BridgesUnityPlayerInput}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static UnityPlayerInputBridgeSnapshot FromPlayerControlBinding(
            PlayerControlBindingSnapshot controlBinding,
            string unityPlayerInputName,
            string bridgeTargetName,
            string source,
            string reason)
        {
            return new UnityPlayerInputBridgeSnapshot(
                controlBinding.PlayerSlotId,
                controlBinding.PlayerControlState,
                controlBinding.PlayerEntryState,
                controlBinding.IsPlayerEntryReadyForControl,
                controlBinding.ControlTargetName,
                controlBinding.InputSourceId,
                controlBinding.BindingTargetName,
                unityPlayerInputName,
                bridgeTargetName,
                source,
                reason);
        }
    }
}
