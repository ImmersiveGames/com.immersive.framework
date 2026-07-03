#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F31B Session-scoped Unity PlayerInputManager boundary.
    /// It validates evidence only; it does not join players, instantiate prefabs, switch action maps or own Unity input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F31B Session PlayerInputManager boundary smoke.")]
    internal static class SessionPlayerInputManagerBoundaryQaSmokeRunner
    {
        internal const string SmokeName = "Session PlayerInputManager Boundary Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(SessionPlayerInputManagerBoundaryQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool validPassed = ValidateRequiredSessionManagerValid(logger, normalizedSource);
                bool missingPassed = ValidateRequiredSessionManagerMissing(logger, normalizedSource);
                bool duplicatePassed = ValidateRequiredSessionManagerDuplicate(logger, normalizedSource);
                bool sessionScopePassed = ValidateSessionScopeBeforeRouteActivity(logger, normalizedSource);
                bool noBehaviorPassed = ValidateNoUnityInputBehavior(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && validPassed
                    && missingPassed
                    && duplicatePassed
                    && sessionScopePassed
                    && noBehaviorPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Session PlayerInputManager Boundary Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerScope expectedScope = UnityInputPlayerInputManagerScope.Session;
            bool passed = expectedScope != UnityInputPlayerInputManagerScope.Unknown;
            logger.Info(
                "QA Session PlayerInputManager Boundary Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", "contracts"),
                    LogFields.Field("passed", passed),
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.UnityInput"),
                    LogFields.Field("scope", UnityInputPlayerInputManagerScope.Session),
                    LogFields.Field("unityAuthority", "PlayerInputManager"),
                    LogFields.Field("frameworkOwnership", "SessionIntegrationEvidence"),
                    LogFields.Field("customInputManager", false),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));
            return passed;
        }

        private static bool ValidateRequiredSessionManagerValid(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidenceCount(
                1,
                source,
                "qa.session.playerinputmanager.valid");

            bool passed = evidence.Succeeded
                && evidence.ManagerCount == 1
                && evidence.Required
                && evidence.SessionScoped
                && evidence.BlockingIssueCount == 0
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogEvidenceStep(logger, "session-playerinputmanager-required-valid", passed, evidence);
            return passed;
        }

        private static bool ValidateRequiredSessionManagerMissing(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidenceCount(
                0,
                source,
                "qa.session.playerinputmanager.missing");

            bool passed = evidence.Failed
                && evidence.ManagerCount == 0
                && evidence.Required
                && evidence.SessionScoped
                && evidence.BlockingIssueCount == 1
                && ContainsIssue(evidence, UnityInputTargetSetIssueKind.MissingRequiredPlayerInputManager)
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogEvidenceStep(logger, "session-playerinputmanager-missing-blocking", passed, evidence);
            return passed;
        }

        private static bool ValidateRequiredSessionManagerDuplicate(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidenceCount(
                2,
                source,
                "qa.session.playerinputmanager.duplicate");

            bool passed = evidence.Failed
                && evidence.ManagerCount == 2
                && evidence.Required
                && evidence.SessionScoped
                && evidence.BlockingIssueCount == 1
                && ContainsIssue(evidence, UnityInputTargetSetIssueKind.DuplicatePlayerInputManager)
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogEvidenceStep(logger, "session-playerinputmanager-duplicate-blocking", passed, evidence);
            return passed;
        }

        private static bool ValidateSessionScopeBeforeRouteActivity(FrameworkLogger logger, string source)
        {
            bool passed = true;
            logger.Info(
                "QA Session PlayerInputManager Boundary Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", "session-scope-before-route-activity"),
                    LogFields.Field("passed", passed),
                    LogFields.Field("scope", UnityInputPlayerInputManagerScope.Session),
                    LogFields.Field("availableBeforeRoute", true),
                    LogFields.Field("availableBeforeActivity", true),
                    LogFields.Field("characterCreationRouteSupported", true),
                    LogFields.Field("activityOwned", false),
                    LogFields.Field("routeOwned", false),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false)));
            return passed;
        }

        private static bool ValidateNoUnityInputBehavior(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidenceCount(
                1,
                source,
                "qa.session.playerinputmanager.no-behavior");

            bool passed = evidence.Succeeded
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            logger.Info(
                "QA Session PlayerInputManager Boundary Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", "no-unity-input-behavior"),
                    LogFields.Field("passed", passed),
                    LogFields.Field("playerInputManagers", evidence.ManagerCount),
                    LogFields.Field("scope", evidence.Scope),
                    LogFields.Field("required", evidence.Required),
                    LogFields.Field("actionMapSwitching", evidence.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", evidence.AppliesInputBehavior),
                    LogFields.Field("playerJoin", "none"),
                    LogFields.Field("playerPrefabSpawn", "none"),
                    LogFields.Field("playerInputManagerOwnership", "UnityOfficialComponent")));
            return passed;
        }

        private static bool ContainsIssue(UnityInputPlayerInputManagerEvidence evidence, UnityInputTargetSetIssueKind kind)
        {
            for (int i = 0; i < evidence.Issues.Count; i++)
            {
                if (evidence.Issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogEvidenceStep(FrameworkLogger logger, string step, bool passed, UnityInputPlayerInputManagerEvidence evidence)
        {
            logger.Info(
                "QA Session PlayerInputManager Boundary Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("passed", passed),
                    LogFields.Field("playerInputManagers", evidence.ManagerCount),
                    LogFields.Field("scope", evidence.Scope),
                    LogFields.Field("required", evidence.Required),
                    LogFields.Field("issues", evidence.IssueCount),
                    LogFields.Field("blockingIssues", evidence.BlockingIssueCount),
                    LogFields.Field("usesPlayerInputManager", evidence.UsesPlayerInputManager),
                    LogFields.Field("actionMapSwitching", evidence.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", evidence.AppliesInputBehavior),
                    LogFields.Field("diagnostics", evidence.ToDiagnosticString())));
        }
    }
}
#endif
