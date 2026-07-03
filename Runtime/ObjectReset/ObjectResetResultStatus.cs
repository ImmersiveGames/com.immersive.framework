using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate status for Object Reset foundation results.
    /// F14H covers target resolution, participant execution and no-participant completion.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset result statuses for target resolution and participant execution.")]
    public enum ObjectResetResultStatus
    {
        Unknown = 0,

        Succeeded = 10,
        SucceededNoParticipants = 20,
        CompletedWithWarnings = 30,

        RejectedInvalidRequest = 100,
        RejectedRuntimeContextUnavailable = 110,
        RejectedTargetNotFound = 120,
        RejectedForeignTarget = 130,

        Failed = 200
    }
}
