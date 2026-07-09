using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit Unity PlayerInput action-map activation adapter.
    /// It switches one configured action map from existing Unity PlayerInput bridge evidence and restores the previous action map on clear.
    /// It does not route InputActions, enable movement, execute gameplay, spawn actors or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52C explicit Unity PlayerInput action-map activation adapter contract.")]
    public static class UnityPlayerInputActivationAdapter
    {
        public static UnityPlayerInputActivationResult Activate(
            IUnityPlayerInputBridgeTarget bridgeTarget,
            IUnityPlayerInputActivationTarget activationTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationAdapter));
            UnityPlayerInputActivationResult validation = ValidateActivationInputs(
                bridgeTarget,
                activationTarget,
                normalizedSource,
                reason);

            if (validation.Failed || validation.NoOp)
            {
                return validation;
            }

            try
            {
                UnityPlayerInputBridgeSnapshot bridge = bridgeTarget.CurrentUnityPlayerInputBridge;
                UnityPlayerInputActivationSnapshot activation = UnityPlayerInputActivationSnapshot.FromUnityPlayerInputBridge(
                    bridge,
                    activationTarget.ActivationTargetName,
                    activationTarget.ConfiguredActionMapName,
                    activationTarget.CurrentActionMapName,
                    normalizedSource,
                    reason);

                UnityPlayerInputActivationResult result = activationTarget.ApplyUnityPlayerInputActivation(
                    activation,
                    normalizedSource,
                    reason);

                if (!result.Succeeded)
                {
                    return UnityPlayerInputActivationResult.Failure(
                        UnityPlayerInputActivationFailureKind.TargetRejectedActivation,
                        bridge.PlayerSlotId.StableText,
                        bridge.BridgeTargetName,
                        activationTarget.ActivationTargetName,
                        activationTarget.UnityPlayerInputName,
                        activationTarget.ConfiguredActionMapName,
                        activation.PreviousActionMapName,
                        activationTarget.CurrentActionMapName,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.UnexpectedException,
                    string.Empty,
                    bridgeTarget != null ? bridgeTarget.BridgeTargetName : string.Empty,
                    activationTarget != null ? activationTarget.ActivationTargetName : string.Empty,
                    activationTarget != null ? activationTarget.UnityPlayerInputName : string.Empty,
                    activationTarget != null ? activationTarget.ConfiguredActionMapName : string.Empty,
                    string.Empty,
                    activationTarget != null ? activationTarget.CurrentActionMapName : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static UnityPlayerInputActivationResult Clear(
            PlayerSlotId playerSlotId,
            IUnityPlayerInputActivationTarget activationTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputActivationAdapter));
            if (activationTarget == null)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputActivationTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput activation clear requires a target.");
            }

            if (!playerSlotId.IsValid)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.InvalidUnityPlayerInputBridge,
                    string.Empty,
                    string.Empty,
                    activationTarget.ActivationTargetName,
                    activationTarget.UnityPlayerInputName,
                    activationTarget.ConfiguredActionMapName,
                    string.Empty,
                    activationTarget.CurrentActionMapName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput activation clear requires a valid PlayerSlotId.");
            }

            try
            {
                return activationTarget.ClearUnityPlayerInputActivation(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    string.Empty,
                    activationTarget.ActivationTargetName,
                    activationTarget.UnityPlayerInputName,
                    activationTarget.ConfiguredActionMapName,
                    string.Empty,
                    activationTarget.CurrentActionMapName,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static UnityPlayerInputActivationResult ValidateActivationInputs(
            IUnityPlayerInputBridgeTarget bridgeTarget,
            IUnityPlayerInputActivationTarget activationTarget,
            string source,
            string reason)
        {
            string bridgeTargetName = bridgeTarget != null ? bridgeTarget.BridgeTargetName : string.Empty;
            string activationTargetName = activationTarget != null ? activationTarget.ActivationTargetName : string.Empty;
            string playerInputName = activationTarget != null ? activationTarget.UnityPlayerInputName : string.Empty;
            string actionMapName = activationTarget != null ? activationTarget.ConfiguredActionMapName : string.Empty;
            string currentActionMapName = activationTarget != null ? activationTarget.CurrentActionMapName : string.Empty;

            if (bridgeTarget == null)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputBridgeTarget,
                    string.Empty,
                    string.Empty,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires a bridge target.");
            }

            if (!bridgeTarget.HasUnityPlayerInputBridge)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputBridge,
                    string.Empty,
                    bridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires existing Unity PlayerInput bridge evidence.");
            }

            UnityPlayerInputBridgeSnapshot bridge = bridgeTarget.CurrentUnityPlayerInputBridge;
            string playerSlotIdText = bridge.PlayerSlotId.IsValid ? bridge.PlayerSlotId.StableText : string.Empty;
            if (!bridge.PlayerSlotId.IsValid || !bridge.BindsControl || !bridge.BridgesUnityPlayerInput)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.InvalidUnityPlayerInputBridge,
                    playerSlotIdText,
                    bridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires valid PlayerInput bridge evidence.");
            }

            if (activationTarget == null)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputActivationTarget,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    source,
                    reason,
                    "Unity PlayerInput activation requires an activation target.");
            }

            if (!activationTarget.HasUnityPlayerInput)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingUnityPlayerInput,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    string.Empty,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires an explicit Unity PlayerInput.");
            }

            if (!activationTarget.TryGetExpectedPlayerSlotId(out PlayerSlotId expectedSlot))
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingExpectedPlayerSlot,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation target requires an expected PlayerSlotId.");
            }

            if (expectedSlot != bridge.PlayerSlotId)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.PlayerSlotMismatch,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    $"Unity PlayerInput activation target expects '{expectedSlot.StableText}' but bridge snapshot targets '{bridge.PlayerSlotId.StableText}'.");
            }

            if (!activationTarget.HasConfiguredActionMapName)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionMapName,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    string.Empty,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires a configured action map name.");
            }

            if (!activationTarget.HasUnityPlayerInputActionAsset)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionAsset,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    "Unity PlayerInput activation requires PlayerInput.actions.");
            }

            if (!activationTarget.HasConfiguredActionMap)
            {
                return UnityPlayerInputActivationResult.Failure(
                    UnityPlayerInputActivationFailureKind.MissingActionMap,
                    playerSlotIdText,
                    bridge.BridgeTargetName,
                    activationTargetName,
                    playerInputName,
                    actionMapName,
                    string.Empty,
                    currentActionMapName,
                    source,
                    reason,
                    $"Unity PlayerInput activation could not find configured action map '{actionMapName}'.");
            }

            return UnityPlayerInputActivationResult.Success(
                bridge.PlayerSlotId,
                bridge.BridgeTargetName,
                activationTargetName,
                playerInputName,
                actionMapName,
                currentActionMapName,
                currentActionMapName,
                source,
                reason,
                "Unity PlayerInput action-map activation validation passed.");
        }
    }
}
