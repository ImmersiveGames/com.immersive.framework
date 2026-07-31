
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares why a request must be released.
    /// This contract does not perform the release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraRequestReleaseCondition
    {
        Undefined = 0,
        ExplicitRelease = 1,
        OwnerLifetimeEnded = 2,
        ScopeExited = 3,
        EligibilityLost = 4,
        SessionEnded = 5
    }
}
