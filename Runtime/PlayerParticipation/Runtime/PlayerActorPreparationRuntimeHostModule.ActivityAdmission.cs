namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        internal void SetActivityPlayerGameplayLifecycleRuntime(
            IActivityPlayerGameplayLifecycleRuntime runtime)
        {
            activityLifecycleParticipant?
                .SetActivityPlayerGameplayLifecycleRuntime(
                    runtime);
        }
    }
}
