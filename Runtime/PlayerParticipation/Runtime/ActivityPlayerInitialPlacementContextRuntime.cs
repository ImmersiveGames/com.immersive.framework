using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Narrow bridge that publishes the target Activity occurrence for first physical
    /// materialization placement. It has no cross-Activity Player transaction.
    /// </summary>
    internal sealed class ActivityPlayerInitialPlacementContextRuntime :
        IActivityPlayerLifecycleAdmissionRuntime
    {
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;

        internal ActivityPlayerInitialPlacementContextRuntime(
            PlayerActorPreparationRuntimeHostModule preparationModule)
        {
            this.preparationModule = preparationModule;
        }

        public ActivityPlayerLifecycleAdmissionResult TryPrepareSameRouteSwitch(
            ActivityAsset previousActivity, ActivityAsset targetActivity,
            string source, string reason) => NotRequired(source, reason);

        public ActivityPlayerLifecycleAdmissionResult TryPrepareRouteStartupSwitch(
            RouteAsset previousRoute, RouteAsset targetRoute,
            ActivityAsset previousActivity, ActivityAsset targetActivity,
            string source, string reason) => NotRequired(source, reason);

        public ActivityPlayerLifecycleAdmissionResult TryAuthorizeTransition(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source, string reason) => NotRequired(source, reason);

        public bool TryConfigureInitialPlacementContext(
            ActivityTransitionPreparationContext context,
            string source,
            string reason,
            out string issue)
        {
            if (preparationModule == null)
            {
                issue = "Player Actor preparation runtime is unavailable for Activity initial placement.";
                return false;
            }

            return preparationModule.TryConfigureActivityInitialPlacementContext(
                context,
                out issue);
        }

        public ActivityPlayerLifecycleAdmissionResult TryCommit(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            ActivityTransitionPreparationContext context,
            string source, string reason) => NotRequired(source, reason);

        public ActivityPlayerLifecycleAdmissionResult TryRetryCommitCleanup(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source, string reason) => NotRequired(source, reason);

        public ActivityPlayerLifecycleAdmissionResult TryRollback(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source, string reason) => NotRequired(source, reason);

        public ActivityPlayerLifecycleAdmissionSnapshot CreateSnapshot() =>
            ActivityPlayerLifecycleAdmissionSnapshot.NotRequired(
                nameof(ActivityPlayerInitialPlacementContextRuntime),
                "initial-placement-context", "No cross-Activity Player transaction is composed.");

        private static ActivityPlayerLifecycleAdmissionResult NotRequired(
            string source, string reason) =>
            ActivityPlayerLifecycleAdmissionResult.NotRequiredResult(
                nameof(ActivityPlayerInitialPlacementContextRuntime), source, reason,
                "Activity transition uses Session physical reuse and contextual reprojection.");
    }
}
