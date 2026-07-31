
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public enum CameraOutputContextOperationKind
    {
        None = 0,
        Admitted = 1,
        Released = 2,
        Blocked = 3,
        NotFound = 4
    }
}
