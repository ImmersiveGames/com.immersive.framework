using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class CameraSessionConfigurationTests
    {
        [Test]
        public void ConfigurationAcceptsExplicitOutputPrefab()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Configuration.TryValidate(
                    out string issue),
                Is.True,
                issue);
            Assert.That(
                fixture.Configuration.OutputPrefabs.Count,
                Is.EqualTo(1));
            Assert.That(
                fixture.Configuration.OutputPrefabs[0],
                Is.SameAs(fixture.OutputPrefab));
        }

        [Test]
        public void DuplicateOutputPrefabIsRejected()
        {
            using var fixture = new Fixture();
            fixture.SetOutputPrefabs(
                fixture.OutputPrefab,
                fixture.OutputPrefab);

            Assert.That(
                fixture.Configuration.TryValidate(
                    out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("duplicate"));
            Assert.That(issue, Does.Contain("Output prefab"));
        }

        [Test]
        public void PlayerBindingMustReferenceConfiguredOutputDefinition()
        {
            using var fixture = new Fixture();
            CameraOutputDefinition other =
                fixture.CreateOutputDefinition();
            PlayerSlotProfile profile =
                fixture.CreatePlayerSlotProfile("player.1");
            var binding =
                new PlayerCameraOutputBindingAuthoring();
            binding.Configure(profile, other);
            fixture.SetPlayerOutputBindings(binding);

            Assert.That(
                fixture.Configuration.TryValidate(
                    out string issue),
                Is.False);
            Assert.That(
                issue,
                Does.Contain("not one of this Session's exact configured Output definitions"));
        }

        [Test]
        public void MaterializationCreatesExactSessionTopologyAndTeardown()
        {
            using var fixture = new Fixture();
            GameObject parent =
                fixture.CreateRoot("Camera Session Parent");

            Assert.That(
                CameraSessionOutputMaterializationRuntime.TryCreate(
                    fixture.Configuration,
                    parent.transform,
                    out CameraSessionOutputMaterializationRuntime runtime,
                    out string diagnostic),
                Is.True,
                diagnostic);

            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.OutputCount, Is.EqualTo(1));
            Assert.That(runtime.Topology.OutputCount, Is.EqualTo(1));
            Assert.That(
                runtime.Topology.TryGetOutput(
                    fixture.OutputDefinition.OutputId,
                    out CameraOutputAuthoring materialized,
                    out string lookupIssue),
                Is.True,
                lookupIssue);
            Assert.That(materialized, Is.Not.Null);
            Assert.That(
                materialized.gameObject,
                Is.Not.SameAs(fixture.OutputPrefab));
            Assert.That(
                materialized.transform.root,
                Is.SameAs(parent.transform));
            Assert.That(materialized.IsInitialized, Is.True);

            runtime.Dispose();

            Assert.That(runtime.IsDisposed, Is.True);
            Assert.DoesNotThrow(() => runtime.Dispose());
        }

        [Test]
        public void DirectPlayerBindingProjectionUsesCameraSessionBindings()
        {
            using var fixture = new Fixture();
            PlayerSlotProfile profile =
                fixture.CreatePlayerSlotProfile("player.1");
            var binding =
                new PlayerCameraOutputBindingAuthoring();
            binding.Configure(
                profile,
                fixture.OutputDefinition);
            fixture.SetPlayerOutputBindings(binding);

            GameObject parent =
                fixture.CreateRoot("Camera Session Parent");
            Assert.That(
                CameraSessionOutputMaterializationRuntime.TryCreate(
                    fixture.Configuration,
                    parent.transform,
                    out CameraSessionOutputMaterializationRuntime runtime,
                    out string materializationIssue),
                Is.True,
                materializationIssue);

            try
            {
                Assert.That(
                    PlayerCameraOutputPolicyProjection.TryCreate(
                        fixture.Configuration.PlayerOutputBindings,
                        runtime.Topology,
                        null,
                        false,
                        out PlayerCameraOutputTopology topology,
                        out string projectionIssue),
                    Is.True,
                    projectionIssue);
                Assert.That(topology, Is.Not.Null);
                Assert.That(topology.BindingCount, Is.EqualTo(1));
            }
            finally
            {
                runtime.Dispose();
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created =
                new List<UnityEngine.Object>();

            internal Fixture()
            {
                OutputDefinition =
                    CreateOutputDefinition();

                Behavior =
                    ScriptableObject.CreateInstance<
                        FixedCameraRigBehaviorDefinition>();
                _created.Add(Behavior);

                OutputPrefab =
                    CreateRoot("Camera Session Output Prefab");
                OutputPrefab.SetActive(false);

                UnityEngine.Camera unityCamera =
                    OutputPrefab.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    OutputPrefab.AddComponent<CinemachineBrain>();

                GameObject defaultRigRoot =
                    CreateRoot("Default Camera Rig");
                defaultRigRoot.transform.SetParent(
                    OutputPrefab.transform,
                    false);
                CameraRigComposer defaultRig =
                    defaultRigRoot.AddComponent<CameraRigComposer>();
                CinemachineCamera cinemachineCamera =
                    defaultRigRoot.AddComponent<CinemachineCamera>();

                SetField(
                    defaultRig,
                    "behaviorDefinition",
                    Behavior);
                SetField(
                    defaultRig,
                    "cinemachineCamera",
                    cinemachineCamera);

                OutputAuthoring =
                    OutputPrefab.AddComponent<CameraOutputAuthoring>();
                SetField(
                    OutputAuthoring,
                    "outputDefinition",
                    OutputDefinition);
                SetField(
                    OutputAuthoring,
                    "unityCamera",
                    unityCamera);
                SetField(
                    OutputAuthoring,
                    "cinemachineBrain",
                    brain);
                SetField(
                    OutputAuthoring,
                    "defaultCameraRig",
                    defaultRig);
                SetField(
                    OutputAuthoring,
                    "initializeOnAwake",
                    false);

                Configuration =
                    new CameraSessionConfiguration();
                SetOutputPrefabs(OutputPrefab);
            }

            internal CameraSessionConfiguration Configuration { get; }

            internal CameraOutputDefinition OutputDefinition { get; }

            internal CameraOutputAuthoring OutputAuthoring { get; }

            internal FixedCameraRigBehaviorDefinition Behavior { get; }

            internal GameObject OutputPrefab { get; }

            internal CameraOutputDefinition CreateOutputDefinition()
            {
                CameraOutputDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraOutputDefinition>();
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                return definition;
            }

            internal PlayerSlotProfile CreatePlayerSlotProfile(
                string slotId)
            {
                PlayerSlotProfile profile =
                    ScriptableObject.CreateInstance<
                        PlayerSlotProfile>();
                _created.Add(profile);
                SetField(
                    profile,
                    "playerSlotId",
                    slotId);
                return profile;
            }

            internal void SetOutputPrefabs(
                params GameObject[] prefabs)
            {
                SetField(
                    Configuration,
                    "outputPrefabs",
                    new List<GameObject>(
                        prefabs ??
                        Array.Empty<GameObject>()));
            }

            internal void SetPlayerOutputBindings(
                params PlayerCameraOutputBindingAuthoring[] bindings)
            {
                SetField(
                    Configuration,
                    "playerOutputBindings",
                    new List<PlayerCameraOutputBindingAuthoring>(
                        bindings ??
                        Array.Empty<PlayerCameraOutputBindingAuthoring>()));
            }

            internal GameObject CreateRoot(string name)
            {
                var root = new GameObject(name);
                _created.Add(root);
                return root;
            }

            public void Dispose()
            {
                for (int index = _created.Count - 1;
                     index >= 0;
                     index--)
                {
                    UnityEngine.Object value =
                        _created[index];
                    if (value != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            value);
                    }
                }
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
