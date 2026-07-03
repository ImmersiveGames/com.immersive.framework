using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Selects which current ObjectEntry descriptors should be reset by a reset operation.
    /// This is a policy, not a Unity side effect or participant discovery mechanism.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F40B Object Reset target selection policy for explicit and scoped reset operations.")]
    public enum ObjectResetSelectionMode
    {
        Unknown = 0,
        ExplicitTargets = 1,
        CurrentActivityEntries = 2,
        CurrentRouteEntries = 3,
        CurrentRouteAndActivityEntries = 4,
        AllCurrentEntries = 5
    }
}
