using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that stores PlayerView camera-target binding evidence.
    /// It does not enable/disable cameras, change camera priority, drive Cinemachine, bind input/control, move objects or spawn actors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Player View Camera Target Binding Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B Unity target for explicit PlayerView camera-target binding evidence.")]
    public sealed class PlayerViewCameraTargetBindingTargetBehaviour : MonoBehaviour, IPlayerViewCameraTargetBindingTarget
    {
        [Tooltip("Human-readable target name used in diagnostics only.")]
        [SerializeField] private string cameraTargetBindingTargetName = "Player View Camera Target Binding Target";

        private PlayerViewCameraTargetBindingSnapshot _currentBinding;
        private Transform _currentCameraTarget;
        private bool _hasBinding;

        public string CameraTargetBindingTargetName => cameraTargetBindingTargetName.NormalizeTextOrFallback(name);

        public bool HasCameraTargetBinding => _hasBinding;

        public Transform CurrentCameraTarget
        {
            get
            {
                if (!_hasBinding || _currentCameraTarget == null)
                {
                    throw new InvalidOperationException("PlayerView camera-target binding target has no current camera target.");
                }

                return _currentCameraTarget;
            }
        }

        public PlayerViewCameraTargetBindingSnapshot CurrentCameraTargetBinding
        {
            get
            {
                if (!_hasBinding)
                {
                    throw new InvalidOperationException("PlayerView camera-target binding target has no current binding.");
                }

                return _currentBinding;
            }
        }

        public bool ActivatesCamera => false;

        public bool BindsControl => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public PlayerViewCameraTargetBindingResult ApplyPlayerViewCameraTargetBinding(
            PlayerViewCameraTargetBindingSnapshot binding,
            Transform cameraTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingTargetBehaviour));
            if (!binding.PlayerSlotId.IsValid)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.InvalidPlayerViewBinding,
                    string.Empty,
                    binding.ViewBindingTargetName,
                    CameraTargetBindingTargetName,
                    cameraTarget != null ? cameraTarget.name : string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding target rejected an invalid PlayerSlotId.");
            }

            if (cameraTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingViewTarget,
                    binding.PlayerSlotId.StableText,
                    binding.ViewBindingTargetName,
                    CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding target requires a Transform target.");
            }

            _currentBinding = binding;
            _currentCameraTarget = cameraTarget;
            _hasBinding = true;
            return PlayerViewCameraTargetBindingResult.Success(
                binding.PlayerSlotId,
                binding.ViewBindingTargetName,
                CameraTargetBindingTargetName,
                cameraTarget.name,
                normalizedSource,
                reason,
                "PlayerView camera-target binding evidence stored on target.");
        }

        public PlayerViewCameraTargetBindingResult ClearPlayerViewCameraTargetBinding(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasBinding)
            {
                return PlayerViewCameraTargetBindingResult.NoOperation(
                    PlayerViewCameraTargetBindingFailureKind.MissingExistingBinding,
                    playerSlotIdText,
                    string.Empty,
                    CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding target had no binding to clear.");
            }

            if (_currentBinding.PlayerSlotId != playerSlotId)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    _currentBinding.ViewBindingTargetName,
                    CameraTargetBindingTargetName,
                    _currentCameraTarget != null ? _currentCameraTarget.name : string.Empty,
                    normalizedSource,
                    reason,
                    $"PlayerView camera-target binding target is bound to '{_currentBinding.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            string viewBindingTargetName = _currentBinding.ViewBindingTargetName;
            string viewTargetName = _currentCameraTarget != null ? _currentCameraTarget.name : _currentBinding.ViewTargetName;
            _currentBinding = default;
            _currentCameraTarget = null;
            _hasBinding = false;
            return PlayerViewCameraTargetBindingResult.Success(
                playerSlotId,
                viewBindingTargetName,
                CameraTargetBindingTargetName,
                viewTargetName,
                normalizedSource,
                reason,
                "PlayerView camera-target binding evidence cleared from target.",
                false,
                false);
        }

        internal void ConfigureForDiagnostics(string targetName)
        {
            cameraTargetBindingTargetName = targetName.NormalizeTextOrFallback(name);
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(cameraTargetBindingTargetName))
            {
                cameraTargetBindingTargetName = name;
            }
        }
    }
}
