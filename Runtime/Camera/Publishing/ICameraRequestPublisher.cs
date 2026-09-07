
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public interface ICameraRequestPublisher
    {
        CameraRequest Request { get; }
        bool IsPublished { get; }

        CameraRequestPublisherResult Publish();
        CameraRequestPublisherResult Release();
    }
}
