using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Editor-only validation for Activity-owned participation Projection and Requirements authoring.
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

        internal static FrameworkAuthoringValidationReport ValidateProjectAssets(
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

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
            if (report.IsValid)
            {
                report.AddInfo(
                    $"Activity participation project validation passed. activities='{activityGuids.Length}'.",
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

            bool requirementsValid = requirements != null && requirements.HasDefinedRequirementLevel;
            ActivityParticipationProjectionDescriptor descriptor = default;
            bool projectionValid = activity.TryGetPlayerParticipationProjectionDescriptor(
                out descriptor,
                out string projectionIssue);

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

            if (!projectionValid)
            {
                report.AddError(projectionIssue, activity);
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
    }
}
