using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Combined evidence for one context mutation and its automatic output application.
    /// </summary>
    public readonly struct CameraOutputSessionResult
    {
        internal CameraOutputSessionResult(
            CameraOutputSessionOperationKind operationKind,
            CameraOutputContextResult contextResult,
            bool hasApplyResult,
            CameraOutputApplyResult applyResult,
            bool hasRollbackContextResult,
            CameraOutputContextResult rollbackContextResult,
            bool hasRollbackApplyResult,
            CameraOutputApplyResult rollbackApplyResult,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            OperationKind = operationKind;
            ContextResult = contextResult;
            HasApplyResult = hasApplyResult;
            ApplyResult = applyResult;
            HasRollbackContextResult = hasRollbackContextResult;
            RollbackContextResult = rollbackContextResult;
            HasRollbackApplyResult = hasRollbackApplyResult;
            RollbackApplyResult = rollbackApplyResult;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraOutputSessionOperationKind OperationKind { get; }

        public CameraOutputContextResult ContextResult { get; }

        public bool HasApplyResult { get; }

        public CameraOutputApplyResult ApplyResult { get; }

        public bool HasRollbackContextResult { get; }

        public CameraOutputContextResult RollbackContextResult { get; }

        public bool HasRollbackApplyResult { get; }

        public CameraOutputApplyResult RollbackApplyResult { get; }

        public CameraIssue[] Issues { get; }

        public string DiagnosticSummary { get; }

        public bool Succeeded =>
            OperationKind == CameraOutputSessionOperationKind.Succeeded;

        public bool WasRejected =>
            OperationKind == CameraOutputSessionOperationKind.Rejected;

        public bool WasRolledBack =>
            OperationKind == CameraOutputSessionOperationKind.RolledBack;

        public bool RollbackFailed =>
            OperationKind == CameraOutputSessionOperationKind.RollbackFailed;
    }
}
