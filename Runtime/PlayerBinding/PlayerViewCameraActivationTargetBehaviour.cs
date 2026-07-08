using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that activates/deactivates one explicit Camera from PlayerView camera-target binding evidence.
    /// It does not select priorities, resolve Camera.main, drive Cinemachine, bind input/control, enable movement or spawn actors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Player View Camera Activation Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C Unity target for explicit PlayerView camera activation.")]
    public sealed class PlayerViewCameraActivationTargetBehaviour : MonoBehaviour, IPlayerViewCameraActivationTarget
    {
        [Tooltip("Human-readable activation target name used in diagnostics only.")]
        [SerializeField] private string cameraActivationTargetName = "Player View Camera Activation Target";
        [Tooltip("Explicit Unity Camera affected by this target. This is not Camera.main lookup and not Cinemachine.")]
        [SerializeField] private UnityEngine.Camera cameraToActivate;

        private PlayerViewCameraActivationSnapshot _currentActivation;
        private UnityEngine.Camera _currentCamera;
        private bool _hasActivation;

        public string CameraActivationTargetName => cameraActivationTargetName.NormalizeTextOrFallback(name);

        public UnityEngine.Camera ActivationCamera => cameraToActivate;

        public bool HasCameraActivation => _hasActivation;

        public PlayerViewCameraActivationSnapshot CurrentCameraActivation
        {
            get
            {
                if (!_hasActivation)
                {
                    throw new InvalidOperationException("PlayerView camera activation target has no current activation.");
                }

                return _currentActivation;
            }
        }

        public bool BindsControl => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public PlayerViewCameraActivationResult ApplyPlayerViewCameraActivation(
            PlayerViewCameraActivationSnapshot activation,
            UnityEngine.Camera camera,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationTargetBehaviour));
            if (!activation.PlayerSlotId.IsValid)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.InvalidCameraTargetBinding,
                    string.Empty,
                    activation.CameraTargetBindingTargetName,
                    CameraActivationTargetName,
                    camera != null ? camera.name : string.Empty,
                    activation.ViewTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation target rejected an invalid PlayerSlotId.");
            }

            if (camera == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCamera,
                    activation.PlayerSlotId.StableText,
                    activation.CameraTargetBindingTargetName,
                    CameraActivationTargetName,
                    string.Empty,
                    activation.ViewTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation target requires an explicit Camera.");
            }

            _currentActivation = activation;
            _currentCamera = camera;
            _hasActivation = true;
            camera.enabled = true;

            return PlayerViewCameraActivationResult.Success(
                activation.PlayerSlotId,
                activation.CameraTargetBindingTargetName,
                CameraActivationTargetName,
                camera.name,
                activation.ViewTargetName,
                normalizedSource,
                reason,
                "PlayerView camera activation applied to explicit Camera.");
        }

        public PlayerViewCameraActivationResult ClearPlayerViewCameraActivation(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasActivation)
            {
                return PlayerViewCameraActivationResult.NoOperation(
                    PlayerViewCameraActivationFailureKind.MissingExistingActivation,
                    playerSlotIdText,
                    string.Empty,
                    CameraActivationTargetName,
                    cameraToActivate != null ? cameraToActivate.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation target had no activation to clear.");
            }

            if (_currentActivation.PlayerSlotId != playerSlotId)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    _currentActivation.CameraTargetBindingTargetName,
                    CameraActivationTargetName,
                    _currentCamera != null ? _currentCamera.name : string.Empty,
                    _currentActivation.ViewTargetName,
                    normalizedSource,
                    reason,
                    $"PlayerView camera activation target is active for '{_currentActivation.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            if (_currentCamera == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCamera,
                    playerSlotIdText,
                    _currentActivation.CameraTargetBindingTargetName,
                    CameraActivationTargetName,
                    string.Empty,
                    _currentActivation.ViewTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation target lost its active Camera reference.");
            }

            string cameraTargetBindingTargetName = _currentActivation.CameraTargetBindingTargetName;
            string cameraName = _currentCamera.name;
            string viewTargetName = _currentActivation.ViewTargetName;
            bool restoreEnabled = _currentActivation.WasCameraEnabledBeforeActivation;
            _currentCamera.enabled = restoreEnabled;
            _currentActivation = default;
            _currentCamera = null;
            _hasActivation = false;

            return PlayerViewCameraActivationResult.Success(
                playerSlotId,
                cameraTargetBindingTargetName,
                CameraActivationTargetName,
                cameraName,
                viewTargetName,
                normalizedSource,
                reason,
                "PlayerView camera activation cleared from explicit Camera.",
                true,
                true,
                false);
        }

        internal void ConfigureForDiagnostics(string targetName, UnityEngine.Camera camera)
        {
            cameraActivationTargetName = targetName.NormalizeTextOrFallback(name);
            cameraToActivate = camera;
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(cameraActivationTargetName))
            {
                cameraActivationTargetName = name;
            }
        }
    }
}
