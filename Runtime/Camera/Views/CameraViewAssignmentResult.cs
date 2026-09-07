using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Immutable result for one logical View Assignment mutation or reconciliation.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B logical Camera View Assignment result.")]
    public sealed class CameraViewAssignmentResult
    {
        internal CameraViewAssignmentResult(
            CameraViewAssignmentStatus status,
            CameraSubjectAssignment assignment,
            CameraSubjectAssignmentToken token,
            int removedCount,
            CameraViewAssignmentSnapshot snapshot,
            string message)
        {
            Status = status;
            Assignment = assignment;
            Token = token;
            RemovedCount = removedCount;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public CameraViewAssignmentStatus Status { get; }
        public CameraSubjectAssignment Assignment { get; }
        public CameraSubjectAssignmentToken Token { get; }
        public int RemovedCount { get; }
        public CameraViewAssignmentSnapshot Snapshot { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            CameraViewAssignmentStatus.SucceededAssigned or
            CameraViewAssignmentStatus.SucceededAlreadyAssigned or
            CameraViewAssignmentStatus.SucceededReleased or
            CameraViewAssignmentStatus.SucceededAlreadyReleased or
            CameraViewAssignmentStatus.SucceededReconciled or
            CameraViewAssignmentStatus.SucceededOwnerReleased;
    }
}
