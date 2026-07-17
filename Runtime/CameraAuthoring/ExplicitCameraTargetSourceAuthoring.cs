using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Designer-facing explicit Follow/LookAt provider.
    /// It supplies target evidence only and never selects a camera or owns runtime output.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Camera/Explicit Camera Target Source")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Explicit typed Camera target-source authoring without Player-specific coupling.")]
    public sealed class ExplicitCameraTargetSourceAuthoring :
        MonoBehaviour,
        ICameraTargetSource
    {
        [Header("Identity")]
        [Tooltip("Stable semantic source id. GameObject names and hierarchy paths are diagnostic only.")]
        [SerializeField] private string logicalSourceId;

        [Header("Targets")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;

        public string LogicalSourceId => logicalSourceId.NormalizeText();
        public Transform FollowTarget => followTarget;
        public Transform LookAtTarget => lookAtTarget;
        public CameraTargetSourceKind TargetSourceKind =>
            CameraTargetSourceKind.ExplicitTransform;

        public CameraTargetResolveResult ResolveCameraTargets(
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement)
        {
            var source = new CameraTargetSourceDescriptor(
                CameraTargetSourceKind.ExplicitTransform,
                this,
                LogicalSourceId,
                string.IsNullOrEmpty(LogicalSourceId)
                    ? "ExplicitCameraTargetSource"
                    : $"ExplicitCameraTargetSource:{LogicalSourceId}");

            var targets = new CameraResolvedTargets(
                followRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : followTarget,
                lookAtRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : lookAtTarget);

            return CameraTargetResolveResult.ValidateRequirements(
                source,
                targets,
                followRequirement,
                lookAtRequirement);
        }
    }
}
