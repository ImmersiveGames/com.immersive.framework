using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit adapter that activates one Unity Camera from PlayerView camera-target binding evidence.
    /// It does not resolve Camera.main, manage camera priority, drive Cinemachine, use CameraDirector, bind input/control, enable movement or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C explicit PlayerView camera activation adapter contract.")]
    public static class PlayerViewCameraActivationAdapter
    {
        public static PlayerViewCameraActivationResult Activate(
            IPlayerViewCameraTargetBindingTarget cameraTargetBindingTarget,
            IPlayerViewCameraActivationTarget cameraActivationTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationAdapter));

            if (cameraActivationTarget == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCameraActivationTarget,
                    string.Empty,
                    cameraTargetBindingTarget != null ? cameraTargetBindingTarget.CameraTargetBindingTargetName : string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation requires a camera activation target.");
            }

            if (cameraTargetBindingTarget == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCameraTargetBindingTarget,
                    string.Empty,
                    string.Empty,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation requires a camera-target binding target.");
            }

            if (!cameraTargetBindingTarget.HasCameraTargetBinding)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCameraTargetBinding,
                    string.Empty,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation requires existing camera-target binding evidence.");
            }

            try
            {
                return Activate(
                    cameraTargetBindingTarget.CurrentCameraTargetBinding,
                    cameraTargetBindingTarget.CurrentCameraTarget,
                    cameraActivationTarget,
                    normalizedSource,
                    reason);
            }
            catch (Exception exception)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.UnexpectedException,
                    string.Empty,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static PlayerViewCameraActivationResult Activate(
            PlayerViewCameraTargetBindingSnapshot cameraTargetBinding,
            Transform viewTarget,
            IPlayerViewCameraActivationTarget cameraActivationTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationAdapter));

            try
            {
                PlayerViewCameraActivationResult validation = ValidateActivationInputs(
                    cameraTargetBinding,
                    viewTarget,
                    cameraActivationTarget,
                    normalizedSource,
                    reason);

                if (validation.Failed || validation.NoOp)
                {
                    return validation;
                }

                PlayerViewCameraActivationSnapshot activation = PlayerViewCameraActivationSnapshot.FromCameraTargetBinding(
                    cameraTargetBinding,
                    viewTarget,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera,
                    normalizedSource,
                    reason);

                PlayerViewCameraActivationResult result = cameraActivationTarget.ApplyPlayerViewCameraActivation(
                    activation,
                    cameraActivationTarget.ActivationCamera,
                    normalizedSource,
                    reason);

                if (!result.Succeeded)
                {
                    return PlayerViewCameraActivationResult.Failure(
                        PlayerViewCameraActivationFailureKind.TargetRejectedActivation,
                        cameraTargetBinding.PlayerSlotId.IsValid ? cameraTargetBinding.PlayerSlotId.StableText : string.Empty,
                        cameraTargetBinding.CameraTargetBindingTargetName,
                        cameraActivationTarget.CameraActivationTargetName,
                        cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                        viewTarget != null ? viewTarget.name : string.Empty,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.UnexpectedException,
                    cameraTargetBinding.PlayerSlotId.IsValid ? cameraTargetBinding.PlayerSlotId.StableText : string.Empty,
                    cameraTargetBinding.CameraTargetBindingTargetName,
                    cameraActivationTarget != null ? cameraActivationTarget.CameraActivationTargetName : string.Empty,
                    cameraActivationTarget != null && cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    viewTarget != null ? viewTarget.name : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static PlayerViewCameraActivationResult Clear(
            PlayerSlotId playerSlotId,
            IPlayerViewCameraActivationTarget cameraActivationTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraActivationAdapter));
            if (cameraActivationTarget == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCameraActivationTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation clear requires an activation target.");
            }

            if (!playerSlotId.IsValid)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.InvalidCameraTargetBinding,
                    string.Empty,
                    string.Empty,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera activation clear requires a valid PlayerSlotId.");
            }

            try
            {
                return cameraActivationTarget.ClearPlayerViewCameraActivation(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    string.Empty,
                    cameraActivationTarget.CameraActivationTargetName,
                    cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static PlayerViewCameraActivationResult ValidateActivationInputs(
            PlayerViewCameraTargetBindingSnapshot cameraTargetBinding,
            Transform viewTarget,
            IPlayerViewCameraActivationTarget cameraActivationTarget,
            string source,
            string reason)
        {
            string playerSlotIdText = cameraTargetBinding.PlayerSlotId.IsValid ? cameraTargetBinding.PlayerSlotId.StableText : string.Empty;
            string cameraTargetBindingTargetName = cameraTargetBinding.CameraTargetBindingTargetName;
            string cameraActivationTargetName = cameraActivationTarget != null ? cameraActivationTarget.CameraActivationTargetName : string.Empty;
            string cameraName = cameraActivationTarget != null && cameraActivationTarget.ActivationCamera != null ? cameraActivationTarget.ActivationCamera.name : string.Empty;
            string viewTargetName = viewTarget != null ? viewTarget.name : cameraTargetBinding.ViewTargetName;

            if (cameraActivationTarget == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCameraActivationTarget,
                    playerSlotIdText,
                    cameraTargetBindingTargetName,
                    string.Empty,
                    string.Empty,
                    viewTargetName,
                    source,
                    reason,
                    "PlayerView camera activation requires a camera activation target.");
            }

            if (!cameraTargetBinding.PlayerSlotId.IsValid)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.InvalidCameraTargetBinding,
                    string.Empty,
                    cameraTargetBindingTargetName,
                    cameraActivationTargetName,
                    cameraName,
                    viewTargetName,
                    source,
                    reason,
                    "PlayerView camera activation requires valid camera-target binding evidence.");
            }

            if (viewTarget == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingViewTarget,
                    playerSlotIdText,
                    cameraTargetBindingTargetName,
                    cameraActivationTargetName,
                    cameraName,
                    cameraTargetBinding.ViewTargetName,
                    source,
                    reason,
                    "PlayerView camera activation requires a Unity Transform view target.");
            }

            string bindingViewTargetName = cameraTargetBinding.ViewTargetName.NormalizeText();
            string actualViewTargetName = viewTarget.name.NormalizeText();
            if (!string.IsNullOrWhiteSpace(bindingViewTargetName)
                && !string.Equals(bindingViewTargetName, actualViewTargetName, StringComparison.Ordinal))
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.ViewTargetMismatch,
                    playerSlotIdText,
                    cameraTargetBindingTargetName,
                    cameraActivationTargetName,
                    cameraName,
                    actualViewTargetName,
                    source,
                    reason,
                    $"PlayerView camera activation target mismatch. Binding='{bindingViewTargetName}' Transform='{actualViewTargetName}'.");
            }

            if (cameraActivationTarget.ActivationCamera == null)
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.MissingCamera,
                    playerSlotIdText,
                    cameraTargetBindingTargetName,
                    cameraActivationTargetName,
                    string.Empty,
                    actualViewTargetName,
                    source,
                    reason,
                    "PlayerView camera activation requires an explicit Unity Camera.");
            }

            string bindingCameraName = cameraTargetBinding.CameraName.NormalizeText();
            string actualCameraName = cameraActivationTarget.ActivationCamera.name.NormalizeText();
            if (!string.IsNullOrWhiteSpace(bindingCameraName)
                && !string.Equals(bindingCameraName, actualCameraName, StringComparison.Ordinal))
            {
                return PlayerViewCameraActivationResult.Failure(
                    PlayerViewCameraActivationFailureKind.CameraMismatch,
                    playerSlotIdText,
                    cameraTargetBindingTargetName,
                    cameraActivationTargetName,
                    actualCameraName,
                    actualViewTargetName,
                    source,
                    reason,
                    $"PlayerView camera activation camera mismatch. Binding='{bindingCameraName}' Camera='{actualCameraName}'.");
            }

            return PlayerViewCameraActivationResult.Success(
                cameraTargetBinding.PlayerSlotId,
                cameraTargetBindingTargetName,
                cameraActivationTargetName,
                actualCameraName,
                actualViewTargetName,
                source,
                reason,
                "PlayerView camera activation inputs validated.");
        }
    }
}
