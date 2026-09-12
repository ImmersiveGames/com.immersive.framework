using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Reusable authored presentation intent for a Camera rig. Definitions contain
    /// data only; the concrete CameraRigComposer remains materialization authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-C reusable Camera Rig behavior authority.")]
    public abstract class CameraRigBehaviorDefinition : ScriptableObject
    {
        public abstract CameraRigPresentationIntent PresentationIntent { get; }

        public abstract CameraTargetRequirement FollowRequirement { get; }

        public abstract CameraTargetRequirement LookAtRequirement { get; }

        public abstract bool TryValidate(out string issue);

        protected bool Invalid(string field, object value, string expectation, out string issue)
        {
            issue = $"Camera Rig Behavior Definition '{name}' has invalid {PresentationIntent} field '{field}' value '{value}'. Expected {expectation}.";
            return false;
        }

        protected static bool IsDefinedRequirement(CameraTargetRequirement value)
        {
            return value == CameraTargetRequirement.NotUsed ||
                   value == CameraTargetRequirement.Optional ||
                   value == CameraTargetRequirement.Required;
        }

        protected static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        protected static bool IsFinite(Vector2 value) =>
            IsFinite(value.x) && IsFinite(value.y);

        protected static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        protected static bool IsFiniteNonNegative(float value) =>
            IsFinite(value) && value >= 0f;

        protected static bool IsFiniteNonNegative(Vector3 value) =>
            IsFinite(value) && value.x >= 0f && value.y >= 0f && value.z >= 0f;

        protected static bool IsOrderedRange(Vector2 value, float minimum, float maximum) =>
            IsFinite(value) && value.x >= minimum && value.y <= maximum && value.x <= value.y;
    }
}
