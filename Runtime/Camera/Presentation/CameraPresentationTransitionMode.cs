using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Authored transition intent for entering a Camera Presentation.
    /// Blend preserves the physical Output Brain's authored blend policy.
    /// Cut enters the winning Presentation immediately.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-B authored Camera Presentation entry transition.")]
    public enum CameraPresentationTransitionMode
    {
        Undefined = 0,
        Blend = 10,
        Cut = 20
    }
}
