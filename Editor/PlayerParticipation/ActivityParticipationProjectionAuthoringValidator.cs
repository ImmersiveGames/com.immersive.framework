using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Editor-only validation for Activity-owned participation Projection and Requirement authoring.
    /// It reports issues only and never mutates Activity assets.
    /// </summary>
    internal static class ActivityParticipationProjectionAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateActivity(ActivityAsset activity)
        {
            return ValidateActivity(
                activity,
                FrameworkValidationMode.Standard);
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
                        validationMode));
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
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (activity == null)
            {
                report.AddError("Activity is missing for Player participation validation.", null);
                return report;
            }

            PlayerParticipationRequirementLevel requirementLevel =
                activity.PlayerParticipationRequirementLevel;
            bool requirementsValid =
                activity.HasDefinedPlayerParticipationRequirementLevel;
            ActivityParticipationProjectionDescriptor descriptor = default;
            bool projectionValid = activity.TryGetPlayerParticipationProjectionDescriptor(
                out descriptor,
                out string projectionIssue);

            if (!requirementsValid)
            {
                report.AddError(
                    $"Activity '{activity.ActivityName}' has an invalid Player participation Requirement Level.",
                    activity);
            }

            if (!projectionValid)
            {
                report.AddError(projectionIssue, activity);
            }

            if (requirementsValid && projectionValid)
            {
                report.AddInfo("Players / Readiness", activity);
                report.AddInfo($"Selected requirement: {requirementLevel}.", activity);
                AddRuntimeEvidenceReport(report, requirementLevel, activity);
                report.AddInfo("Authoring evidence: projection, explicit Slot references, duplicate Slot identities and zero-participant policy are validated from this ActivityAsset.", activity);

                if (descriptor.ProjectsNoSlots &&
                    requirementLevel != PlayerParticipationRequirementLevel.None)
                {
                    report.AddError(
                        $"Activity '{activity.ActivityName}' projects No Slots but requires participation level '{requirementLevel}'. Use Requirement Level None or select a participant projection.",
                        activity);
                }
                else if (descriptor.ProjectsNoSlots &&
                    requirementLevel == PlayerParticipationRequirementLevel.None)
                {
                    report.AddInfo(
                        "Activity Player participation is explicitly configured for no Players: Projection='NoSlots', Requirements='None'.",
                        activity);
                }
                else if (requirementLevel == PlayerParticipationRequirementLevel.None)
                {
                    report.AddInfo(
                        $"Activity projects '{descriptor.Mode}' while Requirements='None'. Projected Slots impose no admission-readiness requirement in this configuration.",
                        activity);
                }
                else if (descriptor.AllowsZeroParticipants)
                {
                    report.AddInfo(
                        $"Activity projects '{descriptor.Mode}' and explicitly allows zero participants while requiring '{requirementLevel}' from every projected Logical Player.",
                        activity);
                }

                AddCoveredPlayerProgressionWarning(
                    report,
                    activity,
                    descriptor,
                    requirementLevel);
            }

            if (report.IsValid && requirementsValid && projectionValid)
            {
                report.AddInfo(
                    $"Activity Player participation authoring is valid. projection='{descriptor.Mode}' allowsZeroParticipants='{descriptor.AllowsZeroParticipants}' requirementLevel='{requirementLevel}'.",
                    activity);
            }

            return report;
        }

        private static void AddCoveredPlayerProgressionWarning(
            FrameworkAuthoringValidationReport report,
            ActivityAsset activity,
            ActivityParticipationProjectionDescriptor descriptor,
            PlayerParticipationRequirementLevel requirementLevel)
        {
            if (!activity.HasDefinedEntryReadinessPolicy ||
                activity.EntryReadinessPolicy != ActivityEntryReadinessPolicy.WaitCovered ||
                descriptor.Mode != ActivityParticipationProjectionMode.ExplicitSlots ||
                requirementLevel == PlayerParticipationRequirementLevel.None)
            {
                return;
            }

            report.AddWarning(
                $"Activity '{activity.ActivityName}' uses WaitCovered with ExplicitSlots and Player requirement '{requirementLevel}'. " +
                "This is valid, but the Activity can remain covered indefinitely when a required Slot still needs Join or later Player progression and that progression can only be triggered from content hidden by the cover. " +
                "Ensure the required Player state is satisfied before entry, progresses automatically, or can be advanced through a control-plane action available outside the covered Activity. " +
                "Use WaitVisible when Player Join or selection is intentionally part of the visible Activity flow.",
                activity);
        }

        private static void AddRuntimeEvidenceReport(
            FrameworkAuthoringValidationReport report,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityAsset activity)
        {
            var required = PlayerParticipationReadinessRequirements.GetRequiredEvidence(requirementLevel);
            if (required.Count == 0)
            {
                report.AddInfo("Runtime-dependent evidence: none. This Activity does not wait for Player readiness.", activity);
                return;
            }

            report.AddInfo("Cumulative runtime evidence required:", activity);
            for (int index = 0; index < required.Count; index++)
            {
                report.AddInfo($"  {index + 1}. {PlayerParticipationReadinessRequirements.GetDisplayName(required[index])}", activity);
            }
            report.AddInfo("Runtime-dependent evidence: joined Slot, Actor selection, Logical Actor preparation, input eligibility, Camera eligibility and gameplay eligibility require runtime evidence. They are not provable from this ActivityAsset alone.", activity);
        }
    }
}
