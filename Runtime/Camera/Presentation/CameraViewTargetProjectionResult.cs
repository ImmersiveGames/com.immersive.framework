using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit result of adapting View Subjects to current presentation capability.
    /// Shared Follow retains multiple Subjects on Input and never fabricates one target.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C/D View presentation projection result.")]
    public sealed class CameraViewTargetProjectionResult
    {
        internal CameraViewTargetProjectionResult(
            CameraViewTargetProjectionStatus status,
            CameraViewPresentationInput input,
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

        public CameraViewTargetProjectionStatus Status { get; }
        public CameraViewPresentationInput Input { get; }
        public CameraResolvedTargets Targets { get; }
        public CameraIssue[] Issues { get; }
        public string BlockingIssue { get; }
        public string DiagnosticSummary { get; }
        public bool Succeeded => Status is
            CameraViewTargetProjectionStatus.SucceededNoTargets or
            CameraViewTargetProjectionStatus.SucceededSingleSubject or
            CameraViewTargetProjectionStatus.SucceededSharedFollow;
        public bool Blocked => Status is
            CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing or
            CameraViewTargetProjectionStatus.BlockedMultipleSubjectsUnsupported;
    }
}
