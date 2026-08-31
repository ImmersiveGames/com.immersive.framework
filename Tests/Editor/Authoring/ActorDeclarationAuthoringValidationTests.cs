using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ActorDeclarationAuthoringValidationTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _createdObjects.Count; index++)
            {
                Object.DestroyImmediate(_createdObjects[index]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ActorDeclaration_EmptyPersistentId_RemainsInvalid()
        {
            ActorDeclaration declaration = Create<ActorDeclaration>();

            FrameworkAuthoringValidationReport report =
                ActorDeclarationAuthoringValidator.Validate(declaration);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(
                ContainsMessage(
                    report,
                    "Actor Declaration requires an explicit Actor ID."),
                Is.True);
        }

        [Test]
        public void PlayerActorDeclaration_EmptyRuntimeId_IsValidWithRuntimeOwnershipWarning()
        {
            PlayerActorDeclaration declaration =
                Create<PlayerActorDeclaration>();

            FrameworkAuthoringValidationReport report =
                ActorDeclarationAuthoringValidator.Validate(declaration);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.ErrorCount, Is.Zero);
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(
                ContainsMessage(
                    report,
                    ActorDeclarationAuthoringValidator
                        .RuntimeActorIdentityWarning),
                Is.True);
            Assert.That(
                ContainsMessage(
                    report,
                    "Actor Declaration requires an explicit Actor ID."),
                Is.False);
        }

        [Test]
        public void PlayerActorDeclaration_AuthoredRuntimeId_IsInvalidWithoutMutation()
        {
            PlayerActorDeclaration declaration =
                Create<PlayerActorDeclaration>();
            SetActorId(declaration, "actor.authored.player");

            FrameworkAuthoringValidationReport report =
                ActorDeclarationAuthoringValidator.Validate(declaration);

            Assert.That(report.IsValid, Is.False);
            Assert.That(
                ContainsMessage(
                    report,
                    ActorDeclarationAuthoringValidator
                        .AuthoredPlayerActorIdentityError),
                Is.True);
            Assert.That(
                GetActorId(declaration),
                Is.EqualTo("actor.authored.player"));
        }

        [Test]
        public void GenerateActorId_StaysAvailableOnlyForPersistentActorDeclarations()
        {
            ActorDeclaration ordinaryDeclaration = Create<ActorDeclaration>();
            PlayerActorDeclaration playerDeclaration =
                Create<PlayerActorDeclaration>();

            Assert.That(
                ActorDeclarationAuthoringValidator
                    .CanGenerateAuthoredActorId(ordinaryDeclaration, string.Empty),
                Is.True);
            Assert.That(
                ActorDeclarationAuthoringValidator
                    .CanGenerateAuthoredActorId(
                        ordinaryDeclaration,
                        "qa.actor.generic"),
                Is.True);
            Assert.That(
                ActorDeclarationAuthoringValidator
                    .CanGenerateAuthoredActorId(playerDeclaration, string.Empty),
                Is.False);
            Assert.That(
                ActorDeclarationAuthoringValidator
                    .CanGenerateAuthoredActorId(
                        playerDeclaration,
                        "player-actor:runtime-evidence"),
                Is.False);
        }

        private T Create<T>() where T : Component
        {
            var gameObject = new GameObject(typeof(T).Name);
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private static void SetActorId(
            ActorDeclaration declaration,
            string actorId)
        {
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("actorId").stringValue = actorId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetActorId(ActorDeclaration declaration)
        {
            var serialized = new SerializedObject(declaration);
            return serialized.FindProperty("actorId").stringValue;
        }

        private static bool ContainsMessage(
            FrameworkAuthoringValidationReport report,
            string expectedMessage)
        {
            for (int index = 0; index < report.Issues.Count; index++)
            {
                if (report.Issues[index].Message.Contains(expectedMessage))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
