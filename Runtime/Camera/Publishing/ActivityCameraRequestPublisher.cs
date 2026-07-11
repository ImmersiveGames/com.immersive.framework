namespace Immersive.Framework.Camera
{
    public sealed class ActivityCameraRequestPublisher :
        ScopedCameraRequestPublisher
    {
        private ActivityCameraRequestPublisher(
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
                    CameraRequestOwnerKind.Activity,
                    CameraRequestLifetimeKind.Activity,
                    nameof(ActivityCameraRequestPublisher),
                    out CameraRequestPublisherCreateResult blockedResult))
            {
                return blockedResult;
            }

            var publisher =
                new ActivityCameraRequestPublisher(session, request);

            return CameraRequestPublisherFactory.Succeeded(
                publisher,
                $"Activity camera request publisher created for request '{request.RequestId}'.");
        }
    }
}
