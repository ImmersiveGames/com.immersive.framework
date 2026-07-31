
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public interface ICameraRequestPublisher
    {
        CameraRequest Request { get; }
        bool IsPublished { get; }

        CameraRequestPublisherResult Publish();
        CameraRequestPublisherResult Release();
    }
}
