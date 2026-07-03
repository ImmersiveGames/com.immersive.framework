using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Explicit adapter from an F32C plan to a Unity PlayerInput side effect.
    /// This is not a manager and does not own PlayerInputManager, join flow, player spawn or movement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32D explicit Unity PlayerInput adapter.")]
    public static class InputModeUnityPlayerInputAdapter
    {
        public static InputModeUnityPlayerInputAdapterResult Apply(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputAdapter));
            var issues = new List<InputModeUnityPlayerInputAdapterIssue>();

            if (plan == null || !plan.Succeeded)
            {
                UnityInputActionMapName actionMapName = plan == null
                    ? UnityInputActionMapName.From(string.Empty)
                    : plan.ActionMapName;

                InputModeKind requestedMode = plan == null ? InputModeKind.Unknown : plan.RequestedMode;
                InputModeUnityApplicationPlanOperation operation = plan == null
                    ? InputModeUnityApplicationPlanOperation.NoOperation
                    : plan.Operation;

                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.InvalidPlan,
                    requestedMode,
                    actionMapName,
                    normalizedSource,
                    "Unity PlayerInput adapter requires a successful InputMode Unity application plan."));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedInvalidPlan,
                    requestedMode,
                    operation,
                    actionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    normalizedSource,
                    reason);
            }

            if (playerInput == null)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.MissingPlayerInput,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    normalizedSource,
                    "Unity PlayerInput adapter requires an explicit PlayerInput instance."));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedMissingPlayerInput,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    false,
                    issues,
                    normalizedSource,
                    reason);
            }

            if (plan.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap)
            {
                return ApplyActionMapSelection(plan, playerInput, normalizedSource, reason, issues);
            }

            if (plan.Operation == InputModeUnityApplicationPlanOperation.LockInput)
            {
                return ApplyInputLock(plan, playerInput, normalizedSource, reason, issues);
            }

            issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                InputModeUnityPlayerInputAdapterIssueKind.UnsupportedOperation,
                plan.RequestedMode,
                plan.ActionMapName,
                normalizedSource,
                "Unity PlayerInput adapter does not support the requested plan operation."));

            return CreateResult(
                InputModeUnityPlayerInputAdapterStatus.FailedUnsupportedOperation,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                false,
                false,
                false,
                issues,
                normalizedSource,
                reason);
        }

        private static InputModeUnityPlayerInputAdapterResult ApplyActionMapSelection(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            List<InputModeUnityPlayerInputAdapterIssue> issues)
        {
            if (!plan.ActionMapName.IsValid)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.MissingActionMap,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "Unity PlayerInput adapter requires an explicit action map name for SelectActionMap."));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedMissingActionMap,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            if (playerInput.actions == null)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.MissingActionAsset,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "Unity PlayerInput adapter requires PlayerInput.actions to contain the requested action map."));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedMissingActionAsset,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            InputActionMap map = playerInput.actions.FindActionMap(plan.ActionMapName.ToString(), false);
            if (map == null)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.MissingActionMap,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "Unity PlayerInput action asset does not contain the requested action map."));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedMissingActionMap,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            try
            {
                playerInput.SwitchCurrentActionMap(plan.ActionMapName.ToString());
            }
            catch (Exception exception)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    exception.Message));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            UnityInputActionMapName applied = UnityInputActionMapName.From(CurrentActionMapName(playerInput));
            return CreateResult(
                InputModeUnityPlayerInputAdapterStatus.Succeeded,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                applied,
                true,
                true,
                false,
                issues,
                source,
                reason);
        }

        private static InputModeUnityPlayerInputAdapterResult ApplyInputLock(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            List<InputModeUnityPlayerInputAdapterIssue> issues)
        {
            try
            {
                playerInput.DeactivateInput();
            }
            catch (Exception exception)
            {
                issues.Add(InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    exception.Message));

                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            return CreateResult(
                InputModeUnityPlayerInputAdapterStatus.Succeeded,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                UnityInputActionMapName.From(CurrentActionMapName(playerInput)),
                true,
                false,
                true,
                issues,
                source,
                reason);
        }

        private static InputModeUnityPlayerInputAdapterResult CreateResult(
            InputModeUnityPlayerInputAdapterStatus status,
            InputModeKind requestedMode,
            InputModeUnityApplicationPlanOperation operation,
            UnityInputActionMapName requestedActionMapName,
            UnityInputActionMapName appliedActionMapName,
            bool applied,
            bool selectedActionMap,
            bool deactivatedPlayerInput,
            List<InputModeUnityPlayerInputAdapterIssue> issues,
            string source,
            string reason)
        {
            return new InputModeUnityPlayerInputAdapterResult(
                status,
                requestedMode,
                operation,
                requestedActionMapName,
                appliedActionMapName,
                applied,
                selectedActionMap,
                deactivatedPlayerInput,
                issues == null ? Array.Empty<InputModeUnityPlayerInputAdapterIssue>() : issues.ToArray(),
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
