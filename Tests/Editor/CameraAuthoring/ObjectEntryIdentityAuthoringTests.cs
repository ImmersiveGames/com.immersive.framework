using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.Authoring;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RouteLifecycle;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ObjectEntryIdentityAuthoringTests
    {
        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private readonly List<Object> _ownedObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _gameObjects.Count - 1; index >= 0; index--)
                if (_gameObjects[index] != null) Object.DestroyImmediate(_gameObjects[index]);
            _gameObjects.Clear();
            for (int index = _ownedObjects.Count - 1; index >= 0; index--)
                if (_ownedObjects[index] != null) Object.DestroyImmediate(_ownedObjects[index]);
            _ownedObjects.Clear();
        }

        [Test]
        public void GenerateObjectEntryId_UsesFrameworkStableIdentityFormat()
        {
            string generated = ImmersiveFrameworkEditorSettingsUtility.GenerateObjectEntryIdText();
            Assert.AreEqual(32, generated.Length);
            Assert.IsTrue(ObjectEntryId.From(generated).IsValid);
        }

        [Test]
        public void RegenerateStableId_ChangesOnlyWhenExplicitlyRequested()
        {
            var gameObject = new GameObject("Entry");
            _gameObjects.Add(gameObject);
            ObjectEntryDeclaration declaration = gameObject.AddComponent<ObjectEntryDeclaration>();
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("objectEntryId").stringValue = "qa.object-entry.original";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ObjectEntryId original = declaration.ObjectEntryId;

            Assert.IsTrue(FrameworkIdentityAuthoringValidator.TryRegenerateStableId(
                declaration, out string previous, out string regenerated, out string issue), issue);

            Assert.AreEqual("qa.object-entry.original", previous);
            Assert.AreNotEqual(original, declaration.ObjectEntryId);
            Assert.AreEqual(regenerated, declaration.ObjectEntryId.Value.Value);
        }

        [Test]
        public void SingleDeclaration_DoesNotReportItselfAsDuplicate()
        {
            ObjectEntryDeclaration declaration = CreateOwnedDeclaration(
                "qa.object-entry.single", CreateRouteOwner());

            FrameworkAuthoringValidationReport report = FrameworkIdentityAuthoringValidator
                .ValidateObjectEntryDeclaration(declaration);

            Assert.AreEqual(0, report.WarningCount);
        }

        [Test]
        public void RegeneratedDeclaration_DoesNotReportItselfAsDuplicate()
        {
            ObjectEntryDeclaration declaration = CreateOwnedDeclaration(
                "qa.object-entry.before-regenerate", CreateRouteOwner());

            Assert.IsTrue(FrameworkIdentityAuthoringValidator.TryRegenerateStableId(
                declaration, out _, out string regenerated, out string issue), issue);
            FrameworkAuthoringValidationReport report = FrameworkIdentityAuthoringValidator
                .ValidateObjectEntryDeclaration(declaration);

            Assert.AreEqual(regenerated, declaration.ObjectEntryId.Value.Value);
            Assert.AreEqual(0, report.WarningCount);
        }

        [Test]
        public void DuplicateId_SameOwner_ReportsOtherDeclaration()
        {
            RouteAsset owner = CreateRouteOwner();
            ObjectEntryDeclaration declaration = CreateOwnedDeclaration("qa.object-entry.same-owner", owner);
            ObjectEntryDeclaration other = CreateOwnedDeclaration("qa.object-entry.same-owner", owner);

            FrameworkAuthoringValidationReport report = FrameworkIdentityAuthoringValidator
                .ValidateObjectEntryDeclaration(declaration);

            Assert.AreEqual(1, report.WarningCount);
            Assert.AreSame(other, report.Issues[0].Context);
        }

        [Test]
        public void DuplicateId_DifferentOwner_IsAllowed()
        {
            RouteAsset firstOwner = CreateRouteOwner();
            RouteAsset secondOwner = CreateRouteOwner();
            ObjectEntryDeclaration declaration = CreateOwnedDeclaration("qa.object-entry.different-owner", firstOwner);
            CreateOwnedDeclaration("qa.object-entry.different-owner", secondOwner);

            FrameworkAuthoringValidationReport report = FrameworkIdentityAuthoringValidator
                .ValidateObjectEntryDeclaration(declaration);

            Assert.AreEqual(0, report.WarningCount);
        }

        private RouteAsset CreateRouteOwner()
        {
            RouteAsset owner = ScriptableObject.CreateInstance<RouteAsset>();
            _ownedObjects.Add(owner);
            return owner;
        }

        private ObjectEntryDeclaration CreateOwnedDeclaration(string id, RouteAsset owner)
        {
            var root = new GameObject("Owner");
            _gameObjects.Add(root);
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            var contributionSerialized = new SerializedObject(contribution);
            contributionSerialized.FindProperty("route").objectReferenceValue = owner;
            contributionSerialized.ApplyModifiedPropertiesWithoutUndo();

            var child = new GameObject("Entry");
            child.transform.SetParent(root.transform);
            ObjectEntryDeclaration declaration = child.AddComponent<ObjectEntryDeclaration>();
            SetId(declaration, id);
            return declaration;
        }

        private static void SetId(ObjectEntryDeclaration declaration, string id)
        {
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("objectEntryId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
