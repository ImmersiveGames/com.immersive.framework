using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local operation evidence kind; not a framework result/status replacement.")]
    internal enum FrameworkLifecycleOperationKind
    {
        Unknown = 0,
        Route = 10,
        Activity = 20,
        RouteStartup = 30,
        ActivityClear = 40
    }
}
