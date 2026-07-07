using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Scene-authored follow/look-at target provider for framework camera bindings.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Anchor Host")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public sealed class FrameworkCameraAnchorHost : MonoBehaviour
    {
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private Transform lookAtTarget;

        public Transform TrackingTarget => trackingTarget;

        public Transform LookAtTarget => lookAtTarget;

        public bool HasAnyTarget => trackingTarget != null || lookAtTarget != null;

        public FrameworkCameraAnchorDescriptor ToDescriptor()
        {
            return new FrameworkCameraAnchorDescriptor(trackingTarget, lookAtTarget);
        }
    }
}
