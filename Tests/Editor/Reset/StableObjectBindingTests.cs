using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset.Tests
{
    public sealed partial class StableObjectBindingTests
    {
        private sealed class NonRecoverableDetachFailureMaterializer : IPartMaterializer
        {
            public Task<PartPreparationResult> PrepareAsync(
                PartDefinition definition,
                PartMaterializationContext context,
                CancellationToken cancellationToken) =>
                Task.FromResult(PartPreparationResult.Prepared(
                    new PartMaterialization(new object(), PartOwnership.Acquired)));

            public PartEffectResult Apply(
                PartMaterialization candidate,
                PartMaterialization previous,
                PartMaterializationContext context) => PartEffectResult.Applied();

            public PartEffectResult Detach(
                PartMaterialization current,
                PartMaterializationContext context) =>
                PartEffectResult.Failed("unrelated detach failure", false);

            public Task<PartReleaseResult> ReleaseAsync(
                PartMaterialization materialization,
                PartMaterializationContext context,
                CancellationToken cancellationToken) =>
                Task.FromResult(PartReleaseResult.Released());
        }

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
        public async Task ObjectEntryWithoutComposition_DoesNotAllocateComposerOrChangeIdentity()
        {
            const string id = "qa.stable.no-composition";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryId identity = root.GetComponent<ObjectEntryDeclaration>().ObjectEntryId;

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string prepareDiagnostic), prepareDiagnostic);
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out string commitDiagnostic), commitDiagnostic);
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.AreEqual(identity, root.GetComponent<ObjectEntryDeclaration>().ObjectEntryId);

            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public void InvalidExplicitComposition_FailsBeforeOwnerCommit()
        {
            GameObject root = CreateEntry("qa.stable.invalid-composition", _routeA, false);
            ObjectEntryCompositionProfile profile = ScriptableObject.CreateInstance<ObjectEntryCompositionProfile>();
            _assets.Add(profile);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);

            Assert.IsFalse(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic));
            StringAssert.Contains("composition", diagnostic.ToLowerInvariant());
            Assert.AreEqual(0, _bindings.BindingCount);
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
        }

        [Test]
        public async Task ValidComposition_MaterializesOnlyAfterCommitAndReleasePreservesRootAcrossReadmission()
        {
            const string id = "qa.stable.composition";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryDeclaration declaration = root.GetComponent<ObjectEntryDeclaration>();
            ObjectEntryId identity = declaration.ObjectEntryId;
            GameObject prefab = Create("qa-capability-prefab");
            ObjectEntryCompositionProfile profile = ScriptableObject.CreateInstance<ObjectEntryCompositionProfile>();
            _assets.Add(profile);
            AddCompositionPart(profile, "capability", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string prepareDiagnostic), prepareDiagnostic);
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out string commitDiagnostic), commitDiagnostic);
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _),
                "Profile-only B2 authoring must not receive dynamic contribution access by default.");
            Assert.AreEqual(0, root.transform.childCount, "Commit publishes the binding but does not materialize before the composition activation stage.");
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _));

            Assert.AreSame(root, declaration.gameObject);
            Assert.AreEqual(identity, declaration.ObjectEntryId);
            Assert.NotNull(root.transform.Find("qa-capability-prefab"));
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(identity, root, out StableObjectBinding admittedBinding, out _, out string recognizeDiagnostic), recognizeDiagnostic);
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(admittedBinding));

            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsFalse(_bindings.IsCompositionTargetCurrent(admittedBinding));

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out prepareDiagnostic), prepareDiagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out commitDiagnostic), commitDiagnostic);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(identity, root, out StableObjectBinding readmittedBinding, out _, out recognizeDiagnostic), recognizeDiagnostic);
            Assert.AreNotSame(admittedBinding, readmittedBinding);
            Assert.IsFalse(_bindings.IsCompositionTargetCurrent(admittedBinding));
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(readmittedBinding));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicOnlyComposition_GrantsAfterCommitAndOpensIndependentContributionsLazily()
        {
            const string id = "qa.stable.dynamic-only";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic), diagnostic);
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out string uncommittedIssue));
            StringAssert.Contains("commit", uncommittedIssue.ToLowerInvariant());
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.AreEqual(0, _bindings.CompositionBindingCount,
                "A profileless opt-in must not create a Composer during registration or commit.");
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _),
                "The owner activation seam has not published the committed occurrence grant yet.");

            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out diagnostic), diagnostic);
            Assert.AreEqual(0, _bindings.CompositionBindingCount,
                "Publishing a profileless grant must not allocate a Composer.");

            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution first, out diagnostic), diagnostic);
            Assert.AreEqual(1, _bindings.CompositionBindingCount);
            Assert.AreEqual(0, root.transform.childCount, "Opening an empty contribution does not materialize content.");
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution second, out diagnostic), diagnostic);

            Assert.AreNotSame(first, second, "Each consumer receives an independent Framework contribution handle.");
            Assert.AreEqual(0, first.Priority);
            Assert.AreEqual(10, second.Priority);
            Assert.AreEqual(1, _bindings.CompositionBindingCount,
                "Subsequent opens must reuse the exact occurrence Composer.");
            Assert.AreEqual(0, root.transform.childCount);

            ObjectEntryCompositionOperationResult firstClose = await first.CloseAsync();
            Assert.IsTrue(firstClose.IsSuccessful, firstClose.Diagnostic);
            Assert.IsTrue((await second.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicOnlyComposition_RejectsReservedBasePriorityWithoutAllocatingComposer()
        {
            GameObject root = CreateEntry("qa.stable.dynamic-reserved-priority", _routeA, false);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string diagnostic), diagnostic);

            Assert.IsFalse(grant.TryOpenContribution(int.MinValue, out _, out string issue));
            StringAssert.Contains("priority", issue.ToLowerInvariant());
            Assert.AreEqual(0, _bindings.CompositionBindingCount,
                "A rejected open must not leave an empty Composer context attached to the occurrence.");
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task ReadOnlyOccurrenceQuery_DoesNotHandOutCompositionAccessOrCreateComposer()
        {
            const string id = "qa.stable.dynamic-readonly-query";
            GameObject target = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = target.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);
            GameObject queryRoot = Create("qa-stable-dynamic-readonly-query-anchor");
            ObjectEntryOccurrenceQueryBinding query = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, target, queryRoot);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));

            ObjectEntryOccurrenceResolution resolution = query.Resolve(StableObjectReference.ForEntry(Id(id)));

            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, resolution.Status);
            Assert.AreSame(target, resolution.Occurrence);
            Assert.IsNull(typeof(ObjectEntryOccurrenceQueryBinding).GetMethod(
                nameof(ObjectEntryCompositionAuthoring.TryGetCompositionAccessGrant)));
            Assert.AreEqual(0, _bindings.CompositionBindingCount,
                "Resolving a physical occurrence must not open a Composer or grant write authority.");
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Assert.AreEqual(0, target.transform.childCount);
            Assert.IsTrue((await contribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicCompositionWithProfile_GrantWaitsForSuccessfulBaseActivation()
        {
            GameObject root = CreateEntry("qa.stable.dynamic-with-profile", _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-base-prefab");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _),
                "Owner commit alone is not proof that the authored Base is active.");
            Assert.AreEqual(0, root.transform.childCount);

            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out diagnostic), diagnostic);
            Assert.AreEqual(1, root.transform.childCount, "The B2 Base must be active before the grant is published.");
            Assert.IsTrue(grant.TryOpenContribution(5, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Assert.AreEqual(1, root.transform.childCount, "Opening a dynamic contribution must not materialize a part.");
            Assert.IsTrue((await contribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.AreEqual(0, root.transform.childCount);
        }

        [Test]
        public async Task DynamicCompositionWithProfile_BaseActivationFailureDoesNotGrantAccess()
        {
            GameObject root = CreateEntry("qa.stable.dynamic-base-failure", _routeA, false);
            ObjectEntryCompositionProfile profile = CreateCompositionProfile(
                "base",
                Create("qa-stable-dynamic-invalidated-base-prefab"));
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            SetProfilePartPrefab(profile, 0, null);

            string activationIssue = await _bindings.ActivateOwnerCompositionsAsync(_ownerA);

            StringAssert.Contains("activation failed", activationIssue.ToLowerInvariant());
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task ProfilelessDynamicGrant_RemainsEligibleWhenSiblingBaseActivationFails()
        {
            GameObject dynamicRoot = CreateEntry("qa.stable.dynamic-sibling", _routeA, false);
            ObjectEntryCompositionAuthoring dynamicAuthoring = dynamicRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(dynamicAuthoring, true);
            GameObject profileRoot = CreateEntry("qa.stable.dynamic-sibling-base", _routeA, false);
            ObjectEntryCompositionProfile profile = CreateCompositionProfile(
                "base",
                Create("qa-stable-sibling-base-prefab"));
            SetAuthoringProfile(profileRoot.AddComponent<ObjectEntryCompositionAuthoring>(), profile);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(
                _ownerA,
                new[] { dynamicRoot, profileRoot },
                out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            SetProfilePartPrefab(profile, 0, null);

            Assert.IsNotEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(dynamicAuthoring.TryGetCompositionAccessGrant(
                out ObjectEntryCompositionAccessGrant grant,
                out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Assert.AreEqual(0, dynamicRoot.transform.childCount);
            Assert.IsTrue((await contribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task ProfilelessDynamicGrant_RemainsEligibleWhenSiblingProfileWasDestroyed()
        {
            GameObject dynamicRoot = CreateEntry("qa.stable.dynamic-destroyed-sibling", _routeA, false);
            ObjectEntryCompositionAuthoring dynamicAuthoring = dynamicRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(dynamicAuthoring, true);
            GameObject profileRoot = CreateEntry("qa.stable.dynamic-destroyed-sibling-base", _routeA, false);
            ObjectEntryCompositionProfile profile = CreateCompositionProfile(
                "base",
                Create("qa-stable-destroyed-sibling-base-prefab"));
            SetAuthoringProfile(profileRoot.AddComponent<ObjectEntryCompositionAuthoring>(), profile);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(
                _ownerA,
                new[] { dynamicRoot, profileRoot },
                out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Object.DestroyImmediate(profile);

            string activationIssue = await _bindings.ActivateOwnerCompositionsAsync(_ownerA);

            StringAssert.Contains("profile", activationIssue.ToLowerInvariant());
            Assert.IsTrue(dynamicAuthoring.TryGetCompositionAccessGrant(
                out ObjectEntryCompositionAccessGrant grant,
                out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Assert.AreEqual(0, dynamicRoot.transform.childCount);
            Assert.IsTrue((await contribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DisabledProfilelessGrant_DoesNotStopSiblingBaseOrGrantPublication()
        {
            GameObject disabledRoot = CreateEntry("qa.stable.dynamic-disabled-sibling", _routeA, false);
            ObjectEntryCompositionAuthoring disabledAuthoring = disabledRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(disabledAuthoring, true);
            GameObject dynamicRoot = CreateEntry("qa.stable.dynamic-enabled-sibling", _routeA, false);
            ObjectEntryCompositionAuthoring dynamicAuthoring = dynamicRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(dynamicAuthoring, true);
            GameObject profileRoot = CreateEntry("qa.stable.dynamic-enabled-sibling-base", _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-enabled-sibling-base");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            SetAuthoringProfile(profileRoot.AddComponent<ObjectEntryCompositionAuthoring>(), profile);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(
                _ownerA,
                new[] { disabledRoot, dynamicRoot, profileRoot },
                out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            SetDynamicOptIn(disabledAuthoring, false);

            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));

            Assert.IsFalse(disabledAuthoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.IsTrue(dynamicAuthoring.TryGetCompositionAccessGrant(
                out ObjectEntryCompositionAccessGrant grant,
                out diagnostic), diagnostic);
            Assert.IsTrue(dynamicRoot.transform.childCount == 0);
            Assert.IsTrue(profileRoot.transform.Find(prefab.name) != null,
                "An opted-out profileless sibling must not abort a valid B2 Base activation.");
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Assert.IsTrue((await contribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }
        [Test]
        public async Task DynamicComposition_GrantAndHandleStayStaleAfterExactRootReadmission()
        {
            const string id = "qa.stable.dynamic-readmission";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant oldGrant, out string diagnostic), diagnostic);
            Assert.IsTrue(oldGrant.TryOpenContribution(0, out ObjectEntryCompositionContribution oldContribution, out diagnostic), diagnostic);

            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.IsFalse(oldGrant.TryOpenContribution(0, out _, out string staleOpenIssue));
            StringAssert.Contains("stale", staleOpenIssue.ToLowerInvariant());
            Assert.AreEqual(
                ObjectEntryCompositionOperationStatus.Stale,
                (await oldContribution.CloseAsync()).Status);

            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant newGrant, out diagnostic), diagnostic);
            Assert.AreNotSame(oldGrant, newGrant);
            Assert.IsFalse(oldGrant.TryOpenContribution(10, out _, out _));
            Assert.AreEqual(
                ObjectEntryCompositionOperationStatus.Stale,
                (await oldContribution.CloseAsync()).Status);
            Assert.IsTrue(newGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution newContribution, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await oldContribution.CloseAsync()).Status);
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsTrue((await newContribution.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicComposition_HostInvalidationAndDestroyedRootRejectAccess()
        {
            GameObject destroyedRoot = CreateEntry("qa.stable.dynamic-destroyed-root", _routeB, false);
            ObjectEntryCompositionAuthoring destroyedAuthoring = destroyedRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(destroyedAuthoring, true);
            Register(_ownerB, destroyedRoot);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerB));
            Assert.IsTrue(destroyedAuthoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant destroyedGrant, out string diagnostic), diagnostic);
            Object.DestroyImmediate(destroyedRoot);

            Assert.IsFalse(destroyedGrant.TryOpenContribution(0, out _, out string destroyedIssue));
            StringAssert.Contains("current", destroyedIssue.ToLowerInvariant());
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerB));

            GameObject hostRoot = CreateEntry("qa.stable.dynamic-host-invalidation", _routeA, false);
            ObjectEntryCompositionAuthoring hostAuthoring = hostRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(hostAuthoring, true);
            Register(_ownerA, hostRoot);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(hostAuthoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant hostGrant, out diagnostic), diagnostic);
            Assert.IsTrue(hostGrant.TryOpenContribution(0, out ObjectEntryCompositionContribution hostHandle, out diagnostic), diagnostic);

            _bindings.InvalidateQueryAccess();

            Assert.IsFalse(hostAuthoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.IsFalse(hostGrant.TryOpenContribution(0, out _, out string hostIssue));
            StringAssert.Contains("invalid", hostIssue.ToLowerInvariant());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await hostHandle.CloseAsync()).Status);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public void DynamicComposition_AmbiguousRegistrationNeverPublishesGrant()
        {
            const string id = "qa.stable.dynamic-ambiguous";
            GameObject firstRoot = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = firstRoot.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);
            GameObject duplicateRoot = CreateEntry(id, _routeA, false);

            Assert.IsFalse(_bindings.TryRegisterOwnerContent(
                _ownerA,
                new[] { firstRoot, duplicateRoot },
                out string diagnostic));
            StringAssert.Contains("ambiguous", diagnostic.ToLowerInvariant());
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.AreEqual(0, _bindings.BindingCount);
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
        }

        [Test]
        public async Task DynamicGrant_OptInMismatchInvalidatesGrantWithoutInvalidatingBaseComposition()
        {
            const string id = "qa.stable.dynamic-opt-in-latch";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-opt-in-base");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant oldGrant, out string diagnostic), diagnostic);
            Assert.IsTrue(oldGrant.TryOpenContribution(0, out ObjectEntryCompositionContribution oldHandle, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out diagnostic), diagnostic);

            SetDynamicOptIn(authoring, false);
            bool staleGrantWasCleared = !authoring.TryGetCompositionAccessGrant(out _, out _);
            bool oldGrantWasRejected = !oldGrant.TryOpenContribution(10, out _, out _);
            bool baseOccurrenceRemainsCurrent = _bindings.IsCompositionTargetCurrent(binding);
            bool basePartRemainsMaterialized = root.transform.Find(prefab.name) != null;
            bool baseRuntimeRemainsActive = binding.CompositionRuntime.IsActive;
            bool baseActivationRemainsSuccessful = string.IsNullOrEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));

            SetDynamicOptIn(authoring, true);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant newGrant, out diagnostic), diagnostic);

            Assert.IsTrue(staleGrantWasCleared);
            Assert.IsTrue(oldGrantWasRejected, "A transient opt-in mismatch must permanently stale the previous grant.");
            Assert.IsTrue(baseOccurrenceRemainsCurrent, "Dynamic access configuration must not alter the shared B2 target guard.");
            Assert.IsTrue(basePartRemainsMaterialized);
            Assert.IsTrue(baseRuntimeRemainsActive);
            Assert.IsTrue(baseActivationRemainsSuccessful, "The B2 Base remains successful while dynamic access is disabled.");
            Assert.AreNotSame(oldGrant, newGrant, "Restoring opt-in must publish a grant on a fresh context slot.");
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await oldHandle.CloseAsync()).Status);
            Assert.IsTrue(newGrant.TryOpenContribution(5, out ObjectEntryCompositionContribution newHandle, out diagnostic), diagnostic);
            Assert.IsTrue((await newHandle.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DestroyedAuthoredProfile_DoesNotBecomeProfilelessDynamicComposition()
        {
            GameObject root = CreateEntry("qa.stable.dynamic-destroyed-profile", _routeA, false);
            GameObject prefab = Create("qa-stable-destroyed-profile-prefab");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Object.DestroyImmediate(profile);

            string activationIssue = await _bindings.ActivateOwnerCompositionsAsync(_ownerA);

            StringAssert.Contains("profile", activationIssue.ToLowerInvariant());
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _),
                "A destroyed authored Base must not be treated as profileless opt-in.");
            Assert.AreEqual(0, _bindings.CompositionBindingCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicGrant_IsUnavailableWhileBaseActivationIsInFlight()
        {
            const string id = "qa.stable.dynamic-base-activation-pending";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-base-activation-pending");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out string diagnostic), diagnostic);

            var allowStagingCleanup = new TaskCompletionSource<bool>();
            async Task<bool> DestroyAfterGate(GameObject instance)
            {
                await allowStagingCleanup.Task;
                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            }

            var runtime = new ObjectEntryCompositionRuntimeBinding(
                binding,
                profile,
                candidate => _bindings.IsCompositionTargetCurrent(candidate),
                DestroyAfterGate,
                allowDynamicContributions: true);
            binding.CompositionRuntime = runtime;

            Task<string> activation = runtime.ActivateAsync();
            bool activationWasInFlight = !activation.IsCompleted;
            bool grantWasUnavailable = !authoring.TryGetCompositionAccessGrant(out _, out _);
            allowStagingCleanup.TrySetResult(true);
            string activationIssue = await activation;

            Assert.IsTrue(activationWasInFlight,
                "The controlled materializer gate must hold Base activation at an observable in-flight boundary.");
            Assert.IsTrue(grantWasUnavailable, "No grant exists while the exact Base runtime is still activating.");
            Assert.IsEmpty(activationIssue);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution handle, out diagnostic), diagnostic);
            Assert.IsTrue((await handle.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }
        [Test]
        public async Task DynamicGrantAndHandle_AreStaleDuringPendingOwnerRelease()
        {
            const string id = "qa.stable.dynamic-pending-release";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-pending-release-part");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution handle, out diagnostic), diagnostic);

            GameObject acquiredPart = root.transform.Find(prefab.name).gameObject;
            Transform originalRootParent = root.transform.parent;
            acquiredPart.transform.SetParent(null, true);
            root.transform.SetParent(acquiredPart.transform, false);

            string pendingIssue = await _bindings.ReleaseOwnerAsync(_ownerA);

            StringAssert.Contains("pending", pendingIssue.ToLowerInvariant());
            Assert.AreSame(root, acquiredPart.transform.GetChild(0).gameObject);
            Assert.IsFalse(authoring.TryGetCompositionAccessGrant(out _, out _));
            Assert.IsFalse(grant.TryOpenContribution(10, out _, out string staleOpenIssue));
            StringAssert.Contains("release", staleOpenIssue.ToLowerInvariant());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await handle.CloseAsync()).Status);
            Assert.AreEqual(1, _bindings.BindingCount, "The physical record remains until the owner release barrier completes.");
            Assert.IsTrue(root != null, "Pending cleanup must preserve the exact Object Entry root.");

            root.transform.SetParent(originalRootParent, false);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.IsTrue(root != null);
            Assert.IsTrue(acquiredPart == null);
        }

        [Test]
        public async Task DynamicGrantAndHandle_StayStaleWhenContextIsReplacedOnSameBinding()
        {
            const string id = "qa.stable.dynamic-context-replacement";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prefab = Create("qa-stable-dynamic-context-replacement-base");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant oldGrant, out string diagnostic), diagnostic);
            Assert.IsTrue(oldGrant.TryOpenContribution(0, out ObjectEntryCompositionContribution oldHandle, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out diagnostic), diagnostic);
            ObjectEntryCompositionRuntimeBinding oldRuntime = binding.CompositionRuntime;
            ObjectEntryDeclaration declaration = root.GetComponent<ObjectEntryDeclaration>();

            declaration.ConfigureForQa(Id("qa.stable.dynamic-context-replacement-stale"), ObjectEntryRequiredness.Required);
            string staleActivationIssue = await _bindings.ActivateOwnerCompositionsAsync(_ownerA);
            StringAssert.Contains("stale", staleActivationIssue.ToLowerInvariant());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await oldHandle.CloseAsync()).Status);

            declaration.ConfigureForQa(Id(id), ObjectEntryRequiredness.Required);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));

            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding reactivatedBinding, out _, out diagnostic), diagnostic);
            Assert.AreSame(binding, reactivatedBinding);
            Assert.AreNotSame(oldRuntime, binding.CompositionRuntime,
                "Reactivation after the stale binding is restored must replace the closed Composition context.");
            Assert.IsFalse(oldGrant.TryOpenContribution(10, out _, out _));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant newGrant, out diagnostic), diagnostic);
            Assert.AreNotSame(oldGrant, newGrant);
            Assert.IsTrue(newGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution newHandle, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, (await oldHandle.CloseAsync()).Status,
                "A handle from the replaced context must remain stale after a new contribution opens.");
            Assert.IsTrue((await newHandle.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task SynchronousOwnerRemoval_RejectionPreservesQueriesAndCompositionGrant()
        {
            const string id = "qa.stable.dynamic-sync-release-barrier";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetDynamicOptIn(authoring, true);
            ObjectEntryOccurrenceQueryBinding query = root.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution handle, out diagnostic), diagnostic);

            bool removed = _bindings.TryReleaseOwner(_ownerA, out string releaseDiagnostic);

            Assert.IsFalse(removed);
            StringAssert.Contains("async", releaseDiagnostic.ToLowerInvariant());
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(binding));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant retainedGrant, out diagnostic), diagnostic);
            Assert.AreSame(grant, retainedGrant);
            Assert.AreSame(root, query.Resolve(StableObjectReference.ForEntry(Id(id))).Occurrence,
                "A rejected synchronous removal must not unbind read-only occurrence queries.");

            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution secondHandle, out diagnostic), diagnostic);
            Assert.IsTrue((await handle.CloseAsync()).IsSuccessful);
            Assert.IsTrue((await secondHandle.CloseAsync()).IsSuccessful);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }
        [Test]
        public async Task AdoptedCompositionResource_RemainsOwnedByTheObjectEntryAfterComposerClose()
        {
            const string id = "qa.stable.adopted-composition";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prefab = Create("qa-adopted-prefab");
            GameObject adopted = Create("qa-adopted-instance");
            adopted.transform.SetParent(root.transform, false);
            adopted.SetActive(false);
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("adopted", prefab);
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out diagnostic), diagnostic);

            Object.DestroyImmediate(adopted);
            var destroyedAdoptionMaterializer = new ObjectEntryCompositionPartMaterializer(
                candidate => _bindings.IsCompositionTargetCurrent(candidate));
            var destroyedAdoption = new ObjectEntryCompositionPartMaterializer.PartBinding(
                binding,
                profile.Parts[0].Prefab,
                adopted);
            var destroyedAdoptionComposer = new CompositionComposer();
            CompositionContribution destroyedAdoptionContribution = destroyedAdoptionComposer.CreateContribution(
                CompositionOwnerId.Create("qa-stable-destroyed-adopted-composition"),
                0);
            CompositionResult destroyedAdoptionResult = await destroyedAdoptionContribution.SetPartWhileCurrentAsync(
                new PartDefinition(ContentKey.Create("adopted"), destroyedAdoption, destroyedAdoptionMaterializer),
                () => _bindings.IsCompositionTargetCurrent(binding));

            Assert.AreNotEqual(CompositionResultStatus.Committed, destroyedAdoptionResult.Status);
            Assert.IsNotEmpty(destroyedAdoptionResult.Issues,
                "A destroyed adopted instance must fail explicitly instead of falling back to its prefab.");
            Assert.AreEqual(0, root.transform.childCount,
                "A destroyed adopted instance must not cause a prefab-backed part to be instantiated.");
            await destroyedAdoptionComposer.CloseAsync();

            adopted = Create("qa-adopted-instance-replacement");
            adopted.transform.SetParent(root.transform, false);
            adopted.SetActive(false);
            var materializer = new ObjectEntryCompositionPartMaterializer(
                candidate => _bindings.IsCompositionTargetCurrent(candidate));
            var part = new ObjectEntryCompositionPartMaterializer.PartBinding(
                binding,
                profile.Parts[0].Prefab,
                adopted);
            var composer = new CompositionComposer();
            CompositionContribution contribution = composer.CreateContribution(
                CompositionOwnerId.Create("qa-stable-adopted-composition"),
                0);
            var definition = new PartDefinition(ContentKey.Create("adopted"), part, materializer);

            CompositionResult setResult = await contribution.SetPartWhileCurrentAsync(
                definition,
                () => _bindings.IsCompositionTargetCurrent(binding));
            Assert.AreEqual(CompositionResultStatus.Committed, setResult.Status);

            CompositionResult closeResult = await composer.CloseAsync();

            Assert.AreEqual(CompositionResultStatus.Committed, closeResult.Status);
            Assert.IsNotNull(adopted);
            Assert.AreSame(root.transform, adopted.transform.parent);
            Assert.IsFalse(adopted.activeSelf);
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out diagnostic), diagnostic);
        }

        [Test]
        public async Task PendingAcquiredPartCleanup_RetainsOwnerUntilRetryConfirmsDestruction()
        {
            const string id = "qa.stable.pending-composition-cleanup";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject prerequisitePrefab = Create("qa-pending-base-prefab");
            GameObject prefab = Create("qa-pending-dependent-prefab");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("base", prerequisitePrefab);
            AddCompositionPart(profile, "dependent", prefab, "base");
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            SetAuthoringProfile(authoring, profile);
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { root }, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(id), root, out StableObjectBinding binding, out _, out diagnostic), diagnostic);

            bool destructionPending = true;
            bool rootWasPassedToDestroy = false;
            var destroyedParts = new List<string>();
            Func<GameObject, Task<bool>> destroyAndConfirm = instance =>
            {
                if (object.ReferenceEquals(instance, root) ||
                    (instance != null && root != null && root.transform.IsChildOf(instance.transform)))
                {
                    rootWasPassedToDestroy = true;
                    return Task.FromResult(false);
                }

                if (destructionPending && instance.name == "qa-pending-dependent-prefab")
                {
                    return Task.FromResult(false);
                }

                if (instance.name == "qa-pending-dependent-prefab" ||
                    instance.name == "qa-pending-base-prefab")
                {
                    destroyedParts.Add(instance.name);
                }

                Object.DestroyImmediate(instance);
                return Task.FromResult(instance == null);
            };
            var composition = new ObjectEntryCompositionRuntimeBinding(
                binding,
                profile,
                candidate => _bindings.IsCompositionTargetCurrent(candidate),
                destroyAndConfirm);
            binding.CompositionRuntime = composition;
            Assert.IsEmpty(await composition.ActivateAsync());
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(binding));

            GameObject acquiredPart = root.transform.Find("qa-pending-dependent-prefab").gameObject;
            GameObject prerequisitePart = root.transform.Find("qa-pending-base-prefab").gameObject;
            Transform originalRootParent = root.transform.parent;
            acquiredPart.transform.SetParent(null, true);
            root.transform.SetParent(acquiredPart.transform, false);

            string pendingIssue = await _bindings.ReleaseOwnerAsync(_ownerA);

            StringAssert.Contains("pending", pendingIssue.ToLowerInvariant());
            Assert.AreEqual(1, _bindings.BindingCount, "The physical owner record must remain available for cleanup retry.");
            Assert.AreEqual(1, _bindings.UnresolvedCompositionCleanupCount);
            Assert.IsFalse(_bindings.IsCompositionTargetCurrent(binding), "Writes are invalidated before cleanup begins.");
            Assert.IsTrue(root != null, "A part that contains the Object Entry root must not be destroyed.");
            Assert.AreSame(root, binding.PhysicalObject);
            Assert.AreEqual(Id(id), root.GetComponent<ObjectEntryDeclaration>().ObjectEntryId);
            Assert.IsTrue(root.transform.IsChildOf(acquiredPart.transform));
            Assert.IsTrue(prerequisitePart != null,
                "A prerequisite must remain available while the dependent part contains the protected root.");
            Assert.IsEmpty(destroyedParts,
                "No acquired hierarchy may be reconciled while an acquired part contains the protected root.");
            Assert.IsFalse(rootWasPassedToDestroy,
                "Cleanup must refuse to pass an acquired part containing its protected root to Unity destruction.");

            root.transform.SetParent(originalRootParent, false);
            StringAssert.Contains(
                "pending",
                (await _bindings.ReleaseOwnerAsync(_ownerA)).ToLowerInvariant());
            Assert.IsTrue(root != null, "A pending part cleanup must preserve the original physical root.");
            Assert.IsTrue(prerequisitePart != null,
                "A prerequisite must remain while destruction of its dependent is still pending.");
            Assert.IsEmpty(destroyedParts,
                "A prerequisite must not be destroyed while its dependent is still pending.");
            Assert.IsFalse(rootWasPassedToDestroy);

            destructionPending = false;
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.IsTrue(root != null, "Retrying acquired-part cleanup must not destroy the declaration root.");
            Assert.IsTrue(acquiredPart == null, "The acquired part must be destroyed after cleanup can safely retry.");
            Assert.IsTrue(prerequisitePart == null, "The prerequisite must be destroyed after its dependent.");
            CollectionAssert.AreEqual(
                new[] { "qa-pending-dependent-prefab", "qa-pending-base-prefab" },
                destroyedParts);
            Assert.AreEqual(0, _bindings.BindingCount);
            Assert.AreEqual(0, _bindings.UnresolvedCompositionCleanupCount);
        }

        [Test]
        public async Task FaultedDetachWithoutProtectedRootEvidence_IsNotRecoverable()
        {
            var composer = new CompositionComposer();
            CompositionContribution contribution = composer.CreateContribution(
                CompositionOwnerId.Create("qa-nonrecoverable-detach-failure"),
                0);
            var materializer = new NonRecoverableDetachFailureMaterializer();
            var definition = new PartDefinition(
                ContentKey.Create("nonrecoverable-detach"),
                new object(),
                materializer);

            CompositionResult setResult = await contribution.SetPartAsync(definition);
            Assert.AreEqual(CompositionResultStatus.Committed, setResult.Status);

            CompositionResult closeResult = await composer.CloseAsync();

            Assert.AreEqual(CompositionResultStatus.Faulted, closeResult.Status);
            Assert.AreEqual(1, closeResult.Issues.Count);
            Assert.AreEqual(CompositionIssueCode.DetachFailed, closeResult.Issues[0].Code);
            StringAssert.Contains("unrelated detach failure", closeResult.Issues[0].Message);
            Assert.IsFalse(ObjectEntryCompositionRuntimeBinding.IsRecoverableCloseFailure(
                closeResult,
                composer.ContextId,
                false),
                "A DetachFailed result without the materializer's protected-root evidence must remain nonrecoverable.");
            Assert.IsFalse(ObjectEntryCompositionRuntimeBinding.IsRecoverableCloseFailure(
                closeResult,
                new CompositionComposer().ContextId,
                true),
                "A DetachFailed result from a different composition context must remain nonrecoverable.");
        }

        [Test]
        public async Task OwnerActivationRetry_ReconcilesPendingRootBeforeActivatingRemainingRoots()
        {
            const string firstId = "qa.stable.pending-activation-first";
            const string secondId = "qa.stable.pending-activation-second";
            GameObject firstRoot = CreateEntry(firstId, _routeA, false);
            GameObject secondRoot = CreateEntry(secondId, _routeA, false);
            ObjectEntryCompositionProfile firstProfile = CreateCompositionProfile(
                "pending-first",
                Create("qa-pending-first-prefab"));
            ObjectEntryCompositionProfile secondProfile = CreateCompositionProfile(
                "pending-second",
                Create("qa-pending-second-prefab"));
            SetAuthoringProfile(
                firstRoot.AddComponent<ObjectEntryCompositionAuthoring>(),
                firstProfile);
            SetAuthoringProfile(
                secondRoot.AddComponent<ObjectEntryCompositionAuthoring>(),
                secondProfile);
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(
                _ownerA,
                new[] { firstRoot, secondRoot },
                out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryRecognizeOccurrence(
                Id(firstId),
                firstRoot,
                out StableObjectBinding firstBinding,
                out _,
                out diagnostic), diagnostic);

            bool destructionPending = true;
            Func<GameObject, Task<bool>> destroyAndConfirm = instance =>
            {
                if (destructionPending && instance.name == "qa-pending-first-prefab")
                    return Task.FromResult(false);

                Object.DestroyImmediate(instance);
                return Task.FromResult(instance == null);
            };
            var firstRuntime = new ObjectEntryCompositionRuntimeBinding(
                firstBinding,
                firstProfile,
                candidate => _bindings.IsCompositionTargetCurrent(candidate),
                destroyAndConfirm);
            firstBinding.CompositionRuntime = firstRuntime;
            Assert.IsEmpty(await firstRuntime.ActivateAsync());
            Assert.IsNotEmpty(await firstRuntime.CloseAndReconcileAsync());
            Assert.IsFalse(firstRuntime.IsActive);
            Assert.IsFalse(firstRuntime.CleanupComplete);

            string pendingIssue = await _bindings.ActivateOwnerCompositionsAsync(_ownerA);

            StringAssert.Contains("recovery remains pending", pendingIssue);
            Assert.AreEqual(0, secondRoot.transform.childCount,
                "The next root must not activate while the previous root has unresolved acquired resources.");

            destructionPending = false;
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.NotNull(firstRoot.transform.Find("qa-pending-first-prefab"));
            Assert.NotNull(secondRoot.transform.Find("qa-pending-second-prefab"));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public void MissingBinding_IsNotFound()
        {
            Assert.IsFalse(_bindings.TryResolve(ObjectEntryId.From("qa.stable.missing"), null, null, out _, out var status, out _));
            Assert.AreEqual(StableObjectBindingResolutionStatus.NotFound, status);
        }

        [Test]
        public void RecognitionQuery_NoDeclarationIsUnrecognizedWithoutMutation()
        {
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);
            GameObject candidateRoot = Create("ordinary-object");
            BoxCollider collider = candidateRoot.AddComponent<BoxCollider>();
            int bindingCount = _bindings.BindingCount;
            int componentCount = candidateRoot.GetComponents<Component>().Length;

            ObjectEntryRecognitionResult result = InvokeRecognition(anchor, collider);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Unrecognized, RecognitionStatus(result));
            Assert.IsFalse(result.ObjectEntryId.IsValid);
            Assert.IsNull(result.Occurrence);
            Assert.AreEqual(bindingCount, _bindings.BindingCount);
            Assert.AreEqual(componentCount, candidateRoot.GetComponents<Component>().Length);
        }

        [Test]
        public void RecognitionQuery_InvalidNearestDeclarationDoesNotFallBackToCommittedAncestor()
        {
            const string outerId = "qa.stable.recognition-outer";
            GameObject outerRoot = CreateEntry(outerId, _routeB, false);
            Register(_ownerB, outerRoot);
            GameObject innerRoot = Create("invalid-inner");
            innerRoot.transform.SetParent(outerRoot.transform, false);
            ObjectEntryDeclaration invalidNearest = innerRoot.AddComponent<ObjectEntryDeclaration>();
            invalidNearest.ConfigureForQa(default, ObjectEntryRequiredness.Required);
            BoxCollider collider = innerRoot.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);

            ObjectEntryRecognitionResult result = InvokeRecognition(anchor, collider);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Invalid, RecognitionStatus(result));
            Assert.IsFalse(result.ObjectEntryId.IsValid);
            Assert.IsNull(result.Occurrence);
        }

        [Test]
        public void RecognitionQuery_DescendantCollidersResolveToTheExactDeclarationRoot()
        {
            const string id = "qa.stable.recognition-descendants";
            GameObject targetRoot = CreateEntry(id, _routeA, false);
            GameObject firstChild = Create("first-collider");
            firstChild.transform.SetParent(targetRoot.transform, false);
            BoxCollider firstCollider = firstChild.AddComponent<BoxCollider>();
            GameObject secondChild = Create("second-collider");
            secondChild.transform.SetParent(targetRoot.transform, false);
            BoxCollider secondCollider = secondChild.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, targetRoot, queryRoot);
            int bindingCount = _bindings.BindingCount;

            ObjectEntryRecognitionResult firstResult = InvokeRecognition(anchor, firstCollider);
            ObjectEntryRecognitionResult secondResult = InvokeRecognition(anchor, secondCollider);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(firstResult));
            Assert.IsTrue(firstResult.Succeeded);
            Assert.AreEqual(Id(id), firstResult.ObjectEntryId);
            Assert.AreSame(targetRoot, firstResult.Occurrence);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(secondResult));
            Assert.AreEqual(firstResult.ObjectEntryId, secondResult.ObjectEntryId);
            Assert.AreSame(firstResult.Occurrence, secondResult.Occurrence);
            Assert.AreEqual(bindingCount, _bindings.BindingCount);
        }

        [Test]
        public void RecognitionQuery_DifferentRootUnderAnotherOwnerDoesNotMakeCandidateAmbiguous()
        {
            const string id = "qa.stable.recognition-distinct-roots";
            GameObject firstRoot = CreateEntry(id, _routeA, false);
            BoxCollider firstCollider = firstRoot.AddComponent<BoxCollider>();
            GameObject secondRoot = CreateEntry(id, _routeB, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, firstRoot, queryRoot);
            Register(_ownerB, secondRoot);

            ObjectEntryRecognitionResult recognition = InvokeRecognition(anchor, firstCollider);
            ObjectEntryOccurrenceResolution identityResolution =
                ((IObjectEntryOccurrenceQuery)anchor).Resolve(StableObjectReference.ForEntry(Id(id)));

            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(recognition));
            Assert.AreSame(firstRoot, recognition.Occurrence);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Ambiguous, identityResolution.Status);
        }

        [Test]
        public void RecognitionQuery_DuplicateIdWithinSameOwnerIsAmbiguousBeforeRootSelection()
        {
            const string id = "qa.stable.recognition-same-owner-duplicates";
            GameObject firstRoot = CreateEntry(id, _routeA, false);
            BoxCollider collider = firstRoot.AddComponent<BoxCollider>();
            GameObject secondRoot = CreateEntry(id, _routeA, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, firstRoot, secondRoot, queryRoot);

            ObjectEntryRecognitionResult result = InvokeRecognition(anchor, collider);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Ambiguous, RecognitionStatus(result));
            Assert.IsFalse(result.Succeeded);
            Assert.IsFalse(result.ObjectEntryId.IsValid);
            Assert.IsNull(result.Occurrence);
        }

        [Test]
        public void RecognitionQuery_SameIdAndRootUnderTwoOwnersIsAmbiguous()
        {
            const string id = "qa.stable.recognition-same-root-two-owners";
            GameObject targetRoot = CreateEntry(id, _routeA, false);
            BoxCollider collider = targetRoot.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, targetRoot, queryRoot);
            Register(_ownerB, targetRoot);

            ObjectEntryRecognitionResult result = InvokeRecognition(anchor, collider);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Ambiguous, RecognitionStatus(result));
            Assert.IsNull(result.Occurrence);
        }

        [Test]
        public void RecognitionQuery_RequiresCommittedTargetAndRespectsTargetRelease()
        {
            const string id = "qa.stable.recognition-target-lifetime";
            GameObject targetRoot = CreateEntry(id, _routeB, false);
            BoxCollider collider = targetRoot.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerB, new[] { targetRoot }, out string diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Unavailable, RecognitionStatus(InvokeRecognition(anchor, collider)));
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerB, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(InvokeRecognition(anchor, collider)));
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerB, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Unavailable, RecognitionStatus(InvokeRecognition(anchor, collider)));
        }

        [Test]
        public void RecognitionQuery_RollbackAndAnchorReleaseInvalidateAccessIndependently()
        {
            const string id = "qa.stable.recognition-anchor-lifetime";
            GameObject targetRoot = CreateEntry(id, _routeB, false);
            BoxCollider collider = targetRoot.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerB, new[] { targetRoot }, out string diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerB, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(InvokeRecognition(anchor, collider)));

            Assert.IsTrue(_bindings.TryRollbackOwner(_ownerB, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Unavailable, RecognitionStatus(InvokeRecognition(anchor, collider)));
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerB, new[] { targetRoot }, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerB, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Resolved, RecognitionStatus(InvokeRecognition(anchor, collider)));

            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Unavailable, RecognitionStatus(InvokeRecognition(anchor, collider)));
            Assert.IsTrue(_bindings.TryResolve(Id(id), null, null, out _, out _, out diagnostic), diagnostic);
        }

        [Test]
        public void RecognitionQuery_DestroyedCandidateIsUnavailableAndNullInputIsInvalid()
        {
            const string id = "qa.stable.recognition-destroyed-candidate";
            GameObject targetRoot = CreateEntry(id, _routeB, false);
            BoxCollider collider = targetRoot.AddComponent<BoxCollider>();
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);
            Register(_ownerB, targetRoot);

            Assert.AreEqual(ObjectEntryRecognitionStatus.Invalid, RecognitionStatus(InvokeRecognition(anchor, null)));
            Object.DestroyImmediate(collider);
            Assert.AreEqual(ObjectEntryRecognitionStatus.Unavailable, RecognitionStatus(InvokeRecognition(anchor, collider)));
        }

        [Test]
        public void OccurrenceQuery_ReportsInvalidUnavailableAndResolvedWithoutChangingBindings()
        {
            const string id = "qa.stable.public-query";
            GameObject target = CreateEntry(id, _routeA, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            IObjectEntryOccurrenceQuery query = anchor;
            StableObjectReference reference = StableObjectReference.ForEntry(Id(id));

            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.InvalidReference, query.Resolve(default).Status);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(reference).Status);

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { target, queryRoot }, out string diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(reference).Status);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);

            int bindingCount = _bindings.BindingCount;
            ObjectEntryOccurrenceResolution resolved = query.Resolve(reference);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, resolved.Status);
            Assert.AreSame(target, resolved.Occurrence);
            Assert.IsTrue(resolved.Succeeded);
            Assert.AreEqual(bindingCount, _bindings.BindingCount);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.InvalidReference, query.Resolve(default).Status);
        }

        [Test]
        public void OccurrenceQuery_AmbiguityRequiresAnExactOwnerSelector()
        {
            const string id = "qa.stable.public-query-ambiguous";
            GameObject first = CreateEntry(id, _routeA, false);
            GameObject second = CreateEntry(id, _routeB, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, first, queryRoot);
            Register(_ownerB, second);

            IObjectEntryOccurrenceQuery query = anchor;
            ObjectEntryOccurrenceResolution ambiguous =
                query.Resolve(StableObjectReference.ForEntry(Id(id)));
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Ambiguous, ambiguous.Status);
            Assert.IsNull(ambiguous.Occurrence);
            Assert.IsFalse(ambiguous.Succeeded);

            ObjectEntryOccurrenceResolution selected = query.Resolve(StableObjectReference.ForRoute(Id(id), _routeA));
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, selected.Status);
            Assert.AreSame(first, selected.Occurrence);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OccurrenceQuery_ActivityRollbackOrReleaseMakesRouteScopedQueryUnavailable(bool rollback)
        {
            const string id = "qa.stable.public-query-activity";
            GameObject queryRoot = Create("route-query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, queryRoot);

            GameObject routeTarget = CreateEntry(id, _routeB, false);
            Register(_ownerB, routeTarget);
            ActivityAsset activity = CreateActivity("qa.stable.activity");
            RuntimeContentOwner activityOwner = RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                RuntimeDefinitionToken.FromUnityObject(activity));
            GameObject target = CreateEntry(id, _routeA, false);
            StableObjectReference activityReference = StableObjectReference.ForActivity(Id(id), activity);
            StableObjectReference unscopedReference = StableObjectReference.ForEntry(Id(id));
            IObjectEntryOccurrenceQuery query = anchor;

            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, query.Resolve(unscopedReference).Status);
            Assert.AreSame(routeTarget, query.Resolve(unscopedReference).Occurrence);
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(activityOwner, new[] { target }, out string diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(activityReference).Status);
            Assert.IsTrue(_bindings.TryCommitOwner(activityOwner, out diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, query.Resolve(activityReference).Status);
            Assert.AreSame(target, query.Resolve(activityReference).Occurrence);
            ObjectEntryOccurrenceResolution ambiguous = query.Resolve(unscopedReference);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Ambiguous, ambiguous.Status);
            Assert.IsNull(ambiguous.Occurrence);

            bool activityOwnerClosed = rollback
                ? _bindings.TryRollbackOwner(activityOwner, out diagnostic)
                : _bindings.TryReleaseOwner(activityOwner, out diagnostic);
            Assert.IsTrue(activityOwnerClosed, diagnostic);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(activityReference).Status);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, query.Resolve(unscopedReference).Status);
            Assert.AreSame(routeTarget, query.Resolve(unscopedReference).Occurrence);

            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerB, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out diagnostic), diagnostic);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OccurrenceQuery_QueryOwnerRollbackOrReleaseUnbindsWhileTargetRemains(bool rollback)
        {
            const string id = "qa.stable.public-query-owner-close";
            GameObject target = CreateEntry(id, _routeB, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerB, target);
            Register(_ownerA, queryRoot);
            IObjectEntryOccurrenceQuery query = anchor;
            StableObjectReference reference = StableObjectReference.ForEntry(Id(id));

            Assert.AreSame(target, query.Resolve(reference).Occurrence);

            bool queryOwnerClosed = rollback
                ? _bindings.TryRollbackOwner(_ownerA, out string diagnostic)
                : _bindings.TryReleaseOwner(_ownerA, out diagnostic);
            Assert.IsTrue(queryOwnerClosed, diagnostic);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(reference).Status);
            Assert.IsTrue(_bindings.TryResolve(Id(id), null, null, out StableObjectBinding remaining, out _, out diagnostic), diagnostic);
            Assert.AreSame(target, remaining.PhysicalObject);
        }

        [Test]
        public void OccurrenceQuery_DestroyedObjectIsUnavailableWithoutPruningTheBinding()
        {
            const string id = "qa.stable.public-query-destroyed";
            GameObject target = CreateEntry(id, _routeA, false);
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            Register(_ownerA, target, queryRoot);

            int bindingCount = _bindings.BindingCount;
            Object.DestroyImmediate(target);
            ObjectEntryOccurrenceResolution resolution = ((IObjectEntryOccurrenceQuery)anchor)
                .Resolve(StableObjectReference.ForEntry(Id(id)));

            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, resolution.Status);
            Assert.AreEqual(bindingCount, _bindings.BindingCount);
        }

        [Test]
        public void OccurrenceQuery_DestroyedAnchorBeforeCommitDoesNotBlockOwnerContent()
        {
            const string id = "qa.stable.public-query-destroyed-anchor";
            GameObject target = CreateEntry(id, _routeA, false);
            GameObject queryRoot = Create("query-anchor");
            queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();

            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, new[] { target, queryRoot }, out string diagnostic), diagnostic);
            Object.DestroyImmediate(queryRoot);

            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.TryResolve(Id(id), null, null, out StableObjectBinding binding, out _, out diagnostic), diagnostic);
            Assert.AreSame(target, binding.PhysicalObject);
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
            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding queryAnchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            IObjectEntryOccurrenceQuery query = queryAnchor;
            GameObject first = CreateEntry(id, _routeA, true);
            Register(_ownerA, first, queryRoot);
            Assert.AreSame(first, query.Resolve(reference).Occurrence);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, new[] { first }, "test", "first", out string resetDiagnostic), resetDiagnostic);
            Assert.IsTrue(ResetTargetResolver.TryResolveStableObjectSubject(_bindings, _resets, reference, out ResetSubject before, out string beforeDiagnostic), beforeDiagnostic);

            Assert.IsTrue(_resetRegistration.TryReleaseOwner(_ownerA, "test", "unload", out string resetReleaseDiagnostic), resetReleaseDiagnostic);
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out string bindingReleaseDiagnostic), bindingReleaseDiagnostic);
            Assert.IsFalse(ResetTargetResolver.TryResolveStableObjectSubject(_bindings, _resets, reference, out _, out _));
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Unavailable, query.Resolve(reference).Status);

            GameObject second = CreateEntry(id, _routeA, true);
            Register(_ownerA, second, queryRoot);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, new[] { second }, "test", "reload", out resetDiagnostic), resetDiagnostic);
            Assert.IsTrue(ResetTargetResolver.TryResolveStableObjectSubject(_bindings, _resets, reference, out ResetSubject after, out string afterDiagnostic), afterDiagnostic);
            Assert.AreNotEqual(before.SubjectId, after.SubjectId);
            Assert.IsTrue(_bindings.TryResolve(ObjectEntryId.From(id), null, null, out StableObjectBinding reloadedBinding, out _, out string bindingDiagnostic), bindingDiagnostic);
            Assert.AreSame(second, reloadedBinding.PhysicalObject);
            Assert.AreSame(second, query.Resolve(reference).Occurrence);
            Assert.AreEqual(_ownerA, after.Owner);
        }

        [Test]
        public void StableReference_RequiresCurrentRegisteredResettable()
        {
            const string id = "qa.stable.unregistered";
            GameObject target = CreateEntry(id, _routeA, true);
            Register(_ownerA, target);
            StableObjectReference reference = StableObjectReference.ForEntry(Id(id));

            Assert.IsFalse(ResetTargetResolver.TryResolveStableObjectSubject(_bindings, _resets, reference, out _, out string diagnostic));
            Assert.IsNotEmpty(diagnostic);
            Assert.IsFalse(target.GetComponent<Resettable>().IsRegistered);
            Assert.AreEqual(0, _resets.SubjectCount);
        }

        [Test]
        public void StableObjectTarget_RejectsBoundOccurrenceWithoutResettable()
        {
            const string id = "qa.stable.no-resettable";
            GameObject target = CreateEntry(id, _routeA, false);
            Register(_ownerA, target);
            StableObjectReference reference = StableObjectReference.ForRoute(Id(id), _routeA);

            Assert.IsFalse(ResetTargetResolver.TryResolveStableObjectSubject(
                _bindings, _resets, reference, out _, out string diagnostic));
            StringAssert.Contains("without a Resettable", diagnostic);
        }

        [Test]
        public void StableCompositionTarget_ResolvesMembersOnBoundOccurrence()
        {
            const string id = "qa.stable.composition";
            GameObject root = Create(id);
            ObjectEntryDeclaration declaration = root.AddComponent<ObjectEntryDeclaration>();
            declaration.ConfigureForQa(Id(id), ObjectEntryRequiredness.Required);
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable member = AddResettable(root.transform, "Member");
            SetExplicitMembers(composition, member);
            var ownerRoots = new[] { root };
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, ownerRoots, out string bindingDiagnostic), bindingDiagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out bindingDiagnostic), bindingDiagnostic);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, ownerRoots, "test", "composition", out string resetDiagnostic), resetDiagnostic);
            StableObjectReference reference = StableObjectReference.ForRoute(Id(id), _routeA);

            Assert.IsTrue(ResetTargetResolver.TryResolveStableCompositionSubjects(
                _bindings, _resets, reference, new[] { _ownerA }, out IReadOnlyList<ResetSubjectId> ids,
                out _, out string diagnostic), diagnostic);
            CollectionAssert.AreEqual(new[] { member.RuntimeSubjectId }, ids);
        }

        [Test]
        public void StableCompositionTarget_RejectsBoundOccurrenceWithoutComposition()
        {
            const string id = "qa.stable.no-composition";
            GameObject root = CreateEntry(id, _routeA, true);
            Register(_ownerA, root);
            StableObjectReference reference = StableObjectReference.ForRoute(Id(id), _routeA);

            Assert.IsFalse(ResetTargetResolver.TryResolveStableCompositionSubjects(
                _bindings, _resets, reference, new[] { _ownerA }, out _, out _, out string diagnostic));
            StringAssert.Contains("without a ResetComposition", diagnostic);
        }

        [Test]
        public void StableCompositionTarget_ReloadResolvesNewPhysicalOccurrenceAndMembers()
        {
            const string id = "qa.stable.composition-reload";
            StableObjectReference reference = StableObjectReference.ForRoute(Id(id), _routeA);
            GameObject firstRoot = Create(id);
            ObjectEntryDeclaration declaration = firstRoot.AddComponent<ObjectEntryDeclaration>();
            declaration.ConfigureForQa(Id(id), ObjectEntryRequiredness.Required);
            ResetComposition firstComposition = firstRoot.AddComponent<ResetComposition>();
            Resettable firstMember = AddResettable(firstRoot.transform, "FirstMember");
            SetExplicitMembers(firstComposition, firstMember);
            RegisterStableCompositionOccurrence(firstRoot, "first");
            Assert.IsTrue(ResetTargetResolver.TryResolveStableCompositionSubjects(
                _bindings, _resets, reference, new[] { _ownerA }, out IReadOnlyList<ResetSubjectId> firstIds,
                out _, out string firstDiagnostic), firstDiagnostic);

            Assert.IsTrue(_resetRegistration.TryReleaseOwner(_ownerA, "test", "unload", out string resetRelease), resetRelease);
            Assert.IsTrue(_bindings.TryReleaseOwner(_ownerA, out string bindingRelease), bindingRelease);
            Assert.IsFalse(ResetTargetResolver.TryResolveStableCompositionSubjects(
                _bindings, _resets, reference, new[] { _ownerA }, out _, out _, out _));

            GameObject secondRoot = Create(id);
            ObjectEntryDeclaration secondDeclaration = secondRoot.AddComponent<ObjectEntryDeclaration>();
            secondDeclaration.ConfigureForQa(Id(id), ObjectEntryRequiredness.Required);
            ResetComposition secondComposition = secondRoot.AddComponent<ResetComposition>();
            Resettable secondMember = AddResettable(secondRoot.transform, "SecondMember");
            SetExplicitMembers(secondComposition, secondMember);
            RegisterStableCompositionOccurrence(secondRoot, "reload");
            Assert.IsTrue(ResetTargetResolver.TryResolveStableCompositionSubjects(
                _bindings, _resets, reference, new[] { _ownerA }, out IReadOnlyList<ResetSubjectId> secondIds,
                out _, out string secondDiagnostic), secondDiagnostic);

            Assert.AreNotEqual(firstIds[0], secondIds[0]);
            CollectionAssert.AreEqual(new[] { secondMember.RuntimeSubjectId }, secondIds);
        }

        [Test]
        public void StableReferenceTarget_LeavesObjectAndCompositionKindsUnchanged()
        {
            Resettable resettable = CreateEntry("qa.stable.target-kinds", _routeA, true).GetComponent<Resettable>();
            ResetComposition composition = Create("Composition").AddComponent<ResetComposition>();
            Assert.AreEqual(ResetTargetKind.Object, ResetTarget.ForObject(ResetObjectTarget.Direct(resettable)).Kind);
            Assert.AreEqual(ResetReferenceMode.Stable, ResetTarget.ForObject(
                ResetObjectTarget.Stable(StableObjectReference.ForEntry(Id("qa.stable.target-kinds")))).ObjectTarget.ReferenceMode);
            Assert.AreEqual(ResetTargetKind.Composition, ResetTarget.ForComposition(ResetCompositionTarget.Direct(composition)).Kind);
            Assert.AreEqual(ResetReferenceMode.Stable, ResetTarget.ForComposition(
                ResetCompositionTarget.Stable(StableObjectReference.ForEntry(Id("qa.stable.target-kinds")))).CompositionTarget.ReferenceMode);
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

        private static ObjectEntryRecognitionResult InvokeRecognition(
            ObjectEntryOccurrenceQueryBinding anchor,
            Collider candidate) =>
            ((IObjectEntryRecognitionQuery)anchor).Recognize(candidate);

        private static ObjectEntryRecognitionStatus RecognitionStatus(ObjectEntryRecognitionResult result) => result.Status;

        private void RegisterStableCompositionOccurrence(GameObject root, string reason)
        {
            var roots = new[] { root };
            Assert.IsTrue(_bindings.TryRegisterOwnerContent(_ownerA, roots, out string bindingDiagnostic), bindingDiagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_ownerA, out bindingDiagnostic), bindingDiagnostic);
            Assert.IsTrue(_resetRegistration.TryRegisterOwnerContent(_ownerA, roots, "test", reason, out string resetDiagnostic), resetDiagnostic);
        }

        private static void SetExplicitMembers(ResetComposition composition, params Resettable[] members)
        {
            var serialized = new SerializedObject(composition);
            serialized.FindProperty("memberMode").enumValueIndex = (int)ResetCompositionMemberMode.ExplicitMembers;
            SerializedProperty list = serialized.FindProperty("explicitMembers");
            list.arraySize = members.Length;
            for (int index = 0; index < members.Length; index++)
                list.GetArrayElementAtIndex(index).objectReferenceValue = members[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddCompositionPart(
            ObjectEntryCompositionProfile profile,
            string key,
            GameObject prefab,
            params string[] dependencies)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty entries = serialized.FindProperty("parts");
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            SerializedProperty dependencyList = element.FindPropertyRelative("dependencies");
            dependencyList.arraySize = dependencies.Length;
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                dependencyList.GetArrayElementAtIndex(dependencyIndex).stringValue = dependencies[dependencyIndex];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private ObjectEntryCompositionProfile CreateCompositionProfile(
            string key,
            GameObject prefab)
        {
            ObjectEntryCompositionProfile profile = ScriptableObject.CreateInstance<ObjectEntryCompositionProfile>();
            _assets.Add(profile);
            AddCompositionPart(profile, key, prefab);
            return profile;
        }

        private static void SetAuthoringProfile(
            ObjectEntryCompositionAuthoring authoring,
            ObjectEntryCompositionProfile profile)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDynamicOptIn(ObjectEntryCompositionAuthoring authoring, bool value)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("allowDynamicContributions").boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetProfilePartPrefab(
            ObjectEntryCompositionProfile profile,
            int partIndex,
            GameObject prefab)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("parts")
                .GetArrayElementAtIndex(partIndex)
                .FindPropertyRelative("prefab")
                .objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static Resettable AddResettable(Transform parent, string objectName)
        {
            var value = new GameObject(objectName);
            value.transform.SetParent(parent, false);
            return value.AddComponent<Resettable>();
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

        private ActivityAsset CreateActivity(string activityId)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            _assets.Add(activity);
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = activityId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static ObjectEntryId Id(string id) => ObjectEntryId.From(id);

        private static RuntimeContentOwner Owner(RouteAsset route) => RuntimeContentOwner.Route(
            route.RouteId.StableText, route.RouteName, RuntimeDefinitionToken.FromUnityObject(route));

        private static FrameworkIdentityKey OwnerKey(RouteAsset route) =>
            FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, route.RouteId.StableText);
    }
}
