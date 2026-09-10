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

        public Transform ObservationTransform => observationTransform;

        public bool HasObservationTransform => observationTransform != null;

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

        public bool TryValidateConfiguration(out string issue)
        {
            return TryResolveObservation(transform, out _, out issue);
        }
    }
}
