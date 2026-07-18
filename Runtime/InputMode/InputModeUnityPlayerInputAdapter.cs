using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Explicit adapter from an InputMode application plan to the canonical Unity PlayerInput
    /// physical writer. It owns no Session state, PlayerInputManager or gameplay semantics.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC1 InputMode requester using the canonical Unity PlayerInput physical writer.")]
    public static class InputModeUnityPlayerInputAdapter
    {
        public static InputModeUnityPlayerInputAdapterResult Apply(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            UnityInputActionMapName[] persistentActionMapNames = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(InputModeUnityPlayerInputAdapter));
            var issues = new List<InputModeUnityPlayerInputAdapterIssue>();

            if (plan == null || !plan.Succeeded)
            {
                UnityInputActionMapName actionMapName = plan == null
                    ? UnityInputActionMapName.From(string.Empty)
                    : plan.ActionMapName;
                InputModeKind requestedMode = plan == null
                    ? InputModeKind.Unknown
                    : plan.RequestedMode;
                InputModeUnityApplicationPlanOperation operation = plan == null
                    ? InputModeUnityApplicationPlanOperation.NoOperation
                    : plan.Operation;

                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
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
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    normalizedSource,
                    reason);
            }

            if (playerInput == null)
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
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

            return plan.Operation switch
            {
                InputModeUnityApplicationPlanOperation.SelectActionMap =>
                    ApplyActionMapSelection(
                        plan,
                        playerInput,
                        normalizedSource,
                        reason,
                        persistentActionMapNames,
                        issues),
                InputModeUnityApplicationPlanOperation.LockInput =>
                    ApplyInputLock(
                        plan,
                        playerInput,
                        normalizedSource,
                        reason,
                        persistentActionMapNames,
                        issues),
                _ => Unsupported(
                    plan,
                    playerInput,
                    normalizedSource,
                    reason,
                    issues)
            };
        }

        private static InputModeUnityPlayerInputAdapterResult
            ApplyActionMapSelection(
                InputModeUnityApplicationPlanResult plan,
                PlayerInput playerInput,
                string source,
                string reason,
                UnityInputActionMapName[] persistentActionMapNames,
                List<InputModeUnityPlayerInputAdapterIssue> issues)
        {
            if (!plan.ActionMapName.IsValid)
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
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
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            if (playerInput.actions == null)
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
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
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            InputActionMap map = playerInput.actions.FindActionMap(
                plan.ActionMapName.ToString(),
                false);
            if (map == null)
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
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
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            UnityPlayerInputGateAdapter writeAuthority =
                playerInput.GetComponent<UnityPlayerInputGateAdapter>();
            if (writeAuthority == null ||
                !ReferenceEquals(writeAuthority.PlayerInput, playerInput))
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                        InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        source,
                        "InputMode requires the explicit co-located UnityPlayerInputGateAdapter write authority."));
                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            string[] desiredActionMapNames = BuildDesiredActionMapNames(
                plan.ActionMapName,
                persistentActionMapNames);
            if (!writeAuthority.TryApplyActionMapSet(
                    plan.ActionMapName.ToString(),
                    desiredActionMapNames,
                    source,
                    reason,
                    out _,
                    out string issue))
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                        InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        source,
                        issue));
                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            UnityInputActionMapName applied = UnityInputActionMapName.From(
                CurrentActionMapName(playerInput));
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
            UnityInputActionMapName[] persistentActionMapNames,
            List<InputModeUnityPlayerInputAdapterIssue> issues)
        {
            UnityPlayerInputGateAdapter writeAuthority =
                playerInput.GetComponent<UnityPlayerInputGateAdapter>();
            if (writeAuthority == null ||
                !ReferenceEquals(writeAuthority.PlayerInput, playerInput))
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                        InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        source,
                        "InputMode LockInput requires the explicit co-located UnityPlayerInputGateAdapter write authority."));
                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    false,
                    false,
                    false,
                    issues,
                    source,
                    reason);
            }

            string[] persistentNames = BuildDesiredActionMapNames(
                UnityInputActionMapName.From(string.Empty),
                persistentActionMapNames);
            if (persistentNames.Length > 0)
            {
                string primary = persistentNames[0];
                if (!writeAuthority.TryApplyActionMapSet(
                        primary,
                        persistentNames,
                        source,
                        reason,
                        out _,
                        out string setIssue))
                {
                    issues.Add(
                        InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                            InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                            plan.RequestedMode,
                            UnityInputActionMapName.From(primary),
                            source,
                            setIssue));
                    return CreateResult(
                        InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                        plan.RequestedMode,
                        plan.Operation,
                        UnityInputActionMapName.From(primary),
                        UnityInputActionMapName.From(
                            CurrentActionMapName(playerInput)),
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
                    UnityInputActionMapName.From(primary),
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
                    true,
                    true,
                    false,
                    issues,
                    source,
                    reason);
            }

            if (!writeAuthority.TrySetPlayerInputActive(
                    false,
                    source,
                    reason,
                    out _,
                    out _,
                    out string issue))
            {
                issues.Add(
                    InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                        InputModeUnityPlayerInputAdapterIssueKind.UnityInputException,
                        plan.RequestedMode,
                        plan.ActionMapName,
                        source,
                        issue));
                return CreateResult(
                    InputModeUnityPlayerInputAdapterStatus.FailedUnityInputException,
                    plan.RequestedMode,
                    plan.Operation,
                    plan.ActionMapName,
                    UnityInputActionMapName.From(
                        CurrentActionMapName(playerInput)),
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
                UnityInputActionMapName.From(
                    CurrentActionMapName(playerInput)),
                true,
                false,
                true,
                issues,
                source,
                reason);
        }

        private static InputModeUnityPlayerInputAdapterResult Unsupported(
            InputModeUnityApplicationPlanResult plan,
            PlayerInput playerInput,
            string source,
            string reason,
            List<InputModeUnityPlayerInputAdapterIssue> issues)
        {
            issues.Add(
                InputModeUnityPlayerInputAdapterIssue.BlockingIssue(
                    InputModeUnityPlayerInputAdapterIssueKind.UnsupportedOperation,
                    plan.RequestedMode,
                    plan.ActionMapName,
                    source,
                    "Unity PlayerInput adapter does not support the requested plan operation."));
            return CreateResult(
                InputModeUnityPlayerInputAdapterStatus.FailedUnsupportedOperation,
                plan.RequestedMode,
                plan.Operation,
                plan.ActionMapName,
                UnityInputActionMapName.From(
                    CurrentActionMapName(playerInput)),
                false,
                false,
                false,
                issues,
                source,
                reason);
        }

        private static string[] BuildDesiredActionMapNames(
            UnityInputActionMapName primaryActionMapName,
            UnityInputActionMapName[] persistentActionMapNames)
        {
            var names = new List<string>();
            AddUnique(names, primaryActionMapName);
            if (persistentActionMapNames != null)
            {
                for (int index = 0;
                     index < persistentActionMapNames.Length;
                     index++)
                {
                    AddUnique(names, persistentActionMapNames[index]);
                }
            }

            return names.ToArray();
        }

        private static void AddUnique(
            List<string> names,
            UnityInputActionMapName actionMapName)
        {
            if (!actionMapName.IsValid)
            {
                return;
            }

            string name = actionMapName.ToString().NormalizeText();
            for (int index = 0; index < names.Count; index++)
            {
                if (string.Equals(
                        names[index],
                        name,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            names.Add(name);
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
                issues == null
                    ? Array.Empty<InputModeUnityPlayerInputAdapterIssue>()
                    : issues.ToArray(),
                source,
                reason);
        }

        private static string CurrentActionMapName(PlayerInput playerInput) =>
            playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name.NormalizeText()
                : string.Empty;
    }
}
