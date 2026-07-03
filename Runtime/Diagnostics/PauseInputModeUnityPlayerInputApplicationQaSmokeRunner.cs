using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F32G completed Pause result to explicit PlayerInput application.
    /// This bridge is explicit and QA-facing; it is not wired automatically into PauseRuntime or FrameworkRuntimeHost.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32G Pause result PlayerInput application smoke.")]
    internal static class PauseInputModeUnityPlayerInputApplicationQaSmokeRunner
    {
        internal const string SmokeName = "Pause InputMode Unity PlayerInput Application Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseInputModeUnityPlayerInputApplicationQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool pausePassed = ValidatePauseResultAppliesUiMap(logger, normalizedSource);
                bool resumePassed = ValidateResumeResultAppliesPlayerMap(logger, normalizedSource);
                bool ignoredPassed = ValidateIgnoredPauseResultCurrentModeNoSideEffects(logger, normalizedSource);
                bool rejectedPassed = ValidateRejectedPauseResultBlocksBeforeApplication(logger, normalizedSource);
                bool missingPlayerActorPassed = ValidateMissingPlayerActorBlocksBeforeApplication(logger, normalizedSource);
                bool noJoinPassed = ValidateNoPlayerInputManagerJoinOrSpawn(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && pausePassed
                    && resumePassed
                    && ignoredPassed
                    && rejectedPassed
                    && missingPlayerActorPassed
                    && noJoinPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Pause InputMode Unity PlayerInput Application Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = PauseInputModeUnityPlayerInputApplicationStatus.Succeeded != PauseInputModeUnityPlayerInputApplicationStatus.Unknown
                && PauseInputModeUnityPlayerInputApplicationStatus.FailedPauseResultNotCompleted != PauseInputModeUnityPlayerInputApplicationStatus.FailedInputModePlayerInputApplication;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("frameworkRole", "ExplicitPauseToPlayerInputBridge"),
                    LogFields.Field("unityAuthority", "PlayerInput"),
                    LogFields.Field("pauseRuntimeWiring", false),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidatePauseResultAppliesUiMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G Pause Result", "Player", true))
            {
                PauseResult pauseResult = PauseResult.AppliedResult(
                    PauseRequest.Pause("qa.pause.inputmode.application.pause", source, "qa.pause.inputmode.application.pause"),
                    PauseState.Running,
                    PauseState.Paused,
                    "QA pause result applied.");

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.pause",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.PauseState == PauseState.Paused
                    && result.PauseStatus == PauseRequestStatus.Applied
                    && result.RequestedMode == InputModeKind.PauseOverlay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.AppliedActionMapName.ToString() == "UI"
                    && result.ActivatedPlayerInput
                    && result.SelectedActionMap
                    && fixture.PlayerInput.currentActionMap is { name: "UI" }
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "pause-result-applies-ui-map", passed, result);
                return passed;
            }
        }

        private static bool ValidateResumeResultAppliesPlayerMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G Resume Result", "UI", true))
            {
                PauseResult pauseResult = PauseResult.AppliedResult(
                    PauseRequest.Resume("qa.pause.inputmode.application.resume", source, "qa.pause.inputmode.application.resume"),
                    PauseState.Paused,
                    PauseState.Running,
                    "QA resume result applied.");

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.PauseOverlay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.resume",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.PauseState == PauseState.Running
                    && result.PauseStatus == PauseRequestStatus.Applied
                    && result.RequestedMode == InputModeKind.Gameplay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.AppliedActionMapName.ToString() == "Player"
                    && result.ActivatedPlayerInput
                    && result.SelectedActionMap
                    && fixture.PlayerInput.currentActionMap is { name: "Player" }
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "resume-result-applies-player-map", passed, result);
                return passed;
            }
        }

        private static bool ValidateIgnoredPauseResultCurrentModeNoSideEffects(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G Ignored Pause Result", "UI", true))
            {
                PauseResult pauseResult = PauseResult.IgnoredNoChangeResult(
                    PauseRequest.Pause("qa.pause.inputmode.application.ignored", source, "qa.pause.inputmode.application.ignored"),
                    PauseState.Paused,
                    "QA pause result ignored because state did not change.");

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.PauseOverlay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.ignored",
                    true,
                    true);

                bool passed = result.Ignored
                    && result.Status == PauseInputModeUnityPlayerInputApplicationStatus.IgnoredInputModeRequest
                    && result.PauseStatus == PauseRequestStatus.IgnoredNoChange
                    && result.RequestedMode == InputModeKind.PauseOverlay
                    && !result.Applied
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.DeactivatedPlayerInput
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "ignored-pause-result-current-mode-no-side-effects", passed, result);
                return passed;
            }
        }

        private static bool ValidateRejectedPauseResultBlocksBeforeApplication(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G Rejected Pause Result", "Player", true))
            {
                PauseResult pauseResult = PauseResult.RejectedResult(
                    PauseRequest.Pause("qa.pause.inputmode.application.rejected", source, "qa.pause.inputmode.application.rejected"),
                    PauseState.Running,
                    "QA pause result rejected.",
                    new[] { PauseIssue.Blocking("qa.pause.rejected", source, "qa.pause.inputmode.application.rejected", "Rejected pause result must not apply input mode.") });

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.rejected",
                    true,
                    true);

                bool passed = result.Failed
                    && result.Status == PauseInputModeUnityPlayerInputApplicationStatus.FailedPauseResultNotCompleted
                    && result.PauseStatus == PauseRequestStatus.Rejected
                    && result.BlockingIssueCount > 0
                    && !result.Applied
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.DeactivatedPlayerInput
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "rejected-pause-result-blocks-before-application", passed, result);
                return passed;
            }
        }

        private static bool ValidateMissingPlayerActorBlocksBeforeApplication(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G Missing PlayerActor", "UI", true))
            {
                PauseResult pauseResult = PauseResult.AppliedResult(
                    PauseRequest.Resume("qa.pause.inputmode.application.missing-playeractor", source, "qa.pause.inputmode.application.missing-playeractor"),
                    PauseState.Paused,
                    PauseState.Running,
                    "QA resume result applied.");

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.PauseOverlay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.missing-playeractor",
                    false,
                    true);

                bool passed = result.Failed
                    && result.Status == PauseInputModeUnityPlayerInputApplicationStatus.FailedInputModePlayerInputApplication
                    && result.BlockingIssueCount > 0
                    && !result.Applied
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.DeactivatedPlayerInput
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "missing-playeractor-blocks-before-application", passed, result);
                return passed;
            }
        }

        private static bool ValidateNoPlayerInputManagerJoinOrSpawn(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32G No Join Spawn", "Player", true))
            {
                PauseResult pauseResult = PauseResult.AppliedResult(
                    PauseRequest.Pause("qa.pause.inputmode.application.no-join-spawn", source, "qa.pause.inputmode.application.no-join-spawn"),
                    PauseState.Running,
                    PauseState.Paused,
                    "QA pause result applied.");

                PauseInputModeUnityPlayerInputApplicationResult result = ApplyPauseResult(
                    pauseResult,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.pause.inputmode.application.no-join-spawn",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.ActivatedPlayerInput
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogStep(
                    logger,
                    "no-playerinputmanager-join-or-spawn",
                    passed,
                    LogFields.Of(
                        LogFields.Field("pauseState", result.PauseState.ToString()),
                        LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                        LogFields.Field("operation", result.Operation.ToString()),
                        LogFields.Field("activatedPlayerInput", result.ActivatedPlayerInput),
                        LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                        LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                        LogFields.Field("playerJoin", result.CallsPlayerJoin),
                        LogFields.Field("actorSpawning", result.SpawnsActor),
                        LogFields.Field("pauseRuntimeWiring", false),
                        LogFields.Field("customInputManager", result.UsesCustomInputManager)));

                return passed;
            }
        }

        private static PauseInputModeUnityPlayerInputApplicationResult ApplyPauseResult(
            PauseResult pauseResult,
            InputModeKind currentMode,
            PlayerInput playerInput,
            string source,
            string reason,
            bool includePlayerActor,
            bool includePlayerActionMap)
        {
            InputModeState currentState = new InputModeState(
                InputModeDefinitions.FromKind(currentMode, source, reason),
                currentMode == InputModeKind.Gameplay ? 0 : 1,
                source,
                reason);

            return PauseInputModeUnityPlayerInputApplication.Apply(
                pauseResult,
                currentState,
                ValidTargetSet(source),
                includePlayerActor ? ValidPlayerActorSet(source) : EmptyPlayerActorSet(source),
                UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(1, source, reason),
                includePlayerActionMap
                    ? UnityInputActionMapEvidence.FromInputActionAsset(playerInput.actions, source, reason)
                    : UnityInputActionMapEvidence.FromActionMapNames(new[] { "UI" }, source, reason),
                InputModeUnityActionMapBindings.CanonicalPlayerUi(),
                playerInput,
                source,
                reason);
        }

        private static UnityInputTargetSet ValidTargetSet(string source)
        {
            return UnityInputTargetSet.FromDescriptors(
                new[]
                {
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.global-ui-pause"),
                        UnityInputTargetRole.GlobalUiPause,
                        true,
                        false,
                        "QA Global UI/Pause Target",
                        "Synthetic",
                        "qa.input.target.global-ui-pause",
                        source,
                        "qa.pause.inputmode.application.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.pause.inputmode.application.targets")
                },
                source,
                "qa.pause.inputmode.application.targets");
        }

        private static PlayerActorSet ValidPlayerActorSet(string source)
        {
            return PlayerActorSet.FromDescriptors(
                new[]
                {
                    new PlayerActorDescriptor(
                        ActorId.From("qa.actor.player.primary"),
                        true,
                        "QA PlayerActor Primary",
                        "Synthetic",
                        "qa.actor.player.primary",
                        source,
                        "qa.pause.inputmode.application.playeractor")
                },
                source,
                "qa.pause.inputmode.application.playeractor");
        }

        private static PlayerActorSet EmptyPlayerActorSet(string source)
        {
            return PlayerActorSet.FromDescriptors(
                Array.Empty<PlayerActorDescriptor>(),
                source,
                "qa.pause.inputmode.application.playeractor.empty");
        }

        private static void LogApplicationStep(FrameworkLogger logger, string step, bool passed, PauseInputModeUnityPlayerInputApplicationResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("pauseStatus", result.PauseStatus.ToString()),
                    LogFields.Field("pauseState", result.PauseState.ToString()),
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
                    LogFields.Field("playerJoin", result.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", result.SpawnsActor),
                    LogFields.Field("pauseRuntimeWiring", false),
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
                "QA Pause InputMode Unity PlayerInput Application Smoke step completed.",
                stepFields);
        }

        private sealed class PlayerInputFixture : IDisposable
        {
            private readonly GameObject _gameObject;
            private readonly InputActionAsset _actionAsset;

            private PlayerInputFixture(GameObject gameObject, InputActionAsset actionAsset, PlayerInput playerInput)
            {
                _gameObject = gameObject;
                _actionAsset = actionAsset;
                PlayerInput = playerInput;
            }

            internal PlayerInput PlayerInput { get; }

            internal static PlayerInputFixture Create(string name, string defaultActionMap, bool includePlayerMap)
            {
                var gameObject = new GameObject(name);
                InputActionAsset actionAsset = CreateActionAsset(includePlayerMap);
                PlayerInput playerInput = gameObject.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = defaultActionMap.NormalizeText();

                return new PlayerInputFixture(gameObject, actionAsset, playerInput);
            }

            public void Dispose()
            {
                if (_gameObject != null)
                {
                    UnityEngine.Object.Destroy(_gameObject);
                }

                if (_actionAsset != null)
                {
                    UnityEngine.Object.Destroy(_actionAsset);
                }
            }

            private static InputActionAsset CreateActionAsset(bool includePlayerMap)
            {
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                if (includePlayerMap)
                {
                    InputActionMap playerMap = new InputActionMap("Player");
                    playerMap.AddAction("Move", InputActionType.Value);
                    asset.AddActionMap(playerMap);
                }

                InputActionMap uiMap = new InputActionMap("UI");
                uiMap.AddAction("Submit", InputActionType.Button);
                asset.AddActionMap(uiMap);

                return asset;
            }
        }
    }
}
#endif
