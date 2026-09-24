using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive planning status for a Cycle Reset operation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset plan status primitive.")]
    public enum CycleResetPlanStatus
    {
        Unknown = 0,
        Planned = 10,
        SkippedNoParticipants = 20,
        RejectedInvalidRequest = 100,
        RejectedInvalidParticipants = 110
    }
}
