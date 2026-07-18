using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Scene-authored product boundary for one PlayerInput-scoped Pause/InputMode authority.
    /// It owns the resident logical InputMode state, serializes posture requests, delegates
    /// physical Unity input effects to the canonical writer pipeline and commits state only
    /// after the complete Pause/InputMode operation succeeds.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input Mode/Pause PlayerInput Runtime Bridge")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 resident InputMode authority and IC3 canonical Pause submitter boundary.")]
    public sealed class PauseInputModeUnityPlayerInputRuntimeBridge : MonoBehaviour
    {
        private const string DefaultSource =
            nameof(PauseInputModeUnityPlayerInputRuntimeBridge);

        private FrameworkLogger _logger;
        private int _requestSequence;
        private PauseInputModeUnityPlayerInputRuntimeBridgeResult _lastResult;
        private InputModeRuntimeContext _inputModeRuntimeContext;
        private InputModeRuntimeOperationResult _lastInputModeRuntimeOperation;
        private string _lastRollbackDiagnostic = string.Empty;

        [Header("Unity PlayerInput")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Framework References")]
        [SerializeField] private UnityInputTargetDeclaration[] unityInputTargets;
        [SerializeField] private PlayerActorDeclaration[] playerActors;
        [SerializeField]
        private LocalPlayerProvisioningAuthoring localPlayerProvisioningAuthoring;
        [Tooltip(
            "When enabled, missing authored arrays may be filled from loaded-scene declarations " +
            "(validators fail closed on duplicates). PlayerInput is never resolved by scene-wide " +
            "Find; use the explicit field, PlayerActors array, or a co-located PlayerInput when " +
            "this flag is on.")]
        [SerializeField] private bool autoDiscoverMissingReferences;
        [SerializeField] private bool requireLocalPlayerProvisioning = true;

        [Header("Action Maps")]
        [SerializeField] private string globalActionMapName = "Global";
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";

        [Header("Request")]
        [SerializeField]
        private string reason = "pause.inputmode.playerinput.runtime.bridge";
        [SerializeField] private bool logResults = true;

        public PlayerInput PlayerInput => playerInput;
        public bool AutoDiscoverMissingReferences =>
            autoDiscoverMissingReferences;
        public bool RequireLocalPlayerProvisioning =>
            requireLocalPlayerProvisioning;
        public string GlobalActionMapName =>
            globalActionMapName.NormalizeTextOrFallback("Global");
        public string GameplayActionMapName =>
            gameplayActionMapName.NormalizeTextOrFallback("Player");
        public string UiActionMapName =>
            uiActionMapName.NormalizeTextOrFallback("UI");
        public PauseInputModeUnityPlayerInputRuntimeBridgeResult LastResult =>
            _lastResult;
        public bool HasLastResult => _lastResult != null;
        public bool LastSucceeded => _lastResult != null && _lastResult.Succeeded;
        public bool LastIgnored => _lastResult != null && _lastResult.Ignored;
        public bool LastFailed => _lastResult != null && _lastResult.Failed;
        public bool HasInputModeRuntime => _inputModeRuntimeContext != null;
        public InputModeRuntimeSnapshot InputModeRuntimeSnapshot =>
            _inputModeRuntimeContext?.CreateSnapshot();
        public InputModeRuntimeOperationResult LastInputModeRuntimeOperation =>
            _lastInputModeRuntimeOperation;
        public string LastRollbackDiagnostic =>
            _lastRollbackDiagnostic.NormalizeText();

        private void Awake()
        {
            _logger =
                FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Pause")]
        public void RequestPause()
        {
            Submit(
                PauseRequestKind.Pause,
                "pause.inputmode.playerinput.runtime.bridge.pause");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Resume")]
        public void RequestResume()
        {
            Submit(
                PauseRequestKind.Resume,
                "pause.inputmode.playerinput.runtime.bridge.resume");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Toggle")]
        public void TogglePause()
        {
            Submit(
                PauseRequestKind.Toggle,
                "pause.inputmode.playerinput.runtime.bridge.toggle");
        }

        private void Submit(PauseRequestKind kind, string fallbackReason)
        {
            _lastResult = SubmitInternal(
                kind,
                DefaultSource,
                ResolveReason(fallbackReason));
            if (logResults && _lastResult != null)
            {
                EnsureLogger();
                _logger.Debug(
                    "Pause InputMode PlayerInput Runtime Bridge completed.",
                    BuildLogFields());
            }
        }

        internal PauseInputModeUnityPlayerInputRuntimeBridgeResult
            SubmitForDiagnostics(
                PauseRequestKind kind,
                string source,
                string requestReason)
        {
            _lastResult = SubmitInternal(kind, source, requestReason);
            return _lastResult;
        }

        internal void ConfigureForDiagnostics(
            PlayerInput input,
            UnityInputTargetDeclaration[] targets,
            PlayerActorDeclaration[] actors,
            LocalPlayerProvisioningAuthoring provisioningAuthoring,
            string playerMap,
            string uiMap,
            bool autoDiscover,
            bool requireProvisioning)
        {
            playerInput = input;
            unityInputTargets = targets;
            playerActors = actors;
            localPlayerProvisioningAuthoring = provisioningAuthoring;
            globalActionMapName = "Global";
            gameplayActionMapName =
                playerMap.NormalizeTextOrFallback("Player");
            uiActionMapName = uiMap.NormalizeTextOrFallback("UI");
            autoDiscoverMissingReferences = autoDiscover;
            requireLocalPlayerProvisioning = requireProvisioning;

            _inputModeRuntimeContext = null;
            _lastInputModeRuntimeOperation = null;
            _lastRollbackDiagnostic =
                "InputMode runtime cleared by explicit diagnostics reconfiguration.";
        }

        private PauseInputModeUnityPlayerInputRuntimeBridgeResult SubmitInternal(
            PauseRequestKind kind,
            string source,
            string requestReason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(DefaultSource);
            string normalizedReason = requestReason.NormalizeTextOrFallback(
                ResolveReason("pause.inputmode.playerinput.runtime.bridge"));
            _lastRollbackDiagnostic = string.Empty;

            FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost);
            ResolvedReferences references = ResolveReferences(
                normalizedSource,
                normalizedReason);
            _requestSequence++;
            string requestId =
                $"pause.inputmode.playerinput.runtime.bridge.{_requestSequence}";

            var request = new PauseInputModeApplyRequest(
                runtimeHost,
                kind,
                requestId,
                references.PlayerInput,
                references.TargetSet,
                references.PlayerActorSet,
                references.LocalPlayerProvisioningValidation,
                references.ActionMapEvidence,
                references.ActionMapBindings,
                CreatePersistentActionMapNames(),
                requireLocalPlayerProvisioning,
                normalizedSource,
                normalizedReason);

            var service = new PauseInputModeApplyService();
            if (runtimeHost == null || references.PlayerInput == null)
            {
                return service.Apply(request).ToRuntimeBridgeResult();
            }

            if (!runtimeHost.TryGetPauseSnapshot(out PauseSnapshot pauseSnapshot) ||
                pauseSnapshot.State == PauseState.Unknown)
            {
                return service.Apply(request).ToRuntimeBridgeResult();
            }

            InputModeKind expectedCurrentMode =
                MapPauseStateToInputMode(pauseSnapshot.State);
            if (!TryEnsureInputModeRuntime(
                    references.PlayerInput,
                    expectedCurrentMode,
                    normalizedSource,
                    normalizedReason,
                    out string runtimeIssue))
            {
                return CreateAuthorityFailure(
                    kind,
                    pauseSnapshot.State,
                    pauseSnapshot.State,
                    normalizedSource,
                    normalizedReason,
                    runtimeIssue);
            }

            InputModeRuntimeSnapshot runtimeSnapshot =
                _inputModeRuntimeContext.CreateSnapshot();
            if (runtimeSnapshot.CurrentMode != expectedCurrentMode)
            {
                return CreateAuthorityFailure(
                    kind,
                    pauseSnapshot.State,
                    pauseSnapshot.State,
                    normalizedSource,
                    normalizedReason,
                    "Resident InputMode state does not match the current Pause runtime state. " +
                    $"context='{runtimeSnapshot.ContextId}' " +
                    $"inputMode='{runtimeSnapshot.CurrentMode}' " +
                    $"pause='{pauseSnapshot.State}'. No implicit reconciliation was applied.");
            }

            request = new PauseInputModeApplyRequest(
                runtimeHost,
                kind,
                requestId,
                references.PlayerInput,
                references.TargetSet,
                references.PlayerActorSet,
                references.LocalPlayerProvisioningValidation,
                references.ActionMapEvidence,
                references.ActionMapBindings,
                CreatePersistentActionMapNames(),
                requireLocalPlayerProvisioning,
                normalizedSource,
                normalizedReason,
                runtimeSnapshot.CurrentState);

            PauseState targetPauseState =
                PauseRequest.ResolveTargetState(kind, pauseSnapshot.State);
            InputModeKind targetInputMode =
                MapPauseStateToInputMode(targetPauseState);
            var inputModeRequest = new InputModeRequest(
                targetInputMode,
                normalizedSource,
                normalizedReason);

            InputModeRuntimeOperationResult beginResult =
                _inputModeRuntimeContext.TryBegin(
                    inputModeRequest,
                    normalizedSource,
                    out InputModeRuntimeTransaction transaction);
            _lastInputModeRuntimeOperation = beginResult;

            if (beginResult.Failed)
            {
                return CreateAuthorityFailure(
                    kind,
                    pauseSnapshot.State,
                    targetPauseState,
                    normalizedSource,
                    normalizedReason,
                    beginResult.Message);
            }

            if (beginResult.Ignored)
            {
                // No logical transition exists. The service still submits the exact Pause
                // request so callers receive the canonical IgnoredNoChange evidence.
                return service.Apply(request).ToRuntimeBridgeResult();
            }

            PauseInputModeApplyResult applyResult = service.Apply(request);
            if (!applyResult.Succeeded)
            {
                InputModeRuntimeOperationResult rollbackResult =
                    _inputModeRuntimeContext.Rollback(
                        transaction,
                        normalizedSource,
                        "pause-inputmode-application-failed");
                _lastInputModeRuntimeOperation = rollbackResult;
                bool pauseRollbackSucceeded = TryRollbackPauseIfNeeded(
                    runtimeHost,
                    applyResult,
                    normalizedSource,
                    normalizedReason,
                    out string rollbackDiagnostic);
                _lastRollbackDiagnostic = rollbackDiagnostic;

                string message =
                    $"{applyResult.Message} " +
                    $"InputModeTransaction='{rollbackResult.Status}'. " +
                    $"PauseRollbackSucceeded='{pauseRollbackSucceeded}'. " +
                    $"PauseRollback='{rollbackDiagnostic}'.";
                return CopyResult(applyResult, message);
            }

            if (applyResult.RequestedMode != targetInputMode ||
                applyResult.CurrentPauseState != targetPauseState)
            {
                InputModeRuntimeOperationResult mismatchRollback =
                    _inputModeRuntimeContext.Rollback(
                        transaction,
                        normalizedSource,
                        "pause-inputmode-result-mismatch");
                _lastInputModeRuntimeOperation = mismatchRollback;
                bool pauseRollbackSucceeded = TryRollbackPauseIfNeeded(
                    runtimeHost,
                    applyResult,
                    normalizedSource,
                    normalizedReason,
                    out string rollbackDiagnostic);
                _lastRollbackDiagnostic = rollbackDiagnostic;
                return CreateAuthorityFailure(
                    kind,
                    pauseSnapshot.State,
                    targetPauseState,
                    normalizedSource,
                    normalizedReason,
                    "Pause/InputMode apply returned evidence that does not match " +
                    "the prepared resident transaction. " +
                    $"requestedMode='{applyResult.RequestedMode}' " +
                    $"expectedMode='{targetInputMode}' " +
                    $"pause='{applyResult.CurrentPauseState}' " +
                    $"expectedPause='{targetPauseState}' " +
                    $"transactionRollback='{mismatchRollback.Status}' " +
                    $"pauseRollbackSucceeded='{pauseRollbackSucceeded}' " +
                    $"pauseRollback='{rollbackDiagnostic}'.");
            }

            InputModeRuntimeOperationResult commitResult =
                _inputModeRuntimeContext.Commit(
                    transaction,
                    normalizedSource,
                    normalizedReason);
            _lastInputModeRuntimeOperation = commitResult;
            if (!commitResult.Committed)
            {
                InputModeRuntimeOperationResult rollbackResult =
                    _inputModeRuntimeContext.Rollback(
                        transaction,
                        normalizedSource,
                        "inputmode-commit-failed");
                _lastInputModeRuntimeOperation = rollbackResult;
                bool pauseRollbackSucceeded = TryRollbackPauseIfNeeded(
                    runtimeHost,
                    applyResult,
                    normalizedSource,
                    normalizedReason,
                    out string rollbackDiagnostic);
                _lastRollbackDiagnostic = rollbackDiagnostic;

                return CreateAuthorityFailure(
                    kind,
                    pauseSnapshot.State,
                    targetPauseState,
                    normalizedSource,
                    normalizedReason,
                    "Physical Pause/InputMode application completed but the resident " +
                    "InputMode transaction could not commit. " +
                    $"commit='{commitResult.Status}' " +
                    $"transactionRollback='{rollbackResult.Status}' " +
                    $"pauseRollbackSucceeded='{pauseRollbackSucceeded}' " +
                    $"pauseRollback='{rollbackDiagnostic}'.");
            }

            return applyResult.ToRuntimeBridgeResult();
        }

        private bool TryEnsureInputModeRuntime(
            PlayerInput resolvedPlayerInput,
            InputModeKind initialMode,
            string source,
            string requestReason,
            out string issue)
        {
            issue = string.Empty;
            if (resolvedPlayerInput == null)
            {
                issue =
                    "Resident InputMode authority requires an explicit PlayerInput.";
                return false;
            }

            string contextId =
                $"inputmode:{resolvedPlayerInput.GetEntityId()}";
            if (_inputModeRuntimeContext == null)
            {
                var initialState = new InputModeState(
                    InputModeDefinitions.FromKind(
                        initialMode,
                        source,
                        requestReason),
                    0,
                    source,
                    requestReason);
                _inputModeRuntimeContext = new InputModeRuntimeContext(
                    contextId,
                    initialState);
                return true;
            }

            if (!string.Equals(
                    _inputModeRuntimeContext.ContextId,
                    contextId,
                    StringComparison.Ordinal))
            {
                issue =
                    "Resident InputMode authority is already bound to another PlayerInput. " +
                    $"current='{_inputModeRuntimeContext.ContextId}' requested='{contextId}'.";
                return false;
            }

            return true;
        }

        private bool TryRollbackPauseIfNeeded(
            FrameworkRuntimeHost runtimeHost,
            PauseInputModeApplyResult applyResult,
            string source,
            string requestReason,
            out string diagnostic)
        {
            if (runtimeHost == null)
            {
                diagnostic = "Skipped: runtime host unavailable.";
                return false;
            }

            if (applyResult == null ||
                !applyResult.PauseResult.IsValid ||
                applyResult.PauseStatus != PauseRequestStatus.Applied ||
                applyResult.CurrentPauseState == applyResult.PreviousPauseState)
            {
                diagnostic = "Not required: Pause state did not change.";
                return true;
            }

            PauseRequestKind rollbackKind =
                applyResult.PreviousPauseState == PauseState.Paused
                    ? PauseRequestKind.Pause
                    : PauseRequestKind.Resume;
            try
            {
                PauseResult rollbackResult = runtimeHost.RequestPause(
                    rollbackKind,
                    source,
                    requestReason.NormalizeTextOrFallback(
                        "pause-inputmode-rollback") + "; rollback");
                bool succeeded =
                    rollbackResult.Completed &&
                    rollbackResult.CurrentState ==
                    applyResult.PreviousPauseState;
                diagnostic =
                    $"status='{rollbackResult.Status}' " +
                    $"current='{rollbackResult.CurrentState}' " +
                    $"expected='{applyResult.PreviousPauseState}'.";
                return succeeded;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Exception='{exception.GetType().Name}' " +
                    $"message='{exception.Message.NormalizeText()}'.";
                return false;
            }
        }

        private static PauseInputModeUnityPlayerInputRuntimeBridgeResult
            CopyResult(
                PauseInputModeApplyResult applyResult,
                string message)
        {
            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status =
                applyResult.Failed
                    ? applyResult.Status
                    : PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedInputModePlayerInputApplication;
            PauseInputModeApplyStage failedStage = applyResult.Failed
                ? applyResult.FailedStage
                : PauseInputModeApplyStage.AdapterApplyFailed;
            return new PauseInputModeUnityPlayerInputRuntimeBridgeResult(
                status,
                applyResult.RequestKind,
                applyResult.PreviousPauseState,
                applyResult.TargetPauseState,
                applyResult.PauseResult,
                applyResult.PreflightPlanResult,
                applyResult.ApplicationResult,
                applyResult.Source,
                applyResult.Reason,
                message,
                failedStage,
                applyResult);
        }

        private static PauseInputModeUnityPlayerInputRuntimeBridgeResult
            CreateAuthorityFailure(
                PauseRequestKind kind,
                PauseState previousPauseState,
                PauseState targetPauseState,
                string source,
                string requestReason,
                string message)
        {
            return new PauseInputModeUnityPlayerInputRuntimeBridgeResult(
                PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight,
                kind,
                previousPauseState,
                targetPauseState,
                default,
                null,
                null,
                source,
                requestReason,
                message,
                PauseInputModeApplyStage.PreflightRejected);
        }

        private ResolvedReferences ResolveReferences(
            string source,
            string requestReason)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            UnityInputTargetSet targetSet = ResolveTargetSet(
                source,
                requestReason);
            PlayerActorSet playerActorSet = ResolvePlayerActorSet(
                source,
                requestReason);
            LocalPlayerProvisioningValidationResult provisioningValidation =
                ResolveLocalPlayerProvisioningValidation(
                    source,
                    requestReason);
            UnityInputActionMapEvidence actionMapEvidence =
                UnityInputActionMapEvidence.FromInputActionAsset(
                    resolvedPlayerInput == null
                        ? null
                        : resolvedPlayerInput.actions,
                    source,
                    requestReason);

            return new ResolvedReferences(
                resolvedPlayerInput,
                targetSet,
                playerActorSet,
                provisioningValidation,
                actionMapEvidence,
                CreateActionMapBindings(source, requestReason));
        }

        private UnityInputTargetSet ResolveTargetSet(
            string source,
            string requestReason)
        {
            if (unityInputTargets != null && unityInputTargets.Length > 0)
            {
                return UnityInputTargetValidator.ValidateDeclarations(
                    unityInputTargets,
                    source,
                    requestReason);
            }

            return autoDiscoverMissingReferences
                ? UnityInputTargetValidator.ValidateLoadedSceneDeclarations(
                    source,
                    requestReason)
                : UnityInputTargetValidator.ValidateDeclarations(
                    Array.Empty<UnityInputTargetDeclaration>(),
                    source,
                    requestReason);
        }

        private PlayerActorSet ResolvePlayerActorSet(
            string source,
            string requestReason)
        {
            if (playerActors != null && playerActors.Length > 0)
            {
                return PlayerActorValidator.ValidateDeclarations(
                    playerActors,
                    source,
                    requestReason);
            }

            return autoDiscoverMissingReferences
                ? PlayerActorValidator.ValidateLoadedSceneDeclarations(
                    source,
                    requestReason)
                : PlayerActorValidator.ValidateDeclarations(
                    Array.Empty<PlayerActorDeclaration>(),
                    source,
                    requestReason);
        }

        private LocalPlayerProvisioningValidationResult
            ResolveLocalPlayerProvisioningValidation(
                string source,
                string requestReason)
        {
            if (localPlayerProvisioningAuthoring != null)
            {
                return LocalPlayerProvisioningConfigurationRules.Validate(
                    new[] { localPlayerProvisioningAuthoring },
                    requireLocalPlayerProvisioning,
                    source,
                    requestReason);
            }

            return LocalPlayerProvisioningConfigurationRules.Validate(
                Array.Empty<LocalPlayerProvisioningAuthoring>(),
                requireLocalPlayerProvisioning,
                source,
                requestReason);
        }

        private PlayerInput ResolvePlayerInput()
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            if (playerActors != null)
            {
                for (int index = 0; index < playerActors.Length; index++)
                {
                    if (playerActors[index] != null &&
                        playerActors[index].PlayerInput != null)
                    {
                        return playerActors[index].PlayerInput;
                    }
                }
            }

            if (!autoDiscoverMissingReferences)
            {
                return null;
            }

            // Co-located evidence only. Scene-wide FindObjectsByType is
            // intentionally not used with multiple Players/additive scenes.
            return GetComponent<PlayerInput>();
        }

        private InputModeUnityActionMapBinding[] CreateActionMapBindings(
            string source,
            string requestReason)
        {
            return new[]
            {
                new InputModeUnityActionMapBinding(
                    InputModeKind.Gameplay,
                    UnityInputActionMapName.From(GameplayActionMapName),
                    true),
                new InputModeUnityActionMapBinding(
                    InputModeKind.PauseOverlay,
                    UnityInputActionMapName.From(UiActionMapName),
                    true),
                new InputModeUnityActionMapBinding(
                    InputModeKind.FrontendMenu,
                    UnityInputActionMapName.From(UiActionMapName),
                    true),
                new InputModeUnityActionMapBinding(
                    InputModeKind.InputLocked,
                    UnityInputActionMapName.From(GlobalActionMapName),
                    true)
            };
        }

        private UnityInputActionMapName[] CreatePersistentActionMapNames()
        {
            return new[]
            {
                UnityInputActionMapName.From(GlobalActionMapName)
            };
        }

        private string ResolveReason(string fallbackReason) =>
            reason.NormalizeTextOrFallback(fallbackReason);

        private Logging.Records.LogField[] BuildLogFields()
        {
            InputModeRuntimeSnapshot snapshot = InputModeRuntimeSnapshot;
            return Logging.Records.LogFields.Of(
                Logging.Records.LogFields.Field(
                    "status",
                    _lastResult.Status.ToString()),
                Logging.Records.LogFields.Field(
                    "failedStage",
                    _lastResult.FailedStage.ToString()),
                Logging.Records.LogFields.Field(
                    "pauseStatus",
                    _lastResult.PauseStatus.ToString()),
                Logging.Records.LogFields.Field(
                    "requestedMode",
                    _lastResult.RequestedMode.ToString()),
                Logging.Records.LogFields.Field(
                    "operation",
                    _lastResult.Operation.ToString()),
                Logging.Records.LogFields.Field(
                    "residentMode",
                    snapshot == null
                        ? InputModeKind.Unknown.ToString()
                        : snapshot.CurrentMode.ToString()),
                Logging.Records.LogFields.Field(
                    "residentRevision",
                    snapshot?.Revision ?? -1),
                Logging.Records.LogFields.Field(
                    "residentOperation",
                    _lastInputModeRuntimeOperation == null
                        ? InputModeRuntimeOperationStatus.Unknown.ToString()
                        : _lastInputModeRuntimeOperation.Status.ToString()),
                Logging.Records.LogFields.Field(
                    "previousActionMap",
                    _lastResult.PreviousActionMapName.ToString()),
                Logging.Records.LogFields.Field(
                    "appliedActionMap",
                    _lastResult.AppliedActionMapName.ToString()),
                Logging.Records.LogFields.Field(
                    "persistentActionMap",
                    GlobalActionMapName),
                Logging.Records.LogFields.Field(
                    "enabledActionMaps",
                    EnabledActionMapSummary()),
                Logging.Records.LogFields.Field(
                    "actionMapSwitching",
                    _lastResult.SwitchesActionMaps),
                Logging.Records.LogFields.Field(
                    "inputBehavior",
                    _lastResult.AppliesInputBehavior),
                Logging.Records.LogFields.Field(
                    "pauseRuntimeWiring",
                    _lastResult.PauseRuntimeWiring),
                Logging.Records.LogFields.Field(
                    "rollback",
                    LastRollbackDiagnostic),
                Logging.Records.LogFields.Field(
                    "playerJoin",
                    _lastResult.CallsPlayerJoin),
                Logging.Records.LogFields.Field(
                    "actorSpawning",
                    _lastResult.SpawnsActor),
                Logging.Records.LogFields.Field(
                    "diagnostics",
                    _lastResult.ToDiagnosticString()));
        }

        private string EnabledActionMapSummary()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return string.Empty;
            }

            var enabled = new System.Collections.Generic.List<string>();
            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                if (map.enabled)
                {
                    enabled.Add(map.name.NormalizeText());
                }
            }

            enabled.Sort(StringComparer.Ordinal);
            return string.Join(",", enabled);
        }

        private void EnsureLogger()
        {
            _logger ??=
                FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
        }

        private static InputModeKind MapPauseStateToInputMode(
            PauseState state)
        {
            return state switch
            {
                PauseState.Running => InputModeKind.Gameplay,
                PauseState.Paused => InputModeKind.PauseOverlay,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "Pause/InputMode authority requires an explicit Pause state.")
            };
        }

        private readonly struct ResolvedReferences
        {
            internal ResolvedReferences(
                PlayerInput playerInput,
                UnityInputTargetSet targetSet,
                PlayerActorSet playerActorSet,
                LocalPlayerProvisioningValidationResult
                    localPlayerProvisioningValidation,
                UnityInputActionMapEvidence actionMapEvidence,
                InputModeUnityActionMapBinding[] actionMapBindings)
            {
                PlayerInput = playerInput;
                TargetSet = targetSet;
                PlayerActorSet = playerActorSet;
                LocalPlayerProvisioningValidation =
                    localPlayerProvisioningValidation;
                ActionMapEvidence = actionMapEvidence;
                ActionMapBindings = actionMapBindings ??
                    Array.Empty<InputModeUnityActionMapBinding>();
            }

            internal PlayerInput PlayerInput { get; }
            internal UnityInputTargetSet TargetSet { get; }
            internal PlayerActorSet PlayerActorSet { get; }
            internal LocalPlayerProvisioningValidationResult
                LocalPlayerProvisioningValidation { get; }
            internal UnityInputActionMapEvidence ActionMapEvidence { get; }
            internal InputModeUnityActionMapBinding[] ActionMapBindings { get; }
        }
    }
}
