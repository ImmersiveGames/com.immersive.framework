using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-READY-04 Activity entry-readiness orchestration terminal status.")]
    internal enum ActivityEntryReadinessExecutionStatus
    {
        Unknown = 0,
        ObserveOnly = 10,
        Ready = 20,
        Failed = 30,
        Invalidated = 40,
        Cancelled = 50,
        RejectedInvalidConfiguration = 60
    }
}
