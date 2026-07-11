using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit result for constructing a valid camera request.
    /// Invalid mandatory data blocks creation without fallback.
    /// </summary>
    public readonly struct CameraRequestCreateResult
    {
        private CameraRequestCreateResult(
            CameraOperationStatus status,
            CameraRequest request,
            CameraIssue[] issues,
            string blockingIssue,
            string diagnosticSummary)
        {
            Status = status;
            Request = request;
            Issues = issues ?? Array.Empty<CameraIssue>();
            BlockingIssue = blockingIssue.NormalizeText();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraOperationStatus Status { get; }

        public CameraRequest Request { get; }

        public CameraIssue[] Issues { get; }

        public string BlockingIssue { get; }

        public string DiagnosticSummary { get; }

        public bool IsSucceeded => Status == CameraOperationStatus.Succeeded;

        public bool IsBlocked => Status == CameraOperationStatus.Blocked;

        public static CameraRequestCreateResult Create(
            CameraRequestId requestId,
            CameraOutputId outputId,
            CameraRequestOwner owner,
            CameraRequestLifetime lifetime,
            CameraRigReference rig,
            CameraTargetSourceDescriptor targetSource,
            CameraRequestPolicy policy,
            CameraRequestReleaseCondition releaseCondition,
            string diagnosticSource,
            string diagnosticReason)
        {
            if (!requestId.IsValid)
                return Blocked("camera.request.id.missing", "Camera request id is required.");

            if (!outputId.IsValid)
                return Blocked("camera.request.output.missing", "Camera output id is required.");

            if (!owner.IsValid)
                return Blocked("camera.request.owner.invalid", "Camera request owner is invalid.");

            if (!lifetime.IsValid)
                return Blocked("camera.request.lifetime.invalid", "Camera request lifetime is invalid.");

            if (!rig.IsValid)
                return Blocked("camera.request.rig.missing", "Camera request requires a recipe or materialized rig.");

            if (targetSource.IsNone)
                return Blocked("camera.request.target-source.missing", "Camera request target source is required.");

            if (releaseCondition == CameraRequestReleaseCondition.Undefined)
                return Blocked("camera.request.release-condition.missing", "Camera request release condition is required.");

            string normalizedDiagnosticSource = diagnosticSource.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedDiagnosticSource))
                return Blocked("camera.request.diagnostic-source.missing", "Camera request diagnostic source is required.");

            string normalizedDiagnosticReason = diagnosticReason.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedDiagnosticReason))
                return Blocked("camera.request.diagnostic-reason.missing", "Camera request diagnostic reason is required.");

            CameraRequest request = new CameraRequest(
                requestId,
                outputId,
                owner,
                lifetime,
                rig,
                targetSource,
                policy,
                releaseCondition,
                normalizedDiagnosticSource,
                normalizedDiagnosticReason);

            return new CameraRequestCreateResult(
                CameraOperationStatus.Succeeded,
                request,
                Array.Empty<CameraIssue>(),
                string.Empty,
                $"Camera request contract created. request='{requestId}' output='{outputId}' owner='{owner}' lifetime='{lifetime}'.");
        }

        private static CameraRequestCreateResult Blocked(string code, string issue)
        {
            string normalizedIssue = issue.NormalizeTextOrFallback("Camera request creation was blocked.");
            return new CameraRequestCreateResult(
                CameraOperationStatus.Blocked,
                default,
                new[] { CameraIssue.Blocking(code, normalizedIssue) },
                normalizedIssue,
                "Camera request contract creation failed because mandatory data is invalid.");
        }
    }
}
