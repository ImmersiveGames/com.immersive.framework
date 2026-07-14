using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 typed gameplay input binding state.")]
    public enum PlayerGameplayInputBindingState
    {
        None = 0,
        Unbound = 10,
        Bound = 20,
        ReleaseFailed = 30
    }
}
