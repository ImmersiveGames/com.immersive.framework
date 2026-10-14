using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Gate;
using Immersive.Framework.PlayerParticipation;
using Immersive.Logging.Records;
using Immersive.Framework.ApiStatus;
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
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public sealed class UnityPlayerInputGateAdapter : MonoBehaviour
    {
        private const string DefaultSource =
            nameof(UnityPlayerInputGateAdapter);

        [Header("Target")]
        [Tooltip("Explicit gameplay-owned PlayerInput gated by this adapter.")]
        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private PlayerInputActionMapReference gameplayActionMap;

        [SerializeField, HideInInspector]
        private string gameplayActionMapName = "Player";

        [Header("Gate Conditions")]
        [SerializeField]
        private bool blockOnInputAcceptance = true;

        [SerializeField]
        private bool blockOnGameplayAction = true;

        [Header("Blocking")]
        [Tooltip("When enabled, the adapter asks the canonical writer to restore only state changed by this Gate block.")]
        [SerializeField]
        private bool restorePreviousState = true;

        [SerializeField]
        private bool applyOnEnable = true;

        [Header("Diagnostics")]
        [SerializeField]
        private bool logStateChanges = true;

        [SerializeField]
        private bool logMissingRuntimeOnce = true;

        [SerializeField]
        private bool logMissingTargetOnce = true;

        private FrameworkLogger _logger;
        private IInputGateRuntimePort _inputGateRuntime;
        private string _inputGateRuntimeBindingDiagnostic =
            "Input Gate runtime port is not bound.";
        private bool _isBlockedByAdapter;
        private PlayerInput _blockedPlayerInput;
        private bool _isBlockedByConsumer;
        private bool _contractedGameplayRestoreSuppressed;
        private bool _consumerBlockOwnsSuppressedPosture;
        private bool _actionMapWasEnabledBeforeBlock;
        private string _lastStatus = "NotApplied";
        private string _lastReason = string.Empty;
        private bool _loggedMissingRuntime;
        private bool _loggedMissingTarget;
        private bool _hasObservedGateIntent;
        private bool _lastObservedGateIntent;
        private int _consecutiveUpdateFailures;
        private const int MaximumAutomaticUpdateAttempts = 2;

        public PlayerInput PlayerInput =>
            ResolvePlayerInput();

        public PlayerInputActionMapReference GameplayActionMapReference =>
            gameplayActionMap;

        public string GameplayActionMapName
        {
            get
            {
                return TryResolveGameplayActionMap(
                        out InputActionMap map,
                        out _)
                    ? map.name.NormalizeText()
                    : string.Empty;
            }
        }

        public bool BlockOnInputAcceptance =>
            blockOnInputAcceptance;

        public bool BlockOnGameplayAction =>
            blockOnGameplayAction;

        public bool IsBlockedByAdapter =>
            _isBlockedByAdapter &&
            IsGameplayActionMapPhysicallyBlocked(_blockedPlayerInput);

        internal bool IsBlockedByConsumer =>
            _isBlockedByConsumer;

        internal bool IsContractedGameplayRestoreSuppressed =>
            _contractedGameplayRestoreSuppressed;

        internal void SetContractedGameplayRestoreSuppressed(bool suppressed) =>
            _contractedGameplayRestoreSuppressed = suppressed;

        public string LastStatus =>
            _lastStatus.NormalizeText();

        public string LastReason =>
            _lastReason.NormalizeText();

        public bool HasInputGateRuntimeBinding =>
            _inputGateRuntime != null;

        public string InputGateRuntimeBindingStatus =>
            HasInputGateRuntimeBinding
                ? "Bound"
                : "Missing";

        public string InputGateRuntimeBindingDiagnostic =>
            _inputGateRuntimeBindingDiagnostic;

        public bool TryValidateAuthoring(
            out string diagnostic)
        {
            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (resolvedPlayerInput == null ||
                resolvedPlayerInput.actions == null)
            {
                diagnostic =
                    "Unity PlayerInput Gate Adapter requires a PlayerInput with actions.";
                return false;
            }

            if (!gameplayActionMap.TryResolve(
                    resolvedPlayerInput.actions,
                    out _,
                    out diagnostic))
            {
                diagnostic =
                    $"Unity PlayerInput Gate Adapter Gameplay Action Map is invalid. {diagnostic}";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        public bool TryResolveGameplayActionMap(
            out InputActionMap actionMap,
            out string diagnostic)
        {
            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (resolvedPlayerInput == null ||
                resolvedPlayerInput.actions == null)
            {
                actionMap = null;
                diagnostic =
                    "Gameplay Action Map resolution requires PlayerInput actions.";
                return false;
            }

            return gameplayActionMap.TryResolve(
                resolvedPlayerInput.actions,
                out actionMap,
                out diagnostic);
        }

        private void Awake() =>
            EnsureLogger();

        private void Reset() =>
            TryMigrateLegacyGameplayMap();

        private void OnValidate() =>
            TryMigrateLegacyGameplayMap();

        internal bool TryBindInputGateRuntime(
            IInputGateRuntimePort inputGateRuntime,
            out string issue)
        {
            if (inputGateRuntime == null)
            {
                issue =
                    "Input Gate runtime port binding requires a non-null port.";
                _inputGateRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_inputGateRuntime == null)
            {
                _inputGateRuntime = inputGateRuntime;
                _loggedMissingRuntime = false;
                _consecutiveUpdateFailures = 0;
                issue = string.Empty;
                _inputGateRuntimeBindingDiagnostic =
                    $"Bound '{inputGateRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(
                    _inputGateRuntime,
                    inputGateRuntime))
            {
                issue = string.Empty;
                _inputGateRuntimeBindingDiagnostic =
                    $"Bound '{inputGateRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue =
                "Input Gate runtime port binding rejected a different port for the current lifetime.";
            _inputGateRuntimeBindingDiagnostic = issue;
            return false;
        }

        internal bool TrySetConsumerGameplayAvailabilityBlocked(
            bool blocked,
            string reason,
            out string issue)
        {
            if (!TryResolveGameplayActionMap(
                    out _,
                    out issue))
            {
                return false;
            }

            bool previous = _isBlockedByConsumer;
            bool previousConsumerPostureOwnership =
                _consumerBlockOwnsSuppressedPosture;
            if (blocked && !_isBlockedByAdapter)
            {
                _consumerBlockOwnsSuppressedPosture = true;
            }
            _isBlockedByConsumer = blocked;
            ApplyFromCurrentRuntimeGate(
                reason.NormalizeTextOrFallback("consumer-gameplay-availability"));

            bool expectedPhysicalBlock =
                blocked || IsBlockedByCurrentRuntimeGate();
            bool resolvedAfterProjection = TryResolveGameplayActionMap(
                out InputActionMap gameplayActionMap,
                out string resolveIssue);
            if (resolvedAfterProjection &&
                !gameplayActionMap.enabled == expectedPhysicalBlock)
            {
                issue = string.Empty;
                return true;
            }

            _isBlockedByConsumer = previous;
            _consumerBlockOwnsSuppressedPosture =
                previousConsumerPostureOwnership;
            ApplyFromCurrentRuntimeGate("consumer-gameplay-availability-rollback");
            issue = !resolvedAfterProjection
                ? $"The canonical input writer could not confirm the consumer gameplay-availability projection because the Gameplay Action Map is unavailable. {resolveIssue}"
                : blocked
                    ? "The canonical input writer could not apply the consumer gameplay-availability block."
                    : "The canonical input writer could not restore gameplay after releasing the consumer block.";
            return false;
        }

        private void OnEnable()
        {
            EnsureLogger();

            if (applyOnEnable)
            {
                ApplyFromCurrentRuntimeGate(
                    "on-enable");
            }
        }

        private void Update()
        {
            bool desiredBlock = IsBlockedByCurrentRuntimeGate() ||
                _isBlockedByConsumer;
            if (!_hasObservedGateIntent ||
                desiredBlock != _lastObservedGateIntent)
            {
                _hasObservedGateIntent = true;
                _lastObservedGateIntent = desiredBlock;
                _consecutiveUpdateFailures = 0;
            }

            if (_consecutiveUpdateFailures >= MaximumAutomaticUpdateAttempts)
            {
                _lastStatus = "RetryLimitReached";
                _lastReason = "update";
                return;
            }

            ApplyFromCurrentRuntimeGate("update");
            if (IsRetryableFailureStatus(_lastStatus))
            {
                _consecutiveUpdateFailures++;
            }
            else
            {
                _consecutiveUpdateFailures = 0;
            }
        }

        private void OnDisable() =>
            RestoreIfNeeded(
                "component-disabled");

        private void OnDestroy() =>
            RestoreIfNeeded(
                "component-destroyed");

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Apply Current Gate")]
        public void ApplyCurrentGate()
        {
            _consecutiveUpdateFailures = 0;
            ApplyFromCurrentRuntimeGate("context-menu");
        }

        [ContextMenu("Immersive Framework/Unity Input/Gate Adapter/Restore")]
        public void Restore()
        {
            _consecutiveUpdateFailures = 0;
            RestoreIfNeeded("context-menu-restore");
        }

        internal bool TrySelectActionMap(
            string actionMapName,
            string source,
            string reason,
            out UnityPlayerInputActionMapWriteReceipt receipt,
            out string issue)
        {
            receipt = default;

            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (resolvedPlayerInput == null)
            {
                issue =
                    "PlayerInput write authority requires an explicit PlayerInput target.";
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
                bool selectedGameplayMap =
                    string.Equals(
                        actionMapName.NormalizeText(),
                        GameplayActionMapName,
                        System.StringComparison.Ordinal);

                if (!selectedGameplayMap)
                {
                    _actionMapWasEnabledBeforeBlock = false;
                }
                else
                {
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

            _lastStatus =
                "ActionMapSelectedByAuthority";
            _lastReason =
                reason.NormalizeTextOrFallback(
                    "action-map-selection");
            return true;
        }

        internal bool TryRestoreActionMap(
            UnityPlayerInputActionMapWriteReceipt receipt,
            string source,
            string reason,
            out string issue)
        {
            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (!UnityPlayerInputStateWriter.TryRestoreActionMap(
                    resolvedPlayerInput,
                    receipt,
                    out issue))
            {
                return false;
            }

            if (_isBlockedByAdapter)
            {
                bool restoredGameplayMap =
                    string.Equals(
                        UnityPlayerInputStateWriter.CurrentActionMapName(
                            resolvedPlayerInput),
                        GameplayActionMapName,
                        System.StringComparison.Ordinal);

                _actionMapWasEnabledBeforeBlock =
                    restoredGameplayMap;

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

            _lastStatus =
                "ActionMapRestoredByAuthority";
            _lastReason =
                reason.NormalizeTextOrFallback(
                    "action-map-restore");
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
            return TryApplyActionMapSet(
                ResolvePlayerInput(),
                primaryActionMapName,
                enabledActionMapNames,
                source,
                reason,
                out receipt,
                out issue);
        }

        internal bool TryApplyActionMapSet(
            PlayerInput exactPlayerInput,
            string primaryActionMapName,
            IReadOnlyList<string> enabledActionMapNames,
            string source,
            string reason,
            out UnityPlayerInputActionMapSetWriteReceipt receipt,
            out string issue)
        {
            receipt = default;
            PlayerInput resolvedPlayerInput = exactPlayerInput;

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

            if (this == null)
            {
                return true;
            }

            if (!_isBlockedByAdapter)
            {
                _actionMapWasEnabledBeforeBlock =
                    ContainsActionMap(
                        enabledActionMapNames,
                        GameplayActionMapName);
            }

            if (ReferenceEquals(resolvedPlayerInput, ResolvePlayerInput()))
            {
                ApplyFromCurrentRuntimeGate("action-map-set-applied");
            }

            _lastStatus =
                "ActionMapSetAppliedByAuthority";
            _lastReason =
                reason.NormalizeTextOrFallback(
                    "action-map-set-application");
            return true;
        }

        internal bool TryRestoreActionMapSet(
            UnityPlayerInputActionMapSetWriteReceipt receipt,
            string source,
            string reason,
            out string issue)
        {
            return TryRestoreActionMapSet(
                ResolvePlayerInput(),
                receipt,
                source,
                reason,
                out issue);
        }

        internal bool TryRestoreActionMapSet(
            PlayerInput exactPlayerInput,
            UnityPlayerInputActionMapSetWriteReceipt receipt,
            string source,
            string reason,
            out string issue)
        {
            PlayerInput resolvedPlayerInput = exactPlayerInput;

            if (!UnityPlayerInputStateWriter.TryRestoreActionMapSet(
                    resolvedPlayerInput,
                    receipt,
                    out issue))
            {
                return false;
            }

            if (this == null)
            {
                return true;
            }

            if (!_isBlockedByAdapter)
            {
                _actionMapWasEnabledBeforeBlock =
                    TryResolveGameplayActionMap(
                        out InputActionMap gameplayMap,
                        out _) &&
                    gameplayMap.enabled;
            }

            if (ReferenceEquals(resolvedPlayerInput, ResolvePlayerInput()))
            {
                ApplyFromCurrentRuntimeGate("action-map-set-restored");
            }

            _lastStatus =
                "ActionMapSetRestoredByAuthority";
            _lastReason =
                reason.NormalizeTextOrFallback(
                    "action-map-set-restore");
            return true;
        }

        internal bool TryCapturePosture(
            out PausePlayerInputPostureReceipt receipt,
            out string issue) =>
            UnityPlayerInputStateWriter.TryCapturePosture(
                ResolvePlayerInput(),
                out receipt,
                out issue);

        internal bool TryCapturePosture(
            PlayerInput exactPlayerInput,
            out PausePlayerInputPostureReceipt receipt,
            out string issue) =>
            UnityPlayerInputStateWriter.TryCapturePosture(
                exactPlayerInput,
                out receipt,
                out issue);

        internal PlayerInputPostureRestoreOutcome TryRestorePosture(
            PausePlayerInputPostureReceipt receipt,
            out string issue) =>
            UnityPlayerInputStateWriter.TryRestorePosture(
                ResolvePlayerInput(),
                receipt,
                out issue);

        internal PlayerInputPostureRestoreOutcome TryRestorePosture(
            PlayerInput exactPlayerInput,
            PausePlayerInputPostureReceipt receipt,
            out string issue) =>
            UnityPlayerInputStateWriter.TryRestorePosture(
                exactPlayerInput,
                receipt,
                out issue);

        private void ApplyFromCurrentRuntimeGate(
            string reason)
        {
            EnsureLogger();

            IInputGateRuntimePort inputGateRuntime =
                _inputGateRuntime;

            if (inputGateRuntime == null)
            {
                const string diagnostic =
                    "Input Gate runtime port is not bound. The adapter will retry on Update.";

                _inputGateRuntimeBindingDiagnostic =
                    diagnostic;

                if (_isBlockedByConsumer)
                {
                    ApplyBlock(
                        "input-gate-runtime-unbound",
                        false,
                        false);
                }
                else
                {
                    RestoreIfNeeded(
                        "input-gate-runtime-unbound");
                }

                if (!_isBlockedByAdapter)
                {
                    _lastStatus =
                        "SkippedMissingInputGateRuntime";
                    _lastReason =
                        reason.NormalizeText();
                }

                if (logMissingRuntimeOnce &&
                    !_loggedMissingRuntime)
                {
                    _loggedMissingRuntime = true;
                    _logger.Trace(
                        "Unity PlayerInput Gate Adapter skipped because the Input Gate runtime port is not bound. The adapter will retry on Update.",
                        BuildLogFields(
                            "SkippedMissingInputGateRuntime",
                            reason,
                            _isBlockedByAdapter,
                            false,
                            false));
                }

                return;
            }

            GateSnapshot gateSnapshot =
                inputGateRuntime.CurrentGateSnapshot;

            bool blocksInput =
                blockOnInputAcceptance &&
                gateSnapshot.IsBlockedForAnyOwner(
                    GateScope.Input,
                    GateDomain.InputAcceptance);

            bool blocksGameplay =
                blockOnGameplayAction &&
                gateSnapshot.IsBlockedForAnyOwner(
                    GateScope.Gameplay,
                    GateDomain.GameplayAction);

            if (blocksInput ||
                blocksGameplay ||
                _isBlockedByConsumer)
            {
                ApplyBlock(
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            RestoreIfNeeded(
                reason);
        }

        private bool IsBlockedByCurrentRuntimeGate()
        {
            IInputGateRuntimePort inputGateRuntime = _inputGateRuntime;
            if (inputGateRuntime == null)
            {
                return false;
            }

            GateSnapshot snapshot = inputGateRuntime.CurrentGateSnapshot;
            return (blockOnInputAcceptance &&
                    snapshot.IsBlockedForAnyOwner(
                        GateScope.Input,
                        GateDomain.InputAcceptance)) ||
                   (blockOnGameplayAction &&
                    snapshot.IsBlockedForAnyOwner(
                        GateScope.Gameplay,
                        GateDomain.GameplayAction));
        }

        private void ApplyBlock(
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (resolvedPlayerInput == null)
            {
                _lastStatus =
                    "SkippedMissingPlayerInput";
                _lastReason =
                    reason.NormalizeText();

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
            if (_isBlockedByAdapter && _blockedPlayerInput == null)
            {
                _isBlockedByAdapter = false;
                _actionMapWasEnabledBeforeBlock = false;
                _consumerBlockOwnsSuppressedPosture = false;
            }

            if (_isBlockedByAdapter &&
                _blockedPlayerInput != null &&
                !ReferenceEquals(_blockedPlayerInput, resolvedPlayerInput))
            {
                _lastStatus = "FailedDifferentPlayerInputWhileBlocked";
                _lastReason = reason.NormalizeText();
                return;
            }

            if (!TryResolveGameplayActionMap(
                    out InputActionMap gameplayMap,
                    out string mapIssue))
            {
                _lastStatus =
                    "FailedGameplayActionMapResolution";
                _lastReason =
                    reason.NormalizeText();

                LogWriteFailure(
                    mapIssue,
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            if (_isBlockedByAdapter && !gameplayMap.enabled)
            {
                _lastStatus = "AlreadyBlocked";
                _lastReason = reason.NormalizeText();
                return;
            }

            bool alreadyOwnedBlock = _isBlockedByAdapter;
            if (_isBlockedByConsumer)
            {
                _consumerBlockOwnsSuppressedPosture = true;
            }

            if (!UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                    resolvedPlayerInput,
                    gameplayMap.name,
                    false,
                    out bool previousEnabled,
                    out _,
                    out string issue))
            {
                if (!gameplayMap.enabled)
                {
                    if (!alreadyOwnedBlock)
                    {
                        _actionMapWasEnabledBeforeBlock = previousEnabled;
                    }
                    _blockedPlayerInput = resolvedPlayerInput;
                    _isBlockedByAdapter = true;
                    _lastStatus = "BlockedActionMapAfterWriteFailure";
                    _lastReason = reason.NormalizeText();
                    LogWriteFailure(issue, reason, blocksInput, blocksGameplay);
                    return;
                }

                _lastStatus =
                    "FailedActionMapBlock";
                _lastReason =
                    reason.NormalizeText();

                LogWriteFailure(
                    issue,
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            if (gameplayMap.enabled)
            {
                _lastStatus = "FailedActionMapBlock";
                _lastReason = reason.NormalizeText();
                LogWriteFailure(
                    "Gameplay Action Map remained enabled after the physical block write.",
                    reason,
                    blocksInput,
                    blocksGameplay);
                return;
            }

            if (!alreadyOwnedBlock)
            {
                _actionMapWasEnabledBeforeBlock = previousEnabled;
            }
            _blockedPlayerInput = resolvedPlayerInput;
            _isBlockedByAdapter = true;
            _lastStatus =
                "BlockedActionMap";
            _lastReason =
                reason.NormalizeText();

            LogStateChange(
                "Unity PlayerInput Gate Adapter requested gameplay action-map blocking.",
                _lastStatus,
                reason,
                true,
                blocksInput,
                blocksGameplay);
        }

        private void RestoreIfNeeded(
            string reason)
        {
            if (!_isBlockedByAdapter)
            {
                _consumerBlockOwnsSuppressedPosture = false;
                _lastStatus =
                    "Allowed";
                _lastReason =
                    reason.NormalizeText();
                return;
            }

            PlayerInput resolvedPlayerInput =
                _blockedPlayerInput;

            if (resolvedPlayerInput == null)
            {
                _isBlockedByAdapter = false;
                _blockedPlayerInput = null;
                _actionMapWasEnabledBeforeBlock = false;
                _consumerBlockOwnsSuppressedPosture = false;
                _lastStatus =
                    "TargetDestroyed";
                _lastReason =
                    reason.NormalizeText();
                return;
            }

            bool restored = true;
            string issue = string.Empty;
            bool restorationRequested = restorePreviousState ||
                _consumerBlockOwnsSuppressedPosture;

            if (restorationRequested)
            {
                if (resolvedPlayerInput.actions == null ||
                    !gameplayActionMap.TryResolve(
                        resolvedPlayerInput.actions,
                        out InputActionMap gameplayMap,
                        out issue))
                {
                    restored = false;
                }
                else
                {
                    restored =
                        UnityPlayerInputStateWriter.TrySetActionMapEnabled(
                            resolvedPlayerInput,
                            gameplayMap.name,
                            _actionMapWasEnabledBeforeBlock,
                            out _,
                            out _,
                            out issue);
                    if (gameplayMap.enabled == _actionMapWasEnabledBeforeBlock)
                    {
                        restored = true;
                    }
                    else if (restored)
                    {
                        restored = false;
                        issue = "Gameplay Action Map did not reach its captured pre-block enabled state.";
                    }
                }
            }

            if (!restored)
            {
                if (resolvedPlayerInput == null)
                {
                    _isBlockedByAdapter = false;
                    _blockedPlayerInput = null;
                    _actionMapWasEnabledBeforeBlock = false;
                    _consumerBlockOwnsSuppressedPosture = false;
                    _lastStatus = "TargetDestroyed";
                    _lastReason = reason.NormalizeText();
                    return;
                }

                _lastStatus =
                    "ReleaseFailed";
                _lastReason =
                    reason.NormalizeText();

                LogWriteFailure(
                    issue,
                    reason,
                    false,
                    false);
                return;
            }

            _isBlockedByAdapter = false;
            _blockedPlayerInput = null;
            _actionMapWasEnabledBeforeBlock = false;
            _consumerBlockOwnsSuppressedPosture = false;
            _lastStatus = restorationRequested
                ? "Released"
                : "ReleasedWithoutRestoration";
            _lastReason =
                reason.NormalizeText();

            LogStateChange(
                restorationRequested
                    ? "Unity PlayerInput Gate Adapter confirmed gameplay posture restoration through the canonical writer."
                    : "Unity PlayerInput Gate Adapter released its block ownership without restoring prior gameplay posture.",
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

            string expected =
                expectedActionMapName.NormalizeText();

            for (int index = 0;
                 index < actionMapNames.Count;
                 index++)
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
            playerInput;

        private bool IsGameplayActionMapPhysicallyBlocked()
        {
            return IsGameplayActionMapPhysicallyBlocked(ResolvePlayerInput());
        }

        private bool IsGameplayActionMapPhysicallyBlocked(PlayerInput exactPlayerInput)
        {
            return exactPlayerInput != null &&
                exactPlayerInput.actions != null &&
                gameplayActionMap.TryResolve(
                    exactPlayerInput.actions,
                    out InputActionMap gameplayMap,
                    out _) &&
                !gameplayMap.enabled;
        }

        private static bool IsRetryableFailureStatus(string status)
        {
            return status.StartsWith(
                    "Failed",
                    System.StringComparison.Ordinal) ||
                status == "ReleaseFailed" ||
                status == "SkippedMissingInputGateRuntime" ||
                status == "SkippedMissingPlayerInput";
        }

        private void TryMigrateLegacyGameplayMap()
        {
            if (gameplayActionMap.IsConfigured)
            {
                return;
            }

            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            if (resolvedPlayerInput == null ||
                resolvedPlayerInput.actions == null)
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
                resolvedPlayerInput.actions.FindActionMap(
                    legacyName,
                    false);

            if (legacyMap != null)
            {
                gameplayActionMap =
                    PlayerInputActionMapReference.From(
                        legacyMap);
            }
        }

        private void LogWriteFailure(
            string issue,
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            EnsureLogger();

            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            _logger.Warning(
                "Unity PlayerInput Gate Adapter physical write failed.",
                LogFields.Of(
                    LogFields.Field(
                        "status",
                        _lastStatus),
                    LogFields.Field(
                        "issue",
                        issue.NormalizeText()),
                    LogFields.Field(
                        "blocksInputAcceptance",
                        blocksInput),
                    LogFields.Field(
                        "blocksGameplayAction",
                        blocksGameplay),
                    LogFields.Field(
                        "source",
                        DefaultSource),
                    LogFields.Field(
                        "reason",
                        reason.NormalizeTextOrFallback(
                            "gate-adapter")),
                    // --- Diagnostics-only fields, see BuildLogFields for rationale.
                    LogFields.Field(
                        "adapterInstanceId",
                        GetEntityId()),
                    LogFields.Field(
                        "playerInputInstanceId",
                        resolvedPlayerInput != null
                            ? (object)resolvedPlayerInput.GetEntityId()
                            : 0),
                    LogFields.Field(
                        "playerInputEnabled",
                        resolvedPlayerInput != null &&
                        resolvedPlayerInput.enabled),
                    LogFields.Field(
                        "currentActionMapName",
                        resolvedPlayerInput != null &&
                        resolvedPlayerInput.currentActionMap != null
                            ? resolvedPlayerInput.currentActionMap.name
                            : "<none>"),
                    LogFields.Field(
                        "currentActionMapId",
                        resolvedPlayerInput != null &&
                        resolvedPlayerInput.currentActionMap != null
                            ? resolvedPlayerInput.currentActionMap.id.ToString()
                            : "<none>"),
                    LogFields.Field(
                        "currentActionMapEnabled",
                        resolvedPlayerInput != null &&
                        resolvedPlayerInput.currentActionMap != null &&
                        resolvedPlayerInput.currentActionMap.enabled),
                    LogFields.Field(
                        "isBlockedByAdapter",
                        _isBlockedByAdapter),
                    LogFields.Field(
                        "actionMapWasEnabledBeforeBlock",
                        _actionMapWasEnabledBeforeBlock),
                    LogFields.Field(
                        "coLocatedLocalPlayerHost",
                        ResolveCoLocatedHostIdentity(
                            resolvedPlayerInput))));
        }

        private void LogMissingTargetOnce(
            string message,
            string reason,
            bool blocksInput,
            bool blocksGameplay)
        {
            if (!logMissingTargetOnce ||
                _loggedMissingTarget)
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
            PlayerInput resolvedPlayerInput =
                ResolvePlayerInput();

            bool resolvedMap =
                TryResolveGameplayActionMap(
                    out InputActionMap actionMap,
                    out string actionMapDiagnostic);

            return LogFields.Of(
                LogFields.Field(
                    "status",
                    status.NormalizeTextOrFallback(
                        "Unknown")),
                LogFields.Field(
                    "blockedByAdapter",
                    blocked),
                LogFields.Field(
                    "blockMode",
                    "DisableGameplayActionMap"),
                LogFields.Field(
                    "blockOnInputAcceptance",
                    blockOnInputAcceptance),
                LogFields.Field(
                    "blockOnGameplayAction",
                    blockOnGameplayAction),
                LogFields.Field(
                    "blockedByConsumerAvailability",
                    _isBlockedByConsumer),
                LogFields.Field(
                    "blocksInputAcceptance",
                    blocksInput),
                LogFields.Field(
                    "blocksGameplayAction",
                    blocksGameplay),
                LogFields.Field(
                    "playerInput",
                    resolvedPlayerInput != null
                        ? resolvedPlayerInput.name
                        : "<none>"),
                LogFields.Field(
                    "actionMap",
                    resolvedMap
                        ? actionMap.name
                        : "<unresolved>"),
                LogFields.Field(
                    "actionMapId",
                    gameplayActionMap.ActionMapId),
                LogFields.Field(
                    "actionMapEnabled",
                    resolvedMap &&
                    actionMap.enabled),
                LogFields.Field(
                    "actionMapDiagnostic",
                    actionMapDiagnostic.NormalizeText()),
                LogFields.Field(
                    "restorePreviousState",
                    restorePreviousState),
                LogFields.Field(
                    "physicalWriter",
                    nameof(UnityPlayerInputStateWriter)),
                LogFields.Field(
                    "source",
                    DefaultSource),
                LogFields.Field(
                    "reason",
                    reason.NormalizeTextOrFallback(
                        "gate-adapter")),
                // --- Diagnostics-only fields added to disambiguate this adapter/PlayerInput
                // instance from another co-existing instance (e.g. a retained Session-owned
                // Host vs. a redundant Scene-Provided candidate). No behavior depends on these.
                LogFields.Field(
                    "adapterInstanceId",
                    GetEntityId()),
                LogFields.Field(
                    "playerInputInstanceId",
                    resolvedPlayerInput != null
                        ? (object)resolvedPlayerInput.GetEntityId()
                        : 0),
                LogFields.Field(
                    "playerInputEnabled",
                    resolvedPlayerInput != null &&
                    resolvedPlayerInput.enabled),
                LogFields.Field(
                    "currentActionMapName",
                    resolvedPlayerInput != null &&
                    resolvedPlayerInput.currentActionMap != null
                        ? resolvedPlayerInput.currentActionMap.name
                        : "<none>"),
                LogFields.Field(
                    "currentActionMapId",
                    resolvedPlayerInput != null &&
                    resolvedPlayerInput.currentActionMap != null
                        ? resolvedPlayerInput.currentActionMap.id.ToString()
                        : "<none>"),
                LogFields.Field(
                    "currentActionMapEnabled",
                    resolvedPlayerInput != null &&
                    resolvedPlayerInput.currentActionMap != null &&
                    resolvedPlayerInput.currentActionMap.enabled),
                LogFields.Field(
                    "isBlockedByAdapter",
                    _isBlockedByAdapter),
                LogFields.Field(
                    "actionMapWasEnabledBeforeBlock",
                    _actionMapWasEnabledBeforeBlock),
                LogFields.Field(
                    "coLocatedLocalPlayerHost",
                    ResolveCoLocatedHostIdentity(
                        resolvedPlayerInput)));
        }

        /// <summary>
        /// Diagnostics-only helper. Looks up a <see cref="LocalPlayerHostAuthoring"/> on the
        /// same GameObject as the resolved PlayerInput (never a global/scene-wide search) so
        /// its identity can be correlated against PlayerHostEvidenceProjection's diagnostic
        /// logs. Returns "&lt;none&gt;" when no such component is co-located.
        /// </summary>
        private static string ResolveCoLocatedHostIdentity(
            PlayerInput resolvedPlayerInput)
        {
            if (resolvedPlayerInput == null)
            {
                return "<none>";
            }

            LocalPlayerHostAuthoring coLocatedHost =
                resolvedPlayerInput.GetComponent<LocalPlayerHostAuthoring>();

            return coLocatedHost != null
                ? $"{coLocatedHost.name}#{coLocatedHost.GetEntityId()}"
                : "<none>";
        }

        private void EnsureLogger() =>
            _logger ??=
                FrameworkLogger.Create<
                    UnityPlayerInputGateAdapter>();
    }
}
