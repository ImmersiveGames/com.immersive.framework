using Immersive.Foundation.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Cinemachine
{
    /// <summary>
    /// Explicit authoring source for a Cinemachine camera output.
    /// It exposes references only; lifecycle remains owned by the Route/Activity binding.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Cinemachine Camera Output Source")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "C8B4A explicit Route/Activity Cinemachine output bridge.")]
    public sealed class FrameworkCinemachineCameraOutputSource : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private int priority;
        [SerializeField] private bool required = true;
        [SerializeField] private string outputId;
        [SerializeField] private string displayName;

        public CinemachineCamera CinemachineCamera => cinemachineCamera;

        public CinemachineBrain CinemachineBrain => cinemachineBrain;

        public Transform FollowTarget => followTarget;

        public Transform LookAtTarget => lookAtTarget;

        public int Priority => priority;

        public bool Required => required;

        public string OutputId => outputId.NormalizeTextOrFallback("camera.output");

        public string DisplayName => displayName.NormalizeTextOrFallback(OutputId);

        public bool TryCreateOutput(
            out CinemachineCameraOutput output,
            out CinemachineCameraOutputDiagnostic diagnostic)
        {
            output = new CinemachineCameraOutput(
                OutputId,
                DisplayName,
                cinemachineCamera,
                cinemachineBrain,
                followTarget,
                lookAtTarget,
                priority,
                required);

            diagnostic = FrameworkCinemachineOutputApplier.Validate(
                output,
                explicitBrainScope: cinemachineBrain != null ? new[] { cinemachineBrain } : null);

            return diagnostic.IsSucceeded;
        }
    }
}
