
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
        RejectedActorMismatch = 140,
        RejectedAuthoringMissing = 150,
        RejectedAuthoringHierarchyMismatch = 160,
        RejectedRequirednessInvalid = 170,
        RejectedRigMissing = 180,
        RejectedRigHierarchyMismatch = 190,
        RejectedRigTargetSourceMismatch = 200,
        RejectedFollowTargetMissing = 210,
        RejectedFollowTargetHierarchyMismatch = 220,
        RejectedLookAtTargetHierarchyMismatch = 230,
        RejectedRigTargetMismatch = 240,
        RejectedRigConfiguration = 250,
        RejectedSlotAlreadyEvaluated = 260,
        RejectedOptionalSkipRequired = 270,
        RejectedForeignOrStaleEligibility = 280
    }
}
