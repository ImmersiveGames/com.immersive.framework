using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerTopology;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Immutable result for passive PlayerView topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49H passive PlayerView topology validation result.")]
    public sealed class PlayerViewTopologyValidationResult
    {
        private readonly PlayerViewSnapshot[] _playerViews;
        private readonly PlayerViewTopologyIssue[] _issues;

        internal PlayerViewTopologyValidationResult(
            PlayerTopologyValidationResult playerTopology,
            IEnumerable<PlayerViewSnapshot> playerViews,
            IEnumerable<PlayerViewTopologyIssue> issues,
            string source,
            string reason)
        {
            PlayerTopology = playerTopology;
            _playerViews = ToArray(playerViews);
            _issues = ToArray(issues);
            Source = source.NormalizeTextOrFallback(nameof(PlayerViewTopologyValidationResult));
            Reason = reason.NormalizeText();
        }

        public PlayerTopologyValidationResult PlayerTopology { get; }

        public IReadOnlyList<PlayerViewSnapshot> PlayerViews => _playerViews;

        public IReadOnlyList<PlayerViewTopologyIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int PlayerSlotCount => PlayerTopology != null ? PlayerTopology.PlayerSlotCount : 0;

        public int PlayerEntryCount => PlayerTopology != null ? PlayerTopology.PlayerEntryCount : 0;

        public int PlayerViewCount => _playerViews.Length;

        public int ParticipatingPlayerViewCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _playerViews.Length; i++)
                {
                    if (!_playerViews[i].IsReleased)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ReleasedPlayerViewCount => PlayerViewCount - ParticipatingPlayerViewCount;

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

        public bool ActivatesCamera => false;

        public bool BindsView => false;

        public bool BindsControl => false;

        public bool OwnsInputBehavior => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerSlots='").Append(PlayerSlotCount).Append("'");
            builder.Append(" playerEntries='").Append(PlayerEntryCount).Append("'");
            builder.Append(" playerViews='").Append(PlayerViewCount).Append("'");
            builder.Append(" participatingViews='").Append(ParticipatingPlayerViewCount).Append("'");
            builder.Append(" releasedViews='").Append(ReleasedPlayerViewCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" activatesCamera='").Append(ActivatesCamera).Append("'");
            builder.Append(" viewBinding='").Append(BindsView).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
            builder.Append(" inputBehavior='").Append(OwnsInputBehavior).Append("'");
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
