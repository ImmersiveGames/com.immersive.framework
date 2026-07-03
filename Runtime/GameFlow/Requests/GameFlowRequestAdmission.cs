using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F48 GameFlow request envelope local admission state; not a framework-wide status.")]
    internal enum GameFlowRequestAdmission
    {
        Unknown = 0,
        Accepted = 1,
        Rejected = 2,
        NotRequested = 3
    }
}
