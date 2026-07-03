using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Declares whether Activity-owned content is released or kept when the active Activity changes.
    /// Activity-owned content is always released when the owning Route changes; Route-level persistence belongs to Session content, not Activity content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F25D1 Activity content release policy controls Activity changes only; Route changes always force Activity content release.")]
    public enum ActivityContentReleasePolicy
    {
        ReleaseOnActivityChange = 0,
        KeepOnActivityChange = 1
    }
}
