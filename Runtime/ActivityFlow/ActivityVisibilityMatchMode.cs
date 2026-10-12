using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity visibility matching remains part of the Experimental Activity Visibility rule contract.")]
    public enum ActivityVisibilityMatchMode
    {
        VisibleWhenAnyListedActivityIsActive = 0,
        HiddenWhenAnyListedActivityIsActive = 1
    }
}
