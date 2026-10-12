using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Selects the Session Player Slots evaluated by one Activity participation policy.
    /// This contract defines authoring intent only; runtime Session ownership is decided later.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-012 Activity Player Slot projection behavior; serialized ActivityAsset data remains Stable.")]
    public enum ActivityParticipationProjectionMode
    {
        NoSlots = 0,
        AllJoinedSlots = 10,
        ExplicitSlots = 20
    }
}
