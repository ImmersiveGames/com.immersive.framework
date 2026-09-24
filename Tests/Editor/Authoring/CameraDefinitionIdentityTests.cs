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
            string folder = "_Camera029E_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            _root = "Assets/" + folder;
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_root);
        }

        [Test]
        public void OutputIdentityRequiresExplicitGenerationAndHasNoImplicitReplacement()
        {
            CameraOutputDefinition definition = Create();
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionValidation.ValidateOutputs(new[] { definition }));
            Assert.Throws<InvalidOperationException>(() => _ = definition.OutputId);
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            string id = definition.OutputId.Value;
            Assert.That(Guid.TryParseExact(id, "N", out _), Is.True);
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(definition), Is.Null);
            Assert.DoesNotThrow(() =>
                CameraDefinitionValidation.ValidateOutputs(new[] { definition, definition }));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionIdentityEditorUtility.RepairCollision(definition));
            Assert.That(definition.OutputId.Value, Is.EqualTo(id));
        }

        [Test]
        public void RenameMoveAndReimportPreserveOutputStableProjection()
        {
            CameraOutputDefinition definition = Create();
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            AssetDatabase.SaveAssets();
            string id = definition.OutputId.Value;
            string original = AssetDatabase.GetAssetPath(definition);
            string guid = AssetDatabase.AssetPathToGUID(original);
            Assert.That(AssetDatabase.RenameAsset(original, "Renamed"), Is.Empty);
            AssetDatabase.CreateFolder(_root, "Moved");
            string destination = _root + "/Moved/Renamed.asset";
            Assert.That(AssetDatabase.MoveAsset(_root + "/Renamed.asset", destination), Is.Empty);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            CameraOutputDefinition reloaded =
                AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(destination);
            Assert.That(reloaded.OutputId.Value, Is.EqualTo(id));
            Assert.That(AssetDatabase.AssetPathToGUID(destination), Is.EqualTo(guid));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(reloaded), Is.Null);
        }

        [Test]
        public void DuplicateOutputIsDistinctAuthorityUntilExplicitRepair()
        {
            CameraOutputDefinition original = Create();
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(original);
            AssetDatabase.SaveAssets();
            string id = original.OutputId.Value;
            string duplicatePath = _root + "/Duplicate.asset";
            Assert.That(AssetDatabase.CopyAsset(
                AssetDatabase.GetAssetPath(original), duplicatePath), Is.True);
            CameraOutputDefinition duplicate =
                AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(duplicatePath);
            Assert.That(ReferenceEquals(original, duplicate), Is.False);
            Assert.That(duplicate.OutputId.Value, Is.EqualTo(id));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(original), Does.Contain("collision"));
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionValidation.ValidateOutputs(new[] { original, duplicate }));
            CameraDefinitionIdentityEditorUtility.RepairCollision(duplicate);
            Assert.That(original.OutputId.Value, Is.EqualTo(id));
            Assert.That(duplicate.OutputId.Value, Is.Not.EqualTo(id));
            Assert.That(CameraDefinitionIdentityEditorUtility.Validate(duplicate), Is.Null);
            Assert.DoesNotThrow(() =>
                CameraDefinitionValidation.ValidateOutputs(new[] { original, duplicate }));
            Undo.PerformUndo();
            Assert.That(duplicate.OutputId.Value, Is.EqualTo(id));
        }

        [Test]
        public void MissingOutputDefinitionIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() =>
                CameraDefinitionValidation.ValidateOutputs(
                    new CameraOutputDefinition[] { null }));
        }

        private CameraOutputDefinition Create()
        {
            var asset = ScriptableObject.CreateInstance<CameraOutputDefinition>();
            AssetDatabase.CreateAsset(asset, _root + "/Original.asset");
            return asset;
        }
    }
}
