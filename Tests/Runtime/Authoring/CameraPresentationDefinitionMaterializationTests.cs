using System;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class CameraPresentationDefinitionMaterializationTests
    {
        [Test]
        public void DefinitionRejectsMissingStableIdentity()
        {
            using var fixture = new Fixture(generatePresentationId: false);

            Assert.That(
                fixture.Definition.TryValidate(out string issue),
                Is.False);
            Assert.That(
                issue,
                Does.Contain("stable ID"));
        }

        [Test]
        public void DefinitionAcceptsOneMaterializedRigPrefab()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Definition.TryValidate(out string issue),
                Is.True,
                issue);
            Assert.That(fixture.Definition.PresentationId.IsValid, Is.True);
            Assert.That(
                fixture.Definition.OutputDefinition,
                Is.SameAs(fixture.OutputDefinition));
            Assert.That(
                fixture.Definition.RigPrefab,
                Is.SameAs(fixture.RigPrefab));
        }

        [Test]
        public void DefinitionRejectsSceneCompositionInsideRigPrefab()
        {
            using var fixture = new Fixture();
            fixture.RigPrefab.AddComponent<CameraSharedComposition>();

            Assert.That(
                fixture.Definition.TryValidate(out string issue),
                Is.False);
            Assert.That(
                issue,
                Does.Contain(nameof(CameraSharedComposition)));
        }

        [Test]
        public void MaterializationUsesRuntimeContentOwnerAndCreatesPresentationRuntime()
        {
            using var fixture = new Fixture();
            RuntimeContentRuntime runtimeContent =
                fixture.CreateRuntimeContent(out RuntimeScopeContext context);
            var materializer =
                new CameraPresentationMaterializationRuntime(runtimeContent);

            CameraPresentationMaterializationResult result =
                materializer.Materialize(
                    context,
                    fixture.Definition,
                    null,
                    nameof(MaterializationUsesRuntimeContentOwnerAndCreatesPresentationRuntime),
                    "test");

            Assert.That(result.Succeeded, Is.True, result.Issue);
            Assert.That(result.Handle, Is.Not.Null);
            Assert.That(
                result.Handle.RuntimeContentIdentity.Owner,
                Is.EqualTo(context.Owner));
            Assert.That(
                result.Handle.Definition,
                Is.SameAs(fixture.Definition));
            Assert.That(result.Handle.RigRoot, Is.Not.Null);
            Assert.That(result.Handle.RigComposer, Is.Not.Null);
            Assert.That(result.Handle.PresentationRuntime, Is.Not.Null);
            Assert.That(
                result.Handle.PresentationRuntime,
                Is.Not.InstanceOf<MonoBehaviour>());
            Assert.That(
                result.Handle.RigComposer.CinemachineCamera.enabled,
                Is.False,
                "A newly materialized Presentation Rig must not physically participate before request arbitration.");

            CameraPresentationMaterializationResult release =
                materializer.Release(
                    result.Handle,
                    nameof(MaterializationUsesRuntimeContentOwnerAndCreatesPresentationRuntime),
                    "cleanup");

            Assert.That(release.Succeeded, Is.True, release.Issue);
        }

        [Test]
        public void SamePresentationCannotMaterializeTwiceForSameOwner()
        {
            using var fixture = new Fixture();
            RuntimeContentRuntime runtimeContent =
                fixture.CreateRuntimeContent(out RuntimeScopeContext context);
            var materializer =
                new CameraPresentationMaterializationRuntime(runtimeContent);

            CameraPresentationMaterializationResult first =
                materializer.Materialize(
                    context,
                    fixture.Definition,
                    null,
                    nameof(SamePresentationCannotMaterializeTwiceForSameOwner),
                    "first");
            Assert.That(first.Succeeded, Is.True, first.Issue);

            CameraPresentationMaterializationResult second =
                materializer.Materialize(
                    context,
                    fixture.Definition,
                    null,
                    nameof(SamePresentationCannotMaterializeTwiceForSameOwner),
                    "duplicate");

            Assert.That(second.Succeeded, Is.False);
            Assert.That(
                second.Status,
                Is.EqualTo(
                    CameraPresentationMaterializationStatus
                        .RejectedDuplicateOccurrence));

            Assert.That(
                materializer.Release(
                    first.Handle,
                    nameof(SamePresentationCannotMaterializeTwiceForSameOwner),
                    "cleanup").Succeeded,
                Is.True);
        }

        [Test]
        public void ReleaseIsIdempotent()
        {
            using var fixture = new Fixture();
            RuntimeContentRuntime runtimeContent =
                fixture.CreateRuntimeContent(out RuntimeScopeContext context);
            var materializer =
                new CameraPresentationMaterializationRuntime(runtimeContent);

            CameraPresentationMaterializationResult created =
                materializer.Materialize(
                    context,
                    fixture.Definition,
                    null,
                    nameof(ReleaseIsIdempotent),
                    "create");
            Assert.That(created.Succeeded, Is.True, created.Issue);

            CameraPresentationMaterializationResult first =
                materializer.Release(
                    created.Handle,
                    nameof(ReleaseIsIdempotent),
                    "first");
            CameraPresentationMaterializationResult second =
                materializer.Release(
                    created.Handle,
                    nameof(ReleaseIsIdempotent),
                    "second");

            Assert.That(first.Succeeded, Is.True, first.Issue);
            Assert.That(second.Succeeded, Is.True, second.Issue);
            Assert.That(
                second.Status,
                Is.EqualTo(
                    CameraPresentationMaterializationStatus
                        .SucceededAlreadyReleased));
        }

        private sealed class Fixture : IDisposable
        {
            internal Fixture(bool generatePresentationId = true)
            {
                OutputDefinition =
                    ScriptableObject.CreateInstance<CameraOutputDefinition>();
                SetField(
                    OutputDefinition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));

                Behavior =
                    ScriptableObject.CreateInstance<
                        FixedCameraRigBehaviorDefinition>();

                RigPrefab = new GameObject("Camera Presentation Rig Prefab");
                RigComposer =
                    RigPrefab.AddComponent<CameraRigComposer>();
                CinemachineCamera =
                    RigPrefab.AddComponent<CinemachineCamera>();

                SetField(
                    RigComposer,
                    "behaviorDefinition",
                    Behavior);
                SetField(
                    RigComposer,
                    "cinemachineCamera",
                    CinemachineCamera);

                Definition =
                    ScriptableObject.CreateInstance<
                        CameraPresentationDefinition>();
                if (generatePresentationId)
                {
                    SetField(
                        Definition,
                        "stableId",
                        Guid.NewGuid().ToString("N"));
                }

                SetField(
                    Definition,
                    "outputDefinition",
                    OutputDefinition);
                SetField(
                    Definition,
                    "rigPrefab",
                    RigPrefab);
                SetField(
                    Definition,
                    "subjectPolicy",
                    CameraSharedCompositionSubjectPolicyKind
                        .AllAvailableSubjects);
                SetField(
                    Definition,
                    "requestPrecedence",
                    100);
            }

            internal CameraOutputDefinition OutputDefinition { get; }

            internal FixedCameraRigBehaviorDefinition Behavior { get; }

            internal GameObject RigPrefab { get; }

            internal CameraRigComposer RigComposer { get; }

            internal CinemachineCamera CinemachineCamera { get; }

            internal CameraPresentationDefinition Definition { get; }

            internal RuntimeContentRuntime CreateRuntimeContent(
                out RuntimeScopeContext context)
            {
                var runtime = new RuntimeContentRuntime();
                RuntimeContentOwner owner =
                    RuntimeContentOwner.Session(
                        Guid.NewGuid().ToString("N"),
                        "Camera Presentation Test Session");

                RuntimeRootRegistryOperationResult root =
                    runtime.CreateScopeRoot(
                        owner,
                        nameof(CameraPresentationDefinitionMaterializationTests),
                        "test");
                Assert.That(
                    root.Applied ||
                    root.Status ==
                        RuntimeRootRegistryOperationStatus.RootAlreadyExists,
                    Is.True,
                    root.Message);

                Assert.That(
                    runtime.TryCreateScopeContext(
                        owner,
                        nameof(CameraPresentationDefinitionMaterializationTests),
                        "test",
                        out context),
                    Is.True);

                return runtime;
            }

            public void Dispose()
            {
                Destroy(Definition);
                Destroy(Behavior);
                Destroy(OutputDefinition);
                Destroy(RigPrefab);
            }

            private static void Destroy(UnityEngine.Object value)
            {
                if (value == null)
                {
                    return;
                }

                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static void SetField(
            object target,
            string name,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
