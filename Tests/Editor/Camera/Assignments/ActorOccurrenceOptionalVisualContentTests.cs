using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class ActorOccurrenceOptionalVisualContentTests
    {
        [Test]
        public void RuntimeHostIsValidWithoutOptionalVisualMount()
        {
            GameObject actorRoot = CreateActorRoot(out PlayerActorRuntimeHost runtimeHost);

            try
            {
                Assert.That(runtimeHost.VisualContentMount, Is.Null);
                Assert.That(runtimeHost.TryValidateConfiguration(out string issue), Is.True, issue);
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        [Test]
        public void RuntimeHostAcceptsExplicitOptionalVisualMount()
        {
            GameObject actorRoot = CreateActorRoot(out PlayerActorRuntimeHost runtimeHost);
            var mountObject = new GameObject("Visual Content Mount");
            mountObject.transform.SetParent(actorRoot.transform, false);
            typeof(PlayerActorRuntimeHost)
                .GetField("presentationMount", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(runtimeHost, mountObject.transform);

            try
            {
                Assert.That(runtimeHost.TryValidateConfiguration(out string issue), Is.True, issue);
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        [Test]
        public void ActorProfileMayOmitVisualContentPrefab()
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();

            try
            {
                Assert.That(profile.HasVisualContentPrefab, Is.False);
                Assert.That(profile.TryGetActorProfileId(out _, out string issue), Is.True, issue);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ManagerProvisionedProfileValidationAcceptsNoVisualContent()
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();

            try
            {
                Assert.That(AttachedPlayerActorMaterializationAdapter.TryValidateProfile(
                    profile,
                    out ActorProfileId actorProfileId,
                    out PlayerActorMaterializationStatus status,
                    out string issue), Is.True, issue);
                Assert.That(actorProfileId.IsValid, Is.True);
                Assert.That(status, Is.EqualTo(PlayerActorMaterializationStatus.SucceededStaged));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ManagerProvisionedProfileValidationRejectsUnmigratedLegacyVisual()
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject legacyVisual = new GameObject("Legacy Visual");
            SetPrivateField(profile, "legacyPresentationPrefab", legacyVisual);

            try
            {
                Assert.That(AttachedPlayerActorMaterializationAdapter.TryValidateProfile(
                    profile,
                    out _,
                    out PlayerActorMaterializationStatus status,
                    out string issue), Is.False);
                Assert.That(status, Is.EqualTo(PlayerActorMaterializationStatus.RejectedInvalidProfile));
                StringAssert.Contains("explicit Actor Profile visual-content migration", issue);
            }
            finally
            {
                Object.DestroyImmediate(legacyVisual);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PreparedActorReplacementPreservesLogicalIdentityUnlessExplicitlyChanged()
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            ActorId currentActorId = ActorId.From("actor.logical-identity-policy-test");

            try
            {
                var defaultRequest = new PlayerPreparedActorReplacementRequest(
                    PlayerSlotId.Player1,
                    profile,
                    "test",
                    "default-policy");
                Assert.That(defaultRequest.IsValid, Is.True);
                Assert.That(
                    defaultRequest.IdentityPolicy,
                    Is.EqualTo(PlayerActorReplacementIdentityPolicy.PreserveLogicalActor));

                Assert.That(PlayerActorPreparationRuntimeContext.TryResolveReplacementActorId(
                    currentActorId,
                    defaultRequest.IdentityPolicy,
                    out ActorId preservedActorId,
                    out string preserveIssue), Is.True, preserveIssue);
                Assert.That(preservedActorId, Is.EqualTo(currentActorId));

                var switchRequest = new PlayerPreparedActorReplacementRequest(
                    PlayerSlotId.Player1,
                    profile,
                    "test",
                    "explicit-logical-actor-switch",
                    PlayerActorReplacementIdentityPolicy.StartNewLogicalActor);
                Assert.That(switchRequest.IsValid, Is.True);
                Assert.That(PlayerActorPreparationRuntimeContext.TryResolveReplacementActorId(
                    currentActorId,
                    switchRequest.IdentityPolicy,
                    out ActorId newActorId,
                    out string switchIssue), Is.True, switchIssue);
                Assert.That(newActorId.IsValid, Is.True);
                Assert.That(newActorId, Is.Not.EqualTo(currentActorId));

                var invalidPolicyRequest = new PlayerPreparedActorReplacementRequest(
                    PlayerSlotId.Player1,
                    profile,
                    "test",
                    "invalid-identity-policy",
                    (PlayerActorReplacementIdentityPolicy)99);
                Assert.That(invalidPolicyRequest.IsValid, Is.False,
                    "Unknown logical Actor identity policies must be rejected instead of falling back silently.");
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SceneProvidedActorCompositionMayOmitVisualContent()
        {
            SceneProvidedLocalPlayerAuthoring authoring = CreateSceneProvidedActor(out GameObject hostObject);

            try
            {
                Assert.That(SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out SceneProvidedLocalPlayerComposition composition,
                    out string issue), Is.True, issue);
                Assert.That(composition.IsValid, Is.True);
                Assert.That(composition.VisualContent, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(authoring.ActorProfile);
                Object.DestroyImmediate(authoring.PlayerSlotProfile);
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void SceneProvidedActorCompositionMayContainOptionalVisualContent()
        {
            SceneProvidedLocalPlayerAuthoring authoring = CreateSceneProvidedActor(out GameObject hostObject);
            PlayerActorRuntimeHost runtimeHost = hostObject.GetComponentInChildren<PlayerActorRuntimeHost>(true);
            GameObject visualPrefab = new GameObject("Actor Visual Prefab");
            SetActorVisualContent(authoring.ActorProfile, visualPrefab);
            var mountObject = new GameObject("Visual Content Mount");
            mountObject.transform.SetParent(runtimeHost.transform, false);
            var visualObject = new GameObject("Visual Content");
            visualObject.transform.SetParent(mountObject.transform, false);
            SetPrivateField(runtimeHost, "presentationMount", mountObject.transform);

            try
            {
                Assert.That(SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out SceneProvidedLocalPlayerComposition composition,
                    out string issue), Is.True, issue);
                Assert.That(composition.VisualContent, Is.SameAs(visualObject));
            }
            finally
            {
                Object.DestroyImmediate(visualPrefab);
                Object.DestroyImmediate(authoring.ActorProfile);
                Object.DestroyImmediate(authoring.PlayerSlotProfile);
                Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void CompositionAdoptsSceneVisualWithoutDuplicatingOrDestroyingIt()
        {
            const string sessionId = "session.scene-composition-adoption";
            RuntimeContentOwner owner = RuntimeContentOwner.Session(sessionId, "Scene composition adoption test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                owner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject actorRoot = new GameObject("Scene Actor Root");
            var mount = new GameObject("Visual Content Mount");
            mount.transform.SetParent(actorRoot.transform, false);
            var visualPrefab = new GameObject("Actor Visual Prefab");
            var sceneVisual = new GameObject("Authored Scene Visual");
            sceneVisual.transform.SetParent(mount.transform, false);
            SetActorVisualContent(profile, visualPrefab);

            try
            {
                Assert.That(ActorCompositionRuntimeBinding.TryCreate(
                    profile,
                    mount.transform,
                    sceneVisual,
                    operationId,
                    out ActorCompositionRuntimeBinding binding,
                    out string issue), Is.True, issue);
                Assert.That(binding, Is.Not.Null);
                Assert.That(binding.VisualContent, Is.SameAs(sceneVisual));
                Assert.That(mount.transform.childCount, Is.EqualTo(1),
                    "Composer adopts the exact Scene object and must not instantiate the Profile prefab beside it.");
                Assert.That(binding.TryClose(out issue), Is.True, issue);
                Assert.That(sceneVisual, Is.Not.Null,
                    "Composer release retains an adopted Scene object; its producer remains responsible for root teardown.");
                Assert.That(sceneVisual.transform.parent, Is.SameAs(mount.transform));
                Assert.That(mount.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
                Object.DestroyImmediate(visualPrefab);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PreparedActorOccurrenceValidityDoesNotRequireVisualContent()
        {
            var actorRoot = new GameObject("Actor Root");
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();
            typeof(ActorDeclaration)
                .GetField("actorId", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(actor, "actor.optional-visual-test");
            var token = new PlayerActorPreparationToken(
                "session.actor-optional-visual-test",
                Immersive.Framework.PlayerSlots.PlayerSlotId.Player1,
                new ActorProfileId("profile.actor-optional-visual-test"),
                1,
                actor.ActorId,
                Immersive.Framework.RuntimeContent.RuntimeContentIdentity.From(
                    Immersive.Framework.RuntimeContent.RuntimeContentOwner.Session(
                        "session.actor-optional-visual-test",
                        "test"),
                    "actor-content"),
                1,
                1);

            try
            {
                var occurrence = new PlayerPreparedActorOccurrence(token, actor, null);

                Assert.That(occurrence.IsValid, Is.True);
                Assert.That(occurrence.VisualContent, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(actorRoot);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MaterializationHandleActivationAndReleaseDoNotRequireVisualContent(bool withVisualContent)
        {
            string sessionId = withVisualContent
                ? "session.actor-optional-visual-on"
                : "session.actor-optional-visual-off";
            var runtime = new RuntimeContentRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session(sessionId, "Actor lifecycle test");
            Assert.That(runtime.CreateScopeRoot(owner, "test", "setup").Applied, Is.True);
            Assert.That(runtime.TryCreateScopeContext(owner, "test", "setup", out RuntimeScopeContext scope), Is.True);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            PlayerSlotProfile slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            GameObject localPlayerRoot = new GameObject("Local Player Host");
            localPlayerRoot.SetActive(false);
            localPlayerRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring localPlayerHost = localPlayerRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorRoot = new GameObject("Actor Occurrence");
            actorRoot.transform.SetParent(localPlayerRoot.transform, false);
            PlayerActorDeclaration declaration = actorRoot.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost runtimeHost = actorRoot.AddComponent<PlayerActorRuntimeHost>();
            typeof(PlayerActorRuntimeHost)
                .GetField("playerActorDeclaration", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(runtimeHost, declaration);

            GameObject visualContent = null;
            if (withVisualContent)
            {
                var mount = new GameObject("Visual Content Mount");
                mount.transform.SetParent(actorRoot.transform, false);
                visualContent = new GameObject("Visual Content");
                visualContent.transform.SetParent(mount.transform, false);
                visualContent.SetActive(false);
                typeof(PlayerActorRuntimeHost)
                    .GetField("presentationMount", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    .SetValue(runtimeHost, mount.transform);
            }

            runtimeHost.gameObject.SetActive(false);
            var slot = new PlayerSlotRuntimeSnapshot(
                0,
                slotProfile,
                PlayerSlotId.Player1,
                PlayerSlotAllocationState.Joined,
                default,
                default,
                1,
                "test",
                "setup",
                profile,
                1,
                "test",
                "selection");
            RuntimeContentId contentId = RuntimeContentId.From("actor-content");
            ActorId actorId = ActorId.From("actor.optional-visual-lifecycle-test");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                owner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);
            var request = new PlayerActorMaterializationRequest(
                operationId,
                sessionId,
                scope,
                slot,
                profile,
                localPlayerHost,
                actorId,
                contentId,
                1,
                "test",
                "materialize");
            Assert.That(request.IsValid, Is.True, request.ToDiagnosticString());
            Assert.That(runtime.TryCreateMaterializationRequest(
                scope,
                contentId,
                new RuntimeMaterializationResource("test", "actor-profile", "Actor", string.Empty),
                "test",
                "materialize",
                out RuntimeMaterializationRequest runtimeRequest,
                out _), Is.True);
            RuntimeContentHandle contentHandle = RuntimeContentHandle.Materialized(
                runtimeRequest.Identity,
                "test",
                "materialize");
            RuntimeMaterializationResult registered = runtime.ApplyMaterializationResult(
                RuntimeMaterializationResult.Success(
                    runtimeRequest,
                    contentHandle,
                    "test",
                    "materialize",
                    "Actor occurrence staged."),
                "test",
                "materialize");
            Assert.That(registered.Succeeded, Is.True, registered.ToDiagnosticString());

            var handle = new PlayerActorMaterializationHandle(
                request,
                runtimeRequest,
                contentHandle,
                localPlayerHost,
                localPlayerRoot.GetComponent<PlayerInput>(),
                runtimeHost,
                visualContent,
                null,
                true,
                "test",
                "materialize");
            var replacementActorRoot = new GameObject("Replacement Actor Occurrence");
            replacementActorRoot.transform.SetParent(localPlayerRoot.transform, false);
            PlayerActorDeclaration replacementDeclaration = replacementActorRoot.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost replacementRuntimeHost = replacementActorRoot.AddComponent<PlayerActorRuntimeHost>();
            SetPrivateField(replacementRuntimeHost, "playerActorDeclaration", replacementDeclaration);
            GameObject replacementVisual = null;
            if (withVisualContent)
            {
                var replacementMount = new GameObject("Replacement Visual Content Mount");
                replacementMount.transform.SetParent(replacementActorRoot.transform, false);
                replacementVisual = new GameObject("Replacement Visual Content");
                replacementVisual.transform.SetParent(replacementMount.transform, false);
                replacementVisual.transform.localPosition = Vector3.right;
                SetPrivateField(replacementRuntimeHost, "presentationMount", replacementMount.transform);
            }
            replacementActorRoot.SetActive(false);
            var replacementHandle = new PlayerActorMaterializationHandle(
                request,
                runtimeRequest,
                contentHandle,
                localPlayerHost,
                localPlayerRoot.GetComponent<PlayerInput>(),
                replacementRuntimeHost,
                replacementVisual,
                null,
                true,
                "test",
                "replacement materialize");
            var adapter = new AttachedPlayerActorMaterializationAdapter(runtime, sessionId);

            try
            {
                var occurrenceIds = new ActorOccurrenceIdSequence();
                Assert.That(handle.VisualContent != null, Is.EqualTo(withVisualContent));
                Assert.That(handle.ActorRoot, Is.SameAs(actorRoot.transform));
                Vector3 actorPosition = new Vector3(3f, 4f, 5f);
                Quaternion actorRotation = Quaternion.Euler(10f, 20f, 30f);
                handle.ActorRoot.SetPositionAndRotation(actorPosition, actorRotation);
                Assert.That(replacementHandle.TrySetActorRootPose(
                    handle.ActorRoot.position,
                    handle.ActorRoot.rotation), Is.True);
                Assert.That(replacementHandle.ActorRoot.position, Is.EqualTo(actorPosition));
                Assert.That(Quaternion.Angle(replacementHandle.ActorRoot.rotation, actorRotation), Is.LessThan(0.01f));
                if (replacementVisual != null)
                {
                    Assert.That(replacementVisual.transform.position, Is.Not.EqualTo(actorPosition));
                }
                Assert.That(
                    RoutePlayerSpatialEntryRuntimeBinding.ResolveActorRoot(handle.PlayerActorRuntimeHost),
                    Is.SameAs(actorRoot.transform),
                    "Route spatial entry targets the Actor root whether optional visual content exists or not.");
                Assert.That(handle.TryCommitOccurrenceIdentity(occurrenceIds.Next()), Is.False,
                    "A staged, inactive materialization cannot publish a physical occurrence identity.");
                Assert.That(handle.OccurrenceId.IsValid, Is.False);
                Assert.That(handle.TryActivate("test", "activate", out string activateIssue), Is.True, activateIssue);
                Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Active));
                ActorOccurrenceId committedOccurrenceId = occurrenceIds.Next();
                Assert.That(handle.TryCommitOccurrenceIdentity(committedOccurrenceId), Is.True);
                Assert.That(handle.OccurrenceId, Is.EqualTo(committedOccurrenceId));
                Assert.That(handle.TryDeactivate("test", "deactivate", out string deactivateIssue), Is.True, deactivateIssue);
                Assert.That(handle.OccurrenceId, Is.EqualTo(committedOccurrenceId),
                    "Temporary deactivation keeps the committed physical occurrence identity on the materialization handle.");
                Assert.That(handle.TryActivate("test", "reactivate", out activateIssue), Is.True, activateIssue);
                Assert.That(handle.OccurrenceId, Is.EqualTo(committedOccurrenceId),
                    "Reactivation must expose the same committed occurrence instead of allocating a legacy identity.");
                Assert.That(handle.TryCommitOccurrenceIdentity(occurrenceIds.Next()), Is.False,
                    "A single materialization handle cannot be reassigned a different occurrence identity.");
                Assert.That(handle.OccurrenceId, Is.EqualTo(committedOccurrenceId));
                Assert.That(adapter.TryReleaseMaterialization(handle, "test", "release", out string releaseIssue), Is.True, releaseIssue);
                Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Released));
                Assert.That(handle.OccurrenceId.IsValid, Is.False,
                    "Terminal physical release invalidates the occurrence identity.");
            }
            finally
            {
                if (localPlayerRoot != null) Object.DestroyImmediate(localPlayerRoot);
                if (profile != null) Object.DestroyImmediate(profile);
                if (slotProfile != null) Object.DestroyImmediate(slotProfile);
            }
        }

        [Test]
        public void ExternallyOwnedActorRelease_DoesNotDestroyBorrowedActorOrVisual()
        {
            const string sessionId = "session.scene-actor-release-ownership";
            var runtime = new RuntimeContentRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session(sessionId, "Scene Actor ownership test");
            Assert.That(runtime.CreateScopeRoot(owner, "test", "setup").Applied, Is.True);
            Assert.That(runtime.TryCreateScopeContext(owner, "test", "setup", out RuntimeScopeContext scope), Is.True);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            PlayerSlotProfile slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            GameObject localPlayerRoot = new GameObject("Scene Local Player Host");
            localPlayerRoot.SetActive(false);
            PlayerInput playerInput = localPlayerRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring localPlayerHost = localPlayerRoot.AddComponent<LocalPlayerHostAuthoring>();

            GameObject borrowedActorRoot = new GameObject("Borrowed Scene Actor");
            PlayerActorDeclaration declaration = borrowedActorRoot.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost runtimeHost = borrowedActorRoot.AddComponent<PlayerActorRuntimeHost>();
            SetPrivateField(runtimeHost, "playerActorDeclaration", declaration);
            GameObject borrowedVisual = new GameObject("Borrowed Scene Visual");
            borrowedVisual.transform.SetParent(borrowedActorRoot.transform, false);

            var slot = new PlayerSlotRuntimeSnapshot(
                0,
                slotProfile,
                PlayerSlotId.Player1,
                PlayerSlotAllocationState.Joined,
                default,
                default,
                1,
                "test",
                "setup",
                profile,
                1,
                "test",
                "selection");
            Assert.That(PlayerActorMaterializationOperationId.TryCreate(
                sessionId,
                owner,
                PlayerSlotId.Player1,
                1,
                out PlayerActorMaterializationOperationId operationId,
                out string operationIssue), Is.True, operationIssue);
            var request = new PlayerActorMaterializationRequest(
                operationId,
                sessionId,
                scope,
                slot,
                profile,
                localPlayerHost,
                ActorId.From("actor.scene-owned-release-test"),
                RuntimeContentId.From("scene-owned-actor"),
                1,
                "test",
                "scene-adoption");
            Assert.That(request.IsValid, Is.True, request.ToDiagnosticString());
            Assert.That(runtime.TryCreateMaterializationRequest(
                scope,
                request.RuntimeContentId,
                new RuntimeMaterializationResource("test", "scene-actor", "Borrowed Actor", string.Empty),
                "test",
                "scene-adoption",
                out RuntimeMaterializationRequest runtimeRequest,
                out _), Is.True);
            RuntimeContentHandle contentHandle = RuntimeContentHandle.Materialized(
                runtimeRequest.Identity,
                "test",
                "scene-adoption");
            RuntimeMaterializationResult registered = runtime.ApplyMaterializationResult(
                RuntimeMaterializationResult.Success(
                    runtimeRequest,
                    contentHandle,
                    "test",
                    "scene-adoption",
                    "Borrowed Scene Actor registered without physical instantiation."),
                "test",
                "scene-adoption");
            Assert.That(registered.Succeeded, Is.True, registered.ToDiagnosticString());

            var handle = new PlayerActorMaterializationHandle(
                request,
                runtimeRequest,
                contentHandle,
                localPlayerHost,
                playerInput,
                runtimeHost,
                borrowedVisual,
                null,
                true,
                "test",
                "scene-adoption");
            Assert.That(handle.TryActivate("test", "activate-borrowed-actor", out string activationIssue),
                Is.True, activationIssue);

            var adapter = new AttachedPlayerActorMaterializationAdapter(runtime, sessionId);
            try
            {
                Assert.That(adapter.TryReleaseMaterialization(
                    handle,
                    "test",
                    "release-scene-adoption",
                    out string releaseIssue), Is.True, releaseIssue);
                Assert.That(handle.RuntimeContentRequest.Identity.Owner, Is.EqualTo(owner));
                Assert.That(borrowedActorRoot, Is.Not.Null,
                    "Scene-Provided ownership keeps the Actor root outside Framework destruction.");
                Assert.That(borrowedActorRoot.activeSelf, Is.False,
                    "Release deactivates the adopted Actor before returning it to its Scene owner.");
                Assert.That(borrowedVisual, Is.Not.Null,
                    "The adopted visual remains owned by the Scene Actor.");
                Assert.That(borrowedVisual.transform.parent, Is.SameAs(borrowedActorRoot.transform));
                Assert.That(borrowedVisual.activeSelf, Is.True);
                Assert.That(borrowedVisual.activeInHierarchy, Is.False);
                Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Released));
                Assert.That(runtime.TryGetHandle(
                    handle.RuntimeContentRequest.Context,
                    handle.RuntimeContentRequest.Identity,
                    out _), Is.False);
            }
            finally
            {
                if (localPlayerRoot != null) Object.DestroyImmediate(localPlayerRoot);
                if (borrowedActorRoot != null) Object.DestroyImmediate(borrowedActorRoot);
                if (profile != null) Object.DestroyImmediate(profile);
                if (slotProfile != null) Object.DestroyImmediate(slotProfile);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ManagerProvisionedMaterializerStagesAndReleasesActorWithOptionalVisualContent(bool withVisualContent)
        {
            string sessionId = withVisualContent
                ? "session.manager-actor-visual-on"
                : "session.manager-actor-visual-off";
            var runtime = new RuntimeContentRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session(sessionId, "Manager Actor materialization test");
            Assert.That(runtime.CreateScopeRoot(owner, "test", "setup").Applied, Is.True);
            Assert.That(runtime.TryCreateScopeContext(owner, "test", "setup", out RuntimeScopeContext scope), Is.True);

            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            PlayerSlotProfile slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            GameObject hostRoot = new GameObject("Local Player Host");
            hostRoot.SetActive(false);
            PlayerInput playerInput = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            var actorMountObject = new GameObject("Actor Mount");
            actorMountObject.transform.SetParent(hostRoot.transform, false);
            SetPrivateField(host, "playerInput", playerInput);
            SetPrivateField(host, "actorMount", actorMountObject.transform);
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetPrivateField(route, "routeId", "manager-composition-activation-test");
            var routeBinding = hostRoot.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            routeBinding.Configure(new RoutePlayerSpatialEntryContext(
                route,
                1,
                new RouteContentDiscoveryScope(
                    route,
                    Array.Empty<RouteContentDiscoveryScene>(),
                    Array.Empty<GameObject>())));

            GameObject runtimeHostPrefabObject = new GameObject("Player Actor Runtime Host Prefab");
            PlayerActorDeclaration prefabDeclaration =
                runtimeHostPrefabObject.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost runtimeHostPrefab =
                runtimeHostPrefabObject.AddComponent<PlayerActorRuntimeHost>();
            SetPrivateField(runtimeHostPrefab, "playerActorDeclaration", prefabDeclaration);
            if (withVisualContent)
            {
                var mountObject = new GameObject("Visual Content Mount");
                mountObject.transform.SetParent(runtimeHostPrefabObject.transform, false);
                SetPrivateField(runtimeHostPrefab, "presentationMount", mountObject.transform);
                GameObject visualPrefab = new GameObject("Visual Content Prefab");
                SetActorVisualContent(profile, visualPrefab);
            }
            SetPrivateField(host, "playerActorRuntimeHostPrefab", runtimeHostPrefab);

            var slot = new PlayerSlotRuntimeSnapshot(
                0,
                slotProfile,
                PlayerSlotId.Player1,
                PlayerSlotAllocationState.Joined,
                default,
                default,
                1,
                "test",
                "setup",
                profile,
                1,
                "test",
                "selection");
            ActorId actorId = ActorId.From("actor.manager-provisioned-materialization-test");

            try
            {
                Assert.That(host.TryRestoreCommittedAdmission(
                    slot,
                    "test",
                    "setup",
                    false,
                    null,
                    out string admissionIssue), Is.True, admissionIssue);

                var adapter = new AttachedPlayerActorMaterializationAdapter(runtime, sessionId);
                PlayerActorMaterializationResult materialized = adapter.TryMaterialize(
                    scope,
                    slot,
                    profile,
                    default,
                    host,
                    "test",
                    "reject-missing-logical-actor-id");
                Assert.That(materialized.Succeeded, Is.False);
                Assert.That(materialized.Status, Is.EqualTo(PlayerActorMaterializationStatus.FailedActorIdentity));

                materialized = adapter.TryMaterialize(
                    scope,
                    slot,
                    profile,
                    actorId,
                    host,
                    "test",
                    "manager-provisioned-materialize");

                Assert.That(materialized.Succeeded, Is.True, materialized.ToDiagnosticString());
                Assert.That(materialized.Request.ActorId, Is.EqualTo(actorId));
                Assert.That(materialized.HasPhysicalEvidence, Is.True);
                Assert.That(materialized.VisualContent != null, Is.EqualTo(withVisualContent));
                if (withVisualContent)
                {
                    Assert.That(materialized.Handle.Composition, Is.Not.Null);
                    Assert.That(materialized.VisualContent.transform.parent,
                        Is.SameAs(materialized.Handle.PlayerActorRuntimeHost.VisualContentMount));
                    Assert.That(materialized.Handle.PlayerActorRuntimeHost.VisualContentMount.childCount,
                        Is.EqualTo(1), "Initial Manager materialization must produce the single Base visual once.");
                    Assert.That(materialized.VisualContent.activeSelf, Is.False,
                        "The acquired Base visual is staged inactive until Actor activation.");
                }
                Assert.That(materialized.Handle.TryActivate(
                    "test",
                    "manager-provisioned-activate",
                    out string activationIssue), Is.True, activationIssue);
                if (withVisualContent)
                {
                    Assert.That(materialized.VisualContent.activeSelf, Is.True,
                        "Actor activation activates the Composer-managed Base visual.");
                }
                Assert.That(adapter.TryReleaseMaterialization(
                    materialized.Handle,
                    "test",
                    "manager-provisioned-release",
                    out string releaseIssue), Is.True, releaseIssue);
            }
            finally
            {
                if (profile.TryGetVisualContentPrefab(out GameObject profileVisualPrefab))
                {
                    Object.DestroyImmediate(profileVisualPrefab);
                }
                Object.DestroyImmediate(hostRoot);
                Object.DestroyImmediate(runtimeHostPrefabObject);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(slotProfile);
                Object.DestroyImmediate(route);
            }
        }

        private static GameObject CreateActorRoot(out PlayerActorRuntimeHost runtimeHost)
        {
            var actorRoot = new GameObject("Actor Root");
            PlayerActorDeclaration declaration = actorRoot.AddComponent<PlayerActorDeclaration>();
            runtimeHost = actorRoot.AddComponent<PlayerActorRuntimeHost>();
            SetPrivateField(runtimeHost, "playerActorDeclaration", declaration);
            return actorRoot;
        }

        private static SceneProvidedLocalPlayerAuthoring CreateSceneProvidedActor(out GameObject hostRoot)
        {
            hostRoot = new GameObject("Scene Provided Local Player");
            hostRoot.SetActive(false);
            PlayerInput playerInput = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring localPlayerHost = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            var actorMountObject = new GameObject("Actor Mount");
            actorMountObject.transform.SetParent(hostRoot.transform, false);
            GameObject actorRoot = CreateActorRoot(out PlayerActorRuntimeHost runtimeHost);
            actorRoot.transform.SetParent(actorMountObject.transform, false);
            SetPrivateField(localPlayerHost, "playerInput", playerInput);
            SetPrivateField(localPlayerHost, "actorMount", actorMountObject.transform);
            SetPrivateField(localPlayerHost, "playerActorRuntimeHostPrefab", runtimeHost);

            PlayerSlotProfile slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            ActorProfile actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            SceneProvidedLocalPlayerAuthoring authoring =
                hostRoot.AddComponent<SceneProvidedLocalPlayerAuthoring>();
            SetPrivateField(authoring, "localPlayerHost", localPlayerHost);
            SetPrivateField(authoring, "playerSlotProfile", slotProfile);
            SetPrivateField(authoring, "actorProfile", actorProfile);
            return authoring;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }

    public sealed class ActorMaterializationReleasePlayModeTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private bool _originalEnterPlayModeOptionsEnabled;
        private EnterPlayModeOptions _originalEnterPlayModeOptions;

        [SetUp]
        public void ConfigurePlayModeOptions()
        {
            _originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload;
        }

        [UnityTest]
        public IEnumerator ManagedVisualRelease_DoesNotReactivateVisualWhenDetachingFromInactiveActor()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(Application.isPlaying, Is.True,
                "The visual release regression requires Play Mode and deferred Unity destruction.");

            const string sessionId = "session.actor-release-visual-reactivation";
            var logger = FrameworkLogger.Create<ActorMaterializationReleasePlayModeTests>();
            var runtime = new RuntimeContentRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session(sessionId, "Actor release regression");
            Assert.That(runtime.CreateScopeRoot(owner, "d01-test", "setup").Applied, Is.True);
            Assert.That(runtime.TryCreateScopeContext(owner, "d01-test", "setup", out RuntimeScopeContext scope), Is.True);

            ActorProfile profile = Track(ScriptableObject.CreateInstance<ActorProfile>());
            PlayerSlotProfile slotProfile = Track(ScriptableObject.CreateInstance<PlayerSlotProfile>());
            RouteAsset route = Track(ScriptableObject.CreateInstance<RouteAsset>());
            SetPrivateField(route, "routeId", "actor-release-regression");

            GameObject hostRoot = Track(new GameObject("Local Player Host"));
            hostRoot.SetActive(false);
            PlayerInput playerInput = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring localPlayerHost = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = Track(new GameObject("Actor Mount"));
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetPrivateField(localPlayerHost, "playerInput", playerInput);
            SetPrivateField(localPlayerHost, "actorMount", actorMount.transform);

            var discoveryScope = new RouteContentDiscoveryScope(
                route,
                Array.Empty<RouteContentDiscoveryScene>(),
                Array.Empty<GameObject>());
            var routeBinding = hostRoot.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            routeBinding.Configure(new RoutePlayerSpatialEntryContext(route, 1, discoveryScope));

            GameObject runtimeHostPrefabObject = Track(new GameObject("Player Actor Runtime Host Prefab"));
            PlayerActorDeclaration prefabDeclaration = runtimeHostPrefabObject.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost runtimeHostPrefab = runtimeHostPrefabObject.AddComponent<PlayerActorRuntimeHost>();
            SetPrivateField(runtimeHostPrefab, "playerActorDeclaration", prefabDeclaration);
            GameObject visualMount = Track(new GameObject("Visual Content Mount"));
            visualMount.transform.SetParent(runtimeHostPrefabObject.transform, false);
            SetPrivateField(runtimeHostPrefab, "presentationMount", visualMount.transform);

            GameObject visualPrefab = Track(new GameObject("Actor Visual Prefab"));
            visualPrefab.SetActive(false);
            visualPrefab.AddComponent<ActorVisualReleaseLifecycleProbe>();
            SetActorVisualContent(profile, visualPrefab);
            SetPrivateField(localPlayerHost, "playerActorRuntimeHostPrefab", runtimeHostPrefab);

            var slot = new PlayerSlotRuntimeSnapshot(
                0,
                slotProfile,
                PlayerSlotId.Player1,
                PlayerSlotAllocationState.Joined,
                default,
                default,
                1,
                "d01-test",
                "setup",
                profile,
                1,
                "d01-test",
                "selection");

            Assert.That(localPlayerHost.TryRestoreCommittedAdmission(
                slot,
                "d01-test",
                "setup",
                false,
                null,
                out string admissionIssue), Is.True, admissionIssue);
            hostRoot.SetActive(true);

            var adapter = new AttachedPlayerActorMaterializationAdapter(runtime, sessionId);
            ActorId actorId = ActorId.From("actor.d01-release-visual-regression");
            PlayerActorMaterializationResult materialized = adapter.TryMaterialize(
                scope,
                slot,
                profile,
                actorId,
                localPlayerHost,
                "d01-test",
                "materialize-for-release-regression");

            Assert.That(materialized.Succeeded, Is.True, materialized.ToDiagnosticString());
            PlayerActorMaterializationHandle handle = materialized.Handle;
            GameObject visual = materialized.VisualContent;
            ActorVisualReleaseLifecycleProbe probe = visual.GetComponent<ActorVisualReleaseLifecycleProbe>();
            probe.Initialize(owner.StableText);
            Assert.That(handle.RuntimeContentRequest.Identity.Owner, Is.EqualTo(owner));
            Assert.That(probe.OnEnableCount, Is.Zero, "Staged visual content is inactive before Actor activation.");

            Assert.That(handle.TryActivate("d01-test", "activate-for-release-regression", out string activationIssue),
                Is.True, activationIssue);
            Assert.That(probe.OnEnableCount, Is.EqualTo(1));
            Assert.That(probe.OnDisableCount, Is.Zero);

            logger.Info(
                "IF-ADR-050 D01 visual release requested",
                new LogField("owner", owner.StableText),
                new LogField("actorId", handle.Request.ActorId.StableText),
                new LogField("operationId", handle.Request.OperationId.StableText),
                new LogField("visualActiveSelf", visual.activeSelf),
                new LogField("visualActiveInHierarchy", visual.activeInHierarchy));

            bool released = adapter.TryReleaseMaterialization(
                handle,
                "d01-test",
                "release-active-visual",
                out string releaseIssue);

            logger.Info(
                "IF-ADR-050 D01 visual release outcome",
                new LogField("owner", owner.StableText),
                new LogField("released", released),
                new LogField("handleState", handle.State.ToString()),
                new LogField("issue", releaseIssue),
                new LogField("onEnableCount", probe.OnEnableCount),
                new LogField("onDisableCount", probe.OnDisableCount));

            Assert.That(released, Is.True, releaseIssue);
            Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Released));
            Assert.That(probe.OnDisableCount, Is.EqualTo(1), "Deactivating the Actor root disables the visual once.");
            Assert.That(probe.OnEnableCount, Is.EqualTo(1), "Detaching the visual must not reactivate it before deferred Destroy.");
            Assert.That(visual.activeSelf, Is.False);
            Assert.That(visual.activeInHierarchy, Is.False);
            Assert.That(visual.transform.parent, Is.Null);
            Assert.That(runtime.TryGetHandle(
                handle.RuntimeContentRequest.Context,
                handle.RuntimeContentRequest.Identity,
                out _), Is.False, "Successful logical release unregisters only this owner's RuntimeContent handle.");

            yield return null;
            Assert.That(visual == null, Is.True, "Physical Destroy is observed after the frame; scheduling it alone is not treated as completion.");
        }

        [UnityTearDown]
        public IEnumerator TearDownPlayMode()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.Destroy(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
            if (Application.isPlaying)
            {
                yield return null;
                yield return new ExitPlayMode();
            }

            EditorSettings.enterPlayModeOptionsEnabled = _originalEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = _originalEnterPlayModeOptions;
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void SetActorVisualContent(ActorProfile profile, GameObject prefab)
        {
            SetPrivateField(
                profile,
                "compositionParts",
                new List<ActorCompositionPart>
                {
                    new ActorCompositionPart("visual", prefab)
                });
        }
    }

    internal sealed class ActorVisualReleaseLifecycleProbe : MonoBehaviour
    {
        private FrameworkLogger _logger;
        private string _owner;

        internal int OnEnableCount { get; private set; }
        internal int OnDisableCount { get; private set; }

        internal void Initialize(string owner)
        {
            _owner = owner;
            _logger = FrameworkLogger.Create<ActorVisualReleaseLifecycleProbe>();
        }

        private void OnEnable()
        {
            OnEnableCount++;
            Record("OnEnable");
        }

        private void OnDisable()
        {
            OnDisableCount++;
            Record("OnDisable");
        }

        private void Record(string callback)
        {
            _logger?.Info(
                "IF-ADR-050 D01 visual lifecycle callback",
                new LogField("owner", _owner ?? string.Empty),
                new LogField("callback", callback),
                new LogField("activeSelf", gameObject.activeSelf),
                new LogField("activeInHierarchy", gameObject.activeInHierarchy));
        }
    }
}
