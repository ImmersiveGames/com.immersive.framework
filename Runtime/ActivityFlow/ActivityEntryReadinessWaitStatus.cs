using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Terminal outcome for one occurrence-scoped Activity entry-readiness wait.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-ADR-007 occurrence-scoped Activity entry-readiness waiting result status.")]
    internal enum ActivityEntryReadinessWaitStatus
    {
        Unknown = 0,
        Ready = 10,
        Failed = 20,
        Invalidated = 30,
        Cancelled = 40
    }
}
