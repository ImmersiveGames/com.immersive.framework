using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraViewAssignmentContextTests
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
        public void AssignmentIdentityUsesOrdinalValueEquality()
        {
            var id = new ViewAssignmentContextId(" assignments ");
            var same = new ViewAssignmentContextId("assignments");
            Assert.That(id == same, Is.True);
            Assert.That(id.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(id != new ViewAssignmentContextId("Assignments"), Is.True);
            Assert.That(id.ToString(), Is.EqualTo("assignments"));
            Assert.That(id.Equals((object)new SubjectAvailabilityContextId("assignments")), Is.False);
            var values = new Dictionary<ViewAssignmentContextId, int> { [id] = 1 };
            Assert.That(values[same], Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        public void InvalidAssignmentOrAvailabilityContextIsRejected(string value)
        {
            var id = new ViewAssignmentContextId(value);
            Assert.That(id.IsValid, Is.False);
            Assert.That(id, Is.EqualTo(default(ViewAssignmentContextId)));
            Assert.That(id.GetHashCode(), Is.EqualTo(default(ViewAssignmentContextId).GetHashCode()));
            Assert.That(id.ToString(), Is.Empty);
            var availability = new SubjectAvailabilityContextId("subjects");
            Assert.Throws<System.ArgumentException>(() => new CameraViewAssignmentContext(id, availability));
            Assert.Throws<System.ArgumentException>(() => new CameraViewAssignmentContext(default, availability));
            Assert.Throws<System.ArgumentException>(() => new CameraViewAssignmentContext(new ViewAssignmentContextId("views"), default));
        }

        [Test]
        public void ViewCanExistWithZeroSubjects()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraViewAssignmentContext context = Views(
                availability,
                View("main"));

            CameraViewAssignmentSnapshot snapshot = context.Reconcile(
                availability.CreateSnapshot()).Snapshot;

            Assert.That(snapshot.ViewCount, Is.EqualTo(1));
            Assert.That(snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(main.AssignmentCount, Is.Zero);
            Assert.That(main.ResolvedSubjectCount, Is.Zero);
        }

        [Test]
        public void OneAvailableSubjectCanBeAssignedToOneView()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));

            CameraViewAssignmentResult result = context.TryAssign(
                ViewId("main"),
                subject.SubjectId,
                AssignmentOwner("gameplay"),
                availability.CreateSnapshot());

            Assert.That(result.Status, Is.EqualTo(CameraViewAssignmentStatus.SucceededAssigned));
            Assert.That(result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(main.ResolvedSubjectCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoAvailableSubjectsCanCoexistInOneView()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Subject("p1-a");
            CameraSubject p2 = Subject("p2-a");
            availability.TryMakeAvailable(p1, AvailabilityOwner());
            availability.TryMakeAvailable(p2, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            context.TryAssign(ViewId("main"), p1.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            CameraViewAssignmentResult result = context.TryAssign(
                ViewId("main"), p2.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());

            Assert.That(result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(main.AssignmentCount, Is.EqualTo(2));
            Assert.That(main.ResolvedSubjectCount, Is.EqualTo(2));
        }

        [Test]
        public void OneSubjectCanBeAssignedToTwoViews()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(
                availability,
                View("main"),
                View("spectator"));
            context.TryAssign(ViewId("main"), subject.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            CameraViewAssignmentResult result = context.TryAssign(
                ViewId("spectator"), subject.SubjectId, AssignmentOwner("spectator"), availability.CreateSnapshot());

            Assert.That(result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(result.Snapshot.TryGetView(ViewId("spectator"), out CameraViewSubjectSnapshot spectator), Is.True);
            Assert.That(main.ResolvedSubjectCount, Is.EqualTo(1));
            Assert.That(spectator.ResolvedSubjectCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateAssignmentIsIdempotent()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            CameraViewAssignmentResult first = context.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            CameraViewAssignmentResult duplicate = context.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());

            Assert.That(duplicate.Status, Is.EqualTo(CameraViewAssignmentStatus.SucceededAlreadyAssigned));
            Assert.That(duplicate.Token, Is.EqualTo(first.Token));
            Assert.That(duplicate.Snapshot.Revision, Is.EqualTo(first.Snapshot.Revision));
        }

        [Test]
        public void ExactReleaseRemovesOnlyIntendedRelation()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Subject("p1-a");
            CameraSubject p2 = Subject("p2-a");
            availability.TryMakeAvailable(p1, AvailabilityOwner());
            availability.TryMakeAvailable(p2, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            CameraSubjectAssignmentToken p1Token = context.TryAssign(
                ViewId("main"), p1.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot()).Token;
            context.TryAssign(ViewId("main"), p2.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());

            CameraViewAssignmentResult result = context.TryRelease(
                p1Token,
                availability.CreateSnapshot());

            Assert.That(result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(main.AssignmentCount, Is.EqualTo(1));
            Assert.That(main.Assignments[0].SubjectId, Is.EqualTo(p2.SubjectId));
        }

        [Test]
        public void ReleasingOneViewRelationDoesNotAffectAnotherView()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"), View("spectator"));
            CameraSubjectAssignmentToken mainToken = context.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner("main-owner"), availability.CreateSnapshot()).Token;
            context.TryAssign(ViewId("spectator"), subject.SubjectId, AssignmentOwner("spectator-owner"), availability.CreateSnapshot());

            CameraViewAssignmentSnapshot snapshot = context.TryRelease(
                mainToken,
                availability.CreateSnapshot()).Snapshot;

            snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main);
            snapshot.TryGetView(ViewId("spectator"), out CameraViewSubjectSnapshot spectator);
            Assert.That(main.AssignmentCount, Is.Zero);
            Assert.That(spectator.AssignmentCount, Is.EqualTo(1));
        }

        [Test]
        public void UnavailableSubjectIsRemovedDuringReconciliation()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            CameraSubjectAvailabilityToken availabilityToken = availability.TryMakeAvailable(
                subject, AvailabilityOwner()).Token;
            CameraViewAssignmentContext context = Views(availability, View("main"));
            context.TryAssign(ViewId("main"), subject.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            availability.TryMakeUnavailable(availabilityToken);

            CameraViewAssignmentResult result = context.Reconcile(availability.CreateSnapshot());

            Assert.That(result.RemovedCount, Is.EqualTo(1));
            result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main);
            Assert.That(main.AssignmentCount, Is.Zero);
            Assert.That(main.ResolvedSubjectCount, Is.Zero);
        }

        [Test]
        public void RejoinIdentityDoesNotSatisfyRemovedOccurrenceAssignment()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject occurrenceA = Subject("p1-occurrence-a");
            CameraSubjectAvailabilityToken tokenA = availability.TryMakeAvailable(
                occurrenceA, AvailabilityOwner()).Token;
            CameraViewAssignmentContext context = Views(availability, View("main"));
            context.TryAssign(ViewId("main"), occurrenceA.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            availability.TryMakeUnavailable(tokenA);
            CameraSubject occurrenceB = Subject("p1-occurrence-b");
            availability.TryMakeAvailable(occurrenceB, AvailabilityOwner());

            CameraViewAssignmentSnapshot snapshot = context.Reconcile(
                availability.CreateSnapshot()).Snapshot;

            snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main);
            Assert.That(main.AssignmentCount, Is.Zero);
            Assert.That(main.ResolvedSubjectCount, Is.Zero);
        }

        [Test]
        public void ViewPersistsAfterLastSubjectBecomesUnavailable()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            CameraSubjectAvailabilityToken token = availability.TryMakeAvailable(
                subject, AvailabilityOwner()).Token;
            CameraViewAssignmentContext context = Views(availability, View("main"));
            context.TryAssign(ViewId("main"), subject.SubjectId, AssignmentOwner("gameplay"), availability.CreateSnapshot());
            availability.TryMakeUnavailable(token);

            CameraViewAssignmentSnapshot snapshot = context.Reconcile(
                availability.CreateSnapshot()).Snapshot;

            Assert.That(snapshot.ViewCount, Is.EqualTo(1));
            Assert.That(snapshot.TryGetView(ViewId("main"), out _), Is.True);
        }

        [Test]
        public void OwnerTeardownRemovesOnlyOwnedAssignments()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject p1 = Subject("p1-a");
            CameraSubject p2 = Subject("p2-a");
            availability.TryMakeAvailable(p1, AvailabilityOwner());
            availability.TryMakeAvailable(p2, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            CameraSubjectAssignmentOwnerId ownerA = AssignmentOwner("owner-a");
            CameraSubjectAssignmentOwnerId ownerB = AssignmentOwner("owner-b");
            context.TryAssign(ViewId("main"), p1.SubjectId, ownerA, availability.CreateSnapshot());
            context.TryAssign(ViewId("main"), p2.SubjectId, ownerB, availability.CreateSnapshot());

            CameraViewAssignmentResult result = context.ReleaseOwner(
                ownerA,
                availability.CreateSnapshot());

            result.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main);
            Assert.That(main.AssignmentCount, Is.EqualTo(1));
            Assert.That(main.Assignments[0].OwnerId, Is.EqualTo(ownerB));
        }

        [Test]
        public void StaleReleaseTokenCannotRemoveCurrentAssignment()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            CameraSubjectAssignmentOwnerId owner = AssignmentOwner("gameplay");
            CameraSubjectAssignmentToken oldToken = context.TryAssign(
                ViewId("main"), subject.SubjectId, owner, availability.CreateSnapshot()).Token;
            context.TryRelease(oldToken, availability.CreateSnapshot());
            CameraSubjectAssignmentToken currentToken = context.TryAssign(
                ViewId("main"), subject.SubjectId, owner, availability.CreateSnapshot()).Token;

            CameraViewAssignmentResult stale = context.TryRelease(
                oldToken,
                availability.CreateSnapshot());

            Assert.That(stale.Status, Is.EqualTo(CameraViewAssignmentStatus.RejectedForeignOrStaleToken));
            Assert.That(stale.Snapshot.TryGetView(ViewId("main"), out CameraViewSubjectSnapshot main), Is.True);
            Assert.That(main.Assignments[0].Token, Is.EqualTo(currentToken));
        }

        [Test]
        public void ForeignReleaseTokenCannotRemoveCurrentAssignment()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext first = Views(availability, "first", View("main"));
            CameraViewAssignmentContext second = Views(availability, "second", View("main"));
            CameraSubjectAssignmentToken foreign = first.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner("first"), availability.CreateSnapshot()).Token;
            second.TryAssign(ViewId("main"), subject.SubjectId, AssignmentOwner("second"), availability.CreateSnapshot());

            CameraViewAssignmentResult result = second.TryRelease(
                foreign,
                availability.CreateSnapshot());

            Assert.That(result.Status, Is.EqualTo(CameraViewAssignmentStatus.RejectedForeignOrStaleToken));
            Assert.That(second.AssignmentCount, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotOrderingIsDeterministic()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subjectB = Subject("subject-b");
            CameraSubject subjectA = Subject("subject-a");
            availability.TryMakeAvailable(subjectB, AvailabilityOwner());
            availability.TryMakeAvailable(subjectA, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(
                availability,
                View("view-b"),
                View("view-a"));
            context.TryAssign(ViewId("view-b"), subjectB.SubjectId, AssignmentOwner("owner"), availability.CreateSnapshot());
            context.TryAssign(ViewId("view-a"), subjectB.SubjectId, AssignmentOwner("owner"), availability.CreateSnapshot());
            CameraViewAssignmentSnapshot snapshot = context.TryAssign(
                ViewId("view-a"), subjectA.SubjectId, AssignmentOwner("owner"), availability.CreateSnapshot()).Snapshot;

            Assert.That(snapshot.Views[0].View.ViewId, Is.EqualTo(ViewId("view-a")));
            Assert.That(snapshot.Views[1].View.ViewId, Is.EqualTo(ViewId("view-b")));
            Assert.That(snapshot.Views[0].Assignments[0].SubjectId, Is.EqualTo(subjectA.SubjectId));
            Assert.That(snapshot.Views[0].Assignments[1].SubjectId, Is.EqualTo(subjectB.SubjectId));
        }

        [Test]
        public void OlderAvailabilitySnapshotCannotPublishNewAssignment()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraSubjectAvailabilitySnapshot stale = availability.CreateSnapshot();
            CameraViewAssignmentContext context = Views(availability, View("main"));
            availability.TryMakeUnavailable(stale.Entries[0].Token);
            context.Reconcile(availability.CreateSnapshot());

            CameraViewAssignmentResult result = context.TryAssign(
                ViewId("main"),
                subject.SubjectId,
                AssignmentOwner("gameplay"),
                stale);

            Assert.That(result.Status, Is.EqualTo(CameraViewAssignmentStatus.RejectedStaleAvailabilitySnapshot));
            Assert.That(context.AssignmentCount, Is.Zero);
        }

        [Test]
        public void ConflictingOwnerCannotClaimExistingRelation()
        {
            CameraSubjectAvailabilityContext availability = Availability();
            CameraSubject subject = Subject("p1-a");
            availability.TryMakeAvailable(subject, AvailabilityOwner());
            CameraViewAssignmentContext context = Views(availability, View("main"));
            context.TryAssign(ViewId("main"), subject.SubjectId, AssignmentOwner("owner-a"), availability.CreateSnapshot());

            CameraViewAssignmentResult result = context.TryAssign(
                ViewId("main"), subject.SubjectId, AssignmentOwner("owner-b"), availability.CreateSnapshot());

            Assert.That(result.Status, Is.EqualTo(CameraViewAssignmentStatus.RejectedAssignmentConflict));
            Assert.That(context.AssignmentCount, Is.EqualTo(1));
        }

        private CameraSubjectAvailabilityContext Availability()
        {
            return new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("subjects-session-a"));
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

        private static CameraSubjectAssignmentOwnerId AssignmentOwner(string id)
        {
            return new CameraSubjectAssignmentOwnerId(id);
        }

        private static CameraViewAssignmentContext Views(
            CameraSubjectAvailabilityContext availability,
            params CameraView[] views)
        {
            return Views(availability, "views-session-a", views);
        }

        private static CameraViewAssignmentContext Views(
            CameraSubjectAvailabilityContext availability,
            string contextId,
            params CameraView[] views)
        {
            return new CameraViewAssignmentContext(
                new ViewAssignmentContextId(contextId),
                availability.ContextId,
                views);
        }
    }
}
