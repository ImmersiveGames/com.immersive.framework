using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete immutable evidence for one effective occupancy operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 effective Player gameplay occupancy operation result.")]
    public sealed class PlayerGameplayOccupancyResult
    {
        internal PlayerGameplayOccupancyResult(
            PlayerGameplayOccupancyStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancySummary previousSummary,
            PlayerGameplayOccupancySummary currentSummary,
            PlayerGameplayOccupancySnapshot snapshot,
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

        public PlayerGameplayOccupancyStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayOccupancySummary PreviousSummary { get; }
        public PlayerGameplayOccupancySummary CurrentSummary { get; }
        public PlayerGameplayOccupancySnapshot Snapshot { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerGameplayOccupancyStatus.SucceededOccupied or
            PlayerGameplayOccupancyStatus.SucceededReleased or
            PlayerGameplayOccupancyStatus.SucceededAlreadyOccupied or
            PlayerGameplayOccupancyStatus.SucceededAlreadyReleased;

        public bool Rejected => !Succeeded && Status != PlayerGameplayOccupancyStatus.None;
        public bool Completed => Status != PlayerGameplayOccupancyStatus.None;
        public bool StateChanged =>
            PreviousSummary.State != CurrentSummary.State ||
            PreviousSummary.Token != CurrentSummary.Token;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"previous=({PreviousSummary.ToDiagnosticString()}) " +
                $"current=({CurrentSummary.ToDiagnosticString()}) message='{Message}'";
        }
    }
}
