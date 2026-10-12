using System;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class LocalPlayerJoinOutcomeProjectionTests
    {
        [TestCase(LocalPlayerJoinStatus.RejectedInvalidRequest, LocalPlayerJoinOutcomeCode.RejectedInvalidRequest)]
        [TestCase(LocalPlayerJoinStatus.RejectedOperationInFlight, LocalPlayerJoinOutcomeCode.RejectedBusy)]
        [TestCase(LocalPlayerJoinStatus.RejectedRuntimeUnavailable, LocalPlayerJoinOutcomeCode.RejectedUnavailable)]
        [TestCase(LocalPlayerJoinStatus.RejectedManagerUnavailable, LocalPlayerJoinOutcomeCode.RejectedUnavailable)]
        [TestCase(LocalPlayerJoinStatus.RejectedManagerConfiguration, LocalPlayerJoinOutcomeCode.RejectedConfiguration)]
        [TestCase(LocalPlayerJoinStatus.RejectedJoiningClosed, LocalPlayerJoinOutcomeCode.RejectedJoiningClosed)]
        [TestCase(LocalPlayerJoinStatus.RejectedNoAvailableSlot, LocalPlayerJoinOutcomeCode.RejectedNoAvailableSlot)]
        [TestCase(LocalPlayerJoinStatus.RejectedDeviceAlreadyOwned, LocalPlayerJoinOutcomeCode.RejectedDeviceAlreadyOwned)]
        public void ProjectsPreAdmissionRejections(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinOutcomeCode expectedCode)
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult result = status switch
            {
                LocalPlayerJoinStatus.RejectedJoiningClosed or
                LocalPlayerJoinStatus.RejectedNoAvailableSlot => fixture.CreateReservationRejection(status),
                _ => fixture.CreateResult(status)
            };
            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            Assert.That(outcome.Code, Is.EqualTo(expectedCode));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.NotAdmitted));
            Assert.That(outcome.OriginalCode, Is.Null);
        }

        [TestCase(LocalPlayerJoinStatus.RejectedProvisioningReturnedNull)]
        [TestCase(LocalPlayerJoinStatus.RejectedCorrelationMismatch)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingPlayerInput)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.FailedAdmission)]
        public void PostReservationFailuresRequireConfirmedRollback(LocalPlayerJoinStatus status)
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult result = fixture.CreatePostReservationFailure(status);

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedAdmission));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.NotAdmitted));
            Assert.That(outcome.OriginalCode, Is.Null);
        }

        [TestCase(LocalPlayerJoinStatus.RejectedProvisioningReturnedNull)]
        [TestCase(LocalPlayerJoinStatus.RejectedCorrelationMismatch)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingPlayerInput)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.FailedAdmission)]
        public void PostReservationFailuresWithoutConfirmedRollbackFailClosed(LocalPlayerJoinStatus status)
        {
            using var fixture = new EvidenceFixture();
            // Deliberately inconsistent synthetic result: a non-FailedRollback terminal status
            // carries no proof that Session recovery completed.
            LocalPlayerJoinResult result = fixture.CreateResult(
                status,
                reservation: fixture.Reserve(),
                rollback: null);

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedUnknown));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
        }

        [TestCase(LocalPlayerJoinStatus.RejectedForeignOrStaleReservation)]
        [TestCase(LocalPlayerJoinStatus.RejectedJoiningClosed)]
        [TestCase(LocalPlayerJoinStatus.RejectedNoAvailableSlot)]
        public void ReservationRejectionsRequireMatchingFailedReservation(LocalPlayerJoinStatus status)
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult result = fixture.CreateReservationRejection(status);

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            LocalPlayerJoinOutcomeCode expected = status switch
            {
                LocalPlayerJoinStatus.RejectedForeignOrStaleReservation => LocalPlayerJoinOutcomeCode.RejectedSessionChanged,
                LocalPlayerJoinStatus.RejectedJoiningClosed => LocalPlayerJoinOutcomeCode.RejectedJoiningClosed,
                _ => LocalPlayerJoinOutcomeCode.RejectedNoAvailableSlot
            };
            Assert.That(outcome.Code, Is.EqualTo(expected));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.NotAdmitted));
        }

        [Test]
        public void InvalidRequestFromReservationPolicyMapsToInvalidRequest()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(
                fixture.CreateProvisioningModeRejection());

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.RejectedInvalidRequest));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.NotAdmitted));
        }

        [Test]
        public void SucceededJoinedProjectsAdmittedOnlyWithSuccessfulReservationAndCommit()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult result = fixture.CreateJoinedResult();

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.Joined));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Admitted));
        }

        [Test]
        public void FailedAdmissionAfterCommitStillRequiresSuccessfulRollback()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(
                fixture.CreatePostCommitFailure());

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedAdmission));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.NotAdmitted));
        }

        [Test]
        public void FailedRollbackRemainsUnknownWhenSessionRollbackSucceededButHostRemainsJoined()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult joined = fixture.CreateJoinedResult();
            PlayerParticipationOperationResult sessionRollback = fixture.AbandonJoinedSlot(
                joined.Slot.PlayerSlotId);
            LocalPlayerJoinResult failedRollback = fixture.CreateResult(
                LocalPlayerJoinStatus.FailedRollback,
                LocalPlayerJoinStatus.FailedAdmission,
                joined.ReservationResult,
                joined.CommitResult,
                sessionRollback,
                joined.PlayerInput,
                joined.LocalPlayerHost,
                sessionRollback.Slot);

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(failedRollback);

            Assert.That(sessionRollback.Succeeded, Is.True);
            Assert.That(joined.LocalPlayerHost.IsJoined, Is.True);
            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedRollback));
            Assert.That(outcome.OriginalCode, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedAdmission));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
        }

        [TestCase(LocalPlayerJoinStatus.FailedAdmission, LocalPlayerJoinOutcomeCode.FailedAdmission)]
        [TestCase(LocalPlayerJoinStatus.RejectedForeignOrStaleReservation, LocalPlayerJoinOutcomeCode.RejectedSessionChanged)]
        [TestCase((LocalPlayerJoinStatus)999, LocalPlayerJoinOutcomeCode.FailedUnknown)]
        public void FailedRollbackPreservesMappedOriginalCause(
            LocalPlayerJoinStatus originalStatus,
            LocalPlayerJoinOutcomeCode expectedOriginalCode)
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinResult result = fixture.CreateFailedRollback(originalStatus);

            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedRollback));
            Assert.That(outcome.OriginalCode, Is.EqualTo(expectedOriginalCode));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
        }

        [Test]
        public void NullAndUnknownStatusesFailClosed()
        {
            LocalPlayerJoinOutcome nullOutcome = LocalPlayerJoinOutcomeProjection.Project(null);
            LocalPlayerJoinOutcome unknownOutcome = LocalPlayerJoinOutcomeProjection.Project(CreateResult((LocalPlayerJoinStatus)999));

            AssertUnknown(nullOutcome);
            AssertUnknown(unknownOutcome);
        }

        [Test]
        public void FalseSuccessFailsClosedAndFailedRollbackRemainsUnknownWithoutSessionProof()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinOutcome falseSuccess = LocalPlayerJoinOutcomeProjection.Project(
                fixture.CreateResult(LocalPlayerJoinStatus.SucceededJoined));
            LocalPlayerJoinOutcome unprovenRollback = LocalPlayerJoinOutcomeProjection.Project(
                fixture.CreateResult(
                    LocalPlayerJoinStatus.FailedRollback,
                    originalStatus: LocalPlayerJoinStatus.FailedAdmission,
                    reservation: fixture.Reserve()));

            AssertUnknown(falseSuccess);
            Assert.That(unprovenRollback.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedRollback));
            Assert.That(unprovenRollback.OriginalCode, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedAdmission));
            Assert.That(unprovenRollback.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
        }

        [Test]
        public void ReservationCommitAndRollbackMustReferToTheSameSlotOccurrence()
        {
            using var operation = new EvidenceFixture();
            using var foreignSession = new EvidenceFixture();
            using var foreignRollbackSession = new EvidenceFixture();
            LocalPlayerJoinResult joined = operation.CreateJoinedResult();
            LocalPlayerJoinResult foreignJoined = foreignSession.CreateJoinedResult();
            LocalPlayerJoinResult mismatchedCommit = operation.CreateResult(
                LocalPlayerJoinStatus.SucceededJoined,
                reservation: joined.ReservationResult,
                commit: foreignJoined.CommitResult,
                playerInput: joined.PlayerInput,
                host: joined.LocalPlayerHost,
                slot: joined.Slot);
            PlayerParticipationOperationResult foreignReservation = foreignRollbackSession.Reserve();
            PlayerParticipationOperationResult foreignRollback = foreignRollbackSession.Release(foreignReservation);
            LocalPlayerJoinResult mismatchedRollback = operation.CreateResult(
                LocalPlayerJoinStatus.FailedAdmission,
                reservation: joined.ReservationResult,
                rollback: foreignRollback);
            LocalPlayerJoinResult commitAsRollback = operation.CreateResult(
                LocalPlayerJoinStatus.FailedAdmission,
                reservation: joined.ReservationResult,
                commit: joined.CommitResult,
                rollback: joined.CommitResult);

            AssertUnknown(LocalPlayerJoinOutcomeProjection.Project(mismatchedCommit));
            AssertUnknown(LocalPlayerJoinOutcomeProjection.Project(mismatchedRollback));
            AssertUnknown(LocalPlayerJoinOutcomeProjection.Project(commitAsRollback));
        }

        [Test]
        public void UnexpectedJoinNeverProjectsAsJoined()
        {
            using var fixture = new EvidenceFixture();
            LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(
                fixture.CreateResult(LocalPlayerJoinStatus.RejectedUnexpectedJoin));

            AssertUnknown(outcome);
        }

        [Test]
        public void OutcomeExposesOnlyTypedDispositionPropertiesAndIsStable()
        {
            var propertyTypes = typeof(LocalPlayerJoinOutcome).GetProperties()
                .Select(property => property.PropertyType)
                .ToArray();

            Assert.That(propertyTypes, Is.EquivalentTo(new[]
            {
                typeof(LocalPlayerJoinOutcomeCode),
                typeof(LocalPlayerJoinOutcomeCode?),
                typeof(LocalPlayerJoinAdmissionState)
            }));
            AssertStable(typeof(LocalPlayerJoinOutcome));
            AssertStable(typeof(LocalPlayerJoinOutcomeCode));
            AssertStable(typeof(LocalPlayerJoinAdmissionState));
        }

        [Test]
        public void OnlyJoinedOutcomeCanCarryAdmittedState()
        {
            foreach (LocalPlayerJoinStatus status in Enum.GetValues(typeof(LocalPlayerJoinStatus)))
            {
                using var fixture = new EvidenceFixture();
                LocalPlayerJoinResult result = status switch
                {
                    LocalPlayerJoinStatus.SucceededJoined => fixture.CreateJoinedResult(),
                    LocalPlayerJoinStatus.RejectedJoiningClosed => fixture.CreateReservationRejection(status),
                    LocalPlayerJoinStatus.RejectedNoAvailableSlot => fixture.CreateReservationRejection(status),
                    LocalPlayerJoinStatus.RejectedForeignOrStaleReservation => fixture.CreateReservationRejection(status),
                    LocalPlayerJoinStatus.RejectedProvisioningReturnedNull or
                    LocalPlayerJoinStatus.RejectedCorrelationMismatch or
                    LocalPlayerJoinStatus.RejectedMissingPlayerInput or
                    LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost or
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost or
                    LocalPlayerJoinStatus.FailedAdmission => fixture.CreatePostReservationFailure(status),
                    LocalPlayerJoinStatus.FailedRollback => fixture.CreateFailedRollback(LocalPlayerJoinStatus.FailedAdmission),
                    _ => fixture.CreateResult(status)
                };
                LocalPlayerJoinOutcome outcome = LocalPlayerJoinOutcomeProjection.Project(result);

                Assert.That(
                    outcome.AdmissionState == LocalPlayerJoinAdmissionState.Admitted,
                    Is.EqualTo(outcome.Code == LocalPlayerJoinOutcomeCode.Joined),
                    status.ToString());
            }
        }

        private static LocalPlayerJoinResult CreateResult(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinStatus originalStatus = LocalPlayerJoinStatus.None,
            PlayerParticipationOperationResult reservation = null,
            PlayerParticipationOperationResult commit = null,
            PlayerParticipationOperationResult rollback = null)
        {
            return new LocalPlayerJoinResult(
                status,
                default,
                new LocalPlayerJoinRequest("outcome-test", "projection"),
                reservation,
                commit,
                rollback,
                default,
                null,
                null,
                -1,
                LocalPlayerJoinCallbackConfirmation.None,
                string.Empty,
                originalStatus);
        }

        private static void AssertUnknown(LocalPlayerJoinOutcome outcome)
        {
            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedUnknown));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
            Assert.That(outcome.OriginalCode, Is.Null);
        }

        private static void AssertStable(Type type)
        {
            FrameworkApiStatusAttribute attribute = (FrameworkApiStatusAttribute)Attribute.GetCustomAttribute(
                type,
                typeof(FrameworkApiStatusAttribute));

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.Status, Is.EqualTo(FrameworkApiStatus.Stable));
        }

        private sealed class EvidenceFixture : IDisposable
        {
            private readonly PlayerSlotProfile _profile;
            private readonly PlayerParticipationRuntimeContext _session;
            private readonly System.Collections.Generic.List<GameObject> _objects = new();

            internal EvidenceFixture()
            {
                _profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                PlayerParticipationRuntimeContext session;
                Assert.That(PlayerParticipationRuntimeContext.TryCreate(
                    new[] { _profile },
                    initialJoiningOpen: true,
                    source: nameof(LocalPlayerJoinOutcomeProjectionTests),
                    reason: "projection evidence fixture",
                    out session).Succeeded, Is.True);
                _session = session;
            }

            internal PlayerParticipationOperationResult Reserve()
            {
                return _session.TryReserveNextAvailableSlot("projection-test", "reserve");
            }

            internal PlayerParticipationOperationResult Release(PlayerParticipationOperationResult reservation)
            {
                return _session.TryReleaseReservation(
                    reservation.ReservationToken,
                    "projection-test",
                    "release");
            }

            internal LocalPlayerJoinResult CreateResult(
                LocalPlayerJoinStatus status,
                LocalPlayerJoinStatus originalStatus = LocalPlayerJoinStatus.None,
                PlayerParticipationOperationResult reservation = null,
                PlayerParticipationOperationResult commit = null,
                PlayerParticipationOperationResult rollback = null,
                PlayerInput playerInput = null,
                LocalPlayerHostAuthoring host = null,
                PlayerSlotRuntimeSnapshot slot = default)
            {
                return new LocalPlayerJoinResult(
                    status,
                    default,
                    new LocalPlayerJoinRequest("outcome-test", "projection"),
                    reservation,
                    commit,
                    rollback,
                    slot,
                    playerInput,
                    host,
                    -1,
                    LocalPlayerJoinCallbackConfirmation.None,
                    string.Empty,
                    originalStatus);
            }

            internal LocalPlayerJoinResult CreatePostReservationFailure(LocalPlayerJoinStatus status)
            {
                PlayerParticipationOperationResult reservation = Reserve();
                PlayerParticipationOperationResult rollback = _session.TryReleaseReservation(
                    reservation.ReservationToken,
                    "projection-test",
                    "rollback");
                return CreateResult(status, reservation: reservation, rollback: rollback);
            }

            internal LocalPlayerJoinResult CreateReservationRejection(LocalPlayerJoinStatus status)
            {
                PlayerParticipationOperationResult reservation;
                if (status == LocalPlayerJoinStatus.RejectedJoiningClosed)
                {
                    _session.TryCloseJoining("projection-test", "close");
                    reservation = Reserve();
                }
                else if (status == LocalPlayerJoinStatus.RejectedNoAvailableSlot)
                {
                    Reserve();
                    reservation = Reserve();
                }
                else
                {
                    reservation = _session.TryReleaseReservation(
                        default,
                        "projection-test",
                        "stale-token");
                }

                return CreateResult(status, reservation: reservation);
            }

            internal LocalPlayerJoinResult CreateProvisioningModeRejection()
            {
                PlayerParticipationOperationResult reservation =
                    _session.TryReserveNextAvailableSlot(
                        PlayerHostProvisioningMode.SceneProvided,
                        "projection-test",
                        "mismatched-provisioning-mode");
                Assert.That(reservation.Status,
                    Is.EqualTo(PlayerParticipationOperationStatus.RejectedInvalidRequest));
                return CreateResult(
                    LocalPlayerJoinStatus.RejectedInvalidRequest,
                    reservation: reservation);
            }

            internal LocalPlayerJoinResult CreateJoinedResult()
            {
                PlayerParticipationOperationResult reservation = Reserve();
                LocalPlayerHostAuthoring host = CreateHost(out PlayerInput playerInput);
                Assert.That(host.TryStageAdmission(
                    reservation.Slot,
                    "projection-test",
                    "join",
                    out string stageIssue), Is.True, stageIssue);
                PlayerParticipationOperationResult commit = _session.TryMarkJoined(
                    reservation.ReservationToken,
                    "projection-test",
                    "commit");
                Assert.That(commit.Succeeded, Is.True);
                host.CommitStagedAdmission(commit.Slot, "projection-test", "commit");
                return CreateResult(
                    LocalPlayerJoinStatus.SucceededJoined,
                    reservation: reservation,
                    commit: commit,
                    playerInput: playerInput,
                    host: host,
                    slot: commit.Slot);
            }

            internal LocalPlayerJoinResult CreatePostCommitFailure()
            {
                PlayerParticipationOperationResult reservation = Reserve();
                PlayerParticipationOperationResult commit = _session.TryMarkJoined(
                    reservation.ReservationToken,
                    "projection-test",
                    "commit");
                Assert.That(commit.Succeeded, Is.True);
                LocalPlayerHostAuthoring host = CreateHost(out PlayerInput playerInput);
                Assert.That(host.TryStageAdmission(
                    reservation.Slot,
                    "projection-test",
                    "join",
                    out string stageIssue), Is.True, stageIssue);
                host.CommitStagedAdmission(commit.Slot, "projection-test", "commit");
                Assert.That(host.TryReleaseCommittedAdmission(
                    commit.Slot.PlayerSlotId,
                    "projection-test",
                    "rollback",
                    out string releaseIssue), Is.True, releaseIssue);
                PlayerParticipationOperationResult rollback = _session.TryAbandonJoinedSlotAfterAssignmentFailure(
                    reservation.Slot.PlayerSlotId,
                    "projection-test",
                    "rollback-after-commit");
                return CreateResult(
                    LocalPlayerJoinStatus.FailedAdmission,
                    reservation: reservation,
                    commit: commit,
                    rollback: rollback,
                    playerInput: playerInput,
                    host: host,
                    slot: rollback.Slot);
            }

            internal PlayerParticipationOperationResult AbandonJoinedSlot(PlayerSlotId playerSlotId)
            {
                return _session.TryAbandonJoinedSlotAfterAssignmentFailure(
                    playerSlotId,
                    "projection-test",
                    "host-release-failure");
            }

            internal LocalPlayerJoinResult CreateFailedRollback(LocalPlayerJoinStatus originalStatus)
            {
                PlayerParticipationOperationResult reservation = Reserve();
                PlayerParticipationOperationResult firstRelease = _session.TryReleaseReservation(
                    reservation.ReservationToken,
                    "projection-test",
                    "first-release");
                Assert.That(firstRelease.Succeeded, Is.True);
                PlayerParticipationOperationResult failedRollback = _session.TryReleaseReservation(
                    reservation.ReservationToken,
                    "projection-test",
                    "stale-second-release");
                return CreateResult(
                    LocalPlayerJoinStatus.FailedRollback,
                    originalStatus,
                    reservation,
                    rollback: failedRollback);
            }

            private LocalPlayerHostAuthoring CreateHost(out PlayerInput playerInput)
            {
                var root = new GameObject("Projection Test Player");
                _objects.Add(root);
                playerInput = root.AddComponent<PlayerInput>();
                LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
                var actorMount = new GameObject("Actor Mount");
                _objects.Add(actorMount);
                actorMount.transform.SetParent(root.transform, false);

                var serializedHost = new SerializedObject(host);
                serializedHost.FindProperty("playerInput").objectReferenceValue = playerInput;
                serializedHost.FindProperty("actorMount").objectReferenceValue = actorMount.transform;
                serializedHost.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(host.TryValidateConfiguration(out string issue), Is.True, issue);
                return host;
            }

            public void Dispose()
            {
                for (int index = _objects.Count - 1; index >= 0; index--)
                {
                    if (_objects[index] != null)
                        UnityEngine.Object.DestroyImmediate(_objects[index]);
                }
                if (_profile != null)
                    UnityEngine.Object.DestroyImmediate(_profile);
            }
        }
    }
}
