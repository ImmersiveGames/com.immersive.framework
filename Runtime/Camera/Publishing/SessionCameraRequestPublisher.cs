namespace Immersive.Framework.Camera
{
    public sealed class SessionCameraRequestPublisher : ScopedCameraRequestPublisher
    {
        private SessionCameraRequestPublisher(CameraOutputSession session, CameraRequest request)
            : base(session, request)
        {
        }

        public static CameraRequestPublisherCreateResult Create(CameraOutputSession session, CameraRequest request)
        {
            if (!CameraRequestPublisherFactory.TryValidate(session, request,
                    CameraRequestOwnerKind.Session, CameraRequestLifetimeKind.Session,
                    nameof(SessionCameraRequestPublisher), out CameraRequestPublisherCreateResult blocked))
            {
                return blocked;
            }

            return CameraRequestPublisherFactory.Succeeded(
                new SessionCameraRequestPublisher(session, request),
                $"Session camera request publisher created for request '{request.RequestId}'.");
        }
    }
}
