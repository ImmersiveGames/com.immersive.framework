using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Runtime authority for one Camera Composition and its request participation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Shared Camera Composition")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-E Composition-owned presentation and Output participation.")]
    public sealed class CameraSharedComposition : MonoBehaviour,
        ICameraSubjectAvailabilityConsumer,
        ICameraOutputSessionConsumer,
        ICameraOutputDefinitionConsumer
    {
        private readonly string membershipContextId = Guid.NewGuid().ToString("N");

        [Header("Subject Selection")]
        [SerializeField] private CameraSharedCompositionSubjectPolicyKind subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;

        [Header("Camera Output")]
        [SerializeField] private CameraOutputDefinition outputDefinition;

        [Header("Composition Rig")]
        [SerializeField] private CameraRigComposer compositionRig;

        [Header("Request Arbitration")]
        [SerializeField] private int requestPrecedence;

        private readonly string requestId = Guid.NewGuid().ToString("N");

        private ICameraSubjectAvailabilitySource _availability;
        private ICameraCompositionSubjectSelectionSource _selection;
        private CameraCompositionMembershipContext _membership;
        private CameraCompositionMembershipSnapshot _currentMembership;
        private bool _subscribed;
        private bool _selectionSubscribed;
        private int _consumedSelectionRevision = -1;
        private CameraOutputAuthoring _output;
        private ICameraRequestPublisher _requestPublisher;

        public CameraOutputDefinition OutputDefinition => outputDefinition;
        public CameraRigComposer CompositionRig => compositionRig;
        public int RequestPrecedence => requestPrecedence;
        public CameraRequestId RequestId => new CameraRequestId(requestId);
        public bool IsRequestPublished => _requestPublisher?.IsPublished == true;
        public string MembershipContextIdText => membershipContextId;
        public string OutputIdText => outputDefinition != null && outputDefinition.HasValidId
            ? outputDefinition.OutputId.Value : string.Empty;
        public CameraOutputId RequestedOutputId => new CameraOutputId(OutputIdText);
        public CameraOutputAuthoring Output => _output;
        public CameraSharedCompositionSubjectPolicyKind SubjectPolicy => subjectPolicy;
        public ICameraCompositionSubjectSelectionSource SubjectSelectionSource => _selection;
        public CameraSharedCompositionSnapshot Snapshot { get; private set; }

        public void Configure(CameraOutputDefinition output,
            CameraSharedCompositionSubjectPolicyKind policy)
        {
            if (_membership != null || _subscribed)
                throw new InvalidOperationException("An active shared Camera composition cannot be reconfigured.");
            CameraDefinitionValidation.ValidateOutputs(new[] { output });
            if (!ReferenceEquals(outputDefinition, output)) _output = null;
            outputDefinition = output;
            subjectPolicy = policy;
            TryStartComposition();
        }

        public void AttachCameraSubjectAvailability(ICameraSubjectAvailabilitySource availabilitySource)
        {
            if (availabilitySource == null) throw new ArgumentNullException(nameof(availabilitySource));
            if (ReferenceEquals(_availability, availabilitySource))
            {
                TryStartComposition();
                return;
            }
            if (!StopComposition())
                throw new InvalidOperationException(
                    $"Camera composition '{name}' could not replace Subject availability because teardown failed: {Snapshot.LastBlockingIssue}");
            _availability = availabilitySource;
            TryStartComposition();
        }

        public void AttachSubjectSelectionSource(ICameraCompositionSubjectSelectionSource selectionSource)
        {
            if (selectionSource == null) throw new ArgumentNullException(nameof(selectionSource));
            if (!selectionSource.ContextId.IsValid)
                throw new ArgumentException(
                    "Camera Composition Subject selection requires a valid context id.",
                    nameof(selectionSource));
            if (ReferenceEquals(_selection, selectionSource))
            {
                TryStartComposition();
                return;
            }

            if (subjectPolicy == CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                !StopComposition())
            {
                throw new InvalidOperationException(
                    $"Camera composition '{name}' could not replace Subject selection because teardown failed: {Snapshot.LastBlockingIssue}");
            }

            _selection = selectionSource;
            _consumedSelectionRevision = -1;
            TryStartComposition();
        }

        public void DetachSubjectSelectionSource(string reason)
        {
            if (_selection == null && !_selectionSubscribed)
                return;
            if (subjectPolicy == CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                !StopComposition())
                return;
            _selection = null;
            _consumedSelectionRevision = -1;
        }

        public void AttachOutputSession(CameraOutputAuthoring binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (!TryValidateDefinitions(out string diagnostic)) throw new InvalidOperationException(diagnostic);
            if (!ReferenceEquals(binding.OutputDefinition, outputDefinition) || binding.OutputId != RequestedOutputId)
                throw new InvalidOperationException(
                    $"Shared Camera composition '{name}' requires its exact Output definition '{outputDefinition.name}'; the injected physical Output references a different definition.");
            if (!StopComposition())
                throw new InvalidOperationException(
                    $"Camera composition '{name}' could not replace its Output because teardown failed: {Snapshot.LastBlockingIssue}");
            _output = binding;
            TryStartComposition();
        }

        public void DetachOutputSession(string reason)
        {
            if (StopComposition())
                _output = null;
        }

        public CameraSharedCompositionSnapshot Reconcile(CameraSubjectAvailabilitySnapshot availability) =>
            Reconcile(availability, _selection?.CurrentSnapshot);

        public CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            if (_membership == null)
                return Record(CameraSharedCompositionReconcileStatus.BlockedInvalidMembership, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, "Shared Camera composition has no active membership context.");
            if (!TryValidateCompositionRig(out string rigDiagnostic))
                return Record(ReferenceEquals(compositionRig, _output?.DefaultCameraRig)
                        ? CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault
                        : CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, rigDiagnostic);
            if (!TryAcceptSubjectSelection(selection, out CameraSharedCompositionReconcileStatus selectionStatus,
                    out string selectionDiagnostic))
                return Record(selectionStatus, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, selectionDiagnostic);

            CameraCompositionMembershipSnapshot previousMembership = _membership.Snapshot;
            CameraRigPresentationState previousPresentation =
                compositionRig.CapturePresentationState();

            IReadOnlyList<CameraSubjectId> desired = availability != null &&
                availability.ContextId == _membership.AvailabilityContextId
                    ? CameraSharedCompositionSubjectPolicy.SelectDesiredSubjects(
                        subjectPolicy, availability, selection)
                    : Array.Empty<CameraSubjectId>();
            CameraCompositionMembershipResult membership = _membership.Reconcile(availability, desired);
            _currentMembership = membership.Snapshot;
            if (!membership.Succeeded)
            {
                CameraSharedCompositionReconcileStatus status = membership.Status switch
                {
                    CameraCompositionMembershipStatus.RejectedForeignAvailabilityContext =>
                        CameraSharedCompositionReconcileStatus.RejectedForeignAvailabilityContext,
                    CameraCompositionMembershipStatus.RejectedStaleAvailabilitySnapshot =>
                        CameraSharedCompositionReconcileStatus.RejectedStaleAvailabilitySnapshot,
                    _ => CameraSharedCompositionReconcileStatus.BlockedMembershipFailure
                };
                return Record(status, availability, membership.AddedCount, membership.RemovedCount,
                    CameraRigPresentationApplyStatus.None, membership.Message);
            }

            CameraCompositionPresentationInputResult projection = CameraCompositionPresentationInputProjection.TryCreate(_currentMembership);
            if (!projection.Succeeded)
            {
                return RollbackAndRecord(
                    "presentation input projection",
                    CameraSharedCompositionReconcileStatus.BlockedProjectionFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    compositionRig.CapturePresentationState(),
                    CameraRigPresentationApplyStatus.None,
                    false,
                    projection.Message);
            }

            CameraRigTargetProjectionResult targetProjection =
                compositionRig.ResolvePresentationTargets(projection.Input);
            if (!targetProjection.Succeeded)
            {
                bool lostRequiredSubjects =
                    targetProjection.Status == CameraRigTargetProjectionStatus.BlockedRequiredSubjectMissing &&
                    projection.Input.SubjectCount == 0;
                if (!lostRequiredSubjects)
                {
                    return RollbackAndRecord(
                        "Composition target projection",
                        CameraSharedCompositionReconcileStatus.BlockedProjectionFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        compositionRig.CapturePresentationState(),
                        CameraRigPresentationApplyStatus.None,
                        false,
                        targetProjection.BlockingIssue);
                }

                CameraRigPresentationApplyResult cleared =
                    compositionRig.ClearPresentation();
                if (!cleared.Succeeded)
                {
                    return RollbackAndRecord(
                        "unpresentable presentation clear",
                        CameraSharedCompositionReconcileStatus.BlockedPresentationFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        compositionRig.CapturePresentationState(),
                        cleared.Status,
                        false,
                        cleared.Diagnostic);
                }

                if (!TryReleaseRequest(out bool releaseOutputRollbackFailed, out string releaseDiagnostic))
                {
                    return RollbackAndRecord(
                        "Composition request release",
                        CameraSharedCompositionReconcileStatus.BlockedRequestFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        compositionRig.CapturePresentationState(),
                        cleared.Status,
                        releaseOutputRollbackFailed,
                        releaseDiagnostic);
                }

                CommitAcceptedSubjectSelection(selection);
                return Record(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects, availability,
                    membership.AddedCount, membership.RemovedCount, cleared.Status, string.Empty);
            }

            CameraRigPresentationApplyResult presentation =
                compositionRig.ApplyCompositionPresentation(projection.Input, _currentMembership);
            if (!presentation.Succeeded)
            {
                return RollbackAndRecord(
                    "Composition presentation apply",
                    CameraSharedCompositionReconcileStatus.BlockedPresentationFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    compositionRig.CapturePresentationState(),
                    presentation.Status,
                    false,
                    presentation.Diagnostic);
            }

            if (!TryEnsureRequestPublished(out bool admitOutputRollbackFailed, out string requestDiagnostic))
            {
                return RollbackAndRecord(
                    "Composition request admission",
                    CameraSharedCompositionReconcileStatus.BlockedRequestFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    compositionRig.CapturePresentationState(),
                    presentation.Status,
                    admitOutputRollbackFailed,
                    requestDiagnostic);
            }

            CommitAcceptedSubjectSelection(selection);
            return Record(
                membership.Status == CameraCompositionMembershipStatus.SucceededNoChange
                    ? CameraSharedCompositionReconcileStatus.SucceededNoChange
                    : CameraSharedCompositionReconcileStatus.SucceededApplied,
                availability, membership.AddedCount, membership.RemovedCount, presentation.Status, string.Empty);
        }

        public bool TryValidateDefinitions(out string diagnostic)
        {
            try
            {
                CameraDefinitionValidation.ValidateOutputs(new[] { outputDefinition });
                diagnostic = string.Empty;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = $"Shared Camera composition '{name}': {exception.Message}";
                return false;
            }
        }

        private void OnEnable() => TryStartComposition();
        private void OnDisable() => StopComposition();
        private void OnDestroy() => StopComposition();

        private void TryStartComposition()
        {
            if (!isActiveAndEnabled || _subscribed || _availability == null) return;
            if (_membership != null && !StopComposition()) return;
            CameraSubjectAvailabilitySnapshot availability = _availability.CreateSnapshot();
            if (!TryValidateDefinitions(out string definitionDiagnostic))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidMembership, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, definitionDiagnostic);
                return;
            }
            if (subjectPolicy != CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects &&
                subjectPolicy != CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedMembershipFailure, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, "Shared Camera composition requires an explicitly supported Subject selection policy.");
                return;
            }
            if (subjectPolicy == CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                _selection == null)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedMissingSubjectSelection, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None,
                    "Explicit Camera Subject selection requires an attached selection source.");
                return;
            }
            if (!RequestedOutputId.IsValid || _output == null ||
                !ReferenceEquals(_output.OutputDefinition, outputDefinition) || _output.OutputId != RequestedOutputId ||
                _output.DefaultCameraRig == null)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, "Shared Camera composition requires its exact injected Output and an explicit Default Camera Rig.");
                return;
            }
            if (compositionRig == null)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None,
                    "Shared Camera composition requires an explicit Composition Camera Rig.");
                return;
            }
            if (ReferenceEquals(compositionRig, _output.DefaultCameraRig))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None,
                    "Composition Camera Rig must be distinct from the Output Default Camera Rig.");
                return;
            }
            if (!_output.TryGetSession(out _, out string sessionDiagnostic))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability, 0, 0,
                    CameraRigPresentationApplyStatus.None, sessionDiagnostic);
                return;
            }

            _membership = new CameraCompositionMembershipContext(
                new CameraCompositionMembershipContextId(membershipContextId), _availability.ContextId);
            _currentMembership = _membership.Snapshot;
            _availability.AvailabilityChanged += HandleAvailabilityChanged;
            _subscribed = true;
            if (subjectPolicy == CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                _selection.SelectionChanged += HandleSelectionChanged;
                _selectionSubscribed = true;
            }
            Reconcile(availability, _selection?.CurrentSnapshot);
        }

        private bool StopComposition()
        {
            bool wasActive = _membership != null;
            UnsubscribeSources();

            if (!wasActive)
                return true;

            CameraCompositionMembershipSnapshot previousMembership = _membership.Snapshot;
            CameraRigPresentationState previousPresentation =
                compositionRig != null ? compositionRig.CapturePresentationState() : null;
            CameraRigPresentationApplyResult cleared =
                compositionRig != null ? compositionRig.ClearPresentation() : null;

            if (cleared == null || !cleared.Succeeded)
            {
                string clearDiagnostic = cleared?.Diagnostic ??
                    "Composition teardown requires its explicit Composition Rig.";
                RollbackTeardownAndRecord(
                    "teardown presentation clear",
                    previousMembership,
                    previousPresentation,
                    compositionRig != null ? compositionRig.CapturePresentationState() : null,
                    cleared?.Status ?? CameraRigPresentationApplyStatus.None,
                    false,
                    clearDiagnostic);
                return false;
            }

            if (!TryReleaseRequest(out bool outputRollbackFailed, out string requestDiagnostic))
            {
                RollbackTeardownAndRecord(
                    "teardown request release",
                    previousMembership,
                    previousPresentation,
                    compositionRig.CapturePresentationState(),
                    cleared.Status,
                    outputRollbackFailed,
                    requestDiagnostic);
                return false;
            }

            int removed = 0;
            CameraCompositionMembershipResult membershipCleared = _membership.Clear();
            _currentMembership = membershipCleared.Snapshot;
            removed = membershipCleared.RemovedCount;
            Record(CameraSharedCompositionReconcileStatus.SucceededStopped,
                _availability?.CreateSnapshot(), 0, removed, cleared.Status, string.Empty);
            _membership = null;
            _currentMembership = null;
            _requestPublisher = null;
            _consumedSelectionRevision = -1;
            return true;
        }

        private void UnsubscribeSources()
        {
            if (_subscribed && _availability != null)
                _availability.AvailabilityChanged -= HandleAvailabilityChanged;
            if (_selectionSubscribed && _selection != null)
                _selection.SelectionChanged -= HandleSelectionChanged;
            _subscribed = false;
            _selectionSubscribed = false;
        }

        private void HandleAvailabilityChanged(CameraSubjectAvailabilitySnapshot availability) =>
            Reconcile(availability, _selection?.CurrentSnapshot);

        private void HandleSelectionChanged(CameraCompositionSubjectSelectionSnapshot selection) =>
            Reconcile(_availability != null ? _availability.CreateSnapshot() : null, selection);

        private bool TryAcceptSubjectSelection(
            CameraCompositionSubjectSelectionSnapshot selection,
            out CameraSharedCompositionReconcileStatus status,
            out string diagnostic)
        {
            status = CameraSharedCompositionReconcileStatus.None;
            diagnostic = string.Empty;
            if (subjectPolicy != CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
                return true;
            if (_selection == null || selection == null)
            {
                status = CameraSharedCompositionReconcileStatus.BlockedMissingSubjectSelection;
                diagnostic = "Explicit Camera Subject selection requires an attached selection source.";
                return false;
            }
            if (selection.ContextId != _selection.ContextId)
            {
                status = CameraSharedCompositionReconcileStatus.RejectedForeignSubjectSelectionContext;
                diagnostic = "Subject selection evidence belongs to another Composition selection context.";
                return false;
            }
            if (selection.Revision < _consumedSelectionRevision)
            {
                status = CameraSharedCompositionReconcileStatus.RejectedStaleSubjectSelectionSnapshot;
                diagnostic = "Subject selection evidence regressed and cannot restore an older Subject occurrence.";
                return false;
            }

            return true;
        }

        private void CommitAcceptedSubjectSelection(CameraCompositionSubjectSelectionSnapshot selection)
        {
            if (subjectPolicy != CameraSharedCompositionSubjectPolicyKind.ExplicitSelection ||
                selection == null ||
                selection.Revision <= _consumedSelectionRevision)
                return;
            _consumedSelectionRevision = selection.Revision;
        }

        private bool TryValidateCompositionRig(out string diagnostic)
        {
            if (compositionRig == null)
            {
                diagnostic = "Shared Camera composition requires an explicit Composition Camera Rig.";
                return false;
            }
            if (_output == null || _output.DefaultCameraRig == null)
            {
                diagnostic = "Shared Camera composition requires an injected Output with an explicit Default Camera Rig.";
                return false;
            }
            if (ReferenceEquals(compositionRig, _output.DefaultCameraRig))
            {
                diagnostic = "Composition Camera Rig must be distinct from the Output Default Camera Rig.";
                return false;
            }
            diagnostic = string.Empty;
            return true;
        }

        private bool TryEnsureRequestPublished(
            out bool outputRollbackFailed,
            out string diagnostic)
        {
            outputRollbackFailed = false;
            if (_requestPublisher != null)
            {
                if (_requestPublisher.Request.Rig.Composer != compositionRig ||
                    _requestPublisher.Request.OutputId != RequestedOutputId)
                {
                    diagnostic = "Active Composition request evidence does not match the configured Rig and Output.";
                    return false;
                }

                CameraRequestPublisherResult preserved = _requestPublisher.Publish();
                outputRollbackFailed = preserved.HasSessionResult &&
                    preserved.SessionResult.RollbackFailed;
                diagnostic = preserved.DiagnosticSummary;
                return preserved.Succeeded;
            }

            if (_output == null)
            {
                diagnostic = "Composition request publication requires an injected Camera Output.";
                return false;
            }
            if (!_output.TryGetSession(out CameraOutputSession session, out diagnostic))
                return false;

            var scopeId = new CameraRequestOwnerScopeId(membershipContextId);
            var lifetimeId = new CameraRequestLifetimeScopeId(membershipContextId);
            CameraRequestCreateResult request = CameraRequestCreateResult.Create(
                RequestId,
                RequestedOutputId,
                new CameraRequestOwner(CameraRequestOwnerKind.Composition, scopeId),
                new CameraRequestLifetime(CameraRequestLifetimeKind.Composition, lifetimeId),
                CameraRigReference.FromComposer(compositionRig),
                CameraTargetSourceDescriptor.Logical(
                    CameraTargetSourceKind.Composition,
                    membershipContextId,
                    "Camera Composition"),
                new CameraRequestPolicy(requestPrecedence, membershipContextId),
                CameraRequestReleaseCondition.EligibilityLost,
                nameof(CameraSharedComposition),
                "Current Camera Composition presentation is presentable.");
            if (!request.IsSucceeded)
            {
                diagnostic = request.BlockingIssue;
                return false;
            }

            CameraRequestPublisherCreateResult creation =
                CompositionCameraRequestPublisher.Create(session, request.Request);
            if (!creation.Succeeded || creation.Publisher == null)
            {
                diagnostic = creation.DiagnosticSummary;
                return false;
            }

            CameraRequestPublisherResult publication = creation.Publisher.Publish();
            outputRollbackFailed = publication.HasSessionResult &&
                publication.SessionResult.RollbackFailed;
            if (!publication.Succeeded)
            {
                if (creation.Publisher.IsPublished)
                    _requestPublisher = creation.Publisher;
                diagnostic = publication.DiagnosticSummary;
                return false;
            }

            _requestPublisher = creation.Publisher;
            diagnostic = publication.DiagnosticSummary;
            return true;
        }

        private bool TryReleaseRequest(
            out bool outputRollbackFailed,
            out string diagnostic)
        {
            outputRollbackFailed = false;
            if (_requestPublisher == null)
            {
                diagnostic = string.Empty;
                return true;
            }

            CameraRequestPublisherResult release = _requestPublisher.Release();
            outputRollbackFailed = release.HasSessionResult &&
                release.SessionResult.RollbackFailed;
            diagnostic = release.DiagnosticSummary;
            return release.Succeeded;
        }

        private CameraSharedCompositionSnapshot RollbackAndRecord(
            string operation,
            CameraSharedCompositionReconcileStatus failureStatus,
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionMembershipResult failedMembership,
            CameraCompositionMembershipSnapshot previousMembership,
            CameraRigPresentationState previousPresentation,
            CameraRigPresentationState failedPresentation,
            CameraRigPresentationApplyStatus presentationStatus,
            bool outputRollbackFailed,
            string originalFailure)
        {
            bool restored = TryRollbackState(
                previousMembership,
                failedMembership.Snapshot,
                previousPresentation,
                failedPresentation,
                out string rollbackDiagnostic);

            bool critical = outputRollbackFailed || !restored;
            string diagnostic = BuildTransactionFailureDiagnostic(
                operation,
                originalFailure,
                outputRollbackFailed
                    ? $"CameraOutputSession rollback failed. {rollbackDiagnostic}"
                    : rollbackDiagnostic);

            return Record(
                critical
                    ? CameraSharedCompositionReconcileStatus.CriticalRollbackFailure
                    : failureStatus,
                availability,
                failedMembership.AddedCount,
                failedMembership.RemovedCount,
                presentationStatus,
                diagnostic);
        }

        private CameraSharedCompositionSnapshot RollbackTeardownAndRecord(
            string operation,
            CameraCompositionMembershipSnapshot previousMembership,
            CameraRigPresentationState previousPresentation,
            CameraRigPresentationState failedPresentation,
            CameraRigPresentationApplyStatus presentationStatus,
            bool outputRollbackFailed,
            string originalFailure)
        {
            CameraCompositionMembershipSnapshot failedMembership = _membership.Snapshot;
            bool restored = TryRollbackState(
                previousMembership,
                failedMembership,
                previousPresentation,
                failedPresentation,
                out string rollbackDiagnostic);
            bool critical = outputRollbackFailed || !restored;
            string diagnostic = BuildTransactionFailureDiagnostic(
                operation,
                originalFailure,
                outputRollbackFailed
                    ? $"CameraOutputSession rollback failed. {rollbackDiagnostic}"
                    : rollbackDiagnostic);

            return Record(
                critical
                    ? CameraSharedCompositionReconcileStatus.CriticalRollbackFailure
                    : CameraSharedCompositionReconcileStatus.BlockedRequestFailure,
                _availability?.CreateSnapshot(),
                0,
                0,
                presentationStatus,
                diagnostic);
        }

        private bool TryRollbackState(
            CameraCompositionMembershipSnapshot previousMembership,
            CameraCompositionMembershipSnapshot expectedCurrentMembership,
            CameraRigPresentationState previousPresentation,
            CameraRigPresentationState expectedCurrentPresentation,
            out string diagnostic)
        {
            string membershipPreflightDiagnostic =
                "Composition membership rollback authority is unavailable.";
            if (_membership == null ||
                !_membership.CanRestore(
                    previousMembership,
                    expectedCurrentMembership,
                    out membershipPreflightDiagnostic))
            {
                diagnostic = $"Membership rollback preflight failed: {membershipPreflightDiagnostic}";
                return false;
            }

            if (compositionRig == null || previousPresentation == null ||
                expectedCurrentPresentation == null)
            {
                diagnostic = "Presentation rollback preflight failed because exact Composer state is unavailable.";
                return false;
            }

            CameraRigPresentationRestoreResult presentationRestore =
                compositionRig.RestorePresentationState(
                    previousPresentation,
                    expectedCurrentPresentation);
            if (!presentationRestore.Succeeded)
            {
                diagnostic = $"Presentation rollback failed: {presentationRestore.Diagnostic}";
                return false;
            }

            if (!_membership.TryRestore(
                    previousMembership,
                    expectedCurrentMembership,
                    out string membershipRestoreDiagnostic))
            {
                diagnostic = $"Membership rollback failed after presentation restore: {membershipRestoreDiagnostic}";
                return false;
            }

            _currentMembership = previousMembership;
            diagnostic = "Composition presentation and membership rollback restored the previous coherent state.";
            return true;
        }

        private string BuildTransactionFailureDiagnostic(
            string operation,
            string originalFailure,
            string rollbackDiagnostic)
        {
            return $"Camera Composition transaction failed. operation='{operation}' " +
                $"composition='{membershipContextId}' request='{RequestId}' output='{RequestedOutputId}' " +
                $"original='{originalFailure}' rollback='{rollbackDiagnostic}'.";
        }

        private CameraSharedCompositionSnapshot Record(
            CameraSharedCompositionReconcileStatus status,
            CameraSubjectAvailabilitySnapshot availability,
            int added,
            int removed,
            CameraRigPresentationApplyStatus presentationStatus,
            string issue)
        {
            CameraCompositionMembershipSnapshot membership = _currentMembership ?? _membership?.Snapshot;
            Snapshot = new CameraSharedCompositionSnapshot(
                status is CameraSharedCompositionReconcileStatus.SucceededApplied or
                    CameraSharedCompositionReconcileStatus.SucceededNoChange or
                    CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects,
                _availability?.ContextId ?? availability?.ContextId ?? default,
                membership?.AvailabilityRevision ?? 0,
                membership?.ContextId ?? new CameraCompositionMembershipContextId(membershipContextId),
                membership?.Revision ?? 0,
                membership?.Count ?? 0,
                added,
                removed,
                status,
                issue,
                presentationStatus);
            return Snapshot;
        }
    }
}
