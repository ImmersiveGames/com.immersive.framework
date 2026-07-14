using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 prepared Player camera eligibility operation status.")]
    public enum PlayerGameplayCameraEligibilityStatus
    {
        None = 0,

        SucceededEligible = 10,
        SucceededSkippedOptional = 20,
        SucceededReleased = 30,
        SucceededAlreadyEligible = 40,
        SucceededAlreadySkipped = 50,
        SucceededAlreadyReleased = 60,

        RejectedInvalidRequest = 100,
        RejectedSessionMismatch = 110,
        RejectedSlotNotConfigured = 120,
        RejectedPreparationNotReady = 130,
        RejectedOccupancyNotReady = 140,
        RejectedForeignOrStaleOccupancy = 150,
        RejectedInputBindingNotReady = 160,
        RejectedForeignOrStaleInputBinding = 170,
        RejectedActorMismatch = 180,
        RejectedAuthoringMissing = 190,
        RejectedAuthoringHierarchyMismatch = 200,
        RejectedRequirednessInvalid = 210,
        RejectedRigMissing = 220,
        RejectedRigHierarchyMismatch = 230,
        RejectedRigUsesPlayerComposer = 240,
        RejectedFollowTargetMissing = 250,
        RejectedFollowTargetHierarchyMismatch = 260,
        RejectedLookAtTargetHierarchyMismatch = 270,
        RejectedRigTargetMismatch = 280,
        RejectedRigConfiguration = 290,
        RejectedSlotAlreadyEvaluated = 300,
        RejectedOptionalSkipRequired = 310,
        RejectedForeignOrStaleEligibility = 320
    }
}
