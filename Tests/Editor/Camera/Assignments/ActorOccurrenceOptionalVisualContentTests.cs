using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

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
                Assert.That(profile.VisualContentPrefab, Is.Null);
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
                Object.DestroyImmediate(authoring.ActorProfile);
                Object.DestroyImmediate(authoring.PlayerSlotProfile);
                Object.DestroyImmediate(hostObject);
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
                Assert.That(handle.TryActivate("test", "activate", out string activateIssue), Is.True, activateIssue);
                Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Active));
                Assert.That(handle.TryDeactivate("test", "deactivate", out string deactivateIssue), Is.True, deactivateIssue);
                Assert.That(adapter.TryReleaseMaterialization(handle, "test", "release", out string releaseIssue), Is.True, releaseIssue);
                Assert.That(handle.State, Is.EqualTo(PlayerActorMaterializationState.Released));
            }
            finally
            {
                if (localPlayerRoot != null) Object.DestroyImmediate(localPlayerRoot);
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
                SetPrivateField(profile, "presentationPrefab", visualPrefab);
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
                    host,
                    "test",
                    "manager-provisioned-materialize");

                Assert.That(materialized.Succeeded, Is.True, materialized.ToDiagnosticString());
                Assert.That(materialized.HasPhysicalEvidence, Is.True);
                Assert.That(materialized.VisualContent != null, Is.EqualTo(withVisualContent));
                Assert.That(adapter.TryReleaseMaterialization(
                    materialized.Handle,
                    "test",
                    "manager-provisioned-release",
                    out string releaseIssue), Is.True, releaseIssue);
            }
            finally
            {
                if (profile.VisualContentPrefab != null)
                {
                    Object.DestroyImmediate(profile.VisualContentPrefab);
                }
                Object.DestroyImmediate(hostRoot);
                Object.DestroyImmediate(runtimeHostPrefabObject);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(slotProfile);
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
}
