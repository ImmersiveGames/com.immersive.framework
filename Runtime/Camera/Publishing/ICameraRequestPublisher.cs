namespace Immersive.Framework.Camera
{
    public interface ICameraRequestPublisher
    {
        CameraRequest Request { get; }
        bool IsPublished { get; }

        CameraRequestPublisherResult Publish();
        CameraRequestPublisherResult Release();
    }
}
