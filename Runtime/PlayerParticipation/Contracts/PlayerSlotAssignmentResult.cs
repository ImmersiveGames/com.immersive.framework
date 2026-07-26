using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Typed result for a canonical current Player Slot assignment operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 current Player Slot assignment operation result.")]
    public sealed class PlayerSlotAssignmentResult
    {
        internal PlayerSlotAssignmentResult(
            PlayerSlotAssignmentStatus status,
            string operation,
            PlayerSlotAssignmentSnapshot previousAssignment,
            PlayerSlotAssignmentSnapshot currentAssignment,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousAssignment = previousAssignment;
            CurrentAssignment = currentAssignment;
            ExpectedToken = expectedToken;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerSlotAssignmentStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotAssignmentSnapshot PreviousAssignment { get; }
        public PlayerSlotAssignmentSnapshot CurrentAssignment { get; }
        public PlayerSlotAssignmentToken ExpectedToken { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerSlotAssignmentStatus.SucceededAssigned or
            PlayerSlotAssignmentStatus.SucceededAlreadyAssigned or
            PlayerSlotAssignmentStatus.SucceededConfirmed or
            PlayerSlotAssignmentStatus.SucceededReleased;

        public bool StateChanged => Status is
            PlayerSlotAssignmentStatus.SucceededAssigned or
            PlayerSlotAssignmentStatus.SucceededReleased;

        public bool HasCurrentAssignment => CurrentAssignment.IsAssigned;

        public PlayerSlotAssignmentToken AssignmentToken =>
            CurrentAssignment.IsAssigned
                ? CurrentAssignment.AssignmentToken
                : ExpectedToken;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{(CurrentAssignment.PlayerSlotId.IsValid ? CurrentAssignment.PlayerSlotId.StableText : PreviousAssignment.PlayerSlotId.StableText)}' " +
                $"token='{AssignmentToken.StableText}' source='{Source}' reason='{Reason}' " +
                $"message='{Message}'";
        }
    }
}
