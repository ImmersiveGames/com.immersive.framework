using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Result status for one synchronous reset participant invocation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset participant result status primitive.")]
    public enum ResetParticipantResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        Skipped = 20,
        Failed = 30
    }
}
