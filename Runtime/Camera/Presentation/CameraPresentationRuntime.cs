using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Runtime occurrence authority for one Camera Presentation.
    ///
    /// This type owns mutable composition state, Subject membership, presentation
    /// application, request participation and rollback. It intentionally has no
    /// MonoBehaviour lifecycle; a product-facing authoring adapter supplies the
    /// configuration and maps Unity enable/disable to SetEnabled.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-A extracted Camera Presentation runtime occurrence authority.")]
    internal sealed class CameraPresentationRuntime : IDisposable
    {
        private readonly string _membershipContextId = Guid.NewGuid().ToString("N");
        private readonly string _requestId = Guid.NewGuid().ToString("N");

        private string _diagnosticName;
        private CameraSharedCompositionSubjectPolicyKind _subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;
        private CameraOutputDefinition _outputDefinition;
        private CameraRigComposer _compositionRig;
        private int _requestPrecedence;
        private CameraPresentationTransitionMode _transitionMode =
            CameraPresentationTransitionMode.Blend;
        private RuntimeScopeContext _lifecycleContext;

        private bool _enabled;
        private bool _disposed;
        private ICameraSubjectAvailabilitySource _availability;
        private ICameraCompositionSubjectSelectionSource _selection;
        private CameraCompositionMembershipContext _membership;
        private CameraCompositionMembershipSnapshot _currentMembership;
        private bool _subscribed;
        private bool _selectionSubscribed;
        private int _consumedSelectionRevision = -1;
        private CameraOutputAuthoring _output;
        private ICameraRequestPublisher _requestPublisher;

        internal CameraPresentationRuntime(string diagnosticName)
        {
            _diagnosticName = NormalizeDiagnosticName(diagnosticName);
        }

        internal CameraRequestId RequestId => new CameraRequestId(_requestId);

        internal bool IsRequestPublished => _requestPublisher?.IsPublished == true;

        internal string MembershipContextIdText => _membershipContextId;

        internal CameraOutputAuthoring Output => _output;

        internal CameraPresentationTransitionMode TransitionMode =>
            _transitionMode;

        internal RuntimeContentScope LifecycleScope =>
            _lifecycleContext.IsValid
                ? _lifecycleContext.Scope
                : RuntimeContentScope.Unknown;

        internal ICameraCompositionSubjectSelectionSource SubjectSelectionSource =>
            _selection;

        internal CameraSharedCompositionSnapshot Snapshot { get; private set; }

        internal bool HasActiveState => _membership != null || _subscribed;

        internal CameraOutputId RequestedOutputId =>
            new CameraOutputId(OutputIdText);

        private string OutputIdText =>
            _outputDefinition != null && _outputDefinition.HasValidId
                ? _outputDefinition.OutputId.Value
                : string.Empty;

        internal void SetDiagnosticName(string diagnosticName)
        {
            _diagnosticName = NormalizeDiagnosticName(diagnosticName);
        }

        internal void Configure(
            CameraOutputDefinition outputDefinition,
            CameraSharedCompositionSubjectPolicyKind subjectPolicy,
            CameraRigComposer compositionRig,
            int requestPrecedence,
            CameraPresentationTransitionMode transitionMode,
            RuntimeScopeContext lifecycleContext = default)
        {
            ThrowIfDisposed();

            bool unchanged =
                ReferenceEquals(_outputDefinition, outputDefinition) &&
                _subjectPolicy == subjectPolicy &&
                ReferenceEquals(_compositionRig, compositionRig) &&
                _requestPrecedence == requestPrecedence &&
                _transitionMode == transitionMode &&
                _lifecycleContext.Equals(lifecycleContext);
            if (unchanged)
            {
                return;
            }

            if (HasActiveState)
            {
                throw new InvalidOperationException(
                    $"An active Camera Presentation '{_diagnosticName}' cannot be reconfigured.");
            }

            if (!ReferenceEquals(_outputDefinition, outputDefinition))
            {
                _output = null;
            }

            _outputDefinition = outputDefinition;
            _subjectPolicy = subjectPolicy;
            _compositionRig = compositionRig;
            _requestPrecedence = requestPrecedence;
            _transitionMode = transitionMode;
            _lifecycleContext = lifecycleContext;
        }

        internal void SetEnabled(bool enabled)
        {
            ThrowIfDisposed();

            _enabled = enabled;
            if (_enabled)
            {
                TryStartPresentation();
            }
            else
            {
                StopPresentation();
            }
        }

        internal void AttachCameraSubjectAvailability(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            ThrowIfDisposed();

            if (availabilitySource == null)
            {
                throw new ArgumentNullException(nameof(availabilitySource));
            }

            if (ReferenceEquals(_availability, availabilitySource))
            {
                TryStartPresentation();
                return;
            }

            if (!StopPresentation())
            {
                throw new InvalidOperationException(
                    $"Camera Presentation '{_diagnosticName}' could not replace Subject availability because teardown failed: {Snapshot.LastBlockingIssue}");
            }

            _availability = availabilitySource;
            TryStartPresentation();
        }

        internal void AttachSubjectSelectionSource(
            ICameraCompositionSubjectSelectionSource selectionSource)
        {
            ThrowIfDisposed();

            if (selectionSource == null)
            {
                throw new ArgumentNullException(nameof(selectionSource));
            }

            if (!selectionSource.ContextId.IsValid)
            {
                throw new ArgumentException(
                    "Camera Presentation Subject selection requires a valid context id.",
                    nameof(selectionSource));
            }

            if (ReferenceEquals(_selection, selectionSource))
            {
                TryStartPresentation();
                return;
            }

            if (_subjectPolicy ==
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                !StopPresentation())
            {
                throw new InvalidOperationException(
                    $"Camera Presentation '{_diagnosticName}' could not replace Subject selection because teardown failed: {Snapshot.LastBlockingIssue}");
            }

            _selection = selectionSource;
            _consumedSelectionRevision = -1;
            TryStartPresentation();
        }

        internal void DetachSubjectSelectionSource(string reason)
        {
            ThrowIfDisposed();

            if (_selection == null && !_selectionSubscribed)
            {
                return;
            }

            if (_subjectPolicy ==
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                !StopPresentation())
            {
                return;
            }

            _selection = null;
            _consumedSelectionRevision = -1;
        }

        internal void AttachOutputSession(CameraOutputAuthoring binding)
        {
            ThrowIfDisposed();

            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (!TryValidateDefinitions(out string diagnostic))
            {
                throw new InvalidOperationException(diagnostic);
            }

            if (!ReferenceEquals(binding.OutputDefinition, _outputDefinition) ||
                binding.OutputId != RequestedOutputId)
            {
                throw new InvalidOperationException(
                    $"Camera Presentation '{_diagnosticName}' requires its exact Output definition '{_outputDefinition.name}'; the injected physical Output references a different definition.");
            }

            if (!StopPresentation())
            {
                throw new InvalidOperationException(
                    $"Camera Presentation '{_diagnosticName}' could not replace its Output because teardown failed: {Snapshot.LastBlockingIssue}");
            }

            _output = binding;
            TryStartPresentation();
        }

        internal void DetachOutputSession(string reason)
        {
            ThrowIfDisposed();

            if (StopPresentation())
            {
                _output = null;
            }
        }

        internal CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability) =>
            Reconcile(availability, _selection?.CurrentSnapshot);

        internal CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            ThrowIfDisposed();

            if (_membership == null)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidMembership,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Camera Presentation has no active membership context.");
            }

            if (!TryValidateCompositionRig(out string rigDiagnostic))
            {
                return Record(
                    ReferenceEquals(_compositionRig, _output?.DefaultCameraRig)
                        ? CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault
                        : CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    rigDiagnostic);
            }

            if (!TryAcceptSubjectSelection(
                    selection,
                    out CameraSharedCompositionReconcileStatus selectionStatus,
                    out string selectionDiagnostic))
            {
                return Record(
                    selectionStatus,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    selectionDiagnostic);
            }

            CameraCompositionMembershipSnapshot previousMembership =
                _membership.Snapshot;
            CameraRigPresentationState previousPresentation =
                _compositionRig.CapturePresentationState();

            IReadOnlyList<CameraSubjectId> desired =
                availability != null &&
                availability.ContextId == _membership.AvailabilityContextId
                    ? CameraSharedCompositionSubjectPolicy.SelectDesiredSubjects(
                        _subjectPolicy,
                        availability,
                        selection)
                    : Array.Empty<CameraSubjectId>();

            CameraCompositionMembershipResult membership =
                _membership.Reconcile(availability, desired);
            _currentMembership = membership.Snapshot;

            if (!membership.Succeeded)
            {
                CameraSharedCompositionReconcileStatus status =
                    membership.Status switch
                    {
                        CameraCompositionMembershipStatus
                            .RejectedForeignAvailabilityContext =>
                            CameraSharedCompositionReconcileStatus
                                .RejectedForeignAvailabilityContext,
                        CameraCompositionMembershipStatus
                            .RejectedStaleAvailabilitySnapshot =>
                            CameraSharedCompositionReconcileStatus
                                .RejectedStaleAvailabilitySnapshot,
                        _ => CameraSharedCompositionReconcileStatus
                            .BlockedMembershipFailure
                    };

                return Record(
                    status,
                    availability,
                    membership.AddedCount,
                    membership.RemovedCount,
                    CameraRigPresentationApplyStatus.None,
                    membership.Message);
            }

            CameraCompositionPresentationInputResult projection =
                CameraCompositionPresentationInputProjection.TryCreate(
                    _currentMembership);
            if (!projection.Succeeded)
            {
                return RollbackAndRecord(
                    "presentation input projection",
                    CameraSharedCompositionReconcileStatus.BlockedProjectionFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    _compositionRig.CapturePresentationState(),
                    CameraRigPresentationApplyStatus.None,
                    false,
                    projection.Message);
            }

            CameraRigTargetProjectionResult targetProjection =
                _compositionRig.ResolvePresentationTargets(projection.Input);
            if (!targetProjection.Succeeded)
            {
                bool lostRequiredSubjects =
                    targetProjection.Status ==
                        CameraRigTargetProjectionStatus
                            .BlockedRequiredSubjectMissing &&
                    projection.Input.SubjectCount == 0;
                if (!lostRequiredSubjects)
                {
                    return RollbackAndRecord(
                        "Camera Presentation target projection",
                        CameraSharedCompositionReconcileStatus
                            .BlockedProjectionFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        _compositionRig.CapturePresentationState(),
                        CameraRigPresentationApplyStatus.None,
                        false,
                        targetProjection.BlockingIssue);
                }

                CameraRigPresentationApplyResult cleared =
                    _compositionRig.ClearPresentation();
                if (!cleared.Succeeded)
                {
                    return RollbackAndRecord(
                        "unpresentable presentation clear",
                        CameraSharedCompositionReconcileStatus
                            .BlockedPresentationFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        _compositionRig.CapturePresentationState(),
                        cleared.Status,
                        false,
                        cleared.Diagnostic);
                }

                if (!TryReleaseRequest(
                        out bool releaseOutputRollbackFailed,
                        out string releaseDiagnostic))
                {
                    return RollbackAndRecord(
                        "Camera Presentation request release",
                        CameraSharedCompositionReconcileStatus
                            .BlockedRequestFailure,
                        availability,
                        membership,
                        previousMembership,
                        previousPresentation,
                        _compositionRig.CapturePresentationState(),
                        cleared.Status,
                        releaseOutputRollbackFailed,
                        releaseDiagnostic);
                }

                CommitAcceptedSubjectSelection(selection);
                return Record(
                    CameraSharedCompositionReconcileStatus
                        .SucceededAwaitingSubjects,
                    availability,
                    membership.AddedCount,
                    membership.RemovedCount,
                    cleared.Status,
                    string.Empty);
            }

            CameraRigPresentationApplyResult presentation =
                _compositionRig.ApplyCompositionPresentation(
                    projection.Input,
                    _currentMembership);
            if (!presentation.Succeeded)
            {
                return RollbackAndRecord(
                    "Camera Presentation apply",
                    CameraSharedCompositionReconcileStatus
                        .BlockedPresentationFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    _compositionRig.CapturePresentationState(),
                    presentation.Status,
                    false,
                    presentation.Diagnostic);
            }

            if (!TryEnsureRequestPublished(
                    out bool admitOutputRollbackFailed,
                    out string requestDiagnostic))
            {
                return RollbackAndRecord(
                    "Camera Presentation request admission",
                    CameraSharedCompositionReconcileStatus.BlockedRequestFailure,
                    availability,
                    membership,
                    previousMembership,
                    previousPresentation,
                    _compositionRig.CapturePresentationState(),
                    presentation.Status,
                    admitOutputRollbackFailed,
                    requestDiagnostic);
            }

            CommitAcceptedSubjectSelection(selection);
            return Record(
                membership.Status ==
                    CameraCompositionMembershipStatus.SucceededNoChange
                    ? CameraSharedCompositionReconcileStatus.SucceededNoChange
                    : CameraSharedCompositionReconcileStatus.SucceededApplied,
                availability,
                membership.AddedCount,
                membership.RemovedCount,
                presentation.Status,
                string.Empty);
        }

        internal bool TryValidateDefinitions(out string diagnostic)
        {
            try
            {
                CameraDefinitionValidation.ValidateOutputs(
                    new[] { _outputDefinition });
                diagnostic = string.Empty;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                diagnostic =
                    $"Camera Presentation '{_diagnosticName}': {exception.Message}";
                return false;
            }
        }

        internal bool StopPresentation()
        {
            bool wasActive = _membership != null;
            UnsubscribeSources();

            if (!wasActive)
            {
                return true;
            }

            CameraCompositionMembershipSnapshot previousMembership =
                _membership.Snapshot;
            CameraRigPresentationState previousPresentation =
                _compositionRig != null
                    ? _compositionRig.CapturePresentationState()
                    : null;
            CameraRigPresentationApplyResult cleared =
                _compositionRig != null
                    ? _compositionRig.ClearPresentation()
                    : null;

            if (cleared == null || !cleared.Succeeded)
            {
                string clearDiagnostic =
                    cleared?.Diagnostic ??
                    "Camera Presentation teardown requires its explicit Composition Rig.";
                RollbackTeardownAndRecord(
                    "teardown presentation clear",
                    previousMembership,
                    previousPresentation,
                    _compositionRig != null
                        ? _compositionRig.CapturePresentationState()
                        : null,
                    cleared?.Status ?? CameraRigPresentationApplyStatus.None,
                    false,
                    clearDiagnostic);
                return false;
            }

            if (!TryReleaseRequest(
                    out bool outputRollbackFailed,
                    out string requestDiagnostic))
            {
                RollbackTeardownAndRecord(
                    "teardown request release",
                    previousMembership,
                    previousPresentation,
                    _compositionRig.CapturePresentationState(),
                    cleared.Status,
                    outputRollbackFailed,
                    requestDiagnostic);
                return false;
            }

            CameraCompositionMembershipResult membershipCleared =
                _membership.Clear();
            _currentMembership = membershipCleared.Snapshot;

            Record(
                CameraSharedCompositionReconcileStatus.SucceededStopped,
                _availability?.CreateSnapshot(),
                0,
                membershipCleared.RemovedCount,
                cleared.Status,
                string.Empty);

            _membership = null;
            _currentMembership = null;
            _requestPublisher = null;
            _consumedSelectionRevision = -1;
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _enabled = false;
            StopPresentation();
            _disposed = true;
        }

        private void TryStartPresentation()
        {
            if (!_enabled || _subscribed || _availability == null)
            {
                return;
            }

            if (_membership != null && !StopPresentation())
            {
                return;
            }

            CameraSubjectAvailabilitySnapshot availability =
                _availability.CreateSnapshot();

            if (!TryValidateDefinitions(out string definitionDiagnostic))
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedInvalidMembership,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    definitionDiagnostic);
                return;
            }

            if (_subjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects &&
                _subjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedMembershipFailure,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Camera Presentation requires an explicitly supported Subject selection policy.");
                return;
            }

            if (_subjectPolicy ==
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection &&
                _selection == null)
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedMissingSubjectSelection,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Explicit Camera Subject selection requires an attached selection source.");
                return;
            }

            if (!RequestedOutputId.IsValid ||
                _output == null ||
                !ReferenceEquals(
                    _output.OutputDefinition,
                    _outputDefinition) ||
                _output.OutputId != RequestedOutputId ||
                _output.DefaultCameraRig == null)
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedInvalidComposer,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Camera Presentation requires its exact injected Output and an explicit Default Camera Rig.");
                return;
            }

            if (_compositionRig == null)
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedInvalidComposer,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Camera Presentation requires an explicit Composition Camera Rig.");
                return;
            }

            if (ReferenceEquals(
                    _compositionRig,
                    _output.DefaultCameraRig))
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedCompositionRigIsDefault,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    "Composition Camera Rig must be distinct from the Output Default Camera Rig.");
                return;
            }

            if (!_output.TryGetSession(
                    out _,
                    out string sessionDiagnostic))
            {
                Record(
                    CameraSharedCompositionReconcileStatus
                        .BlockedRequestFailure,
                    availability,
                    0,
                    0,
                    CameraRigPresentationApplyStatus.None,
                    sessionDiagnostic);
                return;
            }

            _membership = new CameraCompositionMembershipContext(
                new CameraCompositionMembershipContextId(
                    _membershipContextId),
                _availability.ContextId);
            _currentMembership = _membership.Snapshot;

            _availability.AvailabilityChanged +=
                HandleAvailabilityChanged;
            _subscribed = true;

            if (_subjectPolicy ==
                CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                _selection.SelectionChanged += HandleSelectionChanged;
                _selectionSubscribed = true;
            }

            Reconcile(
                availability,
                _selection?.CurrentSnapshot);
        }

        private void UnsubscribeSources()
        {
            if (_subscribed && _availability != null)
            {
                _availability.AvailabilityChanged -=
                    HandleAvailabilityChanged;
            }

            if (_selectionSubscribed && _selection != null)
            {
                _selection.SelectionChanged -=
                    HandleSelectionChanged;
            }

            _subscribed = false;
            _selectionSubscribed = false;
        }

        private void HandleAvailabilityChanged(
            CameraSubjectAvailabilitySnapshot availability) =>
            Reconcile(
                availability,
                _selection?.CurrentSnapshot);

        private void HandleSelectionChanged(
            CameraCompositionSubjectSelectionSnapshot selection) =>
            Reconcile(
                _availability != null
                    ? _availability.CreateSnapshot()
                    : null,
                selection);

        private bool TryAcceptSubjectSelection(
            CameraCompositionSubjectSelectionSnapshot selection,
            out CameraSharedCompositionReconcileStatus status,
            out string diagnostic)
        {
            status = CameraSharedCompositionReconcileStatus.None;
            diagnostic = string.Empty;

            if (_subjectPolicy !=
                CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                return true;
            }

            if (_selection == null || selection == null)
            {
                status = CameraSharedCompositionReconcileStatus
                    .BlockedMissingSubjectSelection;
                diagnostic =
                    "Explicit Camera Subject selection requires an attached selection source.";
                return false;
            }

            if (selection.ContextId != _selection.ContextId)
            {
                status = CameraSharedCompositionReconcileStatus
                    .RejectedForeignSubjectSelectionContext;
                diagnostic =
                    "Subject selection evidence belongs to another Camera Presentation selection context.";
                return false;
            }

            if (selection.Revision < _consumedSelectionRevision)
            {
                status = CameraSharedCompositionReconcileStatus
                    .RejectedStaleSubjectSelectionSnapshot;
                diagnostic =
                    "Subject selection evidence regressed and cannot restore an older Subject occurrence.";
                return false;
            }

            return true;
        }

        private void CommitAcceptedSubjectSelection(
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            if (_subjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection ||
                selection == null ||
                selection.Revision <= _consumedSelectionRevision)
            {
                return;
            }

            _consumedSelectionRevision = selection.Revision;
        }

        private bool TryValidateCompositionRig(out string diagnostic)
        {
            if (_compositionRig == null)
            {
                diagnostic =
                    "Camera Presentation requires an explicit Composition Camera Rig.";
                return false;
            }

            if (_output == null || _output.DefaultCameraRig == null)
            {
                diagnostic =
                    "Camera Presentation requires an injected Output with an explicit Default Camera Rig.";
                return false;
            }

            if (ReferenceEquals(
                    _compositionRig,
                    _output.DefaultCameraRig))
            {
                diagnostic =
                    "Composition Camera Rig must be distinct from the Output Default Camera Rig.";
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
                if (_requestPublisher.Request.Rig.Composer !=
                        _compositionRig ||
                    _requestPublisher.Request.OutputId !=
                        RequestedOutputId)
                {
                    diagnostic =
                        "Active Camera Presentation request evidence does not match the configured Rig and Output.";
                    return false;
                }

                CameraRequestPublisherResult preserved =
                    _requestPublisher.Publish();
                outputRollbackFailed =
                    preserved.HasSessionResult &&
                    preserved.SessionResult.RollbackFailed;
                diagnostic = preserved.DiagnosticSummary;
                return preserved.Succeeded;
            }

            if (_output == null)
            {
                diagnostic =
                    "Camera Presentation request publication requires an injected Camera Output.";
                return false;
            }

            if (!_output.TryGetSession(
                    out CameraOutputSession session,
                    out diagnostic))
            {
                return false;
            }

            ResolveRequestLifecycle(
                out CameraRequestOwnerKind ownerKind,
                out CameraRequestLifetimeKind lifetimeKind,
                out string lifecycleScopeId);

            var scopeId =
                new CameraRequestOwnerScopeId(lifecycleScopeId);
            var lifetimeId =
                new CameraRequestLifetimeScopeId(lifecycleScopeId);

            CameraRequestCreateResult request =
                CameraRequestCreateResult.Create(
                    RequestId,
                    RequestedOutputId,
                    new CameraRequestOwner(
                        ownerKind,
                        scopeId),
                    new CameraRequestLifetime(
                        lifetimeKind,
                        lifetimeId),
                    CameraRigReference.FromComposer(_compositionRig),
                    CameraTargetSourceDescriptor.Logical(
                        CameraTargetSourceKind.Composition,
                        _membershipContextId,
                        "Camera Presentation"),
                    new CameraRequestPolicy(
                        _requestPrecedence,
                        _membershipContextId),
                    _transitionMode,
                    CameraRequestReleaseCondition.EligibilityLost,
                    nameof(CameraPresentationRuntime),
                    "Current Camera Presentation is presentable.");

            if (!request.IsSucceeded)
            {
                diagnostic = request.BlockingIssue;
                return false;
            }

            CameraRequestPublisherCreateResult creation =
                CreateRequestPublisher(
                    session,
                    request.Request,
                    ownerKind);
            if (!creation.Succeeded || creation.Publisher == null)
            {
                diagnostic = creation.DiagnosticSummary;
                return false;
            }

            CameraRequestPublisherResult publication =
                creation.Publisher.Publish();
            outputRollbackFailed =
                publication.HasSessionResult &&
                publication.SessionResult.RollbackFailed;

            if (!publication.Succeeded)
            {
                if (creation.Publisher.IsPublished)
                {
                    _requestPublisher = creation.Publisher;
                }

                diagnostic = publication.DiagnosticSummary;
                return false;
            }

            _requestPublisher = creation.Publisher;
            diagnostic = publication.DiagnosticSummary;
            return true;
        }

        private void ResolveRequestLifecycle(
            out CameraRequestOwnerKind ownerKind,
            out CameraRequestLifetimeKind lifetimeKind,
            out string scopeId)
        {
            if (!_lifecycleContext.IsValid)
            {
                ownerKind = CameraRequestOwnerKind.Composition;
                lifetimeKind = CameraRequestLifetimeKind.Composition;
                scopeId = _membershipContextId;
                return;
            }

            scopeId = _lifecycleContext.Owner.StableText;
            switch (_lifecycleContext.Scope)
            {
                case RuntimeContentScope.Session:
                    ownerKind = CameraRequestOwnerKind.Session;
                    lifetimeKind = CameraRequestLifetimeKind.Session;
                    return;
                case RuntimeContentScope.Route:
                    ownerKind = CameraRequestOwnerKind.Route;
                    lifetimeKind = CameraRequestLifetimeKind.Route;
                    return;
                case RuntimeContentScope.Activity:
                    ownerKind = CameraRequestOwnerKind.Activity;
                    lifetimeKind = CameraRequestLifetimeKind.Activity;
                    return;
                default:
                    ownerKind = CameraRequestOwnerKind.Composition;
                    lifetimeKind = CameraRequestLifetimeKind.Composition;
                    scopeId = _membershipContextId;
                    return;
            }
        }

        private static CameraRequestPublisherCreateResult
            CreateRequestPublisher(
                CameraOutputSession session,
                CameraRequest request,
                CameraRequestOwnerKind ownerKind)
        {
            switch (ownerKind)
            {
                case CameraRequestOwnerKind.Session:
                    return SessionCameraRequestPublisher.Create(
                        session,
                        request);
                case CameraRequestOwnerKind.Route:
                    return RouteCameraRequestPublisher.Create(
                        session,
                        request);
                case CameraRequestOwnerKind.Activity:
                    return ActivityCameraRequestPublisher.Create(
                        session,
                        request);
                default:
                    return CompositionCameraRequestPublisher.Create(
                        session,
                        request);
            }
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

            CameraRequestPublisherResult release =
                _requestPublisher.Release();
            outputRollbackFailed =
                release.HasSessionResult &&
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

            bool critical =
                outputRollbackFailed || !restored;
            string diagnostic =
                BuildTransactionFailureDiagnostic(
                    operation,
                    originalFailure,
                    outputRollbackFailed
                        ? $"CameraOutputSession rollback failed. {rollbackDiagnostic}"
                        : rollbackDiagnostic);

            return Record(
                critical
                    ? CameraSharedCompositionReconcileStatus
                        .CriticalRollbackFailure
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
            CameraCompositionMembershipSnapshot failedMembership =
                _membership.Snapshot;
            bool restored = TryRollbackState(
                previousMembership,
                failedMembership,
                previousPresentation,
                failedPresentation,
                out string rollbackDiagnostic);

            bool critical =
                outputRollbackFailed || !restored;
            string diagnostic =
                BuildTransactionFailureDiagnostic(
                    operation,
                    originalFailure,
                    outputRollbackFailed
                        ? $"CameraOutputSession rollback failed. {rollbackDiagnostic}"
                        : rollbackDiagnostic);

            return Record(
                critical
                    ? CameraSharedCompositionReconcileStatus
                        .CriticalRollbackFailure
                    : CameraSharedCompositionReconcileStatus
                        .BlockedRequestFailure,
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
                "Camera Presentation membership rollback authority is unavailable.";

            if (_membership == null ||
                !_membership.CanRestore(
                    previousMembership,
                    expectedCurrentMembership,
                    out membershipPreflightDiagnostic))
            {
                diagnostic =
                    $"Membership rollback preflight failed: {membershipPreflightDiagnostic}";
                return false;
            }

            if (_compositionRig == null ||
                previousPresentation == null ||
                expectedCurrentPresentation == null)
            {
                diagnostic =
                    "Presentation rollback preflight failed because exact Composer state is unavailable.";
                return false;
            }

            CameraRigPresentationRestoreResult presentationRestore =
                _compositionRig.RestorePresentationState(
                    previousPresentation,
                    expectedCurrentPresentation);
            if (!presentationRestore.Succeeded)
            {
                diagnostic =
                    $"Presentation rollback failed: {presentationRestore.Diagnostic}";
                return false;
            }

            if (!_membership.TryRestore(
                    previousMembership,
                    expectedCurrentMembership,
                    out string membershipRestoreDiagnostic))
            {
                diagnostic =
                    $"Membership rollback failed after presentation restore: {membershipRestoreDiagnostic}";
                return false;
            }

            _currentMembership = previousMembership;
            diagnostic =
                "Camera Presentation and membership rollback restored the previous coherent state.";
            return true;
        }

        private string BuildTransactionFailureDiagnostic(
            string operation,
            string originalFailure,
            string rollbackDiagnostic)
        {
            return
                $"Camera Presentation transaction failed. operation='{operation}' " +
                $"presentation='{_membershipContextId}' request='{RequestId}' output='{RequestedOutputId}' " +
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
            CameraCompositionMembershipSnapshot membership =
                _currentMembership ?? _membership?.Snapshot;

            Snapshot = new CameraSharedCompositionSnapshot(
                status is
                    CameraSharedCompositionReconcileStatus.SucceededApplied or
                    CameraSharedCompositionReconcileStatus.SucceededNoChange or
                    CameraSharedCompositionReconcileStatus
                        .SucceededAwaitingSubjects,
                _availability?.ContextId ??
                    availability?.ContextId ??
                    default,
                membership?.AvailabilityRevision ?? 0,
                membership?.ContextId ??
                    new CameraCompositionMembershipContextId(
                        _membershipContextId),
                membership?.Revision ?? 0,
                membership?.Count ?? 0,
                added,
                removed,
                status,
                issue,
                presentationStatus);

            return Snapshot;
        }

        private static string NormalizeDiagnosticName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? nameof(CameraPresentationRuntime)
                : value.Trim();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CameraPresentationRuntime));
            }
        }
    }
}
