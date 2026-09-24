using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Progressive requirement that is absent or invalid for one projected Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 missing Activity Player admission requirement evidence.")]
    public enum ActivityPlayerAdmissionMissingRequirement
    {
        None = 0,
        JoinedSlot = 10,
        SelectedActor = 20,
        LogicalActorPrepared = 30,
        GameplayReady = 40
    }
}
