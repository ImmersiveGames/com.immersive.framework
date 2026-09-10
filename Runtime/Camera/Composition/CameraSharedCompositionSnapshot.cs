using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Minimal deterministic diagnostic snapshot of one Camera composition's current state.
    /// It exposes evidence for QA without exposing large internal implementation objects.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E shared Camera composition runtime diagnostic snapshot.")]
    public readonly struct CameraSharedCompositionSnapshot
    {
        public CameraSharedCompositionSnapshot(
            bool isReady,
            string viewId,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevisionConsumed,
            ViewAssignmentContextId assignmentContextId,
            int assignmentRevision,
            int subjectCount,
            int addedAssignmentCount,
            int removedAssignmentCount,
            CameraSharedCompositionReconcileStatus lastReconcileStatus,
            string lastBlockingIssue,
            CameraViewPresentationApplyStatus lastPresentationApplyStatus)
        {
            IsReady = isReady;
            ViewId = viewId.NormalizeText();
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevisionConsumed = availabilityRevisionConsumed;
            AssignmentContextId = assignmentContextId;
            AssignmentRevision = assignmentRevision;
            SubjectCount = subjectCount;
            AddedAssignmentCount = addedAssignmentCount;
            RemovedAssignmentCount = removedAssignmentCount;
            LastReconcileStatus = lastReconcileStatus;
            LastBlockingIssue = lastBlockingIssue.NormalizeText();
            LastPresentationApplyStatus = lastPresentationApplyStatus;
        }

        public bool IsReady { get; }
        public string ViewId { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int AvailabilityRevisionConsumed { get; }
        public ViewAssignmentContextId AssignmentContextId { get; }
        public int AssignmentRevision { get; }
        public int SubjectCount { get; }
        public int AddedAssignmentCount { get; }
        public int RemovedAssignmentCount { get; }
        public CameraSharedCompositionReconcileStatus LastReconcileStatus { get; }
        public string LastBlockingIssue { get; }
        public CameraViewPresentationApplyStatus LastPresentationApplyStatus { get; }
    }
}
