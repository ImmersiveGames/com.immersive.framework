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
        "CAMERA-029-E Rig target projection for Composition presentation input.")]
    public static class CameraRigTargetProjector
    {
        public static CameraRigTargetProjectionResult Project(
            CameraCompositionPresentationInput input,
            CameraRigPresentationIntent presentationIntent,
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement)
        {
            if (input == null || !input.IsValid)
            {
                return Rejected(
                    CameraRigTargetProjectionStatus.RejectedInvalidInput,
                    input,
                    "Rig target projection requires valid resolved Composition presentation input.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Undefined ||
                !IsDefined(followRequirement) ||
                !IsDefined(lookAtRequirement))
            {
                return Rejected(
                    CameraRigTargetProjectionStatus.RejectedInvalidPresentation,
                    input,
                    "Rig target projection requires defined presentation intent and target requirements.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Fixed)
            {
                return new CameraRigTargetProjectionResult(
                    CameraRigTargetProjectionStatus.SucceededNoTargets,
                    input,
                    CameraResolvedTargets.None,
                    System.Array.Empty<CameraIssue>(),
                    string.Empty,
                    "Fixed presentation consumes no Camera Subjects as tracking targets.");
            }

            if (presentationIntent == CameraRigPresentationIntent.Group &&
                input.Cardinality != CameraSubjectCardinality.Zero)
            {
                return new CameraRigTargetProjectionResult(
                    CameraRigTargetProjectionStatus.SucceededGroup,
                    input,
                    CameraResolvedTargets.None,
                    System.Array.Empty<CameraIssue>(),
                    string.Empty,
                    $"Group presentation retained '{input.SubjectCount}' ordered Subjects for physical group projection.");
            }

            if (input.Cardinality == CameraSubjectCardinality.Many)
            {
                const string issue =
                    "Multiple Subjects require explicit Group presentation; Follow remains single-target.";
                return new CameraRigTargetProjectionResult(
                    CameraRigTargetProjectionStatus.BlockedMultipleSubjectsUnsupported,
                    input,
                    CameraResolvedTargets.None,
                    new[]
                    {
                        CameraIssue.Blocking(
                            "camera.rig-presentation.multiple-subjects-unsupported",
                            issue)
                    },
                    issue,
                    "Composition input remains valid; no implicit presentation transformation was applied.");
            }

            if (input.Cardinality == CameraSubjectCardinality.Zero)
            {
                if (followRequirement == CameraTargetRequirement.Required ||
                    lookAtRequirement == CameraTargetRequirement.Required)
                {
                    const string issue =
                        "Tracking presentation requires one resolved Camera Subject, but the Composition input is empty.";
                    return new CameraRigTargetProjectionResult(
                        CameraRigTargetProjectionStatus.BlockedRequiredSubjectMissing,
                        input,
                        CameraResolvedTargets.None,
                        new[]
                        {
                            CameraIssue.Blocking(
                                "camera.rig-presentation.subject-required",
                                issue)
                        },
                        issue,
                        "Required Composition Subject is unavailable; no target fallback was used.");
                }

                CameraIssue[] optionalIssues =
                    lookAtRequirement == CameraTargetRequirement.Optional
                        ? new[]
                        {
                            CameraIssue.Warning(
                                "camera.rig-presentation.look-at.optional-missing",
                                "Optional Composition-derived look-at target was not resolved.")
                        }
                        : System.Array.Empty<CameraIssue>();
                return new CameraRigTargetProjectionResult(
                    CameraRigTargetProjectionStatus.SucceededNoTargets,
                    input,
                    CameraResolvedTargets.None,
                    optionalIssues,
                    string.Empty,
                    "Composition presentation input contains zero Subjects and requires no tracking target.");
            }

            Transform observation = input.Subjects[0].Subject.Observation;
            var targets = new CameraResolvedTargets(
                followRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : observation,
                lookAtRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : observation);
            return new CameraRigTargetProjectionResult(
                CameraRigTargetProjectionStatus.SucceededSingleSubject,
                input,
                targets,
                System.Array.Empty<CameraIssue>(),
                string.Empty,
                "The single role-neutral Subject observation was projected to each active presentation target role.");
        }

        private static CameraRigTargetProjectionResult Rejected(
            CameraRigTargetProjectionStatus status,
            CameraCompositionPresentationInput input,
            string issue)
        {
            return new CameraRigTargetProjectionResult(
                status,
                input,
                CameraResolvedTargets.None,
                new[]
                {
                    CameraIssue.Blocking(
                        "camera.rig-presentation.invalid",
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
