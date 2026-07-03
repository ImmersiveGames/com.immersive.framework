using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Per-entry release result status. F6F uses NotExecuted evidence only.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release result entry status vocabulary; execution deferred.")]
    internal enum ContentReleaseEntryStatus
    {
        NotExecuted = 0,
        Released = 10,
        Skipped = 20,
        Failed = 30
    }
}
