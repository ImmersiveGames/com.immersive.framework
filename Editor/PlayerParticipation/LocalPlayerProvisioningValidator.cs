using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-mutating validation for manual local Player technical-host provisioning authoring.
    /// </summary>
    internal static class LocalPlayerProvisioningValidator
    {
        internal static FrameworkAuthoringValidationReport Validate(
            LocalPlayerProvisioningAuthoring authoring,
            GameApplicationAsset gameApplication)
        {
            FrameworkValidationMode validationMode = gameApplication != null
                ? gameApplication.ValidationMode
                : FrameworkValidationMode.Standard;
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (authoring == null)
            {
                report.AddError("Local Player Provisioning Authoring is missing.", null);
                return report;
            }

            PlayerInputManager manager = authoring.PlayerInputManager;
            if (manager == null)
            {
                report.AddError(
                    "Local Player Provisioning requires an explicit PlayerInputManager reference. No runtime singleton lookup or fallback manager is allowed.",
                    authoring);
                return report;
            }

            if (manager.joinBehavior != PlayerJoinBehavior.JoinPlayersManually)
            {
                report.AddError(
                    $"PlayerInputManager '{manager.name}' must use Join Players Manually. Current join behavior is '{manager.joinBehavior}'. Automatic Unity join paths bypass framework admission.",
                    manager);
            }

            if (manager.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
            {
                report.AddError(
                    $"PlayerInputManager '{manager.name}' must use Invoke C# Events notifications. Current notification behavior is '{manager.notificationBehavior}'. The provisioning bridge requires the typed joined callback for correlation and divergence detection.",
                    manager);
            }

            GameObject playerPrefab = manager.playerPrefab;
            if (playerPrefab == null)
            {
                report.AddError(
                    $"PlayerInputManager '{manager.name}' has no Player Prefab. Manual provisioning cannot create a Local Player Host.",
                    manager);
            }
            else
            {
                PlayerInput prefabPlayerInput = playerPrefab.GetComponent<PlayerInput>();
                if (prefabPlayerInput == null)
                {
                    report.AddError(
                        $"Player Prefab '{playerPrefab.name}' has no PlayerInput component.",
                        playerPrefab);
                }

                LocalPlayerHostAuthoring prefabHost =
                    playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
                if (prefabHost == null)
                {
                    report.AddError(
                        $"Player Prefab '{playerPrefab.name}' has no LocalPlayerHostAuthoring. The provisioning prefab must declare a stable technical host rather than a Logical Actor.",
                        playerPrefab);
                }
                else
                {
                    report.AddRange(LocalPlayerHostAuthoringValidator.Validate(prefabHost));
                    if (prefabPlayerInput != null &&
                        !ReferenceEquals(prefabHost.PlayerInput, prefabPlayerInput))
                    {
                        report.AddError(
                            $"LocalPlayerHostAuthoring on prefab '{playerPrefab.name}' does not resolve the prefab PlayerInput.",
                            prefabHost);
                    }
                }
            }

            if (gameApplication == null)
            {
                report.AddWarning(
                    "Game Application is unavailable, so configured Player Slot count cannot be compared with the PlayerInputManager technical ceiling.",
                    authoring);
            }
            else
            {
                int configuredSlots = gameApplication.LocalPlayerSlotCount;
                int technicalCeiling = manager.maxPlayerCount;
                if (configuredSlots <= 0)
                {
                    report.AddError(
                        "Game Application has no configured Local Player Slots. Provisioning cannot allocate a product Slot.",
                        gameApplication);
                }
                else if (technicalCeiling > 0 && configuredSlots > technicalCeiling)
                {
                    report.AddError(
                        $"Game Application configures {configuredSlots} Local Player Slots, but PlayerInputManager maxPlayerCount is {technicalCeiling}. The Session product capacity cannot exceed the authored Unity technical ceiling.",
                        manager);
                }
                else
                {
                    report.AddInfo(
                        $"Player Slot capacity is compatible. configuredSlots='{configuredSlots}' technicalMaxPlayers='{technicalCeiling}'.",
                        manager);
                }
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Local Player Provisioning authoring is valid. manager='{manager.name}' joinBehavior='{manager.joinBehavior}' notificationBehavior='{manager.notificationBehavior}' playerPrefab='{(playerPrefab != null ? playerPrefab.name : string.Empty)}' maxPlayerCount='{manager.maxPlayerCount}'.",
                    authoring);
            }

            return report;
        }
    }
}
