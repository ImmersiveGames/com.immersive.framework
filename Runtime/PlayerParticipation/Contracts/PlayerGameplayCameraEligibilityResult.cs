using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete immutable evidence for one prepared Player camera eligibility operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 prepared Player camera eligibility operation result.")]
    public sealed class PlayerGameplayCameraEligibilityResult
    {
        internal PlayerGameplayCameraEligibilityResult(
            PlayerGameplayCameraEligibilityStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilitySummary previousSummary,
            PlayerGameplayCameraEligibilitySummary currentSummary,
            PlayerGameplayCameraEligibilitySnapshot snapshot,
            string message)
        {
            Status = status;
            Operation = operation ?? string.Empty;
            PlayerSlotId = playerSlotId;
            PreviousSummary = previousSummary;
            CurrentSummary = currentSummary;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public PlayerGameplayCameraEligibilityStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayCameraEligibilitySummary PreviousSummary { get; }
        public PlayerGameplayCameraEligibilitySummary CurrentSummary { get; }
        public PlayerGameplayCameraEligibilitySnapshot Snapshot { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerGameplayCameraEligibilityStatus.SucceededEligible or
            PlayerGameplayCameraEligibilityStatus.SucceededSkippedOptional or
            PlayerGameplayCameraEligibilityStatus.SucceededReleased or
            PlayerGameplayCameraEligibilityStatus.SucceededAlreadyEligible or
            PlayerGameplayCameraEligibilityStatus.SucceededAlreadySkipped or
            PlayerGameplayCameraEligibilityStatus.SucceededAlreadyReleased;

        public bool Rejected =>
            !Succeeded &&
            Status != PlayerGameplayCameraEligibilityStatus.None;

        public bool Completed =>
            Status != PlayerGameplayCameraEligibilityStatus.None;

        public bool StateChanged =>
            PreviousSummary.State != CurrentSummary.State ||
            PreviousSummary.Token != CurrentSummary.Token;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"previous=({PreviousSummary.ToDiagnosticString()}) " +
                $"current=({CurrentSummary.ToDiagnosticString()}) " +
                $"message='{Message}'";
        }
    }
}
