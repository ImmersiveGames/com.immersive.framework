using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
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
        public void PresentationRuntimeProjectsOnlyItsExactOutputDefinition()
        {
            CameraOutputDefinition output = Definition<CameraOutputDefinition>();
            var presentation = new CameraPresentationRuntime("exact-output");
            presentation.Configure(
                output,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects,
                null,
                0,
                CameraPresentationTransitionMode.Blend);

            Assert.That(presentation.RequestedOutputId, Is.EqualTo(output.OutputId));
            Assert.That(presentation.TryValidateDefinitions(out _), Is.True);
            Assert.That(typeof(CameraPresentationRuntime).IsSubclassOf(typeof(MonoBehaviour)), Is.False);
            presentation.Dispose();
        }

        [Test]
        public void PresentationDefinitionSerializesReusableIntentOnly()
        {
            string[] serialized = typeof(CameraPresentationDefinition)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsPublic || field.IsDefined(typeof(SerializeField), false))
                .Select(field => field.Name)
                .ToArray();

            Assert.That(serialized, Is.EquivalentTo(new[]
            {
                "stableId",
                "description",
                "outputDefinition",
                "rigPrefab",
                "transitionMode",
                "subjectPolicy",
                "requestPrecedence"
            }));
            Assert.That(typeof(ScriptableObject).IsAssignableFrom(typeof(CameraPresentationRuntime)), Is.False);
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
        public void MissingOutputDefinitionBlocksPresentation()
        {
            var presentation = new CameraPresentationRuntime("missing-output");
            Assert.That(presentation.TryValidateDefinitions(out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("Output definition"));
            presentation.Configure(
                null,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects,
                null,
                0,
                CameraPresentationTransitionMode.Blend);
            Assert.That(presentation.TryValidateDefinitions(out diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("Output definition"));
            presentation.Dispose();
        }

        [Test]
        public void InvalidOutputIdentityBlocksPresentation()
        {
            CameraOutputDefinition output = Definition<CameraOutputDefinition>();
            SetId(output, "invalid");
            var presentation = new CameraPresentationRuntime("invalid-output");
            presentation.Configure(
                output,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects,
                null,
                0,
                CameraPresentationTransitionMode.Blend);
            Assert.That(presentation.TryValidateDefinitions(out string diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("Output definition"));
            presentation.Dispose();
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
        public void SameIdDifferentOutputAssetCannotSatisfyExactPresentationAttachment()
        {
            CameraOutputDefinition requested = Definition<CameraOutputDefinition>();
            CameraOutputDefinition other = Definition<CameraOutputDefinition>();
            SetId(other, requested.OutputId.Value);
            var presentation = new CameraPresentationRuntime("exact-attachment");
            presentation.Configure(
                requested,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects,
                null,
                0,
                CameraPresentationTransitionMode.Blend);
            CameraOutputAuthoring physical = Component<CameraOutputAuthoring>();
            SetReference(physical, "outputDefinition", other);
            Assert.Throws<InvalidOperationException>(() => presentation.AttachOutputSession(physical));
            Assert.That(presentation.Output, Is.Null);
            SetReference(physical, "outputDefinition", requested);
            Assert.DoesNotThrow(() => presentation.AttachOutputSession(physical));
            Assert.That(presentation.Output, Is.SameAs(physical));
            presentation.Dispose();
        }

        [Test]
        public void LegacyRawIdsDoNotRestoreMissingDefinitionAuthority()
        {
            CameraOutputAuthoring physical = Component<CameraOutputAuthoring>();
            JsonUtility.FromJsonOverwrite("{\"outputId\":\"legacy.output\"}", physical);
            Assert.That(physical.TryValidateDefinition(out _), Is.False);
            Assert.That(physical.OutputId.IsValid, Is.False);
            Assert.That(new SerializedObject(physical).FindProperty("outputId"), Is.Null);
        }

        [Test]
        public void PresentationMembershipContextIsInstanceOwnedAndSurvivesEnable()
        {
            var first = new CameraPresentationRuntime("membership-a");
            var second = new CameraPresentationRuntime("membership-b");
            string context = first.MembershipContextIdText;
            Assert.That(Guid.TryParseExact(context, "N", out _), Is.True);
            Assert.That(second.MembershipContextIdText, Is.Not.EqualTo(context));
            first.SetEnabled(true);
            first.SetEnabled(false);
            first.SetEnabled(true);
            Assert.That(first.MembershipContextIdText, Is.EqualTo(context));
            first.Dispose();
            second.Dispose();
        }

        [Test]
        public void SessionOutputsReceiveExclusiveCinemachineChannelsAndPresentationsInheritThem()
        {
            CameraOutputAuthoring first =
                OutputForChannelTest(
                    "Output A",
                    out CinemachineBrain firstBrain,
                    out CameraRigComposer firstDefaultRig);
            CameraOutputAuthoring second =
                OutputForChannelTest(
                    "Output B",
                    out CinemachineBrain secondBrain,
                    out CameraRigComposer secondDefaultRig);

            Assert.That(
                CameraCinemachineOutputChannelIsolation.TryConfigureOutput(
                    first,
                    0,
                    out string firstIssue),
                Is.True,
                firstIssue);
            Assert.That(
                CameraCinemachineOutputChannelIsolation.TryConfigureOutput(
                    second,
                    1,
                    out string secondIssue),
                Is.True,
                secondIssue);

            Assert.That(
                firstBrain.ChannelMask,
                Is.EqualTo(OutputChannels.Default));
            Assert.That(
                secondBrain.ChannelMask,
                Is.EqualTo(OutputChannels.Channel01));
            Assert.That(
                firstDefaultRig.CinemachineCamera.OutputChannel,
                Is.EqualTo(firstBrain.ChannelMask));
            Assert.That(
                secondDefaultRig.CinemachineCamera.OutputChannel,
                Is.EqualTo(secondBrain.ChannelMask));
            Assert.That(
                firstBrain.ChannelMask,
                Is.Not.EqualTo(secondBrain.ChannelMask));

            CameraRigComposer firstPresentation =
                PresentationRigForChannelTest("Presentation A");
            CameraRigComposer secondPresentation =
                PresentationRigForChannelTest("Presentation B");

            Assert.That(
                CameraCinemachineOutputChannelIsolation
                    .TryConfigurePresentation(
                        first,
                        firstPresentation,
                        out string firstPresentationIssue),
                Is.True,
                firstPresentationIssue);
            Assert.That(
                CameraCinemachineOutputChannelIsolation
                    .TryConfigurePresentation(
                        second,
                        secondPresentation,
                        out string secondPresentationIssue),
                Is.True,
                secondPresentationIssue);

            Assert.That(
                firstPresentation.CinemachineCamera.OutputChannel,
                Is.EqualTo(OutputChannels.Default));
            Assert.That(
                secondPresentation.CinemachineCamera.OutputChannel,
                Is.EqualTo(OutputChannels.Channel01));
        }

        [Test]
        public void CinemachineOutputChannelAllocationRejectsSeventeenthSessionOutput()
        {
            Assert.That(
                CameraCinemachineOutputChannelIsolation.TryResolveChannel(
                    15,
                    out OutputChannels lastChannel,
                    out string lastIssue),
                Is.True,
                lastIssue);
            Assert.That(
                lastChannel,
                Is.EqualTo(OutputChannels.Channel15));

            Assert.That(
                CameraCinemachineOutputChannelIsolation.TryResolveChannel(
                    16,
                    out _,
                    out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("index"));
        }

        private CameraOutputAuthoring OutputForChannelTest(
            string name,
            out CinemachineBrain brain,
            out CameraRigComposer defaultRig)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            _created.Add(root);

            var output = root.AddComponent<CameraOutputAuthoring>();
            var unityCamera = root.AddComponent<UnityEngine.Camera>();
            brain = root.AddComponent<CinemachineBrain>();

            var rigRoot = new GameObject($"{name} Default Rig");
            rigRoot.transform.SetParent(root.transform, false);
            defaultRig = rigRoot.AddComponent<CameraRigComposer>();
            var defaultCamera = rigRoot.AddComponent<CinemachineCamera>();
            defaultRig.EditorSetGeneratedReference(defaultCamera);

            SetReference(output, "unityCamera", unityCamera);
            SetReference(output, "cinemachineBrain", brain);
            SetReference(output, "defaultCameraRig", defaultRig);
            return output;
        }

        private CameraRigComposer PresentationRigForChannelTest(
            string name)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            _created.Add(root);

            var composer = root.AddComponent<CameraRigComposer>();
            var camera = root.AddComponent<CinemachineCamera>();
            composer.EditorSetGeneratedReference(camera);
            return composer;
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
