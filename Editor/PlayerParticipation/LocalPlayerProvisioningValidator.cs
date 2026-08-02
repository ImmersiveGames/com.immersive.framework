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

            LocalPlayerProvisioningValidationResult provisioningValidation =
                LocalPlayerProvisioningConfigurationRules.Validate(
                    new[] { authoring },
                    true,
                    nameof(LocalPlayerProvisioningValidator),
                    "editor-authoring-validation");
            for (int index = 0; index < provisioningValidation.Issues.Count; index++)
            {
                LocalPlayerProvisioningIssue issue = provisioningValidation.Issues[index];
                if (issue.Blocking)
                    report.AddError(issue.Message, authoring);
                else
                    report.AddWarning(issue.Message, authoring);
            }
            if (provisioningValidation.Failed)
                return report;

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

            GameObject localPlayerHostPrefab = authoring.LocalPlayerHostPrefab;
            if (localPlayerHostPrefab == null)
            {
                report.AddError(
                    "Local Player Provisioning requires an explicit Local Player Host Prefab. " +
                    "Do not configure this through the hidden PlayerInputManager field.",
                    authoring);
            }
            else
            {
                if (authoring.HasManagerPrefabDivergence)
                {
                    report.AddError(
                        $"PlayerInputManager '{manager.name}' has Player Prefab '{manager.playerPrefab.name}', but the authored Local Player Host Prefab is '{localPlayerHostPrefab.name}'. Runtime boot rejects this divergence rather than overwriting it.",
                        manager);
                }
                else if (manager.playerPrefab == null)
                {
                    report.AddInfo(
                        $"PlayerInputManager '{manager.name}' has no Player Prefab yet. Framework boot will materialize authored Local Player Host Prefab '{localPlayerHostPrefab.name}'.",
                        authoring);
                }

                PlayerInput prefabPlayerInput = localPlayerHostPrefab.GetComponent<PlayerInput>();
                if (prefabPlayerInput == null)
                {
                    report.AddError(
                        $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no PlayerInput component.",
                        localPlayerHostPrefab);
                }

                LocalPlayerHostAuthoring prefabHost =
                    localPlayerHostPrefab.GetComponent<LocalPlayerHostAuthoring>();
                if (prefabHost == null)
                {
                    report.AddError(
                        $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no LocalPlayerHostAuthoring. The provisioning prefab must declare a stable technical host rather than a Logical Actor.",
                        localPlayerHostPrefab);
                }
                else
                {
                    report.AddRange(LocalPlayerHostAuthoringValidator.Validate(prefabHost));
                    if (prefabPlayerInput != null &&
                        prefabHost.PlayerInput != prefabPlayerInput)
                    {
                        report.AddError(
                        $"LocalPlayerHostAuthoring on Local Player Host Prefab '{localPlayerHostPrefab.name}' does not resolve the prefab PlayerInput.",
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
                    $"Local Player Provisioning authoring is valid. manager='{manager.name}' joinBehavior='{manager.joinBehavior}' notificationBehavior='{manager.notificationBehavior}' localPlayerHostPrefab='{(localPlayerHostPrefab != null ? localPlayerHostPrefab.name : string.Empty)}' materialized='{authoring.IsManagerPrefabMaterialized}' maxPlayerCount='{manager.maxPlayerCount}'.",
                    authoring);
            }

            return report;
        }
    }
}
