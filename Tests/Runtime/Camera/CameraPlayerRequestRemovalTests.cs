using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraPlayerRequestRemovalTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void SharedPlayerSubjectJoinLeaveRejoin_UsesDefaultOutputWithZeroRequests()
        {
            CameraRigComposer composer = Composer("shared-composer", out CinemachineCamera camera);
            CameraOutputAuthoring output = Output(composer);
            var availability = new CameraSubjectAvailabilityContext("session-subjects");
            CameraSharedComposition composition = Composition(availability, output, composer);

            AssertStableOutput(output, composition, composer, camera, 0);

            CameraSubject p1A = Subject("p1-a");
            CameraSubjectAvailabilityToken p1Token = MakeAvailable(availability, p1A);
            AssertStableOutput(output, composition, composer, camera, 1);
            Assert.That(camera.Follow, Is.SameAs(p1A.Observation));

            CameraSubject p2 = Subject("p2");
            MakeAvailable(availability, p2);
            AssertStableOutput(output, composition, composer, camera, 2);
            CinemachineTargetGroup sharedGroup = composer.FrameworkOwnedSharedFollowTargetGroup;
            Assert.That(sharedGroup.Targets.Count, Is.EqualTo(2));

            availability.TryMakeUnavailable(p1Token);
            AssertStableOutput(output, composition, composer, camera, 1);
            Assert.That(camera.Follow, Is.SameAs(p2.Observation));

            CameraSubject p1B = Subject("p1-b");
            MakeAvailable(availability, p1B);
            AssertStableOutput(output, composition, composer, camera, 2);
            Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup, Is.SameAs(sharedGroup));
            Assert.That(
                composer.FrameworkOwnedSharedFollowTargetGroup.Targets.Any(
                    target => ReferenceEquals(target.Object, p1A.Observation)),
                Is.False);
        }

        [Test]
        public void ExplicitRouteRequest_StillPublishesAndReleasesThroughGenericArbitration()
        {
            CameraRigComposer defaultComposer = Composer(
                "default-composer",
                out CinemachineCamera defaultCamera);
            CameraOutputAuthoring output = Output(defaultComposer);
            CameraRigComposer routeComposer = Composer("route-composer", out _);
            Transform target = Subject("route-target").Observation;
            CameraRequestCreateResult request = CameraRequestCreateResult.Create(
                new CameraRequestId("route-request"),
                CameraOutputId.Main,
                new CameraRequestOwner(CameraRequestOwnerKind.Route, "route-owner"),
                new CameraRequestLifetime(CameraRequestLifetimeKind.Route, "route-occurrence"),
                CameraRigReference.FromComposer(routeComposer),
                CameraTargetSourceDescriptor.ExplicitTransform(target, "Route target"),
                new CameraRequestPolicy(100, "route-request"),
                CameraRequestReleaseCondition.ExplicitRelease,
                nameof(CameraPlayerRequestRemovalTests),
                "generic-route-arbitration-preserved");

            Assert.That(request.IsSucceeded, Is.True, request.BlockingIssue);
            CameraRequestPublisherCreateResult publisherResult =
                RouteCameraRequestPublisher.Create(output.Session, request.Request);
            Assert.That(publisherResult.Succeeded, Is.True, publisherResult.DiagnosticSummary);

            ICameraRequestPublisher publisher = publisherResult.Publisher;
            Assert.That(publisher.Publish().Succeeded, Is.True);
            Assert.That(output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(output.Context.Winner.Owner.Kind, Is.EqualTo(CameraRequestOwnerKind.Route));

            Assert.That(publisher.Release().Succeeded, Is.True);
            Assert.That(output.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(output.Context.HasWinner, Is.False);
            Assert.That(output.Applicator.HasAppliedDefault, Is.True);
            Assert.That(output.Applicator.AppliedCamera, Is.SameAs(defaultCamera));
        }

        private CameraSharedComposition Composition(
            CameraSubjectAvailabilityContext availability,
            CameraOutputAuthoring output,
            CameraRigComposer composer)
        {
            var root = new GameObject("shared-composition");
            _created.Add(root);
            CameraSharedComposition composition = root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                new CameraView(new CameraViewId("shared-view"), "Shared View"),
                "shared-assignments",
                new CameraSubjectAssignmentOwnerId("shared-composition-owner"),
                CameraOutputId.Main,
                composer,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            composition.AttachOutputSession(output);
            composition.AttachCameraSubjectAvailability(availability);
            return composition;
        }

        private CameraRigComposer Composer(string name, out CinemachineCamera camera)
        {
            var root = new GameObject(name);
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            var cameraObject = new GameObject($"{name}-camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<CinemachineCamera>();
            SetField(composer, "cinemachineCamera", camera);
            return composer;
        }

        private CameraOutputAuthoring Output(CameraRigComposer defaultComposer)
        {
            var root = new GameObject("main-output");
            _created.Add(root);
            UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            SetField(output, "outputId", CameraOutputId.Main.Value);
            SetField(output, "unityCamera", unityCamera);
            SetField(output, "cinemachineBrain", brain);
            SetField(output, "defaultCameraRig", defaultComposer);
            Assert.That(output.TryInitialize(out string diagnostic), Is.True, diagnostic);
            return output;
        }

        private CameraSubject Subject(string id)
        {
            var root = new GameObject(id);
            _created.Add(root);
            return new CameraSubject(new CameraSubjectId(id), root.transform, id);
        }

        private static CameraSubjectAvailabilityToken MakeAvailable(
            CameraSubjectAvailabilityContext availability,
            CameraSubject subject) =>
            availability.TryMakeAvailable(
                subject,
                new CameraSubjectAvailabilityOwnerId("player-physical-lifetime")).Token;

        private static void AssertStableOutput(
            CameraOutputAuthoring output,
            CameraSharedComposition composition,
            CameraRigComposer composer,
            CinemachineCamera camera,
            int subjectCount)
        {
            Assert.That(output.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(output.Context.HasWinner, Is.False);
            Assert.That(output.DefaultCameraRig, Is.SameAs(composer));
            Assert.That(output.Applicator.HasAppliedDefault, Is.True);
            Assert.That(output.Applicator.AppliedCamera, Is.SameAs(camera));
            Assert.That(composition.ViewId, Is.EqualTo(new CameraViewId("shared-view")));
            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(subjectCount));
            Assert.That(composer.GetComponentsInChildren<CinemachineCamera>(true), Has.Length.EqualTo(1));
            Assert.That(composer.GetComponentsInChildren<CinemachineCamera>(true)[0], Is.SameAs(camera));
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
