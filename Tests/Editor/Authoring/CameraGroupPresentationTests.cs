using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraGroupPresentationTests
    {
        [Test]
        public void FollowAcceptsOneSubject()
        {
            using var fixture = new PresentationFixture("subject-a");

            CameraViewTargetProjectionResult projection = Project(
                fixture.Input,
                CameraRigPresentationIntent.Follow);

            Assert.That(projection.Status, Is.EqualTo(
                CameraViewTargetProjectionStatus.SucceededSingleSubject));
            Assert.That(projection.Targets.FollowTarget, Is.SameAs(
                fixture.Observations["subject-a"]));
        }

        [Test]
        public void FollowWithManySubjectsDoesNotBecomeGroup()
        {
            using var fixture = new PresentationFixture("subject-b", "subject-a");

            CameraViewTargetProjectionResult projection = Project(
                fixture.Input,
                CameraRigPresentationIntent.Follow);

            Assert.That(projection.Status, Is.EqualTo(
                CameraViewTargetProjectionStatus.BlockedMultipleSubjectsUnsupported));
            Assert.That(projection.Succeeded, Is.False);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void GroupAcceptsOneOrManySubjects(int subjectCount)
        {
            string[] ids = subjectCount == 1
                ? new[] { "subject-a" }
                : new[] { "subject-c", "subject-a", "subject-b" };
            using var fixture = new PresentationFixture(ids);

            CameraViewTargetProjectionResult projection = Project(
                fixture.Input,
                CameraRigPresentationIntent.Group);

            Assert.That(projection.Status, Is.EqualTo(
                CameraViewTargetProjectionStatus.SucceededGroup));
            Assert.That(projection.Input.SubjectCount, Is.EqualTo(subjectCount));
        }

        [Test]
        public void GroupRejectsZeroSubjects()
        {
            using var fixture = new PresentationFixture();

            CameraViewTargetProjectionResult projection = Project(
                fixture.Input,
                CameraRigPresentationIntent.Group);

            Assert.That(projection.Status, Is.EqualTo(
                CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing));
        }

        [Test]
        public void GroupWithOneSubjectStillAppliesThroughTargetGroup()
        {
            using var fixture = new PresentationFixture("subject-a");
            var root = new GameObject("single-subject-group-test");
            var behavior = ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>();
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                CameraRigComposerApplyRebuildResult materialization =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer,
                        false,
                        false);
                Assert.That(materialization.Succeeded, Is.True, materialization.BlockingIssue);

                CameraViewPresentationApplyResult applied = composer.ApplyViewPresentation(
                    fixture.Input,
                    fixture.Snapshot);

                Assert.That(applied.Status, Is.EqualTo(
                    CameraViewPresentationApplyStatus.SucceededGroup));
                Assert.That(applied.MemberCount, Is.EqualTo(1));
                Assert.That(composer.CinemachineCamera.Follow, Is.SameAs(
                    applied.TargetGroup.transform));
                Assert.That(composer.CinemachineCamera.Follow, Is.Not.SameAs(
                    fixture.Observations["subject-a"]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void GroupMaterializesAndAppliesDeterministicMembershipAndSettings()
        {
            using var fixture = new PresentationFixture("subject-c", "subject-a", "subject-b");
            var root = new GameObject("group-presentation-test");
            var behavior = ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>();
            try
            {
                var followOffset = new Vector3(1f, 7f, -12f);
                SetField(behavior, "followOffset", followOffset);
                SetField(behavior, "memberWeight", 2f);
                SetField(behavior, "memberRadius", 1.25f);
                SetField(behavior, "framingSize", 0.65f);
                SetField(behavior, "damping", 3.5f);
                SetField(behavior, "fovRange", new Vector2(20f, 80f));
                SetField(behavior, "dollyRange", new Vector2(-10f, 25f));
                SetField(behavior, "orthoSizeRange", new Vector2(2f, 50f));

                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                CameraRigComposerApplyRebuildResult materialization =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer,
                        false,
                        false);

                Assert.That(materialization.Succeeded, Is.True, materialization.BlockingIssue);
                Assert.That(composer.FrameworkOwnedGroupTargetGroup, Is.Not.Null);
                Assert.That(composer.FrameworkOwnedGroupFraming, Is.Not.Null);
                Assert.That(
                    ((CinemachineFollow)composer.FrameworkOwnedPositionControl).FollowOffset,
                    Is.EqualTo(followOffset));

                CameraViewPresentationApplyResult applied = composer.ApplyViewPresentation(
                    fixture.Input,
                    fixture.Snapshot);

                Assert.That(applied.Status, Is.EqualTo(
                    CameraViewPresentationApplyStatus.SucceededGroup));
                Assert.That(applied.MemberCount, Is.EqualTo(3));
                Assert.That(applied.TargetGroup.Targets[0].Object, Is.SameAs(
                    fixture.Observations["subject-a"]));
                Assert.That(applied.TargetGroup.Targets[1].Object, Is.SameAs(
                    fixture.Observations["subject-b"]));
                Assert.That(applied.TargetGroup.Targets[2].Object, Is.SameAs(
                    fixture.Observations["subject-c"]));
                for (int index = 0; index < applied.TargetGroup.Targets.Count; index++)
                {
                    Assert.That(applied.TargetGroup.Targets[index].Weight, Is.EqualTo(2f));
                    Assert.That(applied.TargetGroup.Targets[index].Radius, Is.EqualTo(1.25f));
                }
                Assert.That(applied.GroupFraming.enabled, Is.True);
                Assert.That(applied.GroupFraming.FramingSize, Is.EqualTo(0.65f));
                Assert.That(applied.GroupFraming.Damping, Is.EqualTo(3.5f));
                Assert.That(applied.GroupFraming.FovRange, Is.EqualTo(new Vector2(20f, 80f)));
                Assert.That(applied.GroupFraming.DollyRange, Is.EqualTo(new Vector2(-10f, 25f)));
                Assert.That(applied.GroupFraming.OrthoSizeRange, Is.EqualTo(new Vector2(2f, 50f)));
                Assert.That(composer.CinemachineCamera.Follow, Is.SameAs(
                    applied.TargetGroup.transform));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(behavior);
            }
        }

        private static CameraViewTargetProjectionResult Project(
            CameraViewPresentationInput input,
            CameraRigPresentationIntent intent)
        {
            return CameraViewTargetProjector.Project(
                input,
                intent,
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Optional);
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class PresentationFixture : IDisposable
        {
            private readonly List<GameObject> _roots = new List<GameObject>();

            internal PresentationFixture(params string[] subjectIds)
            {
                var availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId("group-presentation-subjects"));
                var availabilityOwner = new CameraSubjectAvailabilityOwnerId(
                    "group-presentation-owner");
                Observations = new Dictionary<string, Transform>();
                for (int index = 0; index < subjectIds.Length; index++)
                {
                    string id = subjectIds[index];
                    var root = new GameObject(id);
                    _roots.Add(root);
                    Observations.Add(id, root.transform);
                    CameraSubjectAvailabilityResult available = availability.TryMakeAvailable(
                        new CameraSubject(new CameraSubjectId(id), root.transform, id),
                        availabilityOwner);
                    Assert.That(available.Succeeded, Is.True, available.Message);
                }

                var view = new CameraView(
                    new CameraViewId("group-presentation-view"),
                    "Group presentation test view");
                var assignments = new CameraViewAssignmentContext(
                    new ViewAssignmentContextId("group-presentation-assignments"),
                    availability.ContextId,
                    view);
                var assignmentOwner = new CameraSubjectAssignmentOwnerId(
                    "group-presentation-assignment-owner");
                CameraSubjectAvailabilitySnapshot availabilitySnapshot =
                    availability.CreateSnapshot();
                CameraViewAssignmentResult assignmentResult =
                    assignments.Reconcile(availabilitySnapshot);
                for (int index = 0; index < subjectIds.Length; index++)
                {
                    assignmentResult = assignments.TryAssign(
                        view.ViewId,
                        new CameraSubjectId(subjectIds[index]),
                        assignmentOwner,
                        availabilitySnapshot);
                    Assert.That(assignmentResult.Succeeded, Is.True, assignmentResult.Message);
                }

                Snapshot = assignmentResult.Snapshot;
                CameraViewPresentationInputResult projection =
                    CameraViewPresentationInputProjection.TryCreate(
                        Snapshot,
                        view.ViewId);
                Assert.That(projection.Succeeded, Is.True, projection.Message);
                Input = projection.Input;
            }

            internal IReadOnlyDictionary<string, Transform> Observations { get; }
            internal CameraViewAssignmentSnapshot Snapshot { get; }
            internal CameraViewPresentationInput Input { get; }

            public void Dispose()
            {
                for (int index = 0; index < _roots.Count; index++)
                {
                    UnityEngine.Object.DestroyImmediate(_roots[index]);
                }
            }
        }
    }
}
