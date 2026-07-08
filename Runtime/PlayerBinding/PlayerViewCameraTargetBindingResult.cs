using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable result for explicit PlayerView camera-target binding adapter operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B PlayerView camera-target binding adapter operation result.")]
    public readonly struct PlayerViewCameraTargetBindingResult : IEquatable<PlayerViewCameraTargetBindingResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _viewBindingTargetName;
        private readonly string _cameraTargetBindingTargetName;
        private readonly string _viewTargetName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsView;
        private readonly bool _bindsCameraTarget;

        public PlayerViewCameraTargetBindingResult(
            PlayerViewCameraTargetBindingStatus status,
            PlayerViewCameraTargetBindingFailureKind failureKind,
            string playerSlotIdText,
            string viewBindingTargetName,
            string cameraTargetBindingTargetName,
            string viewTargetName,
            string source,
            string reason,
            string message,
            bool bindsView,
            bool bindsCameraTarget)
        {
            if (!Enum.IsDefined(typeof(PlayerViewCameraTargetBindingStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "PlayerView camera-target binding status is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerViewCameraTargetBindingFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "PlayerView camera-target binding failure kind is not defined.");
            }

            if (status == PlayerViewCameraTargetBindingStatus.Succeeded && failureKind != PlayerViewCameraTargetBindingFailureKind.None)
            {
                throw new ArgumentException("Succeeded PlayerView camera-target binding results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != PlayerViewCameraTargetBindingStatus.Succeeded && failureKind == PlayerViewCameraTargetBindingFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp PlayerView camera-target binding results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _viewBindingTargetName = viewBindingTargetName.NormalizeText();
            _cameraTargetBindingTargetName = cameraTargetBindingTargetName.NormalizeText();
            _viewTargetName = viewTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsView = status == PlayerViewCameraTargetBindingStatus.Succeeded && bindsView;
            _bindsCameraTarget = status == PlayerViewCameraTargetBindingStatus.Succeeded && bindsCameraTarget;
        }

        public PlayerViewCameraTargetBindingStatus Status { get; }

        public PlayerViewCameraTargetBindingFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string ViewBindingTargetName => _viewBindingTargetName;

        public string CameraTargetBindingTargetName => _cameraTargetBindingTargetName;

        public string ViewTargetName => _viewTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == PlayerViewCameraTargetBindingStatus.Succeeded;

        public bool Failed => Status == PlayerViewCameraTargetBindingStatus.Failed;

        public bool NoOp => Status == PlayerViewCameraTargetBindingStatus.NoOp;

        public bool BindsView => _bindsView;

        public bool BindsCameraTarget => _bindsCameraTarget;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' viewBindingTarget='{_viewBindingTargetName.ToDiagnosticText()}' cameraTargetBindingTarget='{_cameraTargetBindingTargetName.ToDiagnosticText()}' viewTarget='{_viewTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' cameraTargetBinding='{BindsCameraTarget}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(PlayerViewCameraTargetBindingResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_viewBindingTargetName, other._viewBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraTargetBindingTargetName, other._cameraTargetBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_viewTargetName, other._viewTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsView == other._bindsView
                && _bindsCameraTarget == other._bindsCameraTarget;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewCameraTargetBindingResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ (int)FailureKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_playerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraTargetBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_message ?? string.Empty);
                hash = (hash * 397) ^ _bindsView.GetHashCode();
                hash = (hash * 397) ^ _bindsCameraTarget.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static PlayerViewCameraTargetBindingResult Success(
            PlayerSlotId playerSlotId,
            string viewBindingTargetName,
            string cameraTargetBindingTargetName,
            string viewTargetName,
            string source,
            string reason,
            string message,
            bool bindsView = true,
            bool bindsCameraTarget = true)
        {
            return new PlayerViewCameraTargetBindingResult(
                PlayerViewCameraTargetBindingStatus.Succeeded,
                PlayerViewCameraTargetBindingFailureKind.None,
                playerSlotId.StableText,
                viewBindingTargetName,
                cameraTargetBindingTargetName,
                viewTargetName,
                source,
                reason,
                message,
                bindsView,
                bindsCameraTarget);
        }

        public static PlayerViewCameraTargetBindingResult Failure(
            PlayerViewCameraTargetBindingFailureKind failureKind,
            string playerSlotIdText,
            string viewBindingTargetName,
            string cameraTargetBindingTargetName,
            string viewTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewCameraTargetBindingResult(
                PlayerViewCameraTargetBindingStatus.Failed,
                failureKind,
                playerSlotIdText,
                viewBindingTargetName,
                cameraTargetBindingTargetName,
                viewTargetName,
                source,
                reason,
                message,
                false,
                false);
        }

        public static PlayerViewCameraTargetBindingResult NoOperation(
            PlayerViewCameraTargetBindingFailureKind failureKind,
            string playerSlotIdText,
            string viewBindingTargetName,
            string cameraTargetBindingTargetName,
            string viewTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewCameraTargetBindingResult(
                PlayerViewCameraTargetBindingStatus.NoOp,
                failureKind,
                playerSlotIdText,
                viewBindingTargetName,
                cameraTargetBindingTargetName,
                viewTargetName,
                source,
                reason,
                message,
                false,
                false);
        }
    }
}
