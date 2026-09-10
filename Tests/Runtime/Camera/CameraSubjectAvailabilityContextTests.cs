using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraSubjectAvailabilityContextTests
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
        public void ContextIdentityUsesOrdinalValueEqualityAndDistinctDomains()
        {
            var id = new SubjectAvailabilityContextId(" session-a ");
            var same = new SubjectAvailabilityContextId("session-a");
            Assert.That(id == same, Is.True);
            Assert.That(id.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(id != new SubjectAvailabilityContextId("Session-a"), Is.True);
            Assert.That(id.ToString(), Is.EqualTo("session-a"));
            Assert.That(id.Equals((object)new ViewAssignmentContextId("session-a")), Is.False);
            var values = new Dictionary<SubjectAvailabilityContextId, int> { [id] = 1 };
            Assert.That(values[same], Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        public void InvalidContextIdentityIsExplicitlyRejected(string value)
        {
            var id = new SubjectAvailabilityContextId(value);
            Assert.That(id.IsValid, Is.False);
            Assert.That(id, Is.EqualTo(default(SubjectAvailabilityContextId)));
            Assert.That(id.GetHashCode(), Is.EqualTo(default(SubjectAvailabilityContextId).GetHashCode()));
            Assert.That(id.ToString(), Is.Empty);
            Assert.Throws<System.ArgumentException>(() => new CameraSubjectAvailabilityContext(id));
            Assert.Throws<System.ArgumentException>(() => new CameraSubjectAvailabilityContext(default));
        }

        [Test]
        public void OneSubjectBecomesAvailable()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubjectAvailabilityResult result = context.TryMakeAvailable(
                Subject("p1-a"), Owner("players-a"));

            Assert.That(result.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.SucceededAvailable));
            Assert.That(result.Token.IsValid, Is.True);
            SubjectAvailabilityContextId tokenContext = result.Token.ContextId;
            SubjectAvailabilityContextId snapshotContext = result.Snapshot.ContextId;
            Assert.That(tokenContext, Is.EqualTo(context.ContextId));
            Assert.That(snapshotContext, Is.EqualTo(context.ContextId));
            Assert.That(result.Snapshot.Count, Is.EqualTo(1));
        }

        [Test]
        public void TwoSubjectsRemainIndependent()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            context.TryMakeAvailable(Subject("p1-a"), Owner("players-a"));
            context.TryMakeAvailable(Subject("p2-a"), Owner("players-a"));

            CameraSubjectAvailabilitySnapshot snapshot = context.CreateSnapshot();
            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(snapshot.TryGet(new CameraSubjectId("p1-a"), out _), Is.True);
            Assert.That(snapshot.TryGet(new CameraSubjectId("p2-a"), out _), Is.True);
        }

        [Test]
        public void ExactRemovalLeavesOtherSubjectAvailable()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubjectAvailabilityToken p1 = context.TryMakeAvailable(
                Subject("p1-a"), Owner("players-a")).Token;
            context.TryMakeAvailable(Subject("p2-a"), Owner("players-a"));

            CameraSubjectAvailabilityResult result = context.TryMakeUnavailable(p1);

            Assert.That(result.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.SucceededUnavailable));
            Assert.That(result.Snapshot.Count, Is.EqualTo(1));
            Assert.That(result.Snapshot.TryGet(new CameraSubjectId("p1-a"), out _), Is.False);
            Assert.That(result.Snapshot.TryGet(new CameraSubjectId("p2-a"), out _), Is.True);
        }

        [Test]
        public void DuplicateAvailabilityIsIdempotent()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubject subject = Subject("p1-a");
            CameraSubjectAvailabilityOwnerId owner = Owner("players-a");
            CameraSubjectAvailabilityResult first = context.TryMakeAvailable(subject, owner);
            CameraSubjectAvailabilityResult duplicate = context.TryMakeAvailable(subject, owner);

            Assert.That(duplicate.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.SucceededAlreadyAvailable));
            Assert.That(duplicate.Token, Is.EqualTo(first.Token));
            Assert.That(duplicate.Snapshot.Count, Is.EqualTo(1));
            Assert.That(duplicate.Snapshot.Revision, Is.EqualTo(first.Snapshot.Revision));
        }

        [Test]
        public void AvailabilityNotificationCarriesOnlyChangedImmutableSnapshots()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubject subject = Subject("p1-a");
            CameraSubjectAvailabilityOwnerId owner = Owner("players-a");
            var revisions = new List<int>();
            context.AvailabilityChanged += snapshot => revisions.Add(snapshot.Revision);

            CameraSubjectAvailabilityToken token =
                context.TryMakeAvailable(subject, owner).Token;
            context.TryMakeAvailable(subject, owner);
            context.TryMakeUnavailable(token);
            context.TryMakeUnavailable(token);

            Assert.That(revisions, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void LeaveAndRejoinUseDifferentOccurrenceEvidence()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubjectAvailabilityToken occurrenceA = context.TryMakeAvailable(
                Subject("p1-occurrence-a"), Owner("players-a")).Token;
            context.TryMakeUnavailable(occurrenceA);

            CameraSubjectAvailabilityResult occurrenceB = context.TryMakeAvailable(
                Subject("p1-occurrence-b"), Owner("players-a"));
            CameraSubjectAvailabilityResult staleLeave = context.TryMakeUnavailable(occurrenceA);

            Assert.That(staleLeave.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.SucceededAlreadyUnavailable));
            Assert.That(context.CreateSnapshot().Count, Is.EqualTo(1));
            Assert.That(context.CreateSnapshot().TryGet(occurrenceB.Subject.SubjectId, out _), Is.True);
        }

        [Test]
        public void StaleTokenCannotRemoveRepublishedIdentity()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubjectAvailabilityOwnerId owner = Owner("players-a");
            CameraSubjectAvailabilityToken oldToken = context.TryMakeAvailable(
                Subject("shared-id"), owner).Token;
            context.TryMakeUnavailable(oldToken);
            CameraSubjectAvailabilityToken currentToken = context.TryMakeAvailable(
                Subject("shared-id"), owner).Token;

            CameraSubjectAvailabilityResult staleRemoval = context.TryMakeUnavailable(oldToken);

            Assert.That(staleRemoval.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.RejectedForeignOrStaleToken));
            Assert.That(context.CreateSnapshot().Count, Is.EqualTo(1));
            Assert.That(currentToken, Is.Not.EqualTo(oldToken));
        }

        [Test]
        public void ScopeOwnerTeardownRemovesOnlyOwnedSubjects()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            CameraSubjectAvailabilityOwnerId players = Owner("players-a");
            CameraSubjectAvailabilityOwnerId activity = Owner("activity-a");
            context.TryMakeAvailable(Subject("p1-a"), players);
            context.TryMakeAvailable(Subject("boss-a"), activity);

            int released = context.ReleaseOwner(players);

            Assert.That(released, Is.EqualTo(1));
            Assert.That(context.CreateSnapshot().TryGet(new CameraSubjectId("p1-a"), out _), Is.False);
            Assert.That(context.CreateSnapshot().TryGet(new CameraSubjectId("boss-a"), out _), Is.True);
        }

        [Test]
        public void ForeignContextTokenIsRejected()
        {
            var first = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            var second = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-b"));
            CameraSubjectAvailabilityToken foreign = first.TryMakeAvailable(
                Subject("p1-a"), Owner("players-a")).Token;
            second.TryMakeAvailable(Subject("p1-a"), Owner("players-b"));

            CameraSubjectAvailabilityResult result = second.TryMakeUnavailable(foreign);

            Assert.That(result.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.RejectedForeignOrStaleToken));
            Assert.That(second.CreateSnapshot().Count, Is.EqualTo(1));
        }

        [Test]
        public void SameIdentityWithDifferentObservationIsRejected()
        {
            var context = new CameraSubjectAvailabilityContext(new SubjectAvailabilityContextId("session-a"));
            context.TryMakeAvailable(Subject("p1-a"), Owner("players-a"));

            CameraSubjectAvailabilityResult result = context.TryMakeAvailable(
                Subject("p1-a"), Owner("players-a"));

            Assert.That(result.Status, Is.EqualTo(CameraSubjectAvailabilityStatus.RejectedSubjectConflict));
            Assert.That(result.Snapshot.Count, Is.EqualTo(1));
        }

        private CameraSubject Subject(string id)
        {
            var gameObject = new GameObject(id);
            _created.Add(gameObject);
            return new CameraSubject(new CameraSubjectId(id), gameObject.transform, id);
        }

        private static CameraSubjectAvailabilityOwnerId Owner(string id)
        {
            return new CameraSubjectAvailabilityOwnerId(id);
        }
    }
}
