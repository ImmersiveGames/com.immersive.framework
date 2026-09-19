using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit result of adapting Composition Subjects to current Rig capability.
    /// Group retains its complete ordered Subject set on Input and never fabricates one target.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Rig target projection result.")]
    public sealed class CameraRigTargetProjectionResult
    {
        internal CameraRigTargetProjectionResult(
            CameraRigTargetProjectionStatus status,
            CameraCompositionPresentationInput input,
            CameraResolvedTargets targets,
            CameraIssue[] issues,
            string blockingIssue,
            string diagnosticSummary)
        {
            Status = status;
            Input = input;
            Targets = targets;
            Issues = issues ?? Array.Empty<CameraIssue>();
            BlockingIssue = blockingIssue.NormalizeText();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraRigTargetProjectionStatus Status { get; }
        public CameraCompositionPresentationInput Input { get; }
        public CameraResolvedTargets Targets { get; }
        public CameraIssue[] Issues { get; }
        public string BlockingIssue { get; }
        public string DiagnosticSummary { get; }
        public bool Succeeded => Status is
            CameraRigTargetProjectionStatus.SucceededNoTargets or
            CameraRigTargetProjectionStatus.SucceededSingleSubject or
            CameraRigTargetProjectionStatus.SucceededGroup;
        public bool Blocked => Status is
            CameraRigTargetProjectionStatus.BlockedRequiredSubjectMissing or
            CameraRigTargetProjectionStatus.BlockedMultipleSubjectsUnsupported;
    }
}
