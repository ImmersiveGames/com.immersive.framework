using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// P3D Editor-only validation for Activity participation Projection and Requirements authoring.
    /// It reports issues only and never mutates Activity or Profile assets.
    /// </summary>
    internal static class ActivityParticipationProjectionAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateActivity(ActivityAsset activity)
        {
            var report = new FrameworkAuthoringValidationReport(FrameworkValidationMode.Standard);

            if (activity == null)
            {
                report.AddError("Activity is missing for Player participation validation.", null);
                return report;
            }

            PlayerParticipationRequirementsProfile requirements =
                activity.PlayerParticipationRequirementsProfile;
            ActivityParticipationProjectionProfile projection =
                activity.PlayerParticipationProjectionProfile;

            if (requirements == null)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' is missing its mandatory Player Participation Requirements Profile. Use an explicit None Profile when the Activity requires no Players.",
                    activity);
            }
            else
            {
                report.AddRange(PlayerParticipationAuthoringValidator.ValidateRequirementsProfile(requirements));
            }

            if (projection == null)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' is missing its mandatory Activity Participation Projection Profile. Null never means No Slots or All Joined Slots.",
                    activity);
            }
            else
            {
                report.AddRange(ValidateProjectionProfile(projection));
            }

            if (requirements != null && projection != null &&
                projection.TryCreateDescriptor(out ActivityParticipationProjectionDescriptor descriptor, out _))
            {
                if (descriptor.ProjectsNoSlots && !requirements.IsExplicitNone)
                {
                    report.AddError(
                        $"Activity '{activity.ActivityName}' projects No Slots but requires participation level '{requirements.RequirementLevel}'. Use an explicit None requirements Profile or select a participant projection.",
                        activity);
                }
                else if (descriptor.ProjectsNoSlots && requirements.IsExplicitNone)
                {
                    report.AddInfo(
                        "Activity Player participation is explicitly configured for no Players: Projection='NoSlots', Requirements='None'.",
                        activity);
                }
                else if (requirements.IsExplicitNone)
                {
                    report.AddInfo(
                        $"Activity projects '{descriptor.Mode}' while Requirements='None'. Projected Slots impose no admission-readiness requirement in this configuration.",
                        activity);
                }
                else if (descriptor.ProjectsAllJoinedSlots && descriptor.AllowsZeroParticipants)
                {
                    report.AddInfo(
                        $"Activity projects All Joined Slots and explicitly allows zero participants while requiring '{requirements.RequirementLevel}' from every projected Slot.",
                        activity);
                }
            }

            if (report.IsValid)
            {
                string projectionLabel = projection != null
                    ? projection.ProjectionMode.ToString()
                    : "Missing";
                string requirementLabel = requirements != null
                    ? requirements.RequirementLevel.ToString()
                    : "Missing";
                report.AddInfo(
                    $"Activity Player participation authoring is valid. projection='{projectionLabel}' requirements='{requirementLabel}'.",
                    activity);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectionProfile(
            ActivityParticipationProjectionProfile profile)
        {
            var report = new FrameworkAuthoringValidationReport(FrameworkValidationMode.Standard);

            if (profile == null)
            {
                report.AddError("Activity Participation Projection Profile is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                report.AddWarning(
                    "Activity Participation Projection Profile has no Display Name. Give the reusable projection a designer-facing name.",
                    profile);
            }

            if (!profile.TryCreateDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string issue))
            {
                report.AddError(issue, profile);
                return report;
            }

            report.AddInfo(
                $"Activity Participation Projection Profile is valid. mode='{descriptor.Mode}' zeroParticipants='{descriptor.ZeroParticipantPolicy}' explicitSlots='{descriptor.ExplicitSlotProfiles.Count}'.",
                profile);
            return report;
        }
    }
}
