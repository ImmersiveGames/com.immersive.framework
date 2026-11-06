using System;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset.Tests
{
    public sealed partial class StableObjectBindingTests
    {
        private string _dynamicPrefabFolder;

        private static async Task AssertSignalBeforeCompletionAsync(
            Task signal,
            Task operation,
            string diagnostic)
        {
            Task first = await Task.WhenAny(signal, operation);
            Assert.AreSame(signal, first, diagnostic);
        }

        private void ClearDynamicCompositionFixture()
        {
            ObjectEntryCompositionDisableProbe.Enabled = null;
            ObjectEntryCompositionDisableProbe.Disabled = null;

            try
            {
                if (!string.IsNullOrEmpty(_dynamicPrefabFolder))
                    AssetDatabase.DeleteAsset(_dynamicPrefabFolder);
            }
            finally
            {
                _dynamicPrefabFolder = null;
            }
        }

        [Test]
        public async Task DynamicContribution_SetPartAddsPartAndPreservesExactEntryIdentity()
        {
            const string id = "qa.stable.dynamic-add";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryDeclaration declaration = root.GetComponent<ObjectEntryDeclaration>();
            ObjectEntryId identity = declaration.ObjectEntryId;
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id, root);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);

            GameObject prefab = CreatePrefab("qa-dynamic-added-part");
            ObjectEntryCompositionOperationResult result = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("added", prefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, result.Status, result.Diagnostic);
            CollectionAssert.Contains(result.ChangedKeys, "added");
            Assert.AreSame(root, declaration.gameObject);
            Assert.AreEqual(identity, declaration.ObjectEntryId);
            Assert.AreSame(root, root.transform.Find(prefab.name).parent.gameObject);
            Assert.IsTrue(_bindings.TryResolve(identity, null, null, out StableObjectBinding binding, out _, out diagnostic), diagnostic);
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(binding));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            Assert.IsTrue(root != null);
        }

        [Test]
        public async Task DynamicContribution_SetPartReplacesOnlyItsSameKey()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-replace");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(5, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-first-part");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-second-part");

            ObjectEntryCompositionOperationResult first = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("visual", firstPrefab));
            ObjectEntryCompositionOperationResult replacement = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("visual", secondPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, first.Status, first.Diagnostic);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, replacement.Status, replacement.Diagnostic);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(secondPrefab.name));
            Assert.IsNull(root.transform.Find(firstPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_RemovePartRemovesOnlyItsOwnedKey()
        {
            const string id = "qa.stable.dynamic-remove";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution first, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution second, out diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-remove-first");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-remove-second");

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await first.SetPartAsync(new ObjectEntryCompositionPartDefinition("first", firstPrefab))).Status);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await second.SetPartAsync(new ObjectEntryCompositionPartDefinition("second", secondPrefab))).Status);
            ObjectEntryCompositionOperationResult removal = await first.RemovePartAsync("first");

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, removal.Status, removal.Diagnostic);
            Assert.IsNull(root.transform.Find(firstPrefab.name));
            Assert.IsNotNull(root.transform.Find(secondPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContributions_CloseOneIntentWithoutClosingOwnerOrSibling()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-independent-close");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution first, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution second, out diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-close-first");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-close-second");
            await first.SetPartAsync(new ObjectEntryCompositionPartDefinition("first", firstPrefab));
            await second.SetPartAsync(new ObjectEntryCompositionPartDefinition("second", secondPrefab));

            ObjectEntryCompositionOperationResult close = await first.CloseAsync();

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, close.Status, close.Diagnostic);
            Assert.IsNull(root.transform.Find(firstPrefab.name));
            Assert.IsNotNull(root.transform.Find(secondPrefab.name));
            Assert.AreEqual(1, _bindings.CompositionBindingCount);
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(ResolveBinding("qa.stable.dynamic-independent-close")));
            Assert.IsTrue(grant.TryOpenContribution(20, out ObjectEntryCompositionContribution third, out diagnostic), diagnostic);
            Assert.IsTrue((await third.CloseAsync()).Status is ObjectEntryCompositionOperationStatus.Committed or ObjectEntryCompositionOperationStatus.NoChange);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContributions_HigherPriorityWinsSameKey()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-priority");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution lower, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution higher, out diagnostic), diagnostic);
            GameObject lowerPrefab = CreatePrefab("qa-dynamic-lower-priority");
            GameObject higherPrefab = CreatePrefab("qa-dynamic-higher-priority");

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await lower.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", lowerPrefab))).Status);
            ObjectEntryCompositionOperationResult winning = await higher.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("shared", higherPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, winning.Status, winning.Diagnostic);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(higherPrefab.name));
            Assert.IsNull(root.transform.Find(lowerPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContributions_EqualHighestPriorityRejectsWithoutMaterialization()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-equal-priority");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution first, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution second, out diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-equal-first");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-equal-second");
            await first.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", firstPrefab));

            ObjectEntryCompositionOperationResult conflict = await second.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("shared", secondPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, conflict.Status);
            StringAssert.Contains("EqualPriorityConflict", conflict.Diagnostic);
            Assert.IsFalse(conflict.EffectsMayHaveApplied);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(firstPrefab.name));
            Assert.IsNull(root.transform.Find(secondPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_RemoveOverrideRestoresBasePart()
        {
            const string id = "qa.stable.dynamic-base-restore";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject basePrefab = Create("qa-dynamic-base-restore");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("shared", basePrefab);
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id, root, profile);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject overridePrefab = CreatePrefab("qa-dynamic-override-restore");
            await contribution.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", overridePrefab));

            ObjectEntryCompositionOperationResult removal = await contribution.RemovePartAsync("shared");

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, removal.Status, removal.Diagnostic);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(basePrefab.name));
            Assert.IsNull(root.transform.Find(overridePrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_DependencyCanBeSatisfiedByAnotherContribution()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-cross-contribution-dependency");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution provider, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution consumer, out diagnostic), diagnostic);
            GameObject prerequisitePrefab = CreatePrefab("qa-dynamic-prerequisite");
            GameObject dependentPrefab = CreatePrefab("qa-dynamic-dependent");
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await provider.SetPartAsync(new ObjectEntryCompositionPartDefinition("prerequisite", prerequisitePrefab))).Status);

            ObjectEntryCompositionOperationResult dependent = await consumer.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("dependent", dependentPrefab, new[] { "prerequisite" }));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, dependent.Status, dependent.Diagnostic);
            Assert.AreEqual(2, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(prerequisitePrefab.name));
            Assert.IsNotNull(root.transform.Find(dependentPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_RejectsMissingAndCyclicDependencies()
        {
            const string id = "qa.stable.dynamic-dependency-validation";
            GameObject root = CreateEntry(id, _routeA, false);
            GameObject basePrefab = Create("qa-dynamic-cycle-base");
            ObjectEntryCompositionProfile profile = CreateCompositionProfile("foundation", basePrefab);
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id, root, profile);
            Assert.IsTrue(grant.TryOpenContribution(5, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject missingPrefab = CreatePrefab("qa-dynamic-missing-dependency");
            GameObject extensionPrefab = CreatePrefab("qa-dynamic-cycle-extension");
            GameObject cyclePrefab = CreatePrefab("qa-dynamic-cycle-foundation");

            ObjectEntryCompositionOperationResult missing = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("missing-dependent", missingPrefab, new[] { "not-present" }));
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, missing.Status);
            StringAssert.Contains("MissingDependency", missing.Diagnostic);
            Assert.IsFalse(missing.EffectsMayHaveApplied);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await contribution.SetPartAsync(new ObjectEntryCompositionPartDefinition(
                    "extension", extensionPrefab, new[] { "foundation" }))).Status);

            ObjectEntryCompositionOperationResult cycle = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("foundation", cyclePrefab, new[] { "extension" }));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, cycle.Status);
            StringAssert.Contains("DependencyCycle", cycle.Diagnostic);
            Assert.IsFalse(cycle.EffectsMayHaveApplied);
            Assert.IsNotNull(root.transform.Find(basePrefab.name));
            Assert.IsNotNull(root.transform.Find(extensionPrefab.name));
            Assert.IsNull(root.transform.Find(cyclePrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_RemoveOverrideRejectsTieHiddenByHigherWinner()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-hidden-tie-remove");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution firstLow, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution secondLow, out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution high, out diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-hidden-tie-low-a");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-hidden-tie-low-b");
            GameObject highPrefab = CreatePrefab("qa-dynamic-hidden-tie-high");
            await firstLow.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", firstPrefab));
            await high.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", highPrefab));
            await secondLow.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", secondPrefab));

            ObjectEntryCompositionOperationResult removal = await high.RemovePartAsync("shared");

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, removal.Status);
            StringAssert.Contains("EqualPriorityConflict", removal.Diagnostic);
            Assert.IsNotNull(root.transform.Find(highPrefab.name), "A rejected Remove must preserve the higher-priority winner.");
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_CloseReportsHiddenLowerPriorityTieAsDegraded()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-hidden-tie-close");
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution firstLow, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution secondLow, out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(10, out ObjectEntryCompositionContribution high, out diagnostic), diagnostic);
            GameObject firstPrefab = CreatePrefab("qa-dynamic-close-hidden-low-a");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-close-hidden-low-b");
            GameObject highPrefab = CreatePrefab("qa-dynamic-close-hidden-high");
            await firstLow.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", firstPrefab));
            await high.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", highPrefab));
            await secondLow.SetPartAsync(new ObjectEntryCompositionPartDefinition("shared", secondPrefab));

            ObjectEntryCompositionOperationResult close = await high.CloseAsync();

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Degraded, close.Status);
            StringAssert.Contains("EqualPriorityConflict", close.Diagnostic);
            Assert.IsTrue(close.EffectsMayHaveApplied);
            CollectionAssert.Contains(close.ChangedKeys, "shared");
            Assert.IsNull(root.transform.Find(highPrefab.name));
            Assert.AreEqual(0, root.transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_OldHandleCannotMutateReadmittedSameRoot()
        {
            const string id = "qa.stable.dynamic-stale-mutation";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryDeclaration declaration = root.GetComponent<ObjectEntryDeclaration>();
            ObjectEntryId identity = declaration.ObjectEntryId;
            ObjectEntryCompositionAccessGrant oldGrant = await RegisterDynamicEntry(id, root);
            Assert.IsTrue(oldGrant.TryOpenContribution(0, out ObjectEntryCompositionContribution oldHandle, out string diagnostic), diagnostic);
            GameObject oldPrefab = CreatePrefab("qa-dynamic-stale-old");
            await oldHandle.SetPartAsync(new ObjectEntryCompositionPartDefinition("old", oldPrefab));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));

            ObjectEntryCompositionAccessGrant newGrant = await RegisterDynamicEntry(id, root);
            Assert.IsTrue(newGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution newHandle, out diagnostic), diagnostic);
            ObjectEntryCompositionOperationResult stale = await oldHandle.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("new", CreatePrefab("qa-dynamic-stale-new")));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, stale.Status);
            Assert.IsTrue(_bindings.IsCompositionTargetCurrent(ResolveBinding(id)));
            Assert.AreSame(root, declaration.gameObject);
            Assert.AreEqual(identity, declaration.ObjectEntryId);
            Assert.AreEqual(0, root.transform.childCount);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await newHandle.SetPartAsync(new ObjectEntryCompositionPartDefinition(
                    "new", CreatePrefab("qa-dynamic-current-new")))).Status);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_SetDuringPendingOwnerReleaseReturnsStale()
        {
            const string id = "qa.stable.dynamic-mutation-during-release";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id, root);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject prefab = CreatePrefab("qa-dynamic-pending-release-part");
            await contribution.SetPartAsync(new ObjectEntryCompositionPartDefinition("part", prefab));
            GameObject acquired = root.transform.Find(prefab.name).gameObject;
            Transform originalParent = root.transform.parent;
            acquired.transform.SetParent(null, true);
            root.transform.SetParent(acquired.transform, false);

            string pendingIssue = await _bindings.ReleaseOwnerAsync(_ownerA);
            StringAssert.Contains("pending", pendingIssue.ToLowerInvariant());
            ObjectEntryCompositionOperationResult duringRelease = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("late", CreatePrefab("qa-dynamic-late-release-part")));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale, duringRelease.Status);
            Assert.AreEqual(1, _bindings.BindingCount);
            Assert.IsTrue(root != null);
            Assert.IsNull(root.transform.Find("qa-dynamic-late-release-part"));
            root.transform.SetParent(originalParent, false);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_CancellationDuringPreparationReturnsCancelled()
        {
            const string id = "qa.stable.dynamic-prepare-cancel";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            var preparationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var continuePreparation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            InstallDynamicRuntime(grant, async instance =>
            {
                if (instance != null && instance.name == "Object Entry Composition Preparation")
                {
                    preparationStarted.TrySetResult(true);
                    bool destroy = await continuePreparation.Task;
                    if (destroy && instance != null) Object.DestroyImmediate(instance);
                    return destroy && instance == null;
                }

                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            });
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            using var cancellation = new CancellationTokenSource();
            Task<ObjectEntryCompositionOperationResult> operation = null;
            try
            {
                operation = contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("cancelled", CreatePrefab("qa-dynamic-cancelled-part")),
                    cancellation.Token);
                await AssertSignalBeforeCompletionAsync(
                    preparationStarted.Task,
                    operation,
                    "Preparation must remain pending until the controlled destruction callback is reached.");
                cancellation.Cancel();
                continuePreparation.TrySetResult(true);

                ObjectEntryCompositionOperationResult result = await operation;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Cancelled, result.Status, result.Diagnostic);
                Assert.IsFalse(result.EffectsMayHaveApplied);
                Assert.AreEqual(0, GetGrantRoot(grant).transform.childCount);
                Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
            }
            finally
            {
                continuePreparation.TrySetResult(true);
                if (!cancellation.IsCancellationRequested) cancellation.Cancel();
                if (operation != null) await operation;
                await _bindings.ReleaseOwnerAsync(_ownerA);
            }
        }

        [Test]
        public async Task DynamicContribution_OwnerReleaseCancelsAndDrainsPreparationCleanup()
        {
            const string id = "qa.stable.dynamic-owner-release-during-preparation";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            var preparationCleanupStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowPreparationCleanup = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            InstallDynamicRuntime(grant, async instance =>
            {
                if (instance != null && instance.name == "Object Entry Composition Preparation")
                {
                    preparationCleanupStarted.TrySetResult(true);
                    bool destroy = await allowPreparationCleanup.Task;
                    if (destroy && instance != null) Object.DestroyImmediate(instance);
                    return destroy && instance == null;
                }

                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            });
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);

            Task<ObjectEntryCompositionOperationResult> operation = contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition(
                    "cancelled-by-owner-release",
                    CreatePrefab("qa-dynamic-owner-release-preparation")));
            Task<string> release = null;
            try
            {
                await AssertSignalBeforeCompletionAsync(
                    preparationCleanupStarted.Task,
                    operation,
                    "Preparation cleanup must reach its asynchronous release gate before the operation completes.");
                release = _bindings.ReleaseOwnerAsync(_ownerA);

                Assert.IsFalse(operation.IsCompleted,
                    "Owner release must keep the dynamic operation registered while asynchronous preparation cleanup is pending.");
                Assert.IsFalse(release.IsCompleted,
                    "Owner release must not remove the exact binding before preparation cleanup completes.");
                Assert.AreEqual(1, _bindings.BindingCount);
                Assert.IsFalse(grant.TryOpenContribution(1, out _, out _),
                    "Invalidation must revoke new contribution opens before the drain waits.");

                allowPreparationCleanup.TrySetResult(true);
                ObjectEntryCompositionOperationResult result = await operation;
                string releaseIssue = await release;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Cancelled, result.Status, result.Diagnostic);
                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.AreEqual(0, root.transform.childCount,
                    "The cancelled preparation must release its staged and acquired objects before owner removal.");
            }
            finally
            {
                allowPreparationCleanup.TrySetResult(true);
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                await operation;
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_ConsumerCancellationAndOwnerReleaseDrainTheSameOperation()
        {
            const string id = "qa.stable.dynamic-consumer-and-owner-cancellation";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            var preparationCleanupStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowPreparationCleanup = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            InstallDynamicRuntime(grant, async instance =>
            {
                if (instance != null && instance.name == "Object Entry Composition Preparation")
                {
                    preparationCleanupStarted.TrySetResult(true);
                    bool destroy = await allowPreparationCleanup.Task;
                    if (destroy && instance != null) Object.DestroyImmediate(instance);
                    return destroy && instance == null;
                }

                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            });
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);

            using var consumerCancellation = new CancellationTokenSource();
            Task<string> release = null;
            using CancellationTokenRegistration releaseOnConsumerCancellation =
                consumerCancellation.Token.Register(() => release = _bindings.ReleaseOwnerAsync(_ownerA));
            Task<ObjectEntryCompositionOperationResult> operation = null;
            try
            {
                operation = contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition(
                        "consumer-and-owner-cancelled",
                        CreatePrefab("qa-dynamic-consumer-owner-cancellation")),
                    consumerCancellation.Token);
                await AssertSignalBeforeCompletionAsync(
                    preparationCleanupStarted.Task,
                    operation,
                    "Preparation cleanup must remain in flight before consumer/owner cancellation overlap is tested.");

                consumerCancellation.Cancel();

                Assert.IsNotNull(release,
                    "The consumer cancellation callback must start exact-owner release in the same cancellation turn.");
                Assert.IsFalse(operation.IsCompleted,
                    "Consumer cancellation cannot discard an operation while its asynchronous cleanup is blocked.");
                Assert.IsFalse(release.IsCompleted,
                    "Owner release must drain the cancelled consumer operation to completion.");
                Assert.AreEqual(1, _bindings.BindingCount);

                allowPreparationCleanup.TrySetResult(true);
                ObjectEntryCompositionOperationResult result = await operation;
                string releaseIssue = await release;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Cancelled, result.Status, result.Diagnostic);
                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                allowPreparationCleanup.TrySetResult(true);
                if (!consumerCancellation.IsCancellationRequested) consumerCancellation.Cancel();
                if (operation != null) await operation;
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_OwnerReleaseDrainsPostCommitAsyncReplacementCleanup()
        {
            const string id = "qa.stable.dynamic-owner-release-after-commit";
            GameObject firstPrefab = CreatePrefab("qa-dynamic-owner-release-old");
            GameObject replacementPrefab = CreatePrefab("qa-dynamic-owner-release-new");
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            var oldReleaseStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowOldRelease = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            int oldReleaseCount = 0;
            InstallDynamicRuntime(grant, async instance =>
            {
                if (instance != null && instance.name == firstPrefab.name)
                {
                    int releaseNumber = System.Threading.Interlocked.Increment(ref oldReleaseCount);
                    if (releaseNumber == 1)
                    {
                        oldReleaseStarted.TrySetResult(true);
                        bool destroy = await allowOldRelease.Task;
                        if (destroy && instance != null) Object.DestroyImmediate(instance);
                        return destroy && instance == null;
                    }

                    Object.DestroyImmediate(instance);
                    return instance == null;
                }

                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            });
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("visual", firstPrefab))).Status);

            Task<ObjectEntryCompositionOperationResult> replacement = null;
            Task<string> release = null;
            try
            {
                replacement = contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("visual", replacementPrefab));
                await AssertSignalBeforeCompletionAsync(
                    oldReleaseStarted.Task,
                    replacement,
                    "The replacement's asynchronous post-commit release must begin before its task completes.");

                release = _bindings.ReleaseOwnerAsync(_ownerA);
                Task<string> repeatedRelease = _bindings.ReleaseOwnerAsync(_ownerA);

                Assert.AreSame(release, repeatedRelease,
                    "Concurrent release requests for one exact owner record must share one release flight.");
                Assert.IsFalse(replacement.IsCompleted,
                    "The replacement operation includes its pending post-commit release and must remain pending.");
                Assert.IsFalse(release.IsCompleted,
                    "Composer close and acquired-resource reconciliation must wait for the replacement operation's full cleanup task.");
                Assert.AreEqual(1, oldReleaseCount,
                    "Owner reconciliation must not race a second release against the same materialization.");
                Assert.AreEqual(1, _bindings.BindingCount,
                    "The exact binding remains authoritative while asynchronous replacement cleanup is pending.");

                allowOldRelease.TrySetResult(true);
                ObjectEntryCompositionOperationResult replacementResult = await replacement;
                string releaseIssue = await release;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                    replacementResult.Status, replacementResult.Diagnostic);
                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.IsTrue(root != null,
                    "Owner release must preserve the Object Entry root while it drains and closes subordinate parts.");
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                allowOldRelease.TrySetResult(true);
                if (replacement != null) await replacement;
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_OwnerReleaseDrainsMultipleIndependentOperations()
        {
            const string id = "qa.stable.dynamic-owner-release-multiple-operations";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            var bothPreparationsStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowPreparationCleanup = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            int preparationCleanupCount = 0;
            InstallDynamicRuntime(grant, async instance =>
            {
                if (instance != null && instance.name == "Object Entry Composition Preparation")
                {
                    if (System.Threading.Interlocked.Increment(ref preparationCleanupCount) == 2)
                        bothPreparationsStarted.TrySetResult(true);
                    bool destroy = await allowPreparationCleanup.Task;
                    if (destroy && instance != null) Object.DestroyImmediate(instance);
                    return destroy && instance == null;
                }

                if (instance != null) Object.DestroyImmediate(instance);
                return instance == null;
            });
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution firstContribution, out string diagnostic), diagnostic);
            Assert.IsTrue(grant.TryOpenContribution(
                1, out ObjectEntryCompositionContribution secondContribution, out diagnostic), diagnostic);

            Task<ObjectEntryCompositionOperationResult> first = null;
            Task<ObjectEntryCompositionOperationResult> second = null;
            Task<string> release = null;
            try
            {
                first = firstContribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("independent-first", CreatePrefab("qa-dynamic-independent-first")));
                second = secondContribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("independent-second", CreatePrefab("qa-dynamic-independent-second")));
                Task bothOperationsFinished = Task.WhenAny(first, second);
                await AssertSignalBeforeCompletionAsync(
                    bothPreparationsStarted.Task,
                    bothOperationsFinished,
                    "Both independent operation cleanups must start before either operation completes.");

                release = _bindings.ReleaseOwnerAsync(_ownerA);

                Assert.IsFalse(first.IsCompleted);
                Assert.IsFalse(second.IsCompleted);
                Assert.IsFalse(release.IsCompleted,
                    "The owner drain must wait for every operation, including independent contribution/key closures.");
                Assert.AreEqual(2, preparationCleanupCount);

                allowPreparationCleanup.TrySetResult(true);
                ObjectEntryCompositionOperationResult[] results = await Task.WhenAll(first, second);
                string releaseIssue = await release;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Cancelled, results[0].Status, results[0].Diagnostic);
                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Cancelled, results[1].Status, results[1].Diagnostic);
                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                allowPreparationCleanup.TrySetResult(true);
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                if (first != null) await first;
                if (second != null) await second;
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_OwnerReleaseDuringOnEnableDrainsReentrantMutation()
        {
            const string id = "qa.stable.dynamic-owner-release-on-enable";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject prefab = CreatePrefab(
                "qa-dynamic-owner-release-on-enable",
                value => value.AddComponent<ObjectEntryCompositionDisableProbe>());
            Task<string> release = null;
            ObjectEntryCompositionDisableProbe.Enabled = _ =>
            {
                if (release == null)
                    release = _bindings.ReleaseOwnerAsync(_ownerA);
            };

            Task<ObjectEntryCompositionOperationResult> operation = null;
            try
            {
                operation = contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("callback", prefab));
                ObjectEntryCompositionOperationResult result = await operation;

                Assert.IsNotNull(release,
                    "The Unity OnEnable callback must start owner release while the exact mutation is still applying.");
                string releaseIssue = await release;

                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Degraded, result.Status, result.Diagnostic);
                Assert.IsTrue(result.EffectsMayHaveApplied,
                    "The callback invalidates the occurrence after Unity effects begin, so partial effects remain observable.");
                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.IsTrue(root != null,
                    "Reentrant owner release must preserve the declaration root while draining the callback operation.");
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                if (operation != null) await operation;
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_ReleaseFlightIsSharedDuringOnDisableCleanup()
        {
            const string id = "qa.stable.dynamic-owner-release-on-disable";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject prefab = CreatePrefab(
                "qa-dynamic-owner-release-on-disable",
                value => value.AddComponent<ObjectEntryCompositionDisableProbe>());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("callback", prefab))).Status);
            Task<string> repeatedRelease = null;
            ObjectEntryCompositionDisableProbe.Disabled = _ =>
            {
                if (repeatedRelease == null)
                    repeatedRelease = _bindings.ReleaseOwnerAsync(_ownerA);
            };

            Task<string> release = _bindings.ReleaseOwnerAsync(_ownerA);
            string releaseIssue = await release;

            Assert.IsNotNull(repeatedRelease,
                "The Unity OnDisable callback must reenter release while cleanup is in progress.");
            Assert.AreSame(release, repeatedRelease,
                "Reentrant release must observe the already-published exact-owner release flight.");
            Assert.IsEmpty(releaseIssue);
            Assert.AreEqual(0, _bindings.BindingCount);
            Assert.IsTrue(root != null);
            Assert.AreEqual(0, root.transform.childCount);
        }

        [Test]
        public async Task DynamicContribution_ConsumerCloseDrainsThroughOnDisableOwnerReleaseReentry()
        {
            const string id = "qa.stable.dynamic-consumer-close-on-disable";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            GameObject root = GetGrantRoot(grant);
            GameObject prefab = CreatePrefab(
                "qa-dynamic-consumer-close-on-disable",
                value => value.AddComponent<ObjectEntryCompositionDisableProbe>());
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed,
                (await contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("callback", prefab))).Status);

            Task<string> release = null;
            bool releaseWasPendingInsideCallback = false;
            int bindingCountInsideCallback = 0;
            ObjectEntryCompositionDisableProbe.Disabled = _ =>
            {
                if (release == null)
                {
                    release = _bindings.ReleaseOwnerAsync(_ownerA);
                    releaseWasPendingInsideCallback = !release.IsCompleted;
                    bindingCountInsideCallback = _bindings.BindingCount;
                }
            };
            Task<ObjectEntryCompositionOperationResult> close = null;
            try
            {
                close = contribution.CloseAsync();
                ObjectEntryCompositionOperationResult closeResult = await close;

                Assert.IsNotNull(release,
                    "OnDisable during consumer Close must reenter exact-owner release.");
                Assert.IsTrue(releaseWasPendingInsideCallback,
                    "The exact owner release must observe the consumer Close in the pending-operation ledger.");
                Assert.AreEqual(1, bindingCountInsideCallback,
                    "The exact owner record must remain registered during reentrant Close cleanup.");
                Assert.AreEqual(ObjectEntryCompositionOperationStatus.Degraded,
                    closeResult.Status, closeResult.Diagnostic);
                Assert.IsTrue(closeResult.EffectsMayHaveApplied,
                    "OnDisable invalidates the exact guard after detach effects begin.");
                string releaseIssue = await release;

                Assert.IsEmpty(releaseIssue);
                Assert.AreEqual(0, _bindings.BindingCount);
                Assert.IsTrue(root != null,
                    "Consumer close and owner release must preserve the declaration root.");
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                if (close != null) await close;
                if (release == null) release = _bindings.ReleaseOwnerAsync(_ownerA);
                await release;
            }
        }

        [Test]
        public async Task DynamicContribution_MaterializationFailureDoesNotReportSuccess()
        {
            const string id = "qa.stable.dynamic-materialization-failure";
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            InstallDynamicRuntime(grant, instance =>
            {
                if (instance != null) Object.DestroyImmediate(instance);
                return Task.FromResult(false);
            });
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);

            ObjectEntryCompositionOperationResult failure = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("unavailable", CreatePrefab("qa-dynamic-failed-part")));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Faulted, failure.Status);
            StringAssert.Contains("PreparationFailed", failure.Diagnostic);
            Assert.IsFalse(failure.EffectsMayHaveApplied);
            Assert.AreEqual(0, GetGrantRoot(grant).transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_ReplacementReleaseFailureReportsDegradedPartialResult()
        {
            const string id = "qa.stable.dynamic-partial-replacement";
            bool failOldRelease = true;
            GameObject firstPrefab = CreatePrefab("qa-dynamic-partial-old");
            GameObject secondPrefab = CreatePrefab("qa-dynamic-partial-new");
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id);
            InstallDynamicRuntime(grant, instance =>
            {
                if (instance != null && instance.name == firstPrefab.name && failOldRelease)
                    return Task.FromResult(false);
                if (instance != null) Object.DestroyImmediate(instance);
                return Task.FromResult(instance == null);
            });
            GameObject root = GetGrantRoot(grant);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            await contribution.SetPartAsync(new ObjectEntryCompositionPartDefinition("visual", firstPrefab));

            ObjectEntryCompositionOperationResult replacement = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("visual", secondPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Degraded, replacement.Status);
            Assert.IsTrue(replacement.EffectsMayHaveApplied);
            CollectionAssert.Contains(replacement.ChangedKeys, "visual");
            StringAssert.Contains("ReleaseFailed", replacement.Diagnostic);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsNotNull(root.transform.Find(secondPrefab.name));
            failOldRelease = false;
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_CloseKeepsItsExactGuardThroughUnityCallbacks()
        {
            const string id = "qa.stable.dynamic-close-guard";
            GameObject root = CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry(id, root);
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject prefab = CreatePrefab("qa-dynamic-close-guarded-part", value => value.AddComponent<ObjectEntryCompositionDisableProbe>());
            ObjectEntryCompositionOperationResult set = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("guarded", prefab));
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, set.Status, set.Diagnostic);
            GameObject instance = root.transform.Find(prefab.name).gameObject;
            ObjectEntryCompositionAuthoring authoring = root.GetComponent<ObjectEntryCompositionAuthoring>();
            ObjectEntryCompositionDisableProbe.Disabled = changed =>
            {
                if (!ReferenceEquals(changed.transform.parent, root.transform)) return;
                if (root.GetComponent<ObjectEntryDeclaration>() != null && authoring != null)
                    Object.DestroyImmediate(authoring);
            };

            ObjectEntryCompositionOperationResult close = await contribution.CloseAsync();

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Degraded, close.Status);
            Assert.IsTrue(close.EffectsMayHaveApplied);
            CollectionAssert.Contains(close.ChangedKeys, "guarded");
            Assert.IsTrue(instance != null);
            Assert.AreSame(root.transform, instance.transform.parent,
                "The revoked consumer guard must stop detach after the Unity callback and keep the exact root protected.");
            Assert.AreSame(root, root.GetComponent<ObjectEntryDeclaration>().gameObject);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicContribution_CompletedCallbackFlowCanStartANewMutation()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-inherited-scope");
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject callbackPrefab = CreatePrefab(
                "qa-dynamic-inherited-scope-callback",
                value => value.AddComponent<ObjectEntryCompositionDisableProbe>());
            GameObject deferredPrefab = CreatePrefab("qa-dynamic-inherited-scope-deferred");
            var continueDeferredOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<ObjectEntryCompositionOperationResult> deferredOperation = null;
            ObjectEntryCompositionDisableProbe.Enabled = _ =>
            {
                if (deferredOperation != null) return;
                deferredOperation = RunDeferredOperationAsync();
            };

            async Task<ObjectEntryCompositionOperationResult> RunDeferredOperationAsync()
            {
                await continueDeferredOperation.Task;
                return await contribution.SetPartAsync(
                    new ObjectEntryCompositionPartDefinition("deferred", deferredPrefab));
            }

            ObjectEntryCompositionOperationResult initial = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("callback", callbackPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, initial.Status, initial.Diagnostic);
            Assert.IsNotNull(deferredOperation, "OnEnable must capture the active operation execution context.");
            continueDeferredOperation.TrySetResult(true);
            ObjectEntryCompositionOperationResult deferred = await deferredOperation;

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Committed, deferred.Status, deferred.Diagnostic);
            Assert.IsNotNull(GetGrantRoot(grant).transform.Find(deferredPrefab.name));
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        [Test]
        public async Task DynamicPartDefinition_RejectsSceneObjectsAndInvalidDependencies()
        {
            ObjectEntryCompositionAccessGrant grant = await RegisterDynamicEntry("qa.stable.dynamic-request-validation");
            Assert.IsTrue(grant.TryOpenContribution(0, out ObjectEntryCompositionContribution contribution, out string diagnostic), diagnostic);
            GameObject sceneObject = Create("qa-dynamic-scene-object-not-prefab");
            GameObject prefab = CreatePrefab("qa-dynamic-invalid-dependency-prefab");
            GameObject nestedEntryPrefab = CreatePrefab(
                "qa-dynamic-nested-entry-prefab",
                value => value.AddComponent<ObjectEntryDeclaration>());
            GameObject actorPrefab = CreatePrefab(
                "qa-dynamic-actor-authority-prefab",
                value => value.AddComponent<ActorDeclaration>());

            ObjectEntryCompositionOperationResult sceneReference = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("scene", sceneObject));
            ObjectEntryCompositionOperationResult duplicateDependency = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("duplicates", prefab, new[] { "same", "same" }));
            ObjectEntryCompositionOperationResult invalidKey = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition(" ", prefab));
            ObjectEntryCompositionOperationResult missingPrefab = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("missing-prefab", null));
            ObjectEntryCompositionOperationResult nestedIdentity = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("nested-entry", nestedEntryPrefab));
            ObjectEntryCompositionOperationResult actorAuthority = await contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("actor-authority", actorPrefab));

            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, sceneReference.Status);
            StringAssert.Contains("prefab", sceneReference.Diagnostic.ToLowerInvariant());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, duplicateDependency.Status);
            StringAssert.Contains("duplicate", duplicateDependency.Diagnostic.ToLowerInvariant());
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, invalidKey.Status);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, missingPrefab.Status);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, nestedIdentity.Status);
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Rejected, actorAuthority.Status);
            Assert.AreEqual(0, GetGrantRoot(grant).transform.childCount);
            Assert.IsEmpty(await _bindings.ReleaseOwnerAsync(_ownerA));
        }

        private async Task<ObjectEntryCompositionAccessGrant> RegisterDynamicEntry(
            string id,
            GameObject root = null,
            ObjectEntryCompositionProfile profile = null)
        {
            root ??= CreateEntry(id, _routeA, false);
            ObjectEntryCompositionAuthoring authoring = root.GetComponent<ObjectEntryCompositionAuthoring>() ??
                root.AddComponent<ObjectEntryCompositionAuthoring>();
            if (profile != null) SetAuthoringProfile(authoring, profile);
            SetDynamicOptIn(authoring, true);
            Register(_ownerA, root);
            Assert.IsEmpty(await _bindings.ActivateOwnerCompositionsAsync(_ownerA));
            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(out ObjectEntryCompositionAccessGrant grant, out string diagnostic), diagnostic);
            return grant;
        }

        private GameObject GetGrantRoot(ObjectEntryCompositionAccessGrant grant)
        {
            Assert.IsTrue(grant.TryGetBinding(out _, out StableObjectBinding binding, out _));
            return binding.PhysicalObject;
        }

        private StableObjectBinding ResolveBinding(string id)
        {
            Assert.IsTrue(_bindings.TryResolve(Id(id), null, null, out StableObjectBinding binding, out _, out string diagnostic), diagnostic);
            return binding;
        }

        private void InstallDynamicRuntime(
            ObjectEntryCompositionAccessGrant grant,
            Func<GameObject, Task<bool>> destroyAndConfirm)
        {
            Assert.IsTrue(grant.TryGetBinding(
                out _, out StableObjectBinding binding, out ObjectEntryCompositionRuntimeSlot contextSlot));
            var runtime = new ObjectEntryCompositionRuntimeBinding(
                binding,
                null,
                candidate => _bindings.IsCompositionTargetCurrent(candidate),
                destroyAndConfirm,
                allowDynamicContributions: true);
            Assert.IsTrue(contextSlot.TryBind(runtime));
            binding.CompositionRuntime = runtime;
        }

        private GameObject CreatePrefab(string prefabName, Action<GameObject> configure = null)
        {
            if (string.IsNullOrEmpty(_dynamicPrefabFolder))
            {
                string folderName = "__ObjectEntryCompositionMutationTests_" + Guid.NewGuid().ToString("N");
                AssetDatabase.CreateFolder("Assets", folderName);
                _dynamicPrefabFolder = "Assets/" + folderName;
            }

            GameObject source = Create(prefabName);
            configure?.Invoke(source);
            return PrefabUtility.SaveAsPrefabAsset(source, _dynamicPrefabFolder + "/" + prefabName + ".prefab");
        }
    }
}
