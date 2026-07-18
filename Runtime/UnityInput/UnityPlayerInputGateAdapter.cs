using System.Collections.Generic;
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
    /// Opt-in Gate intent adapter. It evaluates framework Gate snapshots and delegates every
    /// physical PlayerInput/InputActionMap mutation to UnityPlayerInputStateWriter.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Unity Input/Unity PlayerInput Gate Adapter")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC1 Gate intent adapter using the canonical Unity PlayerInput physical writer.")]
    public sealed class UnityPlayerInputGateAdapter : MonoBehaviour
    {
        private const string DefaultSource = nameof(UnityPlayerInputGateAdapter);

        [Header("Target")]
        [Tooltip("Gameplay-owned PlayerInput to gate. If empty, the adapter looks on the same GameObject.")]
        [SerializeField] private PlayerInput playerInput;

        [Tooltip("Action map blocked when Block Mode is Disable Action Map.")]
        [SerializeField] private string gameplayActionMapName = "Player";

        [Header("Gate Conditions")]
        [SerializeField] private bool blockOnInputAcceptance = true;
        [SerializeField] private bool blockOnGameplayAction = true;

        [Header("Blocking")]

        [Tooltip("When enabled, the adapter asks the canonical writer to restore only state changed by this Gate block.")]
        [SerializeField] private bool restorePreviousState = true;
        [SerializeField] private bool applyOnEnable = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logMissingRuntimeOnce = true;
        [SerializeField] private bool logMissingTargetOnce = true;

        private FrameworkLogger _logger;
        private bool _isBlockedByAdapter;
        private bool _actionMapWasEnabledBeforeBlock;
        private string _lastStatus = "NotApplied";
        private string _lastReason = string.Empty;
        private bool _loggedMissingRuntime;
        private bool _loggedMissingTarget;

        public PlayerInput PlayerInput => ResolvePlayerInput();
        public string GameplayActionMapName =>
            gameplayActionMapName.NormalizeTextOrFallback("Player");
        public bool BlockOnInputAcceptance => blockOnInputAcceptance;
        public bool BlockOnGameplayAction => blockOnGameplayAction;
        public bool IsBlockedByAdapter => _isBlockedByAdapter;
        public string LastStatus => _lastStatus.NormalizeText();
        public string LastReason => _lastReason.NormalizeText();

        private void Awake() => EnsureLogger();

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

        private void Update() => ApplyFromCurrentRuntimeGate("update");

        private void OnDisable() => RestoreIfNeeded("component-disabled");
        private void OnDestroy() => RestoreIfNeeded("component-destroyed");

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Apply Current Gate")]
        public void ApplyCurrentGate() =>
            ApplyFromCurrentRuntimeGate("context-menu");

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Restore")]
        public void Restore() => RestoreIfNeeded("context-menu-restore");

        internal bool TrySelectActionMap(
            string actionMapName,
            string source,
            string reason,
            out UnityPlayerInputActionMapWriteReceipt receipt,
            out string issue)
        {
            receipt = default;
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                issue = "PlayerInput write authority requires an explicit PlayerInput target.";
                return false;
            }

            if (!UnityPlayerInputStateWriter.TrySelectActionMap(
                    resolvedPlayerInput,
                    actionMapName,
                    out receipt,
                    out issue))
            {
                return false;
            }

            if (_isBlockedByAdapter)
            {
                bool selectedGameplayMap = string.Equals(
                    actionMapName.NormalizeText(),
                    GameplayActionMapName,
                    System.StringComparison.Ordinal);
                if (!selectedGameplayMap)
                {
                    // Another explicit posture superseded the gameplay map while the Gate was
                    // blocked. Releasing the Gate must not resurrect the older gameplay map.
                    _actionMapWasEnabledBeforeBlock = false;
                }
                else
                {
                    // The new explicit baseline wants gameplay enabled after the Gate releases.
                    _actionMapWasEnabledBeforeBlock = true;
                    if (!UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                            resolvedPlayerInput,
                            GameplayActionMapName,
                            false,
                            out _,
                            out _,
                            out issue))
                    {
                        return false;
                    }
                }
            }

            _lastStatus = "ActionMapSelectedByAuthority";
            _lastReason = reason.NormalizeTextOrFallback("action-map-selection");
            return true;
        }

        internal bool TryRestoreActionMap(
            UnityPlayerInputActionMapWriteReceipt receipt,
            string source,
            string reason,
            out string issue)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (!UnityPlayerInputStateWriter.TryRestoreActionMap(
                    resolvedPlayerInput,
                    receipt,
                    out issue))
            {
                return false;
            }

            if (_isBlockedByAdapter)
            {
                bool restoredGameplayMap = string.Equals(
                    UnityPlayerInputStateWriter.CurrentActionMapName(
                        resolvedPlayerInput),
                    GameplayActionMapName,
                    System.StringComparison.Ordinal);
                _actionMapWasEnabledBeforeBlock = restoredGameplayMap;
                if (restoredGameplayMap &&
                    !UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                        resolvedPlayerInput,
                        GameplayActionMapName,
                        false,
                        out _,
                        out _,
                        out issue))
                {
                    return false;
                }
            }

            _lastStatus = "ActionMapRestoredByAuthority";
            _lastReason = reason.NormalizeTextOrFallback("action-map-restore");
            return true;
        }

        internal bool TryApplyActionMapSet(
            string primaryActionMapName,
            IReadOnlyList<string> enabledActionMapNames,
            string source,
            string reason,
            out UnityPlayerInputActionMapSetWriteReceipt receipt,
            out string issue)
        {
            receipt = default;
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                issue =
                    "PlayerInput write authority requires an explicit PlayerInput target.";
                return false;
            }

            if (!UnityPlayerInputStateWriter.TryApplyActionMapSet(
                    resolvedPlayerInput,
                    primaryActionMapName,
                    enabledActionMapNames,
                    out receipt,
                    out issue))
            {
                return false;
            }

            _actionMapWasEnabledBeforeBlock = ContainsActionMap(
                enabledActionMapNames,
                GameplayActionMapName);
            ApplyFromCurrentRuntimeGate("action-map-set-applied");
            _lastStatus = "ActionMapSetAppliedByAuthority";
            _lastReason = reason.NormalizeTextOrFallback(
                "action-map-set-application");
            return true;
        }

        internal bool TryRestoreActionMapSet(
            UnityPlayerInputActionMapSetWriteReceipt receipt,
            string source,
            string reason,
            out string issue)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (!UnityPlayerInputStateWriter.TryRestoreActionMapSet(
                    resolvedPlayerInput,
                    receipt,
                    out issue))
            {
                return false;
            }

            _actionMapWasEnabledBeforeBlock =
                ResolveGameplayActionMap(resolvedPlayerInput)?.enabled ?? false;
            ApplyFromCurrentRuntimeGate("action-map-set-restored");
            _lastStatus = "ActionMapSetRestoredByAuthority";
            _lastReason = reason.NormalizeTextOrFallback(
                "action-map-set-restore");
            return true;
        }

        private void ApplyFromCurrentRuntimeGate(string reason)
        {
            EnsureLogger();

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                if (logMissingRuntimeOnce && !_loggedMissingRuntime)
                {
                    _loggedMissingRuntime = true;
                    _logger.Trace(
                        "Unity PlayerInput Gate Adapter skipped because FrameworkRuntimeHost is not available. The adapter will retry on Update.",
                        BuildLogFields(
                            "SkippedNoRuntime",
                            reason,
                            false,
                            false,
                            false));
                }

                RestoreIfNeeded("runtime-unavailable");
                return;
            }

            var gateSnapshot = runtimeHost.CurrentGateSnapshot;
            bool blocksInput = blockOnInputAcceptance &&
                gateSnapshot.IsBlocked(
                    GateScope.Input,
                    GateDomain.InputAcceptance);
            bool blocksGameplay = blockOnGameplayAction &&
                gateSnapshot.IsBlocked(
                    GateScope.Gameplay,
                    GateDomain.GameplayAction);

            if (blocksInput || blocksGameplay)
            {
                ApplyBlock(reason, blocksInput, blocksGameplay);
                return;
            }

            RestoreIfNeeded(reason);
        }

        private void ApplyBlock(
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                _lastStatus = "SkippedMissingPlayerInput";
                _lastReason = reason.NormalizeText();
                LogMissingTargetOnce(
                    "Unity PlayerInput Gate Adapter requires a PlayerInput target.",
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            ApplyActionMapBlock(
                resolvedPlayerInput,
                reason,
                blocksInput,
                blocksGameplay);
        }

        private void ApplyActionMapBlock(
            PlayerInput resolvedPlayerInput,
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            if (_isBlockedByAdapter)
            {
                _lastStatus = "AlreadyBlocked";
                _lastReason = reason.NormalizeText();
                return;
            }

            if (!UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                    resolvedPlayerInput,
                    GameplayActionMapName,
                    false,
                    out bool previousEnabled,
                    out _,
                    out string issue))
            {
                _lastStatus = "FailedActionMapBlock";
                _lastReason = reason.NormalizeText();
                LogWriteFailure(issue, reason, blocksInput, blocksGameplay);
                return;
            }

            _actionMapWasEnabledBeforeBlock = previousEnabled;
            _isBlockedByAdapter = true;
            _lastStatus = "BlockedActionMap";
            _lastReason = reason.NormalizeText();
            LogStateChange(
                "Unity PlayerInput Gate Adapter requested gameplay action-map blocking.",
                _lastStatus,
                reason,
                true,
                blocksInput,
                blocksGameplay);
        }

        private void RestoreIfNeeded(string reason)
        {
            if (!_isBlockedByAdapter)
            {
                _lastStatus = "Allowed";
                _lastReason = reason.NormalizeText();
                return;
            }

            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                _isBlockedByAdapter = false;
                _lastStatus = "ReleasedMissingPlayerInput";
                _lastReason = reason.NormalizeText();
                return;
            }

            bool restored = true;
            string issue = string.Empty;
            if (restorePreviousState && _actionMapWasEnabledBeforeBlock)
            {
                restored = UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                    resolvedPlayerInput,
                    GameplayActionMapName,
                    true,
                    out _,
                    out _,
                    out issue);
            }

            if (!restored)
            {
                _lastStatus = "ReleaseFailed";
                _lastReason = reason.NormalizeText();
                LogWriteFailure(issue, reason, false, false);
                return;
            }

            _isBlockedByAdapter = false;
            _actionMapWasEnabledBeforeBlock = false;
            _lastStatus = "Released";
            _lastReason = reason.NormalizeText();
            LogStateChange(
                "Unity PlayerInput Gate Adapter released gameplay input through the canonical writer.",
                _lastStatus,
                reason,
                false,
                false,
                false);
        }

        private static bool ContainsActionMap(
            IReadOnlyList<string> actionMapNames,
            string expectedActionMapName)
        {
            if (actionMapNames == null)
            {
                return false;
            }

            string expected = expectedActionMapName.NormalizeText();
            for (int index = 0; index < actionMapNames.Count; index++)
            {
                if (string.Equals(
                        actionMapNames[index].NormalizeText(),
                        expected,
                        System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private PlayerInput ResolvePlayerInput() =>
            playerInput != null ? playerInput : GetComponent<PlayerInput>();

        private InputActionMap ResolveGameplayActionMap(
            PlayerInput resolvedPlayerInput)
        {
            if (resolvedPlayerInput == null ||
                resolvedPlayerInput.actions == null)
            {
                return null;
            }

            return resolvedPlayerInput.actions.FindActionMap(
                GameplayActionMapName,
                throwIfNotFound: false);
        }

        private void LogWriteFailure(
            string issue,
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            EnsureLogger();
            _logger.Warning(
                "Unity PlayerInput Gate Adapter physical write failed.",
                LogFields.Of(
                    LogFields.Field("status", _lastStatus),
                    LogFields.Field("issue", issue.NormalizeText()),
                    LogFields.Field("blocksInputAcceptance", blocksInput),
                    LogFields.Field("blocksGameplayAction", blocksGameplay),
                    LogFields.Field("source", DefaultSource),
                    LogFields.Field(
                        "reason",
                        reason.NormalizeTextOrFallback("gate-adapter"))));
        }

        private void LogMissingTargetOnce(
            string message,
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            if (!logMissingTargetOnce || _loggedMissingTarget)
            {
                return;
            }

            _loggedMissingTarget = true;
            _logger.Warning(
                message,
                BuildLogFields(
                    _lastStatus,
                    reason,
                    true,
                    blocksInput,
                    blocksGameplay));
        }

        private void LogStateChange(
            string message,
            string status,
            string reason,
            bool blocked,
            bool blocksInput,
            bool blocksGameplay)
        {
            if (!logStateChanges)
            {
                return;
            }

            _logger.Debug(
                message,
                BuildLogFields(
                    status,
                    reason,
                    blocked,
                    blocksInput,
                    blocksGameplay));
        }

        private LogField[] BuildLogFields(
            string status,
            string reason,
            bool blocked,
            bool blocksInput,
            bool blocksGameplay)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            InputActionMap actionMap =
                ResolveGameplayActionMap(resolvedPlayerInput);

            return LogFields.Of(
                LogFields.Field(
                    "status",
                    status.NormalizeTextOrFallback("Unknown")),
                LogFields.Field("blockedByAdapter", blocked),
                LogFields.Field("blockMode", "DisableGameplayActionMap"),
                LogFields.Field(
                    "blockOnInputAcceptance",
                    blockOnInputAcceptance),
                LogFields.Field(
                    "blockOnGameplayAction",
                    blockOnGameplayAction),
                LogFields.Field("blocksInputAcceptance", blocksInput),
                LogFields.Field("blocksGameplayAction", blocksGameplay),
                LogFields.Field(
                    "playerInput",
                    resolvedPlayerInput != null
                        ? resolvedPlayerInput.name
                        : "<none>"),
                LogFields.Field("actionMap", GameplayActionMapName),
                LogFields.Field(
                    "actionMapEnabled",
                    actionMap != null && actionMap.enabled),
                LogFields.Field(
                    "restorePreviousState",
                    restorePreviousState),
                LogFields.Field("physicalWriter", nameof(UnityPlayerInputStateWriter)),
                LogFields.Field("source", DefaultSource),
                LogFields.Field(
                    "reason",
                    reason.NormalizeTextOrFallback("gate-adapter")));
        }

        private void EnsureLogger() =>
            _logger ??= FrameworkLogger.Create<UnityPlayerInputGateAdapter>();

    }
}
