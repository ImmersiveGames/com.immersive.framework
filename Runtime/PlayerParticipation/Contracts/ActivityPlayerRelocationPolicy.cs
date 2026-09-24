namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Activity-owned contextual movement for an already admitted Session Player.
    /// Route spatial entry remains Route-owned and is not represented by this policy.
    /// </summary>
    public enum ActivityPlayerRelocationPolicy
    {
        NoRelocation = 0,
        ApplyExplicitRelocation = 1
    }
}
