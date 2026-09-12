using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraSharedCompositionTests
    {
        private readonly CameraDefinitionTestAssets _definitions = new CameraDefinitionTestAssets();
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            }
            _created.Clear();
            _definitions.Dispose();
        }

        [Test]
        public void AvailabilityTransitionsReconcileOnePersistentSharedViewAndRig()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out CinemachineCamera camera);
            CameraSharedComposition composition = Composition(availability, composer);

            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(camera.Follow, Is.Null);

            CameraSubject p1 = Subject("p1");
            CameraSubjectAvailabilityToken p1Token = MakeAvailable(availability, p1);
            AssertApplied(composition, 1, 1, 0, CameraViewPresentationApplyStatus.SucceededSingleSubject);
            Assert.That(camera.Follow, Is.SameAs(p1.Observation));

            CameraSubject p2 = Subject("p2");
            CameraSubjectAvailabilityToken p2Token = MakeAvailable(availability, p2);
            AssertApplied(composition, 2, 1, 0, CameraViewPresentationApplyStatus.SucceededSharedFollow);
            CinemachineTargetGroup group = composer.FrameworkOwnedSharedFollowTargetGroup;
            AssertMembers(group, p1.Observation, p2.Observation);

            CameraSubject p3 = Subject("p3");
            CameraSubjectAvailabilityToken p3Token = MakeAvailable(availability, p3);
            AssertApplied(composition, 3, 1, 0, CameraViewPresentationApplyStatus.SucceededSharedFollow);
            AssertMembers(group, p1.Observation, p2.Observation, p3.Observation);

            availability.TryMakeUnavailable(p2Token);
            AssertApplied(composition, 2, 0, 1, CameraViewPresentationApplyStatus.SucceededSharedFollow);
            AssertMembers(group, p1.Observation, p3.Observation);

            availability.TryMakeUnavailable(p1Token);
            AssertApplied(composition, 1, 0, 1, CameraViewPresentationApplyStatus.SucceededSingleSubject);
            Assert.That(camera.Follow, Is.SameAs(p3.Observation));

            availability.TryMakeUnavailable(p3Token);
            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(composition.ViewId, Is.EqualTo(new CameraViewId(CameraDefinitionTestAssets.ViewId)));
            Assert.That(camera.Follow, Is.Null);
            Assert.That(composer, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(composer.GetComponentsInChildren<CinemachineCamera>(true).Length, Is.EqualTo(1));
            Assert.That(composer.GetComponentsInChildren<CinemachineTargetGroup>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void SameRevisionIsIdempotentAndOlderOrForeignSnapshotsAreRejected()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out _);
            CameraSharedComposition composition = Composition(availability, composer);
            CameraSubjectAvailabilitySnapshot revisionZero = availability.CreateSnapshot();
            MakeAvailable(availability, Subject("p1"));
            int assignmentRevision = composition.Snapshot.AssignmentRevision;

            CameraSharedCompositionSnapshot duplicate =
                composition.Reconcile(availability.CreateSnapshot());
            Assert.That(
                duplicate.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededNoChange));
            Assert.That(duplicate.AssignmentRevision, Is.EqualTo(assignmentRevision));
            Assert.That(duplicate.AddedAssignmentCount, Is.Zero);

            CameraSharedCompositionSnapshot stale = composition.Reconcile(revisionZero);
            Assert.That(
                stale.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.RejectedStaleAvailabilitySnapshot));
            Assert.That(stale.AvailabilityRevisionConsumed, Is.EqualTo(availability.Revision));

            var foreign = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("foreign-subjects"));
            foreign.TryMakeAvailable(Subject("same-looking-id"), AvailabilityOwner());
            CameraSharedCompositionSnapshot rejected =
                composition.Reconcile(foreign.CreateSnapshot());
            Assert.That(
                rejected.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.RejectedForeignAvailabilityContext));
            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(1));
        }

        [Test]
        public void RejoinUsesOnlyTheNewCameraSubjectOccurrence()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out CinemachineCamera camera);
            CameraSharedComposition composition = Composition(availability, composer);
            CameraSubject occurrenceA = Subject("p1-occurrence-a");
            CameraSubjectAvailabilityToken tokenA = MakeAvailable(availability, occurrenceA);

            availability.TryMakeUnavailable(tokenA);
            CameraSubject occurrenceB = Subject("p1-occurrence-b");
            MakeAvailable(availability, occurrenceB);

            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(camera.Follow, Is.SameAs(occurrenceB.Observation));
            Assert.That(camera.Follow, Is.Not.SameAs(occurrenceA.Observation));
            CinemachineTargetGroup group = composer.FrameworkOwnedSharedFollowTargetGroup;
            Assert.That(group == null || group.Targets.Count == 0, Is.True);
        }

        [Test]
        public void SubjectOrderingComesFromDeterministicCameraDomainEvidence()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out _);
            CameraSharedComposition composition = Composition(availability, composer);
            CameraSubject subjectZ = Subject("subject-z");
            CameraSubject subjectA = Subject("subject-a");

            MakeAvailable(availability, subjectZ);
            MakeAvailable(availability, subjectA);

            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(2));
            AssertMembers(
                composer.FrameworkOwnedSharedFollowTargetGroup,
                subjectA.Observation,
                subjectZ.Observation);
        }

        [Test]
        public void ForeignAssignmentIsNotStolenAndTeardownReleasesOnlyOwnedRelations()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out CinemachineCamera camera);
            CameraSharedComposition composition = Composition(availability, composer);
            CameraSubject p1 = Subject("p1");
            MakeAvailable(availability, p1);
            CameraViewAssignmentContext assignments = GetAssignmentContext(composition);
            CameraViewAssignmentSnapshot current = GetAssignmentSnapshot(composition);
            current.TryGetView(composition.ViewId, out CameraViewSubjectSnapshot view);
            assignments.TryRelease(view.Assignments[0].Token, availability.CreateSnapshot());
            CameraSubjectAssignmentOwnerId foreignOwner =
                new CameraSubjectAssignmentOwnerId("foreign-owner");
            assignments.TryAssign(
                composition.ViewId,
                p1.SubjectId,
                foreignOwner,
                availability.CreateSnapshot());
            SetAssignmentSnapshot(
                composition,
                assignments.Reconcile(availability.CreateSnapshot()).Snapshot);

            MakeAvailable(availability, Subject("p2"));
            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedAssignmentFailure));
            CameraViewAssignmentSnapshot beforeTeardown = GetAssignmentSnapshot(composition);
            beforeTeardown.TryGetView(composition.ViewId, out view);
            Assert.That(view.Assignments[0].OwnerId, Is.EqualTo(foreignOwner));

            composition.enabled = false;

            CameraViewAssignmentSnapshot afterTeardown =
                assignments.Reconcile(availability.CreateSnapshot()).Snapshot;
            afterTeardown.TryGetView(new CameraViewId(CameraDefinitionTestAssets.ViewId), out view);
            Assert.That(view.AssignmentCount, Is.EqualTo(1));
            Assert.That(view.Assignments[0].OwnerId, Is.EqualTo(foreignOwner));
            Assert.That(availability.AvailableCount, Is.EqualTo(2));
            Assert.That(camera.Follow, Is.Null);
            Assert.That(composer, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
        }

        [Test]
        public void RebindingOutputClearsOldRigAndUsesOnlyNewOutputsDefaultRig()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer first = Composer(out CinemachineCamera firstCamera);
            CameraSharedComposition composition = Composition(availability, first);
            CameraSubject subject = Subject("p1");
            MakeAvailable(availability, subject);
            Assert.That(firstCamera.Follow, Is.SameAs(subject.Observation));

            CameraRigComposer second = Composer(out CinemachineCamera secondCamera);
            CameraOutputAuthoring nextOutput = Output(second);
            composition.AttachOutputSession(nextOutput);

            Assert.That(composition.Output, Is.SameAs(nextOutput));
            Assert.That(nextOutput.DefaultCameraRig, Is.SameAs(second));
            Assert.That(firstCamera.Follow, Is.Null);
            Assert.That(secondCamera.Follow, Is.SameAs(subject.Observation));
            Assert.That(composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededApplied));
            composition.DetachOutputSession("test teardown");
            Assert.That(secondCamera.Follow, Is.Null);
            Assert.That(availability.AvailableCount, Is.EqualTo(1));
        }

