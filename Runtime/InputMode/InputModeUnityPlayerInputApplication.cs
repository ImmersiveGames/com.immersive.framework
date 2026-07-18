using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Applies one validated InputMode plan as an exact action-map posture.
    /// PlayerInput activation is not changed implicitly; the canonical writer reconciles
    /// the requested primary map plus persistent maps such as Global.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC4 exact layered Unity PlayerInput action-map posture application.")]
    public static class InputModeUnityPlayerInputApplication
    {
        public static InputModeUnityPlayerInputApplicationResult Apply(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            UnityInputActionMapName[] persistentActionMapNames = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(InputModeUnityPlayerInputApplication));
            string normalizedReason = reason.NormalizeText();
            var issues = new List<InputModeUnityPlayerInputApplicationIssue>();

            if (plan == null || !plan.Succeeded)
            {
                InputModeKind requestedMode = plan == null
                    ? InputModeKind.Unknown
                    : plan.RequestedMode;
                InputModeUnityApplicationPlanOperation operation = plan == null
                    ? InputModeUnityApplicationPlanOperation.NoOperation
                    : plan.Operation;
                UnityInputActionMapName actionMapName = plan == null
                    ? UnityInputActionMapName.From(string.Empty)
                    : plan.ActionMapName;

                issues.Add(
                    InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
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
                issues.Add(
                    InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
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

            if (playerInput.actions == null)
            {
                issues.Add(
                    InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                        InputModeUnityPlayerInputApplicationIssueKind.MissingActionAsset,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        normalizedSource,
                        "PlayerInput application requires PlayerInput.actions."));

                return CreateResult(
                    InputModeUnityPlayerInputApplicationStatus.FailedMissingActionAsset,
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

            if (plan.Operation != InputModeUnityApplicationPlanOperation.SelectActionMap &&
                plan.Operation != InputModeUnityApplicationPlanOperation.LockInput)
            {
                issues.Add(
                    InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
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

            InputModeUnityPlayerInputAdapterResult adapterResult =
                InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    playerInput,
                    normalizedSource,
                    normalizedReason,
                    persistentActionMapNames);

            if (!adapterResult.Succeeded)
            {
                issues.Add(
                    InputModeUnityPlayerInputApplicationIssue.BlockingIssue(
                        InputModeUnityPlayerInputApplicationIssueKind.PlayerInputAdapterFailed,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        normalizedSource,
                        "PlayerInput layered action-map posture was rejected by the explicit adapter."));

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
                issues == null
                    ? Array.Empty<InputModeUnityPlayerInputApplicationIssue>()
                    : issues.ToArray(),
                adapterResult,
                source,
                reason);
        }

        private static string CurrentActionMapName(PlayerInput playerInput) =>
            playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name.NormalizeText()
                : string.Empty;
    }
}
