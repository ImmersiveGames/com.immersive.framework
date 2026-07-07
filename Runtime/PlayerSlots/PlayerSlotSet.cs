using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Passive validation set for PlayerSlot declarations and authored occupancies.
    /// It does not own managers, registries, input behavior, possession or actor replacement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot validation set.")]
    public sealed class PlayerSlotSet
    {
        private readonly PlayerSlotDescriptor[] _descriptors;
        private readonly PlayerSlotOccupancyDescriptor[] _occupancies;
        private readonly PlayerSlotSetIssue[] _issues;

        private PlayerSlotSet(
            PlayerSlotDescriptor[] descriptors,
            PlayerSlotOccupancyDescriptor[] occupancies,
            PlayerSlotSetIssue[] issues,
            string source,
            string reason)
        {
            _descriptors = descriptors ?? Array.Empty<PlayerSlotDescriptor>();
            _occupancies = occupancies ?? Array.Empty<PlayerSlotOccupancyDescriptor>();
            _issues = issues ?? Array.Empty<PlayerSlotSetIssue>();
            Source = source.NormalizeTextOrFallback(nameof(PlayerSlotSet));
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<PlayerSlotDescriptor> Descriptors => _descriptors;

        public IReadOnlyList<PlayerSlotOccupancyDescriptor> Occupancies => _occupancies;

        public IReadOnlyList<PlayerSlotSetIssue> Issues => _issues;

        public string Source { get; }

        public string Reason { get; }

        public int Count => _descriptors.Length;

        public int OccupancyCount => _occupancies.Length;

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

        public int PlayerInputEvidenceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _descriptors.Length; i++)
                {
                    if (_descriptors[i].HasPlayerInputEvidence)
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

        public bool ChangesOccupancy => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("playerSlots='").Append(Count).Append("'");
            builder.Append(" occupancies='").Append(OccupancyCount).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" playerInputEvidence='").Append(PlayerInputEvidenceCount).Append("'");
            builder.Append(" requiresPlayerInput='").Append(RequiresPlayerInput).Append("'");
            builder.Append(" inputBehavior='").Append(OwnsInputBehavior).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" occupancyChanges='").Append(ChangesOccupancy).Append("'");
            for (int i = 0; i < _issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(_issues[i]).Append("'");
            }

            return builder.ToString();
        }

        public static PlayerSlotSet FromDescriptors(
            IEnumerable<PlayerSlotDescriptor> descriptors,
            IEnumerable<PlayerSlotOccupancyDescriptor> occupancies,
            string source,
            string reason)
        {
            return FromDescriptors(descriptors, occupancies, null, source, reason);
        }

        internal static PlayerSlotSet FromDescriptors(
            IEnumerable<PlayerSlotDescriptor> descriptors,
            IEnumerable<PlayerSlotOccupancyDescriptor> occupancies,
            IEnumerable<PlayerSlotSetIssue> existingIssues,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerSlotSet));
            var descriptorList = new List<PlayerSlotDescriptor>();
            var occupancyList = new List<PlayerSlotOccupancyDescriptor>();
            var issues = new List<PlayerSlotSetIssue>();
            var slotIds = new HashSet<PlayerSlotId>();
            var occupancySlotIds = new HashSet<PlayerSlotId>();

            if (existingIssues != null)
            {
                issues.AddRange(existingIssues);
            }

            if (descriptors != null)
            {
                foreach (PlayerSlotDescriptor descriptor in descriptors)
                {
                    descriptorList.Add(descriptor);

                    if (!slotIds.Add(descriptor.PlayerSlotId))
                    {
                        issues.Add(PlayerSlotSetIssue.BlockingIssue(
                            PlayerSlotSetIssueKind.DuplicatePlayerSlotId,
                            descriptor.PlayerSlotId.StableText,
                            string.Empty,
                            normalizedSource,
                            "PlayerSlot id must be unique in the current validation scope."));
                    }
                }
            }

            if (occupancies != null)
            {
                foreach (PlayerSlotOccupancyDescriptor occupancy in occupancies)
                {
                    occupancyList.Add(occupancy);

                    if (!occupancySlotIds.Add(occupancy.PlayerSlotId))
                    {
                        issues.Add(PlayerSlotSetIssue.BlockingIssue(
                            PlayerSlotSetIssueKind.DuplicatePlayerSlotOccupancy,
                            occupancy.PlayerSlotId.StableText,
                            occupancy.OccupiedActorId.StableText,
                            normalizedSource,
                            "A PlayerSlot can have only one authored occupancy in the current validation scope."));
                    }
                }
            }

            return new PlayerSlotSet(
                descriptorList.ToArray(),
                occupancyList.ToArray(),
                issues.ToArray(),
                normalizedSource,
                reason);
        }
    }
}
