using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped orchestration boundary that keeps CameraOutputContext and
    /// CameraOutputRigApplicator synchronized after every accepted mutation.
    /// Winner selection remains exclusively inside CameraOutputContext.
    /// </summary>
    public sealed class CameraOutputSession
    {
        private readonly CameraOutputContext context;
        private readonly CameraOutputRigApplicator applicator;

        public CameraOutputSession(
            CameraOutputContext context,
            CameraOutputRigApplicator applicator)
        {
            this.context = context ??
                throw new ArgumentNullException(nameof(context));

            this.applicator = applicator ??
                throw new ArgumentNullException(nameof(applicator));

            if (context.OutputId != applicator.Binding.OutputId)
            {
                throw new ArgumentException(
                    $"Camera output context '{context.OutputId}' does not match applicator binding '{applicator.Binding.OutputId}'.",
                    nameof(applicator));
            }
        }

        public CameraOutputContext Context => context;

        public CameraOutputRigApplicator Applicator => applicator;

        public CameraOutputId OutputId => context.OutputId;

        public CameraOutputSessionResult Admit(CameraRequest request)
        {
            CameraOutputContextResult contextResult =
                context.Admit(request);

            if (!contextResult.Succeeded)
            {
                return Rejected(
                    contextResult,
                    $"Camera output session rejected admission for request '{request.RequestId}'.");
            }

            CameraOutputApplyResult applyResult =
                applicator.Apply(context);

            if (applyResult.Succeeded)
            {
                return Succeeded(
                    contextResult,
                    applyResult,
                    $"Camera output session admitted and applied request '{request.RequestId}'.");
            }

            CameraOutputContextResult rollbackContext =
                context.Release(request.RequestId);

            CameraOutputApplyResult rollbackApply =
                applicator.Apply(context);

            return CreateRollbackResult(
                contextResult,
                applyResult,
                rollbackContext,
                rollbackApply,
                "admission",
                request.RequestId);
        }

        public CameraOutputSessionResult Release(CameraRequestId requestId)
        {
            CameraOutputContextResult contextResult =
                context.Release(requestId);

            if (!contextResult.Succeeded)
            {
                return Rejected(
                    contextResult,
                    $"Camera output session rejected release for request '{requestId}'.");
            }

            CameraOutputApplyResult applyResult =
                applicator.Apply(context);

            if (applyResult.Succeeded)
            {
                return Succeeded(
                    contextResult,
                    applyResult,
                    $"Camera output session released request '{requestId}' and synchronized the output.");
            }

            CameraRequest releasedRequest =
                contextResult.Request;

            CameraOutputContextResult rollbackContext =
                context.Admit(releasedRequest);

            CameraOutputApplyResult rollbackApply =
                applicator.Apply(context);

            return CreateRollbackResult(
                contextResult,
                applyResult,
                rollbackContext,
                rollbackApply,
                "release",
                requestId);
        }

        public CameraOutputSessionResult Synchronize()
        {
            CameraOutputApplyResult applyResult =
                applicator.Apply(context);

            if (applyResult.Succeeded)
            {
                return new CameraOutputSessionResult(
                    CameraOutputSessionOperationKind.Succeeded,
                    default,
                    true,
                    applyResult,
                    false,
                    default,
                    false,
                    default,
                    Array.Empty<CameraIssue>(),
                    $"Camera output session synchronized output '{OutputId}'.");
            }

            return new CameraOutputSessionResult(
                CameraOutputSessionOperationKind.Rejected,
                default,
                true,
                applyResult,
                false,
                default,
                false,
                default,
                applyResult.Issues,
                $"Camera output session synchronization was blocked. {applyResult.DiagnosticSummary}");
        }

        private static CameraOutputSessionResult Succeeded(
            CameraOutputContextResult contextResult,
            CameraOutputApplyResult applyResult,
            string summary)
        {
            return new CameraOutputSessionResult(
                CameraOutputSessionOperationKind.Succeeded,
                contextResult,
                true,
                applyResult,
                false,
                default,
                false,
                default,
                Array.Empty<CameraIssue>(),
                summary);
        }

        private static CameraOutputSessionResult Rejected(
            CameraOutputContextResult contextResult,
            string summary)
        {
            return new CameraOutputSessionResult(
                CameraOutputSessionOperationKind.Rejected,
                contextResult,
                false,
                default,
                false,
                default,
                false,
                default,
                contextResult.Issues,
                $"{summary} {contextResult.DiagnosticSummary}");
        }

        private static CameraOutputSessionResult CreateRollbackResult(
            CameraOutputContextResult originalContextResult,
            CameraOutputApplyResult originalApplyResult,
            CameraOutputContextResult rollbackContextResult,
            CameraOutputApplyResult rollbackApplyResult,
            string operation,
            CameraRequestId requestId)
        {
            bool rollbackContextSucceeded =
                rollbackContextResult.Succeeded;

            bool rollbackApplySucceeded =
                rollbackApplyResult.Succeeded;

            if (rollbackContextSucceeded && rollbackApplySucceeded)
            {
                CameraIssue issue = CameraIssue.Blocking(
                    "camera.output-session.application-failed-rolled-back",
                    $"Camera output session {operation} for request '{requestId}' was rolled back because output application failed. " +
                    originalApplyResult.DiagnosticSummary);

                return new CameraOutputSessionResult(
                    CameraOutputSessionOperationKind.RolledBack,
                    originalContextResult,
                    true,
                    originalApplyResult,
                    true,
                    rollbackContextResult,
                    true,
                    rollbackApplyResult,
                    new[] { issue },
                    issue.Message);
            }

            CameraIssue fatalIssue = CameraIssue.Blocking(
                "camera.output-session.rollback-failed",
                $"Camera output session {operation} for request '{requestId}' failed during output application and rollback did not fully restore consistency. " +
                $"apply='{originalApplyResult.DiagnosticSummary}' " +
                $"rollbackContext='{rollbackContextResult.DiagnosticSummary}' " +
                $"rollbackApply='{rollbackApplyResult.DiagnosticSummary}'.");

            return new CameraOutputSessionResult(
                CameraOutputSessionOperationKind.RollbackFailed,
                originalContextResult,
                true,
                originalApplyResult,
                true,
                rollbackContextResult,
                true,
                rollbackApplyResult,
                new[] { fatalIssue },
                fatalIssue.Message);
        }
    }
}
