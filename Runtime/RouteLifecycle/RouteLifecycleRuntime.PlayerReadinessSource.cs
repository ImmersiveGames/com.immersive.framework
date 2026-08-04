using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal void SetActivityReadinessParticipantSource(
            IActivityReadinessParticipantSource source)
        {
            _activityFlowRuntime.SetActivityReadinessParticipantSource(source);
        }
    }
}
