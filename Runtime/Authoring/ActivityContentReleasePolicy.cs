using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Declares whether Activity-owned content is released or kept when the active Activity changes.
    /// Activity-owned content is always released when the owning Route changes; Route-level persistence belongs to Session content, not Activity content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public enum ActivityContentReleasePolicy
    {
        ReleaseOnActivityChange = 0,
        KeepOnActivityChange = 1
    }
}
