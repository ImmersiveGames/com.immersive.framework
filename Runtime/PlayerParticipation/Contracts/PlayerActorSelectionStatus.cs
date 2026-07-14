using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3H typed Player Actor selection status.")]
    public enum PlayerActorSelectionStatus
    {
        None = 0,
        SucceededSelected = 10,
        SucceededReplaced = 20,
        SucceededCleared = 30,
        RejectedInvalidRequest = 40,
        RejectedRuntimeUnavailable = 50,
        RejectedSlotNotConfigured = 60,
        RejectedSlotNotJoined = 70,
        RejectedActorProfileMissing = 80,
        RejectedActorProfileInvalid = 90,
        RejectedStaleSelectionRevision = 100,
        RejectedDuplicateActorSelection = 110,
        RejectedLogicalActorAlreadyPrepared = 120,
        RejectedPolicyMissing = 130,
        RejectedPolicyInvalid = 140
    }
}
