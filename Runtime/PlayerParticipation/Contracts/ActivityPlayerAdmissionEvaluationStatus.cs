using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Aggregate Activity Player admission evaluation state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 Activity Player admission evaluation status.")]
    public enum ActivityPlayerAdmissionEvaluationStatus
    {
        None = 0,
        Satisfied = 10,
        PendingResolution = 20,
        Blocked = 30,
        Failed = 40
    }
}
