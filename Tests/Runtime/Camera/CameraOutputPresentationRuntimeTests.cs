using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraOutputPresentationRuntimeTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private readonly List<CameraOutputPresentationRuntime> _runtimes =
            new List<CameraOutputPresentationRuntime>();
        private readonly List<CameraOutputSessionTopology> _topologies =
            new List<CameraOutputSessionTopology>();
        private CameraDefinitionTestAssets _definitions;

        [SetUp]
        public void SetUp()
        {
            _definitions = new CameraDefinitionTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _runtimes.Count - 1; index >= 0; index--)
                _runtimes[index]?.Dispose();
            for (int index = _topologies.Count - 1; index >= 0; index--)
                _topologies[index]?.Dispose();
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _definitions.Dispose();
        }

        [Test]
        public void ApplySingleOutput_CapturesBaselineAndWritesExactRect()
        {
            Rect baseline = new Rect(.11f, .12f, .73f, .74f);
            CameraOutputAuthoring output = Output(Id(1), baseline);
            CameraOutputPresentationRuntime runtime = Runtime(output);
            var intended = new CameraOutputPresentationRect(.2f, .3f, .4f, .5f);

            CameraOutputPresentationResult result = runtime.Apply(
                new[] { new CameraOutputPresentationBinding(output.OutputId, intended) });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.Applied));
            Assert.That(output.UnityCamera.rect, Is.EqualTo(new Rect(.2f, .3f, .4f, .5f)));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(result.Snapshot.OwnedOutputs[0].BaselineCaptured, Is.True);
            Assert.That(result.Snapshot.OwnedOutputs[0].Rect, Is.EqualTo(intended));
        }

        [Test]
        public void ApplyTwoOutputs_WritesEachExactRect()
        {
            CameraOutputAuthoring outputA = Output(Id(1), FullScreen());
            CameraOutputAuthoring outputB = Output(Id(2), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                Binding(outputA, 0f, 0f, .5f, 1f),
                Binding(outputB, .5f, 0f, .5f, 1f)
            });

            Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(new Rect(0f, 0f, .5f, 1f)));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(.5f, 0f, .5f, 1f)));
        }

        [Test]
        public void PartialPresentation_LeavesUnlistedRegisteredOutputUntouched()
        {
            Rect untouched = new Rect(.2f, .2f, .6f, .6f);
            CameraOutputAuthoring outputA = Output(Id(1), FullScreen());
            CameraOutputAuthoring outputB = Output(Id(2), untouched);
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);

            CameraOutputPresentationResult result = runtime.Apply(
                new[] { Binding(outputA, 0f, 0f, .5f, 1f) });

            Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(untouched));
            Assert.That(result.Snapshot.OwnedOutputCount, Is.EqualTo(1));
        }

        [Test]
        public void PresentationWithoutViewAssociation_IsValidAndDoesNotCreateViewState()
        {
            CameraOutputAuthoring output = Output(Id(1), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(output);

            CameraOutputPresentationResult result = runtime.Apply(
                new[] { Binding(output, .25f, .25f, .5f, .5f) });

            Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            Assert.That(output.UnityCamera.rect, Is.EqualTo(new Rect(.25f, .25f, .5f, .5f)));
            Assert.That(typeof(CameraOutputPresentationBinding).GetProperty("ViewId"), Is.Null);
        }

        [Test]
        public void ReplaceRetainedOutput_DoesNotRecaptureBaseline()
        {
            Rect original = new Rect(.1f, .2f, .7f, .6f);
            CameraOutputAuthoring output = Output(Id(1), original);
            CameraOutputPresentationRuntime runtime = Runtime(output);
            Assert.That(runtime.Apply(new[] { Binding(output, 0f, 0f, .5f, 1f) }).Succeeded, Is.True);

            CameraOutputPresentationResult replaced = runtime.Apply(
                new[] { Binding(output, .5f, 0f, .5f, 1f) });
            CameraOutputPresentationResult cleared = runtime.Clear();

            Assert.That(replaced.Status, Is.EqualTo(CameraOutputPresentationStatus.Replaced));
            Assert.That(cleared.Status, Is.EqualTo(CameraOutputPresentationStatus.Cleared));
            Assert.That(output.UnityCamera.rect, Is.EqualTo(original));
        }

        [Test]
        public void ReplaceRemovingOutput_RestoresRemovedBaselineAndKeepsRetainedOutputOwned()
        {
            Rect baselineA = new Rect(.1f, .1f, .8f, .8f);
            Rect baselineB = new Rect(.2f, .2f, .6f, .6f);
            CameraOutputAuthoring outputA = Output(Id(1), baselineA);
            CameraOutputAuthoring outputB = Output(Id(2), baselineB);
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);
            Assert.That(runtime.Apply(new[]
            {
                Binding(outputA, 0f, 0f, .5f, 1f),
                Binding(outputB, .5f, 0f, .5f, 1f)
            }).Succeeded, Is.True);

            CameraOutputPresentationResult result = runtime.Apply(
                new[] { Binding(outputA, 0f, 0f, 1f, 1f) });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.Replaced));
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(FullScreen()));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(baselineB));
            Assert.That(result.Snapshot.OwnedOutputCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_RestoresAllBaselinesAndIsIdempotent()
        {
            Rect baselineA = new Rect(.1f, .1f, .8f, .8f);
            Rect baselineB = new Rect(.2f, .2f, .6f, .6f);
            CameraOutputAuthoring outputA = Output(Id(1), baselineA);
            CameraOutputAuthoring outputB = Output(Id(2), baselineB);
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);
            Assert.That(runtime.Apply(new[]
            {
                Binding(outputA, 0f, 0f, .5f, 1f),
                Binding(outputB, .5f, 0f, .5f, 1f)
            }).Succeeded, Is.True);

            CameraOutputPresentationResult first = runtime.Clear();
            CameraOutputPresentationResult second = runtime.Clear();

            Assert.That(first.Status, Is.EqualTo(CameraOutputPresentationStatus.Cleared));
            Assert.That(second.Status, Is.EqualTo(CameraOutputPresentationStatus.NoChange));
            Assert.That(second.Snapshot.Revision, Is.EqualTo(first.Snapshot.Revision));
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(baselineA));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(baselineB));
            Assert.That(_topologies[0].OutputCount, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_RestoresAllBaselinesAndIsIdempotent()
        {
            Rect baseline = new Rect(.1f, .1f, .8f, .8f);
            CameraOutputAuthoring output = Output(Id(1), baseline);
            CameraOutputPresentationRuntime runtime = Runtime(output);
            Assert.That(runtime.Apply(new[] { Binding(output, 0f, 0f, .5f, 1f) }).Succeeded, Is.True);

            Assert.DoesNotThrow(runtime.Dispose);
            Assert.DoesNotThrow(runtime.Dispose);

            Assert.That(output.UnityCamera.rect, Is.EqualTo(baseline));
            Assert.That(runtime.Snapshot.IsDisposed, Is.True);
            Assert.That(runtime.Snapshot.OwnedOutputCount, Is.Zero);
            Assert.That(runtime.Apply(Array.Empty<CameraOutputPresentationBinding>()).Status,
                Is.EqualTo(CameraOutputPresentationStatus.RejectedDisposed));
        }

        [Test]
        public void ReacquireAfterRelease_CapturesNewExternalBaseline()
        {
            CameraOutputAuthoring output = Output(Id(1), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(output);
            Assert.That(runtime.Apply(new[] { Binding(output, 0f, 0f, .5f, 1f) }).Succeeded, Is.True);
            Assert.That(runtime.Clear().Succeeded, Is.True);
            Rect newBaseline = new Rect(.2f, .3f, .4f, .5f);
            output.UnityCamera.rect = newBaseline;

            Assert.That(runtime.Apply(new[] { Binding(output, .5f, 0f, .5f, 1f) }).Succeeded, Is.True);
            Assert.That(runtime.Clear().Succeeded, Is.True);

            Assert.That(output.UnityCamera.rect, Is.EqualTo(newBaseline));
        }

        [Test]
        public void DuplicateOutput_IsRejectedWithoutMutation()
        {
            Rect baseline = new Rect(.1f, .1f, .8f, .8f);
            CameraOutputAuthoring output = Output(Id(1), baseline);
            CameraOutputPresentationRuntime runtime = Runtime(output);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                Binding(output, 0f, 0f, .5f, 1f),
                Binding(output, .5f, 0f, .5f, 1f)
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.RejectedDuplicateOutput));
            Assert.That(output.UnityCamera.rect, Is.EqualTo(baseline));
            Assert.That(result.Snapshot.OwnedOutputCount, Is.Zero);
            Assert.That(result.Snapshot.Revision, Is.Zero);
        }

        [TestCase(float.NaN, 0f, .5f, 1f)]
        [TestCase(float.PositiveInfinity, 0f, .5f, 1f)]
        [TestCase(0f, float.NaN, .5f, 1f)]
        [TestCase(0f, float.NegativeInfinity, .5f, 1f)]
        [TestCase(0f, 0f, float.PositiveInfinity, 1f)]
        [TestCase(0f, 0f, .5f, float.NaN)]
        [TestCase(-.1f, 0f, .5f, 1f)]
        [TestCase(0f, -.1f, .5f, 1f)]
        [TestCase(0f, 0f, 0f, 1f)]
        [TestCase(0f, 0f, .5f, 0f)]
        [TestCase(0f, 0f, -.5f, 1f)]
        [TestCase(0f, 0f, .5f, -1f)]
        [TestCase(.6f, 0f, .5f, 1f)]
        [TestCase(0f, .6f, 1f, .5f)]
        public void InvalidRect_IsRejectedWithoutMutation(
            float x, float y, float width, float height)
        {
            Rect baseline = new Rect(.1f, .1f, .8f, .8f);
            CameraOutputAuthoring output = Output(Id(1), baseline);
            CameraOutputPresentationRuntime runtime = Runtime(output);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                new CameraOutputPresentationBinding(
                    output.OutputId,
                    new CameraOutputPresentationRect(x, y, width, height))
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.RejectedInvalidRect));
            Assert.That(output.UnityCamera.rect, Is.EqualTo(baseline));
            Assert.That(result.Snapshot.Revision, Is.Zero);
        }

        [Test]
        public void UnavailableOutput_IsRejectedWithoutMutation()
        {
            Rect baseline = new Rect(.1f, .1f, .8f, .8f);
            CameraOutputAuthoring output = Output(Id(1), baseline);
            CameraOutputPresentationRuntime runtime = Runtime(output);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                new CameraOutputPresentationBinding(
                    new CameraOutputId(Id(99)),
                    new CameraOutputPresentationRect(0f, 0f, 1f, 1f))
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.RejectedOutputUnavailable));
            Assert.That(output.UnityCamera.rect, Is.EqualTo(baseline));
            Assert.That(result.Snapshot.Revision, Is.Zero);
        }

        [Test]
        public void MissingCamera_IsRejectedWithoutChangingExistingOwnership()
        {
            CameraOutputAuthoring outputA = Output(Id(1), FullScreen());
            CameraOutputAuthoring outputB = Output(Id(2), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);
            Assert.That(runtime.Apply(new[] { Binding(outputA, 0f, 0f, .5f, 1f) }).Succeeded, Is.True);
            int revision = runtime.Snapshot.Revision;
            UnityEngine.Object.DestroyImmediate(outputB.UnityCamera);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                Binding(outputA, .5f, 0f, .5f, 1f),
                new CameraOutputPresentationBinding(
                    outputB.OutputId,
                    new CameraOutputPresentationRect(0f, 0f, .5f, 1f))
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.RejectedMissingCamera));
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(new Rect(0f, 0f, .5f, 1f)));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void MultiOutputInvalidBinding_IsRejectedAtomically()
        {
            Rect baselineA = new Rect(.1f, .1f, .8f, .8f);
            Rect baselineB = new Rect(.2f, .2f, .6f, .6f);
            CameraOutputAuthoring outputA = Output(Id(1), baselineA);
            CameraOutputAuthoring outputB = Output(Id(2), baselineB);
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                Binding(outputA, 0f, 0f, .5f, 1f),
                Binding(outputB, .75f, 0f, .5f, 1f)
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.RejectedInvalidRect));
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(baselineA));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(baselineB));
            Assert.That(result.Snapshot.OwnedOutputCount, Is.Zero);
        }

        [Test]
        public void OverlappingOutputs_AreAccepted()
        {
            CameraOutputAuthoring outputA = Output(Id(1), FullScreen());
            CameraOutputAuthoring outputB = Output(Id(2), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(outputA, outputB);

            CameraOutputPresentationResult result = runtime.Apply(new[]
            {
                Binding(outputA, 0f, 0f, 1f, 1f),
                Binding(outputB, .5f, .5f, .4f, .4f)
            });

            Assert.That(result.Status, Is.EqualTo(CameraOutputPresentationStatus.Applied));
            Assert.That(outputA.UnityCamera.rect, Is.EqualTo(FullScreen()));
            Assert.That(outputB.UnityCamera.rect, Is.EqualTo(new Rect(.5f, .5f, .4f, .4f)));
        }

        [Test]
        public void ExactReapply_IsNoChangeWithoutRevisionChurn()
        {
            CameraOutputAuthoring output = Output(Id(1), FullScreen());
            CameraOutputPresentationRuntime runtime = Runtime(output);
            CameraOutputPresentationBinding[] snapshot =
                { Binding(output, 0f, 0f, .5f, 1f) };
            CameraOutputPresentationResult first = runtime.Apply(snapshot);

            CameraOutputPresentationResult second = runtime.Apply(snapshot);

            Assert.That(second.Status, Is.EqualTo(CameraOutputPresentationStatus.NoChange));
            Assert.That(second.Snapshot.Revision, Is.EqualTo(first.Snapshot.Revision));
        }

        [Test]
        public void RuntimeCreation_OwnsNoOutputAndWritesNoRect()
        {
            Rect baseline = new Rect(.1f, .2f, .7f, .6f);
            CameraOutputAuthoring output = Output(Id(1), baseline);

            CameraOutputPresentationRuntime runtime = Runtime(output);

            Assert.That(runtime.Snapshot.OwnedOutputCount, Is.Zero);
            Assert.That(runtime.Snapshot.Revision, Is.Zero);
            Assert.That(output.UnityCamera.rect, Is.EqualTo(baseline));
        }

        private CameraOutputPresentationRuntime Runtime(params CameraOutputAuthoring[] outputs)
        {
            Assert.That(CameraOutputSessionTopology.TryCreate(
                outputs,
                out CameraOutputSessionTopology topology,
                out string topologyDiagnostic), Is.True, topologyDiagnostic);
            _topologies.Add(topology);
            Assert.That(CameraOutputPresentationRuntime.TryCreate(
                topology,
                out CameraOutputPresentationRuntime runtime,
                out string diagnostic), Is.True, diagnostic);
            _runtimes.Add(runtime);
            return runtime;
        }

        private CameraOutputAuthoring Output(string outputId, Rect baseline)
        {
            var root = new GameObject($"output-{outputId}");
            _created.Add(root);
            root.SetActive(false);
            UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
            unityCamera.rect = baseline;
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            CameraRigComposer composer = Composer($"{outputId}-default");
            SetField(output, "outputDefinition", _definitions.Output(outputId));
            SetField(output, "unityCamera", unityCamera);
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
            _created.Add(cameraObject);
            cameraObject.transform.SetParent(root.transform, false);
            CinemachineCamera camera = cameraObject.AddComponent<CinemachineCamera>();
            SetField(composer, "cinemachineCamera", camera);
            return composer;
        }

        private static CameraOutputPresentationBinding Binding(
            CameraOutputAuthoring output,
            float x,
            float y,
            float width,
            float height) =>
            new CameraOutputPresentationBinding(
                output.OutputId,
                new CameraOutputPresentationRect(x, y, width, height));

        private static Rect FullScreen() => new Rect(0f, 0f, 1f, 1f);

        private static string Id(int value) => value.ToString("D32");

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
