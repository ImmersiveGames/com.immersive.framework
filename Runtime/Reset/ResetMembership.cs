using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>API status: Experimental. Semantic Reset target membership, independent from content lifetime ownership.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-C Resettable membership policy.")]
    public enum ResetMembership
    {
        FollowOwner = 0,
        Activity = 1,
        Route = 2
    }
}
