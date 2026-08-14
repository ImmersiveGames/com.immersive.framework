using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        internal bool TryCreatePendingActivityTransitionPreparationContext(
            out ActivityTransitionPreparationContext context)
        {
            context = default;
            ActivityTransitionRuntimeTransaction transaction =
                _activeActivityTransition;
            if (transaction == null ||
                transaction.IsTerminal ||
                transaction.CommitReached ||
                transaction.TargetActivity == null)
            {
                return false;
            }

            ActivityAsset activity = transaction.TargetActivity;
            ActivityContentDiscoveryScope discoveryScope =
                _activitySceneCompositionRuntime
                    .CreateActivityContentDiscoveryScope(activity);
            context = new ActivityTransitionPreparationContext(
                activity,
                CreateActivityOwner(activity),
                new ActivityReadinessOccurrence(
                    activity,
                    transaction.Sequence),
                discoveryScope);
            return context.IsValid;
        }
    }
}
