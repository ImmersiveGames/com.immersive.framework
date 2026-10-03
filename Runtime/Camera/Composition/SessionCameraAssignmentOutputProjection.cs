using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    /// <summary>Derives PlayerInput camera routing from Individual Session Camera Assignments.</summary>
    internal static class SessionCameraAssignmentOutputProjection
    {
        internal static bool TryCreate(
            IReadOnlyList<SessionCameraAssignmentAuthoring> assignments,
            CameraOutputSessionTopology outputs,
            PlayerParticipationSnapshot playerSession,
            bool requireCompleteSlotCoverage,
            out PlayerCameraOutputTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (outputs == null)
            {
                diagnostic = "Session Camera Assignment projection requires the current Camera Output Session topology.";
                return false;
            }

            assignments ??= Array.Empty<SessionCameraAssignmentAuthoring>();
            var bindings = new List<PlayerCameraOutputBinding>();
            var outputDefinitions = new List<CameraOutputDefinition>();
            bool hasIndividualAssignment = false;
            for (int index = 0; index < assignments.Count; index++)
            {
                SessionCameraAssignmentAuthoring authored = assignments[index];
                if (authored == null)
                {
                    diagnostic = $"Session Camera Assignments[{index}] is missing and cannot produce Player Output topology.";
                    return false;
                }

                if (!authored.TryBuild(out SessionCameraAssignment assignment, out diagnostic))
                {
                    diagnostic = $"Session Camera Assignments[{index}] cannot produce Player Output topology. {diagnostic}";
                    return false;
                }

                if (assignment.OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer)
                {
                    continue;
                }
                hasIndividualAssignment = true;

                for (int mappingIndex = 0; mappingIndex < assignment.MemberOutputs.Count; mappingIndex++)
                {
                    CameraPlayerOutputMapping mapping = assignment.MemberOutputs[mappingIndex];
                    if (!outputs.TryGetOutput(mapping.OutputId, out CameraOutputAuthoring physicalOutput, out string outputIssue))
                    {
                        diagnostic = $"Individual Assignment '{assignment.Id}' maps Slot '{mapping.PlayerSlotId.StableText}' to an unavailable Output. {outputIssue}";
                        return false;
                    }

                    CameraOutputDefinition definition = physicalOutput.OutputDefinition;
                    bool isExactAssignmentOutput = false;
                    IReadOnlyList<CameraOutputDefinition> assignmentOutputs = authored.OutputDefinitions;
                    for (int outputIndex = 0; outputIndex < assignmentOutputs.Count; outputIndex++)
                    {
                        if (ReferenceEquals(assignmentOutputs[outputIndex], definition))
                        {
                            isExactAssignmentOutput = true;
                            break;
                        }
                    }

                    if (definition == null || definition.OutputId != mapping.OutputId || !isExactAssignmentOutput)
                    {
                        diagnostic = $"Individual Assignment '{assignment.Id}' maps Slot '{mapping.PlayerSlotId.StableText}' to an Output without its exact configured definition.";
                        return false;
                    }

                    outputDefinitions.Add(definition);
                    bindings.Add(new PlayerCameraOutputBinding(mapping.PlayerSlotId, mapping.OutputId));
                }
            }

            try
            {
                CameraDefinitionValidation.ValidateOutputs(outputDefinitions);
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = exception.Message;
                return false;
            }

            if (!PlayerCameraOutputTopology.TryCreate(bindings, outputs, out topology, out diagnostic))
            {
                return false;
            }

            if (!requireCompleteSlotCoverage || !hasIndividualAssignment)
            {
                diagnostic = string.Empty;
                return true;
            }

            if (playerSession == null || !playerSession.IsInitialized)
            {
                topology = null;
                diagnostic = "PlayerInputManager automatic split-screen requires an initialized Framework Player Session.";
                return false;
            }

            for (int index = 0; index < playerSession.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = playerSession.Slots[index];
                if (!slot.IsValid || !topology.TryGetBinding(slot.PlayerSlotId, out _))
                {
                    topology = null;
                    diagnostic = $"PlayerInputManager automatic split-screen requires an Individual Session Camera Assignment Output mapping for configured Player Slot '{slot.PlayerSlotId.StableText}'.";
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}
