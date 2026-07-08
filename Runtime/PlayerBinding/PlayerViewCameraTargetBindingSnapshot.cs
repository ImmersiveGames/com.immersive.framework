using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that a PlayerView binding was associated with a Unity camera target transform.
    /// This is not camera activation, camera priority, Cinemachine control, input routing, movement or actor spawning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B PlayerView camera-target binding snapshot.")]
    public readonly struct PlayerViewCameraTargetBindingSnapshot : IEquatable<PlayerViewCameraTargetBindingSnapshot>
    {
        private readonly string _viewBindingTargetName;
        private readonly string _cameraTargetBindingTargetName;
        private readonly string _cameraName;
        private readonly string _viewTargetName;
        private readonly string _source;
        private readonly string _reason;

        public PlayerViewCameraTargetBindingSnapshot(
            PlayerSlotId playerSlotId,
            string viewBindingTargetName,
            string cameraTargetBindingTargetName,
            string cameraName,
            string viewTargetName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerView camera-target binding snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            PlayerSlotId = playerSlotId;
            _viewBindingTargetName = viewBindingTargetName.NormalizeText();
            _cameraTargetBindingTargetName = cameraTargetBindingTargetName.NormalizeText();
            _cameraName = cameraName.NormalizeText();
            _viewTargetName = viewTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingSnapshot));
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public string ViewBindingTargetName => _viewBindingTargetName;

        public string CameraTargetBindingTargetName => _cameraTargetBindingTargetName;

        public string CameraName => _cameraName;

        public string ViewTargetName => _viewTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public bool BindsView => true;

        public bool BindsCameraTarget => true;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(PlayerViewCameraTargetBindingSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && string.Equals(_viewBindingTargetName, other._viewBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraTargetBindingTargetName, other._cameraTargetBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraName, other._cameraName, StringComparison.Ordinal)
                && string.Equals(_viewTargetName, other._viewTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewCameraTargetBindingSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraTargetBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' viewBindingTarget='{_viewBindingTargetName.ToDiagnosticText()}' cameraTargetBindingTarget='{_cameraTargetBindingTargetName.ToDiagnosticText()}' camera='{_cameraName.ToDiagnosticText()}' viewTarget='{_viewTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' cameraTargetBinding='{BindsCameraTarget}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static PlayerViewCameraTargetBindingSnapshot FromPlayerViewBinding(
            PlayerViewBindingSnapshot viewBinding,
            Transform viewTarget,
            string cameraTargetBindingTargetName,
            string source,
            string reason)
        {
            if (viewTarget == null)
            {
                throw new ArgumentNullException(nameof(viewTarget));
            }

            return new PlayerViewCameraTargetBindingSnapshot(
                viewBinding.PlayerSlotId,
                viewBinding.BindingTargetName,
                cameraTargetBindingTargetName,
                viewBinding.CameraName,
                viewTarget.name,
                source,
                reason);
        }
    }
}
