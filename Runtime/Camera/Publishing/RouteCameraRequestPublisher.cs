namespace Immersive.Framework.Camera
{
    public sealed class RouteCameraRequestPublisher :
        ScopedCameraRequestPublisher
    {
        private RouteCameraRequestPublisher(
            CameraOutputSession session,
            CameraRequest request)
            : base(session, request)
        {
        }

        public static CameraRequestPublisherCreateResult Create(
            CameraOutputSession session,
            CameraRequest request)
        {
            if (!CameraRequestPublisherFactory.TryValidate(
                    session,
                    request,
                    CameraRequestOwnerKind.Route,
                    CameraRequestLifetimeKind.Route,
                    nameof(RouteCameraRequestPublisher),
                    out CameraRequestPublisherCreateResult blockedResult))
            {
                return blockedResult;
            }

            var publisher =
                new RouteCameraRequestPublisher(session, request);

            return CameraRequestPublisherFactory.Succeeded(
                publisher,
                $"Route camera request publisher created for request '{request.RequestId}'.");
        }
    }
}
