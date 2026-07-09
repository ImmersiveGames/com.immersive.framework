using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result for explicit Unity PlayerInput action-map activation/clear operations.
    /// The result never represents movement, gameplay command execution, actor spawning or InputAction routing.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52C Unity PlayerInput action-map activation result.")]
    public readonly struct UnityPlayerInputActivationResult : IEquatable<UnityPlayerInputActivationResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _bridgeTargetName;
        private readonly string _activationTargetName;
        private readonly string _unityPlayerInputName;
        private readonly string _actionMapName;
        private readonly string _previousActionMapName;
        private readonly string _currentActionMapName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsControl;
        private readonly bool _bridgesUnityPlayerInput;
        private readonly bool _activatesInput;

        public UnityPlayerInputActivationResult(
            UnityPlayerInputActivationStatus status,
            UnityPlayerInputActivationFailureKind failureKind,
            string playerSlotIdText,
            string bridgeTargetName,
            string activationTargetName,
            string unityPlayerInputName,
            string actionMapName,
            string previousActionMapName,
            string currentActionMapName,
            string source,
            string reason,
            string message,
            bool bindsControl,
            bool bridgesUnityPlayerInput,
            bool activatesInput)
        {
            if (!Enum.IsDefined(typeof(UnityPlayerInputActivationStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unity PlayerInput activation status is not defined.");
            }

            if (!Enum.IsDefined(typeof(UnityPlayerInputActivationFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "Unity PlayerInput activation failure kind is not defined.");
            }

            if (status == UnityPlayerInputActivationStatus.Succeeded && failureKind != UnityPlayerInputActivationFailureKind.None)
            {
                throw new ArgumentException("Succeeded Unity PlayerInput activation results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != UnityPlayerInputActivationStatus.Succeeded && failureKind == UnityPlayerInputActivationFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp Unity PlayerInput activation results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _bridgeTargetName = bridgeTargetName.NormalizeText();
            _activationTargetName = activationTargetName.NormalizeText();
            _unityPlayerInputName = unityPlayerInputName.NormalizeText();
            _actionMapName = actionMapName.NormalizeText();
            _previousActionMapName = previousActionMapName.NormalizeText();
            _currentActionMapName = currentActionMapName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsControl = status == UnityPlayerInputActivationStatus.Succeeded && bindsControl;
            _bridgesUnityPlayerInput = status == UnityPlayerInputActivationStatus.Succeeded && bridgesUnityPlayerInput;
            _activatesInput = status == UnityPlayerInputActivationStatus.Succeeded && activatesInput;
        }

        public UnityPlayerInputActivationStatus Status { get; }

        public UnityPlayerInputActivationFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string BridgeTargetName => _bridgeTargetName;

        public string ActivationTargetName => _activationTargetName;

        public string UnityPlayerInputName => _unityPlayerInputName;

        public string ActionMapName => _actionMapName;

        public string PreviousActionMapName => _previousActionMapName;

        public string CurrentActionMapName => _currentActionMapName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == UnityPlayerInputActivationStatus.Succeeded;

        public bool Failed => Status == UnityPlayerInputActivationStatus.Failed;

        public bool NoOp => Status == UnityPlayerInputActivationStatus.NoOp;

        public bool BindsView => false;

        public bool BindsControl => _bindsControl;

        public bool BridgesUnityPlayerInput => _bridgesUnityPlayerInput;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => _activatesInput;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' bridgeTarget='{_bridgeTargetName.ToDiagnosticText()}' activationTarget='{_activationTargetName.ToDiagnosticText()}' playerInput='{_unityPlayerInputName.ToDiagnosticText()}' actionMap='{_actionMapName.ToDiagnosticText()}' previousActionMap='{_previousActionMapName.ToDiagnosticText()}' currentActionMap='{_currentActionMapName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' unityPlayerInputBridge='{BridgesUnityPlayerInput}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(UnityPlayerInputActivationResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_bridgeTargetName, other._bridgeTargetName, StringComparison.Ordinal)
                && string.Equals(_activationTargetName, other._activationTargetName, StringComparison.Ordinal)
                && string.Equals(_unityPlayerInputName, other._unityPlayerInputName, StringComparison.Ordinal)
                && string.Equals(_actionMapName, other._actionMapName, StringComparison.Ordinal)
                && string.Equals(_previousActionMapName, other._previousActionMapName, StringComparison.Ordinal)
                && string.Equals(_currentActionMapName, other._currentActionMapName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsControl == other._bindsControl
                && _bridgesUnityPlayerInput == other._bridgesUnityPlayerInput
                && _activatesInput == other._activatesInput;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityPlayerInputActivationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ (int)FailureKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_playerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bridgeTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_activationTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_unityPlayerInputName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_actionMapName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_previousActionMapName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_currentActionMapName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_message ?? string.Empty);
                hash = (hash * 397) ^ _bindsControl.GetHashCode();
                hash = (hash * 397) ^ _bridgesUnityPlayerInput.GetHashCode();
                hash = (hash * 397) ^ _activatesInput.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static UnityPlayerInputActivationResult Success(
            PlayerSlotId playerSlotId,
            string bridgeTargetName,
            string activationTargetName,
            string unityPlayerInputName,
            string actionMapName,
            string previousActionMapName,
            string currentActionMapName,
            string source,
            string reason,
            string message,
            bool activatesInput = true)
        {
            return new UnityPlayerInputActivationResult(
                UnityPlayerInputActivationStatus.Succeeded,
                UnityPlayerInputActivationFailureKind.None,
                playerSlotId.StableText,
                bridgeTargetName,
                activationTargetName,
                unityPlayerInputName,
                actionMapName,
                previousActionMapName,
                currentActionMapName,
                source,
                reason,
                message,
                true,
                true,
                activatesInput);
        }

        public static UnityPlayerInputActivationResult Failure(
            UnityPlayerInputActivationFailureKind failureKind,
            string playerSlotIdText,
            string bridgeTargetName,
            string activationTargetName,
            string unityPlayerInputName,
            string actionMapName,
            string previousActionMapName,
            string currentActionMapName,
            string source,
            string reason,
            string message)
        {
            return new UnityPlayerInputActivationResult(
                UnityPlayerInputActivationStatus.Failed,
                failureKind,
                playerSlotIdText,
                bridgeTargetName,
                activationTargetName,
                unityPlayerInputName,
                actionMapName,
                previousActionMapName,
                currentActionMapName,
                source,
                reason,
                message,
                false,
                false,
                false);
        }

        public static UnityPlayerInputActivationResult NoOperation(
            UnityPlayerInputActivationFailureKind failureKind,
            string playerSlotIdText,
            string bridgeTargetName,
            string activationTargetName,
            string unityPlayerInputName,
            string actionMapName,
            string previousActionMapName,
            string currentActionMapName,
            string source,
            string reason,
            string message)
        {
            return new UnityPlayerInputActivationResult(
                UnityPlayerInputActivationStatus.NoOp,
                failureKind,
                playerSlotIdText,
                bridgeTargetName,
                activationTargetName,
                unityPlayerInputName,
                actionMapName,
                previousActionMapName,
                currentActionMapName,
                source,
                reason,
                message,
                false,
                false,
                false);
        }
    }
}
