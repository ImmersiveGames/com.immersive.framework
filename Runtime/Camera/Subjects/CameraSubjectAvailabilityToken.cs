using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Exact scoped token for one current Subject availability occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A foreign/stale-safe Camera Subject availability token.")]
    public readonly struct CameraSubjectAvailabilityToken : IEquatable<CameraSubjectAvailabilityToken>
    {
        internal CameraSubjectAvailabilityToken(SubjectAvailabilityContextId contextId, CameraSubjectId subjectId, CameraSubjectAvailabilityOwnerId ownerId, int revision)
        {
            ContextId = contextId;
            SubjectId = subjectId;
            OwnerId = ownerId;
            Revision = revision;
        }

        public SubjectAvailabilityContextId ContextId { get; }
        public CameraSubjectId SubjectId { get; }
        public CameraSubjectAvailabilityOwnerId OwnerId { get; }
        public int Revision { get; }
        public bool IsValid => ContextId.IsValid && SubjectId.IsValid && OwnerId.IsValid && Revision > 0;
        public string StableText => IsValid ? $"camera-subject-availability:{ContextId}:{SubjectId.Value}:{OwnerId.Value}:{Revision}" : string.Empty;

        public bool Equals(CameraSubjectAvailabilityToken other) =>
            (ContextId == other.ContextId) &&
            SubjectId == other.SubjectId && OwnerId == other.OwnerId && Revision == other.Revision;

        public override bool Equals(object obj) => obj is CameraSubjectAvailabilityToken other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ContextId.GetHashCode();
                hashCode = hashCode * 397 ^ SubjectId.GetHashCode();
                hashCode = hashCode * 397 ^ OwnerId.GetHashCode();
                hashCode = hashCode * 397 ^ Revision;
                return hashCode;
            }
        }
        public override string ToString() => StableText;
        public static bool operator ==(CameraSubjectAvailabilityToken left, CameraSubjectAvailabilityToken right) => left.Equals(right);
        public static bool operator !=(CameraSubjectAvailabilityToken left, CameraSubjectAvailabilityToken right) => !left.Equals(right);
    }
}
