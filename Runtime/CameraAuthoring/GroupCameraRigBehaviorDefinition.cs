using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Group Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Group")]
    public sealed class GroupCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -8f);
        [SerializeField, Min(0.0001f)] private float memberWeight = 1f;
        [SerializeField, Min(0.0001f)] private float memberRadius = 0.5f;
        [SerializeField, Range(0.01f, 2f)] private float framingSize = 0.8f;
        [SerializeField, Range(0f, 20f)] private float damping = 2f;
        [SerializeField] private Vector2 fovRange = new Vector2(1f, 100f);
        [SerializeField] private Vector2 dollyRange = new Vector2(-100f, 100f);
        [SerializeField] private Vector2 orthoSizeRange = new Vector2(1f, 1000f);

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Group;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public Vector3 FollowOffset => followOffset;
        public float MemberWeight => memberWeight;
        public float MemberRadius => memberRadius;
        public float FramingSize => framingSize;
        public float Damping => damping;
        public Vector2 FovRange => fovRange;
        public Vector2 DollyRange => dollyRange;
        public Vector2 OrthoSizeRange => orthoSizeRange;

        public override bool TryValidate(out string issue)
        {
            if (!IsDefinedRequirement(lookAtRequirement))
                return Invalid(nameof(lookAtRequirement), lookAtRequirement, "Not Used, Optional or Required", out issue);
            if (!IsFinite(followOffset))
                return Invalid(nameof(followOffset), followOffset, "a finite Vector3", out issue);
            if (!IsFinite(memberWeight) || memberWeight <= 0f)
                return Invalid(nameof(memberWeight), memberWeight, "a finite value greater than zero", out issue);
            if (!IsFinite(memberRadius) || memberRadius <= 0f)
                return Invalid(nameof(memberRadius), memberRadius, "a finite value greater than zero", out issue);
            if (!IsFinite(framingSize) || framingSize < 0.01f || framingSize > 2f)
                return Invalid(nameof(framingSize), framingSize, "a finite value from 0.01 through 2", out issue);
            if (!IsFinite(damping) || damping < 0f || damping > 20f)
                return Invalid(nameof(damping), damping, "a finite value from 0 through 20", out issue);
            if (!IsOrderedRange(fovRange, 1f, 179f))
                return Invalid(nameof(fovRange), fovRange, "an ordered finite range within 1 through 179", out issue);
            if (!IsOrderedRange(dollyRange, float.MinValue, float.MaxValue))
                return Invalid(nameof(dollyRange), dollyRange, "an ordered finite range", out issue);
            if (!IsOrderedRange(orthoSizeRange, 0.01f, float.MaxValue))
                return Invalid(nameof(orthoSizeRange), orthoSizeRange, "an ordered finite range starting at 0.01 or greater", out issue);

            issue = string.Empty;
            return true;
        }
    }
}
