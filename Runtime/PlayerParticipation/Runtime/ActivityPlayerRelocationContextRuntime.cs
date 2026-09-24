using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed class ActivityPlayerRelocationContextRuntime : IActivityPlayerLifecycleAdmissionRuntime
    {
        private readonly PlayerActorPreparationRuntimeHostModule _preparationModule;
        internal ActivityPlayerRelocationContextRuntime(PlayerActorPreparationRuntimeHostModule module) => _preparationModule = module;
        public ActivityPlayerLifecycleAdmissionResult TryPrepareSameRouteSwitch(ActivityAsset previousActivity, ActivityAsset targetActivity, string source, string reason) => NotRequired(source, reason);
        public ActivityPlayerLifecycleAdmissionResult TryPrepareRouteStartupSwitch(RouteAsset previousRoute, RouteAsset targetRoute, ActivityAsset previousActivity, ActivityAsset targetActivity, string source, string reason) => NotRequired(source, reason);
        public ActivityPlayerLifecycleAdmissionResult TryAuthorizeTransition(ActivityPlayerLifecycleAdmissionToken expectedTransaction, string source, string reason) => NotRequired(source, reason);
        public bool TryConfigureRelocationContext(ActivityTransitionPreparationContext context, string source, string reason, out string issue)
        {
            if (_preparationModule == null) { issue = "Player Actor preparation runtime is unavailable for Activity relocation."; return false; }
            return _preparationModule.TryConfigureActivityRelocationContext(context, out issue);
        }
        public ActivityPlayerLifecycleAdmissionResult TryCommit(ActivityPlayerLifecycleAdmissionToken expectedTransaction, ActivityTransitionPreparationContext context, string source, string reason) => NotRequired(source, reason);
        public ActivityPlayerLifecycleAdmissionResult TryRetryCommitCleanup(ActivityPlayerLifecycleAdmissionToken expectedTransaction, string source, string reason) => NotRequired(source, reason);
        public ActivityPlayerLifecycleAdmissionResult TryRollback(ActivityPlayerLifecycleAdmissionToken expectedTransaction, string source, string reason) => NotRequired(source, reason);
        public ActivityPlayerLifecycleAdmissionSnapshot CreateSnapshot() => ActivityPlayerLifecycleAdmissionSnapshot.NotRequired(nameof(ActivityPlayerRelocationContextRuntime), "activity-relocation-context", "No cross-Activity Player transaction is composed.");
        private static ActivityPlayerLifecycleAdmissionResult NotRequired(string source, string reason) => ActivityPlayerLifecycleAdmissionResult.NotRequiredResult(nameof(ActivityPlayerRelocationContextRuntime), source, reason, "Activity transition reuses Session physical Player and may apply explicit contextual relocation.");
    }
}
