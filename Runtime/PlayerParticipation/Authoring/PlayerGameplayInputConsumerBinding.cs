using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Optional Logical Actor endpoint for generic gameplay input reads. The Framework
    /// binds this component to the current Activity gameplay occurrence. It never owns
    /// PlayerInput, never changes Action Map posture, and never falls back to hierarchy
    /// or global discovery.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerActorDeclaration))]
    [AddComponentMenu("Immersive Framework/Player/Gameplay Input Consumer Binding")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-GAMEPLAY-INPUT-CONSUMER-01 Actor-local generic gameplay input binding.")]
    public sealed class PlayerGameplayInputConsumerBinding :
        MonoBehaviour, IPlayerGameplayInputReader
    {
        private const string UnboundDiagnostic =
            "Gameplay input consumer is not bound to a current Activity gameplay occurrence.";

        [NonSerialized] private PlayerActorDeclaration actorDeclaration;
        [NonSerialized] private PlayerInput playerInput;
        [NonSerialized] private InputActionAsset runtimeActions;
        [NonSerialized] private InputActionMap gameplayActionMap;
        [NonSerialized] private PlayerGameplayInputBindingToken bindingToken;
        [NonSerialized] private Func<PlayerGameplayInputBindingToken, bool> readinessEvaluator;
        [NonSerialized] private int bindingRevision;
        [NonSerialized] private string diagnostic = UnboundDiagnostic;

        private readonly Dictionary<Guid, InputAction> resolvedActions =
            new Dictionary<Guid, InputAction>();

        public bool HasCurrentGameplayBinding =>
            bindingToken.IsValid &&
            actorDeclaration != null &&
            playerInput != null &&
            runtimeActions != null &&
            gameplayActionMap != null &&
            readinessEvaluator != null;

        public bool GameplayReady =>
            HasCurrentGameplayBinding &&
            isActiveAndEnabled &&
            playerInput.enabled &&
            playerInput.inputIsActive &&
            gameplayActionMap.enabled &&
            readinessEvaluator(bindingToken);

        public int BindingRevision => bindingRevision;
        public PlayerGameplayInputBindingToken CurrentBindingToken => bindingToken;
        public string Diagnostic => diagnostic ?? string.Empty;

        public bool TryReadValue<TValue>(
            InputActionReference authoredAction,
            out TValue value)
            where TValue : struct
        {
            value = default;
            if (!TryResolveReadableAction(authoredAction, out InputAction action))
                return false;

            try
            {
                value = action.ReadValue<TValue>();
                return true;
            }
            catch (Exception exception)
            {
                value = default;
                diagnostic =
                    $"Runtime action '{action.name}' could not be read as '{typeof(TValue).Name}'. {exception.Message}";
                return false;
            }
        }

        public bool TryIsPressed(
            InputActionReference authoredAction,
            out bool isPressed)
        {
            isPressed = false;
            if (!TryResolveReadableAction(authoredAction, out InputAction action))
                return false;

            isPressed = action.IsPressed();
            return true;
        }

        public bool TryWasPressedThisFrame(
            InputActionReference authoredAction,
            out bool wasPressed)
        {
            wasPressed = false;
            if (!TryResolveReadableAction(authoredAction, out InputAction action))
                return false;

            wasPressed = action.WasPressedThisFrame();
            return true;
        }

        public bool TryWasReleasedThisFrame(
            InputActionReference authoredAction,
            out bool wasReleased)
        {
            wasReleased = false;
            if (!TryResolveReadableAction(authoredAction, out InputAction action))
                return false;

            wasReleased = action.WasReleasedThisFrame();
            return true;
        }

        internal bool TryBindRuntime(
            PlayerActorDeclaration resolvedActorDeclaration,
            PlayerInput resolvedPlayerInput,
            InputActionMap resolvedGameplayActionMap,
            PlayerGameplayInputBindingToken resolvedBindingToken,
            Func<PlayerGameplayInputBindingToken, bool> resolvedReadinessEvaluator,
            out string issue)
        {
            issue = string.Empty;

            PlayerActorDeclaration localActor = GetComponent<PlayerActorDeclaration>();
            if (resolvedActorDeclaration == null ||
                !ReferenceEquals(localActor, resolvedActorDeclaration))
            {
                issue =
                    "Gameplay input consumer binding requires the PlayerActorDeclaration on the same Logical Actor.";
                return false;
            }

            if (resolvedPlayerInput == null ||
                resolvedActorDeclaration.PlayerInput == null ||
                !ReferenceEquals(resolvedActorDeclaration.PlayerInput, resolvedPlayerInput))
            {
                issue =
                    "Gameplay input consumer binding requires the exact PlayerInput evidence already correlated to this Logical Actor.";
                return false;
            }

            if (!resolvedBindingToken.IsValid || resolvedReadinessEvaluator == null)
            {
                issue =
                    "Gameplay input consumer binding requires a valid current gameplay input token and readiness evaluator.";
                return false;
            }

            InputActionAsset actions = resolvedPlayerInput.actions;
            if (actions == null || resolvedGameplayActionMap == null ||
                !ReferenceEquals(resolvedGameplayActionMap.asset, actions))
            {
                issue =
                    "Gameplay input consumer binding requires the exact gameplay Action Map from PlayerInput.actions.";
                return false;
            }

            if (HasCurrentGameplayBinding &&
                bindingToken == resolvedBindingToken &&
                ReferenceEquals(actorDeclaration, resolvedActorDeclaration) &&
                ReferenceEquals(playerInput, resolvedPlayerInput) &&
                ReferenceEquals(runtimeActions, actions) &&
                ReferenceEquals(gameplayActionMap, resolvedGameplayActionMap))
            {
                readinessEvaluator = resolvedReadinessEvaluator;
                diagnostic = "Gameplay input consumer binding is already current.";
                return true;
            }

            ClearRuntimeState(false, string.Empty);
            actorDeclaration = resolvedActorDeclaration;
            playerInput = resolvedPlayerInput;
            runtimeActions = actions;
            gameplayActionMap = resolvedGameplayActionMap;
            bindingToken = resolvedBindingToken;
            readinessEvaluator = resolvedReadinessEvaluator;
            resolvedActions.Clear();
            bindingRevision++;
            diagnostic = "Gameplay input consumer binding is current for the Activity gameplay occurrence.";
            return true;
        }

        internal void ReleaseRuntimeBinding(string reason)
        {
            ClearRuntimeState(true, reason);
        }

        private bool TryResolveReadableAction(
            InputActionReference authoredAction,
            out InputAction runtimeAction)
        {
            runtimeAction = null;
            if (!GameplayReady)
            {
                diagnostic = HasCurrentGameplayBinding
                    ? "Gameplay input consumer is bound but current gameplay is not Ready."
                    : UnboundDiagnostic;
                return false;
            }

            InputAction authored = authoredAction != null ? authoredAction.action : null;
            if (authored == null)
            {
                diagnostic =
                    "Gameplay input read requires an authored InputActionReference used as action identity.";
                return false;
            }

            Guid actionId = authored.id;
            if (!resolvedActions.TryGetValue(actionId, out runtimeAction) || runtimeAction == null)
            {
                if (!TryResolveRuntimeActionIdentity(
                        runtimeActions,
                        gameplayActionMap,
                        authoredAction,
                        out runtimeAction,
                        out diagnostic))
                    return false;

                resolvedActions[actionId] = runtimeAction;
            }

            if (!runtimeAction.enabled)
            {
                diagnostic =
                    $"Runtime action '{runtimeAction.name}' is not enabled in the current gameplay binding.";
                runtimeAction = null;
                return false;
            }

            diagnostic = "Gameplay input read resolved the current PlayerInput.actions instance.";
            return true;
        }


        internal static bool TryResolveRuntimeActionIdentity(
            InputActionAsset resolvedRuntimeActions,
            InputActionMap resolvedGameplayActionMap,
            InputActionReference authoredAction,
            out InputAction runtimeAction,
            out string issue)
        {
            runtimeAction = null;
            issue = string.Empty;

            InputAction authored = authoredAction != null ? authoredAction.action : null;
            if (authored == null)
            {
                issue =
                    "Gameplay input action resolution requires an authored InputActionReference used only as stable identity.";
                return false;
            }

            if (resolvedRuntimeActions == null || resolvedGameplayActionMap == null ||
                !ReferenceEquals(resolvedGameplayActionMap.asset, resolvedRuntimeActions))
            {
                issue =
                    "Gameplay input action resolution requires the exact current gameplay Action Map from PlayerInput.actions.";
                return false;
            }

            Guid actionId = authored.id;
            runtimeAction = resolvedRuntimeActions.FindAction(actionId.ToString(), false);
            if (runtimeAction == null)
            {
                issue =
                    $"Action GUID '{actionId}' was not found in the current PlayerInput.actions instance; name fallback is not used.";
                return false;
            }

            if (!ReferenceEquals(runtimeAction.actionMap, resolvedGameplayActionMap))
            {
                issue =
                    $"Action GUID '{actionId}' is not part of the current gameplay Action Map '{resolvedGameplayActionMap.name}'.";
                runtimeAction = null;
                return false;
            }

            return true;
        }

        private void ClearRuntimeState(bool incrementRevision, string reason)
        {
            bool hadBinding = HasCurrentGameplayBinding || bindingToken.IsValid;
            actorDeclaration = null;
            playerInput = null;
            runtimeActions = null;
            gameplayActionMap = null;
            bindingToken = default;
            readinessEvaluator = null;
            resolvedActions.Clear();

            if (incrementRevision && hadBinding)
                bindingRevision++;

            diagnostic = string.IsNullOrWhiteSpace(reason)
                ? UnboundDiagnostic
                : reason.Trim();
        }

        private void OnDestroy()
        {
            ClearRuntimeState(true,
                "Gameplay input consumer binding was destroyed; previous Activity input authority is invalid.");
        }
    }
}
