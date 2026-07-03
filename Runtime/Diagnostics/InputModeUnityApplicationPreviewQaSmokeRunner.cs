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
    /// API status: Development Tooling. Smoke for F32A InputMode Unity application preview.
    /// It validates mapping from logical InputMode requests to official Unity Input evidence only; it does not switch action maps or own PlayerInput.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F32A InputMode Unity application preview smoke.")]
    internal static class InputModeUnityApplicationPreviewQaSmokeRunner
    {
        internal const string SmokeName = "InputMode Unity Application Preview Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPreviewQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool gameplayPassed = ValidateGameplayRequiresPlayerActorAndSessionManager(logger, normalizedSource);
                bool pauseOverlayPassed = ValidatePauseOverlayTargetsGlobalUi(logger, normalizedSource);
                bool missingActorPassed = ValidateMissingPlayerActorBlocking(logger, normalizedSource);
                bool missingSessionManagerPassed = ValidateMissingSessionPlayerInputManagerBlocking(logger, normalizedSource);
                bool lockedPassed = ValidateInputLockedNoTargetRequired(logger, normalizedSource);
                bool noBehaviorPassed = ValidateNoUnityInputBehavior(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && gameplayPassed
                    && pauseOverlayPassed
                    && missingActorPassed
                    && missingSessionManagerPassed
                    && lockedPassed
                    && noBehaviorPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA InputMode Unity Application Preview Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            bool passed = InputModeUnityApplicationPreviewStatus.Succeeded != InputModeUnityApplicationPreviewStatus.Unknown
                && InputModeUnityApplicationPreviewIssueKind.MissingRequiredPlayerActor != InputModeUnityApplicationPreviewIssueKind.None;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.InputMode"),
                    LogFields.Field("unityAuthority", "PlayerInput/PlayerInputManager"),
                    LogFields.Field("frameworkRole", "PreviewOnly"),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));

            return passed;
        }

        private static bool ValidateGameplayRequiresPlayerActorAndSessionManager(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.Gameplay,
                ValidTargetSet(source),
                ValidPlayerActorSet(source),
                ValidSessionManagerEvidence(source),
                source,
                "qa.inputmode.application.gameplay");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.Gameplay
                && preview.TargetRole == UnityInputTargetRole.GameplayCommands
                && preview.TargetRequired
                && preview.TargetAvailable
                && preview.PlayerActorRequired
                && preview.PlayerActorAvailable
                && preview.SessionPlayerInputManagerRequired
                && preview.SessionPlayerInputManagerAvailable
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior
                && !preview.ActivatesPlayerInput
                && !preview.CallsPlayerJoin;

            LogPreviewStep(logger, "gameplay-requires-playeractor-session-manager", passed, preview);
            return passed;
        }

        private static bool ValidatePauseOverlayTargetsGlobalUi(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.PauseOverlay,
                ValidTargetSet(source),
                EmptyPlayerActorSet(source),
                MissingSessionManagerEvidence(source),
                source,
                "qa.inputmode.application.pause-overlay");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.PauseOverlay
                && preview.TargetRole == UnityInputTargetRole.GlobalUiPause
                && preview.TargetRequired
                && preview.TargetAvailable
                && !preview.PlayerActorRequired
                && !preview.SessionPlayerInputManagerRequired
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "pause-overlay-targets-global-ui", passed, preview);
            return passed;
        }

        private static bool ValidateMissingPlayerActorBlocking(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.Gameplay,
                ValidTargetSet(source),
                EmptyPlayerActorSet(source),
                ValidSessionManagerEvidence(source),
                source,
                "qa.inputmode.application.missing-playeractor");

            bool passed = preview.Failed
                && preview.Status == InputModeUnityApplicationPreviewStatus.FailedPlayerActorEvidence
                && preview.PlayerActorRequired
                && !preview.PlayerActorAvailable
                && preview.BlockingIssueCount == 1
                && ContainsIssue(preview, InputModeUnityApplicationPreviewIssueKind.MissingRequiredPlayerActor)
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "missing-playeractor-blocking", passed, preview);
            return passed;
        }

        private static bool ValidateMissingSessionPlayerInputManagerBlocking(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.Gameplay,
                ValidTargetSet(source),
                ValidPlayerActorSet(source),
                MissingSessionManagerEvidence(source),
                source,
                "qa.inputmode.application.missing-session-manager");

            bool passed = preview.Failed
                && preview.Status == InputModeUnityApplicationPreviewStatus.FailedSessionPlayerInputManagerEvidence
                && preview.SessionPlayerInputManagerRequired
                && !preview.SessionPlayerInputManagerAvailable
                && preview.BlockingIssueCount == 1
                && ContainsIssue(preview, InputModeUnityApplicationPreviewIssueKind.MissingRequiredSessionPlayerInputManager)
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "missing-session-playerinputmanager-blocking", passed, preview);
            return passed;
        }

        private static bool ValidateInputLockedNoTargetRequired(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.InputLocked,
                null,
                null,
                null,
                source,
                "qa.inputmode.application.input-locked");

            bool passed = preview.Succeeded
                && preview.RequestedMode == InputModeKind.InputLocked
                && preview.TargetRole == UnityInputTargetRole.Unknown
                && !preview.TargetRequired
                && !preview.PlayerActorRequired
                && !preview.SessionPlayerInputManagerRequired
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior;

            LogPreviewStep(logger, "inputlocked-no-target-required", passed, preview);
            return passed;
        }

        private static bool ValidateNoUnityInputBehavior(FrameworkLogger logger, string source)
        {
            InputModeUnityApplicationPreviewResult preview = CreatePreview(
                InputModeKind.Gameplay,
                ValidTargetSet(source),
                ValidPlayerActorSet(source),
                ValidSessionManagerEvidence(source),
                source,
                "qa.inputmode.application.no-behavior");

            bool passed = preview.Succeeded
                && !preview.SwitchesActionMaps
                && !preview.AppliesInputBehavior
                && !preview.ActivatesPlayerInput
                && !preview.CallsPlayerJoin
                && !preview.SpawnsActor;

            LogStep(
                logger,
                "no-unity-input-behavior",
                passed,
                LogFields.Of(
                    LogFields.Field("requestedMode", preview.RequestedMode.ToString()),
                    LogFields.Field("targetRole", preview.TargetRole.ToString()),
                    LogFields.Field("actionMapSwitching", preview.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", preview.AppliesInputBehavior),
                    LogFields.Field("playerInputActivation", preview.ActivatesPlayerInput),
                    LogFields.Field("playerJoin", preview.CallsPlayerJoin),
                    LogFields.Field("actorSpawning", preview.SpawnsActor),
                    LogFields.Field("customInputManager", false)));

            return passed;
        }

        private static InputModeUnityApplicationPreviewResult CreatePreview(
            InputModeKind targetMode,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            UnityInputPlayerInputManagerEvidence sessionManagerEvidence,
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
                targetSet,
                playerActorSet,
                sessionManagerEvidence,
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
                        "qa.inputmode.application.targets"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.target.gameplay-commands"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Commands Target",
                        "Synthetic",
                        "qa.input.target.gameplay-commands",
                        source,
                        "qa.inputmode.application.targets")
                },
                source,
                "qa.inputmode.application.targets");
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
                        "qa.inputmode.application.playeractor")
                },
                source,
                "qa.inputmode.application.playeractor");
        }

        private static PlayerActorSet EmptyPlayerActorSet(string source)
        {
            return PlayerActorSet.FromDescriptors(
                Array.Empty<PlayerActorDescriptor>(),
                source,
                "qa.inputmode.application.no-playeractor");
        }

        private static UnityInputPlayerInputManagerEvidence ValidSessionManagerEvidence(string source)
        {
            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(
                1,
                source,
                "qa.inputmode.application.session-manager");
        }

        private static UnityInputPlayerInputManagerEvidence MissingSessionManagerEvidence(string source)
        {
            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(
                0,
                source,
                "qa.inputmode.application.missing-session-manager");
        }

        private static bool ContainsIssue(InputModeUnityApplicationPreviewResult preview, InputModeUnityApplicationPreviewIssueKind kind)
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

        private static void LogPreviewStep(FrameworkLogger logger, string step, bool passed, InputModeUnityApplicationPreviewResult preview)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("status", preview.Status.ToString()),
                    LogFields.Field("requestedMode", preview.RequestedMode.ToString()),
                    LogFields.Field("targetRole", preview.TargetRole.ToString()),
                    LogFields.Field("targetRequired", preview.TargetRequired),
                    LogFields.Field("targetAvailable", preview.TargetAvailable),
                    LogFields.Field("playerActorRequired", preview.PlayerActorRequired),
                    LogFields.Field("playerActorAvailable", preview.PlayerActorAvailable),
                    LogFields.Field("sessionPlayerInputManagerRequired", preview.SessionPlayerInputManagerRequired),
                    LogFields.Field("sessionPlayerInputManagerAvailable", preview.SessionPlayerInputManagerAvailable),
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
                "QA InputMode Unity Application Preview Smoke step completed.",
                stepFields);
        }
    }
}
#endif
