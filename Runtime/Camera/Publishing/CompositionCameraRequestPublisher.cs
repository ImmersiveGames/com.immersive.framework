using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed publisher for one Composition-owned Camera request occurrence.
    /// Publication lifetime is independent from Session, Route and Activity scopes.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-C Composition request participation.")]
    public sealed class CompositionCameraRequestPublisher : ScopedCameraRequestPublisher
    {
        private CompositionCameraRequestPublisher(
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
                    CameraRequestOwnerKind.Composition,
                    CameraRequestLifetimeKind.Composition,
                    nameof(CompositionCameraRequestPublisher),
                    out CameraRequestPublisherCreateResult blocked))
            {
                return blocked;
            }

            return CameraRequestPublisherFactory.Succeeded(
                new CompositionCameraRequestPublisher(session, request),
                $"Composition camera request publisher created for request '{request.RequestId}'.");
        }
    }
}
