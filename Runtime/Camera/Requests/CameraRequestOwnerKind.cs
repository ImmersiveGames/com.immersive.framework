
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares the domain owner publishing camera intent.
    /// The owner does not directly control Cinemachine or an output.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraRequestOwnerKind
    {
        Undefined = 0,
        Route = 1,
        Activity = 2,
        Cutscene = 4,
        ModalPresentation = 5,
        Spectator = 6,
        Debug = 7,
        Session = 8,
        Composition = 9
    }
}
