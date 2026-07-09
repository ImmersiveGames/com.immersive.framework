using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit PlayerControl binding adapter.
    /// It validates readiness and applies/clears binding evidence on a target. It does not own runtime lifecycle,
    /// route InputActions, switch action maps, activate input, enable movement, drive gameplay or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A explicit PlayerControl binding adapter contract.")]
    public static class PlayerControlBindingAdapter
    {
        public static PlayerControlBindingResult BindFirstReadyControl(
            PlayerBindingReadinessSummary readinessSummary,
            IPlayerControlBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlBindingAdapter));
            if (!TryFindFirstActiveControl(readinessSummary, out PlayerControlSnapshot control))
            {
                return ValidateBindingInputs(
                    readinessSummary,
                    default,
                    target,
                    normalizedSource,
                    reason,
                    true);
            }

            return Bind(readinessSummary, control, target, normalizedSource, reason);
        }

        public static PlayerControlBindingResult Bind(
            PlayerBindingReadinessSummary readinessSummary,
            PlayerControlSnapshot playerControl,
            IPlayerControlBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlBindingAdapter));
            PlayerControlBindingResult validation = ValidateBindingInputs(
                readinessSummary,
                playerControl,
                target,
                normalizedSource,
                reason,
                false);

            if (validation.Failed || validation.NoOp)
            {
                return validation;
            }

            try
            {
                PlayerControlBindingSnapshot binding = PlayerControlBindingSnapshot.FromPlayerControl(
                    playerControl,
                    target.BindingTargetName,
                    normalizedSource,
                    reason);
                PlayerControlBindingResult result = target.ApplyPlayerControlBinding(binding, normalizedSource, reason);
                if (!result.Succeeded)
                {
                    return PlayerControlBindingResult.Failure(
                        PlayerControlBindingFailureKind.TargetRejectedBinding,
                        playerControl.PlayerSlotId.StableText,
                        target.BindingTargetName,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.UnexpectedException,
                    playerControl.PlayerSlotId.IsValid ? playerControl.PlayerSlotId.StableText : string.Empty,
                    target != null ? target.BindingTargetName : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static PlayerControlBindingResult Clear(
            PlayerSlotId playerSlotId,
            IPlayerControlBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlBindingAdapter));
            if (target == null)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.MissingBindingTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerControl binding clear requires a target.");
            }

            if (!playerSlotId.IsValid)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.InvalidPlayerControl,
                    string.Empty,
                    target.BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerControl binding clear requires a valid PlayerSlotId.");
            }

            try
            {
                return target.ClearPlayerControlBinding(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    target.BindingTargetName,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static PlayerControlBindingResult ValidateBindingInputs(
            PlayerBindingReadinessSummary readinessSummary,
            PlayerControlSnapshot playerControl,
            IPlayerControlBindingTarget target,
            string source,
            string reason,
            bool missingPlayerControlFromSearch)
        {
            string targetName = target != null ? target.BindingTargetName : string.Empty;
            string playerSlotIdText = playerControl.PlayerSlotId.IsValid ? playerControl.PlayerSlotId.StableText : string.Empty;

            if (target == null)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.MissingBindingTarget,
                    playerSlotIdText,
                    string.Empty,
                    source,
                    reason,
                    "PlayerControl binding requires a target.");
            }

            if (readinessSummary == null)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.MissingReadinessSummary,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerControl binding requires a PlayerBindingReadinessSummary.");
            }

            if (!readinessSummary.IsReadyForControlBinding)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.ControlBindingNotReady,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerControl binding readiness summary is not ready for control binding.");
            }

            if (missingPlayerControlFromSearch || !playerControl.PlayerSlotId.IsValid)
            {
                return PlayerControlBindingResult.Failure(
                    missingPlayerControlFromSearch ? PlayerControlBindingFailureKind.MissingPlayerControl : PlayerControlBindingFailureKind.InvalidPlayerControl,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    missingPlayerControlFromSearch ? "No active PlayerControl was found in readiness topology." : "PlayerControl binding requires a valid PlayerControl snapshot.");
            }

            if (!ContainsPlayerControl(readinessSummary, playerControl))
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.PlayerControlNotInReadinessTopology,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerControl binding snapshot is not part of the provided readiness topology.");
            }

            if (!playerControl.IsActive)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.PlayerControlNotActive,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerControl binding requires PlayerControlState.Active.");
            }

            if (!playerControl.IsEligibleForActiveControl)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.PlayerControlNotEligible,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerControl binding requires active PlayerControl evidence with Active PlayerEntry and Actor readiness for control.");
            }

            return PlayerControlBindingResult.Success(
                playerControl.PlayerSlotId,
                targetName,
                source,
                reason,
                "PlayerControl binding inputs validated.");
        }

        private static bool TryFindFirstActiveControl(PlayerBindingReadinessSummary readinessSummary, out PlayerControlSnapshot playerControl)
        {
            playerControl = default;
            if (readinessSummary?.PlayerControlTopology == null)
            {
                return false;
            }

            for (int i = 0; i < readinessSummary.PlayerControlTopology.PlayerControls.Count; i++)
            {
                PlayerControlSnapshot candidate = readinessSummary.PlayerControlTopology.PlayerControls[i];
                if (!candidate.IsReleased && candidate.IsActive && candidate.IsEligibleForActiveControl)
                {
                    playerControl = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPlayerControl(PlayerBindingReadinessSummary readinessSummary, PlayerControlSnapshot playerControl)
        {
            if (readinessSummary?.PlayerControlTopology == null)
            {
                return false;
            }

            for (int i = 0; i < readinessSummary.PlayerControlTopology.PlayerControls.Count; i++)
            {
                if (readinessSummary.PlayerControlTopology.PlayerControls[i].Equals(playerControl))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
