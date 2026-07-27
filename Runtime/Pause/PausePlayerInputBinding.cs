using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause PlayerInput Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P1 scene-local single-player Pause product surface.")]
    public sealed class PausePlayerInputBinding : MonoBehaviour
    {
        [Header("Pause PlayerInput Binding")]
        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private InputActionReference pauseAction;

        [SerializeField]
        private PlayerInputActionMapReference gameplayActionMap;

        [SerializeField, HideInInspector]
        private string gameplayActionMapName = "Player";

        private IPauseProductBindingPort _port;
        private PauseProductBindingToken _token;
        private string _bindingStatus = "Unbound";
        private string _bindingDiagnostic =
            "Pause binding has not been composed by Scene Lifecycle.";

        public PlayerInput PlayerInput => playerInput;

        public InputActionReference PauseAction => pauseAction;

        public PlayerInputActionMapReference GameplayActionMapReference =>
            gameplayActionMap;

        public string GlobalActionMapName
        {
            get
            {
                return TryResolvePauseAction(
                        playerInput,
                        out _,
                        out InputActionMap globalMap,
                        out _)
                    ? globalMap.name.NormalizeText()
                    : string.Empty;
            }
        }

        public string GameplayActionMapName
        {
            get
            {
                return gameplayActionMap.TryResolve(
                        playerInput != null
                            ? playerInput.actions
                            : null,
                        out InputActionMap map,
                        out _)
                    ? map.name.NormalizeText()
                    : string.Empty;
            }
        }

        public string BindingStatus => _bindingStatus.NormalizeText();

        public string BindingDiagnostic => _bindingDiagnostic.NormalizeText();

        public bool HasActiveBinding => _token.IsValid;

        public bool TryValidateAuthoring(
            out string diagnostic)
        {
            return TryResolveConfiguration(
                out _,
                out _,
                out _,
                out _,
                out _,
                out diagnostic);
        }

        internal bool TryInjectBindingPort(
            IPauseProductBindingPort port,
            out string diagnostic)
        {
            if (port == null)
            {
                diagnostic =
                    "Pause PlayerInput Binding requires a non-null lifecycle binding port.";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            if (_token.IsValid)
            {
                if (ReferenceEquals(
                        _port,
                        port))
                {
                    diagnostic =
                        "Pause PlayerInput Binding is already registered (idempotent).";
                    _bindingDiagnostic = diagnostic;
                    return true;
                }

                diagnostic =
                    "Pause PlayerInput Binding rejected a different binding port for its current scene lifetime.";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            if (_port != null)
            {
                diagnostic =
                    "Pause PlayerInput Binding has inconsistent retained port evidence without a binding token.";
                _bindingDiagnostic = diagnostic;
                _bindingStatus = "Failed";
                return false;
            }

            _bindingStatus = "Binding";

            if (!port.TryRegister(
                    this,
                    out PauseProductBindingToken token,
                    out diagnostic))
            {
                _bindingStatus = "Failed";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            if (!token.IsValid)
            {
                diagnostic =
                    "Pause PlayerInput Binding registration returned an invalid binding token.";
                _bindingStatus = "Failed";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            _port = port;
            _token = token;
            _bindingStatus = "Bound";
            _bindingDiagnostic = diagnostic;
            return true;
        }

        internal bool TryGetRuntimeConfiguration(
            out PlayerInput input,
            out InputAction runtimeAction,
            out UnityPlayerInputGateAdapter adapter,
            out string diagnostic)
        {
            return TryResolveConfiguration(
                out input,
                out runtimeAction,
                out _,
                out _,
                out adapter,
                out diagnostic);
        }

        internal bool ReleaseForSceneLifecycle(
            string reason,
            out string diagnostic)
        {
            return TryReleaseBinding(
                reason,
                out diagnostic);
        }

        internal bool TryReleaseBinding(
            string reason,
            out string diagnostic)
        {
            if (!_token.IsValid &&
                _port == null)
            {
                diagnostic =
                    "Pause PlayerInput Binding is already released.";
                return true;
            }

            if (!_token.IsValid ||
                _port == null)
            {
                diagnostic =
                    "Pause PlayerInput Binding has inconsistent retained port/token evidence and cannot release safely.";
                _bindingStatus = "Failed";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            _bindingStatus = "Unbinding";

            bool released =
                _port.ReleaseBinding(
                    _token,
                    reason,
                    out diagnostic);

            if (!released)
            {
                _bindingStatus = "Failed";
                diagnostic =
                    $"Pause PlayerInput Binding release failed; binding retained for retry. {diagnostic.NormalizeTextOrFallback("No release diagnostic was supplied.")}";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            _token = default;
            _port = null;
            _bindingStatus = "Unbound";
            _bindingDiagnostic = diagnostic;
            return true;
        }

        private bool TryResolveConfiguration(
            out PlayerInput input,
            out InputAction runtimeAction,
            out InputActionMap globalMap,
            out InputActionMap gameplayMap,
            out UnityPlayerInputGateAdapter adapter,
            out string diagnostic)
        {
            input = playerInput;
            runtimeAction = null;
            globalMap = null;
            gameplayMap = null;
            adapter = null;

            if (input == null ||
                input.actions == null)
            {
                diagnostic =
                    "Pause PlayerInput Binding requires an explicit PlayerInput with actions.";
                return false;
            }

            if (!TryResolvePauseAction(
                    input,
                    out runtimeAction,
                    out globalMap,
                    out diagnostic))
            {
                return false;
            }

            if (!gameplayActionMap.TryResolve(
                    input.actions,
                    out gameplayMap,
                    out diagnostic))
            {
                diagnostic =
                    $"Pause PlayerInput Binding Gameplay Action Map is invalid. {diagnostic}";
                return false;
            }

            if (globalMap.id ==
                gameplayMap.id)
            {
                diagnostic =
                    "Pause PlayerInput Binding requires distinct Global and Gameplay action maps.";
                return false;
            }

            UnityPlayerInputGateAdapter[] adapters =
                GetComponents<
                    UnityPlayerInputGateAdapter>();

            if (adapters.Length != 1 ||
                adapters[0] == null ||
                !ReferenceEquals(
                    adapters[0].PlayerInput,
                    input))
            {
                diagnostic =
                    "Pause PlayerInput Binding requires exactly one compatible UnityPlayerInputGateAdapter on the same GameObject.";
                return false;
            }

            if (!adapters[0].TryResolveGameplayActionMap(
                    out InputActionMap adapterGameplayMap,
                    out string adapterDiagnostic))
            {
                diagnostic =
                    $"Pause PlayerInput Binding requires a valid Gate Adapter Gameplay Action Map. {adapterDiagnostic}";
                return false;
            }

            if (adapterGameplayMap.id !=
                gameplayMap.id)
            {
                diagnostic =
                    "Pause PlayerInput Binding and UnityPlayerInputGateAdapter must reference the same Gameplay Action Map GUID.";
                return false;
            }

            adapter = adapters[0];
            diagnostic = string.Empty;
            return true;
        }

        private bool TryResolvePauseAction(
            PlayerInput input,
            out InputAction runtimeAction,
            out InputActionMap globalMap,
            out string diagnostic)
        {
            runtimeAction = null;
            globalMap = null;

            if (input == null ||
                input.actions == null)
            {
                diagnostic =
                    "Pause action resolution requires PlayerInput actions.";
                return false;
            }

            if (pauseAction == null ||
                pauseAction.action == null)
            {
                diagnostic =
                    "Pause PlayerInput Binding requires an InputActionReference.";
                return false;
            }

            if (pauseAction.action.actionMap == null ||
                pauseAction.action.actionMap.asset == null)
            {
                diagnostic =
                    "Pause Action must belong to an InputActionMap inside an InputActionAsset.";
                return false;
            }

            runtimeAction =
                input.actions.FindAction(
                    pauseAction.action.id.ToString(),
                    false);

            if (runtimeAction == null)
            {
                diagnostic =
                    "Pause action GUID was not found inside PlayerInput.actions; name fallback is not used.";
                return false;
            }

            globalMap =
                runtimeAction.actionMap;

            if (globalMap == null)
            {
                runtimeAction = null;
                diagnostic =
                    "Pause action resolved by GUID but has no runtime Action Map.";
                return false;
            }

            if (pauseAction.action.actionMap.id !=
                globalMap.id)
            {
                runtimeAction = null;
                globalMap = null;
                diagnostic =
                    "Pause action source and PlayerInput runtime map identities do not match.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            TryMigrateLegacyGameplayMap();
        }

        private void TryMigrateLegacyGameplayMap()
        {
            if (gameplayActionMap.IsConfigured ||
                playerInput == null ||
                playerInput.actions == null)
            {
                return;
            }

            string legacyName =
                gameplayActionMapName.NormalizeText();

            if (string.IsNullOrEmpty(
                    legacyName))
            {
                return;
            }

            InputActionMap legacyMap =
                playerInput.actions.FindActionMap(
                    legacyName,
                    false);

            if (legacyMap != null)
            {
                gameplayActionMap =
                    PlayerInputActionMapReference.From(
                        legacyMap);
            }
        }

        private void OnDisable() =>
            ReleaseLocal(
                "component-disabled");

        private void OnDestroy() =>
            ReleaseLocal(
                "component-destroyed");

        private void ReleaseLocal(
            string reason)
        {
            if (!_token.IsValid ||
                _port == null)
            {
                return;
            }

            ReleaseForSceneLifecycle(
                reason,
                out _);
        }
    }
}
