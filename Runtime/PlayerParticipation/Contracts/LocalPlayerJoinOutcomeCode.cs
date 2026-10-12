using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-046 consumer-level Join disposition codes (I1).")]
    public enum LocalPlayerJoinOutcomeCode
    {
        Joined = 1,
        RejectedInvalidRequest = 10,
        RejectedBusy = 11,
        RejectedUnavailable = 12,
        RejectedConfiguration = 13,
        RejectedJoiningClosed = 14,
        RejectedNoAvailableSlot = 15,
        RejectedDeviceAlreadyOwned = 16,
        RejectedSessionChanged = 17,
        FailedAdmission = 30,
        FailedRollback = 31,
        FailedUnknown = 39
    }
}
