using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>Internal Unity adapter that discovers readiness participants only in explicit Activity content scenes.</summary>
    internal sealed class ActivityReadinessParticipantSource
    {
        private readonly List<ActivityReadinessParticipant> _participants = new List<ActivityReadinessParticipant>();
        private ActivityReadinessOccurrence _trackedOccurrence;
        private Action<ActivityReadinessOccurrence, ActivityReadinessParticipant> _changeSink;

        internal IReadOnlyList<ActivityReadinessParticipant>
            DiscoverAuthorableParticipants(
                ActivityContentDiscoveryScope scope,
                ActivityAsset activity)
        {
            if (activity == null)
            {
                return Array.Empty<ActivityReadinessParticipant>();
            }

            IReadOnlyList<ActivityReadinessParticipant> discovered =
                SceneScopedComponentQuery.GetComponentsInActivityContentScope<ActivityReadinessParticipant>(
                    scope,
                    activity);
            var participants = new List<ActivityReadinessParticipant>(discovered.Count);
            for (int i = 0; i < discovered.Count; i++)
            {
                ActivityReadinessParticipant participant = discovered[i];
                if (participant != null && participant.IsValidForDiscovery(out _))
                {
                    participants.Add(participant);
                }
            }

            return participants;
        }

        internal void StartTracking(
            ActivityAsset activity,
            ActivityReadinessOccurrence occurrence,
            IReadOnlyList<ActivityReadinessParticipant> participants,
            Action<ActivityReadinessOccurrence, ActivityReadinessParticipant> changeSink)
        {
            if (!occurrence.IsValid)
            {
                throw new ArgumentException(
                    "Activity readiness occurrence must be valid.",
                    nameof(occurrence));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (changeSink == null)
            {
                throw new ArgumentNullException(nameof(changeSink));
            }

            ReleaseTracked("Reentry");
            _trackedOccurrence = occurrence;
            _changeSink = changeSink;

            for (int i = 0; i < participants.Count; i++)
            {
                ActivityReadinessParticipant participant = participants[i];
                if (participant == null ||
                    !participant.IsValidForDiscovery(out _) ||
                    _participants.Contains(participant))
                {
                    continue;
                }

                participant.StateChanged += OnParticipantStateChanged;
                _participants.Add(participant);
            }

            for (int i = 0; i < _participants.Count; i++)
            {
                _participants[i].BeginPreparation(occurrence);
            }
        }

        internal void ReleaseTracked(string reason)
        {
            var releasedParticipants =
                new List<ActivityReadinessParticipant>(_participants);
            for (int i = 0; i < _participants.Count; i++)
            {
                ActivityReadinessParticipant participant = _participants[i];
                if (participant == null)
                {
                    continue;
                }

                participant.StateChanged -= OnParticipantStateChanged;
            }

            _changeSink = null;
            _trackedOccurrence = default;
            _participants.Clear();

            for (int i = 0; i < releasedParticipants.Count; i++)
            {
                ActivityReadinessParticipant participant = releasedParticipants[i];
                if (participant == null)
                {
                    continue;
                }

                participant.Release(reason);
            }

        }

        private void OnParticipantStateChanged(ActivityReadinessParticipant participant)
        {
            _changeSink?.Invoke(_trackedOccurrence, participant);
        }
    }
}
