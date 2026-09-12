using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraOutputSessionTopologyTests
    {
        private readonly CameraDefinitionTestAssets _definitions = new CameraDefinitionTestAssets();
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
            _definitions.Dispose();
        }

        [Test]
        public void OneOutput_CreatesExactSessionScopedTopology()
        {
            CameraOutputAuthoring main = Output("10000000000000000000000000000001");

            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { main }, out CameraOutputSessionTopology topology, out string diagnostic),
                Is.True, diagnostic);
            Assert.That(topology.OutputCount, Is.EqualTo(1));
            Assert.That(topology.TryGetOutput(new CameraOutputId("10000000000000000000000000000001"), out CameraOutputAuthoring resolved, out _), Is.True);
            Assert.That(resolved, Is.SameAs(main));
            Assert.That(topology.CaptureSnapshot().Outputs[0].OutputId.Value, Is.EqualTo("10000000000000000000000000000001"));
        }

        [Test]
        public void TwoOutputs_AreOrderedAndKeepIndependentDefaults()
        {
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");

            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputB, outputA }, out CameraOutputSessionTopology topology, out string diagnostic),
                Is.True, diagnostic);
            CameraOutputTopologySnapshot snapshot = topology.CaptureSnapshot();
            Assert.That(snapshot.OutputCount, Is.EqualTo(2));
            Assert.That(snapshot.Outputs[0].OutputId.Value, Is.EqualTo("10000000000000000000000000000002"));
            Assert.That(snapshot.Outputs[1].OutputId.Value, Is.EqualTo("10000000000000000000000000000003"));
            Assert.That(outputA.Applicator.HasAppliedDefault, Is.True);
            Assert.That(outputB.Applicator.HasAppliedDefault, Is.True);
            Assert.That(outputA.Applicator.AppliedCamera, Is.Not.SameAs(outputB.Applicator.AppliedCamera));
        }

        [Test]
        public void DuplicateOrMissingOutputId_IsRejectedWithoutArbitrarySelection()
        {
            CameraOutputAuthoring first = Output("10000000000000000000000000000004");
            CameraOutputAuthoring duplicate = Output("10000000000000000000000000000004");
            CameraOutputAuthoring missing = Output("10000000000000000000000000000005");
            SetField<CameraOutputDefinition>(missing, "outputDefinition", null);

            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { first, duplicate }, out _, out string duplicateDiagnostic), Is.False);
            Assert.That(duplicateDiagnostic, Does.Contain("stable ID collision"));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { missing }, out _, out string missingDiagnostic), Is.False);
            Assert.That(missingDiagnostic, Does.Contain("Missing or invalid Camera Output definition"));
        }

        [Test]
        public void RouteOnAAndCutsceneOnB_CoexistAndReleaseIndependently()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);

            CameraRequest route = Request(outputA, "route-a", CameraRequestOwnerKind.Route, 100);
            CameraRequest cutscene = Request(outputB, "cutscene-b", CameraRequestOwnerKind.Cutscene, 200);
            Assert.That(outputA.Session.Admit(route).Succeeded, Is.True);
            Assert.That(outputB.Session.Admit(cutscene).Succeeded, Is.True);
            Assert.That(outputA.Context.Winner.RequestId, Is.EqualTo(route.RequestId));
            Assert.That(outputB.Context.Winner.RequestId, Is.EqualTo(cutscene.RequestId));

            Assert.That(outputA.Session.Release(route.RequestId).Succeeded, Is.True);
            Assert.That(outputA.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(outputB.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(outputB.Session.Release(cutscene.RequestId).Succeeded, Is.True);
            Assert.That(outputB.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(topology.OutputCount, Is.EqualTo(2));
        }

        [Test]
        public void Injection_BindsOnlyConsumerRequestedOutputId()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);
            CameraOutputTestConsumer consumer = Consumer("10000000000000000000000000000003");

            Assert.That(CameraViewOutputTopology.TryCreate(
                new[]
                {
                    new CameraViewOutputBinding(
                        new CameraViewId("view.b"),
                        new CameraOutputId("10000000000000000000000000000003"),
                        new CameraViewport(0f, 0f, 1f, 1f))
                }, out CameraViewOutputTopology viewOutputs, out diagnostic), Is.True, diagnostic);
            using var injection = new CameraOutputInjectionRuntime(topology, viewOutputs, System.Array.Empty<CameraViewDefinition>());
            Assert.That(injection.AttachExact(consumer, out diagnostic), Is.True, diagnostic);
            Assert.That(consumer.Attached, Is.SameAs(outputB));
            Assert.That(consumer.Attached, Is.Not.SameAs(outputA));

            consumer.SetRequestedId("camera.output.unknown");
            CameraOutputInjectionResult rejected =
                injection.AttachExact(consumer);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(
                rejected.Status,
                Is.EqualTo(CameraOutputInjectionStatus.RejectedUnknownOutput));
            Assert.That(consumer.Attached, Is.Null);
            Assert.That(
                rejected.Diagnostic,
                Does.Contain("not part of the active Session topology"));
            Assert.That(
                injection.AttachExact(consumer).Status,
                Is.EqualTo(CameraOutputInjectionStatus.RejectedUnknownOutput));
            Assert.That(consumer.DetachCount, Is.EqualTo(1));
        }

        [Test]
        public void PersistentRoots_ReceiveExactDependenciesOnlyOnce()
        {
            CameraOutputAuthoring output = Output(CameraDefinitionTestAssets.MainId);
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);
            CameraViewOutputTopology viewOutputs = ViewOutputs(
                "unused-view",
                CameraDefinitionTestAssets.MainId);
            var availability = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-subjects"));
            using var outputInjection =
                new CameraOutputInjectionRuntime(topology, viewOutputs, System.Array.Empty<CameraViewDefinition>());
            using var availabilityInjection =
                new CameraSubjectAvailabilityInjectionRuntime(availability);
            var root = new GameObject("retained-root");
            _created.Add(root);
            CameraPersistentInjectionTestConsumer consumer =
                root.AddComponent<CameraPersistentInjectionTestConsumer>();
            consumer.SetRequestedId(CameraDefinitionTestAssets.MainId);

            outputInjection.AttachRoots(new[] { root });
            availabilityInjection.AttachRoots(new[] { root });
            outputInjection.AttachRoots(new[] { root });
            availabilityInjection.AttachRoots(new[] { root });

            Assert.That(consumer.AttachedOutput, Is.SameAs(output));
            Assert.That(consumer.AvailabilitySource, Is.SameAs(availability));
            Assert.That(consumer.OutputAttachCount, Is.EqualTo(1));
            Assert.That(consumer.AvailabilityAttachCount, Is.EqualTo(1));
            Assert.That(topology.OutputCount, Is.EqualTo(1));
            Assert.That(output.Context.CaptureSnapshot().AdmittedRequestCount, Is.Zero);
        }

        [Test]
        public void PersistentSharedComposition_ReceivesExactDependenciesAndIsReadyWhileEmpty()
        {
            CameraOutputAuthoring output = Output(CameraDefinitionTestAssets.MainId);
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);
            CameraViewOutputTopology viewOutputs = ViewOutputs(
                CameraDefinitionTestAssets.ViewId,
                CameraDefinitionTestAssets.MainId);
            var availability = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-subjects"));
            var viewDefinition = _definitions.View();
            using var outputInjection =
                new CameraOutputInjectionRuntime(topology, viewOutputs, new[] { viewDefinition });
            using var availabilityInjection =
                new CameraSubjectAvailabilityInjectionRuntime(availability);
            var root = new GameObject("retained-shared-composition");
            _created.Add(root);
            CameraSharedComposition composition =
                root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                viewDefinition,
                output.OutputDefinition,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);

            outputInjection.AttachRoots(new[] { root });
            availabilityInjection.AttachRoots(new[] { root });
            CameraSharedCompositionSnapshot readyWhileEmpty =
                composition.Snapshot;
            outputInjection.AttachRoots(new[] { root });
            availabilityInjection.AttachRoots(new[] { root });

            Assert.That(composition.Output, Is.SameAs(output));
            Assert.That(composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(composition.Snapshot.IsReady, Is.True);
            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(
                composition.Snapshot.AvailabilityRevisionConsumed,
                Is.EqualTo(readyWhileEmpty.AvailabilityRevisionConsumed));
            Assert.That(topology.OutputCount, Is.EqualTo(1));
            Assert.That(output.Context.CaptureSnapshot().AdmittedRequestCount, Is.Zero);
        }

        [Test]
        public void PersistentSessionOverride_ReceivesOutputAndRemainsOwnerActive()
        {
            CameraOutputAuthoring output = Output(CameraDefinitionTestAssets.MainId);
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);
            using var injection = new CameraOutputInjectionRuntime(
                topology,
                ViewOutputs("unused-view", CameraDefinitionTestAssets.MainId), System.Array.Empty<CameraViewDefinition>());
            var root = new GameObject("retained-session-override");
            _created.Add(root);
            SessionCameraOverride sessionOverride =
                root.AddComponent<SessionCameraOverride>();
            SetScopedOverrideField(
                sessionOverride,
                "outputDefinition",
                output.OutputDefinition);

            injection.AttachRoots(new[] { root });

            Assert.That(sessionOverride.OutputSession, Is.SameAs(output));
            Assert.That(sessionOverride.IsOwnerActive, Is.True);
            Assert.That(sessionOverride.IsPublished, Is.False);
        }

        [Test]
        public void SubsequentLoadedScenePath_StillInjectsConsumer()
        {
            CameraOutputAuthoring output = Output("10000000000000000000000000000006");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology topology, out string diagnostic), Is.True, diagnostic);
            using var injection = new CameraOutputInjectionRuntime(
                topology,
                ViewOutputs("unused-view", "10000000000000000000000000000006"), System.Array.Empty<CameraViewDefinition>());
            var availability = new CameraSubjectAvailabilityContext(
                new SubjectAvailabilityContextId("session-subjects"));
            using var availabilityInjection =
                new CameraSubjectAvailabilityInjectionRuntime(availability);
            var root = new GameObject("future-route-consumer");
            _created.Add(root);
            CameraPersistentInjectionTestConsumer consumer =
                root.AddComponent<CameraPersistentInjectionTestConsumer>();
            consumer.SetRequestedId("10000000000000000000000000000006");

            Scene scene = root.scene;
            injection.AttachScene(scene);
            availabilityInjection.AttachScene(scene);

            Assert.That(consumer.AttachedOutput, Is.SameAs(output));
            Assert.That(consumer.AvailabilitySource, Is.SameAs(availability));
            Assert.That(consumer.OutputAttachCount, Is.EqualTo(1));
            Assert.That(consumer.AvailabilityAttachCount, Is.EqualTo(1));
        }

        [Test]
        public void PlayerSubjectJoinLeave_DoesNotChangeOutputCount()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology topology, out _), Is.True);
            var availability = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-subjects"));
            var subjectObject = new GameObject("player-subject");
            _created.Add(subjectObject);
            var subject = new CameraSubject(new CameraSubjectId("player-1"), subjectObject.transform, "Player 1");
            CameraSubjectAvailabilityResult joined = availability.TryMakeAvailable(
                subject, new CameraSubjectAvailabilityOwnerId("player-physical-lifetime"));
            Assert.That(joined.Succeeded, Is.True);
            Assert.That(topology.OutputCount, Is.EqualTo(2));
            Assert.That(availability.TryMakeUnavailable(joined.Token).Succeeded, Is.True);
            Assert.That(topology.OutputCount, Is.EqualTo(2));
        }

        [Test]
        public void SessionTeardown_TearsDownAllOutputsInStableSnapshotOrder()
        {
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputB, outputA }, out CameraOutputSessionTopology topology, out _), Is.True);

            topology.Dispose();
            CameraOutputTopologySnapshot snapshot = topology.CaptureSnapshot();
            Assert.That(snapshot.IsTornDown, Is.True);
            Assert.That(snapshot.Outputs[0].OutputId.Value, Is.EqualTo("10000000000000000000000000000002"));
            Assert.That(snapshot.Outputs[1].OutputId.Value, Is.EqualTo("10000000000000000000000000000003"));
            Assert.That(outputA.IsInitialized, Is.False);
            Assert.That(outputB.IsInitialized, Is.False);
            Assert.That(topology.TryGetOutput(new CameraOutputId("10000000000000000000000000000002"), out _, out _), Is.False);
            Assert.DoesNotThrow(topology.Dispose);
        }

        [Test]
        public void IndividualOutputTeardown_DoesNotMutateOtherOutput()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002");
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003");
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out _, out string diagnostic), Is.True, diagnostic);
            CameraRequest requestB = Request(outputB, "route-b", CameraRequestOwnerKind.Route, 100);
            Assert.That(outputB.Session.Admit(requestB).Succeeded, Is.True);

            outputA.TeardownSession("test-independent-output-release");

            Assert.That(outputA.IsInitialized, Is.False);
            Assert.That(outputB.IsInitialized, Is.True);
            Assert.That(outputB.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(outputB.Context.Winner.RequestId, Is.EqualTo(requestB.RequestId));
        }

        private CameraOutputAuthoring Output(string outputId)
        {
            CameraRigComposer composer = Composer($"{outputId}-default");
            var root = new GameObject(outputId);
            _created.Add(root);
            root.SetActive(false);
            UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            SetField(output, "outputDefinition", _definitions.Output(outputId));
            SetField(output, "unityCamera", unityCamera);
            SetField(output, "cinemachineBrain", brain);
            SetField(output, "defaultCameraRig", composer);
            SetField(output, "initializeOnAwake", false);
            root.SetActive(true);
            return output;
        }

        private CameraRigComposer Composer(string name)
        {
            var root = new GameObject(name);
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", _definitions.Behavior<FollowCameraRigBehaviorDefinition>());
            var cameraObject = new GameObject($"{name}-camera");
            cameraObject.transform.SetParent(root.transform, false);
            CinemachineCamera camera = cameraObject.AddComponent<CinemachineCamera>();
            SetField(composer, "cinemachineCamera", camera);
            return composer;
        }

        private static CameraViewOutputTopology ViewOutputs(
            string viewId,
            string outputId)
        {
            Assert.That(CameraViewOutputTopology.TryCreate(
                new[]
                {
                    new CameraViewOutputBinding(
                        new CameraViewId(viewId),
                        new CameraOutputId(outputId),
                        new CameraViewport(0f, 0f, 1f, 1f))
                },
                out CameraViewOutputTopology topology,
                out string diagnostic), Is.True, diagnostic);
            return topology;
        }

        private CameraRequest Request(
            CameraOutputAuthoring output,
            string requestId,
            CameraRequestOwnerKind ownerKind,
            int precedence)
        {
            CameraRigComposer composer = Composer($"{requestId}-rig");
            var targetObject = new GameObject($"{requestId}-target");
            _created.Add(targetObject);
            CameraRequestCreateResult result = CameraRequestCreateResult.Create(
                new CameraRequestId(requestId),
                new CameraOutputId(output.OutputIdText),
                new CameraRequestOwner(ownerKind, new CameraRequestOwnerScopeId($"{requestId}-owner")),
                new CameraRequestLifetime(CameraRequestLifetimeKind.Session, new CameraRequestLifetimeScopeId("session")),
                CameraRigReference.FromComposer(composer),
                CameraTargetSourceDescriptor.ExplicitTransform(targetObject.transform, requestId),
                new CameraRequestPolicy(precedence, requestId),
                CameraRequestReleaseCondition.ExplicitRelease,
                nameof(CameraOutputSessionTopologyTests),
                requestId);
            Assert.That(result.IsSucceeded, Is.True, result.BlockingIssue);
            return result.Request;
        }

        private CameraOutputTestConsumer Consumer(string outputId)
        {
            var root = new GameObject("consumer");
            _created.Add(root);
            CameraOutputTestConsumer consumer = root.AddComponent<CameraOutputTestConsumer>();
            consumer.SetRequestedId(outputId);
            return consumer;
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void SetScopedOverrideField<T>(
            ScopedCameraOverride target,
            string name,
            T value) =>
            typeof(ScopedCameraOverride)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }

    public sealed class CameraOutputTestConsumer : MonoBehaviour, ICameraOutputSessionConsumer
    {
        private CameraOutputId _requested;
        public CameraOutputId RequestedOutputId => _requested;
        public CameraOutputAuthoring Attached { get; private set; }
        public int AttachCount { get; private set; }
        public int DetachCount { get; private set; }
        public void SetRequestedId(string value) => _requested = new CameraOutputId(value);
        public void AttachOutputSession(CameraOutputAuthoring binding)
        {
            Attached = binding;
            AttachCount++;
        }
        public void DetachOutputSession(string reason)
        {
            Attached = null;
            DetachCount++;
        }
    }

    public sealed class CameraPersistentInjectionTestConsumer :
        MonoBehaviour,
        ICameraOutputSessionConsumer,
        ICameraSubjectAvailabilityConsumer
    {
        private CameraOutputId _requested;

        public CameraOutputId RequestedOutputId => _requested;
        public CameraOutputAuthoring AttachedOutput { get; private set; }
        public ICameraSubjectAvailabilitySource AvailabilitySource { get; private set; }
        public int OutputAttachCount { get; private set; }
        public int AvailabilityAttachCount { get; private set; }

        public void SetRequestedId(string value) =>
            _requested = new CameraOutputId(value);

        public void AttachOutputSession(CameraOutputAuthoring binding)
        {
            AttachedOutput = binding;
            OutputAttachCount++;
        }

        public void DetachOutputSession(string reason)
        {
            AttachedOutput = null;
        }

        public void AttachCameraSubjectAvailability(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            AvailabilitySource = availabilitySource;
            AvailabilityAttachCount++;
        }
    }
}
