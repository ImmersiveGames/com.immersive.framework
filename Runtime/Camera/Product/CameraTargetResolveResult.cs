using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Result emitted when a camera product surface resolves follow/look-at targets from an explicit source.
    /// </summary>
    public readonly struct CameraTargetResolveResult
    {
        public CameraTargetResolveResult(
            CameraOperationStatus status,
            CameraTargetSourceDescriptor source,
            CameraResolvedTargets targets,
            CameraIssue[] issues,
            string blockingIssue,
            string diagnosticSummary)
        {
            Status = status;
            Source = source;
            Targets = targets;
            Issues = issues ?? Array.Empty<CameraIssue>();
            BlockingIssue = blockingIssue.NormalizeText();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraOperationStatus Status { get; }

        public CameraTargetSourceDescriptor Source { get; }

        public CameraResolvedTargets Targets { get; }

        public CameraIssue[] Issues { get; }

        public string BlockingIssue { get; }

        public string DiagnosticSummary { get; }

        public bool IsSucceeded => Status == CameraOperationStatus.Succeeded || Status == CameraOperationStatus.SucceededWithWarnings;

        public bool IsBlocked => Status == CameraOperationStatus.Blocked;

        public static CameraTargetResolveResult NotRun(CameraTargetSourceDescriptor source)
        {
            return new CameraTargetResolveResult(
                CameraOperationStatus.NotRun,
                source,
                CameraResolvedTargets.None,
                Array.Empty<CameraIssue>(),
                string.Empty,
                "Camera target resolution has not run.");
        }

        public static CameraTargetResolveResult Succeeded(
            CameraTargetSourceDescriptor source,
            CameraResolvedTargets targets,
            string diagnosticSummary = "")
        {
            return new CameraTargetResolveResult(
                CameraOperationStatus.Succeeded,
                source,
                targets,
                Array.Empty<CameraIssue>(),
                string.Empty,
                diagnosticSummary);
        }

        public static CameraTargetResolveResult SucceededWithWarnings(
            CameraTargetSourceDescriptor source,
            CameraResolvedTargets targets,
            string diagnosticSummary,
            params CameraIssue[] issues)
        {
            return new CameraTargetResolveResult(
                CameraOperationStatus.SucceededWithWarnings,
                source,
                targets,
                issues,
                string.Empty,
                diagnosticSummary);
        }

        public static CameraTargetResolveResult Blocked(
            CameraTargetSourceDescriptor source,
            string blockingIssue,
            string diagnosticSummary = "",
            params CameraIssue[] issues)
        {
            string normalizedIssue = blockingIssue.NormalizeTextOrFallback("Camera target resolution was blocked.");
            CameraIssue[] resultIssues = issues == null || issues.Length == 0
                ? new[] { CameraIssue.Blocking("camera.target-resolution.blocked", normalizedIssue) }
                : issues;

            return new CameraTargetResolveResult(
                CameraOperationStatus.Blocked,
                source,
                CameraResolvedTargets.None,
                resultIssues,
                normalizedIssue,
                diagnosticSummary);
        }

        public static CameraTargetResolveResult ValidateRequirements(
            CameraTargetSourceDescriptor source,
            CameraResolvedTargets targets,
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement)
        {
            if (followRequirement == CameraTargetRequirement.Required && !targets.HasFollowTarget)
            {
                const string issue = "Camera follow target is required but was not resolved.";
                return Blocked(
                    source,
                    issue,
                    "Required follow target missing.",
                    CameraIssue.Blocking("camera.target.follow.missing", issue));
            }

            if (lookAtRequirement == CameraTargetRequirement.Required && !targets.HasLookAtTarget)
            {
                const string issue = "Camera look-at target is required but was not resolved.";
                return Blocked(
                    source,
                    issue,
                    "Required look-at target missing.",
                    CameraIssue.Blocking("camera.target.look-at.missing", issue));
            }

            if (lookAtRequirement == CameraTargetRequirement.Optional && !targets.HasLookAtTarget)
            {
                return SucceededWithWarnings(
                    source,
                    targets,
                    "Optional look-at target was not resolved.",
                    CameraIssue.Warning("camera.target.look-at.optional-missing", "Optional camera look-at target was not resolved."));
            }

            return Succeeded(source, targets, "Camera target requirements satisfied.");
        }
    }
}
