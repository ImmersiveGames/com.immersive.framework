using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraViewOutputTopologyTests
    {
        private readonly CameraDefinitionTestAssets _definitions = new CameraDefinitionTestAssets();
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
            _definitions.Dispose();
        }

        [Test]
        public void SharedView_BindsOutputAFullScreen()
        {
            CameraViewOutputTopology topology = Policy(
                Binding("view.main", "10000000000000000000000000000002", 0f, 0f, 1f, 1f));

            Assert.That(topology.TryGetBinding(
                new CameraViewId("view.main"),
                new CameraOutputId("10000000000000000000000000000002"),
                out CameraViewOutputBinding binding), Is.True);
            Assert.That(binding.Viewport, Is.EqualTo(new CameraViewport(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void SplitViews_ApplyLeftAndRightToExactOutputCameras()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002", new Rect(0f, 0f, 1f, 1f));
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputB, outputA }, out CameraOutputSessionTopology outputs, out string diagnostic), Is.True, diagnostic);
            CameraViewOutputTopology policy = Policy(
                Binding("view.p2", "10000000000000000000000000000003", .5f, 0f, .5f, 1f),
                Binding("view.p1", "10000000000000000000000000000002", 0f, 0f, .5f, 1f));

            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, policy, out CameraViewOutputRuntime runtime, out diagnostic), Is.True, diagnostic);
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(new Rect(0f, 0f, .5f, 1f)));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(.5f, 0f, .5f, 1f)));
            Assert.That(policy.CaptureSnapshot().Bindings[0].OutputId.Value, Is.EqualTo("10000000000000000000000000000002"));
            Assert.That(policy.CaptureSnapshot().Bindings[1].OutputId.Value, Is.EqualTo("10000000000000000000000000000003"));
            runtime.Dispose();
        }

        [Test]
        public void OneViewMayFeedMultipleOutputsWithoutMergingIdentities()
        {
            CameraViewOutputTopology topology = Policy(
                Binding("20000000000000000000000000000002", "10000000000000000000000000000002", 0f, 0f, 1f, 1f),
                Binding("20000000000000000000000000000002", "10000000000000000000000000000004", 0f, 0f, 1f, 1f));

            Assert.That(topology.GetBindings(new CameraViewId("20000000000000000000000000000002")).Count, Is.EqualTo(2));
            Assert.That(new CameraViewId("shared"), Is.Not.EqualTo(new CameraOutputId("shared")));
        }

        [Test]
        public void DuplicateOutputBindingAndInvalidViewport_AreRejected()
        {
            Assert.That(CameraViewOutputTopology.TryCreate(
                new[]
                {
                    Binding("view.a", "10000000000000000000000000000002", 0f, 0f, 1f, 1f),
                    Binding("view.b", "10000000000000000000000000000002", .5f, 0f, .5f, 1f)
                }, out _, out string conflict), Is.False);
            Assert.That(conflict, Does.Contain("conflicting bindings"));

            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "10000000000000000000000000000002", 0f, 0f, 0f, 1f) },
                out _, out string invalid), Is.False);
            Assert.That(invalid, Does.Contain("invalid normalized viewport"));

            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "10000000000000000000000000000002", float.NaN, 0f, 1f, 1f) },
                out _, out _), Is.False);
            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "10000000000000000000000000000002", .8f, 0f, .3f, 1f) },
                out _, out _), Is.False);
        }

        [Test]
        public void SharedComposition_RequiresItsExactViewToOutputBinding()
        {
            CameraOutputAuthoring output = Output("10000000000000000000000000000002", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology policy = Policy(
                Binding("20000000000000000000000000000002", "10000000000000000000000000000002", 0f, 0f, 1f, 1f));
            var root = new GameObject("shared-composition");
            _created.Add(root);
            CameraSharedComposition composition = root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                _definitions.View("20000000000000000000000000000002"),
                output.OutputDefinition,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);

            using var injection = new CameraOutputInjectionRuntime(outputs, policy, new[] { composition.ViewDefinition });
            Assert.That(injection.AttachExact(composition, out string diagnostic), Is.True, diagnostic);
            Assert.That(composition.Output, Is.SameAs(output));

            CameraViewOutputTopology conflicting = Policy(
                Binding("view.other", "10000000000000000000000000000002", 0f, 0f, 1f, 1f));
            using var conflictingInjection = new CameraOutputInjectionRuntime(outputs, conflicting, new[] { composition.ViewDefinition });
            Assert.That(conflictingInjection.AttachExact(composition, out diagnostic), Is.False);
            Assert.That(composition.Output, Is.Null);
            Assert.That(diagnostic, Does.Contain("explicit View '20000000000000000000000000000002'"));
        }

        [Test]
        public void ChangingOutputAViewport_DoesNotMutateOutputBOrItsArbitration()
        {
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002", new Rect(0f, 0f, 1f, 1f));
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology first = Policy(
                Binding("view.a", "10000000000000000000000000000002", 0f, 0f, .5f, 1f),
                Binding("view.b", "10000000000000000000000000000003", .5f, 0f, .5f, 1f));
            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, first, out CameraViewOutputRuntime runtime, out _), Is.True);
            CameraOutputContextSnapshot beforeB = outputB.Context.CaptureSnapshot();

            CameraViewOutputTopology changed = Policy(
                Binding("view.a", "10000000000000000000000000000002", 0f, 0f, .4f, 1f),
                Binding("view.b", "10000000000000000000000000000003", .5f, 0f, .5f, 1f));
            Assert.That(runtime.TryApply(changed, out string diagnostic), Is.True, diagnostic);

            Assert.That(outputA.UnityCamera.rect.width, Is.EqualTo(.4f));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(.5f, 0f, .5f, 1f)));
            CameraOutputContextSnapshot afterB = outputB.Context.CaptureSnapshot();
            Assert.That(afterB.AdmittedRequestCount, Is.EqualTo(beforeB.AdmittedRequestCount));
            Assert.That(afterB.HasWinner, Is.EqualTo(beforeB.HasWinner));
            runtime.Dispose();
        }

        [Test]
        public void RemovingBindingAndTeardown_RestoreAuthoredViewportsWithoutDestroyingOutputs()
        {
            Rect authoredA = new Rect(.1f, .1f, .8f, .8f);
            CameraOutputAuthoring outputA = Output("10000000000000000000000000000002", authoredA);
            CameraOutputAuthoring outputB = Output("10000000000000000000000000000003", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology split = Policy(
                Binding("view.a", "10000000000000000000000000000002", 0f, 0f, .5f, 1f),
                Binding("view.b", "10000000000000000000000000000003", .5f, 0f, .5f, 1f));
            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, split, out CameraViewOutputRuntime runtime, out _), Is.True);

            Assert.That(runtime.TryApply(
                Policy(Binding("view.b", "10000000000000000000000000000003", 0f, 0f, 1f, 1f)), out _), Is.True);
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(authoredA));
            Assert.That(outputA.IsInitialized, Is.True);
            Assert.That(outputB.IsInitialized, Is.True);

            runtime.Dispose();
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(outputA != null && outputB != null, Is.True);
        }

        private static CameraViewOutputBinding Binding(
            string viewId, string outputId, float x, float y, float width, float height) =>
            new CameraViewOutputBinding(
                new CameraViewId(viewId),
                new CameraOutputId(outputId),
                new CameraViewport(x, y, width, height));

        private static CameraViewOutputTopology Policy(params CameraViewOutputBinding[] bindings)
        {
            Assert.That(CameraViewOutputTopology.TryCreate(bindings, out CameraViewOutputTopology topology, out string diagnostic), Is.True, diagnostic);
            return topology;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InjectionCache_CannotMergeDifferentDefinitionAssetsWithSameId(bool replaceView)
        {
            var output = Output(CameraDefinitionTestAssets.MainId, new Rect(0, 0, 1, 1));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out var outputs, out string diagnostic), Is.True, diagnostic);
            var view = _definitions.View();
            var policy = Policy(Binding(view.ViewId.Value, output.OutputId.Value, 0, 0, 1, 1));
            var root = new GameObject("definition-consumer");
            _created.Add(root);
            var composition = root.AddComponent<CameraSharedComposition>();
            composition.Configure(view, output.OutputDefinition,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            using var injection = new CameraOutputInjectionRuntime(outputs, policy, new[] { view });
            Assert.That(injection.AttachExact(composition).Succeeded, Is.True);

            composition.Configure(
                replaceView ? _definitions.View(view.ViewId.Value) : view,
                replaceView ? output.OutputDefinition : _definitions.Output(output.OutputId.Value),
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            var rejected = injection.AttachExact(composition);
            Assert.That(rejected.Status, Is.EqualTo(CameraOutputInjectionStatus.RejectedDefinition));
            Assert.That(composition.Output, Is.Null);
            Assert.That(output.IsInitialized, Is.True);
        }

        private CameraOutputAuthoring Output(string outputId, Rect viewport)
        {
            CameraRigComposer composer = Composer($"{outputId}-default");
            var root = new GameObject(outputId);
            _created.Add(root);
            root.SetActive(false);
            UnityEngine.Camera camera = root.AddComponent<UnityEngine.Camera>();
            camera.rect = viewport;
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            SetField(output, "outputDefinition", _definitions.Output(outputId));
            SetField(output, "unityCamera", camera);
            SetField(output, "cinemachineBrain", brain);
            SetField(output, "defaultCameraRig", composer);
            SetField(output, "initializeOnAwake", false);
            root.SetActive(true);
            return output;
        }

        private CameraRigComposer Composer(string name)
        {
            var root = new GameObject(name);
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", _definitions.Behavior<FollowCameraRigBehaviorDefinition>());
            var cameraObject = new GameObject($"{name}-camera");
            cameraObject.transform.SetParent(root.transform, false);
            SetField(composer, "cinemachineCamera", cameraObject.AddComponent<CinemachineCamera>());
            return composer;
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
