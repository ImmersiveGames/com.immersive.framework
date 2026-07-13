using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// P3C validation for immutable Player participation Profiles referenced by Game/Application.
    /// This validator does not create assets, mutate Profiles or evaluate runtime participation state.
    /// </summary>
    internal static class PlayerParticipationAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication)
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

                if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string issue))
                {
                    report.AddError(
                        $"Local Player Slots[{index}] is invalid. {issue}",
                        profile);
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
    }
}
