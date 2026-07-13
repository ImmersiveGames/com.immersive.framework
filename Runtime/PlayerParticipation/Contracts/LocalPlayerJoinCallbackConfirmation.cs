
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 PlayerInputManager callback correlation evidence.")]
    public enum LocalPlayerJoinCallbackConfirmation
    {
        None = 0,
        Pending = 1,
        ConfirmedSamePlayerInput = 2,
        RejectedUnexpectedCallback = 3,
        RejectedDifferentPlayerInput = 4
    }
}
