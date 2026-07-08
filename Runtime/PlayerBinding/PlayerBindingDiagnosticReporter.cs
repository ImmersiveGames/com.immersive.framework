using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic reporter for Player binding readiness.
    /// It only transforms readiness facts into readable diagnostics and never performs binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49L passive Player binding diagnostic reporter.")]
    public static class PlayerBindingDiagnosticReporter
    {
        public static PlayerBindingDiagnosticReport CreateReport(
            PlayerBindingReadinessSummary summary,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerBindingDiagnosticReporter));
            var messages = new List<PlayerBindingDiagnosticMessage>();

            if (summary == null)
            {
                Add(messages, PlayerBindingDiagnosticMessageKind.MissingReadinessSummary, PlayerBindingDiagnosticSeverity.Error, "Player binding blocked: missing PlayerBindingReadinessSummary.");
                AddPassiveBoundary(messages);
                return new PlayerBindingDiagnosticReport(null, messages, normalizedSource, reason);
            }

            Add(messages, PlayerBindingDiagnosticMessageKind.SummaryAccepted, PlayerBindingDiagnosticSeverity.Info, "Player binding readiness summary accepted.");
            AddViewReadinessMessage(summary, messages);
            AddControlReadinessMessage(summary, messages);
            AddFullReadinessMessage(summary, messages);
            AddReadinessIssues(summary, messages);
            AddPassiveBoundary(messages);

            return new PlayerBindingDiagnosticReport(summary, messages, normalizedSource, reason);
        }

        private static void AddViewReadinessMessage(PlayerBindingReadinessSummary summary, ICollection<PlayerBindingDiagnosticMessage> messages)
        {
            if (summary.IsReadyForViewBinding)
            {
                Add(messages, PlayerBindingDiagnosticMessageKind.ReadyForViewBinding, PlayerBindingDiagnosticSeverity.Info, "Player view binding ready.");
                return;
            }

            Add(
                messages,
                PlayerBindingDiagnosticMessageKind.ViewBindingNotReady,
                HasBlockingViewReason(summary) ? PlayerBindingDiagnosticSeverity.Error : PlayerBindingDiagnosticSeverity.Warning,
                BuildViewNotReadyText(summary));
        }

        private static void AddControlReadinessMessage(PlayerBindingReadinessSummary summary, ICollection<PlayerBindingDiagnosticMessage> messages)
        {
            if (summary.IsReadyForControlBinding)
            {
                Add(messages, PlayerBindingDiagnosticMessageKind.ReadyForControlBinding, PlayerBindingDiagnosticSeverity.Info, "Player control binding ready.");
                return;
            }

            Add(
                messages,
                PlayerBindingDiagnosticMessageKind.ControlBindingNotReady,
                HasBlockingControlReason(summary) ? PlayerBindingDiagnosticSeverity.Error : PlayerBindingDiagnosticSeverity.Warning,
                BuildControlNotReadyText(summary));
        }

        private static void AddFullReadinessMessage(PlayerBindingReadinessSummary summary, ICollection<PlayerBindingDiagnosticMessage> messages)
        {
            if (summary.IsReadyForFullBinding)
            {
                Add(messages, PlayerBindingDiagnosticMessageKind.ReadyForFullBinding, PlayerBindingDiagnosticSeverity.Info, "Player binding ready.");
                return;
            }

            Add(
                messages,
                PlayerBindingDiagnosticMessageKind.FullBindingNotReady,
                summary.BlockingIssueCount > 0 ? PlayerBindingDiagnosticSeverity.Error : PlayerBindingDiagnosticSeverity.Warning,
                "Player binding not ready for full binding.");
        }

        private static void AddReadinessIssues(PlayerBindingReadinessSummary summary, ICollection<PlayerBindingDiagnosticMessage> messages)
        {
            for (int i = 0; i < summary.Issues.Count; i++)
            {
                PlayerBindingReadinessIssue issue = summary.Issues[i];
                PlayerBindingDiagnosticSeverity severity = issue.Blocking
                    ? PlayerBindingDiagnosticSeverity.Error
                    : PlayerBindingDiagnosticSeverity.Warning;

                PlayerBindingDiagnosticMessageKind messageKind = PlayerBindingDiagnosticMessageKind.ReadinessIssue;
                if (issue.Kind == PlayerBindingReadinessIssueKind.NoParticipatingPlayerView)
                {
                    messageKind = PlayerBindingDiagnosticMessageKind.NoParticipatingPlayerView;
                }
                else if (issue.Kind == PlayerBindingReadinessIssueKind.NoParticipatingPlayerControl)
                {
                    messageKind = PlayerBindingDiagnosticMessageKind.NoParticipatingPlayerControl;
                }

                Add(messages, messageKind, severity, BuildIssueText(issue), issue.Kind);
            }
        }

        private static string BuildViewNotReadyText(PlayerBindingReadinessSummary summary)
        {
            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerTopology))
            {
                return "Player view binding blocked: missing PlayerTopologyValidationResult.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerViewTopology))
            {
                return "Player view binding blocked: missing PlayerViewTopologyValidationResult.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue))
            {
                return "Player view binding blocked: PlayerView topology has blocking issues.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyPlayerTopologyMismatch))
            {
                return "Player view binding blocked: PlayerView topology was produced from a different PlayerTopology instance.";
            }

            if (summary.HasIssue(PlayerBindingReadinessIssueKind.NoParticipatingPlayerView))
            {
                return "Player view binding not ready: no participating PlayerView.";
            }

            return "Player view binding not ready.";
        }

        private static string BuildControlNotReadyText(PlayerBindingReadinessSummary summary)
        {
            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerTopology))
            {
                return "Player control binding blocked: missing PlayerTopologyValidationResult.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerControlTopology))
            {
                return "Player control binding blocked: missing PlayerControlTopologyValidationResult.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue))
            {
                return "Player control binding blocked: PlayerControl topology has blocking issues.";
            }

            if (summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyPlayerTopologyMismatch))
            {
                return "Player control binding blocked: PlayerControl topology was produced from a different PlayerTopology instance.";
            }

            if (summary.HasIssue(PlayerBindingReadinessIssueKind.NoParticipatingPlayerControl))
            {
                return "Player control binding not ready: no participating PlayerControl.";
            }

            return "Player control binding not ready.";
        }

        private static string BuildIssueText(PlayerBindingReadinessIssue issue)
        {
            if (string.IsNullOrWhiteSpace(issue.Message))
            {
                return $"Player binding readiness issue: {issue.Kind}.";
            }

            return $"Player binding readiness issue: {issue.Kind}. {issue.Message}";
        }

        private static bool HasBlockingViewReason(PlayerBindingReadinessSummary summary)
        {
            return summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerTopology)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerViewTopology)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerTopologyIssue)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyPlayerTopologyMismatch);
        }

        private static bool HasBlockingControlReason(PlayerBindingReadinessSummary summary)
        {
            return summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerTopology)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.MissingPlayerControlTopology)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerTopologyIssue)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue)
                || summary.HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyPlayerTopologyMismatch);
        }

        private static void AddPassiveBoundary(ICollection<PlayerBindingDiagnosticMessage> messages)
        {
            Add(
                messages,
                PlayerBindingDiagnosticMessageKind.PassiveBoundary,
                PlayerBindingDiagnosticSeverity.Info,
                "Player binding diagnostic reporter is passive: it does not bind view, bind control, activate camera, activate input, enable movement or spawn actors.");
        }

        private static void Add(
            ICollection<PlayerBindingDiagnosticMessage> messages,
            PlayerBindingDiagnosticMessageKind kind,
            PlayerBindingDiagnosticSeverity severity,
            string text,
            PlayerBindingReadinessIssueKind readinessIssueKind = PlayerBindingReadinessIssueKind.None)
        {
            messages.Add(new PlayerBindingDiagnosticMessage(kind, severity, text, readinessIssueKind));
        }
    }
}
