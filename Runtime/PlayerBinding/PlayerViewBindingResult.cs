using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable result for explicit PlayerView binding adapter operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A PlayerView binding adapter operation result.")]
    public readonly struct PlayerViewBindingResult : IEquatable<PlayerViewBindingResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _bindingTargetName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsView;

        public PlayerViewBindingResult(
            PlayerViewBindingStatus status,
            PlayerViewBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message,
            bool bindsView)
        {
            if (!Enum.IsDefined(typeof(PlayerViewBindingStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "PlayerView binding status is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerViewBindingFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "PlayerView binding failure kind is not defined.");
            }

            if (status == PlayerViewBindingStatus.Succeeded && failureKind != PlayerViewBindingFailureKind.None)
            {
                throw new ArgumentException("Succeeded PlayerView binding results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != PlayerViewBindingStatus.Succeeded && failureKind == PlayerViewBindingFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp PlayerView binding results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _bindingTargetName = bindingTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewBindingResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsView = status == PlayerViewBindingStatus.Succeeded && bindsView;
        }

        public PlayerViewBindingStatus Status { get; }

        public PlayerViewBindingFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string BindingTargetName => _bindingTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == PlayerViewBindingStatus.Succeeded;

        public bool Failed => Status == PlayerViewBindingStatus.Failed;

        public bool NoOp => Status == PlayerViewBindingStatus.NoOp;

        public bool BindsView => _bindsView;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' bindingTarget='{_bindingTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(PlayerViewBindingResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_bindingTargetName, other._bindingTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsView == other._bindsView;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewBindingResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ (int)FailureKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_playerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_message ?? string.Empty);
                hash = (hash * 397) ^ _bindsView.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static PlayerViewBindingResult Success(
            PlayerSlotId playerSlotId,
            string bindingTargetName,
            string source,
            string reason,
            string message,
            bool bindsView = true)
        {
            return new PlayerViewBindingResult(
                PlayerViewBindingStatus.Succeeded,
                PlayerViewBindingFailureKind.None,
                playerSlotId.StableText,
                bindingTargetName,
                source,
                reason,
                message,
                bindsView);
        }

        public static PlayerViewBindingResult Failure(
            PlayerViewBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewBindingResult(
                PlayerViewBindingStatus.Failed,
                failureKind,
                playerSlotIdText,
                bindingTargetName,
                source,
                reason,
                message,
                false);
        }

        public static PlayerViewBindingResult NoOperation(
            PlayerViewBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerViewBindingResult(
                PlayerViewBindingStatus.NoOp,
                failureKind,
                playerSlotIdText,
                bindingTargetName,
                source,
                reason,
                message,
                false);
        }
    }
}
