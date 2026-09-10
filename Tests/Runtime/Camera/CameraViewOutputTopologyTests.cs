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
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void SharedView_BindsOutputAFullScreen()
        {
            CameraViewOutputTopology topology = Policy(
                Binding("view.main", "output.a", 0f, 0f, 1f, 1f));

            Assert.That(topology.TryGetBinding(
                new CameraViewId("view.main"),
                new CameraOutputId("output.a"),
                out CameraViewOutputBinding binding), Is.True);
            Assert.That(binding.Viewport, Is.EqualTo(new CameraViewport(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void SplitViews_ApplyLeftAndRightToExactOutputCameras()
        {
            CameraOutputAuthoring outputA = Output("output.a", new Rect(0f, 0f, 1f, 1f));
            CameraOutputAuthoring outputB = Output("output.b", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputB, outputA }, out CameraOutputSessionTopology outputs, out string diagnostic), Is.True, diagnostic);
            CameraViewOutputTopology policy = Policy(
                Binding("view.p2", "output.b", .5f, 0f, .5f, 1f),
                Binding("view.p1", "output.a", 0f, 0f, .5f, 1f));

            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, policy, out CameraViewOutputRuntime runtime, out diagnostic), Is.True, diagnostic);
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(new Rect(0f, 0f, .5f, 1f)));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(.5f, 0f, .5f, 1f)));
            Assert.That(policy.CaptureSnapshot().Bindings[0].OutputId.Value, Is.EqualTo("output.a"));
            Assert.That(policy.CaptureSnapshot().Bindings[1].OutputId.Value, Is.EqualTo("output.b"));
            runtime.Dispose();
        }

        [Test]
        public void OneViewMayFeedMultipleOutputsWithoutMergingIdentities()
        {
            CameraViewOutputTopology topology = Policy(
                Binding("view.shared", "output.a", 0f, 0f, 1f, 1f),
                Binding("view.shared", "output.spectator", 0f, 0f, 1f, 1f));

            Assert.That(topology.GetBindings(new CameraViewId("view.shared")).Count, Is.EqualTo(2));
            Assert.That(new CameraViewId("shared"), Is.Not.EqualTo(new CameraOutputId("shared")));
        }

        [Test]
        public void DuplicateOutputBindingAndInvalidViewport_AreRejected()
        {
            Assert.That(CameraViewOutputTopology.TryCreate(
                new[]
                {
                    Binding("view.a", "output.a", 0f, 0f, 1f, 1f),
                    Binding("view.b", "output.a", .5f, 0f, .5f, 1f)
                }, out _, out string conflict), Is.False);
            Assert.That(conflict, Does.Contain("conflicting bindings"));

            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "output.a", 0f, 0f, 0f, 1f) },
                out _, out string invalid), Is.False);
            Assert.That(invalid, Does.Contain("invalid normalized viewport"));

            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "output.a", float.NaN, 0f, 1f, 1f) },
                out _, out _), Is.False);
            Assert.That(CameraViewOutputTopology.TryCreate(
                new[] { Binding("view.a", "output.a", .8f, 0f, .3f, 1f) },
                out _, out _), Is.False);
        }

        [Test]
        public void SharedComposition_RequiresItsExactViewToOutputBinding()
        {
            CameraOutputAuthoring output = Output("output.a", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { output }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology policy = Policy(
                Binding("view.shared", "output.a", 0f, 0f, 1f, 1f));
            var root = new GameObject("shared-composition");
            _created.Add(root);
            CameraSharedComposition composition = root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                new CameraView(new CameraViewId("view.shared"), "Shared"),
                new ViewAssignmentContextId("shared-context"),
                new CameraSubjectAssignmentOwnerId("shared-owner"),
                new CameraOutputId("output.a"),
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);

            using var injection = new CameraOutputInjectionRuntime(outputs, policy);
            Assert.That(injection.AttachExact(composition, out string diagnostic), Is.True, diagnostic);
            Assert.That(composition.Output, Is.SameAs(output));

            CameraViewOutputTopology conflicting = Policy(
                Binding("view.other", "output.a", 0f, 0f, 1f, 1f));
            using var conflictingInjection = new CameraOutputInjectionRuntime(outputs, conflicting);
            Assert.That(conflictingInjection.AttachExact(composition, out diagnostic), Is.False);
            Assert.That(composition.Output, Is.Null);
            Assert.That(diagnostic, Does.Contain("explicit View 'view.shared'"));
        }

        [Test]
        public void ChangingOutputAViewport_DoesNotMutateOutputBOrItsArbitration()
        {
            CameraOutputAuthoring outputA = Output("output.a", new Rect(0f, 0f, 1f, 1f));
            CameraOutputAuthoring outputB = Output("output.b", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology first = Policy(
                Binding("view.a", "output.a", 0f, 0f, .5f, 1f),
                Binding("view.b", "output.b", .5f, 0f, .5f, 1f));
            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, first, out CameraViewOutputRuntime runtime, out _), Is.True);
            CameraOutputContextSnapshot beforeB = outputB.Context.CaptureSnapshot();

            CameraViewOutputTopology changed = Policy(
                Binding("view.a", "output.a", 0f, 0f, .4f, 1f),
                Binding("view.b", "output.b", .5f, 0f, .5f, 1f));
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
            CameraOutputAuthoring outputA = Output("output.a", authoredA);
            CameraOutputAuthoring outputB = Output("output.b", new Rect(0f, 0f, 1f, 1f));
            Assert.That(CameraOutputSessionTopology.TryCreate(
                new[] { outputA, outputB }, out CameraOutputSessionTopology outputs, out _), Is.True);
            CameraViewOutputTopology split = Policy(
                Binding("view.a", "output.a", 0f, 0f, .5f, 1f),
                Binding("view.b", "output.b", .5f, 0f, .5f, 1f));
            Assert.That(CameraViewOutputRuntime.TryCreate(outputs, split, out CameraViewOutputRuntime runtime, out _), Is.True);

            Assert.That(runtime.TryApply(
                Policy(Binding("view.b", "output.b", 0f, 0f, 1f, 1f)), out _), Is.True);
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

        private CameraOutputAuthoring Output(string outputId, Rect viewport)
        {
            CameraRigComposer composer = Composer($"{outputId}-default");
            var root = new GameObject(outputId);
            _created.Add(root);
            UnityEngine.Camera camera = root.AddComponent<UnityEngine.Camera>();
            camera.rect = viewport;
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            SetField(output, "outputId", outputId);
            SetField(output, "unityCamera", camera);
            SetField(output, "cinemachineBrain", brain);
            SetField(output, "defaultCameraRig", composer);
            return output;
        }

        private CameraRigComposer Composer(string name)
        {
            var root = new GameObject(name);
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            var cameraObject = new GameObject($"{name}-camera");
            cameraObject.transform.SetParent(root.transform, false);
            SetField(composer, "cinemachineCamera", cameraObject.AddComponent<CinemachineCamera>());
            return composer;
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
