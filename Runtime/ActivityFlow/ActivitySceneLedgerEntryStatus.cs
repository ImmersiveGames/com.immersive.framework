using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H Activity scene ledger entry state.")]
    internal enum ActivitySceneLedgerEntryStatus
    {
        Unknown = 0,
        Loaded = 10,
        Released = 20,
        Stale = 30
    }
}
