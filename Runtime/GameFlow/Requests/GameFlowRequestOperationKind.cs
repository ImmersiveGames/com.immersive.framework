using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F48 GameFlow request envelope local operation kind; not a framework-wide status.")]
    internal enum GameFlowRequestOperationKind
    {
        Unknown = 0,
        Route = 1,
        Activity = 2,
        ActivityClear = 3,
        Startup = 4
    }
}
