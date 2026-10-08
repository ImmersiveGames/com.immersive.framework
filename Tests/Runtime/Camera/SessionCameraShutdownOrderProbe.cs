using System;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class SessionCameraShutdownOrderObservation
    {
        public int DisableCallbackCount { get; set; }

        public bool SawActiveAssignmentOnOccurrenceDisable { get; set; }

        public bool SawFallbackCoveringOnOccurrenceDisable { get; set; }

        public bool SawOutputRegisteredOnOccurrenceDisable { get; set; }

        public bool SawFallbackInfrastructureAvailableOnOccurrenceDisable { get; set; }

        public string OutputStatesAtDisable { get; set; }
    }

    [ExecuteAlways]
    public sealed class SessionCameraShutdownOrderProbe : MonoBehaviour
    {
        public CameraOutputSessionTopology Topology { get; set; }

        public CameraOutputAuthoring[] Outputs { get; set; }

        public Action ReenterDispose { get; set; }

        public bool ExpectedFallbackCovering { get; set; }

        public SessionCameraShutdownOrderObservation Observation { get; set; }

        private void OnDisable()
        {
            if (Observation == null)
            {
                return;
            }

            Observation.DisableCallbackCount++;
            if (Outputs == null || Outputs.Length == 0 || Topology == null)
            {
                Observation.OutputStatesAtDisable = "Probe configuration is incomplete.";
                return;
            }

            bool allAssignmentsActive = true;
            bool allOutputsRegistered = true;
            bool allFallbackInfrastructureAvailable = true;
            bool allFallbackStatesPreserved = true;
            var assignmentStates = new string[Outputs.Length];
            for (int index = 0; index < Outputs.Length; index++)
            {
                CameraOutputAuthoring outputAuthoring = Outputs[index];
                CameraOutputSession output = outputAuthoring != null
                    ? outputAuthoring.Session
                    : null;
                bool assignmentActive = output != null && output.OutputState.HasActiveAssignment;
                bool fallbackCovering = output != null && output.OutputState.IsFallbackCovering;
                bool fallbackAvailable = output != null && output.OutputState.IsFallbackAvailable &&
                    outputAuthoring.FallbackCameraRig != null;
                bool outputRegistered = output != null && outputAuthoring.IsInitialized &&
                    Topology.TryGetOutput(outputAuthoring.OutputId,
                        out CameraOutputAuthoring registeredOutput, out _) &&
                    object.ReferenceEquals(registeredOutput, outputAuthoring);
                allAssignmentsActive &= output != null && output.OutputState.HasActiveAssignment;
                assignmentStates[index] = outputAuthoring == null
                    ? "<null Output authoring>"
                    : $"{outputAuthoring.OutputIdText}: session={(output != null)}, activeAssignment={assignmentActive}, assignmentId={(output != null ? output.OutputState.ActiveAssignmentId.ToString() : "<none>")}, hasPresentedNormal={(output != null && output.OutputState.HasPresentedNormalOccurrence)}, presentedNormal={(output != null ? output.OutputState.PresentedNormalOccurrence.ToString() : "<none>")}, hasRetainedNormal={(output != null && output.OutputState.HasRetainedNormalOccurrence)}, retainedNormal={(output != null ? output.OutputState.RetainedNormalOccurrence.ToString() : "<none>")}, fallbackAvailable={(output != null && output.OutputState.IsFallbackAvailable)}, fallbackCovering={fallbackCovering}, fallbackRigAvailable={(outputAuthoring.FallbackCameraRig != null)}, appliedCamera={(outputAuthoring.Applicator != null && outputAuthoring.Applicator.AppliedCamera != null ? outputAuthoring.Applicator.AppliedCamera.name : "<none>")}, appliedCameraEnabled={(outputAuthoring.Applicator != null && outputAuthoring.Applicator.AppliedCamera != null && outputAuthoring.Applicator.AppliedCamera.enabled)}, hasAppliedFallback={(outputAuthoring.Applicator != null && outputAuthoring.Applicator.HasAppliedFallback)}, ownerCount={(output != null ? output.FallbackCoverageOwnerCount : -1)}, registered={outputRegistered}";
                allFallbackInfrastructureAvailable &= fallbackAvailable;
                allFallbackStatesPreserved &= output != null &&
                    output.OutputState.IsFallbackCovering == ExpectedFallbackCovering;
                allOutputsRegistered &= outputRegistered;
            }

            Observation.SawActiveAssignmentOnOccurrenceDisable = allAssignmentsActive;
            Observation.SawFallbackCoveringOnOccurrenceDisable = allFallbackStatesPreserved;
            Observation.SawOutputRegisteredOnOccurrenceDisable = allOutputsRegistered;
            Observation.SawFallbackInfrastructureAvailableOnOccurrenceDisable =
                allFallbackInfrastructureAvailable;
            Observation.OutputStatesAtDisable = string.Join("; ", assignmentStates);
            ReenterDispose?.Invoke();
        }
    }
}
