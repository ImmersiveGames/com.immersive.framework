using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.ActorReferences.Tests
{
    /// <summary>IF-PLAN-ADR-049-A001 Q02 evidence for the scoped on-demand Actor occurrence query.</summary>
    public sealed class ActorOccurrenceQueryTests
    {
        private readonly List<UnityEngine.Object> _created = new();
        private readonly ActorOccurrenceIdSequence _occurrenceIds = new();

        [TearDown]
        public void TearDown()
        {
            ActorOccurrenceQueryReleaseProbe.Disabled = null;
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void QueryContracts_AreExperimental()
        {
            foreach (Type contract in new[] { typeof(IActorOccurrenceQuery), typeof(ActorOccurrenceQueryBinding) })
            {
                FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
                Assert.That(metadata, Is.Not.Null, contract.FullName);
                Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental), contract.FullName);
                Assert.That(metadata.Note, Is.Not.Empty, contract.FullName);
            }
        }

        [Test]
        public void Query_BeforeCommit_AndAfterRollback_ReturnsUnavailable()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.rollback");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.rollback");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Rollback Query");
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.rollback"), owner);

            AssertUnbound(anchor.Resolve(selector));
            Assert.That(runtime.TryRegisterOwnerContent(
                owner, new[] { publication.gameObject, anchor.gameObject }, out string issue), Is.True, issue);
            AssertUnbound(anchor.Resolve(selector));
            Assert.That(anchor.BoundOwner.IsValid, Is.False);

            Assert.That(runtime.TryRollbackOwner(owner, out issue), Is.True, issue);
            AssertUnbound(anchor.Resolve(selector));
            Assert.That(runtime.TryCommitOwner(owner, out _), Is.False);
            AssertUnbound(anchor.Resolve(selector));

            Register(runtime, owner, publication.gameObject, anchor.gameObject);
            Assert.That(anchor.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved),
                "A rolled-back anchor can be registered again by a later owner preparation.");
        }

        [Test]
        public void Query_DoesNotDispatchChangedOrMutateObservedBinding()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.side-effects");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.observed");
            ActorOccurrencePublication other = CreateNonPlayer("npc.query.other");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Side Effect Query");
            ActorOccurrenceReferenceBinding observer = Track(new GameObject("Observed Binding"))
                .AddComponent<ActorOccurrenceReferenceBinding>();
            var observedSelector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.observed"));
            Assert.That(observer.TrySetReference(observedSelector, out string issue), Is.True, issue);
            runtime.BindConsumer(observer);
            Register(runtime, owner, publication.gameObject, other.gameObject, anchor.gameObject);

            var changes = new List<ActorOccurrenceReferenceChange>();
            observer.Changed += changes.Add;
            ActorOccurrenceResolution before = observer.Current;
            Assert.That(before.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));

            for (int repeat = 0; repeat < 3; repeat++)
            {
                Assert.That(anchor.Resolve(observedSelector).Occurrence.Id, Is.EqualTo(before.Occurrence.Id));
                Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.query.other"), owner)).Status,
                    Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
                Assert.That(anchor.Resolve(ActorOccurrenceReference.ForOccurrence(before.Occurrence)).Status,
                    Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
                Assert.That(anchor.Resolve(default).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.InvalidReference));
            }

            Assert.That(changes, Is.Empty, "A query must not dispatch observation notifications.");
            Assert.That(observer.Current.Status, Is.EqualTo(before.Status));
            Assert.That(observer.Current.Occurrence.Id, Is.EqualTo(before.Occurrence.Id));
            Assert.That(observer.TryGetReference(out ActorOccurrenceReference reference, out issue), Is.True, issue);
            Assert.That(reference.Kind, Is.EqualTo(observedSelector.Kind));
            Assert.That(reference.ActorId, Is.EqualTo(observedSelector.ActorId));
            Assert.That(reference.HasOwner, Is.False);
        }

        [Test]
        public void Query_InsideChangedCallback_AndDuringRefreshAll_SeesCommittedState()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner queryOwner = CreateRouteOwner("route.query.reentrant");
            RuntimeContentOwner producer = CreateActivityOwner("activity.query.reentrant");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Reentrant Query");
            Register(runtime, queryOwner, anchor.gameObject);
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.reentrant");
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.reentrant"));

            ActorOccurrenceReferenceBinding observer = Track(new GameObject("Reentrant Observer"))
                .AddComponent<ActorOccurrenceReferenceBinding>();
            Assert.That(observer.TrySetReference(selector, out string issue), Is.True, issue);
            runtime.BindConsumer(observer);
            var observed = new List<(ActorOccurrenceReferenceChangeKind Kind, ActorOccurrenceResolution Query)>();
            observer.Changed += change => observed.Add((change.Kind, anchor.Resolve(selector)));

            Register(runtime, producer, publication.gameObject);
            runtime.RefreshAll();
            Assert.That(runtime.TryReleaseOwner(producer, out issue), Is.True, issue);

            Assert.That(observed.Count, Is.GreaterThanOrEqualTo(2));
            (ActorOccurrenceReferenceChangeKind kind, ActorOccurrenceResolution query) available = observed[0];
            Assert.That(available.query.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(available.query.Occurrence.ActorRoot, Is.SameAs(publication.gameObject));
            (ActorOccurrenceReferenceChangeKind kind, ActorOccurrenceResolution query) last = observed[observed.Count - 1];
            Assert.That(last.kind, Is.EqualTo(ActorOccurrenceReferenceChangeKind.Unavailable));
            Assert.That(last.query.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable),
                "A query inside the release refresh observes the committed state after removal.");
            Assert.That(anchor.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));

            int before = observed.Count;
            runtime.RefreshAll();
            Assert.That(observed.Count, Is.EqualTo(before), "Queries inside callbacks must not cause further refreshes.");
        }

        [Test]
        public void PlayerReplacement_ExactSelectorIsStale_SlotSelectorResolvesNewOccurrence()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.replace", "Session");
            PlayerActorPhysicalOccurrence first = CreatePlayerPhysical(PlayerSlotId.Player1, sessionOwner, "player.query.a", 1);
            source.SetCurrent(first, notify: false);
            runtime.SetPlayerSource(source);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Replacement Query");
            Register(runtime, CreateRouteOwner("route.query.replace"), anchor.gameObject);

            ActorOccurrenceResolution current = anchor.Resolve(ActorOccurrenceReference.ForPlayerSlot(PlayerSlotId.Player1));
            Assert.That(current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            ActorOccurrenceReference exactFirst = ActorOccurrenceReference.ForOccurrence(current.Occurrence);

            PlayerActorPhysicalOccurrence replacement = CreatePlayerPhysical(PlayerSlotId.Player1, sessionOwner, "player.query.b", 2);
            source.SetCurrent(replacement, notify: true);

            Assert.That(anchor.Resolve(exactFirst).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
            ActorOccurrenceResolution bySlot = anchor.Resolve(ActorOccurrenceReference.ForPlayerSlot(PlayerSlotId.Player1));
            Assert.That(bySlot.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(bySlot.Occurrence.Id, Is.EqualTo(replacement.OccurrenceId));
            Assert.That(bySlot.Occurrence.Id, Is.Not.EqualTo(current.Occurrence.Id));
        }

        [Test]
        public void PlayerTransientInactivity_ExactIsStale_SlotIsUnavailable_AndExactRecovers()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.transient", "Session");
            PlayerActorPhysicalOccurrence committed = CreatePlayerPhysical(PlayerSlotId.Player1, sessionOwner, "player.query.transient", 1);
            source.SetCurrent(committed, notify: false);
            runtime.SetPlayerSource(source);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Transient Query");
            Register(runtime, CreateRouteOwner("route.query.transient"), anchor.gameObject);
            var slot = ActorOccurrenceReference.ForPlayerSlot(PlayerSlotId.Player1);
            ActorOccurrenceReference exact = ActorOccurrenceReference.ForOccurrence(anchor.Resolve(slot).Occurrence);

            // The still-committed occurrence is physically unavailable for a moment (for example a guarded handoff).
            source.SetCurrent(default, notify: true);
            Assert.That(anchor.Resolve(exact).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
            Assert.That(anchor.Resolve(slot).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));

            source.SetCurrent(committed, notify: true);
            ActorOccurrenceResolution recovered = anchor.Resolve(exact);
            Assert.That(recovered.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved),
                "Exact-selector Stale is not proof of end of life.");
            Assert.That(recovered.Occurrence.Id, Is.EqualTo(committed.OccurrenceId));
        }

        [Test]
        public void ActivityExit_PreservesSessionOwnedPlayerForQuery()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.activity-exit", "Session");
            PlayerActorPhysicalOccurrence player = CreatePlayerPhysical(PlayerSlotId.Player1, sessionOwner, "player.query.session", 1);
            source.SetCurrent(player, notify: false);
            runtime.SetPlayerSource(source);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Session Query");
            Register(runtime, sessionOwner, anchor.gameObject);
            RuntimeContentOwner activityOwner = CreateActivityOwner("activity.query.exit");

            Register(runtime, activityOwner);
            Assert.That(runtime.TryReleaseOwner(activityOwner, out string issue), Is.True, issue);

            ActorOccurrenceResolution afterExit = anchor.Resolve(ActorOccurrenceReference.ForPlayerSlot(PlayerSlotId.Player1));
            Assert.That(afterExit.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(afterExit.Occurrence.Id, Is.EqualTo(player.OccurrenceId));
            Assert.That(afterExit.Occurrence.Owner, Is.EqualTo(sessionOwner));
            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("player.query.session"), activityOwner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable),
                "An Activity owner selector never resolves a Session-owned Player.");
        }

        [Test]
        public void DuplicateActorId_IsAmbiguousUntilExactOwnerSelected()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner firstOwner = CreateActivityOwner("activity.query.ambiguous");
            RuntimeContentOwner secondOwner = CreateRouteOwner("route.query.ambiguous");
            ActorOccurrencePublication first = CreateNonPlayer("npc.query.shared");
            ActorOccurrencePublication second = CreateNonPlayer("npc.query.shared");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Ambiguity Query");
            Register(runtime, CreateRouteOwner("route.query.anchor"), anchor.gameObject);
            Register(runtime, firstOwner, first.gameObject);
            Register(runtime, secondOwner, second.gameObject);
            var actorId = new ActorId("npc.query.shared");

            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(actorId)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Ambiguous));
            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(actorId, firstOwner)).Occurrence.ActorRoot,
                Is.SameAs(first.gameObject));
            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(actorId, secondOwner)).Occurrence.ActorRoot,
                Is.SameAs(second.gameObject));
        }

        [Test]
        public void ScopeSuspensionCompensation_ExactGoesStaleThenResolvedWithSameId()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.compensation");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Compensation Query");
            Register(runtime, owner, anchor.gameObject);

            GameObject root = Track(new GameObject("Query Publication Composition"));
            ActorOccurrencePublicationScope publicationScope = root.AddComponent<ActorOccurrencePublicationScope>();
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.compensation");
            var participant = new ActorOccurrenceReferenceSceneLifecycleParticipant(runtime);
            SceneCompositionScope scope = SceneCompositionScope.ForScene(root.scene);
            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            Assert.That(publicationScope.TryPublish(publication, owner, out _, out string issue), Is.True, issue);
            var byActor = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.compensation"), owner);
            ActorOccurrenceResolution before = anchor.Resolve(byActor);
            Assert.That(before.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            ActorOccurrenceReference exact = ActorOccurrenceReference.ForOccurrence(before.Occurrence);

            Assert.That(participant.OnSceneReleasing(scope, new[] { root }, "query-compensation").Succeeded, Is.True);
            Assert.That(anchor.Resolve(exact).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
            Assert.That(anchor.Resolve(byActor).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));

            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            ((ISceneLifecycleReleaseCompensationParticipant)participant).OnSceneReleaseCompensated(scope);
            ActorOccurrenceResolution after = anchor.Resolve(exact);
            Assert.That(after.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(after.Occurrence.Id, Is.EqualTo(before.Occurrence.Id));

            Assert.That(participant.OnSceneReleasing(scope, new[] { root }, "query-compensation-final").Succeeded, Is.True);
        }

        [Test]
        public void OwnerRelease_UnbindsAnchor_PlainCSharpHolderGetsUnavailableWithoutThrowing()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateRouteOwner("route.query.holder");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.holder");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Holder Query");
            Register(runtime, owner, publication.gameObject, anchor.gameObject);
            var holder = new QueryHolder(anchor);
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.holder"), owner);
            Assert.That(holder.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));

            Assert.That(runtime.TryReleaseOwner(owner, out string issue), Is.True, issue);
            AssertUnbound(holder.Resolve(selector));
            Assert.That(anchor.BoundOwner.IsValid, Is.False);

            UnityEngine.Object.DestroyImmediate(anchor.gameObject);
            ActorOccurrenceResolution afterDestroy = default;
            Assert.DoesNotThrow(() => afterDestroy = holder.Resolve(selector));
            AssertUnbound(afterDestroy);
        }

        [Test]
        public async Task OwnerRelease_UnbindsAnchorBeforeCompositionCleanup()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.cleanup-order");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.cleanup-order");
            Transform mount = Track(new GameObject("Query Composition Mount")).transform;
            mount.SetParent(publication.transform, false);
            SetPrivateField(typeof(ActorOccurrencePublication), publication, "compositionMount", mount);
            GameObject partPrefab = Track(new GameObject("Query Cleanup Probe Part"));
            partPrefab.AddComponent<ActorOccurrenceQueryReleaseProbe>();
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Cleanup Order Query");
            Register(runtime, owner, publication.gameObject, anchor.gameObject);
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.cleanup-order"), owner);

            Assert.That(publication.TryOpenCompositionContribution(
                priority: 0,
                source: "QueryCleanupOrder",
                reason: "Materialize a part whose cleanup observes the query anchor",
                out ActorOccurrenceCompositionContribution contribution,
                out string issue), Is.True, issue);
            ActorOccurrenceCompositionOperationResult applied = await contribution.SetPartAsync(
                new ActorOccurrenceCompositionPart("probe", partPrefab),
                "QueryCleanupOrder",
                "Add probe part");
            Assert.That(applied.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed), applied.Issue);
            Assert.That(anchor.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));

            var observedDuringCleanup = new List<ActorOccurrenceResolution>();
            ActorOccurrenceQueryReleaseProbe.Disabled = () => observedDuringCleanup.Add(anchor.Resolve(selector));
            try
            {
                runtime.TryReleaseOwner(owner, out issue);
            }
            finally
            {
                ActorOccurrenceQueryReleaseProbe.Disabled = null;
            }

            Assert.That(observedDuringCleanup, Is.Not.Empty,
                "Owner release was expected to clean up the composed probe part.");
            foreach (ActorOccurrenceResolution observed in observedDuringCleanup)
                AssertUnbound(observed);
            AssertUnbound(anchor.Resolve(selector));
        }

        [Test]
        public void InvalidateQueryAccess_UnbindsAllAnchorsWithoutOwnerRelease_IsIdempotent()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner routeOwner = CreateRouteOwner("route.query.teardown");
            RuntimeContentOwner activityOwner = CreateActivityOwner("activity.query.teardown");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.teardown");
            ActorOccurrenceQueryBinding routeAnchor = CreateAnchor("Route Teardown Query");
            ActorOccurrenceQueryBinding activityAnchor = CreateAnchor("Activity Teardown Query");
            Register(runtime, routeOwner, routeAnchor.gameObject);
            Register(runtime, activityOwner, publication.gameObject, activityAnchor.gameObject);
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.teardown"), activityOwner);
            Assert.That(routeAnchor.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));

            runtime.InvalidateQueryAccess();

            AssertUnbound(routeAnchor.Resolve(selector));
            AssertUnbound(activityAnchor.Resolve(selector));
            Assert.That(runtime.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved),
                "Invalidation touches query anchors only, not publications.");
            Assert.DoesNotThrow(runtime.InvalidateQueryAccess);

            ActorOccurrenceQueryBinding lateAnchor = CreateAnchor("Late Query");
            Assert.That(runtime.TryRegisterOwnerContent(
                CreateRouteOwner("route.query.late"), new[] { lateAnchor.gameObject }, out string issue), Is.False);
            StringAssert.Contains("invalidated", issue);
            Register(runtime, CreateRouteOwner("route.query.late-without-anchor"), CreateNonPlayer("npc.query.late").gameObject);
        }

        [Test]
        public void AnchorBoundOwner_EqualsCompositionOwnerExactly()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.owner", "Session");
            RuntimeContentOwner routeOwner = CreateRouteOwner("route.query.owner");
            RuntimeContentOwner activityOwner = CreateActivityOwner("activity.query.owner");
            ActorOccurrenceQueryBinding sessionAnchor = CreateAnchor("Session Owner Query");
            ActorOccurrenceQueryBinding routeAnchor = CreateAnchor("Route Owner Query");
            ActorOccurrenceQueryBinding activityAnchor = CreateAnchor("Activity Owner Query");

            Assert.That(runtime.TryRegisterOwnerContent(routeOwner, new[] { routeAnchor.gameObject }, out string issue), Is.True, issue);
            Assert.That(routeAnchor.BoundOwner.IsValid, Is.False, "Staging does not bind the anchor.");
            Assert.That(runtime.TryCommitOwner(routeOwner, out issue), Is.True, issue);
            Register(runtime, sessionOwner, sessionAnchor.gameObject);
            Register(runtime, activityOwner, activityAnchor.gameObject);

            Assert.That(sessionAnchor.BoundOwner, Is.EqualTo(sessionOwner));
            Assert.That(routeAnchor.BoundOwner, Is.EqualTo(routeOwner));
            Assert.That(activityAnchor.BoundOwner, Is.EqualTo(activityOwner));
        }

        [Test]
        public void AnchorUnderTwoOwners_OrUnsupportedScope_FailsRegistration()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner firstOwner = CreateRouteOwner("route.query.first");
            RuntimeContentOwner secondOwner = CreateActivityOwner("activity.query.second");
            ActorOccurrenceQueryBinding shared = CreateAnchor("Shared Query");

            Assert.That(runtime.TryRegisterOwnerContent(firstOwner, new[] { shared.gameObject }, out string issue), Is.True, issue);
            Assert.That(runtime.TryRegisterOwnerContent(secondOwner, new[] { shared.gameObject }, out issue), Is.False);
            StringAssert.Contains(firstOwner.StableText, issue);
            Assert.That(runtime.TryCommitOwner(firstOwner, out issue), Is.True, issue);
            Assert.That(shared.BoundOwner, Is.EqualTo(firstOwner), "The first owner keeps the anchor; no last-wins.");
            Assert.That(runtime.TryRegisterOwnerContent(secondOwner, new[] { shared.gameObject }, out issue), Is.False);

            RuntimeContentOwner transient = RuntimeContentOwner.Transient("transient.query", "Transient");
            ActorOccurrenceQueryBinding transientAnchor = CreateAnchor("Transient Query Anchor");
            Assert.That(runtime.TryRegisterOwnerContent(transient, new[] { transientAnchor.gameObject }, out issue), Is.False);
            StringAssert.Contains("unsupported", issue);
            Assert.That(runtime.TryRegisterOwnerContent(transient, new[] { CreateNonPlayer("npc.query.transient").gameObject }, out issue),
                Is.True, "Owners without anchors keep their existing behavior. " + issue);
        }

        [Test]
        public void CommitFails_WhenStagedAnchorWasDestroyed_AndPublishesNothing()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.destroyed");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.destroyed");
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Destroyed Query");
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.query.destroyed"), owner);

            Assert.That(runtime.TryRegisterOwnerContent(owner, new[] { publication.gameObject, anchor.gameObject }, out string issue), Is.True, issue);
            UnityEngine.Object.DestroyImmediate(anchor);

            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.False);
            StringAssert.Contains("destroyed", issue);
            Assert.That(runtime.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(runtime.TryRollbackOwner(owner, out issue), Is.True, issue);
        }

        [Test]
        public void SceneLifecycleParticipant_DoesNotBindQueryAnchor()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Scene Only Query");
            var participant = new ActorOccurrenceReferenceSceneLifecycleParticipant(runtime);
            SceneCompositionScope scope = SceneCompositionScope.ForScene(anchor.gameObject.scene);

            Assert.That(participant.OnSceneAvailable(scope, new[] { anchor.gameObject }).Succeeded, Is.True);

            AssertUnbound(anchor.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.any"))));
            Assert.That(anchor.BoundOwner.IsValid, Is.False);
            Assert.That(participant.OnSceneReleasing(scope, new[] { anchor.gameObject }, "scene-only-query").Succeeded, Is.True);
        }

        private static void AssertUnbound(ActorOccurrenceResolution result)
        {
            Assert.That(result.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            StringAssert.Contains("not bound", result.Diagnostic);
        }

        private void Register(ActorOccurrenceReferenceRuntime runtime, RuntimeContentOwner owner, params GameObject[] roots)
        {
            Assert.That(runtime.TryRegisterOwnerContent(owner, roots, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
        }

        private ActorOccurrenceQueryBinding CreateAnchor(string name) =>
            Track(new GameObject(name)).AddComponent<ActorOccurrenceQueryBinding>();

        private ActorOccurrencePublication CreateNonPlayer(string actorId)
        {
            GameObject root = Track(new GameObject(actorId));
            ActorDeclaration declaration = root.AddComponent<ActorDeclaration>();
            SetPrivateField(typeof(ActorDeclaration), declaration, "actorId", actorId);
            ActorOccurrencePublication publication = root.AddComponent<ActorOccurrencePublication>();
            SetPrivateField(typeof(ActorOccurrencePublication), publication, "actorDeclaration", declaration);
            return publication;
        }

        private PlayerActorPhysicalOccurrence CreatePlayerPhysical(
            PlayerSlotId slot,
            RuntimeContentOwner sessionOwner,
            string actorId,
            int revision)
        {
            GameObject root = Track(new GameObject(actorId));
            PlayerActorDeclaration declaration = root.AddComponent<PlayerActorDeclaration>();
            SetPrivateField(typeof(ActorDeclaration), declaration, "actorId", actorId);
            ActorId id = new ActorId(actorId);
            var token = new PlayerActorPreparationToken(
                "session.player",
                slot,
                new ActorProfileId("profile." + actorId),
                revision,
                id,
                RuntimeContentIdentity.From(sessionOwner, "runtime." + actorId),
                revision,
                revision);
            return new PlayerActorPhysicalOccurrence(
                _occurrenceIds.Next(),
                slot,
                token,
                declaration,
                root,
                sessionOwner);
        }

        private RuntimeContentOwner CreateActivityOwner(string id)
        {
            ActivityAsset definition = Track(ScriptableObject.CreateInstance<ActivityAsset>());
            return RuntimeContentOwner.Activity(id, id, RuntimeDefinitionToken.FromUnityObject(definition));
        }

        private RuntimeContentOwner CreateRouteOwner(string id)
        {
            RouteAsset definition = Track(ScriptableObject.CreateInstance<RouteAsset>());
            return RuntimeContentOwner.Route(id, id, RuntimeDefinitionToken.FromUnityObject(definition));
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            _created.Add(value);
            return value;
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected serialized field '{fieldName}'.");
            field.SetValue(instance, value);
        }

        /// <summary>Plain C# consumer that receives the query by explicit hand-off.</summary>
        private sealed class QueryHolder
        {
            private readonly IActorOccurrenceQuery _query;

            internal QueryHolder(IActorOccurrenceQuery query) => _query = query;

            internal ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference) => _query.Resolve(reference);
        }

        private sealed class FakePlayerSource : IPlayerPreparedActorOccurrenceSource
        {
            private PlayerActorPhysicalOccurrence _current;
            public event Action<PlayerSlotId> CurrentActorInvalidated;
            public string SessionContextId => "session.player";

            public bool TryGetCurrentActorOccurrence(PlayerSlotId playerSlotId, out PlayerPreparedActorOccurrence occurrence)
            {
                occurrence = default;
                return false;
            }

            public bool TryGetCurrentPhysicalActorOccurrence(
                PlayerSlotId playerSlotId,
                out PlayerActorPhysicalOccurrence occurrence)
            {
                occurrence = _current;
                return occurrence.IsValid && occurrence.PlayerSlotId == playerSlotId;
            }

            public void CollectCurrentPhysicalActorOccurrences(ICollection<PlayerActorPhysicalOccurrence> occurrences)
            {
                if (_current.IsValid) occurrences.Add(_current);
            }

            internal void SetCurrent(PlayerActorPhysicalOccurrence occurrence, bool notify)
            {
                _current = occurrence;
                if (notify) CurrentActorInvalidated?.Invoke(PlayerSlotId.Player1);
            }
        }
    }

    /// <summary>Observes the moment a composed part is disabled during owner cleanup.</summary>
    [ExecuteAlways]
    internal sealed class ActorOccurrenceQueryReleaseProbe : MonoBehaviour
    {
        internal static Action Disabled;

        private void OnDisable() => Disabled?.Invoke();
    }
}
