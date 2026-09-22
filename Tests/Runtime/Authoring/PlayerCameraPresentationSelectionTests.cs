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
    public sealed class PlayerCameraPresentationSelectionTests
    {
        [Test]
        public void TwoLivePresentationsConsumeOnlyTheirBoundPlayerAndRejoinIsFresh()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Selector.TryAttach(
                    fixture.HandleP1,
                    out bool p1Attached,
                    out string p1Issue),
                Is.True,
                p1Issue);
            Assert.That(p1Attached, Is.True);

            Assert.That(
                fixture.Selector.TryAttach(
                    fixture.HandleP2,
                    out bool p2Attached,
                    out string p2Issue),
                Is.True,
                p2Issue);
            Assert.That(p2Attached, Is.True);

            ICameraCompositionSubjectSelectionSource p1Selection =
                fixture.HandleP1.PresentationRuntime.SubjectSelectionSource;
            ICameraCompositionSubjectSelectionSource p2Selection =
                fixture.HandleP2.PresentationRuntime.SubjectSelectionSource;

            Assert.That(p1Selection, Is.Not.Null);
            Assert.That(p2Selection, Is.Not.Null);
            Assert.That(p1Selection.CurrentSnapshot.Count, Is.Zero);
            Assert.That(p2Selection.CurrentSnapshot.Count, Is.Zero);

            PlayerPreparedActorOccurrence firstP1 =
                fixture.CreateOccurrence(
                    PlayerSlotId.Player1,
                    1,
                    out _,
                    out PlayerActorPreparationToken firstP1Token);
            fixture.Actors.Set(
                PlayerSlotId.Player1,
                firstP1);

            Assert.That(
                fixture.Subjects.TryGetCurrentSubjectId(
                    PlayerSlotId.Player1,
                    out CameraSubjectId firstP1Subject),
                Is.True);
            Assert.That(
                firstP1Subject.Value,
                Is.EqualTo(
                    $"camera.subject.player-actor:{firstP1Token.StableText}"));
            Assert.That(
                p1Selection.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(firstP1Subject));
            Assert.That(
                p2Selection.CurrentSnapshot.Count,
                Is.Zero);

            fixture.Actors.Set(
                PlayerSlotId.Player2,
                fixture.CreateOccurrence(
                    PlayerSlotId.Player2,
                    1,
                    out _,
                    out PlayerActorPreparationToken p2Token));

            Assert.That(
                fixture.Subjects.TryGetCurrentSubjectId(
                    PlayerSlotId.Player2,
                    out CameraSubjectId p2Subject),
                Is.True);
            Assert.That(
                p2Subject.Value,
                Is.EqualTo(
                    $"camera.subject.player-actor:{p2Token.StableText}"));
            Assert.That(
                p2Selection.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(p2Subject));
            Assert.That(
                p2Subject,
                Is.Not.EqualTo(firstP1Subject));

            int p2Revision =
                p2Selection.CurrentSnapshot.Revision;
            fixture.Actors.Clear(PlayerSlotId.Player1);

            Assert.That(
                p1Selection.CurrentSnapshot.Count,
                Is.Zero);
            Assert.That(
                p2Selection.CurrentSnapshot.Revision,
                Is.EqualTo(p2Revision));
            Assert.That(
                p2Selection.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(p2Subject));

            fixture.Actors.Set(
                PlayerSlotId.Player1,
                fixture.CreateOccurrence(
                    PlayerSlotId.Player1,
                    2,
                    out _,
                    out PlayerActorPreparationToken rejoinToken));

            Assert.That(
                fixture.Subjects.TryGetCurrentSubjectId(
                    PlayerSlotId.Player1,
                    out CameraSubjectId rejoinedSubject),
                Is.True);
            Assert.That(
                rejoinedSubject.Value,
                Is.EqualTo(
                    $"camera.subject.player-actor:{rejoinToken.StableText}"));
            Assert.That(
                rejoinedSubject,
                Is.Not.EqualTo(firstP1Subject));
            Assert.That(
                p1Selection.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(rejoinedSubject));
            Assert.That(
                p2Selection.CurrentSnapshot.SubjectIds[0],
                Is.EqualTo(p2Subject));
        }

        [Test]
        public void OnePresentationCannotBeBoundToTwoPlayerSlots()
        {
            using var fixture = new ValidationFixture();
            CameraPresentationDefinition presentation =
                fixture.CreatePresentation(
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection);

            Assert.That(
                PlayerCameraPresentationTopology.TryCreate(
                    new[]
                    {
                        fixture.Binding("player.1", presentation),
                        fixture.Binding("player.2", presentation)
                    },
                    out _,
                    out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("only one Player Slot"));
        }

        [Test]
        public void DuplicatePresentationIdentityAcrossDifferentAssetsIsRejected()
        {
            using var fixture = new ValidationFixture();
            CameraPresentationDefinition first =
                fixture.CreatePresentation(
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection);
            CameraPresentationDefinition second =
                fixture.CreatePresentation(
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection);
            fixture.CopyPresentationIdentity(
                first,
                second);

            Assert.That(
                PlayerCameraPresentationTopology.TryCreate(
                    new[]
                    {
                        fixture.Binding("player.1", first),
                        fixture.Binding("player.2", second)
                    },
                    out _,
                    out string issue),
                Is.False);
            Assert.That(
                issue,
                Does.Contain("duplicate CameraPresentationId"));
        }

        [Test]
        public void PlayerBindingRequiresExplicitSelectionPresentation()
        {
            using var fixture = new ValidationFixture();

            Assert.That(
                PlayerCameraPresentationTopology.TryCreate(
                    new[]
                    {
                        fixture.Binding(
                            "player.1",
                            fixture.CreatePresentation(
                                CameraSharedCompositionSubjectPolicyKind
                                    .AllAvailableSubjects))
                    },
                    out _,
                    out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("ExplicitSelection"));
        }

        private sealed class FakeActors :
            IPlayerPreparedActorOccurrenceSource
        {
            private readonly Dictionary<
                PlayerSlotId,
                PlayerPreparedActorOccurrence> _current =
                    new Dictionary<
                        PlayerSlotId,
                        PlayerPreparedActorOccurrence>();

            public event Action<PlayerSlotId>
                CurrentActorInvalidated;

            public string SessionContextId =>
                "camera-032-e-test-session";

            public bool TryGetCurrentActorOccurrence(
                PlayerSlotId playerSlotId,
                out PlayerPreparedActorOccurrence occurrence) =>
                _current.TryGetValue(
                    playerSlotId,
                    out occurrence);

            internal void Set(
                PlayerSlotId slot,
                PlayerPreparedActorOccurrence occurrence)
            {
                _current[slot] = occurrence;
                CurrentActorInvalidated?.Invoke(slot);
            }

            internal void Clear(PlayerSlotId slot)
            {
                _current.Remove(slot);
                CurrentActorInvalidated?.Invoke(slot);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created =
                new List<UnityEngine.Object>();
            private readonly CameraPresentationMaterializationRuntime
                _materializer;

            internal Fixture()
            {
                Actors = new FakeActors();
                Availability =
                    new CameraSubjectAvailabilityContext(
                        new SubjectAvailabilityContextId(
                            "camera-032-e-availability"));
                Subjects =
                    new PlayerActorCameraSubjectIntegrationRuntime(
                        Actors,
                        Availability);

                RuntimeContent =
                    new RuntimeContentRuntime();
                RuntimeContentOwner owner =
                    RuntimeContentOwner.Session(
                        "camera-032-e-test",
                        "Camera 032 E Test");
                RuntimeContent.CreateScopeRoot(
                    owner,
                    "test",
                    "setup");
                Assert.That(
                    RuntimeContent.TryCreateScopeContext(
                        owner,
                        "test",
                        "setup",
                        out RuntimeScopeContext context),
                    Is.True);
                ScopeContext = context;

                OutputDefinition =
                    CreateOutputDefinition();
                PresentationP1 =
                    CreatePresentation("p1");
                PresentationP2 =
                    CreatePresentation("p2");

                PlayerCameraPresentationBindingAuthoring p1 =
                    Binding(
                        CreateProfile("player.1"),
                        PresentationP1);
                PlayerCameraPresentationBindingAuthoring p2 =
                    Binding(
                        CreateProfile("player.2"),
                        PresentationP2);

                Assert.That(
                    PlayerCameraPresentationTopology.TryCreate(
                        new[] { p1, p2 },
                        out PlayerCameraPresentationTopology topology,
                        out string topologyIssue),
                    Is.True,
                    topologyIssue);

                Assert.That(
                    PlayerCameraPresentationSelectionRuntime.TryCreate(
                        Subjects,
                        topology,
                        out PlayerCameraPresentationSelectionRuntime
                            selector,
                        out string selectorIssue),
                    Is.True,
                    selectorIssue);
                Selector = selector;

                _materializer =
                    new CameraPresentationMaterializationRuntime(
                        RuntimeContent);

                GameObject parent =
                    CreateRoot("Presentation Parent");
                HandleP1 =
                    Materialize(
                        PresentationP1,
                        parent.transform);
                HandleP2 =
                    Materialize(
                        PresentationP2,
                        parent.transform);
            }

            internal FakeActors Actors { get; }

            internal CameraSubjectAvailabilityContext Availability { get; }

            internal PlayerActorCameraSubjectIntegrationRuntime Subjects { get; }

            internal RuntimeContentRuntime RuntimeContent { get; }

            internal RuntimeScopeContext ScopeContext { get; }

            internal CameraOutputDefinition OutputDefinition { get; }

            internal CameraPresentationDefinition PresentationP1 { get; }

            internal CameraPresentationDefinition PresentationP2 { get; }

            internal PlayerCameraPresentationSelectionRuntime Selector { get; }

            internal CameraPresentationMaterializationHandle HandleP1 { get; }

            internal CameraPresentationMaterializationHandle HandleP2 { get; }

            internal PlayerPreparedActorOccurrence CreateOccurrence(
                PlayerSlotId slot,
                int correlation,
                out Transform actorTransform,
                out PlayerActorPreparationToken token)
            {
                GameObject actor =
                    CreateRoot(
                        $"actor-{slot.Value.Value}-{correlation}");
                actorTransform = actor.transform;
                PlayerActorDeclaration declaration =
                    actor.AddComponent<PlayerActorDeclaration>();
                token =
                    new PlayerActorPreparationToken(
                        Actors.SessionContextId,
                        slot,
                        new ActorProfileId("actor.farmer"),
                        1,
                        new ActorId(
                            $"actor.{correlation}.{slot.Value.Value}"),
                        RuntimeContentIdentity.From(
                            RuntimeContentOwner.Session(
                                Actors.SessionContextId,
                                "Session"),
                            $"content-{correlation}-{slot.Value.Value}"),
                        1,
                        correlation);

                return new PlayerPreparedActorOccurrence(
                    token,
                    declaration,
                    actor);
            }

            public void Dispose()
            {
                Selector.Dispose();

                CameraPresentationMaterializationResult p2Release =
                    _materializer.Release(
                        HandleP2,
                        "test",
                        "dispose");
                if (p2Release.Succeeded)
                {
                    Selector.ForgetReleased(HandleP2);
                }

                CameraPresentationMaterializationResult p1Release =
                    _materializer.Release(
                        HandleP1,
                        "test",
                        "dispose");
                if (p1Release.Succeeded)
                {
                    Selector.ForgetReleased(HandleP1);
                }

                Subjects.Dispose();

                for (int index = _created.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (_created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            _created[index]);
                    }
                }
            }

            private CameraPresentationMaterializationHandle Materialize(
                CameraPresentationDefinition definition,
                Transform parent)
            {
                CameraPresentationMaterializationResult result =
                    _materializer.Materialize(
                        ScopeContext,
                        definition,
                        parent,
                        "test",
                        "materialize");
                Assert.That(result.Succeeded, Is.True, result.Issue);
                Assert.That(result.Handle, Is.Not.Null);
                return result.Handle;
            }

            private CameraOutputDefinition CreateOutputDefinition()
            {
                CameraOutputDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraOutputDefinition>();
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                return definition;
            }

            private CameraPresentationDefinition CreatePresentation(
                string label)
            {
                CameraPresentationDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraPresentationDefinition>();
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                SetField(
                    definition,
                    "outputDefinition",
                    OutputDefinition);
                SetField(
                    definition,
                    "subjectPolicy",
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection);
                SetField(
                    definition,
                    "requestPrecedence",
                    100);

                GameObject rigRoot =
                    CreateRoot($"Presentation Rig {label}");
                CameraRigComposer composer =
                    rigRoot.AddComponent<CameraRigComposer>();
                FixedCameraRigBehaviorDefinition behavior =
                    ScriptableObject.CreateInstance<
                        FixedCameraRigBehaviorDefinition>();
                _created.Add(behavior);
                SetField(
                    composer,
                    "behaviorDefinition",
                    behavior);
                SetField(
                    composer,
                    "cinemachineCamera",
                    rigRoot.AddComponent<CinemachineCamera>());
                SetField(
                    definition,
                    "rigPrefab",
                    rigRoot);
                return definition;
            }

            private PlayerSlotProfile CreateProfile(
                string slot)
            {
                PlayerSlotProfile profile =
                    ScriptableObject.CreateInstance<PlayerSlotProfile>();
                _created.Add(profile);
                SetField(
                    profile,
                    "playerSlotId",
                    slot);
                return profile;
            }

            private static PlayerCameraPresentationBindingAuthoring Binding(
                PlayerSlotProfile profile,
                CameraPresentationDefinition presentation)
            {
                var binding =
                    new PlayerCameraPresentationBindingAuthoring();
                binding.Configure(
                    profile,
                    presentation);
                return binding;
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
            private readonly List<UnityEngine.Object> _created =
                new List<UnityEngine.Object>();
            private readonly CameraOutputDefinition _output;

            internal ValidationFixture()
            {
                _output =
                    ScriptableObject.CreateInstance<
                        CameraOutputDefinition>();
                _created.Add(_output);
                SetField(
                    _output,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
            }

            internal CameraPresentationDefinition CreatePresentation(
                CameraSharedCompositionSubjectPolicyKind policy)
            {
                CameraPresentationDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraPresentationDefinition>();
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                SetField(
                    definition,
                    "outputDefinition",
                    _output);
                SetField(
                    definition,
                    "subjectPolicy",
                    policy);

                GameObject rigRoot =
                    new GameObject("validation-rig");
                _created.Add(rigRoot);
                CameraRigComposer composer =
                    rigRoot.AddComponent<CameraRigComposer>();
                FixedCameraRigBehaviorDefinition behavior =
                    ScriptableObject.CreateInstance<
                        FixedCameraRigBehaviorDefinition>();
                _created.Add(behavior);
                SetField(
                    composer,
                    "behaviorDefinition",
                    behavior);
                SetField(
                    composer,
                    "cinemachineCamera",
                    rigRoot.AddComponent<CinemachineCamera>());
                SetField(
                    definition,
                    "rigPrefab",
                    rigRoot);
                return definition;
            }

            internal void CopyPresentationIdentity(
                CameraPresentationDefinition source,
                CameraPresentationDefinition target)
            {
                FieldInfo field =
                    typeof(CameraPresentationDefinition).GetField(
                        "stableId",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(
                    target,
                    field.GetValue(source));
            }

            internal PlayerCameraPresentationBindingAuthoring Binding(
                string slot,
                CameraPresentationDefinition presentation)
            {
                PlayerSlotProfile profile =
                    ScriptableObject.CreateInstance<PlayerSlotProfile>();
                _created.Add(profile);
                SetField(
                    profile,
                    "playerSlotId",
                    slot);

                var binding =
                    new PlayerCameraPresentationBindingAuthoring();
                binding.Configure(
                    profile,
                    presentation);
                return binding;
            }

            public void Dispose()
            {
                for (int index = _created.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (_created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            _created[index]);
                    }
                }
            }
        }

        private static void SetField(
            object target,
            string name,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
