using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Bounded Manager-Provisioned Join admission state under IF-ADR-046.")]
    public enum LocalPlayerJoinAdmissionState
    {
        NotAdmitted = 1,
        Admitted = 2,
        Unknown = 3
    }
}
