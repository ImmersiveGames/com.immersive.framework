using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Aggregate result status for one Cycle Reset execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset aggregate result status primitive.")]
    public enum CycleResetStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoParticipants = 20,
        CompletedWithWarnings = 30,
        Failed = 100,
        RejectedInvalidRequest = 110,
        RejectedInvalidPlan = 120
    }
}
