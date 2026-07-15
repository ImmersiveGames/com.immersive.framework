using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Stable diagnostic classification for Activity Player admission evaluation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 Activity Player admission diagnostic code.")]
    public enum ActivityPlayerAdmissionEvaluationCode
    {
        None = 0,
        Satisfied = 10,
        MissingActivity = 20,
        MissingProjectionProfile = 30,
        MissingRequirementsProfile = 40,
        InvalidProjection = 50,
        InvalidRequirementLevel = 60,
        ContradictoryNoSlotsRequirement = 70,
        MissingParticipationSnapshot = 80,
        MissingPreparationSnapshot = 90,
        MissingGameplayAdmissionSnapshot = 100,
        SessionMismatch = 110,
        SnapshotRosterMismatch = 120,
        ZeroParticipantsRejected = 130,
        SlotNotConfigured = 140,
        InvalidSlotEvidence = 150,
        SlotNotJoined = 160,
        SlotUnavailable = 170,
        SelectedActorMissing = 180,
        PreparationMissing = 190,
        PreparationPending = 200,
        PreparationReleaseFailed = 210,
        PreparationIdentityMismatch = 220,
        GameplayAdmissionMissing = 230,
        GameplayAdmissionPending = 240,
        GameplayAdmissionBlockedByInputGate = 250,
        GameplayAdmissionReleaseFailed = 260,
        GameplayAdmissionIdentityMismatch = 270
    }
}
