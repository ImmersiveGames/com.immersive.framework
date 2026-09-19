using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void SharedCompositionProjectsOnlyItsExactOutputDefinition()
        {
            CameraOutputDefinition output = Definition<CameraOutputDefinition>();
            CameraSharedComposition composition = Component<CameraSharedComposition>();
            composition.Configure(output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(composition.OutputDefinition, Is.SameAs(output));
            Assert.That(composition.RequestedOutputId, Is.EqualTo(output.OutputId));
            Assert.That(composition.TryValidateDefinitions(out _), Is.True);
            Assert.That(new SerializedObject(composition).FindProperty("viewDefinition"), Is.Null);
            Assert.That(typeof(CameraSharedComposition).GetProperties()
                .Any(property => property.Name.Contains("View", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void PhysicalOutputProjectsItsExactDefinition()
        {
            CameraOutputDefinition definition = Definition<CameraOutputDefinition>();
            CameraOutputAuthoring output = Component<CameraOutputAuthoring>();
            SetReference(output, "outputDefinition", definition);
            Assert.That(output.OutputDefinition, Is.SameAs(definition));
            Assert.That(output.OutputId, Is.EqualTo(definition.OutputId));
            Assert.That(output.TryValidateDefinition(out _), Is.True);
        }

        [Test]
        public void MissingOutputDefinitionBlocksSharedComposition()
        {
            CameraSharedComposition composition = Component<CameraSharedComposition>();
            Assert.That(composition.TryValidateDefinitions(out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("Output definition"));
            Assert.Throws<InvalidOperationException>(() => composition.Configure(
                null, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects));
        }

        [Test]
        public void InvalidOutputIdentityBlocksComposition()
        {
            CameraOutputDefinition output = Definition<CameraOutputDefinition>();
            SetId(output, "invalid");
            CameraSharedComposition composition = Component<CameraSharedComposition>();
            SetReference(composition, "outputDefinition", output);
            Assert.That(composition.TryValidateDefinitions(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => composition.Configure(
                output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects));
        }

        [Test]
        public void CollidingOutputDefinitionsBlockBeforePhysicalInitialization()
        {
            CameraOutputDefinition firstDefinition = Definition<CameraOutputDefinition>();
            CameraOutputDefinition secondDefinition = Definition<CameraOutputDefinition>();
            SetId(secondDefinition, firstDefinition.OutputId.Value);
            CameraOutputAuthoring first = Component<CameraOutputAuthoring>();
            CameraOutputAuthoring second = Component<CameraOutputAuthoring>();
            SetReference(first, "outputDefinition", firstDefinition);
            SetReference(second, "outputDefinition", secondDefinition);
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { first, second }, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("collision"));
            Assert.That(first.IsInitialized || second.IsInitialized, Is.False);
        }

        [Test]
        public void SameIdDifferentOutputAssetCannotSatisfyExactInjection()
        {
            CameraOutputDefinition requested = Definition<CameraOutputDefinition>();
            CameraOutputDefinition other = Definition<CameraOutputDefinition>();
            SetId(other, requested.OutputId.Value);
            CameraSharedComposition composition = Component<CameraSharedComposition>();
            composition.Configure(requested, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            CameraOutputAuthoring physical = Component<CameraOutputAuthoring>();
            SetReference(physical, "outputDefinition", other);
            Assert.Throws<InvalidOperationException>(() => composition.AttachOutputSession(physical));
            Assert.That(composition.Output, Is.Null);
            SetReference(physical, "outputDefinition", requested);
            Assert.DoesNotThrow(() => composition.AttachOutputSession(physical));
            Assert.That(composition.Output, Is.SameAs(physical));
        }

        [Test]
        public void LegacyRawIdsDoNotRestoreMissingDefinitionAuthority()
        {
            CameraSharedComposition composition = Component<CameraSharedComposition>();
            CameraOutputAuthoring physical = Component<CameraOutputAuthoring>();
            JsonUtility.FromJsonOverwrite("{\"outputId\":\"legacy.output\",\"viewId\":\"legacy.view\"}", composition);
            JsonUtility.FromJsonOverwrite("{\"outputId\":\"legacy.output\"}", physical);
            Assert.That(composition.TryValidateDefinitions(out _), Is.False);
            Assert.That(physical.TryValidateDefinition(out _), Is.False);
            Assert.That(composition.RequestedOutputId.IsValid || physical.OutputId.IsValid, Is.False);
            Assert.That(new SerializedObject(composition).FindProperty("viewId"), Is.Null);
            Assert.That(new SerializedObject(composition).FindProperty("outputId"), Is.Null);
            Assert.That(new SerializedObject(physical).FindProperty("outputId"), Is.Null);
        }

        [Test]
        public void MembershipInfrastructureIsInstanceOwnedAndSurvivesDisableEnable()
        {
            CameraSharedComposition first = Component<CameraSharedComposition>();
            CameraSharedComposition second = Component<CameraSharedComposition>();
            string context = first.MembershipContextIdText;
            Assert.That(Guid.TryParseExact(context, "N", out _), Is.True);
            Assert.That(second.MembershipContextIdText, Is.Not.EqualTo(context));
            first.gameObject.SetActive(true);
            first.gameObject.SetActive(false);
            first.gameObject.SetActive(true);
            Assert.That(first.MembershipContextIdText, Is.EqualTo(context));
        }

        [Test]
        public void OutputInjectionDependsOnlyOnOutputTopology()
        {
            ConstructorInfo[] constructors = typeof(CameraOutputInjectionRuntime)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(constructors, Has.Length.EqualTo(1));
            Assert.That(constructors[0].GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(CameraOutputSessionTopology) }));
        }

        [Test]
        public void OverrideRequiresExactOutputDefinitionWithoutRawIdFallback()
        {
            CameraOutputDefinition output = Definition<CameraOutputDefinition>();
            CameraOutputDefinition collision = Definition<CameraOutputDefinition>();
            SetId(collision, output.OutputId.Value);
            SessionCameraOverride request = Component<SessionCameraOverride>();
            CameraOutputAuthoring physical = Component<CameraOutputAuthoring>();
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

        private T Component<T>() where T : Component
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
