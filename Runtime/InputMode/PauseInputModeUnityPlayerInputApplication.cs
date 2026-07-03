using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Explicit bridge from a completed logical Pause result to Unity PlayerInput application via InputMode.
    /// It does not own PauseRuntime, FrameworkRuntimeHost, PlayerInputManager, join/spawn, movement or custom input manager behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32G completed Pause result to explicit Unity PlayerInput application bridge.")]
    public static class PauseInputModeUnityPlayerInputApplication
    {
        public static PauseInputModeUnityPlayerInputApplicationResult Apply(
            PauseResult pauseResult,
            InputModeState currentInputModeState,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            UnityInputPlayerInputManagerEvidence sessionPlayerInputManagerEvidence,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            PlayerInput playerInput,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseInputModeUnityPlayerInputApplication));
            string normalizedReason = reason.NormalizeText();

            if (!pauseResult.IsValid)
            {
                throw new ArgumentException("Pause InputMode Unity PlayerInput application requires a valid Pause result.", nameof(pauseResult));
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

            InputModeRequest inputModeRequest = PauseInputModeRequestMapper.CreateRequest(
                pauseResult,
                normalizedSource,
                normalizedReason.NormalizeTextOrFallback("pause-inputmode-unity-playerinput-application"));

            InputModeUnityPlayerInputRequestApplicationResult inputModeApplication = InputModeUnityPlayerInputRequestApplication.Apply(
                currentInputModeState,
                inputModeRequest,
                targetSet,
                playerActorSet,
                sessionPlayerInputManagerEvidence,
                actionMapEvidence,
                actionMapBindings,
                playerInput,
                normalizedSource,
                normalizedReason);

            PauseInputModeUnityPlayerInputApplicationStatus status = inputModeApplication.Succeeded
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
