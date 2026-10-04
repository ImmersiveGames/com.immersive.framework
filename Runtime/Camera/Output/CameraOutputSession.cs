using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    internal readonly struct CameraOutputSessionAssignmentSnapshot
    {
        internal CameraOutputSessionAssignmentSnapshot(
            CameraOutputStateSnapshot outputState,
            CameraOutputRigApplicatorSnapshot applicator,
            CameraRigReference presentedNormalRig,
            CameraOutputFallbackCoverageOwnerId[] fallbackCoverageOwners)
        {
            OutputState = outputState;
            Applicator = applicator;
            PresentedNormalRig = presentedNormalRig;
            FallbackCoverageOwners = fallbackCoverageOwners ??
                Array.Empty<CameraOutputFallbackCoverageOwnerId>();
        }

        internal CameraOutputStateSnapshot OutputState { get; }
        internal CameraOutputRigApplicatorSnapshot Applicator { get; }
        internal CameraRigReference PresentedNormalRig { get; }
        internal CameraOutputFallbackCoverageOwnerId[] FallbackCoverageOwners { get; }
    }

    /// <summary>
    /// Scoped physical Output boundary for Assignment Occurrences and Fallback coverage.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputSession
    {
        private readonly CameraOutputRigApplicator _applicator;
        private readonly CameraRigReference _fallbackRig;
        private readonly HashSet<CameraOutputFallbackCoverageOwnerId> _fallbackCoverageOwners =
            new HashSet<CameraOutputFallbackCoverageOwnerId>();
        private readonly CameraOutputState _outputState;
        private CameraRigReference _presentedNormalRig;

        public CameraOutputSession(
            CameraOutputRigApplicator applicator,
            CameraRigReference fallbackRig)
        {
            this._applicator = applicator ??
                throw new ArgumentNullException(nameof(applicator));
            if (!applicator.Binding.IsValid)
            {
                throw new ArgumentException(
                    "Camera Output Session requires a valid physical Output binding.",
                    nameof(applicator));
            }

            if (!fallbackRig.IsValid)
            {
                throw new ArgumentException(
                    $"Camera output session '{applicator.Binding.OutputId}' requires an explicit valid Fallback Camera Rig.",
                    nameof(fallbackRig));
            }

            _fallbackRig = fallbackRig;
            _outputState = new CameraOutputState(applicator.Binding.OutputId);
        }

        public CameraOutputRigApplicator Applicator => _applicator;

        public CameraOutputId OutputId => _applicator.Binding.OutputId;

        public CameraRigReference FallbackRig => _fallbackRig;

        public CameraOutputState OutputState => _outputState;

        public bool IsFallbackCoverageActive => _fallbackCoverageOwners.Count > 0;

        public int FallbackCoverageOwnerCount => _fallbackCoverageOwners.Count;

        public bool TrySetActiveAssignment(SessionCameraAssignment assignment, out string issue)
        {
            return _outputState.TrySetActiveAssignment(assignment, out issue);
        }

        internal CameraOutputSessionAssignmentSnapshot CaptureAssignmentSnapshot()
        {
            var owners = new CameraOutputFallbackCoverageOwnerId[_fallbackCoverageOwners.Count];
            _fallbackCoverageOwners.CopyTo(owners);
            return new CameraOutputSessionAssignmentSnapshot(
                _outputState.CaptureSnapshot(),
                _applicator != null ? _applicator.CaptureSnapshot() : default,
                _presentedNormalRig,
                owners);
        }

        internal bool CanCommitAssignmentReplacement(
            CameraOutputSessionAssignmentSnapshot expected,
            SessionCameraAssignment assignment,
            CameraOccurrenceIdentity occurrence,
            bool fallbackCovering,
            out string issue)
        {
            return _outputState.CanCommitReplacement(
                expected.OutputState,
                assignment,
                occurrence,
                fallbackCovering,
                out issue);
        }

        internal CameraOccurrenceOutputResult ApplyAssignmentCandidate(
            CameraRigComposer composer,
            bool fallbackCovering)
        {
            if (_applicator == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Assignment replacement requires the concrete Camera Output applicator.");
            }

            bool keepFallbackPresentation = fallbackCovering || IsFallbackCoverageActive;
            return keepFallbackPresentation
                ? _applicator.ApplyFallbackCoverage(_fallbackRig.Composer)
                : _applicator.ApplySessionOccurrence(composer);
        }

        internal bool TryCommitAssignmentReplacement(
            CameraOutputSessionAssignmentSnapshot expected,
            SessionCameraAssignment assignment,
            CameraOccurrenceIdentity occurrence,
            CameraRigComposer composer,
            bool fallbackCovering,
            out string issue)
        {
            bool keepFallbackPresentation = fallbackCovering || IsFallbackCoverageActive;
            if (!_outputState.TryCommitReplacement(
                    expected.OutputState,
                    assignment,
                    occurrence,
                    keepFallbackPresentation,
                    out issue))
            {
                return false;
            }

            _presentedNormalRig = composer != null
                ? CameraRigReference.FromComposer(composer)
                : default;
            return true;
        }

        internal CameraOccurrenceOutputResult RestoreAssignmentSnapshot(
            CameraOutputSessionAssignmentSnapshot snapshot)
        {
            if (_applicator == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Assignment replacement rollback requires the concrete Camera Output applicator.");
            }

            CameraOccurrenceOutputResult restoreResult = _applicator.RestoreSnapshot(snapshot.Applicator);
            _outputState.RestoreSnapshot(snapshot.OutputState);
            _presentedNormalRig = snapshot.PresentedNormalRig;
            _fallbackCoverageOwners.Clear();
            for (int index = 0; index < snapshot.FallbackCoverageOwners.Length; index++)
            {
                _fallbackCoverageOwners.Add(snapshot.FallbackCoverageOwners[index]);
            }
            return restoreResult;
        }

        internal bool ReleaseFallbackCoverageOwnershipWithoutRouting(
            CameraOutputFallbackCoverageOwnerId ownerId)
        {
            return ownerId.IsValid && _fallbackCoverageOwners.Remove(ownerId);
        }

        internal bool RegisterFallbackCoverageOwnershipAfterAssignmentCommit(
            CameraOutputFallbackCoverageOwnerId ownerId)
        {
            return ownerId.IsValid && _outputState.IsFallbackAvailable &&
                _fallbackCoverageOwners.Add(ownerId);
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

            if (!_outputState.CanPresentNormalOccurrence(occurrence, out string issue))
            {
                return BlockedFallbackCoverage("camera.output-session.occurrence.invalid", issue);
            }

            CameraOutputApplyResult applyResult = _applicator != null
                ? _applicator.ApplyNormalOccurrence(occurrenceRig)
                : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                    "Normal Camera Occurrence application requires the concrete Output applicator.");
            if (!applyResult.Succeeded) return applyResult;

            if (!_outputState.TryPresentNormalOccurrence(occurrence, out issue))
            {
                _applicator.ApplyFallbackRig(_fallbackRig);
                return BlockedFallbackCoverage("camera.output-session.occurrence.commit-failed", issue);
            }
            _presentedNormalRig = occurrenceRig;
            return applyResult;
        }

        internal CameraOccurrenceOutputResult PresentNormalOccurrence(
            CameraOccurrenceIdentity occurrence,
            CameraRigComposer occurrenceComposer)
        {
            if (!_outputState.CanPresentNormalOccurrence(occurrence, out string issue))
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    issue);
            }

            if (_applicator == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Session Camera Occurrence application requires the concrete Output applicator.");
            }

            if (_fallbackCoverageOwners.Count > 0)
            {
                if (!_outputState.TryRetainNormalOccurrence(occurrence, out issue))
                {
                    return CameraOccurrenceOutputResult.Rejected(
                        _applicator.AppliedCamera,
                        "Session Camera Occurrence could not be retained under temporary Fallback coverage. " + issue);
                }

                _presentedNormalRig = CameraRigReference.FromComposer(occurrenceComposer);
                return CameraOccurrenceOutputResult.Preserved(_applicator.AppliedCamera);
            }

            CameraOccurrenceOutputResult applyResult =
                _applicator.ApplySessionOccurrence(occurrenceComposer);
            if (!applyResult.Succeeded) return applyResult;

            if (!_outputState.TryPresentNormalOccurrence(occurrence, out issue))
            {
                _applicator.ApplyFallbackCoverage(_fallbackRig.Composer);
                return CameraOccurrenceOutputResult.Rejected(
                    applyResult.PreviousCamera,
                    "Session Camera Occurrence could not be committed to Output state. " + issue);
            }

            _presentedNormalRig = CameraRigReference.FromComposer(occurrenceComposer);
            return applyResult;
        }

        internal CameraOccurrenceOutputResult RemoveIndividualOccurrence(
            CameraOccurrenceIdentity occurrence)
        {
            if (!_outputState.HasRetainedNormalOccurrence ||
                _outputState.RetainedNormalOccurrence != occurrence)
            {
                return CameraOccurrenceOutputResult.Preserved(
                    _applicator.AppliedCamera);
            }

            if (!occurrence.IsIndividual ||
                occurrence.AssignmentId != _outputState.ActiveAssignmentId ||
                occurrence.OutputId != OutputId)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Only the exact active Individual occurrence can be removed from this Output.");
            }

            CameraOccurrenceOutputResult applyResult = _applicator != null
                ? _applicator.ApplyFallbackCoverage(_fallbackRig.Composer)
                : CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Removing an Individual occurrence requires the concrete Output applicator.");
            if (!applyResult.Succeeded)
            {
                return applyResult;
            }

            if (!_outputState.TryRemoveIndividualOccurrence(occurrence, out string issue))
            {
                if (_presentedNormalRig.IsValid)
                {
                    _applicator.ApplyNormalOccurrence(_presentedNormalRig);
                }
                return CameraOccurrenceOutputResult.Rejected(
                    applyResult.PreviousCamera,
                    issue);
            }

            _presentedNormalRig = default;
            return applyResult;
        }

        internal bool ResetSessionAssignmentToFallback(out string issue)
        {
            CameraOccurrenceOutputResult applyResult = _applicator != null
                ? _applicator.ApplyFallbackCoverage(_fallbackRig.Composer)
                : CameraOccurrenceOutputResult.Rejected(
                    _applicator.AppliedCamera,
                    "Session Assignment teardown requires the concrete Output applicator.");
            if (!applyResult.Succeeded)
            {
                issue = applyResult.Diagnostic;
                return false;
            }

            if (!_outputState.IsFallbackAvailable &&
                !_outputState.TryMakeFallbackAvailable(out issue))
            {
                return false;
            }

            _outputState.TryCoverWithFallback(out _);
            if (_outputState.HasActiveAssignment &&
                !_outputState.TryClearActiveAssignment(out issue))
            {
                return false;
            }

            _presentedNormalRig = default;
            issue = string.Empty;
            return true;
        }

        public CameraOutputApplyResult CoverWithFallback(CameraOutputFallbackCoverageOwnerId ownerId)
        {
            if (!ownerId.IsValid)
            {
                return BlockedFallbackCoverage("camera.output-session.fallback.owner-missing",
                    "Fallback coverage requires an explicit owner.");
            }

            if (!_outputState.IsFallbackAvailable)
            {
                return BlockedFallbackCoverage("camera.output-session.fallback.unavailable",
                    "Fallback Camera must be available before it can cover this Output.");
            }

            bool added = _fallbackCoverageOwners.Add(ownerId);
            CameraOutputApplyResult applyResult = _applicator != null
                ? _applicator.ApplyFallbackRig(_fallbackRig)
                : _applicator.ApplyFallbackRig(_fallbackRig);
            if (!applyResult.Succeeded)
            {
                if (added) _fallbackCoverageOwners.Remove(ownerId);
                return applyResult;
            }
            _outputState.TryCoverWithFallback(out _);
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
                    : _applicator.ApplyFallbackRig(_fallbackRig);

            CameraOutputApplyResult applyResult;
            if (_outputState.HasRetainedNormalOccurrence && _presentedNormalRig.IsValid &&
                _outputState.CanRestoreNormalOccurrence(out _))
            {
                applyResult = _applicator != null
                    ? _applicator.ApplyNormalOccurrence(_presentedNormalRig)
                    : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                        "Restoring a normal occurrence requires the concrete Output applicator.");
                if (applyResult.Succeeded) _outputState.TryRestoreNormalOccurrence(out _);
            }
            else if (_outputState.HasActiveAssignment)
            {
                applyResult = _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _applicator.ApplyFallbackRig(_fallbackRig);
                if (applyResult.Succeeded)
                {
                    _outputState.TryCoverWithFallback(out _);
                }
            }
            else
            {
                applyResult = ApplyEffectivePresentation();
            }

            if (!applyResult.Succeeded && removed) _fallbackCoverageOwners.Add(ownerId);
            return applyResult;
        }

        public CameraOutputApplyResult Synchronize()
        {
            CameraOutputApplyResult applyResult =
                ApplyEffectivePresentation();

            if (applyResult.Succeeded)
            {
                if (_applicator != null && _applicator.HasAppliedFallback)
                {
                    _outputState.TryMakeFallbackAvailable(out _);
                }
                return applyResult;
            }
            return applyResult;
        }

        public CameraOutputApplyResult Teardown()
        {
            _fallbackCoverageOwners.Clear();
            return _applicator.Clear();
        }

        private CameraOutputApplyResult ApplyEffectivePresentation()
        {
            if (_fallbackCoverageOwners.Count > 0)
            {
                return _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _applicator.ApplyFallbackRig(_fallbackRig);
            }

            if (_outputState.HasActiveAssignment)
            {
                if (_outputState.HasPresentedNormalOccurrence &&
                    !_outputState.IsFallbackCovering && _presentedNormalRig.IsValid)
                {
                    return _applicator != null
                        ? _applicator.ApplyNormalOccurrence(_presentedNormalRig)
                        : BlockedFallbackCoverage("camera.output-session.applicator.missing",
                            "Normal occurrence synchronization requires the concrete Output applicator.");
                }
                return _applicator != null
                    ? _applicator.ApplyFallbackRig(_fallbackRig)
                    : _applicator.ApplyFallbackRig(_fallbackRig);
            }

            return _applicator.ApplyFallbackRig(_fallbackRig);
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
                _applicator.AppliedCamera,
                _applicator.AppliedCamera,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }

    }
}
