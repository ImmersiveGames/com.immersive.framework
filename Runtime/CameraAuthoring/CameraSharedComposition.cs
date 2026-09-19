using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Runtime authority for one Camera Composition. Productive Subject membership is
    /// Composition-scoped; the View definition remains only as a temporary legacy association.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Shared Camera Composition")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-C Composition request participation.")]
    public sealed class CameraSharedComposition : MonoBehaviour,
        ICameraSubjectAvailabilityConsumer,
        ICameraOutputSessionConsumer,
        ICameraOutputDefinitionConsumer,
        ICameraViewOutputBindingConsumer
    {
        [Header("Logical View (legacy association until CAMERA-029-E)")]
        [SerializeField] private CameraViewDefinition viewDefinition;
        private readonly string membershipContextId = Guid.NewGuid().ToString("N");

        [Header("Subject Selection")]
        [SerializeField] private CameraSharedCompositionSubjectPolicyKind subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;

        [Header("Logical View / Output Association")]
        [SerializeField] private CameraOutputDefinition outputDefinition;

        [Header("Composition Rig")]
        [SerializeField] private CameraRigComposer compositionRig;

        [Header("Request Arbitration")]
        [SerializeField] private int requestPrecedence;

        private readonly string requestId = Guid.NewGuid().ToString("N");

        private ICameraSubjectAvailabilitySource _availability;
        private CameraCompositionMembershipContext _membership;
        private CameraCompositionMembershipSnapshot _currentMembership;
        private bool _subscribed;
        private CameraOutputAuthoring _output;
        private ICameraRequestPublisher _requestPublisher;

        public CameraViewDefinition ViewDefinition => viewDefinition;
        public CameraOutputDefinition OutputDefinition => outputDefinition;
        public CameraRigComposer CompositionRig => compositionRig;
        public int RequestPrecedence => requestPrecedence;
        public CameraRequestId RequestId => new CameraRequestId(requestId);
        public bool IsRequestPublished => _requestPublisher?.IsPublished == true;
        public string MembershipContextIdText => membershipContextId;
        public CameraViewId ViewId => viewDefinition != null && viewDefinition.HasValidId
            ? viewDefinition.ViewId : default;
        public string ViewIdText => ViewId.Value ?? string.Empty;
        CameraViewId ICameraViewOutputBindingConsumer.RequestedViewId => new CameraViewId(ViewIdText);
        public string OutputIdText => outputDefinition != null && outputDefinition.HasValidId
            ? outputDefinition.OutputId.Value : string.Empty;
        public CameraOutputId RequestedOutputId => new CameraOutputId(OutputIdText);
        public CameraOutputAuthoring Output => _output;
        public CameraSharedCompositionSnapshot Snapshot { get; private set; }

        public void Configure(CameraViewDefinition view, CameraOutputDefinition output,
            CameraSharedCompositionSubjectPolicyKind policy)
        {
            if (_membership != null || _subscribed)
                throw new InvalidOperationException("An active shared Camera composition cannot be reconfigured.");
            CameraDefinitionValidation.ValidateViews(new[] { view });
            CameraDefinitionValidation.ValidateOutputs(new[] { output });
            if (!ReferenceEquals(outputDefinition, output)) _output = null;
            viewDefinition = view;
            outputDefinition = output;
            subjectPolicy = policy;
            TryStartComposition();
        }

        public bool TryCreateAssociationBinding(out CameraViewOutputBinding binding, out string diagnostic)
        {
            if (!TryValidateDefinitions(out diagnostic))
            {
                binding = default;
                return false;
            }
            binding = new CameraViewOutputBinding(viewDefinition.ViewId, outputDefinition.OutputId);
            diagnostic = string.Empty;
            return true;
        }

        public void AttachCameraSubjectAvailability(ICameraSubjectAvailabilitySource availabilitySource)
        {
            if (availabilitySource == null) throw new ArgumentNullException(nameof(availabilitySource));
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
            if (!TryValidateDefinitions(out string diagnostic)) throw new InvalidOperationException(diagnostic);
            if (!ReferenceEquals(binding.OutputDefinition, outputDefinition) || binding.OutputId != RequestedOutputId)
                throw new InvalidOperationException(
                    $"Shared Camera composition '{name}' requires its exact Output definition '{outputDefinition.name}'; the injected physical Output references a different definition.");
            StopComposition();
            _output = binding;
            TryStartComposition();
        }

        public void DetachOutputSession(string reason)
        {
            StopComposition();
            _output = null;
        }

        public CameraSharedCompositionSnapshot Reconcile(CameraSubjectAvailabilitySnapshot availability)
        {
            if (_membership == null)
                return Record(CameraSharedCompositionReconcileStatus.BlockedInvalidMembership, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, "Shared Camera composition has no active membership context.");
            if (!TryValidateCompositionRig(out string rigDiagnostic))
                return Record(ReferenceEquals(compositionRig, _output?.DefaultCameraRig)
                        ? CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault
                        : CameraSharedCompositionReconcileStatus.BlockedInvalidComposer,
                    availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, rigDiagnostic);

            IReadOnlyList<CameraSubjectId> desired = availability != null &&
                availability.ContextId == _membership.AvailabilityContextId
                    ? CameraSharedCompositionSubjectPolicy.SelectDesiredSubjects(subjectPolicy, availability)
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
                    CameraViewPresentationApplyStatus.None, membership.Message);
            }

            CameraViewPresentationInputResult projection = CameraViewPresentationInputProjection.TryCreate(_currentMembership);
            if (!projection.Succeeded)
            {
                if (!TryReleaseRequest(out string releaseDiagnostic))
                    return Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability,
                        membership.AddedCount, membership.RemovedCount,
                        CameraViewPresentationApplyStatus.None, releaseDiagnostic);
                CameraViewPresentationApplyStatus clearStatus =
                    compositionRig.ClearViewPresentation().Status;
                return Record(CameraSharedCompositionReconcileStatus.BlockedProjectionFailure, availability,
                    membership.AddedCount, membership.RemovedCount, clearStatus, projection.Message);
            }

            CameraViewTargetProjectionResult targetProjection =
                compositionRig.ResolveViewPresentationTargets(projection.Input);
            if (!targetProjection.Succeeded)
            {
                if (!TryReleaseRequest(out string releaseDiagnostic))
                    return Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability,
                        membership.AddedCount, membership.RemovedCount, CameraViewPresentationApplyStatus.None, releaseDiagnostic);

                CameraViewPresentationApplyStatus clearStatus =
                    compositionRig.ClearViewPresentation().Status;
                if (targetProjection.Status == CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing &&
                    projection.Input.SubjectCount == 0)
                {
                    return Record(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects, availability,
                        membership.AddedCount, membership.RemovedCount, clearStatus, string.Empty);
                }

                return Record(CameraSharedCompositionReconcileStatus.BlockedProjectionFailure, availability,
                    membership.AddedCount, membership.RemovedCount, clearStatus, targetProjection.BlockingIssue);
            }

            CameraViewPresentationApplyResult presentation =
                compositionRig.ApplyCompositionPresentation(projection.Input, _currentMembership);
            if (!presentation.Succeeded)
            {
                if (!TryReleaseRequest(out string releaseDiagnostic))
                    return Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability,
                        membership.AddedCount, membership.RemovedCount, presentation.Status, releaseDiagnostic);
                CameraViewPresentationApplyStatus clearStatus =
                    compositionRig.ClearViewPresentation().Status;
                return Record(CameraSharedCompositionReconcileStatus.BlockedPresentationFailure, availability,
                    membership.AddedCount, membership.RemovedCount, clearStatus, presentation.Diagnostic);
            }

            if (!TryEnsureRequestPublished(out string requestDiagnostic))
            {
                compositionRig.ClearViewPresentation();
                return Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability,
                    membership.AddedCount, membership.RemovedCount, presentation.Status, requestDiagnostic);
            }

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
                CameraDefinitionValidation.ValidateViews(new[] { viewDefinition });
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
            CameraSubjectAvailabilitySnapshot availability = _availability.CreateSnapshot();
            if (!TryValidateDefinitions(out string definitionDiagnostic))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidMembership, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, definitionDiagnostic);
                return;
            }
            if (subjectPolicy != CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedMembershipFailure, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, "Shared Camera composition requires an explicitly supported Subject selection policy.");
                return;
            }
            if (!RequestedOutputId.IsValid || _output == null ||
                !ReferenceEquals(_output.OutputDefinition, outputDefinition) || _output.OutputId != RequestedOutputId ||
                _output.DefaultCameraRig == null)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, "Shared Camera composition requires its exact injected Output and an explicit Default Camera Rig.");
                return;
            }
            if (compositionRig == null)
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None,
                    "Shared Camera composition requires an explicit Composition Camera Rig.");
                return;
            }
            if (ReferenceEquals(compositionRig, _output.DefaultCameraRig))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None,
                    "Composition Camera Rig must be distinct from the Output Default Camera Rig.");
                return;
            }
            if (!_output.TryGetSession(out _, out string sessionDiagnostic))
            {
                Record(CameraSharedCompositionReconcileStatus.BlockedRequestFailure, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, sessionDiagnostic);
                return;
            }

            _membership = new CameraCompositionMembershipContext(
                new CameraCompositionMembershipContextId(membershipContextId), _availability.ContextId);
            _currentMembership = _membership.Snapshot;
            _availability.AvailabilityChanged += HandleAvailabilityChanged;
            _subscribed = true;
            Reconcile(availability);
        }

        private void StopComposition()
        {
            bool wasActive = _membership != null;
            if (_subscribed && _availability != null)
                _availability.AvailabilityChanged -= HandleAvailabilityChanged;
            _subscribed = false;

            bool requestReleased = TryReleaseRequest(out string requestDiagnostic);
            CameraViewPresentationApplyStatus clearStatus = CameraViewPresentationApplyStatus.None;
            if (wasActive && compositionRig != null)
                clearStatus = compositionRig.ClearViewPresentation().Status;

            int removed = 0;
            if (_membership != null)
            {
                CameraCompositionMembershipResult cleared = _membership.Clear();
                _currentMembership = cleared.Snapshot;
                removed = cleared.RemovedCount;
            }
            if (wasActive)
                Record(requestReleased
                        ? CameraSharedCompositionReconcileStatus.SucceededStopped
                        : CameraSharedCompositionReconcileStatus.BlockedRequestFailure,
                    _availability?.CreateSnapshot(),
                    0, removed, clearStatus, requestDiagnostic);
            _membership = null;
            _currentMembership = null;
            if (_requestPublisher?.IsPublished != true)
                _requestPublisher = null;
        }

        private void HandleAvailabilityChanged(CameraSubjectAvailabilitySnapshot availability) => Reconcile(availability);

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

        private bool TryEnsureRequestPublished(out string diagnostic)
        {
            if (_requestPublisher != null)
            {
                if (_requestPublisher.Request.Rig.Composer != compositionRig ||
                    _requestPublisher.Request.OutputId != RequestedOutputId)
                {
                    diagnostic = "Active Composition request evidence does not match the configured Rig and Output.";
                    return false;
                }

                CameraRequestPublisherResult preserved = _requestPublisher.Publish();
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
            if (!publication.Succeeded)
            {
                diagnostic = publication.DiagnosticSummary;
                return false;
            }

            _requestPublisher = creation.Publisher;
            diagnostic = publication.DiagnosticSummary;
            return true;
        }

        private bool TryReleaseRequest(out string diagnostic)
        {
            if (_requestPublisher == null)
            {
                diagnostic = string.Empty;
                return true;
            }

            CameraRequestPublisherResult release = _requestPublisher.Release();
            diagnostic = release.DiagnosticSummary;
            return release.Succeeded;
        }

        private CameraSharedCompositionSnapshot Record(
            CameraSharedCompositionReconcileStatus status,
            CameraSubjectAvailabilitySnapshot availability,
            int added,
            int removed,
            CameraViewPresentationApplyStatus presentationStatus,
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
