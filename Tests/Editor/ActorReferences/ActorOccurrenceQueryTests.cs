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

            // Owner commit does not refresh consumers, so the observer still holds the Unavailable result from
            // BindConsumer. Reading Current re-resolves and legitimately dispatches Available. Settle the
            // observer's own state before subscribing so that only query-caused notifications are recorded.
            ActorOccurrenceResolution before = observer.Current;
            Assert.That(before.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            var changes = new List<ActorOccurrenceReferenceChange>();
            observer.Changed += changes.Add;

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
            ActorOccurrenceResolution after = observer.Current;
            Assert.That(changes, Is.Empty,
                "Re-reading the observer reports no change because the queries mutated no observed state.");
            Assert.That(after.Status, Is.EqualTo(before.Status));
            Assert.That(after.Occurrence.Id, Is.EqualTo(before.Occurrence.Id));
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
        public void RecognitionContracts_AreExperimental()
        {
            foreach (Type contract in new[]
                     {
                         typeof(IActorOccurrenceRecognitionQuery),
                         typeof(ActorOccurrenceRecognitionResult),
                         typeof(ActorOccurrenceRecognitionStatus)
                     })
            {
                FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
                Assert.That(metadata, Is.Not.Null, contract.FullName);
                Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental), contract.FullName);
                Assert.That(metadata.Note, Is.Not.Empty, contract.FullName);
            }
        }

        [Test]
        public void Recognition_NullDestroyedAndUndeclaredCandidatesHaveExplicitOutcomes()
        {
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Recognition Outcomes Query");
            var recognition = (IActorOccurrenceRecognitionQuery)anchor;
            Collider candidate = Track(new GameObject("Undeclared Candidate")).AddComponent<BoxCollider>();

            Assert.That(recognition.Recognize(null).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Invalid));
            Assert.That(recognition.Recognize(candidate).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unrecognized));

            UnityEngine.Object.DestroyImmediate(candidate);
            Assert.That(recognition.Recognize(candidate).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));

            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.unbound-recognition");
            Collider declared = Track(new GameObject("Unbound Candidate Collider")).AddComponent<BoxCollider>();
            declared.transform.SetParent(publication.transform, false);
            Assert.That(recognition.Recognize(declared).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));

            ActorOccurrencePublication unknownKind = CreateNonPlayer("npc.query.unknown-kind-recognition");
            SetPrivateField(typeof(ActorDeclaration), unknownKind.ActorDeclaration, "actorKind", ActorKind.Unknown);
            Collider unknownKindCandidate = Track(new GameObject("Unknown Kind Candidate Collider")).AddComponent<BoxCollider>();
            unknownKindCandidate.transform.SetParent(unknownKind.transform, false);
            Assert.That(recognition.Recognize(unknownKindCandidate).Status,
                Is.EqualTo(ActorOccurrenceRecognitionStatus.Invalid));

            AssertUnbound(anchor.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.query.unbound-recognition"))));
        }

        [Test]
        public void Recognition_InvalidNearestDeclarationDoesNotFallThroughToOuterActor()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.invalid-recognition");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.valid-outer");
            GameObject nestedRoot = Track(new GameObject("Invalid Nested Actor Declaration"));
            nestedRoot.transform.SetParent(publication.transform, false);
            ActorDeclaration invalidDeclaration = nestedRoot.AddComponent<ActorDeclaration>();
            SetPrivateField(typeof(ActorDeclaration), invalidDeclaration, "actorId", string.Empty);
            Collider candidate = Track(new GameObject("Nested Actor Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(nestedRoot.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Invalid Declaration Query");
            Register(runtime, owner, publication.gameObject, anchor.gameObject);

            ActorOccurrenceRecognitionResult result = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(candidate);

            Assert.That(result.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Invalid));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Occurrence.IsValid, Is.False);
        }

        [Test]
        public void Recognition_NonPlayerMatchesExactRootAndDisambiguatesDuplicateActorIds()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.physical-duplicate");
            ActorOccurrencePublication first = CreateNonPlayer("npc.query.physical-duplicate");
            ActorOccurrencePublication second = CreateNonPlayer("npc.query.physical-duplicate");
            Collider firstCollider = Track(new GameObject("First Physical Collider")).AddComponent<BoxCollider>();
            firstCollider.transform.SetParent(first.transform, false);
            Collider secondCollider = Track(new GameObject("Second Physical Collider")).AddComponent<BoxCollider>();
            secondCollider.transform.SetParent(second.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Physical Duplicate Query");
            Register(runtime, CreateRouteOwner("route.query.physical-duplicate-anchor"), anchor.gameObject);
            Register(runtime, owner, first.gameObject, second.gameObject);
            var actorId = new ActorId("npc.query.physical-duplicate");

            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForActorId(actorId, owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Ambiguous));

            ActorOccurrenceRecognitionResult firstResult = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(firstCollider);
            ActorOccurrenceRecognitionResult secondResult = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(secondCollider);

            Assert.That(firstResult.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(secondResult.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(firstResult.Occurrence.ActorId, Is.EqualTo(actorId));
            Assert.That(secondResult.Occurrence.ActorId, Is.EqualTo(actorId));
            Assert.That(firstResult.Occurrence.ActorRoot, Is.SameAs(first.gameObject));
            Assert.That(secondResult.Occurrence.ActorRoot, Is.SameAs(second.gameObject));
            Assert.That(firstResult.Occurrence.Id, Is.Not.EqualTo(secondResult.Occurrence.Id));
            Assert.That(firstResult.Occurrence.Owner, Is.EqualTo(owner));
            Assert.That(secondResult.Occurrence.Owner, Is.EqualTo(owner));
        }

        [Test]
        public void Recognition_StagedAndReleasedNonPlayerOccurrencesAreUnavailable()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner queryOwner = CreateRouteOwner("route.query.recognition-lifecycle");
            RuntimeContentOwner targetOwner = CreateActivityOwner("activity.query.recognition-lifecycle");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.recognition-lifecycle");
            Collider candidate = Track(new GameObject("Lifecycle Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(publication.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Lifecycle Recognition Query");
            Register(runtime, queryOwner, anchor.gameObject);
            var recognition = (IActorOccurrenceRecognitionQuery)anchor;

            Assert.That(runtime.TryRegisterOwnerContent(targetOwner, new[] { publication.gameObject }, out string issue), Is.True, issue);
            Assert.That(recognition.Recognize(candidate).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));

            Assert.That(runtime.TryCommitOwner(targetOwner, out issue), Is.True, issue);
            ActorOccurrenceRecognitionResult committed = recognition.Recognize(candidate);
            Assert.That(committed.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(committed.Occurrence.ActorRoot, Is.SameAs(publication.gameObject));

            Assert.That(runtime.TryReleaseOwner(targetOwner, out issue), Is.True, issue);
            Assert.That(recognition.Recognize(candidate).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));
        }

        [Test]
        public void Recognition_RejectsNonPlayerWhoseCompositionMountLeavesPublishedRoot()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner queryOwner = CreateRouteOwner("route.query.recognition-mount");
            RuntimeContentOwner targetOwner = CreateActivityOwner("activity.query.recognition-mount");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.query.recognition-mount");
            Transform mount = Track(new GameObject("Recognition Composition Mount")).transform;
            mount.SetParent(publication.transform, false);
            SetPrivateField(typeof(ActorOccurrencePublication), publication, "compositionMount", mount);
            Collider candidate = Track(new GameObject("Mount Lifecycle Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(publication.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Mount Lifecycle Recognition Query");
            Register(runtime, queryOwner, anchor.gameObject);
            Assert.That(runtime.TryRegisterOwnerContent(targetOwner, new[] { publication.gameObject }, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(targetOwner, out issue), Is.True, issue);
            var recognition = (IActorOccurrenceRecognitionQuery)anchor;

            ActorOccurrenceRecognitionResult committed = recognition.Recognize(candidate);
            Assert.That(committed.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));

            mount.SetParent(null, true);

            Assert.That(anchor.Resolve(ActorOccurrenceReference.ForOccurrence(committed.Occurrence)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
            Assert.That(recognition.Recognize(candidate).Status,
                Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));
        }

        [Test]
        public void Recognition_RejectsDeclarationThatChangesActorIdDuringKindValidation()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.recognition-reentrant-id");
            GameObject root = Track(new GameObject("Reentrant Actor Declaration"));
            var declaration = root.AddComponent<ActorKindMutatingDeclaration>();
            const string originalId = "npc.query.recognition-reentrant-original";
            const string changedId = "npc.query.recognition-reentrant-changed";
            declaration.Configure(originalId);
            ActorOccurrencePublication publication = root.AddComponent<ActorOccurrencePublication>();
            SetPrivateField(typeof(ActorOccurrencePublication), publication, "actorDeclaration", declaration);
            Collider candidate = Track(new GameObject("Reentrant Actor Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(root.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Reentrant Recognition Query");
            Register(runtime, owner, publication.gameObject, anchor.gameObject);
            declaration.MutateActorIdOnKindRead(2, changedId);

            ActorOccurrenceRecognitionResult result = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(candidate);

            Assert.That(result.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Invalid));
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void Recognition_DoesNotInvokeDeclarationCallbacksDuringOccurrenceLookup()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.query.recognition-no-callback");
            GameObject root = Track(new GameObject("No Callback Actor Declaration"));
            var declaration = root.AddComponent<ActorKindMutatingDeclaration>();
            const string actorId = "npc.query.recognition-no-callback";
            declaration.Configure(actorId);
            ActorOccurrencePublication publication = root.AddComponent<ActorOccurrencePublication>();
            SetPrivateField(typeof(ActorOccurrencePublication), publication, "actorDeclaration", declaration);
            Collider candidate = Track(new GameObject("No Callback Actor Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(root.transform, false);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("No Callback Recognition Query");
            Register(runtime, owner, publication.gameObject, anchor.gameObject);
            declaration.MutateActorIdOnKindRead(3, "npc.query.recognition-callback-ran");

            ActorOccurrenceRecognitionResult result = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(candidate);

            Assert.That(result.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(result.Occurrence.ActorId, Is.EqualTo(new ActorId(actorId)));
            Assert.That(declaration.ActorId, Is.EqualTo(new ActorId(actorId)));
        }

        [Test]
        public void Recognition_PlayerUsesCurrentSessionOccurrenceAndRejectsReplacedRoot()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.player-recognition", "Session");
            PlayerActorPhysicalOccurrence first = CreatePlayerPhysical(
                PlayerSlotId.Player1, sessionOwner, "player.query.recognition-first", 1);
            Collider firstCollider = Track(new GameObject("Player First Collider")).AddComponent<BoxCollider>();
            firstCollider.transform.SetParent(first.ActorRoot.transform, false);
            source.SetCurrent(first, notify: false);
            runtime.SetPlayerSource(source);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Player Recognition Query");
            Register(runtime, CreateRouteOwner("route.query.player-recognition-anchor"), anchor.gameObject);
            var recognition = (IActorOccurrenceRecognitionQuery)anchor;

            ActorOccurrenceRecognitionResult firstResult = recognition.Recognize(firstCollider);
            Assert.That(firstResult.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(firstResult.Occurrence.ActorId, Is.EqualTo(first.ActorId));
            Assert.That(firstResult.Occurrence.Id, Is.EqualTo(first.OccurrenceId));
            Assert.That(firstResult.Occurrence.Owner, Is.EqualTo(sessionOwner));
            Assert.That(firstResult.Occurrence.PlayerSlotId, Is.EqualTo(PlayerSlotId.Player1));
            Assert.That(firstResult.Occurrence.ActorRoot, Is.SameAs(first.ActorRoot));

            PlayerActorPhysicalOccurrence replacement = CreatePlayerPhysical(
                PlayerSlotId.Player1, sessionOwner, "player.query.recognition-replacement", 2);
            Collider replacementCollider = Track(new GameObject("Player Replacement Collider")).AddComponent<BoxCollider>();
            replacementCollider.transform.SetParent(replacement.ActorRoot.transform, false);
            source.SetCurrent(replacement, notify: true);

            Assert.That(recognition.Recognize(firstCollider).Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Unavailable));
            ActorOccurrenceRecognitionResult replacementResult = recognition.Recognize(replacementCollider);
            Assert.That(replacementResult.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Resolved));
            Assert.That(replacementResult.Occurrence.Id, Is.EqualTo(replacement.OccurrenceId));
            Assert.That(replacementResult.Occurrence.ActorRoot, Is.SameAs(replacement.ActorRoot));
        }

        [Test]
        public void Recognition_DuplicateExactPhysicalEvidenceIsAmbiguous()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.query.ambiguous-recognition", "Session");
            PlayerActorPhysicalOccurrence current = CreatePlayerPhysical(
                PlayerSlotId.Player1, sessionOwner, "player.query.ambiguous-recognition", 1);
            Collider candidate = Track(new GameObject("Ambiguous Player Collider")).AddComponent<BoxCollider>();
            candidate.transform.SetParent(current.ActorRoot.transform, false);
            source.SetCurrent(current, notify: false);
            source.AddDuplicate(current);
            runtime.SetPlayerSource(source);
            ActorOccurrenceQueryBinding anchor = CreateAnchor("Ambiguous Recognition Query");
            Register(runtime, CreateRouteOwner("route.query.ambiguous-recognition-anchor"), anchor.gameObject);

            ActorOccurrenceRecognitionResult result = ((IActorOccurrenceRecognitionQuery)anchor).Recognize(candidate);

            Assert.That(result.Status, Is.EqualTo(ActorOccurrenceRecognitionStatus.Ambiguous));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Occurrence.IsValid, Is.False);
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
            // Static owner-registered publications receive composition access from the Actor occurrence
            // scene lifecycle participant, as in the official Scene Lifecycle composition path.
            var participant = new ActorOccurrenceReferenceSceneLifecycleParticipant(runtime);
            SceneCompositionScope sceneScope = SceneCompositionScope.ForScene(publication.gameObject.scene);
            Assert.That(participant.OnSceneAvailable(sceneScope, new[] { publication.gameObject }).Succeeded, Is.True);
            Assert.That(anchor.BoundOwner, Is.EqualTo(owner),
                "The anchor is bound by owner commit, not by the scene lifecycle participant.");
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
            Assert.That(participant.OnSceneReleasing(
                sceneScope, new[] { publication.gameObject }, "query-cleanup-order").Succeeded, Is.True);
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
            private PlayerActorPhysicalOccurrence? _duplicate;
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
                if (_duplicate.HasValue && _duplicate.Value.IsValid) occurrences.Add(_duplicate.Value);
            }

            internal void AddDuplicate(PlayerActorPhysicalOccurrence occurrence) => _duplicate = occurrence;

            internal void SetCurrent(PlayerActorPhysicalOccurrence occurrence, bool notify)
            {
                _current = occurrence;
                if (notify) CurrentActorInvalidated?.Invoke(PlayerSlotId.Player1);
            }
        }

        private sealed class ActorKindMutatingDeclaration : ActorDeclaration
        {
            private int _kindReads;
            private int _mutationRead;
            private string _replacementActorId;

            public override ActorKind ActorKind
            {
                get
                {
                    _kindReads++;
                    if (_kindReads == _mutationRead)
                    {
                        ConfigureForDiagnostics(
                            _replacementActorId,
                            ActorKind.NonPlayer,
                            ActorRole.Neutral,
                            name,
                            "test.recognition.reentrant-id");
                    }

                    return ActorKind.NonPlayer;
                }
            }

            internal void Configure(string actorId)
            {
                ConfigureForDiagnostics(actorId, ActorKind.NonPlayer, ActorRole.Neutral, name, "test.recognition");
                _kindReads = 0;
            }

            internal void MutateActorIdOnKindRead(int read, string actorId)
            {
                _kindReads = 0;
                _mutationRead = read;
                _replacementActorId = actorId;
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
