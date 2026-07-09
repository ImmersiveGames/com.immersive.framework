using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit bridge between PlayerControl binding evidence and a configured Unity PlayerInput.
    /// It does not enable input, switch action maps, route InputActions, enable movement, execute gameplay or own lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B explicit Unity PlayerInput bridge adapter contract.")]
    public static class UnityPlayerInputBridgeAdapter
    {
        public static UnityPlayerInputBridgeResult Bridge(
            IPlayerControlBindingTarget controlBindingTarget,
            IUnityPlayerInputBridgeTarget bridgeTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeAdapter));
            UnityPlayerInputBridgeResult validation = ValidateBridgeInputs(
                controlBindingTarget,
                bridgeTarget,
                normalizedSource,
                reason);

            if (validation.Failed || validation.NoOp)
            {
                return validation;
            }

            try
            {
                PlayerControlBindingSnapshot controlBinding = controlBindingTarget.CurrentPlayerControlBinding;
                UnityPlayerInputBridgeSnapshot bridge = UnityPlayerInputBridgeSnapshot.FromPlayerControlBinding(
                    controlBinding,
                    bridgeTarget.UnityPlayerInputName,
                    bridgeTarget.BridgeTargetName,
                    normalizedSource,
                    reason);

                UnityPlayerInputBridgeResult result = bridgeTarget.ApplyUnityPlayerInputBridge(
                    bridge,
                    normalizedSource,
                    reason);

                if (!result.Succeeded)
                {
                    return UnityPlayerInputBridgeResult.Failure(
                        UnityPlayerInputBridgeFailureKind.TargetRejectedBridge,
                        controlBinding.PlayerSlotId.StableText,
                        controlBindingTarget.BindingTargetName,
                        bridgeTarget.BridgeTargetName,
                        bridgeTarget.UnityPlayerInputName,
                        normalizedSource,
                        reason,
                        result.Message);
                }

                return result;
            }
            catch (Exception exception)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.UnexpectedException,
                    string.Empty,
                    controlBindingTarget != null ? controlBindingTarget.BindingTargetName : string.Empty,
                    bridgeTarget != null ? bridgeTarget.BridgeTargetName : string.Empty,
                    bridgeTarget != null ? bridgeTarget.UnityPlayerInputName : string.Empty,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        public static UnityPlayerInputBridgeResult Clear(
            PlayerSlotId playerSlotId,
            IUnityPlayerInputBridgeTarget bridgeTarget,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityPlayerInputBridgeAdapter));
            if (bridgeTarget == null)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInputBridgeTarget,
                    playerSlotId.IsValid ? playerSlotId.StableText : string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge clear requires a target.");
            }

            if (!playerSlotId.IsValid)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.InvalidPlayerControlBinding,
                    string.Empty,
                    string.Empty,
                    bridgeTarget.BridgeTargetName,
                    bridgeTarget.UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    "Unity PlayerInput bridge clear requires a valid PlayerSlotId.");
            }

            try
            {
                return bridgeTarget.ClearUnityPlayerInputBridge(playerSlotId, normalizedSource, reason);
            }
            catch (Exception exception)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.UnexpectedException,
                    playerSlotId.StableText,
                    string.Empty,
                    bridgeTarget.BridgeTargetName,
                    bridgeTarget.UnityPlayerInputName,
                    normalizedSource,
                    reason,
                    exception.Message);
            }
        }

        private static UnityPlayerInputBridgeResult ValidateBridgeInputs(
            IPlayerControlBindingTarget controlBindingTarget,
            IUnityPlayerInputBridgeTarget bridgeTarget,
            string source,
            string reason)
        {
            string controlBindingTargetName = controlBindingTarget != null ? controlBindingTarget.BindingTargetName : string.Empty;
            string bridgeTargetName = bridgeTarget != null ? bridgeTarget.BridgeTargetName : string.Empty;
            string playerInputName = bridgeTarget != null ? bridgeTarget.UnityPlayerInputName : string.Empty;

            if (controlBindingTarget == null)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingPlayerControlBindingTarget,
                    string.Empty,
                    string.Empty,
                    bridgeTargetName,
                    playerInputName,
                    source,
                    reason,
                    "Unity PlayerInput bridge requires a PlayerControl binding target.");
            }

            if (!controlBindingTarget.HasPlayerControlBinding)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingPlayerControlBinding,
                    string.Empty,
                    controlBindingTargetName,
                    bridgeTargetName,
                    playerInputName,
                    source,
                    reason,
                    "Unity PlayerInput bridge requires existing PlayerControl binding evidence.");
            }

            PlayerControlBindingSnapshot controlBinding = controlBindingTarget.CurrentPlayerControlBinding;
            string playerSlotIdText = controlBinding.PlayerSlotId.IsValid ? controlBinding.PlayerSlotId.StableText : string.Empty;
            if (!controlBinding.PlayerSlotId.IsValid || !controlBinding.BindsControl)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.InvalidPlayerControlBinding,
                    playerSlotIdText,
                    controlBindingTargetName,
                    bridgeTargetName,
                    playerInputName,
                    source,
                    reason,
                    "Unity PlayerInput bridge requires valid PlayerControl binding evidence.");
            }

            if (bridgeTarget == null)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInputBridgeTarget,
                    playerSlotIdText,
                    controlBindingTargetName,
                    string.Empty,
                    string.Empty,
                    source,
                    reason,
                    "Unity PlayerInput bridge requires a bridge target.");
            }

            if (!bridgeTarget.HasUnityPlayerInput)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInput,
                    playerSlotIdText,
                    controlBindingTargetName,
                    bridgeTargetName,
                    string.Empty,
                    source,
                    reason,
                    "Unity PlayerInput bridge requires an explicit Unity PlayerInput.");
            }

            if (!bridgeTarget.TryGetExpectedPlayerSlotId(out PlayerSlotId expectedPlayerSlotId))
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.MissingExpectedPlayerSlot,
                    playerSlotIdText,
                    controlBindingTargetName,
                    bridgeTargetName,
                    playerInputName,
                    source,
                    reason,
                    "Unity PlayerInput bridge target requires an expected PlayerSlotId.");
            }

            if (expectedPlayerSlotId != controlBinding.PlayerSlotId)
            {
                return UnityPlayerInputBridgeResult.Failure(
                    UnityPlayerInputBridgeFailureKind.PlayerSlotMismatch,
                    playerSlotIdText,
                    controlBindingTargetName,
                    bridgeTargetName,
                    playerInputName,
                    source,
                    reason,
                    $"Unity PlayerInput bridge target expects '{expectedPlayerSlotId.StableText}' but PlayerControl binding is '{playerSlotIdText}'.");
            }

            return UnityPlayerInputBridgeResult.Success(
                controlBinding.PlayerSlotId,
                controlBindingTargetName,
                bridgeTargetName,
                playerInputName,
                source,
                reason,
                "Unity PlayerInput bridge inputs validated.");
        }
    }
}
