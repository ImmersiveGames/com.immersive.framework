using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped orchestration boundary that keeps CameraOutputContext and
    /// CameraOutputRigApplicator synchronized after every accepted mutation.
    /// Winner selection remains exclusively inside CameraOutputContext.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputSession
    {
        private readonly CameraOutputContext context;
        private readonly CameraOutputRigApplicator applicator;
        private readonly CameraRigReference defaultRig;
        private readonly HashSet<string> forceDefaultOwners =
            new HashSet<string>(StringComparer.Ordinal);

        public CameraOutputSession(
            CameraOutputContext context,
            CameraOutputRigApplicator applicator,
            CameraRigReference defaultRig)
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

            if (!defaultRig.IsValid)
            {
                throw new ArgumentException(
                    $"Camera output session '{context.OutputId}' requires an explicit valid Default Camera Rig.",
                    nameof(defaultRig));
            }

            this.defaultRig = defaultRig;
        }

        public CameraOutputContext Context => context;

        public CameraOutputRigApplicator Applicator => applicator;

        public CameraOutputId OutputId => context.OutputId;

        public CameraRigReference DefaultRig => defaultRig;

        public bool IsDefaultForced => forceDefaultOwners.Count > 0;

        public int ForceDefaultOwnerCount => forceDefaultOwners.Count;

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
                ApplyEffectivePresentation();

            if (applyResult.Succeeded)
            {
                return Succeeded(
                    contextResult,
                    applyResult,
                    $"Camera output session admitted and synchronized request '{request.RequestId}'.");
            }

            CameraOutputContextResult rollbackContext =
                context.Release(request.RequestId);

            CameraOutputApplyResult rollbackApply =
                ApplyEffectivePresentation();

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
                ApplyEffectivePresentation();

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
                ApplyEffectivePresentation();

            return CreateRollbackResult(
                contextResult,
                applyResult,
                rollbackContext,
                rollbackApply,
                "release",
                requestId);
        }

        public CameraOutputApplyResult ForceDefault(string owner)
        {
            string normalizedOwner = owner.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedOwner))
            {
                return BlockedForceDefaultOwner(
                    "camera.output-session.force-default.owner-missing",
                    "Camera output force-default requires an explicit owner.");
            }

            bool added = forceDefaultOwners.Add(normalizedOwner);
            CameraOutputApplyResult applyResult =
                ApplyEffectivePresentation();

            if (applyResult.Succeeded || !added)
            {
                return applyResult;
            }

            forceDefaultOwners.Remove(normalizedOwner);
            ApplyEffectivePresentation();
            return applyResult;
        }

        public CameraOutputApplyResult ReleaseForceDefault(string owner)
        {
            string normalizedOwner = owner.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedOwner))
            {
                return BlockedForceDefaultOwner(
                    "camera.output-session.force-default.owner-missing",
                    "Camera output force-default release requires an explicit owner.");
            }

            bool removed = forceDefaultOwners.Remove(normalizedOwner);
            CameraOutputApplyResult applyResult =
                ApplyEffectivePresentation();

            if (applyResult.Succeeded || !removed)
            {
                return applyResult;
            }

            forceDefaultOwners.Add(normalizedOwner);
            ApplyEffectivePresentation();
            return applyResult;
        }

        public CameraOutputSessionResult Synchronize()
        {
            CameraOutputApplyResult applyResult =
                ApplyEffectivePresentation();

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

        public CameraOutputApplyResult Teardown()
        {
            forceDefaultOwners.Clear();
            return applicator.Clear();
        }

        private CameraOutputApplyResult ApplyEffectivePresentation()
        {
            return applicator.Apply(
                context,
                defaultRig,
                forceDefaultOwners.Count > 0);
        }

        private CameraOutputApplyResult BlockedForceDefaultOwner(
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera output force-default mutation was blocked.");

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Blocked,
                default,
                applicator.AppliedCamera,
                applicator.AppliedCamera,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }

        private static CameraOutputSessionResult Succeeded(
            CameraOutputContextResult contextResult,
            CameraOutputApplyResult applyResult,
            string summary)
        {
            CameraIssue[] issues = MergeIssues(
                contextResult.Issues,
                applyResult.Issues);

            return new CameraOutputSessionResult(
                CameraOutputSessionOperationKind.Succeeded,
                contextResult,
                true,
                applyResult,
                false,
                default,
                false,
                default,
                issues,
                issues.Length == 0
                    ? summary
                    : $"{summary} {contextResult.DiagnosticSummary}".NormalizeText());
        }

        private static CameraIssue[] MergeIssues(
            CameraIssue[] contextIssues,
            CameraIssue[] applyIssues)
        {
            int contextCount = contextIssues?.Length ?? 0;
            int applyCount = applyIssues?.Length ?? 0;

            if (contextCount == 0 && applyCount == 0)
            {
                return Array.Empty<CameraIssue>();
            }

            var merged = new CameraIssue[contextCount + applyCount];

            if (contextCount > 0)
            {
                Array.Copy(contextIssues, 0, merged, 0, contextCount);
            }

            if (applyCount > 0)
            {
                Array.Copy(applyIssues, 0, merged, contextCount, applyCount);
            }

            return merged;
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
