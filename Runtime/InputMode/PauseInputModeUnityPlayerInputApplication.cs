using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Bridges a completed logical Pause result to the resident InputMode application path.
    /// Persistent maps such as Global remain enabled across Gameplay and PauseOverlay.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC4 completed Pause result to layered Unity PlayerInput posture.")]
    public static class PauseInputModeUnityPlayerInputApplication
    {
        public static PauseInputModeUnityPlayerInputApplicationResult Apply(
            PauseResult pauseResult,
            InputModeState currentInputModeState,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            LocalPlayerProvisioningValidationResult localPlayerProvisioningValidation,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            PlayerInput playerInput,
            string source,
            string reason,
            UnityInputActionMapName[] persistentActionMapNames = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(PauseInputModeUnityPlayerInputApplication));
            string normalizedReason = reason.NormalizeText();

            if (!pauseResult.IsValid)
            {
                throw new ArgumentException(
                    "Pause InputMode Unity PlayerInput application requires a valid Pause result.",
                    nameof(pauseResult));
            }

            if (!pauseResult.Completed)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputApplicationStatus.FailedPauseResultNotCompleted,
                    pauseResult,
                    default,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            InputModeRequest inputModeRequest =
                PauseInputModeRequestMapper.CreateRequest(
                    pauseResult,
                    normalizedSource,
                    normalizedReason.NormalizeTextOrFallback(
                        "pause-inputmode-unity-playerinput-application"));

            InputModeUnityPlayerInputRequestApplicationResult inputModeApplication =
                InputModeUnityPlayerInputRequestApplication.Apply(
                    currentInputModeState,
                    inputModeRequest,
                    targetSet,
                    playerActorSet,
                    localPlayerProvisioningValidation,
                    actionMapEvidence,
                    actionMapBindings,
                    playerInput,
                    normalizedSource,
                    normalizedReason,
                    persistentActionMapNames);

            PauseInputModeUnityPlayerInputApplicationStatus status =
                inputModeApplication.Succeeded
                    ? PauseInputModeUnityPlayerInputApplicationStatus.Succeeded
                    : inputModeApplication.Ignored
                        ? PauseInputModeUnityPlayerInputApplicationStatus.IgnoredInputModeRequest
                        : PauseInputModeUnityPlayerInputApplicationStatus.FailedInputModePlayerInputApplication;

            return CreateResult(
                status,
                pauseResult,
                inputModeRequest,
                inputModeApplication,
                normalizedSource,
                normalizedReason);
        }

        private static PauseInputModeUnityPlayerInputApplicationResult CreateResult(
            PauseInputModeUnityPlayerInputApplicationStatus status,
            PauseResult pauseResult,
            InputModeRequest inputModeRequest,
            InputModeUnityPlayerInputRequestApplicationResult inputModeApplicationResult,
            string source,
            string reason)
        {
            return new PauseInputModeUnityPlayerInputApplicationResult(
                status,
                pauseResult,
                inputModeRequest,
                inputModeApplicationResult,
                source,
                reason);
        }
    }
}
