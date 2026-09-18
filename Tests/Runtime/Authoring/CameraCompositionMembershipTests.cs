using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class CameraCompositionMembershipTests
    {
        [Test]
        public void EmptyMembershipIsValidAndIdempotent()
        {
            using var f = new Fixture();
            CameraCompositionMembershipResult first = f.Reconcile();
            CameraCompositionMembershipResult second = f.Reconcile();
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Snapshot.Count, Is.Zero);
            Assert.That(second.Status, Is.EqualTo(CameraCompositionMembershipStatus.SucceededNoChange));
            Assert.That(second.Snapshot.Revision, Is.EqualTo(first.Snapshot.Revision));
        }

        [Test]
        public void MembershipTransitionsZeroOneManyOneZero()
        {
            using var f = new Fixture();
            f.Add("b");
            Assert.That(f.Reconcile("b").Snapshot.Count, Is.EqualTo(1));
            f.Add("a");
            Assert.That(f.Reconcile("b", "a").Snapshot.Count, Is.EqualTo(2));
            Assert.That(f.Reconcile("a").Snapshot.Count, Is.EqualTo(1));
            Assert.That(f.Reconcile().Snapshot.Count, Is.Zero);
        }

        [Test]
        public void RejoinCreatesNewOccurrenceAndRejectsOldMembershipToken()
        {
            using var f = new Fixture();
            CameraSubjectAvailabilityToken availabilityToken = f.Add("a");
            CameraCompositionMembershipToken old = f.Reconcile("a").Snapshot.Entries[0].Token;
            f.Availability.TryMakeUnavailable(availabilityToken);
            f.Add("a");
            CameraCompositionMembershipSnapshot current = f.Reconcile("a").Snapshot;
            Assert.That(current.Entries[0].Token, Is.Not.EqualTo(old));
            Assert.That(f.Membership.TryRelease(old, f.Availability.CreateSnapshot()).Status,
                Is.EqualTo(CameraCompositionMembershipStatus.RejectedForeignOrStaleToken));
        }

        [Test]
        public void StaleAvailabilityCannotRestoreOlderMembership()
        {
            using var f = new Fixture();
            f.Add("a");
            CameraSubjectAvailabilitySnapshot stale = f.Availability.CreateSnapshot();
            f.Add("b");
            f.Reconcile("a", "b");
            CameraCompositionMembershipResult rejected = f.Membership.Reconcile(
                stale, new[] { new CameraSubjectId("a") });
            Assert.That(rejected.Status, Is.EqualTo(CameraCompositionMembershipStatus.RejectedStaleAvailabilitySnapshot));
            Assert.That(rejected.Snapshot.Count, Is.EqualTo(2));
        }

        [Test]
        public void ForeignAvailabilityAndForeignMembershipTokenAreRejected()
        {
            using var first = new Fixture("first");
            using var second = new Fixture("second");
            first.Add("a");
            second.Add("a");
            CameraCompositionMembershipToken token = first.Reconcile("a").Snapshot.Entries[0].Token;
            Assert.That(second.Membership.TryRelease(token, second.Availability.CreateSnapshot()).Status,
                Is.EqualTo(CameraCompositionMembershipStatus.RejectedForeignOrStaleToken));
            Assert.That(first.Membership.Reconcile(second.Availability.CreateSnapshot(), Array.Empty<CameraSubjectId>()).Status,
                Is.EqualTo(CameraCompositionMembershipStatus.RejectedForeignAvailabilityContext));
        }

        [Test]
        public void MembershipOrderIsDeterministic()
        {
            using var f = new Fixture();
            f.Add("c"); f.Add("a"); f.Add("b");
            CameraCompositionMembershipSnapshot snapshot = f.Reconcile("c", "a", "b").Snapshot;
            Assert.That(snapshot.Entries.Select(entry => entry.SubjectId.Value),
                Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void TwoCompositionsHaveIndependentContextAndOccurrenceIdentity()
        {
            using var f = new Fixture();
            f.Add("a");
            var other = new CameraCompositionMembershipContext(
                new CameraCompositionMembershipContextId("other-membership"), f.Availability.ContextId);
            CameraCompositionMembershipSnapshot first = f.Reconcile("a").Snapshot;
            CameraCompositionMembershipSnapshot second = other.Reconcile(
                f.Availability.CreateSnapshot(), new[] { new CameraSubjectId("a") }).Snapshot;
            Assert.That(first.ContextId, Is.Not.EqualTo(second.ContextId));
            Assert.That(first.Entries[0].Token, Is.Not.EqualTo(second.Entries[0].Token));
        }

        [Test]
        public void PresentationInputRejectsNewerMembershipEvidence()
        {
            using var f = new Fixture();
            f.Add("a");
            CameraCompositionMembershipSnapshot first = f.Reconcile("a").Snapshot;
            CameraViewPresentationInput input = CameraViewPresentationInputProjection.TryCreate(first).Input;
            f.Add("b");
            CameraCompositionMembershipSnapshot second = f.Reconcile("a", "b").Snapshot;
            Assert.That(input.IsCurrentFor(first), Is.True);
            Assert.That(input.IsCurrentFor(second), Is.False);
        }

        [Test]
        public void MembershipContractsDoNotExposeCameraViewIdentity()
        {
            Type[] contracts = { typeof(CameraCompositionMembershipContext),
                typeof(CameraCompositionMembershipSnapshot), typeof(CameraCompositionMembershipEntry),
                typeof(CameraCompositionMembershipToken) };
            foreach (Type contract in contracts)
            {
                Assert.That(contract.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Any(member => member.Name.Contains("View", StringComparison.Ordinal)), Is.False, contract.Name);
                Assert.That(contract.GetProperties().Any(property => property.PropertyType == typeof(CameraViewId)),
                    Is.False, contract.Name);
            }
        }

        [Test]
        public void ClearIsIdempotentAndInvalidatesOldToken()
        {
            using var f = new Fixture();
            f.Add("a");
            CameraCompositionMembershipToken token = f.Reconcile("a").Snapshot.Entries[0].Token;
            Assert.That(f.Membership.Clear().RemovedCount, Is.EqualTo(1));
            int revision = f.Membership.Revision;
            Assert.That(f.Membership.Clear().RemovedCount, Is.Zero);
            Assert.That(f.Membership.Revision, Is.EqualTo(revision));
            Assert.That(f.Membership.TryRelease(token, f.Availability.CreateSnapshot()).Status,
                Is.EqualTo(CameraCompositionMembershipStatus.RejectedForeignOrStaleToken));
        }

        [Test]
        public void CompositionTeardownClearsMembershipAndIsIdempotent()
        {
            using var f = new Fixture();
            f.Add("a");
            CameraCompositionMembershipSnapshot snapshot = f.Reconcile("a").Snapshot;
            var root = new GameObject("composition-teardown-test");
            try
            {
                var composition = root.AddComponent<CameraSharedComposition>();
                SetField(composition, "_availability", f.Availability);
                SetField(composition, "_membership", f.Membership);
                SetField(composition, "_currentMembership", snapshot);
                Invoke(composition, "StopComposition");
                Assert.That(composition.Snapshot.LastReconcileStatus,
                    Is.EqualTo(CameraSharedCompositionReconcileStatus.SucceededStopped));
                Assert.That(composition.Snapshot.RemovedMembershipCount, Is.EqualTo(1));
                Assert.That(f.Membership.Count, Is.Zero);
                Invoke(composition, "StopComposition");
                Assert.That(f.Membership.Count, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);

        private sealed class Fixture : IDisposable
        {
            private readonly List<GameObject> _objects = new List<GameObject>();
            private readonly CameraSubjectAvailabilityOwnerId _owner;

            internal Fixture(string suffix = "fixture")
            {
                Availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId($"availability-{suffix}"));
                Membership = new CameraCompositionMembershipContext(
                    new CameraCompositionMembershipContextId($"membership-{suffix}"), Availability.ContextId);
                _owner = new CameraSubjectAvailabilityOwnerId($"owner-{suffix}");
            }

            internal CameraSubjectAvailabilityContext Availability { get; }
            internal CameraCompositionMembershipContext Membership { get; }

            internal CameraSubjectAvailabilityToken Add(string id)
            {
                var root = new GameObject(id);
                _objects.Add(root);
                return Availability.TryMakeAvailable(
                    new CameraSubject(new CameraSubjectId(id), root.transform, id), _owner).Token;
            }

            internal CameraCompositionMembershipResult Reconcile(params string[] ids) =>
                Membership.Reconcile(Availability.CreateSnapshot(),
                    ids.Select(id => new CameraSubjectId(id)).ToArray());

            public void Dispose()
            {
                for (int index = 0; index < _objects.Count; index++)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
            }
        }
    }
}
