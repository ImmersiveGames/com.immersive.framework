using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Fixed Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Fixed")]
    public sealed class FixedCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Fixed;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.NotUsed;
        public override CameraTargetRequirement LookAtRequirement => CameraTargetRequirement.NotUsed;

        public override bool TryValidate(out string issue)
        {
            issue = string.Empty;
            return true;
        }
    }
}
