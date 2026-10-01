using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.ObjectEntry;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ObjectEntryIdentityAuthoringTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null) Object.DestroyImmediate(_gameObject);
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
            _gameObject = new GameObject("Entry");
            ObjectEntryDeclaration declaration = _gameObject.AddComponent<ObjectEntryDeclaration>();
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
    }
}
