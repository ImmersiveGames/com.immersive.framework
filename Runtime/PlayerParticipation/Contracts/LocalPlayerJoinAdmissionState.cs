using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-046 certainty of Session admission for one Join outcome (I1).")]
    public enum LocalPlayerJoinAdmissionState
    {
        NotAdmitted = 1,
        Admitted = 2,
        Unknown = 3
    }
}
