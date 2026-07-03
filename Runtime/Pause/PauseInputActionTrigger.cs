using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Minimal Unity InputAction trigger for logical Pause requests.
    /// This component subscribes to explicit InputAction callbacks and submits PauseRequest directly to the current FrameworkRuntimeHost.
    /// It does not switch action maps, validate InputMode, own PlayerInputManager, call JoinPlayer, spawn actors or read gameplay commands.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Input Action Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "FIRSTGAME-2B direct Pause InputAction trigger for minimal Pause keyboard flow.")]
    public sealed class PauseInputActionTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseInputActionTrigger);
        private const string DefaultReasonPrefix = "pause.input.action";

        private FrameworkLogger _logger;
        private InputAction _playerAction;
        private InputAction _uiAction;
        private bool _subscribed;
        private bool _playerActionEnabledByThis;
        private bool _uiActionEnabledByThis;
        private int _lastHandledFrame = -1;
        private string _lastHandledAction = string.Empty;
        private string _lastIgnoredReason = string.Empty;
        private PauseRequestStatus _lastStatus = PauseRequestStatus.Unknown;
        private PauseState _lastPreviousState = PauseState.Unknown;
        private PauseState _lastCurrentState = PauseState.Unknown;

        [Header("Input Source")]
        [Tooltip("Optional PlayerInput evidence. If Actions Asset is empty, this component uses PlayerInput.actions.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Optional explicit InputActionAsset. When filled, it takes precedence over PlayerInput.actions.")]
        [SerializeField] private InputActionAsset actionsAsset;

        [Header("Action Names")]
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";
        [SerializeField] private string pauseActionName = "Pause";
        [SerializeField] private bool requirePlayerAction = true;
        [SerializeField] private bool requireUiAction = true;

        [Header("Runtime")]
        [SerializeField] private bool subscribeOnEnable = true;
        [SerializeField] private bool enableResolvedActionsOnEnable = true;
        [SerializeField] private PauseRequestKind requestKind = PauseRequestKind.Toggle;
        [SerializeField] private string reason = "pause.input.action.trigger";
        [SerializeField] private bool logReadyOnEnable = true;
        [SerializeField] private bool logPerformedInput = true;
        [SerializeField] private bool logIgnoredInput = true;

        public PlayerInput PlayerInput => playerInput;

        public InputActionAsset ActionsAsset => actionsAsset;

        public string PlayerActionMapName => playerActionMapName.NormalizeTextOrFallback("Player");

        public string UiActionMapName => uiActionMapName.NormalizeTextOrFallback("UI");

        public string PauseActionName => pauseActionName.NormalizeTextOrFallback("Pause");

        public bool IsSubscribed => _subscribed;

        public string LastHandledAction => _lastHandledAction.NormalizeText();

        public string LastIgnoredReason => _lastIgnoredReason.NormalizeText();

        public PauseRequestStatus LastStatus => _lastStatus;

        public PauseState LastPreviousState => _lastPreviousState;

        public PauseState LastCurrentState => _lastCurrentState;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseInputActionTrigger>();
        }

        private void OnEnable()
        {
            if (subscribeOnEnable)
            {
                SubscribeResolvedActions();
            }
        }

        private void OnDisable()
        {
            UnsubscribeResolvedActions();
        }

        private void OnDestroy()
        {
            UnsubscribeResolvedActions();
        }

        [ContextMenu("Immersive Framework/Pause/Input Action Trigger/Rebind")]
        public void Rebind()
        {
            SubscribeResolvedActions();
        }

        [ContextMenu("Immersive Framework/Pause/Input Action Trigger/Submit Request")]
        public void SubmitRequest()
        {
            SubmitPauseRequest(DefaultSource, ResolveReason("pause.input.action.trigger.context-menu"), "context-menu");
        }

        private void SubscribeResolvedActions()
        {
            EnsureLogger();
            UnsubscribeResolvedActions();

            InputActionAsset asset = ResolveActionsAsset();
            if (asset == null)
            {
                IgnoreConfiguration("actions_asset_missing", "Pause Input Action Trigger requires an InputActionAsset or PlayerInput.actions.");
                return;
            }

            string actionName = PauseActionName;
            _playerAction = ResolveAction(asset, PlayerActionMapName, actionName, requirePlayerAction);
            _uiAction = ResolveAction(asset, UiActionMapName, actionName, requireUiAction);

            if (_playerAction == null && _uiAction == null)
            {
                IgnoreConfiguration("pause_actions_missing", "Pause Input Action Trigger did not resolve any Pause action.");
                return;
            }

            Subscribe(_playerAction, ref _playerActionEnabledByThis);
            Subscribe(_uiAction, ref _uiActionEnabledByThis);
            _subscribed = true;

            if (logReadyOnEnable)
            {
                _logger.Info(
                    "Pause Input Action Trigger ready.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("asset", asset.name),
                        Immersive.Logging.Records.LogFields.Field("playerAction", FormatAction(_playerAction)),
                        Immersive.Logging.Records.LogFields.Field("uiAction", FormatAction(_uiAction)),
                        Immersive.Logging.Records.LogFields.Field("requestKind", requestKind.ToString()),
                        Immersive.Logging.Records.LogFields.Field("enabledOnSubscribe", enableResolvedActionsOnEnable),
                        Immersive.Logging.Records.LogFields.Field("source", DefaultSource),
                        Immersive.Logging.Records.LogFields.Field("reason", ResolveReason("pause.input.action.trigger.ready"))));
            }
        }

        private void Subscribe(InputAction action, ref bool enabledByThis)
        {
            enabledByThis = false;
            if (action == null)
            {
                return;
            }

            action.performed -= OnPauseActionPerformed;
            action.performed += OnPauseActionPerformed;

            if (enableResolvedActionsOnEnable && !action.enabled)
            {
                action.Enable();
                enabledByThis = true;
            }
        }

        private void UnsubscribeResolvedActions()
        {
            Unsubscribe(_playerAction, _playerActionEnabledByThis);
            Unsubscribe(_uiAction, _uiActionEnabledByThis);
            _playerAction = null;
            _uiAction = null;
            _playerActionEnabledByThis = false;
            _uiActionEnabledByThis = false;
            _subscribed = false;
        }

        private void Unsubscribe(InputAction action, bool enabledByThis)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= OnPauseActionPerformed;
            if (enabledByThis && action.enabled)
            {
                action.Disable();
            }
        }

        private void OnPauseActionPerformed(InputAction.CallbackContext context)
        {
            EnsureLogger();

            if (!_subscribed)
            {
                IgnoreInput("trigger_not_subscribed", context.action);
                return;
            }

            if (_lastHandledFrame == Time.frameCount)
            {
                IgnoreInput("dedupe_same_frame", context.action);
                return;
            }

            _lastHandledFrame = Time.frameCount;
            string actionPath = FormatAction(context.action);
            SubmitPauseRequest(DefaultSource, ResolveReason($"{DefaultReasonPrefix}:{actionPath}"), actionPath);
        }

        private void SubmitPauseRequest(string source, string requestReason, string actionPath)
        {
            EnsureLogger();
            if (!Enum.IsDefined(typeof(PauseRequestKind), requestKind) || requestKind == PauseRequestKind.Unknown)
            {
                _lastIgnoredReason = "invalid_request_kind";
                _lastStatus = PauseRequestStatus.Failed;
                _lastPreviousState = PauseState.Unknown;
                _lastCurrentState = PauseState.Unknown;
                if (logIgnoredInput)
                {
                    _logger.Warning(
                        "Pause Input Action Trigger ignored input.",
                        Immersive.Logging.Records.LogFields.Of(
                            Immersive.Logging.Records.LogFields.Field("reason", _lastIgnoredReason),
                            Immersive.Logging.Records.LogFields.Field("requestKind", requestKind.ToString()),
                            Immersive.Logging.Records.LogFields.Field("action", actionPath.NormalizeTextOrFallback("<none>")),
                            Immersive.Logging.Records.LogFields.Field("source", source.NormalizeTextOrFallback(DefaultSource))));
                }

                return;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                _lastIgnoredReason = "runtime_host_unavailable";
                _lastStatus = PauseRequestStatus.Failed;
                _lastPreviousState = PauseState.Unknown;
                _lastCurrentState = PauseState.Unknown;
                if (logIgnoredInput)
                {
                    _logger.Info(
                        "Pause Input Action Trigger ignored input.",
                        Immersive.Logging.Records.LogFields.Of(
                            Immersive.Logging.Records.LogFields.Field("reason", _lastIgnoredReason),
                            Immersive.Logging.Records.LogFields.Field("action", actionPath.NormalizeTextOrFallback("<none>")),
                            Immersive.Logging.Records.LogFields.Field("source", source.NormalizeTextOrFallback(DefaultSource))));
                }

                return;
            }

            PauseResult result;
            try
            {
                result = runtimeHost.RequestPause(requestKind, source.NormalizeTextOrFallback(DefaultSource), requestReason.NormalizeTextOrFallback(DefaultReasonPrefix));
            }
            catch (Exception exception)
            {
                _lastIgnoredReason = "request_exception";
                _lastStatus = PauseRequestStatus.Failed;
                _lastPreviousState = PauseState.Unknown;
                _lastCurrentState = PauseState.Unknown;
                _logger.Error(
                    "Pause Input Action Trigger request failed.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("action", actionPath.NormalizeTextOrFallback("<none>")),
                        Immersive.Logging.Records.LogFields.Field("source", source.NormalizeTextOrFallback(DefaultSource)),
                        Immersive.Logging.Records.LogFields.Field("reason", requestReason.NormalizeTextOrFallback(DefaultReasonPrefix)),
                        Immersive.Logging.Records.LogFields.Field("exception", exception.Message)));
                return;
            }

            _lastHandledAction = actionPath.NormalizeText();
            _lastIgnoredReason = string.Empty;
            _lastStatus = result.Status;
            _lastPreviousState = result.PreviousState;
            _lastCurrentState = result.CurrentState;

            if (logPerformedInput)
            {
                _logger.Info(
                    "Pause Input Action Trigger completed.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("action", actionPath.NormalizeTextOrFallback("<none>")),
                        Immersive.Logging.Records.LogFields.Field("request", result.Request.RequestId.StableText),
                        Immersive.Logging.Records.LogFields.Field("requestKind", requestKind.ToString()),
                        Immersive.Logging.Records.LogFields.Field("status", result.Status.ToString()),
                        Immersive.Logging.Records.LogFields.Field("previousState", result.PreviousState.ToString()),
                        Immersive.Logging.Records.LogFields.Field("currentState", result.CurrentState.ToString()),
                        Immersive.Logging.Records.LogFields.Field("applied", result.Applied),
                        Immersive.Logging.Records.LogFields.Field("ignoredNoChange", result.IgnoredNoChange),
                        Immersive.Logging.Records.LogFields.Field("stateChanged", result.StateChanged),
                        Immersive.Logging.Records.LogFields.Field("source", source.NormalizeTextOrFallback(DefaultSource)),
                        Immersive.Logging.Records.LogFields.Field("reason", requestReason.NormalizeTextOrFallback(DefaultReasonPrefix))));
            }
        }

        private InputActionAsset ResolveActionsAsset()
        {
            if (actionsAsset != null)
            {
                return actionsAsset;
            }

            return playerInput == null ? null : playerInput.actions;
        }

        private InputAction ResolveAction(InputActionAsset asset, string mapName, string actionName, bool required)
        {
            string resolvedMapName = mapName.NormalizeText();
            string resolvedActionName = actionName.NormalizeTextOrFallback("Pause");
            if (string.IsNullOrWhiteSpace(resolvedMapName))
            {
                if (required)
                {
                    IgnoreConfiguration("action_map_name_missing", $"Pause Input Action Trigger requires an action map name for action '{resolvedActionName}'.");
                }

                return null;
            }

            InputActionMap actionMap = asset.FindActionMap(resolvedMapName, false);
            if (actionMap == null)
            {
                if (required)
                {
                    IgnoreConfiguration("action_map_missing", $"Pause Input Action Trigger could not find action map '{resolvedMapName}' in asset '{asset.name}'.");
                }

                return null;
            }

            InputAction action = actionMap.FindAction(resolvedActionName, false);
            if (action == null)
            {
                if (required)
                {
                    IgnoreConfiguration("action_missing", $"Pause Input Action Trigger could not find action '{resolvedActionName}' in action map '{resolvedMapName}' asset '{asset.name}'.");
                }

                return null;
            }

            return action;
        }

        private void IgnoreConfiguration(string reasonCode, string message)
        {
            _lastIgnoredReason = reasonCode.NormalizeText();
            _lastStatus = PauseRequestStatus.Failed;
            _lastPreviousState = PauseState.Unknown;
            _lastCurrentState = PauseState.Unknown;
            if (logIgnoredInput)
            {
                EnsureLogger();
                _logger.Warning(
                    "Pause Input Action Trigger not ready.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("reason", _lastIgnoredReason),
                        Immersive.Logging.Records.LogFields.Field("message", message.NormalizeText()),
                        Immersive.Logging.Records.LogFields.Field("source", DefaultSource)));
            }
        }

        private void IgnoreInput(string reasonCode, InputAction action)
        {
            _lastIgnoredReason = reasonCode.NormalizeText();
            if (logIgnoredInput)
            {
                _logger.Info(
                    "Pause Input Action Trigger ignored input.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("reason", _lastIgnoredReason),
                        Immersive.Logging.Records.LogFields.Field("action", FormatAction(action)),
                        Immersive.Logging.Records.LogFields.Field("frame", Time.frameCount),
                        Immersive.Logging.Records.LogFields.Field("source", DefaultSource)));
            }
        }

        private string ResolveReason(string fallback)
        {
            return reason.NormalizeTextOrFallback(fallback);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseInputActionTrigger>();
            }
        }

        private static string FormatAction(InputAction action)
        {
            if (action == null)
            {
                return "<none>";
            }

            string actionName = action.name.NormalizeTextOrFallback("<unnamed>");
            string mapName = action.actionMap == null ? string.Empty : action.actionMap.name.NormalizeText();
            return string.IsNullOrWhiteSpace(mapName) ? actionName : $"{mapName}/{actionName}";
        }
    }
}
