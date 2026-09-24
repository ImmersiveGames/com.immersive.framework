
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public enum CameraOutputContextChangeKind
    {
        None = 0,
        WinnerEstablished = 1,
        WinnerChanged = 2,
        WinnerPreserved = 3,
        WinnerCleared = 4
    }
}
