using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal enum ActivityReadinessOccurrenceLifecycle
    {
        Pending = 0,
        Current = 1,
        Invalidated = 2
    }

    internal readonly struct ActivityReadinessAuthorableContribution
    {
        internal ActivityReadinessAuthorableContribution(
            int participantCount,
            int requiredCount,
            int optionalCount,
            int pendingCount,
            int completedCount,
            int failedCount,
            int releasedCount,
            int requiredPendingCount,
            int requiredCompletedCount,
            int requiredFailedCount,
            int requiredReleasedCount,
            int optionalPendingCount,
            int optionalCompletedCount,
            int optionalFailedCount,
            int optionalReleasedCount,
            bool isSatisfied,
            string reason)
        {
            ValidateCounts(
                participantCount,
                requiredCount,
                optionalCount,
                pendingCount,
                completedCount,
                failedCount,
                releasedCount,
                requiredPendingCount,
                requiredCompletedCount,
                requiredFailedCount,
                requiredReleasedCount,
                optionalPendingCount,
                optionalCompletedCount,
                optionalFailedCount,
                optionalReleasedCount);

            ParticipantCount = participantCount;
            RequiredCount = requiredCount;
            OptionalCount = optionalCount;
            PendingCount = pendingCount;
            CompletedCount = completedCount;
            FailedCount = failedCount;
            ReleasedCount = releasedCount;
            RequiredPendingCount = requiredPendingCount;
            RequiredCompletedCount = requiredCompletedCount;
            RequiredFailedCount = requiredFailedCount;
            RequiredReleasedCount = requiredReleasedCount;
            OptionalPendingCount = optionalPendingCount;
            OptionalCompletedCount = optionalCompletedCount;
            OptionalFailedCount = optionalFailedCount;
            OptionalReleasedCount = optionalReleasedCount;
            IsSatisfied = isSatisfied;
            Reason = reason ?? string.Empty;
        }

        internal int ParticipantCount { get; }
        internal int RequiredCount { get; }
        internal int OptionalCount { get; }
        internal int PendingCount { get; }
        internal int CompletedCount { get; }
        internal int FailedCount { get; }
        internal int ReleasedCount { get; }
        internal int RequiredPendingCount { get; }
        internal int RequiredCompletedCount { get; }
        internal int RequiredFailedCount { get; }
        internal int RequiredReleasedCount { get; }
        internal int OptionalPendingCount { get; }
        internal int OptionalCompletedCount { get; }
        internal int OptionalFailedCount { get; }
        internal int OptionalReleasedCount { get; }
        internal bool HasRequiredReleased => RequiredReleasedCount > 0;
        internal bool HasTerminalFailure =>
            RequiredFailedCount > 0 ||
            RequiredReleasedCount > 0;
        internal int TerminalBlockingIssueCount =>
            RequiredFailedCount +
            RequiredReleasedCount;
        internal bool IsSatisfied { get; }
        internal string Reason { get; }

        private static void ValidateCounts(
            int participantCount,
            int requiredCount,
            int optionalCount,
            int pendingCount,
            int completedCount,
            int failedCount,
            int releasedCount,
            int requiredPendingCount,
            int requiredCompletedCount,
            int requiredFailedCount,
            int requiredReleasedCount,
            int optionalPendingCount,
            int optionalCompletedCount,
            int optionalFailedCount,
            int optionalReleasedCount)
        {
            ValidateNonNegative(participantCount, nameof(participantCount));
            ValidateNonNegative(requiredCount, nameof(requiredCount));
            ValidateNonNegative(optionalCount, nameof(optionalCount));
            ValidateNonNegative(pendingCount, nameof(pendingCount));
            ValidateNonNegative(completedCount, nameof(completedCount));
            ValidateNonNegative(failedCount, nameof(failedCount));
            ValidateNonNegative(releasedCount, nameof(releasedCount));
            ValidateNonNegative(requiredPendingCount, nameof(requiredPendingCount));
            ValidateNonNegative(requiredCompletedCount, nameof(requiredCompletedCount));
            ValidateNonNegative(requiredFailedCount, nameof(requiredFailedCount));
            ValidateNonNegative(requiredReleasedCount, nameof(requiredReleasedCount));
            ValidateNonNegative(optionalPendingCount, nameof(optionalPendingCount));
            ValidateNonNegative(optionalCompletedCount, nameof(optionalCompletedCount));
            ValidateNonNegative(optionalFailedCount, nameof(optionalFailedCount));
            ValidateNonNegative(optionalReleasedCount, nameof(optionalReleasedCount));

            if (participantCount != requiredCount + optionalCount ||
                pendingCount != requiredPendingCount + optionalPendingCount ||
                completedCount != requiredCompletedCount + optionalCompletedCount ||
                failedCount != requiredFailedCount + optionalFailedCount ||
                releasedCount != requiredReleasedCount + optionalReleasedCount ||
                requiredCount != requiredPendingCount + requiredCompletedCount +
                    requiredFailedCount + requiredReleasedCount ||
                optionalCount != optionalPendingCount + optionalCompletedCount +
                    optionalFailedCount + optionalReleasedCount)
            {
                throw new ArgumentException(
                    "Activity readiness contribution counts are inconsistent.");
            }
        }

        private static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Activity readiness contribution counts cannot be negative.");
            }
        }
    }

    internal sealed class ActivityReadinessOccurrenceState
    {
        private readonly ActivityReadinessOccurrence _occurrence;
        private readonly ActivityAsset _activity;
        private readonly ActivityReadinessState _technicalBaseline;
        private readonly List<ParticipantEntry> _participants;
        private ActivityReadinessOccurrenceLifecycle _lifecycle;
        private ActivityReadinessState _aggregateReadiness;
        private ActivityReadinessAuthorableContribution _authorableContribution;
        private int _revision;

        private ActivityReadinessOccurrenceState(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState technicalBaseline,
            List<ParticipantEntry> participants)
        {
            _occurrence = occurrence;
            _activity = occurrence.Activity;
            _technicalBaseline = technicalBaseline;
            _participants = participants;
            _lifecycle = ActivityReadinessOccurrenceLifecycle.Pending;
            _authorableContribution = CalculateAuthorableContribution(_participants);
            _aggregateReadiness = technicalBaseline;
            _revision = 1;
        }

        internal event Action<ActivityReadinessOccurrenceState> Changed;

        internal ActivityReadinessOccurrence Occurrence => _occurrence;
        internal ActivityAsset Activity => _activity;
        internal ActivityReadinessOccurrenceLifecycle Lifecycle => _lifecycle;
        internal ActivityReadinessState TechnicalBaseline => _technicalBaseline;
        internal ActivityReadinessState AggregateReadiness => _aggregateReadiness;
        internal ActivityReadinessAuthorableContribution AuthorableContribution =>
            _authorableContribution;
        internal ActivityReadinessProgressSnapshot ProgressSnapshot =>
            ActivityReadinessProgressSnapshot.Create(
                _occurrence,
                _aggregateReadiness);
        internal int Revision => _revision;
        internal bool IsPending =>
            _lifecycle == ActivityReadinessOccurrenceLifecycle.Pending;
        internal bool IsCurrent =>
            _lifecycle == ActivityReadinessOccurrenceLifecycle.Current;
        internal bool IsInvalidated =>
            _lifecycle == ActivityReadinessOccurrenceLifecycle.Invalidated;

        internal static ActivityReadinessOccurrenceState CreatePending(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState technicalBaseline,
            IReadOnlyList<ActivityReadinessParticipant> participants)
        {
            if (!occurrence.IsValid)
            {
                throw new ArgumentException(
                    "Activity readiness occurrence must be valid.",
                    nameof(occurrence));
            }

            if (technicalBaseline.HasActivity &&
                !ReferenceEquals(technicalBaseline.Activity, occurrence.Activity))
            {
                throw new ArgumentException(
                    "Technical readiness baseline belongs to another Activity.",
                    nameof(technicalBaseline));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var entries = new List<ParticipantEntry>(participants.Count);
            for (int i = 0; i < participants.Count; i++)
            {
                ActivityReadinessParticipant participant = participants[i];
                if (participant != null)
                {
                    entries.Add(new ParticipantEntry(participant));
                }
            }

            return new ActivityReadinessOccurrenceState(
                occurrence,
                technicalBaseline,
                entries);
        }

        internal bool TryRefreshParticipant(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessParticipant participant,
            out ActivityReadinessAuthorableContribution contribution)
        {
            contribution = _authorableContribution;
            if (IsInvalidated ||
                !_occurrence.Matches(
                    occurrence.Activity,
                    occurrence.TransitionSequence) ||
                participant == null)
            {
                return false;
            }

            ParticipantEntry entry = FindParticipant(participant);
            if (entry == null)
            {
                return false;
            }

            ActivityReadinessParticipantState state = participant.State;
            string reason = participant.LastReason;
            if (entry.State == state &&
                string.Equals(entry.LastReason, reason, StringComparison.Ordinal))
            {
                return false;
            }

            entry.Update(state, reason);
            _authorableContribution =
                CalculateAuthorableContribution(_participants);
            contribution = _authorableContribution;
            return true;
        }

        internal void MarkCurrent()
        {
            if (!IsPending)
            {
                return;
            }

            _lifecycle = ActivityReadinessOccurrenceLifecycle.Current;
            PublishChanged();
        }

        internal void Invalidate()
        {
            if (IsInvalidated)
            {
                return;
            }

            _lifecycle = ActivityReadinessOccurrenceLifecycle.Invalidated;
            for (int i = 0; i < _participants.Count; i++)
            {
                _participants[i].ClearParticipantReference();
            }

            PublishChanged();
        }

        internal bool TrySetAggregateReadiness(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState aggregate)
        {
            if (IsInvalidated ||
                !_occurrence.Matches(
                    occurrence.Activity,
                    occurrence.TransitionSequence) ||
                (aggregate.HasActivity &&
                 !ReferenceEquals(aggregate.Activity, _activity)) ||
                _aggregateReadiness.Equals(aggregate))
            {
                return false;
            }

            _aggregateReadiness = aggregate;
            PublishChanged();
            return true;
        }

        private void PublishChanged()
        {
            _revision++;
            Changed?.Invoke(this);
        }

        private ParticipantEntry FindParticipant(
            ActivityReadinessParticipant participant)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                ParticipantEntry entry = _participants[i];
                if (ReferenceEquals(entry.Participant, participant))
                {
                    return entry;
                }
            }

            return null;
        }

        private static ActivityReadinessAuthorableContribution
            CalculateAuthorableContribution(
                IReadOnlyList<ParticipantEntry> participants)
        {
            int participantCount = participants.Count;
            int requiredCount = 0;
            int optionalCount = 0;
            int pendingCount = 0;
            int completedCount = 0;
            int failedCount = 0;
            int releasedCount = 0;
            int requiredPendingCount = 0;
            int requiredCompletedCount = 0;
            int requiredFailedCount = 0;
            int requiredReleasedCount = 0;
            int optionalPendingCount = 0;
            int optionalCompletedCount = 0;
            int optionalFailedCount = 0;
            int optionalReleasedCount = 0;

            for (int i = 0; i < participants.Count; i++)
            {
                ParticipantEntry participant = participants[i];
                bool isRequired =
                    participant.Requiredness ==
                    ActivityContentExecutionRequiredness.Required;
                if (isRequired)
                {
                    requiredCount++;
                }
                else
                {
                    optionalCount++;
                }

                switch (participant.State)
                {
                    case ActivityReadinessParticipantState.Idle:
                    case ActivityReadinessParticipantState.Preparing:
                        pendingCount++;
                        if (isRequired)
                        {
                            requiredPendingCount++;
                        }
                        else
                        {
                            optionalPendingCount++;
                        }

                        break;
                    case ActivityReadinessParticipantState.Completed:
                        completedCount++;
                        if (isRequired)
                        {
                            requiredCompletedCount++;
                        }
                        else
                        {
                            optionalCompletedCount++;
                        }

                        break;
                    case ActivityReadinessParticipantState.Failed:
                        failedCount++;
                        if (isRequired)
                        {
                            requiredFailedCount++;
                        }
                        else
                        {
                            optionalFailedCount++;
                        }

                        break;
                    case ActivityReadinessParticipantState.Released:
                        releasedCount++;
                        if (isRequired)
                        {
                            requiredReleasedCount++;
                        }
                        else
                        {
                            optionalReleasedCount++;
                        }

                        break;
                }
            }

            if (participantCount == 0)
            {
                return new ActivityReadinessAuthorableContribution(
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    true, "NoParticipants");
            }

            bool isSatisfied = requiredFailedCount == 0 &&
                requiredReleasedCount == 0 &&
                requiredPendingCount == 0;
            string reason = requiredFailedCount > 0
                ? "RequiredParticipantFailed"
                : requiredReleasedCount > 0
                    ? "RequiredParticipantReleased"
                    : requiredPendingCount > 0
                        ? "Preparing"
                        : "Ready";
            return new ActivityReadinessAuthorableContribution(
                participantCount,
                requiredCount,
                optionalCount,
                pendingCount,
                completedCount,
                failedCount,
                releasedCount,
                requiredPendingCount,
                requiredCompletedCount,
                requiredFailedCount,
                requiredReleasedCount,
                optionalPendingCount,
                optionalCompletedCount,
                optionalFailedCount,
                optionalReleasedCount,
                isSatisfied,
                reason);
        }

        private sealed class ParticipantEntry
        {
            internal ParticipantEntry(ActivityReadinessParticipant participant)
            {
                Participant = participant;
                ParticipantId = participant.ParticipantId;
                Requiredness = participant.Requiredness;
                State = participant.State;
                LastReason = participant.LastReason;
            }

            internal ActivityReadinessParticipant Participant { get; private set; }
            internal string ParticipantId { get; }
            internal ActivityContentExecutionRequiredness Requiredness { get; }
            internal ActivityReadinessParticipantState State { get; private set; }
            internal string LastReason { get; private set; }

            internal void Update(
                ActivityReadinessParticipantState state,
                string lastReason)
            {
                State = state;
                LastReason = lastReason;
            }

            internal void ClearParticipantReference()
            {
                Participant = null;
            }
        }
    }
}
