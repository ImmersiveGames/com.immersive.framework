using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that an explicit Unity PlayerInput bridge activated one configured action map.
    /// This is not InputAction routing, movement, gameplay command execution, actor spawning or runtime lifecycle ownership.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52C Unity PlayerInput action-map activation snapshot.")]
    public readonly struct UnityPlayerInputActivationSnapshot : IEquatable<UnityPlayerInputActivationSnapshot>
    {
        private readonly string _controlTargetName;
        private readonly string _inputSourceId;
        private readonly string _controlBindingTargetName;
        private readonly string _unityPlayerInputName;
        private readonly string _bridgeTargetName;
        private readonly string _activationTargetName;
        private readonly string _actionMapName;
        private readonly string _previousActionMapName;
        private readonly string _source;
        private readonly string _reason;

        public UnityPlayerInputActivationSnapshot(
            PlayerSlotId playerSlotId,
            PlayerControlState playerControlState,
            PlayerEntryState playerEntryState,
            bool isPlayerEntryReadyForControl,
            string controlTargetName,
            string inputSourceId,
            string controlBindingTargetName,
            string unityPlayerInputName,
            string bridgeTargetName,
            string activationTargetName,
            string actionMapName,
            string previousActionMapName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("Unity PlayerInput activation snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
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
            _activationTargetName = activationTargetName.NormalizeText();
            _actionMapName = actionMapName.NormalizeText();
            _previousActionMapName = previousActionMapName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationSnapshot));
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

        public string ActivationTargetName => _activationTargetName;

        public string ActionMapName => _actionMapName;

        public string PreviousActionMapName => _previousActionMapName;

        public string Source => _source;

        public string Reason => _reason;

        public bool IsActivePlayerControl => PlayerControlState == PlayerControlState.Active;

        public bool BindsView => false;

        public bool BindsControl => true;

        public bool BridgesUnityPlayerInput => true;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => true;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(UnityPlayerInputActivationSnapshot other)
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
                && string.Equals(_activationTargetName, other._activationTargetName, StringComparison.Ordinal)
                && string.Equals(_actionMapName, other._actionMapName, StringComparison.Ordinal)
                && string.Equals(_previousActionMapName, other._previousActionMapName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityPlayerInputActivationSnapshot other && Equals(other);
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
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_activationTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_actionMapName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_previousActionMapName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' playerControlState='{PlayerControlState}' playerEntryState='{PlayerEntryState}' playerEntryReadyForControl='{IsPlayerEntryReadyForControl}' controlTarget='{_controlTargetName.ToDiagnosticText()}' inputSource='{_inputSourceId.ToDiagnosticText()}' controlBindingTarget='{_controlBindingTargetName.ToDiagnosticText()}' playerInput='{_unityPlayerInputName.ToDiagnosticText()}' bridgeTarget='{_bridgeTargetName.ToDiagnosticText()}' activationTarget='{_activationTargetName.ToDiagnosticText()}' actionMap='{_actionMapName.ToDiagnosticText()}' previousActionMap='{_previousActionMapName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' unityPlayerInputBridge='{BridgesUnityPlayerInput}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static UnityPlayerInputActivationSnapshot FromUnityPlayerInputBridge(
            UnityPlayerInputBridgeSnapshot bridge,
            string activationTargetName,
            string actionMapName,
            string previousActionMapName,
            string source,
            string reason)
        {
            return new UnityPlayerInputActivationSnapshot(
                bridge.PlayerSlotId,
                bridge.PlayerControlState,
                bridge.PlayerEntryState,
                bridge.IsPlayerEntryReadyForControl,
                bridge.ControlTargetName,
                bridge.InputSourceId,
                bridge.ControlBindingTargetName,
                bridge.UnityPlayerInputName,
                bridge.BridgeTargetName,
                activationTargetName,
                actionMapName,
                previousActionMapName,
                source,
                reason);
        }
    }
}
