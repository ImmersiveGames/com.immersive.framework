
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Describes the presentation behavior configured by a materialized camera rig.
    /// This is authoring intent only and never selects a runtime camera.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraRigPresentationIntent
    {
        Undefined = 0,
        Follow = 10,
        Fixed = 20,
        Mounted = 30,
        ThirdPerson = 40,
        Group = 50
    }
}
