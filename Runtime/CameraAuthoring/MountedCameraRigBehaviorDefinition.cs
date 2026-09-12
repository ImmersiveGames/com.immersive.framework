using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Mounted Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Mounted")]
    public sealed class MountedCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField, Min(0f)] private float positionDamping;
        [SerializeField, Min(0f)] private float rotationDamping;

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Mounted;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement => CameraTargetRequirement.NotUsed;
        public float PositionDamping => positionDamping;
        public float RotationDamping => rotationDamping;

        public override bool TryValidate(out string issue)
        {
            if (!IsFiniteNonNegative(positionDamping))
                return Invalid(nameof(positionDamping), positionDamping, "a finite non-negative value", out issue);
            if (!IsFiniteNonNegative(rotationDamping))
                return Invalid(nameof(rotationDamping), rotationDamping, "a finite non-negative value", out issue);

            issue = string.Empty;
            return true;
        }
    }
}
