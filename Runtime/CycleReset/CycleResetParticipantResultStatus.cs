using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Per-participant result status for one Cycle Reset dispatch.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant result status primitive.")]
    public enum CycleResetParticipantResultStatus
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
