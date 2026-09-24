namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Progressive Activity admission requirement for projected Player Slots.
    /// Every level includes the guarantees of the previous level.
    /// </summary>
    public enum PlayerParticipationRequirementLevel
    {
        None = 0,
        JoinedSlots = 10,
        SelectedActors = 20,
        LogicalActorsPrepared = 30,
        GameplayReady = 40
    }
}
