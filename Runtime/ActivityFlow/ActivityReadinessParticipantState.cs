using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "M03 authorable Activity readiness participant state.")]
    public enum ActivityReadinessParticipantState
    {
        Idle = 0,
        Preparing = 10,
        Completed = 20,
        Failed = 30,
        Released = 40
    }
}
