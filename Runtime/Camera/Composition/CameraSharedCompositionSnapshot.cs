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
        "CAMERA-029-B composition-owned membership diagnostic snapshot.")]
    public readonly struct CameraSharedCompositionSnapshot
    {
        public CameraSharedCompositionSnapshot(
            bool isReady,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevisionConsumed,
            CameraCompositionMembershipContextId membershipContextId,
            int membershipRevision,
            int subjectCount,
            int addedMembershipCount,
            int removedMembershipCount,
            CameraSharedCompositionReconcileStatus lastReconcileStatus,
            string lastBlockingIssue,
            CameraViewPresentationApplyStatus lastPresentationApplyStatus)
        {
            IsReady = isReady;
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevisionConsumed = availabilityRevisionConsumed;
            MembershipContextId = membershipContextId;
            MembershipRevision = membershipRevision;
            SubjectCount = subjectCount;
            AddedMembershipCount = addedMembershipCount;
            RemovedMembershipCount = removedMembershipCount;
            LastReconcileStatus = lastReconcileStatus;
            LastBlockingIssue = lastBlockingIssue.NormalizeText();
            LastPresentationApplyStatus = lastPresentationApplyStatus;
        }

        public bool IsReady { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int AvailabilityRevisionConsumed { get; }
        public CameraCompositionMembershipContextId MembershipContextId { get; }
        public int MembershipRevision { get; }
        public int SubjectCount { get; }
        public int AddedMembershipCount { get; }
        public int RemovedMembershipCount { get; }
        public CameraSharedCompositionReconcileStatus LastReconcileStatus { get; }
        public string LastBlockingIssue { get; }
        public CameraViewPresentationApplyStatus LastPresentationApplyStatus { get; }
    }
}
