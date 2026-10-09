using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-044 consumer gameplay-availability block operation result.")]
    public sealed class PlayerGameplayAvailabilityBlockResult
    {
        internal PlayerGameplayAvailabilityBlockResult(
            PlayerGameplayAvailabilityBlockStatus status,
            PlayerSlotId playerSlotId,
            PlayerGameplayAvailabilityBlockToken blockToken,
            string message)
        {
            Status = status;
            PlayerSlotId = playerSlotId;
            BlockToken = blockToken;
            Message = message ?? string.Empty;
        }

        public PlayerGameplayAvailabilityBlockStatus Status { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayAvailabilityBlockToken BlockToken { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerGameplayAvailabilityBlockStatus.SucceededBlocked or
            PlayerGameplayAvailabilityBlockStatus.SucceededReleased;

        internal static PlayerGameplayAvailabilityBlockResult Failure(
            PlayerGameplayAvailabilityBlockStatus status,
            PlayerSlotId playerSlotId,
            string message) =>
            new(status, playerSlotId, default, message);
    }
}
