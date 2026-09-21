using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>Presentation-neutral description of something currently observable by Camera presentation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A presentation-neutral Camera Subject description.")]
    public readonly struct CameraSubject
    {
        public CameraSubject(
            CameraSubjectId subjectId,
            Transform observation,
            string description)
            : this(subjectId, observation, description, 0f)
        {
        }

        public CameraSubject(
            CameraSubjectId subjectId,
            Transform observation,
            string description,
            float framingRadius)
        {
            SubjectId = subjectId;
            Observation = observation;
            Description = description.NormalizeText();
            FramingRadius = framingRadius;
        }

        public CameraSubjectId SubjectId { get; }
        public Transform Observation { get; }
        public string Description { get; }

        /// <summary>
        /// Optional presentation-space radius centered on <see cref="Observation"/>.
        /// Zero means unspecified and lets the consuming presentation use its own fallback.
        /// </summary>
        public float FramingRadius { get; }

        public bool HasFramingRadius => FramingRadius > 0f;

        public bool IsValid =>
            SubjectId.IsValid &&
            Observation != null &&
            IsValidFramingRadius(FramingRadius);

        internal bool HasSameDescription(CameraSubject other) =>
            SubjectId == other.SubjectId &&
            ReferenceEquals(Observation, other.Observation) &&
            FramingRadius.Equals(other.FramingRadius) &&
            string.Equals(Description, other.Description, System.StringComparison.Ordinal);

        private static bool IsValidFramingRadius(float value) =>
            value >= 0f &&
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }
}
