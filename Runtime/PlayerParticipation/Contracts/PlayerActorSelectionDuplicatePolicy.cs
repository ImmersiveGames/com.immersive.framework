namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session policy for comparing selected ActorProfile identities across Joined Player Slots.
    /// </summary>
    public enum PlayerActorSelectionDuplicatePolicy
    {
        Unspecified = 0,
        AllowDuplicates = 10,
        UniqueAcrossJoinedSlots = 20
    }
}
