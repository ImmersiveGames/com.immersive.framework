#if IMMERSIVE_FRAMEWORK_CINEMACHINE
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Cinemachine
{
    /// <summary>
    /// API status: Experimental. Optional Cinemachine 3 adapter for framework camera rig descriptors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Cinemachine Rig Applier")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46C optional Cinemachine adapter.")]
    public sealed class FrameworkCinemachineRigApplier : MonoBehaviour, IFrameworkCameraRigApplier
    {
        public bool Supports(FrameworkCameraRigDescriptor descriptor)
        {
            return TryGetCinemachineCamera(descriptor, out _);
        }

        public void Apply(FrameworkCameraRigDescriptor descriptor)
        {
            if (!TryGetCinemachineCamera(descriptor, out CinemachineCamera cinemachineCamera))
            {
                return;
            }

            cinemachineCamera.Priority = descriptor.Priority.Priority;

            if (descriptor.Anchors.HasAnyTarget)
            {
                cinemachineCamera.Target.TrackingTarget = descriptor.Anchors.TrackingTarget;
                cinemachineCamera.Target.LookAtTarget = descriptor.Anchors.LookAtTarget;
            }
        }

        private static bool TryGetCinemachineCamera(
            FrameworkCameraRigDescriptor descriptor,
            out CinemachineCamera cinemachineCamera)
        {
            cinemachineCamera = null;
            if (!descriptor.IsValid)
            {
                return false;
            }

            cinemachineCamera = descriptor.Rig.GetComponentInChildren<CinemachineCamera>(true);
            return cinemachineCamera != null;
        }
    }
}
#endif
