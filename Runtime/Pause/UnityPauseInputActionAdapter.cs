using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.InputSystem;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Removed. Legacy F27B adapter retained as an inert migration stub only.
    /// The canonical Pause input path is PauseInputActionRuntimeBridgeTrigger ->
    /// PauseInputModeUnityPlayerInputRuntimeBridge -> InputMode Unity PlayerInput application.
    /// This stub deliberately does not subscribe to InputAction callbacks or submit Pause requests.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Removed/Legacy Unity Pause Input Action Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Removed, "F33C retires F27B direct Pause InputAction adapter. Use PauseInputActionRuntimeBridgeTrigger.")]
    public sealed class UnityPauseInputActionAdapter : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityPauseInputActionAdapter);
        private const string DefaultReasonPrefix = "input_action";
        private const string RemovedReason = "legacy_pause_input_action_adapter_removed";
        private const string RemovedMessage = "UnityPauseInputActionAdapter is retired. Use PauseInputActionRuntimeBridgeTrigger with PauseInputModeUnityPlayerInputRuntimeBridge so Pause, InputMode and PlayerInput stay synchronized.";

        private FrameworkLogger _logger;
        private InputAction _playerPauseToggleAction;
        private InputAction _uiPauseToggleAction;
        private bool _subscribed;
        private int _lastHandledFrame = -1;
        private string _lastHandledAction = string.Empty;
        private string _lastIgnoredReason = string.Empty;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private PauseRequestStatus _lastStatus = PauseRequestStatus.Unknown;
        private PauseState _lastPreviousState = PauseState.Unknown;
        private PauseState _lastCurrentState = PauseState.Unknown;

        [Header("Input Asset")]
        [SerializeField] private InputActionAsset actionsAsset;

        [Header("Action Names")]
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";
        [SerializeField] private string pauseToggleActionName = "PauseToggle";

        [Header("Runtime")]
        [SerializeField] private bool enableResolvedActionsOnEnable = true;
        [SerializeField] private bool requirePlayerAction = true;
        [SerializeField] private bool requireUiAction = true;
        [SerializeField] private bool logReadyOnEnable = true;
        [SerializeField] private bool logPerformedInput = true;
        [SerializeField] private bool logIgnoredInput = true;

        public bool IsRemovedAdapter => true;

        public string RemovedAdapterReason => RemovedReason;

        public string RemovedAdapterMessage => RemovedMessage;

        public InputActionAsset ActionsAsset => actionsAsset;

        public string PlayerActionMapName => playerActionMapName;

        public string UiActionMapName => uiActionMapName;

        public string PauseToggleActionName => pauseToggleActionName;

        public bool IsSubscribed => _subscribed;

        public string LastHandledAction => _lastHandledAction;

        public string LastIgnoredReason => _lastIgnoredReason;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public PauseRequestStatus LastStatus => _lastStatus;

        public PauseState LastPreviousState => _lastPreviousState;

        public PauseState LastCurrentState => _lastCurrentState;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<UnityPauseInputActionAdapter>();
        }

        private void OnEnable()
        {
            EnsureLogger();
            ReportRemovedAdapter();
        }

        private void OnDisable()
        {
            UnsubscribeResolvedActions();
        }

        private void OnDestroy()
        {
            UnsubscribeResolvedActions();
        }

        [ContextMenu("Immersive Framework/Rebind Pause Input Actions")]
        public void Rebind()
        {
            EnsureLogger();
            ReportRemovedAdapter();
        }


        private void ReportRemovedAdapter()
        {
            UnsubscribeResolvedActions();
            _lastIgnoredReason = RemovedReason;
            _lastOutcome = FlowRequestOutcome.Failed;
            _lastStatus = PauseRequestStatus.Failed;
            _lastPreviousState = PauseState.Unknown;
            _lastCurrentState = PauseState.Unknown;

            if (logIgnoredInput)
            {
                _logger.Warning(
                    "Legacy Pause Input Action Adapter is removed. "
                    + $"reason='{RemovedReason}' "
                    + $"message='{RemovedMessage}' "
                    + $"source='{DefaultSource}'.");
            }
        }

        private void SubscribeResolvedActions()
        {
            if (_subscribed)
            {
                return;
            }

            if (actionsAsset == null)
            {
                IgnoreConfiguration("actions_asset_missing", "Pause input adapter requires an InputActionAsset.");
                return;
            }

            string resolvedActionName = Normalize(pauseToggleActionName, "PauseToggle");
            _playerPauseToggleAction = ResolveAction(actionsAsset, playerActionMapName, resolvedActionName, requirePlayerAction);
            _uiPauseToggleAction = ResolveAction(actionsAsset, uiActionMapName, resolvedActionName, requireUiAction);

            if (_playerPauseToggleAction == null && _uiPauseToggleAction == null)
            {
                IgnoreConfiguration("pause_toggle_actions_missing", "Pause input adapter did not resolve any PauseToggle action.");
                return;
            }

            Subscribe(_playerPauseToggleAction);
            Subscribe(_uiPauseToggleAction);
            _subscribed = true;

            if (logReadyOnEnable)
            {
                _logger.Debug(
                    "Pause Input Action Adapter ready. "
                    + $"asset='{actionsAsset.name}' "
                    + $"playerAction='{FormatAction(_playerPauseToggleAction)}' "
                    + $"uiAction='{FormatAction(_uiPauseToggleAction)}' "
                    + $"enabledOnSubscribe='{enableResolvedActionsOnEnable}' "
                    + $"source='{DefaultSource}' "
                    + "reason='pause_input_action_adapter_ready'.");
            }
        }

        private void Subscribe(InputAction action)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= OnPauseTogglePerformed;
            action.performed += OnPauseTogglePerformed;

            if (enableResolvedActionsOnEnable && !action.enabled)
            {
                action.Enable();
            }
        }

        private void UnsubscribeResolvedActions()
        {
            Unsubscribe(_playerPauseToggleAction);
            Unsubscribe(_uiPauseToggleAction);
            _playerPauseToggleAction = null;
            _uiPauseToggleAction = null;
            _subscribed = false;
        }

        private void Unsubscribe(InputAction action)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= OnPauseTogglePerformed;
        }

        private InputAction ResolveAction(
            InputActionAsset asset,
            string actionMapName,
            string actionName,
            bool required)
        {
            string resolvedMapName = Normalize(actionMapName, string.Empty);
            if (string.IsNullOrWhiteSpace(resolvedMapName))
            {
                if (required)
                {
                    IgnoreConfiguration("action_map_name_missing", $"Pause input adapter requires an action map name for action '{actionName}'.");
                }

                return null;
            }

            var actionMap = asset.FindActionMap(resolvedMapName, false);
            if (actionMap == null)
            {
                if (required)
                {
                    IgnoreConfiguration(
                        "action_map_missing",
                        $"Pause input adapter could not find action map '{resolvedMapName}' in asset '{asset.name}'.");
                }

                return null;
            }

            var action = actionMap.FindAction(actionName, false);
            if (action == null)
            {
                if (required)
                {
                    IgnoreConfiguration(
                        "action_missing",
                        $"Pause input adapter could not find action '{actionName}' in map '{resolvedMapName}' asset '{asset.name}'.");
                }

                return null;
            }

            return action;
        }

        private void OnPauseTogglePerformed(InputAction.CallbackContext context)
        {
            EnsureLogger();

            if (!_subscribed)
            {
                IgnoreInput("adapter_not_subscribed", context.action);
                return;
            }

            if (_lastHandledFrame == Time.frameCount)
            {
                IgnoreInput("dedupe_same_frame", context.action);
                return;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                IgnoreInput("runtime_host_unavailable", context.action);
                return;
            }

            string actionPath = FormatAction(context.action);
            string reason = $"{DefaultReasonPrefix}:{actionPath}";
            PauseResult result;

            try
            {
                result = runtimeHost.RequestPause(PauseRequestKind.Toggle, DefaultSource, reason);
            }
            catch (Exception exception)
            {
                _lastOutcome = FlowRequestOutcome.Failed;
                _lastStatus = PauseRequestStatus.Failed;
                _lastPreviousState = PauseState.Unknown;
                _lastCurrentState = PauseState.Unknown;
                _lastIgnoredReason = "request_exception";
                _logger.Error(
                    "Pause Input Action request failed. "
                    + $"action='{actionPath}' "
                    + $"source='{DefaultSource}' "
                    + $"reason='{reason}'.",
                    exception);
                return;
            }

            _lastHandledFrame = Time.frameCount;
            _lastHandledAction = actionPath;
            _lastIgnoredReason = string.Empty;
            _lastOutcome = MapOutcome(result);
            _lastStatus = result.Status;
            _lastPreviousState = result.PreviousState;
            _lastCurrentState = result.CurrentState;

            if (logPerformedInput)
            {
                _logger.Debug(
                    "Pause Input Action performed. "
                    + $"action='{actionPath}' "
                    + $"request='{result.Request.RequestId.StableText}' "
                    + $"status='{result.Status}' "
                    + $"previousState='{result.PreviousState}' "
                    + $"currentState='{result.CurrentState}' "
                    + $"source='{DefaultSource}' "
                    + $"reason='{reason}'.");
            }
        }

        private void IgnoreConfiguration(string reason, string message)
        {
            _lastIgnoredReason = reason ?? string.Empty;
            _lastOutcome = FlowRequestOutcome.Failed;
            _lastStatus = PauseRequestStatus.Failed;
            if (logIgnoredInput)
            {
                _logger.Warning(
                    "Pause Input Action Adapter not ready. "
                    + $"reason='{_lastIgnoredReason}' "
                    + $"message='{message}' "
                    + $"source='{DefaultSource}'.");
            }
        }

        private void IgnoreInput(string reason, InputAction action)
        {
            _lastIgnoredReason = reason ?? string.Empty;
            if (logIgnoredInput)
            {
                _logger.Trace(
                    "Pause Input Action ignored. "
                    + $"reason='{_lastIgnoredReason}' "
                    + $"action='{FormatAction(action)}' "
                    + $"frame='{Time.frameCount}' "
                    + $"source='{DefaultSource}'.");
            }
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<UnityPauseInputActionAdapter>();
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }

        private static string FormatAction(InputAction action)
        {
            if (action == null)
            {
                return "<none>";
            }

            string actionName = Normalize(action.name, "<unnamed>");
            string actionMapName = action.actionMap != null ? Normalize(action.actionMap.name, string.Empty) : string.Empty;
            return string.IsNullOrWhiteSpace(actionMapName) ? actionName : $"{actionMapName}/{actionName}";
        }

        private static FlowRequestOutcome MapOutcome(PauseResult result)
        {
            if (result.Applied)
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (result.IgnoredNoChange)
            {
                return FlowRequestOutcome.Ignored;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}
