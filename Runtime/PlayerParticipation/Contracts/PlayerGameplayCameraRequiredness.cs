using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 per-Player camera requiredness for contextual gameplay admission.")]
    public enum PlayerGameplayCameraRequiredness
    {
        None = 0,
        Optional = 10,
        Required = 20
    }
}
