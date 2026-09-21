using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Actor Presentation evidence that explicitly selects the Transform observed by Camera.
    /// Subject identity and lifetime remain owned by the prepared Actor occurrence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Actor Camera Subject")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-026 explicit Actor Presentation observation Transform.")]
    public sealed class ActorCameraSubjectAuthoring : MonoBehaviour
    {
        [Header("Camera Subject")]
        [Tooltip(
            "Required Transform observed by Camera presentation for this Actor Presentation. " +
            "No Actor-root or hierarchy fallback is used when this component is authored.")]
        [SerializeField]
        private Transform observationTransform;

        [Tooltip(
            "Optional presentation-space radius centered on the Observation Transform. " +
            "Use 0 to leave the radius unspecified so the consuming presentation can use its fallback.")]
        [SerializeField, Min(0f)]
        private float framingRadius;

        public Transform ObservationTransform => observationTransform;

        public bool HasObservationTransform => observationTransform != null;

        public float FramingRadius => framingRadius;

        public bool HasFramingRadius => framingRadius > 0f;

        /// <summary>
        /// Resolves the exact authored observation Transform for one materialized Actor
        /// Presentation. The authoring component must be on that Presentation root.
        /// </summary>
        public bool TryResolveObservation(
            Transform presentationRoot,
            out Transform observation,
            out string issue)
        {
            observation = null;
            issue = string.Empty;

            if (presentationRoot == null)
            {
                issue =
                    "Actor Camera Subject resolution requires the exact materialized Actor Presentation root.";
                return false;
            }

            if (transform != presentationRoot)
            {
                issue =
                    "Actor Camera Subject must be authored on the Actor Presentation root.";
                return false;
            }

            if (observationTransform == null)
            {
                issue =
                    "Actor Camera Subject requires an explicit Camera Subject Transform. No Actor-root fallback was used.";
                return false;
            }

            if (observationTransform != presentationRoot &&
                !observationTransform.IsChildOf(presentationRoot))
            {
                issue =
                    "Actor Camera Subject Transform must belong to the authored Actor Presentation.";
                return false;
            }

            observation = observationTransform;
            return true;
        }

        /// <summary>
        /// Resolves the exact authored Subject evidence for one materialized Actor Presentation.
        /// Framing radius is optional; zero means unspecified.
        /// </summary>
        public bool TryResolveSubject(
            Transform presentationRoot,
            out Transform observation,
            out float resolvedFramingRadius,
            out string issue)
        {
            resolvedFramingRadius = 0f;
            if (!TryResolveObservation(presentationRoot, out observation, out issue))
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
            return TryResolveSubject(transform, out _, out _, out issue);
        }
    }
}
