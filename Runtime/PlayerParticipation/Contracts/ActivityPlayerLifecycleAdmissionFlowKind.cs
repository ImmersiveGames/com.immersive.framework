using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Identifies which lifecycle flow owns an Activity Player admission transaction.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Activity Player lifecycle admission flow identity.")]
    public enum ActivityPlayerLifecycleAdmissionFlowKind
    {
        None = 0,
        SameRouteActivitySwitch = 10,
        RouteStartupActivitySwitch = 20
    }
}
