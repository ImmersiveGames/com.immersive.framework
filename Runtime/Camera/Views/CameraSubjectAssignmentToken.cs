using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Exact scoped token for one current View-to-Subject assignment occurrence.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B foreign/stale-safe Camera Subject Assignment token.")]
    public readonly struct CameraSubjectAssignmentToken :
        IEquatable<CameraSubjectAssignmentToken>
    {
        internal CameraSubjectAssignmentToken(
            ViewAssignmentContextId contextId,
            CameraViewId viewId,
            CameraSubjectId subjectId,
            CameraSubjectAssignmentOwnerId ownerId,
            int revision)
        {
            ContextId = contextId;
            ViewId = viewId;
            SubjectId = subjectId;
            OwnerId = ownerId;
            Revision = revision;
        }

        public ViewAssignmentContextId ContextId { get; }
        public CameraViewId ViewId { get; }
        public CameraSubjectId SubjectId { get; }
        public CameraSubjectAssignmentOwnerId OwnerId { get; }
        public int Revision { get; }

        public bool IsValid =>
            ContextId.IsValid &&
            ViewId.IsValid &&
            SubjectId.IsValid &&
            OwnerId.IsValid &&
            Revision > 0;

        public string StableText => IsValid
            ? $"camera-subject-assignment:{ContextId}:{ViewId.Value}:" +
              $"{SubjectId.Value}:{OwnerId.Value}:{Revision}"
            : string.Empty;

        public bool Equals(CameraSubjectAssignmentToken other)
        {
            return (ContextId == other.ContextId) &&
                ViewId == other.ViewId &&
                SubjectId == other.SubjectId &&
                OwnerId == other.OwnerId &&
                Revision == other.Revision;
        }

        public override bool Equals(object obj)
        {
            return obj is CameraSubjectAssignmentToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ContextId.GetHashCode();
                hashCode = hashCode * 397 ^ ViewId.GetHashCode();
                hashCode = hashCode * 397 ^ SubjectId.GetHashCode();
                hashCode = hashCode * 397 ^ OwnerId.GetHashCode();
                hashCode = hashCode * 397 ^ Revision;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static bool operator ==(
            CameraSubjectAssignmentToken left,
            CameraSubjectAssignmentToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CameraSubjectAssignmentToken left,
            CameraSubjectAssignmentToken right)
        {
            return !left.Equals(right);
        }
    }
}
