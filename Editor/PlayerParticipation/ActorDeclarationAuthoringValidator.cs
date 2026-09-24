using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Explicit, non-mutating authoring validation for Actor declarations.
    /// </summary>
    internal static class ActorDeclarationAuthoringValidator
    {
        private const string LegacyQaActorId = "qa.actor.generic";
        internal const string RuntimeActorIdentityWarning =
            "Actor ID is assigned at runtime when this Player Actor occurrence is prepared.";
        internal const string AuthoredPlayerActorIdentityError =
            "Player Actor declarations must not author an Actor ID. " +
            "Actor identity is assigned per runtime occurrence.";

        internal static bool CanGenerateAuthoredActorId(
            ActorDeclaration declaration,
            string currentActorId)
        {
            return declaration is not PlayerActorDeclaration &&
                (string.IsNullOrWhiteSpace(currentActorId) ||
                 string.Equals(
                     currentActorId.Trim(),
                     LegacyQaActorId,
                     StringComparison.Ordinal));
        }

        internal static FrameworkAuthoringValidationReport Validate(
            ActorDeclaration declaration)
        {
            var report = new FrameworkAuthoringValidationReport(
                FrameworkValidationMode.Standard);

            if (declaration == null)
            {
                report.AddError(
                    "Actor Declaration validation requires a target component.",
                    null);
                return report;
            }

            var serialized = new SerializedObject(declaration);
            SerializedProperty actorIdProperty =
                serialized.FindProperty("actorId");
            string rawActorId =
                actorIdProperty != null
                    ? actorIdProperty.stringValue ?? string.Empty
                    : string.Empty;

            if (declaration is PlayerActorDeclaration)
            {
                ValidatePlayerActorIdentity(
                    declaration,
                    rawActorId,
                    report);
            }
            else
            {
                ValidateAuthoredActorIdentity(
                    declaration,
                    rawActorId,
                    report);
            }

            if (!Enum.IsDefined(typeof(ActorKind), declaration.ActorKind))
            {
                report.AddError(
                    $"Actor Declaration has invalid Actor Kind '{declaration.ActorKind}'.",
                    declaration);
            }

            if (!Enum.IsDefined(typeof(ActorRole), declaration.ActorRole))
            {
                report.AddError(
                    $"Actor Declaration has invalid Actor Role '{declaration.ActorRole}'.",
                    declaration);
            }

            if (declaration is PlayerActorDeclaration player)
            {
                PlayerInput[] authoredPlayerInputs =
                    player.GetComponentsInChildren<PlayerInput>(true);
                if (authoredPlayerInputs.Length > 0)
                {
                    report.AddError(
                        $"Player Actor Runtime must not contain PlayerInput. PlayerInput belongs to the Local Player Host. Found '{authoredPlayerInputs.Length}' PlayerInput component(s) in this Actor hierarchy.",
                        player);
                }
            }

            if (report.IsValid)
            {
                report.AddInfo(
                    $"Actor Declaration authoring is valid. actorId='{rawActorId.Trim()}' kind='{declaration.ActorKind}' role='{declaration.ActorRole}'.",
                    declaration);
            }

            return report;
        }

        private static void ValidatePlayerActorIdentity(
            ActorDeclaration declaration,
            string rawActorId,
            FrameworkAuthoringValidationReport report)
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(rawActorId))
            {
                report.AddWarning(
                    RuntimeActorIdentityWarning,
                    declaration);
                return;
            }

            report.AddError(
                AuthoredPlayerActorIdentityError,
                declaration);
        }

        private static void ValidateAuthoredActorIdentity(
            ActorDeclaration declaration,
            string rawActorId,
            FrameworkAuthoringValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(rawActorId))
            {
                report.AddError(
                    "Actor Declaration requires an explicit Actor ID. Open Advanced / Debug and generate one.",
                    declaration);
                return;
            }

            if (string.Equals(
                    rawActorId.Trim(),
                    LegacyQaActorId,
                    StringComparison.Ordinal))
            {
                report.AddError(
                    "Actor Declaration still uses the legacy QA placeholder 'qa.actor.generic'. Open Advanced / Debug and replace it with a generated project identity.",
                    declaration);
                return;
            }

            try
            {
                _ = new ActorId(rawActorId);
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Actor Declaration has an invalid Actor ID. {exception.Message}",
                    declaration);
            }
        }
    }
}
