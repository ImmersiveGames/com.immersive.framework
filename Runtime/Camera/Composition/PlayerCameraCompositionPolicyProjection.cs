using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Projects authored Player Slot to Camera Composition bindings into runtime identity.
    /// This is independent of Player Slot to Camera Output projection.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-031-B Player Slot to Camera Composition policy projection.")]
    internal static class PlayerCameraCompositionPolicyProjection
    {
        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraCompositionPolicyAuthoring> policies,
            out PlayerCameraCompositionTopology topology,
            out string diagnostic)
        {
            topology = null;
            policies ??= Array.Empty<PlayerCameraCompositionPolicyAuthoring>();

            PlayerCameraCompositionPolicyAuthoring policy = null;
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

            if (policyCount == 0)
            {
                topology = PlayerCameraCompositionTopology.Empty;
                diagnostic = string.Empty;
                return true;
            }

            if (policyCount > 1)
            {
                diagnostic =
                    $"Persistent Content requires at most one Player Camera Composition Policy, but found '{policyCount}'.";
                return false;
            }

            IReadOnlyList<PlayerCameraCompositionBindingAuthoring> authoredBindings =
                policy.Bindings;
            if (authoredBindings == null || authoredBindings.Count == 0)
            {
                diagnostic =
                    "Player Camera Composition Policy requires at least one explicit Player Slot to Composition binding.";
                return false;
            }

            var projected = new List<PlayerCameraCompositionBinding>(authoredBindings.Count);
            for (int index = 0; index < authoredBindings.Count; index++)
            {
                PlayerCameraCompositionBindingAuthoring authored = authoredBindings[index];
                if (authored == null)
                {
                    diagnostic =
                        $"Player Camera Composition Policy contains a missing binding at index '{index}'.";
                    return false;
                }

                PlayerSlotProfile profile = authored.PlayerSlotProfile;
                if (profile == null)
                {
                    diagnostic =
                        $"Player Camera Composition binding at index '{index}' requires a valid PlayerSlotProfile.";
                    return false;
                }

                if (!profile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string slotIssue))
                {
                    diagnostic =
                        $"Player Camera Composition binding at index '{index}' requires a valid PlayerSlotProfile. {slotIssue}";
                    return false;
                }

                CameraSharedComposition composition = authored.Composition;
                if (composition == null)
                {
                    diagnostic =
                        $"Player Camera Composition binding for Slot '{playerSlotId.StableText}' requires a Camera Composition.";
                    return false;
                }

                if (composition.SubjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
                {
                    diagnostic =
                        $"Player Camera Composition binding for Slot '{playerSlotId.StableText}' requires Composition '{composition.name}' to use ExplicitSelection. " +
                        $"Current policy is '{composition.SubjectPolicy}'.";
                    return false;
                }

                projected.Add(new PlayerCameraCompositionBinding(playerSlotId, composition));
            }

            return PlayerCameraCompositionTopology.TryCreate(
                projected,
                out topology,
                out diagnostic);
        }
    }
}
