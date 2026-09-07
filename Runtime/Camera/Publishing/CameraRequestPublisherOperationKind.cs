
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraRequestPublisherOperationKind
    {
        None = 0,
        Published = 1,
        Released = 2,
        Preserved = 3,
        Rejected = 4
    }
}
