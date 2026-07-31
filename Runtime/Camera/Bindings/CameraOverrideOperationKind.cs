
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraOverrideOperationKind
    {
        Blocked = 0,
        Requested = 1,
        Released = 2,
        Preserved = 3,
        CleanedUp = 4
    }
}
