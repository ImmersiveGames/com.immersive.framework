using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Resolved follow/look-at targets for a camera rig.
    /// These are explicit targets; there is no Camera.main or hierarchy fallback.
    /// </summary>
    public readonly struct CameraResolvedTargets
    {
        public CameraResolvedTargets(Transform followTarget, Transform lookAtTarget)
        {
            FollowTarget = followTarget;
            LookAtTarget = lookAtTarget;
        }

        public Transform FollowTarget { get; }

        public Transform LookAtTarget { get; }

        public bool HasFollowTarget => FollowTarget != null;

        public bool HasLookAtTarget => LookAtTarget != null;

        public bool HasAnyTarget => HasFollowTarget || HasLookAtTarget;

        public static CameraResolvedTargets None => new CameraResolvedTargets(null, null);

        public static CameraResolvedTargets FromFollow(Transform followTarget)
        {
            return new CameraResolvedTargets(followTarget, null);
        }

        public static CameraResolvedTargets FromFollowAndLookAt(Transform followTarget, Transform lookAtTarget)
        {
            return new CameraResolvedTargets(followTarget, lookAtTarget);
        }
    }
}
