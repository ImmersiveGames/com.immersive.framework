using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Follow Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Follow")]
    public sealed class FollowCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -8f);
        [SerializeField, Min(0.0001f)] private float sharedFollowMemberWeight = 1f;
        [SerializeField, Min(0.0001f)] private float sharedFollowMemberRadius = 0.5f;
        [SerializeField, Range(0.01f, 2f)] private float sharedFollowFramingSize = 0.8f;
        [SerializeField, Range(0f, 20f)] private float sharedFollowDamping = 2f;
        [SerializeField] private Vector2 sharedFollowFovRange = new Vector2(1f, 100f);
        [SerializeField] private Vector2 sharedFollowDollyRange = new Vector2(-100f, 100f);
        [SerializeField] private Vector2 sharedFollowOrthoSizeRange = new Vector2(1f, 1000f);

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Follow;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public Vector3 FollowOffset => followOffset;
        public float SharedFollowMemberWeight => sharedFollowMemberWeight;
        public float SharedFollowMemberRadius => sharedFollowMemberRadius;
        public float SharedFollowFramingSize => sharedFollowFramingSize;
        public float SharedFollowDamping => sharedFollowDamping;
        public Vector2 SharedFollowFovRange => sharedFollowFovRange;
        public Vector2 SharedFollowDollyRange => sharedFollowDollyRange;
        public Vector2 SharedFollowOrthoSizeRange => sharedFollowOrthoSizeRange;

        public override bool TryValidate(out string issue)
        {
            if (!IsDefinedRequirement(lookAtRequirement))
                return Invalid(nameof(lookAtRequirement), lookAtRequirement, "Not Used, Optional or Required", out issue);
            if (!IsFinite(followOffset))
                return Invalid(nameof(followOffset), followOffset, "a finite Vector3", out issue);
            if (!IsFinite(sharedFollowMemberWeight) || sharedFollowMemberWeight <= 0f)
                return Invalid(nameof(sharedFollowMemberWeight), sharedFollowMemberWeight, "a finite value greater than zero", out issue);
            if (!IsFinite(sharedFollowMemberRadius) || sharedFollowMemberRadius <= 0f)
                return Invalid(nameof(sharedFollowMemberRadius), sharedFollowMemberRadius, "a finite value greater than zero", out issue);
            if (!IsFinite(sharedFollowFramingSize) || sharedFollowFramingSize < 0.01f || sharedFollowFramingSize > 2f)
                return Invalid(nameof(sharedFollowFramingSize), sharedFollowFramingSize, "a finite value from 0.01 through 2", out issue);
            if (!IsFinite(sharedFollowDamping) || sharedFollowDamping < 0f || sharedFollowDamping > 20f)
                return Invalid(nameof(sharedFollowDamping), sharedFollowDamping, "a finite value from 0 through 20", out issue);
            if (!IsOrderedRange(sharedFollowFovRange, 1f, 179f))
                return Invalid(nameof(sharedFollowFovRange), sharedFollowFovRange, "an ordered finite range within 1 through 179", out issue);
            if (!IsOrderedRange(sharedFollowDollyRange, float.MinValue, float.MaxValue))
                return Invalid(nameof(sharedFollowDollyRange), sharedFollowDollyRange, "an ordered finite range", out issue);
            if (!IsOrderedRange(sharedFollowOrthoSizeRange, 0.01f, float.MaxValue))
                return Invalid(nameof(sharedFollowOrthoSizeRange), sharedFollowOrthoSizeRange, "an ordered finite range starting at 0.01 or greater", out issue);

            issue = string.Empty;
            return true;
        }
    }
}
