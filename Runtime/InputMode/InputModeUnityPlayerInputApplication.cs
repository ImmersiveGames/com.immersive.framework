using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Explicit PlayerInput application wrapper for F32E.
    /// It activates PlayerInput before selecting gameplay/UI action maps and delegates lock behavior to the F32D adapter.
    /// It is not an input manager and never owns PlayerInputManager, join flow, actor spawn or movement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32E explicit Unity PlayerInput application wrapper.")]
    public static class InputModeUnityPlayerInputApplication
    {
        public static InputModeUnityPlayerInputApplicationResult Apply(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputApplication));
            string normalizedReason = reason.NormalizeText();
            var issues = new List<InputModeUnityPlayerInputApplicationIssue>();

            if (plan == null || !plan.Succeeded)
            {
                InputModeKind requestedMode = plan == null ? InputModeKind.Unknown : plan.RequestedMode;
                InputModeUnityApplicationPlanOperation operation = plan == null
                    ? InputModeUnityApplicationPlanOperation.NoOperation
                    : plan.Operation;
                UnityInputActionMapName actionMapName = plan == null
                    ? UnityInputActionMapName.From(string.Empty)
                    : plan.ActionMapName;

                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.InvalidPlan,
                    requestedMode,
                    actionMapName,
                    normalizedSource,
                    "PlayerInput application requires a successful InputMode Unity application plan."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedInvalidPlan,
                    requestedMode,
                    operation,
                    actionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            if (playerInput == null)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.MissingPlayerInput,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    normalizedSource,
                    "PlayerInput application requires an explicit Unity PlayerInput instance."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedMissingPlayerInput,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            if (plan.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap)
            {
                return ApplyActivateAndSelectActionMap(plan, playerInput, normalizedSource, normalizedReason, issues);
            }

            if (plan.Operation == InputModeUnityApplicationPlanOperation.LockInput)
            {
                InputModeUnityPlayerInputAdapterResult adapterResult = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    playerInput,
                    normalizedSource,
                    normalizedReason);

                if (!adapterResult.Succeeded)
                {
                    issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                        InputModeUnityPlayerInputApplicationIssueKind.PlayerInputAdapterFailed,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        normalizedSource,
                        "PlayerInput lock application was rejected by the explicit PlayerInput adapter."));

                    return CreateResult(
                        InputModeUnityPlayerInputApplicationStatus.FailedPlayerInputAdapter,
                        plan.RequestedMode,
                        plan.Operation,
                        plan.ActionMapName,
                        adapterResult.AppliedActionMapName,
                        false,
                        false,
                        false,
                        false,
                        issues,
                        adapterResult,
                        normalizedSource,
                        normalizedReason);
                }

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.Succeeded,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    adapterResult.AppliedActionMapName,
                    adapterResult.Applied,
                    false,
                    adapterResult.SelectedActionMap,
                    adapterResult.DeactivatedPlayerInput,
                    issues,
                    adapterResult,
                    normalizedSource,
                    normalizedReason);
            }

            issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                InputModeUnityPlayerInputApplicationIssueKind.UnsupportedOperation,
                plan.RequestedMode,
                plan.ActionMapName,
                normalizedSource,
                "PlayerInput application does not support the requested plan operation."));

            return CreateResult(
                InputModeUnityPlayerInputApplicationStatus.FailedUnsupportedOperation,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                false,
                false,
                false,
                false,
                issues,
                null,
                normalizedSource,
                normalizedReason);
        }

        private static InputModeUnityPlayerInputApplicationResult ApplyActivateAndSelectActionMap(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            List<InputModeUnityPlayerInputApplicationIssue> issues)
        {
            if (!plan.ActionMapName.IsValid)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.MissingActionMap,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "PlayerInput application requires an explicit action map name before activation."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedMissingActionMap,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    source,
                    reason);
            }

            if (playerInput.actions == null)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.MissingActionAsset,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "PlayerInput application requires PlayerInput.actions before activation."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedMissingActionAsset,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    source,
                    reason);
            }

            InputActionMap map = playerInput.actions.FindActionMap(plan.ActionMapName.ToString(), false);
            if (map == null)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.MissingActionMap,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "PlayerInput application action asset does not contain the requested action map."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedMissingActionMap,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    source,
                    reason);
            }

            try
            {
                playerInput.ActivateInput();
            }
            catch (Exception exception)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.PlayerInputActivationException,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    exception.Message));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedPlayerInputActivation,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    false,
                    issues,
                    null,
                    source,
                    reason);
            }

            InputModeUnityPlayerInputAdapterResult adapterResult = InputModeUnityPlayerInputAdapter.Apply(
                plan,
                playerInput,
                source,
                reason);

            if (!adapterResult.Succeeded)
            {
                issues.Add(InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                    InputModeUnityPlayerInputApplicationIssueKind.PlayerInputAdapterFailed,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "PlayerInput action-map selection was rejected by the explicit PlayerInput adapter."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedPlayerInputAdapter,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    adapterResult.AppliedActionMapName,
                    false,
                    true,
                    false,
                    false,
                    issues,
                    adapterResult,
                    source,
                    reason);
            }

            return CreateResult(
                InputModeUnityPlayerInputApplicationStatus.Succeeded,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                adapterResult.AppliedActionMapName,
                adapterResult.Applied,
                true,
                adapterResult.SelectedActionMap,
                adapterResult.DeactivatedPlayerInput,
                issues,
                adapterResult,
                source,
                reason);
        }

        private static InputModeUnityPlayerInputApplicationResult CreateResult(
            InputModeUnityPlayerInputApplicationStatus status,
            InputModeKind requestedMode,
            InputModeUnityApplicationPlanOperation operation,
            UnityInputActionMapName requestedActionMapName,
            UnityInputActionMapName appliedActionMapName,
            bool applied,
            bool activatedPlayerInput,
            bool selectedActionMap,
            bool deactivatedPlayerInput,
            List<InputModeUnityPlayerInputApplicationIssue> issues,
            InputModeUnityPlayerInputAdapterResult adapterResult,
            string source,
            string reason)
        {
            return new InputModeUnityPlayerInputApplicationResult(
                status,
                requestedMode,
                operation,
                requestedActionMapName,
                appliedActionMapName,
                applied,
                activatedPlayerInput,
                selectedActionMap,
                deactivatedPlayerInput,
                issues == null ? Array.Empty<InputModeUnityPlayerInputApplicationIssue>() : issues.ToArray(),
                adapterResult,
                source,
                reason);
        }

        private static string CurrentActionMapName(PlayerInput playerInput)
        {
            return playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name
                : string.Empty;
        }
    }
}
