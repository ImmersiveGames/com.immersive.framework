
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Describes whether a target role participates in validation/materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraTargetRequirement
    {
        NotUsed = 0,
        Optional = 10,
        Required = 20
    }
}
