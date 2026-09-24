using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Explicit application-authored physical Camera capacity for one Session.
    ///
    /// The configuration owns only stable Session authoring: 1..N Output prefabs,
    /// optional Player Slot -> Output bindings and optional Player Slot -> Presentation
    /// explicit-selection bindings. Materialized occurrences, Camera requests and
    /// Player runtime state remain outside this object.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-D/E explicit Session Camera capacity and Player integration configuration on GameApplication.")]
    public sealed class CameraSessionConfiguration
    {
        [SerializeField]
        [Tooltip("Explicit 1..N physical Camera Output prefabs materialized once for the Session. Each prefab must contain exactly one CameraOutputAuthoring with its Unity Camera, CinemachineBrain and persistent Default Rig.")]
        private List<GameObject> outputPrefabs =
            new List<GameObject>();

        [SerializeField]
        [Tooltip("Optional explicit Player Slot -> Camera Output bindings for this Session. Bindings reference configured Output definitions; Player count never creates Outputs.")]
        private List<PlayerCameraOutputBindingAuthoring> playerOutputBindings =
            new List<PlayerCameraOutputBindingAuthoring>();

        [SerializeField]
        [Tooltip("Optional Player Slot -> Camera Presentation bindings. Each bound Presentation must use ExplicitSelection and target the same exact Output configured for that Player Slot.")]
        private List<PlayerCameraPresentationBindingAuthoring>
            playerPresentationBindings =
                new List<PlayerCameraPresentationBindingAuthoring>();

        public IReadOnlyList<GameObject> OutputPrefabs
        {
            get
            {
                if (outputPrefabs != null)
                {
                    return outputPrefabs;
                }

                return Array.Empty<GameObject>();
            }
        }

        public IReadOnlyList<PlayerCameraOutputBindingAuthoring>
            PlayerOutputBindings
        {
            get
            {
                if (playerOutputBindings != null)
                {
                    return playerOutputBindings;
                }

                return Array.Empty<PlayerCameraOutputBindingAuthoring>();
            }
        }

        public IReadOnlyList<PlayerCameraPresentationBindingAuthoring>
            PlayerPresentationBindings
        {
            get
            {
                if (playerPresentationBindings != null)
                {
                    return playerPresentationBindings;
                }

                return Array.Empty<PlayerCameraPresentationBindingAuthoring>();
            }
        }

        public bool HasOutputs =>
            outputPrefabs != null &&
            outputPrefabs.Count > 0;

        public bool TryValidate(out string issue)
        {
            IReadOnlyList<GameObject> prefabs = OutputPrefabs;
            if (prefabs.Count == 0)
            {
                issue =
                    "Camera Session configuration requires at least one explicit Camera Output prefab.";
                return false;
            }

            if (prefabs.Count >
                CameraCinemachineOutputChannelIsolation.MaxOutputCount)
            {
                issue =
                    $"Camera Session supports at most '{CameraCinemachineOutputChannelIsolation.MaxOutputCount}' explicit Outputs because each active Output requires one exclusive Cinemachine Output channel.";
                return false;
            }

            var seenPrefabs = new HashSet<GameObject>();
            var seenOutputIds = new HashSet<CameraOutputId>();
            var outputDefinitions =
                new List<CameraOutputDefinition>(prefabs.Count);

            for (int index = 0; index < prefabs.Count; index++)
            {
                GameObject prefab = prefabs[index];
                if (prefab == null)
                {
                    issue =
                        $"Camera Session Output Prefabs[{index}] is missing.";
                    return false;
                }

                if (!seenPrefabs.Add(prefab))
                {
                    issue =
                        $"Camera Session contains duplicate Output prefab reference '{prefab.name}'.";
                    return false;
                }

                CameraOutputAuthoring[] outputs =
                    prefab.GetComponentsInChildren<CameraOutputAuthoring>(true);
                if (outputs.Length != 1 || outputs[0] == null)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' must contain exactly one CameraOutputAuthoring. Found '{outputs.Length}'.";
                    return false;
                }

                CameraOutputAuthoring output = outputs[0];
                if (!output.TryValidateDefinition(out string outputIssue))
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' is invalid. {outputIssue}";
                    return false;
                }

                if (output.UnityCamera == null)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' requires an explicit Unity Camera.";
                    return false;
                }

                if (output.CinemachineBrain == null)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' requires an explicit CinemachineBrain.";
                    return false;
                }

                if (output.CinemachineBrain.gameObject !=
                    output.UnityCamera.gameObject)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' requires its Unity Camera and CinemachineBrain on the same GameObject.";
                    return false;
                }

                if (output.DefaultCameraRig == null)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' requires an explicit persistent Default Camera Rig.";
                    return false;
                }

                if (!output.DefaultCameraRig.TryValidateForApply(
                        out string rigIssue))
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' has an invalid Default Camera Rig. {rigIssue}";
                    return false;
                }

                if (!IsOwnedByPrefab(prefab, output.UnityCamera.transform) ||
                    !IsOwnedByPrefab(prefab, output.CinemachineBrain.transform) ||
                    !IsOwnedByPrefab(prefab, output.DefaultCameraRig.transform))
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' must own its Unity Camera, CinemachineBrain and Default Camera Rig inside the prefab hierarchy.";
                    return false;
                }

                CameraOutputDefinition definition =
                    output.OutputDefinition;
                if (!seenOutputIds.Add(definition.OutputId))
                {
                    issue =
                        $"Camera Session contains duplicate Camera Output identity '{definition.OutputId}'.";
                    return false;
                }

                outputDefinitions.Add(definition);
            }

            try
            {
                CameraDefinitionValidation.ValidateOutputs(
                    outputDefinitions);
            }
            catch (InvalidOperationException exception)
            {
                issue = exception.Message;
                return false;
            }

            IReadOnlyList<PlayerCameraOutputBindingAuthoring> bindings =
                PlayerOutputBindings;
            var seenSlots = new HashSet<PlayerSlotId>();
            var outputBySlot =
                new Dictionary<
                    PlayerSlotId,
                    CameraOutputDefinition>();
            for (int index = 0; index < bindings.Count; index++)
            {
                PlayerCameraOutputBindingAuthoring binding =
                    bindings[index];
                if (binding == null)
                {
                    issue =
                        $"Camera Session Player Output Bindings[{index}] is missing.";
                    return false;
                }

                var playerSlotProfile =
                    binding.PlayerSlotProfile;
                if (playerSlotProfile == null)
                {
                    issue =
                        $"Camera Session Player Output binding at index '{index}' requires a valid PlayerSlotProfile.";
                    return false;
                }

                if (!playerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string slotIssue))
                {
                    issue =
                        $"Camera Session Player Output binding at index '{index}' requires a valid PlayerSlotProfile. {slotIssue}";
                    return false;
                }

                if (!seenSlots.Add(playerSlotId))
                {
                    issue =
                        $"Camera Session contains duplicate or conflicting Player Output bindings for Slot '{playerSlotId.StableText}'.";
                    return false;
                }

                CameraOutputDefinition outputDefinition =
                    binding.OutputDefinition;
                if (outputDefinition == null ||
                    !outputDefinition.HasValidId)
                {
                    issue =
                        $"Camera Session Player Output binding for Slot '{playerSlotId.StableText}' requires a valid CameraOutputDefinition.";
                    return false;
                }

                bool configuredOutput = false;
                for (int outputIndex = 0;
                     outputIndex < outputDefinitions.Count;
                     outputIndex++)
                {
                    if (ReferenceEquals(
                            outputDefinitions[outputIndex],
                            outputDefinition))
                    {
                        configuredOutput = true;
                        break;
                    }
                }

                if (!configuredOutput)
                {
                    issue =
                        $"Camera Session Player Output binding for Slot '{playerSlotId.StableText}' references Output '{outputDefinition.name}' which is not one of this Session's exact configured Output definitions.";
                    return false;
                }

                outputBySlot.Add(
                    playerSlotId,
                    outputDefinition);
            }

            if (!PlayerCameraPresentationTopology.TryCreate(
                    PlayerPresentationBindings,
                    out PlayerCameraPresentationTopology
                        presentationTopology,
                    out issue))
            {
                return false;
            }

            IReadOnlyList<PlayerCameraPresentationBinding>
                presentationBindings =
                    presentationTopology.Bindings;
            for (int index = 0;
                 index < presentationBindings.Count;
                 index++)
            {
                PlayerCameraPresentationBinding binding =
                    presentationBindings[index];

                if (!outputBySlot.TryGetValue(
                        binding.PlayerSlotId,
                        out CameraOutputDefinition playerOutput))
                {
                    issue =
                        $"Camera Session Player Presentation binding for Slot '{binding.PlayerSlotId.StableText}' requires an explicit Player Slot -> Output binding.";
                    return false;
                }

                CameraOutputDefinition presentationOutput =
                    binding.PresentationDefinition.OutputDefinition;
                if (!ReferenceEquals(
                        playerOutput,
                        presentationOutput))
                {
                    issue =
                        $"Camera Session Player Presentation binding for Slot '{binding.PlayerSlotId.StableText}' is incoherent. Player Output is '{playerOutput.name}', but Presentation '{binding.PresentationDefinition.name}' targets '{presentationOutput?.name ?? "<missing>"}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool IsOwnedByPrefab(
            GameObject prefab,
            Transform candidate)
        {
            if (prefab == null || candidate == null)
            {
                return false;
            }

            Transform root = prefab.transform;
            return candidate == root ||
                candidate.IsChildOf(root);
        }
    }
}
