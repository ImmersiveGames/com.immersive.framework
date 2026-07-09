using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable result for explicit Unity PlayerInput bridge operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity PlayerInput bridge operation result.")]
    public readonly struct UnityPlayerInputBridgeResult : IEquatable<UnityPlayerInputBridgeResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _controlBindingTargetName;
        private readonly string _bridgeTargetName;
        private readonly string _unityPlayerInputName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsControl;
        private readonly bool _bridgesUnityPlayerInput;

        public UnityPlayerInputBridgeResult(
            UnityPlayerInputBridgeStatus status,
            UnityPlayerInputBridgeFailureKind failureKind,
            string playerSlotIdText,
            string controlBindingTargetName,
            string bridgeTargetName,
            string unityPlayerInputName,
            string source,
            string reason,
            string message,
            bool bindsControl,
            bool bridgesUnityPlayerInput)
        {
            if (!Enum.IsDefined(typeof(UnityPlayerInputBridgeStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unity PlayerInput bridge status is not defined.");
            }

            if (!Enum.IsDefined(typeof(UnityPlayerInputBridgeFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "Unity PlayerInput bridge failure kind is not defined.");
            }

            if (status == UnityPlayerInputBridgeStatus.Succeeded && failureKind != UnityPlayerInputBridgeFailureKind.None)
            {
                throw new ArgumentException("Succeeded Unity PlayerInput bridge results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != UnityPlayerInputBridgeStatus.Succeeded && failureKind == UnityPlayerInputBridgeFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp Unity PlayerInput bridge results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _controlBindingTargetName = controlBindingTargetName.NormalizeText();
            _bridgeTargetName = bridgeTargetName.NormalizeText();
            _unityPlayerInputName = unityPlayerInputName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsControl = status == UnityPlayerInputBridgeStatus.Succeeded && bindsControl;
            _bridgesUnityPlayerInput = status == UnityPlayerInputBridgeStatus.Succeeded && bridgesUnityPlayerInput;
        }

        public UnityPlayerInputBridgeStatus Status { get; }

        public UnityPlayerInputBridgeFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string ControlBindingTargetName => _controlBindingTargetName;

        public string BridgeTargetName => _bridgeTargetName;

        public string UnityPlayerInputName => _unityPlayerInputName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == UnityPlayerInputBridgeStatus.Succeeded;

        public bool Failed => Status == UnityPlayerInputBridgeStatus.Failed;

        public bool NoOp => Status == UnityPlayerInputBridgeStatus.NoOp;

        public bool BindsView => false;

        public bool BindsControl => _bindsControl;

        public bool BridgesUnityPlayerInput => _bridgesUnityPlayerInput;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' controlBindingTarget='{_controlBindingTargetName.ToDiagnosticText()}' bridgeTarget='{_bridgeTargetName.ToDiagnosticText()}' playerInput='{_unityPlayerInputName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' unityPlayerInputBridge='{BridgesUnityPlayerInput}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(UnityPlayerInputBridgeResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_controlBindingTargetName, other._controlBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_bridgeTargetName, other._bridgeTargetName, StringComparison.Ordinal)
                && string.Equals(_unityPlayerInputName, other._unityPlayerInputName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsControl == other._bindsControl
                && _bridgesUnityPlayerInput == other._bridgesUnityPlayerInput;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityPlayerInputBridgeResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ (int)FailureKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_playerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_controlBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bridgeTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_unityPlayerInputName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_message ?? string.Empty);
                hash = (hash * 397) ^ _bindsControl.GetHashCode();
                hash = (hash * 397) ^ _bridgesUnityPlayerInput.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static UnityPlayerInputBridgeResult Success(
            PlayerSlotId playerSlotId,
            string controlBindingTargetName,
            string bridgeTargetName,
            string unityPlayerInputName,
            string source,
            string reason,
            string message,
            bool bridgesUnityPlayerInput = true)
        {
            return new UnityPlayerInputBridgeResult(
                UnityPlayerInputBridgeStatus.Succeeded,
                UnityPlayerInputBridgeFailureKind.None,
                playerSlotId.StableText,
                controlBindingTargetName,
                bridgeTargetName,
                unityPlayerInputName,
                source,
                reason,
                message,
                true,
                bridgesUnityPlayerInput);
        }

        public static UnityPlayerInputBridgeResult Failure(
            UnityPlayerInputBridgeFailureKind failureKind,
            string playerSlotIdText,
            string controlBindingTargetName,
            string bridgeTargetName,
            string unityPlayerInputName,
            string source,
            string reason,
            string message)
        {
            return new UnityPlayerInputBridgeResult(
                UnityPlayerInputBridgeStatus.Failed,
                failureKind,
                playerSlotIdText,
                controlBindingTargetName,
                bridgeTargetName,
                unityPlayerInputName,
                source,
                reason,
                message,
                false,
                false);
        }

        public static UnityPlayerInputBridgeResult NoOperation(
            UnityPlayerInputBridgeFailureKind failureKind,
            string playerSlotIdText,
            string controlBindingTargetName,
            string bridgeTargetName,
            string unityPlayerInputName,
            string source,
            string reason,
            string message)
        {
            return new UnityPlayerInputBridgeResult(
                UnityPlayerInputBridgeStatus.NoOp,
                failureKind,
                playerSlotIdText,
                controlBindingTargetName,
                bridgeTargetName,
                unityPlayerInputName,
                source,
                reason,
                message,
                false,
                false);
        }
    }
}
