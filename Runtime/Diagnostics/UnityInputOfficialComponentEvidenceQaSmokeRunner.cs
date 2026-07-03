using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F30C official Unity Input component evidence.
    /// It validates PlayerInput/PlayerInputManager evidence only; it does not activate input, switch action maps or join players.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F30C Unity PlayerInput component evidence smoke.")]
    internal static class UnityInputOfficialComponentEvidenceQaSmokeRunner
    {
        internal const string SmokeName = "Unity Input Official Component Evidence Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputOfficialComponentEvidenceQaSmokeRunner));

            try
            {
                bool playerInputValidPassed = ValidateRequiredPlayerInputEvidenceValid(logger, normalizedSource);
                bool playerInputMissingPassed = ValidateRequiredPlayerInputEvidenceMissing(logger, normalizedSource);
                bool managerNonePassed = ValidatePlayerInputManagerOptionalNone(logger, normalizedSource);
                bool managerSinglePassed = ValidatePlayerInputManagerSingleValid(logger, normalizedSource);
                bool managerDuplicatePassed = ValidatePlayerInputManagerDuplicateBlocking(logger, normalizedSource);
                bool noSwitchingPassed = ValidateNoActionMapSwitching(logger, normalizedSource);

                return Task.FromResult(playerInputValidPassed
                    && playerInputMissingPassed
                    && managerNonePassed
                    && managerSinglePassed
                    && managerDuplicatePassed
                    && noSwitchingPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Unity Input Official Component Evidence Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateRequiredPlayerInputEvidenceValid(FrameworkLogger logger, string source)
        {
            var targetObject = new GameObject("QA PlayerInput Evidence Target");
            var globalObject = new GameObject("QA Global UI Pause Evidence Target");
            try
            {
                PlayerInput playerInput = targetObject.AddComponent<PlayerInput>();
                var declaration = targetObject.AddComponent<UnityInputTargetDeclaration>();
                declaration.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.input.evidence.playerinput.valid",
                    "QA PlayerInput Evidence Target",
                    playerInput,
                    "qa.input.evidence.playerinput.valid",
                    true);

                UnityInputTargetSet set = UnityInputTargetValidator.ValidateDeclarations(
                    new[] { declaration, CreateGlobalTarget(globalObject, source) },
                    source,
                    "qa.input.evidence.playerinput.valid");

                bool passed = set.Succeeded
                    && set.PlayerInputReferenceCount == 1
                    && set.RequiredPlayerInputEvidenceCount == 1
                    && !set.SwitchesActionMaps
                    && !set.AppliesInputBehavior;

                LogSetStep(logger, "playerinput-required-evidence-valid", passed, set);
                return passed;
            }
            finally
            {
                UnityEngine.Object.Destroy(targetObject);
                UnityEngine.Object.Destroy(globalObject);
            }
        }

        private static bool ValidateRequiredPlayerInputEvidenceMissing(FrameworkLogger logger, string source)
        {
            var targetObject = new GameObject("QA Missing PlayerInput Evidence Target");
            var globalObject = new GameObject("QA Global UI Pause Evidence Target");
            try
            {
                var declaration = targetObject.AddComponent<UnityInputTargetDeclaration>();
                declaration.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.input.evidence.playerinput.missing",
                    "QA Missing PlayerInput Evidence Target",
                    null,
                    "qa.input.evidence.playerinput.missing",
                    true);

                UnityInputTargetSet set = UnityInputTargetValidator.ValidateDeclarations(
                    new[] { declaration, CreateGlobalTarget(globalObject, source) },
                    source,
                    "qa.input.evidence.playerinput.missing");

                bool passed = set.Failed
                    && set.BlockingIssueCount == 1
                    && set.PlayerInputReferenceCount == 0
                    && set.RequiredPlayerInputEvidenceCount == 1
                    && ContainsIssue(set, UnityInputTargetSetIssueKind.MissingRequiredPlayerInputEvidence, UnityInputTargetRole.GameplayCommands)
                    && !set.SwitchesActionMaps
                    && !set.AppliesInputBehavior;

                LogSetStep(logger, "playerinput-required-evidence-missing", passed, set);
                return passed;
            }
            finally
            {
                UnityEngine.Object.Destroy(targetObject);
                UnityEngine.Object.Destroy(globalObject);
            }
        }

        private static bool ValidatePlayerInputManagerOptionalNone(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidatePlayerInputManagerEvidenceCount(
                0,
                source,
                "qa.input.evidence.playerinputmanager.none");

            bool passed = evidence.Succeeded
                && evidence.ManagerCount == 0
                && !evidence.UsesPlayerInputManager
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogManagerStep(logger, "playerinputmanager-optional-none", passed, evidence);
            return passed;
        }

        private static bool ValidatePlayerInputManagerSingleValid(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidatePlayerInputManagerEvidenceCount(
                1,
                source,
                "qa.input.evidence.playerinputmanager.single");

            bool passed = evidence.Succeeded
                && evidence.ManagerCount == 1
                && evidence.UsesPlayerInputManager
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogManagerStep(logger, "playerinputmanager-single-valid", passed, evidence);
            return passed;
        }

        private static bool ValidatePlayerInputManagerDuplicateBlocking(FrameworkLogger logger, string source)
        {
            UnityInputPlayerInputManagerEvidence evidence = UnityInputTargetValidator.ValidatePlayerInputManagerEvidenceCount(
                2,
                source,
                "qa.input.evidence.playerinputmanager.duplicate");

            bool passed = evidence.Failed
                && evidence.ManagerCount == 2
                && evidence.BlockingIssueCount == 1
                && ContainsIssue(evidence, UnityInputTargetSetIssueKind.DuplicatePlayerInputManager)
                && !evidence.SwitchesActionMaps
                && !evidence.AppliesInputBehavior;

            LogManagerStep(logger, "playerinputmanager-duplicate-blocking", passed, evidence);
            return passed;
        }

        private static bool ValidateNoActionMapSwitching(FrameworkLogger logger, string source)
        {
            UnityInputTargetSet targetSet = UnityInputTargetSet.FromDescriptors(
                new[]
                {
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.evidence.global"),
                        UnityInputTargetRole.GlobalUiPause,
                        false,
                        false,
                        "QA Global Target",
                        "Synthetic",
                        "qa.input.evidence.global",
                        source,
                        "no action map switching"),
                    new UnityInputTargetDescriptor(
                        UnityInputTargetId.From("qa.input.evidence.gameplay"),
                        UnityInputTargetRole.GameplayCommands,
                        true,
                        true,
                        "QA Gameplay Target",
                        "Synthetic",
                        "qa.input.evidence.gameplay",
                        source,
                        "no action map switching")
                },
                source,
                "qa.input.evidence.no-action-map-switching");

            UnityInputPlayerInputManagerEvidence managerEvidence = UnityInputPlayerInputManagerEvidence.FromManagers(
                Array.Empty<PlayerInputManager>(),
                source,
                "qa.input.evidence.no-action-map-switching.manager");

            bool passed = targetSet.Succeeded
                && managerEvidence.Succeeded
                && !targetSet.SwitchesActionMaps
                && !targetSet.AppliesInputBehavior
                && !managerEvidence.SwitchesActionMaps
                && !managerEvidence.AppliesInputBehavior;

            LogStep(
                logger,
                "no-action-map-switching",
                passed,
                LogFields.Of(
                    LogFields.Field("targets", targetSet.Count),
                    LogFields.Field("playerInputReferences", targetSet.PlayerInputReferenceCount),
                    LogFields.Field("requiredPlayerInputEvidence", targetSet.RequiredPlayerInputEvidenceCount),
                    LogFields.Field("playerInputManagers", managerEvidence.ManagerCount),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false),
                    LogFields.Field("playerInputActivation", "none"),
                    LogFields.Field("playerJoin", "none")));

            return passed;
        }

        private static UnityInputTargetDeclaration CreateGlobalTarget(GameObject globalObject, string source)
        {
            var global = globalObject.AddComponent<UnityInputTargetDeclaration>();
            global.ConfigureForDiagnostics(
                UnityInputTargetRole.GlobalUiPause,
                "qa.input.evidence.global-ui-pause",
                "QA Global UI / Pause Evidence Target",
                null,
                source,
                false);
            return global;
        }

        private static bool ContainsIssue(UnityInputTargetSet set, UnityInputTargetSetIssueKind kind, UnityInputTargetRole role)
        {
            for (int i = 0; i < set.Issues.Count; i++)
            {
                UnityInputTargetSetIssue issue = set.Issues[i];
                if (issue.Kind == kind && issue.Role == role && issue.Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIssue(UnityInputPlayerInputManagerEvidence evidence, UnityInputTargetSetIssueKind kind)
        {
            for (int i = 0; i < evidence.Issues.Count; i++)
            {
                UnityInputTargetSetIssue issue = evidence.Issues[i];
                if (issue.Kind == kind && issue.Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogSetStep(FrameworkLogger logger, string step, bool passed, UnityInputTargetSet set)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("targets", set.Count),
                    LogFields.Field("issues", set.IssueCount),
                    LogFields.Field("blockingIssues", set.BlockingIssueCount),
                    LogFields.Field("playerInputReferences", set.PlayerInputReferenceCount),
                    LogFields.Field("requiredPlayerInputEvidence", set.RequiredPlayerInputEvidenceCount),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("diagnostics", set.ToDiagnosticString())));
        }

        private static void LogManagerStep(FrameworkLogger logger, string step, bool passed, UnityInputPlayerInputManagerEvidence evidence)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("playerInputManagers", evidence.ManagerCount),
                    LogFields.Field("issues", evidence.IssueCount),
                    LogFields.Field("blockingIssues", evidence.BlockingIssueCount),
                    LogFields.Field("usesPlayerInputManager", evidence.UsesPlayerInputManager),
                    LogFields.Field("actionMapSwitching", evidence.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", evidence.AppliesInputBehavior),
                    LogFields.Field("diagnostics", evidence.ToDiagnosticString())));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            if (passed)
            {
                logger.Info("QA Unity Input Official Component Evidence Smoke step completed.", PrependStepFields(step, passed, fields));
                return;
            }

            logger.Warning("QA Unity Input Official Component Evidence Smoke step failed.", PrependStepFields(step, passed, fields));
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
