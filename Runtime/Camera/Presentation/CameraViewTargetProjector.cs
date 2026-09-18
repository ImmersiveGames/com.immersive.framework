using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Presentation adapter for zero/single-Subject targets and explicit Group intent.
    /// Physical Cinemachine group projection remains a separate Composer operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C/D explicit View input adapter for current presentation capability.")]
    public static class CameraViewTargetProjector
    {
        public static CameraViewTargetProjectionResult Project(
            CameraViewPresentationInput input,
            CameraRigPresentationIntent presentationIntent,
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement)
        {
            if (input == null || !input.IsValid)
            {
                return Rejected(
                    CameraViewTargetProjectionStatus.RejectedInvalidInput,
                    input,
                    "View target projection requires valid resolved presentation input.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Undefined ||
                !IsDefined(followRequirement) ||
                !IsDefined(lookAtRequirement))
            {
                return Rejected(
                    CameraViewTargetProjectionStatus.RejectedInvalidPresentation,
                    input,
                    "View target projection requires defined presentation intent and target requirements.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Fixed)
            {
                return new CameraViewTargetProjectionResult(
                    CameraViewTargetProjectionStatus.SucceededNoTargets,
                    input,
                    CameraResolvedTargets.None,
                    System.Array.Empty<CameraIssue>(),
                    string.Empty,
                    "Fixed presentation consumes no Camera Subjects as tracking targets.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Group &&
                input.Cardinality != CameraViewSubjectCardinality.Zero)
            {
                return new CameraViewTargetProjectionResult(
                    CameraViewTargetProjectionStatus.SucceededGroup,
                    input,
                    CameraResolvedTargets.None,
                    System.Array.Empty<CameraIssue>(),
                    string.Empty,
                    $"Group presentation retained '{input.SubjectCount}' ordered Subjects for physical group projection.");
            }

            if (input.Cardinality == CameraViewSubjectCardinality.Many)
            {
                const string issue =
                    "Multiple Subjects require explicit Group presentation; Follow remains single-target.";
                return new CameraViewTargetProjectionResult(
                    CameraViewTargetProjectionStatus.BlockedMultipleSubjectsUnsupported,
                    input,
                    CameraResolvedTargets.None,
                    new[]
                    {
                        CameraIssue.Blocking(
                            "camera.view-presentation.multiple-subjects-unsupported",
                            issue)
                    },
                    issue,
                    "Logical View remains valid; no implicit presentation transformation was applied.");
            }

            if (input.Cardinality == CameraViewSubjectCardinality.Zero)
            {
                if (followRequirement == CameraTargetRequirement.Required ||
                    lookAtRequirement == CameraTargetRequirement.Required)
                {
                    const string issue =
                        "Tracking presentation requires one resolved Camera Subject, but the View is empty.";
                    return new CameraViewTargetProjectionResult(
                        CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing,
                        input,
                        CameraResolvedTargets.None,
                        new[]
                        {
                            CameraIssue.Blocking(
                                "camera.view-presentation.subject-required",
                                issue)
                        },
                        issue,
                        "Required View Subject is unavailable; no legacy target fallback was used.");
                }

                CameraIssue[] optionalIssues =
                    lookAtRequirement == CameraTargetRequirement.Optional
                        ? new[]
                        {
                            CameraIssue.Warning(
                                "camera.view-presentation.look-at.optional-missing",
                                "Optional View-derived look-at target was not resolved.")
                        }
                        : System.Array.Empty<CameraIssue>();
                return new CameraViewTargetProjectionResult(
                    CameraViewTargetProjectionStatus.SucceededNoTargets,
                    input,
                    CameraResolvedTargets.None,
                    optionalIssues,
                    string.Empty,
                    "View presentation input contains zero Subjects and requires no tracking target.");
            }

            Transform observation = input.Subjects[0].Subject.Observation;
            var targets = new CameraResolvedTargets(
                followRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : observation,
                lookAtRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : observation);
            return new CameraViewTargetProjectionResult(
                CameraViewTargetProjectionStatus.SucceededSingleSubject,
                input,
                targets,
                System.Array.Empty<CameraIssue>(),
                string.Empty,
                "The single role-neutral Subject observation was projected to each active presentation target role.");
        }

        private static CameraViewTargetProjectionResult Rejected(
            CameraViewTargetProjectionStatus status,
            CameraViewPresentationInput input,
            string issue)
        {
            return new CameraViewTargetProjectionResult(
                status,
                input,
                CameraResolvedTargets.None,
                new[]
                {
                    CameraIssue.Blocking(
                        "camera.view-presentation.invalid",
                        issue)
                },
                issue,
                issue);
        }

        private static bool IsDefined(CameraTargetRequirement requirement)
        {
            return requirement is
                CameraTargetRequirement.NotUsed or
                CameraTargetRequirement.Optional or
                CameraTargetRequirement.Required;
        }
    }
}
