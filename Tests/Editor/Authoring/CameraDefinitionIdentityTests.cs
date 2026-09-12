using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraDefinitionIdentityTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            string folder = "_Camera027_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            _root = "Assets/" + folder;
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_root);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExplicitGeneration_ValidProjection_AndNoImplicitReplacement(bool view)
        {
            var definition = Create(view);
            Assert.Throws<InvalidOperationException>(() => ValidateScope(definition));
            Assert.Throws<InvalidOperationException>(() => Id(definition));
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            string id = Id(definition);
            Assert.That(Guid.TryParseExact(id, "N", out _), Is.True);
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(definition), Is.Null);
            Assert.DoesNotThrow(() => ValidateScope(definition, definition));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionIdentityEditorUtility.RepairCollision(definition));
            Assert.That(Id(definition), Is.EqualTo(id));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RenameMoveAndReimport_PreserveStableProjection(bool view)
        {
            var definition = Create(view);
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            AssetDatabase.SaveAssets();
            string id = Id(definition);
            string original = AssetDatabase.GetAssetPath(definition);
            string guid = AssetDatabase.AssetPathToGUID(original);
            Assert.That(AssetDatabase.RenameAsset(original, "Renamed"), Is.Empty);
            AssetDatabase.CreateFolder(_root, "Moved");
            string destination = _root + "/Moved/Renamed.asset";
            Assert.That(AssetDatabase.MoveAsset(_root + "/Renamed.asset", destination), Is.Empty);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObject>(destination);
            Assert.That(Id(reloaded), Is.EqualTo(id));
            Assert.That(AssetDatabase.AssetPathToGUID(destination), Is.EqualTo(guid));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(reloaded), Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Duplicate_IsDistinctAuthority_BlocksUntilExplicitTargetedRepair(bool view)
        {
            var original = Create(view);
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(original);
            AssetDatabase.SaveAssets();
            string id = Id(original);
            string duplicatePath = _root + "/Duplicate.asset";
            Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), duplicatePath), Is.True);
            var duplicate = AssetDatabase.LoadAssetAtPath<ScriptableObject>(duplicatePath);
            Assert.That(ReferenceEquals(original, duplicate), Is.False);
            Assert.That(Id(duplicate), Is.EqualTo(id));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(original), Does.Contain("collision"));
            Assert.Throws<InvalidOperationException>(() => ValidateScope(original, duplicate));
            Assert.DoesNotThrow(() => ValidateScope(original, original));
            CameraDefinitionIdentityEditorUtility.RepairCollision(duplicate);
            Assert.That(Id(original), Is.EqualTo(id));
            Assert.That(Id(duplicate), Is.Not.EqualTo(id));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(duplicate), Is.Null);
            Assert.DoesNotThrow(() => ValidateScope(original, duplicate));
            Undo.PerformUndo();
            Assert.That(Id(duplicate), Is.EqualTo(id));
            Assert.Throws<InvalidOperationException>(() => ValidateScope(original, duplicate));
        }

        [Test]
        public void TypedSerializedReferences_RetainExactAssetsEvenWhenIdsCollide()
        {
            var holder = ScriptableObject.CreateInstance<CameraDefinitionReferenceFixture>();
            try
            {
                var view = (CameraViewDefinition)Create(true);
                CameraDefinitionIdentityEditorUtility.GenerateMissingId(view);
                AssetDatabase.SaveAssets();
                string copyPath = _root + "/Copy.asset";
                Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(view), copyPath), Is.True);
                var copy = AssetDatabase.LoadAssetAtPath<CameraViewDefinition>(copyPath);
                var output = ScriptableObject.CreateInstance<CameraOutputDefinition>();
                AssetDatabase.CreateAsset(output, _root + "/Output.asset");
                CameraDefinitionIdentityEditorUtility.GenerateMissingId(output);
                var serialized = new SerializedObject(holder);
                serialized.FindProperty("view").objectReferenceValue = copy;
                serialized.FindProperty("output").objectReferenceValue = output;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(ReferenceEquals(holder.view, copy), Is.True);
                Assert.That(ReferenceEquals(holder.view, view), Is.False);
                Assert.That(ReferenceEquals(holder.output, output), Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    CameraDefinitionValidation.ValidateViews(new[] { view, holder.view }));
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }

        [Test]
        public void MissingDefinitions_AreRejected()
        {
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionValidation.ValidateViews(new CameraViewDefinition[] { null }));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionValidation.ValidateOutputs(new CameraOutputDefinition[] { null }));
        }

        private ScriptableObject Create(bool view)
        {
            ScriptableObject asset = view
                ? (ScriptableObject)ScriptableObject.CreateInstance<CameraViewDefinition>()
                : ScriptableObject.CreateInstance<CameraOutputDefinition>();
            AssetDatabase.CreateAsset(asset, _root + "/Original.asset");
            return asset;
        }

        private static string Id(ScriptableObject definition) => definition is CameraViewDefinition view
            ? view.ViewId.Value : ((CameraOutputDefinition)definition).OutputId.Value;

        private static void ValidateScope(params ScriptableObject[] definitions)
        {
            if (definitions[0] is CameraViewDefinition)
                CameraDefinitionValidation.ValidateViews(Array.ConvertAll(definitions, d => (CameraViewDefinition)d));
            else
                CameraDefinitionValidation.ValidateOutputs(Array.ConvertAll(definitions, d => (CameraOutputDefinition)d));
        }
    }

    public sealed class CameraDefinitionReferenceFixture : ScriptableObject
    {
        public CameraViewDefinition view;
        public CameraOutputDefinition output;
    }
}
