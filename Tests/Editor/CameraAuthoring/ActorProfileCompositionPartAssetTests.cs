using System;
using Immersive.Framework.Actors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ActorProfileCompositionPartAssetTests
    {
        private const string TestRoot = "Assets/__IFADR050ActorProfilePartAssetTests";
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
        public void PartAssetReferencesPersistInOrderAndCanBeIntentionallyShared()
        {
            GameObject firstPrefab = CreatePrefab("FirstPart");
            GameObject secondPrefab = CreatePrefab("SecondPart");
            ActorCompositionPartAsset first = CreatePartAsset(
                "01_visual.asset",
                "visual",
                firstPrefab,
                Array.Empty<string>());
            ActorCompositionPartAsset second = CreatePartAsset(
                "02_trim.asset",
                "trim",
                secondPrefab,
                new[] { "visual" });
            ActorProfile firstProfile = CreateProfile("FirstProfile");
            ActorProfile sharedProfile = CreateProfile("SharedProfile");
            SetBaseParts(firstProfile, first, second);
            SetBaseParts(sharedProfile, first);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(firstProfile));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(sharedProfile));

            ActorProfile reloadedFirst = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                AssetDatabase.GetAssetPath(firstProfile));
            ActorProfile reloadedShared = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                AssetDatabase.GetAssetPath(sharedProfile));
            string firstPartGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(first));
            string secondPartGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(second));
            Assert.That(reloadedFirst.BaseContentParts.Count, Is.EqualTo(2));
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reloadedFirst.BaseContentParts[0])),
                Is.EqualTo(firstPartGuid));
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reloadedFirst.BaseContentParts[1])),
                Is.EqualTo(secondPartGuid));
            Assert.That(reloadedFirst.BaseContentParts[0].Key, Is.EqualTo("visual"));
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reloadedFirst.BaseContentParts[0].Prefab)),
                Is.EqualTo(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(firstPrefab))));
            CollectionAssert.AreEqual(new[] { "visual" }, reloadedFirst.BaseContentParts[1].Dependencies);
            Assert.That(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reloadedShared.BaseContentParts[0])),
                Is.EqualTo(firstPartGuid));
        }

        [Test]
        public void LoadingProfileDoesNotConvertLegacyInlineValuesAutomatically()
        {
            GameObject prefab = CreatePrefab("LegacyVisual");
            ActorProfile profile = CreateProfile("LegacyProfile");
            SetLegacyInlineVisual(profile, prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(profile));

            ActorProfile reloaded = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                AssetDatabase.GetAssetPath(profile));
            var serialized = new SerializedObject(reloaded);
            SerializedProperty legacyParts = serialized.FindProperty("compositionParts");

            Assert.That(reloaded.BaseContentParts.Count, Is.Zero);
            Assert.That(legacyParts.arraySize, Is.EqualTo(1));
            Assert.That(
                legacyParts.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue,
                Is.SameAs(prefab));
        }

        [Test]
        public void ProfileValidationReadsPartAssetDependenciesAndRejectsCycles()
        {
            GameObject visualPrefab = CreatePrefab("CycleVisual");
            GameObject trimPrefab = CreatePrefab("CycleTrim");
            ActorCompositionPartAsset visual = CreatePartAsset(
                "cycle-visual.asset",
                "visual",
                visualPrefab,
                new[] { "trim" });
            ActorCompositionPartAsset trim = CreatePartAsset(
                "cycle-trim.asset",
                "trim",
                trimPrefab,
                new[] { "visual" });
            ActorProfile profile = CreateProfile("CycleProfile");
            SetBaseParts(profile, visual, trim);

            Assert.That(
                ActorCompositionProfileContract.TryValidate(profile, out string issue),
                Is.False);
            StringAssert.Contains("cycle", issue);
        }

        private ActorProfile CreateProfile(string name)
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            AssetDatabase.CreateAsset(profile, $"{_folder}/{name}.asset");
            return profile;
        }

        private ActorCompositionPartAsset CreatePartAsset(
            string fileName,
            string key,
            GameObject prefab,
            string[] dependencies)
        {
            ActorCompositionPartAsset part = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
            var serialized = new SerializedObject(part);
            serialized.FindProperty("key").stringValue = key;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            SerializedProperty dependencyList = serialized.FindProperty("dependencies");
            dependencyList.arraySize = dependencies.Length;
            for (int index = 0; index < dependencies.Length; index++)
            {
                dependencyList.GetArrayElementAtIndex(index).stringValue = dependencies[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(part, $"{_folder}/{fileName}");
            return part;
        }

        private static void SetBaseParts(ActorProfile profile, params ActorCompositionPartAsset[] parts)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty references = serialized.FindProperty("baseContentParts");
            references.arraySize = parts.Length;
            for (int index = 0; index < parts.Length; index++)
            {
                references.GetArrayElementAtIndex(index).objectReferenceValue = parts[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private static void SetLegacyInlineVisual(ActorProfile profile, GameObject prefab)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty parts = serialized.FindProperty("compositionParts");
            parts.arraySize = 1;
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            part.FindPropertyRelative("key").stringValue = "visual";
            part.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            part.FindPropertyRelative("dependencies").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private GameObject CreatePrefab(string name)
        {
            var source = new GameObject(name);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, $"{_folder}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(source);
            Assert.IsNotNull(prefab);
            return prefab;
        }
    }
}
