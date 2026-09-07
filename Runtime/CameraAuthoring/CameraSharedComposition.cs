using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Explicit runtime authority for one shared logical Camera View. It owns the View's
    /// scoped Assignment context and reconciliation state, but only references Subject
    /// availability and the physical Composer.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Shared Camera Composition")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E shared Camera View runtime composition.")]
    public sealed class CameraSharedComposition : MonoBehaviour,
        ICameraSubjectAvailabilityConsumer,
        ICameraOutputSessionConsumer,
        ICameraViewOutputBindingConsumer
    {
        [Header("Logical View")]
        [SerializeField] private string viewId;
        [SerializeField] private string viewDescription;
        [SerializeField] private string assignmentContextId;
        [SerializeField] private string assignmentOwnerId;

        [Header("Subject Selection")]
        [SerializeField]
        private CameraSharedCompositionSubjectPolicyKind subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;

        [Header("Presentation")]
        [SerializeField] private string outputId;
        [SerializeField] private CameraRigComposer composer;

        private ICameraSubjectAvailabilitySource _availability;
        private CameraViewAssignmentContext _assignments;
        private CameraViewAssignmentSnapshot _currentAssignments;
        private CameraView _view;
        private CameraSubjectAssignmentOwnerId _ownerId;
        private int _lastConsumedAvailabilityRevision = -1;
        private bool _subscribed;
        private CameraOutputAuthoring _output;

        public CameraViewId ViewId => _view.ViewId;
        public string ViewIdText => viewId.NormalizeText();
        CameraViewId ICameraViewOutputBindingConsumer.RequestedViewId =>
            new CameraViewId(ViewIdText);
        public string OutputIdText => outputId.NormalizeText();
        public CameraOutputId RequestedOutputId => new CameraOutputId(OutputIdText);
        public CameraOutputAuthoring Output => _output;
        public CameraRigComposer Composer => composer;
        public CameraSharedCompositionSnapshot Snapshot { get; private set; }

        public void Configure(
            CameraView view,
            string contextId,
            CameraSubjectAssignmentOwnerId ownerId,
            CameraOutputId targetOutputId,
            CameraRigComposer targetComposer,
            CameraSharedCompositionSubjectPolicyKind policy)
        {
            if (_assignments != null || _subscribed)
            {
                throw new InvalidOperationException(
                    "An active shared Camera composition cannot be reconfigured.");
            }

            viewId = view.ViewId.Value;
            viewDescription = view.Description;
            assignmentContextId = contextId.NormalizeText();
            assignmentOwnerId = ownerId.Value;
            outputId = targetOutputId.Value;
            composer = targetComposer;
            subjectPolicy = policy;
            _view = view;
            _ownerId = ownerId;
            TryStartComposition();
        }

        public void AttachCameraSubjectAvailability(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            if (availabilitySource == null)
            {
                throw new ArgumentNullException(nameof(availabilitySource));
            }
            if (ReferenceEquals(_availability, availabilitySource))
            {
                TryStartComposition();
                return;
            }

            StopComposition();
            _availability = availabilitySource;
            TryStartComposition();
        }

        public void AttachOutputSession(CameraOutputAuthoring binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (new CameraOutputId(binding.OutputIdText) != RequestedOutputId)
            {
                throw new InvalidOperationException(
                    $"Shared Camera composition requested output '{RequestedOutputId}' but received '{binding.OutputIdText}'.");
            }
            StopComposition();
            _output = binding;
            TryStartComposition();
        }

        public void DetachOutputSession(string reason)
        {
            StopComposition();
            _output = null;
        }

        public CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (_assignments == null || !_view.IsValid)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidView,
                    availability,
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition has no valid active View.");
            }
            if (composer == null)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    availability,
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires one explicitly bound CameraRigComposer.");
            }
            if (availability == null ||
                !string.Equals(
                    availability.ContextId,
                    _availability.ContextId,
                    StringComparison.Ordinal))
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.RejectedForeignAvailabilityContext,
                    availability,
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Availability snapshot is not from the explicitly bound Camera Subject context.");
            }
            if (availability.Revision < _lastConsumedAvailabilityRevision)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.RejectedStaleAvailabilitySnapshot,
                    availability,
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Availability snapshot revision regressed and cannot restore older membership.");
            }
            if (availability.Revision == _lastConsumedAvailabilityRevision)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.SucceededNoChange,
                    availability,
                    0,
                    0,
                    Snapshot.LastPresentationApplyStatus,
                    string.Empty);
            }

            IReadOnlyList<CameraSubjectId> desired =
                CameraSharedCompositionSubjectPolicy.SelectDesiredSubjects(
                    subjectPolicy,
                    availability);
            var desiredSet = new HashSet<CameraSubjectId>(desired);
            if (!_currentAssignments.TryGetView(
                    _view.ViewId,
                    out CameraViewSubjectSnapshot currentView))
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidView,
                    availability,
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "The explicitly configured View is absent from its owned Assignment context.");
            }

            for (int index = 0; index < currentView.Assignments.Count; index++)
            {
                CameraSubjectAssignment assignment = currentView.Assignments[index];
                if (desiredSet.Contains(assignment.SubjectId) &&
                    assignment.OwnerId != _ownerId)
                {
                    return Record(
                        CameraSharedCompositionReconcileStatus.BlockedAssignmentFailure,
                        availability,
                        0,
                        0,
                        CameraViewPresentationApplyStatus.None,
                        "A desired View-to-Subject relation is owned by another scope; ownership was not stolen.");
                }
            }

            int removedCount = 0;
            for (int index = 0; index < currentView.Assignments.Count; index++)
            {
                CameraSubjectAssignment assignment = currentView.Assignments[index];
                if (assignment.OwnerId != _ownerId ||
                    desiredSet.Contains(assignment.SubjectId))
                {
                    continue;
                }

                CameraViewAssignmentResult release =
                    _assignments.TryRelease(assignment.Token, availability);
                if (!release.Succeeded)
                {
                    return AssignmentFailure(availability, release, 0, removedCount);
                }
                _currentAssignments = release.Snapshot;
                removedCount++;
            }

            _currentAssignments.TryGetView(_view.ViewId, out currentView);
            var assigned = new HashSet<CameraSubjectId>();
            for (int index = 0; index < currentView.Assignments.Count; index++)
            {
                assigned.Add(currentView.Assignments[index].SubjectId);
            }

            int addedCount = 0;
            for (int index = 0; index < desired.Count; index++)
            {
                CameraSubjectId subjectId = desired[index];
                if (assigned.Contains(subjectId)) continue;

                CameraViewAssignmentResult assignment = _assignments.TryAssign(
                    _view.ViewId,
                    subjectId,
                    _ownerId,
                    availability);
                if (!assignment.Succeeded)
                {
                    return AssignmentFailure(
                        availability,
                        assignment,
                        addedCount,
                        removedCount);
                }
                _currentAssignments = assignment.Snapshot;
                addedCount++;
            }

            CameraViewAssignmentResult logical =
                _assignments.Reconcile(availability);
            if (!logical.Succeeded)
            {
                return AssignmentFailure(
                    availability,
                    logical,
                    addedCount,
                    removedCount);
            }
            _currentAssignments = logical.Snapshot;

            CameraViewPresentationInputResult projection =
                CameraViewPresentationInputProjection.TryCreate(
                    _currentAssignments,
                    _view.ViewId);
            if (!projection.Succeeded)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedProjectionFailure,
                    availability,
                    addedCount,
                    removedCount,
                    CameraViewPresentationApplyStatus.None,
                    projection.Message);
            }

            CameraViewPresentationApplyResult presentation =
                composer.ApplyViewPresentation(
                    projection.Input,
                    _currentAssignments);
            if (presentation.Status ==
                    CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing &&
                projection.Input.SubjectCount == 0)
            {
                _lastConsumedAvailabilityRevision = availability.Revision;
                return Record(
                    CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects,
                    availability,
                    addedCount,
                    removedCount,
                    presentation.Status,
                    string.Empty);
            }
            if (!presentation.Succeeded)
            {
                return Record(
                    CameraSharedCompositionReconcileStatus.BlockedPresentationFailure,
                    availability,
                    addedCount,
                    removedCount,
                    presentation.Status,
                    presentation.Diagnostic);
            }

            _lastConsumedAvailabilityRevision = availability.Revision;
            return Record(
                CameraSharedCompositionReconcileStatus.SucceededApplied,
                availability,
                addedCount,
                removedCount,
                presentation.Status,
                string.Empty);
        }

        private void OnEnable()
        {
            _view = new CameraView(new CameraViewId(viewId), viewDescription);
            _ownerId = new CameraSubjectAssignmentOwnerId(assignmentOwnerId);
            TryStartComposition();
        }

        private void OnDisable() => StopComposition();

        private void OnDestroy() => StopComposition();

        private void TryStartComposition()
        {
            if (!isActiveAndEnabled || _subscribed) return;

            _view = new CameraView(new CameraViewId(viewId), viewDescription);
            _ownerId = new CameraSubjectAssignmentOwnerId(assignmentOwnerId);
            if (_availability == null) return;
            if (!_view.IsValid)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidView,
                    _availability.CreateSnapshot(),
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires an explicit valid View id.");
                return;
            }
            if (string.IsNullOrEmpty(assignmentContextId.NormalizeText()) ||
                !_ownerId.IsValid)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedAssignmentFailure,
                    _availability.CreateSnapshot(),
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires explicit Assignment context and owner ids.");
                return;
            }
            if (subjectPolicy !=
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedAssignmentFailure,
                    _availability.CreateSnapshot(),
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires an explicitly supported Subject selection policy.");
                return;
            }
            if (!RequestedOutputId.IsValid || _output == null)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    _availability.CreateSnapshot(), 0, 0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires one explicit Camera Output ID and its exact injected Output.");
                return;
            }
            if (composer == null)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    _availability.CreateSnapshot(),
                    0,
                    0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires one explicitly bound CameraRigComposer.");
                return;
            }
            if (!ReferenceEquals(_output.DefaultCameraRig, composer))
            {
                Record(
                    CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    _availability.CreateSnapshot(), 0, 0,
                    CameraViewPresentationApplyStatus.None,
                    $"Shared Camera composition Output '{RequestedOutputId}' must use its explicitly authored Default Camera Rig as composer.");
                return;
            }

            _assignments = new CameraViewAssignmentContext(
                assignmentContextId,
                _availability.ContextId,
                _view);
            CameraViewAssignmentResult initial =
                _assignments.Reconcile(_availability.CreateSnapshot());
            _currentAssignments = initial.Snapshot;
            _lastConsumedAvailabilityRevision = -1;
            _availability.AvailabilityChanged += HandleAvailabilityChanged;
            _subscribed = true;
            Reconcile(_availability.CreateSnapshot());
        }

        private void StopComposition()
        {
            bool hadActiveView = _assignments != null;
            CameraSubjectAvailabilitySnapshot availability =
                _availability?.CreateSnapshot();
            int removedCount = 0;
            if (_subscribed && _availability != null)
            {
                _availability.AvailabilityChanged -= HandleAvailabilityChanged;
            }
            _subscribed = false;

            if (_assignments != null && _availability != null)
            {
                CameraViewAssignmentResult released = _assignments.ReleaseOwner(
                    _ownerId,
                    _availability.CreateSnapshot());
                if (released.Snapshot != null)
                {
                    _currentAssignments = released.Snapshot;
                }
                removedCount = released.RemovedCount;
            }
            CameraViewPresentationApplyStatus clearStatus =
                CameraViewPresentationApplyStatus.None;
            if (hadActiveView && composer != null)
            {
                clearStatus = composer.ClearViewPresentation().Status;
            }
            if (hadActiveView)
            {
                Record(
                    CameraSharedCompositionReconcileStatus.SucceededStopped,
                    availability,
                    0,
                    removedCount,
                    clearStatus,
                    string.Empty);
            }
            _assignments = null;
            _currentAssignments = null;
            _lastConsumedAvailabilityRevision = -1;
        }

        private void HandleAvailabilityChanged(
            CameraSubjectAvailabilitySnapshot availability)
        {
            Reconcile(availability);
        }

        private CameraSharedCompositionSnapshot AssignmentFailure(
            CameraSubjectAvailabilitySnapshot availability,
            CameraViewAssignmentResult result,
            int addedCount,
            int removedCount)
        {
            if (result.Snapshot != null) _currentAssignments = result.Snapshot;
            return Record(
                CameraSharedCompositionReconcileStatus.BlockedAssignmentFailure,
                availability,
                addedCount,
                removedCount,
                CameraViewPresentationApplyStatus.None,
                result.Message);
        }

        private CameraSharedCompositionSnapshot Record(
            CameraSharedCompositionReconcileStatus status,
            CameraSubjectAvailabilitySnapshot availability,
            int addedCount,
            int removedCount,
            CameraViewPresentationApplyStatus presentationStatus,
            string issue)
        {
            int subjectCount = 0;
            int assignmentRevision = _assignments?.Revision ?? 0;
            if (_currentAssignments != null &&
                _currentAssignments.TryGetView(
                    _view.ViewId,
                    out CameraViewSubjectSnapshot currentView))
            {
                subjectCount = currentView.ResolvedSubjectCount;
                assignmentRevision = _currentAssignments.Revision;
            }

            Snapshot = new CameraSharedCompositionSnapshot(
                status is
                    CameraSharedCompositionReconcileStatus.SucceededApplied or
                    CameraSharedCompositionReconcileStatus.SucceededNoChange or
                    CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects,
                _view.ViewId.Value,
                _availability?.ContextId ?? availability?.ContextId,
                status == CameraSharedCompositionReconcileStatus.RejectedStaleAvailabilitySnapshot
                    ? _lastConsumedAvailabilityRevision
                    : availability?.Revision ?? _lastConsumedAvailabilityRevision,
                assignmentContextId,
                assignmentRevision,
                subjectCount,
                addedCount,
                removedCount,
                status,
                issue,
                presentationStatus);
            return Snapshot;
        }
    }
}
