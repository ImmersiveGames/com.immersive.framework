using System;
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
