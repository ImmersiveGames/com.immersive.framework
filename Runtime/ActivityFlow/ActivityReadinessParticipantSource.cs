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
        private readonly List<ActivityReadinessEvents> _observers = new List<ActivityReadinessEvents>();
        private ActivityAsset _activity;
        private int _revision;

        internal ActivityContentExecutionParticipantCollection Discover(
            ActivityContentDiscoveryScope scope,
            ActivityAsset activity)
        {
            ReleaseTracked("Reentry");
            _activity = activity;
            _participants.Clear();
            _observers.Clear();

            if (activity == null)
            {
                Publish();
                return ActivityContentExecutionParticipantCollection.Empty();
            }

            IReadOnlyList<ActivityReadinessParticipant> discovered =
                SceneScopedComponentQuery.GetComponentsInActivityContentScope<ActivityReadinessParticipant>(scope, activity);
            var technical = new List<IActivityContentExecutionParticipant>(discovered.Count);
            for (int i = 0; i < discovered.Count; i++)
            {
                ActivityReadinessParticipant participant = discovered[i];
                if (participant == null || !participant.IsValidForDiscovery(out _))
                {
                    continue;
                }

                participant.StateChanged += OnParticipantStateChanged;
                _participants.Add(participant);
                technical.Add(participant);
            }

            IReadOnlyList<ActivityReadinessEvents> observers =
                SceneScopedComponentQuery.GetComponentsInActivityContentScope<ActivityReadinessEvents>(scope, activity);
            for (int i = 0; i < observers.Count; i++)
            {
                if (observers[i] != null)
                {
                    _observers.Add(observers[i]);
                }
            }

            Publish();
            return ActivityContentExecutionParticipantCollection.FromParticipants(technical);
        }

        internal void ReleaseTracked(string reason)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                ActivityReadinessParticipant participant = _participants[i];
                if (participant == null)
                {
                    continue;
                }

                participant.StateChanged -= OnParticipantStateChanged;
                participant.Release(reason);
            }
        }

        private void OnParticipantStateChanged(ActivityReadinessParticipant participant)
        {
            Publish();
        }

        private void Publish()
        {
            int required = 0;
            int optional = 0;
            int pending = 0;
            int completed = 0;
            int failed = 0;
            string reason = _participants.Count == 0 ? "NoParticipants" : "Preparing";

            for (int i = 0; i < _participants.Count; i++)
            {
                ActivityReadinessParticipant participant = _participants[i];
                if (participant == null)
                {
                    continue;
                }

                if (participant.Requiredness == ActivityContentExecutionRequiredness.Required) required++;
                else optional++;

                switch (participant.State)
                {
                    case ActivityReadinessParticipantState.Preparing: pending++; break;
                    case ActivityReadinessParticipantState.Completed: completed++; break;
                    case ActivityReadinessParticipantState.Failed: failed++; reason = participant.LastReason; break;
                    case ActivityReadinessParticipantState.Idle: pending++; break;
                }
            }

            bool requiredPending = false;
            bool requiredFailed = false;
            for (int i = 0; i < _participants.Count; i++)
            {
                ActivityReadinessParticipant participant = _participants[i];
                if (participant == null || participant.Requiredness != ActivityContentExecutionRequiredness.Required) continue;
                requiredPending |= participant.State is ActivityReadinessParticipantState.Idle or ActivityReadinessParticipantState.Preparing;
                requiredFailed |= participant.State == ActivityReadinessParticipantState.Failed;
            }

            bool ready = !requiredPending && !requiredFailed;
            if (ready) reason = "Ready";
            else if (requiredFailed) reason = "RequiredParticipantFailed";
            var snapshot = new ActivityReadinessSnapshot(_activity, ready, reason, _participants.Count, required, optional, pending, completed, failed, ++_revision);
            for (int i = 0; i < _observers.Count; i++)
            {
                _observers[i]?.Apply(snapshot);
            }
        }
    }
}
