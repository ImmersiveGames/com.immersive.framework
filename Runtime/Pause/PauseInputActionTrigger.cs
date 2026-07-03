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
    /// API status: Experimental. Minimal Unity InputAction trigger for Pause requests.
    /// This component listens to one explicit global Pause action and submits PauseRequest directly to the current FrameworkRuntimeHost.
    /// It can optionally switch a supplied gameplay PlayerInput between gameplay and pause UI action maps.
    /// It does not validate InputMode, own PlayerInputManager, call JoinPlayer, spawn actors or read gameplay commands.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Input Action Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "FIRSTGAME-2D direct Global/Pause InputAction trigger for minimal Pause keyboard flow.")]
    public sealed class PauseInputActionTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseInputActionTrigger);
        private const string DefaultReasonPrefix = "pause.input.action";

        private FrameworkLogger _logger;
        private InputAction _pauseAction;
        private bool _subscribed;
        private bool _pauseActionEnabledByThis;
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

        [Header("Pause Action")]
        [Tooltip("Canonical action map for global/system commands. Recommended: Global.")]
        [SerializeField] private string pauseActionMapName = "Global";
        [Tooltip("Canonical Pause action name inside the global/system action map. Recommended: Pause.")]
        [SerializeField] private string pauseActionName = "Pause";
        [Header("Runtime")]
        [SerializeField] private bool subscribeOnEnable = true;
        [SerializeField] private bool enableResolvedActionsOnEnable = true;
        [SerializeField] private PauseRequestKind requestKind = PauseRequestKind.Toggle;
        [SerializeField] private string reason = "pause.input.action.trigger";

        [Header("Optional Gameplay Action Map Switching")]
        [Tooltip("Optional. Use only when the referenced PlayerInput is gameplay-owned and the EventSystem/UI input lane is independent. It is disabled by default to avoid disabling UI buttons after Resume.")]
        [SerializeField] private bool switchPlayerInputActionMap;
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string pauseUiActionMapName = "UI";

        [Header("Diagnostics")]
        [SerializeField] private bool logReadyOnEnable = true;
        [SerializeField] private bool logPerformedInput = true;
        [SerializeField] private bool logIgnoredInput = true;
        [SerializeField] private bool logActionMapSwitching = true;

        public PlayerInput PlayerInput => playerInput;

        public InputActionAsset ActionsAsset => actionsAsset;

        public string PauseActionMapName => pauseActionMapName.NormalizeTextOrFallback("Global");

        public string PauseActionName => pauseActionName.NormalizeTextOrFallback("Pause");

        public bool IsSubscribed => _subscribed;

        public bool SwitchPlayerInputActionMap => switchPlayerInputActionMap;

        public string GameplayActionMapName => gameplayActionMapName.NormalizeTextOrFallback("Player");

        public string PauseUiActionMapName => pauseUiActionMapName.NormalizeTextOrFallback("UI");

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

            _pauseAction = ResolveAction(asset, PauseActionMapName, PauseActionName, true);
            if (_pauseAction == null)
            {
                IgnoreConfiguration("pause_action_missing", "Pause Input Action Trigger did not resolve the configured global Pause action.");
                return;
            }

            Subscribe(_pauseAction, ref _pauseActionEnabledByThis);
            _subscribed = true;

            if (logReadyOnEnable)
            {
                _logger.Info(
                    "Pause Input Action Trigger ready.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("asset", asset.name),
                        Immersive.Logging.Records.LogFields.Field("pauseAction", FormatAction(_pauseAction)),
                        Immersive.Logging.Records.LogFields.Field("pauseActionMap", PauseActionMapName),
                        Immersive.Logging.Records.LogFields.Field("requestKind", requestKind.ToString()),
                        Immersive.Logging.Records.LogFields.Field("enabledOnSubscribe", enableResolvedActionsOnEnable),
                        Immersive.Logging.Records.LogFields.Field("actionMapSwitching", switchPlayerInputActionMap),
                        Immersive.Logging.Records.LogFields.Field("gameplayActionMap", GameplayActionMapName),
                        Immersive.Logging.Records.LogFields.Field("pauseUiActionMap", PauseUiActionMapName),
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
            Unsubscribe(_pauseAction, _pauseActionEnabledByThis);
            _pauseAction = null;
            _pauseActionEnabledByThis = false;
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

            string actionMapSwitchStatus = ApplyPlayerInputActionMapForPauseResult(result, out string selectedActionMap);

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
                        Immersive.Logging.Records.LogFields.Field("actionMapSwitching", switchPlayerInputActionMap),
                        Immersive.Logging.Records.LogFields.Field("actionMapSwitchStatus", actionMapSwitchStatus),
                        Immersive.Logging.Records.LogFields.Field("selectedActionMap", selectedActionMap.NormalizeTextOrFallback("<none>")),
                        Immersive.Logging.Records.LogFields.Field("source", source.NormalizeTextOrFallback(DefaultSource)),
                        Immersive.Logging.Records.LogFields.Field("reason", requestReason.NormalizeTextOrFallback(DefaultReasonPrefix))));
            }
        }

        private string ApplyPlayerInputActionMapForPauseResult(PauseResult result, out string selectedActionMap)
        {
            selectedActionMap = GetCurrentPlayerInputActionMapName();

            if (!switchPlayerInputActionMap)
            {
                return "Disabled";
            }

            if (playerInput == null)
            {
                return "SkippedNoPlayerInput";
            }

            if (!result.Completed)
            {
                return "SkippedRequestNotCompleted";
            }

            string targetActionMapName = result.CurrentState == PauseState.Paused
                ? PauseUiActionMapName
                : GameplayActionMapName;

            if (string.IsNullOrWhiteSpace(targetActionMapName))
            {
                return "SkippedMissingTargetActionMap";
            }

            if (playerInput.actions == null)
            {
                return "FailedNoPlayerInputActions";
            }

            InputActionMap targetActionMap = playerInput.actions.FindActionMap(targetActionMapName, false);
            if (targetActionMap == null)
            {
                return "FailedTargetActionMapMissing";
            }

            string currentActionMapName = GetCurrentPlayerInputActionMapName();
            if (string.Equals(currentActionMapName, targetActionMapName, StringComparison.Ordinal))
            {
                selectedActionMap = currentActionMapName;
                return "AlreadySelected";
            }

            try
            {
                playerInput.SwitchCurrentActionMap(targetActionMapName);
                selectedActionMap = GetCurrentPlayerInputActionMapName();

                if (logActionMapSwitching)
                {
                    _logger.Info(
                        "Pause Input Action Trigger switched PlayerInput action map.",
                        Immersive.Logging.Records.LogFields.Of(
                            Immersive.Logging.Records.LogFields.Field("previousActionMap", currentActionMapName.NormalizeTextOrFallback("<none>")),
                            Immersive.Logging.Records.LogFields.Field("targetActionMap", targetActionMapName),
                            Immersive.Logging.Records.LogFields.Field("selectedActionMap", selectedActionMap.NormalizeTextOrFallback("<none>")),
                            Immersive.Logging.Records.LogFields.Field("pauseState", result.CurrentState.ToString()),
                            Immersive.Logging.Records.LogFields.Field("source", DefaultSource)));
                }

                return string.Equals(selectedActionMap, targetActionMapName, StringComparison.Ordinal)
                    ? "Selected"
                    : "SelectedDifferentMap";
            }
            catch (Exception exception)
            {
                selectedActionMap = GetCurrentPlayerInputActionMapName();
                _logger.Warning(
                    "Pause Input Action Trigger failed to switch PlayerInput action map.",
                    Immersive.Logging.Records.LogFields.Of(
                        Immersive.Logging.Records.LogFields.Field("targetActionMap", targetActionMapName),
                        Immersive.Logging.Records.LogFields.Field("selectedActionMap", selectedActionMap.NormalizeTextOrFallback("<none>")),
                        Immersive.Logging.Records.LogFields.Field("exception", exception.Message),
                        Immersive.Logging.Records.LogFields.Field("source", DefaultSource)));
                return "FailedException";
            }
        }

        private string GetCurrentPlayerInputActionMapName()
        {
            return playerInput == null || playerInput.currentActionMap == null
                ? string.Empty
                : playerInput.currentActionMap.name.NormalizeText();
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
