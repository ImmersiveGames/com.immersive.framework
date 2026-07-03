using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Pause;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F23E Pause boundary reclassification.
    /// It validates intent/requirement contracts only; it does not bind anchors, show overlays, read input, mutate Time.timeScale or create UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F23E Pause intent boundary diagnostics smoke.")]
    internal static class PauseBoundaryIntentQaSmokeRunner
    {
        internal const string SmokeName = "Pause Boundary Intent Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PauseBoundaryIntentQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool contentPassed = ValidateContentRequirementIntent(logger, normalizedSource);
                bool presentationPassed = ValidatePresentationIntent(logger, normalizedSource);
                bool inputPassed = ValidateInputIntent(logger, normalizedSource);
                bool boundaryPassed = ValidateCanonicalBoundary(logger);

                return Task.FromResult(contractsPassed
                    && contentPassed
                    && presentationPassed
                    && inputPassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Pause Boundary Intent Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var requirementId = PauseContentRequirementId.From("qa.pause.requirement.contracts");
            var actionId = PauseInputActionId.From("qa.pause.input.contracts.toggle");

            bool passed = requirementId is { IsValid: true, Domain: Identity.FrameworkIdentityDomain.Pause }
                && actionId is { IsValid: true, Domain: Identity.FrameworkIdentityDomain.Pause }
                && PauseContentRequirementPurpose.PresentationRoot != PauseContentRequirementPurpose.Unknown
                && PauseInputCommandKind.TogglePause != PauseInputCommandKind.Unknown;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Pause"),
                    LogFields.Field("requirement", requirementId.StableText),
                    LogFields.Field("action", actionId.StableText),
                    LogFields.Field("domain", requirementId.Domain.ToString()),
                    LogFields.Field("content", nameof(PauseContentRequirement)),
                    LogFields.Field("presentation", nameof(PausePresentationIntent)),
                    LogFields.Field("input", nameof(PauseInputIntent))));

            return passed;
        }

        private static bool ValidateContentRequirementIntent(FrameworkLogger logger, string source)
        {
            var owner = RuntimeContentOwner.Activity("qa.pause.activity.owner", "QA Pause Activity Owner");
            var requirement = PauseContentRequirement.RequiredPresentationRoot(
                PauseContentRequirementId.From("qa.pause.requirement.presentation-root"),
                PauseState.Paused,
                RuntimeContentScope.Activity,
                owner,
                ContentAnchorScope.Activity,
                ContentAnchorId.From("qa.pause.anchor.presentation-root"),
                source,
                "pause content requirement intent");

            bool passed = requirement is { IsValid: true, IsRequired: true, Purpose: PauseContentRequirementPurpose.PresentationRoot, PauseState: PauseState.Paused, RuntimeScope: RuntimeContentScope.Activity }
                && requirement.Owner.Equals(owner)
                && requirement is { AnchorScope: ContentAnchorScope.Activity, AnchorKind: ContentAnchorKind.Root, AnchorId: { IsValid: true } };

            LogStep(
                logger,
                "content-requirement-intent",
                passed,
                LogFields.Of(
                    LogFields.Field("requirement", requirement.RequirementId.StableText),
                    LogFields.Field("purpose", requirement.Purpose.ToString()),
                    LogFields.Field("pauseState", requirement.PauseState.ToString()),
                    LogFields.Field("runtimeScope", requirement.RuntimeScope.ToString()),
                    LogFields.Field("owner", requirement.Owner.StableText),
                    LogFields.Field("anchorScope", requirement.AnchorScope.ToString()),
                    LogFields.Field("anchorKind", requirement.AnchorKind.ToString()),
                    LogFields.Field("anchor", requirement.AnchorId.StableText),
                    LogFields.Field("binding", "none"),
                    LogFields.Field("materialization", "none")));

            return passed;
        }

        private static bool ValidatePresentationIntent(FrameworkLogger logger, string source)
        {
            var snapshot = PauseSnapshot.FromState(
                PauseState.Paused,
                source,
                "presentation intent",
                new[] { "qa.pause.presentation.intent" });
            var owner = RuntimeContentOwner.Activity("qa.pause.activity.owner", "QA Pause Activity Owner");
            var requirement = PauseContentRequirement.RequiredPresentationRoot(
                PauseContentRequirementId.From("qa.pause.requirement.presentation-intent"),
                snapshot.State,
                RuntimeContentScope.Activity,
                owner,
                ContentAnchorScope.Activity,
                ContentAnchorId.From("qa.pause.anchor.presentation-intent"),
                source,
                "pause presentation intent");
            var presentation = PausePresentationIntent.FromSnapshot(
                snapshot,
                true,
                requirement,
                "Paused",
                "QA pause presentation intent",
                source);

            bool passed = presentation is { IsValid: true, ShouldBeVisible: true, HasContentRequirement: true }
                && presentation.ContentRequirement.Equals(requirement)
                && presentation.Snapshot.IsPaused
                && presentation is { HasTitle: true, HasDetail: true };

            LogStep(
                logger,
                "presentation-intent",
                passed,
                LogFields.Of(
                    LogFields.Field("state", presentation.Snapshot.State.ToString()),
                    LogFields.Field("visible", presentation.ShouldBeVisible),
                    LogFields.Field("hasContentRequirement", presentation.HasContentRequirement),
                    LogFields.Field("title", presentation.Title),
                    LogFields.Field("overlayAdapter", "none"),
                    LogFields.Field("ui", "none")));

            return passed;
        }

        private static bool ValidateInputIntent(FrameworkLogger logger, string source)
        {
            var toggleSignal = PauseInputSignal.Toggle(
                PauseInputActionId.From("qa.pause.input.intent.toggle"),
                PauseInputSourceKind.Synthetic,
                source,
                "toggle pause intent");
            var menuSignal = PauseInputSignal.MenuCommand(
                PauseInputActionId.From("qa.pause.input.intent.navigate-down"),
                PauseInputCommandKind.NavigateDown,
                PauseInputSourceKind.Synthetic,
                source,
                "menu navigation intent");
            var toggleIntent = PauseInputIntent.FromSignal(toggleSignal, source, "toggle pause intent");
            var menuIntent = PauseInputIntent.FromSignal(menuSignal, source, "menu navigation intent");

            bool passed = toggleIntent is { IsValid: true, IsPauseStateIntent: true, IsMenuIntent: false }
                && menuIntent is { IsValid: true, IsPauseStateIntent: false, IsMenuIntent: true }
                && toggleIntent.CommandKind == PauseInputCommandKind.TogglePause
                && menuIntent.CommandKind == PauseInputCommandKind.NavigateDown;

            LogStep(
                logger,
                "input-intent",
                passed,
                LogFields.Of(
                    LogFields.Field("toggleAction", toggleIntent.ActionId.StableText),
                    LogFields.Field("toggleCommand", toggleIntent.CommandKind.ToString()),
                    LogFields.Field("togglePauseStateIntent", toggleIntent.IsPauseStateIntent),
                    LogFields.Field("menuAction", menuIntent.ActionId.StableText),
                    LogFields.Field("menuCommand", menuIntent.CommandKind.ToString()),
                    LogFields.Field("menuIntent", menuIntent.IsMenuIntent),
                    LogFields.Field("inputSystem", "none"),
                    LogFields.Field("resolver", "none"),
                    LogFields.Field("dispatch", "none")));

            return passed;
        }

        private static bool ValidateCanonicalBoundary(FrameworkLogger logger)
        {
            bool passed = typeof(PauseContentRequirement).Namespace == "Immersive.Framework.Pause"
                && typeof(PausePresentationIntent).Namespace == "Immersive.Framework.Pause"
                && typeof(PauseInputIntent).Namespace == "Immersive.Framework.Pause";

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.Pause"),
                    LogFields.Field("contentRequirement", nameof(PauseContentRequirement)),
                    LogFields.Field("presentationIntent", nameof(PausePresentationIntent)),
                    LogFields.Field("inputIntent", nameof(PauseInputIntent)),
                    LogFields.Field("anchorBinding", "none"),
                    LogFields.Field("overlayAdapter", "none"),
                    LogFields.Field("inputAdapter", "none"),
                    LogFields.Field("inputSystem", "none"),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("timeScale", "none"),
                    LogFields.Field("gameplayAdapters", "none"),
                    LogFields.Field("unityBuild", "deferredToF24")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] stepFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));
            if (passed)
            {
                logger.Info("QA Pause Boundary Intent Smoke step completed.", stepFields);
                logger.Debug("QA Pause Boundary Intent Smoke step diagnostics.", fields);
                return;
            }

            logger.Warning("QA Pause Boundary Intent Smoke step failed.", stepFields);
            logger.Debug("QA Pause Boundary Intent Smoke failure diagnostics.", fields);
        }
    }
}
#endif
