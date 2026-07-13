using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// P3C validation for immutable Player participation Profiles and their
    /// ordered Game/Application configuration. This validator never creates,
    /// repairs or mutates product assets.
    /// </summary>
    internal static class PlayerParticipationAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication)
        {
            return ValidateGameApplication(gameApplication, true);
        }

        /// <summary>
        /// Validates ordered Game/Application participation configuration.
        /// Model Readiness can skip repeated Profile detail messages because
        /// project-wide Profile validation is appended separately.
        /// </summary>
        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool includeConfiguredProfileValidation)
        {
            FrameworkValidationMode validationMode = gameApplication != null
                ? gameApplication.ValidationMode
                : FrameworkValidationMode.Standard;
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (gameApplication == null)
            {
                report.AddError("Game Application is missing for Player participation validation.", null);
                return report;
            }

            IReadOnlyList<PlayerSlotProfile> configuredSlots = gameApplication.LocalPlayerSlots;
            if (configuredSlots == null || configuredSlots.Count == 0)
            {
                report.AddError(
                    "Local Player Slots are missing. Configure at least one PlayerSlotProfile in allocation order; the framework does not create a fallback Player 1 Slot.",
                    gameApplication);
                return report;
            }

            var configuredProfileIndices = new Dictionary<PlayerSlotProfile, int>();
            var configuredIdentityIndices = new Dictionary<PlayerSlotId, int>();

            for (int index = 0; index < configuredSlots.Count; index++)
            {
                PlayerSlotProfile profile = configuredSlots[index];
                if (profile == null)
                {
                    report.AddError(
                        $"Local Player Slots[{index}] is missing. Every configured allocation position must reference a PlayerSlotProfile.",
                        gameApplication);
                    continue;
                }

                if (configuredProfileIndices.TryGetValue(profile, out int firstProfileIndex))
                {
                    report.AddError(
                        $"Local Player Slots[{index}] repeats PlayerSlotProfile '{profile.name}' already configured at index {firstProfileIndex}. Each configured seat requires one distinct Profile reference.",
                        gameApplication);
                    continue;
                }

                configuredProfileIndices.Add(profile, index);

                if (includeConfiguredProfileValidation)
                {
                    FrameworkAuthoringValidationReport profileReport =
                        ValidatePlayerSlotProfile(profile, false, validationMode);
                    report.AddRange(profileReport);
                }

                if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string identityIssue))
                {
                    if (!includeConfiguredProfileValidation)
                    {
                        report.AddError(identityIssue, profile);
                    }

                    continue;
                }

                if (configuredIdentityIndices.TryGetValue(playerSlotId, out int firstIdentityIndex))
                {
                    PlayerSlotProfile firstProfile = configuredSlots[firstIdentityIndex];
                    report.AddError(
                        $"Local Player Slots[{index}] Profile '{profile.name}' duplicates PlayerSlotId '{playerSlotId}' already owned by Profile '{firstProfile.name}' at index {firstIdentityIndex}.",
                        gameApplication);
                    continue;
                }

                configuredIdentityIndices.Add(playerSlotId, index);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Local Player Slot configuration is valid. configuredSlots='{configuredSlots.Count}' allocationPolicy='FirstAvailableByConfiguredOrder'.",
                    gameApplication);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidatePlayerSlotProfile(
            PlayerSlotProfile profile,
            bool includeProjectDuplicateScan)
        {
            return ValidatePlayerSlotProfile(
                profile,
                includeProjectDuplicateScan,
                FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateRequirementsProfile(
            PlayerParticipationRequirementsProfile profile)
        {
            var report = new FrameworkAuthoringValidationReport(FrameworkValidationMode.Standard);

            if (profile == null)
            {
                report.AddError("Player Participation Requirements Profile is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                report.AddWarning(
                    "Player Participation Requirements Profile has no Display Name. Give the reusable policy a designer-facing name.",
                    profile);
            }

            if (!profile.HasDefinedRequirementLevel)
            {
                report.AddError(
                    $"Player Participation Requirements Profile '{profile.name}' has an invalid Requirement Level.",
                    profile);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Participation Requirements Profile is valid. level='{profile.RequirementLevel}' explicitNone='{profile.IsExplicitNone}'.",
                    profile);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectProfiles(
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            string[] slotProfileGuids = AssetDatabase.FindAssets("t:PlayerSlotProfile");
            var identityOwners = new Dictionary<PlayerSlotId, PlayerSlotProfile>();
            int validSlotProfiles = 0;

            for (int index = 0; index < slotProfileGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(slotProfileGuids[index]);
                PlayerSlotProfile profile = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(assetPath);
                if (profile == null)
                {
                    report.AddError(
                        $"PlayerSlotProfile asset at '{assetPath}' could not be loaded.",
                        null);
                    continue;
                }

                FrameworkAuthoringValidationReport profileReport =
                    ValidatePlayerSlotProfile(profile, false, validationMode);
                report.AddRange(profileReport);

                if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out _))
                {
                    continue;
                }

                if (identityOwners.TryGetValue(playerSlotId, out PlayerSlotProfile firstOwner))
                {
                    report.AddError(
                        $"PlayerSlotId '{playerSlotId}' is duplicated by Profiles '{firstOwner.name}' and '{profile.name}'. Profile identity must be unique across the project.",
                        profile);
                    continue;
                }

                identityOwners.Add(playerSlotId, profile);
                validSlotProfiles++;
            }

            string[] requirementsGuids = AssetDatabase.FindAssets("t:PlayerParticipationRequirementsProfile");
            for (int index = 0; index < requirementsGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(requirementsGuids[index]);
                PlayerParticipationRequirementsProfile profile =
                    AssetDatabase.LoadAssetAtPath<PlayerParticipationRequirementsProfile>(assetPath);
                report.AddRange(ValidateRequirementsProfile(profile));
            }

            if (slotProfileGuids.Length == 0)
            {
                report.AddError(
                    "No PlayerSlotProfile assets exist in the project. Create explicit Slot Profiles before configuring local participation.",
                    null);
            }

            if (requirementsGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No Player Participation Requirements Profiles exist yet. Create the official requirements set before P3D Activity participation authoring.",
                    null);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Player participation Profile project validation passed. slotProfiles='{validSlotProfiles}' requirementsProfiles='{requirementsGuids.Length}'.",
                    null);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidatePlayerSlotProfile(
            PlayerSlotProfile profile,
            bool includeProjectDuplicateScan,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (profile == null)
            {
                report.AddError("Player Slot Profile is missing.", null);
                return report;
            }

            if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue))
            {
                report.AddError(issue, profile);
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                report.AddWarning(
                    $"PlayerSlotProfile '{profile.name}' has no Display Name. Slot identity remains valid, but product presentation is incomplete.",
                    profile);
            }

            if (includeProjectDuplicateScan)
            {
                string[] profileGuids = AssetDatabase.FindAssets("t:PlayerSlotProfile");
                for (int index = 0; index < profileGuids.Length; index++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(profileGuids[index]);
                    PlayerSlotProfile candidate = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(assetPath);
                    if (candidate == null || candidate == profile)
                    {
                        continue;
                    }

                    if (candidate.TryGetPlayerSlotId(out PlayerSlotId candidateId, out _) &&
                        candidateId == playerSlotId)
                    {
                        report.AddError(
                            $"PlayerSlotId '{playerSlotId}' is also owned by PlayerSlotProfile '{candidate.name}' at '{assetPath}'.",
                            profile);
                    }
                }
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Player Slot Profile is valid. playerSlotId='{playerSlotId}' displayOrder='{profile.DisplayOrder}'.",
                    profile);
            }

            return report;
        }
    }
}
