using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Gate;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Opt-in Unity Input System adapter that makes a PlayerInput obey framework Gate snapshots.
    /// It does not read gameplay input, spawn actors, own PlayerInputManager, switch input modes or create Player lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Unity Input/Unity PlayerInput Gate Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "FIRSTGAME-3B opt-in PlayerInput adapter for Pause/Transition Gate blockers.")]
    public sealed class UnityPlayerInputGateAdapter : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityPlayerInputGateAdapter);

        [Header("Target")]
        [Tooltip("Gameplay-owned PlayerInput to gate. If empty, the adapter looks on the same GameObject.")]
        [SerializeField] private PlayerInput playerInput;

        [Tooltip("Action map disabled when Block Mode is Disable Action Map. Recommended for FIRSTGAME: Player.")]
        [SerializeField] private string gameplayActionMapName = "Player";

        [Header("Gate Conditions")]
        [Tooltip("Block when the framework Gate snapshot blocks Input/InputAcceptance.")]
        [SerializeField] private bool blockOnInputAcceptance = true;

        [Tooltip("Block when the framework Gate snapshot blocks Gameplay/GameplayAction.")]
        [SerializeField] private bool blockOnGameplayAction = true;

        [Header("Blocking")]
        [SerializeField] private UnityPlayerInputGateBlockMode blockMode = UnityPlayerInputGateBlockMode.DisableActionMap;

        [Tooltip("When enabled, the adapter restores only the state it changed.")]
        [SerializeField] private bool restorePreviousState = true;

        [Tooltip("Apply once in OnEnable before the first Update tick.")]
        [SerializeField] private bool applyOnEnable = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logMissingRuntimeOnce = true;
        [SerializeField] private bool logMissingTargetOnce = true;

        private FrameworkLogger _logger;
        private bool _isBlockedByAdapter;
        private bool _actionMapWasEnabledBeforeBlock;
        private bool _playerInputWasActiveBeforeBlock;
        private string _lastStatus = "NotApplied";
        private string _lastReason = string.Empty;
        private bool _loggedMissingRuntime;
        private bool _loggedMissingTarget;

        public PlayerInput PlayerInput => ResolvePlayerInput();

        public string GameplayActionMapName => gameplayActionMapName.NormalizeTextOrFallback("Player");

        public bool BlockOnInputAcceptance => blockOnInputAcceptance;

        public bool BlockOnGameplayAction => blockOnGameplayAction;

        public UnityPlayerInputGateBlockMode BlockMode => IsSupportedBlockMode(blockMode) ? blockMode : UnityPlayerInputGateBlockMode.DisableActionMap;

        public bool IsBlockedByAdapter => _isBlockedByAdapter;

        public string LastStatus => _lastStatus.NormalizeText();

        public string LastReason => _lastReason.NormalizeText();

        private void Awake()
        {
            EnsureLogger();
        }

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
            if (string.IsNullOrWhiteSpace(gameplayActionMapName))
            {
                gameplayActionMapName = "Player";
            }
        }

        private void OnEnable()
        {
            EnsureLogger();
            if (applyOnEnable)
            {
                ApplyFromCurrentRuntimeGate("on-enable");
            }
        }

        private void Update()
        {
            ApplyFromCurrentRuntimeGate("update");
        }

        private void OnDisable()
        {
            RestoreIfNeeded("component-disabled");
        }

        private void OnDestroy()
        {
            RestoreIfNeeded("component-destroyed");
        }

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Apply Current Gate")]
        public void ApplyCurrentGate()
        {
            ApplyFromCurrentRuntimeGate("context-menu");
        }

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Restore")]
        public void Restore()
        {
            RestoreIfNeeded("context-menu-restore");
        }

        private void ApplyFromCurrentRuntimeGate(string reason)
        {
            EnsureLogger();

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                if (logMissingRuntimeOnce && !_loggedMissingRuntime)
                {
                    _loggedMissingRuntime = true;
                    _logger.Info(
                        "Unity PlayerInput Gate Adapter skipped because FrameworkRuntimeHost is not available. The adapter will retry on Update.",
                        BuildLogFields("SkippedNoRuntime", reason, false, false, false));
                }

                RestoreIfNeeded("runtime-unavailable");
                return;
            }

            var gateSnapshot = runtimeHost.CurrentGateSnapshot;
            bool blocksInput = blockOnInputAcceptance
                && gateSnapshot.IsBlocked(GateScope.Input, GateDomain.InputAcceptance);
            bool blocksGameplay = blockOnGameplayAction
                && gateSnapshot.IsBlocked(GateScope.Gameplay, GateDomain.GameplayAction);
            bool shouldBlock = blocksInput || blocksGameplay;

            if (shouldBlock)
            {
                ApplyBlock(reason, blocksInput, blocksGameplay);
                return;
            }

            RestoreIfNeeded(reason);
        }

        private void ApplyBlock(string reason, bool blocksInput, bool blocksGameplay)
        {
            var resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                _lastStatus = "SkippedMissingPlayerInput";
                _lastReason = reason.NormalizeText();
                LogMissingTargetOnce("Unity PlayerInput Gate Adapter requires a PlayerInput target.", reason, blocksInput, blocksGameplay);
                return;
            }

            switch (BlockMode)
            {
                case UnityPlayerInputGateBlockMode.DisableActionMap:
                    ApplyActionMapBlock(resolvedPlayerInput, reason, blocksInput, blocksGameplay);
                    break;
                case UnityPlayerInputGateBlockMode.DeactivatePlayerInput:
                    ApplyPlayerInputBlock(resolvedPlayerInput, reason, blocksInput, blocksGameplay);
                    break;
                default:
                    _lastStatus = "SkippedUnsupportedBlockMode";
                    _lastReason = reason.NormalizeText();
                    if (logStateChanges)
                    {
                        _logger.Warning(
                            "Unity PlayerInput Gate Adapter skipped because Block Mode is unsupported.",
                            BuildLogFields(_lastStatus, reason, true, blocksInput, blocksGameplay));
                    }

                    break;
            }
        }

        private void ApplyActionMapBlock(PlayerInput resolvedPlayerInput, string reason, bool blocksInput, bool blocksGameplay)
        {
            InputActionMap actionMap = ResolveGameplayActionMap(resolvedPlayerInput);
            if (actionMap == null)
            {
                _lastStatus = "SkippedMissingActionMap";
                _lastReason = reason.NormalizeText();
                LogMissingTargetOnce(
                    $"Unity PlayerInput Gate Adapter could not resolve action map '{GameplayActionMapName}'.",
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            if (_isBlockedByAdapter)
            {
                _lastStatus = "AlreadyBlocked";
                _lastReason = reason.NormalizeText();
                return;
            }

            _actionMapWasEnabledBeforeBlock = actionMap.enabled;
            _playerInputWasActiveBeforeBlock = resolvedPlayerInput.enabled;

            if (actionMap.enabled)
            {
                actionMap.Disable();
            }

            _isBlockedByAdapter = true;
            _lastStatus = "BlockedActionMap";
            _lastReason = reason.NormalizeText();
            LogStateChange("Unity PlayerInput Gate Adapter blocked gameplay action map.", _lastStatus, reason, true, blocksInput, blocksGameplay);
        }

        private void ApplyPlayerInputBlock(PlayerInput resolvedPlayerInput, string reason, bool blocksInput, bool blocksGameplay)
        {
            if (_isBlockedByAdapter)
            {
                _lastStatus = "AlreadyBlocked";
                _lastReason = reason.NormalizeText();
                return;
            }

            _playerInputWasActiveBeforeBlock = resolvedPlayerInput.enabled;
            _actionMapWasEnabledBeforeBlock = ResolveGameplayActionMap(resolvedPlayerInput)?.enabled ?? false;

            if (resolvedPlayerInput.enabled)
            {
                resolvedPlayerInput.DeactivateInput();
            }

            _isBlockedByAdapter = true;
            _lastStatus = "BlockedPlayerInput";
            _lastReason = reason.NormalizeText();
            LogStateChange("Unity PlayerInput Gate Adapter deactivated PlayerInput.", _lastStatus, reason, true, blocksInput, blocksGameplay);
        }

        private void RestoreIfNeeded(string reason)
        {
            if (!_isBlockedByAdapter)
            {
                _lastStatus = "Allowed";
                _lastReason = reason.NormalizeText();
                return;
            }

            var resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                _isBlockedByAdapter = false;
                _lastStatus = "ReleasedMissingPlayerInput";
                _lastReason = reason.NormalizeText();
                return;
            }

            if (BlockMode == UnityPlayerInputGateBlockMode.DisableActionMap)
            {
                var actionMap = ResolveGameplayActionMap(resolvedPlayerInput);
                if (actionMap != null && restorePreviousState && _actionMapWasEnabledBeforeBlock && !actionMap.enabled)
                {
                    actionMap.Enable();
                }
            }
            else if (BlockMode == UnityPlayerInputGateBlockMode.DeactivatePlayerInput)
            {
                if (restorePreviousState && _playerInputWasActiveBeforeBlock)
                {
                    resolvedPlayerInput.ActivateInput();
                }
            }

            _isBlockedByAdapter = false;
            _lastStatus = "Released";
            _lastReason = reason.NormalizeText();
            LogStateChange("Unity PlayerInput Gate Adapter released gameplay input.", _lastStatus, reason, false, false, false);
        }

        private PlayerInput ResolvePlayerInput()
        {
            return playerInput != null ? playerInput : GetComponent<PlayerInput>();
        }

        private InputActionMap ResolveGameplayActionMap(PlayerInput resolvedPlayerInput)
        {
            if (resolvedPlayerInput == null || resolvedPlayerInput.actions == null)
            {
                return null;
            }

            return resolvedPlayerInput.actions.FindActionMap(GameplayActionMapName, throwIfNotFound: false);
        }

        private void LogMissingTargetOnce(string message, string reason, bool blocksInput, bool blocksGameplay)
        {
            if (!logMissingTargetOnce || _loggedMissingTarget)
            {
                return;
            }

            _loggedMissingTarget = true;
            _logger.Warning(
                message,
                BuildLogFields(_lastStatus, reason, true, blocksInput, blocksGameplay));
        }

        private void LogStateChange(string message, string status, string reason, bool blocked, bool blocksInput, bool blocksGameplay)
        {
            if (!logStateChanges)
            {
                return;
            }

            _logger.Info(
                message,
                BuildLogFields(status, reason, blocked, blocksInput, blocksGameplay));
        }

        private LogField[] BuildLogFields(string status, string reason, bool blocked, bool blocksInput, bool blocksGameplay)
        {
            var resolvedPlayerInput = ResolvePlayerInput();
            var actionMap = ResolveGameplayActionMap(resolvedPlayerInput);

            return LogFields.Of(
                LogFields.Field("status", status.NormalizeTextOrFallback("Unknown")),
                LogFields.Field("blockedByAdapter", blocked),
                LogFields.Field("blockMode", BlockMode.ToString()),
                LogFields.Field("blockOnInputAcceptance", blockOnInputAcceptance),
                LogFields.Field("blockOnGameplayAction", blockOnGameplayAction),
                LogFields.Field("blocksInputAcceptance", blocksInput),
                LogFields.Field("blocksGameplayAction", blocksGameplay),
                LogFields.Field("playerInput", resolvedPlayerInput != null ? resolvedPlayerInput.name : "<none>"),
                LogFields.Field("actionMap", GameplayActionMapName),
                LogFields.Field("actionMapEnabled", actionMap != null && actionMap.enabled),
                LogFields.Field("restorePreviousState", restorePreviousState),
                LogFields.Field("source", DefaultSource),
                LogFields.Field("reason", reason.NormalizeTextOrFallback("gate-adapter")));
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<UnityPlayerInputGateAdapter>();
        }

        private static bool IsSupportedBlockMode(UnityPlayerInputGateBlockMode mode)
        {
            return mode is UnityPlayerInputGateBlockMode.DisableActionMap
                or UnityPlayerInputGateBlockMode.DeactivatePlayerInput;
        }
    }
}
