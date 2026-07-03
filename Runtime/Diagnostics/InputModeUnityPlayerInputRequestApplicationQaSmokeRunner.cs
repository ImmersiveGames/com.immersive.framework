using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.InputMode;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F32F end-to-end InputMode request to explicit PlayerInput application.
    /// This is the first composed path from logical request to Unity PlayerInput side effect, still without PlayerInputManager join/spawn or movement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32F InputMode request PlayerInput application smoke.")]
    internal static class InputModeUnityPlayerInputRequestApplicationQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Unity PlayerInput Request Application Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputRequestApplicationQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool gameplayPassed = ValidateGameplayRequestAppliesPlayerMap(logger, normalizedSource);
                bool pausePassed = ValidatePauseOverlayRequestAppliesUiMap(logger, normalizedSource);
                bool lockPassed = ValidateInputLockedRequestDeactivatesPlayerInput(logger, normalizedSource);
                bool ignoredPassed = ValidateAlreadyInModeIgnoredNoSideEffects(logger, normalizedSource);
                bool missingPlayerActorPassed = ValidateMissingPlayerActorBlocksBeforeApplication(logger, normalizedSource);
                bool missingMapPassed = ValidateMissingActionMapBlocksBeforeApplication(logger, normalizedSource);
                bool noJoinPassed = ValidateNoPlayerInputManagerJoinOrSpawn(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && gameplayPassed
                    && pausePassed
                    && lockPassed
                    && ignoredPassed
                    && missingPlayerActorPassed
                    && missingMapPassed
                    && noJoinPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Unity PlayerInput Request Application Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = InputModeUnityPlayerInputRequestApplicationStatus.Succeeded != InputModeUnityPlayerInputRequestApplicationStatus.Unknown
                && InputModeUnityPlayerInputRequestApplicationStatus.IgnoredInputModeRequest != InputModeUnityPlayerInputRequestApplicationStatus.FailedInputModeRequest;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("frameworkRole", "ComposedExplicitUnityPlayerInputApplication"),
                    LogFields.Field("unityAuthority", "PlayerInput"),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidateGameplayRequestAppliesPlayerMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F Gameplay Request", "UI", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.PauseOverlay,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.gameplay",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.Gameplay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.ActivatedPlayerInput
                    && result.SelectedActionMap
                    && result.AppliedActionMapName.ToString() == "Player"
                    && fixture.PlayerInput.currentActionMap is { name: "Player" }
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogApplicationStep(logger, "gameplay-request-applies-player-map", passed, result);
                return passed;
            }
        }

        private static bool ValidatePauseOverlayRequestAppliesUiMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F PauseOverlay Request", "Player", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.Gameplay,
                    InputModeKind.PauseOverlay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.pause-overlay",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.PauseOverlay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.ActivatedPlayerInput
                    && result.SelectedActionMap
                    && result.AppliedActionMapName.ToString() == "UI"
                    && fixture.PlayerInput.currentActionMap is { name: "UI" }
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogApplicationStep(logger, "pause-overlay-request-applies-ui-map", passed, result);
                return passed;
            }
        }

        private static bool ValidateInputLockedRequestDeactivatesPlayerInput(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F InputLocked Request", "Player", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.Gameplay,
                    InputModeKind.InputLocked,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.input-locked",
                    true,
                    true);

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.InputLocked
                    && result.Operation == InputModeUnityApplicationPlanOperation.LockInput
                    && result.DeactivatedPlayerInput
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogApplicationStep(logger, "inputlocked-request-deactivates-playerinput", passed, result);
                return passed;
            }
        }

        private static bool ValidateAlreadyInModeIgnoredNoSideEffects(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F Already Gameplay", "Player", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.Gameplay,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.ignored",
                    true,
                    true);

                bool passed = result.Ignored
                    && result.Status == InputModeUnityPlayerInputRequestApplicationStatus.IgnoredInputModeRequest
                    && !result.Applied
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.DeactivatedPlayerInput
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "already-in-mode-ignored-no-side-effects", passed, result);
                return passed;
            }
        }

        private static bool ValidateMissingPlayerActorBlocksBeforeApplication(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F Missing PlayerActor", "UI", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.PauseOverlay,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.missing-playeractor",
                    false,
                    true);

                bool passed = result.Failed
                    && result.Status == InputModeUnityPlayerInputRequestApplicationStatus.FailedUnityApplicationPreview
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

        private static bool ValidateMissingActionMapBlocksBeforeApplication(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F Missing Action Map", "UI", false))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.PauseOverlay,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.missing-action-map",
                    true,
                    false);

                bool passed = result.Failed
                    && result.Status == InputModeUnityPlayerInputRequestApplicationStatus.FailedActionMapPreview
                    && result.BlockingIssueCount > 0
                    && !result.Applied
                    && !result.ActivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.DeactivatedPlayerInput
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogApplicationStep(logger, "missing-action-map-blocks-before-application", passed, result);
                return passed;
            }
        }

        private static bool ValidateNoPlayerInputManagerJoinOrSpawn(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32F No Join Spawn", "UI", true))
            {
                InputModeUnityPlayerInputRequestApplicationResult result = ApplyRequest(
                    InputModeKind.PauseOverlay,
                    InputModeKind.Gameplay,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.request-application.no-join-spawn",
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
                        LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                        LogFields.Field("operation", result.Operation.ToString()),
                        LogFields.Field("activatedPlayerInput", result.ActivatedPlayerInput),
                        LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                        LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                        LogFields.Field("playerJoin", result.CallsPlayerJoin),
                        LogFields.Field("actorSpawning", result.SpawnsActor),
                        LogFields.Field("customInputManager", result.UsesCustomInputManager)));

                return passed;
            }
        }

        private static InputModeUnityPlayerInputRequestApplicationResult ApplyRequest(
            InputModeKind currentMode,
            InputModeKind targetMode,
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

            return InputModeUnityPlayerInputRequestApplication.Apply(
                currentState,
                InputModeRequest.To(targetMode, source, reason),
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
                        "qa.inputmode.request-application.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.inputmode.request-application.targets")
                },
                source,
                "qa.inputmode.request-application.targets");
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
                        "qa.inputmode.request-application.playeractor")
                },
                source,
                "qa.inputmode.request-application.playeractor");
        }

        private static PlayerActorSet EmptyPlayerActorSet(string source)
        {
            return PlayerActorSet.FromDescriptors(
                Array.Empty<PlayerActorDescriptor>(),
                source,
                "qa.inputmode.request-application.playeractor.empty");
        }

        private static void LogApplicationStep(FrameworkLogger logger, string step, bool passed, InputModeUnityPlayerInputRequestApplicationResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
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
                "QA InputMode Unity PlayerInput Request Application Smoke step completed.",
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
