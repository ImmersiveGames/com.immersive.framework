using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerParticipation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation.Editor.Tests
{
    public sealed class SceneProvidedLocalPlayerAuthoringUtilityTests
    {
        private const string TestRootFolder = "Assets/ImmersiveFrameworkEditorTests";
        private const string AssetFolder = TestRootFolder + "/SceneProvidedLocalPlayer";

        private GameObject sceneHost;
        private ActorProfile actorProfile;
        private PlayerSlotProfile playerSlotProfile;
        private GameObject runtimeHostPrefab;
        private PlayerActorRuntimeHost sceneRuntimeHost;
        private GameObject authoredPresentation;
        private Transform actorMount;

        [TearDown]
        public void TearDown()
        {
            if (sceneHost != null)
            {
                Object.DestroyImmediate(sceneHost);
            }

            AssetDatabase.DeleteAsset(TestRootFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Validate_AuthoredComposition_SucceedsWithoutMaterialization()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);

            SceneProvidedLocalPlayerAuthoringResult validation =
                SceneProvidedLocalPlayerAuthoringUtility.Validate(authoring, false);

            Assert.That(validation.Succeeded, Is.True, validation.Message);
            Assert.That(validation.Status, Is.EqualTo(SceneProvidedLocalPlayerAuthoringStatus.Valid));
        }

        [Test]
        public void Resolve_ExactDirectRuntimeHostAndPresentation_Succeeds()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);

            bool resolved = SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                authoring,
                out SceneProvidedLocalPlayerComposition composition,
                out string issue);

            Assert.That(resolved, Is.True, issue);
            Assert.That(composition.PlayerActorRuntimeHost, Is.SameAs(sceneRuntimeHost));
            Assert.That(composition.Presentation, Is.SameAs(authoredPresentation));
        }

        [Test]
        public void Resolve_ZeroRuntimeHost_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            Object.DestroyImmediate(sceneRuntimeHost.gameObject);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolve_MultipleRuntimeHosts_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            PrefabUtility.InstantiatePrefab(runtimeHostPrefab, actorMount);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolve_NestedRuntimeHost_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            Transform intermediary = new GameObject("Intermediary").transform;
            intermediary.SetParent(actorMount, false);
            sceneRuntimeHost.transform.SetParent(intermediary, false);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolve_ZeroPresentation_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            Object.DestroyImmediate(authoredPresentation);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolve_NestedPresentationMount_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            Transform intermediary = new GameObject("Presentation Intermediary").transform;
            intermediary.SetParent(sceneRuntimeHost.transform, false);
            sceneRuntimeHost.PresentationMount.SetParent(intermediary, false);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolve_MultiplePresentations_IsRejected()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            PrefabUtility.InstantiatePrefab(prefab, sceneRuntimeHost.PresentationMount);

            Assert.That(
                SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void Validate_WrongRuntimeHostPrefab_IsRejectedByEditorProvenance()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(prefab, prefab);
            GameObject unexpectedRuntimeHostPrefab = CreateRuntimeHostPrefab("UnexpectedRuntimeHost");
            Object.DestroyImmediate(sceneRuntimeHost.gameObject);
            sceneRuntimeHost =
                ((GameObject)PrefabUtility.InstantiatePrefab(unexpectedRuntimeHostPrefab, actorMount))
                    .GetComponent<PlayerActorRuntimeHost>();
            authoredPresentation = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                sceneRuntimeHost.PresentationMount);

            SceneProvidedLocalPlayerAuthoringResult result =
                SceneProvidedLocalPlayerAuthoringUtility.Validate(authoring, false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(SceneProvidedLocalPlayerAuthoringStatus.InvalidHost));
        }

        [Test]
        public void Validate_WrongPresentationPrefab_IsRejectedByEditorProvenance()
        {
            GameObject profilePrefab = CreatePresentationPrefab("Presentation_A");
            GameObject scenePrefab = CreatePresentationPrefab("Presentation_B");
            SceneProvidedLocalPlayerAuthoring authoring = CreateAuthoring(profilePrefab, scenePrefab);

            SceneProvidedLocalPlayerAuthoringResult result =
                SceneProvidedLocalPlayerAuthoringUtility.Validate(authoring, false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(SceneProvidedLocalPlayerAuthoringStatus.InvalidActorProfile));
        }

        private SceneProvidedLocalPlayerAuthoring CreateAuthoring(
            GameObject profilePrefab,
            GameObject scenePrefab)
        {
            EnsureAssetFolder();
            actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            AssetDatabase.CreateAsset(actorProfile, AssetFolder + "/ActorProfile.asset");
            SetProperty(actorProfile, "actorProfileId", "actor-profile.scene-provided");
            SetProperty(actorProfile, "actorKind", (int)ActorKind.Player);
            SetProperty(actorProfile, "actorRole", (int)ActorRole.Protagonist);
            SetProperty(actorProfile, "presentationPrefab", profilePrefab);

            playerSlotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            AssetDatabase.CreateAsset(playerSlotProfile, AssetFolder + "/PlayerSlotProfile.asset");
            SetProperty(playerSlotProfile, "playerSlotId", "player.1");
            AssetDatabase.SaveAssets();

            sceneHost = new GameObject("Scene-Provided Local Player Host");
            PlayerInput playerInput = sceneHost.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = sceneHost.AddComponent<LocalPlayerHostAuthoring>();
            actorMount = new GameObject("Actor Mount").transform;
            actorMount.SetParent(sceneHost.transform);
            runtimeHostPrefab = CreateRuntimeHostPrefab("PlayerRuntimeHost");
            sceneRuntimeHost =
                ((GameObject)PrefabUtility.InstantiatePrefab(runtimeHostPrefab, actorMount))
                    .GetComponent<PlayerActorRuntimeHost>();
            authoredPresentation = PrefabUtility.InstantiatePrefab(
                scenePrefab,
                sceneRuntimeHost.PresentationMount) as GameObject;
            var provisioning =
                new GameObject("Scene-Provided Local Player");
            provisioning.transform.SetParent(sceneHost.transform);
            SceneProvidedLocalPlayerAuthoring authoring =
                provisioning.AddComponent<SceneProvidedLocalPlayerAuthoring>();

            SetProperty(host, "playerInput", playerInput);
            SetProperty(host, "actorMount", actorMount);
            SetProperty(
                host,
                "playerActorRuntimeHostPrefab",
                runtimeHostPrefab.GetComponent<PlayerActorRuntimeHost>());
            SetProperty(authoring, "localPlayerHost", host);
            SetProperty(authoring, "playerSlotProfile", playerSlotProfile);
            SetProperty(authoring, "actorProfile", actorProfile);
            return authoring;
        }

        private static GameObject CreatePresentationPrefab(string name)
        {
            EnsureAssetFolder();
            var source = new GameObject(name);
            string path = AssetFolder + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateRuntimeHostPrefab(string name)
        {
            EnsureAssetFolder();
            var source = new GameObject(name);
            PlayerActorDeclaration declaration =
                source.AddComponent<PlayerActorDeclaration>();
            PlayerActorRuntimeHost runtimeHost =
                source.AddComponent<PlayerActorRuntimeHost>();
            Transform presentationMount =
                new GameObject("Presentation Mount").transform;
            presentationMount.SetParent(source.transform);
            SetProperty(runtimeHost, "playerActorDeclaration", declaration);
            SetProperty(runtimeHost, "presentationMount", presentationMount);
            string path = AssetFolder + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static void EnsureAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(TestRootFolder))
            {
                AssetDatabase.CreateFolder("Assets", "ImmersiveFrameworkEditorTests");
            }

            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.CreateFolder(TestRootFolder, "SceneProvidedLocalPlayer");
            }
        }

        private static void SetProperty(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetProperty(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetProperty(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
