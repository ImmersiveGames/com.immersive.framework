using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;

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
            return ValidateActivity(
                activity,
                FrameworkValidationMode.Standard,
                includeReferencedProfileValidation: true);
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectionProfile(
            ActivityParticipationProjectionProfile profile)
        {
            return ValidateProjectionProfile(profile, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectAssets(
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            string[] projectionGuids =
                AssetDatabase.FindAssets("t:ActivityParticipationProjectionProfile");
            for (int index = 0; index < projectionGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(projectionGuids[index]);
                ActivityParticipationProjectionProfile profile =
                    AssetDatabase.LoadAssetAtPath<ActivityParticipationProjectionProfile>(assetPath);
                if (profile == null)
                {
                    report.AddError(
                        $"Activity Participation Projection Profile at '{assetPath}' could not be loaded.",
                        null);
                    continue;
                }

                report.AddRange(ValidateProjectionProfile(profile, validationMode));
            }

            string[] activityGuids = AssetDatabase.FindAssets("t:ActivityAsset");
            for (int index = 0; index < activityGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(activityGuids[index]);
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(assetPath);
                if (activity == null)
                {
                    report.AddError($"Activity asset at '{assetPath}' could not be loaded.", null);
                    continue;
                }

                report.AddRange(
                    ValidateActivity(
                        activity,
                        validationMode,
                        includeReferencedProfileValidation: false));
            }

            if (activityGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No Activity assets exist in the project. Activity Player participation authoring validation is skipped.",
                    null);
            }
            else if (projectionGuids.Length == 0)
            {
                report.AddError(
                    "No Activity Participation Projection Profiles exist. Every Activity requires an explicit Projection Profile; create the official Activity Projection Set.",
                    null);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Activity participation project validation passed. activities='{activityGuids.Length}' projectionProfiles='{projectionGuids.Length}'.",
                    null);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivity(
            ActivityAsset activity,
            FrameworkValidationMode validationMode,
            bool includeReferencedProfileValidation)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (activity == null)
            {
                report.AddError("Activity is missing for Player participation validation.", null);
                return report;
            }

            PlayerParticipationRequirementsProfile requirements =
                activity.PlayerParticipationRequirementsProfile;
            ActivityParticipationProjectionProfile projection =
                activity.PlayerParticipationProjectionProfile;

            bool requirementsValid = requirements != null && requirements.HasDefinedRequirementLevel;
            ActivityParticipationProjectionDescriptor descriptor = default;
            bool projectionValid = projection != null &&
                projection.TryCreateDescriptor(out descriptor, out _);

            if (requirements == null)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' is missing its mandatory Player Participation Requirements Profile. Use an explicit None Profile when the Activity requires no Players.",
                    activity);
            }
            else if (includeReferencedProfileValidation)
            {
                report.AddRange(
                    PlayerParticipationAuthoringValidator.ValidateRequirementsProfile(requirements));
            }

            if (projection == null)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' is missing its mandatory Activity Participation Projection Profile. Null never means No Slots or All Joined Slots.",
                    activity);
            }
            else if (includeReferencedProfileValidation)
            {
                report.AddRange(ValidateProjectionProfile(projection, validationMode));
            }

            if (requirementsValid && projectionValid)
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

            if (report.IsValid && requirementsValid && projectionValid)
            {
                report.AddInfo(
                    $"Activity Player participation authoring is valid. projection='{descriptor.Mode}' requirements='{requirements.RequirementLevel}'.",
                    activity);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateProjectionProfile(
            ActivityParticipationProjectionProfile profile,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

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
