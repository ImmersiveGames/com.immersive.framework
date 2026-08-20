using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Validation for immutable Player participation Profiles and their
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

            PlayerActorSelectionDuplicatePolicy actorSelectionPolicy =
                gameApplication.PlayerActorSelectionDuplicatePolicy;
            if (!gameApplication.HasDefinedPlayerActorSelectionDuplicatePolicy)
            {
                report.AddError(
                    $"Player Actor duplicate-selection policy '{actorSelectionPolicy}' is invalid. Choose an explicit value in the Game Application.",
                    gameApplication);
            }

            if (!gameApplication.PlayerSessionEnabled)
            {
                report.AddOptionalSkip(
                    "Player Session is disabled, so Player participation configuration is not required for this Game Application.",
                    gameApplication);
                return report;
            }

            PlayerSessionProfile playerSessionProfile =
                gameApplication.DefaultPlayerSessionProfile;
            if (playerSessionProfile == null)
            {
                report.AddError(
                    "Player Session is enabled but has no Default Player Session Profile.",
                    gameApplication);
                return report;
            }

            if (!playerSessionProfile.TryValidate(out string profileIssue))
            {
                report.AddError(
                    $"Player Session Profile '{playerSessionProfile.name}' is invalid. {profileIssue}",
                    playerSessionProfile);
                return report;
            }

            IReadOnlyList<PlayerSlotProfile> configuredSlots =
                playerSessionProfile.SupportedSlots;

            var configuredProfileIndices = new Dictionary<PlayerSlotProfile, int>();
            var configuredIdentityIndices = new Dictionary<PlayerSlotId, int>();

            for (int index = 0; index < configuredSlots.Count; index++)
            {
                PlayerSlotProfile profile = configuredSlots[index];
                if (profile == null)
                {
                    report.AddError(
                        $"Player Session Profile Supported Slots[{index}] is missing. Every configured allocation position must reference a PlayerSlotProfile.",
                        playerSessionProfile);
                    continue;
                }

                if (configuredProfileIndices.TryGetValue(profile, out int firstProfileIndex))
                {
                    report.AddError(
                        $"Player Session Profile Supported Slots[{index}] repeats PlayerSlotProfile '{profile.name}' already configured at index {firstProfileIndex}. Each configured Slot requires one distinct Profile reference.",
                        playerSessionProfile);
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
                        $"Player Session Profile Supported Slots[{index}] Profile '{profile.name}' duplicates PlayerSlotId '{playerSlotId}' already owned by Profile '{firstProfile.name}' at index {firstIdentityIndex}.",
                        playerSessionProfile);
                    continue;
                }

                configuredIdentityIndices.Add(playerSlotId, index);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Player Session participation configuration is valid. supportedSlots='{configuredSlots.Count}' allocationPolicy='FirstAvailableByConfiguredOrder' actorSelectionPolicy='{actorSelectionPolicy}'.",
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

            report.AddRange(
                PlayerActorSelectionAuthoringValidator.ValidateProjectActorSelectionProfiles(
                    validationMode));

            if (slotProfileGuids.Length == 0)
            {
                report.AddOptionalSkip(
                    "No PlayerSlotProfile assets exist in the project. Player participation remains optional until explicit Slot Profiles are authored.",
                    null);
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Player participation Profile project validation passed. slotProfiles='{validSlotProfiles}'. Activity requirements are configured inline.",
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

            if (profile.DefaultActorProfile != null)
            {
                report.AddRange(
                    PlayerActorSelectionAuthoringValidator.ValidateActorProfile(
                        profile.DefaultActorProfile,
                        false,
                        validationMode));
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
                    $"Player Slot Profile is valid. playerSlotId='{playerSlotId}' displayOrder='{profile.DisplayOrder}' defaultActorProfile='{(profile.DefaultActorProfile != null ? profile.DefaultActorProfile.name : string.Empty)}'.",
                    profile);
            }

            return report;
        }
    }
}
