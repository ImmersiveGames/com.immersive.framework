using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Logical reset-selection scope for a reset subject.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset subject selection scope.")]
    public enum ResetSubjectScope
    {
        Unknown = 0,
        Route = 10,
        Activity = 20,
        Runtime = 30
    }
}
