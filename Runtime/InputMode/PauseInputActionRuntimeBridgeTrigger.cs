using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Common.FlowTriggers;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Opt-in Unity InputAction trigger that forwards a concrete Pause action to the explicit Pause runtime PlayerInput bridge.
    /// This component subscribes to an action performed callback only; it does not own PlayerInputManager, call JoinPlayer, spawn players,
    /// read gameplay commands, switch action maps directly or create a custom input manager.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input Mode/Pause InputAction Runtime Bridge Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33B opt-in Unity InputAction trigger for Pause runtime PlayerInput bridge.")]
    public sealed class PauseInputActionRuntimeBridgeTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseInputActionRuntimeBridgeTrigger);

        private FrameworkLogger _logger;
        private InputAction _subscribedAction;
        private readonly FrameworkFlowTriggerState _triggerState = new FrameworkFlowTriggerState();
        private PauseInputActionRuntimeBridgeTriggerResult _lastResult;

        [Header("Bridge")]
        [SerializeField] private PauseInputModeUnityPlayerInputRuntimeBridge bridge;
        [Tooltip("When enabled and Bridge is unassigned, resolve only a co-located or parent PauseInputModeUnityPlayerInputRuntimeBridge. Scene-wide Find is never used.")]
        [SerializeField] private bool autoDiscoverBridge = false;

        [Header("Unity InputAction Evidence")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string actionMapName = "UI";
        [SerializeField] private string actionName = "Pause";
        [SerializeField] private bool requireActionEvidence = true;
        [SerializeField] private bool subscribeOnEnable = true;

        [Header("Pause Request")]
        [SerializeField] private PauseRequestKind requestKind = PauseRequestKind.Toggle;
        [SerializeField] private string reason = "pause.input.action.runtime.bridge.trigger";
        [SerializeField] private bool logResults = true;

        public PauseInputModeUnityPlayerInputRuntimeBridge Bridge => bridge;

        public PlayerInput PlayerInput => playerInput;

        public string ActionMapName => actionMapName.NormalizeTextOrFallback("UI");

        public string ActionName => actionName.NormalizeTextOrFallback("Pause");

        public bool RequireActionEvidence => requireActionEvidence;

        public bool SubscribeOnEnable => subscribeOnEnable;

        public PauseRequestKind RequestKind => requestKind;

        public PauseInputActionRuntimeBridgeTriggerResult LastResult => _lastResult;

        public bool HasLastResult => _lastResult != null;

        public bool LastSucceeded => _lastResult != null && _lastResult.Succeeded;

        public bool LastIgnored => _lastResult != null && _lastResult.Ignored;

        public bool LastFailed => _lastResult != null && _lastResult.Failed;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseInputActionRuntimeBridgeTrigger>();
        }

        private void OnEnable()
        {
            if (subscribeOnEnable)
            {
                SubscribeConfiguredAction();
            }
        }

        private void OnDisable()
        {
            UnsubscribeConfiguredAction();
        }

        [ContextMenu("Immersive Framework/Pause/InputAction Bridge Trigger/Trigger")]
        public void Trigger()
        {
            _lastResult = SubmitInternal(DefaultSource, ResolveReason("pause.input.action.runtime.bridge.trigger.context-menu"));
            LogLastResult();
        }

        internal void ConfigureForDiagnostics(
            PauseInputModeUnityPlayerInputRuntimeBridge runtimeBridge,
            PlayerInput input,
            string mapName,
            string pauseActionName,
            PauseRequestKind kind,
            bool requireEvidence,
            bool subscribe,
            bool autoDiscoverRuntimeBridge)
        {
            UnsubscribeConfiguredAction();
            bridge = runtimeBridge;
            playerInput = input;
            actionMapName = mapName.NormalizeTextOrFallback("UI");
            actionName = pauseActionName.NormalizeTextOrFallback("Pause");
            requestKind = kind;
            requireActionEvidence = requireEvidence;
            subscribeOnEnable = subscribe;
            autoDiscoverBridge = autoDiscoverRuntimeBridge;
            if (isActiveAndEnabled && subscribeOnEnable)
            {
                SubscribeConfiguredAction();
            }
        }

        internal PauseInputActionRuntimeBridgeTriggerResult SubmitForDiagnostics(string source, string requestReason)
        {
            _lastResult = SubmitInternal(source, requestReason);
            return _lastResult;
        }

        internal bool TryResolveConfiguredActionForDiagnostics(out string message)
        {
            return TryResolveAction(out _, out message);
        }

        private PauseInputActionRuntimeBridgeTriggerResult SubmitInternal(string source, string requestReason)
        {
            string normalizedSource = FrameworkFlowTriggerDiagnostics.NormalizeSource(source, DefaultSource);
            string normalizedReason = FrameworkFlowTriggerDiagnostics.NormalizeReason(requestReason, ResolveReason("pause.input.action.runtime.bridge.trigger"));
            string resolvedMapName = ActionMapName;
            string resolvedActionName = ActionName;

            if (!TryResolveBridge(out PauseInputModeUnityPlayerInputRuntimeBridge resolvedBridge))
            {
                return CreateResult(
                    PauseInputActionRuntimeBridgeTriggerStatus.FailedBridgeMissing,
                    resolvedMapName,
                    resolvedActionName,
                    false,
                    false,
                    null,
                    normalizedSource,
                    normalizedReason,
                    "Pause InputAction runtime bridge trigger requires an explicit PauseInputModeUnityPlayerInputRuntimeBridge.");
            }

            bool actionResolved = TryResolveAction(out _, out string actionMessage);
            if (requireActionEvidence && !actionResolved)
            {
                return CreateResult(
                    PauseInputActionRuntimeBridgeTriggerStatus.FailedActionEvidence,
                    resolvedMapName,
                    resolvedActionName,
                    false,
                    false,
                    null,
                    normalizedSource,
                    normalizedReason,
                    actionMessage);
            }

            PauseInputModeUnityPlayerInputRuntimeBridgeResult bridgeResult = resolvedBridge.SubmitForDiagnostics(
                requestKind,
                normalizedSource,
                normalizedReason);

            PauseInputActionRuntimeBridgeTriggerStatus status = bridgeResult.Succeeded
                ? PauseInputActionRuntimeBridgeTriggerStatus.Succeeded
                : bridgeResult.Ignored
                    ? PauseInputActionRuntimeBridgeTriggerStatus.IgnoredBridgeResult
                    : PauseInputActionRuntimeBridgeTriggerStatus.FailedBridgeRequest;

            return CreateResult(
                status,
                resolvedMapName,
                resolvedActionName,
                actionResolved,
                true,
                bridgeResult,
                normalizedSource,
                normalizedReason,
                $"Pause runtime PlayerInput bridge {bridgeResult.Status}.");
        }

        private void SubscribeConfiguredAction()
        {
            UnsubscribeConfiguredAction();
            if (!TryResolveAction(out InputAction action, out _))
            {
                return;
            }

            _subscribedAction = action;
            _subscribedAction.performed += OnInputActionPerformed;
        }

        private void UnsubscribeConfiguredAction()
        {
            if (_subscribedAction == null)
            {
                return;
            }

            _subscribedAction.performed -= OnInputActionPerformed;
            _subscribedAction = null;
        }

        private void OnInputActionPerformed(InputAction.CallbackContext context)
        {
            _lastResult = SubmitInternal(DefaultSource, ResolveReason("pause.input.action.runtime.bridge.trigger.performed"));
            LogLastResult();
        }

        private bool TryResolveBridge(out PauseInputModeUnityPlayerInputRuntimeBridge resolvedBridge)
        {
            if (bridge != null)
            {
                resolvedBridge = bridge;
                return true;
            }

            if (!autoDiscoverBridge)
            {
                resolvedBridge = null;
                return false;
            }

            // Hierarchy-local evidence only. Never pick the first scene-wide FindObjectsByType hit.
            resolvedBridge = GetComponent<PauseInputModeUnityPlayerInputRuntimeBridge>();
            if (resolvedBridge != null)
            {
                return true;
            }

            resolvedBridge = GetComponentInParent<PauseInputModeUnityPlayerInputRuntimeBridge>();
            return resolvedBridge != null;
        }

        private bool TryResolveAction(out InputAction action, out string message)
        {
            action = null;
            message = string.Empty;

            PlayerInput input = ResolvePlayerInput();
            if (input == null)
            {
                message = "Pause InputAction runtime bridge trigger requires PlayerInput evidence.";
                return false;
            }

            if (input.actions == null)
            {
                message = "Pause InputAction runtime bridge trigger requires PlayerInput.actions.";
                return false;
            }

            string resolvedActionName = ActionName;
            string resolvedMapName = ActionMapName;

            if (!string.IsNullOrWhiteSpace(resolvedMapName))
            {
                InputActionMap map = input.actions.FindActionMap(resolvedMapName, false);
                if (map == null)
                {
                    message = $"Pause InputAction runtime bridge trigger action map '{resolvedMapName}' was not found.";
                    return false;
                }

                action = map.FindAction(resolvedActionName, false);
            }
            else
            {
                action = input.actions.FindAction(resolvedActionName, false);
            }

            if (action == null)
            {
                message = $"Pause InputAction runtime bridge trigger action '{resolvedActionName}' was not found.";
                return false;
            }

            return true;
        }

        private PlayerInput ResolvePlayerInput()
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            if (bridge != null && bridge.PlayerInput != null)
            {
                return bridge.PlayerInput;
            }

            if (autoDiscoverBridge
                && TryResolveBridge(out PauseInputModeUnityPlayerInputRuntimeBridge resolvedBridge)
                && resolvedBridge.PlayerInput != null)
            {
                return resolvedBridge.PlayerInput;
            }

            // Same GameObject only when auto-discover is opted in; otherwise require explicit wiring.
            return autoDiscoverBridge ? GetComponent<PlayerInput>() : null;
        }

        private PauseInputActionRuntimeBridgeTriggerResult CreateResult(
            PauseInputActionRuntimeBridgeTriggerStatus status,
            string resolvedActionMapName,
            string resolvedActionName,
            bool actionResolved,
            bool bridgeSubmitted,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult bridgeResult,
            string source,
            string requestReason,
            string message)
        {
            var result = new PauseInputActionRuntimeBridgeTriggerResult(
                status,
                requestKind,
                resolvedActionMapName,
                resolvedActionName,
                requireActionEvidence,
                actionResolved,
                bridgeSubmitted,
                bridgeResult,
                source,
                requestReason,
                message);
            RecordResult(result);
            return result;
        }

        private string ResolveReason(string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(fallbackReason);
        }

        private void LogLastResult()
        {
            if (!logResults || _lastResult == null)
            {
                return;
            }

            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseInputActionRuntimeBridgeTrigger>();
            }

            _logger.Debug(
                "Pause InputAction Runtime Bridge Trigger completed.",
                LogFields.Of(
                    LogFields.Field("status", _lastResult.Status.ToString()),
                    LogFields.Field("requestKind", _lastResult.RequestKind.ToString()),
                    LogFields.Field("actionMap", _lastResult.ActionMapName),
                    LogFields.Field("action", _lastResult.ActionName),
                    LogFields.Field("bridgeSubmitted", _lastResult.BridgeSubmitted),
                    LogFields.Field("requestedMode", _lastResult.RequestedMode.ToString()),
                    LogFields.Field("operation", _lastResult.Operation.ToString()),
                    LogFields.Field("actionMapSwitching", _lastResult.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", _lastResult.AppliesInputBehavior),
                    LogFields.Field("playerJoin", _lastResult.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", _lastResult.SpawnsActor),
                    LogFields.Field("triggerOutcome", _triggerState.LastOutcome),
                    LogFields.Field("triggerBlockingIssues", _triggerState.LastBlockingIssueCount),
                    LogFields.Field("diagnostics", _lastResult.ToDiagnosticString())));
        }

        private void RecordResult(PauseInputActionRuntimeBridgeTriggerResult result)
        {
            if (result == null)
            {
                _triggerState.CompleteFailed(
                    DefaultSource,
                    ResolveReason("pause.input.action.runtime.bridge.trigger"),
                    "Pause InputAction runtime bridge trigger did not produce a result.",
                    1,
                    1);
                return;
            }

            if (result.Succeeded)
            {
                _triggerState.CompleteSucceeded(
                    result.Source,
                    result.Reason,
                    result.Message,
                    result.IssueCount,
                    result.BlockingIssueCount);
                return;
            }

            if (result.Ignored)
            {
                _triggerState.CompleteIgnored(
                    result.Source,
                    result.Reason,
                    result.Message,
                    result.IssueCount,
                    result.BlockingIssueCount);
                return;
            }

            _triggerState.CompleteFailed(
                result.Source,
                result.Reason,
                result.Message,
                result.IssueCount,
                result.BlockingIssueCount);
        }
    }
}
