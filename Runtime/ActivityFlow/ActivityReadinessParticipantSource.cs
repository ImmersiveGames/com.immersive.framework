using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Internal Unity adapter that discovers readiness participants only in explicit
    /// Activity content scenes and merges explicitly supplied host-scoped participants.
    /// </summary>
    internal sealed class ActivityReadinessParticipantSource
    {
        private readonly List<ActivityReadinessParticipant> _participants =
            new List<ActivityReadinessParticipant>();
        private IActivityReadinessParticipantSource _explicitSource =
            EmptyActivityReadinessParticipantSource.Instance;
        private ActivityReadinessOccurrence _trackedOccurrence;
        private Action<
            ActivityReadinessOccurrence,
            ActivityReadinessParticipant> _changeSink;

        internal void SetExplicitSource(
            IActivityReadinessParticipantSource source)
        {
            _explicitSource = source ??
                EmptyActivityReadinessParticipantSource.Instance;
        }

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
                SceneScopedComponentQuery
                    .GetComponentsInActivityContentScope<
                        ActivityReadinessParticipant>(
                        scope,
                        activity);
            IReadOnlyList<ActivityReadinessParticipant> explicitParticipants =
                _explicitSource.ResolveActivityReadinessParticipants(activity) ??
                Array.Empty<ActivityReadinessParticipant>();

            var participants =
                new List<ActivityReadinessParticipant>(
                    discovered.Count + explicitParticipants.Count);
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < discovered.Count; i++)
            {
                AddValidatedParticipant(
                    participants,
                    ids,
                    discovered[i],
                    "Activity content discovery");
            }

            for (int i = 0; i < explicitParticipants.Count; i++)
            {
                AddValidatedParticipant(
                    participants,
                    ids,
                    explicitParticipants[i],
                    "host-scoped readiness source");
            }

            return participants;
        }

        internal void StartTracking(
            ActivityAsset activity,
            ActivityReadinessOccurrence occurrence,
            IReadOnlyList<ActivityReadinessParticipant> participants,
            Action<
                ActivityReadinessOccurrence,
                ActivityReadinessParticipant> changeSink)
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
                ActivityReadinessParticipant participant =
                    releasedParticipants[i];
                if (participant == null)
                {
                    continue;
                }

                participant.Release(reason);
            }
        }

        private static void AddValidatedParticipant(
            List<ActivityReadinessParticipant> target,
            HashSet<string> ids,
            ActivityReadinessParticipant participant,
            string source)
        {
            if (participant == null)
            {
                return;
            }

            if (!participant.IsValidForDiscovery(out string issue))
            {
                throw new InvalidOperationException(
                    $"{source} returned an invalid Activity readiness participant. {issue}");
            }

            if (target.Contains(participant))
            {
                return;
            }

            string participantId = participant.ParticipantId.Trim();
            if (!ids.Add(participantId))
            {
                throw new InvalidOperationException(
                    $"Activity readiness participant ID '{participantId}' " +
                    "is duplicated between discovered and explicitly " +
                    "supplied contributions.");
            }

            target.Add(participant);
        }

        private void OnParticipantStateChanged(
            ActivityReadinessParticipant participant)
        {
            _changeSink?.Invoke(_trackedOccurrence, participant);
        }
    }
}
