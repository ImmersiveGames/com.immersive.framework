using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class StableObjectBindingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private StableObjectBindingRegistry _bindings;
        private ResetRegistry _resets;
        private ResettableOwnerRegistrationRuntime _resetRegistration;
        private RouteAsset _routeA;
        private RouteAsset _routeB;
        private RuntimeContentOwner _ownerA;
        private RuntimeContentOwner _ownerB;

        [SetUp]
        public void SetUp()
        {
            _bindings = new StableObjectBindingRegistry();
            _resets = new ResetRegistry();
            _resetRegistration = new ResettableOwnerRegistrationRuntime(_resets);
            _routeA = CreateRoute("qa.stable.route-a");
            _routeB = CreateRoute("qa.stable.route-b");
            _ownerA = Owner(_routeA);
            _ownerB = Owner(_routeB);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject value in _objects)
                if (value != null) Object.DestroyImmediate(value);
            foreach (ScriptableObject value in _assets)
                if (value != null) Object.DestroyImmediate(value);
            _objects.Clear();
            _assets.Clear();
        }

        [Test]
        public void OneBinding_ResolvesPhysicalOccurrence()
        {
            const string id = "qa.stable.target";
            GameObject target = CreateEntry(id, _routeA, true);
            Register(_ownerA, target);

            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out StableObjectBinding binding, out var status, out string diagnostic), diagnostic);
            Assert.AreEqual(StableObjectBindingResolutionStatus.Resolved, status);
            Assert.AreSame(target, binding.PhysicalObject);
            Assert.AreEqual(_ownerA, binding.Owner);
        }

        [Test]
        public void MissingBinding_IsNotFound()
        {
            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From("qa.stable.missing"), null, null, out _, out var status, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.NotFound, status);
        }

        [Test]
        public void PreparedBinding_IsNotResolvableUntilOwnerCommit()
        {
            const string id = "qa.stable.prepared";
            GameObject target = CreateEntry(id, _routeA, true);
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { target }, out string prepareDiagnostic), prepareDiagnostic);

            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out _, out var beforeCommitStatus, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.NotFound, beforeCommitStatus);

            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out string commitDiagnostic), commitDiagnostic);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out StableObjectBinding binding, out _, out string resolveDiagnostic), resolveDiagnostic);
            Assert.AreSame(target, binding.PhysicalObject);
        }

        [Test]
        public void DuplicateBindingsInSameOwner_AreAmbiguous()
        {
            const string id = "qa.stable.duplicate";
            GameObject first = CreateEntry(id, _routeA, true);
            GameObject second = CreateEntry(id, _routeA, true);
            Register(_ownerA, first, second);

            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From(id), OwnerKey(_routeA), RuntimeDefinitionToken.FromUnityObject(_routeA), out _, out var status, out string diagnostic));
            Assert.AreEqual(StableObjectBindingResolutionStatus.Ambiguous, status);
            StringAssert.Contains("ambiguous", diagnostic);
        }

        [Test]
        public void SameObjectEntryIdAcrossOwners_IsIndependentAndOwnerSelectorChoosesOwner()
        {
            const string id = "qa.stable.shared";
            GameObject entryA = CreateEntry(id, _routeA, true);
            GameObject entryB = CreateEntry(id, _routeB, true);
            Register(_ownerA, entryA);
            Register(_ownerB, entryB);

            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), OwnerKey(_routeA), RuntimeDefinitionToken.FromUnityObject(_routeA), out StableObjectBinding bindingA, out _, out string diagnosticA), diagnosticA);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), OwnerKey(_routeB), RuntimeDefinitionToken.FromUnityObject(_routeB), out StableObjectBinding bindingB, out _, out string diagnosticB), diagnosticB);
            Assert.AreSame(entryA, bindingA.PhysicalObject);
            Assert.AreSame(entryB, bindingB.PhysicalObject);
            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out _, out var status, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.Ambiguous, status);

            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out string releaseDiagnostic), releaseDiagnostic);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out StableObjectBinding remaining, out _, out string remainingDiagnostic), remainingDiagnostic);
            Assert.AreSame(entryB, remaining.PhysicalObject);
        }

        [Test]
        public void DestroyedPhysicalOccurrence_IsNotResolved()
        {
            const string id = "qa.stable.destroyed";
            GameObject target = CreateEntry(id, _routeA, true);
            Register(_ownerA, target);
            Object.DestroyImmediate(target);

            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out _, out var status, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.NotFound, status);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RollbackAndRelease_RemoveOwnerBindings(bool rollback)
        {
            const string id = "qa.stable.removed";
            GameObject target = CreateEntry(id, _routeA, true);
            Register(_ownerA, target);

            bool removed = rollback
                ? _bindings.TryRollbackOwner(_ownerA, out string diagnostic)
                : _bindings.TryReleaseOwner(_ownerA, out diagnostic);

            Assert.IsTrue(removed, diagnostic);
            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out _, out var status, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.NotFound, status);
        }

        [Test]
        public void Reload_ResolvesNewPhysicalOccurrenceAndNewRuntimeSubject()
        {
            const string id = "qa.stable.reload";
            StableObjectReference reference = StableObjectReference.ForEntry(Id(id));
            GameObject first = CreateEntry(id, _routeA, true);
            Register(_ownerA, first);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, new[] { first }, "test", "first", out string resetDiagnostic), resetDiagnostic);
            Assert.IsTrue(ResetTargetResolver.TryResolveStableReferenceSubject(_bindings, _resets, reference, out ResetSubject before, out string beforeDiagnostic), beforeDiagnostic);

            Assert.IsTrue(_resetRegistration.TryReleaseOwner(_ownerA, "test", "unload", out string resetReleaseDiagnostic), resetReleaseDiagnostic);
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out string bindingReleaseDiagnostic), bindingReleaseDiagnostic);
            Assert.IsFalse(ResetTargetResolver.TryResolveStableReferenceSubject(_bindings, _resets, reference, out _, out _));

            GameObject second = CreateEntry(id, _routeA, true);
            Register(_ownerA, second);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, new[] { second }, "test", "reload", out resetDiagnostic), resetDiagnostic);
            Assert.IsTrue(ResetTargetResolver.TryResolveStableReferenceSubject(_bindings, _resets, reference, out ResetSubject after, out string afterDiagnostic), afterDiagnostic);
            Assert.AreNotEqual(before.SubjectId, after.SubjectId);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out StableObjectBinding reloadedBinding, out _, out string bindingDiagnostic), bindingDiagnostic);
            Assert.AreSame(second, reloadedBinding.PhysicalObject);
            Assert.AreEqual(_ownerA, after.Owner);
        }

        [Test]
        public void StableReference_RequiresCurrentRegisteredResettable()
        {
            const string id = "qa.stable.unregistered";
            GameObject target = CreateEntry(id, _routeA, true);
            Register(_ownerA, target);
            StableObjectReference reference = StableObjectReference.ForEntry(Id(id));

            Assert.IsFalse(ResetTargetResolver.TryResolveStableReferenceSubject(_bindings, _resets, reference, out _, out string diagnostic));
            Assert.IsNotEmpty(diagnostic);
            Assert.IsFalse(target.GetComponent<Resettable>().IsRegistered);
            Assert.AreEqual(0, _resets.SubjectCount);
        }

        [Test]
        public void StableReferenceTarget_LeavesObjectAndCompositionKindsUnchanged()
        {
            Resettable resettable = CreateEntry("qa.stable.target-kinds", _routeA, true).GetComponent<Resettable>();
            ResetComposition composition = Create("Composition").AddComponent<ResetComposition>();
            Assert.AreEqual(ResetTargetKind.Object, ResetTarget.ForObject(resettable).Kind);
            Assert.AreEqual(ResetTargetKind.Composition, ResetTarget.ForComposition(composition).Kind);
            Assert.AreEqual(ResetTargetKind.StableReference,
                ResetTarget.ForStableReference(StableObjectReference.ForEntry(Id("qa.stable.target-kinds"))).Kind);
        }

        [Test]
        public void TypedOwnerSelectors_DeriveIdentityAndDefinitionFromReferencedAssets()
        {
            StableObjectReference routeReference = StableObjectReference.ForRoute(Id("qa.stable.typed"), _routeA);
            Assert.IsTrue(routeReference.TryGetOwnerSelector(out FrameworkIdentityKey? routeKey, out RuntimeDefinitionToken? routeToken));
            Assert.AreEqual(OwnerKey(_routeA), routeKey.Value);
            Assert.AreEqual(RuntimeDefinitionToken.FromUnityObject(_routeA), routeToken.Value);

            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            _assets.Add(activity);
            var activitySerialized = new SerializedObject(activity);
            activitySerialized.FindProperty("activityId").stringValue = "qa.stable.activity";
            activitySerialized.ApplyModifiedPropertiesWithoutUndo();
            StableObjectReference activityReference = StableObjectReference.ForActivity(Id("qa.stable.typed"), activity);
            Assert.IsTrue(activityReference.TryGetOwnerSelector(out FrameworkIdentityKey? activityKey, out RuntimeDefinitionToken? activityToken));
            Assert.AreEqual(FrameworkIdentityKey.From(activity.ActivityId), activityKey.Value);
            Assert.AreEqual(RuntimeDefinitionToken.FromUnityObject(activity), activityToken.Value);
        }

        [Test]
        public void OwnerAwareAdmission_UsesTransactionOwnerWithoutDeclarationOwnerFields()
        {
            const string id = "qa.stable.transaction-owner";
            GameObject entry = CreateEntry(id, _routeA, true);
            Register(_ownerB, entry);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), OwnerKey(_routeB), RuntimeDefinitionToken.FromUnityObject(_routeB),
                out StableObjectBinding binding, out _, out string diagnostic), diagnostic);
            Assert.AreEqual(_ownerB, binding.Owner);
        }

        [Test]
        public void ObjectEntryId_RemainsStableWhenDeclarationObjectIsRenamedOrMoved()
        {
            ObjectEntryDeclaration declaration = CreateEntry("qa.stable.rename-move", _routeA, false).GetComponent<ObjectEntryDeclaration>();
            ObjectEntryId before = declaration.ObjectEntryId;
            declaration.gameObject.name = "Renamed declaration";
            declaration.transform.position = new Vector3(4f, -2f, 9f);
            Assert.AreEqual(before, declaration.ObjectEntryId);
        }

        [Test]
        public void ObjectEntrySet_RejectsDuplicateIdsAsIdentityCollision()
        {
            ObjectEntryId id = Id("qa.stable.collision");
            FrameworkIdentityKey ownerIdentity = OwnerKey(_routeA);
            var first = new ObjectEntryDescriptor(id, ObjectEntryScope.Route, ObjectEntrySourceKind.SceneAuthored,
                ObjectEntryRequiredness.Required, "first", ownerIdentity);
            var second = new ObjectEntryDescriptor(id, ObjectEntryScope.Route, ObjectEntrySourceKind.SceneAuthored,
                ObjectEntryRequiredness.Required, "second", ownerIdentity);
            Assert.Throws<System.ArgumentException>(() => new ObjectEntrySet(new[] { first, second }));
        }

        private void Register(RuntimeContentOwner owner, params GameObject[] roots)
        {
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(owner, roots, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(owner, out string commitDiagnostic), commitDiagnostic);
        }

        private GameObject CreateEntry(string id, RouteAsset route, bool addResettable)
        {
            GameObject value = Create(id);
            ObjectEntryDeclaration declaration = value.AddComponent<ObjectEntryDeclaration>();
            declaration.ConfigureForQa(Id(id), ObjectEntryRequiredness.Required);
            if (addResettable)
            {
                value.AddComponent<Resettable>();
                value.AddComponent<UnityGameObjectActiveResetParticipant>();
            }
            return value;
        }

        private GameObject Create(string objectName)
        {
            var value = new GameObject(objectName);
            _objects.Add(value);
            return value;
        }

        private RouteAsset CreateRoute(string routeId)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            _assets.Add(route);
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static ObjectEntryId Id(string id) => ObjectEntryId.From(id);

        private static RuntimeContentOwner Owner(RouteAsset route) => RuntimeContentOwner.Route(
            route.RouteId.StableText, route.RouteName, RuntimeDefinitionToken.FromUnityObject(route));

        private static FrameworkIdentityKey OwnerKey(RouteAsset route) =>
            FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, route.RouteId.StableText);
    }
}
