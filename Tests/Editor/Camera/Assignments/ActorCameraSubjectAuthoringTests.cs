using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class ActorCameraSubjectAuthoringTests
    {
        [Test]
        public void SceneProvidedAndManagerProvisionedActorsUseSameExplicitObservationContract()
        {
            foreach (string origin in new[] { "SceneProvided", "ManagerProvisioned" })
            {
                var actorRoot = new GameObject(origin + " Actor Root");
                var observationMount = new GameObject(origin + " Observation Mount");
                observationMount.transform.SetParent(actorRoot.transform);
                PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();
                ActorCameraSubjectAuthoring authoring = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
                SetObservation(authoring, observationMount.transform);

                try
                {
                    Assert.That(authoring.TryResolveObservation(actor, out Transform observation, out string issue), Is.True, issue);
                    Assert.That(observation, Is.SameAs(observationMount.transform));
                    Assert.That(observation, Is.Not.SameAs(actor.transform));
                }
                finally
                {
                    Object.DestroyImmediate(actorRoot);
                }
            }
        }

        [Test]
        public void MissingExplicitObservationTransformFailsWithoutRootFallback()
        {
            var actorRoot = new GameObject("Actor Root");
            ActorDeclaration actor = actorRoot.AddComponent<ActorDeclaration>();
            ActorCameraSubjectAuthoring authoring = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();

            try
            {
                Assert.That(authoring.TryResolveObservation(actor, out Transform observation, out string issue), Is.False);
                Assert.That(observation, Is.Null);
                Assert.That(issue, Does.Contain("explicit Observation Transform"));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        [Test]
        public void RejectsObservationTransformOutsideActorOccurrence()
        {
            var actorRoot = new GameObject("Actor Root");
            var foreignRoot = new GameObject("Foreign Root");
            ActorDeclaration actor = actorRoot.AddComponent<ActorDeclaration>();
            ActorCameraSubjectAuthoring authoring = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
            SetObservation(authoring, foreignRoot.transform);

            try
            {
                Assert.That(authoring.TryResolveObservation(actor, out Transform observation, out string issue), Is.False);
                Assert.That(observation, Is.Null);
                Assert.That(issue, Does.Contain("belong to the exact Actor occurrence"));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(foreignRoot);
            }
        }

        [Test]
        public void RejectsObservationTransformInsidePublishedReplaceableContentMount()
        {
            var actorRoot = new GameObject("NonPlayer Actor Root");
            ActorDeclaration actor = actorRoot.AddComponent<ActorDeclaration>();
            ActorOccurrencePublication publication = actorRoot.AddComponent<ActorOccurrencePublication>();
            var contentMount = new GameObject("Replaceable Content Mount");
            contentMount.transform.SetParent(actorRoot.transform, false);
            var replaceableTarget = new GameObject("Replaceable Camera Target");
            replaceableTarget.transform.SetParent(contentMount.transform, false);
            ActorCameraSubjectAuthoring authoring = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
            typeof(ActorOccurrencePublication)
                .GetField("compositionMount", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(publication, contentMount.transform);
            SetObservation(authoring, replaceableTarget.transform);

            try
            {
                Assert.That(authoring.TryResolveObservation(
                    actor,
                    out Transform observation,
                    out string issue), Is.False);
                Assert.That(observation, Is.Null);
                Assert.That(issue, Does.Contain("replaceable composition mount"));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }

            var playerRoot = new GameObject("Player Actor Root");
            PlayerActorDeclaration playerDeclaration = playerRoot.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost playerHost = playerRoot.AddComponent<PlayerActorRuntimeHost>();
            var playerMount = new GameObject("Player Replaceable Content Mount");
            playerMount.transform.SetParent(playerRoot.transform, false);
            var playerTarget = new GameObject("Replaceable Player Camera Target");
            playerTarget.transform.SetParent(playerMount.transform, false);
            ActorCameraSubjectAuthoring playerAuthoring = playerRoot.AddComponent<ActorCameraSubjectAuthoring>();
            SetObservation(playerAuthoring, playerTarget.transform);
            SetPrivateField(playerHost, "playerActorDeclaration", playerDeclaration);
            SetPrivateField(playerHost, "presentationMount", playerMount.transform);
            try
            {
                Assert.That(playerHost.TryValidateConfiguration(out string playerIssue), Is.False);
                Assert.That(playerIssue, Does.Contain("replaceable composition mount"));
            }
            finally
            {
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void MemberActorSubjectResolutionConsumesExplicitObservationAndOccurrenceIdentity()
        {
            var actorRoot = new GameObject("Actor Root");
            var observationMount = new GameObject("Observation Mount");
            observationMount.transform.SetParent(actorRoot.transform);
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();
            typeof(ActorDeclaration)
                .GetField("actorId", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(actor, "actor.camera-subject-test");
            ActorCameraSubjectAuthoring authoring = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
            SetObservation(authoring, observationMount.transform);
            var token = new PlayerActorPreparationToken(
                "session.camera-subject-test",
                PlayerSlotId.Player1,
                new ActorProfileId("profile.camera-subject-test"),
                1,
                actor.ActorId,
                RuntimeContentIdentity.From(
                    RuntimeContentOwner.Session("session.camera-subject-test", "test"),
                    "actor-content"),
                1,
                1);
            var occurrence = new PlayerPreparedActorOccurrence(token, actor, actorRoot);

            try
            {
                Assert.That(SessionCameraMembershipRuntime.TryResolveSubject(
                    occurrence,
                    out CameraSubject subject,
                    out string issue), Is.True, issue);
                Assert.That(subject.Observation, Is.SameAs(observationMount.transform));
                Assert.That(subject.Observation, Is.Not.SameAs(actor.transform));
                Assert.That(subject.SubjectId.Value, Does.Contain(token.StableText));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        [Test]
        public void MemberActorSubjectResolutionDiagnosesMissingExplicitAuthoring()
        {
            var actorRoot = new GameObject("Actor Root");
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();
            typeof(ActorDeclaration)
                .GetField("actorId", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(actor, "actor.camera-subject-missing-test");
            var token = new PlayerActorPreparationToken(
                "session.camera-subject-test",
                PlayerSlotId.Player1,
                new ActorProfileId("profile.camera-subject-test"),
                1,
                actor.ActorId,
                RuntimeContentIdentity.From(
                    RuntimeContentOwner.Session("session.camera-subject-test", "test"),
                    "actor-content"),
                1,
                1);
            var occurrence = new PlayerPreparedActorOccurrence(token, actor, actorRoot);

            try
            {
                Assert.That(SessionCameraMembershipRuntime.TryResolveSubject(
                    occurrence,
                    out CameraSubject subject,
                    out string issue), Is.False);
                Assert.That(subject.IsValid, Is.False);
                Assert.That(issue, Does.Contain("explicit Observation Transform"));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        private static void SetObservation(
            ActorCameraSubjectAuthoring authoring,
            Transform observation)
        {
            typeof(ActorCameraSubjectAuthoring)
                .GetField("observationTransform", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(authoring, observation);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}
