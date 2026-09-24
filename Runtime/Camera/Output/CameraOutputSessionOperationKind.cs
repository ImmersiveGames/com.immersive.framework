
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public enum CameraOutputSessionOperationKind
    {
        None = 0,
        Succeeded = 1,
        Rejected = 2,
        RolledBack = 3,
        RollbackFailed = 4
    }
}
