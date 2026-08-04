using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        internal void SetActivityReadinessParticipantSource(
            IActivityReadinessParticipantSource source)
        {
            _routeLifecycleRuntime.SetActivityReadinessParticipantSource(source);
        }
    }
}
