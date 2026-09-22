using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Transitional Unity authoring adapter for one Camera Presentation occurrence.
    ///
    /// Serialized configuration remains here during CAMERA-032-A, while mutable
    /// membership, presentation, request and rollback state is owned by the
    /// non-MonoBehaviour CameraPresentationRuntime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Shared Camera Composition")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-A transitional scene adapter over CameraPresentationRuntime.")]
    public sealed class CameraSharedComposition : MonoBehaviour,
        ICameraSubjectAvailabilityConsumer,
        ICameraOutputSessionConsumer,
        ICameraOutputDefinitionConsumer
    {
        [Header("Subject Selection")]
        [SerializeField]
        private CameraSharedCompositionSubjectPolicyKind subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;

        [Header("Camera Output")]
        [SerializeField]
        private CameraOutputDefinition outputDefinition;

        [Header("Composition Rig")]
        [SerializeField]
        private CameraRigComposer compositionRig;

        [Header("Request Arbitration")]
        [SerializeField]
        private int requestPrecedence;

        private CameraPresentationRuntime _runtime;

        public CameraOutputDefinition OutputDefinition => outputDefinition;

        public CameraRigComposer CompositionRig => compositionRig;

        public int RequestPrecedence => requestPrecedence;

        public CameraRequestId RequestId => EnsureRuntime().RequestId;

        public bool IsRequestPublished =>
            _runtime?.IsRequestPublished == true;

        public string MembershipContextIdText =>
            EnsureRuntime().MembershipContextIdText;

        public string OutputIdText =>
            outputDefinition != null && outputDefinition.HasValidId
                ? outputDefinition.OutputId.Value
                : string.Empty;

        public CameraOutputId RequestedOutputId =>
            new CameraOutputId(OutputIdText);

        public CameraOutputAuthoring Output =>
            _runtime?.Output;

        public CameraSharedCompositionSubjectPolicyKind SubjectPolicy =>
            subjectPolicy;

        public ICameraCompositionSubjectSelectionSource SubjectSelectionSource =>
            _runtime?.SubjectSelectionSource;

        public CameraSharedCompositionSnapshot Snapshot =>
            _runtime != null
                ? _runtime.Snapshot
                : default;

        public void Configure(
            CameraOutputDefinition output,
            CameraSharedCompositionSubjectPolicyKind policy)
        {
            if (_runtime?.HasActiveState == true)
            {
                throw new InvalidOperationException(
                    "An active shared Camera composition cannot be reconfigured.");
            }

            CameraDefinitionValidation.ValidateOutputs(new[] { output });

            outputDefinition = output;
            subjectPolicy = policy;

            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
        }

        public void AttachCameraSubjectAvailability(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
            runtime.AttachCameraSubjectAvailability(availabilitySource);
        }

        public void AttachSubjectSelectionSource(
            ICameraCompositionSubjectSelectionSource selectionSource)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
            runtime.AttachSubjectSelectionSource(selectionSource);
        }

        public void DetachSubjectSelectionSource(string reason)
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.DetachSubjectSelectionSource(reason);
        }

        public void AttachOutputSession(CameraOutputAuthoring binding)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
            runtime.AttachOutputSession(binding);
        }

        public void DetachOutputSession(string reason)
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.DetachOutputSession(reason);
        }

        public CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
            return runtime.Reconcile(availability);
        }

        public CameraSharedCompositionSnapshot Reconcile(
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(isActiveAndEnabled);
            return runtime.Reconcile(availability, selection);
        }

        public bool TryValidateDefinitions(out string diagnostic)
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            return runtime.TryValidateDefinitions(out diagnostic);
        }

        private void OnEnable()
        {
            CameraPresentationRuntime runtime =
                EnsureRuntimeConfigured();
            runtime.SetEnabled(true);
        }

        private void OnDisable()
        {
            _runtime?.SetEnabled(false);
        }

        private void OnDestroy()
        {
            _runtime?.Dispose();
        }

        private CameraPresentationRuntime EnsureRuntime()
        {
            if (_runtime == null)
            {
                _runtime = new CameraPresentationRuntime(name);
            }

            _runtime.SetDiagnosticName(name);
            return _runtime;
        }

        private CameraPresentationRuntime EnsureRuntimeConfigured()
        {
            CameraPresentationRuntime runtime =
                EnsureRuntime();
            runtime.Configure(
                outputDefinition,
                subjectPolicy,
                compositionRig,
                requestPrecedence,
                CameraPresentationTransitionMode.Blend);
            return runtime;
        }
    }
}
