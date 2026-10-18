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
        public void Migration_ConvertsLegacyReferenceToPartAssetAndIsIdempotent()
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
            SerializedProperty legacyParts = serialized.FindProperty("compositionParts");
            SerializedProperty parts = serialized.FindProperty("baseContentParts");
            Assert.AreEqual(0, legacyParts.arraySize);
            Assert.AreEqual(1, parts.arraySize);
            ActorCompositionPartAsset partAsset =
                parts.GetArrayElementAtIndex(0).objectReferenceValue as ActorCompositionPartAsset;
            Assert.IsNotNull(partAsset);
            var serializedPart = new SerializedObject(partAsset);
            Assert.AreEqual("visual", serializedPart.FindProperty("key").stringValue);
            Assert.AreSame(prefab, serializedPart.FindProperty("prefab").objectReferenceValue);
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
            parts = serialized.FindProperty("baseContentParts");
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Unchanged, second.Status);
            Assert.AreEqual(1, parts.arraySize);
            Assert.AreSame(partAsset, parts.GetArrayElementAtIndex(0).objectReferenceValue);
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
            Assert.AreEqual(0, serialized.FindProperty("baseContentParts").arraySize);
            Assert.IsNull(serialized.FindProperty("legacyPresentationPrefab").objectReferenceValue);
        }

        [Test]
        public void Migration_ReportsConflictingVisualWithoutChangingEitherReference()
        {
            GameObject legacyPrefab = CreatePrefab("LegacyVisual");
            GameObject authoredPrefab = CreatePrefab("AuthoredVisual");
            ActorProfile profile = CreateProfile("ConflictingActor");
            SetLegacyVisual(profile, legacyPrefab);
            SetLegacyInlineVisual(profile, authoredPrefab);

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
        public void Migration_ConvertsOrderedInlinePartsAndPreservesDependencies()
        {
            GameObject visualPrefab = CreatePrefab("OrderedVisual");
            GameObject trimPrefab = CreatePrefab("OrderedTrim");
            ActorProfile profile = CreateProfile("OrderedProfile");
            SetLegacyInlineParts(
                profile,
                new LegacyPart("visual", visualPrefab),
                new LegacyPart("trim", trimPrefab, "visual"));

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, result.Status, result.Message);
            var serialized = new SerializedObject(profile);
            Assert.AreEqual(0, serialized.FindProperty("compositionParts").arraySize);
            SerializedProperty parts = serialized.FindProperty("baseContentParts");
            Assert.AreEqual(2, parts.arraySize);
            ActorCompositionPartAsset visual =
                parts.GetArrayElementAtIndex(0).objectReferenceValue as ActorCompositionPartAsset;
            ActorCompositionPartAsset trim =
                parts.GetArrayElementAtIndex(1).objectReferenceValue as ActorCompositionPartAsset;
            Assert.IsNotNull(visual);
            Assert.IsNotNull(trim);
            var visualData = new SerializedObject(visual);
            var trimData = new SerializedObject(trim);
            Assert.AreEqual("visual", visualData.FindProperty("key").stringValue);
            Assert.AreSame(visualPrefab, visualData.FindProperty("prefab").objectReferenceValue);
            Assert.AreEqual("trim", trimData.FindProperty("key").stringValue);
            Assert.AreSame(trimPrefab, trimData.FindProperty("prefab").objectReferenceValue);
            Assert.AreEqual("visual", trimData.FindProperty("dependencies").GetArrayElementAtIndex(0).stringValue);
            Assert.That(AssetDatabase.GetAssetPath(visual), Does.StartWith(_folder + "/"));
            Assert.That(AssetDatabase.GetAssetPath(trim), Does.StartWith(_folder + "/"));
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(visual)),
                Is.Not.EqualTo(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(trim))));
        }

        [Test]
        public void Migration_RejectsInvalidDependenciesWithoutChangingSourceOrCreatingAssets()
        {
            GameObject visualPrefab = CreatePrefab("InvalidVisual");
            GameObject trimPrefab = CreatePrefab("InvalidTrim");
            ActorProfile profile = CreateProfile("InvalidDependencyProfile");
            SetLegacyInlineParts(
                profile,
                new LegacyPart("visual", visualPrefab),
                new LegacyPart("trim", trimPrefab, "missing"));
            string profilePath = AssetDatabase.GetAssetPath(profile);

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            var serialized = new SerializedObject(profile);
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Invalid, result.Status);
            Assert.AreEqual(2, serialized.FindProperty("compositionParts").arraySize);
            Assert.AreEqual(0, serialized.FindProperty("baseContentParts").arraySize);
            Assert.IsFalse(AssetDatabase.IsValidFolder(
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(profilePath), "InvalidDependencyProfile_BaseContentParts").Replace('\\', '/')));
        }

        [Test]
        public void Migration_BlocksProfileOutsideAssetsWithoutMutation()
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            GameObject prefab = CreatePrefab("UnsavedVisual");
            SetLegacyVisual(profile, prefab);
            try
            {
                ActorProfileVisualContentMigrationResult result =
                    ActorProfileVisualContentMigration.MigrateAsset(profile);

                Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Blocked, result.Status);
                Assert.AreEqual(string.Empty, AssetDatabase.GetAssetPath(profile));
                Assert.IsTrue(profile.HasLegacyPresentationPrefab);
                Assert.AreEqual(0, profile.BaseContentParts.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Migration_MatchingLegacyAndInlineVisualProducesOnePartAsset()
        {
            GameObject prefab = CreatePrefab("MatchingVisual");
            ActorProfile profile = CreateProfile("MatchingProfile");
            SetLegacyVisual(profile, prefab);
            SetLegacyInlineVisual(profile, prefab);

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, result.Status, result.Message);
            Assert.AreEqual(1, profile.BaseContentParts.Count);
            Assert.AreEqual("visual", profile.BaseContentParts[0].Key);
            Assert.AreSame(prefab, profile.BaseContentParts[0].Prefab);
            Assert.IsFalse(profile.HasLegacyPresentationPrefab);
        }

        [Test]
        public void Migration_DoesNotDeduplicateEqualPartsAcrossProfiles()
        {
            GameObject prefab = CreatePrefab("SharedValuePrefab");
            ActorProfile first = CreateProfile("FirstEqualProfile");
            ActorProfile second = CreateProfile("SecondEqualProfile");
            SetLegacyInlineVisual(first, prefab);
            SetLegacyInlineVisual(second, prefab);

            ActorProfileVisualContentMigrationResult firstResult =
                ActorProfileVisualContentMigration.MigrateAsset(first);
            ActorProfileVisualContentMigrationResult secondResult =
                ActorProfileVisualContentMigration.MigrateAsset(second);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, firstResult.Status);
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, secondResult.Status);
            Assert.That(first.BaseContentParts[0], Is.Not.SameAs(second.BaseContentParts[0]));
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(first.BaseContentParts[0])),
                Is.Not.EqualTo(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(second.BaseContentParts[0]))));
        }

        [Test]
        public void Migration_ReusesMatchingPartialOutputAndClearsSourceAfterVerification()
        {
            GameObject prefab = CreatePrefab("PartialVisual");
            ActorProfile profile = CreateProfile("PartialProfile");
            SetLegacyInlineVisual(profile, prefab);
            string partFolder = $"{_folder}/{profile.name}_BaseContentParts";
            Assert.IsNotEmpty(AssetDatabase.CreateFolder(_folder, $"{profile.name}_BaseContentParts"));
            ActorCompositionPartAsset priorOutput = CreatePartAsset(
                $"{partFolder}/00_visual.asset",
                "visual",
                prefab);

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Migrated, result.Status, result.Message);
            Assert.That(profile.BaseContentParts.Count, Is.EqualTo(1));
            Assert.That(profile.BaseContentParts[0], Is.SameAs(priorOutput));
            Assert.That(profile.HasLegacyCompositionData, Is.False);
        }

        [Test]
        public void Migration_ConflictsWithMismatchedPartialOutputWithoutOverwritingIt()
        {
            GameObject sourcePrefab = CreatePrefab("SourceVisual");
            GameObject otherPrefab = CreatePrefab("OtherVisual");
            ActorProfile profile = CreateProfile("MismatchedPartialProfile");
            SetLegacyInlineVisual(profile, sourcePrefab);
            string partFolder = $"{_folder}/{profile.name}_BaseContentParts";
            Assert.IsNotEmpty(AssetDatabase.CreateFolder(_folder, $"{profile.name}_BaseContentParts"));
            ActorCompositionPartAsset existing = CreatePartAsset(
                $"{partFolder}/00_visual.asset",
                "visual",
                otherPrefab);

            ActorProfileVisualContentMigrationResult result =
                ActorProfileVisualContentMigration.MigrateAsset(profile);

            var serialized = new SerializedObject(profile);
            Assert.AreEqual(ActorProfileVisualContentMigrationStatus.Conflict, result.Status);
            Assert.That(profile.HasLegacyCompositionParts, Is.True);
            Assert.That(serialized.FindProperty("baseContentParts").arraySize, Is.Zero);
            Assert.That(profile.LegacyCompositionParts[0].Prefab, Is.SameAs(sourcePrefab));
            Assert.That(existing.Prefab, Is.SameAs(otherPrefab));
        }

        [Test]
        public void ManagerProvisioningValidation_RequiresMountForDefaultProfileComposition()
        {
            GameObject visualPrefab = CreatePrefab("DefaultActorVisual");
            ActorProfile actorProfile = CreateProfile("ComposedDefaultActor");
            SetBaseVisual(actorProfile, visualPrefab);
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

        private void SetLegacyInlineVisual(ActorProfile profile, GameObject prefab)
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

        private void SetLegacyInlineParts(ActorProfile profile, params LegacyPart[] sourceParts)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty parts = serialized.FindProperty("compositionParts");
            parts.arraySize = sourceParts.Length;
            for (int index = 0; index < sourceParts.Length; index++)
            {
                SerializedProperty part = parts.GetArrayElementAtIndex(index);
                part.FindPropertyRelative("key").stringValue = sourceParts[index].Key;
                part.FindPropertyRelative("prefab").objectReferenceValue = sourceParts[index].Prefab;
                SerializedProperty dependencies = part.FindPropertyRelative("dependencies");
                dependencies.arraySize = sourceParts[index].Dependencies.Length;
                for (int dependencyIndex = 0; dependencyIndex < sourceParts[index].Dependencies.Length; dependencyIndex++)
                {
                    dependencies.GetArrayElementAtIndex(dependencyIndex).stringValue =
                        sourceParts[index].Dependencies[dependencyIndex];
                }
            }

            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private void SetBaseVisual(ActorProfile profile, GameObject prefab)
        {
            ActorCompositionPartAsset part = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
            var serializedPart = new SerializedObject(part);
            serializedPart.FindProperty("key").stringValue = "visual";
            serializedPart.FindProperty("prefab").objectReferenceValue = prefab;
            serializedPart.FindProperty("dependencies").arraySize = 0;
            serializedPart.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(part, $"{_folder}/{profile.name}_visual.asset");

            var serializedProfile = new SerializedObject(profile);
            SerializedProperty parts = serializedProfile.FindProperty("baseContentParts");
            parts.arraySize = 1;
            parts.GetArrayElementAtIndex(0).objectReferenceValue = part;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private ActorCompositionPartAsset CreatePartAsset(string path, string key, GameObject prefab)
        {
            ActorCompositionPartAsset part = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
            var serialized = new SerializedObject(part);
            serialized.FindProperty("key").stringValue = key;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("dependencies").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(part, path);
            return part;
        }

        private readonly struct LegacyPart
        {
            internal LegacyPart(string key, GameObject prefab, params string[] dependencies)
            {
                Key = key;
                Prefab = prefab;
                Dependencies = dependencies ?? Array.Empty<string>();
            }

            internal string Key { get; }
            internal GameObject Prefab { get; }
            internal string[] Dependencies { get; }
        }
    }
}
