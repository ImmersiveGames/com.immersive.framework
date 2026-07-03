using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Diagnostic-only result of Activity Content Anchor discovery.
    /// It does not validate required anchors as blocking, expose a registry, bind runtime content or block lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity Content Anchor discovery diagnostics introduced by F9G.")]
    internal readonly struct ActivityContentAnchorDiscoveryResult
    {
        public ActivityContentAnchorDiscoveryResult(
            ActivityAsset activity,
            ContentAnchorSet anchorSet,
            int scannedSceneCount,
            int candidateCount,
            int acceptedCount,
            int skippedActivityMismatchCount,
            int invalidAuthoringCount,
            string source,
            string reason,
            string message,
            int discoverySceneRootCount = 0)
        {
            Activity = activity;
            AnchorSet = anchorSet;
            ScannedSceneCount = scannedSceneCount;
            CandidateCount = candidateCount;
            AcceptedCount = acceptedCount;
            SkippedActivityMismatchCount = skippedActivityMismatchCount;
            InvalidAuthoringCount = invalidAuthoringCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
            DiscoverySceneRootCount = discoverySceneRootCount < 0 ? 0 : discoverySceneRootCount;
        }

        public ActivityAsset Activity { get; }

        public ContentAnchorSet AnchorSet { get; }

        public int ScannedSceneCount { get; }

        public int CandidateCount { get; }

        public int AcceptedCount { get; }

        public int SkippedActivityMismatchCount { get; }

        public int InvalidAuthoringCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int DiscoverySceneRootCount { get; }

        public int AnchorCount => AnchorSet.Count;

        public int IssueCount => AnchorSet.IssueCount;

        public int RequiredCount => AnchorSet.RequiredCount;

        public int OptionalCount => AnchorSet.OptionalCount;

        public int RootCount => AnchorSet.RootCount;

        public int SlotCount => AnchorSet.SlotCount;

        public int PointCount => AnchorSet.PointCount;

        public int DuplicateIdentityCount => AnchorSet.DuplicateIdentityIssueCount;

        public int DuplicateAnchorIdCount => AnchorSet.DuplicateAnchorIdIssueCount;

        public int InvalidDeclarationCount => AnchorSet.InvalidDeclarationIssueCount;

        public bool HasAnchors => AnchorSet.HasAnchors;

        public bool HasIssues => AnchorSet.HasIssues || InvalidAuthoringCount > 0 || SkippedActivityMismatchCount > 0;

        public string DiagnosticMessage => ToDiagnosticString();

        public string ToDiagnosticString()
        {
            return $"Activity Content Anchor discovery activity='{GetActivityName(Activity)}' anchors='{AnchorCount}' candidates='{CandidateCount}' accepted='{AcceptedCount}' scenes='{ScannedSceneCount}' discoverySceneRoots='{DiscoverySceneRootCount}' required='{RequiredCount}' optional='{OptionalCount}' root='{RootCount}' slot='{SlotCount}' point='{PointCount}' issues='{IssueCount}' invalidAuthoring='{InvalidAuthoringCount}' skippedActivityMismatch='{SkippedActivityMismatchCount}' duplicateIdentity='{DuplicateIdentityCount}' duplicateAnchorId='{DuplicateAnchorIdCount}' message='{Message}'.";
        }

        public static ActivityContentAnchorDiscoveryResult Empty(
            ActivityAsset activity,
            string source,
            string reason,
            string message,
            int discoverySceneRootCount = 0)
        {
            return new ActivityContentAnchorDiscoveryResult(
                activity,
                ContentAnchorSet.Empty(),
                0,
                0,
                0,
                0,
                0,
                source,
                reason,
                message,
                discoverySceneRootCount);
        }

        private static string GetActivityName(ActivityAsset activity)
        {
            return activity.ToDiagnosticText(x => x.ActivityName);
        }
    }
}
