using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 Scene-Provided Session Player authority-release status.")]
    internal enum SceneProvidedSessionPlayerLeaveReleaseStatus
    {
        None = 0,
        SucceededReleased = 10,
        SucceededAlreadyReleased = 20,
        SucceededNoCurrentRepresentation = 30,
        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedLeaveCorrelation = 120,
        RejectedProvisioningMode = 130,
        RejectedActivityRepresentationActive = 140,
        RejectedContextualCorrelation = 150,
        FailedHostEvidenceRelease = 200,
        FailedHostAdmissionRelease = 210,
        FailedAssignmentRelease = 220,
        FailedContextualRecordRelease = 230,
        FailedInvariant = 240
    }
}
