using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "ADR-024 typed public prepared Actor replacement result.")]
    public sealed class PlayerPreparedActorReplacementResult
    {
        internal PlayerPreparedActorReplacementResult(PlayerPreparedActorReplacementStatus status, PlayerSlotId slot, PlayerActorPreparationSummary previousActor, PlayerActorPreparationSummary currentActor, PlayerGameplayAdmissionSummary previousGameplay, PlayerGameplayAdmissionSummary currentGameplay, int activityOccurrence, bool committed, bool gameplayReprojected, bool cleanupPending, string message)
        {
            Status = status; PlayerSlotId = slot; PreviousActor = previousActor; CurrentActor = currentActor; PreviousGameplay = previousGameplay; CurrentGameplay = currentGameplay; ActivityOccurrence = activityOccurrence; ReplacementCommitted = committed; GameplayReprojected = gameplayReprojected; CleanupPending = cleanupPending; Message = message ?? string.Empty;
        }
        public PlayerPreparedActorReplacementStatus Status { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerActorPreparationSummary PreviousActor { get; }
        public PlayerActorPreparationSummary CurrentActor { get; }
        public PlayerGameplayAdmissionSummary PreviousGameplay { get; }
        public PlayerGameplayAdmissionSummary CurrentGameplay { get; }
        public int ActivityOccurrence { get; }
        public bool ReplacementCommitted { get; }
        public bool GameplayReprojected { get; }
        public bool CleanupPending { get; }
        public string Message { get; }
        public bool Rejected => !ReplacementCommitted && Status is PlayerPreparedActorReplacementStatus.RejectedInvalidRequest or PlayerPreparedActorReplacementStatus.RejectedRuntimeUnavailable or PlayerPreparedActorReplacementStatus.RejectedStalePublicRevision or PlayerPreparedActorReplacementStatus.RejectedNoActiveActivity or PlayerPreparedActorReplacementStatus.RejectedUnsupportedProvisioning or PlayerPreparedActorReplacementStatus.RejectedPreparedActorUnavailable;
    }
}
