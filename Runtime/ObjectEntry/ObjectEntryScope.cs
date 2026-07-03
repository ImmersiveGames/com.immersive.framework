using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Lifecycle scope where an object entry participates.
    /// This is not a Unity hierarchy scope and does not imply GameObject ownership.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry scope primitive introduced by F13A.")]
    public enum ObjectEntryScope
    {
        Unspecified = 0,
        Session = 10,
        Route = 20,
        Activity = 30
    }
}
