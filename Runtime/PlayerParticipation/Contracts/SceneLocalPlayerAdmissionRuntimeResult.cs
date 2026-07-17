using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete public evidence for one Scene Local Player host/Slot admission or release attempt.
    /// The physical Host and Logical Actor remain externally owned by the scene.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B1 Scene Local Player admission transaction result and diagnostics.")]
    public sealed class SceneLocalPlayerAdmissionRuntimeResult
    {
        internal SceneLocalPlayerAdmissionRuntimeResult(
            SceneLocalPlayerAdmissionRuntimeStatus status,
            SceneLocalPlayerAdmissionRuntimeStatus originalStatus,
            string operation,
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken token,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult slotOperationResult,
            PlayerParticipationOperationResult compensationResult,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot,
            string source,
            string reason,
            string message)
        {
            Status = status;
            OriginalStatus = originalStatus == SceneLocalPlayerAdmissionRuntimeStatus.None
                ? status
                : originalStatus;
            Operation = operation ?? string.Empty;
            Authoring = authoring;
            Token = token;
            ReservationResult = reservationResult;
            SlotOperationResult = slotOperationResult;
            CompensationResult = compensationResult;
            PreviousSlot = previousSlot;
            CurrentSlot = currentSlot;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public SceneLocalPlayerAdmissionRuntimeStatus Status { get; }
        public SceneLocalPlayerAdmissionRuntimeStatus OriginalStatus { get; }
        public string Operation { get; }
        public SceneLocalPlayerAdmissionAuthoring Authoring { get; }
        public SceneLocalPlayerAdmissionToken Token { get; }
        public PlayerParticipationOperationResult ReservationResult { get; }
        public PlayerParticipationOperationResult SlotOperationResult { get; }
        public PlayerParticipationOperationResult CompensationResult { get; }
        public PlayerSlotRuntimeSnapshot PreviousSlot { get; }
        public PlayerSlotRuntimeSnapshot CurrentSlot { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted or
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyAdmitted or
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased or
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased;

        public bool Rejected => Status is
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedRuntimeUnavailable or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotOrderMismatch or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedCapacityReached or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotUnavailable or
            SceneLocalPlayerAdmissionRuntimeStatus.RejectedDependentState;

        public bool Failed => !Succeeded && !Rejected && Status != SceneLocalPlayerAdmissionRuntimeStatus.None;

        public bool StateChanged => Status is
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted or
            SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased;

        public bool HasToken => Token.IsValid;
        public bool HasReservationResult => ReservationResult != null;
        public bool HasSlotOperationResult => SlotOperationResult != null;
        public bool HasCompensationResult => CompensationResult != null;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' originalStatus='{OriginalStatus}' " +
                $"authoring='{(Authoring != null ? Authoring.name : string.Empty)}' " +
                $"token='{Token.StableText}' previousSlot='{SlotText(PreviousSlot)}' " +
                $"currentSlot='{SlotText(CurrentSlot)}' source='{Source}' reason='{Reason}' " +
                $"message='{Message}'";
        }

        internal static SceneLocalPlayerAdmissionRuntimeResult RuntimeUnavailable(
            string operation,
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason,
            string message)
        {
            return new SceneLocalPlayerAdmissionRuntimeResult(
                SceneLocalPlayerAdmissionRuntimeStatus.RejectedRuntimeUnavailable,
                SceneLocalPlayerAdmissionRuntimeStatus.RejectedRuntimeUnavailable,
                operation,
                authoring,
                default,
                null,
                null,
                null,
                default,
                default,
                source,
                reason,
                message);
        }

        private static string SlotText(PlayerSlotRuntimeSnapshot slot)
        {
            return slot.IsValid
                ? $"{slot.PlayerSlotId.StableText}:{slot.AllocationState}:{slot.Revision}"
                : string.Empty;
        }
    }
}
