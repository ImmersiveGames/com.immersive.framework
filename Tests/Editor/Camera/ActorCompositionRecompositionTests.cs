using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class ActorCompositionRecompositionTests
    {
        private readonly List<ActorCompositionPartAsset> _createdPartAssets =
            new List<ActorCompositionPartAsset>();

        [TearDown]
        public void DestroyCreatedPartAssets()
        {
            for (int index = 0; index < _createdPartAssets.Count; index++)
            {
                if (_createdPartAssets[index] != null)
                {
                    Object.DestroyImmediate(_createdPartAssets[index]);
                }
            }

            _createdPartAssets.Clear();
        }

        [Test]
        public async Task RecompositionChangesVisualAndDependentPartWithoutReplacingActorOccurrence()
        {
            const string sessionId = "session.actor-composition-recomposition";
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session(
                sessionId,
                "Actor composition recomposition test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                sessionOwner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject actorRoot = new GameObject("Actor Root");
            PlayerActorDeclaration declaration = actorRoot.AddComponent<PlayerActorDeclaration>();
            var actorId = ActorId.From("actor.recomposition.stable");
            declaration.EstablishRuntimeOccurrenceIdentity(
                actorId,
                "Recomposition Actor",
                null,
                "test actor root identity");
            PlayerActorRuntimeHost actorRuntimeHost = actorRoot.AddComponent<PlayerActorRuntimeHost>();
            actorRoot.transform.SetPositionAndRotation(
                new Vector3(3f, 4f, 5f),
                Quaternion.Euler(0f, 35f, 0f));
            var mount = new GameObject("Visual Content Mount");
            mount.transform.SetParent(actorRoot.transform, false);
            var observationAnchor = new GameObject("Stable Actor Observation");
            observationAnchor.transform.SetParent(actorRoot.transform, false);
            ActorCameraSubjectAuthoring cameraSubject = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
            SetPrivateField(cameraSubject, "observationTransform", observationAnchor.transform);
            SetPrivateField(actorRuntimeHost, "playerActorDeclaration", declaration);
            SetPrivateField(actorRuntimeHost, "presentationMount", mount.transform);
            Assert.That(actorRuntimeHost.TryValidateConfiguration(out string actorHostIssue),
                Is.True, actorHostIssue);
            GameObject baseVisualPrefab = new GameObject("Base Visual Prefab");
            GameObject replacementVisualPrefab = new GameObject("Replacement Visual Prefab");
            GameObject dependentPrefab = new GameObject("Dependent Visual Prefab");
            GameObject trimPrefab = new GameObject("Independent Trim Prefab");
            SetCompositionParts(profile, new ActorCompositionPart("visual", baseVisualPrefab));

            var playerOccurrenceId = new PlayerOccurrenceId("player-occurrence:composition-test:1");
            var actorOccurrenceId = new ActorOccurrenceId(42);
            var request = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                playerOccurrenceId,
                actorId,
                actorOccurrenceId,
                10,
                "test",
                "open actor composition contribution");
            var committedDeltas = new List<long>();
            ActorCompositionRuntimeBinding binding = null;

            try
            {
                Assert.That(ActorCompositionRuntimeBinding.TryCreate(
                    profile,
                    mount.transform,
                    null,
                    operationId,
                    out binding,
                    out string createIssue), Is.True, createIssue);
                Assert.That(binding.TrySetPartsActive(true, out string activationIssue),
                    Is.True, activationIssue);
                GameObject originalVisual = binding.VisualContent;
                Assert.That(originalVisual, Is.Not.Null);
                Assert.That(cameraSubject.TryResolveObservation(
                    declaration,
                    out Transform initialObservation,
                    out string cameraIssue), Is.True, cameraIssue);
                Assert.That(initialObservation, Is.SameAs(observationAnchor.transform));

                binding.Committed += delta => committedDeltas.Add(delta.Revision);
                Assert.That(binding.TryOpenContribution(
                    request,
                    () => true,
                    out PlayerActorCompositionContribution contribution,
                    out string contributionIssue), Is.True, contributionIssue);

                PlayerActorCompositionOperationResult result = await contribution.SetPartsAsync(
                    new[]
                    {
                        new PlayerActorCompositionPart("visual", replacementVisualPrefab),
                        new PlayerActorCompositionPart(
                            "badge",
                            dependentPrefab,
                            new[] { "visual" })
                    },
                    "test",
                    "replace visual and dependent badge");

                Assert.That(result.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(result.ActorId, Is.EqualTo(actorId));
                Assert.That(result.PlayerOccurrenceId, Is.EqualTo(playerOccurrenceId));
                Assert.That(result.ActorOccurrenceId, Is.EqualTo(actorOccurrenceId));
                Assert.That(result.CompositionResult.Revision, Is.EqualTo(2));
                Assert.That(committedDeltas, Is.EqualTo(new long[] { 2 }));
                Assert.That(binding.VisualContent, Is.Not.SameAs(originalVisual));
                Assert.That(binding.VisualContent, Is.Not.Null);
                Assert.That(binding.VisualContent.transform.parent, Is.SameAs(mount.transform));
                Assert.That(binding.VisualContent.activeSelf, Is.True,
                    "A committed content change on the active Actor remains active.");
                Assert.That(mount.transform.childCount, Is.EqualTo(2),
                    "The replaced visual and its dependent part are the only materialized children.");
                Assert.That(actorRoot.transform.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
                Assert.That(actorRoot.transform.rotation, Is.EqualTo(Quaternion.Euler(0f, 35f, 0f)));
                Assert.That(actorRoot.GetComponent<PlayerActorRuntimeHost>(), Is.SameAs(actorRuntimeHost));
                Assert.That(actorRoot.GetComponent<PlayerActorDeclaration>(), Is.SameAs(declaration));
                Assert.That(declaration.ActorId, Is.EqualTo(actorId));
                Assert.That(cameraSubject.TryResolveObservation(
                    declaration,
                    out Transform recomposedObservation,
                    out cameraIssue), Is.True, cameraIssue);
                Assert.That(recomposedObservation, Is.SameAs(initialObservation),
                    "A root-owned Camera Observation anchor remains stable across content-only recomposition.");
                Assert.That(contribution.ActorId, Is.EqualTo(actorId));
                Assert.That(contribution.ActorOccurrenceId, Is.EqualTo(actorOccurrenceId));

                PlayerActorCompositionOperationResult addIndependentPart = await contribution.SetPartAsync(
                    new PlayerActorCompositionPart("trim", trimPrefab),
                    "test",
                    "add one keyed part without replacing existing contribution keys");
                Assert.That(addIndependentPart.Status, Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(addIndependentPart.CompositionResult.Revision, Is.EqualTo(3));
                Assert.That(committedDeltas, Is.EqualTo(new long[] { 2, 3 }));
                Assert.That(mount.transform.childCount, Is.EqualTo(3));
                Assert.That(mount.transform.Find("Independent Trim Prefab"), Is.Not.Null);
            }
            finally
            {
                if (binding != null)
                {
                    Assert.That(binding.TryClose(out string closeIssue), Is.True, closeIssue);
                }

                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(baseVisualPrefab);
                Object.DestroyImmediate(replacementVisualPrefab);
                Object.DestroyImmediate(dependentPrefab);
                Object.DestroyImmediate(trimPrefab);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public async Task PublishedNonPlayerCompositionUsesExactOwnerAndPreservesProducerRootAndAdoptedContent()
        {
            ActivityAsset ownerDefinition = ScriptableObject.CreateInstance<ActivityAsset>();
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "activity.external-nonplayer-composition",
                "External NonPlayer producer",
                RuntimeDefinitionToken.FromUnityObject(ownerDefinition));
            var actorRoot = new GameObject("External NonPlayer Actor");
            ActorDeclaration declaration = actorRoot.AddComponent<ActorDeclaration>();
            SetPrivateField(declaration, "actorId", "actor.external-nonplayer-composition");
            ActorOccurrencePublication publication = actorRoot.AddComponent<ActorOccurrencePublication>();
            ActorOccurrencePublicationScope publicationScope =
                actorRoot.AddComponent<ActorOccurrencePublicationScope>();
            var contentMountObject = new GameObject("Replaceable Content");
            contentMountObject.transform.SetParent(actorRoot.transform, false);
            SetPrivateField(publication, "compositionMount", contentMountObject.transform);
            var observationObject = new GameObject("Stable Observation");
            observationObject.transform.SetParent(actorRoot.transform, false);
            ActorCameraSubjectAuthoring cameraSubject = actorRoot.AddComponent<ActorCameraSubjectAuthoring>();
            SetPrivateField(cameraSubject, "observationTransform", observationObject.transform);
            var adoptedVisual = new GameObject("Producer-Owned Visual");
            adoptedVisual.transform.SetParent(contentMountObject.transform, false);
            GameObject replacementPrefab = new GameObject("Composer-Owned Visual Prefab");
            GameObject producerBadgePrefab = new GameObject("Producer-Owned Badge Prefab");
            var runtime = new ActorOccurrenceReferenceRuntime();
            ActorOccurrenceCompositionContribution adoptedContribution = null;
            ActorOccurrenceCompositionContribution overrideContribution = null;

            try
            {
                Assert.That(cameraSubject.TryResolveObservation(
                    declaration,
                    out Transform observation,
                    out string observationIssue), Is.True, observationIssue);
                Assert.That(observation, Is.SameAs(observationObject.transform));
                Assert.That(runtime.TryRegisterOwnerContent(
                    owner,
                    Array.Empty<GameObject>(),
                    out string issue), Is.True, issue);
                Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
                publicationScope.BindRuntime(runtime);
                Assert.That(publicationScope.TryPublish(
                    publication,
                    owner,
                    out ActorOccurrencePublicationToken token,
                    out issue), Is.True, issue);

                ActorOccurrenceResolution published = runtime.Resolve(
                    ActorOccurrenceReference.ForActorId(declaration.ActorId, owner));
                Assert.That(published.Status, Is.EqualTo(ActorOccurrenceResolutionStatus.Resolved));
                Assert.That(published.Occurrence.HasPlayerSlot, Is.False);
                ActorOccurrenceReference exactReference =
                    ActorOccurrenceReference.ForOccurrence(published.Occurrence);

                SetPrivateField(publication, "compositionMount", null);
                Assert.That(publicationScope.TryOpenCompositionContribution(
                    publication,
                    token,
                    0,
                    "test.scene-content",
                    "reject an implicit composition mount",
                    out _,
                    out issue), Is.False);
                StringAssert.Contains("exact composition mount", issue);
                SetPrivateField(publication, "compositionMount", contentMountObject.transform);

                Assert.That(publicationScope.TryOpenCompositionContribution(
                    publication,
                    token,
                    0,
                    "test.scene-content",
                    "adopt producer-owned visual",
                    out adoptedContribution,
                    out issue), Is.True, issue);
                ActorOccurrenceCompositionOperationResult adoption = await adoptedContribution.SetPartAsync(
                    ActorOccurrenceCompositionPart.AdoptExisting("visual", adoptedVisual),
                    "test.scene-content",
                    "adopt producer-owned visual");
                Assert.That(adoption.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed));
                Assert.That(adoption.ActorId, Is.EqualTo(declaration.ActorId));
                Assert.That(adoption.Owner, Is.EqualTo(owner));
                Assert.That(adoption.ActorOccurrenceId, Is.EqualTo(published.Occurrence.Id));

                ActorOccurrenceCompositionOperationResult badge = await adoptedContribution.SetPartAsync(
                    new ActorOccurrenceCompositionPart("badge", producerBadgePrefab),
                    "test.scene-content",
                    "add a keyed part without replacing the adopted visual intent");
                Assert.That(badge.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed));
                Assert.That(contentMountObject.transform.Find("Producer-Owned Badge Prefab"), Is.Not.Null);

                Assert.That(publicationScope.TryOpenCompositionContribution(
                    publication,
                    token,
                    10,
                    "test.override",
                    "temporarily replace visual",
                    out overrideContribution,
                    out issue), Is.True, issue);
                ActorOccurrenceCompositionOperationResult replacement = await overrideContribution.SetPartAsync(
                    new ActorOccurrenceCompositionPart("visual", replacementPrefab),
                    "test.override",
                    "temporarily replace visual");
                Assert.That(replacement.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed));
                Assert.That(replacement.ActorId, Is.EqualTo(declaration.ActorId));
                Assert.That(replacement.ActorOccurrenceId, Is.EqualTo(published.Occurrence.Id));
                Assert.That(actorRoot.transform, Is.SameAs(published.Occurrence.ActorRoot.transform));
                Assert.That(observationObject.transform, Is.SameAs(cameraSubject.ObservationTransform));
                Assert.That(runtime.Resolve(exactReference).Occurrence.Id, Is.EqualTo(published.Occurrence.Id));

                ActorOccurrenceCompositionOperationResult restored = await overrideContribution.CloseAsync(
                    "test.override",
                    "restore producer-owned visual");
                Assert.That(restored.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Committed));
                Assert.That(adoptedVisual, Is.Not.Null);
                Assert.That(adoptedVisual.transform.parent, Is.SameAs(contentMountObject.transform));
                Assert.That(adoptedVisual.activeSelf, Is.True);
                Assert.That(contentMountObject.transform.Find("Producer-Owned Badge Prefab"), Is.Not.Null,
                    "A later SetPartAsync call adds one keyed part without removing other intent from the same contribution.");

                Assert.That(runtime.TryReleaseOwner(owner, out issue), Is.True, issue);
                Assert.That(runtime.Resolve(exactReference).Status,
                    Is.EqualTo(ActorOccurrenceResolutionStatus.Stale));
                Assert.That(actorRoot, Is.Not.Null,
                    "Publication cleanup must not release the producer-owned physical root.");
                Assert.That(adoptedVisual, Is.Not.Null,
                    "Closing composition must retain the exact adopted producer-owned part.");
                Assert.That(adoptedVisual.transform.parent, Is.SameAs(contentMountObject.transform));

                ActorOccurrenceCompositionOperationResult staleOperation = await adoptedContribution.SetPartAsync(
                    new ActorOccurrenceCompositionPart("badge", replacementPrefab),
                    "test.scene-content",
                    "attempt mutation after exact owner closure");
                Assert.That(staleOperation.Status, Is.EqualTo(ActorOccurrenceCompositionOperationStatus.Stale));
                Assert.That(staleOperation.Owner, Is.EqualTo(owner));
                Assert.That(staleOperation.ActorOccurrenceId, Is.EqualTo(published.Occurrence.Id));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(replacementPrefab);
                Object.DestroyImmediate(producerBadgePrefab);
                Object.DestroyImmediate(ownerDefinition);
            }
        }

        [Test]
        public async Task StaleOccurrenceCannotCommitContributionChanges()
        {
            const string sessionId = "session.actor-composition-stale";
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session(
                sessionId,
                "Actor composition stale target test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                sessionOwner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject actorRoot = new GameObject("Actor Root");
            var mount = new GameObject("Visual Content Mount");
            mount.transform.SetParent(actorRoot.transform, false);
            GameObject baseVisualPrefab = new GameObject("Base Visual Prefab");
            GameObject ownedBadgePrefab = new GameObject("Owned Badge Prefab");
            GameObject staleBadgePrefab = new GameObject("Stale Badge Prefab");
            GameObject recoveryBadgePrefab = new GameObject("Recovery Badge Prefab");
            SetCompositionParts(profile, new ActorCompositionPart("visual", baseVisualPrefab));

            var request = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                new PlayerOccurrenceId("player-occurrence:composition-stale:1"),
                ActorId.From("actor.composition.stale"),
                new ActorOccurrenceId(43),
                10,
                "test",
                "open actor composition contribution");
            ActorCompositionRuntimeBinding binding = null;
            bool isCurrent = true;

            try
            {
                Assert.That(ActorCompositionRuntimeBinding.TryCreate(
                    profile,
                    mount.transform,
                    null,
                    operationId,
                    out binding,
                    out string createIssue), Is.True, createIssue);
                Assert.That(binding.TryOpenContribution(
                    request,
                    () => isCurrent,
                    out PlayerActorCompositionContribution contribution,
                    out string contributionIssue), Is.True, contributionIssue);

                PlayerActorCompositionOperationResult initial = await contribution.SetPartAsync(
                    new PlayerActorCompositionPart("badge", ownedBadgePrefab),
                    "test",
                    "commit an Actor-owned badge before the occurrence becomes stale");
                Assert.That(initial.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(mount.transform.childCount, Is.EqualTo(2));
                long revisionBeforeStaleRequest = binding.Revision;
                isCurrent = false;

                PlayerActorCompositionOperationResult result = await contribution.SetPartAsync(
                    new PlayerActorCompositionPart("badge", staleBadgePrefab),
                    "test",
                    "attempt a contribution update for a stale occurrence");

                Assert.That(result.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Stale));
                Assert.That(result.CompositionResult, Is.Not.Null);
                Assert.That(result.CompositionResult.Status,
                    Is.EqualTo(CompositionResultStatus.Stale));
                Assert.That(binding.Revision, Is.EqualTo(revisionBeforeStaleRequest));
                Assert.That(mount.transform.childCount, Is.EqualTo(2));

                isCurrent = true;
                var recoveryRequest = new PlayerActorCompositionContributionRequest(
                    PlayerSlotId.Player1,
                    request.PlayerOccurrenceId,
                    request.ActorId,
                    request.ActorOccurrenceId,
                    5,
                    "test",
                    "replace content after a stale owner was observed");
                Assert.That(binding.TryOpenContribution(
                    recoveryRequest,
                    () => isCurrent,
                    out PlayerActorCompositionContribution recoveryContribution,
                    out string recoveryIssue), Is.True, recoveryIssue);

                PlayerActorCompositionOperationResult recovery = await recoveryContribution.SetPartAsync(
                    new PlayerActorCompositionPart("badge", recoveryBadgePrefab),
                    "test",
                    "replace the previously owned badge after its target was observed stale");

                Assert.That(recovery.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(mount.transform.childCount, Is.EqualTo(2));
                bool recoveryBadgeMounted = false;
                for (int i = 0; i < mount.transform.childCount; i++)
                {
                    recoveryBadgeMounted |= mount.transform.GetChild(i).name.Contains(recoveryBadgePrefab.name);
                }

                Assert.That(recoveryBadgeMounted, Is.True,
                    "An owner already observed as stale must no longer win when the external guard later returns true.");
            }
            finally
            {
                if (binding != null)
                {
                    Assert.That(binding.TryClose(out string closeIssue), Is.True, closeIssue);
                }

                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(baseVisualPrefab);
                Object.DestroyImmediate(ownedBadgePrefab);
                Object.DestroyImmediate(staleBadgePrefab);
                Object.DestroyImmediate(recoveryBadgePrefab);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public async Task ContributionCompositionCommittedReportsOccurrenceWideDeltas()
        {
            const string sessionId = "session.actor-composition-occurrence-delta";
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session(
                sessionId,
                "Actor composition occurrence delta test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                sessionOwner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject actorRoot = new GameObject("Actor Root");
            var mount = new GameObject("Visual Content Mount");
            mount.transform.SetParent(actorRoot.transform, false);
            GameObject baseVisualPrefab = new GameObject("Base Visual Prefab");
            GameObject contributedPrefab = new GameObject("Contributed Part Prefab");
            SetCompositionParts(profile, new ActorCompositionPart("visual", baseVisualPrefab));

            var actorId = ActorId.From("actor.composition-occurrence-delta");
            var playerOccurrenceId = new PlayerOccurrenceId("player-occurrence:composition-delta:1");
            var actorOccurrenceId = new ActorOccurrenceId(45);
            var firstRequest = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                playerOccurrenceId,
                actorId,
                actorOccurrenceId,
                10,
                "test.first-owner",
                "observe occurrence composition deltas");
            var secondRequest = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                playerOccurrenceId,
                actorId,
                actorOccurrenceId,
                20,
                "test.second-owner",
                "commit an independent part");
            var observedRevisions = new List<long>();
            ActorCompositionRuntimeBinding binding = null;
            PlayerActorCompositionContribution first = null;
            PlayerActorCompositionContribution second = null;

            try
            {
                Assert.That(ActorCompositionRuntimeBinding.TryCreate(
                    profile,
                    mount.transform,
                    null,
                    operationId,
                    out binding,
                    out string createIssue), Is.True, createIssue);
                Assert.That(binding.TrySetPartsActive(true, out string activationIssue),
                    Is.True, activationIssue);
                Assert.That(binding.TryOpenContribution(
                    firstRequest,
                    () => true,
                    out first,
                    out string firstIssue), Is.True, firstIssue);
                Assert.That(binding.TryOpenContribution(
                    secondRequest,
                    () => true,
                    out second,
                    out string secondIssue), Is.True, secondIssue);
                first.CompositionCommitted += delta => observedRevisions.Add(delta.Revision);

                PlayerActorCompositionOperationResult result = await second.SetPartAsync(
                    new PlayerActorCompositionPart("badge", contributedPrefab),
                    "test.second-owner",
                    "commit badge");

                Assert.That(result.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(result.ActorOccurrenceId, Is.EqualTo(actorOccurrenceId));
                Assert.That(observedRevisions, Is.EqualTo(new long[] { 2 }));
            }
            finally
            {
                if (second != null && !second.IsClosed)
                    await second.CloseAsync("test", "cleanup second owner");
                if (first != null && !first.IsClosed)
                    await first.CloseAsync("test", "cleanup first owner");
                if (binding != null)
                    Assert.That(binding.TryClose(out string closeIssue), Is.True, closeIssue);

                UnityEngine.Object.DestroyImmediate(actorRoot);
                UnityEngine.Object.DestroyImmediate(baseVisualPrefab);
                UnityEngine.Object.DestroyImmediate(contributedPrefab);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public async Task SessionScopedCompositionAccessChangesOnlyTheExactCurrentPhysicalActor()
        {
            PlayerSlotProfile slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            ActorProfile actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            ActorProfile replacementActorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            ActorProfile reentrantActorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            SetPrivateField(actorProfile, "actorProfileId", "actor-profile.composition-access.current");
            SetPrivateField(
                replacementActorProfile,
                "actorProfileId",
                "actor-profile.composition-access.replacement");
            SetPrivateField(
                reentrantActorProfile,
                "actorProfileId",
                "actor-profile.composition-access.reentrant");
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetPrivateField(route, "routeId", "route.actor-composition-access-test");
            ActivityAsset compositionActivity = null;
            InputActionAsset playerInputActions = null;
            GameObject localHostRoot = new GameObject("Actor Composition Local Host");
            localHostRoot.SetActive(false);
            GameObject runtimeHostObject = new GameObject("Actor Composition Framework Host");
            GameObject consumerObject = new GameObject("Actor Composition Session Access Consumer");
            GameObject runtimeActorPrefab = new GameObject("Actor Composition Runtime Host Prefab");
            GameObject badgePrefab = new GameObject("Actor Composition Badge Prefab");
            PlayerActorPreparationRuntimeContext preparationContext = null;
            PlayerParticipationRuntimeContext participation = null;
            PlayerSessionScopedAccess access = null;
            PlayerGameplayRuntimeHostModule gameplayModule = null;
            ActivityPlayerActorLifecycleParticipant activityLifecycle = null;
            UnityPlayerInputGateAdapter playerInputGateAdapter = null;
            RuntimeContentRuntime runtimeContent = null;
            RuntimeScopeContext activityScopeContext = default;
            RuntimeScopeContext physicalScopeForCleanup = default;
            RuntimeContentOwner activityOwner = default;
            RuntimeContentOwner sessionOwner = default;
            RuntimeContentHandle retiredRuntimeHandleForCleanup = null;
            RuntimeContentIdentity retiredIdentityForCleanup = default;
            bool retiredHandleUnregistered = false;
            bool activityEntered = false;
            PlayerActorPhysicalOccurrence currentActor = default;
            Action<PlayerSessionChange> selectionObserver = null;
            Action<GameObject> activationObserver = null;
            Action<GameObject> deactivateReplacementOnEnable = null;

            try
            {
                PlayerParticipationOperationResult initialization =
                    PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy(
                        new[] { slotProfile },
                        true,
                        PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                        nameof(ActorCompositionRecompositionTests),
                        "Session-scoped composition integration test",
                        out participation);
                Assert.That(initialization.Succeeded, Is.True, initialization.ToDiagnosticString());

                PlayerParticipationOperationResult reservation = participation.TryReserveNextAvailableSlot(
                    "test",
                    "reserve slot");
                Assert.That(reservation.Succeeded, Is.True, reservation.ToDiagnosticString());
                PlayerParticipationOperationResult joined = participation.TryMarkJoined(
                    reservation.ReservationToken,
                    "test",
                    "join slot");
                Assert.That(joined.Succeeded, Is.True, joined.ToDiagnosticString());
                PlayerActorSelectionResult selection = participation.TrySelectActorProfile(
                    new PlayerActorSelectionRequest(
                        joined.Slot.PlayerSlotId,
                        actorProfile,
                        "test",
                        "select Actor Profile",
                        expectedSelectionRevision: 0));
                Assert.That(selection.Succeeded, Is.True, selection.ToDiagnosticString());
                Assert.That(participation.TryGetActorSelection(
                    joined.Slot.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot selectedSlot), Is.True);

                PlayerInput playerInput = localHostRoot.AddComponent<PlayerInput>();
                playerInputActions = ScriptableObject.CreateInstance<InputActionAsset>();
                var gameplayActionMap = new InputActionMap("Player");
                gameplayActionMap.AddAction("Move", InputActionType.Value);
                playerInputActions.AddActionMap(gameplayActionMap);
                playerInput.actions = playerInputActions;
                playerInputGateAdapter = localHostRoot.AddComponent<UnityPlayerInputGateAdapter>();
                playerInputGateAdapter.enabled = false;
                SetPrivateField(playerInputGateAdapter, "playerInput", playerInput);
                SetPrivateField(
                    playerInputGateAdapter,
                    "gameplayActionMap",
                    PlayerInputActionMapReference.From(gameplayActionMap));
                LocalPlayerHostAuthoring localHost = localHostRoot.AddComponent<LocalPlayerHostAuthoring>();
                var actorMount = new GameObject("Actor Mount");
                actorMount.transform.SetParent(localHostRoot.transform, false);
                SetPrivateField(localHost, "playerInput", playerInput);
                SetPrivateField(localHost, "actorMount", actorMount.transform);

                PlayerActorDeclaration prefabDeclaration =
                    runtimeActorPrefab.AddComponent<PlayerActorDeclaration>();
                PlayerActorRuntimeHost runtimeActor =
                    runtimeActorPrefab.AddComponent<PlayerActorRuntimeHost>();
                runtimeActorPrefab.AddComponent<ActorReplacementActivationProbe>();
                var visualMount = new GameObject("Visual Content Mount");
                visualMount.transform.SetParent(runtimeActorPrefab.transform, false);
                SetPrivateField(runtimeActor, "playerActorDeclaration", prefabDeclaration);
                SetPrivateField(runtimeActor, "presentationMount", visualMount.transform);
                SetPrivateField(localHost, "playerActorRuntimeHostPrefab", runtimeActor);

                var routeScope = new RouteContentDiscoveryScope(
                    route,
                    Array.Empty<RouteContentDiscoveryScene>(),
                    Array.Empty<GameObject>());
                localHostRoot.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>().Configure(
                    new RoutePlayerSpatialEntryContext(route, 1, routeScope));
                Assert.That(localHost.TryRestoreCommittedAdmission(
                    selectedSlot,
                    "test",
                    "restore joined Host",
                    false,
                    null,
                    out string hostIssue), Is.True, hostIssue);

                string sessionId = participation.CreateSnapshot().ContextId;
                sessionOwner = RuntimeContentOwner.Session(
                    sessionId,
                    "Actor composition test Session");
                runtimeContent = new RuntimeContentRuntime();
                Assert.That(runtimeContent.CreateScopeRoot(
                    sessionOwner,
                    "test",
                    "create Session scope").Applied, Is.True);
                Assert.That(runtimeContent.TryCreateScopeContext(
                    sessionOwner,
                    "test",
                    "create physical Actor scope",
                    out RuntimeScopeContext physicalScope), Is.True);
                physicalScopeForCleanup = physicalScope;

                var hostEvidence = new PlayerHostEvidenceProjection(participation);
                Assert.That(hostEvidence.RegisterSessionPhysicalHost(
                    selectedSlot.PlayerSlotId,
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    localHost,
                    "test",
                    "register physical Host").Succeeded, Is.True);
                Assert.That(PlayerActorPreparationRuntimeContext.TryCreate(
                    participation,
                    hostEvidence,
                    new AttachedPlayerActorMaterializationAdapter(runtimeContent, sessionId),
                    new ActorOccurrenceIdSequence(),
                    out preparationContext,
                    out string preparationIssue), Is.True, preparationIssue);

                RuntimeContentOwner routeOwner = RuntimeContentOwner.Route(
                    route.RouteId.StableText,
                    route.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(route));
                var routeScopeContext = new RuntimeScopeContext(
                    routeOwner,
                    "test",
                    "prepare current Actor");
                PlayerActorPreparationResult preparation = preparationContext.TryPrepareSelectedActor(
                    routeScopeContext,
                    physicalScope,
                    selectedSlot.PlayerSlotId,
                    "test",
                    "prepare current Actor");
                Assert.That(preparation.Succeeded, Is.True, preparation.Message);
                localHostRoot.SetActive(true);
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    selectedSlot.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);

                FrameworkRuntimeHost runtimeHost = runtimeHostObject.AddComponent<FrameworkRuntimeHost>();
                Assert.That(runtimeHost.TryBeginStartup(), Is.True);
                runtimeHost.CompleteStartup(succeeded: true);
                PlayerParticipationRuntimeHostModule participationModule =
                    runtimeHostObject.AddComponent<PlayerParticipationRuntimeHostModule>();
                SetPrivateField(participationModule, "_runtimeContext", participation);
                SetPrivateField(participationModule, "_initializationResult", initialization);

                var preparationModule = runtimeHostObject.AddComponent<PlayerActorPreparationRuntimeHostModule>();
                SetPrivateField(preparationModule, "_runtimeHost", runtimeHost);
                SetPrivateField(preparationModule, "_participationContext", participation);
                SetPrivateField(preparationModule, "_hostEvidenceProjection", hostEvidence);
                SetPrivateField(preparationModule, "_preparationContext", preparationContext);
                SetPrivateField(preparationModule, "_sessionPhysicalScopeContext", physicalScope);
                activityLifecycle = new ActivityPlayerActorLifecycleParticipant(
                    preparationModule,
                    participation);
                SetPrivateField(
                    preparationModule,
                    "_activityLifecycleParticipant",
                    activityLifecycle);

                var flow = new GameFlowRuntime(runtimeContent, null, null, null, null, null);
                var routeLifecycle = (RouteLifecycleRuntime)typeof(GameFlowRuntime)
                    .GetField("_routeLifecycleRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(flow);
                SetPrivateField(
                    routeLifecycle,
                    "_currentRouteState",
                    new RouteRuntimeState(
                        route,
                        default,
                        default,
                        default,
                        default,
                        default,
                        null,
                        "test",
                        "Session-scoped composition access",
                        true));
                SetPrivateField(flow, "_hasCurrentFlowContext", true);
                SetPrivateField(runtimeHost, "_gameFlowRuntime", flow);

                PlayerGameplayAvailabilityBlockTrigger consumer =
                    consumerObject.AddComponent<PlayerGameplayAvailabilityBlockTrigger>();
                access = new PlayerSessionScopedAccess(
                    runtimeHost,
                    participation,
                    LocalPlayerProvisioningConsumerScope.Route,
                    routeOwner,
                    consumer);

                var staleRequest = new PlayerActorCompositionContributionRequest(
                    currentActor.PlayerSlotId,
                    joined.Slot.PlayerOccurrenceId,
                    ActorId.From("actor.stale.composition.request"),
                    currentActor.OccurrenceId,
                    10,
                    "test",
                    "reject stale logical Actor identity");
                bool staleOpened = ((IPlayerActorCompositionAccess)access).TryOpenContribution(
                    staleRequest,
                    out PlayerActorCompositionContribution staleContribution,
                    out string staleIssue);
                Assert.That(staleOpened, Is.False);
                Assert.That(staleContribution, Is.Null);
                Assert.That(staleIssue, Does.Contain("current physical Actor occurrence"));

                var request = new PlayerActorCompositionContributionRequest(
                    currentActor.PlayerSlotId,
                    joined.Slot.PlayerOccurrenceId,
                    currentActor.ActorId,
                    currentActor.OccurrenceId,
                    10,
                    "test",
                    "add keyed Actor content");
                Assert.That(((IPlayerActorCompositionAccess)access).TryOpenContribution(
                    request,
                    out PlayerActorCompositionContribution contribution,
                    out string contributionIssue), Is.True, contributionIssue);

                Transform exactActorRoot = currentActor.ActorRoot.transform;
                Vector3 initialPosition = exactActorRoot.position;
                Quaternion initialRotation = exactActorRoot.rotation;
                PlayerActorCompositionOperationResult result = await contribution.SetPartAsync(
                    new PlayerActorCompositionPart("badge", badgePrefab),
                    "test",
                    "commit keyed Actor content");

                Assert.That(result.Status, Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(result.CompositionRevision, Is.EqualTo(1));
                Assert.That(result.PlayerOccurrenceId, Is.EqualTo(joined.Slot.PlayerOccurrenceId));
                Assert.That(result.ActorId, Is.EqualTo(currentActor.ActorId));
                Assert.That(result.ActorOccurrenceId, Is.EqualTo(currentActor.OccurrenceId));
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    selectedSlot.PlayerSlotId,
                    sessionOwner,
                    out PlayerActorPhysicalOccurrence currentActorAfterCommit), Is.True);
                Assert.That(currentActorAfterCommit.ActorId, Is.EqualTo(currentActor.ActorId));
                Assert.That(currentActorAfterCommit.OccurrenceId, Is.EqualTo(currentActor.OccurrenceId));
                Assert.That(currentActorAfterCommit.ActorRoot, Is.SameAs(currentActor.ActorRoot));
                Assert.That(currentActor.ActorRoot.transform, Is.SameAs(exactActorRoot));
                Assert.That(exactActorRoot.position, Is.EqualTo(initialPosition));
                Assert.That(exactActorRoot.rotation, Is.EqualTo(initialRotation));
                Assert.That(currentActor.Declaration.ActorId, Is.EqualTo(currentActor.ActorId));
                PlayerActorRuntimeHost committedRuntimeHost =
                    exactActorRoot.GetComponent<PlayerActorRuntimeHost>();
                Assert.That(committedRuntimeHost, Is.Not.Null,
                    "In-place composition preserves the active Player Actor Runtime Host.");
                Assert.That(committedRuntimeHost.VisualContentMount.childCount, Is.EqualTo(1));

                PlayerActorPhysicalOccurrence previousOccurrence = currentActor;
                ActorId previousActorId = previousOccurrence.ActorId;
                Assert.That(previousActorId.IsValid, Is.True,
                    "The committed Actor A occurrence has a valid logical ActorId before handoff.");
                Vector3 previousPosition = previousOccurrence.ActorRoot.transform.position;
                Quaternion previousRotation = previousOccurrence.ActorRoot.transform.rotation;
                bool handoffPreparationObservedActorAActive = false;
                bool actorBActivatedAfterActorAWasInactive = false;
                bool uncommittedCandidateWasExposedDuringActivation = false;
                bool selectionObserverSawCoherentCommittedActor = false;
                ActorProfile expectedCommittedProfile = replacementActorProfile;
                Transform expectedInactiveActorRoot = previousOccurrence.ActorRoot.transform;
                PlayerActorSelectionStatus reentrantSelectionStatus =
                    PlayerActorSelectionStatus.None;
                activationObserver = activatedRoot =>
                {
                    if (expectedInactiveActorRoot != null &&
                        !ReferenceEquals(activatedRoot, expectedInactiveActorRoot.gameObject))
                    {
                        actorBActivatedAfterActorAWasInactive =
                            !expectedInactiveActorRoot.gameObject.activeInHierarchy;
                    }

                    if (preparationContext.TryGetCurrentPhysicalActorOccurrence(
                            previousOccurrence.PlayerSlotId,
                            sessionOwner,
                            out PlayerActorPhysicalOccurrence observedDuringActivation) &&
                        ReferenceEquals(
                            observedDuringActivation.ActorRoot.gameObject,
                            activatedRoot))
                    {
                        uncommittedCandidateWasExposedDuringActivation = true;
                    }
                };
                ActorReplacementActivationProbe.Activated += activationObserver;
                PlayerActorPreparationResult ReplaceActor(
                    ActorProfile targetProfile,
                    PlayerActorPreparationToken preparationToken,
                    int selectionRevision,
                    string operationReason,
                    Func<bool> prepareHandoff)
                {
                    return preparationContext.TryReplacePreparedActor(
                        routeScopeContext,
                        physicalScope,
                        new PlayerActorSelectionRequest(
                            previousOccurrence.PlayerSlotId,
                            targetProfile,
                            "test.actor-handoff",
                            operationReason,
                            selectionRevision),
                        preparationToken,
                        PlayerActorReplacementIdentityPolicy.PreserveLogicalActor,
                        "test.actor-handoff",
                        operationReason,
                        prepareHandoff,
                        () => true);
                }

                selectionObserver = change =>
                {
                    if (change.Kind != PlayerSessionChangeKind.ActorSelectionChanged ||
                        change.PlayerSlotId != previousOccurrence.PlayerSlotId)
                    {
                        return;
                    }

                    bool hasCurrentPhysicalActor =
                        preparationContext.TryGetCurrentPhysicalActorOccurrence(
                            change.PlayerSlotId,
                            sessionOwner,
                            out PlayerActorPhysicalOccurrence observedOccurrence);
                    selectionObserverSawCoherentCommittedActor =
                        hasCurrentPhysicalActor &&
                        ReferenceEquals(
                            change.CurrentSlot.SelectedActorProfile,
                            expectedCommittedProfile) &&
                        observedOccurrence.ActorId == previousActorId &&
                        observedOccurrence.OccurrenceId !=
                            previousOccurrence.OccurrenceId &&
                        observedOccurrence.ActorRoot.activeInHierarchy;

                    PlayerActorSelectionResult reentrantSelection =
                        preparationContext.TrySelectActorProfile(
                            new PlayerActorSelectionRequest(
                                change.PlayerSlotId,
                                reentrantActorProfile,
                                "test.reentrant",
                                "attempt selection mutation during physical handoff",
                                change.CurrentSlot.SelectionRevision));
                    reentrantSelectionStatus = reentrantSelection.Status;
                };
                participation.Changed += selectionObserver;

                PlayerActorPreparationResult staleTokenReplacement = ReplaceActor(
                    replacementActorProfile,
                    default,
                    selectedSlot.SelectionRevision,
                    "reject a stale prepared Actor token",
                    () => true);
                Assert.That(staleTokenReplacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.RejectedForeignOrStalePreparation));
                Assert.That(previousOccurrence.ActorRoot.activeInHierarchy, Is.True,
                    "A stale preparation token must not disturb the current Actor.");

                bool preparationFailureSawActorAActive = false;
                PlayerActorPreparationResult preparationRejectedReplacement = ReplaceActor(
                    replacementActorProfile,
                    previousOccurrence.Preparation,
                    selectedSlot.SelectionRevision,
                    "retain Actor A when handoff preparation fails",
                    () =>
                    {
                        preparationFailureSawActorAActive =
                            previousOccurrence.ActorRoot.activeInHierarchy;
                        return false;
                    });
                Assert.That(preparationRejectedReplacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.FailedHandoffPreparation),
                    preparationRejectedReplacement.Message);
                Assert.That(preparationFailureSawActorAActive, Is.True);
                Assert.That(previousOccurrence.ActorRoot.activeInHierarchy, Is.True,
                    "Failed pre-handoff preparation retains Actor A as the active Current.");
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    previousOccurrence.PlayerSlotId,
                    sessionOwner,
                    out PlayerActorPhysicalOccurrence actorAfterPreparationFailure), Is.True);
                Assert.That(actorAfterPreparationFailure.ActorRoot,
                    Is.SameAs(previousOccurrence.ActorRoot));

                PlayerActorPreparationResult replacement = ReplaceActor(
                    replacementActorProfile,
                    previousOccurrence.Preparation,
                    selectedSlot.SelectionRevision,
                    "activate and commit Actor B",
                    () =>
                    {
                        handoffPreparationObservedActorAActive =
                            previousOccurrence.ActorRoot.activeInHierarchy;
                        return true;
                    });

                Assert.That(replacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.SucceededReplaced),
                    replacement.Message);
                Assert.That(handoffPreparationObservedActorAActive, Is.True,
                    "Gameplay handoff preparation runs while Actor A is still active.");
                Assert.That(actorBActivatedAfterActorAWasInactive, Is.True,
                    "Actor B activation begins only after Actor A is inactive.");
                Assert.That(uncommittedCandidateWasExposedDuringActivation, Is.False,
                    "Actor B remains private until the physical binding commit.");
                Assert.That(selectionObserverSawCoherentCommittedActor, Is.True,
                    "Selection observers see the new selected profile and authoritative current physical occurrence together.");
                Assert.That(reentrantSelectionStatus,
                    Is.EqualTo(PlayerActorSelectionStatus.RejectedTransitionInProgress));
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    previousOccurrence.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);
                Assert.That(currentActor.ActorId, Is.EqualTo(previousActorId),
                    "PreserveLogicalActor retains ActorId across physical roots.");
                Assert.That(currentActor.OccurrenceId,
                    Is.Not.EqualTo(previousOccurrence.OccurrenceId));
                Assert.That(currentActor.ActorRoot,
                    Is.Not.SameAs(previousOccurrence.ActorRoot));
                Assert.That(currentActor.ActorRoot.transform.position,
                    Is.EqualTo(previousPosition));
                Assert.That(currentActor.ActorRoot.transform.rotation,
                    Is.EqualTo(previousRotation));
                Assert.That(currentActor.Declaration.PlayerInput,
                    Is.SameAs(playerInput),
                    "Physical replacement remains attached to the same Local Player Host input.");
                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot replacementSlot), Is.True);
                Assert.That(replacementSlot.SelectedActorProfile,
                    Is.SameAs(replacementActorProfile));

                expectedInactiveActorRoot = currentActor.ActorRoot.transform;
                deactivateReplacementOnEnable = activatedRoot =>
                {
                    if (!ReferenceEquals(activatedRoot, expectedInactiveActorRoot.gameObject))
                    {
                        activatedRoot.SetActive(false);
                    }
                };
                ActorReplacementActivationProbe.BeforeActivated += deactivateReplacementOnEnable;
                PlayerActorPreparationResult activationFailedReplacement;
                try
                {
                    activationFailedReplacement = ReplaceActor(
                        reentrantActorProfile,
                        currentActor.Preparation,
                        replacementSlot.SelectionRevision,
                        "report an Actor B that did not remain active",
                        () => expectedInactiveActorRoot.gameObject.activeInHierarchy);
                }
                finally
                {
                    ActorReplacementActivationProbe.BeforeActivated -= deactivateReplacementOnEnable;
                }

                Assert.That(activationFailedReplacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.FailedHandoffAfterPreviousDeactivation),
                    activationFailedReplacement.Message);
                Assert.That(uncommittedCandidateWasExposedDuringActivation, Is.False,
                    "A candidate whose activation fails is never published as the physical Current.");
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    previousOccurrence.PlayerSlotId,
                    sessionOwner,
                    out _), Is.False,
                    "A failed B activation must not publish B or claim inactive A is available.");
                Assert.That(participation.TryGetActorSelection(
                    previousOccurrence.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot selectionAfterActivationFailure), Is.True);
                Assert.That(selectionAfterActivationFailure.SelectedActorProfile,
                    Is.SameAs(replacementActorProfile),
                    "A failed B activation must not publish the replacement profile.");
                Assert.That(selectionAfterActivationFailure.SelectionRevision,
                    Is.EqualTo(replacementSlot.SelectionRevision),
                    "A failed B activation must not advance the selection revision.");

                expectedCommittedProfile = actorProfile;
                PlayerActorPreparationResult recoveredReplacement = ReplaceActor(
                    actorProfile,
                    currentActor.Preparation,
                    replacementSlot.SelectionRevision,
                    "recover from failed Actor B activation",
                    () => true);
                Assert.That(recoveredReplacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.SucceededReplaced),
                    recoveredReplacement.Message);
                Assert.That(selectionObserverSawCoherentCommittedActor, Is.True,
                    "The recovery commit publishes its new profile with the matching active physical Actor.");
                Assert.That(reentrantSelectionStatus,
                    Is.EqualTo(PlayerActorSelectionStatus.RejectedTransitionInProgress));
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    previousOccurrence.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);
                Assert.That(currentActor.ActorId, Is.EqualTo(previousActorId));
                Assert.That(currentActor.Declaration.PlayerInput, Is.SameAs(playerInput));
                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot recoveredSlot), Is.True);
                Assert.That(recoveredSlot.SelectedActorProfile, Is.SameAs(actorProfile));

                PlayerActorPreparationResult ownerReleaseDuringHandoff = null;
                PlayerActorPreparationResult ownerClosedReplacement = ReplaceActor(
                    replacementActorProfile,
                    currentActor.Preparation,
                    recoveredSlot.SelectionRevision,
                    "invalidate a staged Actor B when the physical owner closes",
                    () =>
                    {
                        ownerReleaseDuringHandoff = preparationContext.TryReleasePreparedActor(
                            currentActor.PlayerSlotId,
                            currentActor.Preparation,
                            "test.owner-close",
                            "close Session physical Actor owner during handoff");
                        return ownerReleaseDuringHandoff.Succeeded;
                    });

                Assert.That(ownerReleaseDuringHandoff, Is.Not.Null);
                Assert.That(ownerReleaseDuringHandoff.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.SucceededReleased),
                    ownerReleaseDuringHandoff.Message);
                Assert.That(ownerClosedReplacement.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.FailedHandoffAfterPreviousDeactivation),
                    ownerClosedReplacement.Message);
                Assert.That(ownerClosedReplacement.CleanupPending, Is.False,
                    ownerClosedReplacement.ToDiagnosticString());
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    currentActor.PlayerSlotId,
                    sessionOwner,
                    out _), Is.False,
                    "Owner closure invalidates the candidate and leaves no published physical Actor.");
                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot selectionAfterOwnerClose), Is.True);
                Assert.That(selectionAfterOwnerClose.SelectedActorProfile,
                    Is.SameAs(actorProfile),
                    "Owner closure must not publish the uncommitted replacement profile.");
                currentActor = default;

                // From this point, exercise the official Activity-owned replacement path.
                // The earlier direct preparation-context cases remain focused on the
                // physical transaction barrier; this segment covers postcommit projections.
                if (selectionObserver != null)
                {
                    participation.Changed -= selectionObserver;
                    selectionObserver = null;
                }

                PlayerActorPreparationResult activityPreparation =
                    preparationContext.TryPrepareSelectedActor(
                        routeScopeContext,
                        physicalScope,
                        selectedSlot.PlayerSlotId,
                        "test.activity-replacement",
                        "prepare Actor for Activity-owned replacement proofs");
                Assert.That(activityPreparation.Succeeded, Is.True, activityPreparation.ToDiagnosticString());
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    selectedSlot.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);

                Assert.That(PlayerGameplayRuntimeHostModule.TryAttach(
                    runtimeHost,
                    out gameplayModule,
                    out string gameplayIssue), Is.True, gameplayIssue);

                compositionActivity = ScriptableObject.CreateInstance<ActivityAsset>();
                SetPrivateField(compositionActivity, "activityId", "activity.actor-composition-replacement-test");
                SetPrivateField(compositionActivity, "activityName", "Actor Composition Replacement Test");
                SetPrivateField(
                    compositionActivity,
                    "playerParticipationProjectionMode",
                    ActivityParticipationProjectionMode.AllJoinedSlots);
                SetPrivateField(
                    compositionActivity,
                    "playerParticipationRequirementLevel",
                    PlayerParticipationRequirementLevel.GameplayReady);
                activityOwner = RuntimeContentOwner.Activity(
                    compositionActivity.ActivityId.StableText,
                    compositionActivity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(compositionActivity));
                Assert.That(runtimeContent.CreateScopeRoot(
                    activityOwner,
                    "test.activity-replacement",
                    "create Activity scope for replacement proofs").Applied, Is.True);
                Assert.That(runtimeContent.TryCreateScopeContext(
                    activityOwner,
                    "test.activity-replacement",
                    "create Activity replacement scope",
                    out activityScopeContext), Is.True);

                Assert.That(preparationContext.TryReleaseContextualProjection(
                    routeOwner,
                    selectedSlot.PlayerSlotId,
                    "test.activity-replacement",
                    "retire Route-owned Host projection before Activity occurrence",
                    out string routeProjectionReleaseIssue), Is.True, routeProjectionReleaseIssue);

                IReadOnlyList<ActivityReadinessParticipant> readinessParticipants =
                    activityLifecycle.ResolveActivityReadinessParticipants(compositionActivity);
                Assert.That(readinessParticipants, Is.Not.Null);
                Assert.That(readinessParticipants.Count, Is.EqualTo(1));
                readinessParticipants[0].BeginPreparation(
                    new ActivityReadinessOccurrence(compositionActivity, 1));
                ActivityContentExecutionRequest enterRequest =
                    ActivityContentExecutionRequest.Enter(
                        compositionActivity,
                        null,
                        activityScopeContext,
                        RuntimeContentId.From("framework.player-actor.activity-lifecycle"),
                        ActivityContentExecutionRequiredness.Required,
                        "test.activity-replacement",
                        "enter gameplay-ready Activity for replacement proofs");
                ActivityContentExecutionResult activityEntry =
                    activityLifecycle.ExecuteActivityContent(enterRequest);
                Assert.That(activityEntry.Succeeded, Is.True, activityEntry.ToDiagnosticString());
                activityEntered = true;

                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot activitySlot), Is.True);
                Action<PlayerSessionChange> breakGameplayProjection = change =>
                {
                    if (change.Kind == PlayerSessionChangeKind.ActorSelectionChanged &&
                        change.PlayerSlotId == currentActor.PlayerSlotId &&
                        ReferenceEquals(change.CurrentSlot.SelectedActorProfile, replacementActorProfile))
                    {
                        SetPrivateField(
                            playerInputGateAdapter,
                            "gameplayActionMap",
                            default(PlayerInputActionMapReference));
                    }
                };
                participation.Changed += breakGameplayProjection;
                PlayerPreparedActorReplacementResult gameplayProjectionFailure;
                try
                {
                    gameplayProjectionFailure =
                        preparationModule.TryReplacePreparedActorForCurrentActivity(
                            new PlayerPreparedActorReplacementRequest(
                                currentActor.PlayerSlotId,
                                replacementActorProfile,
                                "test.activity-replacement",
                                "prove postcommit Gameplay reprojection failure",
                                activitySlot.SelectionRevision));
                }
                finally
                {
                    participation.Changed -= breakGameplayProjection;
                    SetPrivateField(
                        playerInputGateAdapter,
                        "gameplayActionMap",
                        PlayerInputActionMapReference.From(gameplayActionMap));
                }

                Assert.That(
                    gameplayProjectionFailure.Status,
                    Is.EqualTo(PlayerPreparedActorReplacementStatus.SucceededCommittedGameplayReprojectionFailed),
                    gameplayProjectionFailure.Message);
                Assert.That(gameplayProjectionFailure.ReplacementCommitted, Is.True);
                Assert.That(gameplayProjectionFailure.GameplayReprojected, Is.False);
                Assert.That(gameplayProjectionFailure.CleanupPending, Is.False);
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    currentActor.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);
                Assert.That(currentActor.ActorRoot.activeInHierarchy, Is.True);
                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot committedAfterProjectionFailure), Is.True);
                Assert.That(committedAfterProjectionFailure.SelectedActorProfile,
                    Is.SameAs(replacementActorProfile));

                PlayerGameplayRuntimeOperationResult gameplayRecovery =
                    gameplayModule.TryEnsureCurrentGameplay(
                        currentActor.PlayerSlotId,
                        activityOwner,
                        "test.activity-replacement",
                        "recover committed Actor Gameplay projection");
                Assert.That(gameplayRecovery.Succeeded, Is.True, gameplayRecovery.ToDiagnosticString());
                Assert.That(gameplayRecovery.GameplayReady, Is.True);
                ActivityPlayerActorReconcileResult readinessRecovery =
                    preparationModule.TryReconcileActiveActivityPlayerLifecycle(
                        compositionActivity,
                        activityOwner,
                        readinessParticipants[0].Occurrence,
                        "test.activity-replacement",
                        "complete readiness after Gameplay projection recovery");
                Assert.That(readinessRecovery.Succeeded, Is.True, readinessRecovery.ToDiagnosticString());

                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot cleanupSlot), Is.True);
                retiredIdentityForCleanup = currentActor.Preparation.RuntimeContentIdentity;
                Assert.That(runtimeContent.TryGetHandle(
                    physicalScope,
                    retiredIdentityForCleanup,
                    out retiredRuntimeHandleForCleanup), Is.True,
                    "The previous Actor's exact Session RuntimeContent handle is available for cleanup fault injection.");
                RuntimeRootRegistryOperationResult unregisterRetiredHandle = null;
                Action<PlayerSessionChange> removeRetiredHandleAtCommit = change =>
                {
                    if (change.Kind == PlayerSessionChangeKind.ActorSelectionChanged &&
                        change.PlayerSlotId == currentActor.PlayerSlotId &&
                        ReferenceEquals(change.CurrentSlot.SelectedActorProfile, reentrantActorProfile))
                    {
                        unregisterRetiredHandle = runtimeContent.UnregisterHandle(
                            physicalScope,
                            retiredIdentityForCleanup,
                            "test.activity-replacement",
                            "force retired Actor cleanup registry miss");
                        retiredHandleUnregistered = unregisterRetiredHandle.Applied;
                    }
                };
                participation.Changed += removeRetiredHandleAtCommit;
                PlayerPreparedActorReplacementResult cleanupPendingReplacement;
                try
                {
                    cleanupPendingReplacement =
                        preparationModule.TryReplacePreparedActorForCurrentActivity(
                            new PlayerPreparedActorReplacementRequest(
                                currentActor.PlayerSlotId,
                                reentrantActorProfile,
                                "test.activity-replacement",
                                "prove retired Actor cleanup failure is retained",
                                cleanupSlot.SelectionRevision));
                }
                finally
                {
                    participation.Changed -= removeRetiredHandleAtCommit;
                }

                Assert.That(unregisterRetiredHandle, Is.Not.Null);
                Assert.That(unregisterRetiredHandle.Applied, Is.True,
                    unregisterRetiredHandle.ToDiagnosticString());
                Assert.That(
                    cleanupPendingReplacement.Status,
                    Is.EqualTo(PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending),
                    cleanupPendingReplacement.Message);
                Assert.That(cleanupPendingReplacement.ReplacementCommitted, Is.True);
                Assert.That(cleanupPendingReplacement.GameplayReprojected, Is.True);
                Assert.That(cleanupPendingReplacement.CleanupPending, Is.True);
                Assert.That(preparationContext.CreateSnapshot().RetainedReleaseFailureCount, Is.EqualTo(1));
                Assert.That(preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    currentActor.PlayerSlotId,
                    sessionOwner,
                    out currentActor), Is.True);
                Assert.That(currentActor.ActorRoot.activeInHierarchy, Is.True);
                Assert.That(participation.TryGetActorSelection(
                    currentActor.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot committedWithCleanupPending), Is.True);
                Assert.That(committedWithCleanupPending.SelectedActorProfile,
                    Is.SameAs(reentrantActorProfile));

                RuntimeRootRegistryOperationResult restoreRetiredHandle = runtimeContent.RegisterHandle(
                    physicalScope,
                    retiredRuntimeHandleForCleanup,
                    "test.activity-replacement",
                    "restore retired Actor handle for cleanup retry");
                Assert.That(restoreRetiredHandle.Applied, Is.True,
                    restoreRetiredHandle.ToDiagnosticString());
                retiredHandleUnregistered = false;

                ActivityContentExecutionRequest exitRequest =
                    ActivityContentExecutionRequest.Exit(
                        compositionActivity,
                        null,
                        activityScopeContext,
                        RuntimeContentId.From("framework.player-actor.activity-lifecycle"),
                        ActivityContentExecutionRequiredness.Required,
                        "test.activity-replacement",
                        "exit Activity before final Actor cleanup retry");
                ActivityContentExecutionResult activityExit =
                    activityLifecycle.ExecuteActivityContent(exitRequest);
                Assert.That(activityExit.Succeeded, Is.True, activityExit.ToDiagnosticString());
                activityEntered = false;

                PlayerActorPreparationResult finalActorRelease =
                    preparationContext.TryReleasePreparedActor(
                        currentActor.PlayerSlotId,
                        currentActor.Preparation,
                        "test.activity-replacement",
                        "release current Actor and retry retained retired Actor cleanup");
                Assert.That(finalActorRelease.Status,
                    Is.EqualTo(PlayerActorPreparationStatus.SucceededReleased),
                    finalActorRelease.ToDiagnosticString());
                Assert.That(finalActorRelease.Snapshot.RetainedReleaseFailureCount, Is.Zero);
                currentActor = default;
            }
            finally
            {
                if (participation != null && selectionObserver != null)
                {
                    participation.Changed -= selectionObserver;
                }

                if (activationObserver != null)
                {
                    ActorReplacementActivationProbe.Activated -= activationObserver;
                }

                if (deactivateReplacementOnEnable != null)
                {
                    ActorReplacementActivationProbe.BeforeActivated -= deactivateReplacementOnEnable;
                }

                access?.Dispose();
                if (playerInputGateAdapter != null && playerInputActions != null)
                {
                    InputActionMap cleanupGameplayMap =
                        playerInputActions.FindActionMap("Player", throwIfNotFound: false);
                    if (cleanupGameplayMap != null)
                    {
                        SetPrivateField(
                            playerInputGateAdapter,
                            "gameplayActionMap",
                            PlayerInputActionMapReference.From(cleanupGameplayMap));
                    }
                }

                if (retiredHandleUnregistered && runtimeContent != null &&
                    physicalScopeForCleanup.IsValid && retiredRuntimeHandleForCleanup != null)
                {
                    if (!runtimeContent.TryGetHandle(
                            physicalScopeForCleanup,
                            retiredIdentityForCleanup,
                            out _))
                    {
                        runtimeContent.RegisterHandle(
                            physicalScopeForCleanup,
                            retiredRuntimeHandleForCleanup,
                            "test.cleanup",
                            "restore retained Actor RuntimeContent handle");
                    }
                }

                if (activityEntered && activityLifecycle != null &&
                    compositionActivity != null && activityScopeContext.IsValid)
                {
                    ActivityContentExecutionResult cleanupExit =
                        activityLifecycle.ExecuteActivityContent(
                            ActivityContentExecutionRequest.Exit(
                                compositionActivity,
                                null,
                                activityScopeContext,
                                RuntimeContentId.From("framework.player-actor.activity-lifecycle"),
                                ActivityContentExecutionRequiredness.Required,
                                "test.cleanup",
                                "release Activity gameplay after assertion failure"));
                    Assert.That(cleanupExit.Succeeded, Is.True, cleanupExit.ToDiagnosticString());
                }

                bool released = true;
                string releaseIssue = string.Empty;
                if (preparationContext != null && currentActor.IsValid)
                {
                    if (preparationContext.TryGetCurrentPhysicalActorOccurrence(
                            currentActor.PlayerSlotId,
                            sessionOwner,
                            out PlayerActorPhysicalOccurrence latestCurrentActor))
                    {
                        currentActor = latestCurrentActor;
                    }

                    PlayerActorPreparationResult release = preparationContext.TryReleasePreparedActor(
                        currentActor.PlayerSlotId,
                        currentActor.Preparation,
                        "test",
                        "release prepared Actor");
                    released = release.Succeeded;
                    releaseIssue = release.Message;
                }

                Object.DestroyImmediate(localHostRoot);
                Object.DestroyImmediate(runtimeHostObject);
                Object.DestroyImmediate(consumerObject);
                Object.DestroyImmediate(runtimeActorPrefab);
                Object.DestroyImmediate(badgePrefab);
                Object.DestroyImmediate(route);
                if (compositionActivity != null)
                    Object.DestroyImmediate(compositionActivity);
                if (playerInputActions != null)
                    Object.DestroyImmediate(playerInputActions);
                Object.DestroyImmediate(reentrantActorProfile);
                Object.DestroyImmediate(replacementActorProfile);
                Object.DestroyImmediate(actorProfile);
                Object.DestroyImmediate(slotProfile);
                Assert.That(released, Is.True, releaseIssue);
            }
        }

        [Test]
        public async Task ClosingVisualOverrideRestoresExactSceneProvidedObject()
        {
            const string sessionId = "session.actor-composition-adopted-visual";
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session(
                sessionId,
                "Actor composition adopted visual test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                sessionOwner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject actorRoot = new GameObject("Scene Actor Root");
            var mount = new GameObject("Visual Content Mount");
            mount.transform.SetParent(actorRoot.transform, false);
            var authoredPrefab = new GameObject("Authored Visual Prefab");
            var replacementPrefab = new GameObject("Replacement Visual Prefab");
            var sceneVisual = new GameObject("Scene-Owned Visual");
            sceneVisual.transform.SetParent(mount.transform, false);
            SetCompositionParts(profile, new ActorCompositionPart("visual", authoredPrefab));

            var request = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                new PlayerOccurrenceId("player-occurrence:composition-scene:1"),
                ActorId.From("actor.composition.scene"),
                new ActorOccurrenceId(44),
                20,
                "test",
                "open visual override");
            ActorCompositionRuntimeBinding binding = null;
            PlayerActorCompositionContribution contribution = null;

            try
            {
                Assert.That(ActorCompositionRuntimeBinding.TryCreate(
                    profile,
                    mount.transform,
                    sceneVisual,
                    operationId,
                    out binding,
                    out string createIssue), Is.True, createIssue);
                Assert.That(binding.TrySetPartsActive(true, out string activationIssue),
                    Is.True, activationIssue);
                Assert.That(binding.VisualContent, Is.SameAs(sceneVisual));
                Assert.That(binding.TryOpenContribution(
                    request,
                    () => true,
                    out contribution,
                    out string contributionIssue), Is.True, contributionIssue);

                PlayerActorCompositionOperationResult overrideResult = await contribution.SetPartAsync(
                    new PlayerActorCompositionPart("visual", replacementPrefab),
                    "test",
                    "replace Scene visual");

                Assert.That(overrideResult.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(binding.VisualContent, Is.Not.SameAs(sceneVisual));
                Assert.That(sceneVisual, Is.Not.Null);
                Assert.That(sceneVisual.transform.parent, Is.SameAs(mount.transform));
                Assert.That(sceneVisual.activeSelf, Is.False);

                PlayerActorCompositionOperationResult closeResult = await contribution.CloseAsync(
                    "test",
                    "restore Scene visual");

                Assert.That(closeResult.Status,
                    Is.EqualTo(PlayerActorCompositionOperationStatus.Committed));
                Assert.That(binding.VisualContent, Is.SameAs(sceneVisual));
                Assert.That(sceneVisual, Is.Not.Null);
                Assert.That(sceneVisual.transform.parent, Is.SameAs(mount.transform));
                Assert.That(sceneVisual.activeSelf, Is.True);
            }
            finally
            {
                if (contribution != null && !contribution.IsClosed)
                {
                    await contribution.CloseAsync("test", "cleanup");
                }

                if (binding != null)
                {
                    Assert.That(binding.TryClose(out string closeIssue), Is.True, closeIssue);
                    Assert.That(sceneVisual, Is.Not.Null);
                    Assert.That(sceneVisual.transform.parent, Is.SameAs(mount.transform));
                    Assert.That(sceneVisual.activeSelf, Is.True);
                }

                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(authoredPrefab);
                Object.DestroyImmediate(replacementPrefab);
                Object.DestroyImmediate(profile);
            }
        }

        private void SetCompositionParts(
            ActorProfile profile,
            params ActorCompositionPart[] parts)
        {
            var partAssets = new List<ActorCompositionPartAsset>(parts.Length);
            for (int index = 0; index < parts.Length; index++)
            {
                ActorCompositionPartAsset asset = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
                asset.Initialize(parts[index].Key, parts[index].Prefab, parts[index].Dependencies);
                _createdPartAssets.Add(asset);
                partAssets.Add(asset);
            }

            SetPrivateField(profile, "baseContentParts", partAssets);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }

    [ExecuteAlways]
    internal sealed class ActorReplacementActivationProbe : MonoBehaviour
    {
        internal static event Action<GameObject> Activated;
        internal static event Action<GameObject> BeforeActivated;

        private void OnEnable()
        {
            BeforeActivated?.Invoke(gameObject);
            Activated?.Invoke(gameObject);
        }
    }
}
