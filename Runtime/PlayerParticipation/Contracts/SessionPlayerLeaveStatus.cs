using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Public outcome vocabulary for the canonical ADR-020 Session Player Leave operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-020 canonical Session Player Leave orchestration status.")]
    public enum SessionPlayerLeaveStatus
    {
        None = 0,
        SucceededLeft = 10,
        SucceededAlreadyLeft = 20,
        RejectedRuntimeUnavailable = 100,
        RejectedInvalidRequest = 110,
        RejectedSlotNotConfigured = 120,
        RejectedSlotNotJoined = 130,
        RejectedForeignOrStaleOccurrence = 140,
        RejectedProvisioningMode = 150,
        FailedActivityRepresentationRelease = 200,
        FailedProvisioningRelease = 210,
        FailedTerminalCommit = 220,
        FailedInvariant = 230
    }
}
