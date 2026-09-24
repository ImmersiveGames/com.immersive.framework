namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        internal void SetActivityReadinessParticipantSource(
            IActivityReadinessParticipantSource source)
        {
            _activityReadinessParticipantSource.SetExplicitSource(source);
        }
    }
}
