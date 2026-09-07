using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraViewPresentationInputTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(_created[index]);
            }
            _created.Clear();
        }

        [Test]
        public void EmptyViewProducesExplicitZeroSubjectInput()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraViewAssignmentSnapshot snapshot = ViewSnapshot(
                availability,
                new[] { View("main") });

            CameraViewPresentationInputResult result =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot,
                    ViewId("main"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Input.Cardinality, Is.EqualTo(CameraViewSubjectCardinality.Zero));
            Assert.That(result.Input.SubjectCount, Is.Zero);
        }

        [Test]
        public void SingleSubjectInputPreservesViewIdentitySubjectAndObservation()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentSnapshot snapshot = AssignedSnapshot(
                availability,
                new[] { View("main") },
                ("main", subject.SubjectId));

            CameraViewPresentationInput input =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot,
                    ViewId("main")).Input;

            Assert.That(input.ViewId, Is.EqualTo(ViewId("main")));
            Assert.That(input.Cardinality, Is.EqualTo(CameraViewSubjectCardinality.One));
            Assert.That(input.Subjects[0].Subject.SubjectId, Is.EqualTo(subject.SubjectId));
            Assert.That(input.Subjects[0].Subject.Observation, Is.SameAs(subject.Observation));
        }

        [Test]
        public void TwoSubjectInputPreservesBothInDeterministicOrder()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subjectB = Subject("subject-b");
            CameraSubject subjectA = Subject("subject-a");
            availability.TryMakeAvailable(subjectB, AvailabilityOwner());
            availability.TryMakeAvailable(subjectA, AvailabilityOwner());
            CameraViewAssignmentSnapshot snapshot = AssignedSnapshot(
                availability,
                new[] { View("main") },
                ("main", subjectB.SubjectId),
                ("main", subjectA.SubjectId));

            CameraViewPresentationInput input =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot,
                    ViewId("main")).Input;

            Assert.That(input.Cardinality, Is.EqualTo(CameraViewSubjectCardinality.Many));
            Assert.That(input.SubjectCount, Is.EqualTo(2));
            Assert.That(input.Subjects[0].Subject.SubjectId, Is.EqualTo(subjectA.SubjectId));
            Assert.That(input.Subjects[1].Subject.SubjectId, Is.EqualTo(subjectB.SubjectId));
        }

        [Test]
        public void SameSubjectProducesIndependentViewScopedInputs()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentSnapshot snapshot = AssignedSnapshot(
                availability,
                new[] { View("main"), View("spectator") },
                ("main", subject.SubjectId),
                ("spectator", subject.SubjectId));

            CameraViewPresentationInput main =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot, ViewId("main")).Input;
            CameraViewPresentationInput spectator =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot, ViewId("spectator")).Input;

            Assert.That(main.ViewId, Is.Not.EqualTo(spectator.ViewId));
            Assert.That(main.Subjects[0].Token, Is.EqualTo(spectator.Subjects[0].Token));
        }

        [Test]
        public void LeaveReconciliationProjectsNoStaleObservation()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            CameraSubjectAvailabilityToken token = availability.TryMakeAvailable(
                subject, AvailabilityOwner()).Token;
            CameraViewAssignmentContext assignments = AssignmentContext(
                availability, View("main"));
            assignments.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner(), availability.CreateSnapshot());
            availability.TryMakeUnavailable(token);

            CameraViewAssignmentSnapshot snapshot = assignments.Reconcile(
                availability.CreateSnapshot()).Snapshot;
            CameraViewPresentationInput input =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot, ViewId("main")).Input;

            Assert.That(input.Cardinality, Is.EqualTo(CameraViewSubjectCardinality.Zero));
            Assert.That(input.SubjectCount, Is.Zero);
        }

        [Test]
        public void RejoinDoesNotReplaceOldOccurrenceInPresentationInput()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject occurrenceA = Subject("p1-occurrence-a");
            CameraSubjectAvailabilityToken tokenA = availability.TryMakeAvailable(
                occurrenceA, AvailabilityOwner()).Token;
            CameraViewAssignmentContext assignments = AssignmentContext(
                availability, View("main"));
            CameraViewAssignmentSnapshot assignedA = assignments.TryAssign(
                ViewId("main"), occurrenceA.SubjectId, AssignmentOwner(), availability.CreateSnapshot()).Snapshot;
            CameraViewPresentationInput oldInput =
                CameraViewPresentationInputProjection.TryCreate(
                    assignedA,
                    ViewId("main")).Input;
            availability.TryMakeUnavailable(tokenA);
            CameraSubject occurrenceB = Subject("p1-occurrence-b");
            availability.TryMakeAvailable(occurrenceB, AvailabilityOwner());

            CameraViewAssignmentSnapshot current = assignments.Reconcile(
                availability.CreateSnapshot()).Snapshot;
            CameraViewPresentationInput input =
                CameraViewPresentationInputProjection.TryCreate(
                    current,
                    ViewId("main")).Input;

            Assert.That(input.SubjectCount, Is.Zero);
            Assert.That(oldInput.IsCurrentFor(current), Is.False);
            Assert.That(oldInput.Subjects[0].Subject.SubjectId, Is.EqualTo(occurrenceA.SubjectId));
            Assert.That(oldInput.Subjects[0].Subject.SubjectId, Is.Not.EqualTo(occurrenceB.SubjectId));
        }

        [Test]
        public void WrongViewIdIsRejectedWithoutFallback()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraViewAssignmentSnapshot snapshot = ViewSnapshot(
                availability,
                new[] { View("main") });

            CameraViewPresentationInputResult result =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot,
                    ViewId("missing"));

            Assert.That(result.Status, Is.EqualTo(CameraViewPresentationInputStatus.RejectedViewNotFound));
            Assert.That(result.Input, Is.Null);
        }

        [Test]
        public void EquivalentSnapshotProjectionKeepsDeterministicOrdering()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subjectA = Subject("subject-a");
            CameraSubject subjectB = Subject("subject-b");
            availability.TryMakeAvailable(subjectA, AvailabilityOwner());
            availability.TryMakeAvailable(subjectB, AvailabilityOwner());
            CameraViewAssignmentSnapshot snapshot = AssignedSnapshot(
                availability,
                new[] { View("main") },
                ("main", subjectB.SubjectId),
                ("main", subjectA.SubjectId));

            CameraViewPresentationInput first =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot, ViewId("main")).Input;
            CameraViewPresentationInput second =
                CameraViewPresentationInputProjection.TryCreate(
                    snapshot, ViewId("main")).Input;

            Assert.That(second.Subjects[0].Token, Is.EqualTo(first.Subjects[0].Token));
            Assert.That(second.Subjects[1].Token, Is.EqualTo(first.Subjects[1].Token));
        }

        [Test]
        public void InputReportsStaleAfterAssignmentSnapshotAdvances()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext assignments = AssignmentContext(
                availability, View("main"));
            CameraViewAssignmentSnapshot initial = assignments.Reconcile(
                availability.CreateSnapshot()).Snapshot;
            CameraViewPresentationInput input =
                CameraViewPresentationInputProjection.TryCreate(
                    initial, ViewId("main")).Input;
            CameraViewAssignmentSnapshot advanced = assignments.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner(), availability.CreateSnapshot()).Snapshot;

            Assert.That(input.IsCurrentFor(initial), Is.True);
            Assert.That(input.IsCurrentFor(advanced), Is.False);
        }

        [Test]
        public void FixedWithZeroSubjectsRequiresNoTarget()
        {
            CameraViewPresentationInput input = EmptyInput();

            CameraViewTargetProjectionResult result = CameraViewTargetProjector.Project(
                input,
                CameraRigPresentationIntent.Fixed,
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Required);

            Assert.That(result.Status, Is.EqualTo(CameraViewTargetProjectionStatus.SucceededNoTargets));
            Assert.That(result.Targets.HasAnyTarget, Is.False);
        }

        [Test]
        public void TrackingSingleSubjectUsesSoleObservationForActiveRoles()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewPresentationInput input = CameraViewPresentationInputProjection.TryCreate(
                AssignedSnapshot(
                    availability,
                    new[] { View("main") },
                    ("main", subject.SubjectId)),
                ViewId("main")).Input;

            CameraViewTargetProjectionResult result = CameraViewTargetProjector.Project(
                input,
                CameraRigPresentationIntent.Follow,
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Optional);

            Assert.That(result.Status, Is.EqualTo(CameraViewTargetProjectionStatus.SucceededSingleSubject));
            Assert.That(result.Targets.FollowTarget, Is.SameAs(subject.Observation));
            Assert.That(result.Targets.LookAtTarget, Is.SameAs(subject.Observation));
        }

        [Test]
        public void TrackingWithZeroSubjectsBlocksExplicitly()
        {
            CameraViewTargetProjectionResult result = CameraViewTargetProjector.Project(
                EmptyInput(),
                CameraRigPresentationIntent.Follow,
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Optional);

            Assert.That(result.Status, Is.EqualTo(CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing));
            Assert.That(result.Targets.HasAnyTarget, Is.False);
        }

        [Test]
        public void FollowWithMultipleSubjectsRequiresSharedProjectionWithoutChoosingFirst()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Subject("p1-a");
            CameraSubject p2 = Subject("p2-a");
            availability.TryMakeAvailable(p1, AvailabilityOwner());
            availability.TryMakeAvailable(p2, AvailabilityOwner());
            CameraViewPresentationInput input = CameraViewPresentationInputProjection.TryCreate(
                AssignedSnapshot(
                    availability,
                    new[] { View("main") },
                    ("main", p1.SubjectId),
                    ("main", p2.SubjectId)),
                ViewId("main")).Input;

            CameraViewTargetProjectionResult result = CameraViewTargetProjector.Project(
                input,
                CameraRigPresentationIntent.Follow,
                CameraTargetRequirement.Required,
                CameraTargetRequirement.Optional);

            Assert.That(input.SubjectCount, Is.EqualTo(2));
            Assert.That(result.Status, Is.EqualTo(CameraViewTargetProjectionStatus.SucceededSharedFollow));
            Assert.That(result.Targets.HasAnyTarget, Is.False);
        }

        private CameraViewPresentationInput EmptyInput()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            return CameraViewPresentationInputProjection.TryCreate(
                ViewSnapshot(availability, new[] { View("main") }),
                ViewId("main")).Input;
        }

        private CameraViewAssignmentSnapshot ViewSnapshot(
            CameraSubjectAvailabilityContext availability,
            CameraView[] views)
        {
            return new CameraViewAssignmentContext(
                "view-assignments",
                availability.ContextId,
                views).Reconcile(availability.CreateSnapshot()).Snapshot;
        }

        private CameraViewAssignmentSnapshot AssignedSnapshot(
            CameraSubjectAvailabilityContext availability,
            CameraView[] views,
            params (string viewId, CameraSubjectId subjectId)[] relations)
        {
            CameraViewAssignmentContext context = new CameraViewAssignmentContext(
                "view-assignments",
                availability.ContextId,
                views);
            CameraViewAssignmentResult result = context.Reconcile(
                availability.CreateSnapshot());
            for (int index = 0; index < relations.Length; index++)
            {
                result = context.TryAssign(
                    ViewId(relations[index].viewId),
                    relations[index].subjectId,
                    AssignmentOwner(),
                    availability.CreateSnapshot());
            }
            return result.Snapshot;
        }

        private CameraViewAssignmentContext AssignmentContext(
            CameraSubjectAvailabilityContext availability,
            params CameraView[] views)
        {
            return new CameraViewAssignmentContext(
                "view-assignments",
                availability.ContextId,
                views);
        }

        private CameraSubjectAvailabilityContext Availability()
        {
            return new CameraSubjectAvailabilityContext("subjects-session-a");
        }

        private CameraSubject Subject(string id)
        {
            var gameObject = new GameObject(id);
            _created.Add(gameObject);
            return new CameraSubject(new CameraSubjectId(id), gameObject.transform, id);
        }

        private static CameraView View(string id)
        {
            return new CameraView(ViewId(id), id);
        }

        private static CameraViewId ViewId(string id)
        {
            return new CameraViewId(id);
        }

        private static CameraSubjectAvailabilityOwnerId AvailabilityOwner()
        {
            return new CameraSubjectAvailabilityOwnerId("availability-owner");
        }

        private static CameraSubjectAssignmentOwnerId AssignmentOwner()
        {
            return new CameraSubjectAssignmentOwnerId("assignment-owner");
        }
    }
}
