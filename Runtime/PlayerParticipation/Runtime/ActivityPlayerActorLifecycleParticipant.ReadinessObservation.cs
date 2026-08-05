using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        internal bool TryGetReadinessContributionSnapshot(
            out ActivityPlayerReadinessContributionRuntimeSnapshot snapshot)
        {
            ActivityReadinessParticipant participant =
                playerReadinessParticipant;
            if (participant == null)
            {
                snapshot =
                    ActivityPlayerReadinessContributionRuntimeSnapshot
                        .Unavailable(
                            "The official Player readiness contribution has not been materialized for an Activity occurrence.");
                return false;
            }

            ActivityPlayerActorLifecycleSnapshot lifecycle = Snapshot;
            string activityName =
                lifecycle != null
                    ? lifecycle.ActivityName
                    : string.Empty;
            string requirementLevel =
                lifecycle != null
                    ? lifecycle.RequirementLevel.ToString()
                    : string.Empty;

            snapshot =
                new ActivityPlayerReadinessContributionRuntimeSnapshot(
                    true,
                    activityName,
                    participant.Occurrence,
                    requirementLevel,
                    participant.State,
                    participant.LastReason,
                    "Observed directly from the official ActivityReadinessParticipant owned by the Player Activity lifecycle.");
            return true;
        }
    }
}
