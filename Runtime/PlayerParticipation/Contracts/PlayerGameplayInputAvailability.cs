using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 Gate-derived gameplay input availability.")]
    public enum PlayerGameplayInputAvailability
    {
        Unknown = 0,
        Allowed = 10,
        BlockedByGate = 20,
        PlayerInputDisabled = 30,
        ActionsUnavailable = 40,
        GateUnavailable = 50
    }
}
