using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F33B opt-in Unity InputAction trigger into the Pause runtime PlayerInput bridge.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F33B Pause InputAction runtime bridge trigger smoke.")]
    internal static class PauseInputActionRuntimeBridgeTriggerQaSmokeRunner
    {
        internal const string SmokeName = "Pause InputAction Runtime Bridge Trigger Smoke";

        internal static Task<bool> RunTriggerSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseInputActionRuntimeBridgeTriggerQaSmokeRunner));

            try
            {
                using (var fixture = TriggerFixture.Create("QA F33B InputAction Trigger", includePlayerMap: true, includeUiMap: true, includePauseAction: true))
                {
                    bool contractsPassed = ValidateContracts(logger, normalizedSource);
                    bool evidencePassed = ValidateActionEvidenceValidNoSideEffect(logger, normalizedSource, fixture);
                    bool pausePassed = ValidatePauseActionToggleAppliesUiMap(runtimeHost, logger, normalizedSource, fixture);
                    bool resumePassed = ValidatePauseActionToggleResumesPlayerMap(runtimeHost, logger, normalizedSource, fixture);
                    bool missingActionPassed = ValidateMissingPauseActionBlocksBeforeBridge(runtimeHost, logger, normalizedSource, fixture);
                    bool missingBridgePassed = ValidateMissingBridgeBlocking(logger, normalizedSource, fixture);
                    bool noJoinPassed = ValidateNoPlayerInputManagerJoinOrSpawn(runtimeHost, logger, normalizedSource, fixture);

                    return Task.FromResult(contractsPassed
                        && evidencePassed
                        && pausePassed
                        && resumePassed
                        && missingActionPassed
                        && missingBridgePassed
                        && noJoinPassed);
                }
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Pause InputAction Runtime Bridge Trigger Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = PauseInputActionRuntimeBridgeTriggerStatus.Succeeded != PauseInputActionRuntimeBridgeTriggerStatus.Unknown
                && PauseInputActionRuntimeBridgeTriggerStatus.FailedActionEvidence != PauseInputActionRuntimeBridgeTriggerStatus.FailedBridgeMissing;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("frameworkRole", "OptInUnityInputActionPauseTrigger"),
                    LogFields.Field("unityAuthority", "PlayerInput/InputAction"),
                    LogFields.Field("automaticFrameworkRuntimeHostWiring", false),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidateActionEvidenceValidNoSideEffect(
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            fixture.ConfigureValidTrigger(includePauseAction: true, includeBridge: true);
            bool resolved = fixture.Trigger.TryResolveConfiguredActionForDiagnostics(out string message);
            bool passed = resolved
                && string.IsNullOrWhiteSpace(message);

            LogStep(
                logger,
                "pause-action-evidence-valid-no-side-effect",
                passed,
                LogFields.Of(
                    LogFields.Field("actionMap", "UI"),
                    LogFields.Field("action", "Pause"),
                    LogFields.Field("actionResolved", resolved),
                    LogFields.Field("message", message.NormalizeText()),
                    LogFields.Field("bridgeSubmitted", false),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidatePauseActionToggleAppliesUiMap(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33b.ensure.running.pause");
            fixture.ConfigureValidTrigger(includePauseAction: true, includeBridge: true);

            PauseInputActionRuntimeBridgeTriggerResult result = fixture.Trigger.SubmitForDiagnostics(source, "qa.f33b.pause-action.toggle.to-ui");

            bool passed = result.Succeeded
                && result.ActionResolved
                && result.BridgeSubmitted
                && result.RequestKind == PauseRequestKind.Toggle
                && result.RequestedMode == InputModeKind.PauseOverlay
                && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                && result.AppliedActionMapName.ToString() == "UI"
                && result.ActivatedPlayerInput
                && result.SelectedActionMap
                && fixture.PlayerInput.currentActionMap is { name: "UI" }
                && result.SwitchesActionMaps
                && result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogTriggerStep(logger, "pause-action-toggle-applies-ui-map", passed, result);
            return passed;
        }

        private static bool ValidatePauseActionToggleResumesPlayerMap(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Paused, source, "qa.f33b.ensure.paused.resume");
            fixture.ConfigureValidTrigger(includePauseAction: true, includeBridge: true);

            PauseInputActionRuntimeBridgeTriggerResult result = fixture.Trigger.SubmitForDiagnostics(source, "qa.f33b.pause-action.toggle.to-player");

            bool passed = result.Succeeded
                && result.ActionResolved
                && result.BridgeSubmitted
                && result.RequestKind == PauseRequestKind.Toggle
                && result.RequestedMode == InputModeKind.Gameplay
                && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                && result.AppliedActionMapName.ToString() == "Player"
                && result.ActivatedPlayerInput
                && result.SelectedActionMap
                && fixture.PlayerInput.currentActionMap is { name: "Player" }
                && result.SwitchesActionMaps
                && result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogTriggerStep(logger, "pause-action-toggle-resumes-player-map", passed, result);
            return passed;
        }

        private static bool ValidateMissingPauseActionBlocksBeforeBridge(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33b.ensure.running.missing-action");
            fixture.ConfigureValidTrigger(includePauseAction: false, includeBridge: true);

            PauseInputActionRuntimeBridgeTriggerResult result = fixture.Trigger.SubmitForDiagnostics(source, "qa.f33b.pause-action.missing");
            bool stillRunning = runtimeHost.TryGetPauseSnapshot(out PauseSnapshot snapshot) && snapshot.State == PauseState.Running;

            bool passed = result.Failed
                && result.Status == PauseInputActionRuntimeBridgeTriggerStatus.FailedActionEvidence
                && !result.ActionResolved
                && !result.BridgeSubmitted
                && stillRunning
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogTriggerStep(logger, "missing-pause-action-blocks-before-bridge", passed, result);
            return passed;
        }

        private static bool ValidateMissingBridgeBlocking(
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            fixture.ConfigureValidTrigger(includePauseAction: true, includeBridge: false);

            PauseInputActionRuntimeBridgeTriggerResult result = fixture.Trigger.SubmitForDiagnostics(source, "qa.f33b.missing-bridge");

            bool passed = result.Failed
                && result.Status == PauseInputActionRuntimeBridgeTriggerStatus.FailedBridgeMissing
                && result.ActionResolved == false
                && !result.BridgeSubmitted
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogTriggerStep(logger, "missing-bridge-blocking", passed, result);
            return passed;
        }

        private static bool ValidateNoPlayerInputManagerJoinOrSpawn(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            TriggerFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33b.ensure.running.no-join-spawn");
            fixture.ConfigureValidTrigger(includePauseAction: true, includeBridge: true);

            PauseInputActionRuntimeBridgeTriggerResult result = fixture.Trigger.SubmitForDiagnostics(source, "qa.f33b.no-join-spawn");

            bool passed = result.Succeeded
                && result.BridgeSubmitted
                && result.ActivatedPlayerInput
                && result.SwitchesActionMaps
                && result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor
                && !result.UsesCustomInputManager;

            LogStep(
                logger,
                "no-playerinputmanager-join-or-spawn",
                passed,
                LogFields.Of(
                    LogFields.Field("requestKind", result.RequestKind.ToString()),
                    LogFields.Field("actionMap", result.ActionMapName),
                    LogFields.Field("action", result.ActionName),
                    LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                    LogFields.Field("operation", result.Operation.ToString()),
                    LogFields.Field("activatedPlayerInput", result.ActivatedPlayerInput),
                    LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                    LogFields.Field("pauseRuntimeWiring", result.PauseRuntimeWiring),
                    LogFields.Field("playerJoin", result.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", result.SpawnsActor),
                    LogFields.Field("customInputManager", result.UsesCustomInputManager)));

            return passed;
        }

        private static void EnsurePauseState(
            FrameworkRuntimeHost runtimeHost,
            PauseState targetState,
            string source,
            string reason)
        {
            if (!runtimeHost.TryGetPauseSnapshot(out PauseSnapshot snapshot))
            {
                return;
            }

            if (snapshot.State == targetState)
            {
                return;
            }

            PauseRequestKind kind = targetState == PauseState.Paused ? PauseRequestKind.Pause : PauseRequestKind.Resume;
            runtimeHost.RequestPause(CreatePauseRequest(kind, reason, source, reason));
        }

        private static PauseRequest CreatePauseRequest(PauseRequestKind kind, string requestId, string source, string reason)
        {
            switch (kind)
            {
                case PauseRequestKind.Pause:
                    return PauseRequest.Pause(requestId, source, reason);
                case PauseRequestKind.Resume:
                    return PauseRequest.Resume(requestId, source, reason);
                case PauseRequestKind.Toggle:
                    return PauseRequest.Toggle(requestId, source, reason);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause request kind must be explicit.");
            }
        }

        private static void LogTriggerStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            PauseInputActionRuntimeBridgeTriggerResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("requestKind", result.RequestKind.ToString()),
                    LogFields.Field("actionMap", result.ActionMapName),
                    LogFields.Field("action", result.ActionName),
                    LogFields.Field("actionEvidenceRequired", result.ActionEvidenceRequired),
                    LogFields.Field("actionResolved", result.ActionResolved),
                    LogFields.Field("bridgeSubmitted", result.BridgeSubmitted),
                    LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                    LogFields.Field("operation", result.Operation.ToString()),
                    LogFields.Field("appliedActionMap", result.AppliedActionMapName.ToString()),
                    LogFields.Field("applied", result.Applied),
                    LogFields.Field("activatedPlayerInput", result.ActivatedPlayerInput),
                    LogFields.Field("selectedActionMap", result.SelectedActionMap),
                    LogFields.Field("deactivatedPlayerInput", result.DeactivatedPlayerInput),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                    LogFields.Field("pauseRuntimeWiring", result.PauseRuntimeWiring),
                    LogFields.Field("playerJoin", result.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", result.SpawnsActor),
                    LogFields.Field("customInputManager", result.UsesCustomInputManager),
                    LogFields.Field("diagnostics", result.ToDiagnosticString())));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            int fieldCount = fields == null ? 0 : fields.Length;
            var stepFields = new LogField[fieldCount + 2];
            stepFields[0] = LogFields.Field("step", step);
            stepFields[1] = LogFields.Field("passed", passed);

            if (fieldCount > 0)
            {
                Array.Copy(fields, 0, stepFields, 2, fieldCount);
            }

            logger.Info(
                "QA Pause InputAction Runtime Bridge Trigger Smoke step completed.",
                stepFields);
        }

        private sealed class TriggerFixture : IDisposable
        {
            private readonly GameObject _root;
            private readonly GameObject _sessionObject;
            private readonly GameObject _playerObject;
            private readonly GameObject _globalObject;
            private readonly GameObject _bridgeObject;
            private readonly GameObject _triggerObject;
            private InputActionAsset _actionAsset;

            private TriggerFixture(
                GameObject root,
                GameObject sessionObject,
                GameObject playerObject,
                GameObject globalObject,
                GameObject bridgeObject,
                GameObject triggerObject,
                InputActionAsset actionAsset,
                PlayerInput playerInput,
                PlayerActorDeclaration playerActor,
                UnityInputTargetDeclaration gameplayTarget,
                UnityInputTargetDeclaration globalTarget,
                SessionPlayerInputManagerDeclaration sessionManager,
                PauseInputModeUnityPlayerInputRuntimeBridge bridge,
                PauseInputActionRuntimeBridgeTrigger trigger)
            {
                _root = root;
                _sessionObject = sessionObject;
                _playerObject = playerObject;
                _globalObject = globalObject;
                _bridgeObject = bridgeObject;
                _triggerObject = triggerObject;
                _actionAsset = actionAsset;
                PlayerInput = playerInput;
                PlayerActor = playerActor;
                GameplayTarget = gameplayTarget;
                GlobalTarget = globalTarget;
                SessionManager = sessionManager;
                Bridge = bridge;
                Trigger = trigger;
            }

            internal PlayerInput PlayerInput { get; }

            internal PlayerActorDeclaration PlayerActor { get; }

            internal UnityInputTargetDeclaration GameplayTarget { get; }

            internal UnityInputTargetDeclaration GlobalTarget { get; }

            internal SessionPlayerInputManagerDeclaration SessionManager { get; }

            internal PauseInputModeUnityPlayerInputRuntimeBridge Bridge { get; }

            internal PauseInputActionRuntimeBridgeTrigger Trigger { get; }

            internal static TriggerFixture Create(string name, bool includePlayerMap, bool includeUiMap, bool includePauseAction)
            {
                GameObject root = new GameObject(name);
                GameObject sessionObject = new GameObject(name + " Session PlayerInputManager");
                sessionObject.transform.SetParent(root.transform, false);
                sessionObject.SetActive(false);
                PlayerInputManager manager = sessionObject.AddComponent<PlayerInputManager>();
                SessionPlayerInputManagerDeclaration sessionDeclaration = sessionObject.AddComponent<SessionPlayerInputManagerDeclaration>();
                sessionDeclaration.ConfigureForDiagnostics(
                    "qa.session.player-input-manager",
                    "QA Session PlayerInputManager",
                    manager,
                    "qa.f33b.session.manager");

                GameObject playerObject = new GameObject(name + " PlayerActor");
                playerObject.transform.SetParent(root.transform, false);
                InputActionAsset actionAsset = CreateActionAsset(includePlayerMap, includeUiMap, includePauseAction);
                PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = includePlayerMap ? "Player" : includeUiMap ? "UI" : string.Empty;
                PlayerActorDeclaration playerActor = playerObject.AddComponent<PlayerActorDeclaration>();
                playerActor.ConfigureForDiagnostics(
                    "qa.actor.player.primary",
                    "QA PlayerActor Primary",
                    playerInput,
                    "qa.f33b.playeractor");
                UnityInputTargetDeclaration gameplayTarget = playerObject.AddComponent<UnityInputTargetDeclaration>();
                gameplayTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.input.target.gameplay-commands",
                    "QA Gameplay Commands Target",
                    playerInput,
                    "qa.f33b.gameplay.target",
                    true);

                GameObject globalObject = new GameObject(name + " GlobalUiPause Target");
                globalObject.transform.SetParent(root.transform, false);
                UnityInputTargetDeclaration globalTarget = globalObject.AddComponent<UnityInputTargetDeclaration>();
                globalTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GlobalUiPause,
                    "qa.input.target.global-ui-pause",
                    "QA Global UI Pause Target",
                    null,
                    "qa.f33b.global.target");

                GameObject bridgeObject = new GameObject(name + " Bridge");
                bridgeObject.transform.SetParent(root.transform, false);
                PauseInputModeUnityPlayerInputRuntimeBridge bridge = bridgeObject.AddComponent<PauseInputModeUnityPlayerInputRuntimeBridge>();

                GameObject triggerObject = new GameObject(name + " InputAction Trigger");
                triggerObject.transform.SetParent(root.transform, false);
                PauseInputActionRuntimeBridgeTrigger trigger = triggerObject.AddComponent<PauseInputActionRuntimeBridgeTrigger>();

                var fixture = new TriggerFixture(
                    root,
                    sessionObject,
                    playerObject,
                    globalObject,
                    bridgeObject,
                    triggerObject,
                    actionAsset,
                    playerInput,
                    playerActor,
                    gameplayTarget,
                    globalTarget,
                    sessionDeclaration,
                    bridge,
                    trigger);
                fixture.ConfigureValidTrigger(includePauseAction: includePauseAction, includeBridge: true);
                return fixture;
            }

            internal void ConfigureValidTrigger(bool includePauseAction, bool includeBridge)
            {
                if (includePauseAction != HasAction("UI", "Pause"))
                {
                    ReplaceActionAsset(includePlayerMap: true, includeUiMap: true, includePauseAction: includePauseAction);
                }

                Bridge.ConfigureForDiagnostics(
                    PlayerInput,
                    new[] { GlobalTarget, GameplayTarget },
                    new[] { PlayerActor },
                    new[] { SessionManager },
                    "Player",
                    "UI",
                    false,
                    true);

                Trigger.ConfigureForDiagnostics(
                    includeBridge ? Bridge : null,
                    PlayerInput,
                    "UI",
                    "Pause",
                    PauseRequestKind.Toggle,
                    true,
                    false,
                    false);
            }

            public void Dispose()
            {
                DestroyObject(_root);
                DestroyObject(_actionAsset);
            }

            private void ReplaceActionAsset(bool includePlayerMap, bool includeUiMap, bool includePauseAction)
            {
                InputActionAsset previous = _actionAsset;
                _actionAsset = CreateActionAsset(includePlayerMap, includeUiMap, includePauseAction);
                PlayerInput.actions = _actionAsset;
                PlayerInput.defaultActionMap = includePlayerMap ? "Player" : includeUiMap ? "UI" : string.Empty;
                DestroyObject(previous);
            }

            private bool HasAction(string mapName, string actionName)
            {
                if (PlayerInput.actions == null)
                {
                    return false;
                }

                InputActionMap map = PlayerInput.actions.FindActionMap(mapName, false);
                return map != null && map.FindAction(actionName, false) != null;
            }

            private static InputActionAsset CreateActionAsset(bool includePlayerMap, bool includeUiMap, bool includePauseAction)
            {
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                if (includePlayerMap)
                {
                    InputActionMap playerMap = new InputActionMap("Player");
                    playerMap.AddAction("Move", InputActionType.Value);
                    asset.AddActionMap(playerMap);
                }

                if (includeUiMap)
                {
                    InputActionMap uiMap = new InputActionMap("UI");
                    uiMap.AddAction("Submit", InputActionType.Button);
                    if (includePauseAction)
                    {
                        uiMap.AddAction("Pause", InputActionType.Button);
                    }
                    asset.AddActionMap(uiMap);
                }

                return asset;
            }

            private static void DestroyObject(UnityEngine.Object target)
            {
                if (target == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(target);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }
    }
}
#endif
