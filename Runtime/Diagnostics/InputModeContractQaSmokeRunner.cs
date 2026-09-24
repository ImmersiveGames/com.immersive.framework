using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.InputMode;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F30A InputMode contract vocabulary.
    /// It validates passive identity/state/request/result semantics only; it does not switch action maps or read input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F30A InputMode identity/state/request result contract smoke.")]
    internal static class InputModeContractQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Contract Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeContractQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool initialStatePassed = ValidateInitialState(logger, normalizedSource);
                bool gameplayPassed = ValidateValidGameplayRequest(logger, normalizedSource);
                bool pauseOverlayPassed = ValidateValidPauseOverlayRequest(logger, normalizedSource);
                bool ignoredPassed = ValidateIgnoredSameModeRequest(logger, normalizedSource);
                bool invalidPassed = ValidateInvalidModeRequest(logger, normalizedSource);
                bool noSwitchingPassed = ValidateNoActionMapSwitching(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && initialStatePassed
                    && gameplayPassed
                    && pauseOverlayPassed
                    && ignoredPassed
                    && invalidPassed
                    && noSwitchingPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Contract Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            InputModeDefinition gameplay = InputModeDefinitions.Gameplay(source, "qa.inputmode.contracts.gameplay");
            InputModeDefinition pause = InputModeDefinitions.PauseOverlay(source, "qa.inputmode.contracts.pause");
            InputModeDefinition menu = InputModeDefinitions.FrontendMenu(source, "qa.inputmode.contracts.menu");
            InputModeDefinition locked = InputModeDefinitions.InputLocked(source, "qa.inputmode.contracts.locked");

            bool passed = gameplay.IsValid
                && pause.IsValid
                && menu.IsValid
                && locked.IsValid
                && gameplay.ModeId.Domain == Identity.FrameworkIdentityDomain.InputMode
                && pause.ModeId.Domain == Identity.FrameworkIdentityDomain.InputMode
                && InputModeRules.IsValidKind(InputModeKind.Gameplay)
                && InputModeRules.IsValidKind(InputModeKind.PauseOverlay)
                && InputModeRules.IsValidKind(InputModeKind.FrontendMenu)
                && InputModeRules.IsValidKind(InputModeKind.InputLocked)
                && !InputModeRules.IsValidKind(InputModeKind.Unknown);

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("domain", gameplay.ModeId.Domain.ToString()),
                    LogFields.Field("gameplay", gameplay.ModeId.StableText),
                    LogFields.Field("pauseOverlay", pause.ModeId.StableText),
                    LogFields.Field("frontendMenu", menu.ModeId.StableText),
                    LogFields.Field("inputLocked", locked.ModeId.StableText),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));

            return passed;
        }

        private static bool ValidateInitialState(FrameworkLogger logger, string source)
        {
            InputModeState state = InputModeState.InitialGameplay(source, "qa.inputmode.initial-state");

            bool passed = state is { IsValid: true, CurrentKind: InputModeKind.Gameplay, Revision: 0, SwitchesActionMaps: false, AppliesInputBehavior: false };

            LogStateStep(logger, "initial-state", passed, state);
            return passed;
        }

        private static bool ValidateValidGameplayRequest(FrameworkLogger logger, string source)
        {
            InputModeState pauseState = new InputModeState(InputModeDefinitions.PauseOverlay(source, "qa.inputmode.pause-state"), 1, source, "qa.inputmode.pause-state");
            InputModeRequestResult result = InputModeRequestEvaluator.Preview(
                pauseState,
                InputModeRequest.To(InputModeKind.Gameplay, source, "qa.inputmode.request.gameplay"),
                source);

            bool passed = result.Succeeded
                && result.PreviousState.CurrentKind == InputModeKind.PauseOverlay
                && result.CurrentState is { CurrentKind: InputModeKind.Gameplay, Revision: 2 }
                && result.IssueCount == 0
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior;

            LogResultStep(logger, "valid-gameplay-request", passed, result);
            return passed;
        }

        private static bool ValidateValidPauseOverlayRequest(FrameworkLogger logger, string source)
        {
            InputModeState state = InputModeState.InitialGameplay(source, "qa.inputmode.valid-pause-overlay.initial");
            InputModeRequestResult result = InputModeRequestEvaluator.Preview(
                state,
                InputModeRequest.To(InputModeKind.PauseOverlay, source, "qa.inputmode.request.pause-overlay"),
                source);

            bool passed = result.Succeeded
                && result.PreviousState.CurrentKind == InputModeKind.Gameplay
                && result.CurrentState is { CurrentKind: InputModeKind.PauseOverlay, Revision: 1 }
                && result.IssueCount == 0
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior;

            LogResultStep(logger, "valid-pause-overlay-request", passed, result);
            return passed;
        }

        private static bool ValidateIgnoredSameModeRequest(FrameworkLogger logger, string source)
        {
            InputModeState state = InputModeState.InitialGameplay(source, "qa.inputmode.ignored.initial");
            InputModeRequestResult result = InputModeRequestEvaluator.Preview(
                state,
                InputModeRequest.To(InputModeKind.Gameplay, source, "qa.inputmode.request.same-mode"),
                source);

            bool passed = result.Ignored
                && result.Status == InputModeRequestStatus.IgnoredAlreadyInMode
                && result.PreviousState.CurrentKind == InputModeKind.Gameplay
                && result.CurrentState is { CurrentKind: InputModeKind.Gameplay, Revision: 0 }
                && result.IssueCount == 0
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior;

            LogResultStep(logger, "ignored-same-mode-request", passed, result);
            return passed;
        }

        private static bool ValidateInvalidModeRequest(FrameworkLogger logger, string source)
        {
            InputModeState state = InputModeState.InitialGameplay(source, "qa.inputmode.invalid.initial");
            InputModeRequestResult result = InputModeRequestEvaluator.Preview(
                state,
                InputModeRequest.To(InputModeKind.Unknown, source, "qa.inputmode.request.invalid"),
                source);

            bool passed = result.Failed
                && result.Status == InputModeRequestStatus.FailedInvalidTargetMode
                && result.PreviousState.CurrentKind == InputModeKind.Gameplay
                && result.CurrentState is { CurrentKind: InputModeKind.Gameplay, Revision: 0 }
                && result.BlockingIssueCount == 1
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior;

            LogResultStep(logger, "invalid-mode-request-no-side-effects", passed, result);
            return passed;
        }

        private static bool ValidateNoActionMapSwitching(FrameworkLogger logger, string source)
        {
            InputModeState state = InputModeState.InitialGameplay(source, "qa.inputmode.no-action-map.initial");
            InputModeRequestResult result = InputModeRequestEvaluator.Preview(
                state,
                InputModeRequest.To(InputModeKind.InputLocked, source, "qa.inputmode.request.input-locked"),
                source);

            bool passed = result.Succeeded
                && state is { SwitchesActionMaps: false, AppliesInputBehavior: false }
                && !result.SwitchesActionMaps
                && !result.AppliesInputBehavior;

            LogStep(
                logger,
                "no-action-map-switching",
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("previousMode", result.PreviousState.CurrentKind.ToString()),
                    LogFields.Field("currentMode", result.CurrentState.CurrentKind.ToString()),
                    LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                    LogFields.Field("playerInputOwnership", "none"),
                    LogFields.Field("pauseIntegration", "none")));

            return passed;
        }

        private static void LogStateStep(FrameworkLogger logger, string step, bool passed, InputModeState state)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("currentMode", state.CurrentKind.ToString()),
                    LogFields.Field("revision", state.Revision),
                    LogFields.Field("owner", state.Owner),
                    LogFields.Field("actionMapSwitching", state.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", state.AppliesInputBehavior)));
        }

        private static void LogResultStep(FrameworkLogger logger, string step, bool passed, InputModeRequestResult result)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("previousMode", result.PreviousState.CurrentKind.ToString()),
                    LogFields.Field("currentMode", result.CurrentState.CurrentKind.ToString()),
                    LogFields.Field("revision", result.CurrentState.Revision),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("actionMapSwitching", result.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", result.AppliesInputBehavior),
                    LogFields.Field("diagnostics", result.ToDiagnosticString())));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            if (passed)
            {
                logger.Info("QA InputMode Contract Smoke step completed.", PrependStepFields(step, passed, fields));
                return;
            }

            logger.Warning("QA InputMode Contract Smoke step failed.", PrependStepFields(step, passed, fields));
        }

        private static LogField[] PrependStepFields(string step, bool passed, LogField[] fields)
        {
            var result = new LogField[(fields?.Length ?? 0) + 2];
            result[0] = LogFields.Field("step", step.NormalizeTextOrFallback("<unknown>"));
            result[1] = LogFields.Field("passed", passed);
            if (fields == null)
            {
                return result;
            }

            Array.Copy(fields, 0, result, 2, fields.Length);
            return result;
        }
    }
}
#endif
