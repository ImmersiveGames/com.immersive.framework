using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "No-active-Activity visibility policy remains part of the Experimental Activity Visibility rule contract.")]
    public enum ActivityVisibilityNoActivePolicy
    {
        Hidden = 0,
        Visible = 1
    }
}
