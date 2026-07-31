
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public enum CameraOutputApplyKind
    {
        None = 0,
        Applied = 1,
        Cleared = 2,
        Preserved = 3,
        Blocked = 4
    }
}
