using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    internal static class PlayerCameraOutputPolicyProjection
    {
        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraOutputPolicyAuthoring> policies,
            CameraOutputSessionTopology outputs,
            PlayerParticipationSnapshot playerSession,
            bool requireCompleteSlotCoverage,
            out PlayerCameraOutputTopology topology,
            out string diagnostic)
        {
            topology = null;
            policies ??= Array.Empty<PlayerCameraOutputPolicyAuthoring>();

            PlayerCameraOutputPolicyAuthoring policy = null;
            int policyCount = 0;
            for (int index = 0; index < policies.Count; index++)
            {
                if (policies[index] == null)
                {
                    continue;
                }

                policy = policies[index];
                policyCount++;
            }

            if (policyCount > 1)
            {
                diagnostic =
                    $"Persistent Content requires at most one Player Camera Output Policy, but found '{policyCount}'.";
                return false;
            }

            if (requireCompleteSlotCoverage && policy == null)
            {
                diagnostic =
                    "PlayerInputManager automatic split-screen requires one explicit Player Camera Output Policy covering every configured Player Slot.";
                return false;
            }

            IReadOnlyList<PlayerCameraOutputBindingAuthoring> authoredBindings =
                policy != null
                    ? policy.Bindings
                    : Array.Empty<PlayerCameraOutputBindingAuthoring>();
            if (policy != null &&
                (authoredBindings == null || authoredBindings.Count == 0))
            {
                diagnostic =
                    "Player Camera Output Policy requires at least one explicit Player Slot to Output binding.";
                return false;
            }

            return TryCreate(
                authoredBindings,
                outputs,
                playerSession,
                requireCompleteSlotCoverage,
                out topology,
                out diagnostic);
        }

        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraOutputBindingAuthoring> authoredBindings,
            CameraOutputSessionTopology outputs,
            PlayerParticipationSnapshot playerSession,
            bool requireCompleteSlotCoverage,
            out PlayerCameraOutputTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (outputs == null)
            {
                diagnostic =
                    "Player Camera Output projection requires the current Camera Output Session topology.";
                return false;
            }

            authoredBindings ??=
                Array.Empty<PlayerCameraOutputBindingAuthoring>();

            if (requireCompleteSlotCoverage &&
                authoredBindings.Count == 0)
            {
                diagnostic =
                    "PlayerInputManager automatic split-screen requires explicit GameApplication Camera Session Player Slot -> Output bindings covering every configured Player Slot.";
                return false;
            }

            var projected = new List<PlayerCameraOutputBinding>(
                authoredBindings.Count);
            var outputDefinitions = new List<CameraOutputDefinition>(
                authoredBindings.Count);

            for (int index = 0;
                 index < authoredBindings.Count;
                 index++)
            {
                PlayerCameraOutputBindingAuthoring authored =
                    authoredBindings[index];
                if (authored == null)
                {
                    diagnostic =
                        $"GameApplication Camera Session contains a missing Player Output binding at index '{index}'.";
                    return false;
                }

                PlayerSlotProfile profile =
                    authored.PlayerSlotProfile;
                if (profile == null)
                {
                    diagnostic =
                        $"Camera Session Player Output binding at index '{index}' requires a valid PlayerSlotProfile.";
                    return false;
                }

                if (!profile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string slotIssue))
                {
                    diagnostic =
                        $"Camera Session Player Output binding at index '{index}' requires a valid PlayerSlotProfile. {slotIssue}";
                    return false;
                }

                CameraOutputDefinition outputDefinition =
                    authored.OutputDefinition;
                if (outputDefinition == null ||
                    !outputDefinition.HasValidId)
                {
                    diagnostic =
                        $"Camera Session Player Output binding for Slot '{playerSlotId.StableText}' requires a valid CameraOutputDefinition.";
                    return false;
                }

                if (!outputs.TryGetOutput(
                        outputDefinition.OutputId,
                        out CameraOutputAuthoring physicalOutput,
                        out string outputIssue) ||
                    !ReferenceEquals(
                        physicalOutput.OutputDefinition,
                        outputDefinition))
                {
                    diagnostic =
                        $"Camera Session Player Output binding for Slot '{playerSlotId.StableText}' has no exact physical Output for its CameraOutputDefinition in the current Session topology. {outputIssue}";
                    return false;
                }

                outputDefinitions.Add(outputDefinition);
                projected.Add(
                    new PlayerCameraOutputBinding(
                        playerSlotId,
                        outputDefinition.OutputId));
            }

            try
            {
                CameraDefinitionValidation.ValidateOutputs(
                    outputDefinitions);
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = exception.Message;
                return false;
            }

            if (!PlayerCameraOutputTopology.TryCreate(
                    projected,
                    outputs,
                    out topology,
                    out diagnostic))
            {
                return false;
            }

            if (!requireCompleteSlotCoverage)
            {
                diagnostic = string.Empty;
                return true;
            }

            if (playerSession == null ||
                !playerSession.IsInitialized)
            {
                topology = null;
                diagnostic =
                    "PlayerInputManager automatic split-screen requires an initialized Framework Player Session.";
                return false;
            }

            for (int index = 0;
                 index < playerSession.Slots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot =
                    playerSession.Slots[index];
                if (!slot.IsValid ||
                    !topology.TryGetBinding(
                        slot.PlayerSlotId,
                        out _))
                {
                    topology = null;
                    diagnostic =
                        $"PlayerInputManager automatic split-screen requires an explicit Camera Output binding for configured Player Slot '{slot.PlayerSlotId.StableText}'.";
                    return false;
                }
            }

            diagnostic =
                $"PlayerInputManager automatic split-screen has explicit Camera Output coverage for '{playerSession.ConfiguredSlotCount}' configured Player Slot(s).";
            return true;
        }
    }
}
