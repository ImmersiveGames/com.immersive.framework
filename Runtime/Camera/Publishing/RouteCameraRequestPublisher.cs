
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
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
