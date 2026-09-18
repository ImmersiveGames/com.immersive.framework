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
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B composition-owned Subject membership.")]
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

        private ICameraSubjectAvailabilitySource _availability;
        private CameraCompositionMembershipContext _membership;
        private CameraCompositionMembershipSnapshot _currentMembership;
        private bool _subscribed;
        private CameraOutputAuthoring _output;

        public CameraViewDefinition ViewDefinition => viewDefinition;
        public CameraOutputDefinition OutputDefinition => outputDefinition;
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
            CameraRigComposer composer = _output != null ? _output.DefaultCameraRig : null;
            if (composer == null)
                return Record(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer, availability, 0, 0,
                    CameraViewPresentationApplyStatus.None, "Shared Camera composition requires an explicit Default Camera Rig on its bound Camera Output.");

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
                return Record(CameraSharedCompositionReconcileStatus.BlockedProjectionFailure, availability,
                    membership.AddedCount, membership.RemovedCount, CameraViewPresentationApplyStatus.None, projection.Message);

            CameraViewPresentationApplyResult presentation =
                composer.ApplyCompositionPresentation(projection.Input, _currentMembership);
            if (presentation.Status == CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing &&
                projection.Input.SubjectCount == 0)
                return Record(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects, availability,
                    membership.AddedCount, membership.RemovedCount, presentation.Status, string.Empty);
            if (!presentation.Succeeded)
                return Record(CameraSharedCompositionReconcileStatus.BlockedPresentationFailure, availability,
                    membership.AddedCount, membership.RemovedCount, presentation.Status, presentation.Diagnostic);

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

            int removed = 0;
            if (_membership != null)
            {
                CameraCompositionMembershipResult cleared = _membership.Clear();
                _currentMembership = cleared.Snapshot;
                removed = cleared.RemovedCount;
            }
            CameraViewPresentationApplyStatus clearStatus = CameraViewPresentationApplyStatus.None;
            CameraRigComposer composer = _output != null ? _output.DefaultCameraRig : null;
            if (wasActive && composer != null) clearStatus = composer.ClearViewPresentation().Status;
            if (wasActive)
                Record(CameraSharedCompositionReconcileStatus.SucceededStopped, _availability?.CreateSnapshot(),
                    0, removed, clearStatus, string.Empty);
            _membership = null;
            _currentMembership = null;
        }

        private void HandleAvailabilityChanged(CameraSubjectAvailabilitySnapshot availability) => Reconcile(availability);

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
