using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 derived gameplay admission and readiness state.")]
    public enum PlayerGameplayAdmissionState
    {
        None = 0,
        NotAdmitted = 10,
        Ready = 20,
        BlockedByInputGate = 30,
        ReleaseFailed = 40
    }
}
