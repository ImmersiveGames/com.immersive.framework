using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraDefinitionBackedAuthoringTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
                if (_created[i] != null) Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        [Test]
        public void SharedComposition_ProjectsExactViewAndOutputDefinitions()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(view, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(composition.ViewDefinition, Is.SameAs(view));
            Assert.That(composition.OutputDefinition, Is.SameAs(output));
            Assert.That(composition.ViewId, Is.EqualTo(view.ViewId));
            Assert.That(composition.RequestedOutputId, Is.EqualTo(output.OutputId));
            Assert.That(composition.TryValidateDefinitions(out _), Is.True);
        }

        [Test]
        public void PhysicalOutput_ProjectsItsExactDefinition()
        {
            var definition = Definition<CameraOutputDefinition>();
            var output = Component<CameraOutputAuthoring>();
            SetReference(output, "outputDefinition", definition);
            Assert.That(output.OutputDefinition, Is.SameAs(definition));
            Assert.That(output.OutputId, Is.EqualTo(definition.OutputId));
            Assert.That(output.TryValidateDefinition(out _), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingDefinition_BlocksSharedComposition(bool missingView)
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            SetReference(composition, "viewDefinition", missingView ? null : view);
            SetReference(composition, "outputDefinition", missingView ? output : null);
            Assert.That(composition.TryValidateDefinitions(out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain(missingView ? "View definition" : "Output definition"));
            Assert.Throws<InvalidOperationException>(() => composition.Configure(
                missingView ? null : view, missingView ? output : null,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects));
        }

        [Test]
        public void MissingPhysicalOutputDefinition_BlocksTopologyAdmission()
        {
            var output = Component<CameraOutputAuthoring>();
            Assert.That(output.TryValidateDefinition(out _), Is.False);
            Assert.That(CameraOutputSessionTopology.TryCreate(new[] { output }, out _, out _), Is.False);
            Assert.That(output.IsInitialized, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InvalidDefinitionIdentity_BlocksComposition(bool invalidView)
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            SetId(invalidView ? (ScriptableObject)view : output, "invalid");
            var composition = Component<CameraSharedComposition>();
            SetReference(composition, "viewDefinition", view);
            SetReference(composition, "outputDefinition", output);
            Assert.That(composition.TryValidateDefinitions(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => composition.Configure(
                view, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects));
        }

        [Test]
        public void CollidingOutputDefinitions_BlockBeforePhysicalInitialization()
        {
            var a = Definition<CameraOutputDefinition>();
            var b = Definition<CameraOutputDefinition>();
            SetId(b, a.OutputId.Value);
            var first = Component<CameraOutputAuthoring>();
            var second = Component<CameraOutputAuthoring>();
            SetReference(first, "outputDefinition", a);
            SetReference(second, "outputDefinition", b);
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { first, second }, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("collision"));
            Assert.That(first.IsInitialized || second.IsInitialized, Is.False);
        }

        [Test]
        public void CollidingViewDefinitions_BlockExplicitPolicy()
        {
            var a = Definition<CameraViewDefinition>();
            var b = Definition<CameraViewDefinition>();
            SetId(b, a.ViewId.Value);
            var first = new CameraViewOutputBindingAuthoring();
            var second = new CameraViewOutputBindingAuthoring();
            first.Configure(a, Definition<CameraOutputDefinition>(), new CameraViewport(0, 0, 1, 1));
            second.Configure(b, Definition<CameraOutputDefinition>(), new CameraViewport(0, 0, 1, 1));
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { first, second });
            Assert.That(policy.TryBuildTopology(out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("collision"));
        }

        [Test]
        public void SameIdDifferentOutputAsset_CannotSatisfyExactInjection()
        {
            var view = Definition<CameraViewDefinition>();
            var requested = Definition<CameraOutputDefinition>();
            var other = Definition<CameraOutputDefinition>();
            SetId(other, requested.OutputId.Value);
            var composition = Component<CameraSharedComposition>();
            composition.Configure(view, requested, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var physical = Component<CameraOutputAuthoring>();
            SetReference(physical, "outputDefinition", other);
            Assert.Throws<InvalidOperationException>(() => composition.AttachOutputSession(physical));
            Assert.That(composition.Output, Is.Null);
            SetReference(physical, "outputDefinition", requested);
            Assert.DoesNotThrow(() => composition.AttachOutputSession(physical));
            Assert.That(composition.Output, Is.SameAs(physical));
        }

        [Test]
        public void LegacyRawFields_DoNotRestoreMissingDefinitionAuthority()
        {
            var composition = Component<CameraSharedComposition>();
            var physical = Component<CameraOutputAuthoring>();
            const string legacy = "{\"viewId\":\"legacy.view\",\"outputId\":\"legacy.output\"}";
            JsonUtility.FromJsonOverwrite(legacy, composition);
            JsonUtility.FromJsonOverwrite(legacy, physical);
            Assert.That(composition.TryValidateDefinitions(out _), Is.False);
            Assert.That(physical.TryValidateDefinition(out _), Is.False);
            Assert.That(composition.ViewId.IsValid || composition.RequestedOutputId.IsValid || physical.OutputId.IsValid, Is.False);
            Assert.That(new SerializedObject(composition).FindProperty("viewId"), Is.Null);
            Assert.That(new SerializedObject(composition).FindProperty("outputId"), Is.Null);
            Assert.That(new SerializedObject(physical).FindProperty("outputId"), Is.Null);
        }

        [Test]
        public void AssignmentInfrastructure_IsInstanceOwnedAndSurvivesDisableEnable()
        {
            var first = Component<CameraSharedComposition>();
            var second = Component<CameraSharedComposition>();
            string context = first.AssignmentContextIdText;
            string owner = first.AssignmentOwnerIdText;
            Assert.That(Guid.TryParseExact(context, "N", out _), Is.True);
            Assert.That(Guid.TryParseExact(owner, "N", out _), Is.True);
            Assert.That(second.AssignmentContextIdText, Is.Not.EqualTo(context));
            Assert.That(second.AssignmentOwnerIdText, Is.Not.EqualTo(owner));
            first.gameObject.SetActive(true);
            first.gameObject.SetActive(false);
            first.gameObject.SetActive(true);
            Assert.That(first.AssignmentContextIdText, Is.EqualTo(context));
            Assert.That(first.AssignmentOwnerIdText, Is.EqualTo(owner));
        }

        [Test]
        public void ExplicitPolicy_RequiresExactOutputAssetAndPreservesAuthoredViewport()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var collision = Definition<CameraOutputDefinition>();
            SetId(collision, output.OutputId.Value);
            var binding = new CameraViewOutputBindingAuthoring();
            var viewport = new CameraViewport(0, 0, .5f, 1);
            binding.Configure(view, output, viewport);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { binding });
            var physical = Component<CameraOutputAuthoring>();
            SetReference(physical, "outputDefinition", collision);
            Assert.That(policy.TryValidateOutputs(new[] { physical }, out _), Is.False);
            SetReference(physical, "outputDefinition", output);
            Assert.That(policy.TryValidateOutputs(new[] { physical }, out _), Is.True);
            Assert.That(policy.TryBuildTopology(out var topology, out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.TryGetBinding(view.ViewId, output.OutputId, out var projected), Is.True);
            Assert.That(projected.Viewport, Is.EqualTo(viewport));
            Assert.That(policy.ViewDefinitions[0], Is.SameAs(view));
        }

        [Test]
        public void Override_RequiresExactOutputDefinitionWithoutRawIdFallback()
        {
            var output = Definition<CameraOutputDefinition>();
            var collision = Definition<CameraOutputDefinition>();
            SetId(collision, output.OutputId.Value);
            var request = Component<SessionCameraOverride>();
            var physical = Component<CameraOutputAuthoring>();
            JsonUtility.FromJsonOverwrite("{\"outputId\":\"" + output.OutputId.Value + "\"}", request);
            Assert.That(request.RequestedOutputId.IsValid, Is.False);
            SetReference(request, "outputDefinition", output);
            SetReference(physical, "outputDefinition", collision);
            var consumer = (ICameraOutputSessionConsumer)request;
            Assert.Throws<InvalidOperationException>(() => consumer.AttachOutputSession(physical));
            SetReference(physical, "outputDefinition", output);
            Assert.DoesNotThrow(() => consumer.AttachOutputSession(physical));
            Assert.That(request.OutputSession, Is.SameAs(physical));
        }

        private T Definition<T>() where T : ScriptableObject
        {
            var definition = ScriptableObject.CreateInstance<T>();
            _created.Add(definition);
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);
            return definition;
        }

        private T Component<T>() where T : UnityEngine.Component
        {
            var root = new GameObject(typeof(T).Name);
            root.SetActive(false);
            _created.Add(root);
            return root.AddComponent<T>();
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetId(ScriptableObject definition, string value)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("stableId").stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
