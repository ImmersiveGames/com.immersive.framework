using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Typed result for every Session participation state-changing operation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F Player participation operation result.")]
    public sealed class PlayerParticipationOperationResult
    {
        internal PlayerParticipationOperationResult(
            PlayerParticipationOperationStatus status,
            string operation,
            string source,
            string reason,
            string message,
            int previousRevision,
            int currentRevision,
            PlayerSlotRuntimeSnapshot slot,
            PlayerSlotReservationToken reservationToken,
            PlayerParticipationSnapshot snapshot)
        {
            Status = status;
            Operation = operation ?? string.Empty;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
            PreviousRevision = previousRevision;
            CurrentRevision = currentRevision;
            Slot = slot;
            ReservationToken = reservationToken;
            Snapshot = snapshot;
        }

        public PlayerParticipationOperationStatus Status { get; }

        public string Operation { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int PreviousRevision { get; }

        public int CurrentRevision { get; }

        public PlayerSlotRuntimeSnapshot Slot { get; }

        public PlayerSlotReservationToken ReservationToken { get; }

        public PlayerParticipationSnapshot Snapshot { get; }

        public bool Succeeded => Status == PlayerParticipationOperationStatus.Succeeded;

        public bool IgnoredNoChange => Status == PlayerParticipationOperationStatus.IgnoredNoChange;

        public bool Rejected => Status is
            PlayerParticipationOperationStatus.RejectedInvalidRequest or
            PlayerParticipationOperationStatus.RejectedInvalidState or
            PlayerParticipationOperationStatus.RejectedJoiningClosed or
            PlayerParticipationOperationStatus.RejectedCapacityReached or
            PlayerParticipationOperationStatus.RejectedNoAvailableSlot or
            PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation;

        public bool Failed => Status == PlayerParticipationOperationStatus.FailedInvalidConfiguration;

        public bool Completed => Succeeded || IgnoredNoChange;

        public bool StateChanged => Succeeded && CurrentRevision != PreviousRevision;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' source='{Source}' reason='{Reason}' " +
                $"previousRevision='{PreviousRevision}' currentRevision='{CurrentRevision}' " +
                $"slot='{(Slot.PlayerSlotId.IsValid ? Slot.PlayerSlotId.StableText : string.Empty)}' " +
                $"reservation='{ReservationToken.StableText}' message='{Message}'";
        }

        internal static PlayerParticipationOperationResult RuntimeUnavailable(
            string operation,
            string source,
            string reason,
            string message)
        {
            string resolvedOperation = operation.NormalizeTextOrFallback("PlayerParticipationOperation");
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("runtime-unavailable");
            string resolvedMessage = message.NormalizeTextOrFallback(
                "Player participation runtime is unavailable.");
            PlayerParticipationSnapshot snapshot = PlayerParticipationSnapshot.Empty(
                PlayerParticipationOperationStatus.RejectedInvalidState,
                resolvedMessage);

            return new PlayerParticipationOperationResult(
                PlayerParticipationOperationStatus.RejectedInvalidState,
                resolvedOperation,
                resolvedSource,
                resolvedReason,
                resolvedMessage,
                0,
                0,
                default,
                default,
                snapshot);
        }
    }
}
