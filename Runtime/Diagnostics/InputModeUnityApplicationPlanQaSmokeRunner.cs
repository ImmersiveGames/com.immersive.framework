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
    /// API status: Development Tooling. Smoke for F32C InputMode Unity application dry-run plan.
    /// It validates adapter intent only; it never calls PlayerInput or PlayerInputManager behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32C InputMode Unity application dry-run plan smoke.")]
    internal static class InputModeUnityApplicationPlanQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Unity Application Plan Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPlanQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool gameplayPassed = ValidateGameplayBuildsPlayerActionMapPlan(logger, normalizedSource);
                bool pauseOverlayPassed = ValidatePauseOverlayBuildsUiActionMapPlan(logger, normalizedSource);
                bool frontendMenuPassed = ValidateFrontendMenuBuildsUiActionMapPlan(logger, normalizedSource);
                bool lockedPassed = ValidateInputLockedBuildsLockPlan(logger, normalizedSource);
                bool failedActionMapPassed = ValidateFailedActionMapPreviewBlocking(logger, normalizedSource);
                bool mismatchPassed = ValidatePreviewModeMismatchBlocking(logger, normalizedSource);
                bool noBehaviorPassed = ValidateNoUnityInputBehavior(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && gameplayPassed
                    && pauseOverlayPassed
                    && frontendMenuPassed
                    && lockedPassed
                    && failedActionMapPassed
                    && mismatchPassed
                    && noBehaviorPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Unity Application Plan Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = InputModeUnityApplicationPlanOperation.SelectActionMap != InputModeUnityApplicationPlanOperation.Unknown
                && InputModeUnityApplicationPlanOperation.LockInput != InputModeUnityApplicationPlanOperation.Unknown
                && InputModeUnityApplicationPlanStatus.Succeeded != InputModeUnityApplicationPlanStatus.Unknown
                && InputModeUnityApplicationPlanIssueKind.ActionMapPreviewNotSucceeded != InputModeUnityApplicationPlanIssueKind.None;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("operationGameplay", InputModeUnityApplicationPlanOperation.SelectActionMap.ToString()),
                    LogFields.Field("operationInputLocked", InputModeUnityApplicationPlanOperation.LockInput.ToString()),
                    LogFields.Field("frameworkRole", "DryRunPlanOnly"),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));

            return passed;
        }

        private static bool ValidateGameplayBuildsPlayerActionMapPlan(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.Gameplay, source, "qa.inputmode.plan.gameplay");

            bool passed = plan.Succeeded
                && plan.RequestedMode == InputModeKind.Gameplay
                && plan.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                && plan.TargetRole == UnityInputTargetRole.GameplayCommands
                && plan.ActionMapName.ToString() == "Player"
                && plan.ActionMapRequired
                && plan.ActionMapAvailable
                && plan.PlayerActorRequired
                && plan.SessionPlayerInputManagerRequired
                && plan.WouldSelectActionMap
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "gameplay-builds-player-action-map-plan", passed, plan);
            return passed;
        }

        private static bool ValidatePauseOverlayBuildsUiActionMapPlan(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.PauseOverlay, source, "qa.inputmode.plan.pause-overlay");

            bool passed = plan.Succeeded
                && plan.RequestedMode == InputModeKind.PauseOverlay
                && plan.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                && plan.TargetRole == UnityInputTargetRole.GlobalUiPause
                && plan.ActionMapName.ToString() == "UI"
                && plan.ActionMapRequired
                && plan.ActionMapAvailable
                && !plan.PlayerActorRequired
                && !plan.SessionPlayerInputManagerRequired
                && plan.WouldSelectActionMap
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "pause-overlay-builds-ui-action-map-plan", passed, plan);
            return passed;
        }

        private static bool ValidateFrontendMenuBuildsUiActionMapPlan(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.FrontendMenu, source, "qa.inputmode.plan.frontend-menu");

            bool passed = plan.Succeeded
                && plan.RequestedMode == InputModeKind.FrontendMenu
                && plan.Operation == InputModeUnityApplicationPlanOperation.SelectActionMap
                && plan.TargetRole == UnityInputTargetRole.GlobalUiPause
                && plan.ActionMapName.ToString() == "UI"
                && plan.ActionMapRequired
                && plan.ActionMapAvailable
                && !plan.PlayerActorRequired
                && !plan.SessionPlayerInputManagerRequired
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "frontend-menu-builds-ui-action-map-plan", passed, plan);
            return passed;
        }

        private static bool ValidateInputLockedBuildsLockPlan(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.InputLocked, source, "qa.inputmode.plan.input-locked");

            bool passed = plan.Succeeded
                && plan.RequestedMode == InputModeKind.InputLocked
                && plan.Operation == InputModeUnityApplicationPlanOperation.LockInput
                && plan.TargetRole == UnityInputTargetRole.Unknown
                && !plan.ActionMapRequired
                && plan.ActionMapAvailable
                && !plan.PlayerActorRequired
                && !plan.SessionPlayerInputManagerRequired
                && plan.WouldLockInput
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "inputlocked-builds-lock-plan", passed, plan);
            return passed;
        }

        private static bool ValidateFailedActionMapPreviewBlocking(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(InputModeKind.Gameplay, source, "qa.inputmode.plan.failed-actionmap-preview");
            InputModeUnityActionMapPreviewResult actionMapPreview = CreateActionMapPreview(
                applicationPreview,
                UiOnlyActionMapEvidence(source),
                source,
                "qa.inputmode.plan.failed-actionmap-preview");

            InputModeUnityApplicationPlanResult plan = InputModeUnityApplicationPlanEvaluator.BuildPlan(
                applicationPreview,
                actionMapPreview,
                source,
                "qa.inputmode.plan.failed-actionmap-preview");

            bool passed = plan.Failed
                && plan.Status == InputModeUnityApplicationPlanStatus.FailedActionMapPreview
                && plan.RequestedMode == InputModeKind.Gameplay
                && plan.BlockingIssueCount == 1
                && ContainsIssue(plan, InputModeUnityApplicationPlanIssueKind.ActionMapPreviewNotSucceeded)
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "failed-action-map-preview-blocking", passed, plan);
            return passed;
        }

        private static bool ValidatePreviewModeMismatchBlocking(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(InputModeKind.Gameplay, source, "qa.inputmode.plan.mode-mismatch.application");
            InputModeUnityApplicationPreviewResult pauseApplicationPreview = CreateApplicationPreview(InputModeKind.PauseOverlay, source, "qa.inputmode.plan.mode-mismatch.actionmap");
            InputModeUnityActionMapPreviewResult actionMapPreview = CreateActionMapPreview(
                pauseApplicationPreview,
                ValidActionMapEvidence(source),
                source,
                "qa.inputmode.plan.mode-mismatch.actionmap");

            InputModeUnityApplicationPlanResult plan = InputModeUnityApplicationPlanEvaluator.BuildPlan(
                applicationPreview,
                actionMapPreview,
                source,
                "qa.inputmode.plan.mode-mismatch");

            bool passed = plan.Failed
                && plan.Status == InputModeUnityApplicationPlanStatus.FailedPreviewMismatch
                && plan.RequestedMode == InputModeKind.Gameplay
                && plan.BlockingIssueCount == 1
                && ContainsIssue(plan, InputModeUnityApplicationPlanIssueKind.PreviewModeMismatch)
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior;

            LogPlanStep(logger, "preview-mode-mismatch-blocking", passed, plan);
            return passed;
        }

        private static bool ValidateNoUnityInputBehavior(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPlanResult plan = CreatePlan(InputModeKind.Gameplay, source, "qa.inputmode.plan.no-behavior");

            bool passed = plan.Succeeded
                && plan.WouldSelectActionMap
                && !plan.SwitchesActionMaps
                && !plan.AppliesInputBehavior
                && !plan.ActivatesPlayerInput
                && !plan.CallsPlayerJoin
                && !plan.SpawnsActor;

            LogStep(
                logger,
                "no-unity-input-behavior",
                passed,
                LogFields.Of(
                    LogFields.Field("requestedMode", plan.RequestedMode.ToString()),
                    LogFields.Field("operation", plan.Operation.ToString()),
                    LogFields.Field("wouldSelectActionMap", plan.WouldSelectActionMap),
                    LogFields.Field("wouldLockInput", plan.WouldLockInput),
                    LogFields.Field("actionMapSwitching", plan.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", plan.AppliesInputBehavior),
                    LogFields.Field("playerInputActivation", plan.ActivatesPlayerInput),
                    LogFields.Field("playerJoin", plan.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", plan.SpawnsActor),
                    LogFields.Field("customInputManager", false)));

            return passed;
        }

        private static InputModeUnityApplicationPlanResult CreatePlan(InputModeKind targetMode, string source, string reason)
        {
            InputModeUnityApplicationPreviewResult applicationPreview = CreateApplicationPreview(targetMode, source, reason);
            InputModeUnityActionMapPreviewResult actionMapPreview = CreateActionMapPreview(
                applicationPreview,
                ValidActionMapEvidence(source),
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
                ValidSessionManagerEvidence(source),
                source,
                reason);
        }

        private static InputModeUnityActionMapPreviewResult CreateActionMapPreview(
            InputModeUnityApplicationPreviewResult applicationPreview,
            UnityInputActionMapEvidence evidence,
            string source,
            string reason)
        {
            return InputModeUnityActionMapPreviewEvaluator.Preview(
                applicationPreview,
                evidence,
                InputModeUnityActionMapBindings.CanonicalPlayerUi(),
                source,
                reason);
        }

        private static UnityInputActionMapEvidence ValidActionMapEvidence(string source)
        {
            return UnityInputActionMapEvidence.FromActionMapNames(
                new[] { "Player", "UI" },
                source,
                "qa.inputmode.plan.valid-actionmap-evidence");
        }

        private static UnityInputActionMapEvidence UiOnlyActionMapEvidence(string source)
        {
            return UnityInputActionMapEvidence.FromActionMapNames(
                new[] { "UI" },
                source,
                "qa.inputmode.plan.ui-only-actionmap-evidence");
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
                        "qa.inputmode.plan.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.inputmode.plan.targets")
                },
                source,
                "qa.inputmode.plan.targets");
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
                        "qa.inputmode.plan.playeractor")
                },
                source,
                "qa.inputmode.plan.playeractor");
        }

        private static UnityInputPlayerInputManagerEvidence ValidSessionManagerEvidence(string source)
        {
            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(
                1,
                source,
                "qa.inputmode.plan.session-manager");
        }

        private static bool ContainsIssue(InputModeUnityApplicationPlanResult plan, InputModeUnityApplicationPlanIssueKind kind)
        {
            for (int i = 0; i < plan.Issues.Length; i++)
            {
                if (plan.Issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogPlanStep(FrameworkLogger logger, string step, bool passed, InputModeUnityApplicationPlanResult plan)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", plan.Status.ToString()),
                    LogFields.Field("requestedMode", plan.RequestedMode.ToString()),
                    LogFields.Field("operation", plan.Operation.ToString()),
                    LogFields.Field("targetRole", plan.TargetRole.ToString()),
                    LogFields.Field("actionMapRequired", plan.ActionMapRequired),
                    LogFields.Field("actionMapName", plan.ActionMapName.ToString()),
                    LogFields.Field("actionMapAvailable", plan.ActionMapAvailable),
                    LogFields.Field("playerActorRequired", plan.PlayerActorRequired),
                    LogFields.Field("sessionPlayerInputManagerRequired", plan.SessionPlayerInputManagerRequired),
                    LogFields.Field("wouldSelectActionMap", plan.WouldSelectActionMap),
                    LogFields.Field("wouldLockInput", plan.WouldLockInput),
                    LogFields.Field("issues", plan.IssueCount),
                    LogFields.Field("blockingIssues", plan.BlockingIssueCount),
                    LogFields.Field("actionMapSwitching", plan.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", plan.AppliesInputBehavior),
                    LogFields.Field("diagnostics", plan.ToDiagnosticString())));
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
                "QA InputMode Unity Application Plan Smoke step completed.",
                stepFields);
        }
    }
}
#endif
