using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
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
