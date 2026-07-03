using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive phase plan for Activity Content Execution.
    /// It pairs ordered participants with generated execution requests for one phase only; it does not execute participants or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10F Activity Content Execution phase plan; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionPhasePlan : IEquatable<ActivityContentExecutionPhasePlan>
    {
        private readonly ActivityContentExecutionParticipantEntry[] _entries;
        private readonly ActivityContentExecutionRequest[] _requests;
        private readonly ActivityContentExecutionParticipantCollectionIssue[] _collectionIssues;

        private ActivityContentExecutionPhasePlan(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantEntry> entries,
            IReadOnlyList<ActivityContentExecutionRequest> requests,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            ActivityContentExecutionPhasePlanStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution phase plan phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhasePlanStatus), status)
                || status == ActivityContentExecutionPhasePlanStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution phase plan status must be explicit.");
            }

            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Context = context;
            Status = status;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();

            _entries = FrameworkCollectionCopy.ToArrayOrEmpty(entries);
            _requests = CopyRequests(requests, phase);
            _collectionIssues = CopyIssues(collectionIssues);
        }

        public ActivityContentExecutionPhase Phase { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        private RuntimeScopeContext Context { get; }

        private RuntimeContentOwner Owner => Context.Owner;

        public ActivityContentExecutionPhasePlanStatus Status { get; }

        public IReadOnlyList<ActivityContentExecutionParticipantEntry> Entries => FrameworkCollectionCopy.IsNullOrEmpty(_entries) ? Array.Empty<ActivityContentExecutionParticipantEntry>() : _entries;

        public IReadOnlyList<ActivityContentExecutionRequest> Requests => FrameworkCollectionCopy.IsNullOrEmpty(_requests) ? Array.Empty<ActivityContentExecutionRequest>() : _requests;

        private IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> CollectionIssues => FrameworkCollectionCopy.IsNullOrEmpty(_collectionIssues) ? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>() : _collectionIssues;

        private string Source { get; }

        private string Reason { get; }

        public string Message { get; }

        private bool HasEntries => EntryCount > 0;

        public bool HasRequests => RequestCount > 0;

        private bool HasCollectionIssues => CollectionIssueCount > 0;

        public bool Planned => Status == ActivityContentExecutionPhasePlanStatus.Planned;

        public bool Skipped => Status == ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants;

        public bool Rejected => Status is ActivityContentExecutionPhasePlanStatus.RejectedInvalidPhase or ActivityContentExecutionPhasePlanStatus.RejectedMissingActivity or ActivityContentExecutionPhasePlanStatus.RejectedInvalidContext or ActivityContentExecutionPhasePlanStatus.RejectedInvalidParticipants;

        private string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        private string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        private string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        private int EntryCount => _entries?.Length ?? 0;

        public int RequestCount => _requests?.Length ?? 0;

        private int CollectionIssueCount => _collectionIssues?.Length ?? 0;

        public int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        private int NullParticipantIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant);

        private int InvalidDescriptorIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor);

        private int DuplicateContentIdIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId);

        public ActivityContentExecutionParticipantEntry[] SnapshotEntries()
        {
            return FrameworkCollectionCopy.ToArrayOrEmpty(Entries);
        }

        public bool Equals(ActivityContentExecutionPhasePlan other)
        {
            if (Phase != other.Phase
                || !ReferenceEquals(Activity, other.Activity)
                || !ReferenceEquals(PreviousActivity, other.PreviousActivity)
                || !ReferenceEquals(NextActivity, other.NextActivity)
                || !Context.Equals(other.Context)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || EntryCount != other.EntryCount
                || RequestCount != other.RequestCount
                || CollectionIssueCount != other.CollectionIssueCount)
            {
                return false;
            }

            for (int i = 0; i < EntryCount; i++)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < RequestCount; i++)
            {
                if (!Requests[i].Equals(other.Requests[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < CollectionIssueCount; i++)
            {
                if (!CollectionIssues[i].Equals(other.CollectionIssues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionPhasePlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Phase;
                hashCode = hashCode * 397 ^ (Activity != null ? Activity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ Context.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                for (int i = 0; i < EntryCount; i++)
                {
                    hashCode = hashCode * 397 ^ Entries[i].GetHashCode();
                }

                for (int i = 0; i < RequestCount; i++)
                {
                    hashCode = hashCode * 397 ^ Requests[i].GetHashCode();
                }

                for (int i = 0; i < CollectionIssueCount; i++)
                {
                    hashCode = hashCode * 397 ^ CollectionIssues[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        private string ToDiagnosticString()
        {
            string activityText = ActivityName.ToDiagnosticText();
            string previousText = PreviousActivityName.ToDiagnosticText();
            string nextText = NextActivityName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"Activity Content Execution Phase Plan phase='{Phase}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' status='{Status}' requests='{RequestCount}' entries='{EntryCount}' required='{RequiredCount}' optional='{OptionalCount}' collectionIssues='{CollectionIssueCount}' nullParticipants='{NullParticipantIssueCount}' invalidDescriptors='{InvalidDescriptorIssueCount}' duplicateContentIds='{DuplicateContentIdIssueCount}' owner='{Owner.StableText}' source='{sourceText}' reason='{reasonText}' message='{messageText}' entries=[");
            for (int i = 0; i < Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Entries[i].ToDiagnosticString());
            }

            builder.Append("] requests=[");
            for (int i = 0; i < Requests.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Requests[i].ToDiagnosticString());
            }

            builder.Append("] collectionIssues=[");
            for (int i = 0; i < CollectionIssues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(CollectionIssues[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionPhasePlan PlannedResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantEntry> entries,
            IReadOnlyList<ActivityContentExecutionRequest> requests,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                entries,
                requests,
                collectionIssues,
                ActivityContentExecutionPhasePlanStatus.Planned,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan was created." : message);
        }

        public static ActivityContentExecutionPhasePlan SkippedNoParticipants(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                Array.Empty<ActivityContentExecutionParticipantEntry>(),
                Array.Empty<ActivityContentExecutionRequest>(),
                collectionIssues,
                ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan skipped because no participants support the phase." : message);
        }

        public static ActivityContentExecutionPhasePlan RejectedResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            ActivityContentExecutionPhasePlanStatus status,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            if (status is ActivityContentExecutionPhasePlanStatus.Planned or ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants or ActivityContentExecutionPhasePlanStatus.Unknown)
            {
                throw new ArgumentException("Rejected Activity content execution phase plan requires a rejected status.", nameof(status));
            }

            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                Array.Empty<ActivityContentExecutionParticipantEntry>(),
                Array.Empty<ActivityContentExecutionRequest>(),
                collectionIssues,
                status,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan was rejected." : message);
        }

        public static bool operator ==(ActivityContentExecutionPhasePlan left, ActivityContentExecutionPhasePlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionPhasePlan left, ActivityContentExecutionPhasePlan right)
        {
            return !left.Equals(right);
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (!HasRequests)
            {
                return 0;
            }

            IReadOnlyList<ActivityContentExecutionRequest> items = Requests;

            return items.Count(t => t.Requiredness == requiredness);
        }

        private int CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind kind)
        {
            if (!HasCollectionIssues)
            {
                return 0;
            }

            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> items = CollectionIssues;

            return items.Count(t => t.Kind == kind);
        }

        private static ActivityContentExecutionParticipantEntry[] CopyEntries(IReadOnlyList<ActivityContentExecutionParticipantEntry> source)
        {
            var copy = FrameworkCollectionCopy.ToArrayOrEmpty(source);
            for (int i = 0; i < copy.Length; i++)
            {
                if (!copy[i].IsValid)
                {
                    throw new ArgumentException("Activity content execution phase plan entries must be valid.", nameof(source));
                }
            }

            return copy;
        }

        private static ActivityContentExecutionRequest[] CopyRequests(IReadOnlyList<ActivityContentExecutionRequest> source, ActivityContentExecutionPhase phase)
        {
            var copy = FrameworkCollectionCopy.ToArrayOrEmpty(source);
            for (int i = 0; i < copy.Length; i++)
            {
                var request = copy[i];
                if (!request.IsValid)
                {
                    throw new ArgumentException("Activity content execution phase plan requests must be valid.", nameof(source));
                }

                if (request.Phase != phase)
                {
                    throw new ArgumentException("Activity content execution phase plan requests must match the plan phase.", nameof(source));
                }
                copy[i] = request;
            }

            return copy;
        }

        private static ActivityContentExecutionParticipantCollectionIssue[] CopyIssues(IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
            }

            var copy = new ActivityContentExecutionParticipantCollectionIssue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
