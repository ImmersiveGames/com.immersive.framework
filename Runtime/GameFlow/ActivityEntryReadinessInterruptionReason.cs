using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-READY-04 typed cause for an Activity entry-readiness wait interruption.")]
    internal enum ActivityEntryReadinessInterruptionReason
    {
        None = 0,
        RouteAuthorityReplaced = 10,
        ActivityAuthorityReplaced = 20,
        ActivityAuthorityRemoved = 30,
        RuntimeDisposed = 40
    }
}