#if UNITY_EDITOR
        [Test]
        public void MissingOutputDefaultRigBlocksCompositionExplicitly()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            var root = new GameObject("missing-default-rig");
            root.SetActive(false);
            _created.Add(root);
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            CameraSharedComposition composition = root.AddComponent<CameraSharedComposition>();
            var serializedOutput = new UnityEditor.SerializedObject(output);
            serializedOutput.FindProperty("outputDefinition").objectReferenceValue = _definitions.Main;
            serializedOutput.FindProperty("initializeOnAwake").boolValue = false;
            serializedOutput.ApplyModifiedPropertiesWithoutUndo();
            composition.Configure(
                _definitions.View(),
                _definitions.Main,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            composition.AttachOutputSession(output);
            composition.AttachCameraSubjectAvailability(availability);
            root.SetActive(true);

            Assert.That(output.DefaultCameraRig, Is.Null);
            Assert.That(composition.Snapshot.IsReady, Is.False);
            Assert.That(composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer));
            Assert.That(composition.Snapshot.LastBlockingIssue, Does.Contain("Default Camera Rig"));
            Assert.That(composition.Snapshot.AssignmentRevision, Is.Zero);
        }
#endif

        [Test]
        public void DestroyedExplicitComposerBlocksWithoutChangingAvailability()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out _);
            CameraSharedComposition composition = Composition(availability, composer);
            Object.DestroyImmediate(composer.gameObject);

            MakeAvailable(availability, Subject("p1"));

            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedInvalidComposer));
            Assert.That(availability.AvailableCount, Is.EqualTo(1));
        }

        [Test]
        public void MissingExplicitViewIsStructurallyBlockedWhileEmptyIsNot()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            var invalidRoot = new GameObject("invalid-composition");
            _created.Add(invalidRoot);
            CameraSharedComposition invalid =
                invalidRoot.AddComponent<CameraSharedComposition>();

            invalid.AttachCameraSubjectAvailability(availability);

            Assert.That(
                invalid.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedInvalidView));
            Assert.That(invalid.Snapshot.IsReady, Is.False);

            CameraSharedComposition valid =
                Composition(availability, Composer(out _));
            Assert.That(
                valid.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(valid.Snapshot.IsReady, Is.True);
        }

        [Test]
        public void PresentationFailureExposesDivergenceAndSameRevisionCanRetry()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraRigComposer composer = Composer(out CinemachineCamera camera);
            CameraSharedComposition composition = Composition(availability, composer);
            CameraSubject p1 = Subject("p1");
            MakeAvailable(availability, p1);
            SetField(composer.BehaviorDefinition, "sharedFollowMemberWeight", 0f);

            CameraSubject p2 = Subject("p2");
            MakeAvailable(availability, p2);

            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedPresentationFailure));
            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(2));
            Assert.That(
                composition.Snapshot.LastPresentationApplyStatus,
                Is.EqualTo(CameraViewPresentationApplyStatus.RejectedInvalidSettings));
            Assert.That(camera.Follow, Is.SameAs(p1.Observation));

            SetField(composer.BehaviorDefinition, "sharedFollowMemberWeight", 1f);
            CameraSharedCompositionSnapshot retried =
                composition.Reconcile(availability.CreateSnapshot());

            Assert.That(
                retried.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededApplied));
            Assert.That(retried.AddedAssignmentCount, Is.Zero);
            AssertMembers(
                composer.FrameworkOwnedSharedFollowTargetGroup,
                p1.Observation,
                p2.Observation);
        }

        private CameraSharedComposition Composition(
            CameraSubjectAvailabilityContext availability,
            CameraRigComposer composer)
        {
            var root = new GameObject("shared-composition");
            _created.Add(root);
            CameraSharedComposition composition =
                root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                _definitions.View(),
                _definitions.Main,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            CameraOutputAuthoring output = Output(composer);
            composition.AttachOutputSession(output);
            composition.AttachCameraSubjectAvailability(availability);
            return composition;
        }

        private CameraOutputAuthoring Output(CameraRigComposer defaultComposer)
        {
            var root = new GameObject("main-output");
            _created.Add(root);
            root.SetActive(false);
            UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            SetField(output, "outputDefinition", _definitions.Main);
            SetField(output, "unityCamera", unityCamera);
            SetField(output, "cinemachineBrain", brain);
            SetField(output, "defaultCameraRig", defaultComposer);
            SetField(output, "initializeOnAwake", false);
            root.SetActive(true);
            Assert.That(output.TryInitialize(out string diagnostic), Is.True, diagnostic);
            return output;
        }

        private CameraRigComposer Composer(out CinemachineCamera camera)
        {
            var root = new GameObject("composer");
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", _definitions.Behavior<FollowCameraRigBehaviorDefinition>());
            var cameraObject = new GameObject("cinemachine-camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<CinemachineCamera>();
            SetField(composer, "cinemachineCamera", camera);
            var groupObject = new GameObject("materialized-shared-follow");
            groupObject.transform.SetParent(composer.transform, false);
            var group = groupObject.AddComponent<CinemachineTargetGroup>();
            var framing = camera.gameObject.AddComponent<CinemachineGroupFraming>();
            framing.enabled = false;
            SetField(composer, "frameworkOwnedSharedFollowTargetGroup", group);
            SetField(composer, "frameworkOwnedSharedFollowGroupFraming", framing);

            return composer;
        }

        private CameraSubject Subject(string id)
        {
            var root = new GameObject(id);
            _created.Add(root);
            return new CameraSubject(new CameraSubjectId(id), root.transform, id);
        }

        private static CameraSubjectAvailabilityContext Availability() =>
            new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-subjects"));

        private static CameraSubjectAvailabilityOwnerId AvailabilityOwner() =>
            new CameraSubjectAvailabilityOwnerId("subject-producer");

        private static CameraSubjectAvailabilityToken MakeAvailable(
            CameraSubjectAvailabilityContext availability,
            CameraSubject subject) =>
            availability.TryMakeAvailable(subject, AvailabilityOwner()).Token;

        private static void AssertApplied(
            CameraSharedComposition composition,
            int subjectCount,
            int added,
            int removed,
            CameraViewPresentationApplyStatus presentationStatus)
        {
            Assert.That(
                composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededApplied));
            Assert.That(composition.Snapshot.SubjectCount, Is.EqualTo(subjectCount));
            Assert.That(composition.Snapshot.AddedAssignmentCount, Is.EqualTo(added));
            Assert.That(composition.Snapshot.RemovedAssignmentCount, Is.EqualTo(removed));
            Assert.That(
                composition.Snapshot.LastPresentationApplyStatus,
                Is.EqualTo(presentationStatus));
        }

        private static void AssertMembers(
            CinemachineTargetGroup group,
            params Transform[] expected)
        {
            Assert.That(group.Targets.Count, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(group.Targets[index].Object, Is.SameAs(expected[index]));
            }
        }

        private static CameraViewAssignmentContext GetAssignmentContext(
            CameraSharedComposition composition) =>
            (CameraViewAssignmentContext)GetField(composition, "_assignments");

        private static CameraViewAssignmentSnapshot GetAssignmentSnapshot(
            CameraSharedComposition composition) =>
            (CameraViewAssignmentSnapshot)GetField(composition, "_currentAssignments");

        private static void SetAssignmentSnapshot(
            CameraSharedComposition composition,
            CameraViewAssignmentSnapshot snapshot) =>
            SetField(composition, "_currentAssignments", snapshot);

        private static object GetField(object target, string name) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
