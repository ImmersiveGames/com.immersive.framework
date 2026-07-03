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
    /// API status: Development Tooling. Smoke for F33A opt-in PauseRuntime to Unity PlayerInput wiring.
    /// This validates a scene-authored bridge; it is not automatic FrameworkRuntimeHost wiring.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F33A opt-in Pause runtime PlayerInput bridge smoke.")]
    internal static class PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner
    {
        internal const string SmokeName = "Pause Runtime PlayerInput Bridge Smoke";

        internal static Task<bool> RunRuntimeBridgeSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner));

            try
            {
                using (var fixture = RuntimeBridgeFixture.Create("QA F33A Runtime Bridge", includePlayerMap: true, includeUiMap: true))
                {
                    bool contractsPassed = ValidateContracts(logger, normalizedSource);
                    bool pausePassed = ValidatePauseRequestAppliesUiMap(runtimeHost, logger, normalizedSource, fixture);
                    bool resumePassed = ValidateResumeRequestAppliesPlayerMap(runtimeHost, logger, normalizedSource, fixture);
                    bool ignoredPassed = ValidateAlreadyPausedRequestIgnoredNoSideEffects(runtimeHost, logger, normalizedSource, fixture);
                    bool missingPlayerActorPassed = ValidateMissingPlayerActorPreflightBlocksBeforePauseRequest(runtimeHost, logger, normalizedSource, fixture);
                    bool missingActionMapPassed = ValidateMissingActionMapPreflightBlocksBeforePauseRequest(runtimeHost, logger, normalizedSource, fixture);
                    bool noJoinPassed = ValidateNoPlayerInputManagerJoinOrSpawn(runtimeHost, logger, normalizedSource, fixture);

                    return Task.FromResult(contractsPassed
                        && pausePassed
                        && resumePassed
                        && ignoredPassed
                        && missingPlayerActorPassed
                        && missingActionMapPassed
                        && noJoinPassed);
                }
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Pause Runtime PlayerInput Bridge Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded != PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Unknown
                && PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight != PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPauseRequest;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("frameworkRole", "OptInPauseRuntimePlayerInputBridge"),
                    LogFields.Field("unityAuthority", "PlayerInput"),
                    LogFields.Field("automaticFrameworkRuntimeHostWiring", false),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidatePauseRequestAppliesUiMap(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33a.ensure.running.pause");
            fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: true);

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Pause,
                source,
                "qa.f33a.pause.to.ui");

            bool passed = result.Succeeded
                && result.PauseRequestSubmitted
                && result.PauseStatus == PauseRequestStatus.Applied
                && result.CurrentPauseState == PauseState.Paused
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

            LogBridgeStep(logger, "pause-request-applies-ui-map", passed, result);
            return passed;
        }

        private static bool ValidateResumeRequestAppliesPlayerMap(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Paused, source, "qa.f33a.ensure.paused.resume");
            fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: true);

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Resume,
                source,
                "qa.f33a.resume.to.player");

            bool passed = result.Succeeded
                && result.PauseRequestSubmitted
                && result.PauseStatus == PauseRequestStatus.Applied
                && result.CurrentPauseState == PauseState.Running
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

            LogBridgeStep(logger, "resume-request-applies-player-map", passed, result);
            return passed;
        }

        private static bool ValidateAlreadyPausedRequestIgnoredNoSideEffects(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Paused, source, "qa.f33a.ensure.paused.ignored");
            fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: true);
            fixture.EnsureActionMap("UI");

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Pause,
                source,
                "qa.f33a.pause.ignored");

            bool passed = result.Ignored
                && result.PauseRequestSubmitted
                && result.PauseStatus == PauseRequestStatus.IgnoredNoChange
                && result.CurrentPauseState == PauseState.Paused
                && result.RequestedMode == InputModeKind.PauseOverlay
                && !result.Applied
                && !result.ActivatedPlayerInput
                && !result.SelectedActionMap
                && !result.DeactivatedPlayerInput
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogBridgeStep(logger, "already-paused-request-ignored-no-side-effects", passed, result);
            return passed;
        }

        private static bool ValidateMissingPlayerActorPreflightBlocksBeforePauseRequest(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Paused, source, "qa.f33a.ensure.paused.missing-playeractor");
            fixture.ConfigureValidBridge(includePlayerActor: false, includeUiMap: true);

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Resume,
                source,
                "qa.f33a.resume.missing-playeractor");

            bool stillPaused = runtimeHost.TryGetPauseSnapshot(out PauseSnapshot snapshot) && snapshot.State == PauseState.Paused;
            bool passed = result.Failed
                && result.Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight
                && !result.PauseRequestSubmitted
                && stillPaused
                && result.RequestedMode == InputModeKind.Gameplay
                && !result.Applied
                && !result.ActivatedPlayerInput
                && !result.SelectedActionMap
                && !result.DeactivatedPlayerInput
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogBridgeStep(logger, "missing-playeractor-preflight-blocks-before-pause-request", passed, result);
            return passed;
        }

        private static bool ValidateMissingActionMapPreflightBlocksBeforePauseRequest(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33a.ensure.running.missing-action-map");
            fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: false);

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Pause,
                source,
                "qa.f33a.pause.missing-ui-map");

            bool stillRunning = runtimeHost.TryGetPauseSnapshot(out PauseSnapshot snapshot) && snapshot.State == PauseState.Running;
            bool passed = result.Failed
                && result.Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight
                && !result.PauseRequestSubmitted
                && stillRunning
                && result.RequestedMode == InputModeKind.PauseOverlay
                && !result.Applied
                && !result.ActivatedPlayerInput
                && !result.SelectedActionMap
                && !result.DeactivatedPlayerInput
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior
                && result.PauseRuntimeWiring
                && !result.CallsPlayerJoin
                && !result.SpawnsActor;

            LogBridgeStep(logger, "missing-action-map-preflight-blocks-before-pause-request", passed, result);
            return passed;
        }

        private static bool ValidateNoPlayerInputManagerJoinOrSpawn(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeBridgeFixture fixture)
        {
            EnsurePauseState(runtimeHost, PauseState.Running, source, "qa.f33a.ensure.running.no-join-spawn");
            fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: true);

            PauseInputModeUnityPlayerInputRuntimeBridgeResult result = fixture.Bridge.SubmitForDiagnostics(
                PauseRequestKind.Pause,
                source,
                "qa.f33a.no-join-spawn");

            bool passed = result.Succeeded
                && result.PauseRequestSubmitted
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

        private static void LogBridgeStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("requestKind", result.RequestKind.ToString()),
                    LogFields.Field("previousPauseState", result.PreviousPauseState.ToString()),
                    LogFields.Field("targetPauseState", result.TargetPauseState.ToString()),
                    LogFields.Field("currentPauseState", result.CurrentPauseState.ToString()),
                    LogFields.Field("pauseStatus", result.PauseStatus.ToString()),
                    LogFields.Field("pauseRequestSubmitted", result.PauseRequestSubmitted),
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
                "QA Pause Runtime PlayerInput Bridge Smoke step completed.",
                stepFields);
        }

        private sealed class RuntimeBridgeFixture : IDisposable
        {
            private readonly GameObject _root;
            private readonly GameObject _sessionObject;
            private readonly GameObject _playerObject;
            private readonly GameObject _globalObject;
            private readonly GameObject _bridgeObject;
            private InputActionAsset _actionAsset;

            private RuntimeBridgeFixture(
                GameObject root,
                GameObject sessionObject,
                GameObject playerObject,
                GameObject globalObject,
                GameObject bridgeObject,
                InputActionAsset actionAsset,
                PlayerInput playerInput,
                PlayerActorDeclaration playerActor,
                UnityInputTargetDeclaration gameplayTarget,
                UnityInputTargetDeclaration globalTarget,
                SessionPlayerInputManagerDeclaration sessionManager,
                PauseInputModeUnityPlayerInputRuntimeBridge bridge)
            {
                _root = root;
                _sessionObject = sessionObject;
                _playerObject = playerObject;
                _globalObject = globalObject;
                _bridgeObject = bridgeObject;
                _actionAsset = actionAsset;
                PlayerInput = playerInput;
                PlayerActor = playerActor;
                GameplayTarget = gameplayTarget;
                GlobalTarget = globalTarget;
                SessionManager = sessionManager;
                Bridge = bridge;
            }

            internal PlayerInput PlayerInput { get; }

            internal PlayerActorDeclaration PlayerActor { get; }

            internal UnityInputTargetDeclaration GameplayTarget { get; }

            internal UnityInputTargetDeclaration GlobalTarget { get; }

            internal SessionPlayerInputManagerDeclaration SessionManager { get; }

            internal PauseInputModeUnityPlayerInputRuntimeBridge Bridge { get; }

            internal static RuntimeBridgeFixture Create(string name, bool includePlayerMap, bool includeUiMap)
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
                    "qa.f33a.session.manager");

                GameObject playerObject = new GameObject(name + " PlayerActor");
                playerObject.transform.SetParent(root.transform, false);
                InputActionAsset actionAsset = CreateActionAsset(includePlayerMap, includeUiMap);
                PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = includePlayerMap ? "Player" : includeUiMap ? "UI" : string.Empty;
                PlayerActorDeclaration playerActor = playerObject.AddComponent<PlayerActorDeclaration>();
                playerActor.ConfigureForDiagnostics(
                    "qa.actor.player.primary",
                    "QA PlayerActor Primary",
                    playerInput,
                    "qa.f33a.playeractor");
                UnityInputTargetDeclaration gameplayTarget = playerObject.AddComponent<UnityInputTargetDeclaration>();
                gameplayTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.input.target.gameplay-commands",
                    "QA Gameplay Commands Target",
                    playerInput,
                    "qa.f33a.gameplay.target",
                    true);

                GameObject globalObject = new GameObject(name + " GlobalUiPause Target");
                globalObject.transform.SetParent(root.transform, false);
                UnityInputTargetDeclaration globalTarget = globalObject.AddComponent<UnityInputTargetDeclaration>();
                globalTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GlobalUiPause,
                    "qa.input.target.global-ui-pause",
                    "QA Global UI Pause Target",
                    null,
                    "qa.f33a.global.target");

                GameObject bridgeObject = new GameObject(name + " Bridge");
                bridgeObject.transform.SetParent(root.transform, false);
                PauseInputModeUnityPlayerInputRuntimeBridge bridge = bridgeObject.AddComponent<PauseInputModeUnityPlayerInputRuntimeBridge>();
                var fixture = new RuntimeBridgeFixture(
                    root,
                    sessionObject,
                    playerObject,
                    globalObject,
                    bridgeObject,
                    actionAsset,
                    playerInput,
                    playerActor,
                    gameplayTarget,
                    globalTarget,
                    sessionDeclaration,
                    bridge);
                fixture.ConfigureValidBridge(includePlayerActor: true, includeUiMap: includeUiMap);
                return fixture;
            }

            internal void ConfigureValidBridge(bool includePlayerActor, bool includeUiMap)
            {
                if (includeUiMap != HasActionMap("UI"))
                {
                    ReplaceActionAsset(includePlayerMap: true, includeUiMap: includeUiMap);
                }

                Bridge.ConfigureForDiagnostics(
                    PlayerInput,
                    new[] { GlobalTarget, GameplayTarget },
                    includePlayerActor ? new[] { PlayerActor } : Array.Empty<PlayerActorDeclaration>(),
                    new[] { SessionManager },
                    "Player",
                    "UI",
                    false,
                    true);
            }

            internal void EnsureActionMap(string mapName)
            {
                PlayerInput.ActivateInput();
                PlayerInput.SwitchCurrentActionMap(mapName);
            }

            public void Dispose()
            {
                DestroyObject(_root);
                DestroyObject(_actionAsset);
            }

            private void ReplaceActionAsset(bool includePlayerMap, bool includeUiMap)
            {
                InputActionAsset previous = _actionAsset;
                _actionAsset = CreateActionAsset(includePlayerMap, includeUiMap);
                PlayerInput.actions = _actionAsset;
                PlayerInput.defaultActionMap = includePlayerMap ? "Player" : includeUiMap ? "UI" : string.Empty;
                DestroyObject(previous);
            }

            private bool HasActionMap(string mapName)
            {
                return PlayerInput.actions != null && PlayerInput.actions.FindActionMap(mapName, false) != null;
            }

            private static InputActionAsset CreateActionAsset(bool includePlayerMap, bool includeUiMap)
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
