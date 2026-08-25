
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Internal injection boundary for route-scoped camera request sources.
    /// It intentionally has no static lookup or global registration path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public interface ICameraOutputSessionConsumer
    {
        void AttachOutputSession(CameraOutputAuthoring binding);
        void DetachOutputSession(string reason);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public interface ISessionCameraOverrideConsumer
    {
        void AttachSessionCameraOverride(SessionCameraOverride binding);
    }
}
