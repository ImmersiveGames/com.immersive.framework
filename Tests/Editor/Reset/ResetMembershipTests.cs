using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class ResetMembershipTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private ResetRegistry _registry;
        private ResettableOwnerRegistrationRuntime _registration;
        private RuntimeContentOwner _activityA;
        private RuntimeContentOwner _activityB;
        private RuntimeContentOwner _routeA;
        private RuntimeContentOwner _routeB;

        [SetUp]
        public void SetUp()
        {
            _registry = new ResetRegistry();
            _registration = new ResettableOwnerRegistrationRuntime(_registry);
            _activityA = RuntimeContentOwner.Activity("reset-035-c.activity-a", "Activity A", RuntimeDefinitionToken.MintAnonymous());
            _activityB = RuntimeContentOwner.Activity("reset-035-c.activity-b", "Activity B", RuntimeDefinitionToken.MintAnonymous());
            _routeA = RuntimeContentOwner.Route("reset-035-c.route-a", "Route A", RuntimeDefinitionToken.MintAnonymous());
            _routeB = RuntimeContentOwner.Route("reset-035-c.route-b", "Route B", RuntimeDefinitionToken.MintAnonymous());
        }

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
        public void ActivityOwnedFollowOwner_IsIncludedInCurrentActivity()
        {
            Resettable resettable = CreateResettable("Activity");
            AssertRegistered(_activityA, resettable);

            CollectionAssert.AreEqual(
                new[] { resettable.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA));
            Assert.AreEqual(ResetMembership.FollowOwner, resettable.Membership);
            Assert.AreEqual(ResetMembership.Activity, resettable.Subject.EffectiveMembership);
        }

        [Test]
        public void RouteOwnedFollowOwner_IsExcludedFromCurrentActivity()
        {
            Resettable resettable = CreateResettable("Route");
            AssertRegistered(_routeA, resettable);

            CollectionAssert.IsEmpty(
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA));
        }

        [Test]
        public void RouteOwnedActivityMembership_IsIncludedInCurrentActivity()
        {
            Resettable resettable = CreateResettable("Route");
            SetMembership(resettable, ResetMembership.Activity);
            AssertRegistered(_routeA, resettable);

            CollectionAssert.AreEqual(
                new[] { resettable.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA));
            Assert.AreEqual(_routeA, resettable.Owner);
            Assert.AreEqual(_routeA, resettable.Subject.Owner);
        }

        [Test]
        public void ActivityOwnedRouteMembership_IsRejectedExplicitly()
        {
            Resettable resettable = CreateResettable("Activity");
            SetMembership(resettable, ResetMembership.Route);

            Assert.IsFalse(Register(_activityA, new[] { resettable }, out string diagnostic));
            StringAssert.Contains("membership-owner-incompatible", diagnostic);
            Assert.IsFalse(resettable.IsRegistered);
            Assert.AreEqual(0, _registry.SubjectCount);
        }

        [Test]
        public void CurrentRoute_IncludesRouteMembersAndExcludesOtherRouteOccurrence()
        {
            Resettable currentRouteMember = CreateResettable("CurrentRoute");
            Resettable otherRouteMember = CreateResettable("OtherRoute");
            AssertRegistered(_routeA, currentRouteMember);
            AssertRegistered(_routeB, otherRouteMember);

            CollectionAssert.AreEqual(
                new[] { currentRouteMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _routeA, _activityA));
        }

        [Test]
        public void CurrentActivity_DoesNotIncludeAnotherActivityOccurrence()
        {
            Resettable activityAResettable = CreateResettable("ActivityA");
            Resettable activityBResettable = CreateResettable("ActivityB");
            AssertRegistered(_activityA, activityAResettable);
            AssertRegistered(_activityB, activityBResettable);

            CollectionAssert.AreEqual(
                new[] { activityAResettable.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA));
        }

        [Test]
        public void CurrentRoute_IncludesActivityOwnedSubjectsFromCurrentActivityOnly()
        {
            Resettable currentActivityMember = CreateResettable("CurrentActivity");
            Resettable otherActivityMember = CreateResettable("OtherActivity");
            AssertRegistered(_activityA, currentActivityMember);
            AssertRegistered(_activityB, otherActivityMember);

            CollectionAssert.AreEqual(
                new[] { currentActivityMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _routeA, _activityA));
        }

        [Test]
        public void CurrentActivitySubjectOrdering_IsDeterministic()
        {
            Resettable first = CreateResettable("First");
            Resettable second = CreateResettable("Second");
            Resettable third = CreateResettable("Third");
            AssertRegistered(_activityA, first, second, third);

            ResetSubjectId[] firstResolution = ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA).ToArray();
            ResetSubjectId[] secondResolution = ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activityA, _routeA).ToArray();

            CollectionAssert.AreEqual(firstResolution, secondResolution);
            CollectionAssert.AreEqual(firstResolution.OrderBy(id => id.StableText, System.StringComparer.Ordinal), firstResolution);
        }

        [Test]
        public void ChangingMembership_DoesNotChangeContentOwnership()
        {
            Resettable resettable = CreateResettable("Route");
            AssertRegistered(_routeA, resettable);
            RuntimeContentOwner originalOwner = resettable.Owner;
            Assert.IsTrue(_registration.TryReleaseOwner(_routeA, "test", "change-membership", out string releaseDiagnostic), releaseDiagnostic);

            SetMembership(resettable, ResetMembership.Activity);
            AssertRegistered(_routeA, resettable);

            Assert.AreEqual(originalOwner, resettable.Owner);
            Assert.AreEqual(originalOwner, resettable.Subject.Owner);
            Assert.AreEqual(ResetSubjectScope.Route, resettable.Subject.Scope);
            Assert.AreEqual(ResetMembership.Activity, resettable.Subject.EffectiveMembership);
        }

        [Test]
        public void CurrentRoute_IncludesRouteOwnedSubjectWithActivityMembership()
        {
            Resettable activityMember = CreateResettable("ActivityMember");
            SetMembership(activityMember, ResetMembership.Activity);
            AssertRegistered(_routeA, activityMember);

            CollectionAssert.AreEqual(
                new[] { activityMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _routeA, default));
        }

        private Resettable CreateResettable(string objectName)
        {
            var gameObject = new GameObject(objectName);
            _created.Add(gameObject);
            return gameObject.AddComponent<Resettable>();
        }

        private void AssertRegistered(RuntimeContentOwner owner, params Resettable[] resettables)
        {
            Assert.IsTrue(Register(owner, resettables, out string diagnostic), diagnostic);
        }

        private bool Register(RuntimeContentOwner owner, IReadOnlyList<Resettable> resettables, out string diagnostic)
        {
            var roots = resettables.Select(resettable => resettable.gameObject).ToArray();
            return _registration.TryRegisterOwnerContent(owner, roots, "test", "reset-035-c", out diagnostic);
        }

        private static void SetMembership(Resettable resettable, ResetMembership membership)
        {
            var serializedObject = new SerializedObject(resettable);
            SerializedProperty property = serializedObject.FindProperty("membership");
            Assert.IsNotNull(property, "Resettable must expose serialized Reset membership.");
            property.enumValueIndex = (int)membership;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
