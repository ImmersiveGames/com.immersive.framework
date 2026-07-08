using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that a PlayerView camera target binding activated one explicit Unity Camera.
    /// This is not camera priority, Cinemachine control, CameraDirector orchestration, input routing, movement or actor spawning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C PlayerView camera activation snapshot.")]
    public readonly struct PlayerViewCameraActivationSnapshot : IEquatable<PlayerViewCameraActivationSnapshot>
    {
        private readonly string _cameraTargetBindingTargetName;
        private readonly string _cameraActivationTargetName;
        private readonly string _cameraName;
        private readonly string _viewTargetName;
        private readonly string _source;
        private readonly string _reason;

        public PlayerViewCameraActivationSnapshot(
            PlayerSlotId playerSlotId,
            string cameraTargetBindingTargetName,
            string cameraActivationTargetName,
            string cameraName,
            string viewTargetName,
            bool wasCameraEnabledBeforeActivation,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerView camera activation snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            PlayerSlotId = playerSlotId;
            _cameraTargetBindingTargetName = cameraTargetBindingTargetName.NormalizeText();
            _cameraActivationTargetName = cameraActivationTargetName.NormalizeText();
            _cameraName = cameraName.NormalizeText();
            _viewTargetName = viewTargetName.NormalizeText();
            WasCameraEnabledBeforeActivation = wasCameraEnabledBeforeActivation;
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationSnapshot));
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public string CameraTargetBindingTargetName => _cameraTargetBindingTargetName;

        public string CameraActivationTargetName => _cameraActivationTargetName;

        public string CameraName => _cameraName;

        public string ViewTargetName => _viewTargetName;

        public bool WasCameraEnabledBeforeActivation { get; }

        public string Source => _source;

        public string Reason => _reason;

        public bool BindsView => true;

        public bool BindsCameraTarget => true;

        public bool ActivatesCamera => true;

        public bool BindsControl => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(PlayerViewCameraActivationSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && string.Equals(_cameraTargetBindingTargetName, other._cameraTargetBindingTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraActivationTargetName, other._cameraActivationTargetName, StringComparison.Ordinal)
                && string.Equals(_cameraName, other._cameraName, StringComparison.Ordinal)
                && string.Equals(_viewTargetName, other._viewTargetName, StringComparison.Ordinal)
                && WasCameraEnabledBeforeActivation == other.WasCameraEnabledBeforeActivation
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewCameraActivationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraTargetBindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraActivationTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewTargetName ?? string.Empty);
                hash = (hash * 397) ^ WasCameraEnabledBeforeActivation.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' cameraTargetBindingTarget='{_cameraTargetBindingTargetName.ToDiagnosticText()}' cameraActivationTarget='{_cameraActivationTargetName.ToDiagnosticText()}' camera='{_cameraName.ToDiagnosticText()}' viewTarget='{_viewTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' cameraTargetBinding='{BindsCameraTarget}' cameraActivation='{ActivatesCamera}' controlBinding='{BindsControl}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' wasCameraEnabledBeforeActivation='{WasCameraEnabledBeforeActivation}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static PlayerViewCameraActivationSnapshot FromCameraTargetBinding(
            PlayerViewCameraTargetBindingSnapshot cameraTargetBinding,
            Transform viewTarget,
            string cameraActivationTargetName,
            UnityEngine.Camera camera,
            string source,
            string reason)
        {
            if (viewTarget == null)
            {
                throw new ArgumentNullException(nameof(viewTarget));
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            return new PlayerViewCameraActivationSnapshot(
                cameraTargetBinding.PlayerSlotId,
                cameraTargetBinding.CameraTargetBindingTargetName,
                cameraActivationTargetName,
                camera.name,
                viewTarget.name,
                camera.enabled,
                source,
                reason);
        }
    }
}
