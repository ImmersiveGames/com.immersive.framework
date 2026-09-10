using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraSharedFollowPresentationTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null)
                {
                    Object.DestroyImmediate(_created[index]);
                }
            }
            _created.Clear();
        }

        [Test]
        public void FollowManyUsesMaterializedGroupAndFramingIdempotently()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Subject("p1");
            CameraSubject p2 = Subject("p2");
            availability.TryMakeAvailable(p1, AvailabilityOwner("p1"));
            availability.TryMakeAvailable(p2, AvailabilityOwner("p2"));
            CameraViewAssignmentContext assignments = Assignments(availability);
            Assign(assignments, availability, p1);
            CameraViewAssignmentSnapshot snapshot = Assign(assignments, availability, p2).Snapshot;
            CameraViewPresentationInput input = Input(snapshot);
            CameraRigComposer composer = Composer(out CinemachineCamera camera);

            CameraViewPresentationApplyResult first =
                composer.ApplyViewPresentation(input, snapshot);
            CameraViewPresentationApplyResult second =
                composer.ApplyViewPresentation(input, snapshot);

            Assert.That(first.Status, Is.EqualTo(CameraViewPresentationApplyStatus.SucceededSharedFollow));
            Assert.That(second.Status, Is.EqualTo(CameraViewPresentationApplyStatus.SucceededSharedFollow));
            Assert.That(composer.GetComponentsInChildren<CinemachineCamera>(true).Length, Is.EqualTo(1));
            Assert.That(composer.GetComponentsInChildren<CinemachineTargetGroup>(true).Length, Is.EqualTo(1));
            Assert.That(camera.GetComponents<CinemachineGroupFraming>().Length, Is.EqualTo(1));
            Assert.That(second.TargetGroup, Is.SameAs(first.TargetGroup));
            Assert.That(second.GroupFraming, Is.SameAs(first.GroupFraming));
            Assert.That(second.MemberCount, Is.EqualTo(2));
            Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup, Is.SameAs(first.TargetGroup));
            Assert.That(composer.FrameworkOwnedSharedFollowGroupFraming, Is.SameAs(first.GroupFraming));
            Assert.That(camera.Follow, Is.SameAs(first.TargetGroup.transform));
            Assert.That(first.GroupFraming.FramingSize, Is.EqualTo(composer.SharedFollowFramingSize));
            Assert.That(first.GroupFraming.FovRange, Is.EqualTo(composer.SharedFollowFovRange));
            Assert.That(first.GroupFraming.DollyRange, Is.EqualTo(composer.SharedFollowDollyRange));
            Assert.That(first.GroupFraming.OrthoSizeRange, Is.EqualTo(composer.SharedFollowOrthoSizeRange));
            Assert.That(first.TargetGroup.Targets[0].Weight, Is.GreaterThan(0f));
            Assert.That(first.TargetGroup.Targets[0].Radius, Is.GreaterThan(0f));
        }

        [Test]
        public void CardinalityTransitionsReuseRigAndReconcileExactMembership()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Available(availability, "p1");
            CameraSubject p2 = Available(availability, "p2");
            CameraSubject p3 = Available(availability, "p3");
            CameraViewAssignmentContext assignments = Assignments(availability);
            CameraRigComposer composer = Composer(out CinemachineCamera camera);
            CameraViewAssignmentSnapshot zero = assignments.Reconcile(availability.CreateSnapshot()).Snapshot;

            AssertStatus(composer, zero, CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing, 0);
            CameraViewAssignmentResult p1Assigned = Assign(assignments, availability, p1);
            AssertStatus(composer, p1Assigned.Snapshot, CameraViewPresentationApplyStatus.SucceededSingleSubject, 0);
            CameraViewAssignmentResult p2Assigned = Assign(assignments, availability, p2);
            AssertStatus(composer, p2Assigned.Snapshot, CameraViewPresentationApplyStatus.SucceededSharedFollow, 2);
            CameraViewAssignmentResult p3Assigned = Assign(assignments, availability, p3);
            AssertStatus(composer, p3Assigned.Snapshot, CameraViewPresentationApplyStatus.SucceededSharedFollow, 3);

            CameraViewAssignmentSnapshot two = assignments.TryRelease(
                p2Assigned.Token, availability.CreateSnapshot()).Snapshot;
            AssertStatus(composer, two, CameraViewPresentationApplyStatus.SucceededSharedFollow, 2);
            AssertMembers(composer.FrameworkOwnedSharedFollowTargetGroup, p1.Observation, p3.Observation);

            CameraViewAssignmentSnapshot one = assignments.TryRelease(
                p3Assigned.Token, availability.CreateSnapshot()).Snapshot;
            AssertStatus(composer, one, CameraViewPresentationApplyStatus.SucceededSingleSubject, 0);
            Assert.That(camera.Follow, Is.SameAs(p1.Observation));

            CameraViewAssignmentSnapshot empty = assignments.TryRelease(
                p1Assigned.Token, availability.CreateSnapshot()).Snapshot;
            AssertStatus(composer, empty, CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing, 0);
            Assert.That(camera.Follow, Is.Null);
            Assert.That(camera, Is.SameAs(composer.CinemachineCamera));
            Assert.That(composer.GetComponentsInChildren<CinemachineCamera>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void LeaveRejoinAndOlderInputCannotRestoreStaleOccurrence()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1A = Subject("p1-a");
            CameraSubject p2A = Subject("p2-a");
            CameraSubjectAvailabilityToken p1AToken = availability.TryMakeAvailable(
                p1A, AvailabilityOwner("p1-a")).Token;
            availability.TryMakeAvailable(p2A, AvailabilityOwner("p2-a"));
            CameraViewAssignmentContext assignments = Assignments(availability);
            Assign(assignments, availability, p1A);
            CameraViewAssignmentSnapshot oldSnapshot = Assign(assignments, availability, p2A).Snapshot;
            CameraViewPresentationInput oldInput = Input(oldSnapshot);
            CameraRigComposer composer = Composer(out _);
            composer.ApplyViewPresentation(oldInput, oldSnapshot);

            availability.TryMakeUnavailable(p1AToken);
            CameraViewAssignmentSnapshot afterLeave = assignments.Reconcile(
                availability.CreateSnapshot()).Snapshot;
            AssertStatus(composer, afterLeave, CameraViewPresentationApplyStatus.SucceededSingleSubject, 0);

            CameraSubject p1B = Available(availability, "p1-b");
            CameraViewAssignmentSnapshot rejoined = Assign(assignments, availability, p1B).Snapshot;
            AssertStatus(composer, rejoined, CameraViewPresentationApplyStatus.SucceededSharedFollow, 2);
            AssertMembers(composer.FrameworkOwnedSharedFollowTargetGroup, p1B.Observation, p2A.Observation);
            Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup.FindMember(p1A.Observation), Is.EqualTo(-1));

            CameraViewPresentationApplyResult stale =
                composer.ApplyViewPresentation(oldInput, oldSnapshot);
            Assert.That(stale.Status, Is.EqualTo(CameraViewPresentationApplyStatus.RejectedStaleInput));
            AssertMembers(composer.FrameworkOwnedSharedFollowTargetGroup, p1B.Observation, p2A.Observation);
        }

        [Test]
        public void MultipleSubjectsAreSupportedOnlyForFollowAndFixedNeedsNoGroup()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Available(availability, "p1");
            CameraSubject p2 = Available(availability, "p2");
            CameraViewAssignmentContext assignments = Assignments(availability);
            Assign(assignments, availability, p1);
            CameraViewAssignmentSnapshot many = Assign(assignments, availability, p2).Snapshot;

            CameraRigComposer mounted = Composer(out _, false);
            SetField(mounted, "presentationIntent", CameraRigPresentationIntent.Mounted);
            Assert.That(
                mounted.ApplyViewPresentation(Input(many), many).Status,
                Is.EqualTo(CameraViewPresentationApplyStatus.BlockedUnsupportedPresentation));

            CameraRigComposer thirdPerson = Composer(out _, false);
            SetField(thirdPerson, "presentationIntent", CameraRigPresentationIntent.ThirdPerson);
            Assert.That(
                thirdPerson.ApplyViewPresentation(Input(many), many).Status,
                Is.EqualTo(CameraViewPresentationApplyStatus.BlockedUnsupportedPresentation));

            CameraViewAssignmentContext fixedAssignments = Assignments(availability, "fixed");
            CameraViewAssignmentSnapshot empty = fixedAssignments.Reconcile(
                availability.CreateSnapshot()).Snapshot;
            CameraRigComposer fixedComposer = Composer(out _, false);
            SetField(fixedComposer, "presentationIntent", CameraRigPresentationIntent.Fixed);
            Assert.That(
                fixedComposer.ApplyViewPresentation(Input(empty, "fixed"), empty).Status,
                Is.EqualTo(CameraViewPresentationApplyStatus.SucceededFixedNoTargets));
            Assert.That(fixedComposer.FrameworkOwnedSharedFollowTargetGroup, Is.Null);
            Assert.That(fixedComposer.FrameworkOwnedSharedFollowGroupFraming, Is.Null);
        }

        [Test]
        public void InvalidSettingsAndAuthorOwnedFramingBlockWithoutClaimingContent()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Available(availability, "p1");
            CameraSubject p2 = Available(availability, "p2");
            CameraViewAssignmentContext assignments = Assignments(availability);
            Assign(assignments, availability, p1);
            CameraViewAssignmentSnapshot many = Assign(assignments, availability, p2).Snapshot;

            CameraRigComposer invalid = Composer(out _, false);
            SetField(invalid, "sharedFollowMemberWeight", 0f);
            Assert.That(
                invalid.ApplyViewPresentation(Input(many), many).Status,
                Is.EqualTo(CameraViewPresentationApplyStatus.RejectedInvalidSettings));
            Assert.That(invalid.FrameworkOwnedSharedFollowTargetGroup, Is.Null);

            CameraRigComposer conflicted = Composer(out CinemachineCamera camera, false);
            var authoredGroupObject = new GameObject("author-group");
            _created.Add(authoredGroupObject);
            authoredGroupObject.transform.SetParent(conflicted.transform, false);
            CinemachineTargetGroup authoredGroup =
                authoredGroupObject.AddComponent<CinemachineTargetGroup>();
            CinemachineGroupFraming authored = camera.gameObject.AddComponent<CinemachineGroupFraming>();
            CameraViewPresentationApplyResult result =
                conflicted.ApplyViewPresentation(Input(many), many);

            Assert.That(result.Status, Is.EqualTo(CameraViewPresentationApplyStatus.RejectedOwnershipConflict));
            Assert.That(camera.GetComponents<CinemachineGroupFraming>().Length, Is.EqualTo(1));
            Assert.That(camera.GetComponent<CinemachineGroupFraming>(), Is.SameAs(authored));
            Assert.That(conflicted.GetComponentsInChildren<CinemachineTargetGroup>(true).Length, Is.EqualTo(1));
            Assert.That(conflicted.GetComponentInChildren<CinemachineTargetGroup>(true), Is.SameAs(authoredGroup));
            Assert.That(conflicted.FrameworkOwnedSharedFollowGroupFraming, Is.Null);
            Assert.That(conflicted.FrameworkOwnedSharedFollowTargetGroup, Is.Null);
        }

        [Test]
        public void EverySharedFollowRangeRejectsInvalidAuthoring()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Available(availability, "p1");
            CameraSubject p2 = Available(availability, "p2");
            CameraViewAssignmentContext assignments = Assignments(availability);
            Assign(assignments, availability, p1);
            CameraViewAssignmentSnapshot many = Assign(assignments, availability, p2).Snapshot;
            CameraViewPresentationInput input = Input(many);
            CameraRigComposer composer = Composer(out _);

            AssertInvalidSetting(composer, input, many, "sharedFollowMemberWeight", float.NaN, 1f);
            AssertInvalidSetting(composer, input, many, "sharedFollowMemberRadius", 0f, 0.5f);
            AssertInvalidSetting(composer, input, many, "sharedFollowFramingSize", 0f, 0.8f);
            AssertInvalidSetting(composer, input, many, "sharedFollowDamping", float.PositiveInfinity, 2f);
            AssertInvalidSetting(composer, input, many, "sharedFollowFovRange", new Vector2(100f, 1f), new Vector2(1f, 100f));
            AssertInvalidSetting(composer, input, many, "sharedFollowDollyRange", new Vector2(1f, -1f), new Vector2(-100f, 100f));
            AssertInvalidSetting(composer, input, many, "sharedFollowOrthoSizeRange", new Vector2(0f, 100f), new Vector2(1f, 1000f));
        }

        [Test]
        public void MissingSharedStructureIsRejectedWithoutRuntimeCreation()
        {
            var availability = Availability();
            var assignments = Assignments(availability);
            Assign(assignments, availability, Available(availability, "p1"));
            var snapshot = Assign(assignments, availability, Available(availability, "p2")).Snapshot;
            var composer = Composer(out CinemachineCamera camera, false);
            var result = composer.ApplyViewPresentation(Input(snapshot), snapshot);
            Assert.That(result.Status, Is.EqualTo(CameraViewPresentationApplyStatus.RejectedOwnershipConflict));
            Assert.That(composer.GetComponentsInChildren<CinemachineTargetGroup>(true), Is.Empty);
            Assert.That(camera.GetComponents<CinemachineGroupFraming>(), Is.Empty);
            Assert.That(camera.Follow, Is.Null);
            Assert.That(camera.LookAt, Is.Null);
        }

        private void AssertStatus(
            CameraRigComposer composer,
            CameraViewAssignmentSnapshot snapshot,
            CameraViewPresentationApplyStatus status,
            int memberCount)
        {
            CameraViewPresentationApplyResult result =
                composer.ApplyViewPresentation(Input(snapshot), snapshot);
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.MemberCount, Is.EqualTo(memberCount));
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

        private CameraRigComposer Composer(out CinemachineCamera camera, bool materializeShared = true)
        {
            var root = new GameObject("composer");
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            var cameraObject = new GameObject("cinemachine-camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<CinemachineCamera>();
            SetField(composer, "cinemachineCamera", camera);
            if (materializeShared)
            {
                var groupObject = new GameObject("materialized-shared-follow");
                groupObject.transform.SetParent(composer.transform, false);
                var group = groupObject.AddComponent<CinemachineTargetGroup>();
                var framing = camera.gameObject.AddComponent<CinemachineGroupFraming>();
                framing.enabled = false;
                SetField(composer, "frameworkOwnedSharedFollowTargetGroup", group);
                SetField(composer, "frameworkOwnedSharedFollowGroupFraming", framing);
            }

            return composer;
        }

        private CameraSubject Available(
            CameraSubjectAvailabilityContext availability,
            string id)
        {
            CameraSubject subject = Subject(id);
            availability.TryMakeAvailable(subject, AvailabilityOwner(id));
            return subject;
        }

        private CameraSubject Subject(string id)
        {
            var value = new GameObject(id);
            _created.Add(value);
            return new CameraSubject(new CameraSubjectId(id), value.transform, id);
        }

        private static CameraViewAssignmentContext Assignments(
            CameraSubjectAvailabilityContext availability,
            string viewId = "main")
        {
            return new CameraViewAssignmentContext(
                new ViewAssignmentContextId("assignments-" + viewId),
                availability.ContextId,
                new CameraView(new CameraViewId(viewId), viewId));
        }

        private static CameraViewAssignmentResult Assign(
            CameraViewAssignmentContext assignments,
            CameraSubjectAvailabilityContext availability,
            CameraSubject subject)
        {
            CameraViewId viewId = assignments.Reconcile(
                availability.CreateSnapshot()).Snapshot.Views[0].View.ViewId;
            return assignments.TryAssign(
                viewId,
                subject.SubjectId,
                new CameraSubjectAssignmentOwnerId("assignment-" + subject.SubjectId.Value),
                availability.CreateSnapshot());
        }

        private static CameraViewPresentationInput Input(
            CameraViewAssignmentSnapshot snapshot,
            string viewId = "main")
        {
            return CameraViewPresentationInputProjection.TryCreate(
                snapshot,
                new CameraViewId(viewId)).Input;
        }

        private static CameraSubjectAvailabilityContext Availability()
        {
            return new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("subjects"));
        }

        private static CameraSubjectAvailabilityOwnerId AvailabilityOwner(string id)
        {
            return new CameraSubjectAvailabilityOwnerId("availability-" + id);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void AssertInvalidSetting<T>(
            CameraRigComposer composer,
            CameraViewPresentationInput input,
            CameraViewAssignmentSnapshot snapshot,
            string field,
            T invalid,
            T valid)
        {
            SetField(composer, field, invalid);
            Assert.That(
                composer.ApplyViewPresentation(input, snapshot).Status,
                Is.EqualTo(CameraViewPresentationApplyStatus.RejectedInvalidSettings),
                field);
            SetField(composer, field, valid);
        }
    }
}
