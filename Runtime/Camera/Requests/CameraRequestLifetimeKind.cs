
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares the scope that keeps a camera request eligible.
    /// Runtime enforcement belongs to CameraOutputContext in a later cut.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraRequestLifetimeKind
    {
        Undefined = 0,
        Route = 1,
        Activity = 2,
        ExplicitOperation = 4,
        SpectatorSession = 5,
        Session = 6,
        Composition = 7
    }
}
