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
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionReference pauseAction;

        [Header("Action Maps")]
        [SerializeField] private string globalActionMapName = "Global";
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";

        private IPauseProductBindingPort _port;
        private PauseProductBindingToken _token;
        private string _bindingStatus = "Unbound";
        private string _bindingDiagnostic = "Pause binding has not been composed by Scene Lifecycle.";

        public PlayerInput PlayerInput => playerInput;
        public InputActionReference PauseAction => pauseAction;
        public string GlobalActionMapName => globalActionMapName.NormalizeTextOrFallback("Global");
        public string GameplayActionMapName => gameplayActionMapName.NormalizeTextOrFallback("Player");
        public string UiActionMapName => uiActionMapName.NormalizeTextOrFallback("UI");
        public string BindingStatus => _bindingStatus.NormalizeText();
        public string BindingDiagnostic => _bindingDiagnostic.NormalizeText();
        public bool HasActiveBinding => _token.IsValid;

        internal bool TryInjectBindingPort(IPauseProductBindingPort port, out string diagnostic)
        {
            if (port == null)
            {
                diagnostic = "Pause PlayerInput Binding requires a non-null lifecycle binding port.";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            if (_port != null && !ReferenceEquals(_port, port))
            {
                diagnostic = "Pause PlayerInput Binding rejected a different binding port for its current scene lifetime.";
                _bindingDiagnostic = diagnostic;
                return false;
            }

            _port = port;
            if (_token.IsValid)
            {
                diagnostic = "Pause PlayerInput Binding is already registered (idempotent).";
                _bindingDiagnostic = diagnostic;
                return true;
            }

            _bindingStatus = "Binding";
            if (!port.TryRegister(this, out _token, out diagnostic))
            {
                _bindingStatus = "Failed";
                _bindingDiagnostic = diagnostic;
                return false;
            }

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
            input = playerInput;
            runtimeAction = null;
            adapter = null;
            if (input == null || input.actions == null)
            {
                diagnostic = "Pause PlayerInput Binding requires an explicit PlayerInput with actions.";
                return false;
            }
            if (pauseAction == null || pauseAction.action == null)
            {
                diagnostic = "Pause PlayerInput Binding requires an InputActionReference.";
                return false;
            }

            var globalMap = input.actions.FindActionMap(GlobalActionMapName, false);
            if (globalMap == null)
            {
                diagnostic = $"Pause PlayerInput Binding requires Global action map '{GlobalActionMapName}'.";
                return false;
            }
            if (input.actions.FindActionMap(GameplayActionMapName, false) == null || input.actions.FindActionMap(UiActionMapName, false) == null)
            {
                diagnostic = "Pause PlayerInput Binding requires explicit Gameplay and UI action maps.";
                return false;
            }

            runtimeAction = input.actions.FindAction(pauseAction.action.id.ToString(), false);
            if (runtimeAction == null || ReferenceEquals(runtimeAction, pauseAction.action) || runtimeAction.actionMap == null || runtimeAction.actionMap.id != globalMap.id)
            {
                diagnostic = "Pause action must resolve by GUID inside PlayerInput.actions and belong to the configured Global map; name fallback is not used.";
                return false;
            }

            UnityPlayerInputGateAdapter[] adapters = GetComponents<UnityPlayerInputGateAdapter>();
            if (adapters.Length != 1 || adapters[0] == null || !ReferenceEquals(adapters[0].PlayerInput, input))
            {
                diagnostic = "Pause PlayerInput Binding requires exactly one compatible UnityPlayerInputGateAdapter on the same GameObject.";
                return false;
            }

            adapter = adapters[0];
            diagnostic = string.Empty;
            return true;
        }

        internal bool ReleaseForSceneLifecycle(string reason, out string diagnostic)
        {
            if (!_token.IsValid || _port == null)
            {
                diagnostic = "Pause PlayerInput Binding is already released.";
                return true;
            }

            PauseProductBindingToken token = _token;
            _token = default;
            _bindingStatus = "Unbinding";
            bool released = _port.ReleaseBinding(token, reason, out diagnostic);
            _bindingStatus = released ? "Unbound" : "Failed";
            _bindingDiagnostic = diagnostic;
            if (released)
            {
                _port = null;
            }
            return released;
        }

        private void OnDisable() => ReleaseLocal("component-disabled");
        private void OnDestroy() => ReleaseLocal("component-destroyed");

        private void ReleaseLocal(string reason)
        {
            if (!_token.IsValid || _port == null)
            {
                return;
            }

            ReleaseForSceneLifecycle(reason, out _);
        }
    }
}
