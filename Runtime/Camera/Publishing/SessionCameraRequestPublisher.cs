
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
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
