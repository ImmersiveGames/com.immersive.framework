using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    public abstract class ScopedCameraRequestPublisher :
        ICameraRequestPublisher
    {
        private readonly CameraOutputSession session;
        private readonly CameraRequest request;

        private bool isPublished;

        protected ScopedCameraRequestPublisher(
            CameraOutputSession session,
            CameraRequest request)
        {
            this.session = session;
            this.request = request;
        }

        public CameraRequest Request => request;
        public bool IsPublished => isPublished;

        public CameraRequestPublisherResult Publish()
        {
            if (isPublished)
            {
                return new CameraRequestPublisherResult(
                    CameraRequestPublisherOperationKind.Preserved,
                    request,
                    false,
                    default,
                    Array.Empty<CameraIssue>(),
                    $"Camera request publisher preserved published request '{request.RequestId}'.");
            }

            CameraOutputSessionResult sessionResult =
                session.Admit(request);

            if (!sessionResult.Succeeded)
            {
                return Rejected(
                    sessionResult,
                    $"Camera request publisher failed to publish request '{request.RequestId}'.");
            }

            isPublished = true;

            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Published,
                request,
                true,
                sessionResult,
                sessionResult.Issues,
                sessionResult.Issues.Length == 0
                    ? $"Camera request publisher published request '{request.RequestId}'."
                    : $"Camera request publisher published request '{request.RequestId}'. {sessionResult.DiagnosticSummary}".NormalizeText());
        }

        public CameraRequestPublisherResult Release()
        {
            if (!isPublished)
            {
                return new CameraRequestPublisherResult(
                    CameraRequestPublisherOperationKind.Preserved,
                    request,
                    false,
                    default,
                    Array.Empty<CameraIssue>(),
                    $"Camera request publisher preserved released state for request '{request.RequestId}'.");
            }

            CameraOutputSessionResult sessionResult =
                session.Release(request.RequestId);

            if (!sessionResult.Succeeded)
            {
                return Rejected(
                    sessionResult,
                    $"Camera request publisher failed to release request '{request.RequestId}'.");
            }

            isPublished = false;

            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Released,
                request,
                true,
                sessionResult,
                sessionResult.Issues,
                sessionResult.Issues.Length == 0
                    ? $"Camera request publisher released request '{request.RequestId}'."
                    : $"Camera request publisher released request '{request.RequestId}'. {sessionResult.DiagnosticSummary}".NormalizeText());
        }

        private CameraRequestPublisherResult Rejected(
            CameraOutputSessionResult sessionResult,
            string summary)
        {
            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Rejected,
                request,
                true,
                sessionResult,
                sessionResult.Issues,
                $"{summary} {sessionResult.DiagnosticSummary}".NormalizeText());
        }
    }
}
