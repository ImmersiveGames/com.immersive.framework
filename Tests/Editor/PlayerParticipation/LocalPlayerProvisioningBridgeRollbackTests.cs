using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class LocalPlayerProvisioningBridgeRollbackTests
    {
        private readonly List<GameObject> _objects = new();
        private PlayerSlotProfile _slotProfile;
        private PlayerParticipationRuntimeContext _session;
        private FakeProvisioningBackend _backend;
        private LocalPlayerProvisioningBridge _bridge;
        private GameObject _hostPrefab;
        private Transform _sessionParent;

        [SetUp]
        public void SetUp()
        {
            _slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            Assert.That(PlayerParticipationRuntimeContext.TryCreate(
                new[] { _slotProfile },
                initialJoiningOpen: true,
                source: nameof(LocalPlayerProvisioningBridgeRollbackTests),
                reason: "test setup",
                out _session).Succeeded, Is.True);

            _hostPrefab = CreateValidHostPrefab();
            _sessionParent = CreateObject("Session Host Parent").transform;
            _backend = new FakeProvisioningBackend(_hostPrefab);
            _bridge = new LocalPlayerProvisioningBridge(
                _session,
                _backend,
                _sessionParent,
                _hostPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            _bridge?.Dispose();
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
            }
            _objects.Clear();
            if (_slotProfile != null)
                UnityEngine.Object.DestroyImmediate(_slotProfile);
        }

        [Test]
        public void BackendThrowsAfterReservation_RollsBackSlotAndReturnsTypedFailure()
        {
            _backend.JoinPlayerHandler = _ => throw new InvalidOperationException("synthetic provisioning failure");

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.HasReservationEvidence, Is.True);
            Assert.That(result.ReservationResult.Succeeded, Is.True);
            Assert.That(result.HasRollbackEvidence, Is.True);
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(result.HasLocalPlayerHostEvidence, Is.False);
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);

            // Simulate another Session owner consuming this exact token before provisioning fails.
            _backend.JoinPlayerHandler = _ =>
            {
                PlayerSlotRuntimeSnapshot reservedSlot = _session.CreateSnapshot().Slots[0];
                Assert.That(reservedSlot.IsReserved, Is.True);
                PlayerParticipationOperationResult competingRelease =
                    _session.TryReleaseReservation(
                        reservedSlot.ReservationToken,
                        "test-competing-owner",
                        "invalidate-pending-join-token");
                Assert.That(competingRelease.Succeeded, Is.True);
                throw new InvalidOperationException("synthetic failure after competing release");
            };

            LocalPlayerJoinResult failedRecovery = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot afterFailedRecovery = _session.CreateSnapshot();

            Assert.That(failedRecovery.Status, Is.EqualTo(LocalPlayerJoinStatus.FailedRollback));
            Assert.That(failedRecovery.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(failedRecovery.HasReservationEvidence, Is.True);
            Assert.That(failedRecovery.ReservationResult.Succeeded, Is.True);
            Assert.That(failedRecovery.HasRollbackEvidence, Is.True);
            Assert.That(failedRecovery.RollbackResult.Succeeded, Is.False);
            Assert.That(failedRecovery.RollbackResult.Status,
                Is.EqualTo(PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation));
            Assert.That(_bridge.HasOperationInFlight, Is.False);
            Assert.That(afterFailedRecovery.AvailableCount, Is.EqualTo(1));
            Assert.That(afterFailedRecovery.ReservedCount, Is.Zero);
            Assert.That(afterFailedRecovery.JoinedCount, Is.Zero);
        }

        [Test]
        public void BackendCreatesCallbackPlayerThenThrows_RollsBackAndRequestsCompensation()
        {
            PlayerInput callbackInput = null;
            LocalPlayerHostAuthoring callbackHost = null;
            _backend.JoinPlayerHandler = _ =>
            {
                GameObject provisioned = UnityEngine.Object.Instantiate(_hostPrefab);
                _objects.Add(provisioned);
                callbackInput = provisioned.GetComponent<PlayerInput>();
                callbackHost = provisioned.GetComponent<LocalPlayerHostAuthoring>();
                _backend.RaiseJoined(callbackInput);
                throw new InvalidOperationException("synthetic failure after callback");
            };

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.ReservationResult.Succeeded, Is.True);
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(_backend.RejectedPlayers, Is.EquivalentTo(new[] { callbackInput }));
            Assert.That(ReferenceEquals(_backend.RejectedPlayers[0], callbackInput), Is.True);
            Assert.That(callbackHost.IsAdmissionStaged, Is.False);
            Assert.That(callbackHost.IsJoined, Is.False);
            Assert.That(_bridge.HasOperationInFlight, Is.False);
            Assert.That(_bridge.AdmittedPlayerCount, Is.Zero);
            Assert.That(_bridge.AwaitingCallbackConfirmationCount, Is.Zero);
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void DestroyedPlayerInputReferenceAfterCallback_IsRejectedAndReservationReleased()
        {
            PlayerInput destroyedInput = null;
            _backend.JoinPlayerHandler = _ =>
            {
                GameObject provisioned = UnityEngine.Object.Instantiate(_hostPrefab);
                _objects.Add(provisioned);
                destroyedInput = provisioned.GetComponent<PlayerInput>();
                _backend.RaiseJoined(destroyedInput);
                UnityEngine.Object.DestroyImmediate(provisioned);
                return destroyedInput;
            };

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(destroyedInput == null, Is.True);
            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedMissingPlayerInput));
            Assert.That(result.ReservationResult.Succeeded, Is.True);
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void BackendReturnsNullAfterReservation_RollsBackAndAllowsLaterJoin()
        {
            int calls = 0;
            _backend.JoinPlayerHandler = _ =>
            {
                calls++;
                if (calls == 1)
                    return null;

                GameObject provisioned = UnityEngine.Object.Instantiate(_hostPrefab);
                _objects.Add(provisioned);
                PlayerInput input = provisioned.GetComponent<PlayerInput>();
                _backend.RaiseJoined(input);
                return input;
            };

            LocalPlayerJoinResult rejected = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot afterRollback = _session.CreateSnapshot();
            LocalPlayerJoinResult recovered = _bridge.TryJoin(ValidRequest());

            Assert.That(rejected.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedProvisioningReturnedNull));
            Assert.That(rejected.HasReservationEvidence, Is.True);
            Assert.That(rejected.ReservationResult.Succeeded, Is.True);
            Assert.That(rejected.HasRollbackEvidence, Is.True);
            Assert.That(rejected.RollbackResult.Succeeded, Is.True);
            Assert.That(afterRollback.AvailableCount, Is.EqualTo(1));
            Assert.That(afterRollback.ReservedCount, Is.Zero);
            Assert.That(afterRollback.JoinedCount, Is.Zero);
            Assert.That(recovered.Status, Is.EqualTo(LocalPlayerJoinStatus.SucceededJoined));
            Assert.That(recovered.HasCommitEvidence, Is.True);
            Assert.That(recovered.CommitResult.Succeeded, Is.True);
            Assert.That(recovered.CallbackConfirmation,
                Is.EqualTo(LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput));
        }

        [Test]
        public void DifferentCallbackAndReturnedPlayerInputs_RollsBackAndRejectsBothInputs()
        {
            PlayerInput callbackInput = CreateObject("Callback PlayerInput").AddComponent<PlayerInput>();
            PlayerInput returnedInput = CreateObject("Returned PlayerInput").AddComponent<PlayerInput>();
            _backend.JoinPlayerHandler = _ =>
            {
                _backend.RaiseJoined(callbackInput);
                return returnedInput;
            };

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedCorrelationMismatch));
            Assert.That(result.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.RejectedCorrelationMismatch));
            Assert.That(result.CallbackConfirmation,
                Is.EqualTo(LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput));
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(_backend.RejectedPlayers, Is.EquivalentTo(new[] { returnedInput, callbackInput }));
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void MissingLocalPlayerHost_RollsBackAndRequestsCompensation()
        {
            PlayerInput input = CreateObject("Player Without Local Player Host").AddComponent<PlayerInput>();
            _backend.JoinPlayerHandler = _ =>
            {
                _backend.RaiseJoined(input);
                return input;
            };

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost));
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(_backend.RejectedPlayers, Is.EquivalentTo(new[] { input }));
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void InvalidLocalPlayerHostConfiguration_RollsBackAndRequestsCompensation()
        {
            GameObject provisioned = UnityEngine.Object.Instantiate(_hostPrefab);
            _objects.Add(provisioned);
            PlayerInput input = provisioned.GetComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = provisioned.GetComponent<LocalPlayerHostAuthoring>();
            var serializedHost = new SerializedObject(host);
            serializedHost.FindProperty("actorMount").objectReferenceValue = null;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            _backend.JoinPlayerHandler = _ =>
            {
                _backend.RaiseJoined(input);
                return input;
            };

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot final = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost));
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(_backend.RejectedPlayers, Is.EquivalentTo(new[] { input }));
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void HostCommitThrowsAfterSessionCommit_CompensatesHostSlotAndProvisionedPlayer()
        {
            PlayerInput provisionedInput = null;
            LocalPlayerHostAuthoring provisionedHost = null;
            _backend.JoinPlayerHandler = _ =>
            {
                GameObject provisioned = UnityEngine.Object.Instantiate(_hostPrefab);
                _objects.Add(provisioned);
                provisionedInput = provisioned.GetComponent<PlayerInput>();
                provisionedHost = provisioned.GetComponent<LocalPlayerHostAuthoring>();
                _backend.RaiseJoined(provisionedInput);
                return provisionedInput;
            };

            Action<PlayerSessionChange> invalidateStagedHostAfterSessionCommit = change =>
            {
                if (change.CurrentSlot.IsJoined)
                    provisionedHost.RollbackStagedAdmission("test", "inject host commit failure");
            };
            _session.Changed += invalidateStagedHostAfterSessionCommit;

            LocalPlayerJoinResult result;
            try
            {
                result = _bridge.TryJoin(ValidRequest());
            }
            finally
            {
                _session.Changed -= invalidateStagedHostAfterSessionCommit;
            }

            PlayerParticipationSnapshot final = _session.CreateSnapshot();
            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.FailedAdmission));
            Assert.That(result.HasCommitEvidence, Is.True);
            Assert.That(result.CommitResult.Succeeded, Is.True);
            Assert.That(result.HasRollbackEvidence, Is.True);
            Assert.That(result.RollbackResult.Succeeded, Is.True);
            Assert.That(provisionedHost.IsJoined, Is.False);
            Assert.That(_backend.RejectedPlayers, Is.EquivalentTo(new[] { provisionedInput }));
            Assert.That(final.AvailableCount, Is.EqualTo(1));
            Assert.That(final.ReservedCount, Is.Zero);
            Assert.That(final.JoinedCount, Is.Zero);
        }

        [Test]
        public void JoiningClosed_RejectsBeforeReservationOrBackendCall()
        {
            PlayerParticipationSnapshot initiallyOpen = _session.CreateSnapshot();
            Assert.That(initiallyOpen.JoiningOpen, Is.True);

            PlayerParticipationOperationResult closeResult =
                _session.TryCloseJoining("test", "closed");
            Assert.That(closeResult.Succeeded, Is.True);

            int backendCalls = 0;
            _backend.JoinPlayerHandler = _ =>
            {
                backendCalls++;
                return null;
            };
            PlayerParticipationSnapshot before = _session.CreateSnapshot();
            Assert.That(before.JoiningOpen, Is.False);
            Assert.That(before.AvailableCount, Is.EqualTo(1));
            Assert.That(before.ReservedCount, Is.Zero);

            LocalPlayerJoinResult result = _bridge.TryJoin(ValidRequest());
            PlayerParticipationSnapshot after = _session.CreateSnapshot();

            Assert.That(result.Status, Is.EqualTo(LocalPlayerJoinStatus.RejectedJoiningClosed));
            Assert.That(result.OriginalStatus, Is.EqualTo(LocalPlayerJoinStatus.RejectedJoiningClosed));
            Assert.That(result.HasReservationEvidence, Is.True);
            Assert.That(result.ReservationResult.Status,
                Is.EqualTo(PlayerParticipationOperationStatus.RejectedJoiningClosed));
            Assert.That(result.ReservationResult.Succeeded, Is.False);
            Assert.That(result.HasRollbackEvidence, Is.False);
            Assert.That(backendCalls, Is.Zero);
            Assert.That(after.JoiningOpen, Is.False);
            Assert.That(after.Revision, Is.EqualTo(before.Revision));
            Assert.That(after.AvailableCount, Is.EqualTo(before.AvailableCount));
            Assert.That(after.ReservedCount, Is.Zero);
            Assert.That(after.JoinedCount, Is.Zero);
            Assert.That(_bridge.HasOperationInFlight, Is.False);
        }

        private GameObject CreateValidHostPrefab()
        {
            GameObject root = CreateObject("Test Local Player Host Prefab");
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
            Transform actorMount = CreateObject("Actor Mount").transform;
            actorMount.SetParent(root.transform, false);

            var serializedHost = new SerializedObject(host);
            serializedHost.FindProperty("playerInput").objectReferenceValue = playerInput;
            serializedHost.FindProperty("actorMount").objectReferenceValue = actorMount;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.TryValidateConfiguration(out string issue), Is.True, issue);
            return root;
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        private static LocalPlayerJoinRequest ValidRequest() =>
            new("join-tests", "exercise provisioning contract");

        private sealed class FakeProvisioningBackend :
            ILocalPlayerProvisioningBackend,
            IAdmittedLocalPlayerReleaseBackend
        {
            private readonly GameObject _playerPrefab;

            internal FakeProvisioningBackend(GameObject playerPrefab)
            {
                _playerPrefab = playerPrefab;
            }

            internal Func<LocalPlayerJoinRequest, PlayerInput> JoinPlayerHandler { get; set; }

            internal List<PlayerInput> RejectedPlayers { get; } = new();

            public bool IsAvailable => true;

            public bool UsesManualJoin => true;

            public GameObject PlayerPrefab => _playerPrefab;

            public event Action<PlayerInput> PlayerJoined;

            public PlayerInput JoinPlayer(LocalPlayerJoinRequest request) =>
                JoinPlayerHandler != null ? JoinPlayerHandler(request) : null;

            public void RejectPlayer(PlayerInput playerInput, string source, string reason)
            {
                RejectedPlayers.Add(playerInput);
            }

            public void ReleaseAdmittedPlayer(PlayerInput playerInput, string source, string reason)
            {
            }

            internal void RaiseJoined(PlayerInput playerInput) => PlayerJoined?.Invoke(playerInput);
        }
    }
}
