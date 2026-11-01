using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Explicit outcomes for Object Entry occurrence queries.")]
    public enum ObjectEntryOccurrenceResolutionStatus
    {
        InvalidReference = 0,
        Resolved = 10,
        Unavailable = 20,
        Ambiguous = 30
    }
}
