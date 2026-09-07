
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraOverrideOperationKind
    {
        Blocked = 0,
        Requested = 1,
        Released = 2,
        Preserved = 3,
        CleanedUp = 4
    }
}
