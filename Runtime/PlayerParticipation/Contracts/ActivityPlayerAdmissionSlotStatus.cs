using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Per-Slot result produced by the Activity Player admission evaluator.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 per-Slot Activity admission status.")]
    public enum ActivityPlayerAdmissionSlotStatus
    {
        None = 0,
        Satisfied = 10,
        PendingResolution = 20,
        Blocked = 30,
        Failed = 40
    }
}
