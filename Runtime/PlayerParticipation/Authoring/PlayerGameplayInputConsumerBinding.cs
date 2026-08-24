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

        [NonSerialized] private PlayerActorDeclaration _actorDeclaration;
        [NonSerialized] private PlayerInput _playerInput;
        [NonSerialized] private InputActionAsset _runtimeActions;
        [NonSerialized] private InputActionMap _gameplayActionMap;
        [NonSerialized] private PlayerGameplayInputBindingToken _bindingToken;
        [NonSerialized] private Func<PlayerGameplayInputBindingToken, bool> _readinessEvaluator;
        [NonSerialized] private int _bindingRevision;
        [NonSerialized] private string _diagnostic = UnboundDiagnostic;

        private readonly Dictionary<Guid, InputAction> _resolvedActions =
            new Dictionary<Guid, InputAction>();

        public bool HasCurrentGameplayBinding =>
            _bindingToken.IsValid &&
            _actorDeclaration != null &&
            _playerInput != null &&
            _runtimeActions != null &&
            _gameplayActionMap != null &&
            _readinessEvaluator != null;

        public bool GameplayReady =>
            HasCurrentGameplayBinding &&
            isActiveAndEnabled &&
            _playerInput.enabled &&
            _playerInput.inputIsActive &&
            _gameplayActionMap.enabled &&
            _readinessEvaluator(_bindingToken);

        public int BindingRevision => _bindingRevision;
        public PlayerGameplayInputBindingToken CurrentBindingToken => _bindingToken;
        public string Diagnostic => _diagnostic ?? string.Empty;

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
                _diagnostic =
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
                _bindingToken == resolvedBindingToken &&
                ReferenceEquals(_actorDeclaration, resolvedActorDeclaration) &&
                ReferenceEquals(_playerInput, resolvedPlayerInput) &&
                ReferenceEquals(_runtimeActions, actions) &&
                ReferenceEquals(_gameplayActionMap, resolvedGameplayActionMap))
            {
                _readinessEvaluator = resolvedReadinessEvaluator;
                _diagnostic = "Gameplay input consumer binding is already current.";
                return true;
            }

            ClearRuntimeState(false, string.Empty);
            _actorDeclaration = resolvedActorDeclaration;
            _playerInput = resolvedPlayerInput;
            _runtimeActions = actions;
            _gameplayActionMap = resolvedGameplayActionMap;
            _bindingToken = resolvedBindingToken;
            _readinessEvaluator = resolvedReadinessEvaluator;
            _resolvedActions.Clear();
            _bindingRevision++;
            _diagnostic = "Gameplay input consumer binding is current for the Activity gameplay occurrence.";
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
                _diagnostic = HasCurrentGameplayBinding
                    ? "Gameplay input consumer is bound but current gameplay is not Ready."
                    : UnboundDiagnostic;
                return false;
            }

            InputAction authored = authoredAction != null ? authoredAction.action : null;
            if (authored == null)
            {
                _diagnostic =
                    "Gameplay input read requires an authored InputActionReference used as action identity.";
                return false;
            }

            Guid actionId = authored.id;
            if (!_resolvedActions.TryGetValue(actionId, out runtimeAction) || runtimeAction == null)
            {
                if (!TryResolveRuntimeActionIdentity(
                        _runtimeActions,
                        _gameplayActionMap,
                        authoredAction,
                        out runtimeAction,
                        out _diagnostic))
                    return false;

                _resolvedActions[actionId] = runtimeAction;
            }

            if (!runtimeAction.enabled)
            {
                _diagnostic =
                    $"Runtime action '{runtimeAction.name}' is not enabled in the current gameplay binding.";
                runtimeAction = null;
                return false;
            }

            _diagnostic = "Gameplay input read resolved the current PlayerInput.actions instance.";
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
            bool hadBinding = HasCurrentGameplayBinding || _bindingToken.IsValid;
            _actorDeclaration = null;
            _playerInput = null;
            _runtimeActions = null;
            _gameplayActionMap = null;
            _bindingToken = default;
            _readinessEvaluator = null;
            _resolvedActions.Clear();

            if (incrementRevision && hadBinding)
                _bindingRevision++;

            _diagnostic = string.IsNullOrWhiteSpace(reason)
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
