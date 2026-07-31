
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraRequestPublisherOperationKind
    {
        None = 0,
        Published = 1,
        Released = 2,
        Preserved = 3,
        Rejected = 4
    }
}
