using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    /// <summary>
    /// IF-ADR-035 RESET-035-B contracts for owner-aware Resettable registration over the existing
    /// ResetRegistry / ResetExecutor. Behavioral only: no legacy authoring structure is asserted.
    /// </summary>
    public sealed class ResettableOwnerRegistrationTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private ResetRegistry _registry;
        private ResettableOwnerRegistrationRuntime _registration;
        private RuntimeContentOwner _activityA;
        private RuntimeContentOwner _activityB;
        private RuntimeContentOwner _route;

        [SetUp]
        public void SetUp()
        {
            _registry = new ResetRegistry();
            _registration = new ResettableOwnerRegistrationRuntime(_registry);
            _activityA = RuntimeContentOwner.Activity("reset-035-b.activity-a", "Activity A", RuntimeDefinitionToken.MintAnonymous());
            _activityB = RuntimeContentOwner.Activity("reset-035-b.activity-b", "Activity B", RuntimeDefinitionToken.MintAnonymous());
            _route = RuntimeContentOwner.Route("reset-035-b.route", "Route", RuntimeDefinitionToken.MintAnonymous());
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
        public void Registration_UsesSuppliedTargetOwnerAndGeneratedIdentity()
        {
            GameObject root = CreateObject("Root");
            Resettable resettable = AddResettable(root);
            AddTransformCapability(root);

            Assert.IsTrue(Register(_activityB, root, out string diagnostic), diagnostic);

            Assert.IsTrue(resettable.IsRegistered);
            Assert.AreEqual(_activityB, resettable.Owner);
            Assert.AreEqual(ResetSubjectScope.Activity, resettable.Subject.Scope);
            Assert.AreEqual(1, resettable.RegisteredCapabilityCount);
            StringAssert.StartsWith(ResettableOwnerRegistrationRuntime.RuntimeSubjectPrefix + "#", resettable.RuntimeSubjectId.StableText);
            CollectionAssert.Contains(SubjectIds(ResetSubjectScope.Activity, _activityB), resettable.RuntimeSubjectId);
            CollectionAssert.IsEmpty(SubjectIds(ResetSubjectScope.Activity, _activityA));
        }

        [Test]
        public void Registration_RouteOwnerMapsToRouteScope()
        {
            GameObject root = CreateObject("RouteRoot");
            Resettable resettable = AddResettable(root);
            AddTransformCapability(root);

            Assert.IsTrue(Register(_route, root, out string diagnostic), diagnostic);

            Assert.AreEqual(ResetSubjectScope.Route, resettable.Subject.Scope);
            CollectionAssert.Contains(SubjectIds(ResetSubjectScope.Route, _route), resettable.RuntimeSubjectId);
        }

        [Test]
        public void Registration_RejectsInvalidOwner()
        {
            GameObject root = CreateObject("Root");
            AddResettable(root);

            Assert.IsFalse(Register(default, root, out string diagnostic));
            StringAssert.Contains("resettable-owner-invalid", diagnostic);
            Assert.AreEqual(0, _registry.SubjectCount);
        }

        [Test]
        public void NestedResettable_IsCollectionBoundary()
        {
            GameObject outer = CreateObject("Outer");
            Resettable outerResettable = AddResettable(outer);
            AddTransformCapability(outer);

            GameObject inner = CreateObject("Inner", outer.transform);
            Resettable innerResettable = AddResettable(inner);
            inner.AddComponent<UnityGameObjectActiveResetParticipant>();
            GameObject innerChild = CreateObject("InnerChild", inner.transform);
            AddTransformCapability(innerChild);

            Assert.IsTrue(Register(_activityA, outer, out string diagnostic), diagnostic);

            Assert.AreEqual(1, outerResettable.RegisteredCapabilityCount);
            Assert.AreEqual(2, innerResettable.RegisteredCapabilityCount);
            Assert.AreEqual(2, _registry.SubjectCount);
            Assert.AreEqual(3, _registry.ParticipantCount);
            Assert.AreNotEqual(outerResettable.RuntimeSubjectId, innerResettable.RuntimeSubjectId);
        }

        [Test]
        public void PrefabInstances_DoNotCollideInRuntimeIdentity()
        {
            GameObject template = CreateObject("Template");
            AddResettable(template);
            AddTransformCapability(template);
            template.SetActive(false);

            GameObject first = Object.Instantiate(template);
            GameObject second = Object.Instantiate(template);
            _created.Add(first);
            _created.Add(second);

            Assert.IsTrue(
                _registration.TryRegisterOwnerContent(_activityA, new[] { first, second }, "test", "prefab-instances", out string diagnostic),
                diagnostic);

            ResetSubjectId firstId = first.GetComponent<Resettable>().RuntimeSubjectId;
            ResetSubjectId secondId = second.GetComponent<Resettable>().RuntimeSubjectId;
            Assert.AreNotEqual(firstId, secondId);
            Assert.IsTrue(_registry.TryGetSubject(firstId, out _));
            Assert.IsTrue(_registry.TryGetSubject(secondId, out _));
            Assert.IsTrue(Execute(firstId).Succeeded);
            Assert.IsTrue(Execute(secondId).Succeeded);
        }

        [Test]
        public void Rollback_RemovesTargetRegistrations()
        {
            GameObject root = CreateObject("Root");
            Resettable resettable = AddResettable(root);
            AddTransformCapability(root);
            Assert.IsTrue(Register(_activityA, root, out string diagnostic), diagnostic);

            Assert.IsTrue(_registration.TryRollbackOwner(_activityA, "test", "rollback", out string rollbackDiagnostic), rollbackDiagnostic);

            Assert.IsFalse(resettable.IsRegistered);
            Assert.AreEqual(0, _registry.SubjectCount);
            Assert.AreEqual(0, _registry.ParticipantCount);
            Assert.AreEqual(0, _registration.GetRegisteredResettableCount(_activityA));
        }

        [Test]
        public void Release_RemovesOnlyThatOwnerRegistrations()
        {
            GameObject rootA = CreateObject("RootA");
            Resettable resettableA = AddResettable(rootA);
            AddTransformCapability(rootA);
            GameObject rootB = CreateObject("RootB");
            Resettable resettableB = AddResettable(rootB);
            AddTransformCapability(rootB);
            Assert.IsTrue(Register(_activityA, rootA, out string diagnosticA), diagnosticA);
            Assert.IsTrue(Register(_route, rootB, out string diagnosticB), diagnosticB);

            Assert.IsTrue(_registration.TryReleaseOwner(_activityA, "test", "release", out string releaseDiagnostic), releaseDiagnostic);
            Assert.IsTrue(_registration.TryReleaseOwner(_activityA, "test", "release-again", out string repeatedReleaseDiagnostic), repeatedReleaseDiagnostic);

            Assert.IsFalse(resettableA.IsRegistered);
            Assert.IsTrue(resettableB.IsRegistered);
            CollectionAssert.IsEmpty(SubjectIds(ResetSubjectScope.Activity, _activityA));
            CollectionAssert.Contains(SubjectIds(ResetSubjectScope.Route, _route), resettableB.RuntimeSubjectId);
        }

        [Test]
        public void Registration_RejectsSameOwnerUntilReleased()
        {
            GameObject first = CreateObject("First");
            AddResettable(first);
            GameObject second = CreateObject("Second");
            AddResettable(second);
            Assert.IsTrue(Register(_activityA, first, out string diagnostic), diagnostic);

            Assert.IsFalse(Register(_activityA, second, out string duplicateDiagnostic));
            StringAssert.Contains("resettable-owner-already-registered", duplicateDiagnostic);

            Assert.IsTrue(_registration.TryReleaseOwner(_activityA, "test", "release", out _));
            Assert.IsTrue(Register(_activityA, second, out string reentryDiagnostic), reentryDiagnostic);
        }

        [Test]
        public void MixedLegacyAdapter_InSameBoundary_IsRejectedWithoutPartialRegistration()
        {
            GameObject valid = CreateObject("Valid");
            AddResettable(valid);
            AddTransformCapability(valid);
            GameObject mixed = CreateObject("Mixed");
            AddResettable(mixed);
            mixed.AddComponent<UnityResetSubjectAdapter>();

            bool registered = _registration.TryRegisterOwnerContent(
                _activityA,
                new[] { valid, mixed },
                "test",
                "mixed",
                out string diagnostic);

            Assert.IsFalse(registered);
            StringAssert.Contains("mixed-legacy-adapter", diagnostic);
            Assert.AreEqual(0, _registry.SubjectCount);
            Assert.IsFalse(valid.GetComponent<Resettable>().IsRegistered);
        }

        [Test]
        public void MixedLegacyAdapter_AncestorChildrenDiscovery_IsRejected()
        {
            GameObject legacyParent = CreateObject("LegacyParent");
            legacyParent.AddComponent<UnityResetSubjectAdapter>();
            GameObject child = CreateObject("Child", legacyParent.transform);
            AddResettable(child);

            Assert.IsFalse(Register(_activityA, legacyParent, out string diagnostic));
            StringAssert.Contains("mixed-legacy-adapter", diagnostic);
            Assert.AreEqual(0, _registry.SubjectCount);
        }

        [Test]
        public void LegacyAdapter_OutsideResettableBoundary_DoesNotBlockRegistration()
        {
            GameObject legacy = CreateObject("Legacy");
            legacy.AddComponent<UnityResetSubjectAdapter>();
            GameObject root = CreateObject("Root");
            AddResettable(root);
            AddTransformCapability(root);

            Assert.IsTrue(
                _registration.TryRegisterOwnerContent(_activityA, new[] { legacy, root }, "test", "sibling", out string diagnostic),
                diagnostic);
            Assert.AreEqual(1, _registry.SubjectCount);
        }

        [Test]
        public void ResetExecutor_RestoresRegisteredResettableBaseline()
        {
            GameObject root = CreateObject("Root");
            root.transform.localPosition = new Vector3(1f, 2f, 3f);
            Resettable resettable = AddResettable(root);
            AddTransformCapability(root);
            Assert.IsTrue(Register(_activityA, root, out string diagnostic), diagnostic);

            root.transform.localPosition = new Vector3(9f, -4f, 7f);
            ResetExecutionResult result = Execute(resettable.RuntimeSubjectId);

            Assert.IsTrue(result.Succeeded, result.ToString());
            Assert.AreEqual(1, result.SubjectCount);
            Assert.AreEqual(1, result.ParticipantCount);
            Assert.AreEqual(1, result.ParticipantSucceeded);
            Assert.That(Vector3.Distance(root.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.LessThan(0.0001f));
        }

        private GameObject CreateObject(string objectName, Transform parent = null)
        {
            var gameObject = new GameObject(objectName);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            else
            {
                _created.Add(gameObject);
            }

            return gameObject;
        }

        private static Resettable AddResettable(GameObject gameObject)
        {
            return gameObject.AddComponent<Resettable>();
        }

        private static void AddTransformCapability(GameObject gameObject)
        {
            UnityTransformResetParticipant participant = gameObject.AddComponent<UnityTransformResetParticipant>();
            participant.CaptureBaseline();
        }

        private bool Register(RuntimeContentOwner owner, GameObject root, out string diagnostic)
        {
            return _registration.TryRegisterOwnerContent(owner, new[] { root }, "test", "reset-035-b", out diagnostic);
        }

        private ResetSubjectId[] SubjectIds(ResetSubjectScope scope, RuntimeContentOwner owner)
        {
            return _registry.GetSubjectsByScopeAndOwner(scope, owner)
                .Select(subject => subject.SubjectId)
                .ToArray();
        }

        private ResetExecutionResult Execute(ResetSubjectId subjectId)
        {
            var executor = new ResetExecutor(_registry);
            ResetExecutionRequest request = ResetExecutionRequest.ForSingleSubject(
                subjectId,
                allowNoParticipants: false,
                source: "test",
                reason: "reset-035-b");
            return executor.ExecuteAsync(request).GetAwaiter().GetResult();
        }
    }
}
