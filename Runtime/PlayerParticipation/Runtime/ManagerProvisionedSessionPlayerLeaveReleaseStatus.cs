using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 Manager-Provisioned Session Player physical resource release status.")]
    internal enum ManagerProvisionedSessionPlayerLeaveReleaseStatus
    {
        None = 0,
        SucceededReleased = 10,
        SucceededAlreadyReleased = 20,
        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedLeaveCorrelation = 120,
        RejectedHostEvidenceRelease = 130,
        RejectedAssignmentCorrelation = 140,
        RejectedAssignmentOrigin = 150,
        RejectedHostCorrelation = 160,
        RejectedPlayerNotAdmitted = 170,
        RejectedActivityRepresentationActive = 180,
        RejectedReleaseBackendUnavailable = 190,
        FailedHostAdmissionRelease = 200,
        FailedPhysicalRelease = 210,
        FailedInvariant = 220
    }
}
