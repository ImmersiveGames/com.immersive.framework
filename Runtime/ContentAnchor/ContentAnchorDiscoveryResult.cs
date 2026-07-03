using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Diagnostic-only result of Route Content Anchor discovery.
    /// It does not validate required anchors, expose a registry, bind runtime content or block lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route Content Anchor discovery diagnostics introduced by F7F.")]
    internal readonly struct ContentAnchorDiscoveryResult
    {
        public ContentAnchorDiscoveryResult(
            RouteAsset route,
            ContentAnchorSet anchorSet,
            int scannedSceneCount,
            int candidateCount,
            int acceptedCount,
            int skippedRouteMismatchCount,
            int invalidAuthoringCount,
            string source,
            string reason,
            string message)
        {
            Route = route;
            AnchorSet = anchorSet;
            ScannedSceneCount = scannedSceneCount;
            CandidateCount = candidateCount;
            AcceptedCount = acceptedCount;
            SkippedRouteMismatchCount = skippedRouteMismatchCount;
            InvalidAuthoringCount = invalidAuthoringCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public RouteAsset Route { get; }

        public ContentAnchorSet AnchorSet { get; }

        public int ScannedSceneCount { get; }

        public int CandidateCount { get; }

        public int AcceptedCount { get; }

        public int SkippedRouteMismatchCount { get; }

        public int InvalidAuthoringCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

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

        public bool HasIssues => AnchorSet.HasIssues || InvalidAuthoringCount > 0 || SkippedRouteMismatchCount > 0;

        public string DiagnosticMessage => ToDiagnosticString();

        public string ToDiagnosticString()
        {
            return $"Content Anchor discovery route='{GetRouteName(Route)}' anchors='{AnchorCount}' candidates='{CandidateCount}' accepted='{AcceptedCount}' scenes='{ScannedSceneCount}' required='{RequiredCount}' optional='{OptionalCount}' root='{RootCount}' slot='{SlotCount}' point='{PointCount}' issues='{IssueCount}' invalidAuthoring='{InvalidAuthoringCount}' skippedRouteMismatch='{SkippedRouteMismatchCount}' duplicateIdentity='{DuplicateIdentityCount}' duplicateAnchorId='{DuplicateAnchorIdCount}' message='{Message}'.";
        }

        public static ContentAnchorDiscoveryResult Empty(
            RouteAsset route,
            string source,
            string reason,
            string message)
        {
            return new ContentAnchorDiscoveryResult(
                route,
                ContentAnchorSet.Empty(),
                0,
                0,
                0,
                0,
                0,
                source,
                reason,
                message);
        }

        private static string GetRouteName(RouteAsset route)
        {
            return route.ToDiagnosticText(x => x.RouteName);
        }
    }
}
