using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerTopology;
using Immersive.Framework.PlayerViews;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Passive validation report for authored Player binding evidence.
    /// This report does not bind views, activate cameras, activate input, enable movement or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50C passive Player binding authoring validation report with root-cause cleanup.")]
    public sealed class PlayerBindingAuthoringValidationReport
    {
        private readonly PlayerBindingAuthoringIssue[] _issues;
        private readonly PlayerBindingAuthoringIssue[] _rootCauseIssues;
        private readonly PlayerBindingAuthoringIssue[] _derivedIssues;

        public PlayerBindingAuthoringValidationReport(
            int playerSlotDeclarationCount,
            int playerSlotOccupancyCount,
            int actorReadinessBehaviourCount,
            int playerEntryBehaviourCount,
            int playerViewBehaviourCount,
            int playerControlBehaviourCount,
            PlayerTopologyValidationResult playerTopology,
            PlayerViewTopologyValidationResult playerViewTopology,
            PlayerControlTopologyValidationResult playerControlTopology,
            PlayerBindingReadinessSummary readinessSummary,
            PlayerBindingDiagnosticReport diagnosticReport,
            IEnumerable<PlayerBindingAuthoringIssue> issues,
            string source,
            string reason)
        {
            PlayerSlotDeclarationCount = Math.Max(0, playerSlotDeclarationCount);
            PlayerSlotOccupancyCount = Math.Max(0, playerSlotOccupancyCount);
            ActorReadinessBehaviourCount = Math.Max(0, actorReadinessBehaviourCount);
            PlayerEntryBehaviourCount = Math.Max(0, playerEntryBehaviourCount);
            PlayerViewBehaviourCount = Math.Max(0, playerViewBehaviourCount);
            PlayerControlBehaviourCount = Math.Max(0, playerControlBehaviourCount);
            PlayerTopology = playerTopology;
            PlayerViewTopology = playerViewTopology;
            PlayerControlTopology = playerControlTopology;
            ReadinessSummary = readinessSummary;
            DiagnosticReport = diagnosticReport;
            _issues = ToArray(issues);
            SplitIssues(_issues, out _rootCauseIssues, out _derivedIssues);
            Source = source.NormalizeTextOrFallback(nameof(PlayerBindingAuthoringValidationReport));
            Reason = reason.NormalizeText();
        }

        public int PlayerSlotDeclarationCount { get; }

        public int PlayerSlotOccupancyCount { get; }

        public int ActorReadinessBehaviourCount { get; }

        public int PlayerEntryBehaviourCount { get; }

        public int PlayerViewBehaviourCount { get; }

        public int PlayerControlBehaviourCount { get; }

        public PlayerTopologyValidationResult PlayerTopology { get; }

        public PlayerViewTopologyValidationResult PlayerViewTopology { get; }

        public PlayerControlTopologyValidationResult PlayerControlTopology { get; }

        public PlayerBindingReadinessSummary ReadinessSummary { get; }

        public PlayerBindingDiagnosticReport DiagnosticReport { get; }

        public IReadOnlyList<PlayerBindingAuthoringIssue> Issues => _issues;

        public IReadOnlyList<PlayerBindingAuthoringIssue> RootCauseIssues => _rootCauseIssues;

        public IReadOnlyList<PlayerBindingAuthoringIssue> DerivedIssues => _derivedIssues;

        public string Source { get; }

        public string Reason { get; }

        public int IssueCount => _issues.Length;

        public int RootCauseIssueCount => _rootCauseIssues.Length;

        public int DerivedIssueCount => _derivedIssues.Length;

        public int BlockingIssueCount => CountBlocking(_issues);

        public int RootCauseBlockingIssueCount => CountBlocking(_rootCauseIssues);

        public int DerivedBlockingIssueCount => CountBlocking(_derivedIssues);

        public bool HasDerivedIssues => DerivedIssueCount > 0;

        public bool HasRootCauseIssues => RootCauseIssueCount > 0;

        public bool HasReadinessSummary => ReadinessSummary != null;

        public bool HasDiagnosticReport => DiagnosticReport != null;

        public bool IsReadyForViewBinding => ReadinessSummary != null && ReadinessSummary.IsReadyForViewBinding;

        public bool IsReadyForControlBinding => ReadinessSummary != null && ReadinessSummary.IsReadyForControlBinding;

        public bool IsReadyForFullBinding => ReadinessSummary != null && ReadinessSummary.IsReadyForFullBinding;

        public bool Succeeded => BlockingIssueCount == 0
            && DiagnosticReport != null
            && DiagnosticReport.Succeeded
            && IsReadyForFullBinding;

        public bool Failed => !Succeeded;

        public bool BindsView => false;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool HasIssue(PlayerBindingAuthoringIssueKind kind)
        {
            return HasIssue(_issues, kind);
        }

        public bool HasRootCauseIssue(PlayerBindingAuthoringIssueKind kind)
        {
            return HasIssue(_rootCauseIssues, kind);
        }

        public bool HasDerivedIssue(PlayerBindingAuthoringIssueKind kind)
        {
            return HasIssue(_derivedIssues, kind);
        }

        public bool HasBlockingIssue(PlayerBindingAuthoringIssueKind kind)
        {
            for (int i = 0; i < _issues.Length; i++)
            {
                if (_issues[i].Kind == kind && _issues[i].Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            AppendSummary(builder);
            builder.Append(" rootIssues='").Append(RootCauseIssueCount).Append("'");
            builder.Append(" rootBlockingIssues='").Append(RootCauseBlockingIssueCount).Append("'");
            builder.Append(" derivedIssues='").Append(DerivedIssueCount).Append("'");
            builder.Append(" derivedBlockingIssues='").Append(DerivedBlockingIssueCount).Append("'");
            AppendBoundary(builder);

            for (int i = 0; i < _rootCauseIssues.Length; i++)
            {
                builder.Append(" rootIssue[").Append(i).Append("]='").Append(_rootCauseIssues[i]).Append("'");
            }

            if (_derivedIssues.Length > 0)
            {
                builder.Append(" derivedIssuesSuppressed='").Append(_derivedIssues.Length).Append("'");
            }

            return builder.ToString();
        }

        public string ToDetailedDiagnosticString()
        {
            var builder = new StringBuilder();
            AppendSummary(builder);
            builder.Append(" rootIssues='").Append(RootCauseIssueCount).Append("'");
            builder.Append(" rootBlockingIssues='").Append(RootCauseBlockingIssueCount).Append("'");
            builder.Append(" derivedIssues='").Append(DerivedIssueCount).Append("'");
            builder.Append(" derivedBlockingIssues='").Append(DerivedBlockingIssueCount).Append("'");
            AppendBoundary(builder);

            for (int i = 0; i < _rootCauseIssues.Length; i++)
            {
                builder.Append(" rootIssue[").Append(i).Append("]='").Append(_rootCauseIssues[i]).Append("'");
            }

            for (int i = 0; i < _derivedIssues.Length; i++)
            {
                builder.Append(" derivedIssue[").Append(i).Append("]='").Append(_derivedIssues[i]).Append("'");
            }

            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        private void AppendSummary(StringBuilder builder)
        {
            builder.Append("playerSlotDeclarations='").Append(PlayerSlotDeclarationCount).Append("'");
            builder.Append(" playerSlotOccupancies='").Append(PlayerSlotOccupancyCount).Append("'");
            builder.Append(" actorReadinessBehaviours='").Append(ActorReadinessBehaviourCount).Append("'");
            builder.Append(" playerEntryBehaviours='").Append(PlayerEntryBehaviourCount).Append("'");
            builder.Append(" playerViewBehaviours='").Append(PlayerViewBehaviourCount).Append("'");
            builder.Append(" playerControlBehaviours='").Append(PlayerControlBehaviourCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" readyForViewBinding='").Append(IsReadyForViewBinding).Append("'");
            builder.Append(" readyForControlBinding='").Append(IsReadyForControlBinding).Append("'");
            builder.Append(" readyForFullBinding='").Append(IsReadyForFullBinding).Append("'");
            builder.Append(" diagnosticErrors='").Append(DiagnosticReport != null ? DiagnosticReport.ErrorCount : 0).Append("'");
            builder.Append(" diagnosticWarnings='").Append(DiagnosticReport != null ? DiagnosticReport.WarningCount : 0).Append("'");
        }

        private void AppendBoundary(StringBuilder builder)
        {
            builder.Append(" viewBinding='").Append(BindsView).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
            builder.Append(" cameraActivation='").Append(ActivatesCamera).Append("'");
            builder.Append(" inputActivation='").Append(ActivatesInput).Append("'");
            builder.Append(" movement='").Append(EnablesMovement).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
        }

        private static bool HasIssue(PlayerBindingAuthoringIssue[] issues, PlayerBindingAuthoringIssueKind kind)
        {
            for (int i = 0; i < issues.Length; i++)
            {
                if (issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountBlocking(PlayerBindingAuthoringIssue[] issues)
        {
            int count = 0;
            for (int i = 0; i < issues.Length; i++)
            {
                if (issues[i].Blocking)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SplitIssues(
            PlayerBindingAuthoringIssue[] issues,
            out PlayerBindingAuthoringIssue[] rootCauseIssues,
            out PlayerBindingAuthoringIssue[] derivedIssues)
        {
            var roots = new List<PlayerBindingAuthoringIssue>();
            var derived = new List<PlayerBindingAuthoringIssue>();
            bool hasPrimaryEvidenceIssue = HasPrimaryEvidenceIssue(issues);
            bool hasTopologyIssue = HasTopologyIssue(issues);
            bool hasUpstreamIssue = hasPrimaryEvidenceIssue || hasTopologyIssue;

            for (int i = 0; i < issues.Length; i++)
            {
                PlayerBindingAuthoringIssue issue = issues[i];
                if (IsDerivedIssue(issue.Kind, hasPrimaryEvidenceIssue, hasUpstreamIssue))
                {
                    derived.Add(issue);
                }
                else
                {
                    roots.Add(issue);
                }
            }

            rootCauseIssues = roots.ToArray();
            derivedIssues = derived.ToArray();
        }

        private static bool IsDerivedIssue(
            PlayerBindingAuthoringIssueKind kind,
            bool hasPrimaryEvidenceIssue,
            bool hasUpstreamIssue)
        {
            switch (kind)
            {
                case PlayerBindingAuthoringIssueKind.PlayerTopologyIssue:
                case PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue:
                case PlayerBindingAuthoringIssueKind.PlayerControlTopologyIssue:
                    return hasPrimaryEvidenceIssue;
                case PlayerBindingAuthoringIssueKind.BindingReadinessIssue:
                case PlayerBindingAuthoringIssueKind.BindingDiagnosticError:
                    return hasUpstreamIssue;
                default:
                    return false;
            }
        }

        private static bool HasPrimaryEvidenceIssue(PlayerBindingAuthoringIssue[] issues)
        {
            for (int i = 0; i < issues.Length; i++)
            {
                switch (issues[i].Kind)
                {
                    case PlayerBindingAuthoringIssueKind.MissingValidationRoot:
                    case PlayerBindingAuthoringIssueKind.MissingPlayerSlotDeclaration:
                    case PlayerBindingAuthoringIssueKind.MissingPlayerSlotOccupancy:
                    case PlayerBindingAuthoringIssueKind.MissingActorReadinessBehaviour:
                    case PlayerBindingAuthoringIssueKind.MissingPlayerEntryBehaviour:
                    case PlayerBindingAuthoringIssueKind.MissingPlayerViewBehaviour:
                    case PlayerBindingAuthoringIssueKind.MissingPlayerControlBehaviour:
                    case PlayerBindingAuthoringIssueKind.PlayerSlotDeclarationIssue:
                    case PlayerBindingAuthoringIssueKind.PlayerSlotOccupancyIssue:
                    case PlayerBindingAuthoringIssueKind.PlayerSlotSetIssue:
                    case PlayerBindingAuthoringIssueKind.PlayerEntrySnapshotFailure:
                    case PlayerBindingAuthoringIssueKind.PlayerViewSnapshotFailure:
                    case PlayerBindingAuthoringIssueKind.PlayerControlSnapshotFailure:
                        return true;
                }
            }

            return false;
        }

        private static bool HasTopologyIssue(PlayerBindingAuthoringIssue[] issues)
        {
            for (int i = 0; i < issues.Length; i++)
            {
                switch (issues[i].Kind)
                {
                    case PlayerBindingAuthoringIssueKind.PlayerTopologyIssue:
                    case PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue:
                    case PlayerBindingAuthoringIssueKind.PlayerControlTopologyIssue:
                        return true;
                }
            }

            return false;
        }

        private static PlayerBindingAuthoringIssue[] ToArray(IEnumerable<PlayerBindingAuthoringIssue> issues)
        {
            if (issues == null)
            {
                return Array.Empty<PlayerBindingAuthoringIssue>();
            }

            var list = new List<PlayerBindingAuthoringIssue>();
            foreach (PlayerBindingAuthoringIssue issue in issues)
            {
                list.Add(issue);
            }

            return list.ToArray();
        }
    }
}
