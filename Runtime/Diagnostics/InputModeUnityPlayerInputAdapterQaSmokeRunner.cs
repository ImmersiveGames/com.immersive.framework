using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Actors;
using Immersive.Framework.InputMode;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F32D explicit Unity PlayerInput adapter application.
    /// This is the first F32 smoke that intentionally performs Unity PlayerInput side effects.
    /// It still never owns PlayerInputManager, joins players, spawns actors or moves PlayerActor objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32D Unity PlayerInput adapter smoke.")]
    internal static class InputModeUnityPlayerInputAdapterQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Unity PlayerInput Adapter Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputAdapterQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool gameplayPassed = ValidateGameplaySelectsPlayerActionMap(logger, normalizedSource);
                bool pausePassed = ValidatePauseOverlaySelectsUiActionMap(logger, normalizedSource);
                bool lockPassed = ValidateInputLockedDeactivatesPlayerInput(logger, normalizedSource);
                bool missingMapPassed = ValidateMissingActionMapBlocking(logger, normalizedSource);
                bool failedPlanPassed = ValidateFailedPlanBlocking(logger, normalizedSource);
                bool noJoinPassed = ValidateNoPlayerJoinOrSpawn(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && gameplayPassed
                    && pausePassed
                    && lockPassed
                    && missingMapPassed
                    && failedPlanPassed
                    && noJoinPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Unity PlayerInput Adapter Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = InputModeUnityPlayerInputAdapterStatus.Succeeded != InputModeUnityPlayerInputAdapterStatus.Unknown
                && InputModeUnityPlayerInputAdapterIssueKind.MissingActionMap != InputModeUnityPlayerInputAdapterIssueKind.None;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("frameworkRole", "ExplicitUnityPlayerInputAdapter"),
                    LogFields.Field("unityAuthority", "PlayerInput"),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("playerJoin", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidateGameplaySelectsPlayerActionMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D Gameplay PlayerInput", "UI", true))
            {
                InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.Gameplay, source, "qa.inputmode.adapter.gameplay");
                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.gameplay");

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.Gameplay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.SelectedActionMap
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && result.AppliedActionMapName.ToString() == "Player"
                    && fixture.PlayerInput.currentActionMap is { name: "Player" }
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogAdapterStep(logger, "gameplay-selects-player-action-map", passed, result);
                return passed;
            }
        }

        private static bool ValidatePauseOverlaySelectsUiActionMap(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D PauseOverlay PlayerInput", "Player", true))
            {
                InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.PauseOverlay, source, "qa.inputmode.adapter.pause-overlay");
                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.pause-overlay");

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.PauseOverlay
                    && result.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                    && result.SelectedActionMap
                    && result.SwitchesActionMaps
                    && result.AppliedActionMapName.ToString() == "UI"
                    && fixture.PlayerInput.currentActionMap is { name: "UI" }
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogAdapterStep(logger, "pause-overlay-selects-ui-action-map", passed, result);
                return passed;
            }
        }

        private static bool ValidateInputLockedDeactivatesPlayerInput(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D InputLocked PlayerInput", "Player", true))
            {
                InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.InputLocked, source, "qa.inputmode.adapter.input-locked");
                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.input-locked");

                bool passed = result.Succeeded
                    && result.RequestedMode == InputModeKind.InputLocked
                    && result.Operation == InputModeUnityApplicationPlanOperation.LockInput
                    && result.DeactivatedPlayerInput
                    && !result.SelectedActionMap
                    && !result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.UsesCustomInputManager;

                LogAdapterStep(logger, "inputlocked-deactivates-playerinput", passed, result);
                return passed;
            }
        }

        private static bool ValidateMissingActionMapBlocking(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D Missing Player Map", "UI", false))
            {
                InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.Gameplay, source, "qa.inputmode.adapter.missing-map");
                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.missing-map");

                bool passed = result.Failed
                    && result.Status == InputModeUnityPlayerInputAdapterStatus.FailedMissingActionMap
                    && result.BlockingIssueCount == 1
                    && ContainsIssue(result, InputModeUnityPlayerInputAdapterIssueKind.MissingActionMap)
                    && !result.Applied
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogAdapterStep(logger, "missing-action-map-blocking", passed, result);
                return passed;
            }
        }

        private static bool ValidateFailedPlanBlocking(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D Failed Plan", "UI", true))
            {
                InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(InputModeKind.Gameplay, source, "qa.inputmode.adapter.failed-plan");
                InputModeUnityActionMapPreviewResult actionMapPreview = InputModeUnityActionMapPreviewEvaluator.Preview(
                    applicationPreview,
                    UnityInputActionMapEvidence.FromActionMapNames(new[] { "UI" }, source, "qa.inputmode.adapter.failed-plan.evidence"),
                    InputModeUnityActionMapBindings.CanonicalPlayerUi(),
                    source,
                    "qa.inputmode.adapter.failed-plan");

                InputModeUnityApplicationPlanResult failedPlan = InputModeUnityApplicationPlanEvaluator.BuildPlan(
                    applicationPreview,
                    actionMapPreview,
                    source,
                    "qa.inputmode.adapter.failed-plan");

                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    failedPlan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.failed-plan");

                bool passed = result.Failed
                    && result.Status == InputModeUnityPlayerInputAdapterStatus.FailedInvalidPlan
                    && result.BlockingIssueCount == 1
                    && ContainsIssue(result, InputModeUnityPlayerInputAdapterIssueKind.InvalidPlan)
                    && !result.Applied
                    && !result.SwitchesActionMaps
                    && !result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor;

                LogAdapterStep(logger, "failed-plan-blocking", passed, result);
                return passed;
            }
        }

        private static bool ValidateNoPlayerJoinOrSpawn(FrameworkLogger logger, string source)
        {
            using (var fixture = PlayerInputFixture.Create("QA F32D No Join Spawn", "UI", true))
            {
                InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.Gameplay, source, "qa.inputmode.adapter.no-join-spawn");
                InputModeUnityPlayerInputAdapterResult result = InputModeUnityPlayerInputAdapter.Apply(
                    plan,
                    fixture.PlayerInput,
                    source,
                    "qa.inputmode.adapter.no-join-spawn");

                bool passed = result.Succeeded
                    && result.SwitchesActionMaps
                    && result.AppliesInputBehavior
                    && !result.CallsPlayerJoin
                    && !result.SpawnsActor
                    && !result.ActivatesPlayerInput
                    && !result.UsesCustomInputManager;

                LogStep(
                    logger,
                    "no-playerinputmanager-join-or-spawn",
                    passed,
                    LogFields.Of(
                        LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                        LogFields.Field("operation", result.Operation.ToString()),
                        LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                        LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                        LogFields.Field("playerInputActivation", result.ActivatesPlayerInput),
                        LogFields.Field("playerJoin", result.CallsPlayerJoin),
                        LogFields.Field("actorSpawning", result.SpawnsActor),
                        LogFields.Field("customInputManager", result.UsesCustomInputManager)));

                return passed;
            }
        }

        private static InputModeUnityApplicationPlanResult CreatePlan(InputModeKind targetMode, string source, string reason)
        {
            InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(targetMode, source, reason);
            InputModeUnityActionMapPreviewResult actionMapPreview = InputModeUnityActionMapPreviewEvaluator.Preview(
                applicationPreview,
                UnityInputActionMapEvidence.FromActionMapNames(new[] { "Player", "UI" }, source, reason),
                InputModeUnityActionMapBindings.CanonicalPlayerUi(),
                source,
                reason);

            return InputModeUnityApplicationPlanEvaluator.BuildPlan(applicationPreview, actionMapPreview, source, reason);
        }

        private static InputModeUnityApplicationPreviewResult CreateApplicationPreview(InputModeKind targetMode, string source, string reason)
        {
            InputModeState currentState = targetMode == InputModeKind.Gameplay
                ? new InputModeState(InputModeDefinitions.PauseOverlay(source, reason), 1, source, reason)
                : InputModeState.InitialGameplay(source, reason);

            InputModeRequestResult requestResult = InputModeRequestEvaluator.Preview(
                currentState,
                InputModeRequest.To(targetMode, source, reason),
                source);

            return InputModeUnityApplicationPreviewEvaluator.Preview(
                requestResult,
                ValidTargetSet(source),
                ValidPlayerActorSet(source),
                UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(1, source, reason),
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
                        "qa.inputmode.adapter.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.inputmode.adapter.targets")
                },
                source,
                "qa.inputmode.adapter.targets");
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
                        "qa.inputmode.adapter.playeractor")
                },
                source,
                "qa.inputmode.adapter.playeractor");
        }

        private static bool ContainsIssue(InputModeUnityPlayerInputAdapterResult result, InputModeUnityPlayerInputAdapterIssueKind kind)
        {
            for (int i = 0; i < result.Issues.Length; i++)
            {
                if (result.Issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogAdapterStep(FrameworkLogger logger, string step, bool passed, InputModeUnityPlayerInputAdapterResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("requestedMode", result.RequestedMode.ToString()),
                    LogFields.Field("operation", result.Operation.ToString()),
                    LogFields.Field("requestedActionMap", result.RequestedActionMapName.ToString()),
                    LogFields.Field("appliedActionMap", result.AppliedActionMapName.ToString()),
                    LogFields.Field("applied", result.Applied),
                    LogFields.Field("selectedActionMap", result.SelectedActionMap),
                    LogFields.Field("deactivatedPlayerInput", result.DeactivatedPlayerInput),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                    LogFields.Field("playerInputActivation", result.ActivatesPlayerInput),
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
                "QA InputMode Unity PlayerInput Adapter Smoke step completed.",
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
