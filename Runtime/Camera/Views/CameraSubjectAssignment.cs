using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable logical membership of one explicit Subject identity in one View.
    /// It owns neither the Subject nor any Camera presentation object.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B logical View-to-Subject Assignment evidence.")]
    public readonly struct CameraSubjectAssignment
    {
        internal CameraSubjectAssignment(
            CameraViewId viewId,
            CameraSubjectId subjectId,
            CameraSubjectAssignmentOwnerId ownerId,
            CameraSubjectAssignmentToken token)
        {
            ViewId = viewId;
            SubjectId = subjectId;
            OwnerId = ownerId;
            Token = token;
        }

        public CameraViewId ViewId { get; }
        public CameraSubjectId SubjectId { get; }
        public CameraSubjectAssignmentOwnerId OwnerId { get; }
        public CameraSubjectAssignmentToken Token { get; }

        public bool IsValid =>
            ViewId.IsValid &&
            SubjectId.IsValid &&
            OwnerId.IsValid &&
            Token.IsValid &&
            Token.ViewId == ViewId &&
            Token.SubjectId == SubjectId &&
            Token.OwnerId == OwnerId;
    }
}
