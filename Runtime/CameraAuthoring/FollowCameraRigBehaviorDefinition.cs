using Immersive.Framework.Camera;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Follow Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Follow")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-C reusable Camera Rig behavior; IF-ADR-038 Session Camera use.")]
    public sealed class FollowCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -8f);

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Follow;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public Vector3 FollowOffset => followOffset;

        public override bool TryValidate(out string issue)
        {
            if (!IsDefinedRequirement(lookAtRequirement))
                return Invalid(nameof(lookAtRequirement), lookAtRequirement, "Not Used, Optional or Required", out issue);
            if (!IsFinite(followOffset))
                return Invalid(nameof(followOffset), followOffset, "a finite Vector3", out issue);

            issue = string.Empty;
            return true;
        }
    }
}
