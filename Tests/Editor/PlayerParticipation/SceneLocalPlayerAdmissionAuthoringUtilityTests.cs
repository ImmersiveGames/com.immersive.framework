using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerParticipation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation.Editor.Tests
{
    public sealed class SceneLocalPlayerAdmissionAuthoringUtilityTests
    {
        private const string TestRootFolder = "Assets/ImmersiveFrameworkEditorTests";
        private const string AssetFolder = TestRootFolder + "/SceneLocalPlayerAdmission";

        private GameObject sceneHost;
        private ActorProfile actorProfile;
        private PlayerSlotProfile playerSlotProfile;

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
        public void ApplyOrRebuild_SamePrefabSource_CreatesCompatibleEvidenceAndRemainsIdempotent()
        {
            GameObject prefab = CreatePresentationPrefab("Presentation_A");
            SceneLocalPlayerAdmissionAuthoring authoring = CreateAuthoring(prefab, prefab);

            SceneLocalPlayerAdmissionAuthoringResult first =
                SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(authoring, false, false);
            SceneLocalPlayerAdmissionAuthoringResult validation =
                SceneLocalPlayerAdmissionAuthoringUtility.Validate(authoring, false);
            SceneLocalPlayerAdmissionAuthoringResult second =
                SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(authoring, false, false);

            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(first.Status, Is.EqualTo(SceneLocalPlayerAdmissionAuthoringStatus.Valid));
            Assert.That(first.EvidenceCreated, Is.True);
            Assert.That(authoring.HasTypedActorEvidence, Is.True);
            Assert.That(authoring.IsTypedActorEvidenceCompatibleWith(actorProfile), Is.True);
            Assert.That(validation.Succeeded, Is.True, validation.Message);
            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(second.EvidenceCreated, Is.False);
            Assert.That(second.EvidenceUpdated, Is.False);
        }

        [Test]
        public void ApplyOrRebuild_DifferentPrefabSource_RejectsWithoutWritingEvidence()
        {
            GameObject profilePrefab = CreatePresentationPrefab("Presentation_A");
            GameObject scenePrefab = CreatePresentationPrefab("Presentation_B");
            SceneLocalPlayerAdmissionAuthoring authoring = CreateAuthoring(profilePrefab, scenePrefab);

            SceneLocalPlayerAdmissionAuthoringResult result =
                SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(authoring, false, false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence));
            Assert.That(authoring.HasTypedActorEvidence, Is.False);
        }

        [Test]
        public void ApplyOrRebuild_ActorWithoutPrefabSource_RejectsWithoutWritingEvidence()
        {
            GameObject profilePrefab = CreatePresentationPrefab("Presentation_A");
            SceneLocalPlayerAdmissionAuthoring authoring = CreateAuthoring(profilePrefab, null);

            SceneLocalPlayerAdmissionAuthoringResult result =
                SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(authoring, false, false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(SceneLocalPlayerAdmissionAuthoringStatus.IncompatibleProfileEvidence));
            Assert.That(authoring.HasTypedActorEvidence, Is.False);
        }

        private SceneLocalPlayerAdmissionAuthoring CreateAuthoring(
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

            sceneHost = new GameObject("Scene Local Player Host");
            PlayerInput playerInput = sceneHost.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = sceneHost.AddComponent<LocalPlayerHostAuthoring>();
            Transform actorMount = new GameObject("Actor Mount").transform;
            actorMount.SetParent(sceneHost.transform);
            GameObject runtimeHostPrefab = CreateRuntimeHostPrefab("PlayerRuntimeHost");
            PlayerActorRuntimeHost sceneRuntimeHost =
                ((GameObject)PrefabUtility.InstantiatePrefab(runtimeHostPrefab, actorMount))
                    .GetComponent<PlayerActorRuntimeHost>();
            GameObject scenePresentation = scenePrefab == null
                ? new GameObject("Unconnected Presentation")
                : PrefabUtility.InstantiatePrefab(
                    scenePrefab,
                    sceneRuntimeHost.PresentationMount) as GameObject;
            if (scenePrefab == null)
            {
                scenePresentation.transform.SetParent(
                    sceneRuntimeHost.PresentationMount,
                    false);
            }
            SceneLocalPlayerAdmissionAuthoring authoring =
                sceneHost.AddComponent<SceneLocalPlayerAdmissionAuthoring>();

            SetProperty(host, "playerInput", playerInput);
            SetProperty(host, "actorMount", actorMount);
            SetProperty(
                host,
                "playerActorRuntimeHostPrefab",
                runtimeHostPrefab.GetComponent<PlayerActorRuntimeHost>());
            SetProperty(authoring, "playerSlotProfile", playerSlotProfile);
            SetProperty(authoring, "actorProfile", actorProfile);
            SetProperty(authoring, "scenePlayerActorRuntimeHost", sceneRuntimeHost);
            SetProperty(authoring, "scenePresentation", scenePresentation);
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
                AssetDatabase.CreateFolder(TestRootFolder, "SceneLocalPlayerAdmission");
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
