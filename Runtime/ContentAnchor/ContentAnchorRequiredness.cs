using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Authoring validation requirement for a Content Anchor declaration.
    /// Optional is the default value to avoid blocking lifecycle accidentally before validators exist.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor requiredness primitive introduced by F7B.")]
    public enum ContentAnchorRequiredness
    {
        Optional = 0,
        Required = 10
    }
}
