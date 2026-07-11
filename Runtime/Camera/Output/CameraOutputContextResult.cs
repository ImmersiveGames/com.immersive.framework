using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit result for one admission or release operation.
    /// </summary>
    public readonly struct CameraOutputContextResult
    {
        internal CameraOutputContextResult(
            CameraOutputContextOperationKind operationKind,
            CameraOutputContextChangeKind changeKind,
            CameraRequest request,
            bool hasPreviousWinner,
            CameraRequest previousWinner,
            bool hasCurrentWinner,
            CameraRequest currentWinner,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            OperationKind = operationKind;
            ChangeKind = changeKind;
            Request = request;
            HasPreviousWinner = hasPreviousWinner;
            PreviousWinner = previousWinner;
            HasCurrentWinner = hasCurrentWinner;
            CurrentWinner = currentWinner;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraOutputContextOperationKind OperationKind { get; }

        public CameraOutputContextChangeKind ChangeKind { get; }

        public CameraRequest Request { get; }

        public bool HasPreviousWinner { get; }

        public CameraRequest PreviousWinner { get; }

        public bool HasCurrentWinner { get; }

        public CameraRequest CurrentWinner { get; }

        public CameraIssue[] Issues { get; }

        public string DiagnosticSummary { get; }

        public bool Succeeded =>
            OperationKind == CameraOutputContextOperationKind.Admitted ||
            OperationKind == CameraOutputContextOperationKind.Released;

        public bool IsBlocked =>
            OperationKind == CameraOutputContextOperationKind.Blocked;

        public bool IsNotFound =>
            OperationKind == CameraOutputContextOperationKind.NotFound;
    }
}
