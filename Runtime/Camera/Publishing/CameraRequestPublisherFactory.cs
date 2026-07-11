using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    internal static class CameraRequestPublisherFactory
    {
        public static bool TryValidate(
            CameraOutputSession session,
            CameraRequest request,
            CameraRequestOwnerKind expectedOwnerKind,
            CameraRequestLifetimeKind expectedLifetimeKind,
            string publisherName,
            out CameraRequestPublisherCreateResult blockedResult)
        {
            if (session == null)
            {
                blockedResult = Blocked(
                    "camera.request-publisher.session.missing",
                    $"{publisherName} requires a CameraOutputSession.");
                return false;
            }

            if (!request.IsValid)
            {
                blockedResult = Blocked(
                    "camera.request-publisher.request.invalid",
                    $"{publisherName} requires a valid CameraRequest.");
                return false;
            }

            if (request.OutputId != session.OutputId)
            {
                blockedResult = Blocked(
                    "camera.request-publisher.output-mismatch",
                    $"{publisherName} request output '{request.OutputId}' does not match session output '{session.OutputId}'.");
                return false;
            }

            if (request.Owner.Kind != expectedOwnerKind)
            {
                blockedResult = Blocked(
                    "camera.request-publisher.owner-kind.invalid",
                    $"{publisherName} requires owner kind '{expectedOwnerKind}', found '{request.Owner.Kind}'.");
                return false;
            }

            if (request.Lifetime.Kind != expectedLifetimeKind)
            {
                blockedResult = Blocked(
                    "camera.request-publisher.lifetime-kind.invalid",
                    $"{publisherName} requires lifetime kind '{expectedLifetimeKind}', found '{request.Lifetime.Kind}'.");
                return false;
            }

            blockedResult = default;
            return true;
        }

        public static CameraRequestPublisherCreateResult Succeeded(
            ICameraRequestPublisher publisher,
            string summary)
        {
            return new CameraRequestPublisherCreateResult(
                publisher,
                System.Array.Empty<CameraIssue>(),
                summary);
        }

        private static CameraRequestPublisherCreateResult Blocked(
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera request publisher creation was blocked.");

            return new CameraRequestPublisherCreateResult(
                null,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }
    }
}
