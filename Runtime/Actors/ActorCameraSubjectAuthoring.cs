using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Explicit observation evidence owned by one Actor occurrence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Camera Subject")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-038 Actor occurrence observation Transform.")]
    public sealed class ActorCameraSubjectAuthoring : MonoBehaviour
    {
        [Header("Camera Subject")]
        [Tooltip(
            "Required Transform observed for this Actor occurrence. Assign the Actor root explicitly if it is the desired observation point.")]
        [SerializeField]
        private Transform observationTransform;

        [Tooltip(
            "Optional framing radius centered on the Observation Transform. Use 0 when unspecified.")]
        [SerializeField, Min(0f)]
        private float framingRadius;

        public Transform ObservationTransform => observationTransform;

        public bool HasObservationTransform => observationTransform != null;

        public float FramingRadius => framingRadius;

        public bool HasFramingRadius => framingRadius > 0f;

        /// <summary>
        /// Resolves the exact authored observation Transform for one Actor occurrence.
        /// This component must share the Actor declaration's GameObject.
        /// </summary>
        public bool TryResolveObservation(
            ActorDeclaration actor,
            out Transform observation,
            out string issue)
        {
            observation = null;
            issue = string.Empty;

            if (actor == null || actor.transform == null)
            {
                issue = "Actor Camera Subject resolution requires the exact Actor occurrence declaration.";
                return false;
            }

            if (transform != actor.transform)
            {
                issue = "Actor Camera Subject authoring must belong to the exact Actor occurrence declaration.";
                return false;
            }

            if (observationTransform == null)
            {
                issue =
                    "Actor Camera Subject requires an explicit Observation Transform. No Actor-root or hierarchy/name fallback was used.";
                return false;
            }

            if (observationTransform != actor.transform &&
                !observationTransform.IsChildOf(actor.transform))
            {
                issue = "Actor Camera Subject Observation Transform must belong to the exact Actor occurrence hierarchy.";
                return false;
            }

            observation = observationTransform;
            return true;
        }

        /// <summary>
        /// Resolves authored observation and optional framing evidence for one Actor occurrence.
        /// </summary>
        public bool TryResolveSubject(
            ActorDeclaration actor,
            out Transform observation,
            out float resolvedFramingRadius,
            out string issue)
        {
            resolvedFramingRadius = 0f;
            if (!TryResolveObservation(actor, out observation, out issue))
            {
                return false;
            }

            if (framingRadius < 0f ||
                float.IsNaN(framingRadius) ||
                float.IsInfinity(framingRadius))
            {
                observation = null;
                issue =
                    "Actor Camera Subject Framing Radius must be zero (unspecified) or a finite value greater than zero.";
                return false;
            }

            resolvedFramingRadius = framingRadius;
            return true;
        }

        public bool TryValidateConfiguration(out string issue)
        {
            return TryResolveSubject(GetComponent<ActorDeclaration>(), out _, out _, out issue);
        }
    }
}
