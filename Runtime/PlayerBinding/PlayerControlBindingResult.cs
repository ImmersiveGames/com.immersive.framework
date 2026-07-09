using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable result for explicit PlayerControl binding adapter operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A PlayerControl binding adapter operation result.")]
    public readonly struct PlayerControlBindingResult : IEquatable<PlayerControlBindingResult>
    {
        private readonly string _playerSlotIdText;
        private readonly string _bindingTargetName;
        private readonly string _source;
        private readonly string _reason;
        private readonly string _message;
        private readonly bool _bindsControl;

        public PlayerControlBindingResult(
            PlayerControlBindingStatus status,
            PlayerControlBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message,
            bool bindsControl)
        {
            if (!Enum.IsDefined(typeof(PlayerControlBindingStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "PlayerControl binding status is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerControlBindingFailureKind), failureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "PlayerControl binding failure kind is not defined.");
            }

            if (status == PlayerControlBindingStatus.Succeeded && failureKind != PlayerControlBindingFailureKind.None)
            {
                throw new ArgumentException("Succeeded PlayerControl binding results must use FailureKind.None.", nameof(failureKind));
            }

            if (status != PlayerControlBindingStatus.Succeeded && failureKind == PlayerControlBindingFailureKind.None)
            {
                throw new ArgumentException("Failed or NoOp PlayerControl binding results must include an explicit failure/no-op kind.", nameof(failureKind));
            }

            Status = status;
            FailureKind = failureKind;
            _playerSlotIdText = playerSlotIdText.NormalizeText();
            _bindingTargetName = bindingTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerControlBindingResult));
            _reason = reason.NormalizeText();
            _message = message.NormalizeText();
            _bindsControl = status == PlayerControlBindingStatus.Succeeded && bindsControl;
        }

        public PlayerControlBindingStatus Status { get; }

        public PlayerControlBindingFailureKind FailureKind { get; }

        public string PlayerSlotIdText => _playerSlotIdText;

        public string BindingTargetName => _bindingTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public string Message => _message;

        public bool Succeeded => Status == PlayerControlBindingStatus.Succeeded;

        public bool Failed => Status == PlayerControlBindingStatus.Failed;

        public bool NoOp => Status == PlayerControlBindingStatus.NoOp;

        public bool BindsView => false;

        public bool BindsControl => _bindsControl;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' failureKind='{FailureKind}' playerSlot='{_playerSlotIdText.ToDiagnosticText()}' bindingTarget='{_bindingTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' message='{_message.ToDiagnosticText()}' reason='{_reason.ToDiagnosticText()}'";
        }

        public bool Equals(PlayerControlBindingResult other)
        {
            return Status == other.Status
                && FailureKind == other.FailureKind
                && string.Equals(_playerSlotIdText, other._playerSlotIdText, StringComparison.Ordinal)
                && string.Equals(_bindingTargetName, other._bindingTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal)
                && string.Equals(_message, other._message, StringComparison.Ordinal)
                && _bindsControl == other._bindsControl;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControlBindingResult other && Equals(other);
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
                hash = (hash * 397) ^ _bindsControl.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static PlayerControlBindingResult Success(
            PlayerSlotId playerSlotId,
            string bindingTargetName,
            string source,
            string reason,
            string message,
            bool bindsControl = true)
        {
            return new PlayerControlBindingResult(
                PlayerControlBindingStatus.Succeeded,
                PlayerControlBindingFailureKind.None,
                playerSlotId.StableText,
                bindingTargetName,
                source,
                reason,
                message,
                bindsControl);
        }

        public static PlayerControlBindingResult Failure(
            PlayerControlBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerControlBindingResult(
                PlayerControlBindingStatus.Failed,
                failureKind,
                playerSlotIdText,
                bindingTargetName,
                source,
                reason,
                message,
                false);
        }

        public static PlayerControlBindingResult NoOperation(
            PlayerControlBindingFailureKind failureKind,
            string playerSlotIdText,
            string bindingTargetName,
            string source,
            string reason,
            string message)
        {
            return new PlayerControlBindingResult(
                PlayerControlBindingStatus.NoOp,
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
