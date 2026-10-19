using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.ActorReferences.Tests
{
    public sealed class ActorOccurrenceReferenceRuntimeTests
    {
        private readonly List<UnityEngine.Object> _created = new();
        private readonly ActorOccurrenceIdSequence _occurrenceIds = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void StagedActorIsInvisibleUntilCommitAndExactReferenceBecomesStaleAfterRelease()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.alpha");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.guard");
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.guard"), owner);

            Assert.That(runtime.TryRegisterOwnerContent(owner, new[] { publication.gameObject }, out string issue), Is.True, issue);
            Assert.That(runtime.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);

            ActorOccurrenceResolution current = runtime.Resolve(selector);
            Assert.That(current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(current.Occurrence.ActorRoot, Is.SameAs(publication.gameObject));
            ActorOccurrenceReference exact = ActorOccurrenceReference.ForOccurrence(current.Occurrence);

            Assert.That(runtime.TryReleaseOwner(owner, out issue), Is.True, issue);
            Assert.That(runtime.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(runtime.Resolve(exact).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
        }

        [Test]
        public void DuplicateActorIdentityIsAmbiguousUntilExactOwnerIsSelected()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner firstOwner = CreateActivityOwner("activity.first");
            RuntimeContentOwner secondOwner = CreateRouteOwner("route.second");
            ActorOccurrencePublication first = CreateNonPlayer("npc.shared");
            ActorOccurrencePublication second = CreateNonPlayer("npc.shared");

            Register(runtime, firstOwner, first.gameObject);
            Register(runtime, secondOwner, second.gameObject);

            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.shared"))).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Ambiguous));
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.shared"), firstOwner)).Occurrence.ActorRoot,
                Is.SameAs(first.gameObject));
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.shared"), secondOwner)).Occurrence.ActorRoot,
                Is.SameAs(second.gameObject));
        }

        [Test]
        public void DuplicateIdentityWithinOneOwnerIsNotSilentlyOverwritten()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.duplicates");
            ActorOccurrencePublication first = CreateNonPlayer("npc.duplicate");
            ActorOccurrencePublication second = CreateNonPlayer("npc.duplicate");

            Register(runtime, owner, first.gameObject, second.gameObject);

            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.duplicate"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Ambiguous));
        }

        [Test]
        public void ActivityTransitionPublishesOnlyTheCommittedTargetAndReplacesCurrentOccurrence()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner firstOwner = CreateActivityOwner("activity.transition.first");
            RuntimeContentOwner nextOwner = CreateActivityOwner("activity.transition.next");
            ActorOccurrencePublication first = CreateNonPlayer("npc.transition");
            ActorOccurrencePublication next = CreateNonPlayer("npc.transition");
            ActorOccurrenceReferenceBinding consumer = Track(new GameObject("Transition Reference"))
                .AddComponent<ActorOccurrenceReferenceBinding>();
            var changes = new List<ActorOccurrenceReferenceChange>();
            consumer.Changed += changes.Add;
            Assert.That(consumer.TrySetReference(
                ActorOccurrenceReference.ForActorId(new ActorId("npc.transition")), out string issue), Is.True, issue);
            runtime.BindConsumer(consumer);

            Register(runtime, firstOwner, first.gameObject);
            Assert.That(consumer.Current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            ActorOccurrenceId firstId = consumer.Current.Occurrence.Id;

            Assert.That(runtime.TryRegisterOwnerContent(nextOwner, new[] { next.gameObject }, out issue), Is.True, issue);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.transition"))).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(runtime.TryCommitOwner(nextOwner, out issue), Is.True, issue);
            Assert.That(runtime.TryReleaseOwner(firstOwner, out issue), Is.True, issue);
            runtime.RefreshAll();

            Assert.That(consumer.Current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(consumer.Current.Occurrence.Id, Is.Not.EqualTo(firstId));
            Assert.That(changes.Exists(change => change.Current.Status == ActorOccurrenceResolutionStatus.Ambiguous), Is.False);
            Assert.That(changes.Exists(change => change.Kind == ActorOccurrenceReferenceChangeKind.Replaced), Is.True);
        }

        [Test]
        public void FailedOwnerPreparationRollsBackWithoutPublishingTheActor()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.rollback");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.rollback");
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.rollback"), owner);

            Assert.That(runtime.TryRegisterOwnerContent(owner, new[] { publication.gameObject }, out string issue), Is.True, issue);
            Assert.That(runtime.TryRollbackOwner(owner, out issue), Is.True, issue);
            Assert.That(runtime.Resolve(selector).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(runtime.TryCommitOwner(owner, out _), Is.False);
        }

        [Test]
        public void FailedTargetRegistrationPreservesThePreviouslyCommittedOccurrence()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner currentOwner = CreateActivityOwner("activity.current");
            RuntimeContentOwner targetOwner = CreateActivityOwner("activity.target");
            ActorOccurrencePublication current = CreateNonPlayer("npc.preserved");
            ActorOccurrencePublication invalid = Track(new GameObject("Invalid Publication"))
                .AddComponent<ActorOccurrencePublication>();
            var selector = ActorOccurrenceReference.ForActorId(new ActorId("npc.preserved"), currentOwner);
            Register(runtime, currentOwner, current.gameObject);
            ActorOccurrenceId currentId = runtime.Resolve(selector).Occurrence.Id;

            Assert.That(runtime.TryRegisterOwnerContent(targetOwner, new[] { invalid.gameObject }, out _), Is.False);

            ActorOccurrenceResolution stillCurrent = runtime.Resolve(selector);
            Assert.That(stillCurrent.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(stillCurrent.Occurrence.Id, Is.EqualTo(currentId));
        }

        [Test]
        public void PlayerPhysicalReferenceSurvivesActivityExitAndObservesReplacementAndLeave()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            PlayerSlotId slot = PlayerSlotId.Player1;
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.player", "Session");
            PlayerActorPhysicalOccurrence firstPhysical = CreatePlayerPhysical(slot, sessionOwner, "player.actor.a", 1);
            source.SetCurrent(firstPhysical, notify: false);
            runtime.SetPlayerSource(source);

            ActorOccurrenceReferenceBinding consumer = Track(new GameObject("Actor Reference Binding"))
                .AddComponent<ActorOccurrenceReferenceBinding>();
            var changes = new List<ActorOccurrenceReferenceChange>();
            consumer.Changed += changes.Add;
            Assert.That(consumer.TrySetReference(ActorOccurrenceReference.ForPlayerSlot(slot), out string issue), Is.True, issue);
            runtime.BindConsumer(consumer);
            ActorOccurrenceResolution first = consumer.Current;
            Assert.That(first.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            ActorOccurrenceReference exactFirst = ActorOccurrenceReference.ForOccurrence(first.Occurrence);

            RuntimeContentOwner activityOwner = CreateActivityOwner("activity.player-test");
            Assert.That(runtime.TryRegisterOwnerContent(activityOwner, Array.Empty<GameObject>(), out issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(activityOwner, out issue), Is.True, issue);
            Assert.That(runtime.TryReleaseOwner(activityOwner, out issue), Is.True, issue);
            runtime.RefreshAll();
            Assert.That(consumer.Current.Occurrence.Id, Is.EqualTo(first.Occurrence.Id));
            Assert.That(changes.Exists(change => change.Kind == ActorOccurrenceReferenceChangeKind.Unavailable), Is.False);

            PlayerActorPhysicalOccurrence replacement = CreatePlayerPhysical(slot, sessionOwner, "player.actor.b", 2);
            source.SetCurrent(replacement, notify: true);
            ActorOccurrenceResolution current = consumer.Current;
            Assert.That(current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(current.Occurrence.Id, Is.Not.EqualTo(first.Occurrence.Id));
            Assert.That(changes.Exists(change => change.Kind == ActorOccurrenceReferenceChangeKind.Replaced), Is.True);
            Assert.That(runtime.Resolve(exactFirst).Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));

            source.SetCurrent(default, notify: true);
            Assert.That(consumer.Current.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(changes.Exists(change => change.Kind == ActorOccurrenceReferenceChangeKind.Unavailable), Is.True);
        }

        [Test]
        public void ActorIdResolutionIncludesPlayerAndReportsCrossKindAmbiguity()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            var source = new FakePlayerSource();
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session("session.actor-id", "Session");
            RuntimeContentOwner activityOwner = CreateActivityOwner("activity.actor-id");
            const string sharedActorId = "actor.shared.player-and-npc";
            source.SetCurrent(
                CreatePlayerPhysical(PlayerSlotId.Player1, sessionOwner, sharedActorId, 1),
                notify: false);
            runtime.SetPlayerSource(source);
            Register(runtime, activityOwner, CreateNonPlayer(sharedActorId).gameObject);

            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId(sharedActorId))).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Ambiguous));

            ActorOccurrenceResolution player = runtime.Resolve(
                ActorOccurrenceReference.ForActorId(new ActorId(sharedActorId), sessionOwner));
            Assert.That(player.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(player.Occurrence.HasPlayerSlot, Is.True);
            Assert.That(player.Occurrence.PlayerSlotId, Is.EqualTo(PlayerSlotId.Player1));
        }

        [Test]
        public void DynamicPublicationRequiresCommittedOwnerAndExactScopeToken()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.dynamic");
            Assert.That(runtime.TryRegisterOwnerContent(owner, Array.Empty<GameObject>(), out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
            ActorOccurrencePublicationScope scope = Track(new GameObject("Publication Scope"))
                .AddComponent<ActorOccurrencePublicationScope>();
            scope.BindRuntime(runtime);
            ActorOccurrencePublication publication = CreateNonPlayer("npc.dynamic");

            Assert.That(scope.TryPublish(publication, owner, out ActorOccurrencePublicationToken token, out issue), Is.True, issue);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.dynamic"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(scope.TryUnpublish(publication, token, out issue), Is.True, issue);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(new ActorId("npc.dynamic"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(scope.TryUnpublish(publication, token, out _), Is.False);
        }

        [Test]
        public void ReleasingPublicationScopeWithdrawsItsDynamicOccurrences()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.scope-release");
            Assert.That(runtime.TryRegisterOwnerContent(owner, Array.Empty<GameObject>(), out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
            ActorOccurrencePublicationScope scope = Track(new GameObject("Publication Scope"))
                .AddComponent<ActorOccurrencePublicationScope>();
            scope.BindRuntime(runtime);
            ActorOccurrencePublication publication = CreateNonPlayer("npc.scope-release");
            Assert.That(scope.TryPublish(publication, owner, out ActorOccurrencePublicationToken oldToken, out issue), Is.True, issue);
            ActorOccurrenceId oldOccurrence = runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scope-release"), owner)).Occurrence.Id;

            scope.UnbindRuntime(runtime);

            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scope-release"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            scope.BindRuntime(runtime);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scope-release"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(scope.TryPublish(publication, owner, out ActorOccurrencePublicationToken newToken, out issue), Is.True, issue);
            Assert.That(newToken, Is.Not.EqualTo(oldToken));
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scope-release"), owner)).Occurrence.Id,
                Is.Not.EqualTo(oldOccurrence));
        }

        [Test]
        public async Task StaticActivityActorContributionRestoresAdoptedVisualAndRejectsAfterOwnerRelease()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.static-composition");
            ActorOccurrencePublication publication = CreateNonPlayer("npc.static-composition");
            Transform mount = Track(new GameObject("Composition Mount")).transform;
            mount.SetParent(publication.transform, false);
            GameObject existingVisual = Track(new GameObject("Existing Visual"));
            existingVisual.transform.SetParent(mount, false);
            GameObject replacementPrefab = Track(new GameObject("Replacement Visual"));
            SetPrivateField(typeof(ActorOccurrencePublication), publication,
                "compositionMount", mount);

            Assert.That(publication.GetComponent<ActorOccurrencePublicationScope>(), Is.Null,
                "Static Activity content should not need a dynamic publication Scope.");
            Assert.That(runtime.TryRegisterOwnerContent(owner,
                new[] { publication.gameObject }, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
            var lifecycle = new ActorOccurrenceReferenceSceneLifecycleParticipant(runtime);
            SceneCompositionScope sceneScope = SceneCompositionScope.ForScene(publication.gameObject.scene);
            Assert.That(lifecycle.OnSceneAvailable(
                sceneScope,
                new[] { publication.gameObject }).Succeeded, Is.True);

            ActorOccurrenceResolution resolved = runtime.Resolve(
                ActorOccurrenceReference.ForActorId(new ActorId("npc.static-composition"), owner));
            Assert.That(resolved.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            ActorOccurrenceId occurrenceId = resolved.Occurrence.Id;

            Assert.That(publication.TryOpenCompositionContribution(
                priority: -10,
                source: "ActivityBaseContent",
                reason: "Adopt the authored visual as the Actor baseline",
                out ActorOccurrenceCompositionContribution baseline,
                out issue), Is.True, issue);
            ActorOccurrenceCompositionOperationResult baselineResult =
                await baseline.SetPartAsync(
                    ActorOccurrenceCompositionPart.AdoptExisting("visual", existingVisual),
                    "ActivityBaseContent",
                    "Commit the authored visual baseline");
            Assert.That(baselineResult.Status,
                Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed),
                baselineResult.Issue);

            Assert.That(publication.TryOpenCompositionContribution(
                priority: 20,
                source: "QuestPresentation",
                reason: "Replace the current visual for this Activity",
                out ActorOccurrenceCompositionContribution overrideContribution,
                out issue), Is.True, issue);
            ActorOccurrenceCompositionOperationResult replaceResult =
                await overrideContribution.SetPartAsync(
                    new ActorOccurrenceCompositionPart("visual", replacementPrefab),
                    "QuestPresentation",
                    "Apply temporary visual");
            Assert.That(replaceResult.Status,
                Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed),
                replaceResult.Issue);
            Assert.That(replaceResult.ActorOccurrenceId, Is.EqualTo(occurrenceId));
            Assert.That(replaceResult.Owner, Is.EqualTo(owner));
            Assert.That(publication.gameObject, Is.SameAs(resolved.Occurrence.ActorRoot));
            Assert.That(existingVisual.activeSelf, Is.False);

            ActorOccurrenceCompositionOperationResult removeResult =
                await overrideContribution.RemovePartAsync(
                    "visual",
                    "QuestPresentation",
                    "Restore the Actor's previous visual");
            Assert.That(removeResult.Status,
                Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed),
                removeResult.Issue);
            Assert.That(removeResult.ActorOccurrenceId, Is.EqualTo(occurrenceId));
            Assert.That(removeResult.CompositionRevision,
                Is.GreaterThan(replaceResult.CompositionRevision));
            Assert.That(existingVisual.activeSelf, Is.True);
            Assert.That(existingVisual.transform.parent, Is.SameAs(mount));
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForOccurrence(
                resolved.Occurrence)).Occurrence.ActorRoot,
                Is.SameAs(publication.gameObject));

            Assert.That(runtime.TryReleaseOwner(owner, out issue), Is.True, issue);
            ActorOccurrenceCompositionOperationResult staleResult =
                await overrideContribution.SetPartAsync(
                    new ActorOccurrenceCompositionPart("visual", replacementPrefab),
                    "QuestPresentation",
                    "Attempt an update after Activity release");
            Assert.That(staleResult.Status,
                Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Stale),
                staleResult.Issue);
            Assert.That(staleResult.ActorOccurrenceId, Is.EqualTo(occurrenceId));
            Assert.That(lifecycle.OnSceneReleasing(
                sceneScope,
                new[] { publication.gameObject },
                "release-static-composition-test").Succeeded, Is.True);
        }

        [Test]
        public void SceneLifecycleReentryIsIdempotentAndCompensationRestoresDynamicOccurrenceIdentity()
        {
            var runtime = new ActorOccurrenceReferenceRuntime();
            RuntimeContentOwner owner = CreateActivityOwner("activity.scene-lifecycle");
            Assert.That(runtime.TryRegisterOwnerContent(owner, Array.Empty<GameObject>(), out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);

            GameObject root = Track(new GameObject("Actor Publication Composition"));
            ActorOccurrencePublicationScope publicationScope = root.AddComponent<ActorOccurrencePublicationScope>();
            ActorOccurrencePublication publication = CreateNonPlayer("npc.scene-lifecycle");
            ActorOccurrenceReferenceSceneLifecycleParticipant participant =
                new ActorOccurrenceReferenceSceneLifecycleParticipant(runtime);
            SceneCompositionScope scope = SceneCompositionScope.ForScene(root.scene);

            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            Assert.That(publicationScope.TryPublish(publication, owner, out _, out issue), Is.True, issue);
            ActorOccurrenceResolution beforeRelease = runtime.Resolve(
                ActorOccurrenceReference.ForActorId(new ActorId("npc.scene-lifecycle"), owner));
            Assert.That(beforeRelease.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));

            Assert.That(participant.OnSceneReleasing(scope, new[] { root }, "test-compensation").Succeeded, Is.True);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scene-lifecycle"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            ((ISceneLifecycleReleaseCompensationParticipant)participant).OnSceneReleaseCompensated(scope);

            ActorOccurrenceResolution afterCompensation = runtime.Resolve(
                ActorOccurrenceReference.ForActorId(new ActorId("npc.scene-lifecycle"), owner));
            Assert.That(afterCompensation.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
            Assert.That(afterCompensation.Occurrence.Id, Is.EqualTo(beforeRelease.Occurrence.Id));

            Assert.That(participant.OnSceneReleasing(scope, new[] { root }, "test-release").Succeeded, Is.True);
            Assert.That(runtime.Resolve(ActorOccurrenceReference.ForActorId(
                new ActorId("npc.scene-lifecycle"), owner)).Status,
                Is.EqualTo(ActorOccurrenceResolutionStatus.Unavailable));
        }

        private void Register(ActorOccurrenceReferenceRuntime runtime, RuntimeContentOwner owner, params GameObject[] roots)
        {
            Assert.That(runtime.TryRegisterOwnerContent(owner, roots, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
        }

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
            ActivityAsset definition = Track(ScriptableObject.CreateInstance<ActivityAsset>()) as ActivityAsset;
            return RuntimeContentOwner.Activity(id, id, RuntimeDefinitionToken.FromUnityObject(definition));
        }

        private RuntimeContentOwner CreateRouteOwner(string id)
        {
            RouteAsset definition = Track(ScriptableObject.CreateInstance<RouteAsset>()) as RouteAsset;
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
}
