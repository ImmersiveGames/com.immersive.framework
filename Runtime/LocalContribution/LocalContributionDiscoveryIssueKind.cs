using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Structured issue kinds emitted by local contribution discovery.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Issue categories introduced by F5D local contribution discovery.")]
    internal enum LocalContributionDiscoveryIssueKind
    {
        Unknown = 0,
        MissingOwner = 10,
        MissingLocalContentId = 20,
        InvalidLocalContentIdentity = 30,
        DuplicateLocalContentIdentity = 40
    }
}
