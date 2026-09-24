using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal evidence for one logical Session Player Leave state transition or validation.
    /// Physical resource release is intentionally outside the ADR-020 foundation transaction.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 logical Session Player Leave foundation result and diagnostics.")]
    internal sealed class SessionPlayerLeaveRuntimeResult
    {
        internal SessionPlayerLeaveRuntimeResult(
            SessionPlayerLeaveRuntimeStatus status,
            string operation,
            SessionPlayerLeaveToken token,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot,
            int previousContextRevision,
            int currentContextRevision,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Operation = operation ?? string.Empty;
            Token = token;
            PreviousSlot = previousSlot;
            CurrentSlot = currentSlot;
            PreviousContextRevision = previousContextRevision;
            CurrentContextRevision = currentContextRevision;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal SessionPlayerLeaveRuntimeStatus Status { get; }
        internal string Operation { get; }
        internal SessionPlayerLeaveToken Token { get; }
        internal PlayerSlotRuntimeSnapshot PreviousSlot { get; }
        internal PlayerSlotRuntimeSnapshot CurrentSlot { get; }
        internal int PreviousContextRevision { get; }
        internal int CurrentContextRevision { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal bool Succeeded => Status is
            SessionPlayerLeaveRuntimeStatus.SucceededLeaving or
            SessionPlayerLeaveRuntimeStatus.SucceededAlreadyLeaving or
            SessionPlayerLeaveRuntimeStatus.SucceededConfirmed or
            SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionCleared or
            SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionAlreadyClear or
            SessionPlayerLeaveRuntimeStatus.SucceededCommitted;

        internal bool Rejected => Status is
            SessionPlayerLeaveRuntimeStatus.RejectedInvalidRequest or
            SessionPlayerLeaveRuntimeStatus.RejectedSlotNotConfigured or
            SessionPlayerLeaveRuntimeStatus.RejectedSlotNotJoined or
            SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence or
            SessionPlayerLeaveRuntimeStatus.RejectedDependentState;

        internal bool Failed => Status == SessionPlayerLeaveRuntimeStatus.FailedInvariant;

        internal bool StateChanged => CurrentContextRevision != PreviousContextRevision;

        internal string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' token='{Token.StableText}' " +
                $"previousSlot='{SlotText(PreviousSlot)}' currentSlot='{SlotText(CurrentSlot)}' " +
                $"previousContextRevision='{PreviousContextRevision}' currentContextRevision='{CurrentContextRevision}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        private static string SlotText(PlayerSlotRuntimeSnapshot slot)
        {
            return slot.IsValid
                ? $"{slot.PlayerSlotId.StableText}:{slot.AllocationState}:{slot.Revision}:selection-{slot.SelectionRevision}"
                : string.Empty;
        }
    }
}
