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

                CameraViewPresentationApplyResult applied = composer.ApplyCompositionPresentation(
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

                CameraViewPresentationApplyResult applied = composer.ApplyCompositionPresentation(
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

        [Test]
        public void OlderCompositionPresentationCannotOverwriteNewerAppliedMembership()
        {
            using var fixture = new PresentationFixture("subject-a");
            CameraViewPresentationInput olderInput = fixture.Input;
            CameraCompositionMembershipSnapshot olderSnapshot = fixture.Snapshot;
            CameraCompositionMembershipSnapshot newerSnapshot = fixture.AddSubject("subject-b");
            CameraViewPresentationInput newerInput =
                CameraViewPresentationInputProjection.TryCreate(newerSnapshot).Input;
            var root = new GameObject("stale-composition-presentation-test");
            var behavior = ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>();
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                    composer, false, false).Succeeded, Is.True);
                Assert.That(composer.ApplyCompositionPresentation(newerInput, newerSnapshot).Succeeded, Is.True);
                Assert.That(composer.ApplyCompositionPresentation(olderInput, olderSnapshot).Status,
                    Is.EqualTo(CameraViewPresentationApplyStatus.RejectedStaleInput));
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
            private readonly CameraSubjectAvailabilityContext _availability;
            private readonly CameraCompositionMembershipContext _membership;
            private readonly CameraSubjectAvailabilityOwnerId _availabilityOwner;
            private readonly Dictionary<string, Transform> _observations =
                new Dictionary<string, Transform>();

            internal PresentationFixture(params string[] subjectIds)
            {
                _availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId("group-presentation-subjects"));
                _availabilityOwner = new CameraSubjectAvailabilityOwnerId(
                    "group-presentation-owner");
                for (int index = 0; index < subjectIds.Length; index++)
                {
                    string id = subjectIds[index];
                    var root = new GameObject(id);
                    _roots.Add(root);
                    _observations.Add(id, root.transform);
                    CameraSubjectAvailabilityResult available = _availability.TryMakeAvailable(
                        new CameraSubject(new CameraSubjectId(id), root.transform, id),
                        _availabilityOwner);
                    Assert.That(available.Succeeded, Is.True, available.Message);
                }

                _membership = new CameraCompositionMembershipContext(
                    new CameraCompositionMembershipContextId("group-presentation-membership"),
                    _availability.ContextId);
                CameraSubjectAvailabilitySnapshot availabilitySnapshot =
                    _availability.CreateSnapshot();
                var desired = new List<CameraSubjectId>();
                for (int index = 0; index < subjectIds.Length; index++)
                    desired.Add(new CameraSubjectId(subjectIds[index]));

                CameraCompositionMembershipResult membershipResult =
                    _membership.Reconcile(availabilitySnapshot, desired);
                Assert.That(membershipResult.Succeeded, Is.True, membershipResult.Message);
                Snapshot = membershipResult.Snapshot;
                CameraViewPresentationInputResult projection =
                    CameraViewPresentationInputProjection.TryCreate(Snapshot);
                Assert.That(projection.Succeeded, Is.True, projection.Message);
                Input = projection.Input;
            }

            internal IReadOnlyDictionary<string, Transform> Observations => _observations;
            internal CameraCompositionMembershipSnapshot Snapshot { get; }
            internal CameraViewPresentationInput Input { get; }

            internal CameraCompositionMembershipSnapshot AddSubject(string id)
            {
                var root = new GameObject(id);
                _roots.Add(root);
                _observations.Add(id, root.transform);
                CameraSubjectAvailabilityResult available = _availability.TryMakeAvailable(
                    new CameraSubject(new CameraSubjectId(id), root.transform, id),
                    _availabilityOwner);
                Assert.That(available.Succeeded, Is.True, available.Message);
                var desired = new List<CameraSubjectId>();
                for (int index = 0; index < Snapshot.Count; index++)
                    desired.Add(Snapshot.Entries[index].SubjectId);
                desired.Add(new CameraSubjectId(id));
                CameraCompositionMembershipResult result =
                    _membership.Reconcile(_availability.CreateSnapshot(), desired);
                Assert.That(result.Succeeded, Is.True, result.Message);
                return result.Snapshot;
            }

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
