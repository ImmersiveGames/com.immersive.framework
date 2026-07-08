using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerTopology
{
    /// <summary>
    /// API status: Experimental. Immutable result for passive PlayerTopology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49F passive PlayerTopology validation result.")]
    public sealed class PlayerTopologyValidationResult
    {
        private readonly PlayerEntrySnapshot[] _playerEntries;
        private readonly PlayerTopologyIssue[] _issues;

        internal PlayerTopologyValidationResult(
            PlayerSlotSet playerSlotSet,
            IEnumerable<PlayerEntrySnapshot> playerEntries,
            IEnumerable<PlayerTopologyIssue> issues,
            string source,
            string reason)
        {
            PlayerSlotSet = playerSlotSet;
            _playerEntries = ToArray(playerEntries);
            _issues = ToArray(issues);
            Source = source.NormalizeTextOrFallback(nameof(PlayerTopologyValidationResult));
            Reason = reason.NormalizeText();
        }

        public PlayerSlotSet PlayerSlotSet { get; }

        public IReadOnlyList<PlayerEntrySnapshot> PlayerEntries => _playerEntries;

        public IReadOnlyList<PlayerTopologyIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int PlayerSlotCount => PlayerSlotSet != null ? PlayerSlotSet.Count : 0;

        public int OccupancyCount => PlayerSlotSet != null ? PlayerSlotSet.OccupancyCount : 0;

        public int PlayerEntryCount => _playerEntries.Length;

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

        public bool RequiresPlayerInput => false;

        public bool OwnsInputBehavior => false;

        public bool SpawnsActor => false;

        public bool BindsView => false;

        public bool BindsControl => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerSlots='").Append(PlayerSlotCount).Append("'");
            builder.Append(" occupancies='").Append(OccupancyCount).Append("'");
            builder.Append(" playerEntries='").Append(PlayerEntryCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" requiresPlayerInput='").Append(RequiresPlayerInput).Append("'");
            builder.Append(" inputBehavior='").Append(OwnsInputBehavior).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" viewBinding='").Append(BindsView).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
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
