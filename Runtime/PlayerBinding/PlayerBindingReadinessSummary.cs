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
    /// API status: Experimental. Immutable passive summary that aggregates player topology, view topology
    /// and control topology readiness for later binding cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49K passive Player binding readiness summary.")]
    public sealed class PlayerBindingReadinessSummary
    {
        private readonly PlayerBindingReadinessIssue[] _issues;

        internal PlayerBindingReadinessSummary(
            PlayerTopologyValidationResult playerTopology,
            PlayerViewTopologyValidationResult playerViewTopology,
            PlayerControlTopologyValidationResult playerControlTopology,
            IEnumerable<PlayerBindingReadinessIssue> issues,
            string source,
            string reason)
        {
            PlayerTopology = playerTopology;
            PlayerViewTopology = playerViewTopology;
            PlayerControlTopology = playerControlTopology;
            _issues = ToArray(issues);
            Source = source.NormalizeTextOrFallback(nameof(PlayerBindingReadinessSummary));
            Reason = reason.NormalizeText();
        }

        public PlayerTopologyValidationResult PlayerTopology { get; }

        public PlayerViewTopologyValidationResult PlayerViewTopology { get; }

        public PlayerControlTopologyValidationResult PlayerControlTopology { get; }

        public IReadOnlyList<PlayerBindingReadinessIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public bool HasPlayerTopology => PlayerTopology != null;

        public bool HasPlayerViewTopology => PlayerViewTopology != null;

        public bool HasPlayerControlTopology => PlayerControlTopology != null;

        public int PlayerSlotCount => PlayerTopology != null ? PlayerTopology.PlayerSlotCount : 0;

        public int PlayerEntryCount => PlayerTopology != null ? PlayerTopology.PlayerEntryCount : 0;

        public int PlayerViewCount => PlayerViewTopology != null ? PlayerViewTopology.PlayerViewCount : 0;

        public int ParticipatingPlayerViewCount => PlayerViewTopology != null ? PlayerViewTopology.ParticipatingPlayerViewCount : 0;

        public int PlayerControlCount => PlayerControlTopology != null ? PlayerControlTopology.PlayerControlCount : 0;

        public int ParticipatingPlayerControlCount => PlayerControlTopology != null ? PlayerControlTopology.ParticipatingPlayerControlCount : 0;

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

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public bool IsReadyForViewBinding
        {
            get
            {
                return PlayerTopology != null
                    && PlayerViewTopology != null
                    && PlayerTopology.Succeeded
                    && PlayerViewTopology.Succeeded
                    && ParticipatingPlayerViewCount > 0
                    && !HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyPlayerTopologyMismatch);
            }
        }

        public bool IsReadyForControlBinding
        {
            get
            {
                return PlayerTopology != null
                    && PlayerControlTopology != null
                    && PlayerTopology.Succeeded
                    && PlayerControlTopology.Succeeded
                    && ParticipatingPlayerControlCount > 0
                    && !HasBlockingIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyPlayerTopologyMismatch);
            }
        }

        public bool IsReadyForFullBinding => IsReadyForViewBinding && IsReadyForControlBinding && Succeeded;

        public bool BindsView => false;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool HasIssue(PlayerBindingReadinessIssueKind kind)
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

        public bool HasBlockingIssue(PlayerBindingReadinessIssueKind kind)
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
            builder.Append("playerSlots='").Append(PlayerSlotCount).Append("'");
            builder.Append(" playerEntries='").Append(PlayerEntryCount).Append("'");
            builder.Append(" playerViews='").Append(PlayerViewCount).Append("'");
            builder.Append(" participatingViews='").Append(ParticipatingPlayerViewCount).Append("'");
            builder.Append(" playerControls='").Append(PlayerControlCount).Append("'");
            builder.Append(" participatingControls='").Append(ParticipatingPlayerControlCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" readyForViewBinding='").Append(IsReadyForViewBinding).Append("'");
            builder.Append(" readyForControlBinding='").Append(IsReadyForControlBinding).Append("'");
            builder.Append(" readyForFullBinding='").Append(IsReadyForFullBinding).Append("'");
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

        private static T[] ToArray<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                return Array.Empty<T>();
            }

            var list = new List<T>();
            foreach (T value in values)
            {
                list.Add(value);
            }

            return list.ToArray();
        }
    }
}
