using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive declaration kind for a Content Anchor.
    /// Kind describes authoring intent only; it does not materialize or bind runtime content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor kind primitive introduced by F7B.")]
    public enum ContentAnchorKind
    {
        Unknown = 0,
        Root = 10,
        Slot = 20,
        Point = 30
    }
}
