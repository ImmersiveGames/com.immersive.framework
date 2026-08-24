using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-021 Model B Route-owned baseline Player spatial entry policy.")]
    public enum RoutePlayerSpatialEntryPolicy
    {
        PreserveCurrentPose = 0,
        ApplyExplicitPlacement = 1
    }
}
