using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Explicit request for applying one resident InputMode transaction through
    /// the Pause/InputMode boundary to one Unity PlayerInput.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2/IC4 Pause/InputMode apply request with resident state and persistent map evidence.")]
    internal sealed class PauseInputModeApplyRequest
    {
        internal PauseInputModeApplyRequest(
            FrameworkRuntimeHost runtimeHost,
            PauseRequestKind requestKind,
            string requestId,
            PlayerInput playerInput,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            LocalPlayerProvisioningValidationResult localPlayerProvisioningValidation,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            UnityInputActionMapName[] persistentActionMapNames,
            bool requireLocalPlayerProvisioning,
            string source,
            string reason)
            : this(
                runtimeHost,
                requestKind,
                requestId,
                playerInput,
                targetSet,
                playerActorSet,
                localPlayerProvisioningValidation,
                actionMapEvidence,
                actionMapBindings,
                persistentActionMapNames,
                requireLocalPlayerProvisioning,
                source,
                reason,
                default)
        {
        }

        internal PauseInputModeApplyRequest(
            FrameworkRuntimeHost runtimeHost,
            PauseRequestKind requestKind,
            string requestId,
            PlayerInput playerInput,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            LocalPlayerProvisioningValidationResult localPlayerProvisioningValidation,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            UnityInputActionMapName[] persistentActionMapNames,
            bool requireLocalPlayerProvisioning,
            string source,
            string reason,
            InputModeState currentInputModeState)
        {
            RuntimeHost = runtimeHost;
            RequestKind = requestKind;
            RequestId = requestId.NormalizeTextOrFallback(
                nameof(PauseInputModeApplyRequest));
            PlayerInput = playerInput;
            TargetSet = targetSet;
            PlayerActorSet = playerActorSet;
            LocalPlayerProvisioningValidation = localPlayerProvisioningValidation;
            ActionMapEvidence = actionMapEvidence;
            ActionMapBindings = CopyBindings(actionMapBindings);
            PersistentActionMapNames = CopyMapNames(persistentActionMapNames);
            RequireLocalPlayerProvisioning = requireLocalPlayerProvisioning;
            Source = source.NormalizeTextOrFallback(
                nameof(PauseInputModeApplyRequest));
            Reason = reason.NormalizeText();
            CurrentInputModeState = currentInputModeState;
        }

        internal FrameworkRuntimeHost RuntimeHost { get; }
        internal PauseRequestKind RequestKind { get; }
        internal string RequestId { get; }
        internal PlayerInput PlayerInput { get; }
        internal UnityInputTargetSet TargetSet { get; }
        internal PlayerActorSet PlayerActorSet { get; }
        internal LocalPlayerProvisioningValidationResult LocalPlayerProvisioningValidation { get; }
        internal UnityInputActionMapEvidence ActionMapEvidence { get; }
        internal InputModeUnityActionMapBinding[] ActionMapBindings { get; }
        internal UnityInputActionMapName[] PersistentActionMapNames { get; }
        internal bool RequireLocalPlayerProvisioning { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal InputModeState CurrentInputModeState { get; }

        private static InputModeUnityActionMapBinding[] CopyBindings(
            InputModeUnityActionMapBinding[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
            {
                return Array.Empty<InputModeUnityActionMapBinding>();
            }

            var copy = new InputModeUnityActionMapBinding[bindings.Length];
            Array.Copy(bindings, copy, bindings.Length);
            return copy;
        }

        private static UnityInputActionMapName[] CopyMapNames(
            UnityInputActionMapName[] mapNames)
        {
            if (mapNames == null || mapNames.Length == 0)
            {
                return Array.Empty<UnityInputActionMapName>();
            }

            var copy = new UnityInputActionMapName[mapNames.Length];
            Array.Copy(mapNames, copy, mapNames.Length);
            return copy;
        }
    }
}
