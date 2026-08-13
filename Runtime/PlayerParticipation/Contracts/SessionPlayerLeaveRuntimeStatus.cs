using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 typed Session Player Leave foundation status.")]
    internal enum SessionPlayerLeaveRuntimeStatus
    {
        None = 0,
        SucceededLeaving = 10,
        SucceededAlreadyLeaving = 20,
        SucceededConfirmed = 30,
        SucceededActorSelectionCleared = 40,
        SucceededActorSelectionAlreadyClear = 50,
        SucceededCommitted = 60,
        RejectedInvalidRequest = 100,
        RejectedSlotNotConfigured = 110,
        RejectedSlotNotJoined = 120,
        RejectedForeignOrStaleOccurrence = 130,
        RejectedDependentState = 140,
        FailedInvariant = 200
    }
}
