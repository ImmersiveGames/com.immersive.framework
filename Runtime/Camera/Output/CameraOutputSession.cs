using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped physical Output boundary. Assignment/Occurrence state is independent from
    /// the retained legacy CameraOutputContext request path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputSession
    {
        private readonly CameraOutputContext _context;
        private readonly CameraOutputRigApplicator _applicator;
        private readonly ICameraOutputApplication _application;
        private readonly CameraRigReference _fallbackRig;
        private readonly HashSet<CameraOutputFallbackCoverageOwnerId> _fallbackCoverageOwners =
            new HashSet<CameraOutputFallbackCoverageOwnerId>();
        private readonly CameraOutputState _presentationState;
        private CameraRigReference _presentedNormalRig;

        public CameraOutputSession(
            CameraOutputContext context,
            CameraOutputRigApplicator applicator,
            CameraRigReference fallbackRig)
            : this(context, applicator, fallbackRig, applicator)
        {
        }

        internal CameraOutputSession(
            CameraOutputContext context,
            ICameraOutputApplication application,
            CameraRigReference fallbackRig)
            : this(context, application as CameraOutputRigApplicator, fallbackRig, application)
        {
        }

        private CameraOutputSession(
            CameraOutputContext context,
            CameraOutputRigApplicator applicator,
            CameraRigReference fallbackRig,
            ICameraOutputApplication application)
        {
            this._context = context ??
                throw new ArgumentNullException(nameof(context));

            this._application = application ??
                throw new ArgumentNullException(nameof(application));
            this._applicator = applicator;

            if (context.OutputId != application.Binding.OutputId)
            {
                throw new ArgumentException(
                    $"Camera output context '{context.OutputId}' does not match applicator binding '{application.Binding.OutputId}'.",
                    nameof(application));
            }

            if (!fallbackRig.IsValid)
            {
                throw new ArgumentException(
                    $"Camera output session '{context.OutputId}' requires an explicit valid Fallback Camera Rig.",
                    nameof(fallbackRig));
            }

            _fallbackRig = fallbackRig;
            _presentationState = new CameraOutputState(context.OutputId);
        }

        public CameraOutputContext Context => _context;

        public CameraOutputRigApplicator Applicator => _applicator;

        public CameraOutputId OutputId => _context.OutputId;

        public CameraRigReference FallbackRig => _fallbackRig;

        public CameraOutputState OutputState => _presentationState;

        public bool IsFallbackCoverageActive => _fallbackCoverageOwners.Count > 0;

        public int FallbackCoverageOwnerCount => _fallbackCoverageOwners.Count;

        public CameraOutputSessionResult Admit(CameraRequest request)
        {
            CameraOutputContextResult contextResult =
                _context.Admit(request);

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
                _context.Release(request.RequestId);

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
                _context.Release(requestId);

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
                _context.Admit(releasedRequest);

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

        public bool TrySetActiveAssignment(SessionCameraAssignment assignment, out string issue)
        {
            return _presentationState.TrySetActiveAssignment(assignment, out issue);
        }

        public CameraOutputApplyResult PresentNormalOccurrence(
            CameraOccurrenceIdentity occurrence,
            CameraRigReference occurrenceRig)
        {
            if (_fallbackCoverageOwners.Count > 0)
            {
                return BlockedFallbackCoverage("camera.output-session.occurrence.covered",
                    "A normal occurrence cannot be presented while explicit Fallback coverage is active.");
            }

            if (!_presentationState.CanPresentNormalOccurrence(occurrence, out string issue))
            {
                return BlockedFallbackCoverage("camera.output-session.occurrence.invalid", issue);
            }

            CameraOutputApplyResult applyResult = _applicator != null
                ? _applicator.ApplyNormalOccurrence(occurrenceRig)
                : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                    "Normal Camera Occurrence application requires the concrete Output applicator.");
            if (!applyResult.Succeeded) return applyResult;

            if (!_presentationState.TryPresentNormalOccurrence(occurrence, out issue))
            {
                _applicator.ApplyFallbackRig(_fallbackRig);
                return BlockedFallbackCoverage("camera.output-session.occurrence.commit-failed", issue);
            }
            _presentedNormalRig = occurrenceRig;
            return applyResult;
        }

        public CameraOutputApplyResult CoverWithFallback(CameraOutputFallbackCoverageOwnerId ownerId)
        {
            if (!ownerId.IsValid)
            {
                return BlockedFallbackCoverage("camera.output-session.fallback.owner-missing",
                    "Fallback coverage requires an explicit owner.");
            }

            if (!_presentationState.IsFallbackAvailable)
            {
                return BlockedFallbackCoverage("camera.output-session.fallback.unavailable",
                    "Fallback Camera must be available before it can cover this Output.");
            }

            bool added = _fallbackCoverageOwners.Add(ownerId);
            CameraOutputApplyResult applyResult = _applicator != null
                ? _applicator.ApplyFallbackRig(_fallbackRig)
                : _application.Apply(_context, _fallbackRig, true);
            if (!applyResult.Succeeded)
            {
                if (added) _fallbackCoverageOwners.Remove(ownerId);
                return applyResult;
            }
            _presentationState.TryCoverWithFallback(out _);
            return applyResult;
        }

        public CameraOutputApplyResult ReleaseFallbackCoverage(CameraOutputFallbackCoverageOwnerId ownerId)
        {
            if (!ownerId.IsValid)
                return BlockedFallbackCoverage("camera.output-session.fallback.owner-missing",
                    "Fallback coverage release requires an explicit owner.");

            bool removed = _fallbackCoverageOwners.Remove(ownerId);
            if (_fallbackCoverageOwners.Count > 0)
                return _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _application.Apply(_context, _fallbackRig, true);

            CameraOutputApplyResult applyResult;
            if (_presentationState.HasRetainedNormalOccurrence && _presentedNormalRig.IsValid &&
                _presentationState.CanRestoreNormalOccurrence(out _))
            {
                applyResult = _applicator != null
                    ? _applicator.ApplyNormalOccurrence(_presentedNormalRig)
                    : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                        "Restoring a normal occurrence requires the concrete Output applicator.");
                if (applyResult.Succeeded) _presentationState.TryRestoreNormalOccurrence(out _);
            }
            else
            {
                applyResult = ApplyEffectivePresentation();
            }

            if (!applyResult.Succeeded && removed) _fallbackCoverageOwners.Add(ownerId);
            return applyResult;
        }

        public CameraOutputSessionResult Synchronize()
        {
            CameraOutputApplyResult applyResult =
                ApplyEffectivePresentation();

            if (applyResult.Succeeded)
            {
                if (_applicator != null && _applicator.HasAppliedFallback)
                {
                    _presentationState.TryMakeFallbackAvailable(out _);
                }
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
            _fallbackCoverageOwners.Clear();
            return _application.Clear();
        }

        private CameraOutputApplyResult ApplyEffectivePresentation()
        {
            if (_fallbackCoverageOwners.Count > 0)
            {
                return _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _application.Apply(_context, _fallbackRig, true);
            }

            if (_presentationState.HasActiveAssignment)
            {
                if (_presentationState.HasPresentedNormalOccurrence &&
                    !_presentationState.IsFallbackCovering && _presentedNormalRig.IsValid)
                {
                    return _applicator != null
                        ? _applicator.ApplyNormalOccurrence(_presentedNormalRig)
                        : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                            "Normal occurrence synchronization requires the concrete Output applicator.");
                }
                return _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _application.Apply(_context, _fallbackRig, true);
            }

            return _application.Apply(
                _context,
                _fallbackRig,
                false);
        }

        private CameraOutputApplyResult BlockedFallbackCoverage(
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera output Fallback coverage mutation was blocked.");

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Blocked,
                default,
                _application.AppliedCamera,
                _application.AppliedCamera,
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
