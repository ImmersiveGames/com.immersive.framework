using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F31A PlayerActor identity and PlayerInput evidence.
    /// It validates actor identity and official PlayerInput evidence only; it does not move, spawn, join players or switch action maps.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F31A PlayerActor identity and PlayerInput evidence smoke.")]
    internal static class PlayerActorIdentityQaSmokeRunner
    {
        internal const string SmokeName = "PlayerActor Identity Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerActorIdentityQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool validPassed = ValidatePlayerActorWithPlayerInput(logger, normalizedSource);
                bool missingPassed = ValidateMissingPlayerInputBlocking(logger, normalizedSource);
                bool duplicatePassed = ValidateDuplicatePlayerActorIdBlocking(logger, normalizedSource);
                bool noBehaviorPassed = ValidateNoInputBehavior(logger, normalizedSource);

                return Task.FromResult(contractsPassed
                    && validPassed
                    && missingPassed
                    && duplicatePassed
                    && noBehaviorPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA PlayerActor Identity Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var actorId = ActorId.From("qa.actor.player.primary");
            bool passed = actorId is { IsValid: true, Domain: FrameworkIdentityDomain.Actor, StableText: "Actor:qa.actor.player.primary" }
                && ActorKind.Player != ActorKind.Unknown;

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Actors"),
                    LogFields.Field("domain", actorId.Domain.ToString()),
                    LogFields.Field("actor", actorId.StableText),
                    LogFields.Field("kind", ActorKind.Player.ToString()),
                    LogFields.Field("requiresPlayerInput", true),
                    LogFields.Field("actionMapSwitching", false),
                    LogFields.Field("inputBehavior", false),
                    LogFields.Field("actorSpawning", false)));

            return passed;
        }

        private static bool ValidatePlayerActorWithPlayerInput(FrameworkLogger logger, string source)
        {
            var actorObject = new GameObject("QA PlayerActor With PlayerInput");
            try
            {
                PlayerInput playerInput = actorObject.AddComponent<PlayerInput>();
                var declaration = actorObject.AddComponent<PlayerActorDeclaration>();
                declaration.ConfigureForDiagnostics(
                    "qa.actor.player.valid",
                    "QA PlayerActor Valid",
                    playerInput,
                    "qa.playeractor.valid");

                PlayerActorSet set = PlayerActorValidator.ValidateDeclarations(
                    new[] { declaration },
                    source,
                    "qa.playeractor.valid");

                bool passed = set.Succeeded
                    && set.Count == 1
                    && set.PlayerInputEvidenceCount == 1
                    && !set.SwitchesActionMaps
                    && !set.AppliesInputBehavior
                    && !set.SpawnsActor;

                LogSetStep(logger, "playeractor-with-playerinput-valid", passed, set);
                return passed;
            }
            finally
            {
                UnityEngine.Object.Destroy(actorObject);
            }
        }

        private static bool ValidateMissingPlayerInputBlocking(FrameworkLogger logger, string source)
        {
            PlayerActorSet set = PlayerActorSet.FromDescriptors(
                new[]
                {
                    new PlayerActorDescriptor(
                        ActorId.From("qa.actor.player.missing-playerinput"),
                        false,
                        "QA PlayerActor Missing PlayerInput",
                        "Synthetic",
                        "qa.actor.player.missing-playerinput",
                        source,
                        "qa.playeractor.missing-playerinput")
                },
                source,
                "qa.playeractor.missing-playerinput");

            bool passed = set.Failed
                && set.Count == 1
                && set.PlayerInputEvidenceCount == 0
                && set.BlockingIssueCount == 1
                && ContainsIssue(set, PlayerActorSetIssueKind.MissingRequiredPlayerInputEvidence)
                && !set.SwitchesActionMaps
                && !set.AppliesInputBehavior
                && !set.SpawnsActor;

            LogSetStep(logger, "missing-playerinput-blocking", passed, set);
            return passed;
        }

        private static bool ValidateDuplicatePlayerActorIdBlocking(FrameworkLogger logger, string source)
        {
            PlayerActorSet set = PlayerActorSet.FromDescriptors(
                new[]
                {
                    new PlayerActorDescriptor(
                        ActorId.From("qa.actor.player.duplicate"),
                        true,
                        "QA PlayerActor Duplicate A",
                        "Synthetic",
                        "qa.actor.player.duplicate.a",
                        source,
                        "qa.playeractor.duplicate"),
                    new PlayerActorDescriptor(
                        ActorId.From("qa.actor.player.duplicate"),
                        true,
                        "QA PlayerActor Duplicate B",
                        "Synthetic",
                        "qa.actor.player.duplicate.b",
                        source,
                        "qa.playeractor.duplicate")
                },
                source,
                "qa.playeractor.duplicate");

            bool passed = set.Failed
                && set.Count == 2
                && set.PlayerInputEvidenceCount == 2
                && set.BlockingIssueCount == 1
                && ContainsIssue(set, PlayerActorSetIssueKind.DuplicatePlayerActorId)
                && !set.SwitchesActionMaps
                && !set.AppliesInputBehavior
                && !set.SpawnsActor;

            LogSetStep(logger, "duplicate-playeractor-id-blocking", passed, set);
            return passed;
        }

        private static bool ValidateNoInputBehavior(FrameworkLogger logger, string source)
        {
            PlayerActorSet set = PlayerActorSet.FromDescriptors(
                new[]
                {
                    new PlayerActorDescriptor(
                        ActorId.From("qa.actor.player.no-behavior"),
                        true,
                        "QA PlayerActor No Behavior",
                        "Synthetic",
                        "qa.actor.player.no-behavior",
                        source,
                        "qa.playeractor.no-behavior")
                },
                source,
                "qa.playeractor.no-behavior");

            bool passed = set.Succeeded
                && !set.SwitchesActionMaps
                && !set.AppliesInputBehavior
                && !set.SpawnsActor;

            LogStep(
                logger,
                "no-input-behavior",
                passed,
                LogFields.Of(
                    LogFields.Field("playerActors", set.Count),
                    LogFields.Field("playerInputEvidence", set.PlayerInputEvidenceCount),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("actorSpawning", set.SpawnsActor),
                    LogFields.Field("playerInputActivation", "none"),
                    LogFields.Field("playerInputManagerOwnership", "none"),
                    LogFields.Field("movement", "none")));

            return passed;
        }

        private static bool ContainsIssue(PlayerActorSet set, PlayerActorSetIssueKind kind)
        {
            for (int i = 0; i < set.Issues.Count; i++)
            {
                PlayerActorSetIssue issue = set.Issues[i];
                if (issue.Kind == kind && issue.Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogSetStep(FrameworkLogger logger, string step, bool passed, PlayerActorSet set)
        {
            LogStep(
                logger,
                step,
                passed,
                LogFields.Of(
                    LogFields.Field("playerActors", set.Count),
                    LogFields.Field("issues", set.IssueCount),
                    LogFields.Field("blockingIssues", set.BlockingIssueCount),
                    LogFields.Field("playerInputEvidence", set.PlayerInputEvidenceCount),
                    LogFields.Field("actionMapSwitching", set.SwitchesActionMaps),
                    LogFields.Field("inputBehavior", set.AppliesInputBehavior),
                    LogFields.Field("actorSpawning", set.SpawnsActor),
                    LogFields.Field("diagnostics", set.ToDiagnosticString())));
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            if (passed)
            {
                logger.Info("QA PlayerActor Identity Smoke step completed.", PrependStepFields(step, passed, fields));
                return;
            }

            logger.Warning("QA PlayerActor Identity Smoke step failed.", PrependStepFields(step, passed, fields));
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
