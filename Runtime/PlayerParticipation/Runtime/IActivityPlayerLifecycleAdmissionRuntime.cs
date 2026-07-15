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

        ActivityPlayerLifecycleAdmissionResult TryCommit(
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
