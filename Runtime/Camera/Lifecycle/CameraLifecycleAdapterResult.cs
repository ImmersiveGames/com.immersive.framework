using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    public readonly struct CameraLifecycleAdapterResult
    {
        internal CameraLifecycleAdapterResult(
            CameraLifecycleAdapterOperationKind operationKind,
            string scopeId,
            bool hasPublisherResult,
            CameraRequestPublisherResult publisherResult,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            OperationKind = operationKind;
            ScopeId = scopeId.NormalizeText();
            HasPublisherResult = hasPublisherResult;
            PublisherResult = publisherResult;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraLifecycleAdapterOperationKind OperationKind { get; }
        public string ScopeId { get; }
        public bool HasPublisherResult { get; }
        public CameraRequestPublisherResult PublisherResult { get; }
        public CameraIssue[] Issues { get; }
        public string DiagnosticSummary { get; }

        public bool Succeeded =>
            OperationKind == CameraLifecycleAdapterOperationKind.Entered ||
            OperationKind == CameraLifecycleAdapterOperationKind.Exited ||
            OperationKind == CameraLifecycleAdapterOperationKind.Preserved;

        public bool IsRejected =>
            OperationKind == CameraLifecycleAdapterOperationKind.Rejected;
    }
}
