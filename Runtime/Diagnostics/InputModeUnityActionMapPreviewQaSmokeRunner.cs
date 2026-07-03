
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.InputMode;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F32B InputMode Unity action map preview.
    /// It validates action map evidence only; it never calls PlayerInput.SwitchCurrentActionMap.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32B InputMode Unity action map preview smoke.")]
    internal static class InputModeUnityActionMapPreviewQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Unity Action Map Preview Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityActionMapPreviewQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool gameplayPassed = ValidateGameplayResolvesPlayerMap(logger, normalizedSource);
                bool pauseOverlayPassed = ValidatePauseOverlayResolvesUiMap(logger, normalizedSource);
                bool frontendMenuPassed = ValidateFrontendMenuResolvesUiMap(logger, normalizedSource);
                bool missingGameplayMapPassed = ValidateMissingGameplayMapBlocking(logger, normalizedSource);
                bool lockedPassed = ValidateInputLockedNoActionMapRequired(logger, normalizedSource);
                bool noSwitchingPassed = ValidateNoActionMapSwitching(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && gameplayPassed
                    && pauseOverlayPassed
                    && frontendMenuPassed
                    && missingGameplayMapPassed
                    && lockedPassed
                    && noSwitchingPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Unity Action Map Preview Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapBinding[] bindings = InputModeUnityActionMapBindings.CanonicalPlayerUi();
            bool passed = bindings.Length == 4
                && bindings[0].InputMode == InputModeKind.Gameplay
                && bindings[0].ActionMapRequired
                && bindings[0].ActionMapName.ToString() == "Player"
                && bindings[1].InputMode == InputModeKind.PauseOverlay
                && bindings[1].ActionMapName.ToString() == "UI"
                && bindings[2].InputMode == InputModeKind.FrontendMenu
                && bindings[2].ActionMapName.ToString() == "UI"
                && bindings[3].InputMode == InputModeKind.InputLocked
                && !bindings[3].ActionMapRequired;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("gameplayMap", "Player"),
                    LogFields.Field("pauseOverlayMap", "UI"),
                    LogFields.Field("frontendMenuMap", "UI"),
                    LogFields.Field("inputLockedMapRequired", false),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));

            return passed;
        }

        private static bool ValidateGameplayResolvesPlayerMap(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.Gameplay,
                ValidActionMapEvidence(source),
                source,
                "qa.inputmode.actionmap.gameplay");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.Gameplay
                && preview.ActionMapRequired
                && preview.ActionMapName.ToString() == "Player"
                && preview.ActionMapAvailable
                && preview.HasActionAsset
                && preview.AvailableActionMapCount == 2
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "gameplay-resolves-player-map", passed, preview);
            return passed;
        }

        private static bool ValidatePauseOverlayResolvesUiMap(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.PauseOverlay,
                ValidActionMapEvidence(source),
                source,
                "qa.inputmode.actionmap.pause-overlay");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.PauseOverlay
                && preview.ActionMapRequired
                && preview.ActionMapName.ToString() == "UI"
                && preview.ActionMapAvailable
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "pause-overlay-resolves-ui-map", passed, preview);
            return passed;
        }

        private static bool ValidateFrontendMenuResolvesUiMap(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.FrontendMenu,
                ValidActionMapEvidence(source),
                source,
                "qa.inputmode.actionmap.frontend-menu");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.FrontendMenu
                && preview.ActionMapRequired
                && preview.ActionMapName.ToString() == "UI"
                && preview.ActionMapAvailable
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "frontend-menu-resolves-ui-map", passed, preview);
            return passed;
        }

        private static bool ValidateMissingGameplayMapBlocking(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.Gameplay,
                UiOnlyActionMapEvidence(source),
                source,
                "qa.inputmode.actionmap.missing-gameplay-map");

            bool passed = preview.Failed
                && preview.Status == InputModeUnityActionMapPreviewStatus.FailedActionMapEvidence
                && preview.RequestedMode == InputModeKind.Gameplay
                && preview.ActionMapRequired
                && preview.ActionMapName.ToString() == "Player"
                && !preview.ActionMapAvailable
                && preview.BlockingIssueCount == 1
                && ContainsIssue(preview, InputModeUnityActionMapPreviewIssueKind.MissingRequiredActionMap)
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "missing-gameplay-map-blocking", passed, preview);
            return passed;
        }

        private static bool ValidateInputLockedNoActionMapRequired(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.InputLocked,
                null,
                source,
                "qa.inputmode.actionmap.input-locked");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.InputLocked
                && !preview.ActionMapRequired
                && preview.ActionMapAvailable
                && !preview.HasActionAsset
                && preview.AvailableActionMapCount == 0
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "inputlocked-no-action-map-required", passed, preview);
            return passed;
        }

        private static bool ValidateNoActionMapSwitching(FrameworkLogger logger, string source)
        {
            InputModeUnityActionMapPreviewResult preview = CreateActionMapPreview(
                InputModeKind.Gameplay,
                ValidActionMapEvidence(source),
                source,
                "qa.inputmode.actionmap.no-switching");

            bool passed = preview.Succeeded
                && preview.ActionMapName.ToString() == "Player"
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior
                && !preview.ActivatesPlayerInput
                && !preview.CallsPlayerJoin
                && !preview.SpawnsActor;

            LogStep(
                logger,
                "no-action-map-switching",
                passed,
                LogFields.Of(
                    LogFields.Field("requestedMode", preview.RequestedMode.ToString()),
                    LogFields.Field("actionMapName", preview.ActionMapName.ToString()),
                    LogFields.Field("actionMapSwitching", preview.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", preview.AppliesInputBehavior),
                    LogFields.Field("playerInputActivation", preview.ActivatesPlayerInput),
                    LogFields.Field("playerJoin", preview.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", preview.SpawnsActor),
                    LogFields.Field("customInputManager", false)));

            return passed;
        }

        private static InputModeUnityActionMapPreviewResult CreateActionMapPreview(
            InputModeKind targetMode,
            UnityInputActionMapEvidence evidence,
            string source,
            string reason)
        {
            InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(targetMode, source, reason);
            return InputModeUnityActionMapPreviewEvaluator.Preview(
                applicationPreview,
                evidence,
                InputModeUnityActionMapBindings.CanonicalPlayerUi(),
                source,
                reason);
        }

        private static InputModeUnityApplicationPreviewResult CreateApplicationPreview(
            InputModeKind targetMode,
            string source,
            string reason)
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
                ValidSessionManagerEvidence(source),
                source,
                reason);
        }

        private static UnityInputActionMapEvidence ValidActionMapEvidence(string source)
        {
            return UnityInputActionMapEvidence.FromActionMapNames(
                new[] { "Player", "UI" },
                source,
                "qa.inputmode.actionmap.valid-evidence");
        }

        private static UnityInputActionMapEvidence UiOnlyActionMapEvidence(string source)
        {
            return UnityInputActionMapEvidence.FromActionMapNames(
                new[] { "UI" },
                source,
                "qa.inputmode.actionmap.ui-only-evidence");
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
                        "qa.inputmode.actionmap.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.inputmode.actionmap.targets")
                },
                source,
                "qa.inputmode.actionmap.targets");
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
                        "qa.inputmode.actionmap.playeractor")
                },
                source,
                "qa.inputmode.actionmap.playeractor");
        }

        private static UnityInputPlayerInputManagerEvidence ValidSessionManagerEvidence(string source)
        {
            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(
                1,
                source,
                "qa.inputmode.actionmap.session-manager");
        }

        private static bool ContainsIssue(InputModeUnityActionMapPreviewResult preview, InputModeUnityActionMapPreviewIssueKind kind)
        {
            for (int i = 0; i < preview.Issues.Length; i++)
            {
                if (preview.Issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogPreviewStep(FrameworkLogger logger, string step, bool passed, InputModeUnityActionMapPreviewResult preview)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", preview.Status.ToString()),
                    LogFields.Field("requestedMode", preview.RequestedMode.ToString()),
                    LogFields.Field("actionMapRequired", preview.ActionMapRequired),
                    LogFields.Field("actionMapName", preview.ActionMapName.ToString()),
                    LogFields.Field("actionMapAvailable", preview.ActionMapAvailable),
                    LogFields.Field("hasActionAsset", preview.HasActionAsset),
                    LogFields.Field("availableActionMaps", preview.AvailableActionMapCount),
                    LogFields.Field("issues", preview.IssueCount),
                    LogFields.Field("blockingIssues", preview.BlockingIssueCount),
                    LogFields.Field("actionMapSwitching", preview.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", preview.AppliesInputBehavior),
                    LogFields.Field("diagnostics", preview.ToDiagnosticString())));
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
                "QA InputMode Unity Action Map Preview Smoke step completed.",
                stepFields);
        }
    }
}
#endif
