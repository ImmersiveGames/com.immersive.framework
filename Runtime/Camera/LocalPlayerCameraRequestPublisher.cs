namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed publisher for one camera request owned by an eligible local Player.
    /// It does not discover Players, select winners or mutate Cinemachine directly.
    /// </summary>
    public sealed class LocalPlayerCameraRequestPublisher :
        ScopedCameraRequestPublisher
    {
        private LocalPlayerCameraRequestPublisher(
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
                    CameraRequestOwnerKind.LocalPlayer,
                    CameraRequestLifetimeKind.LocalPlayerEligibility,
                    nameof(LocalPlayerCameraRequestPublisher),
                    out CameraRequestPublisherCreateResult blockedResult))
            {
                return blockedResult;
            }

            var publisher =
                new LocalPlayerCameraRequestPublisher(session, request);

            return CameraRequestPublisherFactory.Succeeded(
                publisher,
                $"Local Player camera request publisher created for request '{request.RequestId}'.");
        }
    }
}
