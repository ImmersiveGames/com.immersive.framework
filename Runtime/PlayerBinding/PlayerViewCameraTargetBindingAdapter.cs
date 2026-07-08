using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit Unity adapter that associates PlayerView binding evidence with a Transform target.
    /// It requires the F51A PlayerView binding evidence first and does not activate cameras, drive Cinemachine,
    /// change priorities, bind input/control, enable movement or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B explicit PlayerView camera-target binding adapter contract.")]
    public static class PlayerViewCameraTargetBindingAdapter
    {
        public static PlayerViewCameraTargetBindingResult Bind(
            IPlayerViewBindingTarget playerViewBindingTarget,
            PlayerViewBehaviour playerViewBehaviour,
            IPlayerViewCameraTargetBindingTarget cameraTargetBindingTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingAdapter));

            if (cameraTargetBindingTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingCameraTargetBindingTarget,
                    string.Empty,
                    playerViewBindingTarget != null ? playerViewBindingTarget.BindingTargetName : string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding requires a camera-target binding target.");
            }

            if (playerViewBindingTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBindingTarget,
                    string.Empty,
                    string.Empty,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding requires a PlayerView binding target.");
            }

            if (!playerViewBindingTarget.HasPlayerViewBinding)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBinding,
                    string.Empty,
                    playerViewBindingTarget.BindingTargetName,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding requires existing PlayerView binding evidence.");
            }

            return Bind(
                playerViewBindingTarget.CurrentPlayerViewBinding,
                playerViewBehaviour,
                cameraTargetBindingTarget,
                normalizedSource,
                reason);
        }

        public static PlayerViewCameraTargetBindingResult Bind(
            PlayerViewBindingSnapshot playerViewBinding,
            PlayerViewBehaviour playerViewBehaviour,
            IPlayerViewCameraTargetBindingTarget cameraTargetBindingTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingAdapter));

            try
            {
                PlayerViewCameraTargetBindingResult validation = ValidateBindingInputs(
                    playerViewBinding,
                    playerViewBehaviour,
                    cameraTargetBindingTarget,
                    normalizedSource,
                    reason);

                if (validation.Failed || validation.NoOp)
                {
                    return validation;
                }

                Transform viewTarget = playerViewBehaviour.ViewTarget;
                PlayerViewCameraTargetBindingSnapshot cameraTargetBinding = PlayerViewCameraTargetBindingSnapshot.FromPlayerViewBinding(
                    playerViewBinding,
                    viewTarget,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    normalizedSource,
                    reason);

                PlayerViewCameraTargetBindingResult result = cameraTargetBindingTarget.ApplyPlayerViewCameraTargetBinding(
                    cameraTargetBinding,
                    viewTarget,
                    normalizedSource,
                    reason);

                if (!result.Succeeded)
                {
                    return PlayerViewCameraTargetBindingResult.Failure(
                        PlayerViewCameraTargetBindingFailureKind.TargetRejectedBinding,
                        playerViewBinding.PlayerSlotId.StableText,
                        playerViewBinding.BindingTargetName,
                        cameraTargetBindingTarget.CameraTargetBindingTargetName,
                        viewTarget != null ? viewTarget.name : string.Empty,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                string slotText = playerViewBinding.PlayerSlotId.IsValid ? playerViewBinding.PlayerSlotId.StableText : string.Empty;
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.UnexpectedException,
                    slotText,
                    playerViewBinding.BindingTargetName,
                    cameraTargetBindingTarget != null ? cameraTargetBindingTarget.CameraTargetBindingTargetName : string.Empty,
                    playerViewBehaviour != null && playerViewBehaviour.ViewTarget != null ? playerViewBehaviour.ViewTarget.name : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static PlayerViewCameraTargetBindingResult Clear(
            PlayerSlotId playerSlotId,
            IPlayerViewCameraTargetBindingTarget cameraTargetBindingTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewCameraTargetBindingAdapter));
            if (cameraTargetBindingTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingCameraTargetBindingTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding clear requires a target.");
            }

            if (!playerSlotId.IsValid)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.InvalidPlayerViewBinding,
                    string.Empty,
                    string.Empty,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView camera-target binding clear requires a valid PlayerSlotId.");
            }

            try
            {
                return cameraTargetBindingTarget.ClearPlayerViewCameraTargetBinding(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    string.Empty,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static PlayerViewCameraTargetBindingResult ValidateBindingInputs(
            PlayerViewBindingSnapshot playerViewBinding,
            PlayerViewBehaviour playerViewBehaviour,
            IPlayerViewCameraTargetBindingTarget cameraTargetBindingTarget,
            string source,
            string reason)
        {
            string cameraTargetBindingTargetName = cameraTargetBindingTarget != null ? cameraTargetBindingTarget.CameraTargetBindingTargetName : string.Empty;
            string playerSlotIdText = playerViewBinding.PlayerSlotId.IsValid ? playerViewBinding.PlayerSlotId.StableText : string.Empty;
            string viewBindingTargetName = playerViewBinding.BindingTargetName;

            if (cameraTargetBindingTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingCameraTargetBindingTarget,
                    playerSlotIdText,
                    viewBindingTargetName,
                    string.Empty,
                    playerViewBinding.ViewTargetName,
                    source,
                    reason,
                    "PlayerView camera-target binding requires a camera-target binding target.");
            }

            if (!playerViewBinding.PlayerSlotId.IsValid || !playerViewBinding.BindsView)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.InvalidPlayerViewBinding,
                    playerSlotIdText,
                    viewBindingTargetName,
                    cameraTargetBindingTargetName,
                    playerViewBinding.ViewTargetName,
                    source,
                    reason,
                    "PlayerView camera-target binding requires valid F51A PlayerView binding evidence.");
            }

            if (playerViewBehaviour == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBehaviour,
                    playerSlotIdText,
                    viewBindingTargetName,
                    cameraTargetBindingTargetName,
                    playerViewBinding.ViewTargetName,
                    source,
                    reason,
                    "PlayerView camera-target binding requires PlayerViewBehaviour evidence.");
            }

            if (playerViewBehaviour.PlayerSlotId != playerViewBinding.PlayerSlotId)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.PlayerSlotMismatch,
                    playerSlotIdText,
                    viewBindingTargetName,
                    cameraTargetBindingTargetName,
                    playerViewBehaviour.ViewTarget != null ? playerViewBehaviour.ViewTarget.name : string.Empty,
                    source,
                    reason,
                    $"PlayerView camera-target binding PlayerSlot mismatch. Binding='{playerViewBinding.PlayerSlotId.StableText}' Behaviour='{playerViewBehaviour.PlayerSlotId.StableText}'.");
            }

            if (playerViewBehaviour.ViewTarget == null)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingViewTarget,
                    playerSlotIdText,
                    viewBindingTargetName,
                    cameraTargetBindingTargetName,
                    playerViewBinding.ViewTargetName,
                    source,
                    reason,
                    "PlayerView camera-target binding requires a Unity Transform view target.");
            }

            string bindingViewTargetName = playerViewBinding.ViewTargetName.NormalizeText();
            string behaviourViewTargetName = playerViewBehaviour.ViewTarget.name.NormalizeText();
            if (!string.Equals(bindingViewTargetName, behaviourViewTargetName, StringComparison.Ordinal))
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.ViewTargetMismatch,
                    playerSlotIdText,
                    viewBindingTargetName,
                    cameraTargetBindingTargetName,
                    behaviourViewTargetName,
                    source,
                    reason,
                    $"PlayerView camera-target binding target mismatch. Binding='{bindingViewTargetName}' Behaviour='{behaviourViewTargetName}'.");
            }

            return PlayerViewCameraTargetBindingResult.Success(
                playerViewBinding.PlayerSlotId,
                viewBindingTargetName,
                cameraTargetBindingTargetName,
                behaviourViewTargetName,
                source,
                reason,
                "PlayerView camera-target binding inputs validated.");
        }
    }
}
