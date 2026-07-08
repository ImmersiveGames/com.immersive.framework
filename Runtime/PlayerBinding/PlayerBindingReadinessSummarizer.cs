using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerTopology;
using Immersive.Framework.PlayerViews;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Passive aggregator for Player binding readiness. It does not bind views,
    /// activate cameras, activate input, bind controls, enable movement or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49K passive Player binding readiness summarizer.")]
    public static class PlayerBindingReadinessSummarizer
    {
        public static PlayerBindingReadinessSummary Summarize(
            PlayerTopologyValidationResult playerTopology,
            PlayerViewTopologyValidationResult playerViewTopology,
            PlayerControlTopologyValidationResult playerControlTopology,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerBindingReadinessSummarizer));
            var issues = new List<PlayerBindingReadinessIssue>();

            AddPlayerTopologyEvidence(playerTopology, issues, normalizedSource);
            AddPlayerViewTopologyEvidence(playerTopology, playerViewTopology, issues, normalizedSource);
            AddPlayerControlTopologyEvidence(playerTopology, playerControlTopology, issues, normalizedSource);
            AddParticipationEvidence(playerViewTopology, playerControlTopology, issues, normalizedSource);

            return new PlayerBindingReadinessSummary(
                playerTopology,
                playerViewTopology,
                playerControlTopology,
                issues,
                normalizedSource,
                reason);
        }

        private static void AddPlayerTopologyEvidence(
            PlayerTopologyValidationResult playerTopology,
            ICollection<PlayerBindingReadinessIssue> issues,
            string normalizedSource)
        {
            if (playerTopology == null)
            {
                issues.Add(PlayerBindingReadinessIssue.BlockingIssue(
                    PlayerBindingReadinessIssueKind.MissingPlayerTopology,
                    normalizedSource,
                    "Player binding readiness requires a PlayerTopologyValidationResult."));
                return;
            }

            for (int i = 0; i < playerTopology.Issues.Count; i++)
            {
                PlayerTopologyIssue issue = playerTopology.Issues[i];
                issues.Add(new PlayerBindingReadinessIssue(
                    PlayerBindingReadinessIssueKind.PlayerTopologyIssue,
                    normalizedSource,
                    $"PlayerTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddPlayerViewTopologyEvidence(
            PlayerTopologyValidationResult playerTopology,
            PlayerViewTopologyValidationResult playerViewTopology,
            ICollection<PlayerBindingReadinessIssue> issues,
            string normalizedSource)
        {
            if (playerViewTopology == null)
            {
                issues.Add(PlayerBindingReadinessIssue.BlockingIssue(
                    PlayerBindingReadinessIssueKind.MissingPlayerViewTopology,
                    normalizedSource,
                    "Player binding readiness requires a PlayerViewTopologyValidationResult."));
                return;
            }

            if (playerTopology != null && !object.ReferenceEquals(playerTopology, playerViewTopology.PlayerTopology))
            {
                issues.Add(PlayerBindingReadinessIssue.BlockingIssue(
                    PlayerBindingReadinessIssueKind.PlayerViewTopologyPlayerTopologyMismatch,
                    normalizedSource,
                    "PlayerViewTopologyValidationResult was produced from a different PlayerTopologyValidationResult instance."));
            }

            for (int i = 0; i < playerViewTopology.Issues.Count; i++)
            {
                PlayerViewTopologyIssue issue = playerViewTopology.Issues[i];
                issues.Add(new PlayerBindingReadinessIssue(
                    PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue,
                    normalizedSource,
                    $"PlayerViewTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddPlayerControlTopologyEvidence(
            PlayerTopologyValidationResult playerTopology,
            PlayerControlTopologyValidationResult playerControlTopology,
            ICollection<PlayerBindingReadinessIssue> issues,
            string normalizedSource)
        {
            if (playerControlTopology == null)
            {
                issues.Add(PlayerBindingReadinessIssue.BlockingIssue(
                    PlayerBindingReadinessIssueKind.MissingPlayerControlTopology,
                    normalizedSource,
                    "Player binding readiness requires a PlayerControlTopologyValidationResult."));
                return;
            }

            if (playerTopology != null && !object.ReferenceEquals(playerTopology, playerControlTopology.PlayerTopology))
            {
                issues.Add(PlayerBindingReadinessIssue.BlockingIssue(
                    PlayerBindingReadinessIssueKind.PlayerControlTopologyPlayerTopologyMismatch,
                    normalizedSource,
                    "PlayerControlTopologyValidationResult was produced from a different PlayerTopologyValidationResult instance."));
            }

            for (int i = 0; i < playerControlTopology.Issues.Count; i++)
            {
                PlayerControlTopologyIssue issue = playerControlTopology.Issues[i];
                issues.Add(new PlayerBindingReadinessIssue(
                    PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue,
                    normalizedSource,
                    $"PlayerControlTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddParticipationEvidence(
            PlayerViewTopologyValidationResult playerViewTopology,
            PlayerControlTopologyValidationResult playerControlTopology,
            ICollection<PlayerBindingReadinessIssue> issues,
            string normalizedSource)
        {
            if (playerViewTopology != null && playerViewTopology.ParticipatingPlayerViewCount == 0)
            {
                issues.Add(PlayerBindingReadinessIssue.NonBlockingIssue(
                    PlayerBindingReadinessIssueKind.NoParticipatingPlayerView,
                    normalizedSource,
                    "PlayerView topology has no participating PlayerView. View binding is not ready, but this is not a configuration failure by itself."));
            }

            if (playerControlTopology != null && playerControlTopology.ParticipatingPlayerControlCount == 0)
            {
                issues.Add(PlayerBindingReadinessIssue.NonBlockingIssue(
                    PlayerBindingReadinessIssueKind.NoParticipatingPlayerControl,
                    normalizedSource,
                    "PlayerControl topology has no participating PlayerControl. Control binding is not ready, but this is not a configuration failure by itself."));
            }
        }
    }
}
