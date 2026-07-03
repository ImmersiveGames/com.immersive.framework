using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Source component family that produced a local contribution during discovery.
    /// This is diagnostic metadata, not a functional identity axis.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Diagnostic source kind introduced by F5D local contribution discovery.")]
    internal enum LocalContributionSourceKind
    {
        Unknown = 0,
        RouteContentBinding = 10,
        ActivityLocalVisibilityAdapter = 20
    }
}
