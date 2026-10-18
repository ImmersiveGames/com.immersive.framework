using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ActorProfileVisualContentMigrationTests
    {
        private const string TestRoot = "Assets/__IFADR050ActorProfileMigrationTests";
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = $"{TestRoot}_{Guid.NewGuid():N}";
            string folderGuid = AssetDatabase.CreateFolder(
                "Assets",
                _folder.Substring("Assets/".Length));
            Assert.IsNotEmpty(folderGuid, $"Could not create temporary test folder '{_folder}'.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_folder) && AssetDatabase.IsValidFolder(_folder))
            {
                Assert.IsTrue(AssetDatabase.DeleteAsset(_folder));
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Migration_ConvertsLegacyReferenceAndIsIdempotent()
        {
            GameObject prefab = CreatePrefab("ActorVisual");
            ActorProfile profile = CreateProfile("LegacyActor");
            PlayerSlotProfile slotProfile = CreateSlotProfile("DefaultPlayer", profile);
            SetLegacyVisual(profile, prefab);
            string actorProfileId = profile.ActorProfileIdText;
            string displayName = profile.DisplayName;
            string profileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
            string prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            string slotProfileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(slotProfile));

            ActorProfileVisualContentMigrationResult first =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, first.Status);
            var serialized = new SerializedObject(profile);
            SerializedProperty parts = serialized.FindProperty("compositionParts");
            Assert.AreEqual(1, parts.arraySize);
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            Assert.AreEqual("visual", part.FindPropertyRelative("key").stringValue);
            Assert.AreSame(prefab, part.FindPropertyRelative("prefab").objectReferenceValue);
            Assert.IsNull(serialized.FindProperty("legacyPresentationPrefab").objectReferenceValue);
            Assert.AreEqual(profileGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)));
            Assert.AreEqual(prefabGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
            Assert.AreEqual(slotProfileGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(slotProfile)));
            Assert.AreSame(profile, slotProfile.DefaultActorProfile,
                "The selected/default Profile reference remains the same asset after its visual data migration.");
            Assert.AreEqual(actorProfileId, profile.ActorProfileIdText);
            Assert.AreEqual(displayName, profile.DisplayName);

            ActorProfileVisualContentMigrationResult second =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            serialized.Update();
            parts = serialized.FindProperty("compositionParts");
            part = parts.GetArrayElementAtIndex(0);
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Unchanged, second.Status);
            Assert.AreEqual(1, parts.arraySize);
            Assert.AreSame(prefab, part.FindPropertyRelative("prefab").objectReferenceValue);
        }

        [Test]
        public void Migration_PreservesNullProfileWithoutCreatingContent()
        {
            ActorProfile profile = CreateProfile("NoVisualActor");

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Ignored, result.Status);
            var serialized = new SerializedObject(profile);
            Assert.AreEqual(0, serialized.FindProperty("compositionParts").arraySize);
            Assert.IsNull(serialized.FindProperty("legacyPresentationPrefab").objectReferenceValue);
        }

        [Test]
        public void Migration_ReportsConflictingVisualWithoutChangingEitherReference()
        {
            GameObject legacyPrefab = CreatePrefab("LegacyVisual");
            GameObject authoredPrefab = CreatePrefab("AuthoredVisual");
            ActorProfile profile = CreateProfile("ConflictingActor");
            SetLegacyVisual(profile, legacyPrefab);
            SetAuthoredVisual(profile, authoredPrefab);

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            var serialized = new SerializedObject(profile);
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Conflict, result.Status);
            Assert.AreSame(legacyPrefab,
                serialized.FindProperty("legacyPresentationPrefab").objectReferenceValue);
            Assert.AreSame(authoredPrefab,
                serialized.FindProperty("compositionParts")
                    .GetArrayElementAtIndex(0)
                    .FindPropertyRelative("prefab")
                    .objectReferenceValue);
        }

        [Test]
        public void ManagerProvisioningValidation_RequiresMountForDefaultProfileComposition()
        {
            GameObject visualPrefab = CreatePrefab("DefaultActorVisual");
            ActorProfile actorProfile = CreateProfile("ComposedDefaultActor");
            SetAuthoredVisual(actorProfile, visualPrefab);
            PlayerSlotProfile slotProfile = CreateSlotProfile("ComposedDefaultSlot", actorProfile);
            PlayerSessionProfile sessionProfile = CreateSessionProfile(slotProfile);
            GameObject runtimeHostObject = new GameObject("Runtime Host Prefab");
            PlayerActorRuntimeHost runtimeHost =
                runtimeHostObject.AddComponent<PlayerActorRuntimeHost>();

            try
            {
                var missingMountReport = new FrameworkAuthoringValidationReport();
                LocalPlayerProvisioningValidator.ValidateDefaultActorCompositionMounts(
                    sessionProfile,
                    runtimeHost,
                    missingMountReport);

                Assert.AreEqual(1, missingMountReport.ErrorCount);
                StringAssert.Contains("has no Visual Content Mount", missingMountReport.Issues[0].Message);

                var mountObject = new GameObject("Visual Content Mount");
                mountObject.transform.SetParent(runtimeHostObject.transform, false);
                var serializedRuntimeHost = new SerializedObject(runtimeHost);
                serializedRuntimeHost.FindProperty("presentationMount").objectReferenceValue =
                    mountObject.transform;
                serializedRuntimeHost.ApplyModifiedPropertiesWithoutUndo();

                var configuredMountReport = new FrameworkAuthoringValidationReport();
                LocalPlayerProvisioningValidator.ValidateDefaultActorCompositionMounts(
                    sessionProfile,
                    runtimeHost,
                    configuredMountReport);

                Assert.AreEqual(0, configuredMountReport.ErrorCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runtimeHostObject);
            }
        }

        private ActorProfile CreateProfile(string name)
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            AssetDatabase.CreateAsset(profile, $"{_folder}/{name}.asset");
            return profile;
        }

        private PlayerSlotProfile CreateSlotProfile(string name, ActorProfile defaultActorProfile)
        {
            PlayerSlotProfile profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            AssetDatabase.CreateAsset(profile, $"{_folder}/{name}.asset");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("defaultActorProfile").objectReferenceValue = defaultActorProfile;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        private PlayerSessionProfile CreateSessionProfile(PlayerSlotProfile slotProfile)
        {
            PlayerSessionProfile profile = ScriptableObject.CreateInstance<PlayerSessionProfile>();
            AssetDatabase.CreateAsset(profile, $"{_folder}/PlayerSession.asset");
            var serialized = new SerializedObject(profile);
            SerializedProperty slots = serialized.FindProperty("supportedSlots");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slotProfile;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        private GameObject CreatePrefab(string name)
        {
            var source = new GameObject(name);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, $"{_folder}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            Assert.IsNotNull(prefab);
            return prefab;
        }

        private static void SetLegacyVisual(ActorProfile profile, GameObject prefab)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("legacyPresentationPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private static void SetAuthoredVisual(ActorProfile profile, GameObject prefab)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty parts = serialized.FindProperty("compositionParts");
            parts.arraySize = 1;
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            part.FindPropertyRelative("key").stringValue = "visual";
            part.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            part.FindPropertyRelative("dependencies").arraySize = 0;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
