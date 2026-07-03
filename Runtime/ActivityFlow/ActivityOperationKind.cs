using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free Activity operation kind used by the F25E operation plan baseline.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation kind; planning language only.")]
    internal enum ActivityOperationKind
    {
        Unknown = 0,
        Start = 10,
        Switch = 20,
        Clear = 30,
        RouteStartup = 40,
        RouteExitCleanup = 50
    }
}
