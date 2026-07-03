using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Per-participant result status for one Object Reset dispatch.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant result status primitive.")]
    public enum ObjectResetParticipantResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SkippedNotApplicable = 20,
        SkippedOptional = 30,
        FailedNonBlocking = 100,
        FailedBlocking = 110,
        RejectedInvalidRequest = 120
    }
}
