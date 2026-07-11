using Immersive.Framework.Camera;
using Immersive.Framework.Common;

namespace Immersive.Framework.CameraAuthoring
{
    public readonly struct CameraRigComposerDebugSnapshot
    {
        public CameraRigComposerDebugSnapshot(CameraRigPresentationIntent presentationIntent, CameraTargetSourceKind targetSourceKind, string logicalSourceId, string diagnosticLabel, string unityCameraName, string cinemachineCameraName, string resolvedFollowTargetName, string resolvedLookAtTargetName, string applyRebuildStatus, string blockingIssue, string targetResolutionSummary, string materializationSummary)
        {
            PresentationIntent = presentationIntent;
            TargetSourceKind = targetSourceKind;
            LogicalSourceId = logicalSourceId.NormalizeText();
            DiagnosticLabel = diagnosticLabel.NormalizeText();
            UnityCameraName = unityCameraName.NormalizeText();
            CinemachineCameraName = cinemachineCameraName.NormalizeText();
            ResolvedFollowTargetName = resolvedFollowTargetName.NormalizeText();
            ResolvedLookAtTargetName = resolvedLookAtTargetName.NormalizeText();
            ApplyRebuildStatus = applyRebuildStatus.NormalizeText();
            BlockingIssue = blockingIssue.NormalizeText();
            TargetResolutionSummary = targetResolutionSummary.NormalizeText();
            MaterializationSummary = materializationSummary.NormalizeText();
        }
        public CameraRigPresentationIntent PresentationIntent { get; }
        public CameraTargetSourceKind TargetSourceKind { get; }
        public string LogicalSourceId { get; }
        public string DiagnosticLabel { get; }
        public string UnityCameraName { get; }
        public string CinemachineCameraName { get; }
        public string ResolvedFollowTargetName { get; }
        public string ResolvedLookAtTargetName { get; }
        public string ApplyRebuildStatus { get; }
        public string BlockingIssue { get; }
        public string TargetResolutionSummary { get; }
        public string MaterializationSummary { get; }
    }
}
