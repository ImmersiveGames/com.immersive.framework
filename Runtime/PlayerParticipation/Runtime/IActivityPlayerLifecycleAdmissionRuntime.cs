using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
    internal interface IActivityPlayerLifecycleAdmissionRuntime
    {
        ActivityPlayerLifecycleAdmissionResult TryPrepareSameRouteSwitch(
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason);

        ActivityPlayerLifecycleAdmissionResult TryPrepareRouteStartupSwitch(
            RouteAsset previousRoute,
            RouteAsset targetRoute,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason);

        ActivityPlayerLifecycleAdmissionResult TryAuthorizeTransition(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason);

        bool TryConfigureInitialPlacementContext(
            ActivityTransitionPreparationContext context,
            string source,
            string reason,
            out string issue);

        ActivityPlayerLifecycleAdmissionResult TryCommit(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            ActivityTransitionPreparationContext context,
            string source,
            string reason);

        ActivityPlayerLifecycleAdmissionResult TryRetryCommitCleanup(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason);

        ActivityPlayerLifecycleAdmissionResult TryRollback(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason);

        ActivityPlayerLifecycleAdmissionSnapshot CreateSnapshot();
    }
}
