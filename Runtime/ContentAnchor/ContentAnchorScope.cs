using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Lifecycle/authored scope that owns a Content Anchor declaration.
    /// F7 intentionally does not introduce Session or global anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor scope primitive introduced by F7B.")]
    public enum ContentAnchorScope
    {
        Unknown = 0,
        Route = 10,
        Activity = 20,
        Local = 30
    }
}
