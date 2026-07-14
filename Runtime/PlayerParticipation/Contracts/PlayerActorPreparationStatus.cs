using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Outcome vocabulary for Session-scoped Logical Player Actor preparation operations.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 Session Logical Player Actor preparation operation status.")]
    public enum PlayerActorPreparationStatus
    {
        None = 0,
        SucceededPrepared = 10,
        SucceededReleased = 20,
        SucceededReplaced = 30,
        SucceededAlreadyPrepared = 40,
        SucceededAlreadyReleased = 50,
        RejectedInvalidRequest = 100,
        RejectedSlotNotConfigured = 110,
        RejectedSlotNotJoined = 120,
        RejectedActorSelectionMissing = 130,
        RejectedHostUnavailable = 140,
        RejectedHostSlotMismatch = 150,
        RejectedPreparedActorConflict = 160,
        RejectedForeignOrStalePreparation = 170,
        RejectedSelectionMutationWhilePrepared = 180,
        RejectedScopeMismatch = 190,
        FailedMaterialization = 200,
        FailedActivation = 210,
        FailedSelectionCommit = 220,
        FailedRelease = 230,
        FailedRollback = 240,
        FailedPreviousRelease = 250
    }
}
