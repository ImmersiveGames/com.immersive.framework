using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerTopology;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Immutable result for passive PlayerControl topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49J passive PlayerControl topology validation result.")]
    public sealed class PlayerControlTopologyValidationResult
    {
        private readonly PlayerControlSnapshot[] _playerControls;
        private readonly PlayerControlTopologyIssue[] _issues;

        internal PlayerControlTopologyValidationResult(
            PlayerTopologyValidationResult playerTopology,
            IEnumerable<PlayerControlSnapshot> playerControls,
            IEnumerable<PlayerControlTopologyIssue> issues,
            string source,
            string reason)
        {
            PlayerTopology = playerTopology;
            _playerControls = ToArray(playerControls);
            _issues = ToArray(issues);
            Source = source.NormalizeTextOrFallback(nameof(PlayerControlTopologyValidationResult));
            Reason = reason.NormalizeText();
        }

        public PlayerTopologyValidationResult PlayerTopology { get; }

        public IReadOnlyList<PlayerControlSnapshot> PlayerControls => _playerControls;

        public IReadOnlyList<PlayerControlTopologyIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int PlayerSlotCount => PlayerTopology != null ? PlayerTopology.PlayerSlotCount : 0;

        public int PlayerEntryCount => PlayerTopology != null ? PlayerTopology.PlayerEntryCount : 0;

        public int PlayerControlCount => _playerControls.Length;

        public int ParticipatingPlayerControlCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _playerControls.Length; i++)
                {
                    if (_playerControls[i].IsParticipating)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ReleasedPlayerControlCount => PlayerControlCount - ParticipatingPlayerControlCount;

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

        public bool ActivatesInput => false;

        public bool BindsControl => false;

        public bool OwnsInputBehavior => false;

        public bool EnablesMovement => false;

        public bool ActivatesCamera => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerSlots='").Append(PlayerSlotCount).Append("'");
            builder.Append(" playerEntries='").Append(PlayerEntryCount).Append("'");
            builder.Append(" playerControls='").Append(PlayerControlCount).Append("'");
            builder.Append(" participatingControls='").Append(ParticipatingPlayerControlCount).Append("'");
            builder.Append(" releasedControls='").Append(ReleasedPlayerControlCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" inputActivation='").Append(ActivatesInput).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
            builder.Append(" inputBehavior='").Append(OwnsInputBehavior).Append("'");
            builder.Append(" movement='").Append(EnablesMovement).Append("'");
            builder.Append(" cameraActivation='").Append(ActivatesCamera).Append("'");
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
