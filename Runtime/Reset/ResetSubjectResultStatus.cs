using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Aggregate status for one subject reset execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C Reset subject result status.")]
    public enum ResetSubjectResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SkippedNoParticipants = 20,
        Failed = 30,
        FailedNoParticipants = 40,
        FailedSubjectNotFound = 50
    }
}
