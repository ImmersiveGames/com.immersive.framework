using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-ADR-046 projection from canonical Join result to its narrow outcome (I1).")]
    internal static class LocalPlayerJoinOutcomeProjection
    {
        internal static LocalPlayerJoinOutcome Project(LocalPlayerJoinResult result)
        {
            if (result == null)
                return Unknown();

            switch (result.Status)
            {
                case LocalPlayerJoinStatus.SucceededJoined:
                    return HasSuccessfulAdmissionEvidence(result)
                        ? Create(LocalPlayerJoinOutcomeCode.Joined, null, LocalPlayerJoinAdmissionState.Admitted)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedInvalidRequest:
                    return IsPreReservationResult(result) || IsReservationRejection(
                            result,
                            PlayerParticipationOperationStatus.RejectedInvalidRequest)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedInvalidRequest)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedOperationInFlight:
                    return IsPreReservationResult(result)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedBusy)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedRuntimeUnavailable:
                    return IsPreReservationResult(result)
                        || IsReservationRejection(
                            result,
                            PlayerParticipationOperationStatus.RejectedInvalidState)
                        || IsReservationRejection(
                            result,
                            PlayerParticipationOperationStatus.FailedInvalidConfiguration)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedUnavailable)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedManagerUnavailable:
                    return IsPreReservationResult(result)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedUnavailable)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedManagerConfiguration:
                    return IsPreReservationResult(result)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedConfiguration)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedJoiningClosed:
                    return IsReservationRejection(
                        result,
                        PlayerParticipationOperationStatus.RejectedJoiningClosed)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedJoiningClosed)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedNoAvailableSlot:
                    return IsReservationRejection(
                        result,
                        PlayerParticipationOperationStatus.RejectedNoAvailableSlot)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedNoAvailableSlot)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedDeviceAlreadyOwned:
                    return IsPreReservationResult(result)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedDeviceAlreadyOwned)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedForeignOrStaleReservation:
                    return IsReservationRejection(
                            result,
                            PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation) ||
                        HasConfirmedRollbackAfterReservation(result, allowSuccessfulCommit: false)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.RejectedSessionChanged)
                        : Unknown();

                case LocalPlayerJoinStatus.RejectedProvisioningReturnedNull:
                case LocalPlayerJoinStatus.RejectedCorrelationMismatch:
                case LocalPlayerJoinStatus.RejectedMissingPlayerInput:
                case LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost:
                case LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost:
                    return HasConfirmedRollbackAfterReservation(result, allowSuccessfulCommit: false)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.FailedAdmission)
                        : Unknown();

                case LocalPlayerJoinStatus.FailedAdmission:
                    return HasConfirmedRollbackAfterReservation(result, allowSuccessfulCommit: true)
                        ? NotAdmitted(LocalPlayerJoinOutcomeCode.FailedAdmission)
                        : Unknown();

                case LocalPlayerJoinStatus.FailedRollback:
                    return Create(
                        LocalPlayerJoinOutcomeCode.FailedRollback,
                        MapOriginalStatus(result.OriginalStatus),
                        LocalPlayerJoinAdmissionState.Unknown);

                case LocalPlayerJoinStatus.None:
                case LocalPlayerJoinStatus.RejectedUnexpectedJoin:
                default:
                    return Unknown();
            }
        }

        private static bool HasSuccessfulAdmissionEvidence(LocalPlayerJoinResult result)
        {
            return result.ReservationResult != null &&
                IsKnownOperationStatus(result.ReservationResult.Status) &&
                result.ReservationResult.Succeeded &&
                result.CommitResult != null &&
                IsKnownOperationStatus(result.CommitResult.Status) &&
                result.CommitResult.Succeeded &&
                result.RollbackResult == null &&
                HasReservationSlot(result.ReservationResult) &&
                result.CommitResult.Snapshot != null &&
                result.CommitResult.Snapshot.ContextId == result.ReservationResult.ReservationToken.ContextId &&
                result.CommitResult.ReservationToken == result.ReservationResult.ReservationToken &&
                result.CommitResult.Slot.IsJoined &&
                result.CommitResult.Slot.PlayerSlotId == result.ReservationResult.Slot.PlayerSlotId &&
                HasSnapshotSlot(result.CommitResult.Snapshot, result.CommitResult.Slot) &&
                result.Slot.IsJoined &&
                result.Slot.PlayerSlotId == result.CommitResult.Slot.PlayerSlotId &&
                result.Slot.Revision == result.CommitResult.Slot.Revision &&
                result.PlayerInput != null &&
                result.LocalPlayerHost != null &&
                object.ReferenceEquals(result.LocalPlayerHost.PlayerInput, result.PlayerInput) &&
                result.LocalPlayerHost.IsJoined &&
                result.LocalPlayerHost.JoinedPlayerSlotId == result.Slot.PlayerSlotId;
        }

        private static bool IsPreReservationResult(LocalPlayerJoinResult result)
        {
            return result.ReservationResult == null &&
                result.CommitResult == null &&
                result.RollbackResult == null;
        }

        private static bool IsReservationRejection(
            LocalPlayerJoinResult result,
            PlayerParticipationOperationStatus expectedStatus)
        {
            return result.ReservationResult != null &&
                IsKnownOperationStatus(result.ReservationResult.Status) &&
                result.ReservationResult.Status == expectedStatus &&
                !result.ReservationResult.Succeeded &&
                result.CommitResult == null &&
                result.RollbackResult == null;
        }

        private static bool HasConfirmedRollbackAfterReservation(
            LocalPlayerJoinResult result,
            bool allowSuccessfulCommit)
        {
            if (result.ReservationResult == null ||
                !IsKnownOperationStatus(result.ReservationResult.Status) ||
                !result.ReservationResult.Succeeded ||
                !HasReservationSlot(result.ReservationResult) ||
                result.RollbackResult == null ||
                !IsKnownOperationStatus(result.RollbackResult.Status) ||
                !result.RollbackResult.Succeeded ||
                !IsRollbackForReservation(result.ReservationResult, result.RollbackResult) ||
                result.RollbackResult.Slot.AllocationState != PlayerSlotAllocationState.Available ||
                !HasSnapshotSlot(result.RollbackResult.Snapshot, result.RollbackResult.Slot))
            {
                return false;
            }

            if (result.CommitResult == null)
                return true;

            if (!IsKnownOperationStatus(result.CommitResult.Status))
                return false;

            if (result.CommitResult.Succeeded)
            {
                return allowSuccessfulCommit &&
                    HasCommitForReservation(result.ReservationResult, result.CommitResult);
            }

            return IsNonSuccessCommitEvidence(result.CommitResult.Status) &&
                HasCommitFailureForReservation(result.ReservationResult, result.CommitResult);
        }

        private static bool HasReservationSlot(PlayerParticipationOperationResult reservationResult)
        {
            return reservationResult != null &&
                reservationResult.Slot.IsReserved &&
                reservationResult.ReservationToken.IsValid &&
                reservationResult.Slot.PlayerSlotId == reservationResult.ReservationToken.PlayerSlotId &&
                reservationResult.Slot.ReservationToken == reservationResult.ReservationToken &&
                reservationResult.Snapshot != null &&
                reservationResult.Snapshot.ContextId == reservationResult.ReservationToken.ContextId &&
                HasSnapshotSlot(reservationResult.Snapshot, reservationResult.Slot);
        }

        private static bool HasCommitForReservation(
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult)
        {
            return HasReservationSlot(reservationResult) &&
                commitResult != null &&
                commitResult.Succeeded &&
                commitResult.Snapshot != null &&
                commitResult.Snapshot.ContextId == reservationResult.ReservationToken.ContextId &&
                commitResult.ReservationToken == reservationResult.ReservationToken &&
                commitResult.Slot.IsJoined &&
                commitResult.Slot.PlayerSlotId == reservationResult.Slot.PlayerSlotId &&
                HasSnapshotSlot(commitResult.Snapshot, commitResult.Slot);
        }

        private static bool HasCommitFailureForReservation(
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult)
        {
            if (!HasReservationSlot(reservationResult) ||
                commitResult == null ||
                !IsNonSuccessCommitEvidence(commitResult.Status) ||
                commitResult.Snapshot == null ||
                commitResult.Snapshot.ContextId != reservationResult.ReservationToken.ContextId ||
                commitResult.ReservationToken != reservationResult.ReservationToken ||
                !HasSnapshotSlotWithId(
                    commitResult.Snapshot,
                    reservationResult.Slot.PlayerSlotId))
            {
                return false;
            }

            return !commitResult.Slot.IsValid ||
                (commitResult.Slot.PlayerSlotId == reservationResult.Slot.PlayerSlotId &&
                 HasSnapshotSlot(commitResult.Snapshot, commitResult.Slot));
        }

        private static bool IsRollbackForReservation(
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult rollbackResult)
        {
            if (reservationResult == null || rollbackResult == null ||
                !HasReservationSlot(reservationResult) ||
                !rollbackResult.Slot.IsValid ||
                rollbackResult.Slot.PlayerSlotId != reservationResult.Slot.PlayerSlotId ||
                rollbackResult.Snapshot == null ||
                rollbackResult.Snapshot.ContextId != reservationResult.ReservationToken.ContextId)
            {
                return false;
            }

            return !rollbackResult.ReservationToken.IsValid ||
                rollbackResult.ReservationToken == reservationResult.ReservationToken;
        }

        private static bool HasSnapshotSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotRuntimeSnapshot expectedSlot)
        {
            if (snapshot == null || !expectedSlot.IsValid)
                return false;

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerSlotId == expectedSlot.PlayerSlotId)
                {
                    return slot.Revision == expectedSlot.Revision &&
                        slot.AllocationState == expectedSlot.AllocationState &&
                        slot.ReservationToken == expectedSlot.ReservationToken;
                }
            }

            return false;
        }

        private static bool HasSnapshotSlotWithId(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            if (snapshot == null || !playerSlotId.IsValid)
                return false;

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                    return true;
            }

            return false;
        }

        private static bool IsNonSuccessCommitEvidence(PlayerParticipationOperationStatus status)
        {
            return status is
                PlayerParticipationOperationStatus.RejectedInvalidRequest or
                PlayerParticipationOperationStatus.RejectedInvalidState or
                PlayerParticipationOperationStatus.RejectedJoiningClosed or
                PlayerParticipationOperationStatus.RejectedNoAvailableSlot or
                PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation or
                PlayerParticipationOperationStatus.FailedInvalidConfiguration;
        }

        private static bool IsKnownOperationStatus(PlayerParticipationOperationStatus status)
        {
            return status is
                PlayerParticipationOperationStatus.None or
                PlayerParticipationOperationStatus.Succeeded or
                PlayerParticipationOperationStatus.IgnoredNoChange or
                PlayerParticipationOperationStatus.RejectedInvalidRequest or
                PlayerParticipationOperationStatus.RejectedInvalidState or
                PlayerParticipationOperationStatus.RejectedJoiningClosed or
                PlayerParticipationOperationStatus.RejectedNoAvailableSlot or
                PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation or
                PlayerParticipationOperationStatus.FailedInvalidConfiguration;
        }

        private static LocalPlayerJoinOutcomeCode MapOriginalStatus(LocalPlayerJoinStatus status)
        {
            return status switch
            {
                LocalPlayerJoinStatus.SucceededJoined => LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinStatus.RejectedInvalidRequest => LocalPlayerJoinOutcomeCode.RejectedInvalidRequest,
                LocalPlayerJoinStatus.RejectedOperationInFlight => LocalPlayerJoinOutcomeCode.RejectedBusy,
                LocalPlayerJoinStatus.RejectedRuntimeUnavailable or
                LocalPlayerJoinStatus.RejectedManagerUnavailable => LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinStatus.RejectedManagerConfiguration => LocalPlayerJoinOutcomeCode.RejectedConfiguration,
                LocalPlayerJoinStatus.RejectedJoiningClosed => LocalPlayerJoinOutcomeCode.RejectedJoiningClosed,
                LocalPlayerJoinStatus.RejectedNoAvailableSlot => LocalPlayerJoinOutcomeCode.RejectedNoAvailableSlot,
                LocalPlayerJoinStatus.RejectedDeviceAlreadyOwned => LocalPlayerJoinOutcomeCode.RejectedDeviceAlreadyOwned,
                LocalPlayerJoinStatus.RejectedForeignOrStaleReservation => LocalPlayerJoinOutcomeCode.RejectedSessionChanged,
                LocalPlayerJoinStatus.RejectedProvisioningReturnedNull or
                LocalPlayerJoinStatus.RejectedCorrelationMismatch or
                LocalPlayerJoinStatus.RejectedMissingPlayerInput or
                LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost or
                LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost or
                LocalPlayerJoinStatus.FailedAdmission => LocalPlayerJoinOutcomeCode.FailedAdmission,
                _ => LocalPlayerJoinOutcomeCode.FailedUnknown
            };
        }

        private static LocalPlayerJoinOutcome NotAdmitted(LocalPlayerJoinOutcomeCode code)
        {
            return Create(code, null, LocalPlayerJoinAdmissionState.NotAdmitted);
        }

        private static LocalPlayerJoinOutcome Unknown()
        {
            return Create(
                LocalPlayerJoinOutcomeCode.FailedUnknown,
                null,
                LocalPlayerJoinAdmissionState.Unknown);
        }

        private static LocalPlayerJoinOutcome Create(
            LocalPlayerJoinOutcomeCode code,
            LocalPlayerJoinOutcomeCode? originalCode,
            LocalPlayerJoinAdmissionState admissionState)
        {
            return new LocalPlayerJoinOutcome(code, originalCode, admissionState);
        }
    }
}
