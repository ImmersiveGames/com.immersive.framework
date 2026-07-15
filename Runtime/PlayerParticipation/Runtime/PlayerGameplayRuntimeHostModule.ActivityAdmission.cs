using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerGameplayRuntimeHostModule
    {
        private ActivityPlayerLifecycleAdmissionRuntimeContext
            activityLifecycleAdmissionContext;

        internal bool TryGetActivityPlayerLifecycleAdmissionSnapshot(
            out ActivityPlayerLifecycleAdmissionSnapshot snapshot)
        {
            snapshot = activityLifecycleAdmissionContext?.CreateSnapshot();
            return snapshot != null;
        }

        private bool TryInitializeActivityLifecycleAdmission(
            out string issue)
        {
            issue = string.Empty;
            if (!ActivityPlayerLifecycleAdmissionRuntimeContext.TryCreate(
                    runtimeHost.RuntimeContentRuntime,
                    participationContext,
                    preparationModule,
                    candidateModule,
                    admissionContext,
                    handoffContext,
                    groupContext,
                    out ActivityPlayerLifecycleAdmissionRuntimeContext context,
                    out issue))
            {
                return false;
            }

            activityLifecycleAdmissionContext = context;
            preparationModule.SetActivityPlayerGameplayLifecycleRuntime(
                context);
            runtimeHost.SetActivityPlayerLifecycleAdmissionRuntime(
                context);
            return true;
        }

        private void ReleaseActivityLifecycleAdmission()
        {
            if (runtimeHost != null)
            {
                runtimeHost.SetActivityPlayerLifecycleAdmissionRuntime(null);
            }

            if (preparationModule != null)
            {
                preparationModule.SetActivityPlayerGameplayLifecycleRuntime(
                    null);
            }

            activityLifecycleAdmissionContext = null;
        }
    }
}
