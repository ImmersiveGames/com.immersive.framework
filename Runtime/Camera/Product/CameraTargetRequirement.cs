
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Describes whether a target role participates in validation/materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraTargetRequirement
    {
        NotUsed = 0,
        Optional = 10,
        Required = 20
    }
}
