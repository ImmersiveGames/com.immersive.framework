using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F29A Unity Input target ownership declaration.
    /// It validates target declarations and diagnostics only; it does not switch action maps, read input, spawn players or create InputMode runtime.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F29A Unity Input target ownership declaration smoke.")]
    internal static class UnityInputTargetOwnershipQaSmokeRunner
    {
        internal const string SmokeName = "Unity Input Target Ownership Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputTargetOwnershipQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool validSetPassed = ValidateValidTargetSet(logger, normalizedSource);
                bool missingPassed = ValidateMissingRequiredTarget(logger, normalizedSource);
                bool duplicatePassed = ValidateDuplicateTarget(logger, normalizedSource);
                bool splitPassed = ValidateGlobalUiAndGameplaySplit(logger, normalizedSource);
                bool noSwitchingPassed = ValidateNoActionMapSwitching(logger, normalizedSource);
                bool loadedSceneFixturePassed = ValidateLoadedSceneFixture(logger, normalizedSource);
                bool declarationPassed = ValidateDeclarationComponent(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && validSetPassed
                    && missingPassed
                    && duplicatePassed
                    && splitPassed
                    && noSwitchingPassed
                    && loadedSceneFixturePassed
                    && declarationPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Unity Input Target Ownership Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var global = UnityInputTargetId.From("qa.input.target.global-ui-pause");
            var gameplay = UnityInputTargetId.From("qa.input.target.gameplay-commands");

            bool passed = global.IsValid
                && gameplay.IsValid
                && global.Domain == Identity.FrameworkIdentityDomain.UnityInput
                && gameplay.Domain == Identity.FrameworkIdentityDomain.UnityInput
                && UnityInputTargetRole.GlobalUiPause != UnityInputTargetRole.Unknown
                && UnityInputTargetRole.GameplayCommands != UnityInputTargetRole.Unknown;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.UnityInput"),
                    LogFields.Field("globalTarget", global.StableText),
                    LogFields.Field("gameplayTarget", gameplay.StableText),
                    LogFields.Field("domain", global.Domain.ToString()),
                    LogFields.Field("roleGlobal", UnityInputTargetRole.GlobalUiPause.ToString()),
                    LogFields.Field("roleGameplay", UnityInputTargetRole.GameplayCommands.ToString())));

            return passed;
        }

        private static bool ValidateValidTargetSet(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetSet.FromDescriptors(
                CreateValidDescriptors(source),
                source,
                "qa.input.target.valid-set");

            bool passed = set.Succeeded
                && set.Count == 2
                && set.IssueCount == 0
                && set.GlobalUiPauseTargetCount == 1
                && set.GameplayCommandTargetCount == 1
                && set.TryGetSingle(UnityInputTargetRole.GlobalUiPause, out _)
                && set.TryGetSingle(UnityInputTargetRole.GameplayCommands, out _);

            LogSetStep(logger, "valid-target-set", passed, set);
            return passed;
        }

        private static bool ValidateMissingRequiredTarget(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetSet.FromDescriptors(
                new[]
                {
                    CreateDescriptor(UnityInputTargetRole.GlobalUiPause, "qa.input.target.global-ui-pause", true, source, "global only")
                },
                source,
                "qa.input.target.missing-required-target");

            bool passed = set.Failed
                && set.BlockingIssueCount == 1
                && set.GlobalUiPauseTargetCount == 1
                && set.GameplayCommandTargetCount == 0
                && ContainsIssue(set, UnityInputTargetSetIssueKind.MissingRequiredRole, UnityInputTargetRole.GameplayCommands);

            LogSetStep(logger, "missing-required-target", passed, set);
            return passed;
        }

        private static bool ValidateDuplicateTarget(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetSet.FromDescriptors(
                new[]
                {
                    CreateDescriptor(UnityInputTargetRole.GlobalUiPause, "qa.input.target.global-ui-pause", true, source, "global one"),
                    CreateDescriptor(UnityInputTargetRole.GlobalUiPause, "qa.input.target.global-ui-pause-copy", true, source, "global duplicate"),
                    CreateDescriptor(UnityInputTargetRole.GameplayCommands, "qa.input.target.gameplay-commands", false, source, "gameplay")
                },
                source,
                "qa.input.target.duplicate-target");

            bool passed = set.Failed
                && set.BlockingIssueCount == 1
                && set.GlobalUiPauseTargetCount == 2
                && set.GameplayCommandTargetCount == 1
                && ContainsIssue(set, UnityInputTargetSetIssueKind.DuplicateRequiredRole, UnityInputTargetRole.GlobalUiPause);

            LogSetStep(logger, "duplicate-target", passed, set);
            return passed;
        }

        private static bool ValidateGlobalUiAndGameplaySplit(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetSet.FromDescriptors(
                CreateValidDescriptors(source),
                source,
                "qa.input.target.split");

            bool hasGlobal = set.TryGetSingle(UnityInputTargetRole.GlobalUiPause, out UnityInputTargetDescriptor global);
            bool hasGameplay = set.TryGetSingle(UnityInputTargetRole.GameplayCommands, out UnityInputTargetDescriptor gameplay);
            bool passed = set.Succeeded
                && hasGlobal
                && hasGameplay
                && global.Role != gameplay.Role
                && global.TargetId != gameplay.TargetId;

            LogStep(
                logger,
                "global-ui-and-gameplay-target-split",
                passed,
                LogFields.Of(
                    LogFields.Field("status", set.Succeeded ? "Succeeded" : "Failed"),
                    LogFields.Field("globalTarget", hasGlobal ? global.TargetId.StableText : "<missing>"),
                    LogFields.Field("gameplayTarget", hasGameplay ? gameplay.TargetId.StableText : "<missing>"),
                    LogFields.Field("globalHasPlayerInput", hasGlobal && global.HasPlayerInputReference),
                    LogFields.Field("gameplayHasPlayerInput", hasGameplay && gameplay.HasPlayerInputReference),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior)));

            return passed;
        }

        private static bool ValidateNoActionMapSwitching(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetSet.FromDescriptors(
                CreateValidDescriptors(source),
                source,
                "qa.input.target.no-action-map-switching");

            bool passed = set.Succeeded
                && !set.SwitchesActionMaps
                && !set.AppliesInputBehavior;

            LogStep(
                logger,
                "no-action-map-switching",
                passed,
                LogFields.Of(
                    LogFields.Field("targets", set.Count),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("inputMode", "none"),
                    LogFields.Field("playerActor", "none")));

            return passed;
        }

        private static bool ValidateLoadedSceneFixture(FrameworkLogger logger, string source)
        {
            var set = UnityInputTargetValidator.ValidateLoadedSceneDeclarations(
                source,
                "qa.input.target.loaded-scene-fixture");

            bool hasGlobal = set.TryGetSingle(UnityInputTargetRole.GlobalUiPause, out UnityInputTargetDescriptor global);
            bool hasGameplay = set.TryGetSingle(UnityInputTargetRole.GameplayCommands, out UnityInputTargetDescriptor gameplay);
            bool passed = set.Succeeded
                && set.Count == 2
                && hasGlobal
                && hasGameplay
                && global.Role != gameplay.Role
                && global.TargetId != gameplay.TargetId
                && !set.SwitchesActionMaps
                && !set.AppliesInputBehavior;

            LogStep(
                logger,
                "loaded-scene-fixture",
                passed,
                LogFields.Of(
                    LogFields.Field("targets", set.Count),
                    LogFields.Field("issues", set.IssueCount),
                    LogFields.Field("blockingIssues", set.BlockingIssueCount),
                    LogFields.Field("globalUiPauseTargets", set.GlobalUiPauseTargetCount),
                    LogFields.Field("gameplayCommandTargets", set.GameplayCommandTargetCount),
                    LogFields.Field("globalTarget", hasGlobal ? global.TargetId.StableText : "<missing>"),
                    LogFields.Field("gameplayTarget", hasGameplay ? gameplay.TargetId.StableText : "<missing>"),
                    LogFields.Field("globalScene", hasGlobal ? global.SceneName : string.Empty),
                    LogFields.Field("gameplayScene", hasGameplay ? gameplay.SceneName : string.Empty),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("diagnostics", set.ToDiagnosticString())));

            return passed;
        }

        private static bool ValidateDeclarationComponent(FrameworkLogger logger, string source)
        {
            var globalObject = new GameObject("QA Unity Input Global Target");
            var gameplayObject = new GameObject("QA Unity Input Gameplay Target");

            try
            {
                var global = globalObject.AddComponent<UnityInputTargetDeclaration>();
                global.ConfigureForDiagnostics(
                    UnityInputTargetRole.GlobalUiPause,
                    "qa.input.declaration.global-ui-pause",
                    "QA Global UI / Pause Target",
                    null,
                    "qa.input.target.declaration.global");

                var gameplay = gameplayObject.AddComponent<UnityInputTargetDeclaration>();
                gameplay.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.input.declaration.gameplay-commands",
                    "QA Gameplay Command Target",
                    null,
                    "qa.input.target.declaration.gameplay");

                var set = UnityInputTargetValidator.ValidateDeclarations(
                    new[] { global, gameplay },
                    source,
                    "qa.input.target.declaration-component");

                bool passed = set.Succeeded
                    && set.Count == 2
                    && set.GlobalUiPauseTargetCount == 1
                    && set.GameplayCommandTargetCount == 1
                    && !set.SwitchesActionMaps
                    && !set.AppliesInputBehavior;

                LogSetStep(logger, "declaration-component", passed, set);
                return passed;
            }
            finally
            {
                UnityEngine.Object.Destroy(globalObject);
                UnityEngine.Object.Destroy(gameplayObject);
            }
        }

        private static UnityInputTargetDescriptor[] CreateValidDescriptors(string source)
        {
            return new[]
            {
                CreateDescriptor(UnityInputTargetRole.GlobalUiPause, "qa.input.target.global-ui-pause", true, source, "global ui and pause target"),
                CreateDescriptor(UnityInputTargetRole.GameplayCommands, "qa.input.target.gameplay-commands", false, source, "gameplay command target")
            };
        }

        private static UnityInputTargetDescriptor CreateDescriptor(
            UnityInputTargetRole role,
            string targetId,
            bool hasPlayerInputReference,
            string source,
            string reason)
        {
            return new UnityInputTargetDescriptor(
                UnityInputTargetId.From(targetId),
                role,
                hasPlayerInputReference,
                targetId,
                "Synthetic",
                targetId,
                source,
                reason);
        }

        private static bool ContainsIssue(
            UnityInputTargetSet set,
            UnityInputTargetSetIssueKind kind,
            UnityInputTargetRole role)
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
                    LogFields.Field("globalUiPauseTargets", set.GlobalUiPauseTargetCount),
                    LogFields.Field("gameplayCommandTargets", set.GameplayCommandTargetCount),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("diagnostics", set.ToDiagnosticString())));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            if (passed)
            {
                logger.Info("QA Unity Input Target Ownership Smoke step completed.", PrependStepFields(step, passed, fields));
                return;
            }

            logger.Warning("QA Unity Input Target Ownership Smoke step failed.", PrependStepFields(step, passed, fields));
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
