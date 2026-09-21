using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class PlayerCameraCompositionIntegrationTests
    {
        [Test]
        public void EachSlotPublishesOnlyItsOwnCompositionAndRejoinUsesAFreshSubject()
        {
            using var fixture = new Fixture();
            fixture.ActivateCompositions();
            Assert.That(fixture.CompositionP1.Snapshot.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.BlockedMissingSubjectSelection));
            Assert.That(fixture.CompositionP2.IsRequestPublished, Is.False);

            Assert.That(PlayerCameraCompositionIntegrationRuntime.TryCreate(
                fixture.Subjects,
                fixture.Topology,
                out PlayerCameraCompositionIntegrationRuntime runtime,
                out string diagnostic), Is.True, diagnostic);
            fixture.Runtime = runtime;

            Assert.That(fixture.CompositionP1.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.CompositionP2.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.False);
            Assert.That(fixture.CompositionP2.IsRequestPublished, Is.False);

            PlayerPreparedActorOccurrence firstP1 = fixture.CreateOccurrence(
                PlayerSlotId.Player1, 1, out Transform firstP1Transform, out PlayerActorPreparationToken firstP1Token);
            fixture.Actors.Set(PlayerSlotId.Player1, firstP1);

            Assert.That(fixture.Subjects.TryGetCurrentSubjectId(
                PlayerSlotId.Player1, out CameraSubjectId firstP1Subject), Is.True);
            Assert.That(firstP1Subject.Value, Is.EqualTo(
                $"camera.subject.player-actor:{firstP1Token.StableText}"));
            Assert.That(fixture.CompositionP1.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.CompositionP1.CompositionRig.CinemachineCamera.Follow,
                Is.SameAs(firstP1Transform));
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.True);
            Assert.That(fixture.CompositionP2.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.CompositionP2.IsRequestPublished, Is.False);
            Assert.That(fixture.CompositionP2.CompositionRig.CinemachineCamera.Follow, Is.Null);
            int p2Revision = fixture.CompositionP2.SubjectSelectionSource.Revision;

            PlayerPreparedActorOccurrence p2 = fixture.CreateOccurrence(
                PlayerSlotId.Player2, 1, out Transform p2Transform, out PlayerActorPreparationToken p2Token);
            fixture.Actors.Set(PlayerSlotId.Player2, p2);
            Assert.That(fixture.Subjects.TryGetCurrentSubjectId(
                PlayerSlotId.Player2, out CameraSubjectId p2Subject), Is.True);
            Assert.That(p2Subject.Value, Is.EqualTo(
                $"camera.subject.player-actor:{p2Token.StableText}"));
            Assert.That(fixture.CompositionP2.CompositionRig.CinemachineCamera.Follow, Is.SameAs(p2Transform));
            Assert.That(fixture.CompositionP2.IsRequestPublished, Is.True);
            Assert.That(fixture.CompositionP1.CompositionRig.CinemachineCamera.Follow,
                Is.SameAs(firstP1Transform));
            Assert.That(fixture.CompositionP1.SubjectSelectionSource.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(firstP1Subject));
            Assert.That(fixture.CompositionP2.SubjectSelectionSource.CurrentSnapshot.Count, Is.EqualTo(1));
            Assert.That(fixture.CompositionP2.SubjectSelectionSource.CurrentSnapshot.SubjectIds[0],
                Is.Not.EqualTo(firstP1Subject));

            int p2RevisionAfterJoin = fixture.CompositionP2.SubjectSelectionSource.Revision;
            fixture.Actors.Clear(PlayerSlotId.Player1);
            Assert.That(fixture.Subjects.TryGetCurrentSubjectId(PlayerSlotId.Player1, out _), Is.False);
            Assert.That(fixture.CompositionP1.SubjectSelectionSource.CurrentSnapshot.Count, Is.Zero);
            Assert.That(fixture.CompositionP1.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.False);
            Assert.That(fixture.CompositionP1.CompositionRig.CinemachineCamera.Follow, Is.Null);
            Assert.That(fixture.CompositionP2.SubjectSelectionSource.Revision, Is.EqualTo(p2RevisionAfterJoin));
            Assert.That(fixture.CompositionP2.CompositionRig.CinemachineCamera.Follow, Is.SameAs(p2Transform));
            Assert.That(fixture.CompositionP2.IsRequestPublished, Is.True);
            Assert.That(p2RevisionAfterJoin, Is.GreaterThan(p2Revision));

            PlayerPreparedActorOccurrence rejoinedP1 = fixture.CreateOccurrence(
                PlayerSlotId.Player1, 2, out Transform rejoinedTransform, out PlayerActorPreparationToken rejoinedToken);
            fixture.Actors.Set(PlayerSlotId.Player1, rejoinedP1);
            Assert.That(fixture.Subjects.TryGetCurrentSubjectId(
                PlayerSlotId.Player1, out CameraSubjectId rejoinedSubject), Is.True);
            Assert.That(rejoinedSubject, Is.Not.EqualTo(firstP1Subject));
            Assert.That(rejoinedSubject.Value, Is.EqualTo(
                $"camera.subject.player-actor:{rejoinedToken.StableText}"));
            Assert.That(fixture.CompositionP1.SubjectSelectionSource.CurrentSnapshot.Count, Is.EqualTo(1));
            Assert.That(fixture.CompositionP1.SubjectSelectionSource.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(rejoinedSubject));
            Assert.That(fixture.CompositionP1.CompositionRig.CinemachineCamera.Follow,
                Is.SameAs(rejoinedTransform));
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.True);
            Assert.That(fixture.CompositionP2.SubjectSelectionSource.Revision, Is.EqualTo(p2RevisionAfterJoin));
            Assert.That(fixture.CompositionP2.CompositionRig.CinemachineCamera.Follow, Is.SameAs(p2Transform));
        }

        [Test]
        public void DuplicateSlotIsRejected()
        {
            using var fixture = new ValidationFixture();
            PlayerCameraCompositionPolicyAuthoring policy = fixture.Policy(
                fixture.Binding(fixture.Profile("player.1"), fixture.ExplicitComposition("p1")),
                fixture.Binding(fixture.Profile("player.1"), fixture.ExplicitComposition("p1-other")));

            Assert.That(PlayerCameraCompositionPolicyProjection.TryCreate(
                new[] { policy }, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("duplicate"));
            Assert.That(diagnostic, Does.Contain("Player Slot"));
        }

        [Test]
        public void DuplicateCompositionIsRejected()
        {
            using var fixture = new ValidationFixture();
            CameraSharedComposition composition = fixture.ExplicitComposition("shared");
            PlayerCameraCompositionPolicyAuthoring policy = fixture.Policy(
                fixture.Binding(fixture.Profile("player.1"), composition),
                fixture.Binding(fixture.Profile("player.2"), composition));

            Assert.That(PlayerCameraCompositionPolicyProjection.TryCreate(
                new[] { policy }, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("duplicate"));
            Assert.That(diagnostic, Does.Contain("Composition"));
        }

        [Test]
        public void NonExplicitSelectionCompositionIsRejected()
        {
            using var fixture = new ValidationFixture();
            PlayerCameraCompositionPolicyAuthoring policy = fixture.Policy(
                fixture.Binding(
                    fixture.Profile("player.1"),
                    fixture.Composition(
                        "shared",
                        CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects)));

            Assert.That(PlayerCameraCompositionPolicyProjection.TryCreate(
                new[] { policy }, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("ExplicitSelection"));
        }

        [Test]
        public void DisposeClearsSelectionAndUnsubscribes()
        {
            using var fixture = new Fixture();
            fixture.ActivateCompositions();
            Assert.That(PlayerCameraCompositionIntegrationRuntime.TryCreate(
                fixture.Subjects, fixture.Topology,
                out PlayerCameraCompositionIntegrationRuntime runtime, out string diagnostic),
                Is.True, diagnostic);
            fixture.Runtime = runtime;
            Assert.That(SubscriberCount(fixture.Subjects), Is.EqualTo(1));

            fixture.Actors.Set(
                PlayerSlotId.Player1,
                fixture.CreateOccurrence(PlayerSlotId.Player1, 1, out _, out _));
            ICameraCompositionSubjectSelectionSource selection =
                fixture.CompositionP1.SubjectSelectionSource;
            Assert.That(selection.CurrentSnapshot.Count, Is.EqualTo(1));
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.True);

            runtime.Dispose();
            Assert.That(selection.CurrentSnapshot.Count, Is.Zero);
            Assert.That(fixture.CompositionP1.SubjectSelectionSource, Is.Null);
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.False);
            Assert.That(fixture.CompositionP2.SubjectSelectionSource, Is.Null);
            Assert.That(SubscriberCount(fixture.Subjects), Is.Zero);

            fixture.Actors.Set(
                PlayerSlotId.Player1,
                fixture.CreateOccurrence(PlayerSlotId.Player1, 2, out _, out _));
            Assert.That(selection.CurrentSnapshot.Count, Is.Zero);
            Assert.That(fixture.CompositionP1.IsRequestPublished, Is.False);
            Assert.That(fixture.Subjects.TryGetCurrentSubjectId(PlayerSlotId.Player1, out _), Is.True);

            Assert.DoesNotThrow(() => runtime.Dispose());
            Assert.That(SubscriberCount(fixture.Subjects), Is.Zero);
        }

        private static int SubscriberCount(PlayerActorCameraSubjectIntegrationRuntime subjects)
        {
            FieldInfo field = typeof(PlayerActorCameraSubjectIntegrationRuntime).GetField(
                "SubjectChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Delegate handler = field.GetValue(subjects) as Delegate;
            return handler == null ? 0 : handler.GetInvocationList().Length;
        }

        private sealed class FakeActors : IPlayerPreparedActorOccurrenceSource
        {
            private readonly Dictionary<PlayerSlotId, PlayerPreparedActorOccurrence> _current =
                new Dictionary<PlayerSlotId, PlayerPreparedActorOccurrence>();

            public event Action<PlayerSlotId> CurrentActorInvalidated;

            public string SessionContextId => "session-test";

            public bool TryGetCurrentActorOccurrence(
                PlayerSlotId playerSlotId,
                out PlayerPreparedActorOccurrence occurrence) =>
                _current.TryGetValue(playerSlotId, out occurrence);

            public void Set(PlayerSlotId slot, PlayerPreparedActorOccurrence occurrence)
            {
                _current[slot] = occurrence;
                CurrentActorInvalidated?.Invoke(slot);
            }

            public void Clear(PlayerSlotId slot)
            {
                _current.Remove(slot);
                CurrentActorInvalidated?.Invoke(slot);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

            internal Fixture()
            {
                Actors = new FakeActors();
                Availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId("player-composition-availability"));
                Subjects = new PlayerActorCameraSubjectIntegrationRuntime(Actors, Availability);
                OutputDefinition = CreateDefinition<CameraOutputDefinition>();
                DefaultRig = CreateRig(CameraRigPresentationIntent.Fixed);
                GameObject outputRoot = CreateRoot("output");
                UnityEngine.Camera unityCamera = outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = outputRoot.AddComponent<CinemachineBrain>();
                Output = outputRoot.AddComponent<CameraOutputAuthoring>();
                SetField(Output, "outputDefinition", OutputDefinition);
                SetField(Output, "unityCamera", unityCamera);
                SetField(Output, "cinemachineBrain", brain);
                SetField(Output, "defaultCameraRig", DefaultRig);
                SetField(Output, "initializeOnAwake", false);
                Context = new CameraOutputContext(OutputDefinition.OutputId);
                Applicator = new CameraOutputRigApplicator(
                    new CameraOutputBinding(OutputDefinition.OutputId, unityCamera, brain));
                Session = new CameraOutputSession(
                    Context, Applicator, CameraRigReference.FromComposer(DefaultRig));
                Assert.That(Session.Synchronize().Succeeded, Is.True);
                SetField(Output, "_context", Context);
                SetField(Output, "_applicator", Applicator);
                SetField(Output, "_session", Session);
                SetField(Output, "_initializedDefinition", OutputDefinition);

                CompositionP1 = CreateComposition("p1", 10);
                CompositionP2 = CreateComposition("p2", 20);
                var policy = CreateRoot("policy").AddComponent<PlayerCameraCompositionPolicyAuthoring>();
                policy.Configure(new[]
                {
                    Binding(Profile("player.1"), CompositionP1),
                    Binding(Profile("player.2"), CompositionP2)
                });
                Assert.That(PlayerCameraCompositionPolicyProjection.TryCreate(
                    new[] { policy }, out PlayerCameraCompositionTopology topology, out string diagnostic),
                    Is.True, diagnostic);
                Topology = topology;
            }

            internal FakeActors Actors { get; }
            internal CameraSubjectAvailabilityContext Availability { get; }
            internal PlayerActorCameraSubjectIntegrationRuntime Subjects { get; }
            internal CameraOutputDefinition OutputDefinition { get; }
            internal CameraRigComposer DefaultRig { get; }
            internal CameraOutputAuthoring Output { get; }
            internal CameraOutputContext Context { get; }
            internal CameraOutputRigApplicator Applicator { get; }
            internal CameraOutputSession Session { get; }
            internal CameraSharedComposition CompositionP1 { get; }
            internal CameraSharedComposition CompositionP2 { get; }
            internal PlayerCameraCompositionTopology Topology { get; }
            internal PlayerCameraCompositionIntegrationRuntime Runtime { get; set; }

            internal void ActivateCompositions()
            {
                CompositionP1.gameObject.SetActive(true);
                CompositionP2.gameObject.SetActive(true);
            }

            internal PlayerPreparedActorOccurrence CreateOccurrence(
                PlayerSlotId slot,
                int correlation,
                out Transform actorTransform,
                out PlayerActorPreparationToken token)
            {
                GameObject actor = CreateRoot($"actor-{slot.Value.Value}-{correlation}");
                actorTransform = actor.transform;
                var declaration = actor.AddComponent<PlayerActorDeclaration>();
                token = new PlayerActorPreparationToken(
                    Actors.SessionContextId,
                    slot,
                    new ActorProfileId("actor.farmer"),
                    1,
                    new ActorId($"actor.{correlation}.{slot.Value.Value}"),
                    RuntimeContentIdentity.From(
                        RuntimeContentOwner.Session(Actors.SessionContextId, "Session"),
                        $"content-{correlation}-{slot.Value.Value}"),
                    1,
                    correlation);
                return new PlayerPreparedActorOccurrence(token, declaration, actor);
            }

            public void Dispose()
            {
                Runtime?.Dispose();
                Subjects.Dispose();
                for (int index = _created.Count - 1; index >= 0; index--)
                {
                    if (_created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_created[index]);
                    }
                }
            }

            private CameraSharedComposition CreateComposition(string name, int precedence)
            {
                GameObject root = CreateRoot(name);
                root.SetActive(false);
                var composition = root.AddComponent<CameraSharedComposition>();
                SetField(composition, "compositionRig", CreateRig(CameraRigPresentationIntent.ThirdPerson));
                SetField(composition, "requestPrecedence", precedence);
                composition.Configure(
                    OutputDefinition,
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection);
                composition.AttachOutputSession(Output);
                composition.AttachCameraSubjectAvailability(Availability);
                return composition;
            }

            private PlayerSlotProfile Profile(string slotId)
            {
                var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                _created.Add(profile);
                SetField(profile, "playerSlotId", slotId);
                return profile;
            }

            private static PlayerCameraCompositionBindingAuthoring Binding(
                PlayerSlotProfile profile,
                CameraSharedComposition composition)
            {
                var binding = new PlayerCameraCompositionBindingAuthoring();
                binding.Configure(profile, composition);
                return binding;
            }

            private CameraRigComposer CreateRig(CameraRigPresentationIntent intent)
            {
                GameObject root = CreateRoot($"rig-{intent}-{Guid.NewGuid():N}");
                var composer = root.AddComponent<CameraRigComposer>();
                CameraRigBehaviorDefinition behavior = intent == CameraRigPresentationIntent.ThirdPerson
                    ? ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>()
                    : ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>();
                _created.Add(behavior);
                SetField(composer, "behaviorDefinition", behavior);
                GameObject cameraRoot = CreateRoot($"camera-{intent}-{Guid.NewGuid():N}");
                cameraRoot.transform.SetParent(root.transform, false);
                SetField(composer, "cinemachineCamera", cameraRoot.AddComponent<CinemachineCamera>());
                return composer;
            }

            private T CreateDefinition<T>() where T : ScriptableObject
            {
                T definition = ScriptableObject.CreateInstance<T>();
                _created.Add(definition);
                SetField(definition, "stableId", Guid.NewGuid().ToString("N"));
                return definition;
            }

            private GameObject CreateRoot(string name)
            {
                var root = new GameObject(name);
                _created.Add(root);
                return root;
            }
        }

        private sealed class ValidationFixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
            private readonly CameraOutputDefinition _output;

            internal ValidationFixture()
            {
                _output = ScriptableObject.CreateInstance<CameraOutputDefinition>();
                _created.Add(_output);
                SetField(_output, "stableId", Guid.NewGuid().ToString("N"));
            }

            internal PlayerSlotProfile Profile(string slotId)
            {
                var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                _created.Add(profile);
                SetField(profile, "playerSlotId", slotId);
                return profile;
            }

            internal CameraSharedComposition ExplicitComposition(string name) =>
                Composition(name, CameraSharedCompositionSubjectPolicyKind.ExplicitSelection);

            internal CameraSharedComposition Composition(
                string name,
                CameraSharedCompositionSubjectPolicyKind policy)
            {
                GameObject root = new GameObject(name);
                _created.Add(root);
                root.SetActive(false);
                var composition = root.AddComponent<CameraSharedComposition>();
                composition.Configure(_output, policy);
                return composition;
            }

            internal PlayerCameraCompositionBindingAuthoring Binding(
                PlayerSlotProfile profile,
                CameraSharedComposition composition)
            {
                var binding = new PlayerCameraCompositionBindingAuthoring();
                binding.Configure(profile, composition);
                return binding;
            }

            internal PlayerCameraCompositionPolicyAuthoring Policy(
                params PlayerCameraCompositionBindingAuthoring[] bindings)
            {
                var policy = new GameObject("policy").AddComponent<PlayerCameraCompositionPolicyAuthoring>();
                _created.Add(policy.gameObject);
                policy.Configure(bindings);
                return policy;
            }

            public void Dispose()
            {
                for (int index = _created.Count - 1; index >= 0; index--)
                {
                    if (_created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_created[index]);
                    }
                }
            }
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
