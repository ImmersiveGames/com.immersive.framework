using Immersive.Foundation.Common;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Cinemachine
{
    /// <summary>
    /// Explicit technical output for a materialized Cinemachine camera rig.
    /// This is not a lifecycle authority and it does not create, find or select cameras.
    /// </summary>
    public readonly struct CinemachineCameraOutput
    {
        public CinemachineCameraOutput(
            string outputId,
            string displayName,
            CinemachineCamera cinemachineCamera,
            CinemachineBrain brain,
            Transform followTarget,
            Transform lookAtTarget,
            int priority,
            bool required)
        {
            OutputId = outputId.NormalizeTextOrFallback("camera.output");
            DisplayName = displayName.NormalizeTextOrFallback(OutputId);
            CinemachineCamera = cinemachineCamera;
            Brain = brain;
            FollowTarget = followTarget;
            LookAtTarget = lookAtTarget;
            Priority = priority;
            Required = required;
        }

        public string OutputId { get; }

        public string DisplayName { get; }

        public CinemachineCamera CinemachineCamera { get; }

        public CinemachineBrain Brain { get; }

        public Transform FollowTarget { get; }

        public Transform LookAtTarget { get; }

        public int Priority { get; }

        public bool Required { get; }

        public bool HasCamera => CinemachineCamera != null;

        public bool HasBrain => Brain != null;

        public bool HasFollowTarget => FollowTarget != null;

        public bool HasLookAtTarget => LookAtTarget != null;

        public bool HasAnyOutputEvidence => HasCamera || HasBrain;

        public bool IsMaterialized => HasCamera && HasBrain;

        public static CinemachineCameraOutput Missing(string outputId, string displayName, bool required, int priority = 0)
        {
            return new CinemachineCameraOutput(
                outputId,
                displayName,
                null,
                null,
                null,
                null,
                priority,
                required);
        }

    }
}
