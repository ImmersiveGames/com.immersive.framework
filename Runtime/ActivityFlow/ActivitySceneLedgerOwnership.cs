using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H Activity scene ledger ownership marker.")]
    internal enum ActivitySceneLedgerOwnership
    {
        Unknown = 0,
        Activity = 10
    }
}
