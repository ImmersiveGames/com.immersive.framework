using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public abstract class ScopedCameraRequestPublisher :
        ICameraRequestPublisher
    {
        private readonly CameraOutputSession _session;
        private readonly CameraRequest _request;

        private bool _isPublished;

        protected ScopedCameraRequestPublisher(
            CameraOutputSession session,
            CameraRequest request)
        {
            this._session = session;
            this._request = request;
        }

        public CameraRequest Request => _request;
        public bool IsPublished => _isPublished;

        public CameraRequestPublisherResult Publish()
        {
            if (_isPublished)
            {
                return new CameraRequestPublisherResult(
                    CameraRequestPublisherOperationKind.Preserved,
                    _request,
                    false,
                    default,
                    Array.Empty<CameraIssue>(),
                    $"Camera request publisher preserved published request '{_request.RequestId}'.");
            }

            CameraOutputSessionResult sessionResult =
                _session.Admit(_request);

            if (!sessionResult.Succeeded)
            {
                return Rejected(
                    sessionResult,
                    $"Camera request publisher failed to publish request '{_request.RequestId}'.");
            }

            _isPublished = true;

            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Published,
                _request,
                true,
                sessionResult,
                sessionResult.Issues,
                sessionResult.Issues.Length == 0
                    ? $"Camera request publisher published request '{_request.RequestId}'."
                    : $"Camera request publisher published request '{_request.RequestId}'. {sessionResult.DiagnosticSummary}".NormalizeText());
        }

        public CameraRequestPublisherResult Release()
        {
            if (!_isPublished)
            {
                return new CameraRequestPublisherResult(
                    CameraRequestPublisherOperationKind.Preserved,
                    _request,
                    false,
                    default,
                    Array.Empty<CameraIssue>(),
                    $"Camera request publisher preserved released state for request '{_request.RequestId}'.");
            }

            CameraOutputSessionResult sessionResult =
                _session.Release(_request.RequestId);

            if (!sessionResult.Succeeded)
            {
                return Rejected(
                    sessionResult,
                    $"Camera request publisher failed to release request '{_request.RequestId}'.");
            }

            _isPublished = false;

            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Released,
                _request,
                true,
                sessionResult,
                sessionResult.Issues,
                sessionResult.Issues.Length == 0
                    ? $"Camera request publisher released request '{_request.RequestId}'."
                    : $"Camera request publisher released request '{_request.RequestId}'. {sessionResult.DiagnosticSummary}".NormalizeText());
        }

        private CameraRequestPublisherResult Rejected(
            CameraOutputSessionResult sessionResult,
            string summary)
        {
            return new CameraRequestPublisherResult(
                CameraRequestPublisherOperationKind.Rejected,
                _request,
                true,
                sessionResult,
                sessionResult.Issues,
                $"{summary} {sessionResult.DiagnosticSummary}".NormalizeText());
        }
    }
}
