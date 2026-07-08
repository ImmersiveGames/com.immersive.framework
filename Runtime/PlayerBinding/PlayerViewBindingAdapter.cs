using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit PlayerView binding adapter.
    /// It validates readiness and applies/clears binding evidence on a target. It does not own runtime lifecycle,
    /// activate cameras, drive Cinemachine, bind control/input, enable movement or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A explicit PlayerView binding adapter contract.")]
    public static class PlayerViewBindingAdapter
    {
        public static PlayerViewBindingResult BindFirstReadyView(
            PlayerBindingReadinessSummary readinessSummary,
            IPlayerViewBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewBindingAdapter));
            if (!TryFindFirstActiveView(readinessSummary, out PlayerViewSnapshot view))
            {
                return ValidateBindingInputs(
                    readinessSummary,
                    default,
                    target,
                    normalizedSource,
                    reason,
                    true);
            }

            return Bind(readinessSummary, view, target, normalizedSource, reason);
        }

        public static PlayerViewBindingResult Bind(
            PlayerBindingReadinessSummary readinessSummary,
            PlayerViewSnapshot playerView,
            IPlayerViewBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewBindingAdapter));
            PlayerViewBindingResult validation = ValidateBindingInputs(
                readinessSummary,
                playerView,
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
                PlayerViewBindingSnapshot binding = PlayerViewBindingSnapshot.FromPlayerView(
                    playerView,
                    target.BindingTargetName,
                    normalizedSource,
                    reason);
                PlayerViewBindingResult result = target.ApplyPlayerViewBinding(binding, normalizedSource, reason);
                if (!result.Succeeded)
                {
                    return PlayerViewBindingResult.Failure(
                        PlayerViewBindingFailureKind.TargetRejectedBinding,
                        playerView.PlayerSlotId.StableText,
                        target.BindingTargetName,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.UnexpectedException,
                    playerView.PlayerSlotId.IsValid ? playerView.PlayerSlotId.StableText : string.Empty,
                    target != null ? target.BindingTargetName : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static PlayerViewBindingResult Clear(
            PlayerSlotId playerSlotId,
            IPlayerViewBindingTarget target,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewBindingAdapter));
            if (target == null)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.MissingBindingTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "PlayerView binding clear requires a target.");
            }

            if (!playerSlotId.IsValid)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.InvalidPlayerView,
                    string.Empty,
                    target.BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView binding clear requires a valid PlayerSlotId.");
            }

            try
            {
                return target.ClearPlayerViewBinding(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    target.BindingTargetName,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static PlayerViewBindingResult ValidateBindingInputs(
            PlayerBindingReadinessSummary readinessSummary,
            PlayerViewSnapshot playerView,
            IPlayerViewBindingTarget target,
            string source,
            string reason,
            bool missingPlayerViewFromSearch)
        {
            string targetName = target != null ? target.BindingTargetName : string.Empty;
            string playerSlotIdText = playerView.PlayerSlotId.IsValid ? playerView.PlayerSlotId.StableText : string.Empty;

            if (target == null)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.MissingBindingTarget,
                    playerSlotIdText,
                    string.Empty,
                    source,
                    reason,
                    "PlayerView binding requires a target.");
            }

            if (readinessSummary == null)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.MissingReadinessSummary,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerView binding requires a PlayerBindingReadinessSummary.");
            }

            if (!readinessSummary.IsReadyForViewBinding)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.ViewBindingNotReady,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerView binding readiness summary is not ready for view binding.");
            }

            if (missingPlayerViewFromSearch || !playerView.PlayerSlotId.IsValid)
            {
                return PlayerViewBindingResult.Failure(
                    missingPlayerViewFromSearch ? PlayerViewBindingFailureKind.MissingPlayerView : PlayerViewBindingFailureKind.InvalidPlayerView,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    missingPlayerViewFromSearch ? "No active PlayerView was found in readiness topology." : "PlayerView binding requires a valid PlayerView snapshot.");
            }

            if (!ContainsPlayerView(readinessSummary, playerView))
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.PlayerViewNotInReadinessTopology,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerView binding snapshot is not part of the provided readiness topology.");
            }

            if (!playerView.IsActive)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.PlayerViewNotActive,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerView binding requires PlayerViewState.Active.");
            }

            if (!playerView.IsEligibleForActiveView)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.PlayerViewNotEligible,
                    playerSlotIdText,
                    targetName,
                    source,
                    reason,
                    "PlayerView binding requires active PlayerView evidence with ViewBound or Active PlayerEntry evidence.");
            }

            return PlayerViewBindingResult.Success(
                playerView.PlayerSlotId,
                targetName,
                source,
                reason,
                "PlayerView binding inputs validated.");
        }

        private static bool TryFindFirstActiveView(PlayerBindingReadinessSummary readinessSummary, out PlayerViewSnapshot playerView)
        {
            playerView = default;
            if (readinessSummary?.PlayerViewTopology == null)
            {
                return false;
            }

            for (int i = 0; i < readinessSummary.PlayerViewTopology.PlayerViews.Count; i++)
            {
                PlayerViewSnapshot candidate = readinessSummary.PlayerViewTopology.PlayerViews[i];
                if (!candidate.IsReleased && candidate.IsActive && candidate.IsEligibleForActiveView)
                {
                    playerView = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPlayerView(PlayerBindingReadinessSummary readinessSummary, PlayerViewSnapshot playerView)
        {
            if (readinessSummary?.PlayerViewTopology == null)
            {
                return false;
            }

            for (int i = 0; i < readinessSummary.PlayerViewTopology.PlayerViews.Count; i++)
            {
                if (readinessSummary.PlayerViewTopology.PlayerViews[i].Equals(playerView))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
