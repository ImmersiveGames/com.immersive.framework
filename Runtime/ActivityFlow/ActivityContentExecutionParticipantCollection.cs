using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Immutable passive collection of Activity Content Execution participants.
    /// It validates descriptors, removes invalid/duplicate entries and exposes deterministic ordering only; it does not discover participants, execute requests or integrate with Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant collection and ordering model; no discovery runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantCollection
    {
        private readonly ActivityContentExecutionParticipantEntry[] _entries;
        private readonly ActivityContentExecutionParticipantCollectionIssue[] _issues;

        private ActivityContentExecutionParticipantCollection(IReadOnlyList<IActivityContentExecutionParticipant> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                _entries = Array.Empty<ActivityContentExecutionParticipantEntry>();
                _issues = Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
                return;
            }

            var acceptedEntries = new List<ActivityContentExecutionParticipantEntry>(participants.Count);
            var detectedIssues = new List<ActivityContentExecutionParticipantCollectionIssue>();
            var contentIdKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                if (participant == null)
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant,
                        i,
                        $"index:{i}",
                        default,
                        "Activity content execution participant is null and was not added to the collection."));
                    continue;
                }

                var descriptor = participant.GetActivityContentExecutionDescriptor();
                if (!descriptor.IsValid)
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor,
                        i,
                        CreateIssueKey(descriptor, i),
                        descriptor,
                        "Activity content execution participant descriptor is invalid and was not added to the collection."));
                    continue;
                }

                string contentIdKey = descriptor.ContentId.StableText;
                if (!contentIdKeys.Add(contentIdKey))
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId,
                        i,
                        contentIdKey,
                        descriptor,
                        "Activity content execution participant content id already exists in the collection; duplicate participant was not added."));
                    continue;
                }

                acceptedEntries.Add(new ActivityContentExecutionParticipantEntry(participant, descriptor, i));
            }

            if (acceptedEntries.Count == 0)
            {
                _entries = Array.Empty<ActivityContentExecutionParticipantEntry>();
            }
            else
            {
                ActivityContentExecutionParticipantEntry[] orderedEntries = acceptedEntries.ToArray();
                Array.Sort(orderedEntries, CompareEntries);
                _entries = orderedEntries;
            }

            _issues = detectedIssues.Count == 0
                ? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>()
                : detectedIssues.ToArray();
        }

        private IReadOnlyList<ActivityContentExecutionParticipantEntry> Entries => _entries ?? Array.Empty<ActivityContentExecutionParticipantEntry>();

        private IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> Issues => _issues ?? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();

        public int Count => Entries.Count;

        public int IssueCount => Issues.Count;

        public bool HasParticipants => Count > 0;

        public bool HasIssues => IssueCount > 0;

        private int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        private int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        public int EnterCount => CountByPhase(ActivityContentExecutionPhase.Enter);

        public int ExitCount => CountByPhase(ActivityContentExecutionPhase.Exit);

        private int NullParticipantIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant);

        private int InvalidDescriptorIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor);

        private int DuplicateContentIdIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId);

        public ActivityContentExecutionParticipantEntry[] SnapshotEntriesForPhase(ActivityContentExecutionPhase phase)
        {
            ValidatePhase(phase);

            if (!HasParticipants)
            {
                return Array.Empty<ActivityContentExecutionParticipantEntry>();
            }

            var selected = new List<ActivityContentExecutionParticipantEntry>();
            IReadOnlyList<ActivityContentExecutionParticipantEntry> items = Entries;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].SupportsPhase(phase))
                {
                    selected.Add(items[i]);
                }
            }

            return selected.Count == 0 ? Array.Empty<ActivityContentExecutionParticipantEntry>() : selected.ToArray();
        }

        public ActivityContentExecutionParticipantCollectionIssue[] SnapshotIssues()
        {
            if (!HasIssues)
            {
                return Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
            }

            var snapshot = new ActivityContentExecutionParticipantCollectionIssue[IssueCount];
            for (int i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append($"Activity Content Execution Participant Collection count='{Count}' required='{RequiredCount}' optional='{OptionalCount}' enter='{EnterCount}' exit='{ExitCount}' issues='{IssueCount}' nullParticipants='{NullParticipantIssueCount}' invalidDescriptors='{InvalidDescriptorIssueCount}' duplicateContentIds='{DuplicateContentIdIssueCount}' entries=[");
            IReadOnlyList<ActivityContentExecutionParticipantEntry> items = Entries;
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(items[i].ToDiagnosticString());
            }

            builder.Append("] issues=[");
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> issueItems = Issues;
            for (int i = 0; i < issueItems.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(issueItems[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionParticipantCollection Empty()
        {
            return new ActivityContentExecutionParticipantCollection(Array.Empty<IActivityContentExecutionParticipant>());
        }

        public static ActivityContentExecutionParticipantCollection FromParticipants(IReadOnlyList<IActivityContentExecutionParticipant> participants)
        {
            return new ActivityContentExecutionParticipantCollection(participants);
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (!HasParticipants)
            {
                return 0;
            }

            IReadOnlyList<ActivityContentExecutionParticipantEntry> items = Entries;

            return items.Count(t => t.Requiredness == requiredness);
        }

        private int CountByPhase(ActivityContentExecutionPhase phase)
        {
            if (!HasParticipants)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<ActivityContentExecutionParticipantEntry> items = Entries;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].SupportsPhase(phase))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind kind)
        {
            if (!HasIssues)
            {
                return 0;
            }

            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> items = Issues;

            return items.Count(t => t.Kind == kind);
        }

        private static int CompareEntries(ActivityContentExecutionParticipantEntry left, ActivityContentExecutionParticipantEntry right)
        {
            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            int indexCompare = left.SourceIndex.CompareTo(right.SourceIndex);
            return indexCompare != 0 ? indexCompare : string.Compare(left.ContentId.StableText, right.ContentId.StableText, StringComparison.Ordinal);

        }

        private static string CreateIssueKey(ActivityContentExecutionParticipantDescriptor descriptor, int index)
        {
            return descriptor.ContentId.IsValid ? descriptor.ContentId.StableText : $"index:{index}";

        }

        private static void ValidatePhase(ActivityContentExecutionPhase phase)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution phase must be explicit.");
            }
        }
    }
}
