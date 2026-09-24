using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H immutable diagnostics snapshot for the Activity scene ledger.")]
    internal readonly struct ActivitySceneLedgerSnapshot
    {
        internal ActivitySceneLedgerSnapshot(int entries, int loaded, int released, int stale)
        {
            EntryCount = entries;
            LoadedCount = loaded;
            ReleasedCount = released;
            StaleCount = stale;
        }

        internal int EntryCount { get; }

        internal int LoadedCount { get; }

        internal int ReleasedCount { get; }

        internal int StaleCount { get; }

        internal bool HasEntries => EntryCount > 0 || LoadedCount > 0 || ReleasedCount > 0 || StaleCount > 0;

        internal string DiagnosticStatus => HasEntries ? "Available" : "Empty";
    }
}
