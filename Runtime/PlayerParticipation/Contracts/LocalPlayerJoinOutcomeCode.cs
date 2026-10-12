using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Bounded Manager-Provisioned Join outcome codes under IF-ADR-046.")]
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
