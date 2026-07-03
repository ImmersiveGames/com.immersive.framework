using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Local issue kind produced while building a passive Content Anchor Set.
    /// These issues are diagnostics only in F7E; they do not drive lifecycle failure or editor validation by themselves.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor Set issue kinds introduced by F7E.")]
    public enum ContentAnchorSetIssueKind
    {
        Unknown = 0,
        InvalidDeclaration = 1,
        DuplicateIdentity = 2,
        DuplicateAnchorId = 3
    }
}
