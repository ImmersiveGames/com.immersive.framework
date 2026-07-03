using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Issue categories emitted by F5G local contribution validators.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Issue categories introduced by F5G local contribution validators.")]
    internal enum LocalContributionValidationIssueKind
    {
        Unknown = 0,
        DiscoveryIssue = 10,
        InvalidExpectedContribution = 20,
        MissingRequiredContribution = 30,
        OptionalContributionSkipped = 40
    }
}
