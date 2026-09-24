using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestPublisherResult
    {
        internal CameraRequestPublisherResult(
            CameraRequestPublisherOperationKind operationKind,
            CameraRequest request,
            bool hasSessionResult,
            CameraOutputSessionResult sessionResult,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            OperationKind = operationKind;
            Request = request;
            HasSessionResult = hasSessionResult;
            SessionResult = sessionResult;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraRequestPublisherOperationKind OperationKind { get; }
        public CameraRequest Request { get; }
        public bool HasSessionResult { get; }
        public CameraOutputSessionResult SessionResult { get; }
        public CameraIssue[] Issues { get; }
        public string DiagnosticSummary { get; }

        public bool Succeeded =>
            OperationKind == CameraRequestPublisherOperationKind.Published ||
            OperationKind == CameraRequestPublisherOperationKind.Released ||
            OperationKind == CameraRequestPublisherOperationKind.Preserved;

        public bool IsRejected =>
            OperationKind == CameraRequestPublisherOperationKind.Rejected;
    }
}
