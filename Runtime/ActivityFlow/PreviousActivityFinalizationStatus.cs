using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Finalization state retained independently from current Activity authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ARCH-A2 previous Activity finalization status.")]
    internal enum PreviousActivityFinalizationStatus
    {
        Unknown = 0,
        NotRequired = 10,
        Pending = 20,
        Succeeded = 30,
        Failed = 40
    }
}
