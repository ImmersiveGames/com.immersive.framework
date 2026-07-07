using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Optional follow/look-at target data for framework camera rigs.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public readonly struct FrameworkCameraAnchorDescriptor
    {
        public FrameworkCameraAnchorDescriptor(Transform trackingTarget, Transform lookAtTarget)
        {
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
        }

        public Transform TrackingTarget { get; }

        public Transform LookAtTarget { get; }

        public bool HasTrackingTarget => TrackingTarget != null;

        public bool HasLookAtTarget => LookAtTarget != null;

        public bool HasAnyTarget => HasTrackingTarget || HasLookAtTarget;

        public static FrameworkCameraAnchorDescriptor Empty => new FrameworkCameraAnchorDescriptor(null, null);
    }
}
