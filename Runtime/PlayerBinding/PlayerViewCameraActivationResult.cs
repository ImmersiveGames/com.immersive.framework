using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable result for explicit PlayerView camera activation operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C PlayerView camera activation adapter operation result.")]
    public readonly struct PlayerViewCameraActivationResult : IEquatable<PlayerViewCameraActivationResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _cameraTargetBindingTargetName;
        private readonly string _cameraActivationTargetName;
        private readonly string _cameraName;
        private readonly string _viewTargetName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsView;
        private readonly bool _bindsCameraTarget;
        private readonly bool _activatesCamera;

        public PlayerViewCameraActivationResult(
            PlayerViewCameraActivationStatus status,
            PlayerViewCameraActivationFailureKind failureKind,
            string playerSlotIdText,
            string cameraTargetBindingTargetName,
            string cameraActivationTargetName,
            string cameraName,
            string viewTargetName,
            string source,
            string reason,
            string message,
            bool bindsView,
            bool bindsCameraTarget,
            bool activatesCamera)
        {
            if (!Enum.IsDefined(typeof(PlayerViewCameraActivationStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "PlayerView camera activation status is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerViewCameraActivationFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "PlayerView camera activation failure kind is not defined.");
            }

            if (status == PlayerViewCameraActivationStatus.Succeeded && failureKind != PlayerViewCameraActivationFailureKind.None)
            {
                throw new ArgumentException("Succeeded PlayerView camera activation results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != PlayerViewCameraActivationStatus.Succeeded && failureKind == PlayerViewCameraActivationFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp PlayerView camera activation results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _cameraTargetBindingTargetName = cameraTargetBindingTargetName.NormalizeText();
            _cameraActivationTargetName = cameraActivationTargetName.NormalizeText();
            _cameraName = cameraName.NormalizeText();
            _viewTargetName = viewTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsView = status == PlayerViewCameraActivationStatus.Succeeded && bindsView;
            _bindsCameraTarget = status == PlayerViewCameraActivationStatus.Succeeded && bindsCameraTarget;
            _activatesCamera = status == PlayerViewCameraActivationStatus.Succeeded && activatesCamera;
        }

        public PlayerViewCameraActivationStatus Status { get; }

        public PlayerViewCameraActivationFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string CameraTargetBindingTargetName => _cameraTargetBindingTargetName;

        public string CameraActivationTargetName => _cameraActivationTargetName;

        public string CameraName => _cameraName;

        public string ViewTargetName => _viewTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == PlayerViewCameraActivationStatus.Succeeded;

        public bool Failed => Status == PlayerViewCameraActivationStatus.Failed;

        public bool NoOp => Status == PlayerViewCameraActivationStatus.NoOp;

        public bool BindsView => _bindsView;

        public bool BindsCameraTarget => _bindsCameraTarget;

        public bool ActivatesCamera => _activatesCamera;

        public bool BindsControl => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' cameraTargetBindingTarget='{_cameraTargetBindingTargetName.ToDiagnosticText()}' cameraActivationTarget='{_cameraActivationTargetName.ToDiagnosticText()}' camera='{_cameraName.ToDiagnosticText()}' viewTarget='{_viewTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' cameraTargetBinding='{BindsCameraTarget}' cameraActivation='{ActivatesCamera}' controlBinding='{BindsControl}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(PlayerViewCameraActivationResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_cameraTargetBindingTargetName, other._cameraTargetBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraActivationTargetName, other._cameraActivationTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraName, other._cameraName, StringComparison.Ordinal)
                && string.Equals(_viewTargetName, other._viewTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsView == other._bindsView
                && _bindsCameraTarget == other._bindsCameraTarget
                && _activatesCamera == other._activatesCamera;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewCameraActivationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ (int)FailureKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_playerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraTargetBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraActivationTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_message ?? string.Empty);
                hash = (hash * 397) ^ _bindsView.GetHashCode();
                hash = (hash * 397) ^ _bindsCameraTarget.GetHashCode();
                hash = (hash * 397) ^ _activatesCamera.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static PlayerViewCameraActivationResult Success(
            PlayerSlotId playerSlotId,
            string cameraTargetBindingTargetName,
            string cameraActivationTargetName,
            string cameraName,
            string viewTargetName,
            string source,
            string reason,
            string message,
            bool bindsView = true,
            bool bindsCameraTarget = true,
            bool activatesCamera = true)
        {
            return new PlayerViewCameraActivationResult(
                PlayerViewCameraActivationStatus.Succeeded,
                PlayerViewCameraActivationFailureKind.None,
                playerSlotId.StableText,
                cameraTargetBindingTargetName,
                cameraActivationTargetName,
                cameraName,
                viewTargetName,
                source,
                reason,
                message,
                bindsView,
                bindsCameraTarget,
                activatesCamera);
        }

        public static PlayerViewCameraActivationResult Failure(
            PlayerViewCameraActivationFailureKind failureKind,
            string playerSlotIdText,
            string cameraTargetBindingTargetName,
            string cameraActivationTargetName,
            string cameraName,
            string viewTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewCameraActivationResult(
                PlayerViewCameraActivationStatus.Failed,
                failureKind,
                playerSlotIdText,
                cameraTargetBindingTargetName,
                cameraActivationTargetName,
                cameraName,
                viewTargetName,
                source,
                reason,
                message,
                false,
                false,
                false);
        }

        public static PlayerViewCameraActivationResult NoOperation(
            PlayerViewCameraActivationFailureKind failureKind,
            string playerSlotIdText,
            string cameraTargetBindingTargetName,
            string cameraActivationTargetName,
            string cameraName,
            string viewTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewCameraActivationResult(
                PlayerViewCameraActivationStatus.NoOp,
                failureKind,
                playerSlotIdText,
                cameraTargetBindingTargetName,
                cameraActivationTargetName,
                cameraName,
                viewTargetName,
                source,
                reason,
                message,
                false,
                false,
                false);
        }
    }
}
