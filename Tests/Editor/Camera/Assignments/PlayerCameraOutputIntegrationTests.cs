using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class PlayerCameraOutputIntegrationTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private GameObject _playerObject;
        private GameObject _cameraObject;
        private GameObject _replacementCameraObject;

        [TearDown]
        public void TearDown()
        {
            for (int index = _disposables.Count - 1; index >= 0; index--)
                _disposables[index].Dispose();
            _disposables.Clear();
            if (_playerObject != null) UnityEngine.Object.DestroyImmediate(_playerObject);
            if (_cameraObject != null) UnityEngine.Object.DestroyImmediate(_cameraObject);
            if (_replacementCameraObject != null) UnityEngine.Object.DestroyImmediate(_replacementCameraObject);
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void SecondJoin_ReassertsPhysicalOutputCoverageAfterPlayerInputManagerSplitRecomposition()
        {
            Fixture fixture = CreateFixture();
            var hostModule = new GameObject("Player Host Evidence").AddComponent<PlayerActorPreparationRuntimeHostModule>();
            _created.Add(hostModule.gameObject);
            SetField(hostModule, "_participationContext", fixture.Participation);
            SetField(hostModule, "_hostEvidenceProjection", new PlayerHostEvidenceProjection(fixture.Participation));
            var managerObject = new GameObject("Player Input Manager");
            _created.Add(managerObject);
            PlayerInputManager manager = managerObject.AddComponent<PlayerInputManager>();
            manager.splitScreen = true;

            Assert.That(PlayerCameraOutputIntegrationRuntime.TryCreate(
                fixture.Participation, hostModule, manager, fixture.Outputs, fixture.Bindings,
                out PlayerCameraOutputIntegrationRuntime integration, out string issue), Is.True, issue);
            _disposables.Add(integration);

            Action<PlayerSlotId> assertPhysicalCoverage = _ =>
                Assert.That(
                    fixture.OutputsBySlot[0].UnityCamera.enabled ||
                    fixture.OutputsBySlot[1].UnityCamera.enabled,
                    Is.True,
                    "The second Join transaction left the Session without a physical Camera Output.");
            Action<PlayerSessionChange> assertSessionCoverage = _ =>
                Assert.That(
                    fixture.OutputsBySlot[0].UnityCamera.enabled ||
                    fixture.OutputsBySlot[1].UnityCamera.enabled,
                    Is.True,
                    "A Session allocation transition left the Session without a physical Camera Output.");
            hostModule.SessionPhysicalHostChanged += assertPhysicalCoverage;
            fixture.Participation.Changed += assertSessionCoverage;

            LocalPlayerHostAuthoring first = JoinAndRegister(fixture, hostModule, PlayerSlotId.Player1);
            Assert.That(first.PlayerInput.camera, Is.SameAs(fixture.OutputsBySlot[0].UnityCamera));
            AssertPhysicalCoverage(fixture, true, false);

            LocalPlayerHostAuthoring second = JoinAndRegister(fixture, hostModule, PlayerSlotId.Player2);

            Assert.That(second.PlayerInput.camera, Is.SameAs(fixture.OutputsBySlot[1].UnityCamera));
            Assert.That(manager.splitScreen, Is.True);
            AssertPhysicalCoverage(fixture, true, true);
            Assert.That(integration.LastReconciliationSucceeded, Is.True, integration.Diagnostic);
            hostModule.SessionPhysicalHostChanged -= assertPhysicalCoverage;
            fixture.Participation.Changed -= assertSessionCoverage;
        }

        [Test]
        public void AssignmentCameraIsAppliedToPlayerInputAndShutdownClearsOwnedAssociation()
        {
            _playerObject = new GameObject("Player Input");
            _cameraObject = new GameObject("Assigned Output Camera");
            PlayerInput playerInput = _playerObject.AddComponent<PlayerInput>();
            UnityEngine.Camera assignedCamera = _cameraObject.AddComponent<UnityEngine.Camera>();

            PlayerCameraOutputIntegrationRuntime.SetCamera(playerInput, assignedCamera);

            Assert.That(playerInput.camera, Is.SameAs(assignedCamera));
            PlayerCameraOutputIntegrationRuntime.ClearCamera(playerInput, assignedCamera);
            Assert.That(playerInput.camera, Is.Null);
        }

        [Test]
        public void ShutdownDoesNotClearCameraAssociationOwnedByANewerAssignment()
        {
            _playerObject = new GameObject("Player Input");
            _cameraObject = new GameObject("Previous Output Camera");
            _replacementCameraObject = new GameObject("Replacement Output Camera");
            PlayerInput playerInput = _playerObject.AddComponent<PlayerInput>();
            UnityEngine.Camera previousCamera = _cameraObject.AddComponent<UnityEngine.Camera>();
            UnityEngine.Camera replacementCamera = _replacementCameraObject.AddComponent<UnityEngine.Camera>();
            PlayerCameraOutputIntegrationRuntime.SetCamera(playerInput, replacementCamera);

            PlayerCameraOutputIntegrationRuntime.ClearCamera(playerInput, previousCamera);

            Assert.That(playerInput.camera, Is.SameAs(replacementCamera));
        }

        private Fixture CreateFixture()
        {
            var outputs = new List<CameraOutputAuthoring>();
            var outputIds = new CameraOutputId[2];
            for (int index = 0; index < 2; index++)
            {
                var definition = ScriptableObject.CreateInstance<CameraOutputDefinition>();
                _created.Add(definition);
                SetField(definition, "stableId", (index + 1).ToString("x32"));
                var outputRoot = new GameObject("Output " + index);
                outputRoot.SetActive(false);
                _created.Add(outputRoot);
                UnityEngine.Camera unityCamera = outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = outputRoot.AddComponent<CinemachineBrain>();
                OutputChannels channel = (OutputChannels)(1 << index);
                brain.ChannelMask = channel;

                GameObject fallbackRoot = CreateRig("Fallback " + index, out CameraRigComposer fallback);
                fallbackRoot.transform.SetParent(outputRoot.transform, false);
                fallback.CinemachineCamera.OutputChannel = channel;
                CameraOutputAuthoring output = outputRoot.AddComponent<CameraOutputAuthoring>();
                SetField(output, "outputDefinition", definition);
                SetField(output, "unityCamera", unityCamera);
                SetField(output, "cinemachineBrain", brain);
                SetField(output, "fallbackCameraRig", fallback);
                SetField(output, "initializeOnAwake", false);
                outputRoot.SetActive(true);
                outputs.Add(output);
                outputIds[index] = output.OutputId;
            }

            Assert.That(CameraOutputSessionTopology.TryCreate(
                outputs, out CameraOutputSessionTopology outputTopology, out string outputIssue), Is.True, outputIssue);
            _disposables.Add(outputTopology);

            var profiles = new List<PlayerSlotProfile>();
            for (int index = 0; index < 2; index++)
            {
                var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                SetField(profile, "playerSlotId", "player." + (index + 1));
                _created.Add(profile);
                profiles.Add(profile);
            }
            Assert.That(PlayerParticipationRuntimeContext.TryCreate(
                profiles, true, nameof(PlayerCameraOutputIntegrationTests), "second-join-regression",
                out PlayerParticipationRuntimeContext participation).Succeeded, Is.True);

            var bindings = new List<PlayerCameraOutputBinding>
            {
                new PlayerCameraOutputBinding(PlayerSlotId.Player1, outputIds[0]),
                new PlayerCameraOutputBinding(PlayerSlotId.Player2, outputIds[1])
            };
            Assert.That(PlayerCameraOutputTopology.TryCreate(
                bindings, outputTopology, out PlayerCameraOutputTopology topology, out string bindingIssue), Is.True, bindingIssue);
            return new Fixture(participation, outputTopology, topology, outputs.ToArray());
        }

        private LocalPlayerHostAuthoring JoinAndRegister(
            Fixture fixture,
            PlayerActorPreparationRuntimeHostModule hostModule,
            PlayerSlotId expectedSlot)
        {
            PlayerParticipationOperationResult reservation = fixture.Participation.TryReserveNextAvailableSlot(
                PlayerHostProvisioningMode.ManagerProvisioned,
                nameof(PlayerCameraOutputIntegrationTests),
                "join");
            Assert.That(reservation.Succeeded, Is.True, reservation.Message);
            Assert.That(reservation.Slot.PlayerSlotId, Is.EqualTo(expectedSlot));

            var hostRoot = new GameObject("Host " + expectedSlot.StableText);
            _created.Add(hostRoot);
            PlayerInput playerInput = hostRoot.AddComponent<PlayerInput>();
            var mount = new GameObject("Actor Mount").transform;
            mount.SetParent(hostRoot.transform, false);
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            SetField(host, "playerInput", playerInput);
            SetField(host, "actorMount", mount);
            Assert.That(host.TryStageAdmission(
                reservation.Slot, nameof(PlayerCameraOutputIntegrationTests), "join", out string stageIssue), Is.True, stageIssue);
            PlayerParticipationOperationResult joined = fixture.Participation.TryMarkJoined(
                reservation.ReservationToken, nameof(PlayerCameraOutputIntegrationTests), "join");
            Assert.That(joined.Succeeded, Is.True, joined.Message);
            Assert.That(fixture.Participation.TryGetSlotSnapshot(
                expectedSlot, out PlayerSlotRuntimeSnapshot slot), Is.True);
            host.CommitStagedAdmission(slot, nameof(PlayerCameraOutputIntegrationTests), "join");
            Assert.That(hostModule.RegisterSessionPhysicalHost(
                expectedSlot, PlayerHostProvisioningMode.ManagerProvisioned, host,
                nameof(PlayerCameraOutputIntegrationTests), "register-host").Succeeded, Is.True);
            return host;
        }

        private static void AssertPhysicalCoverage(Fixture fixture, bool first, bool second)
        {
            Assert.That(fixture.OutputsBySlot[0].UnityCamera.enabled, Is.EqualTo(first));
            Assert.That(fixture.OutputsBySlot[1].UnityCamera.enabled, Is.EqualTo(second));
        }

        private GameObject CreateRig(string rigName, out CameraRigComposer composer)
        {
            var root = new GameObject(rigName);
            _created.Add(root);
            var behavior = ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            CinemachineCamera camera = root.AddComponent<CinemachineCamera>();
            composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);
            SetField(composer, "cinemachineCamera", camera);
            return root;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, PrivateInstance).SetValue(target, value);
        }

        private sealed class Fixture
        {
            internal Fixture(PlayerParticipationRuntimeContext participation,
                CameraOutputSessionTopology outputs,
                PlayerCameraOutputTopology bindings,
                CameraOutputAuthoring[] outputsBySlot)
            {
                Participation = participation;
                Outputs = outputs;
                Bindings = bindings;
                OutputsBySlot = outputsBySlot;
            }

            internal PlayerParticipationRuntimeContext Participation { get; }
            internal CameraOutputSessionTopology Outputs { get; }
            internal PlayerCameraOutputTopology Bindings { get; }
            internal CameraOutputAuthoring[] OutputsBySlot { get; }
        }
    }
}
