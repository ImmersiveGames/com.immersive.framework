using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Explicit application-authored physical Camera capacity for one Session.
    ///
    /// The configuration owns only stable Session authoring: 1..N Output prefabs.
    /// Materialized occurrences and Player runtime state remain outside this object.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-038 explicit Session Camera capacity and Player Output integration configuration on GameApplication.")]
    public sealed class CameraSessionConfiguration
    {
        [SerializeField]
        [Tooltip("Explicit 1..N physical Camera Output prefabs materialized once for the Session. Each prefab must contain exactly one CameraOutputAuthoring with its Unity Camera, CinemachineBrain and persistent Fallback Camera Rig.")]
        private List<GameObject> outputPrefabs =
            new List<GameObject>();

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

                if (output.FallbackCameraRig == null)
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' requires an explicit persistent Fallback Camera.";
                    return false;
                }

                if (!output.FallbackCameraRig.TryValidateForApply(
                        out string rigIssue))
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' has an invalid Fallback Camera. {rigIssue}";
                    return false;
                }

                if (!IsOwnedByPrefab(prefab, output.UnityCamera.transform) ||
                    !IsOwnedByPrefab(prefab, output.CinemachineBrain.transform) ||
                    !IsOwnedByPrefab(prefab, output.FallbackCameraRig.transform))
                {
                    issue =
                        $"Camera Session Output prefab '{prefab.name}' must own its Unity Camera, CinemachineBrain and Fallback Camera inside the prefab hierarchy.";
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
