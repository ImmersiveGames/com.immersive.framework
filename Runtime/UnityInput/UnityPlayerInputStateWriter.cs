using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// The only package-owned physical writer for Unity PlayerInput posture.
    /// Callers retain domain policy and lifecycle ownership; this adapter owns only the
    /// concrete PlayerInput/InputActionMap side effect and exact rollback evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IC1 single physical writer for Unity PlayerInput action-map and activation state.")]
    internal static class UnityPlayerInputStateWriter
    {
        internal static bool TrySelectActionMap(
            PlayerInput playerInput,
            string actionMapName,
            out UnityPlayerInputActionMapWriteReceipt receipt,
            out string issue)
        {
            receipt = default;
            issue = string.Empty;

            if (!TryResolveActionMap(
                    playerInput,
                    actionMapName,
                    out InputActionMap targetActionMap,
                    out string normalizedActionMapName,
                    out issue))
            {
                return false;
            }

            if (!playerInput.inputIsActive)
            {
                issue =
                    "Action-map selection requires an active PlayerInput. " +
                    "Activate input through the same write authority before selecting a map.";
                return false;
            }

            InputActionMap previousActionMap = playerInput.currentActionMap;
            string previousActionMapName = previousActionMap != null
                ? previousActionMap.name.NormalizeText()
                : string.Empty;
            bool changedCurrentActionMap =
                !ReferenceEquals(previousActionMap, targetActionMap);
            bool targetWasEnabled = targetActionMap.enabled;

            try
            {
                if (changedCurrentActionMap)
                {
                    // Use the public PlayerInput state property rather than
                    // SwitchCurrentActionMap. Unity 6.5 guards the method with an
                    // internal lifecycle flag that is not represented by
                    // inputIsActive; the property is the canonical physical state
                    // transition and still disables the previous map before
                    // enabling the requested map.
                    playerInput.currentActionMap = targetActionMap;
                }
                else if (!targetActionMap.enabled)
                {
                    targetActionMap.Enable();
                }

                if (!ReferenceEquals(playerInput.currentActionMap, targetActionMap) ||
                    !targetActionMap.enabled)
                {
                    issue =
                        $"Action map '{normalizedActionMapName}' did not become current and enabled.";
                    return false;
                }

                receipt = new UnityPlayerInputActionMapWriteReceipt(
                    playerInput.GetEntityId(),
                    previousActionMapName,
                    normalizedActionMapName,
                    changedCurrentActionMap,
                    !targetWasEnabled);
                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        internal static bool TryRestoreActionMap(
            PlayerInput playerInput,
            UnityPlayerInputActionMapWriteReceipt receipt,
            out string issue)
        {
            issue = string.Empty;
            if (playerInput == null)
            {
                issue = "Action-map restore requires PlayerInput evidence.";
                return false;
            }

            if (!receipt.IsValid ||
                !receipt.PlayerInputEntityId.Equals(playerInput.GetEntityId()))
            {
                issue =
                    "Action-map restore rejected missing, foreign or stale write evidence.";
                return false;
            }

            if (playerInput.actions == null)
            {
                issue = "Action-map restore requires PlayerInput.actions.";
                return false;
            }

            try
            {
                InputActionMap appliedActionMap = playerInput.actions.FindActionMap(
                    receipt.AppliedActionMapName,
                    throwIfNotFound: false);
                if (appliedActionMap == null)
                {
                    issue =
                        $"Applied action map '{receipt.AppliedActionMapName}' is unavailable during restore.";
                    return false;
                }

                if (receipt.ChangedCurrentActionMap)
                {
                    if (!string.IsNullOrEmpty(receipt.PreviousActionMapName))
                    {
                        InputActionMap previousActionMap =
                            playerInput.actions.FindActionMap(
                                receipt.PreviousActionMapName,
                                throwIfNotFound: false);
                        if (previousActionMap == null)
                        {
                            issue =
                                $"Previous action map '{receipt.PreviousActionMapName}' is unavailable.";
                            return false;
                        }

                        playerInput.currentActionMap = previousActionMap;
                        if (!ReferenceEquals(
                                playerInput.currentActionMap,
                                previousActionMap) ||
                            !previousActionMap.enabled)
                        {
                            issue =
                                $"Previous action map '{receipt.PreviousActionMapName}' was not restored as current and enabled.";
                            return false;
                        }
                    }
                    else
                    {
                        playerInput.currentActionMap = null;
                        if (appliedActionMap.enabled)
                        {
                            appliedActionMap.Disable();
                        }

                        if (playerInput.currentActionMap != null ||
                            appliedActionMap.enabled)
                        {
                            issue =
                                "Applied action map could not be cleared during restore.";
                            return false;
                        }
                    }
                }
                else if (receipt.EnabledAppliedActionMap &&
                         appliedActionMap.enabled)
                {
                    appliedActionMap.Disable();
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        internal static bool TryApplyActionMapSet(
            PlayerInput playerInput,
            string primaryActionMapName,
            IReadOnlyList<string> enabledActionMapNames,
            out UnityPlayerInputActionMapSetWriteReceipt receipt,
            out string issue)
        {
            receipt = default;
            issue = string.Empty;

            if (playerInput == null)
            {
                issue = "Action-map set application requires PlayerInput evidence.";
                return false;
            }

            if (playerInput.actions == null)
            {
                issue = "Action-map set application requires PlayerInput.actions.";
                return false;
            }

            string normalizedPrimary = primaryActionMapName.NormalizeText();
            if (!TryBuildDesiredActionMapSet(
                    playerInput,
                    normalizedPrimary,
                    enabledActionMapNames,
                    out HashSet<string> desiredNames,
                    out issue))
            {
                return false;
            }

            string previousPrimary = CurrentActionMapName(playerInput);
            string[] previousEnabled = GetEnabledActionMapNames(playerInput);
            string[] desiredEnabled = CopyNames(desiredNames);
            var rollbackReceipt = new UnityPlayerInputActionMapSetWriteReceipt(
                playerInput.GetEntityId(),
                previousPrimary,
                previousEnabled,
                normalizedPrimary,
                desiredEnabled);

            if (!TryApplyRawActionMapState(
                    playerInput,
                    normalizedPrimary,
                    desiredNames,
                    out issue))
            {
                TryApplyRawActionMapState(
                    playerInput,
                    previousPrimary,
                    new HashSet<string>(previousEnabled, StringComparer.Ordinal),
                    out string rollbackIssue);
                if (!string.IsNullOrEmpty(rollbackIssue))
                {
                    issue = $"{issue} Rollback='{rollbackIssue}'.";
                }

                return false;
            }

            receipt = rollbackReceipt;
            return true;
        }

        internal static bool TryRestoreActionMapSet(
            PlayerInput playerInput,
            UnityPlayerInputActionMapSetWriteReceipt receipt,
            out string issue)
        {
            issue = string.Empty;
            if (playerInput == null)
            {
                issue = "Action-map set restore requires PlayerInput evidence.";
                return false;
            }

            if (!receipt.IsValid ||
                !receipt.PlayerInputEntityId.Equals(playerInput.GetEntityId()))
            {
                issue =
                    "Action-map set restore rejected missing, foreign or stale write evidence.";
                return false;
            }

            if (playerInput.actions == null)
            {
                issue = "Action-map set restore requires PlayerInput.actions.";
                return false;
            }

            return TryApplyRawActionMapState(
                playerInput,
                receipt.PreviousPrimaryActionMapName,
                new HashSet<string>(
                    receipt.PreviousEnabledActionMapNames,
                    StringComparer.Ordinal),
                out issue);
        }

        internal static bool HasExactEnabledActionMapSet(
            PlayerInput playerInput,
            IReadOnlyList<string> expectedEnabledActionMapNames)
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return false;
            }

            var expected = new HashSet<string>(StringComparer.Ordinal);
            if (expectedEnabledActionMapNames != null)
            {
                for (int index = 0;
                     index < expectedEnabledActionMapNames.Count;
                     index++)
                {
                    string name = expectedEnabledActionMapNames[index]
                        .NormalizeText();
                    if (!string.IsNullOrEmpty(name))
                    {
                        expected.Add(name);
                    }
                }
            }

            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                if (map.enabled != expected.Contains(map.name.NormalizeText()))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TrySetActionMapEnabled(
            PlayerInput playerInput,
            string actionMapName,
            bool enabled,
            out bool previousEnabled,
            out bool changed,
            out string issue)
        {
            previousEnabled = false;
            changed = false;
            issue = string.Empty;

            if (!TryResolveActionMap(
                    playerInput,
                    actionMapName,
                    out InputActionMap actionMap,
                    out _,
                    out issue))
            {
                return false;
            }

            previousEnabled = actionMap.enabled;
            changed = previousEnabled != enabled;
            if (!changed)
            {
                return true;
            }

            try
            {
                if (enabled)
                {
                    actionMap.Enable();
                }
                else
                {
                    actionMap.Disable();
                }

                if (actionMap.enabled != enabled)
                {
                    issue =
                        $"Action map '{actionMap.name}' did not reach enabled='{enabled}'.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        internal static bool TrySetPlayerInputActive(
            PlayerInput playerInput,
            bool active,
            out bool previousActive,
            out bool changed,
            out string issue)
        {
            previousActive = IsInputActive(playerInput);
            changed = false;
            issue = string.Empty;

            if (playerInput == null)
            {
                issue = "PlayerInput activation write requires PlayerInput evidence.";
                return false;
            }

            changed = previousActive != active;
            if (!changed)
            {
                return true;
            }

            try
            {
                if (active)
                {
                    playerInput.ActivateInput();
                }
                else
                {
                    playerInput.DeactivateInput();
                }

                if (playerInput.inputIsActive != active)
                {
                    issue =
                        $"PlayerInput did not reach active='{active}'.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        internal static string CurrentActionMapName(PlayerInput playerInput)
        {
            return playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name.NormalizeText()
                : string.Empty;
        }

        private static bool TryBuildDesiredActionMapSet(
            PlayerInput playerInput,
            string primaryActionMapName,
            IReadOnlyList<string> enabledActionMapNames,
            out HashSet<string> desiredNames,
            out string issue)
        {
            desiredNames = new HashSet<string>(StringComparer.Ordinal);
            issue = string.Empty;

            if (enabledActionMapNames != null)
            {
                for (int index = 0;
                     index < enabledActionMapNames.Count;
                     index++)
                {
                    string name = enabledActionMapNames[index].NormalizeText();
                    if (string.IsNullOrEmpty(name))
                    {
                        issue =
                            "Action-map set contains an empty action map name.";
                        return false;
                    }

                    if (playerInput.actions.FindActionMap(
                            name,
                            throwIfNotFound: false) == null)
                    {
                        issue =
                            $"PlayerInput action asset does not contain action map '{name}'.";
                        return false;
                    }

                    desiredNames.Add(name);
                }
            }

            if (!string.IsNullOrEmpty(primaryActionMapName))
            {
                if (playerInput.actions.FindActionMap(
                        primaryActionMapName,
                        throwIfNotFound: false) == null)
                {
                    issue =
                        $"PlayerInput action asset does not contain primary action map '{primaryActionMapName}'.";
                    return false;
                }

                desiredNames.Add(primaryActionMapName);
            }

            return true;
        }

        private static bool TryApplyRawActionMapState(
            PlayerInput playerInput,
            string primaryActionMapName,
            HashSet<string> desiredNames,
            out string issue)
        {
            issue = string.Empty;
            foreach (string desiredName in desiredNames)
            {
                if (playerInput.actions.FindActionMap(
                        desiredName,
                        throwIfNotFound: false) == null)
                {
                    issue =
                        $"Desired action map '{desiredName}' is unavailable.";
                    return false;
                }
            }

            try
            {
                InputActionMap primaryMap = string.IsNullOrEmpty(
                    primaryActionMapName)
                    ? null
                    : playerInput.actions.FindActionMap(
                        primaryActionMapName,
                        throwIfNotFound: false);

                if (!string.IsNullOrEmpty(primaryActionMapName) &&
                    primaryMap == null)
                {
                    issue =
                        $"Primary action map '{primaryActionMapName}' is unavailable.";
                    return false;
                }

                if (!ReferenceEquals(playerInput.currentActionMap, primaryMap))
                {
                    playerInput.currentActionMap = primaryMap;
                }

                foreach (InputActionMap map in playerInput.actions.actionMaps)
                {
                    bool shouldBeEnabled = desiredNames.Contains(
                        map.name.NormalizeText());
                    if (map.enabled == shouldBeEnabled)
                    {
                        continue;
                    }

                    if (shouldBeEnabled)
                    {
                        map.Enable();
                    }
                    else
                    {
                        map.Disable();
                    }
                }

                if (!ReferenceEquals(playerInput.currentActionMap, primaryMap))
                {
                    issue =
                        $"Primary action map '{primaryActionMapName}' did not become current.";
                    return false;
                }

                foreach (InputActionMap map in playerInput.actions.actionMaps)
                {
                    bool expectedEnabled = desiredNames.Contains(
                        map.name.NormalizeText());
                    if (map.enabled != expectedEnabled)
                    {
                        issue =
                            $"Action map '{map.name}' did not reach enabled='{expectedEnabled}'.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        private static string[] GetEnabledActionMapNames(
            PlayerInput playerInput)
        {
            var names = new List<string>();
            if (playerInput?.actions == null)
            {
                return names.ToArray();
            }

            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                if (map.enabled)
                {
                    names.Add(map.name.NormalizeText());
                }
            }

            names.Sort(StringComparer.Ordinal);
            return names.ToArray();
        }

        private static string[] CopyNames(HashSet<string> names)
        {
            if (names == null || names.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[names.Count];
            names.CopyTo(copy);
            Array.Sort(copy, StringComparer.Ordinal);
            return copy;
        }

        private static bool TryResolveActionMap(
            PlayerInput playerInput,
            string actionMapName,
            out InputActionMap actionMap,
            out string normalizedActionMapName,
            out string issue)
        {
            actionMap = null;
            normalizedActionMapName = actionMapName.NormalizeText();
            issue = string.Empty;

            if (playerInput == null)
            {
                issue = "Unity PlayerInput write requires an explicit PlayerInput instance.";
                return false;
            }

            if (playerInput.actions == null)
            {
                issue = "Unity PlayerInput write requires PlayerInput.actions.";
                return false;
            }

            if (string.IsNullOrEmpty(normalizedActionMapName))
            {
                issue = "Unity PlayerInput write requires an explicit action map name.";
                return false;
            }

            actionMap = playerInput.actions.FindActionMap(
                normalizedActionMapName,
                throwIfNotFound: false);
            if (actionMap == null)
            {
                issue =
                    $"PlayerInput action asset does not contain action map '{normalizedActionMapName}'.";
                return false;
            }

            return true;
        }

        private static bool IsInputActive(PlayerInput playerInput)
        {
            return playerInput != null && playerInput.inputIsActive;
        }
    }

    internal readonly struct UnityPlayerInputActionMapSetWriteReceipt
    {
        internal UnityPlayerInputActionMapSetWriteReceipt(
            EntityId playerInputEntityId,
            string previousPrimaryActionMapName,
            string[] previousEnabledActionMapNames,
            string appliedPrimaryActionMapName,
            string[] appliedEnabledActionMapNames)
        {
            PlayerInputEntityId = playerInputEntityId;
            PreviousPrimaryActionMapName =
                previousPrimaryActionMapName.NormalizeText();
            PreviousEnabledActionMapNames =
                previousEnabledActionMapNames ?? Array.Empty<string>();
            AppliedPrimaryActionMapName =
                appliedPrimaryActionMapName.NormalizeText();
            AppliedEnabledActionMapNames =
                appliedEnabledActionMapNames ?? Array.Empty<string>();
        }

        internal EntityId PlayerInputEntityId { get; }
        internal string PreviousPrimaryActionMapName { get; }
        internal string[] PreviousEnabledActionMapNames { get; }
        internal string AppliedPrimaryActionMapName { get; }
        internal string[] AppliedEnabledActionMapNames { get; }
        internal bool StateChanged =>
            !string.Equals(
                PreviousPrimaryActionMapName,
                AppliedPrimaryActionMapName,
                StringComparison.Ordinal) ||
            !SetsEqual(
                PreviousEnabledActionMapNames,
                AppliedEnabledActionMapNames);
        internal bool IsValid =>
            PreviousEnabledActionMapNames != null &&
            AppliedEnabledActionMapNames != null;

        private static bool SetsEqual(string[] left, string[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (!string.Equals(
                        left[index],
                        right[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly struct UnityPlayerInputActionMapWriteReceipt
    {
        internal UnityPlayerInputActionMapWriteReceipt(
            EntityId playerInputEntityId,
            string previousActionMapName,
            string appliedActionMapName,
            bool changedCurrentActionMap,
            bool enabledAppliedActionMap)
        {
            PlayerInputEntityId = playerInputEntityId;
            PreviousActionMapName = previousActionMapName.NormalizeText();
            AppliedActionMapName = appliedActionMapName.NormalizeText();
            ChangedCurrentActionMap = changedCurrentActionMap;
            EnabledAppliedActionMap = enabledAppliedActionMap;
        }

        internal EntityId PlayerInputEntityId { get; }
        internal string PreviousActionMapName { get; }
        internal string AppliedActionMapName { get; }
        internal bool ChangedCurrentActionMap { get; }
        internal bool EnabledAppliedActionMap { get; }
        internal bool StateChanged =>
            ChangedCurrentActionMap || EnabledAppliedActionMap;
        internal bool IsValid =>
            !string.IsNullOrEmpty(AppliedActionMapName);
    }
}
