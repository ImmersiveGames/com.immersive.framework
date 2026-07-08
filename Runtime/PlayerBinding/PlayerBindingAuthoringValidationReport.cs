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
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50A passive Player binding authoring validation report.")]
    public sealed class PlayerBindingAuthoringValidationReport
    {
        private readonly PlayerBindingAuthoringIssue[] _issues;

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

        public string Source { get; }

        public string Reason { get; }

        public int IssueCount => _issues.Length;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                {
                    if (_issues[i].Blocking)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

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
            for (int i = 0; i < _issues.Length; i++)
            {
                if (_issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
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
            builder.Append(" viewBinding='").Append(BindsView).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
            builder.Append(" cameraActivation='").Append(ActivatesCamera).Append("'");
            builder.Append(" inputActivation='").Append(ActivatesInput).Append("'");
            builder.Append(" movement='").Append(EnablesMovement).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
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
