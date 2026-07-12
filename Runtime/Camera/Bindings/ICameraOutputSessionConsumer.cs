namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Internal injection boundary for route-scoped camera request sources.
    /// It intentionally has no static lookup or global registration path.
    /// </summary>
    public interface ICameraOutputSessionConsumer
    {
        void AttachOutputSession(CameraOutputSessionBinding binding);
        void DetachOutputSession(string reason);
    }

    public interface ISessionCameraOverrideConsumer
    {
        void AttachSessionCameraOverride(SessionCameraOverrideBinding binding);
    }
}
