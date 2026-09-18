using System;
using System.Collections.Generic;
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
            Assert.That(typeof(CameraSharedComposition).GetProperty("Viewport"), Is.Null);
            Assert.That(new SerializedObject(composition).FindProperty("viewport"), Is.Null);
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
            first.Configure(a, Definition<CameraOutputDefinition>());
            second.Configure(b, Definition<CameraOutputDefinition>());
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
        public void MembershipInfrastructure_IsInstanceOwnedAndSurvivesDisableEnable()
        {
            var first = Component<CameraSharedComposition>();
            var second = Component<CameraSharedComposition>();
            string context = first.MembershipContextIdText;
            Assert.That(Guid.TryParseExact(context, "N", out _), Is.True);
            Assert.That(second.MembershipContextIdText, Is.Not.EqualTo(context));
            first.gameObject.SetActive(true);
            first.gameObject.SetActive(false);
            first.gameObject.SetActive(true);
            Assert.That(first.MembershipContextIdText, Is.EqualTo(context));
        }

        [Test]
        public void ExplicitPolicy_RequiresExactOutputAssetAndCreatesIdentityOnlyBinding()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var collision = Definition<CameraOutputDefinition>();
            SetId(collision, output.OutputId.Value);
            var binding = new CameraViewOutputBindingAuthoring();
            binding.Configure(view, output);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { binding });
            var physical = Component<CameraOutputAuthoring>();
            SetReference(physical, "outputDefinition", collision);
            Assert.That(policy.TryValidateOutputs(new[] { physical }, out _), Is.False);
            SetReference(physical, "outputDefinition", output);
            Assert.That(policy.TryValidateOutputs(new[] { physical }, out _), Is.True);
            Assert.That(policy.TryBuildTopology(out var topology, out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.TryGetBinding(view.ViewId, output.OutputId, out var projected), Is.True);
            Assert.That(projected.ViewId, Is.EqualTo(view.ViewId));
            Assert.That(projected.OutputId, Is.EqualTo(output.OutputId));
            Assert.That(typeof(CameraViewOutputBinding).GetProperty("Viewport"), Is.Null);
            Assert.That(typeof(CameraViewOutputBindingAuthoring).GetField(
                "viewport", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
            Assert.That(policy.ViewDefinitions[0], Is.SameAs(view));
        }

        [Test]
        public void SharedComposition_ProjectsIdentityOnlyBindingWithoutPolicy()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(view, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(composition.TryCreateAssociationBinding(out var binding, out string diagnostic), Is.True, diagnostic);
            Assert.That(binding.ViewId, Is.EqualTo(view.ViewId));
            Assert.That(binding.OutputId, Is.EqualTo(output.OutputId));
            Assert.That(typeof(CameraViewOutputBinding).GetProperty("Viewport"), Is.Null);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(output) }, out var topology, out var views, out diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.EqualTo(1));
            Assert.That(views[0], Is.SameAs(view));
            Assert.That(new SerializedObject(composition).FindProperty("viewId"), Is.Null);
            Assert.That(new SerializedObject(composition).FindProperty("outputId"), Is.Null);
        }

        [Test]
        public void SharedComposition_HasNoViewportAuthoringSurface()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(view, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(composition.TryCreateAssociationBinding(out var binding, out string diagnostic), Is.True, diagnostic);
            Assert.That(binding.ViewId, Is.EqualTo(view.ViewId));
            Assert.That(binding.OutputId, Is.EqualTo(output.OutputId));
            Assert.That(typeof(CameraSharedComposition).GetProperty("Viewport"), Is.Null);
            Assert.That(new SerializedObject(composition).FindProperty("viewport"), Is.Null);
        }

        [Test]
        public void MissingCompositionDefinition_BlocksAssociationProjection()
        {
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            SetReference(composition, "outputDefinition", output);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(output) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("View definition"));
        }

        [Test]
        public void MissingCompositionOutputDefinition_BlocksAssociationProjection()
        {
            var view = Definition<CameraViewDefinition>();
            var composition = Component<CameraSharedComposition>();
            SetReference(composition, "viewDefinition", view);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(Definition<CameraOutputDefinition>()) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("Output definition"));
        }

        [Test]
        public void CollidingViewDefinitions_BlockAcrossSimpleAndAdvancedSources()
        {
            var simpleView = Definition<CameraViewDefinition>();
            var policyView = Definition<CameraViewDefinition>();
            SetId(policyView, simpleView.ViewId.Value);
            var outputA = Definition<CameraOutputDefinition>();
            var outputB = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(simpleView, outputA, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var authored = new CameraViewOutputBindingAuthoring();
            authored.Configure(policyView, outputB);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { authored });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, new[] { policy },
                new[] { Physical(outputA), Physical(outputB) },
                out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("collision"));
        }

        [Test]
        public void CollidingOutputDefinitions_BlockAcrossSimpleAndAdvancedSources()
        {
            var viewA = Definition<CameraViewDefinition>();
            var viewB = Definition<CameraViewDefinition>();
            var outputA = Definition<CameraOutputDefinition>();
            var outputB = Definition<CameraOutputDefinition>();
            SetId(outputB, outputA.OutputId.Value);
            var composition = Component<CameraSharedComposition>();
            composition.Configure(viewA, outputA, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var authored = new CameraViewOutputBindingAuthoring();
            authored.Configure(viewB, outputB);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { authored });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, new[] { policy },
                new[] { Physical(outputA) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("collision"));
        }

        [Test]
        public void TwoSimpleAssociations_ProjectDistinctOutputsDeterministically()
        {
            var viewA = Definition<CameraViewDefinition>();
            var viewB = Definition<CameraViewDefinition>();
            var outputA = Definition<CameraOutputDefinition>();
            var outputB = Definition<CameraOutputDefinition>();
            var first = Component<CameraSharedComposition>();
            var second = Component<CameraSharedComposition>();
            first.Configure(viewA, outputA, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            second.Configure(viewB, outputB, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { second, first }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(outputB), Physical(outputA) },
                out var topology, out _, out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.EqualTo(2));
            Assert.That(topology.TryGetBinding(viewA.ViewId, outputA.OutputId, out var a), Is.True);
            Assert.That(topology.TryGetBinding(viewB.ViewId, outputB.OutputId, out var b), Is.True);
            Assert.That(a.ViewId, Is.EqualTo(viewA.ViewId));
            Assert.That(a.OutputId, Is.EqualTo(outputA.OutputId));
            Assert.That(b.ViewId, Is.EqualTo(viewB.ViewId));
            Assert.That(b.OutputId, Is.EqualTo(outputB.OutputId));
        }

        [Test]
        public void TwoAssociations_SameOutput_BlockWithoutPrecedence()
        {
            var viewA = Definition<CameraViewDefinition>();
            var viewB = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var first = Component<CameraSharedComposition>();
            var second = Component<CameraSharedComposition>();
            first.Configure(viewA, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            second.Configure(viewB, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { first, second }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(output) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("conflicting bindings"));
        }

        [Test]
        public void AdvancedPolicyOnly_RemainsSupported()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var authored = new CameraViewOutputBindingAuthoring();
            authored.Configure(view, output);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { authored });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                Array.Empty<CameraSharedComposition>(), new[] { policy },
                new[] { Physical(output) }, out var topology, out var views, out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.EqualTo(1));
            Assert.That(views[0], Is.SameAs(view));
        }

        [Test]
        public void SimpleAndAdvanced_DistinctOutputs_Coexist()
        {
            var viewA = Definition<CameraViewDefinition>();
            var viewB = Definition<CameraViewDefinition>();
            var outputA = Definition<CameraOutputDefinition>();
            var outputB = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(viewA, outputA, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var authored = new CameraViewOutputBindingAuthoring();
            authored.Configure(viewB, outputB);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { authored });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, new[] { policy },
                new[] { Physical(outputA), Physical(outputB) },
                out var topology, out _, out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.EqualTo(2));
            Assert.That(topology.TryGetBinding(viewA.ViewId, outputA.OutputId, out _), Is.True);
            Assert.That(topology.TryGetBinding(viewB.ViewId, outputB.OutputId, out _), Is.True);
        }

        [Test]
        public void SimpleAndAdvanced_SameOutput_BlockWithoutPrecedence()
        {
            var viewA = Definition<CameraViewDefinition>();
            var viewB = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(viewA, output, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var authored = new CameraViewOutputBindingAuthoring();
            authored.Configure(viewB, output);
            var policy = Component<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { authored });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, new[] { policy },
                new[] { Physical(output) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("conflicting bindings"));
        }

        [Test]
        public void ZeroAssociationSources_CreateEmptyTopology()
        {
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                Array.Empty<CameraSharedComposition>(),
                Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(Definition<CameraOutputDefinition>()) },
                out CameraViewOutputTopology topology,
                out IReadOnlyList<CameraViewDefinition> views,
                out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.Zero);
            Assert.That(views, Is.Empty);
        }

        [Test]
        public void ExplicitlyPresentEmptyPolicy_RemainsInvalid()
        {
            var policy = Component<CameraViewOutputPolicyAuthoring>();

            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                Array.Empty<CameraSharedComposition>(),
                new[] { policy },
                new[] { Physical(Definition<CameraOutputDefinition>()) },
                out _,
                out _,
                out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("requires at least one explicit binding"));
        }

        [Test]
        public void MoreThanOneAdvancedPolicy_Blocks()
        {
            var view = Definition<CameraViewDefinition>();
            var output = Definition<CameraOutputDefinition>();
            var first = new CameraViewOutputBindingAuthoring();
            var second = new CameraViewOutputBindingAuthoring();
            first.Configure(view, output);
            second.Configure(Definition<CameraViewDefinition>(), Definition<CameraOutputDefinition>());
            var policyA = Component<CameraViewOutputPolicyAuthoring>();
            var policyB = Component<CameraViewOutputPolicyAuthoring>();
            policyA.Configure(new[] { first });
            policyB.Configure(new[] { second });
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                Array.Empty<CameraSharedComposition>(), new[] { policyA, policyB },
                new[] { Physical(output) }, out _, out _, out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("at most one Camera View Output Policy"));
        }

        [Test]
        public void UnboundPhysicalOutput_AllowsPartialCoverage()
        {
            var view = Definition<CameraViewDefinition>();
            var outputA = Definition<CameraOutputDefinition>();
            var outputB = Definition<CameraOutputDefinition>();
            var composition = Component<CameraSharedComposition>();
            composition.Configure(view, outputA, CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            Assert.That(CameraViewOutputAssociationProjection.TryCreate(
                new[] { composition }, Array.Empty<CameraViewOutputPolicyAuthoring>(),
                new[] { Physical(outputA), Physical(outputB) },
                out CameraViewOutputTopology topology,
                out IReadOnlyList<CameraViewDefinition> views,
                out string diagnostic), Is.True, diagnostic);
            Assert.That(topology.BindingCount, Is.EqualTo(1));
            Assert.That(topology.TryGetBinding(view.ViewId, outputA.OutputId, out _), Is.True);
            Assert.That(topology.TryGetBinding(outputB.OutputId, out _), Is.False);
            Assert.That(views, Has.Count.EqualTo(1));
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

        private CameraOutputAuthoring Physical(CameraOutputDefinition definition)
        {
            var output = Component<CameraOutputAuthoring>();
            SetReference(output, "outputDefinition", definition);
            return output;
        }
    }
}
