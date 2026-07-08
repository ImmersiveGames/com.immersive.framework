using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerTopology
{
    /// <summary>
    /// API status: Experimental. Passive validator that checks coherence between PlayerSlot declarations,
    /// authored PlayerSlot occupancies and PlayerEntry snapshots.
    /// It does not discover scenes, join players, spawn actors, bind views, bind control or move gameplay objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49F passive PlayerTopology validator.")]
    public static class PlayerTopologyValidator
    {
        public static PlayerTopologyValidationResult Validate(
            PlayerSlotSet playerSlotSet,
            IEnumerable<PlayerEntrySnapshot> playerEntries,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerTopologyValidator));
            var entries = ToList(playerEntries);
            var issues = new List<PlayerTopologyIssue>();

            if (playerSlotSet == null)
            {
                issues.Add(PlayerTopologyIssue.BlockingIssue(
                    PlayerTopologyIssueKind.MissingPlayerSlotSet,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    "PlayerTopology validation requires a PlayerSlotSet."));

                return new PlayerTopologyValidationResult(null, entries, issues, normalizedSource, reason);
            }

            AddPlayerSlotSetIssues(playerSlotSet, issues, normalizedSource);

            var slotDescriptors = new Dictionary<PlayerSlotId, PlayerSlotDescriptor>();
            for (int i = 0; i < playerSlotSet.Descriptors.Count; i++)
            {
                PlayerSlotDescriptor descriptor = playerSlotSet.Descriptors[i];
                if (!slotDescriptors.ContainsKey(descriptor.PlayerSlotId))
                {
                    slotDescriptors.Add(descriptor.PlayerSlotId, descriptor);
                }
            }

            var occupanciesBySlot = new Dictionary<PlayerSlotId, PlayerSlotOccupancyDescriptor>();
            var occupanciesByActor = new Dictionary<ActorId, PlayerSlotOccupancyDescriptor>();
            for (int i = 0; i < playerSlotSet.Occupancies.Count; i++)
            {
                PlayerSlotOccupancyDescriptor occupancy = playerSlotSet.Occupancies[i];
                if (!slotDescriptors.ContainsKey(occupancy.PlayerSlotId))
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.PlayerSlotOccupancyWithoutDeclaration,
                        occupancy.PlayerSlotId.StableText,
                        occupancy.OccupiedActorId.StableText,
                        normalizedSource,
                        "PlayerSlot occupancy references a PlayerSlot that has no declaration in the current validation scope."));
                }

                if (!occupanciesBySlot.ContainsKey(occupancy.PlayerSlotId))
                {
                    occupanciesBySlot.Add(occupancy.PlayerSlotId, occupancy);
                }

                if (occupanciesByActor.TryGetValue(occupancy.OccupiedActorId, out PlayerSlotOccupancyDescriptor existingOccupancy)
                    && existingOccupancy.PlayerSlotId != occupancy.PlayerSlotId)
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.DuplicateOccupiedActor,
                        occupancy.PlayerSlotId.StableText,
                        occupancy.OccupiedActorId.StableText,
                        normalizedSource,
                        $"Actor is occupied by more than one PlayerSlot. ExistingSlot='{existingOccupancy.PlayerSlotId.StableText}' DuplicateSlot='{occupancy.PlayerSlotId.StableText}'."));
                }
                else if (!occupanciesByActor.ContainsKey(occupancy.OccupiedActorId))
                {
                    occupanciesByActor.Add(occupancy.OccupiedActorId, occupancy);
                }
            }

            var entriesBySlot = new Dictionary<PlayerSlotId, PlayerEntrySnapshot>();
            var entriesByActor = new Dictionary<ActorId, PlayerEntrySnapshot>();
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerEntrySnapshot entry = entries[i];

                if (entriesBySlot.TryGetValue(entry.PlayerSlotId, out PlayerEntrySnapshot existingSlotEntry))
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.DuplicatePlayerEntrySlot,
                        entry.PlayerSlotId.StableText,
                        entry.ActorId.StableText,
                        normalizedSource,
                        $"A PlayerSlot can have only one PlayerEntry in the current validation scope. ExistingActor='{existingSlotEntry.ActorId.StableText}' DuplicateActor='{entry.ActorId.StableText}'."));
                }
                else
                {
                    entriesBySlot.Add(entry.PlayerSlotId, entry);
                }

                if (entriesByActor.TryGetValue(entry.ActorId, out PlayerEntrySnapshot existingActorEntry))
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.DuplicatePlayerEntryActor,
                        entry.PlayerSlotId.StableText,
                        entry.ActorId.StableText,
                        normalizedSource,
                        $"An Actor can have only one PlayerEntry in the current validation scope. ExistingSlot='{existingActorEntry.PlayerSlotId.StableText}' DuplicateSlot='{entry.PlayerSlotId.StableText}'."));
                }
                else
                {
                    entriesByActor.Add(entry.ActorId, entry);
                }

                if (!slotDescriptors.ContainsKey(entry.PlayerSlotId))
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.PlayerEntryWithoutPlayerSlotDeclaration,
                        entry.PlayerSlotId.StableText,
                        entry.ActorId.StableText,
                        normalizedSource,
                        "PlayerEntry references a PlayerSlot that has no declaration in the current validation scope."));
                }

                if (!occupanciesBySlot.TryGetValue(entry.PlayerSlotId, out PlayerSlotOccupancyDescriptor occupancy))
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.PlayerEntryWithoutPlayerSlotOccupancy,
                        entry.PlayerSlotId.StableText,
                        entry.ActorId.StableText,
                        normalizedSource,
                        "PlayerEntry references a PlayerSlot that has no authored occupancy in the current validation scope."));
                }
                else if (occupancy.OccupiedActorId != entry.ActorId)
                {
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.PlayerEntryActorMismatch,
                        entry.PlayerSlotId.StableText,
                        entry.ActorId.StableText,
                        normalizedSource,
                        $"PlayerEntry Actor does not match authored PlayerSlot occupancy. OccupiedActor='{occupancy.OccupiedActorId.StableText}' EntryActor='{entry.ActorId.StableText}'."));
                }
            }

            foreach (KeyValuePair<PlayerSlotId, PlayerSlotOccupancyDescriptor> pair in occupanciesBySlot)
            {
                if (!entriesBySlot.ContainsKey(pair.Key))
                {
                    PlayerSlotOccupancyDescriptor occupancy = pair.Value;
                    issues.Add(PlayerTopologyIssue.BlockingIssue(
                        PlayerTopologyIssueKind.PlayerSlotOccupancyWithoutPlayerEntry,
                        occupancy.PlayerSlotId.StableText,
                        occupancy.OccupiedActorId.StableText,
                        normalizedSource,
                        "PlayerSlot occupancy has no matching PlayerEntry in the current validation scope."));
                }
            }

            return new PlayerTopologyValidationResult(playerSlotSet, entries, issues, normalizedSource, reason);
        }

        private static void AddPlayerSlotSetIssues(
            PlayerSlotSet playerSlotSet,
            ICollection<PlayerTopologyIssue> issues,
            string normalizedSource)
        {
            for (int i = 0; i < playerSlotSet.Issues.Count; i++)
            {
                PlayerSlotSetIssue issue = playerSlotSet.Issues[i];
                issues.Add(new PlayerTopologyIssue(
                    PlayerTopologyIssueKind.PlayerSlotSetIssue,
                    issue.PlayerSlotIdText,
                    issue.OccupiedActorIdText,
                    normalizedSource,
                    $"PlayerSlotSet issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static List<PlayerEntrySnapshot> ToList(IEnumerable<PlayerEntrySnapshot> playerEntries)
        {
            var entries = new List<PlayerEntrySnapshot>();
            if (playerEntries == null)
            {
                return entries;
            }

            foreach (PlayerEntrySnapshot entry in playerEntries)
            {
                entries.Add(entry);
            }

            return entries;
        }
    }
}
