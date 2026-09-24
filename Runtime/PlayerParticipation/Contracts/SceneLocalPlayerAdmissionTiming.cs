using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Product timing for one scene-authored local Player admission surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4 scene local Player admission timing intent.")]
    public enum SceneLocalPlayerAdmissionTiming
    {
        OnActivityEnter = 0,
        Manual = 10
    }
}
