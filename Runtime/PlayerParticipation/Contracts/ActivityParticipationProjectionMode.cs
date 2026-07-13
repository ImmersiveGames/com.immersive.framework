namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Selects the Session Player Slots evaluated by one Activity participation policy.
    /// This contract defines authoring intent only; runtime Session ownership is decided later.
    /// </summary>
    public enum ActivityParticipationProjectionMode
    {
        NoSlots = 0,
        AllJoinedSlots = 10,
        ExplicitSlots = 20
    }
}
