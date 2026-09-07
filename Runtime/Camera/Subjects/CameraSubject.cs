using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>Presentation-neutral description of something currently observable by Camera presentation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A presentation-neutral Camera Subject description.")]
    public readonly struct CameraSubject
    {
        public CameraSubject(CameraSubjectId subjectId, Transform observation, string description)
        {
            SubjectId = subjectId;
            Observation = observation;
            Description = description.NormalizeText();
        }

        public CameraSubjectId SubjectId { get; }
        public Transform Observation { get; }
        public string Description { get; }
        public bool IsValid => SubjectId.IsValid && Observation != null;

        internal bool HasSameDescription(CameraSubject other) =>
            SubjectId == other.SubjectId &&
            ReferenceEquals(Observation, other.Observation) &&
            string.Equals(Description, other.Description, System.StringComparison.Ordinal);
    }
}
