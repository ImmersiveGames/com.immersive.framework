using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class SessionCameraAssignmentRuntimeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> _created = new List<Object>();
        private readonly List<System.IDisposable> _runtimes = new List<System.IDisposable>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _runtimes.Count - 1; index >= 0; index--)
            {
                _runtimes[index].Dispose();
            }
            _runtimes.Clear();
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            }
            _created.Clear();
        }

        [Test]
        public void StartupPresentsNormalSessionOccurrenceWithoutPlayers()
        {
            Fixture fixture = CreateFixture(1, CameraTargetPolicy.NoSubject);

            Assert.That(SessionCameraAssignmentRuntime.TryCreate(
                fixture.Assignments, fixture.Topology, fixture.Root.transform,
                out SessionCameraAssignmentRuntime runtime, out string issue), Is.True, issue);
            _runtimes.Add(runtime);

            CameraOutputSession output = fixture.Outputs[0].Session;
            CameraOccurrenceIdentity expected = CameraOccurrenceIdentity.ForSessionOrShared(
                fixture.AssignmentId, fixture.OutputDefinitions[0].OutputId);
            Assert.That(output.OutputState.ActiveAssignmentId, Is.EqualTo(fixture.AssignmentId));
            Assert.That(output.OutputState.PresentedNormalOccurrence, Is.EqualTo(expected));
            Assert.That(output.OutputState.IsFallbackCovering, Is.False);
            Assert.That(output.Applicator.HasAppliedFallback, Is.False);
            Assert.That(output.Applicator.AppliedCamera, Is.EqualTo(runtime.Occurrences[0].Composer.CinemachineCamera));
            Assert.That(runtime.Occurrences[0].Identity, Is.EqualTo(expected));

            SessionCameraOccurrence occurrenceBeforePlayerChanges = runtime.Occurrences[0];
            fixture.Outputs[0].SetPlayerPhysicalParticipation(false);
            fixture.Outputs[0].SetPlayerPhysicalParticipation(true);
            Assert.That(runtime.Occurrences[0], Is.SameAs(occurrenceBeforePlayerChanges));
            Assert.That(output.OutputState.PresentedNormalOccurrence, Is.EqualTo(expected));
            Assert.That(output.Applicator.AppliedCamera, Is.EqualTo(runtime.Occurrences[0].Composer.CinemachineCamera));
            Assert.That(fixture.Outputs[0].UnityCamera.enabled, Is.True);
        }

        [Test]
        public void AssignmentMaterializationDoesNotCreatePlayerMembership()
        {
            Fixture fixture = CreateFixture(1, CameraTargetPolicy.NoSubject);
            Assert.That(SessionCameraAssignmentRuntime.TryCreate(
                fixture.Assignments, fixture.Topology, fixture.Root.transform,
                out SessionCameraAssignmentRuntime runtime, out string issue), Is.True, issue);
            _runtimes.Add(runtime);

            Assert.That(runtime.Occurrences, Has.Count.EqualTo(1));
            Assert.That(runtime.Occurrences[0].Assignment.MembershipPolicy, Is.EqualTo(CameraMembershipPolicy.None));
            Assert.That(runtime.Occurrences[0].Assignment.MemberSlots, Is.Empty);
            Assert.That(runtime.Occurrences[0].Identity.IsIndividual, Is.False);
        }

        [Test]
        public void ReusedDefinitionCreatesIndependentOccurrencesPerAssignmentAndOutput()
        {
            Fixture fixture = CreateFixture(2, CameraTargetPolicy.NoSubject, twoAssignments: true);
            Assert.That(SessionCameraAssignmentRuntime.TryCreate(
                fixture.Assignments, fixture.Topology, fixture.Root.transform,
                out SessionCameraAssignmentRuntime runtime, out string issue), Is.True, issue);
            _runtimes.Add(runtime);

            Assert.That(runtime.Occurrences, Has.Count.EqualTo(2));
            Assert.That(runtime.Occurrences[0].Definition, Is.SameAs(runtime.Occurrences[1].Definition));
            Assert.That(runtime.Occurrences[0].Identity, Is.Not.EqualTo(runtime.Occurrences[1].Identity));
            Assert.That(runtime.Occurrences[0].Composer, Is.Not.SameAs(runtime.Occurrences[1].Composer));
            Assert.That(runtime.Occurrences[0].Root, Is.Not.SameAs(runtime.Occurrences[1].Root));
            runtime.Occurrences[0].Composer.CinemachineCamera.enabled = false;
            Assert.That(runtime.Occurrences[1].Composer.CinemachineCamera.enabled, Is.True);
        }

        [Test]
        public void InvalidAssignmentLeavesOutputFallbackUntouched()
        {
            Fixture fixture = CreateFixture(1, CameraTargetPolicy.MemberActorTargets);
            CameraOutputSession output = fixture.Outputs[0].Session;
            CinemachineCamera fallbackCamera = output.Applicator.AppliedCamera;

            Assert.That(SessionCameraAssignmentRuntime.TryCreate(
                fixture.Assignments, fixture.Topology, fixture.Root.transform,
                out SessionCameraAssignmentRuntime runtime, out string issue), Is.False);
            Assert.That(runtime, Is.Null);
            Assert.That(string.IsNullOrWhiteSpace(issue), Is.False);
            Assert.That(output.OutputState.HasActiveAssignment, Is.False);
            Assert.That(output.OutputState.IsFallbackCovering, Is.True);
            Assert.That(output.Applicator.HasAppliedFallback, Is.True);
            Assert.That(output.Applicator.AppliedCamera, Is.EqualTo(fallbackCamera));
        }

        [Test]
        public void SessionOccurrenceMembershipChangesWithoutReplacingOccurrence()
        {
            SessionCameraOccurrence occurrence = CreateMembershipOccurrence(
                out PlayerSlotId firstSlot,
                out PlayerSlotId secondSlot);
            SessionCameraOccurrence originalOccurrence = occurrence;
            CameraOccurrenceIdentity identity = occurrence.Identity;
            var firstPlayer = new PlayerOccurrenceId("player-occurrence:first");
            var secondPlayer = new PlayerOccurrenceId("player-occurrence:second");

            Assert.That(occurrence.Members, Is.Empty);
            Assert.That(occurrence.ReconcileMember(firstPlayer, firstSlot, default), Is.True);
            Assert.That(occurrence.Members, Has.Count.EqualTo(1));
            Assert.That(occurrence.ReconcileMember(secondPlayer, secondSlot, default), Is.True);
            Assert.That(occurrence.Members, Has.Count.EqualTo(2));

            occurrence.RemoveMember(firstPlayer);

            Assert.That(occurrence.Members, Has.Count.EqualTo(1));
            Assert.That(occurrence.Members[0].PlayerOccurrenceId, Is.EqualTo(secondPlayer));
            Assert.That(occurrence.Identity, Is.EqualTo(identity));
            Assert.That(occurrence, Is.SameAs(originalOccurrence));
        }

        [Test]
        public void ActorReplacementUpdatesMemberSubjectAndMissingActorKeepsMembership()
        {
            SessionCameraOccurrence occurrence = CreateMembershipOccurrence(
                out PlayerSlotId firstSlot,
                out _);
            var player = new PlayerOccurrenceId("player-occurrence:stable");
            var firstSubjectRoot = new GameObject("First Subject");
            var replacementSubjectRoot = new GameObject("Replacement Subject");
            _created.Add(firstSubjectRoot);
            _created.Add(replacementSubjectRoot);
            var firstSubject = new CameraSubject(
                new CameraSubjectId("subject.actor.first"),
                firstSubjectRoot.transform,
                "first");
            var replacementSubject = new CameraSubject(
                new CameraSubjectId("subject.actor.replacement"),
                replacementSubjectRoot.transform,
                "replacement");

            Assert.That(occurrence.ReconcileMember(player, firstSlot, firstSubject), Is.True);
            Assert.That(occurrence.ReconcileMember(player, firstSlot, replacementSubject), Is.True);
            Assert.That(occurrence.Members, Has.Count.EqualTo(1));
            Assert.That(occurrence.Members[0].Subject.SubjectId, Is.EqualTo(replacementSubject.SubjectId));
            Assert.That(occurrence.Identity.IsIndividual, Is.False);

            Assert.That(occurrence.ReconcileMember(player, firstSlot, default), Is.True);
            Assert.That(occurrence.Members, Has.Count.EqualTo(1));
            Assert.That(occurrence.Members[0].HasSubject, Is.False);
            Assert.That(occurrence.Identity, Is.EqualTo(CameraOccurrenceIdentity.ForSessionOrShared(
                occurrence.Assignment.Id,
                occurrence.Identity.OutputId)));
        }

        [Test]
        public void PlayerOccurrenceIdentityUsesAllocationLifetimeIndependentOfProviderOrigin()
        {
            PlayerOccurrenceId sceneProvided = PlayerOccurrenceId.Create(
                "session.identity",
                1,
                PlayerSlotId.Player1);
            PlayerOccurrenceId managerProvisioned = PlayerOccurrenceId.Create(
                "session.identity",
                1,
                PlayerSlotId.Player1);
            PlayerOccurrenceId rejoined = PlayerOccurrenceId.Create(
                "session.identity",
                2,
                PlayerSlotId.Player1);

            Assert.That(sceneProvided, Is.EqualTo(managerProvisioned));
            Assert.That(rejoined, Is.Not.EqualTo(sceneProvided));
        }

        private SessionCameraOccurrence CreateMembershipOccurrence(
            out PlayerSlotId firstSlot,
            out PlayerSlotId secondSlot)
        {
            firstSlot = PlayerSlotId.Player1;
            secondSlot = PlayerSlotId.Player2;
            var assignment = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.members"),
                new CameraDefinitionId("definition.members"),
                CameraOccurrenceMode.SharedGroup,
                CameraMembershipPolicy.ExplicitPlayerSlots,
                CameraTargetPolicy.MemberActorTargets,
                new[] { new CameraOutputMapping(new CameraOutputId("output.members")) },
                new[] { firstSlot, secondSlot });
            var root = new GameObject("Member Camera Occurrence");
            _created.Add(root);
            return new SessionCameraOccurrence(
                assignment,
                null,
                CameraOccurrenceIdentity.ForSessionOrShared(
                    assignment.Id,
                    new CameraOutputId("output.members")),
                root,
                null,
                null);
        }

        private Fixture CreateFixture(int outputCount, CameraTargetPolicy targetPolicy, bool twoAssignments = false)
        {
            var root = new GameObject("Session Camera Test");
            _created.Add(root);
            var definition = ScriptableObject.CreateInstance<CameraDefinition>();
            _created.Add(definition);
            SetField(definition, "stableId", "11111111111111111111111111111111");
            GameObject rigPrefab = CreateRig("Normal Rig", out _);
            SetField(definition, "rigPrefab", rigPrefab);

            var outputs = new CameraOutputAuthoring[outputCount];
            var outputDefinitions = new CameraOutputDefinition[outputCount];
            for (int index = 0; index < outputCount; index++)
            {
                outputDefinitions[index] = ScriptableObject.CreateInstance<CameraOutputDefinition>();
                _created.Add(outputDefinitions[index]);
                SetField(outputDefinitions[index], "stableId", (index + 1).ToString("x32"));

                var outputRoot = new GameObject("Output " + index);
                outputRoot.transform.SetParent(root.transform, false);
                outputRoot.SetActive(false);
                _created.Add(outputRoot);
                UnityEngine.Camera unityCamera = outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = outputRoot.AddComponent<CinemachineBrain>();
                OutputChannels outputChannel = (OutputChannels)(1 << index);
                brain.ChannelMask = outputChannel;
                GameObject fallbackRoot = CreateRig("Fallback " + index, out CameraRigComposer fallbackComposer);
                fallbackRoot.transform.SetParent(outputRoot.transform, false);
                fallbackComposer.CinemachineCamera.OutputChannel = outputChannel;
                var authoring = outputRoot.AddComponent<CameraOutputAuthoring>();
                SetField(authoring, "outputDefinition", outputDefinitions[index]);
                SetField(authoring, "unityCamera", unityCamera);
                SetField(authoring, "cinemachineBrain", brain);
                SetField(authoring, "fallbackCameraRig", fallbackComposer);
                SetField(authoring, "initializeOnAwake", false);
                outputRoot.SetActive(true);
                outputs[index] = authoring;
            }

            Assert.That(CameraOutputSessionTopology.TryCreate(outputs, out CameraOutputSessionTopology topology, out string topologyIssue), Is.True, topologyIssue);
            _runtimes.Add(topology);

            var assignments = new List<SessionCameraAssignmentAuthoring>();
            int assignmentCount = twoAssignments ? 2 : 1;
            for (int index = 0; index < assignmentCount; index++)
            {
                var assignment = new SessionCameraAssignmentAuthoring();
                SetField(assignment, "assignmentId", "assignment." + index);
                SetField(assignment, "definition", definition);
                SetField(assignment, "occurrenceMode", CameraOccurrenceMode.SessionScoped);
                SetField(assignment, "membershipPolicy", CameraMembershipPolicy.None);
                SetField(assignment, "targetPolicy", targetPolicy);
                SetField(assignment, "outputDefinitions", new List<CameraOutputDefinition> { outputDefinitions[index] });
                assignments.Add(assignment);
            }

            return new Fixture(root, topology, outputs, outputDefinitions, assignments,
                new SessionCameraAssignmentId("assignment.0"));
        }

        private GameObject CreateRig(string name, out CameraRigComposer composer)
        {
            var rigRoot = new GameObject(name);
            _created.Add(rigRoot);
            var behavior = ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            var camera = rigRoot.AddComponent<CinemachineCamera>();
            composer = rigRoot.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);
            SetField(composer, "cinemachineCamera", camera);
            return rigRoot;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }

        private sealed class Fixture
        {
            internal Fixture(GameObject root, CameraOutputSessionTopology topology,
                CameraOutputAuthoring[] outputs, CameraOutputDefinition[] outputDefinitions,
                List<SessionCameraAssignmentAuthoring> assignments, SessionCameraAssignmentId assignmentId)
            {
                Root = root;
                Topology = topology;
                Outputs = outputs;
                OutputDefinitions = outputDefinitions;
                Assignments = assignments;
                AssignmentId = assignmentId;
            }

            internal GameObject Root { get; }
            internal CameraOutputSessionTopology Topology { get; }
            internal CameraOutputAuthoring[] Outputs { get; }
            internal CameraOutputDefinition[] OutputDefinitions { get; }
            internal List<SessionCameraAssignmentAuthoring> Assignments { get; }
            internal SessionCameraAssignmentId AssignmentId { get; }
        }
    }
}
