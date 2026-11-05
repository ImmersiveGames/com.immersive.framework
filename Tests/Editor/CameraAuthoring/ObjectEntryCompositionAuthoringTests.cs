using System.Collections.Generic;
using Immersive.Framework.ObjectEntry;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ObjectEntryCompositionAuthoringTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    Object.DestroyImmediate(_owned[index]);
            }

            _owned.Clear();
        }

        [Test]
        public void DeclarationWithoutCompositionAuthoring_DoesNotRequireProfile()
        {
            GameObject root = MakeObject("Entry");
            ObjectEntryDeclaration declaration = root.AddComponent<ObjectEntryDeclaration>();
            SetDeclarationId(declaration, "qa.composition.no-profile");

            Assert.IsTrue(declaration.HasObjectEntryId);
            Assert.IsNull(root.GetComponent<ObjectEntryCompositionAuthoring>());
        }

        [Test]
        public void Profile_Empty_IsExplicitlyInvalid()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("at least one", issue);
        }

        [Test]
        public void Profile_ValidPartAndDependency_Accepts()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            GameObject prefab = MakeObject("Capability Prefab");

            AddPart(profile, "core", prefab);
            AddPart(profile, "presentation", prefab, "core");

            Assert.IsTrue(profile.TryValidate(out string issue), issue);
        }

        [Test]
        public void Profile_DuplicateKey_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            GameObject prefab = MakeObject("Capability Prefab");

            AddPart(profile, "capability", prefab);
            AddPart(profile, "capability", prefab);

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("duplicate part key", issue);
        }

        [Test]
        public void Profile_MissingPrefab_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            AddPart(profile, "capability", null);

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("requires a prefab", issue);
        }

        [Test]
        public void Profile_MissingDependency_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            AddPart(profile, "child", MakeObject("Capability"), "missing");

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("missing dependency", issue);
        }

        [Test]
        public void Profile_CyclicDependency_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            GameObject prefab = MakeObject("Capability");
            AddPart(profile, "first", prefab, "second");
            AddPart(profile, "second", prefab, "first");

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("cycle", issue);
        }

        [Test]
        public void Profile_IdenticalDependency_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            AddPart(profile, "part", MakeObject("Capability"), "other", "other");
            AddPart(profile, "other", MakeObject("Dependency"));

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("duplicate dependency", issue);
        }

        [Test]
        public void Profile_NestedDeclaration_Rejects()
        {
            ObjectEntryCompositionProfile profile = MakeProfile();
            GameObject prefab = MakeObject("Invalid Capability");
            prefab.AddComponent<ObjectEntryDeclaration>();
            AddPart(profile, "capability", prefab);

            Assert.IsFalse(profile.TryValidate(out string issue));
            StringAssert.Contains("forbidden", issue);
        }

        [Test]
        public void Authoring_RequiresValidDeclarationAndExplicitProfile()
        {
            GameObject root = MakeObject("Entry");
            ObjectEntryCompositionAuthoring authoring = root.AddComponent<ObjectEntryCompositionAuthoring>();
            ObjectEntryDeclaration declaration = root.GetComponent<ObjectEntryDeclaration>();

            Assert.IsFalse(authoring.TryValidateConfiguration(out string identityIssue));
            StringAssert.Contains("valid ObjectEntryDeclaration", identityIssue);

            SetDeclarationId(declaration, "qa.composition.authoring");
            Assert.IsFalse(authoring.TryValidateConfiguration(out string missingProfileIssue));
            StringAssert.Contains("explicit Object Entry composition profile", missingProfileIssue);

            ObjectEntryCompositionProfile profile = MakeProfile();
            AddPart(profile, "presentation", MakeObject("Capability"));
            SetAuthoringProfile(authoring, profile);

            Assert.IsTrue(authoring.TryValidateConfiguration(out string validIssue), validIssue);
        }

        private ObjectEntryCompositionProfile MakeProfile()
        {
            ObjectEntryCompositionProfile profile = ScriptableObject.CreateInstance<ObjectEntryCompositionProfile>();
            _owned.Add(profile);
            return profile;
        }

        private GameObject MakeObject(string name)
        {
            var gameObject = new GameObject(name);
            _owned.Add(gameObject);
            return gameObject;
        }

        private static void AddPart(
            ObjectEntryCompositionProfile profile,
            string key,
            GameObject prefab,
            params string[] dependencies)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty entries = serialized.FindProperty("parts");
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;

            SerializedProperty dependencyList = element.FindPropertyRelative("dependencies");
            dependencyList.arraySize = dependencies.Length;
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                dependencyList.GetArrayElementAtIndex(dependencyIndex).stringValue = dependencies[dependencyIndex];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDeclarationId(ObjectEntryDeclaration declaration, string id)
        {
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("objectEntryId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAuthoringProfile(
            ObjectEntryCompositionAuthoring authoring,
            ObjectEntryCompositionProfile profile)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
