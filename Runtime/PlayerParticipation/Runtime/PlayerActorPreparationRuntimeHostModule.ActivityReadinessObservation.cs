namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        internal bool TryGetActivityPlayerReadinessContributionSnapshot(
            out ActivityPlayerReadinessContributionRuntimeSnapshot snapshot)
        {
            if (!IsReady || activityLifecycleParticipant == null)
            {
                snapshot =
                    ActivityPlayerReadinessContributionRuntimeSnapshot
                        .Unavailable(
                            "FrameworkRuntimeHost has no ready Player Activity lifecycle participant.");
                return false;
            }

            return activityLifecycleParticipant
                .TryGetReadinessContributionSnapshot(out snapshot);
        }
    }
}
