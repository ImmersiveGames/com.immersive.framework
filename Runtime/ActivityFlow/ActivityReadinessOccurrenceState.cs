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
            int requiredPendingCount,
            int requiredFailedCount,
            bool isSatisfied,
            string reason)
        {
            ParticipantCount = participantCount;
            RequiredCount = requiredCount;
            OptionalCount = optionalCount;
            PendingCount = pendingCount;
            CompletedCount = completedCount;
            FailedCount = failedCount;
            RequiredPendingCount = requiredPendingCount;
            RequiredFailedCount = requiredFailedCount;
            IsSatisfied = isSatisfied;
            Reason = reason ?? string.Empty;
        }

        internal int ParticipantCount { get; }
        internal int RequiredCount { get; }
        internal int OptionalCount { get; }
        internal int PendingCount { get; }
        internal int CompletedCount { get; }
        internal int FailedCount { get; }
        internal int RequiredPendingCount { get; }
        internal int RequiredFailedCount { get; }
        internal bool IsSatisfied { get; }
        internal string Reason { get; }
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
        }

        internal ActivityReadinessOccurrence Occurrence => _occurrence;
        internal ActivityAsset Activity => _activity;
        internal ActivityReadinessOccurrenceLifecycle Lifecycle => _lifecycle;
        internal ActivityReadinessState TechnicalBaseline => _technicalBaseline;
        internal ActivityReadinessState AggregateReadiness => _aggregateReadiness;
        internal ActivityReadinessAuthorableContribution AuthorableContribution =>
            _authorableContribution;
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
            if (IsPending)
            {
                _lifecycle = ActivityReadinessOccurrenceLifecycle.Current;
            }
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
            return true;
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
            int requiredPendingCount = 0;
            int requiredFailedCount = 0;
            bool hasRequiredReleased = false;

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

                        break;
                    case ActivityReadinessParticipantState.Completed:
                        completedCount++;
                        break;
                    case ActivityReadinessParticipantState.Failed:
                        failedCount++;
                        if (isRequired)
                        {
                            requiredFailedCount++;
                        }

                        break;
                    case ActivityReadinessParticipantState.Released:
                        hasRequiredReleased |= isRequired;
                        break;
                }
            }

            if (participantCount == 0)
            {
                return new ActivityReadinessAuthorableContribution(
                    0, 0, 0, 0, 0, 0, 0, 0, true, "NoParticipants");
            }

            bool isSatisfied = requiredFailedCount == 0 &&
                !hasRequiredReleased &&
                requiredPendingCount == 0;
            string reason = requiredFailedCount > 0
                ? "RequiredParticipantFailed"
                : hasRequiredReleased
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
                requiredPendingCount,
                requiredFailedCount,
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
