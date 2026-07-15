using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Operational disposition derived from one P3K.6 Activity Player admission evaluation.
    /// It describes what the Activity flow may do next without resolving Player state itself.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7A pre-activation Activity Player admission flow disposition.")]
    public enum ActivityPlayerAdmissionFlowDisposition
    {
        None = 0,
        Proceed = 10,
        AwaitResolution = 20,
        RejectBlocked = 30,
        RejectFailed = 40
    }
}
