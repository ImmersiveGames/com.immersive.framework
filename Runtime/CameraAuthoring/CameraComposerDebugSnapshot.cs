using Immersive.Framework.Camera;
using Immersive.Framework.Common;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Immutable debug evidence for CameraComposer inspectors, diagnostics and future QA assertions.
    /// Names in this snapshot are diagnostic labels only; they are not functional identity.
    /// </summary>
    public readonly struct CameraComposerDebugSnapshot
    {
        public CameraComposerDebugSnapshot(
            CameraMode mode,
            CameraOwnershipScope ownershipScope,
            CameraTargetSourceKind targetSourceKind,
            string logicalSourceId,
            string diagnosticLabel,
            int priority,
            string unityCameraName,
            string cinemachineCameraName,
            string resolvedFollowTargetName,
            string resolvedLookAtTargetName,
            string lastApplyRebuildStatus,
            string lastBlockingIssue,
            string lastTargetResolutionSummary,
            string lastMaterializationSummary)
        {
            Mode = mode;
            OwnershipScope = ownershipScope;
            TargetSourceKind = targetSourceKind;
            LogicalSourceId = logicalSourceId.NormalizeText();
            DiagnosticLabel = diagnosticLabel.NormalizeText();
            Priority = priority;
            UnityCameraName = unityCameraName.NormalizeText();
            CinemachineCameraName = cinemachineCameraName.NormalizeText();
            ResolvedFollowTargetName = resolvedFollowTargetName.NormalizeText();
            ResolvedLookAtTargetName = resolvedLookAtTargetName.NormalizeText();
            LastApplyRebuildStatus = lastApplyRebuildStatus.NormalizeText();
            LastBlockingIssue = lastBlockingIssue.NormalizeText();
            LastTargetResolutionSummary = lastTargetResolutionSummary.NormalizeText();
            LastMaterializationSummary = lastMaterializationSummary.NormalizeText();
        }

        public CameraMode Mode { get; }

        public CameraOwnershipScope OwnershipScope { get; }

        public CameraTargetSourceKind TargetSourceKind { get; }

        public string LogicalSourceId { get; }

        public string DiagnosticLabel { get; }

        public int Priority { get; }

        public string UnityCameraName { get; }

        public string CinemachineCameraName { get; }

        public string ResolvedFollowTargetName { get; }

        public string ResolvedLookAtTargetName { get; }

        public string LastApplyRebuildStatus { get; }

        public string LastBlockingIssue { get; }

        public string LastTargetResolutionSummary { get; }

        public string LastMaterializationSummary { get; }
    }
}
