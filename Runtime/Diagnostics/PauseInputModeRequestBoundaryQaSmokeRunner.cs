using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F30D Pause to InputMode request boundary.
    /// It validates request mapping only; it does not switch Unity action maps, mutate PlayerInput or dispatch Pause.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F30D Pause InputMode request boundary smoke.")]
    internal static class PauseInputModeRequestBoundaryQaSmokeRunner
    {
        internal const string SmokeName = "Pause InputMode Request Boundary Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseInputModeRequestBoundaryQaSmokeRunner));

            try
            {
                bool pausedStatePassed = ValidatePausedStateMapsToPauseOverlay(logger, normalizedSource);
                bool runningStatePassed = ValidateRunningStateMapsToGameplay(logger, normalizedSource);
                bool pauseResultPassed = ValidatePauseResultMapsToPauseOverlay(logger, normalizedSource);
                bool resumeResultPassed = ValidateResumeResultMapsToGameplay(logger, normalizedSource);
                bool invalidStatePassed = ValidateInvalidPauseStateRejected(logger, normalizedSource);
                bool noInputBehaviorPassed = ValidateNoUnityInputBehavior(logger, normalizedSource);

                return Task.FromResult(pausedStatePassed
                    && runningStatePassed
                    && pauseResultPassed
                    && resumeResultPassed
                    && invalidStatePassed
                    && noInputBehaviorPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Pause InputMode Request Boundary Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidatePausedStateMapsToPauseOverlay(FrameworkLogger logger, string source)
        {
            InputModeRequest request = PauseInputModeRequestMapper.CreateRequest(
                PauseState.Paused,
                source,
                "qa.pause-inputmode.paused-state");

            bool passed = request is { IsValid: true, TargetMode: InputModeKind.PauseOverlay }
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior;

            LogRequestStep(logger, "paused-state-to-pause-overlay", passed, PauseState.Paused, request, "state");
            return passed;
        }

        private static bool ValidateRunningStateMapsToGameplay(FrameworkLogger logger, string source)
        {
            InputModeRequest request = PauseInputModeRequestMapper.CreateRequest(
                PauseState.Running,
                source,
                "qa.pause-inputmode.running-state");

            bool passed = request is { IsValid: true, TargetMode: InputModeKind.Gameplay }
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior;

            LogRequestStep(logger, "running-state-to-gameplay", passed, PauseState.Running, request, "state");
            return passed;
        }

        private static bool ValidatePauseResultMapsToPauseOverlay(FrameworkLogger logger, string source)
        {
            PauseRequest pauseRequest = PauseRequest.Pause(
                "qa.pause-inputmode.pause-request",
                source,
                "qa.pause-inputmode.pause-request");
            PauseResult pauseResult = PauseResult.AppliedResult(
                pauseRequest,
                PauseState.Running,
                PauseState.Paused,
                "QA pause request applied.");

            InputModeRequest request = PauseInputModeRequestMapper.CreateRequest(
                pauseResult,
                source,
                "qa.pause-inputmode.pause-result");

            bool passed = request is { IsValid: true, TargetMode: InputModeKind.PauseOverlay }
                && pauseResult.CurrentState == PauseState.Paused
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior;

            LogRequestStep(logger, "pause-result-to-pause-overlay", passed, pauseResult.CurrentState, request, pauseResult.Status.ToString());
            return passed;
        }

        private static bool ValidateResumeResultMapsToGameplay(FrameworkLogger logger, string source)
        {
            PauseRequest resumeRequest = PauseRequest.Resume(
                "qa.pause-inputmode.resume-request",
                source,
                "qa.pause-inputmode.resume-request");
            PauseResult resumeResult = PauseResult.AppliedResult(
                resumeRequest,
                PauseState.Paused,
                PauseState.Running,
                "QA resume request applied.");

            InputModeRequest request = PauseInputModeRequestMapper.CreateRequest(
                resumeResult,
                source,
                "qa.pause-inputmode.resume-result");

            bool passed = request is { IsValid: true, TargetMode: InputModeKind.Gameplay }
                && resumeResult.CurrentState == PauseState.Running
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior;

            LogRequestStep(logger, "resume-result-to-gameplay", passed, resumeResult.CurrentState, request, resumeResult.Status.ToString());
            return passed;
        }

        private static bool ValidateInvalidPauseStateRejected(FrameworkLogger logger, string source)
        {
            bool threw = false;
            string exceptionName = string.Empty;

            try
            {
                PauseInputModeRequestMapper.CreateRequest(
                    PauseState.Unknown,
                    source,
                    "qa.pause-inputmode.invalid-state");
            }
            catch (ArgumentOutOfRangeException exception)
            {
                threw = true;
                exceptionName = exception.GetType().Name;
            }

            bool passed = threw
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior;

            LogStep(
                logger,
                "invalid-pause-state-rejected",
                passed,
                LogFields.Of(
                    LogFields.Field("pauseState", PauseState.Unknown.ToString()),
                    LogFields.Field("exception", exceptionName.NormalizeTextOrFallback("none")),
                    LogFields.Field("actionMapSwitching", PauseInputModeRequestMapper.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", PauseInputModeRequestMapper.AppliesInputBehavior)));

            return passed;
        }

        private static bool ValidateNoUnityInputBehavior(FrameworkLogger logger, string source)
        {
            InputModeRequest request = PauseInputModeRequestMapper.CreateRequest(
                PauseState.Paused,
                source,
                "qa.pause-inputmode.no-unity-input-behavior");

            bool passed = request.TargetMode == InputModeKind.PauseOverlay
                && !PauseInputModeRequestMapper.SwitchesActionMaps
                && !PauseInputModeRequestMapper.AppliesInputBehavior
                && !PauseInputModeRequestMapper.OwnsPlayerInput
                && !PauseInputModeRequestMapper.OwnsPlayerInputManager;

            LogStep(
                logger,
                "no-unity-input-behavior",
                passed,
                LogFields.Of(
                    LogFields.Field("targetMode", request.TargetMode.ToString()),
                    LogFields.Field("actionMapSwitching", PauseInputModeRequestMapper.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", PauseInputModeRequestMapper.AppliesInputBehavior),
                    LogFields.Field("playerInputOwnership", PauseInputModeRequestMapper.OwnsPlayerInput ? "framework" : "none"),
                    LogFields.Field("playerInputManagerOwnership", PauseInputModeRequestMapper.OwnsPlayerInputManager ? "framework" : "none"),
                    LogFields.Field("pauseRuntimeDispatch", "none")));

            return passed;
        }

        private static void LogRequestStep(FrameworkLogger logger, string step, bool passed, PauseState pauseState, InputModeRequest request, string trigger)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("pauseState", pauseState.ToString()),
                    LogFields.Field("targetMode", request.TargetMode.ToString()),
                    LogFields.Field("requester", request.Requester),
                    LogFields.Field("trigger", trigger.NormalizeTextOrFallback("unknown")),
                    LogFields.Field("actionMapSwitching", PauseInputModeRequestMapper.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", PauseInputModeRequestMapper.AppliesInputBehavior)));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            if (passed)
            {
                logger.Info("QA Pause InputMode Request Boundary Smoke step completed.", PrependStepFields(step, passed, fields));
                return;
            }

            logger.Warning("QA Pause InputMode Request Boundary Smoke step failed.", PrependStepFields(step, passed, fields));
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
