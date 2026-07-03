using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Passive bridge from logical Pause state/result to InputMode requests.
    /// It does not own Pause, PlayerInput, PlayerInputManager, action maps or Unity Input System behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30D passive Pause to InputMode request mapping; no Unity input behavior.")]
    public static class PauseInputModeRequestMapper
    {
        public const bool SwitchesActionMaps = false;
        public const bool AppliesInputBehavior = false;
        public const bool OwnsPlayerInput = false;
        public const bool OwnsPlayerInputManager = false;

        public static InputModeKind ResolveTargetMode(PauseState pauseState)
        {
            switch (pauseState)
            {
                case PauseState.Running:
                    return InputModeKind.Gameplay;
                case PauseState.Paused:
                    return InputModeKind.PauseOverlay;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pauseState), pauseState, "Pause state must be explicit to map to InputMode.");
            }
        }

        public static InputModeRequest CreateRequest(PauseState pauseState, string requester, string reason)
        {
            InputModeKind targetMode = ResolveTargetMode(pauseState);
            return InputModeRequest.To(
                targetMode,
                requester.NormalizeTextOrFallback(nameof(PauseInputModeRequestMapper)),
                reason.NormalizeTextOrFallback($"pause-state.{pauseState}.input-mode-request"));
        }

        public static InputModeRequest CreateRequest(PauseResult pauseResult, string requester, string reason)
        {
            if (!pauseResult.IsValid)
            {
                throw new ArgumentException("Pause InputMode request mapping requires a valid Pause result.", nameof(pauseResult));
            }

            string normalizedReason = reason.NormalizeTextOrFallback($"pause-result.{pauseResult.Status}.{pauseResult.CurrentState}.input-mode-request");
            return CreateRequest(
                pauseResult.CurrentState,
                requester.NormalizeTextOrFallback(pauseResult.Request.Source),
                normalizedReason);
        }
    }
}
