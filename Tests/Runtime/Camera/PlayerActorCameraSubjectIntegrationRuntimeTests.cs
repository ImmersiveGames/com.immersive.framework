using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class PlayerActorCameraSubjectIntegrationRuntimeTests
    {
        private readonly List<UnityEngine.Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_created[index]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void SlotInvalidation_RequeriesCanonicalEvidenceAndPublishesAuthoredObservation()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            Transform observation;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "actor-a",
                1,
                true,
                out observation);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);

            source.SetCurrent(occurrence);
            source.Invalidate(slot);

            Assert.That(source.QueryCount, Is.EqualTo(1));
            CameraSubjectAvailabilitySnapshot snapshot = availability.CreateSnapshot();
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(
                snapshot.TryGet(SubjectId(occurrence.PreparationToken), out CameraSubjectAvailabilityEntry entry),
                Is.True);
            Assert.That(entry.Subject.Observation, Is.SameAs(observation));
        }

        [Test]
        public void ActorAbsent_RemovesPreviousPublication()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "actor-a",
                1,
                false,
                out _);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);
            source.SetCurrent(occurrence);
            source.Invalidate(slot);

            source.RemoveCurrent(slot);
            source.Invalidate(slot);

            Assert.That(availability.AvailableCount, Is.Zero);
            Assert.That(source.QueryCount, Is.EqualTo(2));
        }

        [Test]
        public void ActorReplacement_RemovesStaleSubjectAndPublishesExactNewOccurrence()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerPreparedActorOccurrence actorA = CreateOccurrence(
                slot,
                "actor-a",
                1,
                false,
                out _);
            PlayerPreparedActorOccurrence actorB = CreateOccurrence(
                slot,
                "actor-b",
                2,
                false,
                out Transform actorBObservation);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);
            source.SetCurrent(actorA);
            source.Invalidate(slot);

            source.SetCurrent(actorB);
            source.Invalidate(slot);

            CameraSubjectAvailabilitySnapshot snapshot = availability.CreateSnapshot();
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot.TryGet(SubjectId(actorA.PreparationToken), out _), Is.False);
            Assert.That(
                snapshot.TryGet(SubjectId(actorB.PreparationToken), out CameraSubjectAvailabilityEntry current),
                Is.True);
            Assert.That(current.Subject.Observation, Is.SameAs(actorBObservation));
        }

        [Test]
        public void RepeatedInvalidation_IsIdempotent()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "actor-a",
                1,
                false,
                out _);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);
            source.SetCurrent(occurrence);

            source.Invalidate(slot);
            int publishedRevision = availability.Revision;
            source.Invalidate(slot);

            Assert.That(availability.AvailableCount, Is.EqualTo(1));
            Assert.That(availability.Revision, Is.EqualTo(publishedRevision));
            Assert.That(source.QueryCount, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_ReleasesOnlyIntegrationOwnedPublications()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "actor-a",
                1,
                false,
                out _);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            CameraSubject foreignSubject = CreateForeignSubject("foreign-subject");
            CameraSubjectAvailabilityResult foreignPublication = availability.TryMakeAvailable(
                foreignSubject,
                new CameraSubjectAvailabilityOwnerId("foreign-owner"));
            Assert.That(foreignPublication.Succeeded, Is.True, foreignPublication.Message);
            var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);
            source.SetCurrent(occurrence);
            source.Invalidate(slot);

            integration.Dispose();

            CameraSubjectAvailabilitySnapshot snapshot = availability.CreateSnapshot();
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot.TryGet(foreignSubject.SubjectId, out _), Is.True);
            Assert.That(snapshot.TryGet(SubjectId(occurrence.PreparationToken), out _), Is.False);
        }

        [Test]
        public void CameraPublicationFailure_RemainsObservableAndDoesNotMutatePlayerEvidence()
        {
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "actor-a",
                1,
                false,
                out _);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            CameraSubject conflict = CreateForeignSubject(
                SubjectId(occurrence.PreparationToken).Value);
            CameraSubjectAvailabilityResult foreignPublication = availability.TryMakeAvailable(
                conflict,
                new CameraSubjectAvailabilityOwnerId("foreign-owner"));
            Assert.That(foreignPublication.Succeeded, Is.True, foreignPublication.Message);
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);
            source.SetCurrent(occurrence);

            source.Invalidate(slot);

            Assert.That(integration.LastReconciliationSucceeded, Is.False);
            Assert.That(integration.Diagnostic, Does.Contain("conflict"));
            Assert.That(source.TryGetCurrentActorOccurrence(slot, out PlayerPreparedActorOccurrence retained), Is.True);
            Assert.That(retained.PreparationToken, Is.EqualTo(occurrence.PreparationToken));
        }

        [Test]
        public void SceneProvidedRetainedOccurrence_UsesTheSameCanonicalConsumerPath()
        {
            PlayerSlotId slot = PlayerSlotId.Player2;
            PlayerPreparedActorOccurrence occurrence = CreateOccurrence(
                slot,
                "scene-provided-actor",
                1,
                true,
                out Transform observation);
            var source = new FakePlayerPreparedActorOccurrenceSource("session-a");
            var availability = Availability();
            using var integration = new PlayerActorCameraSubjectIntegrationRuntime(
                source,
                availability);

            source.SetCurrent(occurrence);
            source.Invalidate(slot);

            Assert.That(
                availability.CreateSnapshot().TryGet(
                    SubjectId(occurrence.PreparationToken),
                    out CameraSubjectAvailabilityEntry entry),
                Is.True);
            Assert.That(entry.Subject.Observation, Is.SameAs(observation));
        }

        [Test]
        public void PlayerPreparationHost_HasNoCameraPublicationOwnershipSurface()
        {
            Type playerHost = typeof(PlayerActorPreparationRuntimeHostModule);
            MemberInfo[] cameraMembers = playerHost.GetMembers(
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(
                Array.Exists(cameraMembers, member => member.Name.Contains("Camera")),
                Is.False);
        }

        [Test]
        public void PlayerInvalidationSubscriberFailure_DoesNotEscapeAndRemainsObservable()
        {
            var hostObject = new GameObject("player-actor-preparation-host");
            _created.Add(hostObject);
            PlayerActorPreparationRuntimeHostModule host =
                hostObject.AddComponent<PlayerActorPreparationRuntimeHostModule>();
            IPlayerPreparedActorOccurrenceSource source = host;
            bool laterSubscriberWasNotified = false;
            source.CurrentActorInvalidated += _ =>
                throw new InvalidOperationException("subscriber-failure-marker");
            source.CurrentActorInvalidated += _ => laterSubscriberWasNotified = true;

            Assert.DoesNotThrow(() =>
                host.InvalidateCurrentActorOccurrence(PlayerSlotId.Player1));

            Assert.That(laterSubscriberWasNotified, Is.True);
            Assert.That(
                host.CurrentActorInvalidationDiagnostic,
                Does.Contain(nameof(InvalidOperationException)));
        }

        private CameraSubjectAvailabilityContext Availability()
        {
            return new CameraSubjectAvailabilityContext(
                new SubjectAvailabilityContextId("subjects-session-a"));
        }

        private PlayerPreparedActorOccurrence CreateOccurrence(
            PlayerSlotId slot,
            string actorIdText,
            int occurrenceRevision,
            bool authoredObservation,
            out Transform observation)
        {
            var actorObject = new GameObject(actorIdText);
            _created.Add(actorObject);
            PlayerActorDeclaration declaration =
                actorObject.AddComponent<PlayerActorDeclaration>();
            var actorId = new ActorId(actorIdText);
            declaration.EstablishRuntimeOccurrenceIdentity(
                actorId,
                actorIdText,
                null,
                "camera-integration-test");

            observation = declaration.transform;
            if (authoredObservation)
            {
                var observationObject = new GameObject(actorIdText + "-observation");
                observationObject.transform.SetParent(actorObject.transform, false);
                observation = observationObject.transform;
                ActorCameraSubjectAuthoring authoring =
                    actorObject.AddComponent<ActorCameraSubjectAuthoring>();
                SetField(authoring, "observationTransform", observation);
            }

            var token = new PlayerActorPreparationToken(
                "session-a",
                slot,
                new ActorProfileId("profile-" + actorIdText),
                occurrenceRevision,
                actorId,
                RuntimeContentIdentity.From(
                    RuntimeContentOwner.Session("session-a", "Session A"),
                    "content-" + actorIdText),
                occurrenceRevision,
                occurrenceRevision);
            return new PlayerPreparedActorOccurrence(
                token,
                declaration,
                actorObject);
        }

        private CameraSubject CreateForeignSubject(string id)
        {
            var root = new GameObject(id);
            _created.Add(root);
            return new CameraSubject(new CameraSubjectId(id), root.transform, id);
        }

        private static CameraSubjectId SubjectId(PlayerActorPreparationToken token)
        {
            return new CameraSubjectId(
                $"camera.subject.player-actor:{token.StableText}");
        }

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FakePlayerPreparedActorOccurrenceSource :
            IPlayerPreparedActorOccurrenceSource
        {
            private readonly Dictionary<PlayerSlotId, PlayerPreparedActorOccurrence>
                _current = new();

            internal FakePlayerPreparedActorOccurrenceSource(string sessionContextId)
            {
                SessionContextId = sessionContextId;
            }

            public event Action<PlayerSlotId> CurrentActorInvalidated;

            public string SessionContextId { get; }
            internal int QueryCount { get; private set; }

            public bool TryGetCurrentActorOccurrence(
                PlayerSlotId playerSlotId,
                out PlayerPreparedActorOccurrence occurrence)
            {
                QueryCount++;
                return _current.TryGetValue(playerSlotId, out occurrence);
            }

            internal void SetCurrent(PlayerPreparedActorOccurrence occurrence)
            {
                _current[occurrence.PlayerSlotId] = occurrence;
            }

            internal void RemoveCurrent(PlayerSlotId playerSlotId)
            {
                _current.Remove(playerSlotId);
            }

            internal void Invalidate(PlayerSlotId playerSlotId)
            {
                CurrentActorInvalidated?.Invoke(playerSlotId);
            }
        }
    }
}
