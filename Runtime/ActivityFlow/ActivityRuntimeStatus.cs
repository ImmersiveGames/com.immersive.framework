using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Explicit Activity runtime state status. F4A uses None and Active; Transitioning is reserved
    /// for later cuts that introduce multi-step Activity content lifecycle results.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Activity runtime state boundary introduced by F4A; not game-facing API.")]
    internal enum ActivityRuntimeStatus
    {
        None = 0,
        Active = 10,
        Transitioning = 20
    }
}
